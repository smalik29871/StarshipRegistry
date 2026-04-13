using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StarshipRegistry.Data;
using StarshipRegistry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StarshipRegistry.Helpers
{
    public class StarshipQueryHelper
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _groqUrl;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StarshipQueryHelper> _logger;
        private readonly IMemoryCache _cache;

        private const string SystemPrompt = @"You are a query parser for a Star Wars starship database.
Convert the user's natural language query into a JSON object with these fields:
- SortBy: one of 'cost', 'crew', 'hyperdrive', 'length', 'cargo' — or empty string if not a sort query
- Order: 'asc' or 'desc'
- Take: number of results (default 10)
- Concept: a short keyword or phrase for semantic search if not a sort query, otherwise empty string

Important Star Wars context:
- Hyperdrive rating: LOWER number = FASTER (so 'fastest hyperdrive' = asc, 'slowest' = desc)
- Cost, crew, length, cargo: higher number = more

Respond ONLY with raw JSON, no markdown, no explanation.";

        public StarshipQueryHelper(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ApplicationDbContext context,
            ILogger<StarshipQueryHelper> logger,
            IMemoryCache cache)
        {
            _http = httpClientFactory.CreateClient();
            _apiKey = config["Groq:ApiKey"] ?? string.Empty;
            _model = config["Groq:Model"] ?? "llama-3.1-8b-instant";
            _groqUrl = config["Groq:BaseUrl"] ?? string.Empty;
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<SearchCommand> ParseQueryAsync(string userQuery)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_groqUrl))
                return new SearchCommand { Concept = userQuery };

            var cacheKey = $"groq:{userQuery.Trim().ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out SearchCommand? cached) && cached != null)
                return cached;

            try
            {
                var payload = new
                {
                    model = _model,
                    messages = new[] { new { role = "system", content = SystemPrompt }, new { role = "user", content = userQuery } },
                    temperature = 0,
                    max_tokens = 100,
                    response_format = new { type = "json_object" }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, _groqUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead)
                    .WaitAsync(TimeSpan.FromSeconds(10));
                if (!response.IsSuccessStatusCode) return new SearchCommand { Concept = userQuery };

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";

                var result = JsonSerializer.Deserialize<SearchCommand>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new SearchCommand { Concept = userQuery };

                if (result.Take <= 0) result.Take = 10;

                _cache.Set(cacheKey, result, TimeSpan.FromHours(1));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse query via Groq API. Defaulting to standard concept search.");
                return new SearchCommand { Concept = userQuery };
            }
        }

        public async Task<List<Starship>> ExecuteQueryAsync(SearchCommand command)
        {
            var query = _context.Starships.AsQueryable();

            query = command.SortBy switch
            {
                "cost"       => query.Where(s => s.CostInCredits    != null && s.CostInCredits    != "unknown" && s.CostInCredits    != "n/a"),
                "crew"       => query.Where(s => s.Crew             != null && s.Crew             != "unknown" && s.Crew             != "n/a"),
                "hyperdrive" => query.Where(s => s.HyperdriveRating != null && s.HyperdriveRating != "unknown" && s.HyperdriveRating != "n/a"),
                "length"     => query.Where(s => s.Length           != null && s.Length           != "unknown" && s.Length           != "n/a"),
                "cargo"      => query.Where(s => s.CargoCapacity    != null && s.CargoCapacity    != "unknown" && s.CargoCapacity    != "n/a"),
                _ => query
            };

            const int maxMemorySafeCap = 1000;
            var filteredShips = await query.Take(maxMemorySafeCap).ToListAsync();

            Func<Starship, string?> selector = command.SortBy switch
            {
                "cost" => s => s.CostInCredits,
                "crew" => s => s.Crew,
                "hyperdrive" => s => s.HyperdriveRating,
                "length" => s => s.Length,
                "cargo" => s => s.CargoCapacity,
                _ => s => s.Name
            };

            var sorted = filteredShips
                .OrderBy(s => double.TryParse(selector(s), out var num) ? num : 0);

            var finalQuery = command.Order == "desc" ? sorted.Reverse() : sorted;
            return finalQuery.Take(command.Take).ToList();
        }

        public Task<List<Starship>> FindByNumericValueAsync(string value, int take = 50)
        {
            return _context.Starships
                .Where(s =>
                    (s.CostInCredits        != null && s.CostInCredits.Contains(value)) ||
                    (s.Crew                 != null && s.Crew.Contains(value)) ||
                    (s.Passengers           != null && s.Passengers.Contains(value)) ||
                    (s.Length               != null && s.Length.Contains(value)) ||
                    (s.HyperdriveRating     != null && s.HyperdriveRating.Contains(value)) ||
                    (s.CargoCapacity        != null && s.CargoCapacity.Contains(value)) ||
                    (s.Mglt                 != null && s.Mglt.Contains(value)) ||
                    (s.MaxAtmospheringSpeed != null && s.MaxAtmospheringSpeed.Contains(value)))
                .Take(take)
                .ToListAsync();
        }

        public Task<List<Starship>> FindByTextAsync(string text, int take = 50)
        {
            return _context.Starships
                .Where(s =>
                    s.Name.Contains(text) ||
                    s.Model.Contains(text) ||
                    s.StarshipClass.Contains(text) ||
                    s.Manufacturer.Contains(text) ||
                    (s.CostInCredits        != null && s.CostInCredits.Contains(text)) ||
                    (s.Crew                 != null && s.Crew.Contains(text)) ||
                    (s.Passengers           != null && s.Passengers.Contains(text)) ||
                    (s.Length               != null && s.Length.Contains(text)) ||
                    (s.HyperdriveRating     != null && s.HyperdriveRating.Contains(text)) ||
                    (s.CargoCapacity        != null && s.CargoCapacity.Contains(text)) ||
                    (s.Mglt                 != null && s.Mglt.Contains(text)) ||
                    (s.MaxAtmospheringSpeed != null && s.MaxAtmospheringSpeed.Contains(text)) ||
                    (s.Consumables          != null && s.Consumables.Contains(text)))
                .Take(take)
                .ToListAsync();
        }

        public List<object> MapToRows(List<Starship> ships)
        {
            return ships.Select(s => (object)new
            {
                id = s.Url.TrimEnd('/').Split('/').Last(),
                name = s.Name,
                model = s.Model,
                starshipClass = s.StarshipClass,
                costInCredits = s.CostInCredits ?? "N/A",
                crew = s.Crew ?? "N/A",
                hyperdriveRating = s.HyperdriveRating ?? "N/A",
                created = s.Created?.ToString("yyyy-MM-dd") ?? "N/A"
            }).ToList();
        }
    }
}
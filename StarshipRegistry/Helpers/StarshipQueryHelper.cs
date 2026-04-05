using Microsoft.EntityFrameworkCore;
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
            ILogger<StarshipQueryHelper> logger)
        {
            _http = httpClientFactory.CreateClient();
            _apiKey = config["Groq:ApiKey"] ?? string.Empty;
            _model = config["Groq:Model"] ?? "llama-3.1-8b-instant";
            _groqUrl = config["Groq:BaseUrl"] ?? string.Empty;
            _context = context;
            _logger = logger;
        }

        public async Task<SearchCommand> ParseQueryAsync(string userQuery)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_groqUrl))
                return new SearchCommand { Concept = userQuery };

            try
            {
                var payload = new
                {
                    model = _model,
                    messages = new[] { new { role = "system", content = SystemPrompt }, new { role = "user", content = userQuery } },
                    temperature = 0,
                    max_tokens = 100
                };

                var request = new HttpRequestMessage(HttpMethod.Post, _groqUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode) return new SearchCommand { Concept = userQuery };

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";

                // Pull out the pure JSON block securely
                int start = content.IndexOf('{');
                int end = content.LastIndexOf('}');
                if (start >= 0 && end > start)
                    content = content.Substring(start, end - start + 1);

                return JsonSerializer.Deserialize<SearchCommand>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new SearchCommand { Concept = userQuery };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse query via Groq API. Defaulting to standard concept search.");
                return new SearchCommand { Concept = userQuery };
            }
        }

        public async Task<List<Starship>> ExecuteQueryAsync(SearchCommand command)
        {
            // 1. Start with the database queryable
            var query = _context.Starships.AsQueryable();

            // 2. Narrow down records at the DB level by removing nulls/unknowns for the sorted column
            query = command.SortBy switch
            {
                "cost" => query.Where(s => s.CostInCredits != null && s.CostInCredits != "unknown"),
                "crew" => query.Where(s => s.Crew != null && s.Crew != "unknown"),
                "hyperdrive" => query.Where(s => s.HyperdriveRating != null && s.HyperdriveRating != "unknown"),
                "length" => query.Where(s => s.Length != null && s.Length != "unknown"),
                "cargo" => query.Where(s => s.CargoCapacity != null && s.CargoCapacity != "unknown"),
                _ => query
            };

            // 3. APPLY SAFE UPPER LIMIT
            // If the database scales to 1,000,000 ships, this keeps the app from crashing.
            const int maxMemorySafeCap = 1000;

            var filteredShips = await query
                .Take(maxMemorySafeCap)
                .ToListAsync(); // Database execution ends here

            // 4. Map the sort target to a local property selector
            Func<Starship, string?> selector = command.SortBy switch
            {
                "cost" => s => s.CostInCredits,
                "crew" => s => s.Crew,
                "hyperdrive" => s => s.HyperdriveRating,
                "length" => s => s.Length,
                "cargo" => s => s.CargoCapacity,
                _ => s => s.Name
            };

            // 5. Safely perform numeric sorting on strings in-memory on the restricted set
            var sorted = filteredShips
                .OrderBy(s => double.TryParse(selector(s), out var num) ? num : 0);

            var finalQuery = command.Order == "desc" ? sorted.Reverse() : sorted;

            // 6. Take the requested amount requested by the AI/user
            return finalQuery.Take(command.Take).ToList();
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
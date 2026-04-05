using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StarshipRegistry.Data;
using StarshipRegistry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        // Use a dictionary to map the SortKey to the specific property on the Starship entity
        private static readonly Dictionary<string, Expression<Func<Starship, string?>>> SortMappers = new()
        {
            { "cost", s => s.CostInCredits },
            { "crew", s => s.Crew },
            { "hyperdrive", s => s.HyperdriveRating },
            { "length", s => s.Length },
            { "cargo", s => s.CargoCapacity }
        };

        private const string SystemPrompt = @"You are a query parser for a Star Wars starship database.
Convert the user's natural language query into a JSON object with these fields:
- SortBy: one of 'cost', 'crew', 'hyperdrive', 'length', 'cargo' — or empty string if not a sort query
- Order: 'asc' or 'desc'
- Take: number of results (default 10)
- Concept: a short keyword or phrase for semantic search if not a sort query, otherwise empty string

Important Star Wars context:
- Hyperdrive rating: LOWER number = FASTER (so 'fastest hyperdrive' = asc, 'slowest' = desc)
- Cost, crew, length, cargo: higher number = more

Respond ONLY with raw JSON, no markdown, no explanation.
Example: {""SortBy"": ""crew"", ""Order"": ""desc"", ""Take"": 10, ""Concept"": """"}";

        public StarshipQueryHelper(IHttpClientFactory httpClientFactory, IConfiguration config, ApplicationDbContext context)
        {
            _http = httpClientFactory.CreateClient();
            _apiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey is not configured.");
            _model = config["Groq:Model"] ?? "llama-3.1-8b-instant";
            _groqUrl = config["Groq:BaseUrl"] ?? throw new InvalidOperationException("Groq:BaseUrl is not configured.");
            _context = context;
        }

        public async Task<SearchCommand> ParseQueryAsync(string userQuery)
        {
            try
            {
                var payload = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = SystemPrompt },
                        new { role = "user", content = userQuery }
                    },
                    temperature = 0,
                    max_tokens = 100
                };

                var request = new HttpRequestMessage(HttpMethod.Post, _groqUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _http.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                int start = content.IndexOf('{');
                int end = content.LastIndexOf('}');
                if (start >= 0 && end > start)
                    content = content.Substring(start, end - start + 1);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<SearchCommand>(content, options)
                    ?? new SearchCommand { Concept = userQuery };
            }
            catch
            {
                return new SearchCommand { Concept = userQuery };
            }
        }

        public async Task<List<Starship>> ExecuteSortQueryAsync(SearchCommand command)
        {
            // 1. Guard clause
            if (string.IsNullOrEmpty(command.SortBy) || !SortMappers.ContainsKey(command.SortBy))
                return new List<Starship>();

            var propertySelector = SortMappers[command.SortBy];
            var isDescending = command.Order == "desc";

            // 2. Fetch data from DB. 
            // Because SWAPI uses strings for numeric fields (e.g., "1000" or "unknown"), 
            // we pull them into memory so we can safely parse them as numbers without blowing up EF Core.
            var rawShips = await _context.Starships.ToListAsync();

            // 3. Compile the selector into a delegate so we can invoke it in memory
            var compiledSelector = propertySelector.Compile();

            var query = rawShips
                .Where(s => {
                    var val = compiledSelector(s);
                    return !string.IsNullOrEmpty(val) && val != "unknown";
                })
                .OrderBy(s => {
                    var val = compiledSelector(s);
                    return double.TryParse(val, out var num) ? num : 0;
                });

            // Apply descending if necessary
            var finalQuery = isDescending ? query.Reverse() : query;

            return finalQuery.Take(command.Take).ToList();
        }

        public List<object> MapToRows(List<Starship> ships) =>
            ships.Select(s => (object)new
            {
                id = s.Url.TrimEnd('/').Split('/').Last(),
                name = s.Name,
                model = s.Model,
                starshipClass = s.StarshipClass,
                costInCredits = string.IsNullOrEmpty(s.CostInCredits) ? "N/A" : s.CostInCredits,
                crew = string.IsNullOrEmpty(s.Crew) ? "N/A" : s.Crew,
                hyperdriveRating = string.IsNullOrEmpty(s.HyperdriveRating) ? "N/A" : s.HyperdriveRating,
                created = s.Created.HasValue ? s.Created.Value.ToString("yyyy-MM-dd") : "N/A"
            }).ToList();
    }
}
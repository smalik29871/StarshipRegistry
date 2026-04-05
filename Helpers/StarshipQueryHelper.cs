using Microsoft.Extensions.Configuration;
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

        public StarshipQueryHelper(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _http = httpClientFactory.CreateClient();
            _apiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey is not configured.");
            _model = config["Groq:Model"] ?? "llama-3.1-8b-instant";
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

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
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

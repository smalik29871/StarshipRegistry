using System;
using System.Text.Json.Serialization;

namespace StarshipRegistry.Models
{
    public class StarshipSwapiResponse<T>
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("previous")]
        public string? Previous { get; set; }

        [JsonPropertyName("results")]
        public T[] Results { get; set; } = Array.Empty<T>();
    }
}
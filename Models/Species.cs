using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using StarshipRegistry.Helpers;

namespace StarshipRegistry.Models
{
    public class Species : ITimestampedEntity 
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("classification")]
        public string Classification { get; set; } = string.Empty;

        [JsonPropertyName("designation")]
        public string Designation { get; set; } = string.Empty;

        [JsonPropertyName("average_height")]
        public string AverageHeight { get; set; } = string.Empty;

        [JsonPropertyName("skin_colors")]
        public string SkinColors { get; set; } = string.Empty;

        [JsonPropertyName("hair_colors")]
        public string HairColors { get; set; } = string.Empty;

        [JsonPropertyName("eye_colors")]
        public string EyeColors { get; set; } = string.Empty;

        [JsonPropertyName("average_lifespan")]
        public string AverageLifespan { get; set; } = string.Empty;

        [JsonPropertyName("homeworld")]
        public string? Homeworld { get; set; } = string.Empty;

        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("people")]
        public List<string> People { get; set; } = new();

        [JsonPropertyName("films")]
        public List<string> Films { get; set; } = new();

        [JsonPropertyName("url")]
        [Key]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("created")]
        [JsonConverter(typeof(SwapiDateTimeConverter))]
        public DateTime? Created { get; set; }

        [JsonPropertyName("edited")]
        [JsonConverter(typeof(SwapiDateTimeConverter))]
        public DateTime? Edited { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using StarshipRegistry.Helpers;

namespace StarshipRegistry.Models
{
    public class Character : ISwapiEntity, ITimestampedEntity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("height")]
        public string Height { get; set; } = string.Empty;

        [JsonPropertyName("mass")]
        public string Mass { get; set; } = string.Empty;

        [JsonPropertyName("hair_color")]
        public string HairColor { get; set; } = string.Empty;

        [JsonPropertyName("skin_color")]
        public string SkinColor { get; set; } = string.Empty;

        [JsonPropertyName("eye_color")]
        public string EyeColor { get; set; } = string.Empty;

        [JsonPropertyName("birth_year")]
        public string BirthYear { get; set; } = string.Empty;

        [JsonPropertyName("gender")]
        public string Gender { get; set; } = string.Empty;

        [JsonPropertyName("films")]
        public List<string> Films { get; set; } = new();

        [JsonPropertyName("starships")]
        public List<string> Starships { get; set; } = new();

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

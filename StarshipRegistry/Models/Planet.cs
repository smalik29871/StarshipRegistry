using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using StarshipRegistry.Helpers;

namespace StarshipRegistry.Models
{
    public class Planet : ISwapiEntity, ITimestampedEntity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("rotation_period")]
        public string RotationPeriod { get; set; } = string.Empty;

        [JsonPropertyName("orbital_period")]
        public string OrbitalPeriod { get; set; } = string.Empty;

        [JsonPropertyName("diameter")]
        public string Diameter { get; set; } = string.Empty;

        [JsonPropertyName("climate")]
        public string Climate { get; set; } = string.Empty;

        [JsonPropertyName("gravity")]
        public string Gravity { get; set; } = string.Empty;

        [JsonPropertyName("terrain")]
        public string Terrain { get; set; } = string.Empty;

        [JsonPropertyName("surface_water")]
        public string SurfaceWater { get; set; } = string.Empty;

        [JsonPropertyName("population")]
        public string Population { get; set; } = string.Empty;

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

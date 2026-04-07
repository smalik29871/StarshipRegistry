using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using StarshipRegistry.Helpers;

namespace StarshipRegistry.Models
{
    public class Starship : ISwapiEntity, ITimestampedEntity
    {
        [Key]
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; } = string.Empty;

        [JsonPropertyName("cost_in_credits")]
        public string? CostInCredits { get; set; }

        [JsonPropertyName("length")]
        public string? Length { get; set; }

        [JsonPropertyName("crew")]
        public string? Crew { get; set; }

        [JsonPropertyName("passengers")]
        public string? Passengers { get; set; }

        [JsonPropertyName("hyperdrive_rating")]
        public string? HyperdriveRating { get; set; }

        [StringLength(100)]
        [JsonPropertyName("starship_class")]
        public string StarshipClass { get; set; } = string.Empty;

        [JsonPropertyName("cargo_capacity")]
        public string? CargoCapacity { get; set; }

        [JsonPropertyName("consumables")]
        public string? Consumables { get; set; }

        [JsonPropertyName("MGLT")]
        public string? Mglt { get; set; }

        [JsonPropertyName("max_atmosphering_speed")]
        public string? MaxAtmospheringSpeed { get; set; }

        [JsonPropertyName("pilots")]
        public List<string> Pilots { get; set; } = new();

        [JsonPropertyName("films")]
        public List<string> Films { get; set; } = new();

        [JsonPropertyName("created")]
        [JsonConverter(typeof(SwapiDateTimeConverter))]
        public DateTime? Created { get; set; }

        [JsonPropertyName("edited")]
        [JsonConverter(typeof(SwapiDateTimeConverter))]
        public DateTime? Edited { get; set; }

        [JsonIgnore]
        public bool IsSeeded { get; set; } = false;
    }
}

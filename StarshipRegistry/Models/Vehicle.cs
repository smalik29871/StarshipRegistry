using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using StarshipRegistry.Helpers;

namespace StarshipRegistry.Models
{
    public class Vehicle : ITimestampedEntity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; } = string.Empty;

        [JsonPropertyName("cost_in_credits")]
        public string CostInCredits { get; set; } = string.Empty;

        [JsonPropertyName("length")]
        public string Length { get; set; } = string.Empty;

        [JsonPropertyName("max_atmosphering_speed")]
        public string MaxAtmospheringSpeed { get; set; } = string.Empty;

        [JsonPropertyName("crew")]
        public string Crew { get; set; } = string.Empty;

        [JsonPropertyName("passengers")]
        public string Passengers { get; set; } = string.Empty;

        [JsonPropertyName("cargo_capacity")]
        public string CargoCapacity { get; set; } = string.Empty;

        [JsonPropertyName("vehicle_class")]
        public string VehicleClass { get; set; } = string.Empty;

        [JsonPropertyName("pilots")]
        public List<string> Pilots { get; set; } = new();

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
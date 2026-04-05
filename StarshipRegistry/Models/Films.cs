using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using StarshipRegistry.Helpers;

namespace StarshipRegistry.Models
{
    public class Film : ITimestampedEntity
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("episode_id")]
        public int EpisodeId { get; set; }

        [JsonPropertyName("opening_crawl")]
        public string OpeningCrawl { get; set; } = string.Empty;

        [JsonPropertyName("director")]
        public string Director { get; set; } = string.Empty;

        [JsonPropertyName("producer")]
        public string Producer { get; set; } = string.Empty;

        [JsonPropertyName("release_date")]
        public string ReleaseDate { get; set; } = string.Empty;

        [JsonPropertyName("characters")]
        public List<string> Characters { get; set; } = new();

        [JsonPropertyName("planets")]
        public List<string> Planets { get; set; } = new();

        [JsonPropertyName("starships")]
        public List<string> Starships { get; set; } = new();

        [JsonPropertyName("vehicles")]
        public List<string> Vehicles { get; set; } = new();

        [JsonPropertyName("species")]
        public List<string> Species { get; set; } = new();

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
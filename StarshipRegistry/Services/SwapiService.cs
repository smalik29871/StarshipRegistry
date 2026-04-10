using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarshipRegistry.Configuration;
using StarshipRegistry.Data;
using StarshipRegistry.Helpers;
using StarshipRegistry.Models;

namespace StarshipRegistry.Services
{
    public class SwapiService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SwapiService> _logger;
        private readonly SwapiSettings _swapiSettings;
        private readonly JsonSerializerOptions _jsonOptions;

        public SwapiService(HttpClient httpClient, ApplicationDbContext context, ILogger<SwapiService> logger, IOptions<SwapiSettings> swapiSettings)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
            _swapiSettings = swapiSettings.Value;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _jsonOptions.Converters.Add(new SwapiDateTimeConverter());
        }

        #region LIVE LOOKUPS FOR UI

        public async Task<T?> FetchFromApiAsync<T>(string url) where T : class
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(jsonString, _jsonOptions);
                }

                _logger.LogWarning("API call to {Url} failed with status code {StatusCode}", url?.Replace('\r', '_').Replace('\n', '_'), response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while fetching resource from {Url}", url?.Replace('\r', '_').Replace('\n', '_'));
            }
            return null;
        }

        #endregion

        #region SEEDING & BULK SYNCING

        public async Task SyncAllDataAsync()
        {
            await SyncResourceAsync<Film>($"{_swapiSettings.BaseUrl}films", MapToEntity);
            await SyncResourceAsync<Planet>($"{_swapiSettings.BaseUrl}planets", MapToEntity);

            await Task.Delay(1000);

            await SyncResourceAsync<Character>($"{_swapiSettings.BaseUrl}people", MapToEntity);
            await SyncResourceAsync<Species>($"{_swapiSettings.BaseUrl}species", MapToEntity);
            await SyncResourceAsync<Starship>($"{_swapiSettings.BaseUrl}starships", MapToEntity);
        }

        private async Task SyncResourceAsync<T>(string url, Action<T, Dictionary<string, T>> mapAction) where T : class, ISwapiEntity, ITimestampedEntity
        {
            try
            {
                _context.ChangeTracker.Clear();

                var jsonString = await _httpClient.GetStringAsync(url);
                var results = JsonSerializer.Deserialize<List<T>>(jsonString, _jsonOptions);
                if (results == null) return;

                var existingList = await _context.Set<T>().ToListAsync();
                var existing = existingList.ToDictionary(e => e.Url);

                foreach (var item in results)
                    mapAction(item, existing);

                await _context.SaveRawAsync();
                _logger.LogInformation("Successfully synced {Count} items from {Url}", results.Count, url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing {Url}", url);
            }
        }
        #endregion

        #region PRIVATE MAPPERS

        private void MapToEntity(Starship apiData, Dictionary<string, Starship> existing)
        {
            if (existing.TryGetValue(apiData.Url, out var tracked))
            {
                if (tracked.Edited.HasValue && apiData.Edited.HasValue && apiData.Edited <= tracked.Edited)
                    return;

                _context.Entry(tracked).CurrentValues.SetValues(apiData);
                tracked.Pilots = apiData.Pilots;
                tracked.Films = apiData.Films;
            }
            else
            {
                _context.Starships.Add(apiData);
            }
        }

        private void MapToEntity(Film apiData, Dictionary<string, Film> existing)
        {
            if (existing.TryGetValue(apiData.Url, out var tracked))
            {
                if (tracked.Edited.HasValue && apiData.Edited.HasValue && apiData.Edited <= tracked.Edited)
                    return;

                _context.Entry(tracked).CurrentValues.SetValues(apiData);
                tracked.Characters = apiData.Characters;
                tracked.Planets = apiData.Planets;
                tracked.Starships = apiData.Starships;
            }
            else
            {
                _context.Films.Add(apiData);
            }
        }

        private void MapToEntity(Planet apiData, Dictionary<string, Planet> existing)
        {
            if (existing.TryGetValue(apiData.Url, out var tracked))
            {
                if (tracked.Edited.HasValue && apiData.Edited.HasValue && apiData.Edited <= tracked.Edited)
                    return;

                _context.Entry(tracked).CurrentValues.SetValues(apiData);
                tracked.Films = apiData.Films;
            }
            else
            {
                _context.Planets.Add(apiData);
            }
        }

        private void MapToEntity(Character apiData, Dictionary<string, Character> existing)
        {
            if (existing.TryGetValue(apiData.Url, out var tracked))
            {
                if (tracked.Edited.HasValue && apiData.Edited.HasValue && apiData.Edited <= tracked.Edited)
                    return;

                _context.Entry(tracked).CurrentValues.SetValues(apiData);
                tracked.Films = apiData.Films;
                tracked.Starships = apiData.Starships;
            }
            else
            {
                _context.Characters.Add(apiData);
            }
        }

        private void MapToEntity(Species apiData, Dictionary<string, Species> existing)
        {
            if (existing.TryGetValue(apiData.Url, out var tracked))
            {
                if (tracked.Edited.HasValue && apiData.Edited.HasValue && apiData.Edited <= tracked.Edited)
                    return;

                _context.Entry(tracked).CurrentValues.SetValues(apiData);
                tracked.People = apiData.People;
                tracked.Films = apiData.Films;
            }
            else
            {
                _context.Species.Add(apiData);
            }
        }

        #endregion
    }
}
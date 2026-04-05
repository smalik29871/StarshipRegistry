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

        public SwapiService(HttpClient httpClient, ApplicationDbContext context, ILogger<SwapiService> logger, SwapiSettings swapiSettings)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
            _swapiSettings = swapiSettings;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _jsonOptions.Converters.Add(new SwapiDateTimeConverter());
        }

        #region LIVE LOOKUPS FOR UI

        public async Task<List<Film>> GetFilmsAsync()
        {
            try
            {
                var jsonString = await _httpClient.GetStringAsync($"{_swapiSettings.BaseUrl}films");
                return JsonSerializer.Deserialize<List<Film>>(jsonString, _jsonOptions) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch films from SWAPI.");
                return new List<Film>();
            }
        }

        public async Task<List<Character>> GetPeopleAsync()
        {
            try
            {
                var jsonString = await _httpClient.GetStringAsync($"{_swapiSettings.BaseUrl}people");
                return JsonSerializer.Deserialize<List<Character>>(jsonString, _jsonOptions) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch people from SWAPI.");
                return new List<Character>();
            }
        }

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

                _logger.LogWarning("API call to {Url} failed with status code {StatusCode}", url, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while fetching resource from {Url}", url);
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

        private async Task SyncResourceAsync<T>(string url, Action<T> mapAction) where T : class
        {
            try
            {
                _context.ChangeTracker.Clear();

                var jsonString = await _httpClient.GetStringAsync(url);

                var results = JsonSerializer.Deserialize<List<T>>(jsonString, _jsonOptions);

                if (results != null)
                {
                    foreach (var item in results)
                    {
                        mapAction(item);
                    }
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully synced {Count} items from {Url}", results.Count, url);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing {Url}", url);
            }
        }
        #endregion

        #region PRIVATE MAPPERS

        private void MapToEntity(Starship apiData)
        {
            var existing = _context.Starships.FirstOrDefault(s => s.Url == apiData.Url);

            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(apiData);
                existing.Pilots = apiData.Pilots;
                existing.Films = apiData.Films;
                _context.Starships.Update(existing);
            }
            else
            {
                _context.Starships.Add(apiData);
            }
        }

        private void MapToEntity(Film apiData)
        {
            var existing = _context.Films.FirstOrDefault(f => f.Url == apiData.Url);
            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(apiData);
                existing.Characters = apiData.Characters;
                existing.Planets = apiData.Planets;
                existing.Starships = apiData.Starships;
                _context.Films.Update(existing);
            }
            else
            {
                _context.Films.Add(apiData);
            }
        }

        private void MapToEntity(Planet apiData)
        {
            var existing = _context.Planets.FirstOrDefault(p => p.Url == apiData.Url);
            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(apiData);
                existing.Films = apiData.Films;
                _context.Planets.Update(existing);
            }
            else
            {
                _context.Planets.Add(apiData);
            }
        }

        private void MapToEntity(Character apiData)
        {
            var existing = _context.Characters.FirstOrDefault(c => c.Url == apiData.Url);
            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(apiData);
                existing.Films = apiData.Films;
                existing.Starships = apiData.Starships;
                _context.Characters.Update(existing);
            }
            else
            {
                _context.Characters.Add(apiData);
            }
        }

        private void MapToEntity(Species apiData)
        {
            var existing = _context.Species.FirstOrDefault(s => s.Url == apiData.Url);
            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(apiData);
                existing.People = apiData.People;
                existing.Films = apiData.Films;
                _context.Species.Update(existing);
            }
            else
            {
                _context.Species.Add(apiData);
            }
        }

        #endregion
    }
}
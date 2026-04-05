using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StarshipRegistry.Configuration;
using StarshipRegistry.Data;
using StarshipRegistry.Models;
using StarshipRegistry.Services;
using System.Text.Json;

namespace StarshipRegistry.Helpers
{
    public class DetailsHelper
    {
        private readonly SwapiService _swapiService;
        private readonly ApplicationDbContext _context;

        public DetailsHelper(SwapiService swapiService, ApplicationDbContext context)
        {
            _swapiService = swapiService;
            _context = context;
        }

        public async Task<T?> GetOrFetchAndCacheAsync<T>(
            string id,
            string apiEndpoint) where T : class
        {
            var swapiUrl = $"https://swapi.info/api/{apiEndpoint}/{id}";

            var dbSet = GetDbSet<T>();
            var entity = await dbSet.FirstOrDefaultAsync(e => EF.Property<string>(e, "Url") == swapiUrl);

            if (entity == null)
            {
                entity = await _swapiService.FetchFromApiAsync<T>(swapiUrl);

                if (entity != null)
                {
                    try
                    {
                        bool alreadyExists = await dbSet.AnyAsync(e => EF.Property<string>(e, "Url") == swapiUrl);

                        if (!alreadyExists)
                        {
                            dbSet.Add(entity);
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (DbUpdateException)
                    {
                        _context.Entry(entity).State = EntityState.Detached;
                    }
                }
            }

            return entity;
        }

        public async Task<Dictionary<string, string>> GetFilmNamesBatchAsync(List<string>? urls)
        {
            var result = new Dictionary<string, string>();
            if (urls == null || !urls.Any())
                return result;

            var films = await _context.Films
                .Where(f => urls.Contains(f.Url))
                .ToDictionaryAsync(f => f.Url, f => f.Title);

            var missingUrls = urls.Where(u => !films.ContainsKey(u)).ToList();

            foreach (var url in missingUrls)
            {
                var film = await _swapiService.FetchFromApiAsync<Film>(url);
                if (film != null)
                {
                    films[url] = film.Title;
                    try
                    {
                        _context.Films.Add(film);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                    }
                }
            }

            return films;
        }

        public async Task<Dictionary<string, string>> GetCharacterNamesBatchAsync(List<string>? urls)
        {
            var result = new Dictionary<string, string>();
            if (urls == null || !urls.Any())
                return result;

            var characters = await _context.Characters
                .Where(c => urls.Contains(c.Url))
                .ToDictionaryAsync(c => c.Url, c => c.Name);

            var missingUrls = urls.Where(u => !characters.ContainsKey(u)).ToList();

            foreach (var url in missingUrls)
            {
                var character = await _swapiService.FetchFromApiAsync<Character>(url);
                if (character != null)
                {
                    characters[url] = character.Name;
                    try
                    {
                        _context.Characters.Add(character);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                    }
                }
            }

            return characters;
        }

        private DbSet<T> GetDbSet<T>() where T : class
        {
            return typeof(T).Name switch
            {
                nameof(Character) => (DbSet<T>)(object)_context.Characters,
                nameof(Film) => (DbSet<T>)(object)_context.Films,
                nameof(Planet) => (DbSet<T>)(object)_context.Planets,
                nameof(Species) => (DbSet<T>)(object)_context.Species,
                nameof(Starship) => (DbSet<T>)(object)_context.Starships,
                nameof(Vehicle) => (DbSet<T>)(object)_context.Vehicles,
                _ => throw new InvalidOperationException($"No DbSet found for type {typeof(T).Name}")
            };
        }

        public async Task<Dictionary<string, string>> GetPlanetNamesBatchAsync(List<string>? urls)
        {
            var result = new Dictionary<string, string>();
            if (urls == null || !urls.Any())
                return result;

            var planets = await _context.Planets
                .Where(p => urls.Contains(p.Url))
                .ToDictionaryAsync(p => p.Url, p => p.Name);

            var missingUrls = urls.Where(u => !planets.ContainsKey(u)).ToList();

            foreach (var url in missingUrls)
            {
                var planet = await _swapiService.FetchFromApiAsync<Planet>(url);
                if (planet != null)
                {
                    planets[url] = planet.Name;
                    try
                    {
                        _context.Planets.Add(planet);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException) { }
                }
            }

            return planets;
        }

        public async Task<Dictionary<string, string>> GetStarshipNamesBatchAsync(List<string>? urls)
        {
            var result = new Dictionary<string, string>();
            if (urls == null || !urls.Any())
                return result;

            var starships = await _context.Starships
                .Where(s => urls.Contains(s.Url))
                .ToDictionaryAsync(s => s.Url, s => s.Name);

            var missingUrls = urls.Where(u => !starships.ContainsKey(u)).ToList();

            foreach (var url in missingUrls)
            {
                var starship = await _swapiService.FetchFromApiAsync<Starship>(url);
                if (starship != null)
                {
                    starships[url] = starship.Name;
                    try
                    {
                        _context.Starships.Add(starship);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException) { }
                }
            }

            return starships;
        }

        public async Task<Dictionary<string, string>> GetVehicleNamesBatchAsync(List<string>? urls)
        {
            var result = new Dictionary<string, string>();
            if (urls == null || !urls.Any())
                return result;

            var vehicles = await _context.Vehicles
                .Where(v => urls.Contains(v.Url))
                .ToDictionaryAsync(v => v.Url, v => v.Name);

            var missingUrls = urls.Where(u => !vehicles.ContainsKey(u)).ToList();

            foreach (var url in missingUrls)
            {
                var vehicle = await _swapiService.FetchFromApiAsync<Vehicle>(url);
                if (vehicle != null)
                {
                    vehicles[url] = vehicle.Name;
                    try
                    {
                        _context.Vehicles.Add(vehicle);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException) { }
                }
            }

            return vehicles;
        }

        public async Task<Dictionary<string, string>> GetSpeciesNamesBatchAsync(List<string>? urls)
        {
            var result = new Dictionary<string, string>();
            if (urls == null || !urls.Any())
                return result;

            var species = await _context.Species
                .Where(s => urls.Contains(s.Url))
                .ToDictionaryAsync(s => s.Url, s => s.Name);

            var missingUrls = urls.Where(u => !species.ContainsKey(u)).ToList();

            foreach (var url in missingUrls)
            {
                var spec = await _swapiService.FetchFromApiAsync<Species>(url);
                if (spec != null)
                {
                    species[url] = spec.Name;
                    try
                    {
                        _context.Species.Add(spec);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException) { }
                }
            }

            return species;
        }

    }
}

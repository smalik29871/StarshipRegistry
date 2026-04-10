using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StarshipRegistry.Configuration;
using StarshipRegistry.Data;
using StarshipRegistry.Models;
using StarshipRegistry.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StarshipRegistry.Helpers
{
    public class DetailsHelper
    {
        private readonly SwapiService _swapiService;
        private readonly ApplicationDbContext _context;
        private readonly string _swapiBaseUrl;

        public DetailsHelper(
            SwapiService swapiService,
            ApplicationDbContext context,
            IOptions<SwapiSettings> swapiSettings)
        {
            _swapiService = swapiService;
            _context = context;
            _swapiBaseUrl = swapiSettings.Value.BaseUrl.TrimEnd('/');
        }

        public async Task<T?> GetOrFetchAndCacheAsync<T>(string id, string apiEndpoint) where T : class, ISwapiEntity
        {
            var swapiUrl = $"{_swapiBaseUrl}/{apiEndpoint}/{id}";
            var dbSet = GetDbSet<T>();

            // SWAPI stores URLs with a trailing slash; match both forms to avoid a silent cache miss.
            var entity = await dbSet.FirstOrDefaultAsync(e => e.Url == swapiUrl || e.Url == swapiUrl + "/");

            if (entity != null) return entity;

            // Fetch from API if not in DB
            entity = await _swapiService.FetchFromApiAsync<T>(swapiUrl);
            if (entity == null) return null;

            try
            {
                var alreadyExists = await dbSet.AnyAsync(e => e.Url == swapiUrl || e.Url == swapiUrl + "/");
                if (!alreadyExists)
                {
                    dbSet.Add(entity);
                    await _context.SaveChangesAsync();
                }
            }
            catch (DbUpdateException)
            {
                // Good fallback practice to avoid tracking ghost entities
                _context.Entry(entity).State = EntityState.Detached;
            }

            return entity;
        }

        /// <summary>
        /// A generic method to replace all 6 repetitive batch fetchers.
        /// </summary>
        public async Task<Dictionary<string, string>> GetNamesBatchAsync<T>(
            List<string>? urls,
            Func<T, string> nameSelector) where T : class, ISwapiEntity
        {
            var result = new Dictionary<string, string>();
            if (urls == null || !urls.Any()) return result;

            var dbSet = GetDbSet<T>();

            // Find what we already have cached
            var cachedEntities = await dbSet
                .Where(e => urls.Contains(e.Url))
                .ToListAsync();

            var mappedResults = cachedEntities.ToDictionary(
                e => e.Url,
                nameSelector
            );

            var missingUrls = urls.Where(u => !mappedResults.ContainsKey(u)).ToList();

            // Fetch missing from API
            foreach (var url in missingUrls)
            {
                var externalEntity = await _swapiService.FetchFromApiAsync<T>(url);
                if (externalEntity == null) continue;

                var name = nameSelector(externalEntity);
                mappedResults[url] = name;

                try
                {
                    dbSet.Add(externalEntity);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    _context.Entry(externalEntity).State = EntityState.Detached;
                }
            }

            return mappedResults;
        }

        private DbSet<T> GetDbSet<T>() where T : class, ISwapiEntity
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
    }
}

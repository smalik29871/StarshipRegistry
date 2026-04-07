using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarshipRegistry.Configuration;
using StarshipRegistry.Data;
using StarshipRegistry.Helpers;
using StarshipRegistry.Models;
using StarshipRegistry.Models.ViewModels;
using StarshipRegistry.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StarshipRegistry.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class StarshipController : Controller
    {
        private readonly SwapiService _swapiService;
        private readonly ApplicationDbContext _context;
        private readonly DetailsHelper _detailsHelper;
        private readonly StarshipSearchService _searchService;
        private readonly StarshipQueryHelper _queryHelper;
        private readonly ILogger<StarshipController> _logger;
        private readonly IMemoryCache _cache;
        private readonly string _swapiBaseUrl;

        public StarshipController(
            SwapiService swapiService,
            ApplicationDbContext context,
            DetailsHelper detailsHelper,
            StarshipSearchService searchService,
            StarshipQueryHelper queryHelper,
            ILogger<StarshipController> logger,
            IOptions<SwapiSettings> swapiSettings,
            IMemoryCache cache)
        {
            _swapiService = swapiService;
            _context = context;
            _detailsHelper = detailsHelper;
            _searchService = searchService;
            _queryHelper = queryHelper;
            _logger = logger;
            _cache = cache;
            _swapiBaseUrl = swapiSettings.Value.BaseUrl;
        }

        public async Task<IActionResult> Index()
        {
            if (_cache.TryGetValue<DateTime>("seed:cooldown", out var expiry))
            {
                var remaining = (int)Math.Ceiling(Math.Max(0, (expiry - DateTime.UtcNow).TotalSeconds));
                ViewBag.SyncCooldownSeconds = remaining;
            }
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["PageMode"] = "Create";
            await PopulateFormLookupsAsync();
            return View("Details", new StarshipDetailsViewModel());
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(Starship ship, [FromForm] string[] selectedPilots, [FromForm] string[] selectedFilms)
        {
            if (!ModelState.IsValid)
            {
                ViewData["PageMode"] = "Create";
                ship.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new List<string>();
                ship.Pilots = selectedPilots?.Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new List<string>();
                await PopulateFormLookupsAsync();
                var viewModel = new StarshipDetailsViewModel
                {
                    Starship = ship,
                    FilmNames = await _detailsHelper.GetNamesBatchAsync<Film>(ship.Films, f => f.Title),
                    PilotNames = await _detailsHelper.GetNamesBatchAsync<Character>(ship.Pilots, c => c.Name)
                };
                return View("Details", viewModel);
            }

            ship.Pilots = selectedPilots?.Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new List<string>();
            ship.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new List<string>();

            var topUrl = await _context.Starships
                .OrderByDescending(s => s.Url.Length)
                .ThenByDescending(s => s.Url)
                .Select(s => s.Url)
                .FirstOrDefaultAsync();
            var maxId = int.TryParse(topUrl?.TrimEnd('/').Split('/').Last(), out var n) ? n : 0;
            var newId = Math.Max(maxId + 1, 10000);
            ship.Url = $"{_swapiBaseUrl}starships/{newId}/";

            _context.Starships.Add(ship);
            await _context.SaveChangesAsync();
            await _searchService.AddToIndexAsync(ship);

            TempData["Message"] = $"{ship.Name} was registered successfully!";
            return RedirectToAction(nameof(Details), new { id = newId });
        }

        [HttpGet]
        public async Task<IActionResult> DataTable([FromQuery] DataTableRequest request)
        {
            try
            {
                var query = _context.Starships.AsNoTracking();
                var recordsTotal = await query.CountAsync();
                var searchValue = request.Search?.Value?.Trim();

                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        s.Name.Contains(searchValue) ||
                        s.Model.Contains(searchValue) ||
                        s.StarshipClass.Contains(searchValue));
                }

                var recordsFiltered = await query.CountAsync();
                var pageSize = request.Length > 0 ? request.Length : 10;
                var offset = request.Start >= 0 ? request.Start : 0;

                var ships = await ApplyDataTableOrdering(query, request)
                    .Skip(offset)
                    .Take(pageSize)
                    .ToListAsync();

                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal,
                    recordsFiltered,
                    data = _queryHelper.MapToRows(ships)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serve starship DataTable request.");
                return StatusCode(500, new
                {
                    error = "Failed to load starship registry data."
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> AiSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(Array.Empty<object>());
            }

            try
            {
                var command = await _queryHelper.ParseQueryAsync(query);
                var take = command.Take > 0 ? command.Take : 10;

                List<Starship> ships;
                if (!string.IsNullOrWhiteSpace(command.SortBy))
                {
                    ships = await _queryHelper.ExecuteQueryAsync(command);
                }
                else
                {
                    ships = await _searchService.SearchAsync(command.Concept, take);
                    if (ships.Count == 0 && !string.IsNullOrWhiteSpace(command.Concept))
                    {
                        var kw = command.Concept;
                        ships = await _context.Starships
                            .Where(s => s.Name.Contains(kw) || s.Model.Contains(kw) || s.StarshipClass.Contains(kw))
                            .Take(take)
                            .ToListAsync();
                    }
                }

                return Json(_queryHelper.MapToRows(ships));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI search failed for query {Query}.", query);
                return StatusCode(500, new
                {
                    error = "AI search failed."
                });
            }
        }

        public async Task<IActionResult> Details(string id, bool edit = false, string returnUrl = "")
        {
            var ship = await _detailsHelper.GetOrFetchAndCacheAsync<Starship>(id, "starships");

            if (ship == null)
            {
                _logger.LogWarning("Starship with ID {Id} could not be retrieved from DB or API.", id);
                return NotFound();
            }

            ViewData["PageMode"] = edit ? "Edit" : "Details";
            ViewData["ReturnUrl"] = returnUrl;

            await PopulateFormLookupsAsync();

            var viewModel = new StarshipDetailsViewModel
            {
                Starship = ship,
                FilmNames = await _detailsHelper.GetNamesBatchAsync<Film>(ship.Films, f => f.Title),
                PilotNames = await _detailsHelper.GetNamesBatchAsync<Character>(ship.Pilots, c => c.Name)
            };

            return View("Details", viewModel);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(Starship ship, [FromForm] string[] selectedPilots, [FromForm] string[] selectedFilms, string returnUrl = "")
        {
            if (!ModelState.IsValid)
            {
                ViewData["PageMode"] = "Edit";
                ViewData["ReturnUrl"] = returnUrl;
                ship.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new List<string>();
                ship.Pilots = selectedPilots?.Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new List<string>();

                await PopulateFormLookupsAsync();

                var viewModel = new StarshipDetailsViewModel
                {
                    Starship = ship,
                    FilmNames = await _detailsHelper.GetNamesBatchAsync<Film>(ship.Films, f => f.Title),
                    PilotNames = await _detailsHelper.GetNamesBatchAsync<Character>(ship.Pilots, c => c.Name)
                };

                return View("Details", viewModel);
            }

            ship.Pilots = selectedPilots?.Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new List<string>();
            ship.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new List<string>();

            _context.Update(ship);
            await _context.SaveChangesAsync();
            await _searchService.AddToIndexAsync(ship);

            TempData["Message"] = $"{ship.Name} was successfully updated!";

            var numericId = ship.Url?.TrimEnd('/').Split('/').Last();

            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Details), new { id = numericId });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Seed()
        {
            const string cooldownKey = "seed:cooldown";
            if (_cache.TryGetValue(cooldownKey, out _))
            {
                TempData["Error"] = "Sync is on cooldown. Please wait 10 minutes before syncing again.";
                return RedirectToAction(nameof(Index));
            }

            _cache.Set(cooldownKey, DateTime.UtcNow.AddMinutes(10), TimeSpan.FromMinutes(10));

            try
            {
                await _swapiService.SyncAllDataAsync();
                await _searchService.BuildIndexAsync();
                TempData["Message"] = "Success! The fleet was synchronized and search indices were rebuilt.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete background sync and index rebuild.");
                TempData["Error"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var ship = await _context.Starships.FirstOrDefaultAsync(s =>
                s.Url != null &&
                (s.Url.EndsWith($"/starships/{id}") || s.Url.EndsWith($"/starships/{id}/")));

            if (ship != null)
            {
                _context.Starships.Remove(ship);
                await _context.SaveChangesAsync();

                _searchService.RemoveFromIndex(ship.Url);

                TempData["Message"] = $"{ship.Name} was removed from the registry.";
            }
            else
            {
                _logger.LogWarning("Delete requested for Starship ID {Id}, but it wasn't found.", id);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateFormLookupsAsync()
        {
            ViewData["AvailableCharacters"] = await _context.Characters.OrderBy(c => c.Name).ToListAsync();
            ViewData["AvailableFilms"] = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();
        }

        private static IQueryable<Starship> ApplyDataTableOrdering(IQueryable<Starship> query, DataTableRequest request)
        {
            var order = request.Order?.FirstOrDefault();
            if (order == null)
            {
                return query.OrderBy(s => s.Name);
            }

            var sortColumn = request.Columns != null &&
                             order.Column >= 0 &&
                             order.Column < request.Columns.Length
                ? request.Columns[order.Column].Data
                : null;

            var descending = string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase);

            return (sortColumn ?? "name") switch
            {
                "model" => descending ? query.OrderByDescending(s => s.Model) : query.OrderBy(s => s.Model),
                "starshipClass" => descending ? query.OrderByDescending(s => s.StarshipClass) : query.OrderBy(s => s.StarshipClass),
                "costInCredits" => descending
                    ? query.OrderByDescending(s => (s.CostInCredits ?? "").Length).ThenByDescending(s => s.CostInCredits)
                    : query.OrderBy(s => (s.CostInCredits ?? "").Length).ThenBy(s => s.CostInCredits),
                "crew" => descending
                    ? query.OrderByDescending(s => (s.Crew ?? "").Length).ThenByDescending(s => s.Crew)
                    : query.OrderBy(s => (s.Crew ?? "").Length).ThenBy(s => s.Crew),
                "hyperdriveRating" => descending
                    ? query.OrderByDescending(s => (s.HyperdriveRating ?? "").Length).ThenByDescending(s => s.HyperdriveRating)
                    : query.OrderBy(s => (s.HyperdriveRating ?? "").Length).ThenBy(s => s.HyperdriveRating),
                "created" => descending ? query.OrderByDescending(s => s.Created) : query.OrderBy(s => s.Created),
                _ => descending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name)
            };
        }
    }
}

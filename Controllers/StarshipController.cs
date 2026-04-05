using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public class StarshipController : Controller
    {
        private readonly SwapiService _swapiService;
        private readonly ApplicationDbContext _context;
        private readonly DetailsHelper _detailsHelper;
        private readonly StarshipSearchService _searchService;
        private readonly StarshipQueryHelper _queryHelper;
        private readonly string _swapiBaseUrl;

        public StarshipController(
            SwapiService swapiService,
            ApplicationDbContext context,
            DetailsHelper detailsHelper,
            StarshipSearchService searchService,
            StarshipQueryHelper queryHelper,
            IOptions<SwapiSettings> swapiSettings)
        {
            _swapiService = swapiService;
            _context = context;
            _detailsHelper = detailsHelper;
            _searchService = searchService;
            _queryHelper = queryHelper;
            _swapiBaseUrl = swapiSettings.Value.BaseUrl;
        }

        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentSort"] = sortOrder;

            if (string.IsNullOrEmpty(searchString))
                return View(await _context.Starships.ToListAsync());

            SearchCommand command = await _queryHelper.ParseQueryAsync(searchString);

            var ships = string.IsNullOrEmpty(command.SortBy)
                ? await _searchService.SearchAsync(command.Concept, command.Take)
                : await ExecuteSortQueryAsync(command);

            return View(ships);
        }

        [HttpGet]
        public async Task<IActionResult> AiSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(_queryHelper.MapToRows(await _context.Starships.ToListAsync()));

            SearchCommand command = await _queryHelper.ParseQueryAsync(query);

            List<Starship> ships = string.IsNullOrEmpty(command.SortBy)
                ? await _searchService.SearchAsync(command.Concept, command.Take)
                : await ExecuteSortQueryAsync(command);

            return Json(_queryHelper.MapToRows(ships));
        }

        private static readonly Dictionary<string, string> SortableColumns = new()
        {
            ["cost"]       = "CostInCredits",
            ["crew"]       = "Crew",
            ["hyperdrive"] = "HyperdriveRating",
            ["length"]     = "Length",
            ["cargo"]      = "CargoCapacity"
        };

        private async Task<List<Starship>> ExecuteSortQueryAsync(SearchCommand command)
        {
            if (!SortableColumns.TryGetValue(command.SortBy, out var column))
                return new List<Starship>();

            var order = command.Order == "desc" ? "DESC" : "ASC";
            var sql = $"SELECT * FROM Starships WHERE TRY_CAST([{column}] AS float) IS NOT NULL " +
                      $"ORDER BY TRY_CAST([{column}] AS float) {order} " +
                      $"OFFSET 0 ROWS FETCH NEXT {{0}} ROWS ONLY";

            return await _context.Starships.FromSqlRaw(sql, command.Take).ToListAsync();
        }

        public async Task<IActionResult> Details(string id, bool edit = false, string returnUrl = "")
        {
            var ship = await _detailsHelper.GetOrFetchAndCacheAsync<Starship>(id, "starships");

            if (ship == null)
                return NotFound();

            ViewData["PageMode"] = edit ? "Edit" : "Details";
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["AvailableFilms"] = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();
            ViewData["AvailableCharacters"] = await _context.Characters.OrderBy(c => c.Name).ToListAsync();

            return View(new StarshipDetailsViewModel
            {
                Starship = ship,
                FilmNames = await _detailsHelper.GetFilmNamesBatchAsync(ship.Films),
                PilotNames = await _detailsHelper.GetCharacterNamesBatchAsync(ship.Pilots)
            });
        }

        public async Task<IActionResult> Create()
        {
            ViewData["PageMode"] = "Create";
            ViewData["AvailableFilms"] = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();
            ViewData["AvailableCharacters"] = await _context.Characters.OrderBy(c => c.Name).ToListAsync();

            return View("Details", new StarshipDetailsViewModel
            {
                Starship = new Starship(),
                FilmNames = new Dictionary<string, string>(),
                PilotNames = new Dictionary<string, string>()
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Starship ship, [FromForm(Name = "selectedFilms")] string[] selectedFilms, [FromForm(Name = "selectedPilots")] string[] selectedPilots)
        {
            if (ModelState.IsValid)
            {
                ship.Pilots = selectedPilots?.Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new List<string>();
                ship.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new List<string>();
                ship.Created = DateTime.UtcNow;

                if (string.IsNullOrEmpty(ship.Url))
                {
                    var nextId = await _context.Starships.CountAsync() + 1000;
                    ship.Url = $"{_swapiBaseUrl}starships/{nextId}";
                }

                _context.Starships.Add(ship);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"{ship.Name} was successfully added to the fleet!";
                return RedirectToAction(nameof(Index));
            }

            ship.Pilots ??= new List<string>();
            ship.Films ??= new List<string>();
            ViewData["PageMode"] = "Create";
            ViewData["AvailableFilms"] = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();
            ViewData["AvailableCharacters"] = await _context.Characters.OrderBy(c => c.Name).ToListAsync();

            return View("Details", new StarshipDetailsViewModel
            {
                Starship = ship,
                FilmNames = new Dictionary<string, string>(),
                PilotNames = new Dictionary<string, string>()
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Starship ship, [FromForm(Name = "selectedFilms")] string[] selectedFilms, [FromForm(Name = "selectedPilots")] string[] selectedPilots, string returnUrl = "")
        {
            if (ModelState.IsValid)
            {
                ship.Pilots = selectedPilots?.Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new List<string>();
                ship.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new List<string>();

                _context.Update(ship);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"{ship.Name} was successfully updated!";

                var numericId = ship.Url?.TrimEnd('/').Split('/').Last();
                return !string.IsNullOrEmpty(returnUrl)
                    ? Redirect(returnUrl)
                    : RedirectToAction(nameof(Details), new { id = numericId });
            }

            ship.Pilots ??= new List<string>();
            ship.Films ??= new List<string>();
            ViewData["PageMode"] = "Edit";
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["AvailableFilms"] = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();
            ViewData["AvailableCharacters"] = await _context.Characters.OrderBy(c => c.Name).ToListAsync();

            return View("Details", new StarshipDetailsViewModel
            {
                Starship = ship,
                FilmNames = await _detailsHelper.GetFilmNamesBatchAsync(ship.Films),
                PilotNames = await _detailsHelper.GetCharacterNamesBatchAsync(ship.Pilots)
            });
        }

        [HttpPost]
        public async Task<IActionResult> Seed()
        {
            try
            {
                await _swapiService.SyncAllDataAsync();
                await _searchService.BuildIndexAsync();
                TempData["Message"] = "Success! The fleet was synchronized and search indices were rebuilt.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            string swapiUrl = $"{_swapiBaseUrl}starships/{id}";
            var ship = await _context.Starships.FirstOrDefaultAsync(s => s.Url == swapiUrl);

            if (ship != null)
            {
                _context.Starships.Remove(ship);
                await _context.SaveChangesAsync();
                await _searchService.BuildIndexAsync();
                TempData["Message"] = $"{ship.Name} was removed from the registry.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

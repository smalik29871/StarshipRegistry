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

        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DataTable([FromQuery] DataTableRequest request)
        {
            var search = request.Search?.Value?.Trim().ToLower() ?? "";

            var query = _context.Starships.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s =>
                    s.Name.ToLower().Contains(search) ||
                    s.Model.ToLower().Contains(search) ||
                    s.StarshipClass.ToLower().Contains(search));

            var totalRecords = await _context.Starships.CountAsync();
            var filteredRecords = await query.CountAsync();

            var columnIndex = request.Order?.FirstOrDefault()?.Column ?? 0;
            var dir = request.Order?.FirstOrDefault()?.Dir ?? "asc";

            query = columnIndex switch
            {
                0 => dir == "desc" ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
                1 => dir == "desc" ? query.OrderByDescending(s => s.Model) : query.OrderBy(s => s.Model),
                2 => dir == "desc" ? query.OrderByDescending(s => s.StarshipClass) : query.OrderBy(s => s.StarshipClass),
                3 => dir == "desc" ? query.OrderByDescending(s => s.CostInCredits!.Length).ThenByDescending(s => s.CostInCredits) : query.OrderBy(s => s.CostInCredits!.Length).ThenBy(s => s.CostInCredits),
                4 => dir == "desc" ? query.OrderByDescending(s => s.Crew!.Length).ThenByDescending(s => s.Crew) : query.OrderBy(s => s.Crew!.Length).ThenBy(s => s.Crew),
                5 => dir == "desc" ? query.OrderByDescending(s => s.HyperdriveRating!.Length).ThenByDescending(s => s.HyperdriveRating) : query.OrderBy(s => s.HyperdriveRating!.Length).ThenBy(s => s.HyperdriveRating),
                6 => dir == "desc" ? query.OrderByDescending(s => s.Created) : query.OrderBy(s => s.Created),
                _ => query.OrderBy(s => s.Name)
            };

            var ships = await query.Skip(request.Start).Take(request.Length).ToListAsync();

            return Json(new
            {
                draw = request.Draw,
                recordsTotal = totalRecords,
                recordsFiltered = filteredRecords,
                data = _queryHelper.MapToRows(ships)
            });
        }

        [HttpGet]
        public async Task<IActionResult> AiSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(_queryHelper.MapToRows(await _context.Starships.ToListAsync()));

            SearchCommand command = await _queryHelper.ParseQueryAsync(query);

            List<Starship> ships = string.IsNullOrEmpty(command.SortBy)
                ? await _searchService.SearchAsync(command.Concept, command.Take)
                : await _queryHelper.ExecuteSortQueryAsync(command);

            return Json(_queryHelper.MapToRows(ships));
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

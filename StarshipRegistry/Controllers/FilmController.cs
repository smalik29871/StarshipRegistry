using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarshipRegistry.Data;
using StarshipRegistry.Helpers;
using StarshipRegistry.Models;
using StarshipRegistry.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StarshipRegistry.Controllers
{
    public class FilmController : Controller
    {
        private readonly DetailsHelper _detailsHelper;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FilmController> _logger;

        public FilmController(DetailsHelper detailsHelper, ApplicationDbContext context, ILogger<FilmController> logger)
        {
            _detailsHelper = detailsHelper;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Details(string id, bool edit = false, string returnUrl = "")
        {
            var film = await _detailsHelper.GetOrFetchAndCacheAsync<Film>(id, "films");

            if (film == null)
            {
                _logger.LogWarning("Film with ID {Id} could not be retrieved.", id?.Replace('\r', '_').Replace('\n', '_'));
                return NotFound();
            }

            ViewData["PageMode"] = edit ? "Edit" : "Details";

            if (string.IsNullOrEmpty(returnUrl) && !edit)
            {
                var referer = Request.Headers["Referer"].FirstOrDefault() ?? "";
                if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
                {
                    var localReferer = refererUri.PathAndQuery;
                    if (Url.IsLocalUrl(localReferer) && !localReferer.StartsWith(Request.Path.Value ?? "", StringComparison.OrdinalIgnoreCase))
                        returnUrl = localReferer;
                }
            }

            ViewData["ReturnUrl"] = returnUrl;

            await PopulateFormLookupsAsync();

            var viewModel = new FilmDetailsViewModel
            {
                Film = film,
                CharacterNames = await _detailsHelper.GetNamesBatchAsync<Character>(film.Characters, c => c.Name),
                PlanetNames = await _detailsHelper.GetNamesBatchAsync<Planet>(film.Planets, p => p.Name),
                StarshipNames = await _detailsHelper.GetNamesBatchAsync<Starship>(film.Starships, s => s.Name),
                VehicleNames = await _detailsHelper.GetNamesBatchAsync<Vehicle>(film.Vehicles, v => v.Name),
                SpeciesNames = await _detailsHelper.GetNamesBatchAsync<Species>(film.Species, s => s.Name)
            };

            return View("Details", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Film film,
            [FromForm] string[] selectedCharacters,
            [FromForm] string[] selectedPlanets,
            [FromForm] string[] selectedStarships,
            [FromForm] string[] selectedVehicles,
            [FromForm] string[] selectedSpecies,
            string returnUrl = "")
        {
            if (!ModelState.IsValid)
            {
                ViewData["PageMode"] = "Edit";
                ViewData["ReturnUrl"] = returnUrl;
                film.Characters = selectedCharacters?.Where(x => !string.IsNullOrEmpty(x)).ToList() ?? new List<string>();
                film.Planets    = selectedPlanets?.Where(x => !string.IsNullOrEmpty(x)).ToList()    ?? new List<string>();
                film.Starships  = selectedStarships?.Where(x => !string.IsNullOrEmpty(x)).ToList()  ?? new List<string>();
                film.Vehicles   = selectedVehicles?.Where(x => !string.IsNullOrEmpty(x)).ToList()   ?? new List<string>();
                film.Species    = selectedSpecies?.Where(x => !string.IsNullOrEmpty(x)).ToList()    ?? new List<string>();

                await PopulateFormLookupsAsync();

                var viewModel = new FilmDetailsViewModel
                {
                    Film = film,
                    CharacterNames = await _detailsHelper.GetNamesBatchAsync<Character>(film.Characters, c => c.Name),
                    PlanetNames    = await _detailsHelper.GetNamesBatchAsync<Planet>(film.Planets, p => p.Name),
                    StarshipNames  = await _detailsHelper.GetNamesBatchAsync<Starship>(film.Starships, s => s.Name),
                    VehicleNames   = await _detailsHelper.GetNamesBatchAsync<Vehicle>(film.Vehicles, v => v.Name),
                    SpeciesNames   = await _detailsHelper.GetNamesBatchAsync<Species>(film.Species, s => s.Name)
                };

                return View("Details", viewModel);
            }

            film.Characters = selectedCharacters?.Where(x => !string.IsNullOrEmpty(x)).ToList() ?? new List<string>();
            film.Planets    = selectedPlanets?.Where(x => !string.IsNullOrEmpty(x)).ToList()    ?? new List<string>();
            film.Starships  = selectedStarships?.Where(x => !string.IsNullOrEmpty(x)).ToList()  ?? new List<string>();
            film.Vehicles   = selectedVehicles?.Where(x => !string.IsNullOrEmpty(x)).ToList()   ?? new List<string>();
            film.Species    = selectedSpecies?.Where(x => !string.IsNullOrEmpty(x)).ToList()    ?? new List<string>();

            _context.Update(film);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"{film.Title} was successfully updated!";

            var numericId = film.Url?.TrimEnd('/').Split('/').Last();

            return RedirectToAction(nameof(Details), new { id = numericId, returnUrl });
        }

        private async Task PopulateFormLookupsAsync()
        {
            ViewData["AvailableCharacters"] = await _context.Characters.OrderBy(c => c.Name).ToListAsync();
            ViewData["AvailablePlanets"] = await _context.Planets.OrderBy(p => p.Name).ToListAsync();
            ViewData["AvailableStarships"] = await _context.Starships.OrderBy(s => s.Name).ToListAsync();
            ViewData["AvailableVehicles"] = await _context.Vehicles.OrderBy(v => v.Name).ToListAsync();
            ViewData["AvailableSpecies"] = await _context.Species.OrderBy(s => s.Name).ToListAsync();
        }
    }
}
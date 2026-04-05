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
                _logger.LogWarning("Film with ID {Id} could not be retrieved.", id);
                return NotFound();
            }

            ViewData["PageMode"] = edit ? "Edit" : "Details";
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
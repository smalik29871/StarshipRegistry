using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarshipRegistry.Data;
using StarshipRegistry.Helpers;
using StarshipRegistry.Models;
using StarshipRegistry.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace StarshipRegistry.Controllers
{
    public class FilmController : Controller
    {
        private readonly DetailsHelper _detailsHelper = null!;
        private readonly ApplicationDbContext _context;

        public FilmController(DetailsHelper detailsHelper, ApplicationDbContext context)
        {
            _detailsHelper = detailsHelper;
            _context = context;
        }

        public async Task<IActionResult> Details(string id, bool edit = false, string returnUrl = "")
        {
            var film = await _detailsHelper.GetOrFetchAndCacheAsync<Film>(id, "films");

            if (film == null)
            {
                return NotFound();
            }

            ViewData["PageMode"] = edit ? "Edit" : "Details";
            ViewData["ReturnUrl"] = returnUrl;

            var characters = await _context.Characters.OrderBy(c => c.Name).ToListAsync();
            var planets = await _context.Planets.OrderBy(p => p.Name).ToListAsync();
            var starships = await _context.Starships.OrderBy(s => s.Name).ToListAsync();
            var vehicles = await _context.Vehicles.OrderBy(v => v.Name).ToListAsync();
            var species = await _context.Species.OrderBy(s => s.Name).ToListAsync();

            ViewData["AvailableCharacters"] = characters;
            ViewData["AvailablePlanets"] = planets;
            ViewData["AvailableStarships"] = starships;
            ViewData["AvailableVehicles"] = vehicles;
            ViewData["AvailableSpecies"] = species;

            var characterNames = await _detailsHelper.GetCharacterNamesBatchAsync(film.Characters);
            var planetNames = await _detailsHelper.GetPlanetNamesBatchAsync(film.Planets);
            var starshipNames = await _detailsHelper.GetStarshipNamesBatchAsync(film.Starships);
            var vehicleNames = await _detailsHelper.GetVehicleNamesBatchAsync(film.Vehicles);
            var speciesNames = await _detailsHelper.GetSpeciesNamesBatchAsync(film.Species);

            var viewModel = new FilmDetailsViewModel
            {
                Film = film,
                CharacterNames = characterNames,
                PlanetNames = planetNames,
                StarshipNames = starshipNames,
                VehicleNames = vehicleNames,
                SpeciesNames = speciesNames
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Film film, [FromForm(Name = "selectedCharacters")] string[] selectedCharacters, [FromForm(Name = "selectedPlanets")] string[] selectedPlanets, [FromForm(Name = "selectedStarships")] string[] selectedStarships, [FromForm(Name = "selectedVehicles")] string[] selectedVehicles, [FromForm(Name = "selectedSpecies")] string[] selectedSpecies, string returnUrl = "")
        {
            if (ModelState.IsValid)
            {
                film.Characters = selectedCharacters?.Where(c => !string.IsNullOrEmpty(c)).ToList() ?? new();
                film.Planets = selectedPlanets?.Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new();
                film.Starships = selectedStarships?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new();
                film.Vehicles = selectedVehicles?.Where(v => !string.IsNullOrEmpty(v)).ToList() ?? new();
                film.Species = selectedSpecies?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new();

                _context.Update(film);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"{film.Title} was successfully updated!";

                var numericId = film.Url?.TrimEnd('/').Split('/').Last();

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction(nameof(Details), new { id = numericId });
            }

            ViewData["PageMode"] = "Edit";
            ViewData["ReturnUrl"] = returnUrl;
            film.Characters ??= new();
            film.Planets ??= new();
            film.Starships ??= new();
            film.Vehicles ??= new();
            film.Species ??= new();

            var characters = await _context.Characters.OrderBy(c => c.Name).ToListAsync();
            var planets = await _context.Planets.OrderBy(p => p.Name).ToListAsync();
            var starships = await _context.Starships.OrderBy(s => s.Name).ToListAsync();
            var vehicles = await _context.Vehicles.OrderBy(v => v.Name).ToListAsync();
            var species = await _context.Species.OrderBy(s => s.Name).ToListAsync();

            ViewData["AvailableCharacters"] = characters;
            ViewData["AvailablePlanets"] = planets;
            ViewData["AvailableStarships"] = starships;
            ViewData["AvailableVehicles"] = vehicles;
            ViewData["AvailableSpecies"] = species;

            var viewModel = new FilmDetailsViewModel
            {
                Film = film,
                CharacterNames = await _detailsHelper.GetCharacterNamesBatchAsync(film.Characters),
                PlanetNames = await _detailsHelper.GetPlanetNamesBatchAsync(film.Planets),
                StarshipNames = await _detailsHelper.GetStarshipNamesBatchAsync(film.Starships),
                VehicleNames = await _detailsHelper.GetVehicleNamesBatchAsync(film.Vehicles),
                SpeciesNames = await _detailsHelper.GetSpeciesNamesBatchAsync(film.Species)
            };

            return View("Details", viewModel);
        }
    }
}
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
    public class VehicleController : Controller
    {
        private readonly DetailsHelper _detailsHelper = null!;
        private readonly ApplicationDbContext _context;

        public VehicleController(DetailsHelper detailsHelper, ApplicationDbContext context)
        {
            _detailsHelper = detailsHelper;
            _context = context;
        }

        public async Task<IActionResult> Details(string id, bool edit = false, string returnUrl = "")
        {
            var vehicle = await _detailsHelper.GetOrFetchAndCacheAsync<Vehicle>(id, "vehicles");

            if (vehicle == null)
            {
                return NotFound();
            }

            ViewData["PageMode"] = edit ? "Edit" : "Details";
            ViewData["ReturnUrl"] = returnUrl;

            var characters = await _context.Characters.OrderBy(c => c.Name).ToListAsync();
            var films = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();

            ViewData["AvailableCharacters"] = characters;
            ViewData["AvailableFilms"] = films;

            var pilotNames = await _detailsHelper.GetCharacterNamesBatchAsync(vehicle.Pilots);
            var filmNames = await _detailsHelper.GetFilmNamesBatchAsync(vehicle.Films);

            var viewModel = new VehicleDetailsViewModel
            {
                Vehicle = vehicle,
                PilotNames = pilotNames,
                FilmNames = filmNames
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Vehicle vehicle, [FromForm(Name = "selectedPilots")] string[] selectedPilots, [FromForm(Name = "selectedFilms")] string[] selectedFilms, string returnUrl = "")
        {
            if (ModelState.IsValid)
            {
                vehicle.Pilots = selectedPilots?.Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new();
                vehicle.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new();

                _context.Update(vehicle);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"{vehicle.Name} was successfully updated!";

                var numericId = vehicle.Url?.TrimEnd('/').Split('/').Last();
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Details), new { id = numericId });
            }

            ViewData["PageMode"] = "Edit";
            ViewData["ReturnUrl"] = returnUrl;
            vehicle.Pilots ??= new();
            vehicle.Films ??= new();

            var characters = await _context.Characters.OrderBy(c => c.Name).ToListAsync();
            var films = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();

            ViewData["AvailableCharacters"] = characters;
            ViewData["AvailableFilms"] = films;

            var viewModel = new VehicleDetailsViewModel
            {
                Vehicle = vehicle,
                PilotNames = await _detailsHelper.GetCharacterNamesBatchAsync(vehicle.Pilots),
                FilmNames = await _detailsHelper.GetFilmNamesBatchAsync(vehicle.Films)
            };

            return View("Details", viewModel);
        }
    }
}

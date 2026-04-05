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
    public class VehicleController : Controller
    {
        private readonly DetailsHelper _detailsHelper;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VehicleController> _logger;

        public VehicleController(DetailsHelper detailsHelper, ApplicationDbContext context, ILogger<VehicleController> logger)
        {
            _detailsHelper = detailsHelper;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Details(string id, bool edit = false, string returnUrl = "")
        {
            var vehicle = await _detailsHelper.GetOrFetchAndCacheAsync<Vehicle>(id, "vehicles");

            if (vehicle == null)
            {
                _logger.LogWarning("Vehicle with ID {Id} was not found during fetch.", id);
                return NotFound();
            }

            ViewData["PageMode"] = edit ? "Edit" : "Details";
            ViewData["ReturnUrl"] = returnUrl;

            await PopulateFormLookupsAsync();

            var viewModel = new VehicleDetailsViewModel
            {
                Vehicle = vehicle,
                PilotNames = await _detailsHelper.GetNamesBatchAsync<Character>(vehicle.Pilots, c => c.Name),
                FilmNames = await _detailsHelper.GetNamesBatchAsync<Film>(vehicle.Films, f => f.Title)
            };

            return View("Details", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Vehicle vehicle, [FromForm(Name = "selectedPilots")] string[] selectedPilots, [FromForm(Name = "selectedFilms")] string[] selectedFilms, string returnUrl = "")
        {
            if (!ModelState.IsValid)
            {
                ViewData["PageMode"] = "Edit";
                ViewData["ReturnUrl"] = returnUrl;
                vehicle.Pilots ??= new List<string>();
                vehicle.Films ??= new List<string>();

                await PopulateFormLookupsAsync();

                var viewModel = new VehicleDetailsViewModel
                {
                    Vehicle = vehicle,
                    PilotNames = await _detailsHelper.GetNamesBatchAsync<Character>(vehicle.Pilots, c => c.Name),
                    FilmNames = await _detailsHelper.GetNamesBatchAsync<Film>(vehicle.Films, f => f.Title)
                };

                return View("Details", viewModel);
            }

            vehicle.Pilots = selectedPilots?.Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new List<string>();
            vehicle.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new List<string>();

            _context.Update(vehicle);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"{vehicle.Name} was successfully updated!";

            var numericId = vehicle.Url?.TrimEnd('/').Split('/').Last();

            return !string.IsNullOrEmpty(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Details), new { id = numericId });
        }

        private async Task PopulateFormLookupsAsync()
        {
            ViewData["AvailableCharacters"] = await _context.Characters.OrderBy(c => c.Name).ToListAsync();
            ViewData["AvailableFilms"] = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();
        }
    }
}
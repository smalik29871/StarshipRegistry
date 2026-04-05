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
    public class SpeciesController : Controller
    {
        private readonly DetailsHelper _detailsHelper;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SpeciesController> _logger;

        public SpeciesController(DetailsHelper detailsHelper, ApplicationDbContext context, ILogger<SpeciesController> logger)
        {
            _detailsHelper = detailsHelper;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Details(string id, bool edit = false, string returnUrl = "")
        {
            var species = await _detailsHelper.GetOrFetchAndCacheAsync<Species>(id, "species");

            if (species == null)
            {
                _logger.LogWarning("Species with ID {Id} could not be resolved.", id);
                return NotFound();
            }

            ViewData["PageMode"] = edit ? "Edit" : "Details";
            ViewData["ReturnUrl"] = returnUrl;

            await PopulateFormLookupsAsync();

            var viewModel = new SpeciesDetailsViewModel
            {
                Species = species,
                CharacterNames = await _detailsHelper.GetNamesBatchAsync<Character>(species.People, c => c.Name),
                FilmNames = await _detailsHelper.GetNamesBatchAsync<Film>(species.Films, f => f.Title)
            };

            return View("Details", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Species species, [FromForm] string[] selectedPeople, [FromForm] string[] selectedFilms, string returnUrl = "")
        {
            if (!ModelState.IsValid)
            {
                ViewData["PageMode"] = "Edit";
                ViewData["ReturnUrl"] = returnUrl;
                species.People ??= new List<string>();
                species.Films ??= new List<string>();

                await PopulateFormLookupsAsync();

                var viewModel = new SpeciesDetailsViewModel
                {
                    Species = species,
                    CharacterNames = await _detailsHelper.GetNamesBatchAsync<Character>(species.People, c => c.Name),
                    FilmNames = await _detailsHelper.GetNamesBatchAsync<Film>(species.Films, f => f.Title)
                };

                return View("Details", viewModel);
            }

            species.People = selectedPeople?.Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new List<string>();
            species.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new List<string>();

            _context.Update(species);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"{species.Name} was successfully updated!";

            var numericId = species.Url?.TrimEnd('/').Split('/').Last();

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
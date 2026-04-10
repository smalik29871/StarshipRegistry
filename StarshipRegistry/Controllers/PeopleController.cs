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
    public class PeopleController : Controller
    {
        private readonly DetailsHelper _detailsHelper;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PeopleController> _logger;

        public PeopleController(DetailsHelper detailsHelper, ApplicationDbContext context, ILogger<PeopleController> logger)
        {
            _detailsHelper = detailsHelper;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Details(string id, bool edit = false, string returnUrl = "")
        {
            var person = await _detailsHelper.GetOrFetchAndCacheAsync<Character>(id, "people");

            if (person == null)
            {
                _logger.LogWarning("Character with ID {Id} not found in DB or SWAPI.", id?.Replace('\r', '_').Replace('\n', '_'));
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

            var viewModel = new PeopleDetailsViewModel
            {
                Character = person,
                FilmNames = await _detailsHelper.GetNamesBatchAsync<Film>(person.Films, f => f.Title),
                StarshipNames = await _detailsHelper.GetNamesBatchAsync<Starship>(person.Starships, s => s.Name)
            };

            return View("Details", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Character character, [FromForm] string[] selectedFilms, [FromForm] string[] selectedStarships, string returnUrl = "")
        {
            if (!ModelState.IsValid)
            {
                ViewData["PageMode"] = "Edit";
                ViewData["ReturnUrl"] = returnUrl;
                character.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new List<string>();
                character.Starships = selectedStarships?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();

                await PopulateFormLookupsAsync();

                var viewModel = new PeopleDetailsViewModel
                {
                    Character = character,
                    FilmNames = await _detailsHelper.GetNamesBatchAsync<Film>(character.Films, f => f.Title),
                    StarshipNames = await _detailsHelper.GetNamesBatchAsync<Starship>(character.Starships, s => s.Name)
                };

                return View("Details", viewModel);
            }

            character.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new List<string>();
            character.Starships = selectedStarships?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();

            _context.Update(character);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"{character.Name} was successfully updated!";

            var numericId = character.Url?.TrimEnd('/').Split('/').Last();

            return Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Details), new { id = numericId });
        }

        private async Task PopulateFormLookupsAsync()
        {
            ViewData["AvailableFilms"] = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();
            ViewData["AvailableStarships"] = await _context.Starships.OrderBy(s => s.Name).ToListAsync();
        }
    }
}
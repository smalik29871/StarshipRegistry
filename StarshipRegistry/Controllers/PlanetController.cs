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
    public class PlanetController : Controller
    {
        private readonly DetailsHelper _detailsHelper;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlanetController> _logger;

        public PlanetController(DetailsHelper detailsHelper, ApplicationDbContext context, ILogger<PlanetController> logger)
        {
            _detailsHelper = detailsHelper;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Details(string id, bool edit = false, string returnUrl = "")
        {
            var planet = await _detailsHelper.GetOrFetchAndCacheAsync<Planet>(id, "planets");

            if (planet == null)
            {
                _logger.LogWarning("Planet with ID {Id} was not found during fetch.", id?.Replace('\r', '_').Replace('\n', '_'));
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

            await PopulateAvailableFilmsAsync();

            var viewModel = new PlanetDetailsViewModel
            {
                Planet = planet,
                FilmNames = await _detailsHelper.GetNamesBatchAsync<Film>(planet.Films, f => f.Title)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Planet planet, [FromForm(Name = "selectedFilms")] string[] selectedFilms, string returnUrl = "")
        {
            if (!ModelState.IsValid)
            {
                ViewData["PageMode"] = "Edit";
                ViewData["ReturnUrl"] = returnUrl;
                planet.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new List<string>();

                await PopulateAvailableFilmsAsync();

                var viewModel = new PlanetDetailsViewModel
                {
                    Planet = planet,
                    FilmNames = await _detailsHelper.GetNamesBatchAsync<Film>(planet.Films, f => f.Title)
                };

                return View("Details", viewModel);
            }

            planet.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new List<string>();

            _context.Update(planet);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"{planet.Name} was successfully updated!";

            var numericId = planet.Url?.TrimEnd('/').Split('/').Last();

            return Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Details), new { id = numericId });
        }

        private async Task PopulateAvailableFilmsAsync()
        {
            ViewData["AvailableFilms"] = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();
        }
    }
}
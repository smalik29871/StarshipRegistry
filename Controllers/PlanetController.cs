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
    public class PlanetController : Controller
    {
        private readonly DetailsHelper _detailsHelper = null!;
        private readonly ApplicationDbContext _context;

        public PlanetController(DetailsHelper detailsHelper, ApplicationDbContext context)
        {
            _detailsHelper = detailsHelper;
            _context = context;
        }

        public async Task<IActionResult> Details(string id, bool edit = false, string returnUrl = "")
        {
            var planet = await _detailsHelper.GetOrFetchAndCacheAsync<Planet>(id, "planets");

            if (planet == null)
            {
                return NotFound();
            }

            ViewData["PageMode"] = edit ? "Edit" : "Details";
            ViewData["ReturnUrl"] = returnUrl;

            var films = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();

            ViewData["AvailableFilms"] = films;

            var filmNames = await _detailsHelper.GetFilmNamesBatchAsync(planet.Films);

            var viewModel = new PlanetDetailsViewModel
            {
                Planet = planet,
                FilmNames = filmNames
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Planet planet, [FromForm(Name = "selectedFilms")] string[] selectedFilms, string returnUrl = "")
        {
            if (ModelState.IsValid)
            {
                planet.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new();

                _context.Update(planet);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"{planet.Name} was successfully updated!";

                var numericId = planet.Url?.TrimEnd('/').Split('/').Last();
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Details), new { id = numericId });
            }

            ViewData["PageMode"] = "Edit";
            ViewData["ReturnUrl"] = returnUrl;
            planet.Films ??= new();

            var films = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();

            ViewData["AvailableFilms"] = films;

            var viewModel = new PlanetDetailsViewModel
            {
                Planet = planet,
                FilmNames = await _detailsHelper.GetFilmNamesBatchAsync(planet.Films)
            };

            return View("Details", viewModel);
        }
    }
}

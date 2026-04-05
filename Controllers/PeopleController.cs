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
    public class PeopleController : Controller
    {
        private readonly DetailsHelper _detailsHelper = null!;
        private readonly ApplicationDbContext _context;

        public PeopleController(DetailsHelper detailsHelper, ApplicationDbContext context)
        {
            _detailsHelper = detailsHelper;
            _context = context;
        }

        public async Task<IActionResult> Details(string id, bool edit = false, string returnUrl = "")
        {
            var person = await _detailsHelper.GetOrFetchAndCacheAsync<Character>(id, "people");

            if (person == null)
            {
                return NotFound();
            }

            ViewData["PageMode"] = edit ? "Edit" : "Details";
            ViewData["ReturnUrl"] = returnUrl;

            var films = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();
            var starships = await _context.Starships.OrderBy(s => s.Name).ToListAsync();

            ViewData["AvailableFilms"] = films;
            ViewData["AvailableStarships"] = starships;

            var filmNames = await _detailsHelper.GetFilmNamesBatchAsync(person.Films);
            var starshipNames = await _detailsHelper.GetStarshipNamesBatchAsync(person.Starships);

            var viewModel = new PeopleDetailsViewModel
            {
                Character = person,
                FilmNames = filmNames,
                StarshipNames = starshipNames
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Character character, [FromForm(Name = "selectedFilms")] string[] selectedFilms, [FromForm(Name = "selectedStarships")] string[] selectedStarships, string returnUrl = "")
        {
            if (ModelState.IsValid)
            {
                character.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new();
                character.Starships = selectedStarships?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new();

                _context.Update(character);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"{character.Name} was successfully updated!";

                var numericId = character.Url?.TrimEnd('/').Split('/').Last();
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Details), new { id = numericId });
            }

            ViewData["PageMode"] = "Edit";
            ViewData["ReturnUrl"] = returnUrl;
            character.Films ??= new();
            character.Starships ??= new();

            var films = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();
            var starships = await _context.Starships.OrderBy(s => s.Name).ToListAsync();

            ViewData["AvailableFilms"] = films;
            ViewData["AvailableStarships"] = starships;

            var viewModel = new PeopleDetailsViewModel
            {
                Character = character,
                FilmNames = await _detailsHelper.GetFilmNamesBatchAsync(character.Films),
                StarshipNames = await _detailsHelper.GetStarshipNamesBatchAsync(character.Starships)
            };

            return View("Details", viewModel);
        }
    }
}

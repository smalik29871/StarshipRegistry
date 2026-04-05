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
    public class SpeciesController : Controller
    {
        private readonly DetailsHelper _detailsHelper = null!;
        private readonly ApplicationDbContext _context;

        public SpeciesController(DetailsHelper detailsHelper, ApplicationDbContext context)
        {
            _detailsHelper = detailsHelper;
            _context = context;
        }

        public async Task<IActionResult> Details(string id, bool edit = false, string returnUrl = "")
        {
            var species = await _detailsHelper.GetOrFetchAndCacheAsync<Species>(id, "species");

            if (species == null)
            {
                return NotFound();
            }

            ViewData["PageMode"] = edit ? "Edit" : "Details";
            ViewData["ReturnUrl"] = returnUrl;

            var characters = await _context.Characters.OrderBy(c => c.Name).ToListAsync();
            var films = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();

            ViewData["AvailableCharacters"] = characters;
            ViewData["AvailableFilms"] = films;

            var characterNames = await _detailsHelper.GetCharacterNamesBatchAsync(species.People);
            var filmNames = await _detailsHelper.GetFilmNamesBatchAsync(species.Films);

            var viewModel = new SpeciesDetailsViewModel
            {
                Species = species,
                CharacterNames = characterNames,
                FilmNames = filmNames
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Species species, [FromForm(Name = "selectedPeople")] string[] selectedPeople, [FromForm(Name = "selectedFilms")] string[] selectedFilms, string returnUrl = "")
        {
            if (ModelState.IsValid)
            {
                species.People = selectedPeople?.Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new();
                species.Films = selectedFilms?.Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new();

                _context.Update(species);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"{species.Name} was successfully updated!";

                var numericId = species.Url?.TrimEnd('/').Split('/').Last();
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Details), new { id = numericId });
            }

            ViewData["PageMode"] = "Edit";
            ViewData["ReturnUrl"] = returnUrl;
            species.People ??= new();
            species.Films ??= new();

            var characters = await _context.Characters.OrderBy(c => c.Name).ToListAsync();
            var films = await _context.Films.OrderBy(f => f.EpisodeId).ToListAsync();

            ViewData["AvailableCharacters"] = characters;
            ViewData["AvailableFilms"] = films;

            var viewModel = new SpeciesDetailsViewModel
            {
                Species = species,
                CharacterNames = await _detailsHelper.GetCharacterNamesBatchAsync(species.People),
                FilmNames = await _detailsHelper.GetFilmNamesBatchAsync(species.Films)
            };

            return View("Details", viewModel);
        }
    }
}

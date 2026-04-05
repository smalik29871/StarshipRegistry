using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarshipRegistry.Data;
using StarshipRegistry.Models;

namespace StarshipRegistry.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Total Ships count works perfectly regardless of types!
            ViewBag.TotalShips = await _context.Starships.CountAsync();

            // Pull the raw string data into memory so we can safely parse it
            var shipData = await _context.Starships
                .Select(s => new { s.Name, s.CostInCredits, s.HyperdriveRating })
                .ToListAsync();

            // 2. Calculate Total Cost by safely parsing strings to numbers
            ViewBag.TotalCost = shipData
                .Where(s => !string.IsNullOrEmpty(s.CostInCredits) && s.CostInCredits != "unknown")
                .Select(s => long.TryParse(s.CostInCredits, out long cost) ? cost : 0)
                .Sum();

            // 3. Find the Top Speed Ship by safely parsing strings to decimals
            ViewBag.TopSpeedShip = shipData
                .Where(s => !string.IsNullOrEmpty(s.HyperdriveRating) && s.HyperdriveRating != "unknown")
                .Select(s => new
                {
                    s.Name,
                    Rating = double.TryParse(s.HyperdriveRating, out double rating) ? rating : 0
                })
                .OrderByDescending(s => s.Rating)
                .Select(s => s.Name)
                .FirstOrDefault() ?? "None";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
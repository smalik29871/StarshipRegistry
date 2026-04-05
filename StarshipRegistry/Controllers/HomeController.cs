using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarshipRegistry.Data;
using StarshipRegistry.Models;
using StarshipRegistry.Models.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace StarshipRegistry.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.TotalShips = await _context.Starships.CountAsync();

                // 1. Let's do the string filtering at the database level FIRST. 
                // This keeps memory low by avoiding pulling rows we can't use anyway.
                var validCostQuery = _context.Starships
                    .Where(s => s.CostInCredits != null && s.CostInCredits != "" && s.CostInCredits != "unknown");

                var shipCosts = await validCostQuery
                    .Select(s => s.CostInCredits)
                    .ToListAsync();

                // 2. Safely parse the DB results in memory — EF Core cannot translate custom numeric
                // string-parsing logic into SQL, so we pull the raw string values and parse in .NET.
                ViewBag.TotalCost = shipCosts
                    .Select(costStr => long.TryParse(costStr, out long parsedCost) ? parsedCost : 0)
                    .Sum();

                // 3. Keep database payload thin by selecting only the two columns we need for rating
                var speedData = await _context.Starships
                    .Where(s => s.HyperdriveRating != null && s.HyperdriveRating != "" && s.HyperdriveRating != "unknown")
                    .Select(s => new { s.Name, s.HyperdriveRating })
                    .ToListAsync();

                ViewBag.TopSpeedShip = speedData
                    .Select(s => new
                    {
                        s.Name,
                        Rating = double.TryParse(s.HyperdriveRating, out double rating) ? rating : 0
                    })
                    .OrderByDescending(s => s.Rating)
                    .Select(s => s.Name)
                    .FirstOrDefault() ?? "None";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compute dashboard metrics on HomeController.");
                ViewBag.TotalCost = 0;
                ViewBag.TopSpeedShip = "N/A";
            }

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
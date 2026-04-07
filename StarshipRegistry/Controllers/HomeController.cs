using Microsoft.AspNetCore.Authorization;
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
        private readonly IConfiguration _config;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger, IConfiguration config)
        {
            _context = context;
            _logger = logger;
            _config = config;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.TotalShips = await _context.Starships.CountAsync();

                var validCostQuery = _context.Starships
                    .Where(s => s.CostInCredits != null && s.CostInCredits != "" && s.CostInCredits != "unknown");

                var shipCosts = await validCostQuery
                    .Select(s => s.CostInCredits)
                    .ToListAsync();

                ViewBag.TotalCost = shipCosts
                    .Select(costStr => long.TryParse(costStr, out long parsedCost) ? parsedCost : 0)
                    .Sum();

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

            ViewBag.RegistrationCode = _config["Auth:RegistrationCode"] ?? string.Empty;

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
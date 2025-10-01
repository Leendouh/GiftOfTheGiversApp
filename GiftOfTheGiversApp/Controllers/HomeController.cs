using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace GiftOfTheGiversApp.Controllers
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
            // Get counts for dashboard
            var disasterCount = await _context.Disasters.CountAsync();
            var volunteerCount = await _context.Volunteers.CountAsync();
            var activeMissions = await _context.Missions.CountAsync(m => m.Status == "Open" || m.Status == "In Progress");
            var totalDonations = await _context.Donations.SumAsync(d => d.Quantity);

            ViewBag.DisasterCount = disasterCount;
            ViewBag.VolunteerCount = volunteerCount;
            ViewBag.ActiveMissions = activeMissions;
            ViewBag.TotalDonations = totalDonations;

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

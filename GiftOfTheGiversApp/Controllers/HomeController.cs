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
            if (User.Identity.IsAuthenticated)
            {
                // Get counts for dashboard
                var disasterCount = await _context.Disasters
                    .Where(d => d.Status == "Active")
                    .CountAsync();

                var volunteerCount = await _context.Volunteers.CountAsync();

                var activeMissions = await _context.Missions
                    .Where(m => m.Status == "Open" || m.Status == "In Progress")
                    .CountAsync();

                var activeAssignments = await _context.Assignments
                    .Where(a => a.Status == "Assigned")
                    .CountAsync();

                // Recent missions for activity feed
                var recentMissions = await _context.Missions
                    .Include(m => m.Disaster)
                    .OrderByDescending(m => m.CreatedDate)
                    .Take(5)
                    .Select(m => new
                    {
                        m.Title,
                        m.Status,
                        DisasterName = m.Disaster.Name
                    })
                    .ToListAsync();

                ViewBag.DisasterCount = disasterCount;
                ViewBag.VolunteerCount = volunteerCount;
                ViewBag.ActiveMissions = activeMissions;
                ViewBag.ActiveAssignments = activeAssignments;
                ViewBag.RecentMissions = recentMissions;
            }
            else
            {
                // For non-authenticated users, show public statistics
                var publicDisasterCount = await _context.Disasters
                    .Where(d => d.Status == "Active")
                    .CountAsync();

                var publicVolunteerCount = await _context.Volunteers.CountAsync();

                ViewBag.PublicDisasterCount = publicDisasterCount;
                ViewBag.PublicVolunteerCount = publicVolunteerCount;
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
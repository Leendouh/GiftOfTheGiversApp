using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GiftOfTheGiversApp.Controllers
{
    [Authorize]
    public class MissionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MissionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Missions
        public async Task<IActionResult> Index()
        {
            var missions = await _context.Missions
                .Include(m => m.Disaster)
                .Include(m => m.AssignedTo)
                .Include(m => m.CreatedBy)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();

            return View(missions);
        }

        // GET: Missions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var mission = await _context.Missions
                .Include(m => m.Disaster)
                .Include(m => m.AssignedTo)
                .Include(m => m.CreatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mission == null) return NotFound();

            return View(mission);
        }

        // GET: Missions/Create
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Disasters = await _context.Disasters.Where(d => d.Status == "Active").ToListAsync();
            ViewBag.Volunteers = await _context.Volunteers
                .Include(v => v.User)
                .Where(v => v.AvailabilityStatus == "Active")
                .ToListAsync();
            return View();
        }

        // POST: Missions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Create(Mission mission)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                mission.CreatedById = userId;
                mission.CreatedDate = DateTime.Now;

                _context.Add(mission);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Mission created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Disasters = await _context.Disasters.Where(d => d.Status == "Active").ToListAsync();
            ViewBag.Volunteers = await _context.Volunteers
                .Include(v => v.User)
                .Where(v => v.AvailabilityStatus == "Active")
                .ToListAsync();
            return View(mission);
        }

        // GET: Missions/Edit/5
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var mission = await _context.Missions.FindAsync(id);
            if (mission == null) return NotFound();

            ViewBag.Disasters = await _context.Disasters.ToListAsync();
            ViewBag.Volunteers = await _context.Volunteers
                .Include(v => v.User)
                .ToListAsync();
            return View(mission);
        }

        // POST: Missions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Edit(int id, Mission mission)
        {
            if (id != mission.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mission);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Mission updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MissionExists(mission.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Disasters = await _context.Disasters.ToListAsync();
            ViewBag.Volunteers = await _context.Volunteers
                .Include(v => v.User)
                .ToListAsync();
            return View(mission);
        }

        // POST: Missions/UpdateStatus/5
        [HttpPost]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var mission = await _context.Missions.FindAsync(id);
            if (mission == null) return NotFound();

            mission.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Mission status updated to {status}";
            return RedirectToAction(nameof(Index));
        }

        // GET: Missions/MyMissions
        public async Task<IActionResult> MyMissions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get volunteer ID for current user
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null)
            {
                TempData["ErrorMessage"] = "You need to be registered as a volunteer to view missions.";
                return RedirectToAction("Index", "Volunteers");
            }

            var missions = await _context.Missions
                .Include(m => m.Disaster)
                .Include(m => m.CreatedBy)
                .Where(m => m.AssignedToId == volunteer.Id)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();

            return View(missions);
        }

        private bool MissionExists(int id)
        {
            return _context.Missions.Any(e => e.Id == id);
        }
    }
}
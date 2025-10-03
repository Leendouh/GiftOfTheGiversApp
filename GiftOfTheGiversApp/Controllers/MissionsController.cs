using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using GiftOfTheGiversApp.ViewModels;

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

        // GET: Missions - All authenticated users can view
        public async Task<IActionResult> Index()
        {
            var missions = await _context.Missions
                .Include(m => m.Disaster)
                .Include(m => m.AssignedTo)
                    .ThenInclude(v => v.User)
                .Include(m => m.CreatedBy)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();

            return View(missions);
        }

        // GET: Missions/Details/5 - All authenticated users can view
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var mission = await _context.Missions
                .Include(m => m.Disaster)
                .Include(m => m.AssignedTo)
                    .ThenInclude(v => v.User)
                .Include(m => m.CreatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mission == null) return NotFound();

            // Get related assignments for this mission's disaster
            var assignments = await _context.Assignments
                .Include(a => a.Volunteer)
                    .ThenInclude(v => v.User)
                .Where(a => a.DisasterId == mission.DisasterId)
                .OrderByDescending(a => a.AssignmentDate)
                .ToListAsync();

            ViewBag.Assignments = assignments;

            return View(mission);
        }

        // GET: Missions/Create - Only Admin/Coordinator
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Create()
        {
            var viewModel = new MissionViewModel
            {
                Disasters = await _context.Disasters
                    .Where(d => d.Status == "Active")
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = $"{d.Name} - {d.Location}"
                    })
                    .ToListAsync(),

                // REMOVE the Where clause to show ALL volunteers, not just available ones
                Volunteers = await _context.Volunteers
                    .Include(v => v.User)
                    // .Where(v => v.AvailabilityStatus == "Available") // Remove this line
                    .Select(v => new SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = $"{v.User.FullName} - {v.Skills} - Status: {v.AvailabilityStatus}"
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Missions/Create - Only Admin/Coordinator
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Create(MissionViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    // Convert ViewModel to Entity - only use properties that exist in Mission model
                    var mission = new Mission
                    {
                        DisasterId = viewModel.DisasterId,
                        Title = viewModel.Title,
                        Description = viewModel.Description,
                        AssignedToId = viewModel.AssignedToId,
                        Status = viewModel.Status,
                        Priority = viewModel.Priority,
                        DueDate = viewModel.DueDate,
                        CreatedById = userId,
                        CreatedDate = DateTime.Now
                    };

                    _context.Add(mission);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Mission created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving the mission: " + ex.Message);
                }
            }

            // Reload dropdowns if validation fails
            await ReloadMissionDropdowns(viewModel);
            return View(viewModel);
        }

        // GET: Missions/Edit/5 - Only Admin/Coordinator
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var mission = await _context.Missions.FindAsync(id);
            if (mission == null) return NotFound();

            // Convert Entity to ViewModel
            var viewModel = new MissionViewModel
            {
                Id = mission.Id,
                DisasterId = mission.DisasterId,
                Title = mission.Title,
                Description = mission.Description,
                AssignedToId = mission.AssignedToId,
                Status = mission.Status,
                Priority = mission.Priority,
                DueDate = mission.DueDate,
                Disasters = await _context.Disasters
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = $"{d.Name} - {d.Location}"
                    })
                    .ToListAsync(),
                Volunteers = await _context.Volunteers
                    .Include(v => v.User)
                    .Select(v => new SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = $"{v.User.FullName} - {v.Skills}"
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Missions/Edit/5 - Only Admin/Coordinator
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Edit(int id, MissionViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingMission = await _context.Missions.FindAsync(id);
                    if (existingMission == null) return NotFound();

                    // Update entity from ViewModel - only update properties that exist
                    existingMission.DisasterId = viewModel.DisasterId;
                    existingMission.Title = viewModel.Title;
                    existingMission.Description = viewModel.Description;
                    existingMission.AssignedToId = viewModel.AssignedToId;
                    existingMission.Status = viewModel.Status;
                    existingMission.Priority = viewModel.Priority;
                    existingMission.DueDate = viewModel.DueDate;

                    _context.Update(existingMission);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Mission updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MissionExists(id))
                        return NotFound();
                    else
                        throw;
                }
            }

            // Reload dropdowns if validation fails
            await ReloadMissionDropdowns(viewModel);
            return View(viewModel);
        }

        // GET: Missions/Missions - Volunteers can see their assigned missions
        public async Task<IActionResult> Missions()
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
                .Include(m => m.AssignedTo)
                    .ThenInclude(v => v.User)
                .Where(m => m.AssignedToId == volunteer.Id)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();

            return View(missions);
        }

        // POST: Missions/UpdateStatus/5 - Only Admin/Coordinator
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        // GET: Missions/Delete/5 - Only Admin/Coordinator
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var mission = await _context.Missions
                .Include(m => m.Disaster)
                .Include(m => m.AssignedTo)
                    .ThenInclude(v => v.User)
                .Include(m => m.CreatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mission == null) return NotFound();

            return View(mission);
        }

        // POST: Missions/Delete/5 - Only Admin/Coordinator
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mission = await _context.Missions.FindAsync(id);
            if (mission != null)
            {
                _context.Missions.Remove(mission);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Mission deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper method to reload dropdowns
        private async Task ReloadMissionDropdowns(MissionViewModel viewModel)
        {
            viewModel.Disasters = await _context.Disasters
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = $"{d.Name} - {d.Location}"
                })
                .ToListAsync();

            viewModel.Volunteers = await _context.Volunteers
                .Include(v => v.User)
                // Remove the Where clause here too
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = $"{v.User.FullName} - {v.Skills} - Status: {v.AvailabilityStatus}"
                })
                .ToListAsync();
        }

        private bool MissionExists(int id)
        {
            return _context.Missions.Any(e => e.Id == id);
        }
    }
}
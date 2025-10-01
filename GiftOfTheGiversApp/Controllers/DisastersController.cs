using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using GiftOfTheGiversApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GiftOfTheGiversApp.Controllers
{
    [Authorize]
    public class DisastersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PermissionService _permissionService;
        public DisastersController(ApplicationDbContext context, PermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        // GET: Disasters
        public async Task<IActionResult> Index()
        {
            var disasters = await _context.Disasters
                .Include(d => d.ReportedBy)
                .OrderByDescending(d => d.StartDate)
                .ToListAsync();
            return View(disasters);
        }

        // GET: Disasters/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var disaster = await _context.Disasters
                .Include(d => d.ReportedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (disaster == null) return NotFound();
            return View(disaster);
        }

        // GET: Disasters/Create
        public IActionResult Create()
        {
            return View(new DisasterViewModel());
        }

        // POST: Disasters/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DisasterViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var disaster = new Disaster
                    {
                        Name = viewModel.Name,
                        Location = viewModel.Location,
                        Description = viewModel.Description,
                        DisasterType = viewModel.DisasterType,
                        SeverityLevel = viewModel.SeverityLevel,
                        EstimatedAffected = viewModel.EstimatedAffected,
                        ReportedById = User.FindFirstValue(ClaimTypes.NameIdentifier),
                        StartDate = DateTime.Now,
                        Status = "Active"
                    };

                    _context.Disasters.Add(disaster);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Disaster '{disaster.Name}' reported successfully!";
                    return RedirectToAction(nameof(Details), new { id = disaster.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving the disaster. Please try again.");
                }
            }
            return View(viewModel);
        }

        // GET: Disasters/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var disaster = await _context.Disasters.FindAsync(id);
            if (disaster == null) return NotFound();

            // Security: Only allow original reporter or admin to edit
            var permissions = await _permissionService.GetUserPermissionsAsync(User, disaster.ReportedById);

            if (!permissions.CanEditAllDisasters && !permissions.CanEditOwnDisasters)
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this disaster.";
                return RedirectToAction(nameof(Details), new { id = disaster.Id });
            }

            var viewModel = new DisasterViewModel
            {
                Id = disaster.Id,
                Name = disaster.Name,
                Location = disaster.Location,
                Description = disaster.Description,
                DisasterType = disaster.DisasterType,
                SeverityLevel = disaster.SeverityLevel,
                Status = disaster.Status,
                EstimatedAffected = disaster.EstimatedAffected
            };

            return View(viewModel);
        }

        // POST: Disasters/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DisasterViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var disaster = await _context.Disasters.FindAsync(id);
                    if (disaster == null) return NotFound();

                    // Security: Only allow original reporter or admin to edit
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (disaster.ReportedById != currentUserId && !User.IsInRole("Admin"))
                    {
                        TempData["ErrorMessage"] = "You can only edit disasters that you reported.";
                        return RedirectToAction(nameof(Details), new { id = disaster.Id });
                    }

                    // Update only allowed fields
                    disaster.Name = viewModel.Name;
                    disaster.Location = viewModel.Location;
                    disaster.Description = viewModel.Description;
                    disaster.DisasterType = viewModel.DisasterType;
                    disaster.SeverityLevel = viewModel.SeverityLevel;
                    disaster.Status = viewModel.Status;
                    disaster.EstimatedAffected = viewModel.EstimatedAffected;

                    _context.Update(disaster);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Disaster updated successfully!";
                    return RedirectToAction(nameof(Details), new { id = disaster.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DisasterExists(id)) return NotFound();
                    else throw;
                }
            }
            return View(viewModel);
        }

        // GET: Disasters/Resolve/5 - Safe alternative to delete for regular users
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Resolve(int? id)
        {
            if (id == null) return NotFound();

            var disaster = await _context.Disasters.FindAsync(id);
            if (disaster == null) return NotFound();

            // Allow coordinators and admins to resolve any disaster
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (disaster.ReportedById != currentUserId && !User.IsInRole("Admin") && !User.IsInRole("Coordinator"))
            {
                TempData["ErrorMessage"] = "Only admins, coordinators, or the original reporter can resolve disasters.";
                return RedirectToAction(nameof(Details), new { id = disaster.Id });
            }

            disaster.Status = "Resolved";
            _context.Update(disaster);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Disaster '{disaster.Name}' has been marked as resolved.";
            return RedirectToAction(nameof(Details), new { id = disaster.Id });
        }

        // GET: Disasters/Delete/5 - Only for Admins
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var disaster = await _context.Disasters
                .Include(d => d.ReportedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (disaster == null) return NotFound();

            return View(disaster);
        }

        // POST: Disasters/Delete/5 - Only for Admins
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var disaster = await _context.Disasters.FindAsync(id);
            if (disaster == null) return NotFound();

            var disasterName = disaster.Name;
            _context.Disasters.Remove(disaster);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Disaster '{disasterName}' has been permanently deleted.";
            return RedirectToAction(nameof(Index));
        }

        private bool DisasterExists(int id)
        {
            return _context.Disasters.Any(e => e.Id == id);
        }

        // Helper method to check if user is admin
        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        // Helper method to check if user is coordinator or admin
        private bool IsCoordinatorOrAdmin()
        {
            return User.IsInRole("Admin") || User.IsInRole("Coordinator");
        }
    }
}
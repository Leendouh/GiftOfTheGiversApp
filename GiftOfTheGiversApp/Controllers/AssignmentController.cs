using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using GiftOfTheGiversApp.ViewModels;
using Microsoft.Extensions.Logging;

namespace GiftOfTheGiversApp.Controllers
{
    [Authorize]
    public class AssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AssignmentsController> _logger;

        public AssignmentsController(ApplicationDbContext context, ILogger<AssignmentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Assignments - For Admin/Coordinator to view all assignments
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Index()
        {
            var assignments = await _context.Assignments
                .Include(a => a.Volunteer)
                    .ThenInclude(v => v.User)
                .Include(a => a.Disaster)
                .Include(a => a.AssignedBy)
                .OrderByDescending(a => a.AssignmentDate)
                .ToListAsync();

            return View(assignments);
        }

        // GET: Debug Volunteers - Diagnostic page
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DebugVolunteers()
        {
            var volunteers = await _context.Volunteers
                .Include(v => v.User)
                .Select(v => new
                {
                    v.Id,
                    v.Skills,
                    AvailabilityStatus = v.AvailabilityStatus ?? "Null",
                    UserId = v.UserId ?? "Null",
                    UserFullName = v.User != null ? v.User.FullName : "No User",
                    HasUser = v.User != null
                })
                .ToListAsync();

            ViewBag.Volunteers = volunteers;
            return View();
        }

        // GET: Create Test Volunteer
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTestVolunteer()
        {
            try
            {
                // Get the current user
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = await _context.Users.FindAsync(currentUserId);

                // Create a test volunteer linked to current user
                var volunteer = new Volunteer
                {
                    UserId = currentUserId,
                    Skills = "First Aid, Logistics, Communication",
                    AvailabilityStatus = "Available",
                    // Add other required properties from your Volunteer model
                    EmergencyContact = currentUser?.PhoneNumber ?? "Not set",
                    Address = "Test Address"
                };

                _context.Volunteers.Add(volunteer);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Test volunteer created successfully!";
                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating test volunteer: {ex.Message}";
                return RedirectToAction(nameof(Create));
            }
        }

        // GET: Assignments/Create - Assign volunteer to disaster
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Create()
        {
            try
            {
                // Get available volunteers with multiple fallback approaches
                var volunteers = await GetVolunteersForDropdown();

                var viewModel = new AssignmentViewModel
                {
                    Disasters = await _context.Disasters
                        .Where(d => d.Status == "Active")
                        .Select(d => new SelectListItem
                        {
                            Value = d.Id.ToString(),
                            Text = $"{d.Name} - {d.Location}"
                        })
                        .ToListAsync(),

                    Volunteers = volunteers,

                    Roles = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "Team Lead", Text = "Team Lead" },
                        new SelectListItem { Value = "Medic", Text = "Medical Staff" },
                        new SelectListItem { Value = "Distributor", Text = "Relief Distributor" },
                        new SelectListItem { Value = "Logistics", Text = "Logistics Coordinator" },
                        new SelectListItem { Value = "Assessor", Text = "Damage Assessor" },
                        new SelectListItem { Value = "Driver", Text = "Driver" },
                        new SelectListItem { Value = "General", Text = "General Volunteer" }
                    }
                };

                // Store counts for debugging
                ViewBag.VolunteerCount = volunteers.Count;
                ViewBag.DisasterCount = viewModel.Disasters.Count;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading assignment create form");
                TempData["Error"] = "Error loading assignment form. Please check if volunteers and disasters exist.";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<List<SelectListItem>> GetVolunteersForDropdown()
        {
            // Approach 1: Try with AvailabilityStatus = "Available" and User relationship
            var volunteers = await _context.Volunteers
                .Include(v => v.User)
                .Where(v => v.AvailabilityStatus == "Available")
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = v.User != null ?
                           $"{v.User.FullName} - {v.Skills}" :
                           $"Volunteer {v.Id} - {v.Skills}"
                })
                .ToListAsync();

            // If no available volunteers, try getting any volunteers except assigned
            if (!volunteers.Any())
            {
                volunteers = await _context.Volunteers
                    .Include(v => v.User)
                    .Where(v => v.AvailabilityStatus != "Assigned") // Exclude already assigned
                    .Select(v => new SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = v.User != null ?
                               $"{v.User.FullName} - {v.Skills} ({v.AvailabilityStatus})" :
                               $"Volunteer {v.Id} - {v.Skills} ({v.AvailabilityStatus})"
                    })
                    .ToListAsync();
            }

            // If still no volunteers, get all volunteers
            if (!volunteers.Any())
            {
                volunteers = await _context.Volunteers
                    .Include(v => v.User)
                    .Select(v => new SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = v.User != null ?
                               $"{v.User.FullName} - {v.Skills} ({v.AvailabilityStatus})" :
                               $"Volunteer {v.Id} - {v.Skills} ({v.AvailabilityStatus})"
                    })
                    .ToListAsync();
            }

            return volunteers;
        }

        // POST: Assignments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Create(AssignmentViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    // Check if assignment already exists
                    var existingAssignment = await _context.Assignments
                        .FirstOrDefaultAsync(a => a.VolunteerId == viewModel.VolunteerId &&
                                                a.DisasterId == viewModel.DisasterId &&
                                                a.Status == "Assigned");

                    if (existingAssignment != null)
                    {
                        ModelState.AddModelError("", "This volunteer is already assigned to this disaster.");
                    }
                    else
                    {
                        var assignment = new Assignment
                        {
                            VolunteerId = viewModel.VolunteerId,
                            DisasterId = viewModel.DisasterId,
                            RoleInAssignment = viewModel.RoleInAssignment,
                            Status = viewModel.Status,
                            AssignmentDate = DateTime.Now,
                            AssignedById = userId
                        };

                        _context.Add(assignment);

                        // Update volunteer availability if assigned
                        if (viewModel.Status == "Assigned")
                        {
                            var volunteer = await _context.Volunteers.FindAsync(viewModel.VolunteerId);
                            if (volunteer != null)
                            {
                                volunteer.AvailabilityStatus = "Assigned";
                                _context.Update(volunteer);
                            }
                        }

                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Volunteer assigned successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating assignment");
                    ModelState.AddModelError("", "An error occurred while creating the assignment: " + ex.Message);
                }
            }

            // Reload dropdowns if validation fails
            await ReloadAssignmentDropdowns(viewModel);
            return View(viewModel);
        }

        // GET: Assignments/Edit/5
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Volunteer)
                .Include(a => a.Disaster)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null) return NotFound();

            var viewModel = new AssignmentViewModel
            {
                Id = assignment.Id,
                VolunteerId = assignment.VolunteerId,
                DisasterId = assignment.DisasterId,
                RoleInAssignment = assignment.RoleInAssignment,
                Status = assignment.Status
            };

            await ReloadAssignmentDropdowns(viewModel);
            return View(viewModel);
        }

        // POST: Assignments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Edit(int id, AssignmentViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var assignment = await _context.Assignments
                        .Include(a => a.Volunteer)
                        .FirstOrDefaultAsync(a => a.Id == id);

                    if (assignment == null) return NotFound();

                    // Update volunteer availability based on status change
                    if (assignment.Status != viewModel.Status)
                    {
                        var volunteer = await _context.Volunteers.FindAsync(assignment.VolunteerId);
                        if (volunteer != null)
                        {
                            volunteer.AvailabilityStatus = viewModel.Status == "Assigned" ? "Assigned" : "Available";
                            _context.Update(volunteer);
                        }
                    }

                    assignment.RoleInAssignment = viewModel.RoleInAssignment;
                    assignment.Status = viewModel.Status;

                    _context.Update(assignment);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Assignment updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AssignmentExists(id))
                        return NotFound();
                    else
                        throw;
                }
            }

            await ReloadAssignmentDropdowns(viewModel);
            return View(viewModel);
        }

        // POST: Assignments/UpdateStatus/5
        [HttpPost]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            try
            {
                var assignment = await _context.Assignments
                    .Include(a => a.Volunteer)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (assignment == null) return NotFound();

                assignment.Status = status;

                // Update volunteer availability if assignment is completed or cancelled
                if (status == "Completed" || status == "Cancelled")
                {
                    if (assignment.Volunteer != null)
                    {
                        assignment.Volunteer.AvailabilityStatus = "Available";
                        _context.Update(assignment.Volunteer);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Assignment status updated to {status}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating assignment status");
                TempData["Error"] = "Error updating assignment status";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: MyAssignments - For volunteers to see their assignments
        public async Task<IActionResult> Assignments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get volunteer ID for current user
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null)
            {
                TempData["ErrorMessage"] = "You need to be registered as a volunteer to view assignments.";
                return RedirectToAction("Index", "Volunteers");
            }

            var assignments = await _context.Assignments
                .Include(a => a.Disaster)
                .Include(a => a.AssignedBy)
                .Where(a => a.VolunteerId == volunteer.Id)
                .OrderByDescending(a => a.AssignmentDate)
                .ToListAsync();

            return View(assignments);
        }

        // GET: Assignments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Volunteer)
                    .ThenInclude(v => v.User)
                .Include(a => a.Disaster)
                .Include(a => a.AssignedBy)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null) return NotFound();

            return View(assignment);
        }

        // POST: Assignments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var assignment = await _context.Assignments
                    .Include(a => a.Volunteer)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (assignment != null)
                {
                    // Update volunteer availability before deletion
                    if (assignment.Volunteer != null && assignment.Status == "Assigned")
                    {
                        assignment.Volunteer.AvailabilityStatus = "Available";
                        _context.Update(assignment.Volunteer);
                    }

                    _context.Assignments.Remove(assignment);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Assignment deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Assignment not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting assignment");
                TempData["Error"] = "Error deleting assignment.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task ReloadAssignmentDropdowns(AssignmentViewModel viewModel)
        {
            viewModel.Disasters = await _context.Disasters
                .Where(d => d.Status == "Active")
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = $"{d.Name} - {d.Location}"
                })
                .ToListAsync();

            viewModel.Volunteers = await GetVolunteersForDropdown();

            viewModel.Roles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Team Lead", Text = "Team Lead" },
                new SelectListItem { Value = "Medic", Text = "Medical Staff" },
                new SelectListItem { Value = "Distributor", Text = "Relief Distributor" },
                new SelectListItem { Value = "Logistics", Text = "Logistics Coordinator" },
                new SelectListItem { Value = "Assessor", Text = "Damage Assessor" },
                new SelectListItem { Value = "Driver", Text = "Driver" },
                new SelectListItem { Value = "General", Text = "General Volunteer" }
            };
        }

        private bool AssignmentExists(int id)
        {
            return _context.Assignments.Any(e => e.Id == id);
        }
    }
}
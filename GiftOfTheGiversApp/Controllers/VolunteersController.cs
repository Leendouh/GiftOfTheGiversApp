using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GiftOfTheGiversApp.Controllers
{
    [Authorize]
    public class VolunteersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VolunteersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Volunteers
        public async Task<IActionResult> Index()
        {
            var volunteers = await _context.Volunteers
                .Include(v => v.User)
                .OrderBy(v => v.User.FirstName)
                .ToListAsync();
            return View(volunteers);
        }

        // GET: Volunteers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var volunteer = await _context.Volunteers
                .Include(v => v.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (volunteer == null) return NotFound();
            return View(volunteer);
        }

        // GET: Volunteers/Create
        public IActionResult Create()
        {
            // Check if user already has a volunteer profile
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingVolunteer = _context.Volunteers.FirstOrDefault(v => v.UserId == currentUserId);

            if (existingVolunteer != null)
            {
                TempData["InfoMessage"] = "You already have a volunteer profile!";
                return RedirectToAction(nameof(Details), new { id = existingVolunteer.Id });
            }

            return View(new VolunteerViewModel());
        }

        // POST: Volunteers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VolunteerViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    // Check if user already has a volunteer profile
                    var existingVolunteer = _context.Volunteers.FirstOrDefault(v => v.UserId == currentUserId);
                    if (existingVolunteer != null)
                    {
                        TempData["InfoMessage"] = "You already have a volunteer profile!";
                        return RedirectToAction(nameof(Details), new { id = existingVolunteer.Id });
                    }

                    var volunteer = new Volunteer
                    {
                        UserId = currentUserId,
                        Skills = viewModel.Skills,
                        AvailabilityStatus = viewModel.AvailabilityStatus,
                        Address = viewModel.Address,
                        EmergencyContact = viewModel.EmergencyContact,
                        DateRegistered = DateTime.Now
                    };

                    _context.Volunteers.Add(volunteer);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Volunteer profile created successfully!";
                    return RedirectToAction(nameof(Details), new { id = volunteer.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while creating your volunteer profile. Please try again.");
                }
            }
            return View(viewModel);
        }

        // GET: Volunteers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null) return NotFound();

            // Check if user owns this volunteer profile
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (volunteer.UserId != currentUserId)
            {
                TempData["ErrorMessage"] = "You can only edit your own volunteer profile.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new VolunteerViewModel
            {
                Skills = volunteer.Skills,
                AvailabilityStatus = volunteer.AvailabilityStatus,
                Address = volunteer.Address,
                EmergencyContact = volunteer.EmergencyContact
            };

            return View(viewModel);
        }

        // POST: Volunteers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VolunteerViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var volunteer = await _context.Volunteers.FindAsync(id);
                    if (volunteer == null) return NotFound();

                    // Check if user owns this volunteer profile
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (volunteer.UserId != currentUserId)
                    {
                        TempData["ErrorMessage"] = "You can only edit your own volunteer profile.";
                        return RedirectToAction(nameof(Index));
                    }

                    volunteer.Skills = viewModel.Skills;
                    volunteer.AvailabilityStatus = viewModel.AvailabilityStatus;
                    volunteer.Address = viewModel.Address;
                    volunteer.EmergencyContact = viewModel.EmergencyContact;

                    _context.Update(volunteer);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Volunteer profile updated successfully!";
                    return RedirectToAction(nameof(Details), new { id = volunteer.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VolunteerExists(id)) return NotFound();
                    else throw;
                }
            }
            return View(viewModel);
        }

        private bool VolunteerExists(int id)
        {
            return _context.Volunteers.Any(e => e.Id == id);
        }
    }
}

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
    public class DonationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Donations
        public async Task<IActionResult> Index()
        {
            var donations = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.Resource)
                    .ThenInclude(r => r.Category)
                .OrderByDescending(d => d.DonationDate)
                .ToListAsync();

            return View(donations);
        }

        // GET: Donations/Donations
        public async Task<IActionResult> Donations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var donations = await _context.Donations
                .Include(d => d.Resource)
                    .ThenInclude(r => r.Category)
                .Where(d => d.DonorId == userId)
                .OrderByDescending(d => d.DonationDate)
                .ToListAsync();

            return View(donations);
        }

        // GET: Donations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var donation = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.Resource)
                    .ThenInclude(r => r.Category)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (donation == null) return NotFound();

            return View(donation);
        }

        // GET: Donations/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new DonationCreateViewModel
            {
                Resources = await _context.Resources
                    .Include(r => r.Category)
                    .Where(r => r.CurrentQuantity >= 0)
                    .Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = $"{r.Name} - {r.Category.CategoryName} (Available: {r.CurrentQuantity})"
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Donations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DonationCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var user = await _context.Users.FindAsync(userId);

                    // Convert ViewModel to Entity
                    var donation = new Donation
                    {
                        DonorId = userId,
                        ResourceId = viewModel.ResourceId,
                        Quantity = viewModel.Quantity,
                        Notes = viewModel.Notes,
                        DonationDate = DateTime.Now,
                        Status = "Pending"
                    };

                    // Update resource quantity
                    var resource = await _context.Resources.FindAsync(donation.ResourceId);
                    if (resource != null)
                    {
                        resource.CurrentQuantity += donation.Quantity;
                    }

                    _context.Add(donation);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thank you for your donation! It will be reviewed and allocated to those in need.";
                    return RedirectToAction(nameof(Donations));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving the donation: " + ex.Message);
                }
            }

            // Reload resources if validation fails
            viewModel.Resources = await _context.Resources
                .Include(r => r.Category)
                .Where(r => r.CurrentQuantity >= 0)
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = $"{r.Name} - {r.Category.CategoryName} (Available: {r.CurrentQuantity})"
                })
                .ToListAsync();

            return View(viewModel);
        }

        // GET: Donations/Edit/5 (Admin only)
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var donation = await _context.Donations
                .Include(d => d.Resource)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (donation == null) return NotFound();

            ViewBag.Resources = await _context.Resources.ToListAsync();
            return View(donation);
        }

        // POST: Donations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Edit(int id, Donation donation)
        {
            if (id != donation.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(donation);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Donation updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DonationExists(donation.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Resources = await _context.Resources.ToListAsync();
            return View(donation);
        }

        // POST: Donations/UpdateStatus
        [HttpPost]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var donation = await _context.Donations.FindAsync(id);
            if (donation == null)
            {
                return NotFound();
            }

            donation.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Donation status updated to {status}";
            return RedirectToAction(nameof(Index)); // Fixed this line
        }

        private bool DonationExists(int id)
        {
            return _context.Donations.Any(e => e.Id == id);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using GiftOfTheGiversApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GiftOfTheGiversApp.Controllers
{
    public class DonationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Donations (Admin view - all donations)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var donations = await _context.Donations.ToListAsync();
            return View(donations);
        }

        // GET: My Donations (User-specific view)
        public async Task<IActionResult> Donations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var donations = await _context.Donations
                .Where(d => d.DonorId == userId)
                .ToListAsync();
            return View(donations);
        }

        // GET: Donations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donation = await _context.Donations
                .FirstOrDefaultAsync(m => m.Id == id);

            if (donation == null)
            {
                return NotFound();
            }

            // Check if user is authorized to view this donation
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && donation.DonorId != userId)
            {
                return Forbid();
            }

            return View(donation);
        }

        // GET: Donations/Create
        public async Task<IActionResult> Create()
        {
            var resources = await _context.Resources.ToListAsync();
            var viewModel = new DonationCreateViewModel
            {
                Resources = resources.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = $"{r.Name} ({r.UnitOfMeasure})"
                }).ToList()
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Create donation
                var donation = new Donation
                {
                    DonorId = userId,
                    ResourceId = viewModel.ResourceId,
                    Quantity = viewModel.Quantity,
                    Notes = viewModel.Notes,
                    Status = "Pending",
                    DonationDate = DateTime.Now
                };

                _context.Add(donation);

                // Update resource quantity
                var resource = await _context.Resources.FindAsync(viewModel.ResourceId);
                if (resource != null)
                {
                    resource.CurrentQuantity += viewModel.Quantity;
                    _context.Update(resource);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Donations));
            }

            // Reload resources if validation fails
            viewModel.Resources = (await _context.Resources.ToListAsync())
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = $"{r.Name} ({r.UnitOfMeasure})"
                }).ToList();

            return View(viewModel);
        }

        // GET: Donations/Edit/5 (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donation = await _context.Donations
                .FirstOrDefaultAsync(m => m.Id == id);

            if (donation == null)
            {
                return NotFound();
            }

            return View(donation);
        }

        // POST: Donations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Donation donation)
        {
            if (id != donation.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDonation = await _context.Donations.FindAsync(id);
                    if (existingDonation == null)
                    {
                        return NotFound();
                    }

                    // Update only the fields that should be editable
                    existingDonation.Quantity = donation.Quantity;
                    existingDonation.Status = donation.Status;
                    existingDonation.Notes = donation.Notes;

                    _context.Update(existingDonation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DonationExists(donation.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(donation);
        }

        // POST: Donations/UpdateStatus/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var donation = await _context.Donations.FindAsync(id);
            if (donation == null)
            {
                return NotFound();
            }

            donation.Status = status;
            _context.Update(donation);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool DonationExists(int id)
        {
            return _context.Donations.Any(e => e.Id == id);
        }
    }
}
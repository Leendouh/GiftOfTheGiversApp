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
    [Authorize(Roles = "Admin,Coordinator")]
    public class ResourceRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResourceRequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ResourceRequests
        public async Task<IActionResult> Index()
        {
            var requests = await _context.ResourceRequests
                .Include(r => r.Disaster)
                .Include(r => r.Resource)
                    .ThenInclude(res => res.Category)
                .Include(r => r.RequestedBy)
                .OrderByDescending(r => r.DateRequested)
                .ToListAsync();

            return View(requests);
        }

        // GET: ResourceRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.ResourceRequests
                .Include(r => r.Disaster)
                .Include(r => r.Resource)
                    .ThenInclude(res => res.Category)
                .Include(r => r.RequestedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            return View(request);
        }

        // GET: ResourceRequests/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new ResourceRequestViewModel
            {
                Disasters = await _context.Disasters
                    .Where(d => d.Status == "Active")
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = $"{d.Name} - {d.Location}"
                    })
                    .ToListAsync(),

                Resources = await _context.Resources
                    .Include(r => r.Category)
                    .Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = $"{r.Name} ({r.Category.CategoryName}) - Stock: {r.CurrentQuantity}"
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: ResourceRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResourceRequestViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    // Convert ViewModel to Entity
                    var request = new ResourceRequest
                    {
                        DisasterId = viewModel.DisasterId,
                        ResourceId = viewModel.ResourceId,
                        QuantityRequested = viewModel.QuantityRequested,
                        UrgencyLevel = viewModel.UrgencyLevel,
                        DateRequired = viewModel.DateRequired,
                        RequestedById = userId,
                        DateRequested = DateTime.Now,
                        Status = "Pending"
                    };

                    _context.Add(request);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Resource request submitted successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving the request: " + ex.Message);
                }
            }

            // Reload dropdowns if validation fails
            viewModel.Disasters = await _context.Disasters
                .Where(d => d.Status == "Active")
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = $"{d.Name} - {d.Location}"
                })
                .ToListAsync();

            viewModel.Resources = await _context.Resources
                .Include(r => r.Category)
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = $"{r.Name} ({r.Category.CategoryName}) - Stock: {r.CurrentQuantity}"
                })
                .ToListAsync();

            return View(viewModel);
        }

        // GET: ResourceRequests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.ResourceRequests.FindAsync(id);
            if (request == null) return NotFound();

            ViewBag.Disasters = await _context.Disasters
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = $"{d.Name} - {d.Location}",
                    Selected = d.Id == request.DisasterId
                })
                .ToListAsync();

            ViewBag.Resources = await _context.Resources
                .Include(r => r.Category)
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = $"{r.Name} ({r.Category.CategoryName})",
                    Selected = r.Id == request.ResourceId
                })
                .ToListAsync();

            return View(request);
        }

        // POST: ResourceRequests/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ResourceRequest request)
        {
            if (id != request.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(request);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Resource request updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResourceRequestExists(request.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            ViewBag.Disasters = await _context.Disasters
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = $"{d.Name} - {d.Location}"
                })
                .ToListAsync();

            ViewBag.Resources = await _context.Resources
                .Include(r => r.Category)
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = $"{r.Name} ({r.Category.CategoryName})"
                })
                .ToListAsync();

            return View(request);
        }
        // POST: ResourceRequests/UpdateStatus/5
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var request = await _context.ResourceRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Request status updated to {status}";
            return RedirectToAction(nameof(Index));
        }

        // POST: ResourceRequests/Fulfill/5
        [HttpPost]
        public async Task<IActionResult> Fulfill(int id)
        {
            var request = await _context.ResourceRequests
                .Include(r => r.Resource)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            // Check if we have enough resources
            if (request.Resource.CurrentQuantity >= request.QuantityRequested)
            {
                request.Resource.CurrentQuantity -= request.QuantityRequested;
                request.Status = "Fulfilled";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Resource request fulfilled successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Insufficient resources to fulfill this request. Current stock is too low.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: ResourceRequests/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.ResourceRequests
                .Include(r => r.Disaster)
                .Include(r => r.Resource)
                .Include(r => r.RequestedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            return View(request);
        }

        // POST: ResourceRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var request = await _context.ResourceRequests.FindAsync(id);
            if (request != null)
            {
                _context.ResourceRequests.Remove(request);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Resource request deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ResourceRequestExists(int id)
        {
            return _context.ResourceRequests.Any(e => e.Id == id);
        }
    }
}
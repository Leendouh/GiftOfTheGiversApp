using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using GiftOfTheGiversApp.ViewModels;

namespace GiftOfTheGiversApp.Controllers
{
    [Authorize]
    public class ResourcesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResourcesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Resources
        public async Task<IActionResult> Index()
        {
            var resources = await _context.Resources
                .Include(r => r.Category)
                .OrderBy(r => r.Name)
                .ToListAsync();

            return View(resources);
        }

        // GET: Resources/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var resource = await _context.Resources
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resource == null) return NotFound();

            return View(resource);
        }

        // GET: Resources/Create
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Create()
        {
            var viewModel = new ResourceViewModel
            {
                Categories = await _context.ResourceCategories
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.CategoryName
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Resources/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Create(ResourceViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Convert ViewModel to Entity
                    var resource = new Resource
                    {
                        Name = viewModel.Name,
                        CategoryId = viewModel.CategoryId,
                        Description = viewModel.Description,
                        UnitOfMeasure = viewModel.UnitOfMeasure,
                        CurrentQuantity = viewModel.CurrentQuantity,
                        ThresholdQuantity = viewModel.ThresholdQuantity
                    };

                    _context.Add(resource);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Resource added successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving the resource: " + ex.Message);
                }
            }

            // Reload categories if validation fails
            viewModel.Categories = await _context.ResourceCategories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.CategoryName
                })
                .ToListAsync();

            return View(viewModel);
        }

        // GET: Resources/Edit/5
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var resource = await _context.Resources.FindAsync(id);
            if (resource == null) return NotFound();

            ViewBag.Categories = await _context.ResourceCategories.ToListAsync();
            return View(resource);
        }

        // POST: Resources/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Edit(int id, Resource resource)
        {
            if (id != resource.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(resource);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Resource updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResourceExists(resource.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.ResourceCategories.ToListAsync();
            return View(resource);
        }

        // GET: Resources/Delete/5
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var resource = await _context.Resources
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resource == null) return NotFound();

            return View(resource);
        }

        // POST: Resources/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource != null)
            {
                _context.Resources.Remove(resource);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Resource deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Resources/LowStock
        public async Task<IActionResult> LowStock()
        {
            // Use the condition directly instead of the computed property
            var lowStockResources = await _context.Resources
                .Include(r => r.Category)
                .Where(r => r.CurrentQuantity <= r.ThresholdQuantity) // Fixed this line
                .OrderBy(r => r.CurrentQuantity)
                .ToListAsync();

            return View(lowStockResources);
        }

        private bool ResourceExists(int id)
        {
            return _context.Resources.Any(e => e.Id == id);
        }
    }
}
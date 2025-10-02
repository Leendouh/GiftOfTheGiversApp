using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace GiftOfTheGiversApp.Controllers
{
    [Authorize(Roles = "Admin,Coordinator")]
    public class ResourceCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResourceCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ResourceCategories
        public async Task<IActionResult> Index()
        {
            return View(await _context.ResourceCategories.ToListAsync());
        }

        // GET: ResourceCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ResourceCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResourceCategory resourceCategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(resourceCategory);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Category created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(resourceCategory);
        }

        // GET: ResourceCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var resourceCategory = await _context.ResourceCategories.FindAsync(id);
            if (resourceCategory == null) return NotFound();

            return View(resourceCategory);
        }

        // POST: ResourceCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ResourceCategory resourceCategory)
        {
            if (id != resourceCategory.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(resourceCategory);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Category updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResourceCategoryExists(resourceCategory.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(resourceCategory);
        }

        private bool ResourceCategoryExists(int id)
        {
            return _context.ResourceCategories.Any(e => e.Id == id);
        }
    }
}
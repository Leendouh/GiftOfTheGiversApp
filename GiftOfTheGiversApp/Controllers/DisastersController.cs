using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
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

        public DisastersController(ApplicationDbContext context)
        {
            _context = context;
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
                    return RedirectToAction(nameof(Index));
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

            return View(disaster);
        }

        // POST: Disasters/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Location,Description,DisasterType,SeverityLevel,Status,EstimatedAffected")] Disaster disaster)
        {
            if (id != disaster.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(disaster);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Disaster updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DisasterExists(disaster.Id)) return NotFound();
                    else throw;
                }
            }
            return View(disaster);
        }

        // GET: Disasters/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var disaster = await _context.Disasters.FirstOrDefaultAsync(m => m.Id == id);
            if (disaster == null) return NotFound();

            return View(disaster);
        }

        // POST: Disasters/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var disaster = await _context.Disasters.FindAsync(id);
            if (disaster != null)
            {
                _context.Disasters.Remove(disaster);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Disaster deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DisasterExists(int id)
        {
            return _context.Disasters.Any(e => e.Id == id);
        }
    }
}
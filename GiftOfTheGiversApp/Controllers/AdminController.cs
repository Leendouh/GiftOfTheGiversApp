using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.ViewModels;

namespace GiftOfTheGiversApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<ApplicationUser> userManager,
                             RoleManager<IdentityRole> roleManager,
                             ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var userRolesViewModel = new List<RoleManagementViewModel>();
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            foreach (var user in users)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);

                userRolesViewModel.Add(new RoleManagementViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    FullName = user.FullName,
                    Email = user.Email,
                    // PhoneNumber = user.PhoneNumber, // Remove this line since it doesn't exist in your model
                    CurrentRoles = currentRoles.ToList(),
                    AvailableRoles = allRoles
                });
            }

            return View(userRolesViewModel);
        }

        // POST: Admin/UpdateRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoles(string userId, List<string> selectedRoles)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove roles that are no longer selected
            var rolesToRemove = currentRoles.Except(selectedRoles);
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            // Add new roles
            var rolesToAdd = selectedRoles.Except(currentRoles);
            await _userManager.AddToRolesAsync(user, rolesToAdd);

            TempData["SuccessMessage"] = $"Roles updated successfully for {user.FullName}";
            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // Prevent admin from deleting themselves
            var currentUser = await _userManager.GetUserAsync(User);
            if (user.Id == currentUser.Id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"User {user.FullName} has been deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Error deleting user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
            }

            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            // User Statistics
            var totalUsers = await _userManager.Users.CountAsync();
            var totalVolunteers = await _context.Volunteers.CountAsync();

            // Disaster Statistics
            var totalDisasters = await _context.Disasters.CountAsync();
            var activeDisasters = await _context.Disasters.CountAsync(d => d.Status == "Active");

            // Mission Statistics
            var totalMissions = await _context.Missions.CountAsync();
            var activeMissions = await _context.Missions.CountAsync(m => m.Status == "Open" || m.Status == "In Progress");
            var completedMissions = await _context.Missions.CountAsync(m => m.Status == "Completed");

            // Assignment Statistics
            var activeAssignments = await _context.Assignments.CountAsync(a => a.Status == "Assigned");

            // Donation Statistics
            var totalDonations = await _context.Donations.SumAsync(d => (int?)d.Quantity) ?? 0;

            // Recent Activity
            var recentActivity = new List<dynamic>
            {
                new { Title = "Admin Login", Time = "Just now", Description = "You logged into the admin panel" },
                new { Title = "System Check", Time = "Today", Description = "All systems running normally" },
                new { Title = "User Registration", Time = "2 hours ago", Description = "New volunteer registered in system" },
                new { Title = "Mission Update", Time = "4 hours ago", Description = "New mission created for flood relief" }
            };

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalVolunteers = totalVolunteers;
            ViewBag.TotalDisasters = totalDisasters;
            ViewBag.ActiveDisasters = activeDisasters;
            ViewBag.TotalMissions = totalMissions;
            ViewBag.ActiveMissions = activeMissions;
            ViewBag.CompletedMissions = completedMissions;
            ViewBag.ActiveAssignments = activeAssignments;
            ViewBag.TotalDonations = totalDonations;
            ViewBag.RecentActivity = recentActivity;

            return View();
        }
    }
}
using Microsoft.AspNetCore.Identity;
using GiftOfTheGiversApp.Models;
using System.Security.Claims;

namespace GiftOfTheGiversApp.Services
{
    public class PermissionService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public PermissionService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public class Permissions
        {
            // Disaster Permissions
            public bool CanViewDisasters { get; set; }
            public bool CanCreateDisasters { get; set; }
            public bool CanEditAllDisasters { get; set; }
            public bool CanEditOwnDisasters { get; set; }
            public bool CanResolveDisasters { get; set; }
            public bool CanDeleteDisasters { get; set; }

            // Volunteer Permissions
            public bool CanViewVolunteers { get; set; }
            public bool CanRegisterAsVolunteer { get; set; }
            public bool CanEditAllVolunteers { get; set; }
            public bool CanEditOwnVolunteer { get; set; }
            public bool CanContactVolunteers { get; set; }

            // Donation Permissions
            public bool CanViewDonations { get; set; }
            public bool CanCreateDonations { get; set; }
            public bool CanManageDonations { get; set; }

            // Mission Permissions
            public bool CanViewMissions { get; set; }
            public bool CanCreateMissions { get; set; }
            public bool CanAssignMissions { get; set; }
            public bool CanManageMissions { get; set; }

            // Admin Permissions
            public bool CanManageUsers { get; set; }
            public bool CanViewReports { get; set; }
            public bool CanManageSystem { get; set; }
        }

        public async Task<Permissions> GetUserPermissionsAsync(ClaimsPrincipal user, string resourceOwnerId = null)
        {
            var permissions = new Permissions();
            var currentUser = await _userManager.GetUserAsync(user);

            if (currentUser == null) return permissions;

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isAdmin = userRoles.Contains("Admin");
            var isCoordinator = userRoles.Contains("Coordinator");
            var isVolunteer = userRoles.Contains("Volunteer");
            var isDonor = userRoles.Contains("Donor");
            var isOwner = resourceOwnerId == currentUser.Id;

            // Disaster Permissions
            permissions.CanViewDisasters = true; // All authenticated users
            permissions.CanCreateDisasters = true; // All authenticated users
            permissions.CanEditOwnDisasters = isOwner;
            permissions.CanEditAllDisasters = isAdmin || isCoordinator;
            permissions.CanResolveDisasters = isAdmin || isCoordinator || isOwner;
            permissions.CanDeleteDisasters = isAdmin;

            // Volunteer Permissions
            permissions.CanViewVolunteers = true; // All authenticated users
            permissions.CanRegisterAsVolunteer = true; // All authenticated users
            permissions.CanEditOwnVolunteer = isOwner;
            permissions.CanEditAllVolunteers = isAdmin;
            permissions.CanContactVolunteers = isAdmin || isCoordinator;

            // Donation Permissions (we'll implement these next)
            permissions.CanViewDonations = true;
            permissions.CanCreateDonations = true;
            permissions.CanManageDonations = isAdmin || isCoordinator;

            // Mission Permissions (we'll implement these next)
            permissions.CanViewMissions = true;
            permissions.CanCreateMissions = isAdmin || isCoordinator;
            permissions.CanAssignMissions = isAdmin || isCoordinator;
            permissions.CanManageMissions = isAdmin || isCoordinator;

            // Admin Permissions
            permissions.CanManageUsers = isAdmin;
            permissions.CanViewReports = isAdmin || isCoordinator;
            permissions.CanManageSystem = isAdmin;

            return permissions;
        }
    }
}
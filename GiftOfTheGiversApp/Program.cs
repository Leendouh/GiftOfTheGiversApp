using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using GiftOfTheGiversApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity with ApplicationUser
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>() // ADD THIS LINE - CRITICAL!
.AddEntityFrameworkStores<ApplicationDbContext>();

// Add PermissionService
builder.Services.AddScoped<PermissionService>();

// Add other services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Create admin role and user - FIXED VERSION
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Create roles
        string[] roleNames = { "Admin", "Coordinator", "Volunteer", "Donor" };
        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Create admin user if doesn't exist
        var adminUser = await userManager.FindByEmailAsync("admin@giftofthegivers.org");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@giftofthegivers.org",
                Email = "admin@giftofthegivers.org",
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                CreatedDate = DateTime.Now
            };

            var createAdmin = await userManager.CreateAsync(adminUser, "Admin123!");
            if (createAdmin.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine("Admin user created successfully!");
            }
            else
            {
                Console.WriteLine("Failed to create admin user: " + string.Join(", ", createAdmin.Errors));
            }
        }
        else
        {
            // Ensure existing admin user has the Admin role
            var isInRole = await userManager.IsInRoleAsync(adminUser, "Admin");
            if (!isInRole)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine("Admin role added to existing user!");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// * Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

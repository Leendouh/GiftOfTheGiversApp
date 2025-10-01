namespace GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for all your models
    public DbSet<Disaster> Disasters { get; set; }
    public DbSet<Volunteer> Volunteers { get; set; }
    public DbSet<ResourceCategory> ResourceCategories { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<Donation> Donations { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<Mission> Missions { get; set; } // CHANGED FROM Tasks
    public DbSet<ResourceRequest> ResourceRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure relationships with explicit delete behavior
        builder.Entity<Volunteer>()
            .HasOne(v => v.User)
            .WithMany(u => u.VolunteerProfiles)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Assignment>()
            .HasOne(a => a.Volunteer)
            .WithMany(v => v.Assignments)
            .HasForeignKey(a => a.VolunteerId)
            .OnDelete(DeleteBehavior.Restrict); // CHANGED from Cascade to Restrict

        builder.Entity<Assignment>()
            .HasOne(a => a.Disaster)
            .WithMany(d => d.Assignments)
            .HasForeignKey(a => a.DisasterId)
            .OnDelete(DeleteBehavior.Restrict); // CHANGED from Cascade to Restrict

        builder.Entity<Assignment>()
            .HasOne(a => a.AssignedBy)
            .WithMany()
            .HasForeignKey(a => a.AssignedById)
            .OnDelete(DeleteBehavior.Restrict); // CHANGED from Cascade to Restrict

        builder.Entity<Donation>()
            .HasOne(d => d.Donor)
            .WithMany(u => u.Donations)
            .HasForeignKey(d => d.DonorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Disaster>()
            .HasOne(d => d.ReportedBy)
            .WithMany(u => u.ReportedDisasters)
            .HasForeignKey(d => d.ReportedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Mission>()
            .HasOne(m => m.Disaster)
            .WithMany(d => d.Missions)
            .HasForeignKey(m => m.DisasterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Mission>()
            .HasOne(m => m.AssignedTo)
            .WithMany(v => v.AssignedMissions)
            .HasForeignKey(m => m.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Mission>()
            .HasOne(m => m.CreatedBy)
            .WithMany()
            .HasForeignKey(m => m.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ResourceRequest>()
            .HasOne(rr => rr.Disaster)
            .WithMany(d => d.ResourceRequests)
            .HasForeignKey(rr => rr.DisasterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ResourceRequest>()
            .HasOne(rr => rr.Resource)
            .WithMany(r => r.ResourceRequests)
            .HasForeignKey(rr => rr.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ResourceRequest>()
            .HasOne(rr => rr.RequestedBy)
            .WithMany()
            .HasForeignKey(rr => rr.RequestedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
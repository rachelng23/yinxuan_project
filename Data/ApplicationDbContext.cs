using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using yinxuan_project.Models;

namespace yinxuan_project.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Booking> Bookings { get; set; }
        public DbSet<PricingRule>? PricingRules { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Specify precision and scale for the Price property in the Booking entity
            builder.Entity<Booking>()
                .Property(b => b.Price)
                .HasColumnType("decimal(18, 2)"); // Adjust these values as needed

            // Specify precision and scale for BasePrice and DynamicFactor in the PricingRule entity
            builder.Entity<PricingRule>()
                .Property(p => p.BasePrice)
                .HasColumnType("decimal(18, 2)"); // Up to 18 digits, 2 of which are after the decimal

            builder.Entity<PricingRule>()
                .Property(p => p.DynamicFactor)
                .HasColumnType("decimal(18, 2)"); // Up to 18 digits, 2 of which are after the decimal

            // Optionally seed some data into the PricingRules table for easier testing
            builder.Entity<PricingRule>().HasData(
                new PricingRule
                {
                    Id = 1,
                    FacilityDescription = "Tennis Court",
                    UserType = "Member",
                    StartTime = new TimeSpan(17, 0, 0),
                    EndTime = new TimeSpan(20, 0, 0),
                    BasePrice = 100,
                    DynamicFactor = 1.5m
                },
                new PricingRule
                {
                    Id = 2,
                    FacilityDescription = "Tennis Court",
                    UserType = "Guest",
                    StartTime = new TimeSpan(17, 0, 0),
                    EndTime = new TimeSpan(20, 0, 0),
                    BasePrice = 100,
                    DynamicFactor = 1.75m
                }
            );
        }
    }
}

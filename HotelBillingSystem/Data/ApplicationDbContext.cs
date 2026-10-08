using HotelBillingSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelBillingSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<RestaurantTable> RestaurantTables { get; set; } = null!;
        public DbSet<TableSession> TableSessions { get; set; } = null!;
        public DbSet<FoodCategory> FoodCategories { get; set; } = null!;
        public DbSet<FoodItem> FoodItems { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; } = null!;
        public DbSet<KitchenStaffStatus> KitchenStaffStatuses { get; set; } = null!;
        public DbSet<Bill> Bills { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RestaurantTable>()
                .HasIndex(t => t.TableNumber)
                .IsUnique();

            builder.Entity<RestaurantTable>()
                .HasIndex(t => t.QrToken)
                .IsUnique();

            builder.Entity<Order>()
                .HasOne(o => o.TableSession)
                .WithMany(s => s.Orders)
                .HasForeignKey(o => o.TableSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .HasOne(o => o.Table)
                .WithMany()
                .HasForeignKey(o => o.TableId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .HasOne(o => o.AssignedKitchenStaff)
                .WithMany()
                .HasForeignKey(o => o.AssignedKitchenStaffId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Bill>()
                .HasOne(b => b.TableSession)
                .WithMany(s => s.Bills)
                .HasForeignKey(b => b.TableSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Bill>()
                .HasOne(b => b.Table)
                .WithMany()
                .HasForeignKey(b => b.TableId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBillingSystem.Data;
using HotelBillingSystem.Models;
using HotelBillingSystem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelBillingSystem.Tests
{
    public class KitchenAssignmentServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task FindBestKitchenStaff_SelectsStaffWithLowestActiveWorkload()
        {
            // Arrange
            var db = GetInMemoryDbContext("Test_KitchenWorkload_" + Guid.NewGuid());

            var staffA = new ApplicationUser { Id = "staffA", UserName = "staffA@hotel.com", FullName = "Chef A", StaffRole = "KitchenStaff", IsActive = true };
            var staffB = new ApplicationUser { Id = "staffB", UserName = "staffB@hotel.com", FullName = "Chef B", StaffRole = "KitchenStaff", IsActive = true };
            var staffC = new ApplicationUser { Id = "staffC", UserName = "staffC@hotel.com", FullName = "Chef C", StaffRole = "KitchenStaff", IsActive = true };

            db.Users.AddRange(staffA, staffB, staffC);

            db.KitchenStaffStatuses.AddRange(
                new KitchenStaffStatus { UserId = "staffA", CurrentStatus = StaffStatus.Available },
                new KitchenStaffStatus { UserId = "staffB", CurrentStatus = StaffStatus.Available },
                new KitchenStaffStatus { UserId = "staffC", CurrentStatus = StaffStatus.Available }
            );

            // Assign 4 orders to staffA, 1 order to staffB, 2 orders to staffC
            for (int i = 0; i < 4; i++)
            {
                db.Orders.Add(new Order { OrderNumber = $"ORD-A{i}", AssignedKitchenStaffId = "staffA", Status = OrderStatus.Preparing, CreatedAt = DateTime.UtcNow });
            }
            db.Orders.Add(new Order { OrderNumber = "ORD-B0", AssignedKitchenStaffId = "staffB", Status = OrderStatus.Preparing, CreatedAt = DateTime.UtcNow });
            for (int i = 0; i < 2; i++)
            {
                db.Orders.Add(new Order { OrderNumber = $"ORD-C{i}", AssignedKitchenStaffId = "staffC", Status = OrderStatus.Preparing, CreatedAt = DateTime.UtcNow });
            }

            await db.SaveChangesAsync();

            var service = new KitchenAssignmentService(db);

            // Act
            var selectedStaff = await service.FindBestKitchenStaffAsync();

            // Assert
            Assert.NotNull(selectedStaff);
            Assert.Equal("staffB", selectedStaff!.Id); // Staff B has the lowest active workload (1 order)
        }

        [Fact]
        public async Task AssignOrderToBestKitchenStaff_UpdatesOrderWithSelectedStaffAndStatusAssigned()
        {
            // Arrange
            var db = GetInMemoryDbContext("Test_AssignOrder_" + Guid.NewGuid());

            var staff = new ApplicationUser { Id = "staff1", UserName = "chef1@hotel.com", FullName = "Chef One", StaffRole = "KitchenStaff", IsActive = true };
            db.Users.Add(staff);
            db.KitchenStaffStatuses.Add(new KitchenStaffStatus { UserId = "staff1", CurrentStatus = StaffStatus.Available });

            var order = new Order { Id = 1, OrderNumber = "ORD-001", Status = OrderStatus.New, CreatedAt = DateTime.UtcNow };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var service = new KitchenAssignmentService(db);

            // Act
            bool success = await service.AssignOrderToBestKitchenStaffAsync(1);

            // Assert
            Assert.True(success);
            var updatedOrder = await db.Orders.FindAsync(1);
            Assert.NotNull(updatedOrder);
            Assert.Equal("staff1", updatedOrder!.AssignedKitchenStaffId);
            Assert.Equal(OrderStatus.Assigned, updatedOrder.Status);
        }
    }
}

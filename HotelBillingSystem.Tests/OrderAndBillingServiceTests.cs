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
    public class OrderAndBillingServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CreateOrder_RejectsUnavailableFoodItems()
        {
            // Arrange
            var db = GetInMemoryDbContext("Test_UnavailableFood_" + Guid.NewGuid());

            db.RestaurantTables.Add(new RestaurantTable { Id = 1, TableNumber = "Table 01", QrToken = "token-1", IsActive = true });
            db.FoodItems.Add(new FoodItem { Id = 10, Name = "Mutton Kottu", Price = 1800.00m, IsAvailable = false, IsActive = true });
            await db.SaveChangesAsync();

            var tableService = new TableService(db);
            var kitchenService = new KitchenAssignmentService(db);
            var orderService = new OrderService(db, tableService, kitchenService);

            var items = new List<CartItemRequest>
            {
                new CartItemRequest { FoodItemId = 10, Quantity = 1 }
            };

            // Act
            var result = await orderService.CreateOrderAsync(1, "Guest Rahul", "0771234567", items, "Test note");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("currently out of stock/unavailable", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateOrder_CalculatesRealServerPrices_IgnoringClientManipulation()
        {
            // Arrange
            var db = GetInMemoryDbContext("Test_ServerPrices_" + Guid.NewGuid());

            db.RestaurantTables.Add(new RestaurantTable { Id = 1, TableNumber = "Table 01", QrToken = "token-1", IsActive = true });
            db.FoodItems.Add(new FoodItem { Id = 1, Name = "Chicken Kottu", Price = 1100.00m, IsAvailable = true, IsActive = true });
            await db.SaveChangesAsync();

            var tableService = new TableService(db);
            var kitchenService = new KitchenAssignmentService(db);
            var orderService = new OrderService(db, tableService, kitchenService);

            var items = new List<CartItemRequest>
            {
                new CartItemRequest { FoodItemId = 1, Quantity = 2 }
            };

            // Act
            var result = await orderService.CreateOrderAsync(1, "Guest Rahul", "0771234567", items, "Extra spicy");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Order);
            Assert.Equal(2200.00m, result.Order!.SubTotal); // 1100 * 2
            Assert.Equal(220.00m, result.Order.ServiceCharge); // 10%
            Assert.Equal(2420.00m, result.Order.TotalAmount); // 2200 + 220
        }

        [Fact]
        public async Task ProcessPayment_ClosesTableSessionAndResetsTableToAvailable()
        {
            // Arrange
            var db = GetInMemoryDbContext("Test_BillingClosure_" + Guid.NewGuid());

            var table = new RestaurantTable { Id = 1, TableNumber = "Table 01", Status = TableStatus.Occupied, QrToken = "token-1", IsActive = true };
            var session = new TableSession { Id = 100, SessionCode = "TS-100", TableId = 1, IsActive = true };
            db.RestaurantTables.Add(table);
            db.TableSessions.Add(session);

            var bill = new Bill
            {
                Id = 50,
                InvoiceNumber = "INV-001",
                TableSessionId = 100,
                TableId = 1,
                SubTotal = 2000.00m,
                ServiceCharge = 200.00m,
                GrandTotal = 2200.00m,
                PaymentStatus = PaymentStatus.Pending
            };
            db.Bills.Add(bill);
            await db.SaveChangesAsync();

            var billingService = new BillingService(db);

            // Act
            bool success = await billingService.ProcessPaymentAsync(50, PaymentMethod.Cash, "TXN-999", "cashier1");

            // Assert
            Assert.True(success);

            var updatedBill = await db.Bills.FindAsync(50);
            Assert.Equal(PaymentStatus.Paid, updatedBill!.PaymentStatus);

            var updatedSession = await db.TableSessions.FindAsync(100);
            Assert.False(updatedSession!.IsActive);
            Assert.NotNull(updatedSession.EndTime);

            var updatedTable = await db.RestaurantTables.FindAsync(1);
            Assert.Equal(TableStatus.Available, updatedTable!.Status);
        }
    }
}

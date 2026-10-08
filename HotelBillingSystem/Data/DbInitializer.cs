using HotelBillingSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelBillingSystem.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure DB created
            await context.Database.EnsureCreatedAsync();

            // 1. Roles
            string[] roles = new[] { "Admin", "KitchenStaff", "CashierStaff" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Admin User
            var adminEmail = "admin@hotel.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Hotel Administrator",
                    StaffRole = "Admin",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Kitchen Staff Users
            var kitchenStaff1 = await SeedStaffUser(userManager, "kitchen1@hotel.com", "Chef Sanath", "KitchenStaff", "Kitchen@123");
            var kitchenStaff2 = await SeedStaffUser(userManager, "kitchen2@hotel.com", "Chef Kamal", "KitchenStaff", "Kitchen@123");
            var kitchenStaff3 = await SeedStaffUser(userManager, "kitchen3@hotel.com", "Chef Nimal", "KitchenStaff", "Kitchen@123");

            // Ensure KitchenStaffStatuses
            var kitchenUsers = new[] { kitchenStaff1, kitchenStaff2, kitchenStaff3 };
            foreach (var chef in kitchenUsers)
            {
                if (chef != null && !await context.KitchenStaffStatuses.AnyAsync(k => k.UserId == chef.Id))
                {
                    context.KitchenStaffStatuses.Add(new KitchenStaffStatus
                    {
                        UserId = chef.Id,
                        CurrentStatus = StaffStatus.Available,
                        LastStatusUpdate = DateTime.UtcNow
                    });
                }
            }
            await context.SaveChangesAsync();

            // 4. Cashier User
            await SeedStaffUser(userManager, "cashier1@hotel.com", "Cashier Ruwan", "CashierStaff", "Cashier@123");
            await SeedStaffUser(userManager, "cashier2@hotel.com", "Cashier Dilani", "CashierStaff", "Cashier@123");

            // 5. Seed Restaurant Tables (1 to 10)
            if (!await context.RestaurantTables.AnyAsync())
            {
                var tables = new List<RestaurantTable>();
                for (int i = 1; i <= 10; i++)
                {
                    tables.Add(new RestaurantTable
                    {
                        TableNumber = $"Table {i:D2}",
                        Capacity = i <= 4 ? 4 : (i <= 8 ? 6 : 8),
                        Location = i <= 6 ? "Main Hall" : "Garden View",
                        Status = TableStatus.Available,
                        QrToken = $"table-token-{i:D2}",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                context.RestaurantTables.AddRange(tables);
                await context.SaveChangesAsync();
            }

            // 6. Seed Food Categories & Items
            if (!await context.FoodCategories.AnyAsync())
            {
                var categories = new List<FoodCategory>
                {
                    new FoodCategory { Name = "Rice", Description = "Delicious Fried Rice, Biryani, and Steamed Rice", DisplayOrder = 1, IsActive = true },
                    new FoodCategory { Name = "Kottu", Description = "Signature Sri Lankan Kottu Roti items", DisplayOrder = 2, IsActive = true },
                    new FoodCategory { Name = "Noodles", Description = "Wok-fried Noodles and Pasta", DisplayOrder = 3, IsActive = true },
                    new FoodCategory { Name = "Curry", Description = "Rich Sri Lankan & Indian Curries", DisplayOrder = 4, IsActive = true },
                    new FoodCategory { Name = "Beverages", Description = "Fresh Juices, Milkshakes & Soft Drinks", DisplayOrder = 5, IsActive = true },
                    new FoodCategory { Name = "Desserts", Description = "Sweet Delights & Ice Creams", DisplayOrder = 6, IsActive = true }
                };

                context.FoodCategories.AddRange(categories);
                await context.SaveChangesAsync();

                var riceCat = categories.First(c => c.Name == "Rice");
                var kottuCat = categories.First(c => c.Name == "Kottu");
                var noodleCat = categories.First(c => c.Name == "Noodles");
                var bevCat = categories.First(c => c.Name == "Beverages");
                var dessertCat = categories.First(c => c.Name == "Desserts");

                var foodItems = new List<FoodItem>
                {
                    // Rice
                    new FoodItem { CategoryId = riceCat.Id, Name = "Chicken Fried Rice", Description = "Basmati rice wok-fried with juicy chicken chunks and fresh veg.", Price = 1200.00m, PrepTimeMinutes = 15, IsAvailable = true, ImageUrl = "/images/chicken_rice.jpg" },
                    new FoodItem { CategoryId = riceCat.Id, Name = "Seafood Mixed Rice", Description = "Special fried rice with prawns, cuttlefish, and fried eggs.", Price = 1500.00m, PrepTimeMinutes = 20, IsAvailable = true, ImageUrl = "/images/seafood_rice.jpg" },
                    new FoodItem { CategoryId = riceCat.Id, Name = "Egg Fried Rice", Description = "Classic egg fried rice served with chili paste and gravy.", Price = 850.00m, PrepTimeMinutes = 12, IsAvailable = true, ImageUrl = "/images/egg_rice.jpg" },

                    // Kottu
                    new FoodItem { CategoryId = kottuCat.Id, Name = "Chicken Kottu", Description = "Shredded roti stir-fried with roast chicken, egg and spices.", Price = 1100.00m, PrepTimeMinutes = 15, IsAvailable = true, ImageUrl = "/images/chicken_kottu.jpg" },
                    new FoodItem { CategoryId = kottuCat.Id, Name = "Cheese Chicken Kottu", Description = "Rich creamy melted cheese over hot chicken kottu.", Price = 1450.00m, PrepTimeMinutes = 18, IsAvailable = true, ImageUrl = "/images/cheese_kottu.jpg" },
                    new FoodItem { CategoryId = kottuCat.Id, Name = "Mutton Kottu", Description = "Spicy roasted mutton kottu with thick gravy.", Price = 1800.00m, PrepTimeMinutes = 20, IsAvailable = false, ImageUrl = "/images/mutton_kottu.jpg" },

                    // Noodles
                    new FoodItem { CategoryId = noodleCat.Id, Name = "Chicken Fried Noodles", Description = "Egg noodles tossed with shredded chicken and chili paste.", Price = 1050.00m, PrepTimeMinutes = 15, IsAvailable = true, ImageUrl = "/images/chicken_noodles.jpg" },
                    new FoodItem { CategoryId = noodleCat.Id, Name = "Veg Garlic Noodles", Description = "Stir-fried yellow noodles with garlic butter and vegetables.", Price = 800.00m, PrepTimeMinutes = 12, IsAvailable = true, ImageUrl = "/images/veg_noodles.jpg" },

                    // Beverages
                    new FoodItem { CategoryId = bevCat.Id, Name = "Fresh Lime Juice", Description = "Chilled fresh lime juice with mint.", Price = 300.00m, PrepTimeMinutes = 5, IsAvailable = true, ImageUrl = "/images/lime_juice.jpg" },
                    new FoodItem { CategoryId = bevCat.Id, Name = "Chocolate Milkshake", Description = "Thick chocolate ice cream milkshake.", Price = 650.00m, PrepTimeMinutes = 8, IsAvailable = true, ImageUrl = "/images/choc_shake.jpg" },
                    new FoodItem { CategoryId = bevCat.Id, Name = "Iced Coffee", Description = "Traditional Sri Lankan chilled iced coffee.", Price = 400.00m, PrepTimeMinutes = 5, IsAvailable = true, ImageUrl = "/images/iced_coffee.jpg" },

                    // Desserts
                    new FoodItem { CategoryId = dessertCat.Id, Name = "Fruit Salad with Ice Cream", Description = "Tropical fresh fruit salad topped with vanilla ice cream.", Price = 500.00m, PrepTimeMinutes = 10, IsAvailable = true, ImageUrl = "/images/fruit_salad.jpg" },
                    new FoodItem { CategoryId = dessertCat.Id, Name = "Watalappan", Description = "Traditional coconut milk and jaggery custard with cashews.", Price = 450.00m, PrepTimeMinutes = 5, IsAvailable = true, ImageUrl = "/images/watalappan.jpg" }
                };

                context.FoodItems.AddRange(foodItems);
                await context.SaveChangesAsync();
            }
        }

        private static async Task<ApplicationUser?> SeedStaffUser(
            UserManager<ApplicationUser> userManager,
            string email, string fullName, string role, string password)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    StaffRole = role,
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
            return user;
        }
    }
}

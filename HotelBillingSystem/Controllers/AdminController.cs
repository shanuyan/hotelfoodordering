using HotelBillingSystem.Data;
using HotelBillingSystem.Hubs;
using HotelBillingSystem.Models;
using HotelBillingSystem.Services;
using HotelBillingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HotelBillingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IReportService _reportService;
        private readonly ITableService _tableService;
        private readonly IMenuService _menuService;
        private readonly IOrderService _orderService;
        private readonly IQrCodeService _qrCodeService;
        private readonly IAuditLogService _auditLogService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<HotelHub> _hubContext;

        public AdminController(
            IReportService reportService,
            ITableService tableService,
            IMenuService menuService,
            IOrderService orderService,
            IQrCodeService qrCodeService,
            IAuditLogService auditLogService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IHubContext<HotelHub> hubContext)
        {
            _reportService = reportService;
            _tableService = tableService;
            _menuService = menuService;
            _orderService = orderService;
            _qrCodeService = qrCodeService;
            _auditLogService = auditLogService;
            _userManager = userManager;
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var summary = await _reportService.GetAdminDashboardSummaryAsync();
            var recentOrders = await _orderService.GetAllActiveOrdersAsync();
            var tables = await _tableService.GetAllTablesAsync();
            var kitchenPerf = await _reportService.GetKitchenPerformanceAsync();

            var viewModel = new AdminDashboardViewModel
            {
                Summary = summary,
                RecentOrders = recentOrders,
                Tables = tables,
                KitchenPerformance = kitchenPerf
            };

            return View(viewModel);
        }

        #region User & Staff Management
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.OrderBy(u => u.StaffRole).ThenBy(u => u.FullName).ToListAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new UserCreateEditViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(UserCreateEditViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                StaffRole = model.StaffRole,
                IsActive = model.IsActive,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password ?? "Staff@123");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.StaffRole);

                if (model.StaffRole == "KitchenStaff")
                {
                    _context.KitchenStaffStatuses.Add(new KitchenStaffStatus
                    {
                        UserId = user.Id,
                        CurrentStatus = StaffStatus.Available,
                        LastStatusUpdate = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }

                await _auditLogService.LogAsync(User.Identity?.Name, User.Identity?.Name ?? "Admin", "CREATE_USER", "ApplicationUser", user.Id, $"Created user {model.Email} with role {model.StaffRole}");

                TempData["SuccessMessage"] = "User created successfully.";
                return RedirectToAction("Users");
            }

            foreach (var err in result.Errors)
            {
                ModelState.AddModelError("", err.Description);
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Json(new { success = false, message = "User not found." });

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            await _auditLogService.LogAsync(User.Identity?.Name, User.Identity?.Name ?? "Admin", "TOGGLE_USER_STATUS", "ApplicationUser", user.Id, $"Updated user {user.Email} IsActive to {user.IsActive}");

            return Json(new { success = true, isActive = user.IsActive });
        }
        #endregion

        #region Table Management & QR
        [HttpGet]
        public async Task<IActionResult> Tables()
        {
            var tables = await _tableService.GetAllTablesAsync();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            ViewBag.BaseUrl = baseUrl;
            return View(tables);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTable(string tableNumber, int capacity, string location)
        {
            if (string.IsNullOrEmpty(tableNumber))
            {
                TempData["ErrorMessage"] = "Table number required.";
                return RedirectToAction("Tables");
            }

            var table = await _tableService.CreateTableAsync(tableNumber, capacity, location);
            await _auditLogService.LogAsync(User.Identity?.Name, User.Identity?.Name ?? "Admin", "CREATE_TABLE", "RestaurantTable", table.Id.ToString(), $"Created Table {tableNumber}");

            TempData["SuccessMessage"] = "Table created successfully.";
            return RedirectToAction("Tables");
        }

        [HttpGet]
        public async Task<IActionResult> ViewQr(int tableId)
        {
            var table = await _tableService.GetTableByIdAsync(tableId);
            if (table == null) return NotFound();

            var qrUrl = $"{Request.Scheme}://{Request.Host}/order/table/{table.QrToken}";
            var qrBase64 = _qrCodeService.GenerateQrCodeBase64(qrUrl);

            ViewBag.QrBase64 = qrBase64;
            ViewBag.QrUrl = qrUrl;

            return View(table);
        }

        [HttpPost]
        public async Task<IActionResult> RegenerateQr(int tableId)
        {
            var token = await _tableService.RegenerateQrTokenAsync(tableId);
            await _auditLogService.LogAsync(User.Identity?.Name, User.Identity?.Name ?? "Admin", "REGENERATE_QR", "RestaurantTable", tableId.ToString(), "Regenerated table QR token");

            TempData["SuccessMessage"] = "QR Code token regenerated.";
            return RedirectToAction("Tables");
        }
        #endregion

        #region Menu Management
        [HttpGet]
        public async Task<IActionResult> Menu()
        {
            var items = await _menuService.GetAllFoodItemsAsync();
            var categories = await _menuService.GetAllCategoriesAsync();

            ViewBag.Categories = categories;
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> CreateFood()
        {
            ViewBag.Categories = await _menuService.GetAllCategoriesAsync();
            return View(new FoodItemCreateEditViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFood(FoodItemCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _menuService.GetAllCategoriesAsync();
                return View(model);
            }

            var foodItem = new FoodItem
            {
                CategoryId = model.CategoryId,
                Name = model.Name,
                Description = model.Description ?? "",
                Price = model.Price,
                ImageUrl = string.IsNullOrEmpty(model.ImageUrl) ? "/images/food-placeholder.png" : model.ImageUrl,
                PrepTimeMinutes = model.PrepTimeMinutes > 0 ? model.PrepTimeMinutes : 15,
                IsAvailable = model.IsAvailable,
                IsActive = model.IsActive
            };

            await _menuService.CreateFoodItemAsync(foodItem);
            await _auditLogService.LogAsync(User.Identity?.Name, User.Identity?.Name ?? "Admin", "CREATE_FOOD", "FoodItem", foodItem.Id.ToString(), $"Created food {foodItem.Name}");

            TempData["SuccessMessage"] = "Food item created successfully.";
            return RedirectToAction("Menu");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleDailyMenu(int foodId)
        {
            var food = await _menuService.GetFoodItemByIdAsync(foodId);
            if (food == null) return Json(new { success = false, message = "Item not found." });

            food.IsDailyMenu = !food.IsDailyMenu;
            await _menuService.UpdateFoodItemAsync(food);

            await _auditLogService.LogAsync(User.Identity?.Name, User.Identity?.Name ?? "Admin", "TOGGLE_DAILY_MENU", "FoodItem", foodId.ToString(), $"Toggled {food.Name} IsDailyMenu to {food.IsDailyMenu}");

            return Json(new { success = true, isDailyMenu = food.IsDailyMenu });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFoodAvailability(int foodId)
        {
            var food = await _menuService.GetFoodItemByIdAsync(foodId);
            if (food == null) return Json(new { success = false, message = "Item not found." });

            bool newAvailability = !food.IsAvailable;
            await _menuService.SetItemAvailabilityAsync(foodId, newAvailability);

            // SignalR broadcast to all connected clients (customers, kitchen, cashier)
            await _hubContext.Clients.All.SendAsync("FoodAvailabilityUpdated", foodId, food.Name, newAvailability);

            await _auditLogService.LogAsync(User.Identity?.Name, User.Identity?.Name ?? "Admin", "TOGGLE_FOOD_AVAILABILITY", "FoodItem", foodId.ToString(), $"Toggled {food.Name} availability to {newAvailability}");

            return Json(new { success = true, isAvailable = newAvailability });
        }
        #endregion

        #region Reports & Audit Logs
        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            var summary = await _reportService.GetAdminDashboardSummaryAsync();
            return View(summary);
        }

        [HttpGet]
        public async Task<IActionResult> AuditLogs()
        {
            var logs = await _auditLogService.GetLogsAsync(150);
            return View(logs);
        }
        #endregion
    }
}

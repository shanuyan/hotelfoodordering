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
    [Authorize(Roles = "KitchenStaff,Admin")]
    public class KitchenController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IKitchenAssignmentService _kitchenAssignmentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<HotelHub> _hubContext;

        public KitchenController(
            IOrderService orderService,
            IKitchenAssignmentService kitchenAssignmentService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IHubContext<HotelHub> hubContext)
        {
            _orderService = orderService;
            _kitchenAssignmentService = kitchenAssignmentService;
            _userManager = userManager;
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var assignedOrders = await _orderService.GetKitchenAssignedOrdersAsync(user.Id);
            var staffStatus = await _context.KitchenStaffStatuses
                .FirstOrDefaultAsync(k => k.UserId == user.Id);

            var workloads = await _kitchenAssignmentService.GetStaffWorkloadsAsync();

            var viewModel = new KitchenDashboardViewModel
            {
                KitchenUser = user,
                CurrentStaffStatus = staffStatus?.CurrentStatus ?? StaffStatus.Available,
                AssignedActiveOrders = assignedOrders,
                AllStaffWorkloads = workloads
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus newStatus, string remarks = "")
        {
            var user = await _userManager.GetUserAsync(User);
            string userId = user?.Id ?? string.Empty;

            var success = await _orderService.UpdateOrderStatusAsync(orderId, newStatus, userId, remarks);
            if (!success)
            {
                return Json(new { success = false, message = "Could not update order status." });
            }

            var order = await _orderService.GetOrderByIdAsync(orderId);

            // SignalR Notification
            await _hubContext.Clients.Group($"Order_{orderId}").SendAsync("OrderStatusChanged", orderId, newStatus.ToString(), remarks);
            await _hubContext.Clients.Group($"Table_{order?.TableId}").SendAsync("OrderStatusChanged", orderId, newStatus.ToString(), remarks);
            await _hubContext.Clients.Group("Kitchen").SendAsync("OrderUpdated", orderId, newStatus.ToString());
            await _hubContext.Clients.Group("Cashier").SendAsync("OrderUpdated", orderId, newStatus.ToString());
            await _hubContext.Clients.Group("Admin").SendAsync("OrderUpdated", orderId, newStatus.ToString());

            return Json(new { success = true, newStatus = newStatus.ToString() });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStaffStatus(StaffStatus status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Unauthorized" });

            var staffStatus = await _context.KitchenStaffStatuses
                .FirstOrDefaultAsync(k => k.UserId == user.Id);

            if (staffStatus == null)
            {
                staffStatus = new KitchenStaffStatus { UserId = user.Id, CurrentStatus = status, LastStatusUpdate = DateTime.UtcNow };
                _context.KitchenStaffStatuses.Add(staffStatus);
            }
            else
            {
                staffStatus.CurrentStatus = status;
                staffStatus.LastStatusUpdate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Broadcast to Admin
            await _hubContext.Clients.Group("Admin").SendAsync("KitchenStaffStatusUpdated", user.Id, user.FullName, status.ToString());

            return Json(new { success = true, currentStatus = status.ToString() });
        }
    }
}

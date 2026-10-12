using HotelBillingSystem.Data;
using HotelBillingSystem.Hubs;
using HotelBillingSystem.Models;
using HotelBillingSystem.Services;
using HotelBillingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HotelBillingSystem.Controllers
{
    public class PlaceOrderRequest
    {
        public int TableId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public List<CartItemRequest> Items { get; set; } = new List<CartItemRequest>();
        public string CustomerNotes { get; set; } = string.Empty;
    }

    public class SendOtpRequest
    {
        public string Phone { get; set; } = string.Empty;
    }

    public class VerifyOtpOrderRequest
    {
        public int TableId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public List<CartItemRequest> Items { get; set; } = new List<CartItemRequest>();
        public string CustomerNotes { get; set; } = string.Empty;
    }

    public class OrderController : Controller
    {
        private readonly ITableService _tableService;
        private readonly IMenuService _menuService;
        private readonly IOrderService _orderService;
        private readonly IBillingService _billingService;
        private readonly IHubContext<HotelHub> _hubContext;

        public OrderController(
            ITableService tableService,
            IMenuService menuService,
            IOrderService orderService,
            IBillingService billingService,
            IHubContext<HotelHub> hubContext)
        {
            _tableService = tableService;
            _menuService = menuService;
            _orderService = orderService;
            _billingService = billingService;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Customer(int? tableId)
        {
            int tId = tableId ?? HttpContext.Session.GetInt32("TableId") ?? 1;
            HttpContext.Session.SetInt32("TableId", tId);

            var table = await _tableService.GetTableByIdAsync(tId);
            if (table == null)
            {
                table = (await _tableService.GetAllTablesAsync()).FirstOrDefault();
            }

            if (table == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var allTables = await _tableService.GetAllTablesAsync();
            var session = await _tableService.GetOrCreateActiveSessionAsync(table.Id);
            var categories = await _menuService.GetActiveCategoriesWithItemsAsync(availableOnly: true);
            var sessionOrders = await _orderService.GetOrdersByTableSessionAsync(session.Id);

            var viewModel = new CustomerMenuViewModel
            {
                Table = table,
                Session = session,
                AllTables = allTables,
                Categories = categories,
                SessionOrders = sessionOrders
            };

            return View("Menu", viewModel);
        }

        [HttpPost]
        public IActionResult SendOtp([FromBody] SendOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Phone) || request.Phone.Trim().Length < 8)
            {
                return Json(new { success = false, message = "Please enter a valid mobile phone number." });
            }

            var random = new Random();
            string generatedOtp = random.Next(1000, 9999).ToString();

            HttpContext.Session.SetString("Otp_Phone", request.Phone.Trim());
            HttpContext.Session.SetString("Otp_Code", generatedOtp);
            HttpContext.Session.SetString("Otp_Expiry", DateTime.UtcNow.AddMinutes(5).ToString("o"));

            return Json(new
            {
                success = true,
                message = $"4-Digit SMS verification code generated.",
                simulatedOtp = generatedOtp
            });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyAndPlaceOrder([FromBody] VerifyOtpOrderRequest request)
        {
            if (request == null || !request.Items.Any())
            {
                return Json(new { success = false, message = "Cart items cannot be empty." });
            }

            string? savedOtp = HttpContext.Session.GetString("Otp_Code");
            string? savedPhone = HttpContext.Session.GetString("Otp_Phone");
            string? expiryStr = HttpContext.Session.GetString("Otp_Expiry");

            bool isOtpValid = false;
            if (!string.IsNullOrEmpty(savedOtp) && savedOtp == request.OtpCode?.Trim())
            {
                if (DateTime.TryParse(expiryStr, out var expiry) && expiry > DateTime.UtcNow)
                {
                    isOtpValid = true;
                }
            }

            if (!isOtpValid)
            {
                return Json(new { success = false, message = "Invalid or expired 4-digit SMS OTP code. Please request a new code." });
            }

            int tableId = request.TableId > 0 ? request.TableId : HttpContext.Session.GetInt32("TableId") ?? 1;

            var result = await _orderService.CreateOrderAsync(tableId, request.CustomerName, request.CustomerPhone, request.Items, request.CustomerNotes);

            if (!result.Success || result.Order == null)
            {
                return Json(new { success = false, message = result.ErrorMessage });
            }

            HttpContext.Session.SetString("IsPhoneVerified", "true");
            HttpContext.Session.Remove("Otp_Code");

            // Real-Time SignalR Broadcasts
            await _hubContext.Clients.Group("Kitchen").SendAsync("ReceiveNewKitchenOrder", new
            {
                orderId = result.Order.Id,
                orderNumber = result.Order.OrderNumber,
                tableNumber = result.Order.Table?.TableNumber,
                assignedStaffId = result.Order.AssignedKitchenStaffId,
                assignedStaffName = result.Order.AssignedKitchenStaff?.FullName ?? "Assigned Chef",
                itemsCount = result.Order.OrderItems.Count,
                items = result.Order.OrderItems.Select(i => new { i.FoodName, i.Quantity, i.SpecialInstructions }),
                createdAt = result.Order.CreatedAt.ToString("HH:mm:ss"),
                customerNotes = result.Order.CustomerNotes
            });

            await _hubContext.Clients.Group("Cashier").SendAsync("ReceiveCashierOrderNotification", new
            {
                orderId = result.Order.Id,
                tableNumber = result.Order.Table?.TableNumber,
                totalAmount = result.Order.TotalAmount
            });

            return Json(new
            {
                success = true,
                orderId = result.Order.Id,
                orderNumber = result.Order.OrderNumber,
                assignedStaff = result.Order.AssignedKitchenStaff?.FullName ?? "Kitchen Chef",
                redirectUrl = Url.Action("Track", "Order", new { id = result.Order.Id })
            });
        }

        [HttpGet]
        public async Task<IActionResult> Track(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return RedirectToAction("Customer");
            }

            var history = await _orderService.GetOrderStatusHistoryAsync(id);

            var viewModel = new OrderTrackingViewModel
            {
                Order = order,
                StatusHistory = history
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> RequestBill(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null) return Json(new { success = false, message = "Order not found" });

            await _hubContext.Clients.Group("Cashier").SendAsync("BillRequestedForTable", order.TableId, order.Table?.TableNumber);
            return Json(new { success = true, message = "Bill request sent to Cashier." });
        }
    }
}

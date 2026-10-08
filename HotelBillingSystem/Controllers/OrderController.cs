using HotelBillingSystem.Hubs;
using HotelBillingSystem.Models;
using HotelBillingSystem.Services;
using HotelBillingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HotelBillingSystem.Controllers
{
    public class PlaceOrderRequest
    {
        public int TableId { get; set; }
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

        // Endpoint triggered by QR Scan: /order/table/{token}
        [HttpGet("order/table/{token}")]
        public async Task<IActionResult> Table(string token)
        {
            var table = await _tableService.GetTableByTokenAsync(token);
            if (table == null)
            {
                return View("Error", new { Message = "Invalid or expired QR code for this table." });
            }

            var session = await _tableService.GetOrCreateActiveSessionAsync(table.Id);

            HttpContext.Session.SetInt32("TableId", table.Id);
            HttpContext.Session.SetInt32("SessionId", session.Id);

            return RedirectToAction("Menu", new { tableId = table.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Menu(int? tableId)
        {
            int tId = tableId ?? HttpContext.Session.GetInt32("TableId") ?? 1;

            var table = await _tableService.GetTableByIdAsync(tId);
            if (table == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var session = await _tableService.GetOrCreateActiveSessionAsync(table.Id);
            var categories = await _menuService.GetActiveCategoriesWithItemsAsync(availableOnly: true);
            var sessionOrders = await _orderService.GetOrdersByTableSessionAsync(session.Id);

            var viewModel = new CustomerMenuViewModel
            {
                Table = table,
                Session = session,
                Categories = categories,
                SessionOrders = sessionOrders
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            if (request == null || !request.Items.Any())
            {
                return Json(new { success = false, message = "Cart items cannot be empty." });
            }

            int tableId = request.TableId > 0 ? request.TableId : HttpContext.Session.GetInt32("TableId") ?? 1;

            var result = await _orderService.CreateOrderAsync(tableId, request.Items, request.CustomerNotes);

            if (!result.Success || result.Order == null)
            {
                return Json(new { success = false, message = result.ErrorMessage });
            }

            // Real-Time SignalR Broadcasts
            // 1. Notify Assigned Kitchen Staff & Kitchen Group
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

            // 2. Notify Cashier & Admin
            await _hubContext.Clients.Group("Cashier").SendAsync("NewOrderPlaced", result.Order.OrderNumber, result.Order.Table?.TableNumber);
            await _hubContext.Clients.Group("Admin").SendAsync("NewOrderPlaced", result.Order.OrderNumber, result.Order.Table?.TableNumber);

            return Json(new
            {
                success = true,
                orderId = result.Order.Id,
                orderNumber = result.Order.OrderNumber,
                assignedStaff = result.Order.AssignedKitchenStaff?.FullName ?? "Kitchen Staff",
                redirectUrl = Url.Action("Track", "Order", new { id = result.Order.Id })
            });
        }

        [HttpGet]
        public async Task<IActionResult> Track(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var viewModel = new OrderTrackingViewModel
            {
                Order = order,
                StatusHistory = order.StatusHistory.OrderBy(h => h.ChangedAt).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> RequestBill(int tableId)
        {
            int tId = tableId > 0 ? tableId : HttpContext.Session.GetInt32("TableId") ?? 1;

            var table = await _tableService.GetTableByIdAsync(tId);
            var session = await _tableService.GetActiveSessionAsync(tId);

            if (session == null)
            {
                return Json(new { success = false, message = "No active table session found." });
            }

            await _orderService.RequestBillAsync(tId);

            // SignalR notify Cashier & Admin
            await _hubContext.Clients.Group("Cashier").SendAsync("BillRequestedNotification", tId, table?.TableNumber ?? $"Table {tId}", session.Id);
            await _hubContext.Clients.Group("Admin").SendAsync("BillRequestedNotification", tId, table?.TableNumber ?? $"Table {tId}", session.Id);

            return Json(new { success = true, message = "Bill request sent to cashier! A staff member will attend to your table shortly." });
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(int billId)
        {
            var bill = await _billingService.GetBillByIdAsync(billId);
            if (bill == null) return NotFound();

            var viewModel = new BillInvoiceViewModel
            {
                Bill = bill,
                Session = bill.TableSession!,
                Table = bill.Table!,
                Orders = bill.TableSession?.Orders.ToList() ?? new List<Order>()
            };

            return View(viewModel);
        }
    }
}

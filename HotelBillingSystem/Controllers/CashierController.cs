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
    [Authorize(Roles = "CashierStaff,Admin")]
    public class CashierController : Controller
    {
        private readonly IBillingService _billingService;
        private readonly ITableService _tableService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<HotelHub> _hubContext;

        public CashierController(
            IBillingService billingService,
            ITableService tableService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IHubContext<HotelHub> hubContext)
        {
            _billingService = billingService;
            _tableService = tableService;
            _userManager = userManager;
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var activeSessions = await _billingService.GetActiveSessionsWithBillRequestsAsync();
            var tables = await _tableService.GetAllTablesAsync();
            var recentBills = await _context.Bills
                .Include(b => b.TableSession)
                .Include(b => b.Table)
                .OrderByDescending(b => b.CreatedAt)
                .Take(15)
                .ToListAsync();

            var viewModel = new CashierDashboardViewModel
            {
                ActiveSessions = activeSessions,
                Tables = tables,
                RecentBills = recentBills
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GenerateBill(int sessionId)
        {
            var session = await _context.TableSessions
                .Include(s => s.Table)
                .Include(s => s.Orders)
                    .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var bill = await _billingService.GenerateBillForSessionAsync(sessionId, 0, "", user?.Id ?? "");

            var viewModel = new BillInvoiceViewModel
            {
                Bill = bill,
                Session = session,
                Table = session.Table!,
                Orders = session.Orders.Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Rejected).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int billId, PaymentMethod method, decimal discountAmount, string discountReason, string reference)
        {
            var user = await _userManager.GetUserAsync(User);
            string userId = user?.Id ?? "";

            var bill = await _billingService.GetBillByIdAsync(billId);
            if (bill == null) return NotFound();

            // Re-generate bill with discount if specified
            if (discountAmount > 0)
            {
                bill = await _billingService.GenerateBillForSessionAsync(bill.TableSessionId, discountAmount, discountReason, userId);
            }

            var success = await _billingService.ProcessPaymentAsync(bill.Id, method, reference, userId);
            if (!success)
            {
                TempData["ErrorMessage"] = "Payment processing failed.";
                return RedirectToAction("GenerateBill", new { sessionId = bill.TableSessionId });
            }

            // SignalR notifications
            await _hubContext.Clients.Group($"Table_{bill.TableId}").SendAsync("PaymentCompleted", bill.Id);
            await _hubContext.Clients.Group("Cashier").SendAsync("TableSessionClosed", bill.TableId, bill.Table?.TableNumber);
            await _hubContext.Clients.Group("Admin").SendAsync("TableSessionClosed", bill.TableId, bill.Table?.TableNumber);

            return RedirectToAction("Receipt", new { id = bill.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(int id)
        {
            var bill = await _billingService.GetBillByIdAsync(id);
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

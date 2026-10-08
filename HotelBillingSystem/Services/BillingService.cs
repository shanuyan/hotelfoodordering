using HotelBillingSystem.Data;
using HotelBillingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBillingSystem.Services
{
    public class BillingService : IBillingService
    {
        private readonly ApplicationDbContext _context;

        public BillingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Bill?> GetBillBySessionIdAsync(int sessionId)
        {
            return await _context.Bills
                .Include(b => b.TableSession)
                    .ThenInclude(s => s!.Orders)
                        .ThenInclude(o => o.OrderItems)
                .Include(b => b.Table)
                .Include(b => b.CashierUser)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.TableSessionId == sessionId);
        }

        public async Task<Bill?> GetBillByIdAsync(int billId)
        {
            return await _context.Bills
                .Include(b => b.TableSession)
                    .ThenInclude(s => s!.Orders)
                        .ThenInclude(o => o.OrderItems)
                .Include(b => b.Table)
                .Include(b => b.CashierUser)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == billId);
        }

        public async Task<Bill> GenerateBillForSessionAsync(int sessionId, decimal discountAmount, string discountReason, string cashierUserId)
        {
            var session = await _context.TableSessions
                .Include(s => s.Orders)
                    .ThenInclude(o => o.OrderItems)
                .Include(s => s.Table)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                throw new InvalidOperationException("Table session not found.");
            }

            // Check if bill already generated for this session
            var existingBill = await _context.Bills
                .FirstOrDefaultAsync(b => b.TableSessionId == sessionId);

            decimal subTotal = session.Orders
                .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Rejected)
                .Sum(o => o.SubTotal);

            decimal serviceCharge = session.Orders
                .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Rejected)
                .Sum(o => o.ServiceCharge);

            decimal taxAmount = session.Orders
                .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Rejected)
                .Sum(o => o.TaxAmount);

            if (discountAmount > subTotal) discountAmount = subTotal;

            decimal grandTotal = subTotal + serviceCharge + taxAmount - discountAmount;

            if (existingBill != null)
            {
                existingBill.SubTotal = subTotal;
                existingBill.DiscountAmount = discountAmount;
                existingBill.DiscountReason = discountReason ?? string.Empty;
                existingBill.ServiceCharge = serviceCharge;
                existingBill.TaxAmount = taxAmount;
                existingBill.GrandTotal = grandTotal;
                existingBill.CashierUserId = cashierUserId;
                await _context.SaveChangesAsync();
                return existingBill;
            }

            string invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpper()}";

            var bill = new Bill
            {
                InvoiceNumber = invoiceNumber,
                TableSessionId = sessionId,
                TableId = session.TableId,
                SubTotal = subTotal,
                DiscountAmount = discountAmount,
                DiscountReason = discountReason ?? string.Empty,
                ServiceCharge = serviceCharge,
                TaxAmount = taxAmount,
                GrandTotal = grandTotal,
                PaymentStatus = PaymentStatus.Pending,
                CashierUserId = cashierUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();
            return bill;
        }

        public async Task<bool> ProcessPaymentAsync(int billId, PaymentMethod method, string reference, string cashierUserId)
        {
            var bill = await _context.Bills
                .Include(b => b.TableSession)
                .FirstOrDefaultAsync(b => b.Id == billId);

            if (bill == null) return false;

            bill.PaymentMethod = method;
            bill.PaymentStatus = PaymentStatus.Paid;

            var payment = new Payment
            {
                BillId = bill.Id,
                Amount = bill.GrandTotal,
                PaymentMethod = method,
                PaymentStatus = PaymentStatus.Paid,
                TransactionReference = reference ?? $"TXN-{Guid.NewGuid():N}"[..12].ToUpper(),
                PaidAt = DateTime.UtcNow,
                ProcessedByUserId = cashierUserId
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Automatically close the table session upon successful payment
            await CloseTableSessionAsync(bill.TableSessionId);

            return true;
        }

        public async Task<bool> CloseTableSessionAsync(int sessionId)
        {
            var session = await _context.TableSessions
                .Include(s => s.Orders)
                .Include(s => s.Table)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null) return false;

            session.IsActive = false;
            session.EndTime = DateTime.UtcNow;

            // Mark all active orders for this session as Completed
            foreach (var order in session.Orders)
            {
                if (order.Status != OrderStatus.Cancelled && order.Status != OrderStatus.Rejected)
                {
                    order.Status = OrderStatus.Completed;
                    order.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Free the table
            if (session.Table != null)
            {
                session.Table.Status = TableStatus.Available;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<TableSession>> GetActiveSessionsWithBillRequestsAsync()
        {
            return await _context.TableSessions
                .Include(s => s.Table)
                .Include(s => s.Orders)
                    .ThenInclude(o => o.OrderItems)
                .Include(s => s.Bills)
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.IsBillRequested)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }
    }
}

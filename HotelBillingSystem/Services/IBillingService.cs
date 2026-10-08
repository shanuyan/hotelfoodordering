using HotelBillingSystem.Models;

namespace HotelBillingSystem.Services
{
    public interface IBillingService
    {
        Task<Bill?> GetBillBySessionIdAsync(int sessionId);
        Task<Bill?> GetBillByIdAsync(int billId);
        Task<Bill> GenerateBillForSessionAsync(int sessionId, decimal discountAmount, string discountReason, string cashierUserId);
        Task<bool> ProcessPaymentAsync(int billId, PaymentMethod method, string reference, string cashierUserId);
        Task<bool> CloseTableSessionAsync(int sessionId);
        Task<List<TableSession>> GetActiveSessionsWithBillRequestsAsync();
    }
}

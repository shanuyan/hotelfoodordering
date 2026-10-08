using HotelBillingSystem.Data;
using HotelBillingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBillingSystem.Services
{
    public class TableService : ITableService
    {
        private readonly ApplicationDbContext _context;

        public TableService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RestaurantTable?> GetTableByTokenAsync(string token)
        {
            return await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.QrToken == token && t.IsActive);
        }

        public async Task<RestaurantTable?> GetTableByIdAsync(int id)
        {
            return await _context.RestaurantTables.FindAsync(id);
        }

        public async Task<List<RestaurantTable>> GetAllTablesAsync()
        {
            return await _context.RestaurantTables
                .OrderBy(t => t.TableNumber)
                .ToListAsync();
        }

        public async Task<TableSession?> GetActiveSessionAsync(int tableId)
        {
            return await _context.TableSessions
                .Include(s => s.Orders)
                    .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(s => s.TableId == tableId && s.IsActive);
        }

        public async Task<TableSession> GetOrCreateActiveSessionAsync(int tableId)
        {
            var session = await GetActiveSessionAsync(tableId);
            if (session != null) return session;

            var table = await _context.RestaurantTables.FindAsync(tableId);
            if (table != null)
            {
                table.Status = TableStatus.Occupied;
            }

            string sessionCode = $"TS-{DateTime.UtcNow:yyyyMMdd}-{tableId:D2}-{Guid.NewGuid().ToString("N")[..4]}";
            session = new TableSession
            {
                TableId = tableId,
                SessionCode = sessionCode,
                StartTime = DateTime.UtcNow,
                IsActive = true,
                TotalAmount = 0
            };

            _context.TableSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<bool> UpdateTableStatusAsync(int tableId, TableStatus status)
        {
            var table = await _context.RestaurantTables.FindAsync(tableId);
            if (table == null) return false;

            table.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<RestaurantTable> CreateTableAsync(string tableNumber, int capacity, string location)
        {
            var table = new RestaurantTable
            {
                TableNumber = tableNumber,
                Capacity = capacity,
                Location = location,
                Status = TableStatus.Available,
                QrToken = $"table-token-{Guid.NewGuid():N}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.RestaurantTables.Add(table);
            await _context.SaveChangesAsync();
            return table;
        }

        public async Task<string> RegenerateQrTokenAsync(int tableId)
        {
            var table = await _context.RestaurantTables.FindAsync(tableId);
            if (table == null) return string.Empty;

            table.QrToken = $"table-token-{Guid.NewGuid():N}";
            await _context.SaveChangesAsync();
            return table.QrToken;
        }
    }
}

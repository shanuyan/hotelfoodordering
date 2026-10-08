using HotelBillingSystem.Data;
using HotelBillingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBillingSystem.Services
{
    public class MenuService : IMenuService
    {
        private readonly ApplicationDbContext _context;

        public MenuService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<FoodCategory>> GetActiveCategoriesWithItemsAsync(bool availableOnly = true)
        {
            var query = _context.FoodCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Include(c => c.FoodItems.Where(i => i.IsActive && (!availableOnly || i.IsAvailable)))
                .AsQueryable();

            return await query.ToListAsync();
        }

        public async Task<List<FoodItem>> GetAllFoodItemsAsync()
        {
            return await _context.FoodItems
                .Include(i => i.Category)
                .OrderBy(i => i.CategoryId)
                .ThenBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<FoodItem?> GetFoodItemByIdAsync(int id)
        {
            return await _context.FoodItems
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<bool> SetItemAvailabilityAsync(int foodId, bool isAvailable)
        {
            var item = await _context.FoodItems.FindAsync(foodId);
            if (item == null) return false;

            item.IsAvailable = isAvailable;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<FoodItem> CreateFoodItemAsync(FoodItem item)
        {
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            _context.FoodItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> UpdateFoodItemAsync(FoodItem item)
        {
            var existing = await _context.FoodItems.FindAsync(item.Id);
            if (existing == null) return false;

            existing.Name = item.Name;
            existing.Description = item.Description;
            existing.Price = item.Price;
            existing.CategoryId = item.CategoryId;
            existing.ImageUrl = item.ImageUrl;
            existing.PrepTimeMinutes = item.PrepTimeMinutes;
            existing.IsAvailable = item.IsAvailable;
            existing.IsActive = item.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<FoodCategory>> GetAllCategoriesAsync()
        {
            return await _context.FoodCategories
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }

        public async Task<FoodCategory> CreateCategoryAsync(FoodCategory category)
        {
            _context.FoodCategories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }
    }
}

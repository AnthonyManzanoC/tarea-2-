using System.Collections.Generic;
using System.Threading.Tasks;
using CoronelExpress.Models;
using Microsoft.EntityFrameworkCore;

namespace CoronelExpress.Data
{
    public interface IPromotionRepository
    {
        Task AddPromotionAsync(Promotion promotion);
        Task<Promotion> GetPromotionByTokenAsync(string token);
        Task<IEnumerable<Promotion>> GetAllPromotionsAsync();
        Task<Promotion> GetPromotionByIdAsync(int id);
        Task UpdatePromotionAsync(Promotion promotion);
        Task DeletePromotionAsync(int id);
    }

    public class PromotionRepository : IPromotionRepository
    {
        private readonly ApplicationDbContext _context;

        public PromotionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddPromotionAsync(Promotion promotion)
        {
            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();
        }

        public async Task<Promotion> GetPromotionByTokenAsync(string token)
        {
            return await _context.Promotions
                .Include(p => p.Subscription)
                .FirstOrDefaultAsync(p => p.Token == token);
        }

        public async Task<IEnumerable<Promotion>> GetAllPromotionsAsync()
        {
            return await _context.Promotions
                .Include(p => p.Subscription)
                .ToListAsync();
        }

        public async Task<Promotion> GetPromotionByIdAsync(int id)
        {
            return await _context.Promotions
                .Include(p => p.Subscription)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task UpdatePromotionAsync(Promotion promotion)
        {
            _context.Promotions.Update(promotion);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePromotionAsync(int id)
        {
            var promotion = await GetPromotionByIdAsync(id);
            if (promotion != null)
            {
                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync();
            }
        }
    }
}

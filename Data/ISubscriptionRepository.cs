using CoronelExpress.Models;
using System.Threading.Tasks;


namespace CoronelExpress.Data

{
    public interface ISubscriptionRepository
    {
        Task AddSubscriptionAsync(Subscription subscription);
    }
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddSubscriptionAsync(Subscription subscription)
        {
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
        }
    }
}
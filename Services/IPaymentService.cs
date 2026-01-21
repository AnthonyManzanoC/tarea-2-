using CoronelExpress.Models;

namespace CoronelExpress.Services
{
    public interface IPaymentService
    {
        Task RegisterPaymentAsync(Order order);
    }

}

using System.Threading.Tasks;

namespace ClothesShop.Web.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationEmailAsync(string email, string fullName, string orderId, decimal totalPrice);
    }
}

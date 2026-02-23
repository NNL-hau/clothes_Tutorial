using System;
using System.Threading.Tasks;

namespace ClothesShop.Web.Services
{
    public class MockEmailService : IEmailService
    {
        public Task SendOrderConfirmationEmailAsync(string email, string fullName, string orderId, decimal totalPrice)
        {
            // Simulate sending email by logging to console/debug
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"[EMAIL SIMULATOR] Gửi email đến: {email}");
            Console.WriteLine($"Chào {fullName}, đơn hàng #{orderId.Substring(0, 8)} đã được xác nhận.");
            Console.WriteLine($"Tổng tiền: {totalPrice:N0} VND");
            Console.WriteLine("Cảm ơn bạn đã mua sắm tại ClothesShop!");
            Console.WriteLine("--------------------------------------------------");
            
            return Task.CompletedTask;
        }
    }
}

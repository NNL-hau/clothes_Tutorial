namespace ClothesShop.Web.Models
{
    /// <summary>
    /// Request tạo giao dịch thanh toán từ phía Blazor WebAssembly.
    /// Khớp với CreateTransactionDto ở Payment.API.
    /// </summary>
    public record CreateTransactionRequest(
        Guid OrderId,
        string UserName,
        decimal Amount,
        string PaymentMethod,
        string FullName = "",
        string Email = "",
        string PhoneNumber = ""
    );
}




using System.Text.Json.Serialization;

namespace Payment.API.DTOs
{
    public record TransactionDto(Guid Id, Guid OrderId, string UserName, decimal Amount, string PaymentMethod, string Status, DateTime CreatedAt, [property: JsonPropertyName("paymentUrl")] string? PaymentUrl = null, [property: JsonPropertyName("qrCodeUrl")] string? QrCodeUrl = null);
    public record CreateTransactionDto(Guid OrderId, string UserName, decimal Amount, string PaymentMethod, string FullName = "", string Email = "", string PhoneNumber = "");
    public record UpdateStatusRequest(string Status);
}

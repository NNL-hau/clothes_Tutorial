using System.Text.Json.Serialization;

namespace ClothesShop.Web.Models
{
    public record TransactionDto(
        Guid Id,
        Guid OrderId,
        string UserName,
        decimal Amount,
        string PaymentMethod,
        string Status,
        DateTime CreatedAt,
        [property: JsonPropertyName("paymentUrl")] string? PaymentUrl = null,
        [property: JsonPropertyName("qrCodeUrl")]  string? QrCodeUrl  = null
    );
}

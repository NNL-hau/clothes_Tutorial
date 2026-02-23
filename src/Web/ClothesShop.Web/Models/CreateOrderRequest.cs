namespace ClothesShop.Web.Models
{
    public record CreateOrderRequest(
        string UserName, 
        decimal TotalPrice,
        string? FullName,
        string? PhoneNumber,
        string? EmailAddress,
        string? Province,
        string? District,
        string? Ward,
        string? AddressDetail,
        string? AddressLine,
        string? Country,
        string? State,
        string? ZipCode,
        string? CardName,
        string? CardNumber,
        string? Expiration,
        string? CVV,
        string? PaymentMethodName,
        int PaymentMethod,
        List<CreateOrderItemRequest> OrderItems);

    public record CreateOrderItemRequest(Guid ProductId, string ProductName, decimal Price, int Quantity);
}

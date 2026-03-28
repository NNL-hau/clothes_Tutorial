namespace ClothesShop.Web.Models
{
    public record OrderDto(
        Guid Id,
        string UserName,
        decimal TotalPrice,
        string OrderStatus,
        DateTime CreatedAt,
        string? FullName,
        string? PhoneNumber,
        string? Province,
        string? District,
        string? Ward,
        string? AddressDetail,
        string? PaymentMethodName,
        string? CouponCode,
        decimal CouponAmount,
        List<OrderItemDto> OrderItems
    );

    public record OrderItemDto(
        Guid Id,
        Guid ProductId,
        string ProductName,
        decimal Price,
        int Quantity
    );

    public record UpdateOrderStatusDto(
        string Status
    );
}

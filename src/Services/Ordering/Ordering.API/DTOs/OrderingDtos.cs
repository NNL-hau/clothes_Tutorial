namespace Ordering.API.DTOs
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
        List<OrderItemDto> OrderItems);
    public record OrderItemDto(Guid Id, Guid ProductId, string ProductName, decimal Price, int Quantity);
    public record UpdateOrderStatusDto(string Status);

    public record CreateOrderDto(
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
        string? CouponCode,
        decimal CouponAmount,
        List<CreateOrderItemDto> OrderItems);

    public record CreateOrderItemDto(Guid ProductId, string ProductName, decimal Price, int Quantity);
}

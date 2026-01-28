namespace ClothesShop.Web.Models
{
    public record OrderDto(
        Guid Id,
        string UserName,
        decimal TotalPrice,
        string OrderStatus,
        DateTime CreatedAt,
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

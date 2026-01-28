namespace ClothesShop.Web.Models
{
    public record ProductDto(
        Guid Id,
        string Name,
        string? Description,
        decimal Price,
        string? ImageUrl,
        Guid CategoryId,
        int StockQuantity,
        DateTime CreatedAt
    );

    public record CreateProductDto(
        string Name,
        string? Description,
        decimal Price,
        string? ImageUrl,
        Guid CategoryId,
        int StockQuantity
    );
}

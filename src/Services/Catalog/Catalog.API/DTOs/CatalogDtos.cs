namespace Catalog.API.DTOs
{
    public record CreateProductDto(string Name, string? Description, decimal Price, string? ImageUrl, Guid CategoryId, int StockQuantity);
    public record ProductDto(Guid Id, string Name, string? Description, decimal Price, string? ImageUrl, Guid CategoryId, int StockQuantity, DateTime CreatedAt);
    
    public record CreateCategoryDto(string Name, string? Description);
    public record CategoryDto(Guid Id, string Name, string? Description);
    
    public record CreateBannerDto(string Title, string? SubTitle, string ImageUrl, string? LinkUrl, bool IsActive, int DisplayOrder);
    public record BannerDto(Guid Id, string Title, string? SubTitle, string ImageUrl, string? LinkUrl, bool IsActive, int DisplayOrder);
}

namespace Catalog.API.DTOs
{
    public record CreateProductDto(string Name, string? Description, decimal Price, string? ImageUrl, Guid CategoryId, int StockQuantity, string? Colors, string? Sizes);
    public record ProductDto(Guid Id, string Name, string? Description, decimal Price, string? ImageUrl, Guid CategoryId, int StockQuantity, int SoldQuantity, string? Colors, string? Sizes, DateTime CreatedAt);
    
    public record CreateCategoryDto(string Name, string? Description);
    public record CategoryDto(Guid Id, string Name, string? Description);
    
    public record CreateBannerDto(string? Title, string? SubTitle, string ImageUrl, string? LinkUrl, bool IsActive, int DisplayOrder);
    public record BannerDto(Guid Id, string? Title, string? SubTitle, string ImageUrl, string? LinkUrl, bool IsActive, int DisplayOrder);

    public record ChatRequest(
        string Message, 
        string? Username = null,
        string? BasketContext = null,
        string? OrderContext = null,
        string? UserProfile = null);
    public record ChatResponse(string Response);
    public record SuggestionDto(string Text, string Icon);
}

namespace ClothesShop.Web.Models
{
    public record CategoryDto(
        Guid Id,
        string Name,
        string? Description
    );
    
    public record CreateCategoryDto(
        string Name,
        string? Description
    );
}

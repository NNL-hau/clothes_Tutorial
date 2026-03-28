namespace ClothesShop.Web.Models
{
    public record BannerDto(
        Guid Id,
        string? Title,
        string? SubTitle,
        string ImageUrl,
        string? LinkUrl,
        bool IsActive,
        int DisplayOrder
    );

    public record CreateBannerDto(
        string? Title,
        string? SubTitle,
        string ImageUrl,
        string? LinkUrl,
        bool IsActive,
        int DisplayOrder
    );
}

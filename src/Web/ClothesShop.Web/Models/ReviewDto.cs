namespace ClothesShop.Web.Models
{
    public record ReviewDto(
        Guid Id,
        Guid ProductId,
        string UserName,
        int Rating,
        string Comment,
        DateTime CreatedAt,
        bool IsApproved
    );
}

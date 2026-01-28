namespace ClothesShop.Web.Models
{
    public record UserDto(
        Guid Id,
        string Email,
        string FullName,
        string Role,
        string? PhoneNumber,
        string? Address,
        DateTime CreatedAt
    );
}

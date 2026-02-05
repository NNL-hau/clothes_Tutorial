namespace Review.API.DTOs
{
    public record ReviewDto(Guid Id, Guid ProductId, string UserName, int Rating, string? Comment, DateTime CreatedAt, bool IsApproved);
    public record CreateReviewDto(Guid ProductId, string UserName, int Rating, string? Comment);
}

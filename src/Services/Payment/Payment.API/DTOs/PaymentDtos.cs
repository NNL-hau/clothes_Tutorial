namespace Payment.API.DTOs
{
    public record TransactionDto(Guid Id, Guid OrderId, string UserName, decimal Amount, string PaymentMethod, string Status, DateTime CreatedAt);
    public record CreateTransactionDto(Guid OrderId, string UserName, decimal Amount, string PaymentMethod);
}

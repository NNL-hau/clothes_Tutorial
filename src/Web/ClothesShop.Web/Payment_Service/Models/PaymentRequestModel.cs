using Payment_Service.Enums;

namespace Payment_Service.Models
{
    public class PaymentRequestModel
    {
        public string OrderCode { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string Description { get; set; }
        public string ReturnUrl { get; set; }
        public string NotifyUrl { get; set; }
        public Dictionary<string, string> ExtraData { get; set; }
    }

    public class PaymentResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string PaymentUrl { get; set; }
        public string TransactionId { get; set; }
        public PaymentStatus Status { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }
}

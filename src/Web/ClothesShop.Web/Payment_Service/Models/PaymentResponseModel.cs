using Payment_Service.Enums;

namespace Payment_Service.Models
{
    /// <summary>
    /// Model cho response thanh toán
    /// </summary>
    public class PaymentResponseModel
    {
        /// <summary>
        /// Mã giao dịch
        /// </summary>
        public string TransactionId { get; set; }

        /// <summary>
        /// Mã đơn hàng
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Số tiền thanh toán
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Phương thức thanh toán
        /// </summary>
        public PaymentMethod PaymentMethod { get; set; }

        /// <summary>
        /// Trạng thái thanh toán
        /// </summary>
        public PaymentStatus Status { get; set; }

        /// <summary>
        /// Thành công hay không
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Thông báo
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// URL thanh toán (cho MoMo, VNPay)
        /// </summary>
        public string PaymentUrl { get; set; }

        /// <summary>
        /// QR Code URL (cho MoMo)
        /// </summary>
        public string QrCodeUrl { get; set; }

        /// <summary>
        /// Mã lỗi
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Thời gian tạo giao dịch
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Thời gian hoàn thành giao dịch
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Dữ liệu bổ sung
        /// </summary>
        public Dictionary<string, string> ExtraData { get; set; }
    }
}

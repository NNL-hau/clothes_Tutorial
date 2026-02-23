namespace Payment_Service.Enums
{
    public enum PaymentMethod
    {
        COD = 1,
        MoMo = 2,
        VNPay = 3
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Success = 1,
        Failed = 2,
        Cancelled = 3,
        Refunded = 4
    }

    public enum OrderStatus
    {
        Pending = 0,
        Processing = 1,
        Shipping = 2,
        Delivered = 3,
        Cancelled = 4,
        Refunded = 5
    }
}
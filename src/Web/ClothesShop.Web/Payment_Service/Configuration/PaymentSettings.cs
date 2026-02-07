namespace Payment_Service.Configuration
{
    public class PaymentSettings
    {
        public MoMoSettings MoMo { get; set; }
        public VNPaySettings VNPay { get; set; }
    }

    public class MoMoSettings
    {
        public string PartnerCode { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string PaymentUrl { get; set; }
        public string ReturnUrl { get; set; }
        public string NotifyUrl { get; set; }
    }

    public class VNPaySettings
    {
        public string TmnCode { get; set; }
        public string HashSecret { get; set; }
        public string PaymentUrl { get; set; }
        public string ReturnUrl { get; set; }
        public string Version { get; set; }
        public string Command { get; set; }
        public string CurrCode { get; set; }
        public string Locale { get; set; }
    }
}

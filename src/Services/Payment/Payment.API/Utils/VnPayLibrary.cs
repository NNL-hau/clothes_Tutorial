using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Payment.API.Utils
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
                _requestData.Add(key, value);
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
                _responseData.Add(key, value);
        }

        public string GetResponseData(string key) =>
            _responseData.TryGetValue(key, out var v) ? v : "";

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var data = new StringBuilder();
            foreach (var kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(StandardUrlEncode(kv.Key) + "=" + StandardUrlEncode(kv.Value) + "&");
                }
            }

            string queryString = data.ToString().TrimEnd('&');
            string secureHash = HmacSHA512(hashSecret, queryString);

            Console.WriteLine("=== [PAYMENT_VERIFY] VNPay Request Debug ===");
            Console.WriteLine($"Raw Data: {queryString}");
            Console.WriteLine($"Secret: {hashSecret}");
            Console.WriteLine($"Hash: {secureHash}");
            Console.WriteLine("===========================");

            return $"{baseUrl}?{queryString}&vnp_SecureHash={secureHash}";
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var data = new StringBuilder();
            foreach (var kv in _responseData)
            {
                if (kv.Key == "vnp_SecureHash" || kv.Key == "vnp_SecureHashType")
                    continue;

                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(StandardUrlEncode(kv.Key) + "=" + StandardUrlEncode(kv.Value) + "&");
                }
            }

            string rawData = data.ToString().TrimEnd('&');
            string myHash = HmacSHA512(secretKey, rawData);

            Console.WriteLine("=== [PAYMENT_VERIFY] VNPay Callback Debug ===");
            Console.WriteLine($"Raw Data: {rawData}");
            Console.WriteLine($"Secret: {secretKey}");
            Console.WriteLine($"Input Hash: {inputHash}");
            Console.WriteLine($"My Hash: {myHash}");
            Console.WriteLine("============================");

            return myHash.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string StandardUrlEncode(string str)
        {
            return WebUtility.UrlEncode(str);
        }

        public static string HmacSHA512(string key, string input)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            using var hmac = new HMACSHA512(keyBytes);
            byte[] hashBytes = hmac.ComputeHash(inputBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    public class VnPayCompare : IComparer<string?>
    {
        public int Compare(string? x, string? y) => string.CompareOrdinal(x, y);
    }
}

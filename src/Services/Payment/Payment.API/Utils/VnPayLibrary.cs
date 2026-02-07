using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Payment.API.Utils
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
        {
            StringBuilder data = new StringBuilder();
            
            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    // VNPay 2.1.0 requires hashing the ENCODED query string
                    // Symbols must be uppercase hex (e.g. %20, %3A)
                    string key = UrlEncodeUppercase(kv.Key);
                    string value = UrlEncodeUppercase(kv.Value);
                    data.Append(key + "=" + value + "&");
                }
            }

            string queryString = data.ToString();
            if (queryString.EndsWith("&"))
            {
                queryString = queryString.Remove(queryString.Length - 1, 1);
            }

            // Calculate hash based on the encoded query string
            string vnpSecureHash = HmacSha512(vnpHashSecret, queryString);
            string paymentUrl = baseUrl + "?" + queryString + "&vnp_SecureHash=" + vnpSecureHash;

            return paymentUrl;
        }

        private string UrlEncodeUppercase(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            
            // WebUtility.UrlEncode uses lowercase hex and '+' for spaces
            string encoded = WebUtility.UrlEncode(value).Replace("+", "%20");
            
            // Force hex escapes to uppercase using Regex or manual replacement
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(@"%[a-f0-9]{2}");
            return reg.Replace(encoded, m => m.Value.ToUpperInvariant());
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            string rspRaw = GetResponseRaw();
            string myChecksum = HmacSha512(secretKey, rspRaw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResponseRaw()
        {
            StringBuilder data = new StringBuilder();
            
            // Collect all response data EXCEPT vnp_SecureHash and vnp_SecureHashType
            foreach (KeyValuePair<string, string> kv in _responseData)
            {
                if (!string.IsNullOrEmpty(kv.Value) && kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                {
                    // Response hashing also typically follows the same pattern
                    data.Append(UrlEncodeUppercase(kv.Key) + "=" + UrlEncodeUppercase(kv.Value) + "&");
                }
            }

            // Remove last '&'
            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }

            return data.ToString();
        }

        public static string HmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString().ToUpper();
        }
    }

    public class VnPayCompare : IComparer<string?>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}

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
            StringBuilder hashData = new StringBuilder();
            StringBuilder queryString = new StringBuilder();

            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    // Hash Data: key=value (NO encoding)
                    hashData.Append(kv.Key + "=" + kv.Value + "&");

                    // Query String: key=value (WITH URL encoding)
                    queryString.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            // Remove last '&'
            string rawHash = hashData.ToString().TrimEnd('&');
            string query = queryString.ToString().TrimEnd('&');

            // Calculate HMAC SHA512
            string vnpSecureHash = HmacSHA512(vnpHashSecret, rawHash);

            // Final URL
            return $"{baseUrl}?{query}&vnp_SecureHash={vnpSecureHash}";
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            string rspRaw = GetResponseRaw();
            string myChecksum = HmacSHA512(secretKey, rspRaw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResponseRaw()
        {
            StringBuilder data = new StringBuilder();

            // Remove vnp_SecureHash and vnp_SecureHashType before hashing
            if (_responseData.ContainsKey("vnp_SecureHashType"))
            {
                _responseData.Remove("vnp_SecureHashType");
            }
            if (_responseData.ContainsKey("vnp_SecureHash"))
            {
                _responseData.Remove("vnp_SecureHash");
            }

            foreach (KeyValuePair<string, string> kv in _responseData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    // ⚠️ KHÔNG URL ENCODE khi tính hash
                    data.Append(kv.Key + "=" + kv.Value + "&");
                }
            }

            // Remove trailing '&'
            return data.ToString().TrimEnd('&');
        }

        public static string HmacSHA512(string key, string inputData)
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

            return hash.ToString();
        }
    }

    public class VnPayCompare : IComparer<string>
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

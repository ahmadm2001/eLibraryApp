using System.Security.Cryptography;
using System.Text;

namespace eLibraryApp.Models
{
    public class SignedUrlHelper
    {
        private const string SecretKey = "your-secure-key"; // Replace with a strong key

        public static string GenerateSignedUrl(string filePath, DateTime expiryDate)
        {
            var expiryTimestamp = ((DateTimeOffset)expiryDate).ToUnixTimeSeconds();
            var dataToSign = $"{filePath}:{expiryTimestamp}";
            var signature = GenerateHmacSha256(dataToSign, SecretKey);

            return $"{filePath}?expiry={expiryTimestamp}&signature={signature}";
        }

        private static string GenerateHmacSha256(string data, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

        public static bool ValidateSignedUrl(string filePath, long expiry, string signature)
        {
            if (DateTimeOffset.FromUnixTimeSeconds(expiry) < DateTimeOffset.Now)
            {
                return false; // URL has expired
            }

            var dataToSign = $"{filePath}:{expiry}";
            var expectedSignature = GenerateHmacSha256(dataToSign, SecretKey);

            return signature == expectedSignature;
        }
    }
}

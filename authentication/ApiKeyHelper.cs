using System.Security.Cryptography;
using System.Text;

namespace gauntlet_framework_api.authentication {
    public class ApiKeyHelper {
        public static string GenerateApiKey(string prefix = "run_") {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            var secret = Convert.ToBase64String(bytes);
            return $"{prefix}{secret}";
        }

        public static string HashApiKey(string apiKey) {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}

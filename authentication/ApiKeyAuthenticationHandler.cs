using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using gauntlet_framework_api.models;
using gauntlet_framework_api.database;


namespace gauntlet_framework_api.authentication {

    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions {
        public const string DefaultScheme = "ApiKey";
        public string HeaderName { get; set; } = "X-Api-Key";
    }

    public class ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AppDbContext dbContext
    ) : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder) {

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
            if (!Request.Headers.TryGetValue(Options.HeaderName, out var extractedKeyValues) || string.IsNullOrWhiteSpace(extractedKeyValues.FirstOrDefault())) {
                // CRITICAL FIX: Return NoResult so [AllowAnonymous] endpoints are allowed to run
                return AuthenticateResult.NoResult();
            }

            var providedKey = extractedKeyValues.FirstOrDefault()!;
            var providedKeyHash = ApiKeyHelper.HashApiKey(providedKey);

            // Optional Performance Optimization:
            // Hash search directly in database instead of loading all active keys into memory
            var matchingKey = await dbContext.ApiKeys
                .FirstOrDefaultAsync(k => k.IsActive && k.KeyHash == providedKeyHash);

            if (matchingKey is null) {
                return AuthenticateResult.Fail("Invalid or revoked API Key.");
            }

            var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, matchingKey.Id.ToString()),
            new Claim("ApiKeyId", matchingKey.Id.ToString())
        };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}

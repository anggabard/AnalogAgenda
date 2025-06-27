using System.Security.Claims;

namespace AnalogAgenda.Server.Identity
{
    public enum AllowedClaims
    {
        Name,
        Email
    }

    public static class ClaimsPrincipalExtensions
    {
        public static string GetClaim(this ClaimsPrincipal principal, AllowedClaims claim)
        {
            CheckClaimsPrincipal(principal);
            return principal.FindFirstValue(claim.SystemClaim()) ?? throw new Exception($"Claim '{claim}' is not set!");
        }

        public static bool IsAuthenticated(this ClaimsPrincipal principal) => principal?.Identity?.IsAuthenticated ?? false;

        public static string Name(this ClaimsPrincipal principal) => GetClaim(principal, AllowedClaims.Name);

        public static string Email(this ClaimsPrincipal principal) => GetClaim(principal, AllowedClaims.Email);

        private static void CheckClaimsPrincipal(ClaimsPrincipal principal)
        {
            if (principal == null) throw new NullReferenceException("Claims Principal is null.");
        }

        private static string SystemClaim(this AllowedClaims claim) => claim switch
        {
            AllowedClaims.Name => ClaimTypes.Name,
            AllowedClaims.Email => ClaimTypes.Email,
            _ => throw new Exception($"'{claim}' is not allowed."),
        };
    }
}

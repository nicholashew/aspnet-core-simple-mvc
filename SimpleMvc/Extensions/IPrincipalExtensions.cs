using System.Security.Claims;

namespace SimpleMvc.Extensions
{
    public static class IPrincipalExtensions
    {
        public static bool IsInAnyRole(this ClaimsPrincipal user, params string[] roles)
        {
            foreach (string role in roles)
            {
                if (user.IsInRole(role))
                    return true;
            }
            return false;
        }
    }
}
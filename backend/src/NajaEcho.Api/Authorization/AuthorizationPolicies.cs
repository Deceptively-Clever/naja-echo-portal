using Microsoft.AspNetCore.Authorization;

namespace NajaEcho.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string Admin = "Admin";

    public static AuthorizationOptions AddAdminPolicy(this AuthorizationOptions options)
    {
        options.AddPolicy(Admin, policy => policy.RequireRole(Admin));
        return options;
    }
}

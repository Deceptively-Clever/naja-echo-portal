using Microsoft.AspNetCore.Authorization;

namespace NajaEcho.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string Admin = "Admin";
    public const string Quartermaster = "Quartermaster";

    public static AuthorizationOptions AddPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(Admin, policy => policy.RequireRole(Admin));
        options.AddPolicy(Quartermaster, policy => policy.RequireRole(Quartermaster, Admin));
        return options;
    }
}

using Microsoft.AspNetCore.Authorization;

namespace GeminiCode.Thomas.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public class AuthorizeForAttribute : AuthorizeAttribute
{
    public AuthorizeForAttribute(params string[] roles)
    {
        Roles = string.Join(",", roles);
    }
}

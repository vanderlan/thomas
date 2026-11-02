using System.ComponentModel;
using static GeminiCode.Thomas.Domain.Core.Authentication.AuthorizationConfiguration;

namespace GeminiCode.Thomas.Domain.Core.Entities.Enuns;

public enum UserProfile
{
    [Description(Roles.Admin)]
    Admin = 1,

    [Description(Roles.Operator)]
    Operator = 2
}

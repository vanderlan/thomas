using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Entities.Enuns;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Users.Commands.Create;

public class UserCreateRequest : IRequest<UserCreateResponse>
{
    public string Name { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }
    public UserProfile Profile { get; set; }

    public static explicit operator User(UserCreateRequest request)
    {
        if (request is null)
            return default;

        return new User
        {
            Name = request.Name,
            Password = request.Password,
            Email = request.Email,
            Profile = request.Profile
        };
    }
}


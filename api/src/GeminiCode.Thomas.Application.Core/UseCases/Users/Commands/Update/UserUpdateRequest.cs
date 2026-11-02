using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Entities.Enuns;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Users.Commands.Update;

public class UserUpdateRequest : IRequest
{
    public string PublicId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public UserProfile Profile { get; set; }

    public static explicit operator User(UserUpdateRequest request)
    {
        if (request is null)
            return default;

        return new User
        {
            PublicId = request.PublicId,
            Name = request.Name,
            Email = request.Email,
            Profile = request.Profile
        };
    }
}

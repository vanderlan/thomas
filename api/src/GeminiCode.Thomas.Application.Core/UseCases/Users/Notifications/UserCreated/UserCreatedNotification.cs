using GeminiCode.Thomas.Domain.Core.Entities;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Users.Notifications.UserCreated
{
    public class UserCreatedNotification(User user) : INotification
    {
        public User Entity { get; set; } = user;
    }
}

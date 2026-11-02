using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Users.Commands.ChangePassword
{
    public class UserChangePasswordRequest : IRequest
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string NewPasswordConfirm { get; set; }
    }
}

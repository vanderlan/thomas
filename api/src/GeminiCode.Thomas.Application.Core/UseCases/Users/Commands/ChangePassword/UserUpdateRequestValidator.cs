using GeminiCode.Thomas.Crosscutting.Resources;
using GeminiCode.Thomas.Crosscutting.Resources.Messages;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace GeminiCode.Thomas.Application.Core.UseCases.Users.Commands.ChangePassword;

public class UserChangePasswordRequestValidator : AbstractValidator<UserChangePasswordRequest>
{
    public UserChangePasswordRequestValidator(IStringLocalizer<ThomasResources> stringLocalizer)
    {

        RuleFor(c => c.CurrentPassword)
            .NotEmpty().WithMessage(stringLocalizer[MessagesKeys.UserMessages.EmptyPassword]);

        RuleFor(c => c.NewPassword)
            .NotEmpty().WithMessage(stringLocalizer[MessagesKeys.UserMessages.EmptyNewPassword]);

        RuleFor(c => c.NewPasswordConfirm)
            .NotEmpty().WithMessage(stringLocalizer[MessagesKeys.UserMessages.EmptyNewPasswordConfirmation]);

        RuleFor(x => x.NewPassword).Equal(x => x.NewPasswordConfirm)
            .WithMessage(stringLocalizer[MessagesKeys.UserMessages.PasswordAndConfirmationDifferent]);
    }
}

using GeminiCode.Thomas.Crosscutting.Resources;
using GeminiCode.Thomas.Crosscutting.Resources.Messages;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace GeminiCode.Thomas.Application.Core.UseCases.Users.Commands.Update;

public class UserUpdateRequestValidator : AbstractValidator<UserUpdateRequest>
{
    public UserUpdateRequestValidator(IStringLocalizer<ThomasResources> stringLocalizer)
    {
        RuleFor(c => c)
            .NotNull()
            .WithMessage(stringLocalizer[MessagesKeys.UserMessages.NullEntity]);

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage(stringLocalizer[MessagesKeys.UserMessages.EmptyName]);

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage(stringLocalizer[MessagesKeys.UserMessages.EmptyEmail]);

        RuleFor(c => c.PublicId)
            .NotEmpty().WithMessage(stringLocalizer[MessagesKeys.UserMessages.EmptyId]);
    }
}

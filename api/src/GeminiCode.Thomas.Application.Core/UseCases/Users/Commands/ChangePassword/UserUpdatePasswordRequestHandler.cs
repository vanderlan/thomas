using GeminiCode.Thomas.Application.Core.UseCases.Users.Notifications.UserPasswordChanged;
using GeminiCode.Thomas.Crosscutting.Resources;
using GeminiCode.Thomas.Domain.Core.Authentication;
using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Domain.Core.Extensions;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Localization;
using static GeminiCode.Thomas.Crosscutting.Resources.Messages.MessagesKeys;

namespace GeminiCode.Thomas.Application.Core.UseCases.Users.Commands.ChangePassword;

public class UserUpdatePasswordRequestHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IMediator mediator,
    IStringLocalizer<ThomasResources> resourceMessages) : IRequestHandler<UserChangePasswordRequest>
{
    public async Task Handle(UserChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(currentUser.Id) ?? throw new NotFoundException(currentUser.Id);

        if (user.Password != request.CurrentPassword.ToSha512())
            throw new BusinessException(resourceMessages[UserMessages.InvalidPassword]);

        user.Password = request.NewPassword.ToSha512();

        unitOfWork.UserRepository.Update(user);

        await unitOfWork.CommitAsync();

        await mediator.Publish(new UserPasswordChangedNotification(), cancellationToken);
    }
}

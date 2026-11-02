using GeminiCode.Thomas.Crosscutting.Resources;
using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Localization;
using static GeminiCode.Thomas.Crosscutting.Resources.Messages.MessagesKeys;

namespace GeminiCode.Thomas.Application.Core.UseCases.Users.Commands.Update;

public class UserUpdateRequestHandler(IUnitOfWork unitOfWork, IStringLocalizer<ThomasResources> resourceMessages) 
    : IRequestHandler<UserUpdateRequest>
{
    public async Task Handle(UserUpdateRequest request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(request.PublicId);
        await ValidateUser(request);

        user.Email = request.Email;
        user.Name = request.Name;
        user.Profile = request.Profile;

        unitOfWork.UserRepository.Update(user);

        await unitOfWork.CommitAsync();
    }

    private async Task ValidateUser(UserUpdateRequest request)
    {
        var userFound = await unitOfWork.UserRepository.FindByEmailAsync(request.Email);

        if (userFound != null && userFound.PublicId != request.PublicId)
            throw new ConflictException(resourceMessages[UserMessages.EmailExists], resourceMessages[ExceptionsTitles.ValidationError]);
    }
}
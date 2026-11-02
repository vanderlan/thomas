using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Users.Commands.Delete;

public class UserDeleteRequestHandler(IUnitOfWork unitOfWork) : IRequestHandler<UserDeleteRequest>
{
    public async Task Handle(UserDeleteRequest request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(request.PublicId);

        if (user == null)
            throw new NotFoundException(request.PublicId);

        await unitOfWork.UserRepository.DeleteAsync(request.PublicId);
        await unitOfWork.CommitAsync();
    }
}
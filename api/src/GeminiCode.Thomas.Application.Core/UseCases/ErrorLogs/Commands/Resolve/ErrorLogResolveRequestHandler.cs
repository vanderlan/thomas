using GeminiCode.Thomas.Crosscutting.Resources;
using GeminiCode.Thomas.Domain.Core.Entities.Enuns;
using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Localization;
using static GeminiCode.Thomas.Crosscutting.Resources.Messages.MessagesKeys;

namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.Resolve;

public class ErrorLogResolveRequestHandler(
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IStringLocalizer<ThomasResources> resourceMessages) : IRequestHandler<ErrorLogResolveRequest>
{
    public async Task Handle(ErrorLogResolveRequest request, CancellationToken cancellationToken)
    {
        var errorLog = await unitOfWork.ErrorLogRepository.GetByIdAsync(request.PublicId)
            ?? throw new NotFoundException(request.PublicId);

        if (errorLog.Status == ErrorLogStatus.Resolved)
            throw new BusinessException(resourceMessages[ErrorLogMessages.AlreadyResolved], resourceMessages[ExceptionsTitles.ValidationError]);

        errorLog.Resolve(timeProvider.GetUtcNow().UtcDateTime);

        unitOfWork.ErrorLogRepository.Update(errorLog);

        await unitOfWork.CommitAsync();
    }
}

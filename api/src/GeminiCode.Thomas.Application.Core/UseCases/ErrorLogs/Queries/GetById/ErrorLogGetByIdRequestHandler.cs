using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Queries.GetById;

public class ErrorLogGetByIdRequestHandler(IUnitOfWork unitOfWork) : IRequestHandler<ErrorLogGetByIdRequest, ErrorLogGetByIdResponse>
{
    public async Task<ErrorLogGetByIdResponse> Handle(ErrorLogGetByIdRequest request, CancellationToken cancellationToken)
    {
        var errorLog = await unitOfWork.ErrorLogRepository.GetByIdAsync(request.PublicId)
            ?? throw new NotFoundException(request.PublicId);

        return (ErrorLogGetByIdResponse)errorLog;
    }
}

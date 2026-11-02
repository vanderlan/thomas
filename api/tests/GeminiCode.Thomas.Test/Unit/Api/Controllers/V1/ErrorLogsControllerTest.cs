using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeminiCode.Thomas.Api.Controllers.V1;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.Resolve;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Queries.GetById;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Queries.GetPaginated;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;
using GeminiCode.Thomas.Test.Shared.Faker;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Api.Controllers.V1;

public class ErrorLogsControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ErrorLogsController _errorLogsController;
    private readonly ErrorLog _validErrorLog = ErrorLogFaker.Get();

    public ErrorLogsControllerTest()
    {
        var errorLogList = new List<ErrorLogGetPaginatedResponse>(2)
        {
            _validErrorLog,
            ErrorLogFaker.Get()
        };

        _mediatorMock.Setup(x => x.Send(It.IsAny<ErrorLogGetPaginatedRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedList<ErrorLogGetPaginatedResponse>(errorLogList, 2));

        _mediatorMock.Setup(x => x.Send(It.IsAny<ErrorLogGetByIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorLogGetByIdResponse)_validErrorLog);

        _errorLogsController = new ErrorLogsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetErrorLogById_WithValidId_ReturnsTheErrorLogWithItsStackTrace()
    {
        //arrange & act
        var result = await _errorLogsController.Get(_validErrorLog.PublicId);

        var contentResult = (OkObjectResult)result;
        var errorLog = (ErrorLogGetByIdResponse)contentResult.Value;

        //assert
        Assert.Equal(200, contentResult.StatusCode);
        Assert.Equal(_validErrorLog.Fingerprint, errorLog.Fingerprint);
        Assert.Equal(_validErrorLog.ErrorClass, errorLog.ErrorClass);
        Assert.Equal(_validErrorLog.StackTrace, errorLog.StackTrace);
        Assert.Equal(_validErrorLog.Occurrences, errorLog.Occurrences);
    }

    [Fact]
    public async Task GetErrorLogs_WithValidFilter_ReturnsAPagedList()
    {
        //arrange & act
        var result = await _errorLogsController.Get(new ErrorLogGetPaginatedRequest { ProjectId = _validErrorLog.ProjectId });

        var contentResult = (OkObjectResult)result;
        var pagedList = (PagedList<ErrorLogGetPaginatedResponse>)contentResult.Value;

        //assert
        Assert.Equal(200, contentResult.StatusCode);
        Assert.Equal(2, pagedList.Count);
    }

    [Fact]
    public async Task PatchResolve_WithExistingId_ReturnsAccepted()
    {
        //arrange & act
        var result = await _errorLogsController.PatchResolve(_validErrorLog.PublicId);

        var contentResult = (AcceptedResult)result;

        //assert
        Assert.Equal(202, contentResult.StatusCode);
        _mediatorMock.Verify(x => x.Send(It.Is<ErrorLogResolveRequest>(request => request.PublicId == _validErrorLog.PublicId), It.IsAny<CancellationToken>()), Times.Once);
    }
}

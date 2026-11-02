using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using GeminiCode.Thomas.Api.Attributes;
using GeminiCode.Thomas.Api.Controllers.Base;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.Resolve;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Queries.GetById;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Queries.GetPaginated;
using static GeminiCode.Thomas.Domain.Core.Authentication.AuthorizationConfiguration;

namespace GeminiCode.Thomas.Api.Controllers.V1;

/// <summary>
/// Exposes the errors knowledge base built by the sync job.
/// </summary>
[ApiVersion(1.0)]
[Route("[controller]")]
[AuthorizeFor(Roles.Admin, Roles.Operator)]
public class ErrorLogsController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet]
    [SwaggerResponse((int)HttpStatusCode.OK, "A success response with a list of Error Logs paginated, filtered by project, status or text", typeof(PagedList<ErrorLogGetPaginatedResponse>))]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Get([FromQuery] ErrorLogGetPaginatedRequest filter)
    {
        return Ok(await Mediator.Send(filter));
    }

    [HttpGet("{id}")]
    [SwaggerResponse((int)HttpStatusCode.OK, "A success response with a single Error Log, including its stack trace", typeof(ErrorLogGetByIdResponse))]
    [SwaggerResponse((int)HttpStatusCode.NotFound)]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Get(string id)
    {
        var errorLog = await Mediator.Send(new ErrorLogGetByIdRequest(id));

        if (errorLog is null)
            return NotFound();

        return Ok(errorLog);
    }

    [HttpPatch("{id}/Resolve")]
    [AuthorizeFor(Roles.Admin)]
    [SwaggerResponse((int)HttpStatusCode.Accepted)]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "A error response with the error description", typeof(ExceptionResponse))]
    [SwaggerResponse((int)HttpStatusCode.NotFound)]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> PatchResolve(string id)
    {
        await Mediator.Send(new ErrorLogResolveRequest(id));

        return Accepted();
    }
}

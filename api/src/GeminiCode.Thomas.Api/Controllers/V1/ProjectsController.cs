using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using GeminiCode.Thomas.Api.Attributes;
using GeminiCode.Thomas.Api.Controllers.Base;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncProjectErrors;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Create;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Delete;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Update;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Queries.GetById;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Queries.GetPaginated;
using static GeminiCode.Thomas.Domain.Core.Authentication.AuthorizationConfiguration;

namespace GeminiCode.Thomas.Api.Controllers.V1;

/// <summary>
/// Manages the portfolio of projects monitored by the error log sync job.
/// </summary>
[ApiVersion(1.0)]
[Route("[controller]")]
[AuthorizeFor(Roles.Admin)]
public class ProjectsController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet]
    [SwaggerResponse((int)HttpStatusCode.OK, "A success response with a list of Projects paginated", typeof(PagedList<ProjectGetPaginatedResponse>))]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Get([FromQuery] ProjectGetPaginatedRequest filter)
    {
        return Ok(await Mediator.Send(filter));
    }

    [HttpGet("{id}")]
    [SwaggerResponse((int)HttpStatusCode.OK, "A success response with a single Project", typeof(ProjectGetByIdResponse))]
    [SwaggerResponse((int)HttpStatusCode.NotFound)]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Get(string id)
    {
        var project = await Mediator.Send(new ProjectGetByIdRequest(id));

        if (project is null)
            return NotFound();

        return Ok(project);
    }

    [HttpPost]
    [SwaggerResponse((int)HttpStatusCode.Created, "A valid Project created", typeof(ProjectCreateResponse))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "A error response with the error description", typeof(ExceptionResponse))]
    [SwaggerResponse((int)HttpStatusCode.Conflict, "There is already a project with the same name", typeof(ExceptionResponse))]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Post([FromBody] ProjectCreateRequest projectCreateRequest)
    {
        return Created(await Mediator.Send(projectCreateRequest));
    }

    [HttpPut("{id}")]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "A error response with the error description", typeof(ExceptionResponse))]
    [SwaggerResponse((int)HttpStatusCode.Accepted)]
    [SwaggerResponse((int)HttpStatusCode.NotFound)]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Put(string id, [FromBody] ProjectUpdateRequest projectUpdateRequest)
    {
        projectUpdateRequest.PublicId = id;

        await Mediator.Send(projectUpdateRequest);

        return Accepted();
    }

    [HttpDelete("{id}")]
    [SwaggerResponse((int)HttpStatusCode.NoContent)]
    [SwaggerResponse((int)HttpStatusCode.NotFound)]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Delete(string id)
    {
        await Mediator.Send(new ProjectDeleteRequest(id));

        return NoContent();
    }

    /// <summary>
    /// Triggers, on demand, the same synchronization executed by the background job:
    /// collects the errors of the project from NewRelic and updates its knowledge base.
    /// </summary>
    [HttpPost("{id}/Errors/Sync")]
    [SwaggerResponse((int)HttpStatusCode.OK, "A summary of the synchronization", typeof(SyncProjectErrorsResponse))]
    [SwaggerResponse((int)HttpStatusCode.BadGateway, "The NewRelic API could not be reached or rejected the request", typeof(ExceptionResponse))]
    [SwaggerResponse((int)HttpStatusCode.NotFound)]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> PostSyncErrors(string id)
    {
        return Ok(await Mediator.Send(new SyncProjectErrorsRequest(id)));
    }
}

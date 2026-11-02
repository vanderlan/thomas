using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeminiCode.Thomas.Api.Controllers.Base;

[ApiController]
public abstract class ApiController(IMediator mediator) : ControllerBase
{
    protected readonly IMediator Mediator = mediator;

    protected CreatedResult Created(object entity)
    {
        return base.Created("{id}", entity);
    }
}
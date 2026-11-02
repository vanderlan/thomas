using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GeminiCode.Thomas.Domain.Core.Exceptions;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using GeminiCode.Thomas.Api.Configuration;
using GeminiCode.Thomas.Api.Controllers.Base;
using GeminiCode.Thomas.Api.Models;
using GeminiCode.Thomas.Application.Core.UseCases.Auth.Commands.LoginWithCredentials;
using GeminiCode.Thomas.Application.Core.UseCases.Auth.Commands.LoginWithRefreshToken;

namespace GeminiCode.Thomas.Api.Controllers.V1;

[ApiVersion(1.0)]
[Route("[controller]")]
[AllowAnonymous]
public class AuthController(IMediator mediator, IConfiguration configuration) : ApiController(mediator)
{
    [HttpPost("Login")]
    [SwaggerResponse((int)HttpStatusCode.OK, "A success reponse with a Token, Refresh Token and expiration date", typeof(LoginWithCredentialsResponse))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "A error response with the error description", typeof(ExceptionResponse))]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginWithCredentialsRequest loginWithCredentialsRequest)
    {
        return AuthorizeUser(await Mediator.Send(loginWithCredentialsRequest));
    }

    [HttpPost("RefreshToken")]
    [SwaggerResponse((int)HttpStatusCode.OK, "A success reponse with a Token, Refresh Token and expiration date", typeof(LoginWithRefreshTokenResponse))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "A error response with the error description", typeof(ExceptionResponse))]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] LoginWithRefreshTokenRequest loginWithRefreshTokenRequest)
    {
        return AuthorizeUser(await Mediator.Send(loginWithRefreshTokenRequest));
    }

    private OkObjectResult AuthorizeUser(LoginWithCredentialsResponse loginWithCredentialsResponse)
    {
        var (token, validTo) = AuthenticationConfiguration.CreateToken(loginWithCredentialsResponse, configuration);

        return Ok(
            new UserApiTokenModel
            {
                Token = token,
                Expiration = validTo,
                RefreshToken = loginWithCredentialsResponse.RefreshToken
            });
    }
}
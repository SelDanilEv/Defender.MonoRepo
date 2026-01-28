using System.Threading.Tasks;
using AutoMapper;
using Defender.Common.DB.Pagination;
using Defender.Common.Entities.Secrets;
using Defender.SecretManagementService.Application.Modules.Secret.Commands;
using Defender.SecretManagementService.Application.Modules.Secret.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.V1;

public class SecretController : BaseApiController
{
    public SecretController(IMediator mediator, IMapper mapper) : base(mediator, mapper)
    {
    }

    [HttpGet("getValue")]
    //[Auth(Roles.SuperAdmin)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<string> GetSecretValue([FromQuery] GetSecretValueQuery query)
    {
        return await ProcessApiCallAsync<GetSecretValueQuery, string>(query);
    }

    [HttpGet("get-all/keys")]
    //[Auth(Roles.SuperAdmin)]
    [ProducesResponseType(typeof(PagedResult<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<PagedResult<string>> GetAllSecretKeys([FromQuery] GetAllSecretKeysQuery query)
    {
        return await ProcessApiCallWithoutMappingAsync<GetAllSecretKeysQuery, PagedResult<string>>(query);
    }

    [HttpGet("get")]
    //[Auth(Roles.SuperAdmin)]
    [ProducesResponseType(typeof(MongoSecret), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<MongoSecret> GetSecret([FromQuery] GetSecretQuery query)
    {
        return await ProcessApiCallAsync<GetSecretQuery, MongoSecret>(query);
    }

    [HttpPost("create-or-update")]
    //[Auth(Roles.SuperAdmin)]
    [ProducesResponseType(typeof(MongoSecret), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<MongoSecret> CreateOrUpdateSecret([FromBody] CreateOrUpdateSecretCommand command)
    {
        return await ProcessApiCallAsync<CreateOrUpdateSecretCommand, MongoSecret>(command);
    }

    [HttpDelete("delete")]
    //[Auth(Roles.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task CreateOrUpdateSecret([FromBody] DeleteSecretCommand command)
    {
        await ProcessApiCallAsync(command);
    }

}

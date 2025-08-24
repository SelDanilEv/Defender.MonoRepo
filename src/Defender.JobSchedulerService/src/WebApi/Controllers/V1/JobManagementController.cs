using System.Threading.Tasks;
using AutoMapper;
using Defender.Common.DB.Pagination;
using Defender.JobSchedulerService.Application.Modules.Jobs.Commands;
using Defender.JobSchedulerService.Application.Modules.Jobs.Queries;
using Defender.JobSchedulerService.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.V1;

public class JobManagementController : BaseApiController
{
    public JobManagementController(IMediator mediator, IMapper mapper) : base(mediator, mapper)
    {
    }

    [HttpGet("get")]
    //[Auth(Roles.Admin)]
    [ProducesResponseType(typeof(PagedResult<ScheduledJob>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<PagedResult<ScheduledJob>> GetJobsAsync([FromQuery] GetJobsQuery query)
    {
        return await ProcessApiCallAsync<GetJobsQuery, PagedResult<ScheduledJob>>(query);
    }

    [HttpPost("start")]
    //[Auth(Roles.Admin)]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task StartJobAsync([FromBody] StartJobCommand command)
    {
        await ProcessApiCallAsync(command);
    }

    [HttpPost("create")]
    //[Auth(Roles.Admin)]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task CreateJobAsync([FromBody] CreateJobCommand command)
    {
        await ProcessApiCallAsync(command);
    }

    [HttpPut("update")]
    //[Auth(Roles.Admin)]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task UpdateJobAsync([FromBody] UpdateJobCommand command)
    {
        await ProcessApiCallAsync(command);
    }

    [HttpDelete("delete")]
    //[Auth(Roles.Admin)]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task DeleteJobAsync([FromBody] DeleteJobCommand command)
    {
        await ProcessApiCallAsync(command);
    }

}

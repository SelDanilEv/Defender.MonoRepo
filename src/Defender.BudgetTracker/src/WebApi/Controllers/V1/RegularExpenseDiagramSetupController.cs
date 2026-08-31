using AutoMapper;
using Defender.BudgetTracker.Application.DTOs;
using Defender.BudgetTracker.Application.Modules.RegularExpenseDiagramSetups.Commands;
using Defender.BudgetTracker.Application.Modules.RegularExpenseDiagramSetups.Queries;
using Defender.Common.Attributes;
using Defender.Common.Consts;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.V1;

public class RegularExpenseDiagramSetupController : BaseApiController
{
    public RegularExpenseDiagramSetupController(IMediator mediator, IMapper mapper)
        : base(mediator, mapper)
    {
    }

    [HttpGet]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(RegularExpenseDiagramSetupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<RegularExpenseDiagramSetupDto> Get(
        [FromQuery] GetRegularExpenseDiagramSetupQuery query)
    {
        return await ProcessApiCallAsync<
            GetRegularExpenseDiagramSetupQuery,
            RegularExpenseDiagramSetupDto>(query);
    }

    [HttpPost]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(RegularExpenseDiagramSetupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<RegularExpenseDiagramSetupDto> Update(
        [FromBody] UpdateRegularExpenseDiagramSetupCommand command)
    {
        return await ProcessApiCallAsync<
            UpdateRegularExpenseDiagramSetupCommand,
            RegularExpenseDiagramSetupDto>(command);
    }
}

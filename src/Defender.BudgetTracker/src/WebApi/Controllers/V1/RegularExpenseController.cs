using AutoMapper;
using Defender.BudgetTracker.Application.DTOs;
using Defender.BudgetTracker.Application.Modules.RegularExpenses.Commands;
using Defender.BudgetTracker.Application.Modules.RegularExpenses.Queries;
using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.Common.DB.Pagination;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.V1;

public class RegularExpenseController : BaseApiController
{
    public RegularExpenseController(IMediator mediator, IMapper mapper)
        : base(mediator, mapper)
    {
    }

    [HttpGet]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PagedResult<RegularExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<PagedResult<RegularExpenseDto>> GetRegularExpenses(
        [FromQuery] GetRegularExpensesQuery query)
    {
        return await ProcessApiCallAsync<GetRegularExpensesQuery, PagedResult<RegularExpenseDto>>(query);
    }

    [HttpPost]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(RegularExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<RegularExpenseDto> Create([FromBody] CreateRegularExpenseCommand command)
    {
        return await ProcessApiCallAsync<CreateRegularExpenseCommand, RegularExpenseDto>(command);
    }

    [HttpPut]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(RegularExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<RegularExpenseDto> Update([FromBody] UpdateRegularExpenseCommand command)
    {
        return await ProcessApiCallAsync<UpdateRegularExpenseCommand, RegularExpenseDto>(command);
    }

    [HttpDelete]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<Guid> Delete([FromQuery] DeleteRegularExpenseCommand command)
    {
        return await ProcessApiCallAsync<DeleteRegularExpenseCommand, Guid>(command);
    }
}

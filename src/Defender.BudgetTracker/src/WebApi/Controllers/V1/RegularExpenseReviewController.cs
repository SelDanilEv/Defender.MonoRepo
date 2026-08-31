using AutoMapper;
using Defender.BudgetTracker.Application.DTOs;
using Defender.BudgetTracker.Application.Modules.RegularExpenseReviews.Commands;
using Defender.BudgetTracker.Application.Modules.RegularExpenseReviews.Queries;
using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.Common.DB.Pagination;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.V1;

public class RegularExpenseReviewController : BaseApiController
{
    public RegularExpenseReviewController(IMediator mediator, IMapper mapper)
        : base(mediator, mapper)
    {
    }

    [HttpGet]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PagedResult<RegularExpenseReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<PagedResult<RegularExpenseReviewDto>> GetRegularExpenseReviews(
        [FromQuery] GetRegularExpenseReviewsQuery query)
    {
        return await ProcessApiCallAsync<GetRegularExpenseReviewsQuery, PagedResult<RegularExpenseReviewDto>>(query);
    }

    [HttpGet("by-date-range")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(List<RegularExpenseReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<List<RegularExpenseReviewDto>> GetRegularExpenseReviewsByDateRange(
        [FromQuery] GetRegularExpenseReviewsByDateRangeQuery query)
    {
        return await ProcessApiCallAsync<
            GetRegularExpenseReviewsByDateRangeQuery,
            List<RegularExpenseReviewDto>>(query);
    }

    [HttpGet("template")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(RegularExpenseReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<RegularExpenseReviewDto> GetTemplate(
        [FromQuery] GetRegularExpenseReviewTemplateQuery query)
    {
        return await ProcessApiCallAsync<
            GetRegularExpenseReviewTemplateQuery,
            RegularExpenseReviewDto>(query);
    }

    [HttpPost]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(RegularExpenseReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<RegularExpenseReviewDto> Publish(
        [FromBody] PublishRegularExpenseReviewCommand command)
    {
        return await ProcessApiCallAsync<
            PublishRegularExpenseReviewCommand,
            RegularExpenseReviewDto>(command);
    }

    [HttpDelete]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<Guid> Delete([FromQuery] DeleteRegularExpenseReviewCommand command)
    {
        return await ProcessApiCallAsync<DeleteRegularExpenseReviewCommand, Guid>(command);
    }
}

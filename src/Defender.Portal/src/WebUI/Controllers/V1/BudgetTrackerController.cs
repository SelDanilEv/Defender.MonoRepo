using AutoMapper;
using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.Common.DB.Pagination;
using Defender.Portal.Application.DTOs.BudgetTracking.DiagramSetup;
using Defender.Portal.Application.DTOs.BudgetTracking.Groups;
using Defender.Portal.Application.DTOs.BudgetTracking.Positions;
using Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;
using Defender.Portal.Application.DTOs.BudgetTracking.Reviews;
using Defender.Portal.Application.Modules.BudgetTracking.BudgetReviews.Commands;
using Defender.Portal.Application.Modules.BudgetTracking.BudgetReviews.Queries;
using Defender.Portal.Application.Modules.BudgetTracking.Groups.Commands;
using Defender.Portal.Application.Modules.BudgetTracking.Groups.Queries;
using Defender.Portal.Application.Modules.BudgetTracking.MainDiagramSetup.Commands;
using Defender.Portal.Application.Modules.BudgetTracking.MainDiagramSetup.Queries;
using Defender.Portal.Application.Modules.BudgetTracking.Positions.Commands;
using Defender.Portal.Application.Modules.BudgetTracking.Positions.Queries;
using Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Commands;
using Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Defender.Portal.WebUI.Controllers.V1;

public class BudgetTrackerController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    #region BudgetPosition

    [HttpGet("positions")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PagedResult<PortalBudgetPosition>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetPositions([FromQuery] GetPositionsQuery query)
    {
        return await ProcessApiCallAsync<GetPositionsQuery, PagedResult<PortalBudgetPosition>>(query);
    }

    [HttpPost("position")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalBudgetPosition), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreatePosition([FromBody] CreatePositionCommand command)
    {
        return await ProcessApiCallAsync<CreatePositionCommand, PortalBudgetPosition>(command);
    }

    [HttpPut("position")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalBudgetPosition), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdatePosition([FromBody] UpdatePositionCommand command)
    {
        return await ProcessApiCallAsync<UpdatePositionCommand, PortalBudgetPosition>(command);
    }

    [HttpDelete("position/{Id}")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeletePosition(DeletePositionCommand command)
    {
        return await ProcessApiCallAsync<DeletePositionCommand, Guid>(command);
    }

    #endregion


    #region BudgetReview

    [HttpGet("budget-reviews")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PagedResult<PortalBudgetReview>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetBudgetReviews([FromQuery] GetBudgetReviewsQuery query)
    {
        return await ProcessApiCallAsync<GetBudgetReviewsQuery, PagedResult<PortalBudgetReview>>(query);
    }

    [HttpGet("budget-reviews/by-date-range")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(List<PortalBudgetReview>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetBudgetReviewsByDateRanges([FromQuery] GetBudgetReviewsByDateRangeQuery query)
    {
        return await ProcessApiCallAsync<GetBudgetReviewsByDateRangeQuery, List<PortalBudgetReview>>(query);
    }

    [HttpGet("budget-review/template")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalBudgetReview), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateBudgetReview([FromQuery] GetBudgetReviewTemplateQuery query)
    {
        return await ProcessApiCallAsync<GetBudgetReviewTemplateQuery, PortalBudgetReview>(query);
    }

    [HttpPost("budget-review")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalBudgetReview), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdateBudgetReview([FromBody] PublishReviewCommand command)
    {
        return await ProcessApiCallAsync<PublishReviewCommand, PortalBudgetReview>(command);
    }

    [HttpDelete("budget-review/{Id}")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteBudgetReview(DeleteBudgetReviewCommand command)
    {
        return await ProcessApiCallAsync<DeleteBudgetReviewCommand, Guid>(command);
    }

    #endregion


    #region Main Diagram Setup

    [HttpGet("diagram-setup/main")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalMainDiagramSetup), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetMainDiagramSetup([FromQuery] GetMainDiagramSetupQuery query)
    {
        return await ProcessApiCallAsync<GetMainDiagramSetupQuery, PortalMainDiagramSetup>(query);
    }

    [HttpPost("diagram-setup/main")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalMainDiagramSetup), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdateMainDiagramSetup([FromBody] PublishMainDiagramSetupCommand command)
    {
        return await ProcessApiCallAsync<PublishMainDiagramSetupCommand, PortalMainDiagramSetup>(command);
    }

    #endregion


    #region Regular Expenses

    [HttpGet("regular-expenses/expenses")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PagedResult<PortalRegularExpense>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetRegularExpenses([FromQuery] GetRegularExpensesQuery query)
    {
        return await ProcessApiCallAsync<GetRegularExpensesQuery, PagedResult<PortalRegularExpense>>(query);
    }

    [HttpPost("regular-expenses/expense")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalRegularExpense), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateRegularExpense([FromBody] CreateRegularExpenseCommand command)
    {
        return await ProcessApiCallAsync<CreateRegularExpenseCommand, PortalRegularExpense>(command);
    }

    [HttpPut("regular-expenses/expense")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalRegularExpense), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdateRegularExpense([FromBody] UpdateRegularExpenseCommand command)
    {
        return await ProcessApiCallAsync<UpdateRegularExpenseCommand, PortalRegularExpense>(command);
    }

    [HttpDelete("regular-expenses/expense/{Id}")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteRegularExpense(DeleteRegularExpenseCommand command)
    {
        return await ProcessApiCallAsync<DeleteRegularExpenseCommand, Guid>(command);
    }

    [HttpGet("regular-expenses/reviews")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PagedResult<PortalRegularExpenseReview>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetRegularExpenseReviews([FromQuery] GetRegularExpenseReviewsQuery query)
    {
        return await ProcessApiCallAsync<
            GetRegularExpenseReviewsQuery,
            PagedResult<PortalRegularExpenseReview>>(query);
    }

    [HttpGet("regular-expenses/reviews/by-date-range")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(List<PortalRegularExpenseReview>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetRegularExpenseReviewsByDateRange(
        [FromQuery] GetRegularExpenseReviewsByDateRangeQuery query)
    {
        return await ProcessApiCallAsync<
            GetRegularExpenseReviewsByDateRangeQuery,
            List<PortalRegularExpenseReview>>(query);
    }

    [HttpGet("regular-expenses/review/template")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalRegularExpenseReview), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetRegularExpenseReviewTemplate(
        [FromQuery] GetRegularExpenseReviewTemplateQuery query)
    {
        return await ProcessApiCallAsync<
            GetRegularExpenseReviewTemplateQuery,
            PortalRegularExpenseReview>(query);
    }

    [HttpPost("regular-expenses/review")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalRegularExpenseReview), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PublishRegularExpenseReview(
        [FromBody] PublishRegularExpenseReviewCommand command)
    {
        return await ProcessApiCallAsync<
            PublishRegularExpenseReviewCommand,
            PortalRegularExpenseReview>(command);
    }

    [HttpDelete("regular-expenses/review/{Id}")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteRegularExpenseReview(DeleteRegularExpenseReviewCommand command)
    {
        return await ProcessApiCallAsync<DeleteRegularExpenseReviewCommand, Guid>(command);
    }

    [HttpGet("regular-expenses/diagram-setup")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalRegularExpenseDiagramSetup), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetRegularExpenseDiagramSetup(
        [FromQuery] GetRegularExpenseDiagramSetupQuery query)
    {
        return await ProcessApiCallAsync<
            GetRegularExpenseDiagramSetupQuery,
            PortalRegularExpenseDiagramSetup>(query);
    }

    [HttpPost("regular-expenses/diagram-setup")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalRegularExpenseDiagramSetup), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdateRegularExpenseDiagramSetup(
        [FromBody] UpdateRegularExpenseDiagramSetupCommand command)
    {
        return await ProcessApiCallAsync<
            UpdateRegularExpenseDiagramSetupCommand,
            PortalRegularExpenseDiagramSetup>(command);
    }

    #endregion


    #region BudgetPosition

    [HttpGet("groups")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PagedResult<PortalBudgetGroup>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetBudgetGroups([FromQuery] GetBudgetGroupsQuery query)
    {
        return await ProcessApiCallAsync<GetBudgetGroupsQuery, PagedResult<PortalBudgetGroup>>(query);
    }

    [HttpPost("group")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalBudgetGroup), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateBudgetGroup([FromBody] CreateBudgetGroupCommand command)
    {
        return await ProcessApiCallAsync<CreateBudgetGroupCommand, PortalBudgetGroup>(command);
    }

    [HttpPut("group")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalBudgetGroup), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdateBudgetGroup([FromBody] UpdateBudgetGroupCommand command)
    {
        return await ProcessApiCallAsync<UpdateBudgetGroupCommand, PortalBudgetGroup>(command);
    }

    [HttpDelete("group/{Id}")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteBudgetGroup(DeleteBudgetGroupCommand command)
    {
        return await ProcessApiCallAsync<DeleteBudgetGroupCommand, Guid>(command);
    }

    #endregion

}

using Defender.Common.DB.Pagination;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;
using FluentValidation;
using MediatR;

namespace Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Queries;

public record GetRegularExpensesQuery : PaginationRequest, IRequest<PagedResult<PortalRegularExpense>>;

public sealed class GetRegularExpensesQueryValidator : AbstractValidator<GetRegularExpensesQuery>
{
}

public sealed class GetRegularExpensesQueryHandler(
    IBudgetTrackerWrapper budgetTrackerWrapper)
    : IRequestHandler<GetRegularExpensesQuery, PagedResult<PortalRegularExpense>>
{
    public Task<PagedResult<PortalRegularExpense>> Handle(
        GetRegularExpensesQuery request,
        CancellationToken cancellationToken)
    {
        return budgetTrackerWrapper.GetRegularExpensesAsync(request);
    }
}

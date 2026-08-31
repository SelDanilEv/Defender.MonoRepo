using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.Common.DB.Pagination;
using FluentValidation;
using MediatR;

namespace Defender.BudgetTracker.Application.Modules.RegularExpenses.Queries;

public record GetRegularExpensesQuery : PaginationRequest, IRequest<PagedResult<RegularExpense>>
{
}

public sealed class GetRegularExpensesQueryValidator : AbstractValidator<GetRegularExpensesQuery>
{
}

public sealed class GetRegularExpensesQueryHandler(
    IRegularExpenseService regularExpenseService)
    : IRequestHandler<GetRegularExpensesQuery, PagedResult<RegularExpense>>
{
    public Task<PagedResult<RegularExpense>> Handle(
        GetRegularExpensesQuery request,
        CancellationToken cancellationToken)
    {
        return regularExpenseService.GetCurrentUserRegularExpensesAsync(request);
    }
}

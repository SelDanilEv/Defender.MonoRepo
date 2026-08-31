using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.Common.Errors;
using Defender.Common.Extension;
using FluentValidation;
using MediatR;

namespace Defender.BudgetTracker.Application.Modules.RegularExpenses.Commands;

public record DeleteRegularExpenseCommand : IRequest<Guid>
{
    public Guid Id { get; init; }
}

public sealed class DeleteRegularExpenseCommandValidator : AbstractValidator<DeleteRegularExpenseCommand>
{
    public DeleteRegularExpenseCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(ErrorCode.VL_InvalidRequest);
    }
}

public sealed class DeleteRegularExpenseCommandHandler(
    IRegularExpenseService regularExpenseService)
    : IRequestHandler<DeleteRegularExpenseCommand, Guid>
{
    public Task<Guid> Handle(
        DeleteRegularExpenseCommand request,
        CancellationToken cancellationToken)
    {
        return regularExpenseService.DeleteRegularExpenseAsync(request.Id);
    }
}

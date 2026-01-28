using Defender.Common.Errors;
using Defender.Common.Extension;
using Defender.Common.Interfaces;
using Defender.SecretManagementService.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Defender.SecretManagementService.Application.Modules.Secret.Commands;

public record DeleteSecretCommand : IRequest<Unit>
{
    public string? SecretName { get; set; }
};

public sealed class DeleteSecretCommandValidator
    : AbstractValidator<DeleteSecretCommand>
{
    public DeleteSecretCommandValidator()
    {
        RuleFor(s => s.SecretName)
            .NotNull()
            .NotEmpty()
            .WithMessage(ErrorCode.VL_SCM_EmptySecretName);
    }
}

public sealed class DeleteSecretCommandHandler
    : IRequestHandler<DeleteSecretCommand, Unit>
{
    private readonly IAccountAccessor _accountAccessor;
    private readonly ISecretManagementService _secretManagementService;

    public DeleteSecretCommandHandler(
        IAccountAccessor accountAccessor,
        ISecretManagementService secretManagementService
        )
    {
        _accountAccessor = accountAccessor;
        _secretManagementService = secretManagementService;
    }

    public async Task<Unit> Handle(
        DeleteSecretCommand request,
        CancellationToken cancellationToken)
    {
        await _secretManagementService
            .DeleteSecretAsync(request.SecretName);

        return Unit.Value;
    }
}

using Defender.Common.Entities.Secrets;
using Defender.Common.Errors;
using Defender.Common.Extension;
using Defender.Common.Interfaces;
using Defender.SecretManagementService.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Defender.SecretManagementService.Application.Modules.Secret.Commands;

public record CreateOrUpdateSecretCommand : IRequest<MongoSecret>
{
    public string? SecretName { get; set; }
    public string? SecretValue { get; set; }
};

public sealed class CreateOrUpdateSecretCommandValidator
    : AbstractValidator<CreateOrUpdateSecretCommand>
{
    public CreateOrUpdateSecretCommandValidator()
    {
        RuleFor(s => s.SecretName)
            .NotNull()
            .NotEmpty()
            .WithMessage(ErrorCode.VL_SCM_EmptySecretName);

        RuleFor(s => s.SecretValue)
            .NotNull()
            .NotEmpty()
            .WithMessage(ErrorCode.VL_SCM_EmptySecretValue);
    }
}

public sealed class CreateOrUpdateSecretCommandHandler
    : IRequestHandler<CreateOrUpdateSecretCommand, MongoSecret>
{
    private readonly IAccountAccessor _accountAccessor;
    private readonly ISecretManagementService _secretManagementService;

    public CreateOrUpdateSecretCommandHandler(
        IAccountAccessor accountAccessor,
        ISecretManagementService secretManagementService
        )
    {
        _accountAccessor = accountAccessor;
        _secretManagementService = secretManagementService;
    }

    public async Task<MongoSecret> Handle(
        CreateOrUpdateSecretCommand request,
        CancellationToken cancellationToken)
    {
        return await _secretManagementService
            .CreateOrUpdateSecretAsync(request.SecretName, request.SecretValue);
    }
}

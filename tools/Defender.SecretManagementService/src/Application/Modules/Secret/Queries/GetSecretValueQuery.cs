using Defender.Common.Errors;
using Defender.Common.Extension;
using Defender.Common.Interfaces;
using Defender.SecretManagementService.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Defender.SecretManagementService.Application.Modules.Secret.Queries;

public record GetSecretValueQuery : IRequest<string>
{
    public string? SecretName { get; set; }
};

public sealed class GetSecretValueQueryValidator : AbstractValidator<GetSecretValueQuery>
{
    public GetSecretValueQueryValidator()
    {
        RuleFor(s => s.SecretName)
            .NotNull()
            .NotEmpty()
            .WithMessage(ErrorCode.VL_SCM_EmptySecretName);
    }
}

public sealed class GetSecretValueQueryHandler
    : IRequestHandler<GetSecretValueQuery, string>
{
    private readonly IAccountAccessor _accountAccessor;
    private readonly ISecretManagementService _secretManagementService;

    public GetSecretValueQueryHandler(
        IAccountAccessor accountAccessor,
        ISecretManagementService secretManagementService
        )
    {
        _accountAccessor = accountAccessor;
        _secretManagementService = secretManagementService;
    }

    public async Task<string> Handle(
        GetSecretValueQuery request,
        CancellationToken cancellationToken)
    {
        return await _secretManagementService
            .GetSecretValueAsync(request.SecretName);
    }
}

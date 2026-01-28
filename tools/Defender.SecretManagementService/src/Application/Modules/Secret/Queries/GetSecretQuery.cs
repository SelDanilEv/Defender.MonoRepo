using Defender.Common.Entities.Secrets;
using Defender.Common.Errors;
using Defender.Common.Extension;
using Defender.Common.Interfaces;
using Defender.SecretManagementService.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Defender.SecretManagementService.Application.Modules.Secret.Queries;

public record GetSecretQuery : IRequest<MongoSecret>
{
    public string? SecretName { get; set; }
};

public sealed class GetSecretValueSecretQueryValidator : AbstractValidator<GetSecretQuery>
{
    public GetSecretValueSecretQueryValidator()
    {
        RuleFor(s => s.SecretName)
            .NotNull()
            .NotEmpty()
            .WithMessage(ErrorCode.VL_SCM_EmptySecretName);
    }
}

public sealed class GetSecretValueSecretQueryHandler
    : IRequestHandler<GetSecretQuery, MongoSecret>
{
    private readonly IAccountAccessor _accountAccessor;
    private readonly ISecretManagementService _secretManagementService;

    public GetSecretValueSecretQueryHandler(
        IAccountAccessor accountAccessor,
        ISecretManagementService secretManagementService
        )
    {
        _accountAccessor = accountAccessor;
        _secretManagementService = secretManagementService;
    }

    public async Task<MongoSecret> Handle(
        GetSecretQuery request,
        CancellationToken cancellationToken)
    {
        return await _secretManagementService
            .GetSecretAsync(request.SecretName);
    }
}

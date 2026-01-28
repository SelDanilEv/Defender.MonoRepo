using Defender.Common.DB.Pagination;
using Defender.Common.Interfaces;
using Defender.SecretManagementService.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Defender.SecretManagementService.Application.Modules.Secret.Queries;

public record GetAllSecretKeysQuery : PaginationRequest, IRequest<PagedResult<string>>
{
};

public sealed class GetAllSecretKeysValueSecretQueryValidator : AbstractValidator<GetAllSecretKeysQuery>
{
    public GetAllSecretKeysValueSecretQueryValidator()
    {
    }
}

public sealed class GetAllSecretKeysValueSecretQueryHandler
    : IRequestHandler<GetAllSecretKeysQuery, PagedResult<string>>
{
    private readonly IAccountAccessor _accountAccessor;
    private readonly ISecretManagementService _secretManagementService;

    public GetAllSecretKeysValueSecretQueryHandler(
        IAccountAccessor accountAccessor,
        ISecretManagementService secretManagementService
        )
    {
        _accountAccessor = accountAccessor;
        _secretManagementService = secretManagementService;
    }

    public async Task<PagedResult<string>> Handle(
        GetAllSecretKeysQuery request,
        CancellationToken cancellationToken)
    {
        return await _secretManagementService
            .GetAllSecretKeysAsync(request);
    }
}

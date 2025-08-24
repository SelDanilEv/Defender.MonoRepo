using Defender.Common.DB.Pagination;
using Defender.JobSchedulerService.Application.Common.Interfaces.Services;
using Defender.JobSchedulerService.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Defender.JobSchedulerService.Application.Modules.Jobs.Queries;

public record GetJobsQuery : PaginationRequest, IRequest<PagedResult<ScheduledJob>>
{
    public string? Name { get; set; } = string.Empty;
};

public sealed class GetJobsQueryValidator : AbstractValidator<GetJobsQuery>
{
    public GetJobsQueryValidator()
    {
    }
}

public sealed class GetJobsQueryHandler(
        IJobManagementService accountManagementService)
    : IRequestHandler<GetJobsQuery, PagedResult<ScheduledJob>>
{
    public async Task<PagedResult<ScheduledJob>> Handle(
        GetJobsQuery request,
        CancellationToken cancellationToken)
    {
        return await accountManagementService.GetJobsAsync(request, request.Name);
    }
}

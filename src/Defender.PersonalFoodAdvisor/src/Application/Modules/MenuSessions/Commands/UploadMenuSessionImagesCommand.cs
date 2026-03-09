using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Defender.PersonalFoodAdvisor.Application.Modules.MenuSessions.Commands;

public record UploadMenuSessionImagesCommand : IRequest<IReadOnlyList<string>>
{
    public Guid SessionId { get; init; }
    public Guid UserId { get; init; }
    public IReadOnlyList<(Stream Stream, string ContentType)> Files { get; init; } = [];
}

public sealed class UploadMenuSessionImagesCommandValidator : AbstractValidator<UploadMenuSessionImagesCommand>
{
    public UploadMenuSessionImagesCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Files).NotNull();
        RuleFor(x => x.Files.Count).GreaterThan(0);
    }
}

public sealed class UploadMenuSessionImagesCommandHandler(
    IMenuSessionService menuSessionService,
    IImageUploadService imageUploadService)
    : IRequestHandler<UploadMenuSessionImagesCommand, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(UploadMenuSessionImagesCommand request, CancellationToken cancellationToken)
    {
        var session = await menuSessionService.GetByIdAsync(request.SessionId, request.UserId, cancellationToken);
        if (session == null)
        {
            return [];
        }

        return await imageUploadService.UploadAsync(request.SessionId, request.Files, cancellationToken);
    }
}

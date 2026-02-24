using AutoMapper;
using Defender.Common.Exceptions;
using Defender.NotificationService.Application.Common.Interfaces.Repositories;
using Defender.NotificationService.Application.Common.Interfaces.Services;
using Defender.NotificationService.Application.Models;
using NotificationServiceClass = Defender.NotificationService.Application.Services.NotificationService;
using Defender.NotificationService.Domain.Entities;

namespace Defender.NotificationService.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IMapper> _mapper = new();

    [Fact]
    public async Task SendNotificationAsync_WhenEmailRequested_SendsAndUpdates()
    {
        var request = NotificationRequest.Email("user@test.dev", "subject", "body");
        _notificationRepository
            .Setup(x => x.CreateNotificationAsync(It.IsAny<Notification>()))
            .ReturnsAsync((Notification notification) => notification);
        _emailService
            .Setup(x => x.SendEmailAsync(request))
            .ReturnsAsync("ext-id");
        _notificationRepository
            .Setup(x => x.UpdateNotificationAsync(
                It.IsAny<Guid>(),
                It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<Notification>>()))
            .Returns(Task.CompletedTask);
        _mapper
            .Setup(x => x.Map<NotificationResponse>(It.IsAny<Notification>()))
            .Returns((Notification notification) => new NotificationResponse
            {
                Id = notification.Id,
                Type = notification.Type,
                Recipient = notification.Recipient,
                ExternalNotificationId = notification.ExternalNotificationId,
                Status = notification.Status,
            });
        var sut = new NotificationServiceClass(
            _notificationRepository.Object,
            _emailService.Object,
            _mapper.Object);

        var result = await sut.SendNotificationAsync(request);

        Assert.NotNull(result);
        _emailService.Verify(x => x.SendEmailAsync(request), Times.Once);
        _notificationRepository.Verify(
            x => x.UpdateNotificationAsync(
                It.IsAny<Guid>(),
                It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<Notification>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_WhenEmailServiceThrows_ThrowsServiceExceptionAndUpdates()
    {
        var request = NotificationRequest.Email("user@test.dev", "subject", "body");
        _notificationRepository
            .Setup(x => x.CreateNotificationAsync(It.IsAny<Notification>()))
            .ReturnsAsync((Notification notification) => notification);
        _emailService
            .Setup(x => x.SendEmailAsync(request))
            .ThrowsAsync(new InvalidOperationException("failure"));
        _notificationRepository
            .Setup(x => x.UpdateNotificationAsync(
                It.IsAny<Guid>(),
                It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<Notification>>()))
            .Returns(Task.CompletedTask);
        var sut = new NotificationServiceClass(
            _notificationRepository.Object,
            _emailService.Object,
            _mapper.Object);

        await Assert.ThrowsAsync<ServiceException>(() => sut.SendNotificationAsync(request));
        _notificationRepository.Verify(
            x => x.UpdateNotificationAsync(
                It.IsAny<Guid>(),
                It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<Notification>>()),
            Times.Once);
    }
}

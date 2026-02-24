using Defender.NotificationService.Application.Models;
using Defender.NotificationService.Domain.Entities;
using Defender.NotificationService.Domain.Enum;

namespace Defender.NotificationService.Tests.Domain;

public class NotificationTests
{
    [Fact]
    public void InitEmailNotification_WhenCalled_SetsEmailTypeAndPreparingStatus()
    {
        var notification = Notification.InitEmailNotificaton();

        Assert.Equal(NotificationType.Email, notification.Type);
        Assert.Equal(NotificationStatus.PreparingToSend, notification.Status);
    }

    [Fact]
    public void FillNotificationData_WhenCalled_FillsFieldsAndReturnsSameInstance()
    {
        var notification = Notification.InitSMSNotificaton();

        var result = notification.FillNotificatonData("user@mail.com", "subject", "body");

        Assert.Same(notification, result);
        Assert.Equal("user@mail.com", notification.Recipient);
        Assert.Equal("subject", notification.Header);
        Assert.Equal("body", notification.Message);
    }

    [Fact]
    public void VerificationCode_WhenCalled_CreatesEmailRequestWithFormattedBody()
    {
        var request = NotificationRequest.VerificationCode("recipient@mail.com", 123456);

        Assert.Equal(NotificationType.Email, request.Type);
        Assert.Equal("recipient@mail.com", request.Recipient);
        Assert.Contains("123456", request.Body);
        Assert.False(string.IsNullOrWhiteSpace(request.Subject));
    }
}

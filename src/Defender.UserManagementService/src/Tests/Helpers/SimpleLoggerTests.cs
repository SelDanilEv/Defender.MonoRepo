using Defender.UserManagementService.Application.Helpers;

namespace Defender.UserManagementService.Tests.Helpers;

public class SimpleLoggerTests
{
    [Fact]
    public void Log_WhenExceptionContainsInnerException_DoesNotThrow()
    {
        var exception = new InvalidOperationException(
            "outer",
            new ArgumentException("inner"));

        var action = () => SimpleLogger.Log(exception, "custom message");

        var result = Record.Exception(action);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(SimpleLogger.LogLevel.Debug)]
    [InlineData(SimpleLogger.LogLevel.Info)]
    [InlineData(SimpleLogger.LogLevel.Warning)]
    public void Log_WhenCalledWithEachLogLevel_DoesNotThrow(SimpleLogger.LogLevel level)
    {
        var action = () => SimpleLogger.Log("test message", level);

        var result = Record.Exception(action);
        Assert.Null(result);
    }
}

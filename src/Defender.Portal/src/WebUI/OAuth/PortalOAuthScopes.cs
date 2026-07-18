namespace Defender.Portal.WebUI.OAuth;

public static class PortalOAuthScopes
{
    public const string Read = "mcp:portal:read";
    public const string CalendarWrite = "mcp:calendar:write";
    public const string CalendarDelete = "mcp:calendar:delete";

    public static readonly string[] All = [Read, CalendarWrite, CalendarDelete];
}

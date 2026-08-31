namespace Defender.Portal.WebUI.OAuth;

public static class PortalOAuthScopes
{
    public const string Read = "mcp:portal:read";
    public const string CalendarWrite = "mcp:calendar:write";
    public const string CalendarDelete = "mcp:calendar:delete";
    public const string BudgetWrite = "mcp:budget:write";
    public const string BudgetDelete = "mcp:budget:delete";

    public static readonly string[] All = [Read, CalendarWrite, CalendarDelete, BudgetWrite, BudgetDelete];
}

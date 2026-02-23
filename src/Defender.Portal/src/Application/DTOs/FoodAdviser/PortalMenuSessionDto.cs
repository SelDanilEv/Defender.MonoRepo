namespace Defender.Portal.Application.DTOs.FoodAdviser;

public class PortalMenuSessionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> ImageRefs { get; set; } = [];
    public List<string> ParsedItems { get; set; } = [];
    public List<string> ConfirmedItems { get; set; } = [];
    public List<string> RankedItems { get; set; } = [];
    public bool TrySomethingNew { get; set; }
}

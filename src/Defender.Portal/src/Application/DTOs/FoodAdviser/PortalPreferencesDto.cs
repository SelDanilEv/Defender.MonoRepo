namespace Defender.Portal.Application.DTOs.FoodAdviser;

public class PortalPreferencesDto
{
    public Guid UserId { get; set; }
    public List<string> Likes { get; set; } = [];
    public List<string> Dislikes { get; set; } = [];
}

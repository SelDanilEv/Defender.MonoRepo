namespace Defender.Portal.Application.DTOs.FoodAdvisor;

public class PortalPreferencesDto
{
    public Guid UserId { get; set; }
    public List<string> Likes { get; set; } = [];
    public List<string> Dislikes { get; set; } = [];
}

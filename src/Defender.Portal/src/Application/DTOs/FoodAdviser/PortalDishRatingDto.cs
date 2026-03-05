namespace Defender.Portal.Application.DTOs.FoodAdviser;

public class PortalDishRatingDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public Guid? SessionId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

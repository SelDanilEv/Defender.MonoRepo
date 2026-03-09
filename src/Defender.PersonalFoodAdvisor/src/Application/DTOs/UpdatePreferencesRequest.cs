namespace Defender.PersonalFoodAdvisor.Application.DTOs;

public record UpdatePreferencesRequest(IReadOnlyList<string> Likes, IReadOnlyList<string> Dislikes);

namespace Defender.PersonalFoodAdvisor.Application.DTOs;

public record SubmitRatingRequest(string DishName, int Rating, Guid? SessionId = null);

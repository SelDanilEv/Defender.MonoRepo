namespace Defender.PersonalFoodAdviser.Application.DTOs;

public record SubmitRatingRequest(string DishName, int Rating, Guid? SessionId = null);

using Defender.UserManagementService.Application.Modules.Users.Queries;
using FluentValidation.TestHelper;

namespace Defender.UserManagementService.Tests.Validators;

public class GetUserByIdQueryValidatorTests
{
    private readonly GetUserByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenUserIdEmpty_HasValidationError()
    {
        var query = new GetUserByIdQuery { UserId = Guid.Empty };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.UserId);
    }

    [Fact]
    public void Validate_WhenUserIdSet_HasNoErrors()
    {
        var query = new GetUserByIdQuery { UserId = Guid.NewGuid() };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

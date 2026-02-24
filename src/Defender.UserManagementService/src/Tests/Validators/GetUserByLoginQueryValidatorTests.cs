using Defender.UserManagementService.Application.Modules.Users.Queries;
using FluentValidation.TestHelper;

namespace Defender.UserManagementService.Tests.Validators;

public class GetUserByLoginQueryValidatorTests
{
    private readonly GetUserByLoginQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenLoginEmpty_HasValidationError()
    {
        var query = new GetUserByLoginQuery { Login = "" };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.Login);
    }

    [Fact]
    public void Validate_WhenLoginSet_HasNoErrors()
    {
        var query = new GetUserByLoginQuery { Login = "user@b.com" };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

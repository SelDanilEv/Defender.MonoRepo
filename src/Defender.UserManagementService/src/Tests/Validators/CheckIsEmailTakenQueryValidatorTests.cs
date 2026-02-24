using Defender.UserManagementService.Application.Modules.Users.Queries;
using FluentValidation.TestHelper;

namespace Defender.UserManagementService.Tests.Validators;

public class CheckIsEmailTakenQueryValidatorTests
{
    private readonly IsEmailTakenQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenEmailEmpty_HasValidationError()
    {
        var query = new CheckIsEmailTakenQuery { Email = "" };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.Email);
    }

    [Fact]
    public void Validate_WhenEmailSet_HasNoErrors()
    {
        var query = new CheckIsEmailTakenQuery { Email = "a@b.com" };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

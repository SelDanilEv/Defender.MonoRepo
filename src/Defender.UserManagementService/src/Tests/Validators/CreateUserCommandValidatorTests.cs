using Defender.UserManagementService.Application.Modules.Users.Commands;
using FluentValidation.TestHelper;

namespace Defender.UserManagementService.Tests.Validators;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenEmailEmpty_HasValidationError()
    {
        var command = new CreateUserCommand { Email = "", PhoneNumber = null, Nickname = "ab" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_WhenEmailInvalid_HasValidationError()
    {
        var command = new CreateUserCommand { Email = "notanemail", PhoneNumber = null, Nickname = "ab" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_WhenNicknameTooShort_HasValidationError()
    {
        var command = new CreateUserCommand { Email = "a@b.com", PhoneNumber = null, Nickname = "a" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Nickname);
    }

    [Fact]
    public void Validate_WhenAllValid_HasNoErrors()
    {
        var command = new CreateUserCommand { Email = "u@b.com", PhoneNumber = null, Nickname = "validnick" };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

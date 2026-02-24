using Defender.UserManagementService.Application.Modules.Users.Commands;
using FluentValidation.TestHelper;

namespace Defender.UserManagementService.Tests.Validators;

public class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenIdEmpty_HasValidationError()
    {
        var command = new UpdateUserCommand { Id = Guid.Empty, Nickname = "ab" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Validate_WhenNoFieldSet_HasValidationError()
    {
        var command = new UpdateUserCommand { Id = Guid.NewGuid() };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c);
    }

    [Fact]
    public void Validate_WhenEmailInvalid_HasValidationError()
    {
        var command = new UpdateUserCommand { Id = Guid.NewGuid(), Email = "bad" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_WhenNicknameTooShort_HasValidationError()
    {
        var command = new UpdateUserCommand { Id = Guid.NewGuid(), Nickname = "a" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Nickname);
    }

    [Fact]
    public void Validate_WhenIdAndOneFieldValid_HasNoErrors()
    {
        var command = new UpdateUserCommand { Id = Guid.NewGuid(), Nickname = "validnick" };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

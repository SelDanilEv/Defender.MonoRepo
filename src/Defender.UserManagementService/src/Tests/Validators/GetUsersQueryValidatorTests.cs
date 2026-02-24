using Defender.UserManagementService.Application.Modules.Users.Queries;
using FluentValidation.TestHelper;

namespace Defender.UserManagementService.Tests.Validators;

public class GetUsersQueryValidatorTests
{
    private readonly GetUsersQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryProvided_HasNoErrors()
    {
        var query = new GetUsersQuery { Page = 1, PageSize = 10 };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

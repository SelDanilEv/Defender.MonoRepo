using Defender.Common.Configuration.Options;
using Defender.Common.Consts;
using Defender.Common.DB.Model;
using Defender.Common.DB.Pagination;
using Defender.Common.Entities;
using Defender.Common.Entities.AccountInfo;
using Defender.Common.Entities.Secrets;
using Defender.Common.Enums;
using Defender.Common.Helpers;
using MongoDB.Driver;

namespace Defender.Common.Tests;

public class HelpersAndDataModelTests
{
    [Fact]
    public void GetHighestRole_WhenSuperAdminExists_ReturnsSuperAdmin()
    {
        var roles = new List<string> { Roles.User, Roles.SuperAdmin };

        var result = RolesHelper.GetHighestRole(roles);

        Assert.Equal(Role.SuperAdmin, result);
    }

    [Fact]
    public void GetRolesList_WhenAdminRequested_ReturnsAdminAndLowerRoles()
    {
        var roles = RolesHelper.GetRolesList(Role.Admin);

        Assert.Equal(new[] { Roles.Admin, Roles.User, Roles.Guest }, roles);
    }

    [Fact]
    public void GuardNotNull_WhenValueIsNull_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => Guard.NotNull<string>(null!, "value"));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void MapEnum_WhenNameExists_ReturnsMappedEnum()
    {
        var result = MappingHelper.MapEnum<Role>("Admin", Role.Guest);

        Assert.Equal(Role.Admin, result);
    }

    [Fact]
    public void MapEnum_WhenNameMissing_ReturnsDefaultEnum()
    {
        var result = MappingHelper.MapEnum<Role>("Nope", Role.User);

        Assert.Equal(Role.User, result);
    }

    [Fact]
    public void BaseAccountInfo_WhenRolesUpdated_ComputedPropertiesReturnExpectedValues()
    {
        var account = new BaseAccountInfo
        {
            Id = Guid.NewGuid(),
            Roles = new List<string> { Roles.Admin }
        };

        Assert.True(account.IsAdmin);
        Assert.False(account.IsSuperAdmin);
        Assert.True(account.HasRole(Roles.Admin));
        Assert.Equal(Role.Admin, account.GetHighestRole());
    }

    [Fact]
    public void MongoDbOptions_WhenBuiltFromBaseOptions_CopiesExpectedFields()
    {
        var baseOptions = new MongoDbOptions
        {
            Environment = "local",
            ConnectionString = "mongodb://localhost"
        };

        var options = new MongoDbOptions("wallet", baseOptions);

        Assert.Equal("wallet", options.AppName);
        Assert.Equal("local_wallet", options.GetDatabaseName());
    }

    [Fact]
    public void SharedMongoDbOptions_WhenBuiltFromMongoOptions_UsesSharedAppName()
    {
        var baseOptions = new MongoDbOptions
        {
            Environment = "dev",
            ConnectionString = "mongodb://localhost"
        };

        var options = new SharedMongoDbOptions(baseOptions);

        Assert.Equal(ConstValues.SharedDatabaseName, options.AppName);
        Assert.Equal("dev_Shared", options.GetDatabaseName());
    }

    [Fact]
    public void PaginationSettingsFromRequest_WhenInvalidValuesProvided_UsesDefaults()
    {
        var request = new PaginationRequest { Page = -5, PageSize = 0 };

        var settings = PaginationSettings<TestModel>.FromPaginationRequest(request);

        Assert.Equal(ConstValues.DefaultPaginationStartPage, settings.Page);
        Assert.Equal(ConstValues.DefaultPaginationPageSize, settings.PageSize);
    }

    [Fact]
    public void PaginationSettingsSetupFindOptions_WhenRequestProvided_UsesRequestFilterAndSort()
    {
        var request = FindModelRequest<TestModel>
            .Init(x => x.Name, "John")
            .Sort(x => x.Name, SortType.Desc);

        var settings = PaginationSettings<TestModel>.WithoutPagination().SetupFindOptions(request);

        Assert.NotNull(settings.Filter);
        Assert.NotNull(settings.Sort);
        Assert.NotEqual(FilterDefinition<TestModel>.Empty, settings.Filter);
        Assert.NotNull(settings.Sort.ToString());
    }

    [Fact]
    public void PagedResultFromPagedResult_WhenMapperProvided_MapsItemsAndMeta()
    {
        var input = new PagedResult<int>
        {
            TotalItemsCount = 10,
            CurrentPage = 2,
            PageSize = 5,
            Items = new List<int> { 1, 2, 3 }
        };

        var result = PagedResult<int>.FromPagedResult(input, x => x * 2);

        Assert.Equal(10, result.TotalItemsCount);
        Assert.Equal(2, result.CurrentPage);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(new[] { 2, 4, 6 }, result.Items);
    }

    [Fact]
    public void PaginationRequestCreateWithoutPagination_WhenCalled_ReturnsDefaultRequest()
    {
        var request = PaginationRequest.CreateWithoutPagination;

        Assert.Equal(ConstValues.DefaultPaginationStartPage, request.Page);
        Assert.Equal(ConstValues.DefaultPaginationPageSize, request.PageSize);
    }

    [Fact]
    public void FindModelRequest_WhenAndOrSortApplied_BuildsDefinitions()
    {
        var request = FindModelRequest<TestModel>.Init(x => x.Name, "A")
            .And(x => x.Age, 18, FilterType.Gte)
            .Or(x => x.Name, "B")
            .Sort(x => x.CreatedAt, SortType.Desc);

        var filter = request.BuildFilterDefinition();
        var sort = request.BuildSortDefinition();

        Assert.NotNull(filter);
        Assert.NotNull(sort);
        Assert.NotEqual(FilterDefinition<TestModel>.Empty, filter);
        Assert.NotNull(sort.ToString());
    }

    [Fact]
    public void UpdateModelRequest_WhenSetAndAddToSetApplied_BuildsUpdateDocument()
    {
        var request = UpdateModelRequest<TestModel>.Init(Guid.NewGuid())
            .Set(x => x.Name, "Updated")
            .SetIfNotNull(x => x.Optional, "Value")
            .SetIfNotNull(x => x.Optional, string.Empty)
            .AddToSet(x => x.Tags, "new-tag");

        var updateDocument = request.BuildUpdateDefinition();

        Assert.NotEqual(Guid.Empty, request.ModelId);
        Assert.NotNull(updateDocument);
        Assert.Contains("Update", updateDocument.ToString());
    }

    [Fact]
    public void MongoSecretFromSecret_WhenCalled_MapsNameAndValue()
    {
        var secret = MongoSecret.FromSecret(Secret.JwtSecret, "jwt");

        Assert.Equal(nameof(Secret.JwtSecret), secret.SecretName);
        Assert.Equal("jwt", secret.Value);
    }

    [Fact]
    public void MongoSecretFromSecretName_WhenCalled_MapsNameAndValue()
    {
        var secret = MongoSecret.FromSecretName("custom", "value");

        Assert.Equal("custom", secret.SecretName);
        Assert.Equal("value", secret.Value);
    }

    private sealed class TestModel : IBaseModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Optional { get; set; }
        public List<string> Tags { get; set; } = [];
    }
}

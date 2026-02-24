---
name: generate-unit-tests
description: Generate xUnit + Moq unit tests for a Defender microservice targeting 80%+ code coverage across all layers (Application, Domain, Infrastructure, WebApi). Use when the user asks to write tests, add test coverage, generate unit tests, or improve coverage for a service.
---

# Generate Unit Tests

## Goal

Write unit tests for a Defender service across **all layers** to reach **80%+ line coverage** on critical code.

## Environment constraints

- **Use the system-installed `dotnet` CLI.** Never install a local .NET SDK, never run `dotnet new tool-manifest`, and never create a `.dotnet` folder or any other tooling directory in the repo.
- Do not create any folders outside `src/Defender.{Service}/src/Tests/` (the test project directory).
- Do not modify global tooling files (`global.json`, `.config/dotnet-tools.json`, etc.) unless the user explicitly asks.

## Workflow

### 1. Identify the target service

Resolve the service name to `src/Defender.{Service}`. If the user didn't specify one, ask.

### 2. Read the service code

Scan all layers under `src/Defender.{Service}/src/`:

| Priority | Layer | Path pattern | What to test |
|----------|-------|-------------|--------------|
| **P0** | Application | `Application/Services/*.cs` | Core business logic |
| **P0** | Application | `Application/Modules/**/Commands/*.cs` | Command handlers + validators |
| **P0** | Application | `Application/Modules/**/Queries/*.cs` | Query handlers + validators |
| **P1** | Domain | `Domain/Entities/*.cs` | Entities with logic (factory methods, computed props, state transitions) |
| **P1** | Domain | `Domain/Helpers/*.cs` | Static helper/calculation methods |
| **P1** | Infrastructure | `Infrastructure/Clients/**/*.cs` | External API wrappers (error handling, request building) |
| **P2** | Application | `Application/Models/*.cs` | Request/DTO mapping methods |
| **P2** | Infrastructure | `Infrastructure/Mappings/*.cs` | AutoMapper profiles |
| **P2** | WebApi | `WebApi/Controllers/**/*.cs` | Controller action methods |
| P3 | Domain | `Domain/Enums/*.cs`, `Domain/Consts/*.cs` | Skip unless they contain methods |
| P3 | Infrastructure | `Infrastructure/Repositories/**/*.cs` | Only custom query-building logic |
| P3 | WebApi | `WebApi/ConfigureServices.cs`, `WebApi/Program.cs` | Skip — tested via integration tests |

Read every interface in `Application/Common/Interfaces/` to understand the mocking surface.

### 3. Ensure the Tests project is ready

Tests live in `src/Defender.{Service}/src/Tests/`.

Required `.csproj` shape (versions come from `src/Directory.Packages.props`):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>Defender.{Service}.Tests</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Moq" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\Defender.Common\src\Defender.Common\Defender.Common.csproj" />
    <ProjectReference Include="..\Application\Application.csproj" />
    <ProjectReference Include="..\Domain\Domain.csproj" />
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Moq" />
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

If `Moq` or the `Defender.Common` project reference is missing, add them. Never add package versions — they come from central package management.

### 4. Write tests

#### File layout

```
Tests/
├── Services/
│   └── {ServiceClass}Tests.cs
├── Handlers/
│   └── {Handler}Tests.cs
├── Validators/
│   └── {Validator}Tests.cs
├── Models/
│   └── {Model}Tests.cs
├── Domain/
│   └── {Entity}Tests.cs
├── Infrastructure/
│   ├── Clients/
│   │   └── {Wrapper}Tests.cs
│   └── Mappings/
│       └── MappingProfileTests.cs
└── Controllers/
    └── {Controller}Tests.cs
```

#### Naming convention

```
Method_WhenCondition_ExpectedResult
```

---

#### Application layer patterns

**Service tests** — mock every constructor dependency, use `_sut`:

```csharp
public class SomeServiceTests
{
    private readonly Mock<ISomeRepository> _repository;
    private readonly SomeService _sut;

    public SomeServiceTests()
    {
        _repository = new Mock<ISomeRepository>();
        _sut = new SomeService(_repository.Object);
    }

    [Fact]
    public async Task GetById_WhenCalled_ReturnsMappedResult()
    {
        var expected = new Entity { Id = Guid.NewGuid() };
        _repository.Setup(r => r.GetByIdAsync(expected.Id)).ReturnsAsync(expected);

        var result = await _sut.GetByIdAsync(expected.Id);

        Assert.Same(expected, result);
        _repository.Verify(r => r.GetByIdAsync(expected.Id), Times.Once);
    }
}
```

**Handler tests** — each MediatR handler gets its own file. Mock `ICurrentAccountAccessor`, `IAuthorizationCheckingService` as needed.

**Validator tests** — use `FluentValidation.TestHelper` (ships inside the main FluentValidation package):

```csharp
using FluentValidation.TestHelper;

public class CreateCommandValidatorTests
{
    private readonly CreateCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenFieldEmpty_HasError()
    {
        var cmd = new CreateCommand { Field = "" };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Field);
    }
}
```

---

#### Domain layer patterns

Test entities that have **factory methods, computed properties, state transitions, or helper logic**. Skip pure data containers.

```csharp
public class TransactionTests
{
    [Fact]
    public void CreateRecharge_WhenValid_SetsCorrectFields()
    {
        var tx = Transaction.CreateRecharge(userId, 100, Currency.USD);

        Assert.Equal(userId, tx.UserId);
        Assert.Equal(100, tx.Amount);
        Assert.NotNull(tx.TransactionId);
    }

    [Fact]
    public void IsExpired_WhenPastExpiration_ReturnsTrue()
    {
        var code = new AccessCode { CreatedDate = DateTime.UtcNow.AddHours(-2), LifeSpanInMinutes = 30 };
        Assert.True(code.IsExpired);
    }
}
```

Common domain patterns to cover:
- **Factory methods**: `Create(...)`, `CreateRecharge(...)`, `FromPosition(...)` — verify all fields are set
- **Computed properties**: `IsExpired`, `IsActive`, `TotalAmount`, `IncomePercentage` — test boundary values
- **State transitions**: `UpdateNextStartDate()`, `ScheduleNextRun()` — verify before/after state
- **Static helpers**: `LotteryHelpers.CalculatePrizeAmount(...)` — test with various inputs and edge cases

---

#### Infrastructure layer patterns

**Client/Wrapper tests** — wrappers inherit from `BaseSwaggerWrapper` or `BaseInternalSwaggerWrapper`. Mock the underlying service client and mapper:

```csharp
public class SomeWrapperTests
{
    private readonly Mock<ISomeServiceClient> _client;
    private readonly Mock<IMapper> _mapper;
    private readonly SomeWrapper _sut;

    public SomeWrapperTests()
    {
        _client = new Mock<ISomeServiceClient>();
        _mapper = new Mock<IMapper>();
        _sut = new SomeWrapper(_client.Object, _mapper.Object);
    }
}
```

**AutoMapper profile tests** — validate configuration and spot-check mappings:

```csharp
public class MappingProfileTests
{
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Config_WhenCreated_IsValid()
    {
        Assert.NotNull(_mapper);
    }

    [Fact]
    public void Map_EntityToDto_MapsCorrectly()
    {
        var entity = new SomeEntity { Id = Guid.NewGuid(), Name = "test" };
        var dto = _mapper.Map<SomeDto>(entity);
        Assert.Equal(entity.Id, dto.Id);
    }
}
```

**Repository tests** — repositories inherit from `BaseMongoRepository<T>` and are tightly coupled to MongoDB. Only test custom query-building logic if present. Otherwise skip (they need integration tests with a real/in-memory MongoDB).

---

#### WebApi layer patterns

Controllers are thin (MediatR dispatch + AutoMapper). Mock `ISender` (MediatR) and `IMapper`:

```csharp
public class SomeControllerTests
{
    private readonly Mock<ISender> _mediator;
    private readonly Mock<IMapper> _mapper;

    [Fact]
    public async Task GetById_WhenCalled_ReturnsMappedResult()
    {
        // Test that controller dispatches the right command/query
        // and maps the response correctly
    }
}
```

If controllers only call `ProcessApiCallAsync` from `ApiControllerBase`, a single test per action verifying the MediatR request type is sufficient.

---

#### What to cover per class

| Class kind | Must cover |
|------------|-----------|
| Service | Every public method: happy path, each error/exception branch, conditional logic |
| Command handler | Role-based branching, access-code validation, delegation to service |
| Query handler | Delegation to service (one test per handler) |
| Validator | One test per rule + one "all valid" test |
| Model/DTO | Mapping methods, mutation methods |
| Domain entity | Factory methods, computed properties, state transitions, edge cases |
| Domain helper | Every static method with representative inputs |
| Infra wrapper | Method delegation, error handling, mapping calls |
| Mapper profile | Config validity + spot-check key mappings |
| Controller | One test per action verifying correct MediatR dispatch |

### 5. Build and run

```powershell
dotnet test src/Defender.{Service}/src/Tests/Defender.{Service}.Tests.csproj -c Debug --verbosity minimal
```

Fix all compilation errors. All tests must pass (exit code 0).

### 6. Measure coverage

The user runs `pwsh scripts/coverage-dashboard.ps1` manually. Coverage output lives under `artifacts/coverage/`. When iterating on uncovered code, read `artifacts/coverage/dashboard/Summary.txt` or grep the Cobertura XML per assembly:

```powershell
Select-String 'package name="Defender.{Service}' artifacts/coverage/raw/Defender.{Service}/*.cobertura.xml
```

### 7. Iterate if below 80%

If the line-rate is below 0.80 for any assembly:
1. Identify uncovered classes/methods from the Cobertura XML or Summary.txt.
2. Add tests for the missing branches.
3. Re-run from step 5.

### 8. Report results

When done, summarize:
- Total tests added
- Per-assembly line coverage % and branch coverage %
- Any classes intentionally skipped with reason

## Common pitfalls

- **Namespace/type conflict**: If the service class name matches the namespace, qualify it: `Application.Services.{ClassName}`.
- **Moq generic delegates**: Use `It.IsAny<Func<Task<T>>>()` (triple closing bracket) and `.Returns<Guid, Func<Task<T>>, bool, ErrorCode>((_, f, _, _) => f!.Invoke())` for `ExecuteWithAuthCheckAsync`.
- **Null warnings**: Use `null!` for non-nullable parameters in negative test cases.
- **Central package management**: Never put version attributes on `<PackageReference>` — versions live in `src/Directory.Packages.props`.
- **AutoMapper in tests**: Create a real `MapperConfiguration` with the profile under test — don't mock `IMapper` for mapping-correctness tests.
- **BaseMongoRepository**: Don't try to unit-test repository CRUD — it's coupled to MongoDB driver internals. Only test custom filter/query-building if any.

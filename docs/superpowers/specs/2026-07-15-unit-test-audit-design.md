# Unit-test audit and cleanup

## Goal

Remove unit tests that do not protect observable application behavior. Keep every test that verifies a contract, business rule, validation result, mapping, API result, integration boundary, or meaningful model invariant.

## Removal criteria

Remove a test file only when its tests are coverage-oriented reflection smoke tests with all of these traits:

- Enumerate assemblies, types, methods, or validators dynamically rather than exercising named behavior.
- Generate arbitrary objects or mocks through reflection.
- Suppress invocation failures with an empty `catch` or otherwise ignore them.
- Assert only construction, non-nullness, non-emptiness, or absence of a crash.
- Have no stable, business-relevant input/output contract.

Do not remove a test merely because it is small, tests a constructor or DTO, or uses mocks. Such tests stay when they lock down a meaningful contract.

## Scope

1. Inspect all candidate `*CoverageSmokeTests` and `*SurfaceCoverageTests` files.
2. Delete only files meeting every removal criterion.
3. Keep behavior-specific coverage tests even when their filenames include `Coverage`.
4. Do not rewrite production code or replace removed tests in this cleanup.

## Validation

Run each affected test project after deletion, then run `dotnet test Defender.Core.sln`. Report deleted files, retained borderline tests, and test results.

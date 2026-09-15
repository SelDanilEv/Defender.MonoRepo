import Role from "src/consts/Role";
import { canAccessMyGarage } from "src/routes/myGarageAccess";

describe("My Garage role access", () => {
  test.each([
    [Role.User, true],
    [Role.Admin, true],
    [Role.SuperAdmin, true],
    [Role.Guest, false],
    [Role.NoRole, false],
  ])("canAccessMyGarage_WhenRoleIs%s_Returns%s", (role, expected) => {
    expect(canAccessMyGarage(role)).toBe(expected);
  });
});

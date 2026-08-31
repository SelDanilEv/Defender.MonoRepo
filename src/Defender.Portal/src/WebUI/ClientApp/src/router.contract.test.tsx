import type { RouteObject } from "react-router";
import routes from "src/router";
import Role from "src/consts/Role";
import UserService from "src/services/UserService";

const collectPaths = (items: RouteObject[], parent = ""): string[] =>
  items.flatMap((route) => {
    const own = route.path ? `${parent}/${route.path}`.replace(/\/+/g, "/") : parent || "/";
    return [own, ...collectPaths(route.children ?? [], own)];
  });

describe("critical route contract", () => {
  const paths = collectPaths(routes);

  test.each([
    "/welcome/login",
    "/telegram",
    "/telegram/link",
    "/banking",
    "/games/lottery/tickets",
    "/budget-tracker/positions",
    "/budget-tracker/reviews",
    "/budget-tracker/regular-expenses/expenses",
    "/budget-tracker/regular-expenses/reviews",
    "/budget-tracker/regular-expenses/diagram",
    "/admin/users",
  ])("keeps journey route %s", (path) => expect(paths).toContain(path));

  test("admin role precedence cannot be gained from ordinary user role", () => {
    expect(UserService.GetHighestRole([Role.User])).toBe(Role.User);
    expect(UserService.GetHighestRole([Role.User, Role.Admin])).toBe(Role.Admin);
    expect(UserService.GetHighestRole([Role.Admin, Role.SuperAdmin])).toBe(Role.SuperAdmin);
  });
});

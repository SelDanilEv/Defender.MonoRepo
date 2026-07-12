import type { RouteObject } from "react-router-dom";
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
    "/banking",
    "/games/lottery/tickets",
    "/budget-tracker/positions",
    "/budget-tracker/reviews",
    "/admin/users",
  ])("keeps journey route %s", (path) => expect(paths).toContain(path));

  test("admin role precedence cannot be gained from ordinary user role", () => {
    expect(UserService.GetHighestRole([Role.User])).toBe(Role.User);
    expect(UserService.GetHighestRole([Role.User, Role.Admin])).toBe(Role.Admin);
    expect(UserService.GetHighestRole([Role.Admin, Role.SuperAdmin])).toBe(Role.SuperAdmin);
  });
});

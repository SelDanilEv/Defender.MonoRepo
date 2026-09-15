import routes from "src/router";
import i18n from "src/localization/i18n";
import enMyGarage from "src/localization/en/myGarage.json";
import ruMyGarage from "src/localization/ru/myGarage.json";
import enSidebar from "src/localization/en/sidebar_menu.json";
import ruSidebar from "src/localization/ru/sidebar_menu.json";
import SidebarLayout from "src/layouts/SidebarLayout";
import Role from "src/consts/Role";
import { canAccessMyGarage } from "src/routes/myGarageAccess";
import { MY_GARAGE_PLACEHOLDER_MODULE, MyGarageRouteGuard } from "src/router";
import { MemoryRouter, Route, Routes } from "react-router";
import { render, screen } from "@testing-library/react";
import { Provider } from "react-redux";
import store from "src/state/store";
import { SidebarProvider } from "src/contexts/SidebarContext";
import { RoleBasedMenu } from "src/layouts/SidebarLayout/Sidebar/SidebarMenu/RoleBasedMenu";
import ThemeProvider from "src/theme/ThemeProvider";

type RouteNode = { path?: string; element?: { type?: unknown }; children?: RouteNode[] };

const carServiceErrorCodes = [
  "CAR_HISTORY_PAGINATION_INVALID",
  "CAR_VEHICLE_NOT_FOUND",
  "CAR_VEHICLE_ARCHIVED",
  "CAR_MAINTENANCE_NOT_FOUND",
  "CAR_MAINTENANCE_REFERENCED",
  "CAR_VEHICLE_DISPLAY_NAME_REQUIRED",
  "CAR_VEHICLE_FIELD_TOO_LONG",
  "CAR_VEHICLE_YEAR_INVALID",
  "CAR_VEHICLE_VIN_INVALID",
  "CAR_MAINTENANCE_NAME_REQUIRED",
  "CAR_MAINTENANCE_INTERVAL_REQUIRED",
  "CAR_MAINTENANCE_INTERVAL_INVALID",
  "CAR_MAINTENANCE_BASELINE_INVALID",
  "CAR_MAINTENANCE_BASELINE_LOCKED",
  "CAR_HISTORY_DATE_INVALID",
  "CAR_HISTORY_DATE_FUTURE",
  "CAR_HISTORY_ODOMETER_INVALID",
  "CAR_HISTORY_TYPE_INVALID",
  "CAR_HISTORY_TITLE_REQUIRED",
  "CAR_HISTORY_LINK_INVALID",
  "CAR_HISTORY_COST_PAIR_INVALID",
  "CAR_HISTORY_COST_INVALID",
  "CAR_INSURANCE_PROVIDER_REQUIRED",
  "CAR_INSURANCE_DATE_RANGE_INVALID",
  "CAR_INSURANCE_FIELD_TOO_LONG",
  "CAR_CURRENCY_INVALID",
  "CAR_ODOMETER_SEQUENCE_INVALID",
  "CAR_HISTORY_NOT_FOUND",
  "CAR_INSURANCE_NOT_FOUND",
  "CAR_CONCURRENCY_CONFLICT",
  "CAR_DATABASE_UNAVAILABLE",
  "CAR_UNHANDLED_ERROR",
] as const;

const flattenPaths = (nodes: RouteNode[], prefix = ""): string[] => nodes.flatMap((node) => {
  const path = node.path ?? "";
  const fullPath = path.startsWith(":") || path === "*" || path === "" ? `${prefix}${path}` : `${prefix}/${path}`;
  return [fullPath || "/", ...(node.children ? flattenPaths(node.children, fullPath) : [])];
});

describe("My Garage routes and resources", () => {
  test("routeTree_WhenLoaded_ContainsTheAuthenticatedGaragePaths", () => {
    const paths = flattenPaths(routes as RouteNode[]);

    expect(paths).toEqual(expect.arrayContaining([
      "/my-garage",
      "/my-garage/vehicles",
      "/my-garage/vehicles/:vehicleId",
      "/my-garage/vehicles/:vehicleId/maintenance",
      "/my-garage/vehicles/:vehicleId/history",
      "/my-garage/vehicles/:vehicleId/insurance",
    ]));
  });

  test("resources_WhenLoaded_ExposeGarageAndErrorCopyInBothLanguages", () => {
    expect(enMyGarage.title).toBe("My Garage");
    expect(ruMyGarage.title).toBeTruthy();
    expect(enMyGarage.statuses.Overdue).toBeTruthy();
    expect(ruMyGarage.statuses.Overdue).toBeTruthy();
    for (const code of carServiceErrorCodes) {
      expect(enMyGarage.errors[code]).toBeTruthy();
      expect(ruMyGarage.errors[code]).toBeTruthy();
    }
    expect(enSidebar.header_my_garage).toBeTruthy();
    expect(ruSidebar.header_my_garage).toBeTruthy();
    expect(enSidebar.page_my_garage).toBeTruthy();
    expect(ruSidebar.page_my_garage).toBeTruthy();
  });

  test("resources_WhenRegistered_ExposeTheGarageNamespaceInBothLanguages", () => {
    expect(i18n.hasResourceBundle("en", "myGarage")).toBe(true);
    expect(i18n.hasResourceBundle("ru", "myGarage")).toBe(true);
    expect(i18n.t("myGarage:title", { lng: "en" })).toBe(enMyGarage.title);
    expect(i18n.t("myGarage:title", { lng: "ru" })).toBe(ruMyGarage.title);
  });

  test("routeTree_WhenLoaded_UsesSidebarAuthWrapperAndRoleGuard", () => {
    const garageRoute = (routes as RouteNode[]).find((route) => route.path === "my-garage");
    expect(garageRoute?.element?.type).toBe(SidebarLayout);

    const guardRoute = garageRoute?.children?.find((route) => route.children?.length);
    expect(guardRoute?.element).toBeTruthy();
    expect(guardRoute?.children?.map((route) => route.path)).toEqual(expect.arrayContaining([
      "",
      "vehicles",
      "vehicles/:vehicleId",
      "vehicles/:vehicleId/maintenance",
      "vehicles/:vehicleId/history",
      "vehicles/:vehicleId/insurance",
    ]));
  });

  test("routeTree_WhenPageIsNotImplemented_UsesTheComingSoonPlaceholder", () => {
    expect(MY_GARAGE_PLACEHOLDER_MODULE).toBe("src/content/basePages/Status/ComingSoon");
    expect(enMyGarage.empty.vehicles).toBeTruthy();
  });

  test.each([
    [Role.User, true],
    [Role.Admin, true],
    [Role.SuperAdmin, true],
    [Role.Guest, false],
  ])("navigation_WhenRoleIs%s_UsesTheSameGarageAccessRule", (role, expected) => {
    expect(canAccessMyGarage(role)).toBe(expected);
  });

  test.each([
    [Role.User, true],
    [Role.Admin, true],
    [Role.Guest, false],
  ])("routeGuard_WhenRoleIs%s_ReturnsExpectedAccess%s", (role, allowed) => {
    render(
      <MemoryRouter initialEntries={["/my-garage/vehicles"]}>
        <Routes>
          <Route path="/my-garage/vehicles" element={<MyGarageRouteGuard role={role} />}>
            <Route index element={<div>garage content</div>} />
          </Route>
          <Route path="/home" element={<div>home content</div>} />
        </Routes>
      </MemoryRouter>,
    );

    if (allowed) {
      expect(screen.queryByText("garage content")).not.toBeNull();
      expect(screen.queryByText("home content")).toBeNull();
    } else {
      expect(screen.queryByText("garage content")).toBeNull();
      expect(screen.queryByText("home content")).not.toBeNull();
    }
  });

  test.each([
    [Role.User, true],
    [Role.Admin, true],
    [Role.Guest, false],
  ])("sidebar_WhenRoleIs%s_ShowsGarageOnlyWhenAllowed%s", (role, visible) => {
    render(
      <Provider store={store}>
        <ThemeProvider>
          <MemoryRouter>
            <SidebarProvider>
              <RoleBasedMenu role={role} />
            </SidebarProvider>
          </MemoryRouter>
        </ThemeProvider>
      </Provider>,
    );

    const garageLabels = screen.queryAllByText(enSidebar.header_my_garage);
    expect(garageLabels.length > 0).toBe(visible);
    expect(screen.queryByTestId("GarageIcon") !== null).toBe(visible);
  });
});

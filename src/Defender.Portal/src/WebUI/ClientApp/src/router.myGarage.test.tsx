import routes from "src/router";
import enMyGarage from "src/localization/en/myGarage.json";
import ruMyGarage from "src/localization/ru/myGarage.json";
import enSidebar from "src/localization/en/sidebar_menu.json";
import ruSidebar from "src/localization/ru/sidebar_menu.json";

type RouteNode = { path?: string; children?: RouteNode[] };

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
    expect(enMyGarage.errors.CAR_CONCURRENCY_CONFLICT).toBeTruthy();
    expect(ruMyGarage.errors.CAR_CONCURRENCY_CONFLICT).toBeTruthy();
    expect(enSidebar.header_my_garage).toBeTruthy();
    expect(ruSidebar.header_my_garage).toBeTruthy();
    expect(enSidebar.page_my_garage).toBeTruthy();
    expect(ruSidebar.page_my_garage).toBeTruthy();
  });
});

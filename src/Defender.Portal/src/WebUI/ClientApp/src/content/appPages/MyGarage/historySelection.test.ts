import { describe, expect, test } from "vitest";

import { normalizeMaintenanceSelection, getLinkedMaintenanceLabels, toggleMaintenanceSelection } from "./helpers/historySelection";

describe("My Garage history maintenance selection", () => {
  test("normalizeSelection_WhenIdsRepeat_RemovesDuplicatesInFirstSeenOrder", () => {
    expect(normalizeMaintenanceSelection(["b", "a", "b", "a"])).toEqual(["b", "a"]);
  });

  test("toggleSelection_WhenIdIsSelected_RemovesOnlyThatId", () => {
    expect(toggleMaintenanceSelection(["a", "b"], "a")).toEqual(["b"]);
    expect(toggleMaintenanceSelection(["a"], "b")).toEqual(["a", "b"]);
  });

  test("linkedLabels_WhenItemsHaveStableOrder_ReturnsStableLabels", () => {
    expect(getLinkedMaintenanceLabels(
      ["two", "missing", "one"],
      [{ id: "one", name: "Oil" }, { id: "two", name: "Brakes" }],
    )).toEqual(["Oil", "Brakes"]);
  });
});

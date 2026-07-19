import assert from "node:assert/strict";
import test from "node:test";
import { calendarOperationNames, getCalendarOperationPath } from "./portal-tools.js";

test("calendarOperationNames_WhenComparedToPortalController_CoversEveryMutationRoute", () => {
  assert.deepEqual(calendarOperationNames, [
    "update_theme", "create_queued_trip", "create_event_from_date", "create_event", "update_event", "delete_event", "auto_schedule_event", "add_point", "update_point", "delete_point", "add_participant", "delete_participant", "update_my_participation", "add_packing_item", "update_packing_item", "delete_packing_item",
  ]);
});

test("getCalendarOperationPath_WhenEventPointIsUpdated_UsesPortalControllerRoute", () => {
  const path = getCalendarOperationPath("update_point", {
    eventId: "00000000-0000-0000-0000-000000000001",
    pointId: "00000000-0000-0000-0000-000000000002",
  });

  assert.equal(path, "/api/travelcalendar/events/00000000-0000-0000-0000-000000000001/points/00000000-0000-0000-0000-000000000002");
});

test("getCalendarOperationPath_WhenDeleteParticipantLacksIdentifier_ReturnsUndefined", () => {
  assert.equal(getCalendarOperationPath("delete_participant", { eventId: "00000000-0000-0000-0000-000000000001" }), undefined);
});

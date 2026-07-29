import { fireEvent, render, screen } from "@testing-library/react";

import "src/localization/i18n";
import type { TravelEvent } from "src/api/travelCalendar";
import { InvitationPanel } from "./InvitationPanel";

const invite = (status: "Pending" | "Declined"): TravelEvent => ({
  id: status,
  version: 1,
  ownerUserId: "organizer",
  ownerDisplayName: "Alice",
  title: `${status} trip`,
  type: "DayTrip",
  startDate: "2026-07-18",
  endDate: "2026-07-18",
  isMustVisit: false,
  queueOrder: 0,
  participants: [],
  myParticipationStatus: status,
  canEdit: false,
  canRespond: status === "Pending",
  distanceKm: 0,
  points: [],
  otherCostPln: 0,
  transportCostPln: 0,
  totalCostPln: 0,
});

describe("InvitationPanel", () => {
  test("WhenIncomingInvitationsHaveDifferentStatuses_ShowsPendingByDefaultAndLetsUserSwitchToDeclined", () => {
    render(
      <InvitationPanel events={[invite("Pending"), invite("Declined")]} onOpen={vi.fn()} />,
    );

    expect(screen.getByText("Pending trip")).not.toBeNull();
    expect(screen.getByText("Alice")).not.toBeNull();
    expect(screen.queryByText("Declined trip")).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Declined" }));

    expect(screen.getByText("Declined trip")).not.toBeNull();
    expect(screen.getByText("Declined")).not.toBeNull();
  });
});

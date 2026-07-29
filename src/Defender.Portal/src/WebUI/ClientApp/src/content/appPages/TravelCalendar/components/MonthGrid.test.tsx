import { render, screen } from "@testing-library/react";
import "src/localization/i18n";
import ThemeProvider from "src/theme/ThemeProvider";
import type { TravelEvent } from "src/api/travelCalendar";
import { MonthGrid } from "./MonthGrid";

const invitedEvent: TravelEvent = {
  id: "event-1",
  version: 1,
  ownerUserId: "organizer",
  title: "Weekend trip",
  type: "DayTrip",
  startDate: "2026-07-18",
  endDate: "2026-07-18",
  isMustVisit: false,
  queueOrder: 0,
  participants: [],
  myParticipationStatus: "Pending",
  canEdit: false,
  canRespond: true,
  distanceKm: 0,
  points: [],
  otherCostPln: 0,
  transportCostPln: 0,
  totalCostPln: 0,
};

describe("MonthGrid", () => {
  test("WhenEventInvitationIsPending_ExposesItsDistinctInvitationStatus", () => {
    render(
      <ThemeProvider>
        <MonthGrid year={2026} month={6} events={[invitedEvent]} holidays={[]} onDate={vi.fn()} onEvent={vi.fn()} />
      </ThemeProvider>,
    );

    expect(screen.getByRole("button", { name: /2026-07-18.*Pending invitation/i })).not.toBeNull();
  });
});

import { fireEvent, render, screen } from "@testing-library/react";

import "src/localization/i18n";
import type { TravelEvent } from "src/api/travelCalendar";
import { InvitationPanel } from "./InvitationPanel";

const invite = (status: "Pending" | "Declined", id: string = status): TravelEvent => ({
  id,
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

  test("WhenOnlyPendingInvitationsExist_SwitchingToDeclinedKeepsTheDeclinedTabSelected", () => {
    render(
      <InvitationPanel events={[invite("Pending")]} onOpen={vi.fn()} />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Declined" }));

    expect(screen.getByRole("button", { name: "Declined", pressed: true })).not.toBeNull();
    expect(screen.getByText("No Declined invitations")).not.toBeNull();
    expect(screen.queryByText("Pending trip")).toBeNull();
  });

  test("WhenThereAreNoInvitations_DefaultsToThePendingTab", () => {
    render(
      <InvitationPanel events={[]} onOpen={vi.fn()} />,
    );

    expect(screen.getByRole("button", { name: "Pending", pressed: true })).not.toBeNull();
    expect(screen.getByText("No Pending invitations")).not.toBeNull();
  });

  test("WhenOnlyDeclinedInvitationsExist_SelectsDeclinedOnMount", () => {
    render(
      <InvitationPanel events={[invite("Declined")]} onOpen={vi.fn()} />,
    );

    expect(screen.getByRole("button", { name: "Declined", pressed: true })).not.toBeNull();
    expect(screen.getByText("Declined trip")).not.toBeNull();
  });

  test("WhenCalendarRefreshesWithoutStatusChanges_KeepsTheTabTheUserChose", () => {
    const { rerender } = render(
      <InvitationPanel events={[invite("Pending")]} onOpen={vi.fn()} />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Declined" }));
    expect(screen.getByRole("button", { name: "Declined", pressed: true })).not.toBeNull();

    // Fresh array + fresh objects, same content — simulates a focus-driven refetch.
    rerender(
      <InvitationPanel events={[invite("Pending")]} onOpen={vi.fn()} />,
    );

    expect(screen.getByRole("button", { name: "Declined", pressed: true })).not.toBeNull();
  });

  test("WhenTheLastPendingInvitationIsAnswered_MovesToTheDeclinedTab", () => {
    const { rerender } = render(
      <InvitationPanel events={[invite("Pending"), invite("Declined")]} onOpen={vi.fn()} />,
    );

    expect(screen.getByRole("button", { name: "Pending", pressed: true })).not.toBeNull();

    rerender(
      <InvitationPanel events={[invite("Declined")]} onOpen={vi.fn()} />,
    );

    expect(screen.getByRole("button", { name: "Declined", pressed: true })).not.toBeNull();
  });
});

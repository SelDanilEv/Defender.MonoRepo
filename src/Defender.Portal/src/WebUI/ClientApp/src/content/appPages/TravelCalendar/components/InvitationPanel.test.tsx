import { fireEvent, render, screen } from "@testing-library/react";

import "src/localization/i18n";
import ThemeProvider from "src/theme/ThemeProvider";
import type { TravelEvent, TravelParticipant, TravelParticipantStatus } from "src/api/travelCalendar";
import { InvitationPanel } from "./InvitationPanel";

const receivedInvite = (status: "Pending" | "Accepted" | "Declined", overrides: Partial<TravelEvent> = {}): TravelEvent => ({
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
  points: [],
  otherCostPln: 0,
  transportCostPln: 0,
  totalCostPln: 0,
  ...overrides,
});

const participant = (displayName: string, status: TravelParticipantStatus, userId: string = displayName.toLowerCase()): TravelParticipant => ({
  userId,
  displayName,
  status,
});

const sentEvent = (overrides: Partial<TravelEvent> = {}): TravelEvent => ({
  id: "sent-event",
  version: 1,
  ownerUserId: "me",
  ownerDisplayName: "Me",
  title: "Sent trip",
  type: "DayTrip",
  startDate: "2026-07-18",
  endDate: "2026-07-18",
  isMustVisit: false,
  queueOrder: 0,
  participants: [participant("Bob", "Pending")],
  canEdit: true,
  canRespond: false,
  points: [],
  otherCostPln: 0,
  transportCostPln: 0,
  totalCostPln: 0,
  ...overrides,
});

// InvitationPanel reads theme.colors.success.main (a custom NebulaFighterTheme
// augmentation, not a stock MUI Theme property) for the Accepted status accent, so it
// needs the app's real ThemeProvider rather than MUI's bare default - same reason
// MonthGrid.test.tsx wraps with it.
const renderPanel = (overrides: Partial<Parameters<typeof InvitationPanel>[0]> = {}) =>
  render(
    <ThemeProvider>
      <InvitationPanel
        events={[]}
        onOpen={vi.fn()}
        onRespond={vi.fn().mockResolvedValue(null)}
        onRemoveParticipant={vi.fn().mockResolvedValue(null)}
        busy={false}
        {...overrides}
      />
    </ThemeProvider>,
  );

describe("InvitationPanel - Received tab status filter", () => {
  test("WhenIncomingInvitationsHaveDifferentStatuses_ShowsPendingByDefaultAndLetsUserSwitchToDeclined", () => {
    renderPanel({ events: [receivedInvite("Pending"), receivedInvite("Declined")] });

    expect(screen.getByText("Pending trip")).not.toBeNull();
    expect(screen.getByText("Alice")).not.toBeNull();
    expect(screen.queryByText("Declined trip")).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Declined" }));

    expect(screen.getByText("Declined trip")).not.toBeNull();
    expect(screen.getByText("Declined")).not.toBeNull();
  });

  test("WhenOnlyPendingInvitationsExist_SwitchingToDeclinedKeepsTheDeclinedTabSelected", () => {
    renderPanel({ events: [receivedInvite("Pending")] });

    fireEvent.click(screen.getByRole("button", { name: "Declined" }));

    expect(screen.getByRole("button", { name: "Declined", pressed: true })).not.toBeNull();
    expect(screen.getByText("No Declined invitations")).not.toBeNull();
    expect(screen.queryByText("Pending trip")).toBeNull();
  });

  test("WhenThereAreNoInvitations_DefaultsToThePendingTab", () => {
    renderPanel({ events: [] });

    expect(screen.getByRole("button", { name: "Pending", pressed: true })).not.toBeNull();
    expect(screen.getByText("No Pending invitations")).not.toBeNull();
  });

  test("WhenOnlyDeclinedInvitationsExist_SelectsDeclinedOnMount", () => {
    renderPanel({ events: [receivedInvite("Declined")] });

    expect(screen.getByRole("button", { name: "Declined", pressed: true })).not.toBeNull();
    expect(screen.getByText("Declined trip")).not.toBeNull();
  });

  test("WhenCalendarRefreshesWithoutStatusChanges_KeepsTheTabTheUserChose", () => {
    const { rerender } = renderPanel({ events: [receivedInvite("Pending")] });

    fireEvent.click(screen.getByRole("button", { name: "Declined" }));
    expect(screen.getByRole("button", { name: "Declined", pressed: true })).not.toBeNull();

    // Fresh array + fresh objects, same content - simulates a focus-driven refetch.
    rerender(
      <ThemeProvider>
        <InvitationPanel events={[receivedInvite("Pending")]} onOpen={vi.fn()} onRespond={vi.fn()} onRemoveParticipant={vi.fn()} busy={false} />
      </ThemeProvider>,
    );

    expect(screen.getByRole("button", { name: "Declined", pressed: true })).not.toBeNull();
  });

  test("WhenTheLastPendingInvitationIsAnswered_MovesToTheDeclinedTab", () => {
    const { rerender } = renderPanel({ events: [receivedInvite("Pending"), receivedInvite("Declined")] });

    expect(screen.getByRole("button", { name: "Pending", pressed: true })).not.toBeNull();

    rerender(
      <ThemeProvider>
        <InvitationPanel events={[receivedInvite("Declined")]} onOpen={vi.fn()} onRespond={vi.fn()} onRemoveParticipant={vi.fn()} busy={false} />
      </ThemeProvider>,
    );

    expect(screen.getByRole("button", { name: "Declined", pressed: true })).not.toBeNull();
  });

  test("WhenReceivedInvitationIsAccepted_ShowsItOnTheAcceptedTab", () => {
    // Headline regression: the old 2-status (Pending/Declined) panel had nowhere for an
    // accepted invitation to show up at all once answered - it just silently vanished.
    renderPanel({ events: [receivedInvite("Accepted"), receivedInvite("Pending")] });

    fireEvent.click(screen.getByRole("button", { name: "Accepted" }));

    expect(screen.getByText("Accepted trip")).not.toBeNull();
    expect(screen.queryByText("Pending trip")).toBeNull();
  });
});

describe("InvitationPanel - Received/Sent direction tabs", () => {
  test("ReceivedTab_WhenManyInvitationsAreVisible_PreventsCardsFromShrinkingIntoEachOther", () => {
    renderPanel({
      events: Array.from({ length: 9 }, (_, index) =>
        receivedInvite("Accepted", { id: `accepted-${index}`, title: `Accepted event ${index}` }),
      ),
    });

    const cards = screen
      .getAllByRole("button", { name: /^Open invitation:/ })
      .map((button) => button.parentElement);

    expect(cards).toHaveLength(9);
    expect(cards.every((card) => card && getComputedStyle(card).flexShrink === "0")).toBe(true);
  });

  test("SentTab_WhenManyInvitationsAreVisible_PreventsCardsFromShrinkingIntoEachOther", () => {
    renderPanel({
      events: Array.from({ length: 9 }, (_, index) =>
        sentEvent({ id: `sent-${index}`, title: `Sent event ${index}` }),
      ),
    });

    fireEvent.click(screen.getByRole("tab", { name: "Sent" }));

    const cards = screen
      .getAllByRole("button", { name: /^Open sent invitation:/ })
      .map((button) => button.parentElement);

    expect(cards).toHaveLength(9);
    expect(cards.every((card) => card && getComputedStyle(card).flexShrink === "0")).toBe(true);
  });

  test("WhenUserSwitchesToTheSentTab_ShowsOrganizedEventsInsteadOfReceivedOnes", () => {
    renderPanel({
      events: [receivedInvite("Pending"), sentEvent({ id: "sent-1", title: "Organized trip" })],
    });

    expect(screen.getByText("Pending trip")).not.toBeNull();

    fireEvent.click(screen.getByRole("tab", { name: "Sent" }));

    expect(screen.getByText("Organized trip")).not.toBeNull();
    expect(screen.queryByText("Pending trip")).toBeNull();
  });

  test("WhenOnlySentEventsExist_DefaultsToTheSentTab", () => {
    renderPanel({ events: [sentEvent({ id: "sent-1", title: "Organized trip" })] });

    expect(screen.getByRole("tab", { name: "Sent", selected: true })).not.toBeNull();
    expect(screen.getByText("Organized trip")).not.toBeNull();
  });

  test("WhenCalendarRefreshesWithoutCountsChanging_KeepsTheSentTabSelected", () => {
    const { rerender } = renderPanel({
      events: [receivedInvite("Pending"), sentEvent({ id: "sent-1", title: "Organized trip" })],
    });

    fireEvent.click(screen.getByRole("tab", { name: "Sent" }));
    expect(screen.getByRole("tab", { name: "Sent", selected: true })).not.toBeNull();

    // Fresh array + fresh objects, same content and same counts either side.
    rerender(
      <ThemeProvider>
        <InvitationPanel
          events={[receivedInvite("Pending"), sentEvent({ id: "sent-1", title: "Organized trip" })]}
          onOpen={vi.fn()}
          onRespond={vi.fn()}
          onRemoveParticipant={vi.fn()}
          busy={false}
        />
      </ThemeProvider>,
    );

    expect(screen.getByRole("tab", { name: "Sent", selected: true })).not.toBeNull();
  });

  test("HeaderCountChip_ReflectsOnlyTheActiveTabsCurrentlyVisibleRows", () => {
    renderPanel({
      events: [
        receivedInvite("Pending", { id: "p1" }),
        receivedInvite("Pending", { id: "p2" }),
        receivedInvite("Accepted", { id: "a1" }),
        sentEvent({ id: "s1" }),
        sentEvent({ id: "s2" }),
        sentEvent({ id: "s3" }),
      ],
    });

    // Received tab, Pending sub-filter active by default: 2 of the 3 received rows.
    expect(screen.getByText("2")).not.toBeNull();

    fireEvent.click(screen.getByRole("tab", { name: "Sent" }));

    // Sent tab has no status sub-filter: all 3 sent events count.
    expect(screen.getByText("3")).not.toBeNull();
  });
});

describe("InvitationPanel - Sent tab", () => {
  test("SentTab_ShowsOneCardPerEventWithAllParticipantChipsAndAnAwaitingCountForPendingOnes", () => {
    renderPanel({
      events: [
        sentEvent({
          id: "sent-1",
          title: "Group trip",
          participants: [participant("Bob", "Accepted"), participant("Carol", "Pending")],
        }),
      ],
    });

    fireEvent.click(screen.getByRole("tab", { name: "Sent" }));

    expect(screen.getByText("Group trip")).not.toBeNull();
    expect(screen.getByText("Bob")).not.toBeNull();
    expect(screen.getByText("Carol")).not.toBeNull();
    expect(screen.getByText("1 awaiting reply")).not.toBeNull();
  });

  test("SentTab_WhenNoParticipantIsPending_HidesTheAwaitingChip", () => {
    renderPanel({
      events: [sentEvent({ id: "sent-1", participants: [participant("Bob", "Accepted")] })],
    });

    fireEvent.click(screen.getByRole("tab", { name: "Sent" }));

    expect(screen.queryByText(/awaiting reply/)).toBeNull();
  });

  test("SentTab_EachParticipantChipCarriesANameAndStatusAriaLabel", () => {
    renderPanel({
      events: [sentEvent({ id: "sent-1", participants: [participant("Bob", "Accepted")] })],
    });

    fireEvent.click(screen.getByRole("tab", { name: "Sent" }));

    const chip = screen.getByText("Bob").closest("[aria-label]");
    expect(chip?.getAttribute("aria-label")).toBe("Bob — Accepted");
  });

  test("SentTab_WhenThereAreNoSentEvents_ShowsTheEmptySentMessage", () => {
    renderPanel({ events: [receivedInvite("Pending")] });

    fireEvent.click(screen.getByRole("tab", { name: "Sent" }));

    expect(screen.getByText("You haven't invited anyone yet")).not.toBeNull();
  });
});

describe("InvitationPanel - lookback and past events", () => {
  test("WhenAPendingInvitationIsInThePast_StillShowsOnPendingWithAPastChip", () => {
    renderPanel({ events: [receivedInvite("Pending", { startDate: "2020-01-01", endDate: "2020-01-01" })] });

    expect(screen.getByText("Pending trip")).not.toBeNull();
    expect(screen.getByText("Past")).not.toBeNull();
  });

  test("WhenADeclinedInvitationIsOlderThanTheLookbackWindow_IsHiddenFromTheDeclinedTab", () => {
    renderPanel({ events: [receivedInvite("Declined", { startDate: "2020-01-01", endDate: "2020-01-01" })] });

    fireEvent.click(screen.getByRole("button", { name: "Declined" }));

    expect(screen.getByText("No Declined invitations")).not.toBeNull();
  });
});

describe("InvitationPanel - inline actions", () => {
  test("ReceivedRow_ClickingAccept_CallsOnRespondAndDoesNotAlsoOpenTheDrawer", () => {
    const onOpen = vi.fn();
    const onRespond = vi.fn().mockResolvedValue(null);
    renderPanel({ events: [receivedInvite("Pending", { id: "evt-1" })], onOpen, onRespond });

    fireEvent.click(screen.getByRole("button", { name: "Accept" }));

    expect(onRespond).toHaveBeenCalledWith("evt-1", "Accepted");
    expect(onOpen).not.toHaveBeenCalled();
  });

  test("ReceivedRow_ClickingDecline_CallsOnRespondAndDoesNotAlsoOpenTheDrawer", () => {
    const onOpen = vi.fn();
    const onRespond = vi.fn().mockResolvedValue(null);
    renderPanel({ events: [receivedInvite("Pending", { id: "evt-1" })], onOpen, onRespond });

    fireEvent.click(screen.getByRole("button", { name: "Decline" }));

    expect(onRespond).toHaveBeenCalledWith("evt-1", "Declined");
    expect(onOpen).not.toHaveBeenCalled();
  });

  test("ReceivedRow_OpeningViaTheHeaderStillWorks", () => {
    const onOpen = vi.fn();
    renderPanel({ events: [receivedInvite("Pending", { id: "evt-1", title: "Ski trip" })], onOpen });

    fireEvent.click(screen.getByRole("button", { name: "Open invitation: Ski trip" }));

    expect(onOpen).toHaveBeenCalledWith("evt-1");
  });

  test("ReceivedRow_GatesAcceptDeclineOnCanRespondRatherThanOnStatus", () => {
    // Deliberately an Accepted invitation with canRespond still true: the buttons must
    // key off the backend-computed flag, not assume it's only ever true for Pending.
    renderPanel({ events: [receivedInvite("Accepted", { id: "evt-1", canRespond: true })] });

    fireEvent.click(screen.getByRole("button", { name: "Accepted" }));

    expect(screen.getByRole("button", { name: "Accept" })).not.toBeNull();
    expect(screen.getByRole("button", { name: "Decline" })).not.toBeNull();
  });

  test("ReceivedRow_WhenCanRespondIsFalse_HidesAcceptAndDeclineEvenForAPendingInvitation", () => {
    renderPanel({ events: [receivedInvite("Pending", { id: "evt-1", canRespond: false })] });

    expect(screen.queryByRole("button", { name: "Accept" })).toBeNull();
    expect(screen.queryByRole("button", { name: "Decline" })).toBeNull();
  });

  test("SentRow_ClickingRemoveOnAParticipant_CallsOnRemoveParticipantAndDoesNotOpenTheDrawer", () => {
    const onOpen = vi.fn();
    const onRemoveParticipant = vi.fn().mockResolvedValue(null);
    renderPanel({
      events: [sentEvent({ id: "evt-2", participants: [participant("Bob", "Pending", "user-bob")] })],
      onOpen,
      onRemoveParticipant,
    });
    fireEvent.click(screen.getByRole("tab", { name: "Sent" }));

    fireEvent.click(screen.getByRole("button", { name: "Remove" }));

    expect(onRemoveParticipant).toHaveBeenCalledWith("evt-2", "user-bob");
    expect(onOpen).not.toHaveBeenCalled();
  });

  test("SentRow_OpeningViaTheHeaderStillWorks", () => {
    const onOpen = vi.fn();
    renderPanel({ events: [sentEvent({ id: "evt-2", title: "Group ski trip" })], onOpen });
    fireEvent.click(screen.getByRole("tab", { name: "Sent" }));

    fireEvent.click(screen.getByRole("button", { name: "Open sent invitation: Group ski trip" }));

    expect(onOpen).toHaveBeenCalledWith("evt-2");
  });

  test("WhenBusyIsTrue_DisablesInlineAcceptDeclineAndRemoveButtons", () => {
    renderPanel({
      events: [
        receivedInvite("Pending", { id: "evt-1" }),
        sentEvent({ id: "evt-2", participants: [participant("Bob", "Pending", "user-bob")] }),
      ],
      busy: true,
    });

    expect((screen.getByRole("button", { name: "Accept" }) as HTMLButtonElement).disabled).toBe(true);
    expect((screen.getByRole("button", { name: "Decline" }) as HTMLButtonElement).disabled).toBe(true);

    fireEvent.click(screen.getByRole("tab", { name: "Sent" }));

    expect((screen.getByRole("button", { name: "Remove" }) as HTMLButtonElement).disabled).toBe(true);
  });

  test("WhenBusyIsFalse_InlineAcceptDeclineAndRemoveButtonsAreEnabled", () => {
    renderPanel({
      events: [
        receivedInvite("Pending", { id: "evt-1" }),
        sentEvent({ id: "evt-2", participants: [participant("Bob", "Pending", "user-bob")] }),
      ],
      busy: false,
    });

    expect((screen.getByRole("button", { name: "Accept" }) as HTMLButtonElement).disabled).toBe(false);

    fireEvent.click(screen.getByRole("tab", { name: "Sent" }));

    expect((screen.getByRole("button", { name: "Remove" }) as HTMLButtonElement).disabled).toBe(false);
  });
});

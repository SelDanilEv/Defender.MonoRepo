import type { TravelParticipantStatus } from "src/api/travelCalendar";

// Shared between EventDrawer (full participant list) and InvitationPanel (Sent tab's
// per-event participant chips) so the two renderings of "what color is this status"
// can't drift apart.
export const participantColor = (status?: TravelParticipantStatus) => {
  switch (status) {
    case "Accepted":
      return "success";
    case "Declined":
      return "default";
    default:
      return "warning";
  }
};

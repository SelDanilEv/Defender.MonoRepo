import { describe, expect, test } from "vitest";

import { createDraftEvent } from "../draftEvent";
import { createEventDraft, EventDraft, toAmount, toUpdateEventRequest } from "./eventDrawerDraft";

describe("createEventDraft", () => {
  test("createEventDraft_WhenBudgetValuesAreZeroOrAbsent_ReturnsEmptyStrings", () => {
    const event = createDraftEvent("2026-07-18");

    const draft = createEventDraft(event);

    expect(draft.hotelCostPln).toBe("");
    expect(draft.transportCostPln).toBe("");
    expect(draft.otherCostPln).toBe("");
  });

  test("createEventDraft_WhenBudgetValuesArePresent_ReturnsStringifiedValues", () => {
    const event = {
      ...createDraftEvent("2026-07-18"),
      hotel: { isBooked: true, costPln: 300 },
      transportCostPln: 120.5,
      otherCostPln: 40,
    };

    const draft = createEventDraft(event);

    expect(draft.hotelCostPln).toBe("300");
    expect(draft.transportCostPln).toBe("120.5");
    expect(draft.otherCostPln).toBe("40");
  });
});

describe("toUpdateEventRequest", () => {
  const baseDraft: EventDraft = {
    title: "Weekend trip",
    type: "OvernightTrip",
    startDate: "2026-07-18",
    endDate: "2026-07-19",
    notes: "",
    hotelBooked: false,
    hotelName: "",
    hotelAddress: "",
    hotelBookingUrl: "",
    hotelCostPln: "",
    transportCostPln: "",
    mainPoint: "",
    otherCostPln: "",
  };

  test("toUpdateEventRequest_WhenBudgetFieldsAreBlank_SendsZero", () => {
    const request = toUpdateEventRequest(baseDraft);

    expect(request.hotelCostPln).toBe(0);
    expect(typeof request.hotelCostPln).toBe("number");
    expect(request.transportCostPln).toBe(0);
    expect(typeof request.transportCostPln).toBe("number");
    expect(request.otherCostPln).toBe(0);
    expect(typeof request.otherCostPln).toBe("number");
  });

  test("toUpdateEventRequest_WhenBudgetFieldsAreTyped_SendsNumbers", () => {
    const request = toUpdateEventRequest({
      ...baseDraft,
      hotelCostPln: "250",
      transportCostPln: "120.5",
      otherCostPln: "40",
    });

    expect(request.hotelCostPln).toBe(250);
    expect(request.transportCostPln).toBe(120.5);
    expect(request.otherCostPln).toBe(40);
  });

  test("toUpdateEventRequest_WhenNonBudgetFieldsAreSet_PassesThemThrough", () => {
    const request = toUpdateEventRequest({
      ...baseDraft,
      title: "Mountain trip",
      notes: "Bring boots",
      hotelBooked: true,
      hotelName: "Alpine Lodge",
    });

    expect(request.title).toBe("Mountain trip");
    expect(request.notes).toBe("Bring boots");
    expect(request.hotelBooked).toBe(true);
    expect(request.hotelName).toBe("Alpine Lodge");
  });
});

describe("toAmount", () => {
  test("toAmount_WhenValueIsNotANumber_ReturnsZero", () => {
    expect(toAmount("not-a-number")).toBe(0);
  });
});

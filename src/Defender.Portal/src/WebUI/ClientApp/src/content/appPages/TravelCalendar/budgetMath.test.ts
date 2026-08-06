import { liveTotal } from "./budgetMath";

describe("travel budget math", () => {
  it("adds hotel only for overnight trips", () => expect(liveTotal("OvernightTrip", 252, 427.68, 200)).toBe(879.68));
  it("excludes transport for a non-trip type", () => expect(liveTotal("Event", 252, 427.68, 200)).toBe(200));
});

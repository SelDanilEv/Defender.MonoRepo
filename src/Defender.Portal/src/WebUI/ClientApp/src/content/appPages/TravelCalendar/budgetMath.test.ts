import { calculateTransport, liveTotal } from "./budgetMath";

describe("travel budget math", () => {
  it("matches server fuel calculation", () => expect(calculateTransport(540, 12, 6.6)).toBe(427.68));
  it("adds hotel only for overnight trips", () => expect(liveTotal("OvernightTrip", 252, 540, 200, 12, 6.6)).toBe(879.68));
});

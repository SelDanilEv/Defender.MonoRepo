import config from "src/config.json";
import stateLoader from "src/state/StateLoader";

const stateKey = `${config.LOCAL_STORAGE_KEY}:state`;

describe("StateLoader", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  test("saveState_WhenTokenProvided_PersistsSanitizedToken", () => {
    stateLoader.saveState({
      isAuthenticated: true,
      language: "en",
      token: "secret-token",
    });

    const raw = localStorage.getItem(stateKey);
    expect(raw).not.toBeNull();

    const saved = JSON.parse(raw!);
    expect(saved.token).toBe("");
    expect(saved.isAuthenticated).toBe(true);
  });

  test("loadState_WhenLegacyTokenStored_ReturnsSanitizedSession", () => {
    localStorage.setItem(
      stateKey,
      JSON.stringify({
        isAuthenticated: true,
        token: "legacy-token",
      })
    );

    const loaded = stateLoader.loadState() as any;

    expect(loaded.session.token).toBe("");
    expect(loaded.session.isAuthenticated).toBe(true);
  });
});

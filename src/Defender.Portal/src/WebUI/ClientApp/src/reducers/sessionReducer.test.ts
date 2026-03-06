import sessionReducer, {
  logoutActionName,
} from "src/reducers/sessionReducer";
import stateLoader from "src/state/StateLoader";

describe("sessionReducer", () => {
  afterEach(() => {
    jest.restoreAllMocks();
  });

  test("logout_WhenAuthenticated_ClearsTokenAndAuthentication", () => {
    const saveStateSpy = jest.spyOn(stateLoader, "saveState").mockImplementation(() => {});
    const state = {
      user: {
        id: "1",
        nickname: "nick",
        email: "mail@example.com",
        phone: "+1000000000",
        isEmailVerified: true,
        isPhoneVerified: true,
        isBlocked: false,
        roles: ["User"],
        createdDate: undefined,
      },
      language: "en",
      isAuthenticated: true,
      token: "secret-token",
    };

    const result = sessionReducer(state as any, {
      type: logoutActionName,
      payload: "",
    });

    expect(result.isAuthenticated).toBe(false);
    expect(result.token).toBe("");
    expect(result.user.id).toBe("");
    expect(saveStateSpy).toHaveBeenCalledWith(expect.objectContaining({ token: "" }));
  });
});

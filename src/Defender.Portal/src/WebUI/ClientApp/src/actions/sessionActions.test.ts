import { logout } from "src/actions/sessionActions";
import { logoutActionName } from "src/reducers/sessionReducer";
import { cleanWalletInfoActionName } from "src/reducers/walletReducer";

describe("sessionActions.logout", () => {
  test("WhenCalled_CallsServerLogoutAndDispatchesCleanupActions", async () => {
    const fetchMock = jest.fn().mockResolvedValue({});
    (global as any).fetch = fetchMock;
    const dispatch = jest.fn();

    await logout()(dispatch);

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/authorization/logout",
      expect.objectContaining({
        method: "POST",
        credentials: "same-origin",
      })
    );
    expect(dispatch).toHaveBeenNthCalledWith(1, {
      type: logoutActionName,
      payload: "",
    });
    expect(dispatch).toHaveBeenNthCalledWith(2, {
      type: cleanWalletInfoActionName,
      payload: "",
    });
  });
});

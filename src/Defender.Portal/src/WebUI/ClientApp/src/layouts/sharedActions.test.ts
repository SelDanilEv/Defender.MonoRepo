import { logoutPortal } from "./sharedActions";
import WarningToast from "src/components/Toast/WarningToast";
import stateLoader from "src/state/StateLoader";
import { resetSessionExpiryHandling } from "src/services/SessionExpiryService";

jest.mock("src/components/Toast/WarningToast", () => jest.fn());
jest.mock("src/state/StateLoader", () => ({ __esModule: true, default: { cleanState: jest.fn() } }));

describe("logoutPortal", () => {
  beforeEach(() => {
    resetSessionExpiryHandling();
    jest.clearAllMocks();
  });

  test("WhenConcurrentSessionExpiryCalls_OnlyShowsOneNotification", () => {
    const logout = jest.fn();
    const navigate = jest.fn();
    const utils: any = { t: jest.fn().mockReturnValue("Session expired"), react: { navigate } };

    logoutPortal(utils, logout);
    logoutPortal(utils, logout);

    expect(WarningToast).toHaveBeenCalledTimes(1);
    expect(stateLoader.cleanState).toHaveBeenCalledTimes(1);
    expect(logout).toHaveBeenCalledTimes(1);
    expect(navigate).toHaveBeenCalledTimes(1);
  });
});

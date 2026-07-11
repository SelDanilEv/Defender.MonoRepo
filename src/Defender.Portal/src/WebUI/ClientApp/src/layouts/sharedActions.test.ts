import { logoutPortal } from "./sharedActions";
import WarningToast from "src/components/Toast/WarningToast";
import stateLoader from "src/state/StateLoader";
import { resetSessionExpiryHandling } from "src/services/SessionExpiryService";

vi.mock("src/components/Toast/WarningToast", () => ({ default: vi.fn() }));
vi.mock("src/state/StateLoader", () => ({ __esModule: true, default: { cleanState: vi.fn() } }));

describe("logoutPortal", () => {
  beforeEach(() => {
    resetSessionExpiryHandling();
    vi.clearAllMocks();
  });

  test("WhenConcurrentSessionExpiryCalls_OnlyShowsOneNotification", () => {
    const logout = vi.fn();
    const navigate = vi.fn();
    const utils: any = { t: vi.fn().mockReturnValue("Session expired"), react: { navigate } };

    logoutPortal(utils, logout);
    logoutPortal(utils, logout);

    expect(WarningToast).toHaveBeenCalledTimes(1);
    expect(stateLoader.cleanState).toHaveBeenCalledTimes(1);
    expect(logout).toHaveBeenCalledTimes(1);
    expect(navigate).toHaveBeenCalledTimes(1);
  });
});

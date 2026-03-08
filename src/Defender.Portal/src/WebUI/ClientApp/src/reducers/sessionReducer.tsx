import stateLoader from "src/state/StateLoader";
import { Session } from "src/models/Session";
import { UserAccountInfo } from "src/models/UserAccountInfo";

type SessionAction = {
  type: string;
  payload?: Session | UserAccountInfo | string;
};

const initialUser: UserAccountInfo = {
  id: "",
  nickname: "",
  email: "",
  phone: "",
  isEmailVerified: false,
  isPhoneVerified: false,
  isBlocked: false,
  roles: [],
  createdDate: undefined,
};

const initialState: Session = {
  user: initialUser,
  language: "en",
  isAuthenticated: false,
  token: "",
};

const sessionReducer = (
  state: Session = initialState,
  action: SessionAction
) => {
  switch (action.type) {
    case loginActionName:
      if (!action.payload || typeof action.payload === "string") {
        return state;
      }

      const sessionPayload = action.payload as Session;
      state = {
        ...state,
        user: sessionPayload.user,
        token: sessionPayload.token,
        isAuthenticated: true,
      };
      break;
    case logoutActionName:
      state = {
        ...state,
        token: "",
        isAuthenticated: false,
        user: initialUser,
      };
      break;
    case updateLanguageActionName:
      if (state.language && typeof action.payload === "string") {
        state = {
          ...state,
          language: action.payload,
        };
      }
      break;
    case updateUserInfoActionName:
      if (!action.payload || typeof action.payload === "string") {
        return state;
      }

      const updatedUser = action.payload as UserAccountInfo;
      state = {
        ...state,
        user: {
          ...state.user,
          nickname: updatedUser.nickname,
        },
      };
      break;
    default:
      return state;
  }

  stateLoader.saveState(state);

  return state;
};

export default sessionReducer;

export const loginActionName = "LOGIN";
export const logoutActionName = "LOGOUT";
export const updateLanguageActionName = "UPDATE_LANGUAGE";
export const updateUserInfoActionName = "UPDATE_USER_INFO";

export type { SessionAction };

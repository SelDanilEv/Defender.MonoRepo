import config from "src/config.json";
import type { Session } from "src/models/Session";

const stateName = config.LOCAL_STORAGE_KEY + ":state";
type SessionPreloadedState = {
  session: Partial<Session>;
};

class StateLoader {
  private sanitizeState = (
    state?: Partial<Session> | null
  ): Partial<Session> | null | undefined => {
    if (!state) {
      return state;
    }

    return {
      ...state,
      token: "",
    };
  };

  loadState = (): SessionPreloadedState | Record<string, never> => {
    try {
      const serializedState = localStorage.getItem(stateName);

      if (serializedState === null) {
        return this.initializeState();
      }

      const stateJson = this.sanitizeState(JSON.parse(serializedState) as Partial<Session> | null);

      if (!stateJson) {
        return this.initializeState();
      }

      const state = {
        session: stateJson,
      };

      return state;
    } catch (err) {
      return this.initializeState();
    }
  };

  saveState = (state: Partial<Session>) => {
    try {
      const serializedState = JSON.stringify(this.sanitizeState(state));
      localStorage.setItem(stateName, serializedState);
    } catch (err) {}
  };

  cleanState = () => {
    try {
      localStorage.removeItem(stateName);
    } catch (err) {
      console.error("error clean state");
    }
  };

  initializeState = (): Record<string, never> => {
    return {};
  };
}

const stateLoader = new StateLoader();

export default stateLoader;

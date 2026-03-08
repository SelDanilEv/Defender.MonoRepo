import { combineReducers } from "redux";
import { configureStore } from "@reduxjs/toolkit";

import stateLoader from "./StateLoader";

import session from "src/reducers/sessionReducer";
import loading from "src/reducers/loadingReducer";
import wallet from "src/reducers/walletReducer";
import budgetTrackerSetup from "src/reducers/budgetTrackerSetupReducer";
import budgetTrackerGroups from "src/reducers/budgetTrackerGroupsReducer";

const rootReducer = combineReducers({
  wallet,
  session,
  loading,
  budgetTrackerSetup,
  budgetTrackerGroups,
});

const store = configureStore({
  reducer: rootReducer,
  preloadedState: stateLoader.loadState(),
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: false,
      immutableCheck: false,
    }),
  devTools: process.env.NODE_ENV !== "production",
});

export type RootState = ReturnType<typeof rootReducer>;
export type AppDispatch = typeof store.dispatch;

export default store;

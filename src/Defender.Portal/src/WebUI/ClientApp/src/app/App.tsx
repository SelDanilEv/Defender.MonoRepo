import { useRoutes } from "react-router-dom";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import { AdapterDateFns } from "@mui/x-date-pickers/AdapterDateFns";
import { CssBaseline } from "@mui/material";

import { useAppSelector } from "src/state/hooks";
import LoadingBar from "src/components/LoadingBar/LoadingBar";
import AppToastContainer from "src/components/ToastContainer";
import ThemeProvider from "src/theme/ThemeProvider";
import router from "src/router";

import "src/custom.css";
import "react-toastify/dist/ReactToastify.css";

import "src/localization/i18n";
import DateLocales from "src/consts/DateLocales";

const App = () => {
  const content = useRoutes(router);
  const currentLanguage = useAppSelector((state) => state.session.language);

  return (
    <ThemeProvider>
      <LocalizationProvider dateAdapter={AdapterDateFns} adapterLocale={DateLocales[currentLanguage]}>
        <AppToastContainer />
        <LoadingBar />
        <CssBaseline />
        {content}
      </LocalizationProvider>
    </ThemeProvider>
  );
};

export default App;

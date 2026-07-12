import { Stack } from "@mui/material";

import LanguageSwither from "src/components/LanguageSwitcher";
import ThemeModeToggle from "src/components/ThemeModeToggle";

const HeaderPreferences = () => (
  <Stack direction="row" spacing={1} sx={{
    alignItems: "center"
  }}>
    <ThemeModeToggle />
    <LanguageSwither />
  </Stack>
);

export default HeaderPreferences;

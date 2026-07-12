import { Stack } from "@mui/material";

import LanguageSwither from "src/components/LanguageSwitcher";
import ThemeModeToggle from "src/components/ThemeModeToggle";

const HeaderPreferences = () => (
  <Stack direction="row" alignItems="center" spacing={1}>
    <ThemeModeToggle />
    <LanguageSwither />
  </Stack>
);

export default HeaderPreferences;

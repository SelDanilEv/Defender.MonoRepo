export const welcomeHeaderLayout = {
  display: "grid",
  gridTemplateColumns: { xs: "1fr", sm: "1fr auto 1fr" },
  alignItems: "center",
  pt: 5,
};

export const welcomeLogoLayout = {
  gridColumn: { xs: "1", sm: "2" },
  justifySelf: "center",
};

export const welcomePreferencesLayout = {
  gridColumn: { xs: "1", sm: "3" },
  justifySelf: { xs: "center", sm: "end" },
  mt: { xs: 1, sm: 0 },
};

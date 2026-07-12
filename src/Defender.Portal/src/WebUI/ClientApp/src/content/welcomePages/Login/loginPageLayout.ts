export const loginPageLayout = {
  display: "grid",
  gridTemplateColumns: { xs: "1fr", md: "52% 48%" },
  minHeight: "100dvh",
  width: "100%",
  overflow: "hidden",
};

export const loginStoryPanelLayout = {
  display: { xs: "none", md: "flex" },
  flexDirection: "column",
  justifyContent: "space-between",
  p: { md: 5, lg: 8 },
  color: "common.white",
  background:
    "radial-gradient(circle at 24% 28%, rgba(126, 104, 255, 0.48), transparent 34%), linear-gradient(145deg, #17124a 0%, #080d2b 68%)",
};

export const loginFormPanelLayout = {
  minHeight: { xs: "100dvh", md: "100vh" },
  px: { xs: 2.5, sm: 5, lg: 10 },
  py: { xs: 2, sm: 3 },
  display: "flex",
  flexDirection: "column",
  position: "relative",
  backgroundColor: "background.default",
};

export const mobileLoginBrandLayout = {
  display: { xs: "flex", md: "none" },
  justifyContent: "center",
  alignItems: "center",
  mt: 5,
};

export const loginFormContentLayout = {
  width: "100%",
  maxWidth: 420,
  m: "auto",
  py: { xs: 4, sm: 6 },
};

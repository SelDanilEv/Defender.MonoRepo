import { Box, Chip, styled } from "@mui/material";
import { alpha } from "@mui/material/styles";

import config from "src/config.json";

const LogoWrapper = styled(Box)(
  ({ theme }) => `
        color: ${theme.palette.text.primary};
        display: flex;
        text-decoration: none;
        width: fit-content;
        margin: 0 auto;
        font-weight: ${theme.typography.fontWeightBold};
        position: relative;
`
);

const LogoInnerStyled = styled(Box)(
  ({ theme }) => `
    width: ${(props) => props.width || theme.spacing(15)};
    height: ${(props) => props.height || theme.spacing(15)};
    border-radius: 20%;
    background-color: #e5f7f3;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    margin: 0 auto ${theme.spacing(2)};

    img {
      width: 100%;
      height: 100%;
      display: block;
    }
`
);

const LogoInner = ({ height, width, children, sx }) => {
  return (
    <LogoInnerStyled sx={{ width, height, ...sx }}>
      {children}
    </LogoInnerStyled>
  );
};

const Logo = (props: any) => {
  const { compact = false } = props;

  return (
    <LogoWrapper sx={{ margin: compact ? 0 : undefined }}>
      <LogoInner
        height={props.height}
        width={props.width}
        sx={{ marginBottom: compact ? 0 : undefined }}
      >
        <img src="/static/images/logo/Logo.png" alt="Logo" />
      </LogoInner>
      <Chip
        label={`v${config.VERSION_OF_APP}`}
        aria-label={`Frontend version ${config.VERSION_OF_APP}`}
        size="small"
        variant="outlined"
        sx={{
          position: "absolute",
          top: -6,
          right: -12,
          height: compact ? 20 : 22,
          borderRadius: 999,
          borderColor: (theme) => alpha(theme.palette.primary.main, 0.42),
          bgcolor: (theme) => alpha(theme.palette.background.paper, 0.94),
          color: "text.primary",
          boxShadow: (theme) => `0 4px 12px ${alpha(theme.palette.common.black, 0.18)}`,
          backdropFilter: "blur(8px)",
          "& .MuiChip-label": {
            px: 0.8,
            fontSize: compact ? "0.62rem" : "0.68rem",
            fontWeight: 700,
            letterSpacing: "0.02em",
          },
        }}
      />
    </LogoWrapper>
  );
};

export default Logo;

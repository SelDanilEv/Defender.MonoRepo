import { Box, Badge, styled } from "@mui/material";

import config from "src/config.json";

const LogoWrapper = styled(Box)(
  ({ theme }) => `
        color: ${theme.palette.text.primary};
        display: flex;
        text-decoration: none;
        width: fit-content;
        margin: 0 auto;
        font-weight: ${theme.typography.fontWeightBold};
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
    <LogoInnerStyled height={height} width={width} sx={sx}>
      {children}
    </LogoInnerStyled>
  );
};

const Logo = (props: any) => {
  const { compact = false } = props;

  return (
    <LogoWrapper sx={{ margin: compact ? 0 : undefined }}>
      <Badge
        sx={{
          ".MuiBadge-badge": {
            fontSize: props.height / 6 || 20,
            padding: props.height / 70 || 1.3,
            right: 3,
            top: props.height / 20 || 8,
          },
        }}
        overlap="circular"
        color="success"
        badgeContent={config.VERSION_OF_APP}
      >
        <LogoInner
          height={props.height}
          width={props.width}
          sx={{ marginBottom: compact ? 0 : undefined }}
        >
          <img src="/static/images/logo/Logo.png" alt="Logo" />
        </LogoInner>
      </Badge>
    </LogoWrapper>
  );
};

export default Logo;

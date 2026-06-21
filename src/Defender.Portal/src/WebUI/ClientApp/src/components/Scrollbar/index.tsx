import { FC, ReactNode } from 'react';
import PropTypes from 'prop-types';
import { Scrollbars } from 'react-custom-scrollbars-2';
import { Box, useTheme } from '@mui/material';


interface ScrollbarProps {
  className?: string;
  children?: ReactNode;
}

const trackBaseStyle = {
  borderRadius: 4,
  background: "transparent",
};

const Scrollbar: FC<ScrollbarProps> = ({ className, children, ...rest }) => {
  const theme = useTheme();

  return (
    <Scrollbars
      autoHide
      autoHideTimeout={600}
      autoHideDuration={180}
      renderTrackVertical={({ style, ...props }) => (
        <Box
          {...props}
          style={{
            ...style,
            top: 2,
            right: 2,
            bottom: 2,
            width: 6,
          }}
          sx={{
            borderRadius: trackBaseStyle.borderRadius,
            background: trackBaseStyle.background,
          }}
        />
      )}
      renderTrackHorizontal={({ style, ...props }) => (
        <Box
          {...props}
          style={{
            ...style,
            right: 2,
            bottom: 2,
            left: 2,
            height: 6,
          }}
          sx={{
            borderRadius: trackBaseStyle.borderRadius,
            background: trackBaseStyle.background,
          }}
        />
      )}
      renderThumbVertical={(props) => {
        return (
          <Box
            {...props}
            sx={{
              width: 4,
              minHeight: 24,
              background: theme.colors.primary.light,
              borderRadius: theme.general.borderRadiusLg,
              transition: theme.transitions.create(["background", "width"]),

              '&:hover': {
                width: 5,
                background: theme.colors.primary.main,
              }
            }}
          />
        );
      }}
      renderThumbHorizontal={(props) => {
        return (
          <Box
            {...props}
            sx={{
              height: 4,
              minWidth: 24,
              background: theme.colors.primary.light,
              borderRadius: theme.general.borderRadiusLg,
              transition: theme.transitions.create(["background", "height"]),

              '&:hover': {
                height: 5,
                background: theme.colors.primary.main,
              }
            }}
          />
        );
      }}
      {...rest}
    >
      {children}
    </Scrollbars>
  );
};

Scrollbar.propTypes = {
  children: PropTypes.node,
  className: PropTypes.string
};

export default Scrollbar;

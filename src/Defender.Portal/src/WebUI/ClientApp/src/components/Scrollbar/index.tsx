import { FC, ReactNode } from 'react';
import { Box } from '@mui/material';


interface ScrollbarProps {
  className?: string;
  children?: ReactNode;
}

const Scrollbar: FC<ScrollbarProps> = ({ className, children, ...rest }) => {
  return (
    <Box
      className={className}
      sx={{ overflow: "auto", width: "100%", height: "100%" }}
      {...rest}
    >
      {children}
    </Box>
  );
};

export default Scrollbar;

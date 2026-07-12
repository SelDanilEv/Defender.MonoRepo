import { useEffect } from 'react';
import NProgress from 'nprogress';
import { Box, CircularProgress } from '@mui/material';


function SuspenseLoader() {
  useEffect(() => {
    NProgress.configure({
      barSelector: '[role="progressbar"]',
      spinnerSelector: '[role="status"]',
      template: '<div class="bar" role="progressbar" aria-label="Loading"></div><div class="spinner" role="status" aria-label="Loading"><div class="spinner-icon"></div></div>',
    });
    NProgress.start();

    return () => {
      NProgress.done();
    };
  }, []);

  return (
    <Box
      sx={{
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        position: 'fixed',
        left: 0,
        top: 0,
        width: '100%',
        height: '100%'
      }}>
      <CircularProgress size={64} disableShrink thickness={3} />
    </Box>
  );
}

export default SuspenseLoader;

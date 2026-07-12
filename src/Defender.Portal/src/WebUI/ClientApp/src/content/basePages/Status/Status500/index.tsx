import {
  Box,
  Typography,
  Container,
  Button,
  Grid,
  styled
} from '@mui/material';
import { Helmet } from 'react-helmet-async';

const MainContent = styled(Box)(
  ({ theme }) => `
    margin-top: 100px;
    height: 100%;
    display: flex;
    flex: 1;
    overflow: auto;
    flex-direction: column;
    align-items: center;
    justify-content: center;
`
);

function Status500() {

  return (
    <>
      <Helmet>
        <title>Status - 500</title>
      </Helmet>
      <MainContent>
        <Grid
          container
          spacing={0}
          sx={{
            alignItems: "stretch",
            height: '100%'
          }}>
          <Container maxWidth="sm">
            <Box sx={{
              textAlign: "center"
            }}>
              <img
                alt="500"
                height={260}
                src="/static/images/status/500.svg"
              />
              <Typography variant="h2" sx={{ my: 2 }}>
                There was an error, please try again later
              </Typography>
              <Typography
                variant="h4"
                sx={{
                  color: "text.secondary",
                  fontWeight: "normal",
                  mb: 4
                }}>
                The server encountered an internal error and was not able to
                complete your request
              </Typography>
              <Button href="/" variant="contained" sx={{ ml: 1 }}>
                Go back
              </Button>
            </Box>
          </Container>
        </Grid>
      </MainContent>
    </>
  );
}

export default Status500;

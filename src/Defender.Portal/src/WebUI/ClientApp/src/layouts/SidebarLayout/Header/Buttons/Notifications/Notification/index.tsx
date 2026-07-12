import {
  Box,
  Typography
} from '@mui/material';

const Notification = (props: any) => {
  return (
    <>
      <Box
        sx={{
          flex: "1",
          pb: 1
        }}>
        <Box
          sx={{
            display: "flex",
            justifyContent: "space-between"
          }}>
          <Typography sx={{ fontWeight: 'bold' }}>
            {props.topic}
          </Typography>
        </Box>
        <Typography
          component="span"
          variant="body2"
          sx={{
            color: "text.secondary"
          }}
        >
          {' '}
          {props.body}
        </Typography>
      </Box>
    </>
  );
}

export default Notification;

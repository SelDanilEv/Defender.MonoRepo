import Box from "@mui/material/Box";
import LinearProgress from "@mui/material/LinearProgress";
import { connect } from "react-redux";
import type { RootState } from "src/state/store";

interface LoadingBarProps {
  isLoading: boolean;
}

const LoadingBar = ({ isLoading }: LoadingBarProps) => {
  return (
    <Box
      sx={{
        position: "fixed",
        top: 0,
        zIndex: 10000,
        width: "100%",
        height: "5px",
      }}
    >
      {isLoading && <LinearProgress />}
    </Box>
  );
};

const mapStateToProps = (state: RootState): LoadingBarProps => {
  return {
    isLoading: state.loading.loading,
  };
};

export default connect(mapStateToProps)(LoadingBar);

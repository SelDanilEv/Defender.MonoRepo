import React, { ErrorInfo, ReactNode } from "react";
import {
  Alert,
  Box,
  Button,
  Card,
  CardActions,
  CardContent,
  Stack,
  Typography,
} from "@mui/material";

interface ErrorBoundaryFallbackProps {
  description: string;
  onRetry: () => void;
  title: string;
}

interface ErrorBoundaryProps {
  children: ReactNode;
  fallbackDescription?: string;
  fallbackTitle?: string;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
  resetKeys?: unknown[];
}

interface ErrorBoundaryState {
  error: Error | null;
}

const haveResetKeysChanged = (
  previousResetKeys: unknown[] = [],
  nextResetKeys: unknown[] = []
) => {
  if (previousResetKeys.length !== nextResetKeys.length) {
    return true;
  }

  return previousResetKeys.some((item, index) => item !== nextResetKeys[index]);
};

const ErrorBoundaryFallback = ({
  description,
  onRetry,
  title,
}: ErrorBoundaryFallbackProps) => {
  const handleReload = () => {
    window.location.reload();
  };

  return (
    <Box
      display="flex"
      justifyContent="center"
      alignItems="center"
      minHeight="50vh"
      px={2}
      py={4}
    >
      <Card sx={{ maxWidth: 560, width: "100%" }}>
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="h4">{title}</Typography>
            <Typography color="text.secondary">
              {description}
            </Typography>
            <Alert severity="error">
              The page hit an unexpected client-side error.
            </Alert>
          </Stack>
        </CardContent>
        <CardActions sx={{ px: 2, pb: 2 }}>
          <Button variant="contained" onClick={onRetry}>
            Try again
          </Button>
          <Button variant="outlined" onClick={handleReload}>
            Reload page
          </Button>
        </CardActions>
      </Card>
    </Box>
  );
};

class ErrorBoundary extends React.Component<
  ErrorBoundaryProps,
  ErrorBoundaryState
> {
  public state: ErrorBoundaryState = {
    error: null,
  };

  public static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return {
      error,
    };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    if (this.props.onError) {
      this.props.onError(error, errorInfo);
      return;
    }

    // Keep a default reporting hook until a real frontend telemetry sink exists.
    console.error("Unhandled UI error", error, errorInfo);
  }

  public componentDidUpdate(previousProps: ErrorBoundaryProps) {
    const { error } = this.state;

    if (
      error &&
      haveResetKeysChanged(previousProps.resetKeys, this.props.resetKeys)
    ) {
      this.resetErrorBoundary();
    }
  }

  private resetErrorBoundary = () => {
    this.setState({
      error: null,
    });
  };

  public render() {
    if (this.state.error) {
      return (
        <ErrorBoundaryFallback
          title={this.props.fallbackTitle ?? "Something went wrong"}
          description={
            this.props.fallbackDescription ??
            "Try this page again. If the problem persists, reload the application."
          }
          onRetry={this.resetErrorBoundary}
        />
      );
    }

    return this.props.children;
  }
}

export { haveResetKeysChanged };
export default ErrorBoundary;

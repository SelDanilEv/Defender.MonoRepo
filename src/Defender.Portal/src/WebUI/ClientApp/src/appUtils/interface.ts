import { Theme } from "@mui/material";
import { useNavigate } from "react-router-dom";

export default interface IUtils {
  searchParams: URLSearchParams;
  react: {
    navigate: ReturnType<typeof useNavigate>;
    locationState: <T>(element: string) => T;
    theme: Theme;
  };
  t: (key: string) => string;
  log: (...values: unknown[]) => void;
  debug: (value: unknown) => void;
  e: (errorCode: string) => void;
  isMobile: boolean;
  isLargeScreen: boolean;
}

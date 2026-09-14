import { useLocation } from "react-router";

import VehiclesPage from "./Vehicles";
import VehicleOverviewPage from "./VehicleOverview";
import MaintenancePage from "./Maintenance";
import HistoryPage from "./History";
import InsurancePage from "./Insurance";

export default function MyGaragePage() {
  const { pathname } = useLocation();

  if (pathname.endsWith("/maintenance")) return <MaintenancePage />;
  if (pathname.endsWith("/history")) return <HistoryPage />;
  if (pathname.endsWith("/insurance")) return <InsurancePage />;
  if (/\/vehicles\/[^/]+$/.test(pathname)) return <VehicleOverviewPage />;
  return <VehiclesPage />;
}

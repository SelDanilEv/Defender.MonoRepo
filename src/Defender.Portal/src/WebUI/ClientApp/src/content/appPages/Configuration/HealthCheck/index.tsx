import { Grid, ListItem, ListItemText } from "@mui/material";
import { useEffect, useRef, useState } from "react";

import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import PendingStatusLabel from "src/components/Label/StatusLabels/Pending";
import SuccessStatusLabel from "src/components/Label/StatusLabels/Success";
import ErrorStatusLabel from "src/components/Label/StatusLabels/Error";
import apiUrls from "src/api/apiUrls";
import useUtils from "src/appUtils";

const HealthCheck = (props: any) => {
  const u = useUtils();
  const utilsRef = useRef(u);
  utilsRef.current = u;

  const theme = u.react.theme;

  const [healthCheck, setHealthCheck]: any = useState();

  useEffect(() => {
    APICallWrapper({
      url: apiUrls.home.healthcheck,
      options: {
        method: "GET",
      },
      utils: utilsRef.current,
      onSuccess: async (response) => {
        setHealthCheck(true);
      },
      onFailure: async (response) => {
        setHealthCheck(false);
      },
    });
  }, []);

  const isHealthy = () => {
    switch (healthCheck) {
      case undefined:
        return <PendingStatusLabel text={u.t("Pending")} />;
      case true:
        return <SuccessStatusLabel text={u.t("Healthy")} />;
      case false:
        return <ErrorStatusLabel text={u.t("Unhealthy")} />;
    }
  };

  return (
    <ListItem sx={{ p: 3 }} key="HealthCheck">
      <ListItemText
        primary={u.t("configuration_page__api_status")}
        slotProps={{
          primary: {
            variant: "h5",
            gutterBottom: true,
            sx: { fontSize: theme.typography.pxToRem(15) },
          }
        }}
      />
      <Grid
        size={{
          xs: 12,
          sm: 8,
          md: 9
        }}>
        {isHealthy()}
      </Grid>
    </ListItem>
  );
};

export default HealthCheck;

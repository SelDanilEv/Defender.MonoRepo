import {
  Divider,
  Grid,
  ListItem,
  ListItemText,
  MenuItem,
  TextField,
} from "@mui/material";
import { useEffect, useRef, useState } from "react";

import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import PendingStatusLabel from "src/components/Label/StatusLabels/Pending";
import apiUrls from "src/api/apiUrls";
import useUtils from "src/appUtils";

const Configuration = (props: any) => {
  const u = useUtils();
  const utilsRef = useRef(u);
  utilsRef.current = u;

  const theme = u.react.theme;

  const [configuration, setConfiguration]: any = useState();

  useEffect(() => {
    APICallWrapper({
      url: apiUrls.home.configuration,
      options: {
        method: "GET",
      },
      utils: utilsRef.current,
      onSuccess: async (response) => {
        setConfiguration(await response.json());
      },
    });
  }, []);

  const renderConfiguration = () => {
    if (!configuration) {
      return (
        <Grid
          sx={{
            textAlign: "center"
          }}
          size={{
            xs: 12,
            sm: 12,
            md: 12
          }}>
          <PendingStatusLabel text={u.t("Pending")} />
        </Grid>
      );
    }

    let envVariables = Object.keys(configuration);

    if (envVariables.length == 0) {
      return (
        <Grid
          sx={{
            textAlign: "center"
          }}
          size={{
            xs: 12,
            sm: 12,
            md: 12
          }}>
          <PendingStatusLabel text={u.t("NoData")} />
        </Grid>
      );
    }

    return envVariables.map((option) => (
      <Grid container key={option} size={12}>
        <Grid size={4}>
          {option}
        </Grid>
        <Grid size={8}>
          {configuration[option]}
        </Grid>
        <Grid
          sx={{
            margin: 1
          }}
          size={12}>
          <Divider />
        </Grid>
      </Grid>
    ));
  };

  const configurationLevels = [
    {
      value: "Hide",
      label: "Hide",
    },
    {
      value: "Admin",
      label: "Admin",
    },
    {
      value: "All",
      label: "All",
    },
  ];

  const [configurationLevel, setConfigurationLevel] = useState("Hide");

  const handleChange = (event) => {
    APICallWrapper({
      url: `${apiUrls.home.configuration}?configurationLevel=${event.target.value}`,
      options: {
        method: "GET",
      },
      utils: u,
      onSuccess: async (response) => {
        setConfiguration(await response.json());
      },
    });

    setConfigurationLevel(event.target.value);
  };

  return (
    <ListItem sx={{ p: 3 }} key="Configuration">
      <Grid container>
        <Grid
          size={{
            xs: 6,
            sm: 7,
            md: 8
          }}>
          <ListItemText
            primary={u.t("configuration_page__configuration")}
            slotProps={{
              primary: {
                variant: "h5",
                gutterBottom: true,
                sx: { fontSize: theme.typography.pxToRem(15) },
              }
            }}
          />
        </Grid>

        <Grid
          size={{
            xs: 6,
            sm: 5,
            md: 4
          }}>
          <TextField
            select
            fullWidth
            label={u.t("configuration_page__configuration_level")}
            value={configurationLevel}
            onChange={handleChange}
          >
            {configurationLevels.map((option) => (
              <MenuItem key={option.value} value={option.value}>
                {option.label}
              </MenuItem>
            ))}
          </TextField>
        </Grid>

        <Grid container sx={{ mt: 3 }}>
          {renderConfiguration()}
        </Grid>
      </Grid>
    </ListItem>
  );
};

export default Configuration;

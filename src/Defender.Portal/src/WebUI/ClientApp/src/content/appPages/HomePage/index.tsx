import { connect } from "react-redux";

import useUtils from "src/appUtils";
import WalletAccountsInfo from "../Shared/WalletAccountsInfo";
import {
  Box,
  Card,
  CardActionArea,
  CardContent,
  Grid,
  Typography,
} from "@mui/material";
import { Link } from "react-router-dom";
import StackedLineChartIcon from "@mui/icons-material/StackedLineChart";
import CloudUploadIcon from "@mui/icons-material/CloudUpload";
import LocalActivityIcon from "@mui/icons-material/LocalActivity";
import SportsEsportsIcon from "@mui/icons-material/SportsEsports";
import HeadsetMicIcon from "@mui/icons-material/HeadsetMic";
import RestaurantMenuIcon from "@mui/icons-material/RestaurantMenu";

const quickLinks = [
  {
    key: "cloud",
    labelKey: "home:quick_menu_cloud",
    href: "https://cloud.coded-by-danil.dev/",
    icon: CloudUploadIcon,
  },
  {
    key: "diagram",
    labelKey: "home:quick_menu_diagram",
    to: "/budget-tracker/diagram",
    icon: StackedLineChartIcon,
  },
  {
    key: "food_adviser",
    labelKey: "home:quick_menu_food_adviser",
    to: "/food-adviser",
    icon: RestaurantMenuIcon,
  },
  {
    key: "lottery",
    labelKey: "home:quick_menu_lottery",
    to: "/games/lottery",
    icon: LocalActivityIcon,
  },
  {
    key: "web_games",
    labelKey: "home:quick_menu_web_games",
    href: "https://games.coded-by-danil.dev/",
    icon: SportsEsportsIcon,
  },
  {
    key: "smart_note",
    labelKey: "home:quick_menu_smart_note",
    href: "https://chat.coded-by-danil.dev/",
    icon: HeadsetMicIcon,
  },
];

const HomePage = (props: any) => {
  const u = useUtils();

  return (
    <>
      <Grid container spacing={2}>
        <Grid item xs={12}>
          <WalletAccountsInfo></WalletAccountsInfo>
        </Grid>

        {quickLinks.map((item) => {
          const Icon = item.icon;

          return (
            <Grid item xs={12} sm={6} md={2} key={item.key}>
              <Card sx={{ height: { xs: 84, sm: 92 } }}>
                <CardActionArea
                  component={item.to ? Link : "a"}
                  {...(item.to
                    ? { to: item.to }
                    : {
                        href: item.href,
                        target: "_blank",
                        rel: "noopener noreferrer",
                      })}
                  sx={{ height: "100%" }}
                >
                  <CardContent
                    sx={{
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "center",
                      gap: 1,
                      height: "100%",
                      px: 1.5,
                    }}
                  >
                    <Typography
                      variant="h5"
                      component="div"
                      align="center"
                      sx={{ lineHeight: 1.2 }}
                    >
                      {u.t(item.labelKey)}
                    </Typography>
                    <Icon sx={{ fontSize: 22, flexShrink: 0 }} />
                  </CardContent>
                </CardActionArea>
              </Card>
            </Grid>
          );
        })}
      </Grid>
    </>
  );
};

const mapStateToProps = (state: any) => {
  return {
    currentUser: state.session.user,
  };
};

export default connect(mapStateToProps)(HomePage);

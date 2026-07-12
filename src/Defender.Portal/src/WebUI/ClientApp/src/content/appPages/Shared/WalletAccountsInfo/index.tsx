import {
  Box,
  Typography,
  Grid,
  Divider,
  Card,
  CardHeader,
  CardActionArea,
} from "@mui/material";
import { connect } from "react-redux";

import AddTwoToneIcon from "@mui/icons-material/AddTwoTone";

import useUtils from "src/appUtils";
import { useEffect, useRef, useState } from "react";
import { WalletInfo } from "src/models/banking/WalletInfo";
import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import apiUrls from "src/api/apiUrls";
import {
  AvatarAddWrapper,
  CardAddAction,
  CardCc,
  CardLogo,
} from "./styledComponents";
import CustomDialog from "src/components/Dialog";
import CreateAccountDialogBody from "./CreateAccountDialogBody";
import { BankingSupportedCurrencies } from "src/consts/SupportedCurrencies";
import RechargeOrRefundDialogBody from "./RechargeOrRefundDialogBody";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";

import HomeIcon from "@mui/icons-material/Home";
import AccountBalanceIcon from "@mui/icons-material/AccountBalance";
import CachedIcon from "@mui/icons-material/Cached";
import { updateWalletInfo } from "src/actions/walletActions";
import CurrencySymbolsMap from "src/consts/CurrencySymbolsMap";

const WalletAccountsInfo = (props: any) => {
  const u = useUtils();
  const utilsRef = useRef(u);
  utilsRef.current = u;
  const updateUIWalletInfoRef = useRef<(walletInfo: WalletInfo) => void>(() => undefined);

  useEffect(() => {
    let isMounted = true;

    const updateWalletInfo = () => {
      APICallWrapper({
        url: `${apiUrls.banking.walletInfo}`,
        options: {
          method: "GET",
          headers: {
            "Content-Type": "application/json",
          },
          cache: "default",
        },
        utils: utilsRef.current,
        onSuccess: async (response) => {
          const walletInfo: WalletInfo = await response.json();
          if (isMounted) {
            // Check if the component is still mounted
            updateUIWalletInfoRef.current(walletInfo);
          }
        },
        showError: false,
      });
    };

    updateWalletInfo();

    return () => {
      isMounted = false; // Set isMounted to false when the component unmounts
    };
  }, []);

  const [wallet, setWallet] = useState<WalletInfo>(props.walletInfo);

  const [showCreateAccountDialog, setShowCreateAccountDialog] =
    useState<boolean>(false);
  const [
    showRechargeOrRefundAccountDialog,
    setShowRechargeOrRefundAccountDialog,
  ] = useState<boolean>(false);

  const updateWalletInfo = () => {
    APICallWrapper({
      url: `${apiUrls.banking.walletInfo}`,
      options: {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
        cache: "default",
      },
      utils: u,
      onSuccess: async (response) => {
        const walletInfo: WalletInfo = await response.json();
        updateUIWalletInfo(walletInfo);
      },
      showError: false,
    });
  };

  const updateUIWalletInfo = (walletInfo: WalletInfo) => {
    props.updateWalletInfo(walletInfo);
    setWallet(walletInfo);
  };
  updateUIWalletInfoRef.current = updateUIWalletInfo;

  const displayAccounts = () => {
    let result = [];

    if (wallet.walletNumber) {
      for (const account of wallet.currencyAccounts) {
        result.push(
          <Grid
            key={account.currency}
            size={{
              xs: 12,
              sm: 6,
              md: 2
            }}>
            <CardCc>
              <Box
                sx={{
                  display: "flex",
                  alignItems: "center"
                }}>
                <CardLogo>{account.currency}</CardLogo>
                <Box
                  sx={{
                    marginLeft: "auto",
                    marginRight: { xs: "1em", sm: "1.25em" }
                  }}>
                  <Typography
                    variant="h5"
                    sx={{
                      fontWeight: "normal",
                      whiteSpace: "nowrap"
                    }}>
                    {account.balance / 100 +
                      CurrencySymbolsMap[account.currency]}
                  </Typography>
                </Box>
              </Box>
            </CardCc>
          </Grid>
        );
      }

      if (result.length < BankingSupportedCurrencies.length)
        result.push(
          <Grid
            key={-1}
            size={{
              xs: 12,
              sm: 6,
              md: 2
            }}>
            <CardAddAction>
              <CardActionArea
                sx={{
                  width: "100%",
                  height: "100%",
                  minHeight: "inherit",
                  px: 1,
                  py: 0,
                  display: "flex",
                  flexDirection: "row",
                  alignItems: "center",
                  justifyContent: "center",
                  gap: 0.75,
                  textAlign: "center",
                }}
                onClick={createNewAccount}
              >
                <Box
                  sx={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center"
                  }}>
                  <AvatarAddWrapper>
                    <AddTwoToneIcon sx={{ fontSize: 18 }} />
                  </AvatarAddWrapper>
                </Box>
                <Typography
                  align="center"
                  sx={{ fontSize: "0.82rem", lineHeight: 1.05 }}
                >
                  {u.t("banking_page__wallet_button_create_account")}
                </Typography>
              </CardActionArea>
            </CardAddAction>
          </Grid>
        );
    }

    return result;
  };

  const createNewAccount = () => {
    setShowCreateAccountDialog(true);
  };

  const renderNavigationButton = () => {
    if (window.location.pathname === "/home") {
      return (
        <LockedButton
          startIcon={<AccountBalanceIcon />}
          variant="outlined"
          color="primary"
          sx={{ fontSize: "1.1em" }}
          onClick={() => u.react.navigate("/banking")}
        >
          {u.t("banking_page__wallet_button_open_banking")}
        </LockedButton>
      );
    } else {
      return (
        <LockedButton
          startIcon={<HomeIcon />}
          variant="outlined"
          color="primary"
          sx={{ fontSize: "1.1em" }}
          onClick={() => u.react.navigate("/home")}
        >
          {u.t("banking_page__wallet_button_home")}
        </LockedButton>
      );
    }
  };

  return (
    <>
      <Card>
        <CardHeader
          title={
            u.t("banking_page__wallet_title") +
            " " +
            (wallet.walletNumber ?? "******")
          }
          action={
            <Box
              sx={{
                display: "flex",

                flexDirection: {
                  xs: "column",
                  sm: "row",
                },

                gap: 1
              }}>
              <Box
                sx={{
                  display: "flex",

                  flexDirection: {
                    xs: "row",
                  },

                  gap: 1
                }}>
                {renderNavigationButton()}
                <LockedButton variant="outlined" onClick={updateWalletInfo}>
                  <CachedIcon />
                </LockedButton>
              </Box>
              <LockedButton
                variant="outlined"
                color="primary"
                sx={{ fontSize: "1.1em" }}
                onClick={() => setShowRechargeOrRefundAccountDialog(true)}
              >
                {u.t("banking_page__wallet_button_recharge_or_refund")}
              </LockedButton>
            </Box>
          }
          slotProps={{
            title: {
              style: { fontSize: u.isMobile ? "1.15rem" : "1.35rem" },
            }
          }}
        />
        <Divider />
        <Grid container spacing={0.5}>
          {displayAccounts()}
        </Grid>
      </Card>
      <CustomDialog
        title={u.t("banking_page__wallet_dialog_title_create_account")}
        open={showCreateAccountDialog}
        onClose={() => setShowCreateAccountDialog(false)}
        children={
          <CreateAccountDialogBody
            updateWallet={updateUIWalletInfo}
            closeDialog={() => setShowCreateAccountDialog(false)}
          />
        }
      />
      <CustomDialog
        title={u.t("banking_page__wallet_dialog_title_recharge_or_refund")}
        open={showRechargeOrRefundAccountDialog}
        onClose={() => setShowRechargeOrRefundAccountDialog(false)}
        children={<RechargeOrRefundDialogBody />}
      />
    </>
  );
};

const mapDispatchToProps = (dispatch: any) => {
  return {
    updateWalletInfo: (wallet) => {
      dispatch(updateWalletInfo(wallet));
    },
  };
};

const mapStateToProps = (state: any) => {
  return {
    walletInfo: state.wallet,
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(WalletAccountsInfo);

import { Grid, Divider, Card, CardHeader, TextField } from "@mui/material";

import useUtils from "src/appUtils";
import { useEffect, useState } from "react";

import { AdminSearchUserRequest } from "src/models/requests/admin/searchUser/AdminSearchUserRequest";
import {
  EmailMaskRegex,
  EmailRegex,
  GuidMaskRegex,
  GuidRegex,
  WalletNumberMaskRegex,
  WalletNumberRegex,
} from "src/consts/Regexes";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import ParamsObjectBuilder from "src/helpers/ParamsObjectBuilder";

interface SearchUserPannelProps {
  searchFullUserInfo: (fullUserInfo: AdminSearchUserRequest) => void;
}

const SearchUserPannel = (props: SearchUserPannelProps) => {
  const u = useUtils();

  const { searchFullUserInfo } = props;

  const [searchRequest, setSearchRequest] = useState<AdminSearchUserRequest>({
    userId: "",
    email: "",
    walletNumber: 0,
  });

  const [isSearchAllowed, setIsSearchAllowed] = useState<boolean>(true);

  useEffect(() => {
    setIsSearchAllowed(
      GuidRegex.test(searchRequest.userId) ||
        EmailRegex.test(searchRequest.email) ||
        WalletNumberRegex.test(searchRequest.walletNumber.toString())
    );
  }, [searchRequest]);

  const searchParams = ParamsObjectBuilder.Build(u, searchRequest);

  const UpdateRequest = (event) => {
    const { name, value } = event.target;

    setSearchRequest((prevState) => {
      if (
        name === searchParams.userId &&
        value !== "" &&
        !GuidMaskRegex.test(value)
      ) {
        return prevState;
      }

      if (
        name === searchParams.walletNumber &&
        value !== "" &&
        !WalletNumberMaskRegex.test(value)
      ) {
        return prevState;
      }

      if (
        name === searchParams.email &&
        value !== "" &&
        !EmailMaskRegex.test(value)
      ) {
        return prevState;
      }

      return { ...prevState, [name]: value };
    });
  };

  const prepareRequest = () => {
    if (GuidRegex.test(searchRequest.userId)) {
      return { userId: searchRequest.userId };
    }
    if (WalletNumberRegex.test(searchRequest.walletNumber.toString())) {
      return { walletNumber: searchRequest.walletNumber };
    }
    if (EmailRegex.test(searchRequest.email)) {
      return { email: searchRequest.email };
    }

    return null;
  };

  const handleSearch = () => {
    var request = prepareRequest() as AdminSearchUserRequest;
    if (!request) return;

    searchFullUserInfo(request);
  };

  return (
    <>
      <Card>
        <CardHeader
          title={u.t("admin_users_page__search_title")}
          slotProps={{
            title: {
              style: { fontSize: u.isMobile ? "1.5em" : "2em" },
            }
          }}
        />
        <Divider />
        <Grid container spacing={3} sx={{
          p: 2
        }}>
          <Grid
            size={{
              xs: 12,
              sm: 9
            }}>
            <TextField
              name={searchParams.userId}
              label={u.t("admin_users_page__search_user_id_label")}
              value={searchRequest.userId}
              onChange={UpdateRequest}
              variant="outlined"
              fullWidth
              slotProps={{
                input: { style: { fontSize: "1.5em" } }
              }}
            />
          </Grid>
          <Grid
            size={{
              xs: 5,
              sm: 3
            }}>
            <TextField
              name={searchParams.walletNumber}
              label={u.t("admin_users_page__search_wallet_number_label")}
              value={
                searchRequest.walletNumber ? searchRequest.walletNumber : ""
              }
              onChange={UpdateRequest}
              variant="outlined"
              fullWidth
              slotProps={{
                input: { style: { fontSize: "1.5em" } }
              }}
            />
          </Grid>
          <Grid
            size={{
              xs: 7,
              sm: 8
            }}>
            <TextField
              name={searchParams.email}
              label={u.t("admin_users_page__search_email_label")}
              value={searchRequest.email}
              onChange={UpdateRequest}
              variant="outlined"
              fullWidth
              slotProps={{
                input: { style: { fontSize: "1.5em" } }
              }}
            />
          </Grid>
          <Grid
            size={{
              xs: 12,
              sm: 4
            }}>
            <LockedButton
              disabled={!isSearchAllowed}
              onClick={handleSearch}
              variant="outlined"
              fullWidth
              sx={{ height: { xs: "55px", sm: "100%" } }}
            >
              {u.t("admin_users_page__search_button_search")}
            </LockedButton>
          </Grid>
        </Grid>
      </Card>
    </>
  );
};

export default SearchUserPannel;

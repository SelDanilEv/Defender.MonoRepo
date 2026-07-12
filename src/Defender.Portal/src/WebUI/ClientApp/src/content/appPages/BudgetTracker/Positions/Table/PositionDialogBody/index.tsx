import { Divider, Grid, MenuItem } from "@mui/material";
import { useEffect, useState } from "react";
import { connect } from "react-redux";

import useUtils from "src/appUtils";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import LockedChipList from "src/components/LockedComponents/LockedChipList/LockedChipList";
import LockedSelect from "src/components/LockedComponents/LockedSelect/LockedSelect";
import LockedTextField from "src/components/LockedComponents/LockedTextField/LockedTextField";
import ParamsObjectBuilder from "src/helpers/ParamsObjectBuilder";
import { BudgetPosition } from "src/models/budgetTracker/BudgetPositions";
import { BudgetTrackerAvailableCurrencies } from "src/models/shared/Currency";
import { DialogMode } from "src/models/shared/DialogMode";

import { CreatePosition, DeletePosition, UpdatePosition } from "./actions";

const HorizontalDivider = () => {
  return (
    <Grid style={{ paddingTop: 5 }} size={12}>
      <Divider />
    </Grid>
  );
};

const gridItem = {
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
} as any;

interface PositionDialogBodyProps {
  dialogMode: DialogMode;
  inputModel: BudgetPosition;
  closeDialog: () => void;
}

const PositionDialogBody = (props: PositionDialogBodyProps) => {
  const u = useUtils();

  const { dialogMode, inputModel, closeDialog } = props;

  const [model, setModel] = useState<BudgetPosition>(
    inputModel || ({} as BudgetPosition)
  );

  const isModelValid = model && model.name;

  useEffect(() => {
    setModel(inputModel);
  }, [inputModel]);

  const onChipsChange = (tags: string[]) => {
    setModel((position) => {
      return { ...position, tags: tags };
    });
  };

  if (dialogMode === DialogMode.Hide || !inputModel) return <></>;

  const modelParams = ParamsObjectBuilder.Build(u, model);

  const handleUpdateModel = (event) => {
    const { name, type } = event.target;
    let value = type === "checkbox" ? event.target.checked : event.target.value;

    setModel((prevState) => {
      return { ...prevState, [name]: value };
    });
  };

  const renderActionButton = () => {
    switch (dialogMode) {
      case DialogMode.Create:
        return (
          <LockedButton
            fullWidth={u.isMobile}
            disabled={!isModelValid}
            onClick={() => {
              CreatePosition(model, u, closeDialog);
            }}
            variant="outlined"
          >
            {u.t("Create")}
          </LockedButton>
        );
      case DialogMode.Update:
        return (
          <LockedButton
            fullWidth={u.isMobile}
            disabled={!isModelValid}
            onClick={() => {
              UpdatePosition(model, u, closeDialog);
            }}
            variant="outlined"
          >
            {u.t("Update")}
          </LockedButton>
        );
      case DialogMode.Delete:
        return (
          <LockedButton
            color="error"
            fullWidth={u.isMobile}
            onClick={() => {
              DeletePosition(model, u, closeDialog);
            }}
            variant="outlined"
          >
            {u.t("Delete")}
          </LockedButton>
        );
    }

    return <></>;
  };

  return (
    <Grid
      container
      spacing={2}
      sx={{
        p: 2,
        justifyContent: "center",
        alignContent: "center",
        fontSize: "1.3em"
      }}>
      {model && (
        <>
          <Grid
            style={gridItem}
            size={{
              xs: 12,
              sm: 7
            }}>
            <LockedTextField
              fullWidth
              label={u.t("budgetTracker:positions_table_name_column")}
              name={modelParams.name}
              value={model.name}
              onChange={handleUpdateModel}
              variant="standard"
            />
          </Grid>

          <Grid
            style={gridItem}
            size={{
              xs: 6,
              sm: 3
            }}>
            <LockedSelect
              name={modelParams.currency}
              value={model.currency}
              onChange={handleUpdateModel}
            >
              {BudgetTrackerAvailableCurrencies.map((currency) => (
                <MenuItem key={currency} value={currency}>
                  {currency}
                </MenuItem>
              ))}
            </LockedSelect>
          </Grid>

          <Grid
            size={{
              xs: 6,
              sm: 2
            }}>
            <LockedTextField
              fullWidth
              label={u.t("budgetTracker:positions_dialog_priority_label")}
              type="number"
              name={modelParams.orderPriority}
              value={model.orderPriority}
              onChange={handleUpdateModel}
              variant="standard"
            />
          </Grid>

          {HorizontalDivider()}

          <Grid
            size={{
              xs: 12,
              sm: 12
            }}>
            <LockedChipList
              fullWidth
              label={u.t("budgetTracker:positions_dialog_tags_label")}
              variant="standard"
              initialChips={inputModel.tags}
              onChange={onChipsChange}
            />
          </Grid>

          {HorizontalDivider()}

          <Grid
            style={gridItem}
            size={{
              xs: 12,
              sm: 12
            }}>
            {renderActionButton()}
          </Grid>
        </>
      )}
    </Grid>
  );
};

const mapStateToProps = (state: any) => {
  return {
    currentLanguage: state.session.language,
  };
};

export default connect(mapStateToProps)(PositionDialogBody);

import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { HealthDateRangeSelection } from "./dateRange";
import HealthDateRangeSelector from "./HealthDateRangeSelector";

vi.mock("src/appUtils", () => ({
  default: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@mui/x-date-pickers/DateTimePicker", () => ({
  DateTimePicker: ({
    label,
    value,
    onChange,
    disabled,
  }: {
    label: string;
    value: Date | null;
    onChange: (value: Date | null) => void;
    disabled?: boolean;
  }) => (
    <input
      aria-label={label}
      disabled={disabled}
      value={value?.toISOString().slice(0, 16) ?? ""}
      onInput={(event) => {
        const input = event.target as HTMLInputElement;
        onChange(
          input.value
            ? new Date(`${input.value}:00.000Z`)
            : null
        );
      }}
    />
  ),
}));

const customSelection = {
  kind: "custom" as const,
  from: new Date("2026-06-18T08:30:00.000Z"),
  to: new Date("2026-06-19T17:00:00.000Z"),
};

const choosePreset = (label: string) => {
  fireEvent.mouseDown(screen.getByRole("combobox"));
  fireEvent.click(screen.getByRole("option", { name: label }));
};

describe("HealthDateRangeSelector", () => {
  test("WhenPresetChanges_EmitsTheSelectedPreset", () => {
    const onChange = vi.fn();

    render(
      <HealthDateRangeSelector
        value={{ kind: "preset", preset: "week" }}
        onChange={onChange}
      />
    );

    choosePreset("healthCare:range_month");

    expect(onChange).toHaveBeenCalledWith({ kind: "preset", preset: "month" });
  });

  test("WhenCustomIsSelected_RendersBothDateTimeControlsAndEmitsValidRange", async () => {
    const onChange = vi.fn();

    const { rerender } = render(
      <HealthDateRangeSelector
        value={{ kind: "preset", preset: "week" }}
        onChange={onChange}
      />
    );

    choosePreset("healthCare:range_custom");

    rerender(
      <HealthDateRangeSelector value={customSelection} onChange={onChange} />
    );

    const from = screen.getByLabelText("healthCare:custom_range_from");
    const to = screen.getByLabelText("healthCare:custom_range_to");
    expect(from).not.toBeNull();
    expect(to).not.toBeNull();

    await waitFor(() => {
      expect((from as HTMLInputElement).value).toBe("2026-06-18T08:30");
      expect((to as HTMLInputElement).value).toBe("2026-06-19T17:00");
    });

    fireEvent.input(from, { target: { value: "2026-06-18T08:30" } });
    fireEvent.input(to, { target: { value: "2026-06-19T17:00" } });

    expect(onChange).toHaveBeenCalledWith(customSelection);
  });

  test("WhenCustomDatesAreReversed_ShowsValidationAndDoesNotCommitInvalidRange", () => {
    const onChange = vi.fn();

    render(
      <HealthDateRangeSelector value={customSelection} onChange={onChange} />
    );

    fireEvent.input(screen.getByLabelText("healthCare:custom_range_from"), {
      target: { value: "2026-06-20T08:30" },
    });

    expect(screen.getByText("healthCare:custom_range_invalid_order")).not.toBeNull();
    expect(onChange).not.toHaveBeenCalled();
  });

  test("WhenApplyIsDisabled_DisablesTheOptionalApplyActionAndInputs", () => {
    const onChange = vi.fn();
    const onApply = vi.fn();

    render(
      <HealthDateRangeSelector
        value={customSelection as HealthDateRangeSelection}
        onChange={onChange}
        disabled
        showApply
        onApply={onApply}
        applyLabel="healthCare:update_shared_range"
        applyDisabled
      />
    );

    expect(screen.getByRole("button", { name: "healthCare:update_shared_range" })).toHaveProperty("disabled", true);
    expect(screen.getByLabelText("healthCare:custom_range_from")).toHaveProperty("disabled", true);
    expect(screen.getByLabelText("healthCare:custom_range_to")).toHaveProperty("disabled", true);
  });
});

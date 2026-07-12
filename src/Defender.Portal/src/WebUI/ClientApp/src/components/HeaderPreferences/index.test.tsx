import HeaderPreferences from ".";

describe("HeaderPreferences", () => {
  test("HeaderPreferences_WhenRendered_SeparatesThemeAndLanguageControls", () => {
    const preferences = HeaderPreferences();

    expect(preferences.props.direction).toBe("row");
    expect(preferences.props.alignItems).toBe("center");
    expect(preferences.props.spacing).toBe(1);
    expect(preferences.props.children).toHaveLength(2);
  });
});

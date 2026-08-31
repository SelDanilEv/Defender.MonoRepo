import { readFileSync } from "node:fs";
import { describe, expect, test } from "vitest";

import apiUrls from "src/api/apiUrls";
import enBudgetTracker from "src/localization/en/budgetTracker.json";
import ruBudgetTracker from "src/localization/ru/budgetTracker.json";
import enSidebarMenu from "src/localization/en/sidebar_menu.json";
import ruSidebarMenu from "src/localization/ru/sidebar_menu.json";

const source = (path: string) => readFileSync(new URL(path, import.meta.url), "utf8");

describe("regular expenses Portal contracts", () => {
  test("ApiUrls_WhenRegularExpensesRegistered_KeepDedicatedBffRouteGroup", () => {
    expect(apiUrls.budgetTracker.regularExpenses).toEqual({
      getExpenses: "/api/budgetTracker/regular-expenses/expenses",
      expense: "/api/budgetTracker/regular-expenses/expense",
      getReviews: "/api/budgetTracker/regular-expenses/reviews",
      getReviewsByDateRange: "/api/budgetTracker/regular-expenses/reviews/by-date-range",
      getReviewTemplate: "/api/budgetTracker/regular-expenses/review/template",
      review: "/api/budgetTracker/regular-expenses/review",
      diagramSetup: "/api/budgetTracker/regular-expenses/diagram-setup",
    });
  });

  test("SidebarLocalization_WhenBothLanguagesLoaded_LabelsBalanceAndRegularExpensesGroups", () => {
    expect(enSidebarMenu.group_budget_tracker_balance).toBe("Balance");
    expect(enSidebarMenu.group_budget_tracker_regular_expenses).toBe("Regular expenses");
    expect(ruSidebarMenu.group_budget_tracker_balance).toBe("Баланс");
    expect(ruSidebarMenu.group_budget_tracker_regular_expenses).toBe("Регулярные расходы");
    expect(enBudgetTracker.regular_expenses_chart_total).toBeTruthy();
    expect(ruBudgetTracker.regular_expenses_chart_total).toBeTruthy();
  });

  test("RegularExpensesPages_WhenSourceInspected_DoNotImportBalanceReduxState", () => {
    const pages = ["./Expenses/index.tsx", "./Reviews/index.tsx", "./Diagram/index.tsx"];
    pages.forEach((path) => {
      const content = source(path);
      expect(content).not.toMatch(/budgetTrackerActions|budgetTrackerSetupReducer|budgetTrackerGroupsReducer/);
    });
  });
});

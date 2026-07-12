# Header Preferences Spacing Design

## Goal

Keep theme toggle and language selector consistently separated in authenticated and welcome headers.

## Design

Create `HeaderPreferences`, a small MUI `Stack` owning horizontal alignment and `spacing={1}`. It renders existing `ThemeModeToggle` and `LanguageSwitcher` without changing either control. Both `SidebarLayout/Header` and `WelcomeLayout` consume this shared component.

## Verification

Add a focused source-level regression test proving both layouts use the shared component and spacing remains centralized. Run targeted Vitest, frontend build, then inspect login and calendar headers locally.


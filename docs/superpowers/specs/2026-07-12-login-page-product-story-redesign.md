# Login Page Product Story Redesign

## Goal

Replace the legacy centered login card with approved Concept A: a branded product-story split on laptop and a focused single-column login on mobile.

## Layout

- `md+`: 52/48 split, full-height story panel left and authentication panel right.
- Below `md`: hide the large story panel; show compact centered Defender brand above the form.
- Theme and language controls remain top-right at all widths.
- Other welcome routes retain their existing shared welcome layout.

## Authentication UX

- Full-width official-style `Continue with Google` action comes first.
- Divider separates federated and password login.
- Email/login and password use explicit labels and autocomplete attributes.
- Password visibility toggle, forgot-password link, contained primary sign-in action, and quiet create-account link remain available.
- Existing API calls, validation, loading locks, routing, localization, and theme persistence remain unchanged.

## Quality

- Use MUI components and existing shared controls.
- Add English and Russian copy.
- Verify component/layout tests, full frontend suite, build, keyboard semantics, console, and local visual layouts at 1440x900, 1024x768, 723x898, and 390x844.
- Commit, push, and deploy only after local visual result matches approved concept.

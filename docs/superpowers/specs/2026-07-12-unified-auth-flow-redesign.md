# Unified Authentication Flow Redesign

## Goal
Extend the Product Story design from login to create-account, reset-password, verification, and verify-email routes.

## Shared Shell
- `md+`: 52/48 branded story and content split.
- Below `md`: hide story panel; center an `80px` logo without adjacent Defender text.
- Theme and language controls remain top-right with 44px targets.
- Each route supplies title, description, story copy, and focused content.

## Route Content
- Login: current polished Google and password flow.
- Create: full-width Google action, divider, email/nickname/phone/password fields, password visibility, create action, login link.
- Reset: email request step followed by new-password/code step, preserving current state machine.
- Verification: email instructions, resend, and back action.
- Verify email: accessible pending/success/error state and back action.

## Verification
Preserve API behavior. Add shared-shell and form layout tests. Run full tests/build/lint, then visually inspect all routes in dark/light at laptop and mobile widths before commit, push, deploy, and live smoke testing.

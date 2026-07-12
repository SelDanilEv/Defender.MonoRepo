# Welcome Header Responsive Layout Design

## Goal

Prevent welcome-page theme and language controls from overlapping the centered logo.

## Design

Replace absolute positioning with a responsive MUI grid. At `sm` and wider, use `1fr auto 1fr`: logo occupies the centered column and preferences align to the end of the right column. On `xs`, use one column and place preferences below the logo with one spacing unit. Existing controls remain unchanged.

## Verification

Test exported layout tokens, run full frontend tests/build, and inspect local login at `723x898` plus desktop width.


This service is for users who want to keep track of their budget.

## Regular Expenses

Regular Expenses is independent from balance positions, reviews, groups, and balance diagram settings. Authenticated users can manage expense definitions under `api/RegularExpense`, publish one monthly snapshot per normalized month under `api/RegularExpenseReview`, and store chart settings under `api/RegularExpenseDiagramSetup`.

Definitions use integer minor-unit amounts. `Regular` and `Subscription` amounts contribute their full monthly amount; `Annual` contributes `Amount / 12m` without premature rounding. Review snapshots retain definition metadata and captured exchange rates, so later definition edits do not rewrite history.

All Regular Expenses reads and mutations are scoped to authenticated `UserId`. Review months and setup end months normalize to calendar day 1. Names must be nonblank and at most 200 characters; amounts are non-negative, and unknown type/currency values are rejected.

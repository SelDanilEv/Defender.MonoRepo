# Travel Calendar Shared Events Design

Date: 2026-07-06
Status: Approved architecture direction, pending written-spec review
Scope: Extend the travel calendar design so one event can belong to multiple platform users while each user still keeps a personal calendar shell.

## 1. Problem statement

The current travel calendar implementation path assumes one MongoDB aggregate per user with all travel events embedded inside that user's `TravelCalendar` document. That model works for a strictly personal calendar, but it breaks down once an event must appear in more than one user's calendar.

The new requirement is:

- each platform user has their own calendar;
- a user can add other platform users to an event;
- if user A invites user B, the event must appear in both users' calendars;
- ownership and edit permissions must stay predictable and conflict-safe.

The approved product rule is:

- the event owner edits shared event details;
- invited users can view the event and respond to the invitation;
- invited users do not edit shared event core fields.

## 2. Architecture options considered

### Option A. Separate canonical `TravelEvents` collection

Store personal calendar settings in `TravelCalendar`, but move all event records into a dedicated `TravelEvents` collection. Each event stores its owner plus a participant list with invitation statuses.

Pros:

- one source of truth for shared events;
- edits made by the owner immediately affect every participant view;
- no fan-out synchronization problem;
- event-level optimistic concurrency is straightforward;
- invitation lifecycle is explicit and testable.

Cons:

- requires refactoring from the currently implemented embedded-event model;
- calendar reads become a merge of personal settings plus shared events.

### Option B. Hybrid model: embedded personal events plus external shared events

Keep personal-only events embedded in `TravelCalendar`; store shared events in a separate collection.

Pros:

- smaller migration from the current implementation;
- personal-only scenarios still use the simpler document shape.

Cons:

- two event storage paths and two business-rule branches;
- duplicate read logic in backend and frontend;
- more corner cases for converting personal events into shared events later.

### Option C. Duplicate event copies into every participant calendar

Persist one copy of the event in each participant's calendar and synchronize them manually.

Pros:

- easiest to describe initially.

Cons:

- high risk of data drift;
- partial failures create inconsistent calendars;
- deletes, edits, and participant changes become fragile;
- version conflicts are much harder to reason about.

## 3. Recommended design

Option A is the recommended and approved direction.

`TravelCalendar` remains the personal calendar container. `TravelEvent` becomes a canonical shared resource. A calendar page is rendered from:

- one user-owned `TravelCalendar` document for preferences and packing items;
- all `TravelEvent` documents where the current user is either the owner or a participant.

This design preserves the existing product expectation that each user has a personal calendar while correctly supporting one event appearing in multiple user calendars without duplication.

## 4. Domain model

### 4.1 Personal calendar aggregate

`TravelCalendar` keeps only personal, user-scoped data:

- `Id`
- `UserId`
- `Version`
- `Theme`
- `BaseCity`
- `Currency`
- `SeasonStart`
- `SeasonEnd`
- `VehicleSettings`
- `PackingItems`
- `UpdatedAtUtc`

It no longer owns the `Events` collection.

### 4.2 Shared event aggregate

Create a new root aggregate `TravelEvent` in a dedicated MongoDB collection.

Proposed fields:

- `Id`
- `OwnerUserId`
- `Title`
- `Type`
- `StartDate`
- `EndDate`
- `IsMustVisit`
- `Hotel`
- `PointsOfInterest`
- `OtherCostPln`
- `DistanceKm`
- `Notes`
- `Participants`
- `CreatedAtUtc`
- `UpdatedAtUtc`
- `Version`

`Participants` is a collection of membership objects:

- `UserId`
- `DisplaySnapshot`
- `Status`
- `InvitedAtUtc`
- `RespondedAtUtc`

`DisplaySnapshot` should contain lightweight UI-friendly values such as nickname and avatar URL at invite time. This avoids hard dependency on a user lookup during every calendar read, while still allowing later refresh if product rules require it.

### 4.3 Invitation statuses

Use explicit participant statuses:

- `Pending`
- `Accepted`
- `Declined`

Optional future status, not needed now:

- `Removed`

The event appears in both calendars immediately after the invite. In the invited user's calendar it is marked as `Pending` until they respond.

### 4.4 Ownership and permissions

Owner permissions:

- create event;
- update shared event fields;
- add participants;
- remove participants;
- delete event.

Participant permissions:

- view event;
- accept invitation;
- decline invitation;
- leave accepted event.

Participant restrictions:

- cannot change title, dates, type, hotel, budget, POIs, or other shared fields;
- cannot invite or remove other users.

## 5. Data storage design

### 5.1 Mongo collections

Use two collections:

- `TravelCalendars`
- `TravelEvents`

### 5.2 Indexes

`TravelCalendars`:

- unique index on `UserId`

`TravelEvents`:

- index on `OwnerUserId`
- multikey index on `Participants.UserId`
- compound index on `StartDate`, `EndDate`
- optional compound index on `OwnerUserId`, `StartDate`

### 5.3 Concurrency

Event edits must use event-level optimistic concurrency through `Version`.

Reason:

- a participant responding to an invite should not conflict with unrelated changes to another part of their personal calendar;
- shared-event edits should not force replacement of an entire personal calendar document;
- ownership conflicts belong to the event, not to the container page state.

`TravelCalendar.Version` continues to protect personal settings and packing list changes.

## 6. Read model and page composition

The backend page DTO should be assembled from two sources:

- personal calendar settings from `TravelCalendars`;
- visible events from `TravelEvents`.

Visible events are events where:

- `OwnerUserId == currentUserId`, or
- `Participants` contains `currentUserId`.

The page response should merge these into one `TravelCalendarDto` so the frontend keeps a simple single-load mental model.

The DTO needs new event fields:

- `ownerUserId`
- `ownerDisplayName`
- `participants`
- `myParticipationStatus`
- `canEdit`
- `canRespond`

Participant entries should include at minimum:

- `userId`
- `displayName`
- `avatarUrl`
- `status`

## 7. API design changes

### 7.1 Existing personal-calendar APIs that stay personal

These operations remain on `TravelCalendar`:

- get page state
- set theme
- packing item CRUD

### 7.2 Event APIs that move to canonical shared-event behavior

These operations must target `TravelEvent` roots:

- create event from date
- update event
- remove event
- auto-schedule queued trip
- point-of-interest CRUD

These APIs continue returning refreshed page state, but their storage target becomes `TravelEvents`.

### 7.3 New participant APIs

Add event participant operations:

- `POST /api/v1/travelCalendar/events/{eventId}/participants`
- `DELETE /api/v1/travelCalendar/events/{eventId}/participants/{participantUserId}`
- `PATCH /api/v1/travelCalendar/events/{eventId}/my-participation`

Suggested request models:

- `AddParticipantRequest { expectedVersion, userId }`
- `UpdateMyParticipationRequest { expectedVersion, status }`
- `RemoveParticipantRequest { expectedVersion }`

Rules:

- owner cannot invite the same user twice;
- owner cannot remove themselves through participant endpoint;
- participant cannot respond if they are not on the event;
- declining keeps visibility unless product later asks to hide declined events;
- leaving is equivalent to owner-side removal for that participant membership.

### 7.4 User search API for the picker

The UI needs a practical way to find platform users. Current repository state shows that the existing User Management flow only exposes paginated list retrieval, not actual search.

Therefore the design must include one of these implementation paths:

- preferred: extend `UserManagementService` with server-side search by nickname/email prefix for a dedicated lightweight endpoint;
- fallback: add a Portal BFF endpoint that paginates existing users and filters server-side before returning autocomplete suggestions.

Recommendation: extend `UserManagementService` with proper search. It is cleaner, more scalable, and reusable outside Travel Calendar.

Response shape for the picker:

- `userId`
- `displayName`
- `email`
- `avatarUrl`

Privacy constraint:

- return only the minimal public user information needed for invitations.

## 8. UI and UX design

### 8.1 Event drawer additions

The travel event drawer needs a new participants section.

Desktop behavior:

- searchable autocomplete input for platform users;
- selected users shown as chips or compact participant rows;
- status badge for each participant;
- owner badge for the creator.

Mobile behavior:

- same data, but stacked rows and full-width autocomplete.

### 8.2 Calendar cell behavior

When a shared event is shown in the month grid:

- owner and participant both see the same title and dates;
- invited user sees a `Pending` visual state when applicable;
- tooltips should mention who invited the user if they are not the owner.

### 8.3 Event detail actions

Owner sees:

- add participant;
- remove participant;
- full save;
- delete event.

Participant sees:

- accept;
- decline;
- leave.

Participant does not see owner-only editable controls.

### 8.4 Visibility of declined events

For this release, declined events remain visible in the invited user's calendar with a declined state indicator.

Reason:

- it preserves auditability and avoids confusing disappear/reappear behavior during testing;
- it gives the owner and participant a consistent event history;
- it is easier to revise later than reintroducing hidden declined events.

If product later wants declined events hidden from the month view, that can be a UI filter on top of the same model.

## 9. Business rules

### 9.1 Scheduling and conflict checks

Shared events still use the existing date-range and overlap rules. Additional requirements:

- owner cannot create or edit a shared event that overlaps another visible event in their own calendar if that remains a global product rule;
- invited participants may already have conflicting events, but the initial design should not block invites on participant conflicts unless explicitly requested;
- participant conflicts can be surfaced as a warning later, but not as a hard stop in this spec.

### 9.2 Queue and must-visit behavior

Queued trips remain personal to the owner's calendar unless the owner schedules them into a shared event.

When a must-visit trip becomes a shared event:

- the canonical event still belongs to the owner;
- participants are simply members of that event.

### 9.3 Budget behavior

Budget totals on each user's calendar include visible shared events regardless of ownership.

Reason:

- the event appears in both calendars;
- the current requirement is visibility parity, not cost partitioning.

Future enhancement, out of scope now:

- per-participant cost split or personal contribution fields.

### 9.4 Notifications

No email, push, or in-app notification system is added in this scope. Shared visibility inside the calendar is sufficient for the current release.

## 10. Migration impact on the current implementation branch

The current branch already contains a working personal-calendar implementation based on embedded events. To align it with the approved shared-event design, the implementation plan must explicitly refactor these areas:

- remove `Events` ownership from the `TravelCalendar` aggregate;
- create `TravelEvent` aggregate, repository, mappings, and collection;
- change all event commands to target the event repository instead of replacing the entire calendar document;
- rebuild `GetAsync` page composition from calendar settings plus visible events;
- extend DTOs, BFF contracts, and frontend types with ownership and participants;
- add user search/autocomplete support;
- revise tests that currently assume all events live inside the personal calendar document.

No destructive migration of production data is required for this feature right now because the service is still being built and has no established production traffic.

## 11. Testing strategy

### 11.1 Backend unit tests

Add tests for:

- owner invites participant and event is visible to both users;
- owner updates shared event and both users see updated data;
- invited user accepts invitation;
- invited user declines invitation;
- invited user leaves accepted event;
- duplicate invite is rejected;
- owner cannot invite themselves as a participant;
- non-owner cannot edit shared event fields;
- non-owner cannot remove another participant;
- event page query returns both owned and invited events;
- event version conflict returns `409`.

### 11.2 Integration tests

Add focused integration coverage for:

- event creation for user A;
- participant add for user B;
- calendar load for user A includes the event;
- calendar load for user B includes the same event;
- update by user A is reflected in both reads;
- response by user B updates membership status;
- removal by user A removes visibility for user B.

### 11.3 Frontend tests

Add tests for:

- participant autocomplete rendering and selection;
- owner-only controls visibility;
- participant-only accept/decline/leave controls visibility;
- pending badge rendering in drawer and month view;
- save flow refreshes both participant list and page summary;
- unauthorized edit controls are not rendered for invited users.

### 11.4 Manual verification

Minimum manual scenario:

1. Open calendar as user A.
2. Create or open an event.
3. Add user B through participant picker.
4. Open calendar as user B and verify the event is visible with `Pending`.
5. Accept as user B and verify status changes for both users.
6. Edit title/dates as user A and verify both calendars update.
7. Decline or leave as user B and verify resulting status/visibility.

## 12. Risks and mitigations

Risk: current implementation branch is already optimized around embedded events.

Mitigation:

- treat this as a planned refactor before merge, not an incremental patch on top of the old model.

Risk: existing user lookup APIs are not sufficient for a good participant picker.

Mitigation:

- make user search a first-class implementation task, not a UI afterthought.

Risk: event-level and calendar-level concurrency may drift if responsibilities are blurred.

Mitigation:

- keep personal mutations on `TravelCalendar`;
- keep shared-event mutations on `TravelEvent`.

Risk: budget totals may surprise users if shared-event costs count in both calendars.

Mitigation:

- make this behavior explicit in product copy and tests for the first release.

## 13. Final recommendation

Proceed with a two-root design:

- `TravelCalendar` for personal shell data;
- `TravelEvent` for canonical shared events.

This is the simplest design that correctly satisfies the approved rule set:

- user A owns the event and edits it;
- user B sees the event in their calendar after invitation;
- the same event appears in both calendars without duplication;
- permissions remain simple and predictable;
- the backend stays conflict-safe under concurrent usage.

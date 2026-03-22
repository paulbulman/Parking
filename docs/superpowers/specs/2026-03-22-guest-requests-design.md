# Guest Requests Feature Design

## Overview

Team leader users can create parking requests for guests. Guest requests have the highest allocation priority and are allocated as long as spaces are available.

## Scope

The following existing endpoints are **not affected**:
- Overview (not used by the app)
- Summary (shows current user's own requests only)
- Notifications (no guest-related notifications)

## Data Model

### GuestRequest

- `Id` (string) - unique identifier (GUID), generated on creation
- `Date` (LocalDate)
- `Name` (string) - guest's name; unique per date (case-insensitive)
- `VisitingUserId` (string) - user being visited
- `RegistrationNumber` (string?) - optional
- `Status` - Pending, Allocated, or Interrupted only (subset of RequestStatus; SoftInterrupted/HardInterrupted/Cancelled do not apply to guests)

### DynamoDB Storage

- `PK: GLOBAL`, `SK: GUESTS#YYYY-MM`
- `Guests` attribute: map of day number to list of guest objects

```json
{
  "21": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "name": "Alice Smith",
      "visitingUserId": "user123",
      "registrationNumber": "AB12 CDE",
      "status": "P"
    }
  ]
}
```

Status codes reuse existing convention: P (Pending), A (Allocated), I (Interrupted).

This is structurally more complex than existing DynamoDB items (requests use `Dictionary<string, string>`, reservations use `Dictionary<string, List<string>>`). A new `IPropertyConverter` implementation will be needed, similar to `ReservationsConverter`.

Note: all guests for a month are stored in a single DynamoDB item. All write operations (save, update, delete) are read-modify-write cycles on the monthly item. Concurrent writes to the same month could result in a lost update. This is an accepted trade-off (same as reservations) given TeamLeader-only access.

### IGuestRequestRepository

- `GetGuestRequests(DateInterval)` - returns all guest requests in range
- `SaveGuestRequests(IReadOnlyCollection<GuestRequest>)` - saves guest requests (batch, for allocation status updates)
- `SaveGuestRequest(GuestRequest)` - adds a single guest request
- `UpdateGuestRequest(GuestRequest)` - updates an existing guest request (identified by Id)
- `DeleteGuestRequest(LocalDate, string id)` - removes a guest request

## API Endpoints

All guest request endpoints require TeamLeader authorization.

### POST /guest-requests

Creates a new guest request.

**Request body:**
```json
{
  "date": "2026-03-21",
  "name": "Alice Smith",
  "visitingUserId": "user123",
  "registrationNumber": "AB12 CDE"
}
```

- `date`: required, must not be in the past, must be no later than end of subsequent month
- `name`: required, non-empty, must be unique for that date (case-insensitive)
- `visitingUserId`: required, must be a non-deleted user
- `registrationNumber`: optional

Creates with Pending status. Generates a GUID for the Id. Triggers allocation re-run.

### PUT /guest-requests/{date}/{id}

Updates an existing guest request.

**Request body:**
```json
{
  "name": "Alice Jones",
  "visitingUserId": "user456",
  "registrationNumber": "AB12 CDE"
}
```

- Identified by date + Id in URL (date needed to locate the correct monthly DynamoDB item)
- Can update: name, visitingUserId, registrationNumber
- Cannot change date (delete and recreate instead)
- If name is changed, new name must be unique for that date (case-insensitive)
- New visitingUserId must be a non-deleted user
- Preserves current status
- Does NOT trigger allocation re-run (none of the updatable fields affect allocation)

Returns 404 if guest request not found.

### DELETE /guest-requests/{date}/{id}

Deletes an existing guest request.

- Identified by date + Id in URL (date needed to locate the correct monthly DynamoDB item)
- Works regardless of current status
- Triggers allocation re-run (frees a space for reallocation)

Returns 404 if guest request not found.

### GET /guest-requests

Lists guest requests from 60 days ago through end of subsequent month.

**Response:**
```json
{
  "guestRequests": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "date": "2026-03-21",
      "name": "Alice Smith",
      "visitingUserId": "user123",
      "visitingUserDisplayName": "John Doe",
      "registrationNumber": "AB12 CDE",
      "status": "Allocated"
    }
  ]
}
```

Sorted by date ascending, then name ascending.

## Allocation Integration

### AllocationCreator Changes

AllocationCreator receives guest requests for the date as an additional input.

1. Count already-allocated guests and regular requests (`AlreadyAllocatedSpaces` must include both)
2. Allocate pending guest requests first, up to available spaces (set status to Allocated)
3. Any remaining pending guests that don't fit are set to Interrupted
4. Remaining spaces go through existing RequestSorter for regular requests/reservations

```
AlreadyAllocatedSpaces = AllocatedRegularRequests + AllocatedGuestRequests
AvailableSpaces = AllocatableSpaces - AlreadyAllocatedSpaces
PendingGuests = guests with Pending or Interrupted status
GuestsToAllocate = min(PendingGuestCount, AvailableSpaces)
RemainingSpaces = AvailableSpaces - GuestsToAllocate
// Then allocate regular requests using RemainingSpaces via RequestSorter
```

Guest requests do not flow through RequestSorter.

### RequestUpdater Changes

RequestUpdater orchestrates the allocation cycle. It must be updated to:

1. Fetch guest requests for the allocation date range
2. For each allocation date, pass guest requests to AllocationCreator alongside regular requests
3. Persist updated guest request statuses via `guestRequestRepository.SaveGuestRequests()`

Guest request status lifecycle matches regular requests: previously-Allocated guests remain Allocated across re-runs. Only Pending guests that fail to be allocated are set to Interrupted. When a guest is deleted and allocation re-runs, the reduced `AlreadyAllocatedSpaces` count frees a space for the next eligible request.

The existing trigger mechanism (POST/DELETE add a trigger, scheduled service calls RequestUpdater.Update()) is unchanged - it already supports re-runs.

### AllocationCreator Return Type

Currently `AllocationCreator.Create()` returns `IReadOnlyCollection<Request>` (the allocated regular requests). It must be updated to also return updated guest requests. This can be achieved by returning a result object containing both the allocated regular requests and the updated guest requests (with Allocated or Interrupted status set).

## Daily Details Changes

Guest requests appear in the appropriate list based on status:

- **Allocated** guests appear in `AllocatedUsers`
- **Pending** guests appear in `PendingUsers`
- **Interrupted** guests appear in `InterruptedUsers`

Name format: `"Alice Smith (visiting John Doe)"`

If the visiting user has been deleted, display as: `"Alice Smith (visiting deleted user)"`

No registration number in daily details.

## Registration Numbers Changes

Guest request registration numbers are included in the existing search endpoint:

- Searched using the same normalization/matching logic
- Name format: `"Alice Smith (visiting John Doe)"` (or `"visiting deleted user"` if deleted)
- Guests with no registration number are excluded

## Test Plan

### GuestRequestsController - POST
- Creates guest request successfully
- Triggers allocation re-run
- Rejects missing date
- Rejects missing/empty name
- Rejects missing visitingUserId
- Rejects non-existent visitingUserId
- Rejects deleted visitingUserId
- Rejects duplicate name on same date (case-insensitive)
- Allows same name on different dates
- Rejects past date
- Rejects date beyond end of subsequent month
- Non-team-leader gets 403

### GuestRequestsController - PUT
- Updates guest request successfully (name, visitingUserId, registrationNumber)
- Rejects when guest request not found (404)
- Rejects invalid new visitingUserId (non-existent or deleted)
- Rejects if new name already exists on that date (case-insensitive)
- Allows keeping same name (no false duplicate rejection)
- Preserves current status
- Does not trigger allocation re-run
- Non-team-leader gets 403

### GuestRequestsController - DELETE
- Deletes guest request successfully
- Triggers allocation re-run
- Rejects when guest request not found (404)
- Non-team-leader gets 403

### GuestRequestsController - GET
- Returns guest requests within 60-day lookback and through end of subsequent month
- Excludes guest request 61 days ago
- Includes guest request exactly 60 days ago
- Returns empty list when no guest requests
- Sorted by date ascending, then name ascending
- Includes visiting user display name
- Includes registration number
- Includes status
- Includes id
- Deleted visiting user shows "deleted user" as display name

### AllocationCreator
- Guest requests allocated before regular requests
- Guest requests allocated before reservations
- More guests than spaces - only allocates up to available spaces
- Guests mixed with reservations and regular requests
- Zero guests - existing behaviour unchanged
- Multiple guests on same date counted correctly
- Guest requests set to Allocated when spaces available
- Guest requests set to Interrupted when no spaces available
- Mix of allocated and interrupted guests when partially full
- AlreadyAllocatedSpaces includes previously-allocated guests
- Delete guest triggers reallocation - previously-unallocated request gets allocated

### RequestUpdater
- Fetches guest requests for allocation date range
- Resets guest request statuses before allocation
- Passes guest requests to AllocationCreator
- Persists updated guest request statuses

### Daily Details
- Allocated guest appears in allocated users list
- Pending guest appears in pending users list
- Interrupted guest appears in interrupted users list
- Name format: "Guest Name (visiting User Name)"
- Deleted visiting user shows "Guest Name (visiting deleted user)"
- Unallocated guest (spaces full) does not appear in allocated list
- Mix of guest and regular allocated users

### Registration Numbers
- Guest registration number searchable
- Guest with no registration number excluded from results
- Name format: "Guest Name (visiting User Name)"
- Deleted visiting user shows "Guest Name (visiting deleted user)"
- Normalization works on guest registration numbers
- Duplicate registration numbers across guests and users both returned

### GuestRequestRepository (Integration)
- Save and retrieve guest request
- Update guest request
- Delete guest request
- Query by date range spanning multiple months
- Batch save (status updates)

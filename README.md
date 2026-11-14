# Smart Locker ETL

Console application (.NET 10) that reads the daily parcel batch (`datasource.csv`), allocates a locker
compartment for each parcel, generates a pickup PIN, and notifies the recipient.

## Requirements
- .NET 10 SDK
- Docker Desktop with Docker Compose v2 (for the mock API)

## Run
```
docker compose pull
docker compose up --wait
dotnet run --project LockerEtl
```
The command works from the repository root. `appsettings.json` and `Data/datasource.csv` are copied next to
the executable, and the application looks for them there, not in the current directory.

Use another batch file or API address:
```
dotnet run --project LockerEtl -- --data:path=C:\batch\file.csv --api:baseUrl=http://localhost:18080/api/v1/
```
Absolute data paths are used as they are. A relative `data:path` is resolved against the application folder,
not the current directory, so use an absolute path for files elsewhere.

Exit codes:

| Code | Meaning |
|---|---|
| `0` | Every parcel ended as `Delivered` or `AlreadyAllocated`, with no parse errors and no rejected parcels. |
| `1` | A person should look at the run: parse errors, rejected parcels, `NoCapacity`, `NotificationFailed` or `Error`. |
| `2` | Configuration or input error (missing setting, unreadable file). |
| `130` | Stopped with Ctrl+C. The current parcel was finished; run again to continue. |

The sample batch contains two parcels with missing data (`PARCEL-005`, `PARCEL-009`), so it exits with `1` by design.

Press Ctrl+C once to stop after the parcel in progress (so no compartment is allocated without a notification
attempt); press it twice to quit immediately.

## Test
```
dotnet test
```
Unit tests cover reading, consolidation, validation, PIN generation, processing (with fakes) and both HTTP clients
(with a stubbed `HttpMessageHandler`). There is no automated integration test against the Docker mock yet.

## Pipeline
1. **Read** – parse the semicolon CSV; bad rows are reported, not fatal.
2. **Consolidate** – drop duplicates, group per parcel, order by timestamp, latest non-empty value wins per field.
3. **Validate** – reject parcels missing size, locker, or any contact channel.
4. **Process** – skip parcels already in a compartment (idempotent), allocate with a secure 6-digit PIN,
   notify by SMS or fall back to email.

## Behaviour of the external APIs
- **409 from the Locker API** (no free compartment of that size): reported as `NoCapacity`; the parcel is not
  retried or moved to another locker, and the batch continues.
- **422 from the Notification API** (channel cannot be delivered): the next channel is tried (SMS, then email).
  If every channel fails, the outcome is `NotificationFailed`.
- **Other HTTP errors and timeouts**: the parcel is reported as `Error` and the batch continues.

## Assumptions
- An empty CSV field means "no change", not "clear the value".
- Missing size or locker is rejected rather than guessed.
- PIN format is not specified; 6 digits from `RandomNumberGenerator` was chosen.
- Timestamps without an offset are read as UTC.
- A parcel id appears once per run after consolidation, so a compartment snapshot per locker is enough for the
  idempotency check.

## Known weak spots
- **Allocated but not notified.** If the notification fails (all channels rejected, or an HTTP error after a
  successful allocation), the compartment stays reserved. The run reports `NotificationFailed` or `Error` and exits
  with `1`, but a rerun finds the parcel in its compartment, reports `AlreadyAllocated` and does **not** send the
  notification again. A proper fix needs a stored notification status per parcel plus an outbox that retries.
  Releasing the compartment automatically was rejected: the release API has no fitting reason code and a person should decide.
- **Idempotency only looks in the target locker.** If a parcel's target locker changes between two runs, it is not
  found in the new locker, so it is allocated again and the old compartment stays occupied.
- **Two runs at the same time** can both allocate the same parcel. Run one instance at a time.
- **Observed result against the mock:** first run: 50 rows, 1 duplicate, 10 parcels, 2 rejected, 8 `Delivered`
  (exit `1` because of the rejected parcels). Second run: 8 `AlreadyAllocated`, no new allocations or notifications.

## Possible improvements
- Outbox/retry for parcels allocated but not notified (see above).
- Release the old compartment when a parcel changes locker between runs, and look for the parcel in all lockers.
- Choose a locker by postal code when none is given (needs a business rule).
- Retry with backoff (e.g. Polly) for transient HTTP errors, GET requests only.
- Integration test against the Docker mock (e.g. Testcontainers).
- Structured run report (JSON) for monitoring.
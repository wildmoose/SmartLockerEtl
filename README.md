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
Use another batch file or API address:
```
dotnet run --project LockerEtl -- --data:path=C:\batch\file.csv --api:baseUrl=http://localhost:18080/api/v1/
```
Exit codes: `0` all good, `1` some parcels need attention, `2` configuration or input error.

## Test
```
dotnet test
```

## Pipeline
1. **Read** – parse the semicolon CSV; bad rows are reported, not fatal.
2. **Consolidate** – drop duplicates, group per parcel, order by timestamp, latest non-empty value wins per field.
3. **Validate** – reject parcels missing size, locker, or any contact channel.
4. **Process** – skip parcels already in a compartment (idempotent), allocate with a secure 6-digit PIN,
   notify by SMS or fall back to email.

## Assumptions
- An empty CSV field means "no change", not "clear the value".
- Missing size or locker is rejected rather than guessed.
- PIN format is not specified; 6 digits from `RandomNumberGenerator` was chosen.

## Possible improvements
- Outbox/retry for parcels allocated but not notified.
- Release the old compartment when a parcel changes locker between runs.
- Choose a locker by postal code when none is given (needs a business rule).
- Retry with backoff (e.g. Polly) for transient HTTP errors.
- Structured run report (JSON) for monitoring.
# Keyloop Challenge — Unified Service Scheduler

Backend implementation of **Scenario A** for the Keyloop challenge. An authenticated advisor can search availability and book a service appointment only when **both a service bay and a qualified technician** are free for the whole service duration. Confirmed appointments are persisted in SQL Server and can be read or cancelled.

No custom client UI is included. The client contract is exposed through OpenAPI, Swagger UI, a `.http` request collection and a PowerShell smoke test.

---

## Contents

- [Quick start](#quick-start)
- [Seeded accounts and data](#seeded-accounts-and-data)
- [API](#api)
- [Booking rules](#booking-rules)
- [Architecture](#architecture)
- [Concurrency and consistency](#concurrency-and-consistency)
- [Observability](#observability)
- [Tests](#tests)
- [Database operations](#database-operations)
- [AI collaboration narrative](#ai-collaboration-narrative)
- [Trade-offs and next steps](#trade-offs-and-next-steps)

---

## Quick start

### Prerequisites

- Windows with **.NET SDK 9 or newer** (the solution targets `net9.0`; `Directory.Build.props` sets `RollForward=LatestMajor` so an installed .NET 10 runtime works too).
- **SQL Server LocalDB** (`(localdb)\MSSQLLocalDB`) — the default. An existing SQL Server instance such as `.\SQLEXPRESS` also works; see [Use SQL Server Express instead of LocalDB](#use-sql-server-express-instead-of-localdb).
- The `dotnet-ef` tool is pinned in `.config/dotnet-tools.json`.

### Run

```powershell
dotnet tool restore
dotnet restore
dotnet build KeyloopChallenge.sln
dotnet run --project Keyloop.API --launch-profile http
```

The API starts at `http://localhost:5110`. In Development it automatically applies migrations and seeds the demo data before it starts listening. The startup log prints the actual database that was migrated and seeded.

Verify:

```powershell
Invoke-WebRequest http://localhost:5110/health/live
Invoke-WebRequest http://localhost:5110/health/ready
```

Swagger UI is available at <http://localhost:5110/swagger> (the root URL redirects to it in Development). The raw OpenAPI document is at <http://localhost:5110/openapi/v1.json>; a saved copy is committed as [`docs/openapi.json`](docs/openapi.json).

To call authenticated endpoints in Swagger, log in with `POST /api/v1/authentication/login`, copy the `accessToken` value, click **Authorize**, and paste the raw token (without the `Bearer ` prefix).

### One-command smoke test

With the API running:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/smoke-test.ps1
```

The script logs in, reads reference data and availability, creates an appointment, reads it back, cancels it twice (proving idempotency) and prints a pass/fail summary.

---

## Seeded accounts and data

| Account | Credentials | Source |
|---|---|---|
| Administrator | `admin@keyloop.local` / `Admin123!ChangeMe` | `Authentication:Admin` configuration |
| Demo advisor | `demo@keyloop.local` / `Demo123!ChangeMe` | Seeded application user |

Seed data:

| Data | Value |
|---|---|
| Dealerships | Keyloop Hanoi, Keyloop London, Keyloop New York |
| Service types | Oil & Filter Change (60m), Brake Inspection (90m), Full Service (120m), Diagnostics (60m), Tyre Replacement (45m) |
| Service bays | 3 per dealership (9 total) |
| Technicians | 3 per dealership (9 total), with per-dealership qualifications |
| Customers / vehicles | 6 customers, 7 vehicles (one customer has two vehicles) |
| Demo appointment | One confirmed appointment at Keyloop Hanoi on the next working day at 09:00 local time |

The seeder is idempotent: if dealerships already exist it returns the existing counts instead of inserting duplicates.

> The administrator account is validated from configuration, not stored in the database. Change the development JWT signing key and admin password before using this outside a local demo.

---

## API

All scheduler endpoints require a JWT from `POST /api/v1/authentication/login`.

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `/api/v1/authentication/login` | Obtain a JWT (anonymous) |
| `POST` | `/api/v1/authentication/register` | Register a user (anonymous) |
| `GET` | `/api/v1/authentication/me` | Inspect the current principal |
| `GET` | `/api/v1/dealerships` | List dealerships |
| `GET` | `/api/v1/servicetypes` | List service types and durations |
| `GET` | `/api/v1/customers` | List customers |
| `GET` | `/api/v1/customers/{id}/vehicles` | List a customer's vehicles |
| `GET` | `/api/v1/dealerships/{id}/availability?serviceTypeId={id}&date={yyyy-MM-dd}` | List bookable slots |
| `POST` | `/api/v1/appointments` | Create a confirmed appointment |
| `GET` | `/api/v1/appointments/{id}` | Read an appointment |
| `POST` | `/api/v1/appointments/{id}/cancel` | Cancel an appointment (idempotent) |
| `GET` | `/health/live` | Liveness probe |
| `GET` | `/health/ready` | Readiness probe, including SQL connectivity |

Errors use RFC 7807 `application/problem+json`:

| Status | When | `code` examples |
|---|---|---|
| `400` | Invalid request contract or business rule | `validation_failed` |
| `401` | Missing/invalid token or bad credentials | — |
| `403` | Vehicle does not belong to the customer | `forbidden` |
| `404` | Unknown customer, vehicle, dealership, service type or appointment | `not_found` |
| `409` | Requested slot cannot be allocated | `NO_BAY_AVAILABLE`, `NO_TECHNICIAN_AVAILABLE`, `OUTSIDE_OPENING_HOURS`, `START_IN_PAST`, `INVALID_TIME_GRID` |
| `409` | The same vehicle is already booked at that time | `conflict` |
| `500` | Unexpected failure | — |

### Example: log in and create an appointment

PowerShell:

```powershell
$base = 'http://localhost:5110'
$login = Invoke-RestMethod -Method Post -Uri "$base/api/v1/authentication/login" `
    -ContentType 'application/json' `
    -Body '{"email":"demo@keyloop.local","password":"Demo123!ChangeMe"}'
$headers = @{ Authorization = "Bearer $($login.accessToken)" }

$body = @{
    customerId   = 1
    vehicleId    = 1
    dealershipId = 1
    serviceTypeId = 1
    startTime    = '2026-11-03T10:00:00+07:00'   # future, 15-minute grid, Hanoi hours
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "$base/api/v1/appointments" `
    -Headers $headers -ContentType 'application/json' -Body $body
```

Success returns `201 Created`, a `Location` header and the confirmed allocation:

```json
{
  "id": "70934d53-ecd0-43f9-9325-de1a103fa4e2",
  "status": "Confirmed",
  "startUtc": "2026-11-03T03:00:00Z",
  "endUtc": "2026-11-03T04:00:00Z",
  "customer": { "id": 1, "fullName": "Nguyen Van An" },
  "vehicle": { "id": 1, "vin": "1HGBH41JXMN109186", "make": "Toyota", "model": "Camry" },
  "dealership": { "id": 1, "name": "Keyloop Hanoi" },
  "serviceType": { "id": 1, "name": "Oil & Filter Change", "durationMinutes": 60 },
  "technician": { "id": 2, "name": "Bao Tran" },
  "serviceBay": { "id": 1, "name": "Bay 1" }
}
```

cURL (Git Bash / WSL):

```bash
curl -s -X POST http://localhost:5110/api/v1/authentication/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@keyloop.local","password":"Demo123!ChangeMe"}'

curl -s -X POST http://localhost:5110/api/v1/appointments \
  -H "Authorization: Bearer <accessToken>" \
  -H "Content-Type: application/json" \
  -d '{"customerId":1,"vehicleId":1,"dealershipId":1,"serviceTypeId":1,"startTime":"2026-11-03T10:00:00+07:00"}'
```

The complete request collection is [`Keyloop.API/Keyloop.API.http`](Keyloop.API/Keyloop.API.http). Update the date and IDs to match seeded data and the current date.

---

## Booking rules

1. The customer, vehicle, dealership and service type must exist.
2. The vehicle must belong to the customer.
3. `startTime` must be in the future.
4. The local start time must align to a **15-minute boundary** in the dealership time zone.
5. The whole appointment must fit inside the dealership opening hours (Hanoi 08:00–17:00, London 09:00–18:00, New York 08:00–17:00).
6. The vehicle must not already have a confirmed overlapping appointment.
7. One service bay and one technician qualified for the service type must be free for the full duration.
8. Availability is returned in **30-minute steps**, each slot carrying a UTC start/end.
9. Cancelling an appointment frees its bay and technician immediately; cancelling twice is safe.

Assumptions kept deliberately small:

- The API is a staff-facing tool: any authenticated user may act for any customer.
- Appointments are single-day; there is no holiday calendar or multi-day work order.
- There is no payment, notification, parts inventory or customer-facing authentication in this scenario.
- Customers, vehicles, bays and technicians are seeded reference data. Customer/vehicle/master-data CRUD is out of scope; the API focuses on booking and availability.
- Dealership time zones are stored as Windows time-zone IDs because the development database and host are Windows.
- The delivered client contract is the OpenAPI document plus the `.http`/PowerShell examples; Swagger UI is the manual test client for Development.

---

## Architecture

```text
Keyloop.API            ASP.NET Core controllers, JWT, ProblemDetails, health, OpenAPI, telemetry wiring
        │
Keyloop.Application    MediatR commands/queries, FluentValidation, transaction/audit behaviors, ports and DTOs
        │
Keyloop.Domain         Appointment, TimeInterval, opening-hours rule, ResourceAllocator; no EF/HTTP dependencies
        │
Keyloop.Infrastructure EF Core mappings, SQL Server, AppointmentScheduler, seeder, JWT/user services
```

Booking flow:

1. `AppointmentsController` accepts `CreateAppointmentRequest` and dispatches `CreateAppointmentCommand`.
2. Validation runs, then `TransactionBehavior` opens a database transaction.
3. `AppointmentScheduler` loads reference data, validates ownership, time grid and opening hours.
4. It takes an exclusive SQL Server application lock scoped to the dealership.
5. It re-reads confirmed overlaps, checks the vehicle and calls `ResourceAllocator`.
6. On success it inserts one `Appointment`; the transaction commits through the unit of work.
7. The controller returns `201 Created` with the full allocation.

The full sequence, data model, API contract and design trade-offs are in [`docs/SYSTEM_DESIGN.md`](docs/SYSTEM_DESIGN.md).

### Project layout

| Path | Responsibility |
|---|---|
| `Keyloop.Domain/Entities/Scheduler` | Scheduler entities and opening-hours invariant |
| `Keyloop.Domain/Services/ResourceAllocator.cs` | Bay/technician selection and failure codes |
| `Keyloop.Application/Features/Appointments` | Create/get/cancel/availability use cases |
| `Keyloop.Application/Features/ReferenceData` | Dealership, service type, customer and vehicle queries |
| `Keyloop.Infrastructure/Persistence/Scheduler` | Scheduler implementation and SQL locking |
| `Keyloop.Infrastructure/Persistence/Seed` | Idempotent demo seed |
| `Keyloop.API/Controllers/V1` | Versioned HTTP endpoints |
| `Keyloop.UnitTests` | Domain, scheduler, seeder and validator tests |

---

## Concurrency and consistency

Two advisors can attempt to book the last bay at the same moment. The scheduler prevents over-allocation with three layers:

1. **Transaction boundary** — `TransactionBehavior` wraps the command; insertion and audit commit atomically.
2. **Dealership application lock** — on SQL Server, `sp_getapplock` takes an exclusive transaction-owned lock named `appointment-scheduler:dealership:{id}` with a 5-second timeout. Competing bookings for the same dealership serialize; different dealerships proceed in parallel.
3. **Recheck after locking** — conflicting confirmed appointments are queried inside the lock, then one bay and one qualified technician are chosen. The vehicle double-booking guard is also checked.

The application lock is skipped automatically for the EF InMemory provider used by unit tests. SQL Server remains the authority in production.

This deliberately trades a small amount of dealership-level contention for simple, auditable correctness. A production system could move to per-resource exclusion constraints or a queue, but that adds schema and operational complexity this scenario does not need.

---

## Observability

- **Structured logs** via Serilog: request method/path/status/latency, booking IDs, dealership IDs and rejection codes. Customer PII is not logged.
- **Traces and metrics** via OpenTelemetry:
  - meter `Keyloop.Scheduler`
  - `scheduler.bookings.created`
  - `scheduler.bookings.rejected` (tagged by reason)
  - `scheduler.booking.duration` (ms)
  - ASP.NET Core and HTTP client instrumentation
- A console exporter is available for local inspection:

  ```powershell
  $env:Observability__ConsoleExporter = 'true'
  dotnet run --project Keyloop.API
  ```

- **Health probes**: `/health/live` checks the process; `/health/ready` runs a `SELECT 1` against the configured SQL Server.

Expected domain rejections (409/403/404/validation) are logged as warnings; unexpected failures are logged as errors with stack traces.

---

## Tests

```powershell
dotnet test KeyloopChallenge.sln
```

**41 tests pass.** Coverage by area:

| Area | Tests |
|---|---|
| `TimeInterval` | UTC enforcement, positive duration, overlap/adjacent/disjoint |
| `Dealership` | opening-hours boundaries and midnight crossing |
| `ResourceAllocator` | bay/technician selection, no-bay/no-technician codes, qualification preference |
| `Appointment` | creation, cancellation, overlap rules |
| `AppointmentScheduler` | success, allocation of distinct resources, vehicle ownership, double booking, past/grid/opening-hours rejection, resource saturation, availability filtering, cancellation idempotency |
| `DatabaseSeeder` | expected counts, idempotency, future working-day demo appointment |
| Validators | create/availability/vehicle query rules |

The HTTP acceptance path is exercised by `scripts/smoke-test.ps1` and the manual transcript in [`docs/DEMO.md`](docs/DEMO.md), including 401/403/404/409/400 responses and resource saturation.

---

## Database operations

### Connection and startup behaviour

The default connection string is in [`Keyloop.API/appsettings.json`](Keyloop.API/appsettings.json):

```text
Server=(localdb)\mssqllocaldb;Database=KeyloopChallenge;Trusted_Connection=True;TrustServerCertificate=True
```

`Trusted_Connection=True` uses Windows integrated authentication, so no SQL password is stored in the repository. If an instance requires SQL authentication, supply `User Id`/`Password` through `ConnectionStrings__DefaultConnection` or .NET user-secrets instead of committing it to `appsettings.json`.

[`Keyloop.API/appsettings.Development.json`](Keyloop.API/appsettings.Development.json) enables automatic database setup:

| Key | Development | Base default |
|---|---|---|
| `Database:AutoMigrate` | `true` | `false` |
| `Database:Seed` | `true` | `false` |

On startup, `DatabaseInitializationHostedService` calls `MigrateAsync()` and then `DatabaseSeeder.SeedAsync()`. The seeder is idempotent and stops if a dealership already exists. Seed data is C# in [`DatabaseSeeder.cs`](Keyloop.Infrastructure/Persistence/Seed/DatabaseSeeder.cs), not JSON: it creates related dealerships, bays, technicians, qualifications, customers, vehicles and a demo appointment with generated IDs and a future working-day time. No separate JSON/CSV seed file is required.

The startup log prints the actual server and database, for example:

```text
Database ready on (localdb)\MSSQLLocalDB/KeyloopChallenge: 3 dealerships, ...
```

### Use SQL Server Express instead of LocalDB

Override the connection string at run time (the double underscore maps to `:`):

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__DefaultConnection = 'Server=.\SQLEXPRESS;Database=KeyloopChallenge;Trusted_Connection=True;TrustServerCertificate=True'
dotnet run --project Keyloop.API --launch-profile http
```

Alternatively, change the `DefaultConnection` value in `appsettings.json`. On the first run the API creates the database, applies the migration and seeds it.

### Verify the seeded data

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d KeyloopChallenge -Q "SET NOCOUNT ON; SELECT (SELECT COUNT(*) FROM Dealerships) AS Dealerships, (SELECT COUNT(*) FROM ServiceTypes) AS ServiceTypes, (SELECT COUNT(*) FROM ServiceBays) AS Bays, (SELECT COUNT(*) FROM Technicians) AS Technicians, (SELECT COUNT(*) FROM Customers) AS Customers, (SELECT COUNT(*) FROM Vehicles) AS Vehicles, (SELECT COUNT(*) FROM Appointments) AS Appointments;"
```

For `.\SQLEXPRESS`, replace `-S "(localdb)\MSSQLLocalDB"` with `-S ".\SQLEXPRESS"`.

### Apply migrations manually

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
dotnet ef database update --project Keyloop.Infrastructure --startup-project Keyloop.API
```

### Reset and reseed

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
dotnet ef database drop --force --project Keyloop.Infrastructure --startup-project Keyloop.API
dotnet run --project Keyloop.API --launch-profile http
```

`dotnet ef` and `dotnet run` must see the same connection string. If you used the `ConnectionStrings__DefaultConnection` override above, set it in the same terminal before dropping.

---

## AI collaboration narrative

I used an AI coding assistant as a pair-programming and review aid, not as an unreviewed code generator. The collaboration followed the same phases as the commits:

1. **Analysis and planning.** The assistant read the existing Clean Architecture scaffold and the assessment PDF, identified the missing scheduler path and proposed a phased plan. During design I used it to compare per-bay locks, row-version retries and a dealership-level application lock, and to challenge the API scope. I chose Scenario A, the final concurrency design, LocalDB and per-phase commits before implementation started.
2. **Domain and application.** We added the appointment navigation properties, availability/cancel/reference use cases and telemetry names. I reviewed each contract and kept business rules in the domain rather than controllers.
3. **Infrastructure.** The assistant drafted EF configurations, the scheduler implementation, seed data and the migration. I verified the generated SQL schema, the indexes and the seeded counts directly in LocalDB.
4. **API and observability.** Controllers, ProblemDetails mappings, health checks, Serilog and OpenTelemetry were added. The smoke test was run against a real LocalDB instance, not just mocks.
5. **Testing and hardening.** Unit tests were added for domain rules, scheduler edge cases, seeder idempotency and validators. Running the real API exposed three issues that the assistant then fixed:
   - EF Core's retrying execution strategy was incompatible with the explicit transactions used by the unit of work and seeder;
   - the DbContext factory registered a second, differently configured context;
   - request logging wrapped the exception handler in the wrong order, so handled 409 responses were temporarily logged as 500s.

   Each fix was followed by a rebuild, the full test suite and a fresh smoke test.
6. **Documentation.** The README, system design, video script and demo transcript were written from the actual implementation and observed API responses.

Prompts that worked best were specific about constraints: “no overengineering”, “keep business rules out of controllers”, “preserve transaction/audit ordering”, and “show the SQL concurrency strategy”. The final decisions, trade-offs and verification remain mine.

---

## Trade-offs and next steps

| Decision | Why | What I would change at larger scale |
|---|---|---|
| Dealership-level `sp_getapplock` | Simple correctness for concurrent bookings; no schema tricks | Per-resource locks or a distributed booking queue for very high throughput |
| Manual time-grid/opening-hours validation | Explicit, testable business rules | Holiday calendars, per-day hours, multi-day work orders |
| No retrying EF execution strategy | Explicit user transactions stay simple and predictable | Adopt an execution-strategy wrapper around the whole unit of work before enabling retries |
| LocalDB + Windows time-zone IDs | Zero-setup local demo on the assessment machine | Containerized SQL Server and IANA time-zone IDs for cross-platform deployment |
| Staff-facing JWT only | Matches the backend scenario and keeps the demo small | OIDC, per-dealership roles, customer-scoped authorization |
| No client UI | The scenario asks for backend; contract is OpenAPI/.http/script | A thin advisor UI or generated client |

Natural next steps: appointment rescheduling, a list/search endpoint with pagination, customer notifications, holiday calendars, idempotency keys on create, and a CI pipeline running build + tests + migration against a disposable SQL Server.

---

## Documentation

- [`docs/SYSTEM_DESIGN.md`](docs/SYSTEM_DESIGN.md) — architecture, data model, sequence diagrams, concurrency, observability, trade-offs.
- [`docs/VIDEO_SCRIPT.md`](docs/VIDEO_SCRIPT.md) — 8–10 minute walkthrough script.
- [`docs/DEMO.md`](docs/DEMO.md) — verified smoke-test transcript.
- [`docs/openapi.json`](docs/openapi.json) — committed OpenAPI 3 document.
- [`Keyloop.API/Keyloop.API.http`](Keyloop.API/Keyloop.API.http) — ready-to-run HTTP requests.

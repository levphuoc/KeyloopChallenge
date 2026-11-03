# Demo and Acceptance Transcript

This document records the verified behaviour of the Unified Service Scheduler against SQL Server LocalDB and the real HTTP API. It is both a demo guide and the acceptance evidence for the HTTP layer.

The automated unit suite covers domain and scheduler rules; this transcript covers the end-to-end path a client actually uses.

---

## 1. Prerequisites

- Windows with the .NET SDK installed.
- SQL Server LocalDB available as `(localdb)\MSSQLLocalDB` (default), or another instance such as `.\SQLEXPRESS` selected with `ConnectionStrings__DefaultConnection`.
- Repository restored and built:

```powershell
dotnet tool restore
dotnet restore
dotnet build KeyloopChallenge.sln
```

For a clean, repeatable run, reset the database before starting:

```powershell
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
dotnet ef database drop --force --project Keyloop.Infrastructure --startup-project Keyloop.API
```

Start the API in Development so migrations and the seed run automatically:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project Keyloop.API --no-launch-profile --urls http://localhost:5110
```

Expected startup log:

```text
Applying database migrations.
Database ready on (localdb)\mssqllocaldb/KeyloopChallenge: 3 dealerships, 5 service types, 9 bays, 9 technicians, 6 customers, 7 vehicles, 1 appointments.
Now listening on: http://localhost:5110
```

---

## 2. One-command smoke test

In a second terminal:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/smoke-test.ps1
```

Verified output:

```text
== Health ==
live=200 ready=Healthy

== Login ==
authenticated as demo@keyloop.local (User)

== Reference data ==
dealerships=3 serviceTypes=5 customers=6 vehiclesForCustomer1=2

== Availability on 2026-11-03 ==
availableSlots=17 firstSlot=2026-11-03T01:00:00Z

== Create appointment ==
created=96ef447e-574b-45f6-9e35-429abed29145 status=Confirmed
technician=Bao Tran bay=Bay 1
window=2026-11-03T01:00:00Z .. 2026-11-03T02:00:00Z

== Read appointment ==
status=Confirmed customer=Nguyen Van An
vehicle=Toyota Camry (1HGBH41JXMN109186)

== Cancel appointment (idempotent) ==
first=Cancelled second=Cancelled

== Result ==
Smoke test passed.
```

What this proves:

| Step | Evidence |
|---|---|
| Health | Liveness returns `200`; readiness reports `Healthy` after a real SQL `SELECT 1`. |
| Login | The seeded advisor authenticates and receives a JWT. |
| Reference data | Dealerships, service types, customers and the customer's vehicles are readable. |
| Availability | The API returns real slots for a future working day, in UTC. |
| Create | A bay and a qualified technician are allocated; the appointment is `Confirmed`. |
| Read | The persisted appointment returns the full customer/vehicle allocation. |
| Cancel | Cancellation is idempotent: the second call still returns `Cancelled`. |

The script chooses the next weekday dynamically (`Get-Date`), so it does not depend on a hard-coded date.

---

## 3. Detailed acceptance matrix

These requests were executed against `http://localhost:5110` with the demo advisor's token. Dates were future working days at the time of the run.

Setup:

```powershell
$base = 'http://localhost:5110'
$login = Invoke-RestMethod -Method Post -Uri "$base/api/v1/authentication/login" `
    -ContentType 'application/json' `
    -Body '{"email":"demo@keyloop.local","password":"Demo123!ChangeMe"}'
$headers = @{ Authorization = "Bearer $($login.accessToken)" }
```

### 3.1 Health, authentication and reference data

| Check | Request | Verified result |
|---|---|---|
| Liveness | `GET /health/live` | `200` |
| Readiness | `GET /health/ready` | `200 Healthy` |
| Login | `POST /api/v1/authentication/login` | `200`, JWT issued for `demo@keyloop.local` (`User`) |
| Unauthenticated access | `GET /api/v1/dealerships` without token | `401` |
| Dealerships | `GET /api/v1/dealerships` | `3` |
| Service types | `GET /api/v1/servicetypes` | `5` |
| Customers | `GET /api/v1/customers` | `6` |
| Customer vehicles | `GET /api/v1/customers/1/vehicles` | `2` |
| Availability | `GET /api/v1/dealerships/1/availability?serviceTypeId=1&date=2026-11-03` | `17` slots, `01:00Z .. 09:00Z` |

### 3.2 Happy path and allocation

Two bookings were made for the same dealership, service type and start time using different vehicles. The allocator chose distinct resources:

```text
CREATED 1 => 70934d53-ecd0-43f9-9325-de1a103fa4e2 Confirmed tech=Bao Tran bay=Bay 1
CREATED 2 => 94635e11-9e97-4b71-a7be-089a2ad87672 Confirmed tech=An Nguyen bay=Bay 2
LOCATION  => http://localhost:5110/api/v1/Appointments/70934d53-ecd0-43f9-9325-de1a103fa4e2
GET BY ID => 70934d53-ecd0-43f9-9325-de1a103fa4e2 Confirmed Nguyen Van An / Toyota Camry
```

The `Location` header confirms `201 Created` semantics, and the read-back confirms persistence.

### 3.3 Rejections

| Case | Request body | Verified response | What it proves |
|---|---|---|---|
| Resource saturation | Third booking at the same time | `409 NO_TECHNICIAN_AVAILABLE` | Only two seeded technicians are qualified for Oil & Filter Change; the third cannot be allocated. |
| Vehicle ownership | Customer 2 books customer 1's vehicle | `403 forbidden` | Ownership is enforced in the scheduler. |
| Unknown customer | `customerId = 999` | `404 not_found` | Reference data is validated before allocation. |
| Past start | A time before `now` | `409 START_IN_PAST` | The time rule is enforced server-side. |
| Invalid grid | `11:07` local time | `409 INVALID_TIME_GRID` | Starts must align to 15-minute boundaries. |
| Invalid contract | All IDs `0` | `400 validation_failed` | FluentValidation rejects the request before any handler runs. |
| Missing token | `GET /api/v1/dealerships` | `401` | Scheduler endpoints require authentication. |

Example problem response:

```json
{
  "type": "https://httpstatuses.com/409",
  "title": "Slot unavailable",
  "status": 409,
  "detail": "No qualified technician is available for the requested time.",
  "instance": "/api/v1/appointments",
  "code": "NO_TECHNICIAN_AVAILABLE",
  "traceId": "..."
}
```

### 3.4 Cancellation and availability

```text
CANCEL IDEMPOTENT       => Cancelled
AVAILABILITY AFTER CANCEL => 17 slots
```

Cancelling frees the bay and technician immediately, and calling cancel twice still returns `Cancelled`.

### 3.5 OpenAPI and Swagger UI

```text
SWAGGER UI => HTTP 200 at /swagger
OPENAPI    => HTTP 200 at /openapi/v1.json
```

Swagger UI is served at `/swagger` in Development; the root URL redirects there. It uses the JWT bearer scheme, so the login token can be pasted into **Authorize** (raw token, without the `Bearer ` prefix). The document is committed as [`openapi.json`](openapi.json).

---

## 4. Concurrency demonstration

The saturation case above is the clearest visible proof of the allocation invariant. To show the locking behaviour itself:

1. Reset the database and start the API.
2. Send two overlapping create requests for the **same dealership** at the same instant (for example with two PowerShell jobs or two HTTP clients).
3. Both requests serialize on `sp_getapplock`; one gets the last available qualified technician and the other receives `409 NO_TECHNICIAN_AVAILABLE`.
4. Repeat with **different dealerships**: both succeed because the lock resource is scoped per dealership.

The lock is taken inside the command transaction and released on commit or rollback. The full design is in [`SYSTEM_DESIGN.md`](SYSTEM_DESIGN.md#6-concurrency-and-consistency).

---

## 5. Test evidence

```powershell
dotnet test KeyloopChallenge.sln
```

Verified result:

```text
Passed! - Failed: 0, Passed: 41, Skipped: 0, Total: 41
```

Build verified with `0 Warning(s)` and `0 Error(s)`.

---

## 6. Observations from the real run

Running against LocalDB (not only mocks) surfaced three issues that were fixed before delivery:

1. **Explicit transactions vs. retrying execution strategy** — EF Core rejects user transactions while a retrying strategy is active. The retry strategy was removed from the scheduler context/factory and the trade-off documented.
2. **Duplicate DbContext configuration** — the factory registered a second context with different options. It now reuses the same configuration.
3. **Middleware order** — Serilog request logging wrapped the exception handler, so handled `409`s were temporarily logged as `500`s. Request logging now wraps the handler, and expected domain rejections log as warnings.

These are recorded because they are exactly the kind of integration defects unit tests with InMemory providers do not catch.

---

## 7. Reset after the demo

To return to a clean seed state:

```powershell
# stop the API first
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
dotnet ef database drop --force --project Keyloop.Infrastructure --startup-project Keyloop.API
dotnet run --project Keyloop.API --launch-profile http
```

The Development hosted service migrates and seeds on startup. Stop the API after the startup log reports the seed counts; the database remains ready for the next run.

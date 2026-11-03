# Video Script — Unified Service Scheduler

**Target length:** 7–9 minutes  
**Scenario:** A — book a service appointment only when a bay and a qualified technician are free.

The script is written for a screen recording with three windows: terminal, editor showing key files, and browser/SQL client. Commands are PowerShell, matching the development environment.

---

## 0:00–0:45 — Problem and scope

**On screen:** README title and scenario summary.

**Narration:**

> This is Scenario A of the Keyloop challenge: a unified service scheduler. An advisor chooses a customer, vehicle, dealership, service type and desired time. The system may confirm the booking only if it can reserve both a service bay and a technician qualified for that service for the whole duration.
>
> I implemented the backend end to end: domain rules, use cases, EF Core persistence, SQL Server migration and seed data, a versioned REST API, concurrency protection, tests, health checks and observability.
>
> The client side is intentionally stubbed through OpenAPI, Swagger UI, a `.http` collection and a PowerShell smoke test. There is no custom UI in scope.

---

## 0:45–2:00 — Architecture

**On screen:** `docs/SYSTEM_DESIGN.md`, architecture section; then Solution Explorer.

**Narration:**

> The solution keeps the existing Clean Architecture shape.
>
> - `Keyloop.Domain` contains `Appointment`, `TimeInterval`, dealership opening-hours logic and `ResourceAllocator`. It has no EF Core or HTTP dependencies.
> - `Keyloop.Application` contains MediatR commands and queries, FluentValidation validators and the transaction, validation, logging and audit behaviors.
> - `Keyloop.Infrastructure` implements persistence, the scheduler, seeding, JWT and auditing.
> - `Keyloop.API` exposes versioned controllers, ProblemDetails, health probes, OpenAPI, Serilog and OpenTelemetry.
>
> The booking command runs through validation, then a transaction, then audit capture, then the scheduler. The transaction commits only after the appointment and its audit row are ready.

**Show briefly:** `Keyloop.API/Controllers/V1/AppointmentsController.cs` and `Keyloop.Application/MediatRServiceCollectionExtensions.cs`.

---

## 2:00–3:00 — Start the API and seed the database

**On screen:** terminal in the repository root.

```powershell
dotnet tool restore
dotnet run --project Keyloop.API --launch-profile http
```

**Narration:**

> In Development the API applies migrations and runs the idempotent seeder before it starts listening. The seed creates three dealerships, five service types, nine bays, nine technicians, six customers, seven vehicles and one demo appointment.

Show the startup log:

```text
Applying database migrations.
Database ready on (localdb)\mssqllocaldb/KeyloopChallenge: 3 dealerships,
5 service types, 9 bays, 9 technicians, 6 customers, 7 vehicles, 1 appointments.
Now listening on: http://localhost:5110
```

```powershell
Invoke-WebRequest http://localhost:5110/health/live
Invoke-WebRequest http://localhost:5110/health/ready
```

**Narration:**

> Liveness only proves the process is up. Readiness executes `SELECT 1` against LocalDB, so a broken database connection removes the instance from a load balancer.

---

## 3:00–3:45 — Log in and inspect reference data

**On screen:** terminal.

```powershell
$base = 'http://localhost:5110'
$login = Invoke-RestMethod -Method Post -Uri "$base/api/v1/authentication/login" `
  -ContentType 'application/json' `
  -Body '{"email":"demo@keyloop.local","password":"Demo123!ChangeMe"}'

$headers = @{ Authorization = "Bearer $($login.accessToken)" }

Invoke-RestMethod "$base/api/v1/dealerships" -Headers $headers
Invoke-RestMethod "$base/api/v1/servicetypes" -Headers $headers
Invoke-RestMethod "$base/api/v1/customers/1/vehicles" -Headers $headers
```

**Narration:**

> Scheduler endpoints require a JWT. The demo advisor is seeded in the database; the administrator account is validated from configuration so the demo works before any user row exists.
>
> Reference endpoints give a client everything it needs to build the booking form: dealerships, service types and durations, customers and the selected customer's vehicles.

---

## 3:45–4:30 — Availability

**On screen:** terminal. Choose the next working day dynamically.

```powershell
$date = (Get-Date).Date.AddDays(1)
while ($date.DayOfWeek -in @('Saturday','Sunday')) { $date = $date.AddDays(1) }
$dateText = $date.ToString('yyyy-MM-dd')

$slots = Invoke-RestMethod `
  "$base/api/v1/dealerships/1/availability?serviceTypeId=1&date=$dateText" `
  -Headers $headers

$slots.Count
$slots | Select-Object -First 3
```

**Narration:**

> Availability is calculated in the dealership's time zone and returned as UTC instants. Slots are offered every thirty minutes, and each candidate is checked against both the bay pool and the qualified technician pool.
>
> The seeded appointment occupies nine to ten AM Hanoi time, but because three bays and multiple technicians exist, most other overlapping slots remain bookable. Availability is advisory; the create path revalidates under a lock.

---

## 4:30–5:30 — Create a confirmed appointment

**On screen:** terminal, then SQL Server Object Explorer or `sqlcmd`.

```powershell
$slot = $slots | Select-Object -First 1
$body = @{
    customerId = 1
    vehicleId = 1
    dealershipId = 1
    serviceTypeId = 1
    startTime = ([DateTimeOffset]$slot.startUtc).ToString('o')
} | ConvertTo-Json

$created = Invoke-RestMethod -Method Post -Uri "$base/api/v1/appointments" `
  -Headers $headers -ContentType 'application/json' -Body $body

$created
```

Show:

```text
status        : Confirmed
customer      : Nguyen Van An
vehicle       : Toyota Camry
serviceType   : Oil & Filter Change (60 minutes)
technician    : Bao Tran
serviceBay    : Bay 1
```

**Narration:**

> The response is a full allocation, not just an ID: customer, vehicle, dealership, service type, technician, bay and the UTC window. The controller returns `201 Created` with a `Location` header.
>
> The selected technician is qualified for the service type. The allocator prefers the qualified technician with the fewest qualifications, preserving specialists for work only they can do.

Optional SQL:

```sql
SELECT Status, StartUtc, EndUtc, TechnicianId, ServiceBayId
FROM Appointments
ORDER BY StartUtc;
```

---

## 5:30–6:45 — Concurrency and resource saturation

**On screen:** terminal.

**Narration:**

> The interesting case is contention. I will create two appointments at the same time for different vehicles. Hanoi has three bays, but only two technicians are qualified for Oil & Filter Change: An and Bao. One is already occupied by the seeded nine AM appointment, but at ten AM both are free.
>
> After two simultaneous bookings, the third request must fail. It cannot invent a qualified technician.

Create two overlapping bookings (same `startTime`, different vehicles), then a third:

```powershell
$startTime = '2026-11-03T10:00:00+07:00'   # use a future working day in your run

$first  = Invoke-RestMethod -Method Post -Uri "$base/api/v1/appointments" -Headers $headers -ContentType 'application/json' -Body "{`"customerId`":1,`"vehicleId`":1,`"dealershipId`":1,`"serviceTypeId`":1,`"startTime`":`"$startTime`"}"
$second = Invoke-RestMethod -Method Post -Uri "$base/api/v1/appointments" -Headers $headers -ContentType 'application/json' -Body "{`"customerId`":1,`"vehicleId`":2,`"dealershipId`":1,`"serviceTypeId`":1,`"startTime`":`"$startTime`"}"
```

A simpler alternative is to use the `.http` file or `scripts/smoke-test.ps1` and pause the recording on the saturation request.

Third request:

```powershell
try {
    Invoke-RestMethod -Method Post -Uri "$base/api/v1/appointments" `
      -Headers $headers -ContentType 'application/json' `
      -Body '{"customerId":2,"vehicleId":3,"dealershipId":1,"serviceTypeId":1,"startTime":"2026-11-03T10:00:00+07:00"}'
} catch {
    $_.ErrorDetails.Message
}
```

Show:

```json
{
  "status": 409,
  "title": "Slot unavailable",
  "detail": "No qualified technician is available for the requested time.",
  "code": "NO_TECHNICIAN_AVAILABLE"
}
```

**Narration:**

> On SQL Server, the scheduler takes an exclusive `sp_getapplock` scoped to the dealership for the duration of the transaction, then rechecks overlaps and allocates. Two requests for the same dealership serialize, so they cannot both claim the last bay or technician. Different dealerships use different lock resources and proceed in parallel.
>
> The failure is a `409 Conflict` with a stable code. The client can offer another slot rather than retrying blindly.

Show briefly: `AppointmentScheduler.AcquireDealershipLockAsync` and `ResourceAllocator.Allocate`.

---

## 6:45–7:45 — Read, cancel, tests

**On screen:** terminal.

```powershell
$fetched = Invoke-RestMethod "$base/api/v1/appointments/$($created.id)" -Headers $headers
$fetched.status

$cancelled = Invoke-RestMethod -Method Post -Uri "$base/api/v1/appointments/$($created.id)/cancel" -Headers $headers
$cancelled.status

# Second cancel proves idempotency
(Invoke-RestMethod -Method Post -Uri "$base/api/v1/appointments/$($created.id)/cancel" -Headers $headers).status
```

**Narration:**

> Cancellation frees the bay and technician immediately. Calling it twice is safe and still returns `Cancelled`.
>
> The automated suite covers the domain and the scheduler with EF InMemory: overlap rules, ownership, double booking, opening hours, time grid, resource saturation, availability filtering, seeding and validation.

```powershell
dotnet test KeyloopChallenge.sln
```

Show:

```text
Passed! - Failed: 0, Passed: 41, Skipped: 0, Total: 41
```

---

## 7:45–8:45 — Observability and close

**On screen:** terminal output of request logs; optionally enable the console exporter.

**Narration:**

> Every request is logged with method, path, status and latency. Expected domain rejections are warnings; unexpected failures are errors with stack traces. Customer names, VINs and emails are not logged.
>
> OpenTelemetry instruments ASP.NET Core and HTTP calls. The scheduler publishes booking-created and booking-rejected counters and a booking-duration histogram. The `Keyloop.Scheduler` meter is registered so any exporter or collector can pick it up. For a local demo, `Observability__ConsoleExporter=true` prints traces and metrics to the console.

```powershell
$env:Observability__ConsoleExporter = 'true'
# restart the API to see traces/metrics on stdout
```

Show briefly:

- `docs/SYSTEM_DESIGN.md` — data model, sequence diagram, trade-offs.
- `docs/openapi.json` — committed contract.
- `Keyloop.API/Keyloop.API.http` — runnable requests.

**Narration:**

> The design document explains the allocation invariant, the dealership-level lock, the availability algorithm, the seed model and the trade-offs I deliberately did not take on, such as payments, notifications, holiday calendars and customer-scoped authorization.
>
> To summarize: Scenario A is complete end to end, with a correct concurrency story, persistent confirmation, real-time availability, automated tests and a client contract that can be generated or exercised immediately.
>
> Thanks for watching.

---

## Recording checklist

- [ ] LocalDB running and database reset (optional, for a clean seed).
- [ ] API started from a clean terminal.
- [ ] Token, dealership ID, vehicle IDs and date checked against seed data.
- [ ] `dotnet test` result visible.
- [ ] OpenAPI and `.http` file visible.
- [ ] No secrets beyond the documented development demo credentials on screen.

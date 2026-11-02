# Unified Service Scheduler — System Design

## Architecture

```text
Blazor Server UI ──HTTP/JWT──> ASP.NET Core API
                                  │ Controllers / ProblemDetails
                                  ▼
                       Application (MediatR use cases)
                    validation → transaction → audit → handler
                                  ▼
                  Domain (Appointment, TimeInterval, allocator)
                                  ▼
             Infrastructure (EF Core, SQL Server, cache, telemetry)
```

The API is the authority for resource allocation. A booking reads reference data outside the transaction, then locks and rechecks conflicting appointments inside its transaction before assigning one bay and one qualified technician. This keeps the critical section short and makes allocation deterministic.

## Components

| Component | Responsibility |
|---|---|
| Domain | Pure overlap, opening-hours and allocation rules; no EF or HTTP dependencies. |
| Application | Commands/queries, validators and ports. Commands own the transaction boundary. |
| Infrastructure | SQL Server persistence, reference-data cache, seed data, audit persistence and Dapper read models. |
| API | Versioned REST contracts, JWT, health endpoints and ProblemDetails. |
| Web | Blazor Server booking flow and schedule/availability views. |

## Data flow: create appointment

1. API validates its request contract and dispatches `CreateAppointmentCommand`.
2. Application validates IDs/time grid, loads reference data, verifies vehicle ownership and opening hours.
3. In one transaction it locks the dealership, queries overlaps, invokes `ResourceAllocator`, and saves a confirmed appointment.
4. The audit behavior creates an audit row before commit; logging records IDs and elapsed time.
5. The API returns `201 Created` with the confirmed allocation.

## Technology choices

- .NET 9 / ASP.NET Core: versioned REST API and Blazor Server client.
- EF Core + SQL Server: transactions, locking and relational integrity; Dapper is retained for high-volume read models.
- MediatR + FluentValidation: explicit use cases and cross-cutting request behavior.
- JWT bearer authentication: simple testable API security; production should use a standards-compliant OIDC provider.

## Observability

Structured request logging includes IDs only, not customer PII. OpenTelemetry traces HTTP/database calls. Metrics include booking success/rejection counts with reason tags and booking duration. `live` checks process health while `ready` checks SQL connectivity.

## GenAI assistance

GenAI was used as a pair-programming aid to propose folder boundaries, draft repetitive DTO/configuration code and review transaction/audit ordering. Architectural choices, security defaults, domain invariants and all resulting code were reviewed and verified through builds/tests by the developer.

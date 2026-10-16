# RaceDay API (PROG6212 – Part 2)

RESTful API for the RaceDay event management system for road running, walking and cycling events. Built with ASP.NET Core 8 Web API, Entity Framework Core (Code-First) and SQL Server.

## Setup and run instructions

**Prerequisites:** .NET 8 SDK, SQL Server LocalDB (installed with Visual Studio) or any SQL Server instance, and the EF Core CLI (`dotnet tool install --global dotnet-ef`).

1. Clone the repository and open `RaceDay.sln` in Visual Studio (or use the CLI).
2. Check the connection string in `RaceDay.Api/appsettings.json` (defaults to LocalDB, database `RaceDayDb`).
3. The database and tables are created automatically on first run (no migration step needed).
4. Run the API:
   ```bash
   dotnet run --project RaceDay.Api
   ```
5. Open Swagger UI at `https://localhost:<port>/swagger`.
6. Run the unit tests (they use an in-memory database, so no SQL Server is needed):
   ```bash
   dotnet test
   ```

### Trying the API in Swagger
1. `POST /api/auth/register` to create an Organiser and a Participant account.
2. `POST /api/auth/login` – the session cookie is stored by the browser and sent automatically on every following request.
3. Call any endpoint. To switch roles, call `POST /api/auth/logout` and log in as the other user.

## Roles

| Role | Can do |
|---|---|
| **Organiser** | Create, update and delete their **own** events, categories and route information; view enrolments for their events; capture results. Cannot access Participant functionality. |
| **Participant** | Browse events, categories and routes; enrol in categories; view their own enrolments and results. Cannot access Organiser functionality or report their own results. |

Public (no login): `GET` events, categories and route information. Everything else requires an authenticated session.

## Authentication design
Server-side session authentication. On login the API stores the user's **ID and role in the server-side session** (ASP.NET Core `Session`); the client only holds an opaque `RaceDay.Session` id cookie. A custom authentication handler rebuilds the user's identity and role from the session on every request, so `[Authorize]` and role policies work throughout. Passwords are hashed with ASP.NET Core's `PasswordHasher` (PBKDF2) and never stored in plain text. Unauthenticated requests get `401`; wrong role or not the owner gets `403`.

## Documentation
The `docs/` folder holds the Part 1 ERD (`RaceDay_ERD.png`) and the API endpoint plan. Add your Part 1 SQL script here too.

## Endpoints
See `docs/RaceDay_API_Endpoint_Plan.md` (Part 1) – all endpoints are implemented, plus `POST /api/auth/logout` and `GET /api/categories/{id}`.

## CI/CD
GitHub Actions (`.github/workflows/dotnet.yml`) validates the repository structure, builds the solution and runs all unit tests on every push.

![Green build](docs/green-build.png) <!-- TODO: add screenshot of a successful workflow run -->

## Demo video
<!-- TODO: add unlisted YouTube link -->

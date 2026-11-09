# RaceDay – PROG6212 POE Parts 2 and 3

This solution combines the original Part 2 ASP.NET Core REST API and SQL Server data layer with the Part 3 ASP.NET Core MVC website. The MVC project uses the API for all data operations; it contains no EF Core/database access and does not connect to Azure directly.

## Solution structure

- `RaceDay.Api/` – Part 2 REST API, session authentication, EF Core models and controllers, plus Part 3 dashboard/profile/image endpoints.
- `RaceDay.Api.Tests/` – Part 2 and Part 3 API integration tests (authentication, event management, enrolments, results, dashboard, entrants, profile, and image-upload flow).
- `RaceDay.Web/` – Part 3 MVC front end for Organisers and Participants.
- `docs/Part3/Part3_Database_Upgrade.sql` – adds Part 3 columns to an existing Part 2 database.

## Requirements

- Visual Studio 2022 with the **ASP.NET and web development** workload.
- .NET 8 SDK.
- SQL Server LocalDB (included with the relevant Visual Studio SQL tooling) or a SQL Server instance.
- Docker Desktop only if testing the MVC container.
- Azure Storage account only if testing image uploads.

## First-time setup

1. Open `RaceDay.sln` in Visual Studio 2022.
2. Check `RaceDay.Api/appsettings.json` and update `ConnectionStrings:DefaultConnection` if your SQL Server is not LocalDB.
3. If you already have a Part 2 database, back it up and run `docs/Part3/Part3_Database_Upgrade.sql` against that database. A brand-new database is created by the API on first startup with the Part 3 fields included.
4. Configure Azure Blob Storage only when you are ready to test uploads. Do not commit the connection string. Use Visual Studio user-secrets or an environment variable named `AzureBlob__ConnectionString`; optionally set `AzureBlob__Container` to `raceday-images`. The storage account/container must permit the public blob access mode used to display image URLs.

Example user-secrets commands, run from the API project directory:

```bash
dotnet user-secrets init
dotnet user-secrets set "AzureBlob:ConnectionString" "YOUR_STORAGE_CONNECTION_STRING"
dotnet user-secrets set "AzureBlob:Container" "raceday-images"
```

## Run locally in Visual Studio

1. Right-click the solution and choose **Set Startup Projects**.
2. Select **Multiple startup projects**.
3. Set `RaceDay.Api` and `RaceDay.Web` to **Start** (API first is recommended).
4. Confirm the API HTTPS address in `RaceDay.Api/Properties/launchSettings.json` is `https://localhost:7100`.
5. Confirm `RaceDay.Web/appsettings.json` has `Api:BaseUrl` set to `https://localhost:7100/`.
6. Run the solution. Swagger opens at `https://localhost:7100/swagger`; the MVC site uses its own launch URL shown by Visual Studio.
7. Register an Organiser and a Participant from the MVC website, then sign in to test each role.

If the API HTTPS development certificate is not trusted, run `dotnet dev-certs https --trust` from a Developer PowerShell/terminal, then restart Visual Studio.

## Docker – MVC website

The API must be running at a URL reachable from the Docker container. `localhost` inside the container refers to the container itself, not the host computer. For local Docker Desktop testing, use `https://host.docker.internal:7100/` only if the API is listening on an accessible host interface and its HTTPS certificate is trusted by the container; otherwise host the API over HTTPS and use its reachable URL.

From the repository root:

```bash
docker build -t raceday-web ./RaceDay.Web && docker run --rm -p 8080:8080 -e Api__BaseUrl=https://YOUR-REACHABLE-API-URL/ raceday-web
```

Open `http://localhost:8080`. Replace the placeholder with the actual reachable API base URL. The MVC container does not contain the API database or Azure credentials.

## API endpoints added for Part 3

- `GET /api/events/browse` and `GET /api/events/{id}/detail`
- `PUT /api/events/{id}/meta` and `POST /api/events/{id}/banner`
- `GET /api/organiser/dashboard`
- `GET /api/events/{id}/entrants` and `PUT /api/enrolments/{id}/status`
- `GET /api/enrolments/me/detail` and `GET /api/results/me/detail`
- `GET /api/users/me/profile` and `POST /api/users/me/picture`

Existing Part 2 endpoints for event CRUD, categories, enrolment, profile editing, authentication and results are retained.

## CI/CD and tests

GitHub Actions builds the complete solution, runs the API integration tests, and builds the MVC Docker image. The test project starts the real API pipeline with an isolated EF Core InMemory database. Azure Blob Storage is replaced by a deterministic fake during tests, so tests do not require an Azure account or upload files to the cloud.

Part 3 API test coverage includes:
- Public event browsing and Part 3 event metadata.
- Organiser dashboard ownership and enrolment totals.
- Entrant listing, enrolment status updates, and ownership restrictions.
- Participant-only enrolment and race-history details, including total finishers.
- Logged-in profile retrieval.
- Event banner upload flow, persisted URL, and organiser ownership checks.

Run locally with:

```bash
dotnet restore RaceDay.sln
dotnet build RaceDay.sln -c Release
dotnet test RaceDay.sln -c Release
```

## Submission video

Add the unlisted YouTube demonstration URL here after recording: `YOUR_UNLISTED_YOUTUBE_LINK`.

## Important verification note

The source integration and project structure are prepared, but the final .NET build, database upgrade, Azure upload, and Docker run must be executed in Visual Studio/Docker with your local SQL Server and Azure configuration. Do not claim these have passed until the checks are completed.

## Remaining submission tasks

- Make at least 20 meaningful commits on the required Part 3 branch; do not manufacture empty commits. Commit coherent features, fixes, tests, documentation and CI changes as they are completed.
- Run the full build and API test suite on the development computer.
- Test Azure uploads with a real configured Storage account and verify that both banner and profile picture URLs display.
- Test both roles end-to-end, including event CRUD, enrolment status, results after the event date, profile updates and logout.
- Run the Docker image against a reachable API and record the successful run for the demonstration video.

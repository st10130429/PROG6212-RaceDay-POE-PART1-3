# RaceDay – Part 3: MVC Web Application

ASP.NET Core 8 MVC front end for the RaceDay platform. It consumes the Part 2 REST API for **all** data operations (no database access or duplicated business logic in the MVC project), uses session-based authentication with separate Organiser and Participant interfaces, stores images in **Azure Blob Storage through the API**, and runs in **Docker**.

## Roles
| Role | Can do |
|---|---|
| **Organiser** | Dashboard (events, enrolment totals, upcoming dates), create/edit/delete events with banner upload, manage categories, view/confirm enrolments, capture and publish results |
| **Participant** | Browse & filter upcoming events, view event details/categories, enrol, see own enrolments (colour-coded status), see race history (e.g. "47th out of 312 finishers"), edit profile and upload a profile picture |

## Architecture
```
Browser -> RaceDay.Web (MVC, session) -> RaceDay.Api (REST, session cookie) -> SQL Server
                                                   \-> Azure Blob Storage (images)
```
* On login the MVC app stores the user's id, name and role in its session and keeps the API's session cookie so every later call is made as that user.
* `RoleRequired` filter: unauthenticated users go to Login; wrong-role users are redirected to their own home.
* Invalid input is caught by model validation and by friendly API-error mapping – raw errors are never shown.

## Run locally
```bash
# 1. Start the API (see Part2/README.md and Part3/ApiAdditions/API_CHANGES.md)
# 2. Point the MVC app at it, then run:
cd Part3/RaceDay.Web
dotnet run   # set Api:BaseUrl in appsettings.json or env var Api__BaseUrl
```

## Run with Docker (single command)
```bash
docker build -t raceday-web Part3/RaceDay.Web && docker run -p 8080:8080 -e Api__BaseUrl=https://<your-hosted-api>/ raceday-web
```
Then open http://localhost:8080.

## CI/CD
GitHub Actions (`.github/workflows/ci.yml`) builds and tests the API, builds the MVC project and builds the Docker image on every push.

![Green build](docs/CI-GREEN-BUILD.PNG)

> Replace this screenshot with a new one after your Part 3 run turns green.

## Video demonstration
Unlisted YouTube link: _add your link here_

# RaceDay - PROG6212 POE - Part 3

Part 3 builds on Part 2. Part 2 handled Auth + Events CRUD + Roles.
Part 3 adds the full RaceDay experience: public browsing, event detail,
organiser dashboard, entrants, participant enrolments, results and profile.

## Tech Stack
- C# / ASP.NET Core Web API
- Entity Framework Core - InMemory / SQL Server
- Swagger / OpenAPI for testing
- Cookie Authentication - login via `/api/auth/login` sets session cookie
- Role-based Authorization - Organiser / Participant

## Projecture

## How To Run
1. Open `RaceDay.API.sln` in Visual Studio
2. Press **F5** (or Ctrl+F5)
3. Open Swagger:
   `https://localhost:7100/swagger`
4. **IMPORTANT:** Log in FIRST:
   - `POST /api/auth/login`
   - Use Organiser or Participant account
   - Swagger will then send the session cookie automatically for all other calls

## Test Users
| Role | Email | Password |
| --- | --- | --- |
| Organiser | organiser@test.com | Test123! |
| Participant | participant@test.com | Test123! |

> Use these exact users for all Part 3 screenshots so results are repeatable.

## Part 3 Endpoints - All Tested

### 1. Public Browsing
| Method | Endpoint | Role | Expected |
| --- | --- | --- | --- |
| GET | `/api/events/browse` | Public | 200 - List of events with type, distance, banner |
| GET | `/api/events/{id}/detail` | Public | 200 - Full event detail + entrants count |

### 2. Organiser - Manage Events
| Method | Endpoint | Role | Expected |
| --- | --- | --- | --- |
| PUT | `/api/events/{id}/meta` | Organiser (owner only) | 200 - Updates title, description, date, location |
| POST | `/api/events/{id}/banner` | Organiser (owner only) | 201 - Uploads / replaces event banner |
| GET | `/api/organiser/dashboard` | Organiser | 200 - My events, total entrants, upcoming events |
| GET | `/api/organiser/events/{id}/entrants` | Organiser (owner only) | 200 - List of entrants for my event |

### 3. Participant - Enrolments & Results
| Method | Endpoint | Role | Expected |
| --- | --- | --- | --- |
| GET | `/api/participant/enrolments/{id}/status` | Participant (own only) | 200 - Enrolment status: Pending / Confirmed / Cancelled |
| GET | `/api/participant/enrolments/my` | Participant | 200 - All my enrolments |
| GET | `/api/participant/results/my` | Participant | 200 - My race results + times + positions |

### 4. Participant - Profile
| Method | Endpoint | Role | Expected |
| --- | --- | --- | --- |
| GET | `/api/participant/profile/my` | Participant | 200 - My profile: name, email, age, club |
| POST | `/api/participant/profile/picture` | Participant | 201 - Upload profile picture |

## Authorization Rules Tested
- Unauthenticated user -> 401 Unauthorized on protected endpoints
- Participant trying to access Organiser dashboard -> 403 Forbidden
- Organiser trying to edit another Organiser's event -> 403 Forbidden
- Participant trying to view another Participant's enrolment -> 403 Forbidden
- Organiser trying to view `/api/participant/...` -> 403 Forbidden

## How To Test In Swagger (Step-by-Step)
1. `POST /api/auth/login` as Organiser -> 200
2. `GET /api/events/browse` -> 200 - verify banner + distance show
3. `GET /api/events/1/detail` -> 200
4. `PUT /api/events/1/meta` - change title -> 200
5. `POST /api/events/1/banner` - upload image -> 201
6. `GET /api/organiser/dashboard` -> 200
7. `GET /api/organiser/events/1/entrants` -> 200
8. Log out, login as Participant
9. `GET /api/participant/enrolments/my` -> 200
10. `GET /api/participant/results/my` -> 200
11. `GET /api/participant/profile/my` -> 200
12. `POST /api/participant/profile/picture` -> 201

## Evidence
All proof screenshots are in:
`Part3/screenshots/`
- `00-swagger-overview-part3.png` - All Part 3 endpoints visible
- `01-browse-200.png` to `11-upload-picture-201.png` - One per endpoint, showing Request + 200/201 Response

## Notes / Known Limitations
- Banners and profile pictures are stored as file path / URL, not in DB blob
- InMemory DB resets on restart - re-seed test users/events if empty
- Swagger cookie auth is used for testing - production would use JWT

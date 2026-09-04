# RaceDay – API Endpoint Plan (Part 1)

This document lists every planned REST API endpoint for the RaceDay system, to be implemented in Part 2. It covers Authentication, User Profile, Events, Categories, Event Enrolments, Results, and Route Information.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new user account as Organiser or Participant | None (public) | { name, email, password, role } | 201 Created – user created; 409 Conflict – email already exists |
| POST | /api/auth/login | Authenticates a user and returns a token | None (public) | { email, password } | 200 OK – token + user info; 401 Unauthorized – invalid credentials |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the logged-in user's own profile | Any (logged in) | None | 200 OK – user profile |
| PUT | /api/users/me | Updates the logged-in user's own profile details | Any (logged in) | { name, email } | 200 OK – updated profile; 400 Bad Request – invalid data |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all upcoming events | None (public) | None | 200 OK – list of events |
| GET | /api/events/{id} | Gets details of a specific event | None (public) | None | 200 OK – event details; 404 Not Found |
| POST | /api/events | Creates a new event | Organiser | { name, description, location, eventDate } | 201 Created – event created |
| PUT | /api/events/{id} | Updates an event the Organiser owns | Organiser | { name, description, location, eventDate } | 200 OK – updated; 403 Forbidden – not owner; 404 Not Found |
| DELETE | /api/events/{id} | Deletes an event the Organiser owns | Organiser | None | 204 No Content; 403 Forbidden; 404 Not Found |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists categories for an event | None (public) | None | 200 OK – list of categories |
| POST | /api/events/{eventId}/categories | Adds a category to an event | Organiser | { name, distanceKm, price } | 201 Created; 403 Forbidden – not owner |
| PUT | /api/categories/{id} | Updates a category | Organiser | { name, distanceKm, price } | 200 OK; 403 Forbidden; 404 Not Found |
| DELETE | /api/categories/{id} | Deletes a category | Organiser | None | 204 No Content; 403 Forbidden; 404 Not Found |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/categories/{categoryId}/enrolments | Enrols the logged-in Participant into a category | Participant | None | 201 Created – enrolment created; 409 Conflict – already enrolled |
| GET | /api/enrolments/me | Lists the logged-in Participant's own enrolments | Participant | None | 200 OK – list of enrolments |
| GET | /api/events/{eventId}/enrolments | Lists all enrolments for an event (organiser view) | Organiser | None | 200 OK – list of enrolments; 403 Forbidden – not owner |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/results | Captures a result for a specific enrolment | Organiser | { finishTime, position } | 201 Created; 403 Forbidden – not event owner; 409 Conflict – result already recorded |
| GET | /api/results/me | Lists the logged-in Participant's own result history | Participant | None | 200 OK – list of results |

## Route Information

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/categories/{categoryId}/route | Gets route info (distance, elevation, map) for a category | None (public) | None | 200 OK – route details; 404 Not Found |
| POST | /api/categories/{categoryId}/route | Adds/sets route info for a category | Organiser | { distanceKm, elevationGain, mapUrl } | 201 Created; 403 Forbidden – not owner |

## Design Notes

- Organisers can only manage (update/delete) events, categories, results, and route information tied to events they own. This is enforced via 403 Forbidden responses and will be implemented as role + ownership checks at the API level in Part 2.
- Participants cannot self-report their own results; only the Organiser of the event can capture results, matching the real-world workflow of a race timing/results process.
- All GET endpoints returning public event/category/route data require no authentication, so participants can browse before registering.
- Role-based access enforced here at the API level (Part 2) will be reflected consistently in the MVC interface in Part 3.

# Part 2 - Screenshots Evidence - LIBPC082 - ST10130429

This folder contains Swagger test evidence for PROG6212 Part 2.

## Files in this folder

| File | What it proves | Rubric | Status |
|------|----------------|--------|--------|
| 02_Auth_Register_Organiser_201_Created.png | POST /api/auth/register works, returns 201 | Auth [20] | PASS |
| 04_Auth_Login_Participant_200_OK.png | POST /api/auth/login works, returns JWT + 200 OK | Auth [20] | PASS |
| 05b_Events_POST_As_Participant.png | POST /api/events creates event in DB | API + DB [25] | PASS |
| 06_Events_GET_ById_1_200_OK.png | GET /api/events/{id} retrieves from DB - 200 OK | API + DB [25] | PASS |
| 07_Unit_Tests_Passing.png | dotnet test results (to be added) | Unit Testing [15] | TODO |

## Evidence Details
- All screenshots taken from Swagger UI - https://localhost:5001/swagger
- Shows LIBPC082 in request where required
- Shows 201 Created and 200 OK status codes
- JWT token visible in login response
- Comrades2026 test data used

## Commit History
Commits include LIBPC082 identifier to avoid -5% penalty.

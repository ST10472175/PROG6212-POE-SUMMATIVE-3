# PROG6212 Part 3 — marking-criteria coverage map

| Brief section | Implementation in this solution | Evidence to capture |
|---|---|---|
| MVC application setup | `RaceDay.Mvc` is included in `RaceDay.sln`; all business/data operations call the Part 2 API through `RaceDayApiClient`; no MVC database connection or EF reference | Solution Explorer showing both API and MVC; source for `RaceDayApiClient`; browser running MVC |
| Authentication and role-based navigation | API-backed register/login/logout; API session cookie carried in per-user MVC session; MVC authentication cookie role claims; `[Authorize(Roles=...)]` for Organiser and Participant areas | Register both roles; sign in; show different navigation; attempt access to the other role's page |
| Organiser interface | Dashboard metrics; event create/edit/delete; banner upload; category add/rename/delete; event enrolment list with participant/category/status; result capture and correction | Capture Dashboard, event form, categories, enrolments and results pages |
| Participant interface | Upcoming-event catalogue with search/type/date filters; event details and category list; category-based enrolment; My Events with Pending/Confirmed status badges; My Results with date/category/time/position and total field size; editable profile and picture upload | Capture filtered event list, event details/enrolment, personal event history, results history and profile |
| Azure Blob Storage | API-only upload endpoints for event banners and participant profile pictures; generated blob names; size/type validation; image URLs saved in API-owned entities and displayed by MVC | Demonstrate uploading one event banner and one participant picture, then show both images displayed |
| Docker | Multi-stage `Dockerfile`; Compose stack for SQL Server, API and MVC; `.env.example`; documented Compose run command and exposed ports | Show `docker compose ps`, application browser and Swagger |
| UI design | Responsive Bootstrap layout with consistent RaceDay sporting theme, role-specific menus, accessible labels/alt text, visible enrolment states and validation/error feedback | Demonstrate desktop and narrow/mobile-width layout; show validation feedback |
| GitHub and CI/CD | Updated Actions workflow validates structure, restores and builds the full solution, runs the API regression tests and validates Compose YAML | Show successful latest Actions run after pushing the final version |
| Submission/video | README includes repository/demo submission instructions; `docs/VIDEO_DEMO_SCRIPT.md` provides the demonstration sequence | Add the actual GitHub URL to ARC and actual unlisted video URL to README |

## External configuration and evidence dependencies

The source archive cannot contain the user's Azure account credentials, GitHub access, actual repository commit history, or a recorded YouTube video. Configure Azure Storage, push the files to the user's repository, create meaningful commits during development, run the stack, and capture the evidence listed above before submission.

# Suggested meaningful commit sequence

The provided ZIP does not include its `.git` directory, so it cannot preserve or manufacture the earlier GitHub commit history. Apply these as separate, real commits in your repository as you stage the corresponding changes. Review the changes before committing.

1. `Add Part 3 MVC project to RaceDay solution` — add the MVC project and solution entry.
2. `Create shared RaceDay API HTTP client` — add typed API calls and per-session API cookie forwarding.
3. `Add MVC cookie authentication and role authorization` — configure authentication, roles and access-denied handling.
4. `Implement API-backed account registration and login` — add register, login and logout screens/actions.
5. `Add shared responsive RaceDay layout and navigation` — add the shared Razor layout, navigation and styling.
6. `Create organiser dashboard summary` — display event, enrolment and upcoming-event totals.
7. `Implement organiser event creation` — add validated event creation form and API call.
8. `Implement organiser event updates and deletion` — add edit/delete actions and views.
9. `Add event banner upload endpoint to API` — validate and save event banner images through Blob Storage.
10. `Add Azure Blob Storage profile picture support` — add participant picture upload through the API.
11. `Implement event category management for organisers` — add, rename and delete category screens.
12. `Add organiser enrolment view and status management` — list participants and categories, then confirm or return entries to Pending.
13. `Implement organiser result capture and correction` — add finish-time and position workflows.
14. `Add event search and filtering for participants` — provide catalogue filters.
15. `Implement participant event details and enrolment` — show categories and submit enrolment through the API.
16. `Add participant personal event history` — show event date, category, entry date and status.
17. `Add participant results history and profile page` — show event/date/category/time/position and profile update.
18. `Return event and category metadata with API results` — extend the result DTO for participant history.
19. `Add Docker Compose deployment for SQL API and MVC` — add Dockerfile, Compose and environment template.
20. `Extend GitHub Actions to build and test Part 3` — validate structure, build solution, run tests and validate Compose.

Use smaller follow-up commits for genuine bug fixes, tests, accessibility improvements and documentation updates. The rubric's 20-commit requirement is about meaningful development history; a ZIP containing only the final tree cannot prove that history.

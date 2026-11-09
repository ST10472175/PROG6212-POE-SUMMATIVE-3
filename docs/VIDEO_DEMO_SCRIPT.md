# Suggested unlisted YouTube demonstration script

Use this as a checklist while recording the actual running application. Keep the browser and Visual Studio/terminal visible when the section asks for them.

## 1. Introduction

"This is my PROG6212 Part 3 RaceDay application. It uses an ASP.NET Core MVC front end to consume the REST API developed in Part 2. The system supports Organisers and Participants, with separate role-based navigation and workflows."

## 2. Solution structure and architecture

Open the solution in Visual Studio. Show `RaceDay.Api`, `RaceDay.Mvc`, and `RaceDay.Tests`. Briefly show `RaceDayApiClient.cs` and explain that MVC communicates with the API and does not directly access SQL Server or Azure Blob Storage.

## 3. Registration, authentication and access control

Register or sign in as an Organiser. Show the organiser dashboard and organiser-only navigation. Sign out, register or sign in as a Participant, and show the participant navigation. Attempting to open an Organiser page while signed in as a Participant should show Access Denied.

## 4. Organiser workflow

As an Organiser, create an event with its name, description, date and time, location, distance and event type. Upload a banner image. Show the banner on the participant event details page after it is stored in Azure Blob Storage. Return to the organiser view and add, rename and delete a category. Open the enrolments list and show participant names, selected categories and the amber Pending status. Confirm an enrolment, show the green Confirmed status, and demonstrate returning it to Pending.

After using an event whose scheduled date has passed, open Results. Capture a participant's finish time and finishing position. Correct a result and show the updated result.

## 5. Participant workflow

Sign in as a Participant. Search the event catalogue and apply event-type/date filters. Open an event, inspect details/categories, choose a category and enrol. Show the new Pending entry in My Events, then confirm it as the Organiser and refresh My Events to show the updated Confirmed status. Update participant profile details and upload a profile picture. Show the image on the profile page. Show My Results with event name, event date, category, recorded time and finishing position out of the total number of entrants.

## 6. Docker setup

Show the root `Dockerfile` and `docker-compose.yml`. Run `docker compose ps` and show the SQL Server, API and MVC containers running. Open the MVC application at `http://localhost:5081` and Swagger at `http://localhost:5080/swagger`.

## 7. GitHub Actions and conclusion

Open the repository's latest successful GitHub Actions run. Explain that the workflow checks the project structure, restores/builds the solution, runs the API tests and validates Docker Compose. Finish by summarising how the MVC front end, API, Azure Blob Storage and Docker work together.
## 8. Recording preparation and final checks

Before recording:

- [ ] Start the API and MVC applications and confirm both are accessible.
- [ ] Confirm the database connection is working.
- [ ] Check that Azure Blob Storage is configured and image uploads work.
- [ ] Prepare working Organiser and Participant accounts.
- [ ] Prepare an event with a past date for the results demonstration.
- [ ] Confirm Docker Compose starts all required containers.
- [ ] Confirm the latest GitHub Actions workflow has passed.
- [ ] Close unrelated applications and notifications.
- [ ] Ensure the browser text and Visual Studio code are readable in the recording.
- [ ] Test the recording audio and screen visibility.
- [ ] Review the recording before uploading it as an unlisted YouTube video.
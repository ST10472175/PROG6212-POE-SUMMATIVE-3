# Part 3 submission checklist

## Application evidence

- [ ] Show the RaceDay MVC home page and responsive layout.
- [ ] Demonstrate registration and sign-in for both Participant and Organiser roles.
- [ ] Show that role navigation is different and that direct access to the other role's pages is blocked.
- [ ] As an Organiser, create an event with all required fields and upload a banner.
- [ ] Show the banner displayed on the event page after upload to Azure Blob Storage.
- [ ] Add, rename and delete event categories.
- [ ] Show participants enrolled in an event and their selected categories; confirm a Pending entry and show the green Confirmed and amber Pending states.
- [ ] After an event date has passed, capture a participant finish time and finishing position; correct a result.
- [ ] As a Participant, filter events, open event details, select a category and enrol.
- [ ] Show My Events and My Results, including event name, date, category, finish time and position.
- [ ] Update the participant profile and upload a profile picture through the API.

## Container and CI evidence

- [ ] Build and start the complete application with `docker compose up --build -d`.
- [ ] Show the SQL Server, API and MVC containers running.
- [ ] Open the MVC website and API Swagger page from the published ports.
- [ ] Show the Dockerfile and the Docker run/Compose instructions in README.
- [ ] Show a successful GitHub Actions build-and-test run after the final changes.
- [ ] Confirm at least 20 meaningful commits are visible in the actual GitHub repository.

## Submission

- [ ] Push the complete source code and Docker configuration to GitHub.
- [ ] Add the GitHub repository link to ARC.
- [ ] Record the unlisted YouTube demonstration covering the full running application, MVC/API structure, Blob Storage and Docker setup.
- [ ] Replace the YouTube URL placeholder in README with the actual unlisted video URL.

## Final verification

- [ ] Confirm that `README.md` matches the current project structure.
- [ ] Check that `.env.example` contains all required configuration keys and no real secrets.
- [ ] Verify that no connection strings, passwords, API keys or other secrets are committed.
- [ ] Run the automated test suite and confirm that all tests pass.
- [ ] Confirm that the latest changes have been committed and pushed to the `main` branch.
- [ ] Open the GitHub repository in a browser and verify that the submitted files are visible.
- [ ] Test the unlisted YouTube link and confirm that the video is accessible.
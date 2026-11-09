# RaceDay — PROG6212 Part 3

RaceDay is a role-based event management web application for running, walking and cycling events. The Part 3 solution adds an ASP.NET Core MVC front end to the existing Part 2 REST API. The MVC application communicates with the API over HTTP and does **not** connect to SQL Server or contain a second copy of the business rules.

## Table of contents

```text
RaceDay/
├── RaceDay.sln
├── README.md
├── global.json                     Pins CLI builds to the .NET 8 SDK family
├── Dockerfile                      Multi-stage .NET 8 container build
├── docker-compose.yml              SQL Server + API + MVC application
├── .env.example                    Environment-variable template (no real keys)
├── .github/workflows/dotnet.yml    Repository checks, restore, build, tests and Compose validation
├── RaceDay.Api/                    Part 2 REST API
│   ├── Controllers/                Auth, profile, events, categories, enrolments and results
│   ├── Data/RaceDayContext.cs       Entity Framework Core database context
│   ├── Dtos/                       API request and response contracts
│   ├── Models/                     Database entities and roles
│   └── Services/BlobStorageService.cs
├── RaceDay.Mvc/                    Part 3 ASP.NET Core MVC application
│   ├── Controllers/                Account, Home, Organiser and Participant
│   ├── Models/                     MVC form models and API DTOs
│   ├── Services/RaceDayApiClient.cs
│   ├── Views/                      Role-specific Razor pages and shared layout
│   └── wwwroot/css/site.css        Consistent responsive RaceDay styling
├── RaceDay.Tests/                  Part 2 API xUnit regression tests
└── docs/
    ├── COMMIT_PLAN.md              Suggested meaningful commit sequence
    ├── RUBRIC_COVERAGE.md          Marking-criteria implementation map
    ├── SUBMISSION_CHECKLIST.md     Evidence and final submission checklist
    ├── UPGRADE_FROM_PART2.sql      Existing database schema update
    ├── VIDEO_DEMO_SCRIPT.md        Unlisted-video walkthrough script
    └── DOTNET8_MIGRATION.md       Framework/package migration notes
```

## Requirements implemented

### MVC application and API integration

- `RaceDay.Mvc` is an ASP.NET Core 8 MVC project included in `RaceDay.sln`.
- MVC uses `RaceDayApiClient` for every API call. It does not reference Entity Framework, SQL Server or the API project.
- The API session cookie is stored in the current MVC session and forwarded on server-to-server API calls. The cookie is not stored in one global `CookieContainer`, which would allow one visitor's API session to be reused by another visitor.
- The MVC app maintains its own authenticated identity with a secure, HttpOnly authentication cookie and role claims.

### Authentication and role-specific navigation

- Users register and sign in through `/Account/Register` and `/Account/Login`, which call `POST /api/auth/register` and `POST /api/auth/login`.
- The API remains responsible for BCrypt password hashing, role assignment and API session creation.
- Organiser and Participant pages are protected with role authorization. Unauthenticated users are redirected to sign in; a user with the wrong role receives the Access Denied page.
- The navigation changes according to the authenticated role. Both roles can sign out.
- Organiser registration requires the organiser code configured by the API.

### Organiser interface

- Dashboard: total events, total enrolments, and upcoming events.
- Event management: create, view, update and delete events with name, description, date/time, location, distance and event type.
- Event banner upload: select JPG, PNG or WEBP (maximum 5 MB); the MVC application posts the image to the API, and the API stores it in Azure Blob Storage.
- Category management: add, rename and delete age/distance categories for an event.
- Enrolments: view enrolled participants, selected category and enrolment status for an event. New entries start as Pending; the event Organiser can confirm or return an entry to Pending, and both roles see the colour-coded status.
- Results: record participant finish time and position, and correct a previously recorded result. The capture form is presented after the event date has passed.

### Participant interface

- Event catalogue with text search, event-type filtering and a start-date filter.
- Event details page with description, date, location, distance, organiser, banner and available categories.
- Enrol in an event by selecting a category; the API writes the enrolment immediately and prevents duplicate entries.
- My Events lists the participant's events, event date, selected category, entry date and live enrolment status (Pending or Confirmed).
- My Results lists event name, event date, enrolled category, finish time and finishing position.
- Profile page updates personal details and uploads a profile picture through the API to Azure Blob Storage.

### Azure Blob Storage

- The MVC project sends files only to the Part 2 API. It never connects directly to Azure.
- The API validates the file size and image content type, generates a random blob name and stores event banners in `event-banners` and participant photos in `profile-pictures`.
- Image URLs are saved on the related Event or Participant entity and returned in API DTOs for display in MVC.
- The containers are created with Blob-level read access so event banners and profile pictures can be rendered by the browser. Configure the storage account to permit anonymous blob access before uploading images. Do not put a real account connection string or account key in source control.

## Run locally with .NET 8

**Prerequisites:** .NET 8 SDK, SQL Server LocalDB (or the supplied SQL Server Docker service), and an Azure Storage account for image uploads. **Support notice:** Microsoft ends .NET 8 support on 10 November 2026; this target is retained at your request for assignment compatibility. For Visual Studio, use Visual Studio 2022 version 17.8 or later with the .NET 8 SDK installed. You can also build and test using the .NET CLI from PowerShell. Download the current .NET 8 SDK from [Microsoft's .NET 8 download page](https://dotnet.microsoft.com/download/dotnet/8.0).

1. Open `RaceDay.sln` in Visual Studio.
2. Set the `RaceDay.Api` project URL to `http://localhost:5080` (the supplied launch profile already uses this address). Keep `RaceDay.Mvc` at `http://localhost:5081`.
3. Check `RaceDay.Api/appsettings.json` and confirm `ConnectionStrings:DefaultConnection` points to your LocalDB instance. The API uses `EnsureCreated()` because the supplied Part 2 archive did not contain migration files; the database schema is created on first start. If you already have a database created from an earlier Part 2 version, run `docs/UPGRADE_FROM_PART2.sql` against it before starting the updated API. The script adds the two nullable image URL columns and the Pending/Confirmed enrolment status column only when they are missing.
4. Set the `AzureBlobStorage__ConnectionString` environment variable to the Azure Storage account connection string. Set `Registration__OrganiserCode` to a private organiser registration code; no organiser code is enabled by default in the checked-in settings. Do not commit these values.
5. Start `RaceDay.Api` first. Confirm Swagger opens at `http://localhost:5080/swagger`.
6. Start `RaceDay.Mvc`. Open `http://localhost:5081` and register either a Participant or an Organiser. Use the configured organiser code for an Organiser account.
7. To upload images, make sure the configured Azure Storage account allows Blob anonymous read access. The API creates the two containers when the first image is uploaded.
8. Run the existing API regression tests with `dotnet test RaceDay.sln --configuration Release`.

If no Azure connection string is configured, the MVC application and non-image workflows still run, but image-upload requests return a clear HTTP 503 configuration message.

## Run with Docker Compose

**Prerequisites:** Docker Desktop with Linux containers enabled and an Azure Storage account for image uploads.

1. Copy `.env.example` to `.env`.
2. Replace `SA_PASSWORD` and `ORGANISER_CODE` with strong values from your own environment. Set `AZURE_BLOB_CONNECTION_STRING` for image upload support. Compose requires the SQL password and organiser code; it will stop with a clear configuration error if either is missing. The `.env` file is ignored by Git; never commit it.
3. From the solution root, run:

   ```bash
   docker compose up --build -d
   ```

4. Open the MVC application at `http://localhost:5081`.
5. The API Swagger page is at `http://localhost:5080/swagger`.
6. View logs with `docker compose logs -f api mvc` and stop the stack with `docker compose down`. To remove the SQL Server data volume as well, use `docker compose down -v` (this permanently deletes the local database data).

The API creates the RaceDay database schema on first startup. The MVC container calls the API using the internal Docker address `http://api:8080`; it does not call the database container directly.

### Build and run individual containers with Docker commands

The Compose command above is recommended because it starts all three services on one network. When connecting to an already running SQL Server and API, the MVC image can also be built and started by itself:

```bash
docker build -t raceday-mvc --build-arg PROJECT=RaceDay.Mvc -f Dockerfile .
docker run --rm --name raceday-mvc -p 5081:8080 -e Api__BaseUrl=http://host.docker.internal:5080/ raceday-mvc
```

The API image can be built and run separately when a SQL Server instance is already available:

```bash
docker build -t raceday-api --build-arg PROJECT=RaceDay.Api -f Dockerfile .
docker run --rm --name raceday-api -p 5080:8080 -e "ConnectionStrings__DefaultConnection=Server=host.docker.internal,1433;Database=RaceDayDb;User Id=sa;Password=YOUR_SQL_PASSWORD;TrustServerCertificate=True" -e "Registration__OrganiserCode=YOUR_PRIVATE_ORGANISER_CODE" -e "AzureBlobStorage__ConnectionString=YOUR_AZURE_STORAGE_CONNECTION_STRING" raceday-api
```

Replace the example values with your own database and Azure Storage settings. The single-command full-stack option remains `docker compose up --build -d`.

## CI/CD

The repository pins CLI selection to the .NET 8 SDK family through `global.json`; Docker uses the .NET 8 SDK and ASP.NET runtime images. The GitHub Actions workflow `.github/workflows/dotnet.yml` runs on pushes and pull requests. It validates the required Part 3 files, restores the solution, builds the API, MVC and tests in Release mode, runs the API xUnit regression suite, and validates the Docker Compose configuration.

## Automated PowerShell validation

Run `Test-RaceDay.ps1` from PowerShell to restore NuGet packages, build the API/MVC/tests, execute the xUnit suite, publish both applications, validate the Docker Compose configuration, and build the Docker images when Docker Engine is available. The script writes `TestResults/RaceDay-Test-Report.txt` and a TRX test result file.

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\Test-RaceDay.ps1
```

To start the complete SQL Server + API + MVC Docker stack and test the Swagger and MVC HTTP addresses, run:

```powershell
.\Test-RaceDay.ps1 -RunDockerStack
```

Use `-KeepDockerStack` with `-RunDockerStack` to leave the containers running after the checks. Without that option, the script stops the containers but preserves the SQL Server data volume. The Docker stack test uses the project's `.env` file when present; otherwise, it uses temporary values copied from `.env.example` for a local-only test. Configure a real Azure Storage connection string to test image uploads. The script does not replace manual browser testing, a GitHub Actions run, or the required unlisted YouTube demonstration.

## Test coverage retained from Part 2

The existing xUnit suite covers registration and login, session authentication, role restrictions, event/category operations, enrolments, results and result corrections. The Part 3 changes preserve those API endpoints and response contracts while adding event banner/profile picture endpoints and the date/category metadata required by the MVC participant history.

## GitHub and submission requirements

- Push the complete Part 3 source to the Part 3 branch/folder of your GitHub repository.
- Ensure the latest GitHub Actions run is green and include a screenshot of the successful run in your evidence.
- The marking brief requires at least 20 meaningful commits. Use the suggested sequence in `docs/COMMIT_PLAN.md` as separate commits during development; do not create empty commits simply to increase the count.
- Submit your GitHub repository link on ARC.
- Record and upload an **unlisted YouTube demonstration** showing the running application, MVC structure, API integration, Azure Blob Storage image upload and Docker setup. Add the real video URL below before submitting.

**Unlisted YouTube demonstration:** _Add your actual video URL here after recording and uploading it._

## Validate the automated test suite

Run the dedicated xUnit test-suite validator from the extracted project folder:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\Test-RaceDay-TestSuite.ps1
```

The script checks the .NET 8 SDK, xUnit project configuration, expected test-suite files, restores and builds `RaceDay.Tests`, executes the tests, and verifies the generated TRX counters. It creates a timestamped `.trx`, `.log`, and text report in `TestResults/`.

Optional switches:

```powershell
.\Test-RaceDay-TestSuite.ps1 -Configuration Debug
.\Test-RaceDay-TestSuite.ps1 -NoRestore
.\Test-RaceDay-TestSuite.ps1 -NoBuild
```

`-NoBuild` assumes the matching configuration has already been built. This script tests the API xUnit test project; it does not replace browser-based checks of the MVC user interfaces, live Azure Blob Storage credentials, GitHub Actions, or the Docker demonstration.

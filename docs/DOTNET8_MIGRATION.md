# .NET 8 migration

The RaceDay API, MVC application and xUnit test project target `net8.0`. The repository's container images and GitHub Actions workflow also use .NET 8.

## Framework and package alignment

- API, MVC and tests: `net8.0`
- Entity Framework Core SQL Server, Design and InMemory: `8.0.33`
- ASP.NET Core MVC testing: `8.0.33`
- Microsoft.NET.Test.Sdk: `18.11.0`
- xUnit v2: `2.9.3`; Visual Studio runner: `4.0.1`
- Docker build/runtime images: `8.0`
- GitHub Actions SDK channel: `8.0.x`

The existing Swashbuckle package is intentionally retained because a major Swashbuckle upgrade changes its OpenAPI types and would require unrelated source changes.

## Local requirements

Install the .NET 8 SDK from <https://dotnet.microsoft.com/download/dotnet/8.0>. For Visual Studio, use Visual Studio 2022 version 17.8 or later with the .NET 8 SDK installed. From PowerShell in the solution root, run:

```powershell
dotnet --list-sdks
dotnet restore RaceDay.sln
dotnet build RaceDay.sln --configuration Release
dotnet test RaceDay.sln --configuration Release
```

The .NET 8 SDK installation is separate from changing the project target framework; both are required to build and run this solution locally.

## Test-host provider replacement fix

The integration-test factory removes the `RaceDayContext`, typed
`DbContextOptions<RaceDayContext>`, and non-generic `DbContextOptions` registrations
before re-registering the context with EF Core InMemory. This follows the EF Core 8
provider-replacement approach; `IDbContextOptionsConfiguration<TContext>` is not used
because that API is introduced in EF Core 9.

The API and test projects also pin `System.Security.Cryptography.Pkcs` to 8.0.2
to avoid the vulnerable 9.0.11 transitive version reported by NuGet audit.


> **Support lifecycle notice (8 November 2026):** Microsoft lists .NET 8 support ending on **10 November 2026**. This target is selected at your request, but it is at the end of its support window. Plan to move to a supported .NET release after the assignment if the application will remain in use.

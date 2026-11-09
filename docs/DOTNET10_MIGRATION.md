# .NET 10 migration

The RaceDay API, MVC application and xUnit test project target `net10.0`. The repository's container images and GitHub Actions workflow also use .NET 10.

## Framework and package alignment

- API, MVC and tests: `net10.0`
- Entity Framework Core SQL Server, Design and InMemory: `10.0.14`
- ASP.NET Core MVC testing: `10.0.14`
- Microsoft.NET.Test.Sdk: `18.11.0`
- xUnit v2: `2.9.3`; Visual Studio runner: `4.0.1`
- Docker build/runtime images: `10.0`
- GitHub Actions SDK channel: `10.0.x`

The existing Swashbuckle package is intentionally retained because a major Swashbuckle upgrade changes its OpenAPI types and would require unrelated source changes.

## Local requirements

Install the .NET 10 SDK from <https://dotnet.microsoft.com/download/dotnet/10.0>. Microsoft lists .NET 10 SDK support in Visual Studio 2026 version 18.0 or later. From PowerShell in the solution root, run:

```powershell
dotnet --list-sdks
dotnet restore RaceDay.sln
dotnet build RaceDay.sln --configuration Release
dotnet test RaceDay.sln --configuration Release
```

The .NET 10 SDK installation is separate from changing the project target framework; both are required to build and run this solution locally.

## Test-host provider replacement fix

The integration-test factory removes both `DbContextOptions<RaceDayContext>` and
`IDbContextOptionsConfiguration<RaceDayContext>` before registering EF Core InMemory.
Removing only the options descriptor leaves the API's SQL Server provider configuration
in dependency injection, which causes EF Core to reject the test host because two
providers are registered.

The API and test projects also pin `System.Security.Cryptography.Pkcs` to 10.0.14
to avoid the vulnerable 9.0.11 transitive version reported by NuGet audit.

[CmdletBinding()]
param(
    [switch]$RunDockerStack,
    [switch]$KeepDockerStack,
    [switch]$SkipDockerImageBuild,
    [int]$HealthTimeoutSeconds = 240
)

# RaceDay Part 3 validation script.
# Run this file from any working directory; paths are resolved from this script.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ResultsDirectory = Join-Path $ProjectRoot 'TestResults'
$ReportPath = Join-Path $ResultsDirectory 'RaceDay-Test-Report.txt'
$script:CheckResults = New-Object System.Collections.Generic.List[object]
$script:LastCheckPassed = $false
$script:FailureCount = 0
$script:SkipCount = 0
$script:WarningCount = 0
$script:TempEnvFile = $null
$script:StackStarted = $false
$script:StackEnvFile = $null

function Add-CheckResult {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][ValidateSet('PASS', 'FAIL', 'SKIP', 'WARN')][string]$Status,
        [Parameter(Mandatory = $true)][string]$Details
    )

    $script:CheckResults.Add([PSCustomObject]@{
        Time    = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        Status  = $Status
        Check   = $Name
        Details = ($Details -replace '[\r\n]+', ' ').Trim()
    }) | Out-Null

    $colour = switch ($Status) {
        'PASS' { 'Green' }
        'FAIL' { 'Red' }
        'SKIP' { 'Yellow' }
        'WARN' { 'DarkYellow' }
    }
    Write-Host ("[{0}] {1} - {2}" -f $Status, $Name, $Details) -ForegroundColor $colour

    switch ($Status) {
        'FAIL' { $script:FailureCount++ }
        'SKIP' { $script:SkipCount++ }
        'WARN' { $script:WarningCount++ }
    }
}

function Invoke-Check {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][scriptblock]$Action
    )

    $script:LastCheckPassed = $false
    Write-Host "`n--- $Name ---" -ForegroundColor Cyan
    try {
        & $Action
        $script:LastCheckPassed = $true
        Add-CheckResult -Name $Name -Status 'PASS' -Details 'Completed successfully.'
    }
    catch {
        Add-CheckResult -Name $Name -Status 'FAIL' -Details $_.Exception.Message
    }
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList
    )

    & $FilePath @ArgumentList
    $nativeExitCode = $LASTEXITCODE
    if ($nativeExitCode -ne 0) {
        throw ("Command '{0} {1}' exited with code {2}." -f $FilePath, ($ArgumentList -join ' '), $nativeExitCode)
    }
}

function Wait-ForHttpEndpoint {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][int]$TimeoutSeconds
    )

    $stopAt = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $stopAt) {
        try {
            $response = Invoke-WebRequest -Uri $Uri -Method Get -UseBasicParsing -TimeoutSec 8
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400) {
                return $true
            }
        }
        catch {
            # The services may still be starting. Retry until the timeout expires.
        }
        Start-Sleep -Seconds 3
    }
    return $false
}

function Get-HttpStatusCode {
    param([Parameter(Mandatory = $true)][string]$Uri)

    try {
        $response = Invoke-WebRequest -Uri $Uri -Method Get -UseBasicParsing -MaximumRedirection 0 -TimeoutSec 10 -ErrorAction Stop
        return [int]$response.StatusCode
    }
    catch {
        $httpResponse = $_.Exception.Response
        if ($null -eq $httpResponse) {
            throw ("Could not obtain an HTTP response from {0}: {1}" -f $Uri, $_.Exception.Message)
        }
        return [int]$httpResponse.StatusCode
    }
}

function Invoke-DockerCompose {
    param([Parameter(Mandatory = $true)][string[]]$ComposeArguments)

    $composeArgs = @('compose', '--env-file', $script:StackEnvFile) + $ComposeArguments
    Invoke-NativeCommand -FilePath 'docker' -ArgumentList $composeArgs
}

Push-Location $ProjectRoot
try {
    Write-Host 'RaceDay Part 3 - automated validation' -ForegroundColor White
    Write-Host "Project folder: $ProjectRoot"
    Write-Host 'The script does not delete the SQL Server Docker volume or modify source files.'

    # 1. Required project structure and marking-evidence files.
    Invoke-Check -Name 'Required project structure' -Action {
        $requiredPaths = @(
            'README.md', 'global.json', 'RaceDay.sln', 'Dockerfile', 'docker-compose.yml', '.env.example',
            'RaceDay.Api/RaceDay.Api.csproj', 'RaceDay.Api/Program.cs', 'RaceDay.Api/Controllers',
            'RaceDay.Api/Models', 'RaceDay.Api/Data', 'RaceDay.Api/Dtos',
            'RaceDay.Mvc/RaceDay.Mvc.csproj', 'RaceDay.Mvc/Program.cs', 'RaceDay.Mvc/Controllers',
            'RaceDay.Mvc/Views', 'RaceDay.Mvc/Services/RaceDayApiClient.cs',
            'RaceDay.Tests/RaceDay.Tests.csproj', '.github/workflows/dotnet.yml',
            'docs/RUBRIC_COVERAGE.md', 'docs/SUBMISSION_CHECKLIST.md', 'docs/VIDEO_DEMO_SCRIPT.md', 'docs/DOTNET8_MIGRATION.md'
        )
        $missing = @($requiredPaths | Where-Object { -not (Test-Path (Join-Path $ProjectRoot $_)) })
        if ($missing.Count -gt 0) {
            throw ('Missing required paths: ' + ($missing -join ', '))
        }
        Write-Host ("Found all {0} required paths." -f $requiredPaths.Count)
    }

    # 2. Validate JSON files and a few source-level project requirements.
    Invoke-Check -Name '.NET 8 target frameworks' -Action {
        foreach ($projectRelativePath in @('RaceDay.Api/RaceDay.Api.csproj', 'RaceDay.Mvc/RaceDay.Mvc.csproj', 'RaceDay.Tests/RaceDay.Tests.csproj')) {
            [xml]$projectXml = Get-Content -LiteralPath (Join-Path $ProjectRoot $projectRelativePath) -Raw
            $target = [string]$projectXml.Project.PropertyGroup.TargetFramework
            if ($target -ne 'net8.0') {
                throw "$projectRelativePath must target net8.0; found '$target'."
            }
        }
        Write-Host 'API, MVC and test projects all target net8.0.'
    }

    Invoke-Check -Name 'JSON configuration files' -Action {
        $jsonFiles = @(
            'RaceDay.Api/appsettings.json',
            'RaceDay.Api/Properties/launchSettings.json',
            'RaceDay.Mvc/Properties/launchSettings.json'
        )
        foreach ($file in $jsonFiles) {
            $null = Get-Content -LiteralPath (Join-Path $ProjectRoot $file) -Raw | ConvertFrom-Json
            Write-Host "Valid JSON: $file"
        }
    }

    Invoke-Check -Name 'MVC/API separation and Razor page coverage' -Action {
        $mvcSource = Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'RaceDay.Mvc') -Recurse -File -Filter '*.cs'
        $databaseReferences = @($mvcSource | Select-String -Pattern 'Microsoft\.EntityFrameworkCore|\bDbContext\b|UseSqlServer' -ErrorAction SilentlyContinue)
        if ($databaseReferences.Count -gt 0) {
            throw 'MVC source contains direct Entity Framework/database references. MVC must access data through the API.'
        }

        $viewCount = @(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'RaceDay.Mvc/Views') -Recurse -File -Filter '*.cshtml').Count
        if ($viewCount -lt 15) {
            throw "Expected at least 15 Razor views; found $viewCount."
        }
        Write-Host "MVC has $viewCount Razor views and no direct EF Core/SQL Server references."
    }

    Invoke-Check -Name 'GitHub Actions workflow coverage' -Action {
        $workflowPath = Join-Path $ProjectRoot '.github/workflows/dotnet.yml'
        $workflow = Get-Content -LiteralPath $workflowPath -Raw
        foreach ($requiredText in @('dotnet-version: 8.0.x', 'dotnet restore', 'dotnet build', 'dotnet test', 'docker compose config')) {
            if ($workflow -notmatch [regex]::Escape($requiredText)) {
                throw "GitHub Actions workflow is missing '$requiredText'."
            }
        }
        Write-Host 'Workflow provisions .NET 8 and contains restore, build, test and Docker Compose checks.'
    }

    # 3. .NET SDK restore, build, tests, and publishing.
    $dotnetCommand = Get-Command 'dotnet' -ErrorAction SilentlyContinue
    $dotnetAvailable = $null -ne $dotnetCommand
    $sdk8Available = $false

    if ($dotnetAvailable) {
        Invoke-Check -Name '.NET 8 SDK availability' -Action {
            $sdkList = & dotnet --list-sdks
            if ($LASTEXITCODE -ne 0) {
                throw 'dotnet --list-sdks failed.'
            }
            $sdk8 = @($sdkList | Where-Object { $_ -match '^8\.' })
            if ($sdk8.Count -eq 0) {
                throw 'The .NET 8 SDK was not found. Install the .NET 8 SDK and rerun this script.'
            }
            $activeSdk = [string](& dotnet --version 2>&1)
            if ($LASTEXITCODE -ne 0 -or $activeSdk -notmatch '^8\.') {
                throw "The selected SDK is '$activeSdk', not .NET 8. Check global.json and install the .NET 8 SDK."
            }
            Write-Host "Selected SDK: $activeSdk"
            $sdk8 | ForEach-Object { Write-Host "Found SDK: $_" }
            $script:DotNet8Found = $true
        }
        $sdk8Available = [bool]$script:LastCheckPassed
    }
    else {
        Add-CheckResult -Name '.NET 8 SDK availability' -Status 'SKIP' -Details 'The dotnet command is not installed or is not on PATH.'
    }

    if ($sdk8Available) {
        Invoke-Check -Name 'Restore NuGet packages' -Action {
            Invoke-NativeCommand -FilePath 'dotnet' -ArgumentList @('restore', 'RaceDay.sln')
        }
        $restorePassed = [bool]$script:LastCheckPassed

        if ($restorePassed) {
            Invoke-Check -Name 'Build API, MVC and tests (Release)' -Action {
                Invoke-NativeCommand -FilePath 'dotnet' -ArgumentList @('build', 'RaceDay.sln', '--configuration', 'Release', '--no-restore')
            }
            $buildPassed = [bool]$script:LastCheckPassed

            if ($buildPassed) {
                Invoke-Check -Name 'Run automated xUnit regression tests' -Action {
                    if (-not (Test-Path $ResultsDirectory)) {
                        New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null
                    }
                    Invoke-NativeCommand -FilePath 'dotnet' -ArgumentList @(
                        'test', 'RaceDay.sln', '--configuration', 'Release', '--no-build',
                        '--logger', 'trx;LogFileName=RaceDay-tests.trx',
                        '--results-directory', $ResultsDirectory
                    )
                }

                Invoke-Check -Name 'Publish API and MVC projects (Release)' -Action {
                    $publishRoot = Join-Path $env:TEMP ("RaceDay-Publish-{0}" -f $PID)
                    $apiOutput = Join-Path $publishRoot 'api'
                    $mvcOutput = Join-Path $publishRoot 'mvc'
                    if (Test-Path $publishRoot) {
                        Remove-Item -LiteralPath $publishRoot -Recurse -Force
                    }
                    New-Item -ItemType Directory -Path $apiOutput -Force | Out-Null
                    New-Item -ItemType Directory -Path $mvcOutput -Force | Out-Null
                    Invoke-NativeCommand -FilePath 'dotnet' -ArgumentList @('publish', 'RaceDay.Api/RaceDay.Api.csproj', '--configuration', 'Release', '--no-restore', '--output', $apiOutput)
                    Invoke-NativeCommand -FilePath 'dotnet' -ArgumentList @('publish', 'RaceDay.Mvc/RaceDay.Mvc.csproj', '--configuration', 'Release', '--no-restore', '--output', $mvcOutput)
                    if (-not (Test-Path (Join-Path $apiOutput 'RaceDay.Api.dll'))) { throw 'Published API DLL was not found.' }
                    if (-not (Test-Path (Join-Path $mvcOutput 'RaceDay.Mvc.dll'))) { throw 'Published MVC DLL was not found.' }
                    Remove-Item -LiteralPath $publishRoot -Recurse -Force
                }
            }
            else {
                Add-CheckResult -Name 'Run automated xUnit regression tests' -Status 'SKIP' -Details 'Skipped because the Release build failed.'
                Add-CheckResult -Name 'Publish API and MVC projects (Release)' -Status 'SKIP' -Details 'Skipped because the Release build failed.'
            }
        }
        else {
            Add-CheckResult -Name 'Build API, MVC and tests (Release)' -Status 'SKIP' -Details 'Skipped because NuGet restore failed.'
            Add-CheckResult -Name 'Run automated xUnit regression tests' -Status 'SKIP' -Details 'Skipped because NuGet restore failed.'
            Add-CheckResult -Name 'Publish API and MVC projects (Release)' -Status 'SKIP' -Details 'Skipped because NuGet restore failed.'
        }
    }
    else {
        Add-CheckResult -Name 'Restore NuGet packages' -Status 'SKIP' -Details 'Requires the .NET 8 SDK.'
        Add-CheckResult -Name 'Build API, MVC and tests (Release)' -Status 'SKIP' -Details 'Requires the .NET 8 SDK.'
        Add-CheckResult -Name 'Run automated xUnit regression tests' -Status 'SKIP' -Details 'Requires a successful .NET 8 build.'
        Add-CheckResult -Name 'Publish API and MVC projects (Release)' -Status 'SKIP' -Details 'Requires a successful .NET 8 build.'
    }

    # 4. Azure storage is intentionally optional for app startup, but required to demonstrate uploads.
    $azureValue = [Environment]::GetEnvironmentVariable('AzureBlobStorage__ConnectionString')
    if ([string]::IsNullOrWhiteSpace($azureValue)) {
        $azureValue = [Environment]::GetEnvironmentVariable('AZURE_BLOB_CONNECTION_STRING')
    }
    if ([string]::IsNullOrWhiteSpace($azureValue) -and (Test-Path (Join-Path $ProjectRoot '.env'))) {
        $azureLine = Get-Content -LiteralPath (Join-Path $ProjectRoot '.env') | Where-Object { $_ -match '^\s*AZURE_BLOB_CONNECTION_STRING\s*=' } | Select-Object -First 1
        if ($azureLine) {
            $azureValue = ($azureLine -split '=', 2)[1].Trim().Trim('"').Trim("'")
        }
    }
    if ([string]::IsNullOrWhiteSpace($azureValue)) {
        Add-CheckResult -Name 'Azure Blob Storage configuration' -Status 'WARN' -Details 'No Azure connection string was found in the current environment. Non-image flows can run, but banner/profile upload tests require a real storage account connection string.'
    }
    else {
        Add-CheckResult -Name 'Azure Blob Storage configuration' -Status 'PASS' -Details 'A storage connection string is present in the environment. Its value is not displayed.'
    }

    # 5. Docker Compose validation and optional container image build.
    $dockerCommand = Get-Command 'docker' -ErrorAction SilentlyContinue
    $dockerAvailable = $null -ne $dockerCommand
    $composeAvailable = $false
    $dockerEngineAvailable = $false

    if (-not (Test-Path (Join-Path $ProjectRoot '.env'))) {
        $script:TempEnvFile = Join-Path $env:TEMP ("RaceDay-compose-{0}.env" -f $PID)
        Copy-Item -LiteralPath (Join-Path $ProjectRoot '.env.example') -Destination $script:TempEnvFile -Force
    }
    else {
        $script:TempEnvFile = Join-Path $ProjectRoot '.env'
    }
    $script:StackEnvFile = $script:TempEnvFile

    if ($dockerAvailable) {
        Invoke-Check -Name 'Docker Compose CLI availability' -Action {
            Invoke-NativeCommand -FilePath 'docker' -ArgumentList @('compose', 'version')
        }
        $composeAvailable = [bool]$script:LastCheckPassed

        if ($composeAvailable) {
            Invoke-Check -Name 'Docker Compose configuration validation' -Action {
                Invoke-NativeCommand -FilePath 'docker' -ArgumentList @('compose', '--env-file', $script:TempEnvFile, 'config', '--quiet')
            }

            Invoke-Check -Name 'Docker engine availability' -Action {
                Invoke-NativeCommand -FilePath 'docker' -ArgumentList @('info', '--format', '{{.ServerVersion}}')
            }
            $dockerEngineAvailable = [bool]$script:LastCheckPassed

            if ($dockerEngineAvailable -and -not $SkipDockerImageBuild) {
                Invoke-Check -Name 'Build API and MVC Docker images' -Action {
                    Invoke-NativeCommand -FilePath 'docker' -ArgumentList @('compose', '--env-file', $script:TempEnvFile, 'build', 'api', 'mvc')
                }
            }
            elseif ($SkipDockerImageBuild) {
                Add-CheckResult -Name 'Build API and MVC Docker images' -Status 'SKIP' -Details 'Skipped because -SkipDockerImageBuild was specified.'
            }
            else {
                Add-CheckResult -Name 'Build API and MVC Docker images' -Status 'SKIP' -Details 'Docker Engine is not running or is not reachable.'
            }
        }
        else {
            Add-CheckResult -Name 'Docker Compose configuration validation' -Status 'SKIP' -Details 'Docker Compose CLI is unavailable.'
            Add-CheckResult -Name 'Docker engine availability' -Status 'SKIP' -Details 'Docker Compose CLI is unavailable.'
            Add-CheckResult -Name 'Build API and MVC Docker images' -Status 'SKIP' -Details 'Docker Compose CLI is unavailable.'
        }
    }
    else {
        Add-CheckResult -Name 'Docker Compose CLI availability' -Status 'SKIP' -Details 'Docker is not installed or is not on PATH.'
        Add-CheckResult -Name 'Docker Compose configuration validation' -Status 'SKIP' -Details 'Requires Docker Compose.'
        Add-CheckResult -Name 'Docker engine availability' -Status 'SKIP' -Details 'Requires Docker Engine.'
        Add-CheckResult -Name 'Build API and MVC Docker images' -Status 'SKIP' -Details 'Requires Docker Engine.'
    }

    # 6. Optional real container startup and HTTP smoke tests.
    if ($RunDockerStack) {
        if (-not $composeAvailable -or -not $dockerEngineAvailable) {
            Add-CheckResult -Name 'Start Docker Compose stack and smoke-test URLs' -Status 'SKIP' -Details 'Requires Docker Compose and a running Docker Engine.'
        }
        else {
            Invoke-Check -Name 'Start Docker Compose stack' -Action {
                Invoke-DockerCompose -ComposeArguments @('up', '-d', '--build')
                $script:StackStarted = $true
            }

            if ($script:LastCheckPassed) {
                Invoke-Check -Name 'API Swagger HTTP smoke test (http://localhost:5080/swagger/index.html)' -Action {
                    if (-not (Wait-ForHttpEndpoint -Uri 'http://localhost:5080/swagger/index.html' -TimeoutSeconds $HealthTimeoutSeconds)) {
                        throw "API Swagger did not become available within $HealthTimeoutSeconds seconds."
                    }
                }

                Invoke-Check -Name 'API rejects unauthenticated event requests' -Action {
                    $statusCode = Get-HttpStatusCode -Uri 'http://localhost:5080/api/events'
                    if ($statusCode -ne 401) {
                        throw "Expected HTTP 401 from the protected events endpoint; received HTTP $statusCode."
                    }
                    Write-Host 'Protected API endpoint correctly returned HTTP 401 without a session cookie.'
                }

                Invoke-Check -Name 'MVC home page HTTP smoke test (http://localhost:5081/)' -Action {
                    if (-not (Wait-ForHttpEndpoint -Uri 'http://localhost:5081/' -TimeoutSeconds $HealthTimeoutSeconds)) {
                        throw "MVC home page did not become available within $HealthTimeoutSeconds seconds."
                    }
                }

                if ($script:FailureCount -gt 0) {
                    Write-Host "`nRecent Docker logs (for troubleshooting):" -ForegroundColor Yellow
                    try { Invoke-DockerCompose -ComposeArguments @('logs', '--tail', '80', 'database', 'api', 'mvc') } catch { Write-Host $_.Exception.Message -ForegroundColor Red }
                }
            }

            if ($script:StackStarted -and -not $KeepDockerStack) {
                Write-Host "`nStopping containers (the SQL Server data volume is preserved)..." -ForegroundColor Cyan
                try {
                    Invoke-DockerCompose -ComposeArguments @('down')
                    Add-CheckResult -Name 'Docker Compose cleanup' -Status 'PASS' -Details 'Containers stopped; named SQL data volume was not deleted.'
                    $script:StackStarted = $false
                }
                catch {
                    Add-CheckResult -Name 'Docker Compose cleanup' -Status 'FAIL' -Details $_.Exception.Message
                }
            }
            elseif ($script:StackStarted -and $KeepDockerStack) {
                Add-CheckResult -Name 'Docker Compose cleanup' -Status 'WARN' -Details 'Stack was left running because -KeepDockerStack was specified.'
            }
        }
    }
    else {
        Add-CheckResult -Name 'Start Docker Compose stack and smoke-test URLs' -Status 'SKIP' -Details 'Optional. Rerun with -RunDockerStack to start SQL Server, API and MVC containers and test their HTTP endpoints.'
    }

    # 7. Git/submission checks. These cannot be completed from a ZIP without its .git folder.
    if (Test-Path (Join-Path $ProjectRoot '.git')) {
        $gitCommand = Get-Command 'git' -ErrorAction SilentlyContinue
        if ($null -ne $gitCommand) {
            Invoke-Check -Name 'Git repository and commit count' -Action {
                $commitCountText = & git rev-list --count HEAD
                if ($LASTEXITCODE -ne 0) { throw 'Could not read the Git commit count.' }
                $commitCount = [int]$commitCountText
                Write-Host "Git commits in current repository: $commitCount"
                if ($commitCount -lt 20) {
                    throw "Only $commitCount commits found. The rubric requires at least 20 meaningful commits."
                }
                $remote = & git remote -v
                if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace(($remote -join ''))) {
                    throw 'No Git remote is configured. Add your GitHub remote and push the project.'
                }
                Write-Host 'Git remote is configured.'
            }
        }
        else {
            Add-CheckResult -Name 'Git repository and commit count' -Status 'SKIP' -Details 'Git is not installed or is not on PATH.'
        }
    }
    else {
        Add-CheckResult -Name 'Git repository and commit count' -Status 'SKIP' -Details 'This folder has no .git directory (common when working from the downloaded ZIP). GitHub commit history must be checked in your cloned repository.'
    }

    $readmeText = Get-Content -LiteralPath (Join-Path $ProjectRoot 'README.md') -Raw
    if ($readmeText -match 'Add your actual video URL here') {
        Add-CheckResult -Name 'YouTube demonstration link' -Status 'WARN' -Details 'README still contains the video URL placeholder. Add the real unlisted YouTube URL after recording.'
    }
    else {
        Add-CheckResult -Name 'YouTube demonstration link' -Status 'PASS' -Details 'The README no longer contains the default video URL placeholder. Manually confirm that the link is valid and unlisted.'
    }

}
finally {
    # If interrupted after starting containers, try to stop them unless the user asked to keep them.
    if ($script:StackStarted -and -not $KeepDockerStack -and $script:StackEnvFile) {
        try {
            Invoke-DockerCompose -ComposeArguments @('down')
            Write-Host 'Docker Compose stack stopped during cleanup.' -ForegroundColor Cyan
        }
        catch {
            Write-Host ("Could not stop Docker Compose automatically: {0}" -f $_.Exception.Message) -ForegroundColor Red
        }
    }

    if ($script:TempEnvFile -and (Test-Path $script:TempEnvFile) -and ($script:TempEnvFile -like (Join-Path $env:TEMP 'RaceDay-compose-*.env'))) {
        Remove-Item -LiteralPath $script:TempEnvFile -Force -ErrorAction SilentlyContinue
    }

    Pop-Location
}

# 8. Save a readable report for assignment evidence/troubleshooting.
if (-not (Test-Path $ResultsDirectory)) {
    New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null
}
$summary = @(
    'RaceDay Part 3 - Automated Test Report',
    ('Generated: ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')),
    ('Project: ' + $ProjectRoot),
    ('PASS: ' + @($script:CheckResults | Where-Object Status -eq 'PASS').Count),
    ('FAIL: ' + $script:FailureCount),
    ('SKIP: ' + $script:SkipCount),
    ('WARN: ' + $script:WarningCount),
    '',
    ($script:CheckResults | Format-Table Status, Check, Details -AutoSize | Out-String -Width 220)
)
$summary | Set-Content -LiteralPath $ReportPath -Encoding UTF8

Write-Host "`n================ FINAL SUMMARY ================" -ForegroundColor White
$script:CheckResults | Format-Table Status, Check, Details -AutoSize -Wrap
Write-Host "Report saved to: $ReportPath" -ForegroundColor Cyan
Write-Host ("PASS: {0} | FAIL: {1} | SKIP: {2} | WARN: {3}" -f @($script:CheckResults | Where-Object Status -eq 'PASS').Count, $script:FailureCount, $script:SkipCount, $script:WarningCount)

if ($script:FailureCount -gt 0) {
    Write-Host 'RESULT: FAILED - fix the failed checks and run the script again.' -ForegroundColor Red
    exit 1
}
elseif ($script:SkipCount -gt 0) {
    Write-Host 'RESULT: PARTIAL - no failed checks, but one or more tests were skipped because a prerequisite or optional test was unavailable.' -ForegroundColor Yellow
    exit 2
}
else {
    Write-Host 'RESULT: PASSED - all checks selected by this run completed without failure.' -ForegroundColor Green
    exit 0
}

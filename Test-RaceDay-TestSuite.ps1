[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path -Parent $MyInvocation.MyCommand.Path),
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$NoRestore,
    [switch]$NoBuild,
    [switch]$SkipSourceAudit
)

# RaceDay xUnit test-suite validator.
# This script checks that the test project is configured, that expected test
# suites exist, executes the tests, and independently inspects the generated TRX.
# Compatible with Windows PowerShell 5.1 and PowerShell 7+.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$TestProject = Join-Path $ProjectRoot 'RaceDay.Tests/RaceDay.Tests.csproj'
$ResultsDirectory = Join-Path $ProjectRoot 'TestResults'
$RunId = Get-Date -Format 'yyyyMMdd-HHmmss'
$TrxName = "RaceDay-TestSuite-$RunId.trx"
$TrxPath = Join-Path $ResultsDirectory $TrxName
$LogPath = Join-Path $ResultsDirectory "RaceDay-TestSuite-$RunId.log"
$ReportPath = Join-Path $ResultsDirectory "RaceDay-TestSuite-Report-$RunId.txt"

$script:Checks = New-Object System.Collections.Generic.List[object]
$script:Failures = 0
$script:Warnings = 0

function Write-Check {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][ValidateSet('PASS', 'FAIL', 'WARN')][string]$Status,
        [Parameter(Mandatory = $true)][string]$Details
    )

    $script:Checks.Add([pscustomobject]@{
        Status = $Status
        Check = $Name
        Details = ($Details -replace '[\r\n]+', ' ').Trim()
    }) | Out-Null

    $color = switch ($Status) {
        'PASS' { 'Green' }
        'FAIL' { 'Red' }
        default { 'Yellow' }
    }
    Write-Host ("[{0}] {1}: {2}" -f $Status, $Name, $Details) -ForegroundColor $color

    if ($Status -eq 'FAIL') { $script:Failures++ }
    if ($Status -eq 'WARN') { $script:Warnings++ }
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$Description,
        [switch]$AllowNonZeroExit
    )

    Write-Host "`n> dotnet $($Arguments -join ' ')" -ForegroundColor Cyan
    $commandOutput = @(& dotnet @Arguments 2>&1)
    $exitCode = $LASTEXITCODE

    foreach ($line in $commandOutput) {
        $text = [string]$line
        Write-Host $text
        Add-Content -LiteralPath $LogPath -Value $text
    }

    if ($exitCode -ne 0 -and -not $AllowNonZeroExit) {
        throw "$Description failed (dotnet exit code $exitCode). See $LogPath"
    }
    return $exitCode
}

function Get-TrxCounter {
    param(
        [Parameter(Mandatory = $true)][System.Xml.XmlNode]$CounterNode,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $attribute = $CounterNode.Attributes[$Name]
    if ($null -eq $attribute) { return 0 }
    $value = 0
    if ([int]::TryParse($attribute.Value, [ref]$value)) { return $value }
    return 0
}

try {
    Write-Host 'RaceDay - TEST THE TESTS' -ForegroundColor White
    Write-Host "Project root: $ProjectRoot"
    Write-Host "Configuration: $Configuration"

    if (-not (Test-Path -LiteralPath $ProjectRoot -PathType Container)) {
        throw "Project folder does not exist: $ProjectRoot"
    }
    if (-not (Test-Path -LiteralPath $TestProject -PathType Leaf)) {
        throw "Test project was not found: $TestProject"
    }
    if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot 'RaceDay.Api/RaceDay.Api.csproj') -PathType Leaf)) {
        throw 'RaceDay.Api project reference is missing. Check that you selected the Part 3 project root.'
    }

    New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null
    Set-Content -LiteralPath $LogPath -Value "RaceDay test-suite run started: $(Get-Date -Format o)"

    # Check the .NET SDK before attempting restore/build/test.
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($null -eq $dotnet) {
        throw 'The dotnet CLI was not found. Install the .NET 8 SDK, reopen PowerShell, and run this script again.'
    }

    $sdkOutput = @(& dotnet --list-sdks 2>&1)
    $sdkExitCode = $LASTEXITCODE
    if ($sdkExitCode -ne 0) { throw 'Could not read installed .NET SDK versions.' }
    $sdk8 = @($sdkOutput | Where-Object { ([string]$_) -match '^8\.' })
    if ($sdk8.Count -eq 0) {
        throw '.NET 8 SDK was not found. Install the .NET 8 SDK because all RaceDay projects target net8.0.'
    }
    $activeSdk = [string](& dotnet --version 2>&1)
    if ($LASTEXITCODE -ne 0 -or $activeSdk -notmatch '^8\.') {
        throw "The selected SDK is '$activeSdk', not .NET 8. Check global.json and install the .NET 8 SDK."
    }
    Write-Check -Name '.NET 8 SDK' -Status 'PASS' -Details ("Selected SDK: {0}; installed .NET 8 SDKs: {1}" -f $activeSdk, ($sdk8 -join '; '))

    # Verify xUnit project metadata and the source-level test suites.
    if (-not $SkipSourceAudit) {
        [xml]$projectXml = Get-Content -LiteralPath $TestProject -Raw
        $isTestProject = $projectXml.Project.PropertyGroup.IsTestProject -contains 'true'
        $targetFramework = [string]$projectXml.Project.PropertyGroup.TargetFramework
        if (-not $isTestProject) {
            throw 'RaceDay.Tests.csproj does not declare IsTestProject=true.'
        }
        if ($targetFramework -ne 'net8.0') {
            throw "Expected test target framework net8.0, found '$targetFramework'."
        }

        foreach ($projectRelativePath in @('RaceDay.Api/RaceDay.Api.csproj', 'RaceDay.Mvc/RaceDay.Mvc.csproj')) {
            [xml]$relatedProjectXml = Get-Content -LiteralPath (Join-Path $ProjectRoot $projectRelativePath) -Raw
            $relatedTarget = [string]$relatedProjectXml.Project.PropertyGroup.TargetFramework
            if ($relatedTarget -ne 'net8.0') {
                throw "Expected $projectRelativePath to target net8.0, found '$relatedTarget'."
            }
        }
        Write-Check -Name 'Project framework configuration' -Status 'PASS' -Details 'API, MVC and Tests all target net8.0; IsTestProject=true.'

        $requiredSuites = @(
            'AuthTests.cs',
            'EnrolmentTests.cs',
            'EventTests.cs',
            'Part3ApiRegressionTests.cs',
            'ResultsTests.cs',
            'RoleAccessTests.cs'
        )
        $missingSuites = @($requiredSuites | Where-Object { -not (Test-Path -LiteralPath (Join-Path $ProjectRoot "RaceDay.Tests/$_") -PathType Leaf) })
        if ($missingSuites.Count -gt 0) {
            throw ('Missing expected test suite files: ' + ($missingSuites -join ', '))
        }

        $testSourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'RaceDay.Tests') -Filter '*.cs' -File -Recurse)
        $testAttributeCount = 0
        foreach ($sourceFile in $testSourceFiles) {
            $source = Get-Content -LiteralPath $sourceFile.FullName -Raw
            $matches = [regex]::Matches($source, '(?m)^\s*\[(Fact|Theory)(\s*\([^\]]*\))?\]')
            $testAttributeCount += $matches.Count
        }
        if ($testAttributeCount -lt 1) {
            throw 'No xUnit [Fact] or [Theory] test methods were found in RaceDay.Tests.'
        }
        Write-Check -Name 'Expected xUnit test suites' -Status 'PASS' -Details ("Found all {0} expected suite files and {1} Fact/Theory declarations." -f $requiredSuites.Count, $testAttributeCount)
    }
    else {
        Write-Check -Name 'Source audit' -Status 'WARN' -Details 'Skipped by -SkipSourceAudit.'
    }

    Push-Location $ProjectRoot
    try {
        if (-not $NoRestore) {
            Invoke-DotNet -Arguments @('restore', $TestProject) -Description 'NuGet restore'
            Write-Check -Name 'NuGet restore' -Status 'PASS' -Details 'Test project and API project dependencies restored.'
        }
        else {
            Write-Check -Name 'NuGet restore' -Status 'WARN' -Details 'Skipped by -NoRestore; existing assets must be current.'
        }

        if (-not $NoBuild) {
            Invoke-DotNet -Arguments @('build', $TestProject, '--configuration', $Configuration, '--no-restore') -Description 'Test project build'
            Write-Check -Name 'Test project build' -Status 'PASS' -Details "$Configuration build completed."
        }
        else {
            Write-Check -Name 'Test project build' -Status 'WARN' -Details 'Skipped by -NoBuild; existing binaries will be used.'
        }

        $testArguments = @(
            'test', $TestProject,
            '--configuration', $Configuration,
            '--no-restore',
            '--logger', "trx;LogFileName=$TrxName",
            '--results-directory', $ResultsDirectory,
            '--verbosity', 'normal'
        )
        if ($NoBuild) { $testArguments += '--no-build' }

        $testExitCode = Invoke-DotNet -Arguments $testArguments -Description 'xUnit test execution' -AllowNonZeroExit
        if ($testExitCode -eq 0) {
            Write-Check -Name 'dotnet test command' -Status 'PASS' -Details 'The test runner returned exit code 0.'
        }
        else {
            # Continue to parse the TRX so the user gets exact failing-test details.
            Write-Check -Name 'dotnet test command' -Status 'FAIL' -Details ("The test runner returned exit code {0}; inspect the TRX summary below." -f $testExitCode)
        }
    }
    finally {
        Pop-Location
    }

    # A successful dotnet exit code alone is not enough: inspect the TRX contents.
    if (-not (Test-Path -LiteralPath $TrxPath -PathType Leaf)) {
        throw "The test command did not generate the expected TRX file: $TrxPath"
    }

    [xml]$trx = Get-Content -LiteralPath $TrxPath -Raw
    $counterNode = $trx.SelectSingleNode("//*[local-name()='ResultSummary']/*[local-name()='Counters']")
    if ($null -eq $counterNode) {
        throw 'The TRX report does not contain a ResultSummary/Counters element; test execution cannot be verified.'
    }

    $total = Get-TrxCounter -CounterNode $counterNode -Name 'total'
    $executed = Get-TrxCounter -CounterNode $counterNode -Name 'executed'
    $passed = Get-TrxCounter -CounterNode $counterNode -Name 'passed'
    $failed = Get-TrxCounter -CounterNode $counterNode -Name 'failed'
    $errorCount = Get-TrxCounter -CounterNode $counterNode -Name 'error'
    $timeout = Get-TrxCounter -CounterNode $counterNode -Name 'timeout'
    $aborted = Get-TrxCounter -CounterNode $counterNode -Name 'aborted'
    $notExecuted = Get-TrxCounter -CounterNode $counterNode -Name 'notExecuted'
    $inconclusive = Get-TrxCounter -CounterNode $counterNode -Name 'inconclusive'

    if ($total -lt 1) {
        throw 'The TRX report shows zero discovered tests. The test suite did not actually run.'
    }
    Write-Check -Name 'Tests discovered and executed' -Status 'PASS' -Details ("Discovered: {0}; executed: {1}; passed: {2}." -f $total, $executed, $passed)

    if ($failed -gt 0 -or $errorCount -gt 0 -or $timeout -gt 0 -or $aborted -gt 0) {
        Write-Check -Name 'No failing/error/timeout/aborted tests' -Status 'FAIL' -Details ("Failed={0}, Errors={1}, Timeouts={2}, Aborted={3}." -f $failed, $errorCount, $timeout, $aborted)
    }
    else {
        Write-Check -Name 'No failing/error/timeout/aborted tests' -Status 'PASS' -Details 'All executed tests completed without failures.'
    }

    if ($notExecuted -gt 0 -or $inconclusive -gt 0 -or $executed -lt $total) {
        Write-Check -Name 'All discovered tests executed' -Status 'WARN' -Details ("Not executed={0}; inconclusive={1}; total={2}; executed={3}." -f $notExecuted, $inconclusive, $total, $executed)
    }
    else {
        Write-Check -Name 'All discovered tests executed' -Status 'PASS' -Details 'No skipped, inconclusive or unexecuted tests were reported.'
    }

    $resultNodes = @($trx.SelectNodes("//*[local-name()='Results']/*[local-name()='UnitTestResult']"))
    $failedResults = @($resultNodes | Where-Object { $_.GetAttribute('outcome') -eq 'Failed' })
    if ($failedResults.Count -gt 0) {
        Write-Host "`nFailed test details:" -ForegroundColor Red
        foreach ($failedResult in $failedResults) {
            Write-Host (" - {0}" -f $failedResult.GetAttribute('testName')) -ForegroundColor Red
        }
    }

    $summary = @(
        'RaceDay xUnit Test Suite Report',
        ('Run date: {0}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')),
        ('Project: {0}' -f $TestProject),
        ('Configuration: {0}' -f $Configuration),
        ('TRX: {0}' -f $TrxPath),
        ('Total discovered: {0}' -f $total),
        ('Executed: {0}' -f $executed),
        ('Passed: {0}' -f $passed),
        ('Failed: {0}' -f $failed),
        ('Errors: {0}' -f $errorCount),
        ('Timeouts: {0}' -f $timeout),
        ('Aborted: {0}' -f $aborted),
        ('Not executed: {0}' -f $notExecuted),
        ('Inconclusive: {0}' -f $inconclusive),
        '',
        'Checks:',
        ($script:Checks | ForEach-Object { '[{0}] {1} - {2}' -f $_.Status, $_.Check, $_.Details })
    )
    Set-Content -LiteralPath $ReportPath -Value $summary

    Write-Host "`n================ TEST SUITE SUMMARY ================" -ForegroundColor White
    Write-Host ("Total: {0} | Executed: {1} | Passed: {2} | Failed: {3}" -f $total, $executed, $passed, $failed)
    Write-Host "TRX report: $TrxPath"
    Write-Host "Text report: $ReportPath"
    Write-Host "Log file: $LogPath"

    if ($script:Failures -gt 0 -or $failed -gt 0 -or $errorCount -gt 0 -or $timeout -gt 0 -or $aborted -gt 0) {
        Write-Host 'RESULT: FAIL - investigate the failed tests before submission.' -ForegroundColor Red
        exit 1
    }

    if ($script:Warnings -gt 0) {
        Write-Host 'RESULT: PASS WITH WARNINGS - review skipped/inconclusive checks.' -ForegroundColor Yellow
    }
    else {
        Write-Host 'RESULT: PASS - all discovered RaceDay xUnit tests passed.' -ForegroundColor Green
    }
    exit 0
}
catch {
    Write-Host "`nRESULT: SCRIPT ERROR - $($_.Exception.Message)" -ForegroundColor Red
    if (Test-Path -LiteralPath $LogPath) {
        Write-Host "See log: $LogPath" -ForegroundColor Yellow
    }
    exit 1
}

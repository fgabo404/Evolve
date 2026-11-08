[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9_-]+$')]
    [string]$Label = 'P03-A4',
    [ValidateSet('Worldgen', 'TectonicAudit', 'TectonicCalibration', 'StagedPangaea', 'LaboratoryPangaea', 'LaboratoryControls')]
    [string]$ValidationSet = 'Worldgen',
    [switch]$IsolatedArtifacts
)

# Run from the repository root. This script never commits, pushes, deletes reports,
# changes application code, or launches the interactive game.
$ErrorActionPreference = 'Stop'
$repoRoot = (& git rev-parse --show-toplevel 2>$null | Out-String).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Run this script in the Evolve repository.' }
$repoRoot = [IO.Path]::GetFullPath($repoRoot)
if ([IO.Path]::GetFullPath((Get-Location).Path).TrimEnd('\', '/') -ne $repoRoot.TrimEnd('\', '/')) {
    throw 'Run commands.ps1 from the repository root.'
}
$branch = (& git branch --show-current | Out-String).Trim()
if ($branch -ne 'worldgeneration_pangea') { throw 'Expected branch worldgeneration_pangea.' }
$validatedSha = (& git rev-parse HEAD | Out-String).Trim()
$validatedSubject = (& git log -1 --format=%s | Out-String).Trim()
& git diff --quiet HEAD -- GEvolve.Definitions GEvolve.State GEvolve.Engine GEvolve.Game GEvolve.Tests ':!GEvolve.Tests/ValidationReports'
$sourceDirty = $LASTEXITCODE -ne 0
$startedUtc = [DateTime]::UtcNow
$runId = '{0}_{1}_{2}' -f $Label, $startedUtc.ToString('yyyyMMdd_HHmmssfffZ'), [Guid]::NewGuid().ToString('N').Substring(0, 8)
$reportsRoot = Join-Path $repoRoot 'GEvolve.Tests/ValidationReports'
$reportDirectory = Join-Path $reportsRoot $runId
[void][IO.Directory]::CreateDirectory($reportDirectory)
$utf8 = New-Object System.Text.UTF8Encoding($false)
[IO.File]::Copy($PSCommandPath, (Join-Path $reportDirectory 'commands.ps1'))
$script:privacyReplacements = 0

function Protect-Log([string]$Value) {
    # Preserve messages and numbers; replace only sensitive substrings.
    $locations = @(
        @($repoRoot, '<REPO>'), @($env:USERPROFILE, '<USERPROFILE>'),
        @($env:TEMP, '<TEMP>'), @($env:TMP, '<TEMP>'),
        @($env:LOCALAPPDATA, '<LOCALAPPDATA>'), @($env:APPDATA, '<APPDATA>')
    )
    foreach ($location in $locations) {
        if ([string]::IsNullOrWhiteSpace($location[0])) { continue }
        foreach ($path in @($location[0], $location[0].Replace('\', '/'))) {
            $pattern = [regex]::Escape($path)
            $matchesFound = [regex]::Matches($Value, $pattern, 'IgnoreCase').Count
            $script:privacyReplacements += $matchesFound
            $Value = [regex]::Replace($Value, $pattern, $location[1], 'IgnoreCase')
        }
    }
    $patterns = @(
        @('(?i)[A-Z]:[\\/]Users[\\/][^\\/\s"'']+', '<USERPROFILE>'),
        @('(?i)(https?://)[^/\s@]+@', '$1<REDACTED>@'),
        @('(?i)(authorization:\s*(?:Bearer|Basic)\s+)\S+', '$1<REDACTED>'),
        @('(?i)\b(password|passwd|token|api[_-]?key|secret|client_secret)\s*([=:])\s*[^\s;,]+', '$1$2<REDACTED>'),
        @('(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b', '<EMAIL>')
    )
    foreach ($entry in $patterns) {
        $script:privacyReplacements += [regex]::Matches($Value, $entry[0]).Count
        $Value = [regex]::Replace($Value, $entry[0], $entry[1])
    }
    return $Value
}

function Invoke-Dotnet([string[]]$Arguments) {
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo.FileName = (Get-Command dotnet -CommandType Application -ErrorAction Stop).Source
    # All arguments are the fixed, space-free tokens defined below; no shell is involved.
    $process.StartInfo.Arguments = $Arguments -join ' '
    $process.StartInfo.WorkingDirectory = $repoRoot
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.CreateNoWindow = $true
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    $process.StartInfo.StandardOutputEncoding = $utf8
    $process.StartInfo.StandardErrorEncoding = $utf8
    $timer = [Diagnostics.Stopwatch]::StartNew()
    try {
        [void]$process.Start()
        # Drain BOTH streams asynchronously to avoid pipe-buffer deadlocks.
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $stdout = $stdoutTask.GetAwaiter().GetResult()
        $stderr = $stderrTask.GetAwaiter().GetResult()
        $timer.Stop()
        return [pscustomobject]@{ ExitCode = $process.ExitCode; Stdout = $stdout; Stderr = $stderr; Seconds = $timer.Elapsed.TotalSeconds }
    }
    finally { $process.Dispose() }
}

$sdk = 'Unavailable'
try {
    $version = Invoke-Dotnet @('--version')
    if ($version.ExitCode -eq 0) { $sdk = $version.Stdout.Trim() }
}
catch { $sdk = Protect-Log $_.Exception.Message }
$steps = @(
    @{ Name = 'build'; Arguments = @('build') },
    @{ Name = 'pangaea-elevations'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--pangaea-elevations') },
    @{ Name = 'mountain-ranges'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--mountain-ranges') },
    @{ Name = 'all-suites'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--all-suites') }
)
if ($ValidationSet -eq 'TectonicAudit') {
    $steps = @(
        @{ Name = 'build'; Arguments = @('build') },
        @{ Name = 'tectonic-audit'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--pangaea-tectonic-audit') }
    )
}
$results = @()
if ($ValidationSet -eq 'TectonicCalibration') {
    $steps = @(
        @{ Name = 'build'; Arguments = @('build') },
        @{ Name = 'pangaea-elevations'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--pangaea-elevations') },
        @{ Name = 'mountain-ranges'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--mountain-ranges') },
        @{ Name = 'tectonic-audit'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--pangaea-tectonic-audit') },
        @{ Name = 'pangaea-uplift'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--pangaea-uplift') },
        @{ Name = 'all-suites'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--all-suites') }
    )
}
$buildFailed = $false
if ($ValidationSet -eq 'StagedPangaea') {
    $steps = @(
        @{ Name = 'build'; Arguments = @('build') },
        @{ Name = 'pangaea-staged'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--pangaea-staged') },
        @{ Name = 'pangaea-elevations'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--pangaea-elevations') },
        @{ Name = 'mountain-ranges'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--mountain-ranges') },
        @{ Name = 'tectonic-audit'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--pangaea-tectonic-audit') },
        @{ Name = 'pangaea-uplift'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--pangaea-uplift') },
        @{ Name = 'all-suites'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--all-suites') }
    )
}
if ($ValidationSet -eq 'LaboratoryPangaea') {
    $steps = @(
        @{ Name = 'build'; Arguments = @('build') },
        @{ Name = 'pangaea-laboratory'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--pangaea-laboratory') },
        @{ Name = 'pangaea-staged'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--pangaea-staged') },
        @{ Name = 'all-suites'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--all-suites') }
    )
}
if ($ValidationSet -eq 'LaboratoryControls') {
    $steps = @(
        @{ Name = 'build'; Arguments = @('build') },
        @{ Name = 'pangaea-laboratory'; Arguments = @('run', '--project', 'GEvolve.Tests', '--', '--pangaea-laboratory') }
    )
}
$artifactsDirectory = 'GEvolve.Tests/bin/ValidationArtifacts/' + $runId
if ($IsolatedArtifacts) {
    foreach ($step in $steps) {
        if ($step.Arguments[0] -eq 'build') {
            $step.Arguments = @('build', '--artifacts-path', $artifactsDirectory)
        }
        elseif ($step.Arguments[0] -eq 'run') {
            $separator = [Array]::IndexOf($step.Arguments, '--')
            $step.Arguments = @($step.Arguments[0..($separator - 1)]) + @('--artifacts-path', $artifactsDirectory) +
                @($step.Arguments[$separator..($step.Arguments.Length - 1)])
        }
    }
}
foreach ($step in $steps) {
    $command = 'dotnet ' + ($step.Arguments -join ' ')
    $stepStarted = [DateTime]::UtcNow
    Write-Host ('START {0} UTC {1}' -f $step.Name, $stepStarted.ToString('o'))
    $code = $null
    $seconds = 0.0
    if ($buildFailed) {
        $status = 'SKIPPED'
        $body = "SKIPPED: dotnet build failed; dependent validation was not started.`r`n"
    }
    else {
        try {
            $execution = Invoke-Dotnet $step.Arguments
            $code = $execution.ExitCode
            $seconds = $execution.Seconds
            $status = if ($code -eq 0) { 'PASS' } else { 'FAIL' }
            $body = "=== STDOUT (complete) ===`r`n" + $execution.Stdout + "`r`n=== STDERR (complete) ===`r`n" + $execution.Stderr
        }
        catch {
            $status = 'FAIL'
            $body = "LAUNCH/CAPTURE ERROR (no native exit code available):`r`n" + ($_ | Out-String)
        }
    }
    if ($step.Name -eq 'build' -and $status -eq 'FAIL') { $buildFailed = $true }
    $exitText = if ($null -eq $code) { 'N/A' } else { [string]$code }
    $header = "Command: $command`r`nStarted UTC: $($stepStarted.ToString('o'))`r`nStatus: $status`r`nNative exit code: $exitText`r`nDuration seconds: $($seconds.ToString('F3', [Globalization.CultureInfo]::InvariantCulture))`r`n`r`n"
    $publicLog = Protect-Log ($header + $body)
    [IO.File]::WriteAllText((Join-Path $reportDirectory ($step.Name + '.log')), $publicLog, $utf8)
    $results += [pscustomobject]@{ Name = $step.Name; Command = $command; Status = $status; Code = $exitText; Seconds = $seconds; Text = $publicLog }
    Write-Host ('END {0}: {1}; exit={2}; seconds={3:F3}' -f $step.Name, $status, $exitText, $seconds)
}

$summary = New-Object 'System.Collections.Generic.List[string]'
$summary.Add('# World generation validation report')
$summary.Add('')
$summary.Add('- Run: `' + $runId + '`')
$summary.Add('- Started UTC: ' + $startedUtc.ToString('o'))
$summary.Add('- Finished UTC: ' + [DateTime]::UtcNow.ToString('o'))
$summary.Add('- Branch: `' + $branch + '`')
$summary.Add('- Validated commit: `' + $validatedSha + '` - ' + $validatedSubject)
$summary.Add('- .NET SDK: `' + $sdk + '`')
$summary.Add('- Validation set: `' + $ValidationSet + '`')
$summary.Add('- Isolated build artifacts: ' + [bool]$IsolatedArtifacts)
if ($IsolatedArtifacts) { $summary.Add('- Ignored artifact directory: `' + $artifactsDirectory + '` (never published).') }
$summary.Add('- PowerShell: ' + $PSVersionTable.PSVersion.ToString() + '; OS: ' + [Environment]::OSVersion.VersionString + '; 64-bit OS: ' + [Environment]::Is64BitOperatingSystem)
$summary.Add('- Tracked source differs from validated HEAD: ' + $sourceDirty)
$summary.Add('- The report/script changes are operational and excluded from the tracked-source comparison.')
$summary.Add('')
$summary.Add('| Command | Status | Native exit code | Duration (seconds) | Complete log |')
$summary.Add('|---|---|---:|---:|---|')
foreach ($result in $results) {
    $duration = $result.Seconds.ToString('F3', [Globalization.CultureInfo]::InvariantCulture)
    $summary.Add('| `' + $result.Command + '` | ' + $result.Status + ' | ' + $result.Code + ' | ' + $duration + ' | [' + $result.Name + '.log](' + $result.Name + '.log) |')
}
$summary.Add('')
$summary.Add('## Reported suite totals')
foreach ($result in $results) {
    $passed = [regex]::Matches($result.Text, '(?m)^SUITE PASS:').Count
    $failed = [regex]::Matches($result.Text, '(?m)^SUITE FAIL:').Count
    if ($passed + $failed -gt 0) { $summary.Add('- ' + $result.Name + ': ' + $passed + ' suites passed; ' + $failed + ' suites failed.') }
}
$summary.Add('Individual assertion/test totals are not inferred from PASS messages; this console runner does not report a framework test count.')
$summary.Add('')
$summary.Add('## Compilation errors, failed suites and exception reasons')
$issues = @($results | ForEach-Object { $_.Text -split '\r?\n' } | Where-Object {
    $_ -match '(?i)\berror\s+[A-Z]+\d+|^SUITE FAIL:|\b(?:[\w.]+Exception):|^Unhandled exception|^LAUNCH/CAPTURE ERROR'
} | Select-Object -Unique)
if ($issues.Count -eq 0) { $summary.Add('No matching error/exception lines were emitted; see exit codes and full logs.') }
else { $summary.Add('```text'); foreach ($issue in $issues) { $summary.Add($issue) }; $summary.Add('```') }
$summary.Add('')
$summary.Add('## Warnings emitted')
$warnings = @($results | ForEach-Object { $_.Text -split '\r?\n' } | Where-Object { $_ -match '(?i)\b(warning|advertencia)\b' } | Select-Object -Unique)
if ($warnings.Count -eq 0) { $summary.Add('No warning lines were emitted.') }
else { $summary.Add('```text'); foreach ($warning in $warnings) { $summary.Add($warning) }; $summary.Add('```') }
$summary.Add('')
$summary.Add('## Actual diagnostic and regression evidence')
$diagnosticName = if ($ValidationSet -in @('LaboratoryPangaea', 'LaboratoryControls')) { 'pangaea-laboratory' } elseif ($ValidationSet -eq 'StagedPangaea') { 'pangaea-staged' } elseif ($ValidationSet -eq 'TectonicAudit') { 'tectonic-audit' } elseif ($ValidationSet -eq 'TectonicCalibration') { 'pangaea-uplift' } else { 'pangaea-elevations' }
$diagnostic = ($results | Where-Object { $_.Name -eq $diagnosticName }).Text
$excerpts = @($diagnostic -split '\r?\n' | Where-Object {
    $_ -match '^LAB_(CASE_PASS|ASYNC_PASS|SEEDS_PASS|PASS)|^STAGED_(CASE_PASS|GRAPH_PASS|OTHER_PASS|ASYNC_PASS|PASS)|^UPLIFT_COUNTS |^UPLIFT_PASS:|^AUDIT_ENV |^AUDIT_CASE |^AUDIT_COUNTS |^AUDIT_SURFACE |^AUDIT_PASS:|^P03-A\.[46] |^  recovered base cells=|^  SYSTEMS |^  MOUNTAIN P03-A\.[46] FINAL:|^  SHA256|^  P03-A\.3 BASE correlations:'
})
if ($excerpts.Count -eq 0) { $summary.Add('No diagnostic records were reached/emitted. No metrics are invented.') }
else { $summary.Add('Selected actual records; complete emitted output is in ' + $diagnosticName + '.log.'); $summary.Add('```text'); foreach ($line in $excerpts) { $summary.Add($line) }; $summary.Add('```') }
$mountainRecords = @((($results | Where-Object { $_.Name -eq 'mountain-ranges' }).Text -split '\r?\n') | Where-Object { $_ -match '^P03-A |^PASS:' })
if ($mountainRecords.Count -gt 0) { $summary.Add('```text'); foreach ($line in $mountainRecords) { $summary.Add($line) }; $summary.Add('```') }
$regressions = @((($results | Where-Object { $_.Name -eq 'all-suites' }).Text -split '\r?\n') | Where-Object { $_ -match '^SUITE (PASS|FAIL):|BASELINE MISMATCH|baseline.*differ' } | Select-Object -Unique)
if ($regressions.Count -gt 0) { $summary.Add('```text'); foreach ($line in $regressions) { $summary.Add($line) }; $summary.Add('```') }
$summary.Add('')
$summary.Add('## Capture, privacy and retention')
$summary.Add('- Both streams are complete in separate labeled sections; cross-stream chronology is not asserted.')
$summary.Add('- Personal paths, URL credentials, recognizable secret assignments and emails are replaced by explicit markers; errors and numbers are retained.')
$summary.Add('- Sensitive-substring replacements across captured logs: ' + $script:privacyReplacements + '.')
$summary.Add('- Review all files for additional sensitive data before publishing; this script does not commit or push.')
$summary.Add('- Keep milestone/failure reports and one canonical report per commit/SDK. Review routine successes manually after five; remove only redundant successes after review, never automatically. Git retains published history. See [retention policy](../README.md).')
$summary.Add('')
$summary.Add('## Manual game validation and repeat command')
$summary.Add('The Game entry point creates EvolveWindow and calls window.Run(); it is interactive and was not launched. After a successful build, run `dotnet run --project GEvolve.Game`, inspect the required worlds and BASE/APORTE/FINAL/RELIEVE/PLACAS views, then close the window normally. Visual approval is not implied by these reports.')
$summary.Add('')
$summary.Add('From the repository root, repeat this exact script snapshot:')
$summary.Add('```powershell')
$summary.Add('.\GEvolve.Tests\ValidationReports\' + $runId + '\commands.ps1 -ValidationSet ' + $ValidationSet + ' -Label ' + $Label + $(if ($IsolatedArtifacts) { ' -IsolatedArtifacts' } else { '' }))
$summary.Add('```')
$summary.Add('Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.')
[IO.File]::WriteAllText((Join-Path $reportDirectory 'SUMMARY.md'), ($summary -join "`r`n"), $utf8)
Write-Host ('REPORT: GEvolve.Tests/ValidationReports/' + $runId + '/SUMMARY.md')
if (@($results | Where-Object { $_.Status -eq 'FAIL' }).Count -gt 0) { exit 1 }
exit 0

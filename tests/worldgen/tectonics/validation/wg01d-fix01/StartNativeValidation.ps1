param([string]$EvidenceDirectory)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (git rev-parse --show-toplevel).Trim()
if ((git branch --show-current).Trim() -ne 'world-tectonics') { throw 'Use the authorized world-tectonics branch.' }
Set-Location -LiteralPath $repositoryRoot
if ([string]::IsNullOrWhiteSpace($EvidenceDirectory)) {
    $EvidenceDirectory = Join-Path $repositoryRoot ('output/native-gate01-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
$EvidenceDirectory = [IO.Path]::GetFullPath($EvidenceDirectory)
New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'tests/worldgen/tectonics/validation/wg01d/native-validation.md') -Destination (Join-Path $EvidenceDirectory 'checklist.md')
Start-Transcript -Path (Join-Path $EvidenceDirectory 'session.txt') -Force | Out-Null
try {
    git branch --show-current
    git rev-parse HEAD
    Get-Content -LiteralPath (Join-Path $repositoryRoot 'VERSION')
    dotnet --info
    Write-Output 'Record monitor, resolution, DPI, GPU/driver, steps and evidence for each checklist row. No row is automatically approved.'
    dotnet build .\game\Evolve.Game.csproj --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
    dotnet run --project .\game\Evolve.Game.csproj --configuration Release --no-build
    $gameExitCode = $LASTEXITCODE
    Write-Output "Game exit code: $gameExitCode. This alone does not prove physical GPU reclamation or every native checklist row."
    if ($gameExitCode -ne 0) { throw "Game exited with code $gameExitCode." }
    Write-Output "Attach manual observations/captures to $EvidenceDirectory and update only rows supported by actual evidence."
} finally {
    Stop-Transcript | Out-Null
}

[CmdletBinding()]
param()
# Four interleaved executions of prebuilt TEST binaries; never builds or launches Game.
$ErrorActionPreference = 'Stop'
$repoRoot = (& git rev-parse --show-toplevel | Out-String).Trim()
if ([IO.Path]::GetFullPath((Get-Location).Path) -ne [IO.Path]::GetFullPath($repoRoot)) { throw 'Run from repository root.' }
$old = 'GEvolve.Tests/bin/ValidationArtifacts/P03-A74-reference_20261108_221618802Z_0fd6f9ac/bin/GEvolve.Tests/debug/GEvolve.Tests.dll'
$new = 'GEvolve.Tests/bin/ValidationArtifacts/P03-A74_20261108_222619723Z_ca4e68da/bin/GEvolve.Tests/debug/GEvolve.Tests.dll'
foreach ($path in @($old,$new)) { if (-not (Test-Path -LiteralPath $path)) { throw 'Required prebuilt test artifacts unavailable; they are deliberately not published.' } }
$utf8 = New-Object Text.UTF8Encoding($false)
$rows = New-Object 'System.Collections.Generic.List[string]'
$rows.Add('# P03-A.7.4 — Paired normal-path timing')
$rows.Add('')
$rows.Add('- UTC start: '+[DateTime]::UtcNow.ToString('o'))
$rows.Add('- Old engine source: 84932b9396001f0c883ce2d4c6a4d4d3dcd29f45; prior-capture harness.')
$rows.Add('- New engine source: d1d4571d262a17ea4b1da0643c4bad0f254448c6.')
$rows.Add('- Order old/new/new/old; no concurrent validation suite; each process warms Small, then three samples per each of eight worlds.')
$rows.Add('- Timings are GenerateGeology, without ecology or detailed fields; no zero-cost/benchmark approval is inferred.')
foreach ($path in @($old,$new)) {
$engine = Join-Path (Split-Path $path) 'GEvolve.Engine.dll'
$rows.Add('- '+$path+': Tests SHA256 '+(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash+'; Engine SHA256 '+(Get-FileHash -LiteralPath $engine -Algorithm SHA256).Hash)
}
$rows.Add('')
$rows.Add('| Run | Exit code | Seconds | Full log |')
$rows.Add('|---|---:|---:|---|')
$failed = $false
foreach ($step in @(@{Name='old-1';File=$old},@{Name='new-1';File=$new},@{Name='new-2';File=$new},@{Name='old-2';File=$old})) {
Write-Host ('START '+$step.Name)
$p=New-Object Diagnostics.Process
$p.StartInfo.FileName=(Get-Command dotnet -CommandType Application).Source
$p.StartInfo.Arguments=$step.File+' --pangaea-base-reference'
$p.StartInfo.WorkingDirectory=$repoRoot
$p.StartInfo.UseShellExecute=$false; $p.StartInfo.CreateNoWindow=$true
$p.StartInfo.RedirectStandardOutput=$true; $p.StartInfo.RedirectStandardError=$true
$p.StartInfo.StandardOutputEncoding=$utf8; $p.StartInfo.StandardErrorEncoding=$utf8
$timer=[Diagnostics.Stopwatch]::StartNew()
try {
[void]$p.Start(); $stdout=$p.StandardOutput.ReadToEndAsync(); $stderr=$p.StandardError.ReadToEndAsync(); $p.WaitForExit()
$out=$stdout.GetAwaiter().GetResult(); $err=$stderr.GetAwaiter().GetResult(); $code=$p.ExitCode
} finally { $timer.Stop(); $p.Dispose() }
$body='Command: dotnet '+$step.File+' --pangaea-base-reference'+"`r`nNative exit code: "+$code+"`r`n=== STDOUT (complete) ===`r`n"+$out+"`r`n=== STDERR (complete) ===`r`n"+$err
foreach ($entry in @(@($repoRoot,'<REPO>'),@($env:USERPROFILE,'<USERPROFILE>'),@($env:TEMP,'<TEMP>'))) {
if (-not [string]::IsNullOrEmpty($entry[0])) { $body=$body.Replace($entry[0],$entry[1]).Replace($entry[0].Replace('\','/'),$entry[1]) }
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot ($step.Name+'.log')),$body,$utf8)
$rows.Add('| '+$step.Name+' | '+$code+' | '+$timer.Elapsed.TotalSeconds.ToString('F3',[Globalization.CultureInfo]::InvariantCulture)+' | ['+$step.Name+'.log]('+$step.Name+'.log) |')
Write-Host ('END '+$step.Name+'; exit='+$code)
if ($code -ne 0) { $failed=$true }
}
$rows.Add(''); $rows.Add('- UTC finish: '+[DateTime]::UtcNow.ToString('o'))
$rows.Add('- Test binaries are isolated/ignored and not published. Logs retain all output; local paths are replaced explicitly. Review before publication.')
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'SUMMARY.md'),($rows -join "`r`n"),$utf8)
if ($failed) { exit 1 }; exit 0

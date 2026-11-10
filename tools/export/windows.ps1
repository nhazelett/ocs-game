param(
    [Parameter(Mandatory)][string]$Godot,
    [Parameter(Mandatory)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$gameRoot = Join-Path $repoRoot 'game'
$output = [IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $output) -or (Test-Path -LiteralPath "$output.zip")) {
    throw 'Choose a new output directory: existing builds and zip files are never overwritten.'
}
$Godot = (Get-Item -LiteralPath $Godot).FullName
$version = & $Godot --version
if ($LASTEXITCODE -ne 0 -or $version -notmatch '^4\.6\.1\.stable\.mono\.') {
    throw "Expected Godot 4.6.1 .NET; got $version"
}

dotnet build (Join-Path $gameRoot 'LinesOfSupport.Game.csproj') -c Debug
if ($LASTEXITCODE -ne 0) { throw 'Game build failed.' }
& $Godot --headless --path $gameRoot --editor --import --quit
if ($LASTEXITCODE -ne 0) { throw 'Godot import failed.' }
New-Item -ItemType Directory -Path $output | Out-Null
$exe = Join-Path $output 'LinesOfSupport.exe'
& $Godot --headless --path $gameRoot --export-release 'Windows Desktop' $exe
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $exe)) { throw 'Windows export failed.' }

# Only runtime JSON is copied, including authored approach logs used by Replay.
# Golden expected reports, reference sources, tools and docs stay in the repo.
$scenarioOutput = New-Item -ItemType Directory -Path (Join-Path $output 'scenarios')
foreach ($folder in Get-ChildItem -LiteralPath (Join-Path $repoRoot 'scenarios') -Directory) {
    if ($folder.Name.StartsWith('_') -or -not (Test-Path -LiteralPath (Join-Path $folder.FullName 'scenario.json'))) { continue }
    $target = New-Item -ItemType Directory -Path (Join-Path $scenarioOutput.FullName $folder.Name)
    Copy-Item -LiteralPath (Join-Path $folder.FullName 'scenario.json') -Destination $target.FullName
    $theme = Join-Path $folder.FullName 'theme.json'
    if (Test-Path -LiteralPath $theme) { Copy-Item -LiteralPath $theme -Destination $target.FullName }
    $golden = Join-Path $folder.FullName 'golden'
    if (Test-Path -LiteralPath $golden) {
        $logs = @(Get-ChildItem -LiteralPath $golden -Filter 'approach*.json' -File)
        if ($logs.Count -gt 0) {
            $replayOutput = New-Item -ItemType Directory -Path (Join-Path $target.FullName 'golden')
            $logs | Copy-Item -Destination $replayOutput.FullName
        }
    }
}
$rulesOutput = New-Item -ItemType Directory -Path (Join-Path $scenarioOutput.FullName 'rules')
Get-ChildItem -LiteralPath (Join-Path $repoRoot 'scenarios/rules') -Filter 'rules-*.json' -File |
    Copy-Item -Destination $rulesOutput.FullName

if (Test-Path -LiteralPath (Join-Path $scenarioOutput.FullName '_stub')) { throw 'Development stub must not ship.' }

# The chosen Tabletop typeface is a runtime dependency, including its redistribution license.
$fontOutput = New-Item -ItemType Directory -Force -Path (Join-Path $output 'assets/art-studies/fonts')
foreach ($font in @('AtkinsonHyperlegible-Regular.ttf', 'AtkinsonHyperlegible-Bold.ttf', 'Atkinson-OFL.txt')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot "assets/art-studies/fonts/$font") -Destination $fontOutput.FullName
}
$portraitOutput = New-Item -ItemType Directory -Force -Path (Join-Path $output 'assets/portraits')
Get-ChildItem -LiteralPath (Join-Path $repoRoot 'assets/portraits') -Filter '*.svg' -File | Copy-Item -Destination $portraitOutput.FullName

# Original short cues and their redistribution grant are runtime files.
$audioOutput = New-Item -ItemType Directory -Path (Join-Path $output 'assets/audio')
Get-ChildItem -LiteralPath (Join-Path $repoRoot 'assets/audio') -File |
    Copy-Item -Destination $audioOutput.FullName

# Launch from outside both the checkout and build folder, so cwd-dependent data loading cannot pass.
# Release templates disallow scene-path overrides; the flag checks the actual main scene instead.
$smokeLog = Join-Path ([IO.Path]::GetTempPath()) ("los-export-" + [guid]::NewGuid().ToString('N') + '.log')
try {
    $process = Start-Process -FilePath $exe -ArgumentList @('--headless', '--log-file', "`"$smokeLog`"", '--', '--export-smoke-test') -WorkingDirectory ([IO.Path]::GetTempPath()) -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(60000)) { $process.Kill(); throw 'Export smoke check timed out.' }
    $log = Get-Content -LiteralPath $smokeLog
    $log | Write-Host
    if ($process.ExitCode -ne 0 -or -not ($log -match 'EXPORT_SMOKE_PASS:')) { throw 'Exported game smoke check failed.' }
} finally { if (Test-Path -LiteralPath $smokeLog) { Remove-Item -LiteralPath $smokeLog } }

Compress-Archive -Path (Join-Path $output '*') -DestinationPath "$output.zip"
Write-Host "Windows package: $output.zip"

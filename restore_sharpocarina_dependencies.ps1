$ErrorActionPreference = "Stop"

$nuget = Join-Path $env:TEMP "nuget.exe"
if (-not (Test-Path $nuget)) {
    Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nuget
}

& $nuget restore (Join-Path $PSScriptRoot "SharpOcarina.sln") -NonInteractive

$archive = Join-Path $env:TEMP "Zelda64-Text-Editor.zip"
$extract = Join-Path $env:TEMP "Zelda64-Text-Editor-sharpocarina"
Invoke-WebRequest "https://github.com/skawo/Zelda64-Text-Editor/releases/download/v.3.62/Zelda.64.Text.Editor.3.62.zip" -OutFile $archive
Expand-Archive $archive $extract -Force
Copy-Item (Join-Path $extract "ZeldaMsgPreview.dll") (Join-Path $PSScriptRoot "ZeldaMsgPreview.dll") -Force

Write-Host "SharpOcarina dependencies restored. Build SharpOcarina.sln with Visual Studio or MSBuild."

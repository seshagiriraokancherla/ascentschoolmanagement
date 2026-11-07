# Builds AscentBellTool.exe (Release) and, if Inno Setup 6 is installed, Installer\Output\AscentBellSetup.exe.
# Usage (from anywhere):  .\AscentBellTool\build.ps1
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'MSBuild not found (install Visual Studio or Build Tools).' }

& $msbuild "$root\AscentBellTool.csproj" /p:Configuration=Release /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed ($LASTEXITCODE)" }

$iscc = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe", "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    Write-Warning 'Inno Setup 6 not found — exe built, installer skipped. Install from https://jrsoftware.org/isdl.php and re-run.'
    return
}

& $iscc "$root\Installer\AscentBell.iss"
if ($LASTEXITCODE -ne 0) { throw "Installer build failed ($LASTEXITCODE)" }
Write-Host "Installer: $root\Installer\Output\AscentBellSetup.exe" -ForegroundColor Green

# Build (MSBuild publish) + FTP-deploy the .NET API.
#   .\deploy\deploy-api.ps1              # publish (Release/FolderProfile) then upload
#   .\deploy\deploy-api.ps1 -SkipBuild   # upload the existing publish folder only
#
# Notes:
#  - Uploads/ is never uploaded (excludeDirs in config) — preserves server media.
#  - web.config: uploaded only if uploadWebConfig=true AND it doesn't still contain
#    the placeholder Jwt:Secret (guard against breaking production auth).
#  - Additive upload (nothing deleted on the server).
param(
    [switch] $SkipBuild
)

. (Join-Path $PSScriptRoot '_common.ps1')

$cfg     = Get-DeployConfig
$repo    = Get-RepoRoot
$proj    = Join-Path $repo $cfg.api.projectDir
$publish = Join-Path $repo $cfg.api.publishDir

if (-not $SkipBuild) {
    Write-Host "=== Publishing API (Release / FolderProfile) ===" -ForegroundColor Yellow

    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) { $vswhere = "$env:ProgramFiles\Microsoft Visual Studio\Installer\vswhere.exe" }
    if (-not (Test-Path $vswhere)) { throw "vswhere.exe not found. Install Visual Studio / Build Tools, or run with -SkipBuild after publishing from Visual Studio." }

    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
    if (-not $msbuild) { throw "MSBuild not found via vswhere. Run with -SkipBuild after publishing from Visual Studio." }

    # Locate the installed web-publish targets so the build works under any VS version
    # (the csproj imports them via $(VSToolsPath), which we point at the real folder).
    $webTargets = & $vswhere -latest -find "MSBuild\**\WebApplications\Microsoft.WebApplication.targets" | Select-Object -First 1
    if (-not $webTargets) { throw "WebApplication.targets not found. Install the 'ASP.NET and web development' workload, or run with -SkipBuild." }
    $vsToolsPath = Split-Path (Split-Path $webTargets -Parent) -Parent   # -> ...\Microsoft\VisualStudio\v<N>.0

    Write-Host "MSBuild:     $msbuild"
    Write-Host "VSToolsPath: $vsToolsPath"

    $csproj = Join-Path $proj 'AscentSchools.API.csproj'
    & $msbuild $csproj /p:DeployOnBuild=true /p:PublishProfile=FolderProfile /p:Configuration=Release /p:VSToolsPath="$vsToolsPath" /m /nologo /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "API publish failed." }
}

if (-not (Test-Path $publish)) {
    throw "Publish folder not found: $publish`nBuild first (drop -SkipBuild) or publish from Visual Studio, or fix api.publishDir."
}

# web.config safety guard
$excludeFiles = @()
if ($cfg.api.uploadWebConfig) {
    $webcfg = Join-Path $publish 'Web.config'
    if ((Test-Path $webcfg) -and (Select-String -LiteralPath $webcfg -Pattern 'CHANGE_THIS_TO_A_STRONG_SECRET' -Quiet)) {
        Write-Warning "Web.config still contains the placeholder Jwt:Secret - SKIPPING it to avoid breaking production. Configure real values or upload web.config manually."
        $excludeFiles += 'Web.config'
    }
} else {
    $excludeFiles += 'Web.config'
}

Send-FtpTree -LocalDir $publish -RemotePath $cfg.api.remotePath -Config $cfg `
    -ExcludeDirs $cfg.api.excludeDirs -ExcludeFiles $excludeFiles

Write-Host "`nAPI deploy complete." -ForegroundColor Green

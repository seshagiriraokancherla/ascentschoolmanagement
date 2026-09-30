# Build + FTP-deploy the Control app (single build, no per-school flavors) to
# the root of edu-care.in's httpdocs/ (same FTP account as the API/school/parent apps).
# Additive upload only — never touches sibling /httpdocs/api, /httpdocs/{chakin,depaul,...}.
#   .\deploy\deploy-control.ps1
#   .\deploy\deploy-control.ps1 -SkipBuild
param(
    [switch] $SkipBuild
)

. (Join-Path $PSScriptRoot '_common.ps1')

$cfg    = Get-DeployConfig
$repo   = Get-RepoRoot
$appDir = Join-Path $repo $cfg.control.appDir
$outRel = 'dist'
$outAbs = Join-Path $appDir $outRel

if (-not $SkipBuild) {
    Write-Host "`n=== Building Control app ===" -ForegroundColor Yellow
    Push-Location $appDir
    try {
        # No .env.production exists for this app — VITE_API_URL must be injected via
        # process env at build time, else it falls back to the localhost dev default.
        $prevApiUrl = $env:VITE_API_URL
        $env:VITE_API_URL = $cfg.control.apiUrl
        $prev = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        & npm run build "--" "--outDir" $outRel "--emptyOutDir" 2>&1 | ForEach-Object { Write-Host $_ }
        $code = $LASTEXITCODE
        $ErrorActionPreference = $prev
        if ($code -ne 0) { throw "Build failed for Control app" }
    } finally {
        $env:VITE_API_URL = $prevApiUrl
        Pop-Location
    }
}

Write-Host "`n=== Deploying Control app -> $($cfg.control.remotePath) ===" -ForegroundColor Yellow
Send-FtpTree -LocalDir $outAbs -RemotePath $cfg.control.remotePath -Config $cfg

Write-Host "`nControl app deploy complete." -ForegroundColor Green

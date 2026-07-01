# Build + FTP-deploy the School app for one school (or all).
#   .\deploy\deploy-school.ps1                 # all schools
#   .\deploy\deploy-school.ps1 -School stannsasf
#   .\deploy\deploy-school.ps1 -School stannsasf -SkipBuild   # upload existing build only
param(
    [string] $School = 'all',
    [switch] $SkipBuild
)

. (Join-Path $PSScriptRoot '_common.ps1')

$cfg    = Get-DeployConfig
$repo   = Get-RepoRoot
$appDir = Join-Path $repo $cfg.schoolApp.appDir

$targets = if ($School -eq 'all') { $cfg.schools } else { $cfg.schools | Where-Object { $_.name -eq $School } }
if (-not $targets) { throw "Unknown school '$School'. Known: $(( $cfg.schools.name) -join ', ')" }

foreach ($s in $targets) {
    Write-Host "`n=== School app: $($s.name) ===" -ForegroundColor Yellow
    if (-not $s.schoolRemote) { Write-Warning "No schoolRemote configured for $($s.name) - skipping."; continue }

    $outRel = "dist/$($s.name)"
    $outAbs = Join-Path $appDir $outRel
    if (-not $SkipBuild) { Invoke-ViteBuild $appDir $s.mode $outRel }

    Send-FtpTree -LocalDir $outAbs -RemotePath $s.schoolRemote -Config $cfg
}

Write-Host "`nSchool deploy complete." -ForegroundColor Green

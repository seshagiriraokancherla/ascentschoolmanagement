# Build + FTP-deploy the Parent app for one school (or all).
#   .\deploy\deploy-parent.ps1                 # all schools that have a parentRemote
#   .\deploy\deploy-parent.ps1 -School stannsasf
#   .\deploy\deploy-parent.ps1 -School stannsasf -SkipBuild
param(
    [string] $School = 'all',
    [switch] $SkipBuild
)

. (Join-Path $PSScriptRoot '_common.ps1')

$cfg    = Get-DeployConfig
$repo   = Get-RepoRoot
$appDir = Join-Path $repo $cfg.parentApp.appDir

$targets = if ($School -eq 'all') { $cfg.schools } else { $cfg.schools | Where-Object { $_.name -eq $School } }
if (-not $targets) { throw "Unknown school '$School'. Known: $(( $cfg.schools.name) -join ', ')" }

foreach ($s in $targets) {
    if (-not $s.parentRemote) {
        if ($School -ne 'all') { Write-Warning "No parentRemote configured for $($s.name)." }
        continue
    }
    Write-Host "`n=== Parent app: $($s.name) ===" -ForegroundColor Yellow

    $outRel = "dist/$($s.name)"
    $outAbs = Join-Path $appDir $outRel
    if (-not $SkipBuild) { Invoke-ViteBuild $appDir $s.mode $outRel }

    Send-FtpTree -LocalDir $outAbs -RemotePath $s.parentRemote -Config $cfg
}

Write-Host "`nParent deploy complete." -ForegroundColor Green

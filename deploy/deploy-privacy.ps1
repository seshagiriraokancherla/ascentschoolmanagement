# Deploy the static per-school privacy-policy pages to edu-care.in/{folder}/.
#   .\deploy\deploy-privacy.ps1                        # privacy-policy.html only
#   .\deploy\deploy-privacy.ps1 -IncludeAccountDeletion  # also re-upload account-deletion.html
#
# Served at https://edu-care.in/{folder}/privacy-policy.html
#   -> remote /httpdocs/{folder}/privacy-policy.html   (docroot derived from api.remotePath)
param([switch] $IncludeAccountDeletion)

. (Join-Path $PSScriptRoot '_common.ps1')

$cfg     = Get-DeployConfig
$repo    = Get-RepoRoot
$cred    = New-FtpCredential $cfg
$passive = [bool]$cfg.ftp.passive

# Main-domain docroot = api.remotePath with the trailing /api removed (e.g. /httpdocs/api -> /httpdocs)
$docroot = ($cfg.api.remotePath -replace '/api/?$', '')
if ([string]::IsNullOrWhiteSpace($docroot)) { $docroot = '/httpdocs' }

$folders = @('chakin', 'stannsasf', 'holyspiritjm', 'depaul')
$files   = @('privacy-policy.html')
if ($IncludeAccountDeletion) { $files += 'account-deletion.html' }

Write-Host "Docroot: $docroot" -ForegroundColor Yellow
foreach ($f in $folders) {
    $remoteDir = "$docroot/$f"
    Confirm-FtpDir (Get-FtpUri $cfg $remoteDir $null) $cred $passive
    foreach ($file in $files) {
        $local = Join-Path $repo (Join-Path $f $file)
        if (-not (Test-Path -LiteralPath $local)) { Write-Warning "skip (missing): $local"; continue }
        Send-FtpFile $local (Get-FtpUri $cfg $remoteDir $file) $cred $passive
        Write-Host "  Uploaded $f/$file -> $remoteDir/$file" -ForegroundColor Green
    }
}
Write-Host "Privacy pages deployed." -ForegroundColor Cyan

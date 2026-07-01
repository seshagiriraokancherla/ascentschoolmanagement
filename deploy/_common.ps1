# Shared helpers for the deploy scripts: config loading + native FTP upload
# (no third-party tools — uses System.Net.FtpWebRequest).

$ErrorActionPreference = 'Stop'

function Get-DeployConfig {
    $path = Join-Path $PSScriptRoot 'deploy.config.json'
    if (-not (Test-Path $path)) {
        throw "Missing deploy.config.json. Copy deploy.config.example.json to deploy.config.json and fill in the FTP password + paths."
    }
    $cfg = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    if ([string]::IsNullOrWhiteSpace($cfg.ftp.password) -or $cfg.ftp.password -eq 'REPLACE_WITH_FTP_PASSWORD') {
        throw "Set ftp.password in deploy/deploy.config.json before deploying."
    }
    return $cfg
}

function Get-RepoRoot { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }

function New-FtpCredential($cfg) {
    return New-Object System.Net.NetworkCredential($cfg.ftp.username, $cfg.ftp.password)
}

# Build ftp://host/<remotePath>/<relative> with each path segment URL-escaped.
function Get-FtpUri($cfg, $remotePath, $relative) {
    $segments = @()
    foreach ($p in ($remotePath -split '/'))                 { if ($p) { $segments += $p } }
    if ($relative) { foreach ($p in (($relative -replace '\\','/') -split '/')) { if ($p) { $segments += $p } } }
    $escaped = ($segments | ForEach-Object { [Uri]::EscapeDataString($_) }) -join '/'
    return "ftp://$($cfg.ftp.host)/$escaped"
}

function Confirm-FtpDir($uri, $cred, $passive) {
    try {
        $req = [System.Net.FtpWebRequest]::Create($uri)
        $req.Credentials = $cred
        $req.Method      = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $req.UsePassive  = $passive
        $resp = $req.GetResponse(); $resp.Close()
    } catch [System.Net.WebException] {
        # 550 = directory already exists -> ignore; close the response either way
        if ($_.Exception.Response) { $_.Exception.Response.Close() }
    }
}

function Send-FtpFile($localFile, $uri, $cred, $passive) {
    $req = [System.Net.FtpWebRequest]::Create($uri)
    $req.Credentials  = $cred
    $req.Method       = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $req.UseBinary    = $true
    $req.UsePassive   = $passive
    $bytes = [System.IO.File]::ReadAllBytes($localFile)
    $req.ContentLength = $bytes.Length
    $s = $req.GetRequestStream()
    $s.Write($bytes, 0, $bytes.Length)
    $s.Close()
    $resp = $req.GetResponse(); $resp.Close()
}

# Recursively upload $LocalDir to $RemotePath. Native FTP has no diff-sync, so this
# uploads every file each run (creating remote dirs as needed). Existing remote files
# not in $LocalDir are left untouched (additive — nothing is deleted).
function Send-FtpTree {
    param(
        [Parameter(Mandatory)] [string] $LocalDir,
        [Parameter(Mandatory)] [string] $RemotePath,
        [Parameter(Mandatory)] $Config,
        [string[]] $ExcludeDirs  = @(),
        [string[]] $ExcludeFiles = @()
    )
    if (-not (Test-Path -LiteralPath $LocalDir)) { throw "Local folder not found: $LocalDir" }

    $cred    = New-FtpCredential $Config
    $passive = [bool]$Config.ftp.passive
    $root    = (Resolve-Path -LiteralPath $LocalDir).Path
    if (-not $root.EndsWith([IO.Path]::DirectorySeparatorChar)) { $root += [IO.Path]::DirectorySeparatorChar }

    function Test-Excluded($rel) {
        $top = ($rel -split '[\\/]')[0]
        return ($ExcludeDirs -contains $top)
    }

    Write-Host "Uploading -> $RemotePath" -ForegroundColor Cyan
    Confirm-FtpDir (Get-FtpUri $Config $RemotePath $null) $cred $passive

    # Create directories parent-first
    Get-ChildItem -LiteralPath $root -Recurse -Directory |
        Sort-Object { $_.FullName.Length } |
        ForEach-Object {
            $rel = $_.FullName.Substring($root.Length)
            if (-not (Test-Excluded $rel)) {
                Confirm-FtpDir (Get-FtpUri $Config $RemotePath $rel) $cred $passive
            }
        }

    # Upload files
    $count = 0
    Get-ChildItem -LiteralPath $root -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring($root.Length)
        if ((Test-Excluded $rel) -or ($ExcludeFiles -contains $_.Name)) { return }
        Send-FtpFile $_.FullName (Get-FtpUri $Config $RemotePath $rel) $cred $passive
        $count++
        if ($count % 25 -eq 0) { Write-Host "  ...$count files" }
    }
    Write-Host "Uploaded $count files to $RemotePath" -ForegroundColor Green
}

# Build one Vite app flavor into its own output folder (avoids the shared dist/ clobber).
function Invoke-ViteBuild($appDir, $mode, $outRel) {
    Push-Location $appDir
    try {
        # Vite writes warnings (e.g. chunk-size) to stderr; with ErrorActionPreference=Stop
        # PowerShell would treat that as fatal. Relax it and rely on the real exit code.
        $prev = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        & npm run "build:$mode" "--" "--outDir" $outRel "--emptyOutDir" 2>&1 | ForEach-Object { Write-Host $_ }
        $code = $LASTEXITCODE
        $ErrorActionPreference = $prev
        if ($code -ne 0) { throw "Build failed for mode '$mode' in $appDir" }
    } finally { Pop-Location }
}

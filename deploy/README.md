# Deploy scripts

One-command build + FTP deploy for the three targets. Native PowerShell FTP
(`System.Net.FtpWebRequest`) — no WinSCP/other tools needed.

## One-time setup
1. `copy deploy\deploy.config.example.json deploy\deploy.config.json` (already created; gitignored).
2. Put the **FTP password** in `deploy/deploy.config.json` (`ftp.password`).
3. **Verify each `schoolRemote` / `parentRemote`** path against Plesk
   (Websites & Domains -> Hosting Settings -> Document Root). Only `stannsasf` and the
   API path (`/httpdocs/api`) are confirmed; the rest are pattern-guessed.
   Set `parentRemote` to `""` for any school with no parent subdomain.

## Usage (run from the repo root)
```powershell
# School app
.\deploy\deploy-school.ps1                      # all schools
.\deploy\deploy-school.ps1 -School stannsasf    # one school
.\deploy\deploy-school.ps1 -School stannsasf -SkipBuild   # upload existing build only

# Parent app (same flags; skips schools whose parentRemote is "")
.\deploy\deploy-parent.ps1
.\deploy\deploy-parent.ps1 -School stannsasf

# API (publish Release via FolderProfile, then upload)
.\deploy\deploy-api.ps1
.\deploy\deploy-api.ps1 -SkipBuild              # upload existing publish folder only
```

## What the scripts do
- **School/Parent:** build each flavor into its **own** folder (`dist/<name>`) via
  `npm run build:<mode> -- --outDir dist/<name>` (no more shared-`dist` clobbering),
  then upload that folder to the configured docroot. The SPA-fallback files
  (`.htaccess` / `web.config`) in the build are uploaded too.
- **API:** MSBuild publish (Release / `FolderProfile` -> `bin/Release/Publish`), then upload.

## Safety behavior (API)
- **`Uploads/` is never uploaded** (`api.excludeDirs`) so server media (logos, photos,
  attachments) is preserved.
- **`web.config`** is uploaded only if `api.uploadWebConfig=true` **and** it does not still
  contain the placeholder `Jwt:Secret`. If it does, it's **skipped** (so you can't
  accidentally overwrite production secrets with the template). Set real prod values in the
  published `web.config` (or upload it manually) to push it.

## Caveats
- Native FTP has no diff-sync: every run uploads the **full** file set (dirs created as
  needed). Nothing on the server is **deleted** (additive).
- Plain FTP sends credentials/files in cleartext. Prefer FTPS if your host supports it.
- API file locks: copying DLLs while the app runs can occasionally 500. If you hit that,
  recycle the app pool in Plesk (or drop an `app_offline.htm` before/after).
- Rotate the FTP password periodically; `deploy.config.json` is gitignored — never commit it.

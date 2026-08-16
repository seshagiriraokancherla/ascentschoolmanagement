# Cloudflare R2 Storage — Per-School Setup Guide

File storage for the school apps (student photos, homework / announcement / event
attachments) uses **Cloudflare R2**, configured **per school**. Uploads go straight
from the browser to R2 via presigned URLs — our API only signs the request and records
the public URL. This guide is the repeatable checklist for onboarding a new school onto R2.

> **When to do this:** whenever a new school needs file uploads (student photos, media
> attachments). Until R2 is configured + enabled for that school, uploads are skipped
> gracefully — a student still saves, the photo is just omitted with a warning.

---

## The 5 values the app collects

School web app → **Settings → R2 Storage** asks for these:

| Field | Where it comes from |
|---|---|
| **Account ID** | Cloudflare account ID — the `<ACCOUNT_ID>` in the S3 endpoint `https://<ACCOUNT_ID>.r2.cloudflarestorage.com` (also on the R2 overview page). |
| **Access Key ID** | From an R2 API token (created in step 3). |
| **Secret Access Key** | Shown **once** when the token is created — copy it then. Write-only in the app (never returned; leave blank on later edits to keep the stored value). |
| **Bucket Name** | The bucket you create for this school (step 1). |
| **Public Base URL** | The bucket's public URL (r2.dev subdomain or custom domain), **no trailing slash** (step 2). |

Plus an **Enabled** toggle.

---

## Step-by-step (Cloudflare dashboard)

### 1. Create a bucket (one per school — recommended)
- R2 → **Create bucket** → name it, e.g. `stannsasf-media` → **Create**.
- One bucket per school keeps each school's media cleanly separable (isolation, easy
  offboarding). The config is per-school either way, but separate buckets are cleaner.

### 2. Enable public read access → gives the **Public Base URL**
- Open the bucket → **Settings** → **Public access**.
- Turn on the **R2.dev subdomain** (fastest) → you get `https://pub-xxxxxxxx.r2.dev`,
  **or** connect a **Custom Domain** like `https://files.<school>.edu-care.in`.
- Copy that URL (**no trailing slash**) → this is the **Public Base URL**.
- Required so stored photos/PDFs are viewable in the web + mobile apps (direct GET reads).

### 3. Create an API token → gives **Access Key ID + Secret + Account ID**
- R2 → **Manage R2 API Tokens** → **Create API token**.
- Permission: **Object Read & Write**.
- Scope: **this bucket only** (recommended) or all buckets.
- **Create** → copy the **Access Key ID**, **Secret Access Key**, and note the
  **Account ID** from the shown S3 endpoint. The secret is displayed only once.

### 4. Add a CORS policy (required — staff upload directly from the browser to R2)
- Bucket → **Settings** → **CORS policy** → **Add/Edit** → paste:

```json
[
  {
    "AllowedOrigins": ["https://THE-SCHOOL-SITE-ORIGIN"],
    "AllowedMethods": ["PUT", "GET"],
    "AllowedHeaders": ["*"],
    "MaxAgeSeconds": 3600
  }
]
```

- Replace `THE-SCHOOL-SITE-ORIGIN` with the **exact origin** staff use to open the school
  web app — open it and copy the address-bar origin (e.g. `https://stannsasf.edu-care.in`
  or `https://edu-care.in`). No path, no trailing slash. List multiple origins if needed.
- **Why:** the app asks our API for a presigned URL, then the browser does a direct **PUT**
  to R2. Without this rule the PUT is blocked by CORS. The upload sends only a `Content-Type`
  request header, so `"*"` (or `"Content-Type"`) covers it.

---

## In the app

### 5. Prerequisite (once per tenant DB)
The `r2_configs` table must exist in the school's tenant DB. If the DB was created **before**
R2 existed, run **`Database/r2_config_migration.sql`** against that school's
`ascent_group_{N}` DB. New groups already include it (part of `tenant_tables.sql`).

### 6. Enter the values
School app → **Settings → R2 Storage** (needs the `SETTINGS.MANAGE` permission) → fill
Account ID, Access Key ID, Secret, Bucket Name, Public Base URL → **Enabled** on → **Save**.

### 7. Verify
Open a student → upload a photo (≤ 1 MB, jpg/png/webp). If it saves and displays, R2 works.
If the PUT fails, it's almost always the **CORS origin** not matching the school's actual
site origin (step 4).

---

## Upload caps (enforced by the app)

| Type | Limit | Accepted |
|---|---|---|
| Image | 1 MB | jpg / png / webp |
| Document | 2 MB | pdf |
| Audio | 10 MB | mp3 |
| Video | 1 GB | mp4 |

---

## How it works (reference)

- **Direct-to-R2 uploads:** browser → `POST /school/uploads/presign` (our API signs a
  SigV4 presigned PUT URL; host-only signed header, UNSIGNED-PAYLOAD, region `auto`) →
  browser PUTs the file straight to R2 → the returned **public URL** is saved in the DB.
  File bytes never pass through our API/IIS (needed for the 1 GB event videos).
- **Secret handling:** `secret_access_key` is stored in `r2_configs` and **never returned**
  by any API response — the settings page only ever shows a `hasSecretKey` flag.
- **Object key layout:** student photos → `student-images/{AdmissionNo}_{academicYear}.{ext}`;
  homework → `homeworks/{academicYear}/{id}/…` (year folders, so old years can be pruned);
  announcements → `announcements/{id}/…`; events → `events/{id}/…`.

**Related code:** `R2ConfigRepository`, `R2Service` (manual SigV4), `SchoolUploadsController`
(`GET/PUT /school/settings/r2`, `POST /school/uploads/presign`, `POST /school/media/attach`),
`pages/settings/R2StoragePage.jsx`, `api/r2Upload.js`. See Phases 84 & 85.

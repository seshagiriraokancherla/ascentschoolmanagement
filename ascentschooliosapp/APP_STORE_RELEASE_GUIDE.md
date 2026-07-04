# iOS App Store Release Guide — Ascent Schools

Roadmap for publishing the iOS apps to the App Store. Each school is a separate
App Store listing. This is the high-level plan; detailed click-by-click steps
for each step are added as we work through them.

---

## The big picture

We ship **5 separate App Store apps**, each its own listing, each built from
the same Xcode project via its own scheme + build configuration + `.xcconfig`.

| App | Bundle ID | Xcode Scheme | Config file | School resolution |
|---|---|---|---|---|
| CHAK IN | `in.educare.app` | Chakin | `Config/Chakin.xcconfig` | Runtime (parent enters 4-digit school code) |
| St Anns | `in.educare.stannsasf` | Stannsasf | `Config/Stannsasf.xcconfig` | Baked (school fixed at build time) |
| Depaul | `in.educare.depaulemyv` | Depaulemyv | `Config/Depaulemyv.xcconfig` | Baked |
| Holy Spirit | `in.educare.holyspiritjm` | Holyspiritjm | `Config/Holyspiritjm.xcconfig` | Baked |
| Demo | `in.educare.demo` | Demo | `Config/Demo.xcconfig` | Baked (usually internal/testing — not published) |

- **CHAK IN** is the flexible generic app: any school can use it by entering the
  4-digit `login_code` shared by their office. It shows the school-code screen
  on first launch, then loads that school's branding.
- **Baked apps** have `SCHOOL_CODE` compiled in, skip the code screen, and are
  branded per school.

---

## Roadmap (high level)

### Step 1 — One-time account setup
- Confirm Apple Developer Program membership is active (paid, ~$99/yr).
- In Xcode → Settings → Accounts, add your Apple ID; confirm the Team
  (`DKVJKK3UBT`, already in `Config/Common.xcconfig`) is selectable.

### Step 2 — Per-app prep inside Xcode (once per app)
- **App icon**: each app needs a real 1024×1024 icon (CHAK IN logo + each
  school's logo). Currently all reuse the shared `AppIcon` set — create one
  icon set per flavor and point each `.xcconfig`'s
  `ASSETCATALOG_COMPILER_APPICON_NAME` at it.
- **Version & build number**: set marketing version (e.g. `1.0.0`) and build
  number (e.g. `1`) — these live in `Config/Common.xcconfig`
  (`MARKETING_VERSION`, `CURRENT_PROJECT_VERSION`) or can be overridden per
  flavor.
- **Signing**: switch each scheme's signing to your Team with "Automatically
  manage signing".

### Step 3 — Register App IDs (Apple Developer portal)
- Create an App ID (identifier) for each bundle id, OR let Xcode auto-create
  them on first upload.

### Step 4 — Create app records (App Store Connect)
- One "New App" record per bundle id — name, primary language, category, etc.

### Step 5 — Archive & upload each app
- Select the scheme → **Product → Archive** → upload the build to App Store
  Connect via the Organizer window.
- Repeat per school (change scheme, archive, upload).

### Step 6 — Fill in each listing
- Screenshots (required sizes — at least 6.7" iPhone), description, keywords,
  support URL, **privacy policy URL** (already exists on the website — reused
  from Android), the **App Privacy questionnaire** (declare data collection:
  Razorpay payments, SMS OTP, student data), and **App Review notes**.
- **CHAK IN review notes MUST include the demo login**:
  `School code 1000, mobile 9999999999, OTP 123456`
  (demo bypass, seeded on the server) so Apple review can sign in.

### Step 7 — Submit for review
- Submit each app. Apple review is typically 24–48 h. Fix any rejections and
  resubmit.

---

## Suggested order

1. Publish **CHAK IN first** — it's the most flexible (any school works via the
   code) and gets us through the review process once.
2. Then repeat the now-familiar process for the baked school apps
   (Stannsasf / Depaulemyv / Holyspiritjm).
3. **Demo** typically stays unpublished (internal testing only).

---

## Before starting — gather these

1. **App icons** — 1024×1024 PNG for CHAK IN and each school. Without them the
   apps look generic and Apple may reject. A placeholder can get through the
   first *build* but not final submission.
2. **Screenshots** — at least one 6.7" iPhone set per app; capture from the
   simulator once the app runs with realistic data.
3. **Server prerequisites** (mirror the Android setup):
   - `ascent_master.app_config` row for each iOS bundle id with
     `platform='ios'` + an App Store `store_url` (drives the in-app update gate
     + the "Update App" button). Until set, the update check fails open (no
     prompt) — safe, but the manual button won't have a URL.
   - Privacy policy + account-deletion URLs set in each App Store Connect
     listing (reuse the existing website pages).

---

## Status / notes

- ✅ Xcode project builds clean on the Chakin scheme; school-code screen shows
  on first launch (generic gate verified in the simulator).
- ✅ `Config/Chakin.xcconfig` created (root `Config/` folder, referenced in the
  project); `Chakin` build configuration + scheme wired up.
- ⏳ App icons, screenshots, App Store Connect records, and the per-bundle
  `app_config` rows are not done yet.
- **Reminder**: never edit `*.xcodeproj/project.pbxproj` outside Xcode — do all
  config/scheme/target changes through the Xcode UI.

---

*Detailed click-by-click steps for each step above will be filled in as we work
through them.*

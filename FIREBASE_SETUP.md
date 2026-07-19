# Firebase Cloud Messaging (FCM) Setup — Ascent Schools

Reference for setting up push notifications, and for adding **new white-label school apps**
to the existing Firebase project later.

Push uses **FCM HTTP v1** (the legacy server-key API is retired). The backend mints an
OAuth2 token from a **service-account JSON**; the app is configured by **google-services.json**.

---

## One-time project setup (already done)

1. **Create the Firebase project** — https://console.firebase.google.com → *Add project*.
   - One project (`ascent-schools`) serves **all** white-label apps.
   - Google Analytics not required for FCM.

2. **Register each Android app** (one per applicationId):
   - Project Overview → **Android** icon (*Add app*).
   - **Android package name** = the exact `applicationId` from `app/build.gradle.kts`.
   - **Debug SHA-1: leave blank** — FCM does not need it (SHA-1 is only for Auth / Dynamic Links).
   - Register → skip the SDK snippet steps → *Continue to console*.

   Currently registered applicationIds:
   | Flavor | applicationId |
   |---|---|
   | stannsasf | `in.educare.stannsasf` |
   | holyspiritjm | `in.educare.holyspiritjm` |
   | Demo | `in.educare.demo` |
   | depaulemyv | `in.educare.depaulemyv` |
   | ascent (generic / CHAK IN) | `in.educare.app` |

3. **Download `google-services.json`** (the merged, multi-client file):
   - Project settings (gear) → **General** → *Your apps* → download `google-services.json`.
   - Once multiple apps exist, this single file contains **all** of them (a `client[]` entry each).
   - Place it at **`AscentSchoolsMobile/app/google-services.json`**.
   - The `com.google.gms.google-services` Gradle plugin reads it at build time and fails the build
     if a flavor's applicationId has no matching client entry — so verify all apps are registered.

4. **Generate the service-account key** (backend send credential):
   - Project settings → **Service accounts** → **Generate new private key** → downloads a JSON.
   - Place it at **`API/AscentSchools.API/App_Data/firebase-service-account.json`**.
   - `App_Data` is chosen deliberately: **IIS denies HTTP access to `App_Data`**, so the secret
     is never web-served. Web.config points to it via `Fcm:ServiceAccountPath` (`~/App_Data/...`).

---

## Secrets & git

- **`firebase-service-account.json` is a real secret** (grants FCM send on the whole project).
  It is **gitignored** (`**/firebase-service-account.json`) — never commit it. Keep a private backup.
- `google-services.json` is a client config (not a secret) but is also gitignored to keep it out of the repo.
- Both files must be present **locally** to build/deploy; they are not in source control.

---

## Adding a NEW school app (repeat per new flavor)

1. Add the flavor in `app/build.gradle.kts` (new `applicationId`, `SCHOOL_CODE`, `app_name`) — normal white-label step.
2. Firebase console → **Add app** → Android → enter the new `applicationId` (SHA-1 blank).
3. Re-download `google-services.json` (now includes the new client) → overwrite `app/google-services.json`.
4. **Master DB:** add a row to `app_config` for the new applicationId (update gate) — unrelated to FCM but part of onboarding.
5. Build the flavor. Nothing else — the backend send path and service account are shared across all apps.

> The **service account is per Firebase *project*, not per app** — a new flavor needs a new
> `google-services.json` but the **same** `firebase-service-account.json`.

---

## How it works (implementation)

**Backend** (`API/AscentSchools.API/Helpers/`):
- `FcmPushService` — hand-rolls the v1 auth (no SDK, same style as R2's manual SigV4):
  builds an RS256-signed JWT from the service account → exchanges it at Google's token endpoint
  for an access token (cached ~55 min) → `POST /v1/projects/{projectId}/messages:send` per token.
  Reads the JSON path from Web.config `Fcm:ServiceAccountPath`. **Unconfigured / missing file →
  push silently no-ops** (never breaks a create).
- `PushNotifier` — resolves target parent tokens (class/section/school) and fans out. Fire-and-forget
  on a background task; captures values on the request thread (never touches `TenantContext` inside
  the task — see the Phase 61 SMS `Task.Run` NRE rule).
- Tokens live in **master DB** `device_push_tokens` (one row per device; parent or teacher).

**Triggers** (server-side, so web + mobile both notify): announcement create (web + teacher),
homework create + `/batch`, event create.

**Android:**
- `com.google.gms.google-services` plugin + `firebase-bom` + `firebase-messaging`.
- `push/AscentFirebaseMessagingService` — `onNewToken` re-registers (if logged in); `onMessageReceived`
  shows a local notification. Channel `ascent_default` (created in `AscentApp` + manifest meta-data).
- `POST_NOTIFICATIONS` permission (runtime prompt on Android 13+).
- `data/repository/PushRepository` — fetches the FCM token and calls
  `POST /mobile/notifications/register-token` (after login + cold-start refresh) /
  `unregister-token` (on logout).

---

## Testing notes

- FCM delivery works on any install (debug/sideload/Play) — you can test push end-to-end from Android Studio.
  (Unlike Play In-App Updates, which need a Play/Internal-App-Sharing install.)
- A **notification-payload** message is drawn by the system when the app is backgrounded (uses the
  manifest default channel); **foreground** messages go through `onMessageReceived`. The backend sends
  both `notification` + `data`, so it works in every state.
- Quick manual test: Firebase console → **Messaging** → *New campaign* / *Send test message* → paste a
  device's FCM token (log it from `PushRepository.fetchFcmToken` if needed).

## Data safety (Play Console)

- Declare the **FCM token = "Device or other IDs"**, linked to the account.
- `POST_NOTIFICATIONS` is not a sensitive permission (no extra form).
- Avoid spammy notifications (Play policy).

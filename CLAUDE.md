# Ascent Schools Application — Project Guide

## Overview
Converting a legacy VB6 school management application (SAS) into a modern **SaaS** web + mobile platform.
Multi-tenant architecture: one DB per school group/society, shared master DB for SaaS control.

---

## Tech Stack

| Layer | Technology | Notes |
|---|---|---|
| Frontend — School App | React JS + Vite | Ant Design UI, Zustand state |
| Frontend — Control App | React JS + Vite | Ant Design UI, Zustand state |
| Backend API | .NET Framework 4.8 Web API | Single API serving both apps |
| ORM / Data Access | Dapper | Lightweight, SQL-friendly, easy multi-tenant connection switching |
| Database | SQL Server | Master DB + one tenant DB per school group |
| Auth | JWT (access + refresh token) | See Auth section below |
| UI Component Library | Ant Design (AntD) | MIT license, fully free, enterprise tables/forms built-in |
| State Management | Zustand | Lightweight, simple, sufficient for this scale |
| HTTP Client | Native `fetch` API | No axios — avoids third-party dependency vulnerabilities |
| Mobile | Android — Kotlin + Jetpack Compose | MVVM, Retrofit, EncryptedSharedPreferences |

---

## Two Applications

### 1. Ascent Control App (`admin.edu-care.in`)
- **Who uses it:** Ascent internal team only (SaaS admin)
- **Purpose:** Onboard school groups, manage subscriptions, toggle modules, manage branding
- **Auth:** Against `ascent_master.control_users`
- **RBAC:** Simple — 3 roles as a column: `super_admin` / `support` / `billing`

### 2. Ascent School App (`{subdomain}.edu-care.in`)
- **Who uses it:** School staff (Principal, Admin Clerk, Fee Clerk, Teachers, etc.)
- **Purpose:** Full school management — students, fees, attendance, marks, library, transport
- **Auth:** Against tenant DB `users` table (after resolving subdomain → tenant DB)
- **RBAC:** Full role/permission system managed per school group

---

## SaaS Architecture: Two-Database Model

### Why two databases
- Schools are legally sensitive about student data → group-level isolation
- One group's load must not affect other groups
- Clean per-group backup / restore / offboarding
- Cross-branch reports within a group remain simple (same DB)

### Database 1 — `ascent_master` (Central / Control Plane)

| # | Table | Description |
|---|---|---|
| 1 | `school_groups` | One row per paying client. Has `subdomain` for tenant routing. `db_username`/`db_password` store per-DB SQL credentials (fastwebhost assigns separate creds per DB). |
| 2 | `schools` | Individual branches under a group |
| 3 | `subscriptions` | SaaS plan, billing cycle, expiry, max_schools, max_students |
| 4 | `master_audit_logs` | SaaS-level events: logins, subscription changes, school creation |
| 5 | `control_users` | Ascent internal staff only |
| 6 | `refresh_tokens` | Hashed refresh tokens for control_users |
| 7 | `school_settings` | App configuration per school branch (1 row per branch) |
| 8 | `modules` | Master list of all system features, seeded with 7 modules |
| 9 | `group_modules` | Module on/off per school group (SaaS admin controls) |
| 10 | `school_modules` | Module on/off per branch (group admin controls, within group limits) |
| 11 | `group_branding` | Default branding for all branches in a group |
| 12 | `school_branding` | Branch-level branding override |

### Database 2 — `ascent_group_{group_id}` (Tenant DB)
One DB per school group. DB name is always `ascent_group_{group_id}` — derived from PK, never stored.
Run `tenant_tables.sql` on onboarding (called automatically by `POST /control/school-groups`).

| # | Table | Description | Source (VB6) |
|---|---|---|---|
| 1 | `permissions` | Granular permission codes (seeded) | *(new)* |
| 2 | `roles` | Staff roles per group/branch (seeded with defaults) | *(new)* |
| 3 | `role_permissions` | Role ↔ permission mapping | *(new)* |
| 4 | `users` | School staff login users | *(new)* |
| 5 | `refresh_tokens` | Hashed refresh tokens for school staff | *(new)* |
| 6 | `user_roles` | User ↔ role ↔ branch assignment | *(new)* |
| 7 | `audit_logs` | Row-level change tracking (FK to users in same DB) | SAS_TransLogData |
| 8 | `academic_years` | Academic year config per school | SAS_AcdYear |
| 9 | `class_groups` | Pre-Primary / Primary / High-School / General | SAS_ClassGrps |
| 10 | `fee_categories` | General / Staff Child / VTPS etc. | SAS_FeeCategory |
| 11 | `classes` | Individual classes/grades | SAS_ClassNames |
| 12 | `sections` | Sections per class (e.g. A, B, C) — school-defined | *(new)* |
| 13 | `fee_types` | Fee types per academic year | SAS_FeeTypes |
| 14 | `terms` | Terms/months per academic year | SAS_TermMonthData |
| 15 | `subjects` | Academic subjects | SAS_Subjects |
| 16 | `payment_modes` | Cash / Cheque / Online | SAS_PaymentMods |
| 17 | `fee_periods` | Monthly fee periods per school/academic year (month_no, year_no, period_label, sequence_no); used when fee_structures.payment_type = 'Monthly' | *(new)* |
| 18 | `fee_structures` | Fee amount per class+category+type+term/period; `payment_type` (Term/Monthly), `fee_period_id` FK → fee_periods, `admission_type` (New/Old/NULL) | SAS_FeeMaster |
| 19 | `buses` | Bus vehicle details | SAS_BussData |
| 20 | `bus_routes` | Bus route definitions | SAS_BusRoutes |
| 21 | `bus_fee_structures` | Bus fee per route+term | SAS_BusMaster |
| 22 | `students` | Student master data; `student_unique_id` (stable cross-year INT, same value across promotions); `section_id` FK → sections; `blocked_reason`, `is_detained`, `detained_reason`, `join_type` columns | SAS_StudentMaster |
| 23 | `fee_receipts` | Payment receipt header (receipt_no, total_amount, status Active/Cancelled, student_unique_id for cross-year reporting) | *(new)* |
| 24 | `fee_receipt_items` | Receipt line items (fee_type_id, term_id, fee_period_id, amount, concession_amount, net_amount) | *(new)* |
| 25 | `gateway_configs` | Payment gateway API keys per school (key_secret never returned in API) | *(new)* |
| 26 | `payment_gateway_orders` | Online payment lifecycle tracking (Pending/Paid/Failed) | *(new)* |
| 27 | `student_mobile_accounts` | Mobile app student accounts (PIN hash) | *(new)* |
| 28 | `student_refresh_tokens` | Mobile student sliding-expiry refresh tokens | *(new)* |
| 29 | `student_attendance` | Daily attendance per student | *(new)* |
| 30 | `exam_types` | Exam type definitions per academic year | *(new)* |
| 31 | `student_marks` | Per-student per-subject marks per exam | *(new)* |
| 32 | `homework` | Homework assignments per class+section (`section_id` nullable FK — NULL means class-wide) | *(new)* |
| 33 | `homework_attachments` | File attachments for homework entries | *(new)* |
| 34 | `announcements` | School/class announcements with optional PDF attachment_url | *(new)* |
| 35 | `school_events` | Events gallery metadata + external media URLs (YouTube/Cloudinary) | *(new)* |
| 36 | `staff` | Employee master per school branch | *(new)* |
| 37 | `staff_attendance` | Daily staff attendance (Present/Absent/Late/HalfDay/OnLeave) | *(new)* |
| 38 | `staff_advances` | Advance/loan records per staff member | *(new)* |
| 39 | `staff_advance_repayments` | Repayments against staff advances | *(new)* |
| 40 | `staff_salary_components` | Salary structure template per staff (Earning/Deduction) | *(new)* |
| 41 | `staff_salaries` | Monthly salary header per staff (Draft/Paid/Cancelled) | *(new)* |
| 42 | `staff_salary_items` | Snapshot of components at salary processing time | *(new)* |
| 43 | `sms_logs` | Audit trail of every SMS sent from the school app | *(new)* |
| 44 | `fee_concessions` | Concession amounts per student per fee type per term/period (school fees) OR per bus route per term (transport); `fee_type_id` nullable — NULL for transport rows; `bus_route_id` nullable FK → bus_routes; three filtered unique indexes: term+fee_type, period+fee_type, transport (bus_route+term); receipt_no format CFR{yr8}{D5} | *(new)* |

> **Migration:** Run `Database/sections_migration.sql` on existing tenant DBs to add the `sections` table and `section_id` column on `students`. Run `Database/fee_tables_migration.sql` for fee receipt tables. Run `Database/fee_periods_migration.sql` for fee_periods table + payment_type/fee_period_id columns. Run `Database/student_unique_id_migration.sql` to add and backfill student_unique_id on students. Run `Database/fee_concessions_migration.sql` for fee_concessions table. New groups get all 44 tables automatically via the updated `tenant_tables.sql`.

---

## Authentication & Tenant Routing

### Subdomain-based tenant identification
Each school group gets a unique subdomain:
```
srividya.edu-care.in      → resolves to group_id → ascent_group_1
ascent-public.edu-care.in → different group      → ascent_group_2
admin.edu-care.in         → control app (master DB only)
```

### JWT Strategy — Two token types

#### Control App tokens (`tokenType=control`)
- Claims: `tokenType=control`, `userId`, `fullName`, `role`
- Generated by `JwtHelper.GenerateControlAccessToken(ControlTokenClaims)`
- Validated by `ControlAuthAttribute` on all control controllers
- Refresh token stored in `ascent_master.refresh_tokens` (cookie name: `controlRefreshToken`)

#### School App tokens (`tokenType=school`)
- Claims: `tokenType=school`, `userId`, `groupId`, `schoolId`, `dbName`, `perm[]`
- Generated by `JwtHelper.GenerateAccessToken(TokenClaims)`
- Validated by global `JwtAuthenticationFilter`, populates `TenantContext.Current`
- `JwtAuthenticationFilter` skips TenantContext for `tokenType=control` tokens
- Refresh token stored in tenant DB `refresh_tokens` (cookie name: `schoolRefreshToken`)

**Access token:** 30 min | **Refresh token:** 7 days, sliding expiry, HttpOnly cookie, SHA-256 hash in DB

### Password hashing
SHA-256 (same as `JwtHelper.HashRefreshToken`) — used for both `control_users.password_hash` and `users.password_hash`.

### School App login flow
```
1. React app loads → GET /branding (public, no auth) → applies school colors/logo
2. Silent refresh attempted on mount → restores session if cookie is valid
3. User submits login form → POST /school/auth/login with X-Subdomain header
4. API resolves subdomain → group_id → ascent_group_{group_id}
5. Validates username + password_hash against tenant.users
6. Determines schoolId (from users.school_id or first from user_roles)
7. Loads permissions for that school
8. Returns: access token (JWT body) + sets HttpOnly schoolRefreshToken cookie
9. React stores schoolId in localStorage (for refresh calls across page reloads)
10. React stores access token in Zustand memory only (XSS protection)
11. All API calls send Authorization: Bearer {access_token} + X-Subdomain header
```

### School App refresh flow
```
POST /school/auth/refresh?schoolId={id}
Headers: X-Subdomain: {subdomain}
Cookie: schoolRefreshToken (HttpOnly)
→ Rotates refresh token, returns new access token
```

---

## RBAC Design (Tenant DB)

### Tables
- `permissions` — codes like `STUDENT_FEE.COLLECT`, format: `MODULE_CODE.ACTION`
- `roles` — named roles, can be group-wide (`school_id NULL`) or branch-specific
- `role_permissions` — many-to-many
- `users` — school staff; `school_id NULL` = access to all branches
- `user_roles` — user ↔ role ↔ branch (different roles in different branches supported)

### Default permissions seeded (24 codes)
| Module | Permission Codes |
|---|---|
| STUDENT_PROFILE | VIEW, CREATE, EDIT, DELETE |
| STUDENT_FEE | VIEW, COLLECT, EDIT, CANCEL_RECEIPT, CONCESSION |
| ATTENDANCE | VIEW, MARK, EDIT |
| MARKS | VIEW, ENTER, EDIT, PUBLISH |
| LIBRARY | VIEW, ISSUE, RETURN, MANAGE |
| TRANSPORT | VIEW, MANAGE |
| HOSTEL | VIEW, MANAGE |

### Default roles seeded
`Principal`, `Admin Clerk`, `Fee Clerk`, `Class Teacher`, `Librarian`

### API permission check (every protected school endpoint)
```
[RequireModule(ModuleCodes.StudentFee)]           ← checks group_modules + school_modules
[RequirePermission(PermissionCodes.StudentFee.Collect)]  ← checks permissions[] in JWT
public HttpResponseMessage CollectFee(...) { }
     ↓
SchoolAuthAttribute       → TenantContext.Current == null → 401
RequireModuleAttribute    → module disabled for group/branch → 403
RequirePermissionAttribute → permission missing in JWT claims → 403
     ↓
Controller executes
```

### Filter execution order
Declare `[RequireModule]` before `[RequirePermission]` — Web API runs attribute filters in declaration
order within the same scope. `SchoolAuthAttribute` is always first (applied at class level on
BaseSchoolController). Module check runs second, permission check last.

### ModuleRepository.IsModuleEnabled(groupId, schoolId, moduleCode)
Single SQL query covering the full three-level hierarchy:
- group row disabled (is_enabled=0) → CAST(0 AS BIT) — hard block
- school row exists                 → use school is_enabled value
- no school row                     → ISNULL(group is_enabled, 1) — inherit, default on
- module code not in modules table  → returns null → treated as false (disabled)

---

## Module Access Control

### Three-level hierarchy
```
modules (system-wide, Ascent manages)
  └── group_modules (per society, SaaS admin controls)
        └── school_modules (per branch, group admin controls)
```

### Rules
- Group disabled → branch **cannot** override (hard block)
- Group enabled → branch can disable for their branch
- No row in `school_modules` → inherits group setting
- `modules.plan_tier` — Basic / Standard / Premium (enforced against subscription)

### Known modules (seeded in master DB)
| Code | Name | Min Plan |
|---|---|---|
| STUDENT_PROFILE | Student Profile | Basic |
| STUDENT_FEE | Student Fee | Basic |
| ATTENDANCE | Student Attendance | Basic |
| MARKS | Student Marks | Standard |
| LIBRARY | Library | Standard |
| TRANSPORT | Transport | Basic |
| HOSTEL | Hostel | Premium |

---

## Branding

### What is configurable per school/group
- Logo (header, login page, receipts/reports)
- Favicon
- Primary color, secondary color, header background, nav text color
- Display name (override school name shown in UI)
- Tagline (shown on login page)
- Receipt/report footer text
- Login page background image

### Hierarchy
```
group_branding   ← default for all branches
  └── school_branding  ← overrides group when set for a specific branch
```

### How React applies it (School App)
1. Public endpoint `GET /branding` called before login (no auth needed)
2. API resolves subdomain → returns most specific branding (branch → group fallback)
3. React applies via Ant Design `ConfigProvider` theme tokens:
   ```jsx
   <ConfigProvider theme={{ token: { colorPrimary: branding.primaryColor } }}>
   ```
4. CSS variables set on `document.documentElement` for non-AntD elements.

---

## Tenant DB Provisioning (on school group onboarding)
Tenant DB is provisioned **manually** — the control app UI only creates the master DB record.

**Control app flow:**
1. `POST /control/school-groups` with `{ groupName, subdomain, description }`
2. API inserts into `school_groups` → gets `group_id`, seeds `group_modules` (all enabled)
3. UI shows success message with the DB name to provision (`ascent_group_{group_id}`)

**Manual DB setup (run in SSMS on SQL Server):**
1. Connect to SQL Server
2. Run `Database/tenant_tables.sql` while connected to `master` — creates `ascent_group_{N}` DB with all 44 tables + seeds permissions and roles
3. Connect to `ascent_group_{N}`
4. Run `Database/seed_tenant_data.sql` — seeds role-permission mappings, payment modes (Cash/Cheque/Online), and default admin login
5. Set `@SchoolId` in `seed_tenant_data.sql` to the `school_id` from `ascent_master.schools` (add a school branch first via the control app)
6. In control app → school group → **Edit Settings** → fill in **DB Name**, **DB Username**, **DB Password** → Save
7. School group is ready to use

---

## Build Order — Status

```
Phase 0  ✅  Project scaffolding + conventions (API solution + React apps)
Phase 1  ✅  Control App (full: onboarding, subscriptions, modules, branding, user management)
Phase 2a ✅  School App — login + JWT + branding load + silent refresh
Phase 2b ✅  School App — RBAC screens (roles/permissions, user management)
Phase 2c ✅  API middleware (module check + permission filter — both fully wired)
Phase 3  ✅  Master data screens (academic years, class groups, classes, sections, fee categories, fee types, terms, subjects, payment modes)
Phase 4a ✅  Student Profile module
Phase 4b ✅  Student Fee module (fee structure setup, fee collection, receipts + cancel)
Phase 4b+ ✅ Payment Gateway integration (Razorpay; pluggable for Cashfree/PayU; webhook support)
Phase 4c ✅  Attendance module (mark attendance + monthly summary; student_attendance table from 6A)
Phase 4d ✅  Marks module (exam types + marks entry grid built in Phase 6C)
Phase 4e ⏸   Library module (deferred — schema not yet designed)
Phase 4f ✅  Transport module (buses, routes, bus fee structure, student assignment)
Phase 5  ✅  Dashboard + cross-module summary (students, attendance, fee trend, recent receipts, upcoming homework)
Phase 6A ✅  Mobile app — DB schema (all new tables)
Phase 6B ✅  Mobile app — Backend APIs (auth + data endpoints)
Phase 6C ✅  Mobile app — Web UI additions (marks entry, homework, announcements)
Phase 6D ✅  Mobile app — Android app (Kotlin + Jetpack Compose)
Phase 7  ✅  Bulk CSV import — students (POST /school/students/bulk) + fee structure (POST /school/fees/structure/bulk); papaparse client-side parsing, preview table, example data, error report download
Phase 8  ✅  Student promotion — multi-year model (one row per student per academic year); PromoteStudentsPage: from year+class+section → preview → to year+class+section (required) → confirm; mobile auth re-points to latest student_id on login
Phase 9  ✅  Android app — parent-only login (student login removed); product flavors for white-label multi-school builds (one APK per school, unique applicationId + SCHOOL_CODE baked in per flavor)
Phase 10 ✅  Android app — SMS OTP auth (replaces PIN login); device binding (one device per parent); teacher staff login via bottom sheet; SmsHelper for smslogin.mobi gateway
Phase 11 ✅  School events gallery — video + image gallery tab in mobile app; media stored externally (YouTube for videos, Cloudinary for images); SQL Server stores metadata + URLs only; school web app has Events Gallery management page
Phase 12 ✅  SMS Center — send Absent / Fee Due / Custom SMS to parents; client-side batching (25/50 per API call) + server-side Task.WhenAll parallel dispatch; smslogin.mobi gateway; sms_logs audit trail; SMS History tab
Phase 13 ✅  Student lifecycle flags — Block (status=Blocked, excluded from attendance+SMS) and Detain (is_detained flag, excluded from promotion, remains Active for daily ops); warn on promote preview with detained count
Phase 14 ✅  Blood Group Search — dedicated page under Students; filters GET /school/students by bloodGroup param; colour-coded blood group tags; PDF + CSV export
Phase 15 ✅  Reports additions — Regular Absentees (threshold-based, date range, class/section filter) + Detained Students report; both with PDF/CSV export
Phase 16 ✅  Per-DB SQL credentials — `db_username`/`db_password` added to `school_groups`; `TenantConnectionFactory` looks them up with 15-min cache, falls back to template; control app Edit Settings modal exposes all three DB fields; cache invalidated on save
Phase 17 ✅  Parent OTP auto-link — `request-otp` gates on student existing in school's DB by `father_mobile` (no pre-existing parent account required); `verify-otp` auto-creates `parent_accounts` + auto-links all matching students from that school; no manual SQL or link-child flow needed
Phase 18 ✅  School-scoped children + child link upsert — `GET /mobile/auth/parent/children` filters by `group_id` (current school's group from X-School-Code) so each white-label app only shows its own school's children; `POST /mobile/auth/parent/select-child` validates link belongs to current school (defense-in-depth); `verify-otp` auto-link loop changed to upsert by `admission_no` — updates `class_name`, `student_name`, `student_id` on existing links rather than skipping them (fixes NULL class_name on old links and handles post-promotion student_id changes)
Phase 19 ✅  Homework section field — `section_id` (nullable FK) added to `homework` table; web UI HomeworkPage shows Section dropdown (loaded by class); mobile homework feed filters by `section_id IS NULL OR section_id = @sectionId` so class-wide entries always show but section-specific entries only reach matching section
Phase 20 ✅  Bulk student import extended — 7 new columns added: `MotherMobile`, `AadharNo`, `Caste`, `CasteCode`, `Religion`, `JoiningClass`, `MotherTongue`; `StudentsImportPage` column reference table + example rows updated; `StudentBulkRow` DTO + `BulkCreate` INSERT extended
Phase 21 ✅  Android login screen logo + app icon fix — school logo (`R.mipmap.ic_launcher_round`) displayed above school name in `SmsAuthScreen`; `AndroidManifest.xml` fixed from `@drawable/ic_launcher` → `@mipmap/ic_launcher` (was loading old vector XML, bypassing Image Asset mipmap files); flavor background XML changed to plain white; `versionCode` bumped to 2
Phase 22 ✅  Android UI polish — custom navy/gold palette in `Theme.kt`; login screen: diagonal gradient bg + frosted glass card + `AsyncImage` logo (Coil); shimmer skeletons on all data screens; attendance: monthly calendar grid + summary card + stat pills; marks: animated bar charts per subject + score badge + staggered entrance; `HomeScreen`: `AnimatedContent` tab transitions + navy `TopAppBar` + navy-tinted `NavigationBar`; `NavigationBar` crash fixed (removed `tonalElevation = Dp.Unspecified`); version string shown on login screen (`BuildConfig.VERSION_NAME`)
Phase 23 ✅  Android release build fix — R8/ProGuard stripping Google Tink (used internally by `EncryptedSharedPreferences`) caused silent crash on splash screen in Play Store release builds; fixed by adding full Tink keep rules to `proguard-rules.pro`; `TokenStore` wrapped in try-catch with plain `SharedPreferences` fallback so any future Tink failure degrades gracefully instead of crashing; `versionCode` bumped to 5, `versionName` = "1.0.5"
Phase 24 ✅  Fee collection redesign — 5 separate collection screens (Admission/School/Transport/Hostel/Other); `student_unique_id` added to `fee_receipts`; new receipt_no format `{prefix}{yr6}{00001}` (T=Transport, H=Hostel, O=Other, classGroupPrefix for Admission/School); `GET /school/fees/student-unique/{uniqueId}?feeTypeCategory=` returns cross-year outstanding summary across all academic years; fee type category filter uses `description` LIKE patterns; Admission screen filters students by `join_type='New'`; checkboxes replace editable Pay Now; concession locked at 0 (configurable in future); `joinType` filter added to `GET /school/students`; `student_unique_id` added to StudentListDto; old `FeeCollectionPage` removed from nav/routes
Phase 25 ✅  Monthly fee periods + fee structure enhancements — `fee_periods` table (month_no, year_no, period_label, sequence_no) added as tenant table 17; `payment_type` (Term/Monthly) and `fee_period_id` FK added to `fee_structures`; `fee_period_id` added to `fee_receipt_items`; `student_unique_id` stable INT identifier added to `students` (auto MAX+1 on insert, same value retained on promotion, backfilled via migration); `FeeStructurePage` supports Term/Monthly toggle (Monthly uses fee_periods as columns, Term uses terms) and Admission Type filter (New/Old/All); `FeePeriodsTab` added to Master Data (GET/POST/PUT `/school/master/fee-periods`); `MasterDataPage` refactored — each tab loads its own data independently; `FeePeriodDto`/`SaveFeePeriodRequest` in `FeePeriodDtos.cs`
Phase 26 ✅  Fee Concession — `fee_concessions` table (school_id, academic_year_id, student_id, student_unique_id, fee_type_id, term_id nullable FK → terms, fee_period_id nullable FK → fee_periods, concession_type, amount, remarks, receipt_no); two filtered unique indexes (one per term_id IS NOT NULL, one per fee_period_id IS NOT NULL) for per-term/per-period concession isolation; receipt_no format `CFR{yr8}{D5}`; `FeeConcessionRepository` upserts by student+fee_type+term/period match; `DELETE /school/fees/concessions/{id}` soft-cancels (status=Cancelled) with Popconfirm in UI; outstanding subquery matches by `ISNULL(fc.term_id,0)=ISNULL(fs.term_id,0)` so each term/period line deducts only its own concession; `ConcessionAmount` bug fixed in `FeeCollectionBase.jsx` (was hardcoded 0.00, now reads `li.concessionAmount`); `FeeConcessionPage` has 3 tabs with Term/Period dropdowns in Individual + Bulk tabs; Concession List has delete column (Popconfirm) + Term/Period column; "Fee Concession" added to Fees nav sub-menu
Phase 27 ✅  Transport enhancements — Student Assignment tab: Year/Class/Section/Route filter bar (sections load per class); no auto-load on mount; `GET /school/transport/students` extended with academicYearId, classId, sectionId params; uses `IN ('Active','Y')` for legacy data; `StudentTransportDto` now includes `StudentUniqueId` (INT) and `AcademicYearId`; transport update route changed from `{studentId:long}` to `{studentUniqueId:int}`; UPDATE WHERE uses `student_unique_id + academic_year_id` (stable cross-year) instead of `student_id` (year-specific IDENTITY)
Phase 28 ✅  Transport fee collection — route-based outstanding (not class-based); `bus_route_id INT NULL` added to `fee_receipt_items` (migration: `transport_receipt_migration.sql`); `GetCrossYearFeeSummary` branches on `feeTypeCategory=Transport`: reads amounts from `bus_fee_structures` (route+term+year), skips years with no `bus_route_id` assigned, paid subquery matches `fri.bus_route_id + fri.term_id + student_id`; `fee_structures` transport rows ignored entirely; `FeeLineItemDto` + `CollectFeeItem` get `BusRouteId?`; `FeeReceiptItemDto` gets `RouteName`; `CollectFee` stores `bus_route_id` in receipt items; `GetReceiptById` JOINs `bus_routes` for receipt display; `FeeCollectionBase.jsx` `buildItems` passes `BusRouteId`; route name shown as FeeTypeName in collection UI
Phase 29 ✅  Transport concession — `bus_route_id INT NULL` added to `fee_concessions` (migration: `transport_concession_migration.sql`); `fee_type_id` made nullable (NULL for transport rows, bus_route_id set instead); existing unique indexes recreated with `fee_type_id IS NOT NULL` filter; new `UQ_fee_concessions_transport` index on `(student_id, bus_route_id, school_id, term_id)`; `FeeConcessionDto` + save requests extended with `BusRouteId?`; controller validation changed from `FeeTypeId <= 0` to `FeeTypeId == null && BusRouteId == null`; `FeeConcessionRepository` all 4 methods handle `bus_route_id`; `GetTransportLineItems` gains concession subquery matching by `bus_route_id`; `StudentRepository.GetAll` + `StudentsController.GetStudents` gain `busRouteId` optional filter; `FeeConcessionPage` gains global Radio.Group "School Fee"|"Transport" toggle — transport mode shows route+term dropdowns, bulk mode loads students by `?busRouteId=`
Phase 30 ✅  Hostel module — `hostels` table (hostel_id, hostel_name, description, capacity, contact_no, address, no_of_rooms, school_id, created_by); `hostel_fee_structures` table (hostel_id + academic_year_id + term_id/fee_period_id + payment_type Term/Monthly + amount); `hostel_id INT NULL FK` added to `students`, `fee_receipt_items`, `fee_concessions`; two filtered unique indexes on `fee_concessions` for hostel concessions (`hostel_id IS NOT NULL AND term_id IS NOT NULL`, `hostel_id IS NOT NULL AND fee_period_id IS NOT NULL`); `students.hostel_name VARCHAR` replaced by `hostel_id INT FK`; `HostelRepository` (CRUD + GetFeeStructure/SaveFeeStructure + BulkSaveFeeStructure + GetStudentHostels/UpdateStudentHostel); `HostelController` at `[RoutePrefix("school/hostel")]` (GET/POST hostels, GET/POST fee-structure, POST fee-structure/bulk, GET/PUT students); `GetCrossYearFeeSummary` branches on `feeTypeCategory=Hostel` → reads from `hostel_fee_structures` via `GetHostelLineItems`; `CollectFee` stores `hostel_id` in `fee_receipt_items`; `GetReceiptById` JOINs `hostels` for HostelName; `FeeConcessionRepository` all 4 methods handle `hostel_id`; `FeeConcessionPage` gains "Hostel" Radio.Button (3rd mode); `HostelPage.jsx` — 4 tabs: Hostels CRUD, Fee Structure (Term/Monthly toggle), Student Assignment (year/class/section/hostel filter bar), Bulk Import CSV; `StudentFormPage.jsx` Other Info tab: hostelName Input → hostelId Select (loaded from `/school/hostel`); `AppLayout.jsx` gains Hostel nav item (HomeOutlined icon) after Transport; migration: `Database/hostel_migration.sql`
Phase 31 ✅  Parent web portal (`ascent-parent-app/`) — gateway-only fee payment for parents; reuses all existing `/mobile/auth/parent/*` endpoints; React/Vite app on port 5174; Ant Design 5 + Zustand; routes: /login (SMS OTP — mobile → OTP → 30s resend), /select-child (list children scoped to school group), /fees (FeeHomePage: 3 tabs School/Transport/Hostel → cross-year outstanding with checkboxes → Razorpay checkout → /fees/success), /fees/success (PaymentSuccessPage: receipt details + print); `MobileFeesController` at `[RoutePrefix("mobile/fees")]` — `GET /mobile/fees/outstanding?feeTypeCategory=` (wraps GetCrossYearFeeSummary), `GET /mobile/fees/gateway-config`, `POST /mobile/fees/payment-orders` (creates Razorpay order, looks up student_unique_id + year-specific student_id + online payment mode id server-side), `POST /mobile/fees/payment-orders/{id}/verify` (verifies HMAC, creates receipt with "Parent Portal" as createdBy); `GatewayRepository.GetOnlinePaymentModeId` — looks up `payment_modes WHERE is_online=1`; `StudentRepository.GetStudentUniqueId` and `GetStudentIdForYear` helper methods; parent app sends `X-School-Code` + `X-Subdomain` headers; `MobileCreateOrderRequest` DTO (AcademicYearId, FeeTypeCategory, Items); items sent with `Amount=li.outstanding, ConcessionAmount=0`; `MobileFeesController.cs` + `MobileCreateOrderRequest` registered in `AscentSchools.API.csproj`; `.env.demo` and `.env.srividya` gitignored
Phase 32 ✅  Legacy receipt bulk import — `POST /school/fees/receipts/bulk` (max 1000 rows); `BulkReceiptRow` + `BulkReceiptImportRequest` DTOs in `BulkImportDtos.cs`; `BulkImportReceipts` in `FeeRepository`: name-based lookups (fee_types, terms, fee_periods, bus_routes, hostels, payment_modes, academic_years), groups rows by LegacyReceiptNo (if provided) else by AdmissionNo+AcademicYear+PaymentDate+PaymentMode, validates per group (student exists, payment mode exists, date parseable), inserts one receipt per group with multiple line items in a transaction (receipt_no atomicity), LegacyReceiptNo stored in remarks as "Legacy: {no}"; `FeeReceiptsImportPage.jsx` — CSV template download, column reference, example rows, papaparse client-side upload, preview + result with error table + download errors CSV; nav item "Legacy Receipt Import" under Fees sub-menu; `GET /school/fees/receipts` gains `createdAfter` DateTime? filter (on `fee_receipts.created_at`) for end-of-day sync; `ReceiptsPage.jsx` gains "Created after" DatePicker (with time) + "Export CSV" button (client-side papaparse CSV from loaded receipts)
Phase 33 ✅  Android fee collection redesign — fee screen updated to match parent web portal (3 category tabs: School/Transport/Hostel; cross-year outstanding grouped by academic year; concession amounts shown per item; selection scoped to one year at a time — switching year clears prior selection); `ApiService.kt` old endpoints (`/mobile/student/fees`, `/mobile/student/fees/order`, `/mobile/student/fees/verify`) replaced with new parent portal endpoints (`GET /mobile/fees/outstanding?feeTypeCategory=`, `POST /mobile/fees/payment-orders`, `POST /mobile/fees/payment-orders/{gatewayOrderId}/verify`); `ApiModels.kt` DTOs updated: `MobileFeeLineItemDto` gains `feePeriodId`, `busRouteId`, `hostelId`, `concessionAmount`; `MobileCreateOrderRequest` removes `paymentModeId` (server resolves online mode), adds `feeTypeCategory`; `MobileFeeOrderItem` gains `feePeriodId`, `busRouteId`, `hostelId`; `MobileOrderResponse` gains `gatewayName`; `FeeRepository.kt` updated: `getOutstanding(feeTypeCategory)` returns `List<MobileFeeSummaryDto>`, `verifyPayment` takes `gatewayOrderId` as explicit param; `FeeViewModel.kt` rewrites state to `FeeUiState.Success(years: List<>)`, adds `selectedCategory` StateFlow, `selectCategory()` clears and reloads; `FeeScreen.kt` rewrites with `TabRow`, year-grouped `LazyColumn`, per-year Select All, sticky pay bar; `HomeScreen.kt` + `MainActivity.kt` callback signature updated: `paymentModeId: Int` → `feeTypeCategory: String`; `GatewayDtos.cs` `MobileCreateOrderRequest` renamed to `MobileParentOrderRequest` to resolve compile-time ambiguity with `Mobile.Data.MobileCreateOrderRequest`; `MobileFeesController` updated to `MobileParentOrderRequest`; `Global.asax.cs` `Access-Control-Allow-Headers` extended with `X-School-Code` (was blocked by CORS preflight for parent web app); parent app `LoginPage.jsx` fixed: `MobileNo` → `Mobile` to match `ParentOtpRequest` DTO field name
Phase 34 ✅  Android persistent session — fix for "token expired, must re-login with OTP after closing app for a day"; root cause: OkHttp's default cookie jar is in-memory so the 7-day `parentRefreshToken` HttpOnly cookie was lost on app kill; `PersistentCookieJar` class added to `RetrofitClient.kt` — serializes all OkHttp cookies to a plain `SharedPreferences` file (`ascent_cookies`) so they survive process death; `RetrofitClient.init()` signature changed from `(TokenStore)` to `(TokenStore, Context)` to create the cookie prefs — `AscentApp.kt` updated accordingly; `AuthRepository.kt` gains `silentRefresh()` (calls `POST /mobile/auth/parent/refresh`, saves new access token) and `silentRefreshTeacher()` (calls `POST /mobile/auth/teacher/refresh`); `MainActivity.kt` adds `isCheckingSession` state (true when stored token exists on cold start) + `LaunchedEffect(Unit)` that calls the appropriate silent refresh, shows `CircularProgressIndicator` while waiting, then clears session and shows login screen if refresh fails
```

---

## API Project Structure (current)
```
AscentSchools.sln
├── AscentSchools.API          ← Web API project (.NET 4.8)
│   ├── App_Start/
│   │   └── WebApiConfig.cs    ← CORS, JSON formatting, global filters, routes
│   ├── Controllers/
│   │   ├── BaseApiController.cs           ← response helpers only (no auth)
│   │   ├── Control/
│   │   │   ├── BaseControlController.cs   ← [ControlAuth], response helpers
│   │   │   ├── ControlAuthController.cs   ← POST /control/auth/login|refresh|logout
│   │   │   ├── SchoolGroupsController.cs  ← CRUD + school CRUD + user management; invalidates TenantConnectionFactory credential cache on group update
│   │   │   ├── SubscriptionsController.cs ← GET/PUT /control/school-groups/{id}/subscription
│   │   │   ├── ModulesController.cs       ← GET modules, GET/PUT group modules
│   │   │   └── BrandingController.cs      ← GET/PUT /control/school-groups/{id}/branding
│   │   └── School/
│   │       ├── BaseSchoolController.cs    ← [SchoolAuth], response helpers, Tenant property
│   │       ├── PublicController.cs        ← GET /branding (AllowAnonymous)
│   │       ├── SchoolAuthController.cs    ← POST /school/auth/login|refresh|logout
│   │       ├── RolesController.cs         ← CRUD roles + GET/PUT permissions per role
│   │       ├── SchoolUsersController.cs   ← CRUD users + reset-password + status
│   │       ├── MasterDataController.cs    ← [RoutePrefix("school/master")] — 10 entities (incl. sections + fee periods), 3 endpoints each
│   │       ├── StudentsController.cs      ← GET list (?bloodGroup, ?joinType, ?busRouteId, ?hostelId filters), GET by id, POST create, PUT update, POST /{id}/photo, POST /bulk, GET /promote/preview, POST /promote, PUT /{id}/block|unblock|detain|release
│   │       ├── FeeController.cs           ← [RoutePrefix("school/fees")] — structure, collect, receipts (GET ?createdAfter for sync), POST structure/bulk, POST receipts/bulk (legacy import), gateway settings, payment orders + webhook
│   │       ├── FeeConcessionController.cs ← [RoutePrefix("school/fees/concessions")] — GET list, POST individual (upsert), POST /bulk, DELETE /{id} (soft-cancel); validation: FeeTypeId==null && BusRouteId==null && HostelId==null → 400
│   │       ├── HostelController.cs        ← [RoutePrefix("school/hostel")] — GET/POST hostels, GET/POST fee-structure, POST fee-structure/bulk, GET/PUT students
│   │       ├── AttendanceController.cs    ← [RoutePrefix("school/attendance")] — mark by date + monthly summary; classId+sectionId both required
│   │       ├── MarksController.cs         ← [RoutePrefix("school/marks")] — exam types + marks grid + save
│   │       ├── HomeworkController.cs      ← [RoutePrefix("school/homework")] — list, create, update, delete
│   │       ├── AnnouncementsController.cs ← [RoutePrefix("school/announcements")] — list, create, update, delete
│   │       ├── ReportsController.cs       ← [RoutePrefix("school/reports")] — class-students, total-strength, absents, attendance-sheet, transport-students, study-certificate, daily-attendance-summary, monthly-attendance-sheet, attendance-register, exam-hall-ticket, toppers, failed-students, homework, staff-salary-statement, detained-students, regular-absentees
│   │       └── SmsController.cs           ← [RoutePrefix("school/sms")] — GET recipients (by type), POST send (parallel dispatch via Task.WhenAll), GET logs
│   │   └── Mobile/
│   │       ├── MobileAuthController.cs        ← student auth (backend-only) + parent request-otp (gates on student in school DB by father_mobile)/verify-otp (auto-creates parent_account + upserts child links by admission_no — updates class_name/student_id on existing links)/refresh/logout/children (scoped to current school group)/select-child (validates link.GroupId == tenant.GroupId)/link-child
│   │       ├── MobileStudentController.cs     ← profile, attendance, marks, homework, announcements (student + parent JWT)
│   │       ├── MobileFeesController.cs        ← [MobileAuth(requireChildContext:true)] GET /mobile/fees/outstanding (cross-year by category), GET /mobile/fees/gateway-config, POST /mobile/fees/payment-orders (accepts MobileParentOrderRequest; looks up student_unique_id + year student_id + online payment mode id server-side), POST /mobile/fees/payment-orders/{id}/verify (HMAC verify + receipt creation)
│   │       ├── MobileTeacherAuthController.cs ← POST /mobile/auth/teacher/login|refresh|logout (tokenType=teacher)
│   │       └── MobileTeacherController.cs     ← [MobileTeacherAuth] GET classes, GET sections, GET/POST attendance, GET/POST homework
│   ├── Filters/
│   │   ├── ControlAuthAttribute.cs        ← validates tokenType=control, optional role check
│   │   ├── SchoolAuthAttribute.cs         ← validates TenantContext populated (school JWT)
│   │   ├── MobileAuthAttribute.cs         ← validates MobileContext populated; requireChildContext flag for data endpoints
│   │   ├── MobileTeacherAuthAttribute.cs  ← validates TeacherContext populated (tokenType=teacher)
│   │   ├── JwtAuthenticationFilter.cs     ← global filter, validates tokens, populates TenantContext/MobileContext/TeacherContext
│   │   ├── RequireModuleAttribute.cs      ← checks group_modules + school_modules hierarchy → 403
│   │   └── RequirePermissionAttribute.cs  ← checks permissions[] in TenantContext → 403
│   ├── Helpers/
│   │   ├── JwtHelper.cs                   ← GenerateAccessToken (tokenType=school), GenerateControlAccessToken, GenerateMobileStudentToken, GenerateMobileParentToken, GenerateMobileTeacherToken
│   │   └── SmsHelper.cs                   ← SendSms (generic gateway call, returns status string) + SendOtp; DLT template ID constants are placeholders — register with smslogin.mobi before go-live
│   ├── Middleware/
│   │   ├── TenantContext.cs               ← per-request state for school users
│   │   ├── MobileContext.cs               ← per-request state for mobile users (student + parent)
│   │   ├── TeacherContext.cs              ← per-request state for teacher mobile users (tokenType=teacher)
│   │   └── ControlContext.cs              ← per-request state for control users
│   └── Global.asax.cs
│
├── AscentSchools.Core         ← models, DTOs, constants
│   ├── Constants/
│   │   ├── ModuleCodes.cs
│   │   └── PermissionCodes.cs
│   ├── DTOs/
│   │   ├── Auth/              ← school app: LoginRequest (username, password, schoolId?), LoginResponse, TokenClaims
│   │   ├── Branding/          ← BrandingResponse
│   │   ├── Control/
│   │   │   ├── Auth/          ← ControlLoginRequest, ControlLoginResponse, ControlTokenClaims, ControlUserRecord
│   │   │   ├── Branding/      ← GroupBrandingDto, UpdateGroupBrandingRequest
│   │   │   ├── Modules/       ← ModuleDto, GroupModuleDto, UpdateGroupModuleRequest
│   │   │   ├── SchoolGroups/  ← SchoolGroupDto (incl. DbUsername, DbPassword), CreateSchoolGroupRequest, UpdateSchoolGroupRequest (incl. DbName, DbUsername, DbPassword)
│   │   │   ├── Schools/       ← SchoolDto, CreateSchoolRequest, UpdateSchoolRequest
│   │   │   ├── Subscriptions/ ← SubscriptionDto, UpdateSubscriptionRequest
│   │   │   └── Users/         ← TenantUserDto, TenantRoleDto, CreateTenantUserRequest, ResetPasswordRequest, SetUserStatusRequest
│   │   └── School/
│   │       ├── Auth/          ← SchoolUserRecord (internal)
│   │       ├── Rbac/          ← SchoolRoleDto, SchoolPermissionDto, CreateSchoolRoleRequest, UpdateSchoolRoleRequest, RolePermissionsRequest
│   │       ├── Master/        ← FeePeriodDtos.cs (FeePeriodDto, SaveFeePeriodRequest)
│   │       ├── Students/      ← StudentDtos.cs (StudentListDto now incl. StudentUniqueId + JoinType) + BulkImportDtos.cs (StudentBulkRow, FeeStructureBulkRow, BulkStudentImportRequest, BulkFeeStructureImportRequest, BulkImportResult, BulkRowError, PromoteStudentsRequest, PromoteStudentsResult, PromotePreviewDto)
│   │       ├── Fee/           ← GatewayDtos.cs (GatewayConfigDto, SaveGatewayConfigRequest, MobileParentOrderRequest [renamed from MobileCreateOrderRequest to avoid ambiguity with Mobile.Data version], CreatePaymentOrderRequest, CreatePaymentOrderResponse, VerifyPaymentRequest); FeeDtos.cs (CollectFeeRequest, CollectFeeItem, FeeLineItemDto, CrossYearFeeSummaryDto etc.)
│   │       └── Transport/     ← TransportDtos.cs (BusDto, SaveBusRequest, BusRouteDto, SaveBusRouteRequest, BusFeeStructureDto, BusFeeTermDto, SaveBusFeeStructureRequest, BusFeeTermEntry, StudentTransportDto (incl. StudentUniqueId + AcademicYearId), UpdateStudentTransportRequest (incl. AcademicYearId))
│   └── Models/
│       └── ApiResponse.cs     ← standard envelope: { success, data, message, errors }
│
└── AscentSchools.Data         ← Dapper repositories, connection factory
    ├── ConnectionFactory/
    │   ├── IConnectionFactory.cs
    │   └── TenantConnectionFactory.cs     ← GetMasterConnection, GetTenantConnection (looks up per-DB credentials from school_groups with 15-min ConcurrentDictionary cache; falls back to TenantDb:ConnectionTemplate), ProvisionTenantDatabase, InvalidateCredentialCache
    └── Repositories/
        ├── Control/
        │   ├── ControlUserRepository.cs           ← GetByUsername, GetById, UpdateLastLogin
        │   ├── MasterRefreshTokenRepository.cs    ← Create, GetByHash, Revoke, RevokeAllForUser
        │   ├── SchoolGroupRepository.cs           ← GetAll, GetById, SubdomainExists, GetGroupIdBySubdomain, Create, Update (SELECT/UPDATE include db_username, db_password)
        │   ├── SchoolRepository.cs                ← GetByGroupId, GetById, Create, Update
        │   ├── SubscriptionRepository.cs          ← GetByGroupId, Upsert
        │   ├── ModuleRepository.cs                ← GetAll, GetGroupModules, UpdateGroupModule, IsModuleEnabled
        │   ├── BrandingRepository.cs              ← GetGroupBranding, UpsertGroupBranding
        │   └── TenantUserRepository.cs            ← GetAll, UsernameExists, GetRoles, Create, ResetPassword, SetStatus
        └── School/
            ├── SchoolAuthRepository.cs            ← GetById, GetByUsername, GetUserSchoolIds, GetUserPermissions, UpdateLastLogin, Create/Get/RevokeRefreshToken
            ├── SchoolBrandingRepository.cs        ← GetBySubdomain (school_branding → group_branding fallback)
            ├── SchoolRbacRepository.cs            ← GetRoles, CreateRole, UpdateRole, GetAllPermissions, GetRolePermissionIds, SetRolePermissions
            ├── MasterDataRepository.cs            ← Get/Create/Update for all 10 master tables (academic_years, class_groups, fee_categories, classes, sections, fee_types, terms, subjects, payment_modes, fee_periods)
            ├── StudentRepository.cs               ← GetAll (filtered, incl. bloodGroup, joinType, busRouteId, hostelId; SELECT includes student_unique_id + join_type + hostel_id), GetById, Create (auto-assigns student_unique_id via MAX+1), Update, UpdatePhotoPath, BulkCreate, GetForPromotion (status IN 'Active','Y', excludes detained), CountDetained, Promote, BlockStudent, UnblockStudent, DetainStudent, ReleaseStudent
            ├── FeeRepository.cs                   ← GetFeeStructure (incl. fee_period_id/payment_type), SaveFeeStructure (scoped by admission_type slice), GetStudentFeeSummary, GetCrossYearFeeSummary (by student_unique_id; transport branch reads bus_fee_structures; hostel branch reads hostel_fee_structures via GetHostelLineItems; outstanding = structure - paid - concession); CollectFee (stores bus_route_id + hostel_id in receipt items), GetReceipts (?createdAfter for end-of-day sync), GetReceiptById (JOINs bus_routes + hostels for display names), CancelReceipt, BulkSaveFeeStructure, BulkImportReceipts (groups by LegacyReceiptNo or AdmissionNo+date+mode; name-based lookups; per-receipt transaction)
            ├── FeeConcessionRepository.cs          ← GetConcessions (JOINs fee_types+bus_routes+hostels+terms+fee_periods; returns BusRouteId/RouteName/HostelId/HostelName), SaveConcession (upsert by student+fee_type+bus_route+hostel+term/period match; INSERT includes bus_route_id + hostel_id), BulkSaveConcessions (same), DeleteConcession (soft-cancel status→Cancelled)
            ├── HostelRepository.cs                 ← GetAll, Create, Update; GetFeeStructure/SaveFeeStructure (Term/Monthly, delete+insert per hostel+year); BulkSaveFeeStructure; GetStudentHostels (filters: academicYearId, classId, sectionId, hostelId), UpdateStudentHostel (WHERE student_unique_id + academic_year_id)
            ├── MarksRepository.cs                 ← GetExamTypes, CreateExamType, UpdateExamType, GetMarksGrid, SaveMarks
            ├── HomeworkRepository.cs              ← GetHomework, CreateHomework, UpdateHomework, DeleteHomework
            ├── AnnouncementsRepository.cs         ← GetAnnouncements, CreateAnnouncement, UpdateAnnouncement, DeleteAnnouncement
            ├── ReportsRepository.cs               ← GetClassStudents, GetTotalStrength, GetAbsents, GetAttendanceSheet, GetTransportStudents, GetStudyCertificate, GetDailyAttendanceSummary, GetMonthlyAttendanceSheet, GetAttendanceRegister, GetExamHallTicket, GetExamToppers, GetClassToppers, GetFailedStudents, GetAcademicYearToppers, GetHomeworkStatement, GetSubjectHomework, GetStaffSalaryStatement, GetDetainedStudents, GetRegularAbsentees
            ├── TransportRepository.cs             ← GetBuses/CreateBus/UpdateBus, GetRoutes/CreateRoute/UpdateRoute, GetBusFeeStructure/SaveBusFeeStructure, GetStudentTransport (filters: routeId, academicYearId, classId, sectionId; SELECT includes student_unique_id, academic_year_id; IN ('Active','Y')), UpdateStudentTransport (WHERE student_unique_id + academic_year_id — stable cross-year, not student_id)
            ├── HostelRepository.cs                ← GetAll/Create/Update hostels; GetFeeStructure/SaveFeeStructure (Term/Monthly, delete+insert per hostel+year); BulkSaveFeeStructure; GetStudentHostels (filters: academicYearId, classId, sectionId, hostelId; IN ('Active','Y')), UpdateStudentHostel (WHERE student_unique_id + academic_year_id)
            └── SmsRepository.cs                   ← GetAbsentRecipients, GetFeeDueRecipients (OUTER APPLY for outstanding), GetCustomRecipients, LogSms, GetLogs
        └── Mobile/
            ├── MobileStudentAuthRepository.cs     ← student_mobile_accounts + student_refresh_tokens CRUD; GetStudentByAdmissionNo uses ORDER BY academic_year_id DESC; GetAccountByAdmissionNo fallback for post-promotion login; UpdateAccountStudentId re-points account after promotion; GetStudentsByParentMobile (looks up active students by father_mobile, latest year per student)
            ├── MobileParentAuthRepository.cs      ← parent_accounts + parent_children + parent_refresh_tokens + parent_otp (master DB); CreateOtp, GetValidOtp, MarkOtpUsed, GetRecentOtpCount, BindDevice; GetOrCreateParentByMobile (auto-creates OTP-only account on first login); GetChildLinkByAdmissionNo (lookup by admission_no+group_id for upsert); UpdateChildLink (refreshes student_id/student_name/class_name on existing link); GetChildren now filtered by group_id
            └── MobileDataRepository.cs            ← GetStudentProfile, GetAttendance, GetMarks, GetHomework, GetAnnouncements
```

## React Project Structure (current)
```
ascent-control-app/src/
├── api/
│   └── axiosInstance.js       ← fetch wrapper: Bearer header, silent refresh on 401, same api.get/post/put/delete interface
├── store/
│   └── authStore.js           ← zustand: user, accessToken (sessionStorage), login/logout/hasRole
├── layouts/
│   └── MainLayout.jsx         ← Sider nav + Header (user menu + logout)
├── pages/
│   ├── auth/
│   │   └── LoginPage.jsx
│   └── school-groups/
│       ├── SchoolGroupsPage.jsx        ← list + create modal (triggers DB provisioning)
│       ├── SchoolGroupDetailPage.jsx   ← breadcrumb + 5-tab detail view + "Edit Settings" modal (GroupName, Description, Status, DbName, DbUsername, DbPassword)
│       └── tabs/
│           ├── SchoolsTab.jsx          ← branch list + create/edit modal
│           ├── UsersTab.jsx            ← tenant user list + create/reset-password/status
│           ├── SubscriptionTab.jsx     ← plan/dates/limits form
│           ├── ModulesTab.jsx          ← enable/disable modules (super_admin only)
│           └── BrandingTab.jsx         ← colors, logos, tagline form
└── App.jsx                    ← routes: /login, /school-groups, /school-groups/:id

ascent-school-app/src/
├── api/
│   └── axiosInstance.js       ← fetch wrapper: Bearer header, X-Subdomain header, silent refresh on 401
├── store/
│   ├── authStore.js           ← zustand: accessToken (memory only), user {userId,groupId,schoolId,fullName,permissions[]}
│   └── brandingStore.js       ← zustand: branding defaults, isLoaded, setBranding
├── layouts/
│   └── AppLayout.jsx          ← sticky header + collapsible sidebar nav + Outlet; Students sub-menu: List / Bulk Import / Promote Students / Blood Group Search; Fees sub-menu: Fee Structure / Bulk Import Structure / Admission Fee / School Fee / Transport Fee / Hostel Fee / Other Fee / Receipts / Fee Concession; Hostel nav item (HomeOutlined, after Transport); SMS Center nav item
├── pages/
│   ├── auth/
│   │   └── LoginPage.jsx      ← branding-aware login (logo, tagline, loginBgPath)
│   ├── rbac/
│   │   ├── RolesPage.jsx      ← role list + create/edit modal + permission drawer (grouped by module)
│   │   └── UsersPage.jsx      ← user list + create modal + reset-password + activate/deactivate
│   ├── students/
│   │   ├── StudentsPage.jsx       ← searchable/filterable list; status includes Blocked; Block/Unblock (with reason modal) + Detain/Release actions; detained badge shown in status column
│   │   ├── StudentFormPage.jsx    ← full-page 7-tab form (Basic, Academic, Family, Address, Identity, Transport, Other) + photo upload; section is a dropdown loaded by classId; Other tab: hostelId Select loaded from /school/hostel (replaces free-text hostelName); all tabs use `forceRender: true` to prevent AntD lazy-tab field wiping
│   │   ├── StudentsImportPage.jsx ← CSV bulk import; column reference table + example rows + template download + PapaParse preview + result with error download; POST /school/students/bulk; 20 columns including MotherMobile, AadharNo, Caste, CasteCode, Religion, JoiningClass, MotherTongue
│   │   ├── PromoteStudentsPage.jsx ← Step 1: from year+class+section → preview eligible students (status IN Active/Y, not detained) + detained count warning; Step 2: to year+class+section → confirm modal → result stats
│   │   └── BloodGroupSearchPage.jsx ← blood group dropdown (A+/A-/B+/B-/AB+/AB-/O+/O-) → table with colour-coded tags; PDF + CSV export
│   ├── fee/
│   │   ├── FeeStructurePage.jsx       ← matrix grid (fee types × terms or fee_periods) with InputNumber cells; Term/Monthly toggle; Admission Type filter (New/Old/All); load/save per class+category+year+admissionType
│   │   ├── FeeStructureImportPage.jsx ← CSV bulk import for fee structure; upserts per Class/Category/FeeType/Term row; POST /school/fees/structure/bulk
│   │   ├── FeeReceiptsImportPage.jsx  ← CSV bulk import for legacy receipts; 14-column format; groups rows by LegacyReceiptNo or AdmissionNo+Date+Mode; POST /school/fees/receipts/bulk
│   │   ├── FeeCollectionBase.jsx      ← shared fee collection component; student search by unique_id; loads cross-year summary (CrossYearFeeSummaryDto); year tabs; checkbox selection; online/offline payment
│   │   ├── AdmissionFeePage.jsx       ← feeTypeCategory=Admission; joinTypeFilter='New' (student search restricted to join_type=New)
│   │   ├── SchoolFeePage.jsx          ← feeTypeCategory=School
│   │   ├── TransportFeePage.jsx       ← feeTypeCategory=Transport
│   │   ├── HostelFeePage.jsx          ← feeTypeCategory=Hostel
│   │   ├── OtherFeePage.jsx           ← feeTypeCategory=Other
│   │   ├── ReceiptsPage.jsx           ← filterable receipts list + detail Drawer + cancel Modal; "Created after" DatePicker (for end-of-day sync) + "Export CSV" button (client-side papaparse)
│   │   └── FeeConcessionPage.jsx      ← 3-tab page; global Radio.Group toggle: "School Fee" | "Transport" | "Hostel"; Individual Entry (student search → school: fee type+term/period; transport: route+term; hostel: hostel+term/period → type+amount+remarks → save); Bulk Apply (school: fee type+term/period+load by class; transport: route+term+load by busRouteId; hostel: hostel+term/period+load by hostelId → editable amount/remarks per row + apply-to-all → save); Concession List (load by year+class+section; "Fee Type / Route / Hostel" column shows feeTypeName, routeName, or hostelName; Print Selected → multi-page jsPDF with amountToWords)
│   ├── master/
│   │   ├── MasterDataPage.jsx ← Tabs container; each tab loads its own data independently (no shared prop-drilling)
│   │   ├── AcademicYearsTab.jsx
│   │   ├── ClassGroupsTab.jsx
│   │   ├── FeeCategoriesTab.jsx
│   │   ├── ClassesTab.jsx
│   │   ├── SectionsTab.jsx    ← class dropdown → section list per class → create/edit modal
│   │   ├── FeeTypesTab.jsx
│   │   ├── TermsTab.jsx
│   │   ├── SubjectsTab.jsx
│   │   ├── PaymentModesTab.jsx
│   │   └── FeePeriodsTab.jsx  ← academic year dropdown → fee periods list → create/edit modal (month, year, label, sequence)
│   ├── sms/
│   │   └── SMSPage.jsx        ← two tabs: Send SMS (type selector → filters → load recipients → select rows → batch size 25/50 → message preview → send with progress) + SMS History (filterable log)
│   └── reports/
│       ├── ReportsPage.jsx    ← sidebar nav → 20 reports rendered in main card
│       ├── [existing reports...]
│       ├── DetainedStudentsReport.jsx ← year/class/section filters (all optional); loads on mount; volcano tags for reason; PDF+CSV
│       └── RegularAbsenteesReport.jsx ← date range + min days InputNumber (default 3) + class/section; absent days colour-coded tag; scroll={{x:'max-content'}}; PDF+CSV
├── marks/
│   └── MarksEntryPage.jsx     ← select year+exam+class+section (all required) → student×subject grid with InputNumber + absent checkbox
├── homework/
│   └── HomeworkPage.jsx       ← list with class filter + create/edit modal; Section dropdown (loaded by class, optional); section column in table
├── announcements/
│   └── AnnouncementsPage.jsx  ← list (pinned first) + create/edit modal (scope School/Class)
├── transport/
│   └── TransportPage.jsx      ← 4-tab page: Buses (create/edit), Routes (create/edit), Bus Fee Structure (route+year → term amounts), Student Assignment (Year/Class/Section/Route filter bar → load → edit modal; PUT uses studentUniqueId; body includes academicYearId)
├── hostel/
│   └── HostelPage.jsx         ← 4-tab page: Hostels CRUD (hostelName, description, capacity, noOfRooms, contactNo, address); Fee Structure (hostel+year+paymentType Term/Monthly → term/period amount columns → save); Student Assignment (Year/Class/Section/Hostel filter bar → load → edit modal with hostel dropdown; PUT uses studentUniqueId + academicYearId); Bulk Import CSV (hostel fee structure rows: HoslelId/AcademicYearId/TermId/Amount → POST /school/hostel/fee-structure/bulk)
└── App.jsx                    ← branding load, silent refresh on mount, routes: /login, /master, /students, /students/import, /students/promote, /students/blood-group, /fees/structure, /fees/structure/import, /fees/collect/admission, /fees/collect/school, /fees/collect/transport, /fees/collect/hostel, /fees/collect/other, /fees/receipts, /fees/receipts/import, /fees/concessions, /hostel, /marks, /homework, /announcements, /sms, /reports, /settings/*

ascent-parent-app/src/
├── api/
│   └── axiosInstance.js     ← sends X-School-Code + X-Subdomain headers; silent refresh via /mobile/auth/parent/refresh; same api.get/post interface
├── store/
│   ├── authStore.js         ← accessToken (memory only), parent {parentId, displayName}, child {studentId, studentName, className, admissionNo}; login/setChild/logout/setToken
│   └── brandingStore.js     ← same pattern as school app
├── layouts/
│   └── AppLayout.jsx        ← header shows child name + class Tag + Switch Child / Logout dropdown
├── pages/
│   ├── auth/
│   │   ├── LoginPage.jsx        ← Step 0: 10-digit mobile → OTP; Step 1: AntD Input.OTP 6-digit + 30s resend countdown
│   │   └── ChildSelectorPage.jsx← loads /mobile/auth/parent/children, POST select-child, navigate to /fees
│   └── fees/
│       ├── FeeHomePage.jsx      ← 3 tabs (School/Transport/Hostel); each tab independent with own useEffect; FeeTab + YearSection components; Razorpay checkout; loadRazorpayScript lazy loader
│       └── PaymentSuccessPage.jsx← receipt Descriptions + items Table + print button (@media print CSS)
└── App.jsx                  ← RequireAuth + RequireChild guards; silent refresh on mount; routes: /login, /select-child, /fees, /fees/success
```

---

## School App API Endpoints (Phase 2a + 2b + 3)

| Method | URL | Auth | Notes |
|---|---|---|---|
| GET  | `/branding` | Public | X-Subdomain header + optional ?schoolId |
| POST | `/school/auth/login` | Public | X-Subdomain header, returns token + cookie |
| POST | `/school/auth/refresh` | Cookie | X-Subdomain + ?schoolId, rotates cookie |
| POST | `/school/auth/logout` | Cookie | Revokes refresh token |
| GET  | `/school/roles` | School JWT | List roles with permission count |
| POST | `/school/roles` | School JWT | Create role |
| PUT  | `/school/roles/{id}` | School JWT | Update role name/description |
| GET  | `/school/roles/permissions` | School JWT | All available permissions |
| GET  | `/school/roles/{id}/permissions` | School JWT | Permission IDs for a role |
| PUT  | `/school/roles/{id}/permissions` | School JWT | Replace full permission set (atomic) |
| GET  | `/school/users` | School JWT | List all users in group |
| POST | `/school/users` | School JWT | Create user + assign role to branches |
| GET  | `/school/users/roles` | School JWT | Roles list (for create-user dropdown) |
| GET  | `/school/users/schools` | School JWT | Branches list (for create-user dropdown) |
| PUT  | `/school/users/{id}/reset-password` | School JWT | Reset password + revoke refresh tokens |
| PUT  | `/school/users/{id}/status` | School JWT | Activate / Deactivate |
| GET  | `/school/master/academic-years` | School JWT | List academic years |
| POST | `/school/master/academic-years` | School JWT | Create |
| PUT  | `/school/master/academic-years/{id}` | School JWT | Update |
| GET  | `/school/master/class-groups` | School JWT | List class groups |
| POST | `/school/master/class-groups` | School JWT | Create |
| PUT  | `/school/master/class-groups/{id}` | School JWT | Update |
| GET  | `/school/master/fee-categories` | School JWT | List fee categories |
| POST | `/school/master/fee-categories` | School JWT | Create |
| PUT  | `/school/master/fee-categories/{id}` | School JWT | Update |
| GET  | `/school/master/classes` | School JWT | List classes (with class group name via JOIN) |
| POST | `/school/master/classes` | School JWT | Create |
| PUT  | `/school/master/classes/{id}` | School JWT | Update |
| GET  | `/school/master/sections` | School JWT | List sections for a class (?classId required) |
| POST | `/school/master/sections` | School JWT | Create section |
| PUT  | `/school/master/sections/{id}` | School JWT | Update section |
| GET  | `/school/master/fee-types` | School JWT | List fee types |
| POST | `/school/master/fee-types` | School JWT | Create |
| PUT  | `/school/master/fee-types/{id}` | School JWT | Update |
| GET  | `/school/master/terms` | School JWT | List terms |
| POST | `/school/master/terms` | School JWT | Create |
| PUT  | `/school/master/terms/{id}` | School JWT | Update |
| GET  | `/school/master/subjects` | School JWT | List subjects |
| POST | `/school/master/subjects` | School JWT | Create |
| PUT  | `/school/master/subjects/{id}` | School JWT | Update |
| GET  | `/school/master/payment-modes` | School JWT | List payment modes |
| POST | `/school/master/payment-modes` | School JWT | Create |
| PUT  | `/school/master/payment-modes/{id}` | School JWT | Update |
| GET  | `/school/master/fee-periods` | School JWT | List fee periods (?academicYearId optional) |
| POST | `/school/master/fee-periods` | School JWT | Create fee period |
| PUT  | `/school/master/fee-periods/{id}` | School JWT | Update fee period |
| GET  | `/school/students` | School JWT | List students (search, classId, sectionId, academicYearId, status, bloodGroup, joinType) |
| GET  | `/school/students/{id}` | School JWT | Full student detail |
| POST | `/school/students` | School JWT | Create student |
| PUT  | `/school/students/{id}` | School JWT | Update student |
| POST | `/school/students/{id}/photo` | School JWT | Upload student photo (multipart) |
| POST | `/school/students/bulk` | School JWT | Bulk create students from CSV rows (max 500); duplicate admissionNo returns error |
| GET  | `/school/students/promote/preview` | School JWT | Preview eligible students + detainedCount (fromYearId, fromClassId, fromSectionId optional) |
| POST | `/school/students/promote` | School JWT | Promote students — INSERT new rows for target year+class+section; skips already-promoted |
| PUT  | `/school/students/{id}/block` | School JWT | Set status=Blocked with mandatory reason |
| PUT  | `/school/students/{id}/unblock` | School JWT | Set status=Active, clear blocked_reason |
| PUT  | `/school/students/{id}/detain` | School JWT | Set is_detained=1 with mandatory reason |
| PUT  | `/school/students/{id}/release` | School JWT | Set is_detained=0, clear detained_reason |
| GET  | `/school/fees/structure` | School JWT | Get fee structure (classId, feeCategoryId, academicYearId, admissionType optional) |
| POST | `/school/fees/structure` | School JWT | Save fee structure (delete+insert scoped by admissionType slice; supports payment_type=Term|Monthly and fee_period_id) |
| POST | `/school/fees/structure/bulk` | School JWT | Bulk upsert fee structure rows from CSV (max 500); each row = one Class+Category+FeeType+Term |
| GET  | `/school/fees/student/{studentId}` | School JWT | Student fee summary with outstanding dues |
| POST | `/school/fees/collect` | School JWT | Collect fee + generate receipt |
| GET  | `/school/fees/receipts` | School JWT | List receipts (search, dateFrom, dateTo, status) |
| GET  | `/school/fees/receipts/{id}` | School JWT | Full receipt detail with line items |
| PUT  | `/school/fees/receipts/{id}/cancel` | School JWT | Cancel receipt (requires CancelReason) |
| POST | `/school/fees/receipts/bulk` | School JWT | Bulk import legacy receipts from CSV rows (max 1000); groups by LegacyReceiptNo or AdmissionNo+Date+Mode |
| GET    | `/school/fees/concessions` | School JWT | List concessions (?academicYearId, ?classId, ?sectionId) |
| POST   | `/school/fees/concessions` | School JWT | Save/upsert individual concession (one active record per student+fee_type+term/period) |
| POST   | `/school/fees/concessions/bulk` | School JWT | Bulk save concessions for a class — one row per student |
| DELETE | `/school/fees/concessions/{id}` | School JWT | Soft-cancel concession (sets status=Cancelled; outstanding recalculates on next load) |
| GET  | `/school/fees/gateway-config` | School JWT | Active gateway key_id (no secret) for checkout |
| GET  | `/school/fees/gateway-settings` | School JWT | Admin settings view (key_id + hasWebhookSecret flag) |
| PUT  | `/school/fees/gateway-settings` | School JWT | Save/update gateway API keys |
| POST | `/school/fees/payment-orders` | School JWT | Create Razorpay order, returns externalOrderId + keyId |
| POST | `/school/fees/payment-orders/{id}/verify` | School JWT | Verify HMAC signature, create receipt |
| POST | `/school/fees/payment-webhook` | Public (AllowAnonymous) | Razorpay server webhook — payment.captured event |
| GET  | `/school/marks/exam-types` | School JWT | List exam types (?academicYearId optional) |
| POST | `/school/marks/exam-types` | School JWT | Create exam type |
| PUT  | `/school/marks/exam-types/{id}` | School JWT | Update exam type |
| GET  | `/school/attendance` | School JWT | Attendance grid (classId, sectionId, date — all required) |
| POST | `/school/attendance` | School JWT | Save/upsert attendance entries |
| GET  | `/school/attendance/summary` | School JWT | Monthly summary (classId, sectionId, month, year — all required) |
| GET  | `/school/marks` | School JWT | Get marks grid (classId, sectionId, examTypeId, academicYearId — all required) |
| POST | `/school/marks` | School JWT | Save/upsert marks batch |
| GET  | `/school/homework` | School JWT | List homework (?classId optional) |
| POST | `/school/homework` | School JWT | Create homework |
| PUT  | `/school/homework/{id}` | School JWT | Update homework |
| DELETE | `/school/homework/{id}` | School JWT | Soft-delete homework (status=Cancelled) |
| GET  | `/school/announcements` | School JWT | List announcements (?classId optional) |
| POST | `/school/announcements` | School JWT | Create announcement |
| PUT  | `/school/announcements/{id}` | School JWT | Update announcement |
| DELETE | `/school/announcements/{id}` | School JWT | Soft-delete announcement (status=Inactive) |
| GET    | `/school/events` | School JWT | List active events (?classId optional); ordered pinned-first then date DESC |
| POST   | `/school/events` | School JWT | Create event (title, eventDate, mediaType, mediaUrl required) |
| PUT    | `/school/events/{id}` | School JWT | Update event |
| DELETE | `/school/events/{id}` | School JWT | Soft-delete event (status=Inactive) |
| GET    | `/school/reports/detained-students` | School JWT | Detained students (academicYearId, classId, sectionId — all optional) |
| GET    | `/school/reports/regular-absentees` | School JWT | Students absent ≥ minDays within dateFrom–dateTo; optional classId/sectionId |
| GET    | `/school/sms/recipients` | School JWT | Load SMS recipients by smsType (Absent/FeeDue/Custom) with filters |
| POST   | `/school/sms/send` | School JWT | Send SMS batch; parallel dispatch via Task.WhenAll; logs results to sms_logs |
| GET    | `/school/sms/logs` | School JWT | SMS history (smsType, date filters) |
| GET  | `/school/transport/buses` | School JWT | List buses |
| POST | `/school/transport/buses` | School JWT | Create bus |
| PUT  | `/school/transport/buses/{id}` | School JWT | Update bus |
| GET  | `/school/transport/routes` | School JWT | List routes |
| POST | `/school/transport/routes` | School JWT | Create route |
| PUT  | `/school/transport/routes/{id}` | School JWT | Update route |
| GET  | `/school/transport/fee-structure` | School JWT | Get bus fee structure (?routeId, ?academicYearId required) |
| POST | `/school/transport/fee-structure` | School JWT | Save bus fee structure (delete+insert per route+year) |
| GET  | `/school/transport/students` | School JWT | List students with transport (?routeId, ?academicYearId, ?classId, ?sectionId optional) |
| PUT  | `/school/transport/students/{studentUniqueId}` | School JWT | Update student transport (body includes academicYearId; WHERE uses student_unique_id + academic_year_id) |
| GET  | `/school/hostel` | School JWT | List hostels for the school |
| POST | `/school/hostel` | School JWT | Create hostel |
| PUT  | `/school/hostel/{id}` | School JWT | Update hostel |
| GET  | `/school/hostel/fee-structure` | School JWT | Get hostel fee structure (?hostelId, ?academicYearId required) |
| POST | `/school/hostel/fee-structure` | School JWT | Save hostel fee structure (delete+insert per hostel+year; supports Term/Monthly) |
| POST | `/school/hostel/fee-structure/bulk` | School JWT | Bulk upsert hostel fee structure from CSV rows |
| GET  | `/school/hostel/students` | School JWT | List students with hostel assignment (?academicYearId, ?classId, ?sectionId, ?hostelId optional) |
| PUT  | `/school/hostel/students/{studentUniqueId}` | School JWT | Update student hostel assignment (body includes academicYearId + hostelId; WHERE uses student_unique_id + academic_year_id) |

## Mobile App API Endpoints (Phase 6B + 10)

> **Note:** Android app uses SMS OTP for parents (Phase 10) and teacher credentials for staff. Student auth endpoints exist on the backend only.

| Method | URL | Auth | Notes |
|---|---|---|---|
| POST | `/mobile/auth/student/register` | Public | *(backend only — not used by Android app)* |
| POST | `/mobile/auth/student/login` | Public | *(backend only — not used by Android app)* |
| POST | `/mobile/auth/student/refresh` | Cookie | *(backend only — not used by Android app)* |
| POST | `/mobile/auth/student/logout` | Cookie | *(backend only — not used by Android app)* |
| POST | `/mobile/auth/student/request-otp` | Public | *(backend only — not used by Android app)* |
| POST | `/mobile/auth/student/reset-pin` | Public | *(backend only — not used by Android app)* |
| POST | `/mobile/auth/parent/request-otp` | Public | Resolves school from X-School-Code; gates on student existing in school DB by father_mobile; rate-limits (5/10 min); sends OTP; always returns generic message |
| POST | `/mobile/auth/parent/verify-otp` | Public | Validates OTP; auto-creates parent_account if first login; auto-links all matching students from school; binds deviceId; revokes old tokens on device change; issues JWT + cookie |
| POST | `/mobile/auth/parent/register` | Public | Creates parent_accounts in master DB (legacy, used by support) |
| POST | `/mobile/auth/parent/login` | Public | PIN-based login (legacy, kept for backward compat) |
| POST | `/mobile/auth/parent/refresh` | Cookie | Rotates parentRefreshToken cookie |
| POST | `/mobile/auth/parent/logout` | Cookie | Revokes refresh token |
| GET  | `/mobile/auth/parent/children` | parent JWT | List linked children scoped to current school's group (X-School-Code) |
| POST | `/mobile/auth/parent/select-child` | parent JWT | Re-issues parent JWT with selected child's context; validates link belongs to current school group |
| POST | `/mobile/auth/parent/link-child` | parent JWT | Adds new child link (admission_no + school_code) |
| POST | `/mobile/auth/teacher/login` | Public | Validates teacher against tenant users table; issues tokenType=teacher JWT + cookie |
| POST | `/mobile/auth/teacher/refresh` | Cookie | Rotates teacherRefreshToken cookie |
| POST | `/mobile/auth/teacher/logout` | Cookie | Revokes refresh token |
| GET  | `/mobile/teacher/classes` | teacher JWT | List active classes for school |
| GET  | `/mobile/teacher/sections?classId=` | teacher JWT | List sections for a class |
| GET  | `/mobile/teacher/attendance?classId=&sectionId=&date=` | teacher JWT | Attendance grid for date |
| POST | `/mobile/teacher/attendance` | teacher JWT | Save/upsert attendance entries (MERGE) |
| GET  | `/mobile/teacher/homework?classId=` | teacher JWT | List homework for class |
| POST | `/mobile/teacher/homework` | teacher JWT | Create homework entry |
| GET  | `/mobile/student/profile` | student/parent JWT | Full student profile |
| GET  | `/mobile/student/attendance?month=&year=` | student/parent JWT | Monthly attendance summary + records |
| GET  | `/mobile/student/marks?academicYearId=` | student/parent JWT | Marks grouped by exam type |
| GET  | `/mobile/student/homework` | student/parent JWT | Recent homework for student's class |
| GET  | `/mobile/student/announcements` | student/parent JWT | School + class announcements (pinned first) |
| GET  | `/mobile/student/events` | student/parent JWT | School + class events (pinned first, most recent 50) |
| GET  | `/mobile/fees/outstanding` | parent JWT (child ctx) | Cross-year outstanding summary by feeTypeCategory for selected child |
| GET  | `/mobile/fees/gateway-config` | parent JWT (child ctx) | Active gateway key_id for Razorpay checkout |
| POST | `/mobile/fees/payment-orders` | parent JWT (child ctx) | Create Razorpay order; server resolves student_unique_id + year student_id + online payment_mode_id |
| POST | `/mobile/fees/payment-orders/{id}/verify` | parent JWT (child ctx) | Verify Razorpay HMAC signature, create receipt |

---

## Control App API Endpoints (Phase 1)

| Method | URL | Auth | Notes |
|---|---|---|---|
| POST | `/control/auth/login` | Public | Returns access token + sets HttpOnly cookie |
| POST | `/control/auth/refresh` | Cookie | Rotates refresh token (sliding expiry) |
| POST | `/control/auth/logout` | Cookie | Revokes refresh token |
| GET | `/control/school-groups` | Any control role | List all groups |
| POST | `/control/school-groups` | Any control role | Create group + provision tenant DB |
| GET | `/control/school-groups/{id}` | Any control role | Get single group |
| PUT | `/control/school-groups/{id}` | Any control role | Update name/description/status/dbName/dbUsername/dbPassword; invalidates credential cache |
| GET | `/control/school-groups/{id}/schools` | Any control role | List branches |
| POST | `/control/school-groups/{id}/schools` | Any control role | Add branch |
| PUT | `/control/school-groups/{id}/schools/{schoolId}` | Any control role | Update branch |
| GET | `/control/school-groups/{id}/users` | Any control role | List tenant users |
| POST | `/control/school-groups/{id}/users` | Any control role | Create initial user |
| PUT | `/control/school-groups/{id}/users/{uid}/reset-password` | Any control role | Reset password |
| PUT | `/control/school-groups/{id}/users/{uid}/status` | Any control role | Activate/Deactivate |
| GET | `/control/school-groups/{id}/roles` | Any control role | Tenant roles list |
| GET | `/control/school-groups/{id}/subscription` | Any control role | Get subscription |
| PUT | `/control/school-groups/{id}/subscription` | Any control role | Upsert subscription |
| GET | `/control/modules` | Any control role | Master module list |
| GET | `/control/school-groups/{id}/modules` | Any control role | Group module status |
| PUT | `/control/school-groups/{id}/modules/{moduleId}` | `super_admin` only | Toggle module |
| GET | `/control/school-groups/{id}/branding` | Any control role | Get branding |
| PUT | `/control/school-groups/{id}/branding` | Any control role | Upsert branding |

---

## Database Naming Conventions
- Tables: `snake_case`, plural
- Columns: `snake_case`
- PKs: `INT IDENTITY(1,1)` on all tables except `students` (BIGINT) and `school_settings` (school_id as PK)
- FKs: `FK_<table>_<referenced_table>`
- `school_id` on every tenant table — filters by branch within group
- `group_id` NOT in tenant tables — implied by the DB itself

## Key Design Decisions
1. **StuID FLOAT → BIGINT IDENTITY** — float is never valid as a PK
2. **AcademicYear string → academic_year_id FK** — normalized across all tables
3. **All VARCHAR IDs → INT IDENTITY** — performance + referential integrity
4. **MONEY → DECIMAL(12,2)** — portable across DB engines
5. **Signature image → file path string** — no binary blobs in DB
6. **Subdomain login** — chosen over username dropdown for tenant resolution
7. **Nullable columns retained** — `fee_type_name`, `trip_data`, `cleaner_name`, `owner_data`, `route_name`, `bus_no_data`, `bus_name`, `category_name` — kept in schema, UI sends null
8. **Access token in sessionStorage (control) / memory only (school)** — prevents XSS token theft
9. **Tenant DB name stored in `school_groups.db_name`** — set manually after provisioning the tenant DB. Login returns 503 if `db_name` is NULL. `master_db_migration.sql` adds the column and auto-fills existing groups with `ascent_group_{group_id}`.
10. **`Microsoft.AspNet.WebApi.Client` version 6.0.0** — 5.3.0 does not exist on NuGet; 6.0.0 is the correct package for `System.Net.Http.Formatting.dll`
11. **No axios** — both React apps use native `fetch` API with a thin wrapper to avoid third-party dependency vulnerabilities. The wrapper exposes `api.get/post/put/delete` and maintains the same `{ data }` response shape.
12. **schoolId in localStorage** — not sensitive (just an int); stored so the refresh endpoint can re-issue the correct school-scoped token after page reload.
13. **`tokenType` claim in JWT** — `tokenType=control` (control tokens), `tokenType=school` (school web app tokens), `tokenType=student`/`parent` (mobile student/parent), `tokenType=teacher` (mobile teacher). `JwtAuthenticationFilter` routes each type to its own context (TenantContext / MobileContext / TeacherContext / ControlContext).
14. **`fullName` claim in school JWT** — added to `TokenClaims`, `JwtHelper`, `TenantContext`, `JwtAuthenticationFilter`, and `SchoolAuthController.BuildAuthResponse`. Required by `FeeController` to store `created_by` / `cancelled_by` on receipts.
15. **Receipt number format** — `{year}-{receiptId:D5}` (e.g. `2024-00001`). Generated by INSERT with `receipt_no='TMP'`, then UPDATE after getting `SCOPE_IDENTITY()` — avoids race conditions.
16. **Fee structure storage** — delete+insert in transaction on every save. No partial updates; entire class/category/year combination is replaced atomically.
17. **Payment gateway abstraction** — `IGatewayService` interface + `GatewayServiceFactory` dictionary. Add new gateway: implement interface, add entry to factory, add option to UI dropdown. No other changes needed.
18. **Gateway key security** — `key_secret` stored in `gateway_configs` table; never returned in any API response. Frontend only ever receives `key_id`. `key_secret` fetched internally by server for Razorpay API calls.
19. **Online payment flow** — Staff initiates → server creates Razorpay order → frontend opens checkout (parent pays UPI/card/netbanking) → JS callback → server verifies HMAC signature → receipt created. Webhook (`payment-webhook`) provides fallback for browser-close edge cases; uses `notes.group_id` embedded in order to identify tenant.
20. **`is_online` flag on payment_modes** — determines whether a mode triggers gateway checkout vs offline collection. Migration adds column; existing "Online" mode auto-flagged.
21. **Gateway tables** — `gateway_configs` (API keys per school), `payment_gateway_orders` (order lifecycle tracking). Run `gateway_tables_migration.sql` on existing tenant DBs.
22. **Mobile tenant routing via `X-School-Code` header** — mobile app sends `X-School-Code: {subdomain}` (same value as `school_groups.subdomain`). API resolves it identically to `X-Subdomain`. School distributes their code to students at onboarding.
23. **Two mobile account types** — `student` accounts live in tenant DB (`student_mobile_accounts`); `parent` accounts live in `ascent_master` (`parent_accounts`). Both use SHA-256 PIN hashing (same as existing password hashing).
24. **Parent multi-child routing** — `parent_children` in master DB links a parent to students across any tenant DB (stores `student_id`, `group_id`, `db_name`, `school_id`). After parent login, selecting a child calls `POST /mobile/auth/select-child/{linkId}` which returns a `tokenType=student_view` JWT with the child's full context. All data endpoints work identically for student and student_view tokens.
25. **Mobile JWT token types** — `tokenType=student` (student login, has full child context), `tokenType=parent` (parent login; without child context after plain login, with child context after `select-child`). Claims: `accountId/parentId`, `studentId`, `schoolId`, `groupId`, `dbName`, `studentName`, `className`, `admissionNo`. Parent `select-child` re-issues parent token with the selected child's context embedded — same data endpoints work for both token types when `dbName` is present.
26. **Mobile new modules** — `student_attendance`, `exam_types`, `student_marks`, `homework`, `homework_attachments`, `announcements` added to tenant DB. `parent_accounts`, `parent_children`, `parent_refresh_tokens` added to master DB. Run `mobile_master_migration.sql` on `ascent_master` and `mobile_tenant_migration.sql` on each tenant DB.
27. **Sections** — A class can have 1 or more school-defined sections (A, B, C…). `sections` table in tenant DB; `students.section_id` is a nullable FK. Fee structure stays at class level (no per-section fees). Attendance and marks grids require class + section selection. Student form section field is a dropdown loaded by classId. Existing DBs: run `sections_migration.sql`, then `sample_sections.sql`, then `update_student_section_id.sql` (preview first, then uncomment UPDATE).
28. **Bulk CSV import** — Client parses CSV with `papaparse` (npm package), previews first 10 rows, sends rows as JSON array to server. Server builds lookup maps (names → IDs) in one query each, then validates + inserts row by row. Duplicate `admissionNo` within the batch OR already in DB → error row (never silent skip). Fee structure bulk upserts per row (delete+insert per Class+Category+FeeType+Term combination). Max 500 rows per upload. Response: `{ total, imported, failed, errors: [{ row, admissionNo/identifier, reason }] }`. Frontend shows progress bar + error table + download-errors CSV button.
29. **Multi-year student model** — `students` table has one row per student per academic year (same `admission_no`, different `student_id` and `academic_year_id`). Uniqueness enforced by filtered index `UQ_students_admno_school_year` on `(admission_no, school_id, academic_year_id)` WHERE both NOT NULL. Run `student_promotion_migration.sql` on existing tenant DBs. Promotion copies all personal fields into a new row with the target year+class+section; `section_id` is required (staff must pick it — no auto-assign). Students already present in the target year are skipped (idempotent).
30. **Mobile auth after promotion** — A promoted student gets a new `student_id`. On next login, `GetStudentByAdmissionNo` uses `ORDER BY academic_year_id DESC` to find the latest row. Login flow tries `GetAccountByStudentId(latest student_id)` first; if the account still references the old `student_id`, falls back to `GetAccountByAdmissionNo` (JOIN via students table), then calls `UpdateAccountStudentId` to silently re-point the account. No action needed by the student.
31. **Android white-label via product flavors** — One codebase, one repo, one Play Store developer account. Each school is a Gradle product flavor with its own `applicationId` (e.g. `in.educare.srividya`), `BuildConfig.SCHOOL_CODE` string, and `app_name` string resource. The `X-School-Code` API header is set from `BuildConfig.SCHOOL_CODE` in `RetrofitClient` — no runtime school selection. `TokenStore.schoolCode` field removed. Per-school icons go in `src/{flavorName}/res/mipmap-*/`. Each flavor is a separate Play Store listing under the same developer account.
32. **Parent-only mobile app** — Student login removed from Android app. Parent SMS OTP flow: mobile entry → OTP entry → child selector → home. Teacher login via "Staff Login" bottom sheet on auth screen. Backend student auth endpoints still exist for future use.
33. **SMS OTP auth** — `parent_otp` table in `ascent_master` stores SHA-256-hashed OTPs with 5-minute expiry and device_id. Rate limit: max 5 requests per mobile per 10 minutes. `SmsHelper.cs` calls smslogin.mobi HTTP API. Response is always generic — never reveals whether mobile is registered. OTP is only generated if a student with that `father_mobile` exists in the school's tenant DB (resolved from `X-School-Code`). `parent_accounts` has `device_id` + `device_bound_at` columns (run `sms_otp_migration.sql`).
34. **Device binding** — On OTP verify, `device_id` (UUID generated once on install, stored in `EncryptedSharedPreferences`, preserved across `TokenStore.clear()`) is bound to the parent account. If a different device logs in: all old refresh tokens are revoked (previous device silently logged out), new device is bound. Reinstall gets new UUID → treated as new device → OTP required → old session revoked.
35. **School events media storage** — No binary files in SQL Server (fastwebhost.in has limited disk). Media lives on free external services: YouTube (unlisted) for videos, Cloudinary free tier (25 GB) for images. `school_events` tenant table stores only metadata + URLs (~200 bytes/row). Android loads images with Coil (`AsyncImage`); videos open via YouTube Intent. School workflow: upload to YouTube/Cloudinary → paste URL in school web app → appears in mobile gallery tab.
36. **PDF/doc attachments on events and announcements** — `attachment_url` column (nullable VARCHAR 500) added to both `school_events` and `announcements` tables. Stores a Google Drive or Cloudinary PDF/doc link — no files on the server. Android shows an "Download / View Document" `OutlinedButton` when present; tapping opens with `Intent.ACTION_VIEW` (browser or PDF viewer). Homework uses the existing `homework_attachments` table; each attachment is now a tappable button in the Android app showing the filename.
37. **React web app white-label builds (Vite modes)** — Each school has a `.env.{school}` file (`VITE_API_BASE_URL`, `VITE_SUBDOMAIN`) and a `build:{school}` npm script. `npm run build:all` builds all schools at once. `build:all` output is one `dist/` per run — build schools sequentially and upload each `dist/` to the school's hosting directory. Currently configured schools: `demo`, `stannsasf`, `holyspiritjm`. Add a new school: create `.env.{school}`, add three lines to `package.json` scripts. `.env.{school}` files are gitignored.
38. **Coil image loading (Android)** — `io.coil-kt:coil-compose:2.6.0` added to `app/build.gradle.kts`. Used in `EventsScreen.kt` for thumbnail loading via `AsyncImage`. Required for any Compose screen that loads remote images.
39. **Block vs Detain distinction** — `Block` changes `status = 'Blocked'` (excluded from attendance marking, SMS recipient lists, and most active-student queries). `Detain` sets `is_detained = 1` while keeping `status = 'Active'` (student still attends, pays fees, gets marks — but is excluded from the Promote Students preview). Both require a mandatory reason.
40. **SMS bulk sending** — Two-layer approach prevents HTTP timeouts: client splits recipients into batches of 25 or 50 (user-selectable) and POSTs each batch separately; server uses `Task.WhenAll` to dispatch all SMS in a batch in parallel. Each sent/failed result is logged to `sms_logs`. DLT template IDs are placeholder constants in `SmsHelper.cs` — register templates with smslogin.mobi before go-live.
41. **Student promotion eligibility filter** — `GetForPromotion` uses `status IN ('Active', 'Y')` to handle both new-system students (`Active`) and VB6-migrated data (`Y`). Using strict `status = 'Active'` caused 0 results for schools with legacy data. Always use `IN ('Active', 'Y')` for "active student" queries rather than equality.
42. **Legacy status values** — VB6 system used `'Y'`/`'N'` for Active/Inactive. New system uses `'Active'`/`'Inactive'`/`'Blocked'`. Queries that need all active students should use `IN ('Active', 'Y')`. Run `subjects_status_migration.sql` to normalise subject status on existing DBs.
43. **Per-DB SQL credentials** — fastwebhost.in assigns a unique SQL Server username/password per database. `school_groups.db_username`/`db_password` store these. `TenantConnectionFactory.GetTenantConnection` looks them up from master DB (15-min `ConcurrentDictionary` cache), builds the connection string using the server from `TenantDb:ConnectionTemplate` but with per-DB credentials. Falls back to template credentials when columns are NULL (existing/demo groups). Cache is invalidated via `InvalidateCredentialCache(dbName)` after each `PUT /control/school-groups/{id}`. No JWT changes, no repository changes needed.
44. **Parent OTP auto-link** — `parent_accounts` and `parent_children` live in master DB (shared across all schools), enabling a parent with children in multiple schools to have one account. `request-otp` resolves tenant from `X-School-Code`, checks `students.father_mobile` in that school's tenant DB — OTP is only sent if a matching student is found (prevents OTP spam from unregistered numbers). `verify-otp` auto-creates `parent_accounts` if first-time login and upserts `parent_children` rows for every matching student (by `admission_no+group_id` — updates `class_name`, `student_name`, `student_id` if link exists, inserts if not). No pre-existing account or manual link-child flow required. Auto-created accounts have empty `pin_hash` (OTP-only; PIN login returns unauthorized for these accounts, which is correct).
45. **School-scoped child links** — Each white-label Android app (one per school) should only show children belonging to that school. `GET /mobile/auth/parent/children` filters `parent_children` by `group_id` resolved from `X-School-Code`. `POST /mobile/auth/parent/select-child` validates `link.GroupId == tenant.GroupId` as defense-in-depth (a linkId from a different school is rejected with 403). Child link upsert in `verify-otp` uses `GetChildLinkByAdmissionNo` + `UpdateChildLink` so existing links always have up-to-date `class_name` — fixes NULL class_name on links created before class data was assigned or before this fix was deployed.
46. **AntD multi-tab form — `forceRender: true`** — AntD v5 tabs render lazily by default; Form.Items on unvisited tabs are never mounted, so `form.validateFields()` returns `undefined` for their values and `form.setFieldsValue()` silently overwrites them with nulls on save. Fix: add `forceRender: true` to every tab item so all fields mount upfront. Apply to any AntD Tabs that wrap Form fields.
47. **Android app icon — `@mipmap/` not `@drawable/`** — `AndroidManifest.xml` must reference `@mipmap/ic_launcher` and `@mipmap/ic_launcher_round`. Using `@drawable/ic_launcher` loads the old vector XML directly, bypassing all mipmap density buckets generated by Image Asset Studio and producing the default blue rectangle on device. Always use `@mipmap/` for launcher icons in the manifest.
48. **Android Compose logo — use `ic_launcher_round` not `ic_launcher`** — `painterResource(R.mipmap.ic_launcher)` throws a runtime crash on API 26+ because `ic_launcher` resolves to an adaptive icon XML (`<adaptive-icon>`) which `painterResource` cannot decode. `ic_launcher_round` is a pre-rendered WebP bitmap and loads correctly. Always use `R.mipmap.ic_launcher_round` when displaying the launcher icon inside Compose.
49. **`Dp.Unspecified` crashes Material3 `NavigationBar`** — `Dp.Unspecified` equals `Float.NaN`. Passing it as `tonalElevation` to `NavigationBar` causes `surfaceColorAtElevation` to call `Color.copy(alpha = NaN)`, throwing `IllegalArgumentException` and crashing immediately. Never pass `Dp.Unspecified` to any composable that performs arithmetic on it — omit the parameter or use `0.dp`.
50. **R8 strips Google Tink in release builds — `EncryptedSharedPreferences` crash** — `EncryptedSharedPreferences` uses Google Tink internally and loads `KeyTypeManager` subclasses via reflection. R8 removes them as "unused", causing a crash in `Application.onCreate()` before any Activity starts (splash screen + immediate stop). Fix: add `-keep class com.google.crypto.tink.** { *; }` and matching `-keepnames` rules for `KeyTypeManager`/`PrivateKeyTypeManager` to `proguard-rules.pro`. Also wrap `EncryptedSharedPreferences.create()` in try-catch with a plain `SharedPreferences` fallback so future failures degrade gracefully.
51. **Fee type category detection via `description` LIKE** — The 5 fee collection screens filter `fee_types` by `description` column using LIKE patterns: Admission=`%admission%`, School=`%school%|%tuition%`, Transport=`%transport%|%bus%`, Hostel=`%hostel%`, Other=catch-all (NOT matching any of the above). School admins must set `description` on fee types to match the expected keyword (e.g. "Transport Fee", "Hostel Fee") for them to appear in the correct screen. Fee types with no description fall into the Other screen.
52. **Receipt number format change (Phase 24)** — New format: `{prefix}{yr6}{00001}`. `yr6` = `LEFT(academic_year,4) + RIGHT(academic_year,2)` e.g. "2024-25"→"202425". Prefix: T (Transport), H (Hostel), O (Other), or `class_groups.prefix` for Admission/School (falls back to "S"). Counter = MAX of last 5 digits matching `{prefix}{yr6}%` per school. Since the software was not live at the time of this change, no backfill of old receipts is needed.
53. **Cross-year fee outstanding** — `GET /school/fees/student-unique/{uniqueId}?feeTypeCategory=Transport` returns all academic year rows for a student (by `student_unique_id`), with outstanding per year. Each year row uses its own `student_id` for paid-amount calculation (since `fee_receipts.student_id` points to the year-specific student row). `student_unique_id` is also stored in `fee_receipts` for future cross-year reporting.
54. **`student_unique_id` — stable cross-year student identifier** — `students.student_unique_id` is an INT column that stays the same across all promoted rows for the same physical student. Different from `student_id` (IDENTITY, changes every year on promotion) and `admission_no` (can change at class 6 in some schools). Auto-assigned on INSERT via `MAX(student_unique_id)+1 WHERE school_id=@schoolId` — no sequence object needed. Promotion copies the value. Fee collection uses this to look up all academic year balances. `student_unique_id_migration.sql` backfills existing rows by grouping on `admission_no+school_id`.
55. **Monthly payment type for fee structures** — `fee_structures.payment_type` can be 'Term' or 'Monthly'. Term mode uses `term_id` FK; Monthly mode uses `fee_period_id` FK → `fee_periods` table. `FeeStructurePage` detects the type from loaded rows and shows the correct column set. Amount map key format: `{feeTypeId}_T_{termId}` (Term) or `{feeTypeId}_P_{periodId}` (Monthly). Switching payment type in the UI clears the map to prevent stale data. Fee periods are managed in Master Data → Fee Periods tab and loaded by academic year.
56. **Fee concession receipt number format** — `CFR{yr8}{D5}`: prefix "CFR" (3), yr8 = academic_year digits concatenated ("2026-2027"→"20262027", "2026-27"→"202627"), 5-digit counter per school per year. `FeeConcessionRepository.FormatYr8` splits on '-' and concatenates both parts. Counter = `MAX(TRY_CAST(RIGHT(receipt_no,5) AS INT))` filtered by pattern + exact length. Contrast with fee receipt format `{prefix}{yr6}{D5}` which uses only last 6 year digits.
57. **Fee concession outstanding integration** — `fee_concessions` stores one active concession per (student_id, fee_type_id, school_id, term_id) for Term-based fees and per (student_id, fee_type_id, school_id, fee_period_id) for Monthly fees. The outstanding subquery in `FeeRepository.GetStudentFeeSummary` and `GetCrossYearFeeSummary` matches by `ISNULL(fc.term_id,0)=ISNULL(fs.term_id,0)` and `ISNULL(fc.fee_period_id,0)=ISNULL(fs.fee_period_id,0)` so each term/period line only deducts its own concession. `outstanding = structure_amount - paid_receipts - concession_amount`. `ConcessionAmount` added to `FeeLineItemDto` — NOT included in PaidAmount. `FeeConcessionPage` loads terms+fee_periods when year changes and exposes Term/Period dropdowns in both Individual and Bulk tabs; term/period column shown in Concession List and printed on PDF receipt.
58. **Fee concession per-term unique index** — SQL Server treats NULLs as distinct in standard unique indexes (two NULL/NULL rows are allowed). For fee_concessions where term_id or fee_period_id is NULL for the other type, a single unique index on (student_id, fee_type_id, school_id, term_id, fee_period_id) would allow unlimited NULL pairs. Solution: two separate filtered indexes — `UQ_fee_concessions_term` WHERE term_id IS NOT NULL and `UQ_fee_concessions_period` WHERE fee_period_id IS NOT NULL. This enforces exactly one active concession per student+fee_type per term (or per period) while allowing both types to coexist.
59. **Transport update uses student_unique_id, not student_id** — `students.student_id` is a BIGINT IDENTITY that changes every year on promotion. Using it as the transport update key would silently fail after promotion (no matching row). `PUT /school/transport/students/{studentUniqueId}` uses `student_unique_id` (stable INT, same across promotions) + `academic_year_id` (from request body) in the WHERE clause. `StudentTransportDto.StudentUniqueId` and `AcademicYearId` are returned from `GetStudentTransport` so the React modal has these values available when submitting the update.
60. **Transport fee collection uses bus_fee_structures, not fee_structures** — `GetCrossYearFeeSummary` with `feeTypeCategory=Transport` bypasses `fee_structures` entirely. It reads amounts from `bus_fee_structures` (keyed by `route_id + term_id + academic_year_id`) and matches them to the student's `bus_route_id`. Years where the student has no route assigned are skipped (not shown at all). Paid-amount tracking uses the new `fee_receipt_items.bus_route_id` column. This means different students in the same class correctly see different outstanding amounts based on which route they ride.
61. **Transport concession via fee_concessions** — `fee_concessions.fee_type_id` is now nullable; when NULL, `bus_route_id` is set instead (transport concession). The concession subquery in `GetTransportLineItems` matches by `bus_route_id + term_id`. Existing school-fee unique indexes recreated with `AND fee_type_id IS NOT NULL` in filter so transport rows (NULL fee_type_id) are excluded; a separate `UQ_fee_concessions_transport` index enforces one active transport concession per `(student_id, bus_route_id, school_id, term_id)`. `FeeConcessionPage` global toggle "School Fee | Transport" switches both Individual Entry (route+term instead of fee type) and Bulk Apply (loads students by `?busRouteId=` via new filter on `GET /school/students`).
62. **Parent portal is gateway-only** — parents can only pay via Razorpay (UPI/card/netbanking); cash/cheque is not offered through the portal. `MobileFeesController` creates Razorpay orders and verifies HMAC signatures server-side; it never calls `CollectFee` with an offline payment mode. The fee items are sent with `Amount = li.outstanding` and `ConcessionAmount = 0` because `outstanding` already nets out concessions; the server-side outstanding calculation (`GetCrossYearFeeSummary`) applies concessions before returning line items.
63. **Parent portal child context — session-scoped** — after page reload the silent refresh (`POST /mobile/auth/parent/refresh`) restores the parent JWT (without child context, since the cookie is the parent refresh token). The app redirects to `/select-child` rather than persisting the child selection to localStorage. This is intentional: each session the parent explicitly picks which child to pay for, preventing accidental payment for the wrong child.
64. **`Input.OTP` requires AntD ≥ 5.16.0** — `Input.OTP` component was added in AntD 5.16.0. The parent app `package.json` is pinned to `^5.16.0`; the school app is on `^6.x`. Using `^5.14.0` (the original minimum) could install a version without `Input.OTP` if a lockfile is present. Always ensure AntD ≥ 5.16 when `Input.OTP` is used.
65. **Legacy receipt grouping key** — `BulkImportReceipts` groups CSV rows into receipts. When `LegacyReceiptNo` is non-empty, all rows sharing the same `{LegacyReceiptNo}+{AdmissionNo}+{AcademicYear}` form one receipt (preserving the original receipt boundary). When `LegacyReceiptNo` is blank, rows are grouped by `{AdmissionNo}+{AcademicYear}+{PaymentDate}+{PaymentMode}` — one receipt per student per date per payment mode. This covers both: schools that have legacy receipt numbers (use them as keys) and those that only have raw payment data (auto-group by date/mode). `LegacyReceiptNo` is stored in `fee_receipts.remarks` as `"Legacy: {no}"` for audit traceability.
66. **End-of-day receipt sync** — `GET /school/fees/receipts` accepts `?createdAfter=datetime` (ISO 8601) which filters on `fee_receipts.created_at`. The school's legacy system calls this at end-of-day with the previous sync timestamp to get only that day's new receipts, then downloads as CSV via the "Export CSV" button. The `ReceiptsPage` client-side papaparse export matches the exact columns the legacy system expects.
67. **`MobileParentOrderRequest` vs `MobileCreateOrderRequest` — naming disambiguation** — `GatewayDtos.cs` (namespace `School.Fee`) originally defined `MobileCreateOrderRequest` for the parent portal endpoint (`POST /mobile/fees/payment-orders`). `Mobile/Data/FeeDtos.cs` already had a `MobileCreateOrderRequest` for the old student fee endpoint. Both were imported in `MobileStudentController.cs`, causing CS0104 ambiguous reference. Fix: renamed the `School.Fee` version to `MobileParentOrderRequest` throughout `GatewayDtos.cs` and `MobileFeesController.cs`. The two types have different shapes: `MobileParentOrderRequest` has `FeeTypeCategory + List<CollectFeeItem>` (server resolves payment mode); `MobileCreateOrderRequest` has `PaymentModeId + List<MobileFeeOrderItem>` (client-provided mode).
68. **CORS — `X-School-Code` must be in `Access-Control-Allow-Headers`** — `Global.asax.cs` `Application_BeginRequest` sets the preflight response headers. `X-School-Code` was missing from `Access-Control-Allow-Headers`, blocking the parent web app (`localhost:5174`) from sending that header. Added alongside `X-Subdomain`. Both headers must be listed whenever a new custom header is introduced; otherwise the browser blocks the preflight before any request reaches the API.
69. **Android fee screen — selection scoped to one academic year** — items from different academic years cannot be mixed in a single Razorpay order because the backend `POST /mobile/fees/payment-orders` accepts a single `academicYearId`. `FeeScreen.kt` tracks `selectedYearId`; tapping an item from a different year automatically clears the previous selection and starts fresh. This prevents a silent wrong-year order on the backend.
70. **Android persistent cookie jar — OkHttp default jar is in-memory** — OkHttp's `CookieJar.NO_COOKIES` (the default) discards all cookies when the process dies. The server's `parentRefreshToken` (7-day HttpOnly sliding-expiry) was lost on every app kill, forcing OTP re-login the next day. Fix: `PersistentCookieJar` implements `CookieJar`, serializes each `Cookie` as a `SerializedCookie` data class (name, value, domain, path, expiresAt, secure, httpOnly) to a plain `SharedPreferences` file via Gson. Cookies are reloaded into an in-memory `cache` map on construction (keyed `"domain|name"`) and flushed on every `saveFromResponse`. Existing ProGuard rule `-keep class com.ascentschools.mobile.data.api.**` covers `SerializedCookie` and `PersistentCookieJar` (both live in that package).
71. **Android startup silent refresh** — after fixing cookie persistence (see #70), a cold start may still have an expired access token (30-min JWT) even though the refresh cookie is valid. `MainActivity` tracks `isCheckingSession` (initially `true` when `tokenStore.isLoggedIn`). A `LaunchedEffect(Unit)` calls `authRepo.silentRefresh()` (parent) or `authRepo.silentRefreshTeacher()` based on `tokenStore.userType`, shows `CircularProgressIndicator` while in progress, then sets `isCheckingSession = false`. On success: home screen is shown with a fresh token. On failure: `tokenStore.clear()` is called and the user lands on the OTP login screen. This mirrors the silent refresh pattern already used in both React web apps.

---

## Mobile App Architecture
- **Platform:** Android (Kotlin + Jetpack Compose, MVVM)
- **Auth flow (Parent):** SMS OTP — enter mobile → receive OTP → verify → child selector → HomeScreen
- **Auth flow (Teacher/Staff):** "Staff Login" bottom sheet on auth screen → username + password → TeacherHomeScreen
- **White-label:** Android product flavors — one APK per school; `applicationId`, `SCHOOL_CODE`, and `app_name` set per flavor in `build.gradle.kts`. `X-School-Code` header is baked in via `BuildConfig.SCHOOL_CODE` (no runtime school selection). Add a new school: copy the flavor template block, update 3 fields, drop icons in `src/{flavorName}/res/mipmap-*/`.
- **Build a school APK:** `./gradlew assemble{FlavorName}Release` (e.g. `assembleSrividyaRelease`). In Android Studio: select flavor from Build Variants panel.
- **Token storage:** `EncryptedSharedPreferences`; `deviceId` auto-generated UUID on first install, preserved across logout (`TokenStore.clear()` saves and restores it); `userType` field ("parent"/"teacher") determines which home screen to show
- **HTTP client:** Retrofit + OkHttp; `X-School-Code` from `BuildConfig.SCHOOL_CODE`, `Authorization` from `TokenStore.accessToken`
- **Android file structure:**
```
ui/auth/
├── SmsAuthScreen.kt      ← primary entry: school logo (`R.mipmap.ic_launcher_round`) + name on login; mobile entry card → OTP entry card; "Staff Login" bottom sheet for teachers
├── SmsAuthViewModel.kt   ← state machine: Idle/Loading/OtpSent/ParentSuccess/TeacherSuccess/Error
├── LoginScreen.kt        ← (legacy, superseded by SmsAuthScreen — may be removed)
└── LoginViewModel.kt     ← (legacy, superseded by SmsAuthViewModel — may be removed)
ui/teacher/
├── TeacherHomeScreen.kt      ← class + section dropdowns; action cards for Attendance and Homework
├── TeacherAttendanceScreen.kt← student list; tap=toggle P↔A; long-press=status sheet (P/A/Late); date picker; summary chips
├── TeacherHomeworkScreen.kt  ← homework list with due-date highlighting; FAB → CreateHomeworkSheet
└── TeacherViewModel.kt       ← loadClasses/Sections/Attendance/Homework; toggleStudentStatus; markAllPresent; saveAttendance; createHomework
data/local/TokenStore.kt      ← deviceId (auto-UUID, never cleared); userType; accessToken; studentName etc.
data/repository/AuthRepository.kt    ← requestOtp, verifyOtp, loginTeacher, logoutTeacher, logoutParent, getChildren, selectChild, silentRefresh (parent), silentRefreshTeacher
data/repository/StudentRepository.kt ← getProfile, getAttendance, getMarks, getHomework, getAnnouncements, getEvents
data/repository/FeeRepository.kt     ← getOutstanding(feeTypeCategory) → List<MobileFeeSummaryDto>; createOrder(MobileCreateOrderRequest); verifyPayment(gatewayOrderId, MobileVerifyRequest)
data/repository/TeacherRepository.kt ← getClasses, getSections, getAttendance, saveAttendance, getHomework, createHomework
ui/fee/FeeViewModel.kt        ← categories list (School/Transport/Hostel); selectedCategory StateFlow; FeeUiState.Success holds List<MobileFeeSummaryDto> (cross-year); initiatePayment(items, academicYearId, feeTypeCategory); verifyPayment passes gatewayOrderId as path param
ui/fee/FeeScreen.kt           ← TabRow (School/Transport/Hostel); LazyColumn grouped by academic year; per-year YearSummaryCard + pending/paid items; selection scoped to one year (switching year clears prior selection); concession shown in blue on item card; sticky pay bar
ui/events/EventsViewModel.kt         ← Loading/Success/Error state; load()
ui/events/EventsScreen.kt            ← card list with Coil thumbnail; YouTube overlay (dark scrim + SmartDisplay icon + red badge); attachment OutlinedButton opens Intent.ACTION_VIEW
MainActivity.kt               ← routes: not logged in → SmsAuthScreen; userType=teacher → TeacherHomeScreen; else → HomeScreen (parent); onInitiatePayment callback takes feeTypeCategory: String (not paymentModeId); startup silent refresh via isCheckingSession + LaunchedEffect(Unit) → CircularProgressIndicator while refreshing, force re-login on failure
```
- **Screens (Parent):** SmsAuthScreen → Child Selector → HomeScreen → Attendance / Marks / Homework / Announcements / Profile / Fees / Events (Phase 11)
- **Screens (Teacher):** SmsAuthScreen (Staff Login sheet) → TeacherHomeScreen → TeacherAttendanceScreen / TeacherHomeworkScreen

---

## Pending / TBD
- **Staff UI** — `staff`, `staff_attendance`, `staff_advances`, `staff_salaries` tables exist in DB (via `staff_tables_migration.sql`) and the Staff section exists in the school app nav, but the full staff management UI (StaffPage, StaffAttendancePage, etc.) may need completion/verification
- **SMS DLT templates** — `SmsHelper.AbsentTemplateId`, `FeeDueTemplateId`, `CustomTemplateId` are placeholder constants; register actual templates with smslogin.mobi before SMS Center goes live
- **`ascent-control-app` env** — create `.env.local` with `VITE_API_URL=http://localhost:62845` for local dev
- **`ascent-school-app` env for local dev** — the school `.env.{school}` files point to `https://edu-care.in/api`; for local dev against a local API, temporarily change `VITE_API_BASE_URL` to `http://localhost:62845`

---

## Reference Files
| File | Purpose |
|---|---|
| `Database\master_tables.sql` | Run once to create `ascent_master` DB (15 tables, includes parent mobile tables) |
| `Database\master_db_migration.sql` | Add `db_name` column to existing `ascent_master.school_groups` |
| `Database\master_db_credentials_migration.sql` | Add `db_username` and `db_password` columns to `ascent_master.school_groups` |
| `Database\tenant_tables.sql` | Run once per school group for `ascent_group_{id}` DB (46 tables + permissions + roles; includes all migrations up to Phase 30) |
| `Database\seed_control_user.sql` | Seed a control app login user into `ascent_master.control_users` |
| `Database\seed_tenant_data.sql` | Seed role-permission mappings, payment modes, default school admin login into tenant DB |
| `Database\sections_migration.sql` | Add `sections` table + `section_id` column to `students` on existing tenant DBs |
| `Database\sample_sections.sql` | Insert Section A and B for every class in a school (run after sections_migration.sql) |
| `Database\update_student_section_id.sql` | Map existing students' free-text section → section_id FK (preview then uncomment UPDATE) |
| `Database\gateway_tables_migration.sql` | Add gateway tables + `is_online` to existing tenant DBs |
| `Database\fee_tables_migration.sql` | Add fee receipt tables to existing tenant DBs |
| `Database\mobile_master_migration.sql` | Add parent_accounts, parent_children, parent_refresh_tokens to `ascent_master` |
| `Database\seed_mobile_parent_data.sql` | Seed 3 demo parent accounts + child links into `ascent_master` (run after demo_data.sql) |
| `Database\mobile_tenant_migration.sql` | Add student_mobile_accounts, student_refresh_tokens, student_attendance, exam_types, student_marks, homework, homework_attachments, announcements to tenant DBs |
| `Database\student_promotion_migration.sql` | Add `UQ_students_admno_school_year` filtered unique index to enable multi-year student records on existing tenant DBs |
| `Database\sms_otp_migration.sql` | Add `device_id`/`device_bound_at` to `parent_accounts`; create `parent_otp` table in `ascent_master` |
| `Database\events_migration.sql` | Add `school_events` table (with `attachment_url`) to existing tenant DBs (Phase 11) |
| `Database\announcements_attachment_migration.sql` | Add `attachment_url` column to existing `announcements` table |
| `Database\block_student_migration.sql` | Add `blocked_reason` column to `students` on existing tenant DBs |
| `Database\detained_students_migration.sql` | Add `is_detained` + `detained_reason` columns to `students` on existing tenant DBs |
| `Database\staff_tables_migration.sql` | Add staff, staff_attendance, staff_advances, staff_advance_repayments, staff_salary_components, staff_salaries, staff_salary_items to existing tenant DBs |
| `Database\sms_migration.sql` | Add `sms_logs` table to existing tenant DBs |
| `Database\subjects_status_migration.sql` | Widen subjects.status VARCHAR(5→10); normalise 'Y'→'Active', 'N'→'Inactive' on existing tenant DBs |
| `Database\homework_attachment_url_migration.sql` | Add `attachment_url` column to `homework` table on existing tenant DBs |
| `Database\homework_section_migration.sql` | Add `section_id` nullable FK column to `homework` table on existing tenant DBs |
| `Database\fee_periods_migration.sql` | Add `fee_periods` table + `payment_type`/`fee_period_id` to `fee_structures` + `fee_period_id` to `fee_receipt_items` on existing tenant DBs |
| `Database\student_unique_id_migration.sql` | Add `student_unique_id` INT column to `students` and backfill (groups by admission_no+school_id) on existing tenant DBs |
| `Database\fee_collection_redesign_migration.sql` | Add `student_unique_id` column to `fee_receipts` on existing tenant DBs |
| `Database\fee_concessions_migration.sql` | Add `fee_concessions` table (with `term_id`/`fee_period_id` columns and two filtered unique indexes) on existing tenant DBs |
| `Database\transport_receipt_migration.sql` | Add `bus_route_id INT NULL` + FK to `fee_receipt_items` on existing tenant DBs |
| `Database\transport_concession_migration.sql` | Make `fee_concessions.fee_type_id` nullable; add `bus_route_id INT NULL` + FK; recreate term/period unique indexes with `fee_type_id IS NOT NULL` filter; add `UQ_fee_concessions_transport` index on existing tenant DBs |
| `Database\hostel_migration.sql` | Add `hostels` table, `hostel_fee_structures` table; add `hostel_id INT NULL FK` to `students`, `fee_receipt_items`, `fee_concessions`; add hostel concession filtered unique indexes on existing tenant DBs |
| `Database\create_tables.sql` | Legacy single-DB version — reference only, do not use |
| `Schema_Proposal.md` | Full old VB6 → new column mapping with FK summary |
| `Clarification Answers.txt` | 35 domain Q&A answered by client |

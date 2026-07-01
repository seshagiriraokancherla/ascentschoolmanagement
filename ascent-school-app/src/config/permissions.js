/**
 * Frontend permission codes — must mirror permissions.permission_code in the tenant DB
 * and AscentSchools.Core.Constants.PermissionCodes on the backend.
 *
 * Single source of truth for page-level access control (Phase A — UX gating).
 * Backend endpoint enforcement (Phase B) is separate; the frontend only HIDES what a
 * user cannot use — it is not a security boundary on its own.
 */
export const P = {
  // Student Profile
  STUDENT_VIEW:   'STUDENT_PROFILE.VIEW',
  STUDENT_CREATE: 'STUDENT_PROFILE.CREATE',
  STUDENT_EDIT:   'STUDENT_PROFILE.EDIT',
  STUDENT_DELETE: 'STUDENT_PROFILE.DELETE',

  // Student Fee
  FEE_VIEW:       'STUDENT_FEE.VIEW',
  FEE_COLLECT:    'STUDENT_FEE.COLLECT',
  FEE_EDIT:       'STUDENT_FEE.EDIT',
  FEE_CANCEL:     'STUDENT_FEE.CANCEL_RECEIPT',
  FEE_CONCESSION: 'STUDENT_FEE.CONCESSION',

  // Attendance
  ATTENDANCE_VIEW: 'ATTENDANCE.VIEW',
  ATTENDANCE_MARK: 'ATTENDANCE.MARK',

  // Marks
  MARKS_VIEW:  'MARKS.VIEW',
  MARKS_ENTER: 'MARKS.ENTER',

  // Transport
  TRANSPORT_VIEW:   'TRANSPORT.VIEW',
  TRANSPORT_MANAGE: 'TRANSPORT.MANAGE',

  // Hostel
  HOSTEL_VIEW:   'HOSTEL.VIEW',
  HOSTEL_MANAGE: 'HOSTEL.MANAGE',

  // ── New admin / settings / supporting modules (Phase A) ──────────────
  MASTER_VIEW:   'MASTER_DATA.VIEW',
  MASTER_MANAGE: 'MASTER_DATA.MANAGE',

  HOMEWORK_VIEW:   'HOMEWORK.VIEW',
  HOMEWORK_MANAGE: 'HOMEWORK.MANAGE',

  ANNOUNCEMENT_VIEW:   'ANNOUNCEMENT.VIEW',
  ANNOUNCEMENT_MANAGE: 'ANNOUNCEMENT.MANAGE',

  EVENTS_VIEW:   'EVENTS.VIEW',
  EVENTS_MANAGE: 'EVENTS.MANAGE',

  STAFF_VIEW:   'STAFF.VIEW',
  STAFF_MANAGE: 'STAFF.MANAGE',

  REPORTS_VIEW: 'REPORTS.VIEW',

  SMS_SEND: 'SMS.SEND',

  SETTINGS_MANAGE: 'SETTINGS.MANAGE',

  USER_MANAGE: 'USER_MGMT.MANAGE',
}

/**
 * Maps an absolute route path → the permission required to access it.
 * `null` means the page is available to every authenticated user (e.g. Dashboard).
 * A page whose path is absent from this map is treated as unrestricted.
 *
 * Keys here also match the nav `key` values in AppLayout, so the nav filter and the
 * route guard share this one map.
 */
export const PATH_PERM = {
  '/':                          null, // Dashboard — always accessible

  '/students':                  P.STUDENT_VIEW,
  '/students/new':              P.STUDENT_CREATE,
  '/students/import':           P.STUDENT_CREATE,
  '/students/promote':          P.STUDENT_EDIT,
  '/students/blood-group':      P.STUDENT_VIEW,
  '/students/:id':              P.STUDENT_VIEW,

  '/fees/structure':            P.FEE_EDIT,
  '/fees/structure/import':     P.FEE_EDIT,
  '/fees/collect/admission':    P.FEE_COLLECT,
  '/fees/collect/school':       P.FEE_COLLECT,
  '/fees/collect/transport':    P.FEE_COLLECT,
  '/fees/collect/hostel':       P.FEE_COLLECT,
  '/fees/collect/other':        P.FEE_COLLECT,
  '/fees/receipts':             P.FEE_VIEW,
  '/fees/receipts/import':      P.FEE_EDIT,
  '/fees/pending-payments':     P.FEE_COLLECT,
  '/fees/concessions':          P.FEE_CONCESSION,

  '/master':                    P.MASTER_VIEW,
  '/attendance':                P.ATTENDANCE_VIEW,
  '/transport':                 P.TRANSPORT_VIEW,
  '/hostel':                    P.HOSTEL_VIEW,
  '/marks':                     P.MARKS_VIEW,
  '/homework':                  P.HOMEWORK_VIEW,
  '/homework/daily':            P.HOMEWORK_MANAGE,
  '/announcements':             P.ANNOUNCEMENT_VIEW,
  '/events':                    P.EVENTS_VIEW,
  '/reports':                   P.REPORTS_VIEW,

  '/staff':                     P.STAFF_VIEW,
  '/staff/attendance':          P.STAFF_MANAGE,
  '/staff/attendance/summary':  P.STAFF_VIEW,
  '/staff/advances':            P.STAFF_MANAGE,
  '/staff/salaries':            P.STAFF_MANAGE,

  '/sms':                       P.SMS_SEND,

  '/settings/school':           P.SETTINGS_MANAGE,
  '/settings/roles':            P.USER_MANAGE,
  '/settings/users':            P.USER_MANAGE,
  '/settings/payment-gateway':  P.SETTINGS_MANAGE,
  '/settings/sms-gateway':      P.SETTINGS_MANAGE,
}

/** True if the user (via hasPermission) may access the given absolute path. */
export function canAccessPath(path, hasPermission) {
  const perm = PATH_PERM[path]
  return !perm || hasPermission(perm)
}

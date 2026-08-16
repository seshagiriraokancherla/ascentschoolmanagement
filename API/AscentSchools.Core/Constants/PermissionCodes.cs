namespace AscentSchools.Core.Constants
{
    /// <summary>
    /// Permission codes — must match permissions.permission_code seeded in tenant DB.
    /// Format: MODULE_CODE.ACTION
    /// Use these constants in [RequirePermission(...)] attributes — never hardcode strings.
    /// </summary>
    public static class PermissionCodes
    {
        public static class StudentProfile
        {
            public const string View   = "STUDENT_PROFILE.VIEW";
            public const string Create = "STUDENT_PROFILE.CREATE";
            public const string Edit   = "STUDENT_PROFILE.EDIT";
            public const string Delete = "STUDENT_PROFILE.DELETE";
        }

        public static class StudentFee
        {
            public const string View          = "STUDENT_FEE.VIEW";
            public const string Collect       = "STUDENT_FEE.COLLECT";
            public const string Edit          = "STUDENT_FEE.EDIT";
            public const string CancelReceipt = "STUDENT_FEE.CANCEL_RECEIPT";
            public const string Concession    = "STUDENT_FEE.CONCESSION";
        }

        public static class Attendance
        {
            public const string View = "ATTENDANCE.VIEW";
            public const string Mark = "ATTENDANCE.MARK";
            public const string Edit = "ATTENDANCE.EDIT";
        }

        public static class Marks
        {
            public const string View    = "MARKS.VIEW";
            public const string Enter   = "MARKS.ENTER";
            public const string Edit    = "MARKS.EDIT";
            public const string Publish = "MARKS.PUBLISH";
        }

        public static class Library
        {
            public const string View   = "LIBRARY.VIEW";
            public const string Issue  = "LIBRARY.ISSUE";
            public const string Return = "LIBRARY.RETURN";
            public const string Manage = "LIBRARY.MANAGE";
        }

        public static class Transport
        {
            public const string View   = "TRANSPORT.VIEW";
            public const string Manage = "TRANSPORT.MANAGE";
        }

        public static class Hostel
        {
            public const string View   = "HOSTEL.VIEW";
            public const string Manage = "HOSTEL.MANAGE";
        }

        // ── Admin / settings / supporting modules ────────────────────────
        public static class MasterData
        {
            public const string View   = "MASTER_DATA.VIEW";
            public const string Manage = "MASTER_DATA.MANAGE";
        }

        public static class Homework
        {
            public const string View   = "HOMEWORK.VIEW";
            public const string Manage = "HOMEWORK.MANAGE";
        }

        public static class Announcement
        {
            public const string View   = "ANNOUNCEMENT.VIEW";
            public const string Manage = "ANNOUNCEMENT.MANAGE";
        }

        public static class Events
        {
            public const string View   = "EVENTS.VIEW";
            public const string Manage = "EVENTS.MANAGE";
        }

        public static class Staff
        {
            public const string View   = "STAFF.VIEW";
            public const string Manage = "STAFF.MANAGE";
        }

        public static class Reports
        {
            public const string View = "REPORTS.VIEW";
        }

        public static class Sms
        {
            public const string Send = "SMS.SEND";
        }

        public static class Settings
        {
            public const string Manage = "SETTINGS.MANAGE";
        }

        public static class Messages
        {
            public const string View = "MESSAGES.VIEW";   // read-only admin view of parent↔teacher conversations
        }

        public static class UserManagement
        {
            public const string Manage = "USER_MGMT.MANAGE";
        }
    }
}

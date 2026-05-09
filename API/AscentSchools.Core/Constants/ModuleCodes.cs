namespace AscentSchools.Core.Constants
{
    /// <summary>
    /// Module codes — must match modules.module_code in ascent_master DB.
    /// Used in RequirePermission attributes and module access checks.
    /// </summary>
    public static class ModuleCodes
    {
        public const string StudentProfile = "STUDENT_PROFILE";
        public const string StudentFee     = "STUDENT_FEE";
        public const string Attendance     = "ATTENDANCE";
        public const string Marks          = "MARKS";
        public const string Library        = "LIBRARY";
        public const string Transport      = "TRANSPORT";
        public const string Hostel         = "HOSTEL";
    }
}

import SwiftUI

// Semantic colour palette mirroring Android `Theme.kt` (Phase 22 navy/gold).
// Use `AppTheme.Palette.*` everywhere — do not hard-code hex values in views.
enum AppTheme {

    enum Palette {
        // Primary (navy)
        static let navyBlue        = Color(hex: 0x1E3A8A)
        static let navyBlueLight   = Color(hex: 0x2563EB)
        static let navyContainer   = Color(hex: 0xDBEAFE)
        static let onNavyContainer = Color(hex: 0x1E3A8A)

        // Secondary (gold)
        static let gold            = Color(hex: 0xB45309)
        static let goldContainer   = Color(hex: 0xFEF3C7)
        static let onGoldContainer = Color(hex: 0x78350F)

        // Tertiary
        static let teal            = Color(hex: 0x0891B2)

        // Neutrals
        static let appBackground   = Color(hex: 0xF1F5F9)
        static let appSurface      = Color(hex: 0xFFFFFF)
        static let surfaceVariant  = Color(hex: 0xE2E8F0)
        static let textPrimary     = Color(hex: 0x0F172A)
        static let textSecondary   = Color(hex: 0x64748B)

        // Attendance status
        static let present  = Color(hex: 0x22C55E)   // green
        static let absent   = Color(hex: 0xEF4444)   // red
        static let late     = Color(hex: 0xF97316)   // orange
        static let halfDay  = Color(hex: 0xFBBF24)   // amber
    }

    // Maps attendance status string (Android contract: "Present"/"Absent"/"Late"/"HalfDay") to a colour.
    static func color(forStatus status: String?) -> Color {
        switch (status ?? "").lowercased() {
        case "present", "p": return Palette.present
        case "absent",  "a": return Palette.absent
        case "late",    "l": return Palette.late
        case "halfday", "half day", "hd": return Palette.halfDay
        default:             return Palette.textSecondary
        }
    }

    // Login-screen gradient (Android `SmsAuthScreen.kt` diagonal gradient).
    static let loginGradient = LinearGradient(
        colors: [
            Color(hex: 0x1E3A8A),
            Color(hex: 0x1D4ED8),
            Color(hex: 0x0369A1),
        ],
        startPoint: .topLeading,
        endPoint: .bottomTrailing
    )
}

import SwiftUI

// Type scale that mirrors the Material 3 sizes used by Android `Theme.kt`.
// Sizes are in points (SwiftUI scales them for Dynamic Type automatically).
extension Font {
    static let appDisplayLarge   = Font.system(size: 57, weight: .bold)
    static let appDisplayMedium  = Font.system(size: 45, weight: .bold)
    static let appDisplaySmall   = Font.system(size: 36, weight: .bold)

    static let appHeadlineLarge  = Font.system(size: 32, weight: .semibold)
    static let appHeadlineMedium = Font.system(size: 28, weight: .semibold)
    static let appHeadlineSmall  = Font.system(size: 24, weight: .semibold)

    static let appTitleLarge     = Font.system(size: 22, weight: .semibold)
    static let appTitleMedium    = Font.system(size: 16, weight: .semibold)
    static let appTitleSmall     = Font.system(size: 14, weight: .medium)

    static let appBodyLarge      = Font.system(size: 16, weight: .regular)
    static let appBodyMedium     = Font.system(size: 14, weight: .regular)
    static let appBodySmall      = Font.system(size: 12, weight: .regular)

    static let appLabelLarge     = Font.system(size: 14, weight: .medium)
    static let appLabelMedium    = Font.system(size: 12, weight: .medium)
    static let appLabelSmall     = Font.system(size: 11, weight: .medium)
}

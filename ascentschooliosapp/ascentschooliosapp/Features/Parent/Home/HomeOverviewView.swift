import SwiftUI

struct HomeOverviewView: View {
    private var store: KeychainTokenStore { KeychainTokenStore.shared }

    var body: some View {
        ScrollView {
            VStack(spacing: 16) {
                welcomeCard

                quickLinks

                Text("Use the tabs below to view attendance, marks, fees and more.")
                    .font(.appBodySmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
                    .multilineTextAlignment(.center)
                    .padding(.top, 4)
            }
            .padding(20)
        }
        .background(AppTheme.Palette.appBackground)
    }

    // MARK: - Pieces

    private var welcomeCard: some View {
        HStack(alignment: .center, spacing: 14) {
            Circle()
                .fill(AppTheme.Palette.navyContainer)
                .frame(width: 56, height: 56)
                .overlay(
                    Text(initials(for: store.studentName ?? ""))
                        .font(.appTitleMedium)
                        .foregroundStyle(AppTheme.Palette.onNavyContainer)
                )
                .accessibilityHidden(true)   // student name is read from the card body

            VStack(alignment: .leading, spacing: 4) {
                Text("Welcome")
                    .font(.appLabelMedium)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
                Text(store.studentName ?? "—")
                    .font(.appHeadlineSmall)
                    .foregroundStyle(AppTheme.Palette.textPrimary)

                HStack(spacing: 6) {
                    if let className = store.className, !className.isEmpty {
                        Text(className)
                    }
                    if let admission = store.admissionNo, !admission.isEmpty {
                        Text("·").foregroundStyle(AppTheme.Palette.textSecondary.opacity(0.5))
                        Text(admission)
                    }
                }
                .font(.appLabelSmall)
                .foregroundStyle(AppTheme.Palette.textSecondary)
            }

            Spacer()
        }
        .padding(16)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 16))
    }

    private var quickLinks: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("Quick links")
                .font(.appLabelLarge)
                .foregroundStyle(AppTheme.Palette.textSecondary)

            VStack(spacing: 0) {
                quickRow(icon: "calendar", title: "Attendance", subtitle: "Monthly summary and daily marks")
                Divider().padding(.leading, 50)
                quickRow(icon: "chart.bar.fill", title: "Marks", subtitle: "Exam results by academic year")
                Divider().padding(.leading, 50)
                quickRow(icon: "person.circle", title: "Profile", subtitle: "Personal & contact information")
            }
            .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 16))
        }
    }

    private func quickRow(icon: String, title: String, subtitle: String) -> some View {
        HStack(spacing: 14) {
            Image(systemName: icon)
                .font(.system(size: 18))
                .foregroundStyle(AppTheme.Palette.navyBlue)
                .frame(width: 22)

            VStack(alignment: .leading, spacing: 2) {
                Text(title)
                    .font(.appBodyMedium)
                    .foregroundStyle(AppTheme.Palette.textPrimary)
                Text(subtitle)
                    .font(.appLabelSmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
            }
            Spacer()
        }
        .padding(.horizontal, 14)
        .padding(.vertical, 12)
    }

    private func initials(for name: String) -> String {
        let parts = name
            .split(separator: " ")
            .prefix(2)
            .compactMap { $0.first.map(String.init) }
        return parts.joined().uppercased()
    }
}

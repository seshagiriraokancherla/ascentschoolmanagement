import SwiftUI

struct ChildSelectorView: View {
    let viewModel: AuthViewModel
    let children: [ChildDto]

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            VStack(alignment: .leading, spacing: 4) {
                Text("Choose a child")
                    .font(.appTitleLarge)
                    .foregroundStyle(AppTheme.Palette.textPrimary)
                Text("You'll only see one child at a time. You can switch later from the home screen.")
                    .font(.appBodySmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
            }

            VStack(spacing: 10) {
                ForEach(children) { child in
                    Button {
                        Task { await viewModel.selectChild(linkId: child.linkId) }
                    } label: {
                        row(for: child)
                    }
                    .buttonStyle(.plain)
                    .disabled(viewModel.isLoading)
                }
            }
        }
        .padding(24)
        .overlay {
            if viewModel.isLoading {
                ProgressView()
                    .progressViewStyle(.circular)
                    .tint(AppTheme.Palette.navyBlue)
                    .padding(20)
                    .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 12))
            }
        }
    }

    private func row(for child: ChildDto) -> some View {
        HStack(spacing: 12) {
            Circle()
                .fill(AppTheme.Palette.navyContainer)
                .frame(width: 44, height: 44)
                .overlay(
                    Text(initials(for: child.studentName))
                        .font(.appLabelLarge)
                        .foregroundStyle(AppTheme.Palette.onNavyContainer)
                )
                .accessibilityHidden(true)   // child name is read from the row text

            VStack(alignment: .leading, spacing: 2) {
                Text(child.studentName)
                    .font(.appTitleMedium)
                    .foregroundStyle(AppTheme.Palette.textPrimary)
                HStack(spacing: 6) {
                    if let className = child.className, !className.isEmpty {
                        Text(className)
                            .font(.appLabelSmall)
                            .foregroundStyle(AppTheme.Palette.textSecondary)
                    }
                    if let section = child.sectionName, !section.isEmpty {
                        Text("· \(section)")
                            .font(.appLabelSmall)
                            .foregroundStyle(AppTheme.Palette.textSecondary)
                    }
                    if let admission = child.admissionNo, !admission.isEmpty {
                        Text("· \(admission)")
                            .font(.appLabelSmall)
                            .foregroundStyle(AppTheme.Palette.textSecondary)
                    }
                }
            }

            Spacer()

            Image(systemName: "chevron.right")
                .foregroundStyle(AppTheme.Palette.textSecondary)
        }
        .padding(12)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 12))
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(AppTheme.Palette.surfaceVariant, lineWidth: 1)
        )
    }

    private func initials(for name: String) -> String {
        let parts = name
            .split(separator: " ")
            .prefix(2)
            .compactMap { $0.first.map(String.init) }
        return parts.joined().uppercased()
    }
}

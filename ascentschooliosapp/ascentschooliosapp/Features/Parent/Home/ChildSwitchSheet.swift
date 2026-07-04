import SwiftUI

// Phase 40 (Android parity): bottom sheet listing every linked child with a
// checkmark on the currently-active one. Selecting a different child triggers
// `onSelect(linkId)` — the caller re-issues the token via /select-child, saves
// the new context, and bumps ParentHomeView's childEpoch so every tab reloads
// with the newly-selected child's data.
struct ChildSwitchSheet: View {
    let children: [ChildDto]
    let currentStudentId: Int64?
    let onSelect: (Int) -> Void
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        NavigationStack {
            List {
                Section {
                    ForEach(children) { child in
                        row(for: child)
                    }
                } header: {
                    Text("Choose a child")
                        .font(.appLabelMedium)
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                }
            }
            .listStyle(.insetGrouped)
            .navigationTitle("Switch Child")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Done") { dismiss() }
                }
            }
        }
        .presentationDetents([.medium, .large])
        .presentationDragIndicator(.visible)
    }

    private func row(for child: ChildDto) -> some View {
        let isCurrent = currentStudentId != nil && child.studentId == currentStudentId!

        return Button {
            guard !isCurrent else { return }
            onSelect(child.linkId)
            dismiss()
        } label: {
            HStack(spacing: 12) {
                Circle()
                    .fill(AppTheme.Palette.navyContainer)
                    .frame(width: 40, height: 40)
                    .overlay(
                        Text(initials(for: child.studentName))
                            .font(.appLabelLarge)
                            .foregroundStyle(AppTheme.Palette.onNavyContainer)
                    )
                    .accessibilityHidden(true)

                VStack(alignment: .leading, spacing: 2) {
                    Text(child.studentName)
                        .font(.appBodyMedium)
                        .foregroundStyle(AppTheme.Palette.textPrimary)
                    HStack(spacing: 6) {
                        if let className = child.className, !className.isEmpty {
                            Text(className)
                        }
                        if let admission = child.admissionNo, !admission.isEmpty {
                            Text("· \(admission)")
                        }
                    }
                    .font(.appLabelSmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
                }

                Spacer()

                if isCurrent {
                    Image(systemName: "checkmark.circle.fill")
                        .foregroundStyle(AppTheme.Palette.present)
                        .imageScale(.large)
                        .accessibilityLabel("Currently selected")
                }
            }
            .contentShape(Rectangle())
        }
        .buttonStyle(.plain)
        .disabled(isCurrent)
    }

    private func initials(for name: String) -> String {
        let parts = name
            .split(separator: " ")
            .prefix(2)
            .compactMap { $0.first.map(String.init) }
        return parts.joined().uppercased()
    }
}

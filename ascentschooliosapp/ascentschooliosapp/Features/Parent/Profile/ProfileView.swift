import SwiftUI

struct ProfileView: View {
    @State private var viewModel = ProfileViewModel()

    var body: some View {
        ScrollView {
            content
                .padding(.horizontal, 16)
                .padding(.vertical, 14)
                .frame(maxWidth: .infinity)
        }
        .background(AppTheme.Palette.appBackground)
        .refreshable {
            await viewModel.load()
        }
        .task {
            await viewModel.load()
        }
    }

    @ViewBuilder
    private var content: some View {
        switch viewModel.state {
        case .idle, .loading:
            VStack(spacing: 14) {
                LoadingCard(lineCount: 3, height: 200)
                LoadingCard()
                LoadingCard()
            }
        case .success(let profile):
            VStack(spacing: 14) {
                photoHeader(profile)
                infoSection("Personal", rows: personalRows(profile))
                infoSection("Family", rows: familyRows(profile))
                infoSection("Contact", rows: contactRows(profile))
            }
        case .failure(let message):
            ErrorView(message: message) {
                Task { await viewModel.load() }
            }
            .frame(minHeight: 280)
        }
    }

    // MARK: - Photo header

    private func photoHeader(_ profile: StudentProfileDto) -> some View {
        VStack(spacing: 12) {
            photo(profile)
                .frame(width: 110, height: 110)
                .clipShape(Circle())
                .overlay(
                    Circle().stroke(AppTheme.Palette.navyBlue.opacity(0.2), lineWidth: 2)
                )
                .shadow(color: .black.opacity(0.08), radius: 4, y: 2)

            VStack(spacing: 2) {
                Text(profile.fullName ?? "—")
                    .font(.appHeadlineSmall)
                    .foregroundStyle(AppTheme.Palette.textPrimary)

                HStack(spacing: 6) {
                    if let className = profile.className, !className.isEmpty {
                        Text(className)
                    }
                    if let section = profile.sectionName, !section.isEmpty {
                        Text("· \(section)")
                    }
                    if let admission = profile.admissionNo, !admission.isEmpty {
                        Text("· \(admission)")
                    }
                }
                .font(.appLabelSmall)
                .foregroundStyle(AppTheme.Palette.textSecondary)

                if let year = profile.academicYear, !year.isEmpty {
                    Text(year)
                        .font(.appLabelSmall)
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                }
            }
        }
        .frame(maxWidth: .infinity)
        .padding(20)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 18))
    }

    @ViewBuilder
    private func photo(_ profile: StudentProfileDto) -> some View {
        if let url = AppInfo.absoluteURL(forPath: profile.photoPath) {
            AsyncImage(url: url) { phase in
                switch phase {
                case .empty:
                    photoPlaceholder(profile)
                case .success(let image):
                    image
                        .resizable()
                        .aspectRatio(contentMode: .fill)
                case .failure:
                    photoPlaceholder(profile)
                @unknown default:
                    photoPlaceholder(profile)
                }
            }
        } else {
            photoPlaceholder(profile)
        }
    }

    private func photoPlaceholder(_ profile: StudentProfileDto) -> some View {
        ZStack {
            AppTheme.Palette.navyContainer
            Text(initials(for: profile.fullName ?? ""))
                .font(.appHeadlineSmall)
                .foregroundStyle(AppTheme.Palette.onNavyContainer)
        }
    }

    // MARK: - Info sections

    private func infoSection(_ title: String, rows: [(String, String?)]) -> some View {
        let visible = rows.filter { $0.1?.isEmpty == false }
        return Group {
            if !visible.isEmpty {
                VStack(alignment: .leading, spacing: 8) {
                    Text(title)
                        .font(.appLabelLarge)
                        .foregroundStyle(AppTheme.Palette.navyBlue)

                    VStack(spacing: 0) {
                        ForEach(Array(visible.enumerated()), id: \.offset) { idx, pair in
                            infoRow(label: pair.0, value: pair.1 ?? "—")
                            if idx < visible.count - 1 {
                                Divider().padding(.leading, 14)
                            }
                        }
                    }
                    .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 14))
                }
            }
        }
    }

    private func infoRow(label: String, value: String) -> some View {
        HStack(alignment: .firstTextBaseline) {
            Text(label)
                .font(.appLabelMedium)
                .foregroundStyle(AppTheme.Palette.textSecondary)
                .frame(width: 110, alignment: .leading)

            Text(value)
                .font(.appBodyMedium)
                .foregroundStyle(AppTheme.Palette.textPrimary)
                .frame(maxWidth: .infinity, alignment: .leading)
                .multilineTextAlignment(.leading)
        }
        .padding(.horizontal, 14)
        .padding(.vertical, 10)
    }

    // MARK: - Row builders

    private func personalRows(_ p: StudentProfileDto) -> [(String, String?)] {
        [
            ("Date of birth", p.dateOfBirth),
            ("Gender", p.gender),
            ("Blood group", p.bloodGroup),
        ]
    }

    private func familyRows(_ p: StudentProfileDto) -> [(String, String?)] {
        [
            ("Father", p.fatherName),
            ("Mother", p.motherName),
        ]
    }

    private func contactRows(_ p: StudentProfileDto) -> [(String, String?)] {
        [
            ("Mobile", p.mobile),
            ("Email", p.email),
            ("Address", p.address),
        ]
    }

    private func initials(for name: String) -> String {
        let parts = name
            .split(separator: " ")
            .prefix(2)
            .compactMap { $0.first.map(String.init) }
        return parts.joined().uppercased()
    }
}

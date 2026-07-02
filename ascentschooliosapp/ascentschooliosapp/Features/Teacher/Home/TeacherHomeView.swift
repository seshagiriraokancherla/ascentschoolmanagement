import SwiftUI

struct TeacherHomeView: View {
    @State private var viewModel = TeacherHomeViewModel()
    private var store: KeychainTokenStore { KeychainTokenStore.shared }

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 18) {
                    teacherCard
                    classSectionCard
                    actionCards
                }
                .padding(.horizontal, 16)
                .padding(.vertical, 14)
            }
            .background(AppTheme.Palette.appBackground)
            .toolbar {
                BrandedTopBar(title: AppInfo.displayName, onLogout: logout)
            }
            .toolbarBackground(AppTheme.Palette.navyBlue, for: .navigationBar)
            .toolbarBackground(.visible, for: .navigationBar)
            .toolbarColorScheme(.dark, for: .navigationBar)
            .task {
                if viewModel.classes.isEmpty {
                    await viewModel.loadClasses()
                }
            }
            .refreshable {
                await viewModel.loadClasses()
            }
        }
    }

    // MARK: - Pieces

    private var teacherCard: some View {
        HStack(spacing: 14) {
            Circle()
                .fill(AppTheme.Palette.navyContainer)
                .frame(width: 56, height: 56)
                .overlay(
                    Image(systemName: "person.fill")
                        .font(.system(size: 26))
                        .foregroundStyle(AppTheme.Palette.onNavyContainer)
                )

            VStack(alignment: .leading, spacing: 2) {
                Text("Welcome back")
                    .font(.appLabelMedium)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
                Text(store.studentName ?? "Teacher")
                    .font(.appHeadlineSmall)
                    .foregroundStyle(AppTheme.Palette.textPrimary)
            }
            Spacer()
        }
        .padding(16)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 16))
    }

    private var classSectionCard: some View {
        VStack(alignment: .leading, spacing: 14) {
            Text("Choose a class and section")
                .font(.appLabelLarge)
                .foregroundStyle(AppTheme.Palette.navyBlue)

            pickerRow(
                title: "Class",
                isLoading: viewModel.isLoadingClasses,
                error: viewModel.classesError,
                retry: { Task { await viewModel.loadClasses() } }
            ) {
                classPicker
            }

            if viewModel.selectedClassId != nil {
                pickerRow(
                    title: "Section",
                    isLoading: viewModel.isLoadingSections,
                    error: viewModel.sectionsError,
                    retry: { Task { await viewModel.loadSections() } }
                ) {
                    sectionPicker
                }
            }
        }
        .padding(16)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 16))
    }

    @ViewBuilder
    private func pickerRow<Content: View>(
        title: String,
        isLoading: Bool,
        error: String?,
        retry: @escaping () -> Void,
        @ViewBuilder content: () -> Content
    ) -> some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(title)
                .font(.appLabelSmall)
                .foregroundStyle(AppTheme.Palette.textSecondary)

            if isLoading {
                HStack {
                    ProgressView().progressViewStyle(.circular)
                    Text("Loading…")
                        .font(.appLabelMedium)
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                }
                .padding(.vertical, 8)
            } else if let error {
                HStack(spacing: 8) {
                    Image(systemName: "exclamationmark.triangle.fill")
                        .foregroundStyle(AppTheme.Palette.absent)
                    Text(error)
                        .font(.appLabelSmall)
                        .foregroundStyle(AppTheme.Palette.absent)
                        .lineLimit(2)
                    Spacer()
                    Button("Retry", action: retry)
                        .font(.appLabelSmall.bold())
                        .tint(AppTheme.Palette.navyBlue)
                }
                .padding(.vertical, 6)
            } else {
                content()
            }
        }
    }

    private var classPicker: some View {
        Menu {
            ForEach(viewModel.classes) { c in
                Button(c.className) {
                    viewModel.selectedClassId = c.classId
                    Task { await viewModel.loadSections() }
                }
            }
        } label: {
            pickerLabel(text: viewModel.selectedClassName ?? "Select class")
        }
    }

    private var sectionPicker: some View {
        Menu {
            ForEach(viewModel.sections) { s in
                Button(s.sectionName) {
                    viewModel.selectedSectionId = s.sectionId
                }
            }
        } label: {
            pickerLabel(text: viewModel.selectedSectionName ?? "Select section")
        }
    }

    private func pickerLabel(text: String) -> some View {
        HStack {
            Text(text)
                .font(.appBodyMedium)
                .foregroundStyle(AppTheme.Palette.textPrimary)
            Spacer()
            Image(systemName: "chevron.up.chevron.down")
                .foregroundStyle(AppTheme.Palette.textSecondary)
        }
        .padding(.vertical, 12)
        .padding(.horizontal, 14)
        .background(AppTheme.Palette.appBackground, in: RoundedRectangle(cornerRadius: 10))
        .overlay(
            RoundedRectangle(cornerRadius: 10)
                .stroke(AppTheme.Palette.surfaceVariant, lineWidth: 1)
        )
    }

    private var actionCards: some View {
        VStack(spacing: 12) {
            actionCard(
                icon: "checklist",
                title: "Mark Attendance",
                subtitle: "Record today's roll-call",
                destination: { classId, sectionId in
                    TeacherAttendanceView(classId: classId, sectionId: sectionId)
                }
            )
            actionCard(
                icon: "book.closed",
                title: "Set Homework",
                subtitle: "Post or review assignments",
                destination: { classId, _ in
                    TeacherHomeworkView(classId: classId)
                }
            )
        }
    }

    @ViewBuilder
    private func actionCard<Destination: View>(
        icon: String,
        title: String,
        subtitle: String,
        @ViewBuilder destination: (Int, Int) -> Destination
    ) -> some View {
        if viewModel.isReadyForAction,
           let classId = viewModel.selectedClassId,
           let sectionId = viewModel.selectedSectionId {
            NavigationLink {
                destination(classId, sectionId)
            } label: {
                actionCardBody(icon: icon, title: title, subtitle: subtitle, enabled: true)
            }
        } else {
            actionCardBody(icon: icon, title: title, subtitle: subtitle, enabled: false)
        }
    }

    private func actionCardBody(icon: String, title: String, subtitle: String, enabled: Bool) -> some View {
        HStack(spacing: 14) {
            Image(systemName: icon)
                .font(.system(size: 26))
                .foregroundStyle(enabled ? AppTheme.Palette.navyBlue : AppTheme.Palette.textSecondary.opacity(0.5))
                .frame(width: 44, height: 44)
                .background(AppTheme.Palette.navyContainer.opacity(enabled ? 1 : 0.4), in: Circle())

            VStack(alignment: .leading, spacing: 2) {
                Text(title)
                    .font(.appTitleMedium)
                    .foregroundStyle(enabled ? AppTheme.Palette.textPrimary : AppTheme.Palette.textSecondary)
                Text(enabled ? subtitle : "Pick a class and section first")
                    .font(.appLabelSmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
            }
            Spacer()
            Image(systemName: "chevron.right")
                .foregroundStyle(AppTheme.Palette.textSecondary)
        }
        .padding(16)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 16))
        .opacity(enabled ? 1 : 0.65)
    }

    private func logout() {
        Task {
            try? await APIClient.shared.logoutTeacher()
            KeychainTokenStore.shared.clear()
            CookiePersistence.clear()
        }
    }
}

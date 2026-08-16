import SwiftUI

// Phase 90 (Android parity): teacher posts class/section announcements.
// Mirrors TeacherHomeworkView (History | Create) with an added optional
// "Target section" picker (defaults to whole class).
struct TeacherAnnouncementView: View {
    @State private var viewModel: TeacherAnnouncementViewModel

    init(classId: Int) {
        _viewModel = State(initialValue: TeacherAnnouncementViewModel(classId: classId))
    }

    var body: some View {
        VStack(spacing: 0) {
            tabPicker

            Group {
                switch viewModel.selectedTab {
                case .history: historyContent
                case .create:  createContent
                }
            }
        }
        .navigationTitle("Announcements")
        .navigationBarTitleDisplayMode(.inline)
        .toolbarBackground(AppTheme.Palette.navyBlue, for: .navigationBar)
        .toolbarBackground(.visible, for: .navigationBar)
        .toolbarColorScheme(.dark, for: .navigationBar)
        .task {
            if case .idle = viewModel.historyState {
                await viewModel.loadHistory()
            }
            if viewModel.sections.isEmpty {
                await viewModel.loadSections()
            }
        }
        .alert("Announcement posted", isPresented: createSuccessBinding) {
            Button("OK", role: .cancel) { viewModel.dismissCreateSuccess() }
        } message: {
            Text("Parents will see it in their notices feed.")
        }
        .alert("Couldn't post", isPresented: createErrorBinding) {
            Button("OK", role: .cancel) { viewModel.dismissCreateError() }
        } message: {
            Text(viewModel.createError ?? "")
        }
    }

    // MARK: - Tabs

    private var tabPicker: some View {
        Picker("", selection: $viewModel.selectedTab) {
            Text("History").tag(TeacherAnnouncementViewModel.Tab.history)
            Text("Create").tag(TeacherAnnouncementViewModel.Tab.create)
        }
        .pickerStyle(.segmented)
        .padding(.horizontal, 14)
        .padding(.vertical, 10)
        .background(AppTheme.Palette.navyBlue)
    }

    // MARK: - History

    @ViewBuilder
    private var historyContent: some View {
        ScrollView {
            VStack(spacing: 12) {
                switch viewModel.historyState {
                case .idle, .loading:
                    LoadingCard()
                    LoadingCard()
                    LoadingCard()
                case .success(let items):
                    if items.isEmpty {
                        EmptyState(
                            systemImage: "megaphone",
                            title: "No announcements yet",
                            message: "Switch to Create to post a notice to your class."
                        )
                        .frame(minHeight: 280)
                    } else {
                        ForEach(items) { item in
                            historyCard(item)
                        }
                    }
                case .failure(let message):
                    ErrorView(message: message) {
                        Task { await viewModel.loadHistory() }
                    }
                    .frame(minHeight: 240)
                }
            }
            .padding(.horizontal, 16)
            .padding(.vertical, 14)
        }
        .background(AppTheme.Palette.appBackground)
        .refreshable {
            await viewModel.loadHistory()
        }
    }

    private func historyCard(_ item: TeacherAnnouncementDto) -> some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                // Target scope: a section name if targeted, else "Whole class".
                Text(item.sectionName.map { "Section \($0)" } ?? "Whole class")
                    .font(.appLabelSmall.bold())
                    .foregroundStyle(AppTheme.Palette.onNavyContainer)
                    .padding(.horizontal, 8)
                    .padding(.vertical, 3)
                    .background(AppTheme.Palette.navyContainer, in: Capsule())
                Spacer()
                if item.isPinned == true {
                    Label("Pinned", systemImage: "pin.fill")
                        .font(.appLabelSmall.bold())
                        .foregroundStyle(AppTheme.Palette.gold)
                }
            }

            if let title = item.title, !title.isEmpty {
                Text(title)
                    .font(.appTitleMedium)
                    .foregroundStyle(AppTheme.Palette.textPrimary)
            }

            if let desc = item.description, !desc.isEmpty {
                Text(desc)
                    .font(.appBodySmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
                    .fixedSize(horizontal: false, vertical: true)
            }

            if let published = item.publishedDate, !published.isEmpty {
                Label(published.friendlyDate(), systemImage: "calendar")
                    .font(.appLabelSmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
            }

            if let url = item.attachmentUrl, !url.isEmpty {
                ExternalLinkButton(
                    title: "View attachment",
                    systemImage: "doc.text",
                    urlString: url
                )
                .padding(.top, 2)
            }
        }
        .padding(14)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 16))
    }

    // MARK: - Create

    private var createContent: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                field("Target section") {
                    Menu {
                        Button("All sections (whole class)") {
                            viewModel.selectedSectionId = nil
                        }
                        ForEach(viewModel.sections) { section in
                            Button(section.sectionName) {
                                viewModel.selectedSectionId = section.sectionId
                            }
                        }
                    } label: {
                        HStack {
                            Text(viewModel.selectedSectionName)
                                .font(.appBodyMedium)
                                .foregroundStyle(AppTheme.Palette.textPrimary)
                            Spacer()
                            Image(systemName: "chevron.up.chevron.down")
                                .foregroundStyle(AppTheme.Palette.textSecondary)
                        }
                        .padding(.vertical, 12)
                        .padding(.horizontal, 12)
                        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 10))
                        .overlay(
                            RoundedRectangle(cornerRadius: 10)
                                .stroke(AppTheme.Palette.surfaceVariant, lineWidth: 1)
                        )
                    }
                }

                field("Title") {
                    TextField("e.g. PTM on Friday", text: $viewModel.title)
                        .textInputAutocapitalization(.sentences)
                        .padding(.vertical, 10)
                        .padding(.horizontal, 12)
                        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 10))
                        .overlay(
                            RoundedRectangle(cornerRadius: 10)
                                .stroke(AppTheme.Palette.surfaceVariant, lineWidth: 1)
                        )
                }

                field("Message") {
                    TextEditor(text: $viewModel.body)
                        .scrollContentBackground(.hidden)
                        .frame(minHeight: 110)
                        .padding(8)
                        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 10))
                        .overlay(
                            RoundedRectangle(cornerRadius: 10)
                                .stroke(AppTheme.Palette.surfaceVariant, lineWidth: 1)
                        )
                }

                Button {
                    Task { await viewModel.create() }
                } label: {
                    HStack(spacing: 8) {
                        if viewModel.isCreating {
                            ProgressView().progressViewStyle(.circular).tint(.white)
                        }
                        Text(viewModel.isCreating ? "Posting…" : "Post announcement")
                            .font(.appLabelLarge.bold())
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
                }
                .buttonStyle(.borderedProminent)
                .tint(AppTheme.Palette.navyBlue)
                .disabled(viewModel.isCreating || viewModel.title.trimmingCharacters(in: .whitespaces).isEmpty)
            }
            .padding(.horizontal, 16)
            .padding(.vertical, 18)
        }
        .background(AppTheme.Palette.appBackground)
    }

    private func field<Content: View>(_ label: String, @ViewBuilder content: () -> Content) -> some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(label)
                .font(.appLabelSmall)
                .foregroundStyle(AppTheme.Palette.textSecondary)
            content()
        }
    }

    // MARK: - Bindings

    private var createSuccessBinding: Binding<Bool> {
        Binding(get: { viewModel.createSuccess }, set: { if !$0 { viewModel.dismissCreateSuccess() } })
    }

    private var createErrorBinding: Binding<Bool> {
        Binding(get: { viewModel.createError != nil }, set: { if !$0 { viewModel.dismissCreateError() } })
    }
}

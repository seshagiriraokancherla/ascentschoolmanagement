import SwiftUI

struct TeacherHomeworkView: View {
    @State private var viewModel: TeacherHomeworkViewModel

    init(classId: Int) {
        _viewModel = State(initialValue: TeacherHomeworkViewModel(classId: classId))
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
        .navigationTitle("Homework")
        .navigationBarTitleDisplayMode(.inline)
        .toolbarBackground(AppTheme.Palette.navyBlue, for: .navigationBar)
        .toolbarBackground(.visible, for: .navigationBar)
        .toolbarColorScheme(.dark, for: .navigationBar)
        .task {
            if case .idle = viewModel.historyState {
                await viewModel.loadHistory()
            }
        }
        .alert("Homework posted", isPresented: createSuccessBinding) {
            Button("OK", role: .cancel) { viewModel.dismissCreateSuccess() }
        } message: {
            Text("Students will see it in their feed.")
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
            Text("History").tag(TeacherHomeworkViewModel.Tab.history)
            Text("Create").tag(TeacherHomeworkViewModel.Tab.create)
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
                            systemImage: "tray",
                            title: "No homework yet",
                            message: "Switch to Create to post a new assignment."
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

    private func historyCard(_ item: TeacherHomeworkDto) -> some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                if let subject = item.subjectName, !subject.isEmpty {
                    Text(subject.uppercased())
                        .font(.appLabelSmall.bold())
                        .foregroundStyle(AppTheme.Palette.navyBlue)
                }
                Spacer()
                if let due = item.dueDate, !due.isEmpty {
                    Text("Due \(due.friendlyDate())")
                        .font(.appLabelSmall.bold())
                        .foregroundStyle(.white)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 3)
                        .background(AppTheme.Palette.navyBlue, in: Capsule())
                }
            }

            Text(item.title)
                .font(.appTitleMedium)
                .foregroundStyle(AppTheme.Palette.textPrimary)

            if let desc = item.description, !desc.isEmpty {
                Text(desc)
                    .font(.appBodySmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
                    .fixedSize(horizontal: false, vertical: true)
            }

            HStack(spacing: 14) {
                if let assigned = item.assignedDate, !assigned.isEmpty {
                    Label(assigned.friendlyDate(), systemImage: "calendar")
                        .font(.appLabelSmall)
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                }
                if let creator = item.createdBy, !creator.isEmpty {
                    Label(creator, systemImage: "person")
                        .font(.appLabelSmall)
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                }
                Spacer()
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
                field("Title") {
                    TextField("e.g. Chapter 4 — exercises 1–10", text: $viewModel.title)
                        .textInputAutocapitalization(.sentences)
                        .padding(.vertical, 10)
                        .padding(.horizontal, 12)
                        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 10))
                        .overlay(
                            RoundedRectangle(cornerRadius: 10)
                                .stroke(AppTheme.Palette.surfaceVariant, lineWidth: 1)
                        )
                }

                field("Description") {
                    TextEditor(text: $viewModel.description)
                        .scrollContentBackground(.hidden)
                        .frame(minHeight: 110)
                        .padding(8)
                        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 10))
                        .overlay(
                            RoundedRectangle(cornerRadius: 10)
                                .stroke(AppTheme.Palette.surfaceVariant, lineWidth: 1)
                        )
                }

                field("Assigned date") {
                    DatePicker(
                        "",
                        selection: $viewModel.assignedDate,
                        displayedComponents: .date
                    )
                    .labelsHidden()
                    .datePickerStyle(.compact)
                }

                field("Due date") {
                    DatePicker(
                        "",
                        selection: $viewModel.dueDate,
                        in: viewModel.assignedDate...,
                        displayedComponents: .date
                    )
                    .labelsHidden()
                    .datePickerStyle(.compact)
                }

                Button {
                    Task { await viewModel.create() }
                } label: {
                    HStack(spacing: 8) {
                        if viewModel.isCreating {
                            ProgressView().progressViewStyle(.circular).tint(.white)
                        }
                        Text(viewModel.isCreating ? "Posting…" : "Post homework")
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

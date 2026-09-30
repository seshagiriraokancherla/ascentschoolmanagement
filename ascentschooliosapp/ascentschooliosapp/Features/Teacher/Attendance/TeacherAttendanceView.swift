import SwiftUI

struct TeacherAttendanceView: View {
    @State private var viewModel: TeacherAttendanceViewModel

    init(classId: Int, sectionId: Int) {
        _viewModel = State(initialValue: TeacherAttendanceViewModel(classId: classId, sectionId: sectionId))
    }

    var body: some View {
        VStack(spacing: 0) {
            controlBar

            ZStack(alignment: .bottom) {
                ScrollView {
                    content
                        .padding(.horizontal, 16)
                        .padding(.top, 14)
                        .padding(.bottom, viewModel.hasAnyStatusSet ? 96 : 14)
                        .frame(maxWidth: .infinity)
                }
                .background(AppTheme.Palette.appBackground)
                .refreshable {
                    await viewModel.load()
                }

                if viewModel.hasAnyStatusSet {
                    saveBar
                }
            }
        }
        .task(id: viewModel.dateString) {
            await viewModel.load()
        }
        .navigationTitle("Attendance")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Button("Mark all present") {
                    viewModel.markAllPresent()
                }
                .tint(.white)
                .disabled(viewModel.students.isEmpty)
            }
        }
        .toolbarBackground(AppTheme.Palette.navyBlue, for: .navigationBar)
        .toolbarBackground(.visible, for: .navigationBar)
        .toolbarColorScheme(.dark, for: .navigationBar)
        .alert("Saved", isPresented: saveSuccessBinding) {
            Button("OK", role: .cancel) { viewModel.dismissSaveSuccess() }
        } message: {
            Text("Attendance for \(viewModel.dateString) saved.")
        }
        .alert("Couldn't save", isPresented: saveErrorBinding) {
            Button("OK", role: .cancel) { viewModel.dismissSaveError() }
        } message: {
            Text(viewModel.saveError ?? "")
        }
        .overlay {
            if viewModel.isSaving {
                ZStack {
                    Color.black.opacity(0.15).ignoresSafeArea()
                    VStack(spacing: 10) {
                        ProgressView().progressViewStyle(.circular).tint(AppTheme.Palette.navyBlue)
                        Text("Saving…")
                            .font(.appLabelMedium)
                            .foregroundStyle(AppTheme.Palette.textPrimary)
                    }
                    .padding(20)
                    .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 14))
                }
            }
        }
    }

    // MARK: - Top control bar

    private var controlBar: some View {
        HStack(spacing: 12) {
            DatePicker(
                "",
                selection: $viewModel.date,
                in: ...Date(),
                displayedComponents: .date
            )
            .labelsHidden()
            .tint(.white)
            .colorScheme(.dark)

            Spacer()

            if viewModel.isMarked {
                Label("Marked", systemImage: "checkmark.seal.fill")
                    .font(.appLabelSmall.bold())
                    .foregroundStyle(.white)
                    .padding(.horizontal, 8)
                    .padding(.vertical, 4)
                    .background(.white.opacity(0.15), in: Capsule())
            }
        }
        .padding(.horizontal, 14)
        .padding(.vertical, 10)
        .background(AppTheme.Palette.navyBlue)
    }

    // MARK: - Content

    @ViewBuilder
    private var content: some View {
        if viewModel.isLoading && viewModel.students.isEmpty {
            VStack(spacing: 12) {
                LoadingCard()
                LoadingCard()
                LoadingCard()
            }
        } else if let error = viewModel.loadError, viewModel.students.isEmpty {
            ErrorView(message: error) {
                Task { await viewModel.load() }
            }
            .frame(minHeight: 280)
        } else if viewModel.students.isEmpty {
            EmptyState(
                systemImage: "person.3",
                title: "No students",
                message: "No students assigned to this class & section yet."
            )
            .frame(minHeight: 280)
        } else {
            VStack(spacing: 12) {
                summaryChips
                studentList
            }
        }
    }

    private var summaryChips: some View {
        HStack(spacing: 6) {
            chip("Present", count: viewModel.presentCount, color: AppTheme.Palette.present)
            chip("Absent",  count: viewModel.absentCount,  color: AppTheme.Palette.absent)
            chip("Late",    count: viewModel.lateCount,    color: AppTheme.Palette.late)
            // Phase 73: Half Day count (settable via long-press menu).
            chip("Half Day", count: viewModel.halfDayCount, color: AppTheme.Palette.halfDay)
            chip("Unmarked", count: viewModel.unmarkedCount, color: AppTheme.Palette.textSecondary)
        }
    }

    private func chip(_ label: String, count: Int, color: Color) -> some View {
        VStack(spacing: 2) {
            Text("\(count)")
                .font(.appTitleMedium.bold())
                .foregroundStyle(color)
            Text(label)
                .font(.appLabelSmall)
                .foregroundStyle(AppTheme.Palette.textSecondary)
        }
        .frame(maxWidth: .infinity)
        .padding(.vertical, 10)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 12))
    }

    private var studentList: some View {
        VStack(spacing: 8) {
            ForEach(Array(viewModel.students.enumerated()), id: \.element.id) { index, student in
                studentRow(at: index, student: student)
            }
        }
    }

    private func studentRow(at index: Int, student: TeacherAttendanceStudentDto) -> some View {
        Button {
            viewModel.cycleStatus(at: index)
        } label: {
            HStack(spacing: 12) {
                Circle()
                    .fill(AppTheme.Palette.navyContainer)
                    .frame(width: 36, height: 36)
                    .overlay(
                        Text(initials(for: student.studentName))
                            .font(.appLabelMedium.bold())
                            .foregroundStyle(AppTheme.Palette.onNavyContainer)
                    )

                VStack(alignment: .leading, spacing: 2) {
                    Text(student.studentName)
                        .font(.appBodyMedium)
                        .foregroundStyle(AppTheme.Palette.textPrimary)
                    if let admission = student.admissionNo, !admission.isEmpty {
                        Text(admission)
                            .font(.appLabelSmall)
                            .foregroundStyle(AppTheme.Palette.textSecondary)
                    }
                }

                Spacer()

                statusBadge(student.status)
            }
            .padding(12)
            .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 12))
        }
        .buttonStyle(.plain)
        .contextMenu {
            Button { viewModel.setStatus(at: index, status: "Present") } label: {
                Label("Present", systemImage: "checkmark.circle")
            }
            Button { viewModel.setStatus(at: index, status: "Absent") } label: {
                Label("Absent", systemImage: "xmark.circle")
            }
            Button { viewModel.setStatus(at: index, status: "Late") } label: {
                Label("Late", systemImage: "clock")
            }
            Button { viewModel.setStatus(at: index, status: "HalfDay") } label: {
                Label("Half day", systemImage: "sun.haze")
            }
        }
    }

    @ViewBuilder
    private func statusBadge(_ status: String?) -> some View {
        let s = (status ?? "").capitalized
        if s.isEmpty {
            Text("Tap")
                .font(.appLabelSmall.bold())
                .foregroundStyle(AppTheme.Palette.textSecondary)
                .padding(.horizontal, 10)
                .padding(.vertical, 4)
                .background(AppTheme.Palette.surfaceVariant, in: Capsule())
        } else {
            Text(s)
                .font(.appLabelSmall.bold())
                .foregroundStyle(.white)
                .padding(.horizontal, 10)
                .padding(.vertical, 4)
                .background(AppTheme.color(forStatus: status), in: Capsule())
        }
    }

    // MARK: - Save bar

    private var saveBar: some View {
        HStack {
            VStack(alignment: .leading, spacing: 2) {
                Text("\(viewModel.students.count - viewModel.unmarkedCount) of \(viewModel.students.count) marked")
                    .font(.appLabelSmall)
                    .foregroundStyle(.white.opacity(0.85))
                Text("for \(viewModel.dateString)")
                    .font(.appLabelSmall)
                    .foregroundStyle(.white.opacity(0.7))
            }
            Spacer()
            Button {
                Task { await viewModel.save() }
            } label: {
                HStack(spacing: 6) {
                    if viewModel.isSaving {
                        ProgressView().progressViewStyle(.circular).tint(.white)
                    }
                    Text(viewModel.isSaving ? "Saving…" : "Save")
                        .font(.appLabelLarge.bold())
                }
                .padding(.horizontal, 22)
                .padding(.vertical, 10)
                .background(AppTheme.Palette.gold, in: Capsule())
                .foregroundStyle(.white)
            }
            .disabled(viewModel.isSaving)
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 14)
        .background(AppTheme.Palette.navyBlue)
        .clipShape(UnevenRoundedRectangle(topLeadingRadius: 18, topTrailingRadius: 18))
        .shadow(color: .black.opacity(0.2), radius: 10, y: -4)
    }

    // MARK: - Helpers

    private func initials(for name: String) -> String {
        name.split(separator: " ").prefix(2).compactMap { $0.first.map(String.init) }.joined().uppercased()
    }

    private var saveSuccessBinding: Binding<Bool> {
        Binding(get: { viewModel.saveSuccess }, set: { if !$0 { viewModel.dismissSaveSuccess() } })
    }

    private var saveErrorBinding: Binding<Bool> {
        Binding(get: { viewModel.saveError != nil }, set: { if !$0 { viewModel.dismissSaveError() } })
    }
}

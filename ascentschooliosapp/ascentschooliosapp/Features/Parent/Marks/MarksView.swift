import SwiftUI

struct MarksView: View {
    @State private var viewModel = MarksViewModel()

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
                LoadingCard()
                LoadingCard()
            }
        case .success(let results):
            if results.isEmpty {
                EmptyState(
                    systemImage: "doc.text.magnifyingglass",
                    title: "No marks yet",
                    message: "Exam results will appear here once teachers publish them."
                )
                .frame(minHeight: 280)
            } else {
                VStack(spacing: 16) {
                    ForEach(results) { yearResult in
                        yearSection(yearResult)
                    }
                }
            }
        case .failure(let message):
            ErrorView(message: message) {
                Task { await viewModel.load() }
            }
            .frame(minHeight: 240)
        }
    }

    // MARK: - Sections

    private func yearSection(_ result: MarksResultDto) -> some View {
        VStack(alignment: .leading, spacing: 10) {
            Text(result.academicYear ?? "Academic Year")
                .font(.appLabelLarge)
                .foregroundStyle(AppTheme.Palette.navyBlue)

            if result.exams.isEmpty {
                EmptyState(
                    systemImage: "tray",
                    title: "No exams yet",
                    message: nil
                )
                .frame(height: 140)
                .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 16))
            } else {
                VStack(spacing: 12) {
                    ForEach(result.exams) { exam in
                        examCard(exam)
                    }
                }
            }
        }
    }

    private func examCard(_ exam: ExamResultDto) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack(alignment: .firstTextBaseline) {
                Text(exam.examName ?? "Exam")
                    .font(.appTitleMedium)
                    .foregroundStyle(AppTheme.Palette.textPrimary)
                Spacer()
                if let percent = exam.percentage {
                    Text(String(format: "%.1f%%", percent))
                        .font(.appLabelLarge)
                        .foregroundStyle(.white)
                        .padding(.horizontal, 10)
                        .padding(.vertical, 4)
                        .background(badgeColor(forPercent: percent), in: Capsule())
                }
            }

            if let obtained = exam.obtainedMarks, let total = exam.totalMarks {
                Text("\(formatNumber(obtained)) / \(formatNumber(total))")
                    .font(.appLabelSmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
            }

            VStack(spacing: 8) {
                ForEach(exam.subjects) { subject in
                    subjectRow(subject)
                }
            }
        }
        .padding(14)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 16))
    }

    private func subjectRow(_ subject: SubjectMarkDto) -> some View {
        let max = subject.maxMarks ?? 0
        let obtained = subject.obtainedMarks ?? 0
        let ratio = max > 0 ? CGFloat(obtained / max) : 0
        let clamped = min(1, Swift.max(0, ratio))
        let isAbsent = subject.isAbsent ?? false

        return VStack(alignment: .leading, spacing: 6) {
            HStack {
                Text(subject.subjectName ?? "Subject")
                    .font(.appBodyMedium)
                    .foregroundStyle(AppTheme.Palette.textPrimary)
                Spacer()
                if isAbsent {
                    Text("Absent")
                        .font(.appLabelSmall.bold())
                        .foregroundStyle(.white)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 2)
                        .background(AppTheme.Palette.absent, in: Capsule())
                } else {
                    Text("\(formatNumber(obtained)) / \(formatNumber(max))")
                        .font(.appLabelMedium.monospaced())
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                }
            }

            GeometryReader { proxy in
                ZStack(alignment: .leading) {
                    RoundedRectangle(cornerRadius: 4)
                        .fill(AppTheme.Palette.surfaceVariant)
                    RoundedRectangle(cornerRadius: 4)
                        .fill(isAbsent ? AppTheme.Palette.absent : barColor(forRatio: clamped))
                        .frame(width: proxy.size.width * (isAbsent ? 0 : clamped))
                }
            }
            .frame(height: 6)
        }
    }

    // MARK: - Helpers

    private func barColor(forRatio r: CGFloat) -> Color {
        switch r {
        case 0.75...:      return AppTheme.Palette.present
        case 0.40..<0.75:  return AppTheme.Palette.navyBlueLight
        case 0.25..<0.40:  return AppTheme.Palette.late
        default:           return AppTheme.Palette.absent
        }
    }

    private func badgeColor(forPercent p: Double) -> Color {
        switch p {
        case 75...:       return AppTheme.Palette.present
        case 40..<75:     return AppTheme.Palette.navyBlue
        case 25..<40:     return AppTheme.Palette.late
        default:          return AppTheme.Palette.absent
        }
    }

    private func formatNumber(_ value: Double) -> String {
        if value == floor(value) {
            return String(Int(value))
        }
        return String(format: "%.1f", value)
    }
}

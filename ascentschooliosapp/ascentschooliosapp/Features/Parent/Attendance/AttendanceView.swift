import SwiftUI

struct AttendanceView: View {
    @State private var viewModel = AttendanceViewModel()

    var body: some View {
        VStack(spacing: 0) {
            monthBar

            ScrollView {
                content
                    .padding(.horizontal, 16)
                    .padding(.vertical, 14)
                    .frame(maxWidth: .infinity)
            }
            .refreshable {
                await viewModel.load()
            }
            .background(AppTheme.Palette.appBackground)
        }
        .task(id: monthKey) {
            await viewModel.load()
        }
    }

    // Used to retrigger `.task` whenever the user changes month or year.
    private var monthKey: String {
        "\(viewModel.year)-\(viewModel.month)"
    }

    // MARK: - Pieces

    private var monthBar: some View {
        HStack {
            Button {
                viewModel.goToPreviousMonth()
            } label: {
                Image(systemName: "chevron.left")
                    .imageScale(.large)
                    .padding(8)
            }
            .tint(.white)
            .accessibilityLabel("Previous month")

            Spacer()

            Text(viewModel.monthLabel)
                .font(.appTitleMedium)
                .foregroundStyle(.white)
                .accessibilityAddTraits(.isHeader)

            Spacer()

            Button {
                viewModel.goToNextMonth()
            } label: {
                Image(systemName: "chevron.right")
                    .imageScale(.large)
                    .padding(8)
            }
            .tint(.white)
            .accessibilityLabel("Next month")
        }
        .padding(.horizontal, 8)
        .padding(.vertical, 6)
        .background(AppTheme.Palette.navyBlue)
    }

    @ViewBuilder
    private var content: some View {
        switch viewModel.state {
        case .idle, .loading:
            skeleton
        case .success(let summary):
            VStack(spacing: 14) {
                summaryCard(summary)

                if let records = summary.records, !records.isEmpty {
                    recordList(records)
                } else {
                    EmptyState(
                        systemImage: "calendar.badge.exclamationmark",
                        title: "No records",
                        message: "Attendance hasn't been marked for this month yet."
                    )
                    .frame(minHeight: 220)
                }
            }
        case .failure(let message):
            ErrorView(message: message) {
                Task { await viewModel.load() }
            }
            .frame(minHeight: 240)
        }
    }

    private var skeleton: some View {
        VStack(spacing: 14) {
            LoadingCard(lineCount: 2, height: 80)
            LoadingCard()
            LoadingCard()
        }
    }

    private func summaryCard(_ summary: AttendanceSummaryDto) -> some View {
        // Phase 73: 5 status counts (Total / Present / Absent / Late / Half Day) plus
        // an attendance percentage that credits Half Day as 0.5 of a Present day.
        // LazyVGrid re-flows to 2 rows on narrow devices; on standard iPhones the
        // 3-column grid keeps every tile legible without truncation.
        let columns = [GridItem(.flexible()), GridItem(.flexible()), GridItem(.flexible())]

        return VStack(spacing: 12) {
            LazyVGrid(columns: columns, spacing: 12) {
                statTile("Total", value: "\(summary.totalDays)", color: AppTheme.Palette.navyBlue)
                statTile("Present", value: "\(summary.presentDays)", color: AppTheme.Palette.present)
                statTile("Absent", value: "\(summary.absentDays)", color: AppTheme.Palette.absent)
                statTile("Late", value: "\(summary.lateDays)", color: AppTheme.Palette.late)
                statTile("Half Day", value: "\(summary.halfDayDays ?? 0)", color: AppTheme.Palette.halfDay)
                statTile("Attendance", value: String(format: "%.1f%%", summary.attendancePercent), color: AppTheme.Palette.teal)
            }
        }
        .padding(14)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 16))
    }

    private func statTile(_ label: String, value: String, color: Color) -> some View {
        VStack(spacing: 4) {
            Text(value)
                .font(.appHeadlineSmall)
                .foregroundStyle(color)
            Text(label)
                .font(.appLabelSmall)
                .foregroundStyle(AppTheme.Palette.textSecondary)
        }
        .frame(maxWidth: .infinity)
    }

    private func recordList(_ records: [AttendanceRecordDto]) -> some View {
        VStack(spacing: 8) {
            ForEach(records) { record in
                recordRow(record)
            }
        }
    }

    private func recordRow(_ record: AttendanceRecordDto) -> some View {
        HStack {
            Text(formatDate(record.date))
                .font(.appBodyMedium)
                .foregroundStyle(AppTheme.Palette.textPrimary)

            Spacer()

            if let remarks = record.remarks, !remarks.isEmpty {
                Text(remarks)
                    .font(.appLabelSmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
                    .lineLimit(1)
                    .padding(.trailing, 8)
            }

            Text(displayStatus(record.status))
                .font(.appLabelSmall.bold())
                .foregroundStyle(.white)
                .padding(.horizontal, 10)
                .padding(.vertical, 4)
                .background(AppTheme.color(forStatus: record.status), in: Capsule())
        }
        .padding(12)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 12))
    }

    // Server stores "HalfDay" (no space, Android convention) — surface it as
    // "Half Day" so the pill reads naturally. Any other status is title-cased.
    private func displayStatus(_ status: String) -> String {
        switch status.lowercased() {
        case "halfday", "half day", "hd": return "Half Day"
        default: return status.capitalized
        }
    }

    private func formatDate(_ iso: String) -> String {
        let display = DateFormatter()
        display.dateFormat = "d MMM, EEE"

        // Try several common shapes produced by .NET serializers.
        let isoFull = ISO8601DateFormatter()
        isoFull.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        if let date = isoFull.date(from: iso) { return display.string(from: date) }

        let isoNoFraction = ISO8601DateFormatter()
        isoNoFraction.formatOptions = [.withInternetDateTime]
        if let date = isoNoFraction.date(from: iso) { return display.string(from: date) }

        let dateOnly = DateFormatter()
        dateOnly.dateFormat = "yyyy-MM-dd"
        if let date = dateOnly.date(from: iso) { return display.string(from: date) }

        return iso
    }
}

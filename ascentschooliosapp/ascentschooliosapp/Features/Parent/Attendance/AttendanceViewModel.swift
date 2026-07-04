import Foundation
import Observation

@Observable
final class AttendanceViewModel {

    enum State {
        case idle
        case loading
        case success(AttendanceSummaryDto)
        case failure(String)
    }

    var state: State = .idle
    var month: Int
    var year: Int

    init() {
        let now = Date()
        let calendar = Calendar.current
        self.month = calendar.component(.month, from: now)
        self.year = calendar.component(.year, from: now)
    }

    var monthLabel: String {
        var components = DateComponents()
        components.month = month
        components.year = year
        components.day = 1
        let date = Calendar.current.date(from: components) ?? Date()

        let formatter = DateFormatter()
        formatter.dateFormat = "MMMM yyyy"
        return formatter.string(from: date)
    }

    func load() async {
        state = .loading
        do {
            let summary = try await APIClient.shared.studentAttendance(month: month, year: year)
            state = .success(summary)
        } catch {
            let msg = (error as? APIError)?.errorDescription ?? error.localizedDescription
            state = .failure(msg)
        }
    }

    func goToPreviousMonth() {
        if month == 1 {
            month = 12
            year -= 1
        } else {
            month -= 1
        }
    }

    func goToNextMonth() {
        if month == 12 {
            month = 1
            year += 1
        } else {
            month += 1
        }
    }
}

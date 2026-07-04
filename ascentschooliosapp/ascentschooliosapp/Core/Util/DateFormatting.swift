import Foundation

extension String {

    // Parses a date the server emitted (various .NET / ISO 8601 shapes) and
    // returns it formatted via `style`. Falls back to the raw string if nothing
    // matches — better to show "weird" than to show nothing.
    func friendlyDate(style: String = "d MMM yyyy") -> String {
        let display = DateFormatter()
        display.locale = Locale(identifier: "en_US_POSIX")
        display.dateFormat = style

        // 1. ISO 8601 with fractional seconds — e.g. "2026-05-25T18:30:00.0000000"
        let isoFull = ISO8601DateFormatter()
        isoFull.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        if let date = isoFull.date(from: self) { return display.string(from: date) }

        // 2. ISO 8601 without fractions — "2026-05-25T18:30:00"
        let isoNoFraction = ISO8601DateFormatter()
        isoNoFraction.formatOptions = [.withInternetDateTime]
        if let date = isoNoFraction.date(from: self) { return display.string(from: date) }

        // 3. Date-only — "2026-05-25"
        let dateOnly = DateFormatter()
        dateOnly.locale = Locale(identifier: "en_US_POSIX")
        dateOnly.dateFormat = "yyyy-MM-dd"
        if let date = dateOnly.date(from: self) { return display.string(from: date) }

        // 4. ".NET-style" with space — "2026-05-25 18:30:00"
        let dotNet = DateFormatter()
        dotNet.locale = Locale(identifier: "en_US_POSIX")
        dotNet.dateFormat = "yyyy-MM-dd HH:mm:ss"
        if let date = dotNet.date(from: self) { return display.string(from: date) }

        return self
    }
}

import Foundation

// Lightweight prefixed logger that compiles to a no-op in Release builds.
// Use `DebugLogger.log(.network, "…")` to keep logs grouped by area.
enum DebugLogger {

    enum Area: String {
        case network = "NET"
        case auth    = "AUTH"
        case storage = "STORE"
    }

    static func log(_ area: Area, _ message: @autoclosure () -> String) {
        #if DEBUG
        let stamp = Date().formatted(.dateTime.hour().minute().second().secondFraction(.fractional(3)))
        print("[\(stamp)] [\(area.rawValue)] \(message())")
        #endif
    }
}

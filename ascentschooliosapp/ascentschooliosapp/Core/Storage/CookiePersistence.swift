import Foundation

// iOS counterpart of Android's `PersistentCookieJar` (RetrofitClient.kt).
// The 7-day sliding-expiry `parentRefreshToken` / `teacherRefreshToken` is set
// by the server as an HttpOnly cookie. `HTTPCookieStorage.shared` keeps it in
// memory across foreground sessions but does NOT persist HttpOnly cookies past
// app termination — so we archive them to disk on background and restore on
// launch. Without this, a cold start the day after login would force OTP re-auth.
enum CookiePersistence {

    private static var fileURL: URL? {
        guard let supportDir = try? FileManager.default.url(
            for: .applicationSupportDirectory,
            in: .userDomainMask,
            appropriateFor: nil,
            create: true
        ) else { return nil }
        return supportDir.appendingPathComponent("ascent-cookies.plist")
    }

    static func restore() {
        guard let url = fileURL,
              FileManager.default.fileExists(atPath: url.path) else { return }
        do {
            let data = try Data(contentsOf: url)
            let allowed: [AnyClass] = [
                NSArray.self,
                NSDictionary.self,
                NSString.self,
                NSDate.self,
                NSNumber.self,
            ]
            guard let raw = try NSKeyedUnarchiver.unarchivedObject(
                ofClasses: allowed,
                from: data
            ) as? [[String: Any]] else { return }

            let storage = HTTPCookieStorage.shared
            for stringKeyed in raw {
                let properties = Dictionary(uniqueKeysWithValues:
                    stringKeyed.map { (HTTPCookiePropertyKey($0.key), $0.value) }
                )
                if let cookie = HTTPCookie(properties: properties) {
                    storage.setCookie(cookie)
                }
            }
        } catch {
            // Best-effort: if the archive is corrupt the user simply re-authenticates.
        }
    }

    static func persist() {
        guard let url = fileURL,
              let cookies = HTTPCookieStorage.shared.cookies else { return }

        // Convert each cookie's HTTPCookiePropertyKey-keyed dictionary to a plain
        // [String: Any] so the archive is fully Objective-C bridge-compatible.
        let serializable: [[String: Any]] = cookies.compactMap { cookie in
            guard let props = cookie.properties else { return nil }
            return Dictionary(uniqueKeysWithValues: props.map { ($0.key.rawValue, $0.value) })
        }

        do {
            let data = try NSKeyedArchiver.archivedData(
                withRootObject: serializable as NSArray,
                requiringSecureCoding: true
            )
            try data.write(to: url, options: [.atomic, .completeFileProtectionUnlessOpen])
        } catch {
            // Silently ignore — losing the archive forces a single OTP re-auth.
        }
    }

    static func clear() {
        if let url = fileURL {
            try? FileManager.default.removeItem(at: url)
        }
        if let cookies = HTTPCookieStorage.shared.cookies {
            for cookie in cookies {
                HTTPCookieStorage.shared.deleteCookie(cookie)
            }
        }
    }
}

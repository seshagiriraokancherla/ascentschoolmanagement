import Foundation
import Security

// Thin wrapper around `kSecClassGenericPassword` items.
// All entries share the bundle identifier as the service so per-flavor builds
// (in.educare.demo, in.educare.stannsasf, …) have isolated keychain spaces.
enum KeychainHelper {

    private static let service: String = Bundle.main.bundleIdentifier ?? "in.educare.app"

    @discardableResult
    static func save(
        _ value: String,
        for key: String,
        accessibility: CFString = kSecAttrAccessibleAfterFirstUnlock
    ) -> Bool {
        guard let data = value.data(using: .utf8) else { return false }
        return save(data, for: key, accessibility: accessibility)
    }

    @discardableResult
    static func save(
        _ data: Data,
        for key: String,
        accessibility: CFString = kSecAttrAccessibleAfterFirstUnlock
    ) -> Bool {
        // Idempotent: delete any existing entry first, then insert fresh.
        delete(key: key)

        let attributes: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: key,
            kSecValueData as String: data,
            kSecAttrAccessible as String: accessibility,
        ]
        return SecItemAdd(attributes as CFDictionary, nil) == errSecSuccess
    }

    static func readString(_ key: String) -> String? {
        guard let data = readData(key) else { return nil }
        return String(data: data, encoding: .utf8)
    }

    static func readData(_ key: String) -> Data? {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: key,
            kSecReturnData as String: true,
            kSecMatchLimit as String: kSecMatchLimitOne,
        ]
        var result: AnyObject?
        let status = SecItemCopyMatching(query as CFDictionary, &result)
        guard status == errSecSuccess else { return nil }
        return result as? Data
    }

    @discardableResult
    static func delete(key: String) -> Bool {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: key,
        ]
        let status = SecItemDelete(query as CFDictionary)
        return status == errSecSuccess || status == errSecItemNotFound
    }
}

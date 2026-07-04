import Foundation

// Per-install device UUID. Generated on first read and stored in the Keychain
// with `kSecAttrAccessibleAfterFirstUnlock` so it survives:
//   - logout (KeychainTokenStore.clear() doesn't touch this key)
//   - app upgrades
// A fresh install of the same flavor will get a new UUID — that's intentional
// (matches Android's per-install device_id), so the backend treats reinstalls
// as new devices and revokes old refresh tokens.
enum DeviceIDProvider {
    private static let key = "device.id"

    static var deviceId: String {
        if let existing = KeychainHelper.readString(key), !existing.isEmpty {
            return existing
        }
        let generated = UUID().uuidString
        KeychainHelper.save(generated, for: key)
        return generated
    }
}

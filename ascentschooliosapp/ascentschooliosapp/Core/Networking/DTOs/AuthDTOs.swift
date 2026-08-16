import Foundation

// MARK: - SMS OTP (parent)

struct SmsOtpRequest: Encodable {
    let mobile: String
    let deviceId: String
}

struct SmsVerifyOtpRequest: Encodable {
    let mobile: String
    let otp: String
    let deviceId: String
}

// MARK: - Parent credential (legacy PIN flow)

struct ParentRegisterRequest: Encodable {
    let fullName: String
    let mobile: String
    let pin: String
    let email: String?
}

struct ParentLoginRequest: Encodable {
    let identifier: String
    let pin: String
}

// MARK: - Teacher credentials

struct TeacherLoginRequest: Encodable {
    let username: String
    let password: String
}

// MARK: - Child context

struct SelectChildRequest: Encodable {
    let linkId: Int
}

// MARK: - Responses

struct AuthResponse: Decodable {
    let accessToken: String
    // Phase 88 (Android parity): the app now holds its own refresh token and
    // sends it as `X-Refresh-Token` (server reads header-first, cookie fallback).
    // Optional — web clients / older API builds omit it and fall back to the cookie.
    // Phase 96 made it stable (non-rotating), so the value returned by /refresh
    // equals the one sent, but we always re-save defensively.
    let refreshToken: String?
    let tokenType: String?
    let fullName: String?
    let className: String?
    let sectionName: String?
    let admissionNo: String?
    let studentId: Int64?
    let parentId: Int?
    let groupId: Int?
    let schoolId: Int?
    let schoolCode: String?
}

struct TeacherAuthResponse: Decodable {
    let accessToken: String
    let refreshToken: String?   // Phase 88 — see AuthResponse
    let tokenType: String?
    let fullName: String?
    let userId: Int?
    let schoolId: Int?
    let groupId: Int?
}

struct ChildDto: Decodable, Identifiable {
    let linkId: Int
    let studentId: Int64
    let studentName: String
    let className: String?
    let sectionName: String?
    let admissionNo: String?
    let schoolCode: String?
    let isActive: Bool?

    var id: Int { linkId }
}

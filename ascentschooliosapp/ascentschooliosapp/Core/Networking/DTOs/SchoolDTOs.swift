import Foundation

// Phase 44 (Android parity: SchoolByCodeDto) — response to
// GET /mobile/auth/school-by-code?code=<4-digit>. AllowAnonymous — called
// before any login, so no token required. `schoolCode` is the subdomain that
// downstream requests should send as X-School-Code / X-Subdomain.
struct SchoolByCodeDto: Decodable {
    let schoolCode: String
    let name: String?
}

// Phase 44 — public branding endpoint (GET /branding). The generic-flavor
// login screen renders the school's logo + display name once a code has been
// resolved. Optional fields on all colours so a school with only a logo works.
struct BrandingDto: Decodable {
    let displayName: String?
    let tagline: String?
    let logoUrl: String?
    let loginBgPath: String?
    let primaryColor: String?
    let secondaryColor: String?
    let receiptFooterText: String?
}

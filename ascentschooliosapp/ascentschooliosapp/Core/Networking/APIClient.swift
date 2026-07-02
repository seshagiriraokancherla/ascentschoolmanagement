import Foundation

final class APIClient {
    static let shared = APIClient()

    private let session: URLSession
    private let decoder: JSONDecoder
    private let encoder: JSONEncoder
    private let baseURL: String

    // Serialises concurrent 401 retries so we only refresh once even if
    // multiple parallel requests race to unauthorized. Actor-isolated state
    // — reads and writes are safe from any thread.
    private let refreshCoordinator = RefreshCoordinator()

    // Phase iOS-3 (KeychainTokenStore) installs this hook.
    var tokenProvider: (() -> String?)?

    private init() {
        let config = URLSessionConfiguration.default
        config.httpCookieStorage = HTTPCookieStorage.shared
        config.httpCookieAcceptPolicy = .always
        config.httpShouldSetCookies = true
        config.timeoutIntervalForRequest = 30
        config.timeoutIntervalForResource = 60
        config.waitsForConnectivity = true

        self.session = URLSession(configuration: config)
        self.decoder = JSONDecoder()
        self.encoder = JSONEncoder()
        self.baseURL = AppInfo.apiBaseURL
    }

    // MARK: - Public request entry (Phase 65 — 401 auto-refresh-and-retry)
    //
    // Wraps `sendOnce` with the Android-parity keep-alive behaviour: on 401 we
    // fire the appropriate refresh endpoint (parent: refresh + re-select-child;
    // teacher: refresh), then retry the original request exactly once. Multiple
    // concurrent 401s share a single refresh via `RefreshCoordinator`. On
    // refresh failure we clear the session and let RootView route back to auth.
    func send<T: Decodable>(
        _ path: String,
        method: HTTPMethod = .get,
        query: [URLQueryItem] = [],
        body: (any Encodable)? = nil
    ) async throws -> T {
        do {
            return try await sendOnce(path, method: method, query: query, body: body)
        } catch APIError.unauthorized {
            // Auth endpoints (login/refresh/logout/otp/select-child) legitimately
            // return 401 and are also called from inside `performRefresh` — never
            // retry them, otherwise we'd recurse or paper over a bad password.
            guard shouldRetryOn401(path: path) else { throw APIError.unauthorized }

            do {
                try await refreshCoordinator.refresh { [weak self] in
                    try await self?.performRefresh()
                }
            } catch {
                // Refresh cookie expired / network failure / no userType — treat as
                // full logout so RootView (which observes KeychainTokenStore) swaps
                // back to AuthFlow.
                KeychainTokenStore.shared.clear()
                CookiePersistence.clear()
                throw APIError.unauthorized
            }

            return try await sendOnce(path, method: method, query: query, body: body)
        }
    }

    private func sendOnce<T: Decodable>(
        _ path: String,
        method: HTTPMethod = .get,
        query: [URLQueryItem] = [],
        body: (any Encodable)? = nil
    ) async throws -> T {
        let request = try buildRequest(path: path, method: method, query: query, body: body)

        logRequest(request)

        let data: Data
        let response: URLResponse
        do {
            (data, response) = try await session.data(for: request)
        } catch {
            DebugLogger.log(.network, "↯ \(method.rawValue) \(path) — network error: \(error.localizedDescription)")
            throw APIError.network(error)
        }

        guard let http = response as? HTTPURLResponse else { throw APIError.unknown }

        logResponse(http, body: data, for: path)

        // Try envelope decode FIRST — even on 4xx the server often returns a
        // structured `{ success:false, message:"..." }`. We want to surface
        // *that* message ("Invalid username or password.") not a generic
        // "Session expired" mapping. Fall back to HTTP-status errors only
        // when the body isn't a parseable envelope (e.g. a raw 401 from the
        // JWT filter, or an HTML 5xx page).
        do {
            return try decodeEnvelope(data: data, path: path)
        } catch let error as APIError {
            if case .decoding = error {
                if http.statusCode == 401 { throw APIError.unauthorized }
                if !(200...299).contains(http.statusCode) {
                    throw APIError.http(statusCode: http.statusCode, body: data)
                }
            }
            throw error
        }
    }

    private func shouldRetryOn401(path: String) -> Bool {
        // Substring match (paths can be with or without leading "/").
        let lower = path.lowercased()
        let neverRetry = [
            "auth/parent/refresh",
            "auth/parent/login",
            "auth/parent/register",
            "auth/parent/logout",
            "auth/parent/request-otp",
            "auth/parent/verify-otp",
            "auth/parent/select-child",
            "auth/teacher/login",
            "auth/teacher/refresh",
            "auth/teacher/logout",
        ]
        return !neverRetry.contains(where: { lower.contains($0) })
    }

    // Called by the RefreshCoordinator on the first 401. Uses `sendOnce`
    // (not `send`) to bypass the retry logic and avoid infinite recursion.
    private func performRefresh() async throws {
        let store = KeychainTokenStore.shared
        switch store.userType {
        case .parent:
            let auth: AuthResponse = try await sendOnce(
                "mobile/auth/parent/refresh",
                method: .post
            )
            store.updateAccessToken(auth.accessToken)

            // Phase 68 parity: re-establish child context after refresh. The
            // refresh cookie is parent-level and returns a parent-only JWT, so
            // /mobile/student/* would 401 again with "Please select a child
            // first." link_id is stable across promotions.
            if let linkId = store.childLinkId {
                let childAuth: AuthResponse = try await sendOnce(
                    "mobile/auth/parent/select-child",
                    method: .post,
                    body: SelectChildRequest(linkId: linkId)
                )
                store.saveParentAuth(childAuth)
            }
        case .teacher:
            let auth: TeacherAuthResponse = try await sendOnce(
                "mobile/auth/teacher/refresh",
                method: .post
            )
            store.saveTeacherAuth(auth)
        case .none:
            // Had a token in the store but no userType — inconsistent, force re-auth.
            throw APIError.unauthorized
        }
    }

    func sendVoid(
        _ path: String,
        method: HTTPMethod = .post,
        query: [URLQueryItem] = [],
        body: (any Encodable)? = nil
    ) async throws {
        let _: EmptyData = try await send(path, method: method, query: query, body: body)
    }

    // MARK: - Request building

    private func buildRequest(
        path: String,
        method: HTTPMethod,
        query: [URLQueryItem],
        body: (any Encodable)?
    ) throws -> URLRequest {
        let trimmed = path.hasPrefix("/") ? String(path.dropFirst()) : path
        let urlString = baseURL.hasSuffix("/") ? baseURL + trimmed : baseURL + "/" + trimmed

        guard var components = URLComponents(string: urlString) else {
            throw APIError.invalidURL
        }
        if !query.isEmpty {
            components.queryItems = query
        }
        guard let url = components.url else { throw APIError.invalidURL }

        var request = URLRequest(url: url)
        request.httpMethod = method.rawValue
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.setValue("application/json", forHTTPHeaderField: "Accept")

        // Phase 44: `AppInfo.schoolCode` returns the baked xcconfig value on
        // school-specific flavors and the user-picked subdomain (persisted in
        // KeychainTokenStore) on the generic "CHAK IN" flavor. Sending both
        // headers matches Android — /branding uses X-Subdomain, /mobile/* uses
        // X-School-Code; keeping them in sync avoids the tenant-mismatch class
        // of bugs. Both are omitted when neither source has a value (the
        // school-code entry screen runs before we know the school).
        let resolvedSchoolCode = AppInfo.schoolCode
        if !resolvedSchoolCode.isEmpty {
            request.setValue(resolvedSchoolCode, forHTTPHeaderField: "X-School-Code")
            request.setValue(resolvedSchoolCode, forHTTPHeaderField: "X-Subdomain")
        }

        if let token = tokenProvider?() {
            request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        }

        if let body {
            request.httpBody = try encoder.encode(AnyEncodableBox(body))
        }

        return request
    }

    // MARK: - Decoding

    private func decodeEnvelope<T: Decodable>(data: Data, path: String) throws -> T {
        let envelope: APIResponse<T>
        do {
            envelope = try decoder.decode(APIResponse<T>.self, from: data)
        } catch {
            DebugLogger.log(.network, "↯ \(path) — decoding error: \(error)")
            throw APIError.decoding(error)
        }

        if envelope.success {
            if T.self == EmptyData.self {
                // Caller only cares about success/failure — return an empty placeholder.
                return EmptyData() as! T
            }
            if let value = envelope.data {
                return value
            }
            throw APIError.missingData
        }

        let serverMsg = envelope.message ?? envelope.errors?.joined(separator: ", ") ?? "Request failed"
        DebugLogger.log(.network, "↯ \(path) — server returned success=false: \"\(serverMsg)\"")
        throw APIError.server(message: serverMsg)
    }

    // MARK: - Logging

    private func logRequest(_ request: URLRequest) {
        #if DEBUG
        let method = request.httpMethod ?? "?"
        let url = request.url?.absoluteString ?? "?"
        let schoolCode = request.value(forHTTPHeaderField: "X-School-Code") ?? "<missing>"
        let auth = request.value(forHTTPHeaderField: "Authorization") ?? "<none>"
        let authPreview: String = {
            guard auth.hasPrefix("Bearer ") else { return auth }
            let token = String(auth.dropFirst("Bearer ".count))
            let head = token.prefix(12)
            let tail = token.suffix(6)
            return "Bearer \(head)…\(tail) (len=\(token.count))"
        }()
        let cookieHeader = request.value(forHTTPHeaderField: "Cookie") ?? "<none>"
        DebugLogger.log(.network, "→ \(method) \(url)")
        DebugLogger.log(.network, "    X-School-Code: \(schoolCode)")
        DebugLogger.log(.network, "    Authorization: \(authPreview)")
        DebugLogger.log(.network, "    Cookie: \(cookieHeader.count > 80 ? String(cookieHeader.prefix(80)) + "…" : cookieHeader)")
        #endif
    }

    private func logResponse(_ http: HTTPURLResponse, body: Data, for path: String) {
        #if DEBUG
        let bodyString: String = {
            guard let text = String(data: body, encoding: .utf8) else { return "<\(body.count) bytes binary>" }
            return text.count > 600 ? String(text.prefix(600)) + "…(truncated)" : text
        }()
        DebugLogger.log(.network, "← \(http.statusCode) \(path)")
        DebugLogger.log(.network, "    body: \(bodyString)")
        #endif
    }
}

// Type-erased Encodable so callers can pass any Encodable value without generics on the function.
private struct AnyEncodableBox: Encodable {
    private let _encode: (Encoder) throws -> Void
    init(_ wrapped: any Encodable) {
        self._encode = wrapped.encode
    }
    func encode(to encoder: Encoder) throws {
        try _encode(encoder)
    }
}

// Serialises concurrent 401 refreshes: if a refresh is already in flight
// every subsequent caller awaits the same Task instead of firing another
// refresh. Mirrors Android's `refreshLock` in RetrofitClient.kt (Phase 65).
//
// Kept as a MainActor-isolated class (matches the project default in xcconfig)
// rather than a bare actor — this avoids @Sendable friction with APIClient,
// which is also MainActor. Serialisation is safe because the check-and-set of
// `inFlight` happens without any intervening await, and MainActor guarantees
// no interleaving inside a synchronous window.
private final class RefreshCoordinator {
    private var inFlight: Task<Void, Error>?

    func refresh(using perform: @escaping () async throws -> Void) async throws {
        if let existing = inFlight {
            _ = try await existing.value
            return
        }
        let task = Task { try await perform() }
        inFlight = task
        defer { inFlight = nil }
        _ = try await task.value
    }
}

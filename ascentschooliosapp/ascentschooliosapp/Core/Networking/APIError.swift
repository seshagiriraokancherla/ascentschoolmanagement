import Foundation

enum APIError: Error, LocalizedError {
    case invalidURL
    case network(Error)
    case http(statusCode: Int, body: Data?)
    case unauthorized
    case decoding(Error)
    case server(message: String)
    case missingData
    case unknown

    var errorDescription: String? {
        switch self {
        case .invalidURL: return "Invalid URL"
        case .network(let error): return error.localizedDescription
        case .http(let code, _): return "HTTP \(code)"
        case .unauthorized: return "Session expired. Please sign in again."
        case .decoding(let error): return "Could not parse server response: \(error.localizedDescription)"
        case .server(let message): return message
        case .missingData: return "No data returned from the server."
        case .unknown: return "Something went wrong."
        }
    }
}

import Foundation

struct APIResponse<T: Decodable>: Decodable {
    let success: Bool
    let data: T?
    let message: String?
    let errors: [String]?
}

struct EmptyData: Decodable {}

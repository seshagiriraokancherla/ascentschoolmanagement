import Foundation

// Phase 92 (Android parity): parent ↔ teacher messaging. Field names/shapes
// mirror the server `MessagingDtos.cs` exactly. Dates arrive as ISO strings
// and are kept as `String?` (formatted for display with `friendlyDate()` /
// a short time formatter), matching the rest of the app's date handling.

// One message in a thread. `senderType` is "parent" | "teacher"; `status` is
// "Active" | "Removed" (removed messages render as "This message was removed.").
struct MessageDto: Decodable, Identifiable {
    let messageId: Int
    let threadId: Int
    let senderType: String
    let senderId: Int
    let senderName: String?
    let body: String
    let status: String?
    let readAt: String?
    let createdAt: String?

    var id: Int { messageId }

    var isRemoved: Bool { (status ?? "").caseInsensitiveCompare("Removed") == .orderedSame }
}

// A parent↔teacher conversation about one child. Used by the teacher inbox.
struct MessageThreadDto: Decodable, Identifiable {
    let threadId: Int
    let studentUniqueId: Int?
    let parentId: Int?
    let studentName: String?
    let admissionNo: String?
    let className: String?
    let sectionName: String?
    let status: String?          // Active | Blocked
    let blockedByType: String?
    let blockedAt: String?
    let lastMessageBody: String?
    let lastMessageAt: String?
    let unreadCount: Int?        // unread for the caller's side
    let createdAt: String?

    var id: Int { threadId }
}

// GET /mobile/teacher/messages/{threadId} — a thread plus its messages.
struct MessageThreadDetailDto: Decodable {
    let thread: MessageThreadDto?
    let messages: [MessageDto]
}

// GET /mobile/messages — everything the parent chat screen opens with.
struct ParentThreadViewDto: Decodable {
    let canMessage: Bool
    let reason: String?            // set when canMessage == false
    let teachers: [String]         // recipient display names
    let threadId: Int?             // nil until the first message
    let status: String?            // Active | Blocked
    let blockedByType: String?     // parent | teacher
    let messages: [MessageDto]
}

// POST send/reply response payload ({ messageId, threadId }).
struct SendMessageResult: Decodable {
    let messageId: Int
    let threadId: Int
}

struct SendMessageRequest: Encodable {
    let body: String
}

struct ReportMessageRequest: Encodable {
    let messageId: Int
    let reason: String?
}

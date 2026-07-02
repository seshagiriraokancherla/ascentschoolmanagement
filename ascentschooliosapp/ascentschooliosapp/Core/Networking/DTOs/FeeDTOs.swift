import Foundation

// Server categorizes fees into "School", "Transport", "Hostel" (Android Phase 33 contract).
enum FeeTypeCategory: String, CaseIterable, Identifiable {
    case school    = "School"
    case transport = "Transport"
    case hostel    = "Hostel"

    var id: String { rawValue }
    var displayName: String { rawValue }
}

// MARK: - Cross-year outstanding summary

struct CrossYearFeeSummaryDto: Decodable {
    let studentUniqueId: Int?
    let years: [MobileFeeSummaryDto]
}

struct MobileFeeSummaryDto: Decodable, Identifiable {
    let academicYearId: Int
    let academicYear: String?
    let studentId: Int64?
    let totalAmount: Double           // server: totalStructure
    let paidAmount: Double            // server: totalPaid
    let outstandingAmount: Double     // server: totalOutstanding
    let lineItems: [MobileFeeLineItemDto]

    var id: Int { academicYearId }

    private enum CodingKeys: String, CodingKey {
        case academicYearId, academicYear, studentId, lineItems
        case totalAmount       = "totalStructure"
        case paidAmount        = "totalPaid"
        case outstandingAmount = "totalOutstanding"
    }
}

struct MobileFeeLineItemDto: Decodable, Identifiable {
    let feeTypeId: Int?
    let feeTypeName: String?
    let termId: Int?
    let termName: String?
    let feePeriodId: Int?
    let feePeriodLabel: String?
    let busRouteId: Int?
    let routeName: String?
    let hostelId: Int?
    let hostelName: String?
    let amount: Double                // server: structureAmount
    let paidAmount: Double
    let outstanding: Double
    let concessionAmount: Double
    // Phase 58 (Android parity): server correlates each paid line to its
    // receipt so the parent app can offer in-app print/view for
    // paid-via-Mobile-App items. `createdBy` is "Mobile App" for iOS/Android
    // payments and "Parent Portal" for the web app — we only enable the print
    // action on our own payments.
    let receiptId: Int?
    let createdBy: String?

    // Synthesized stable id per line — Android uses position-in-list; we'll combine the FKs.
    var id: String {
        "\(feeTypeId ?? 0)-\(termId ?? 0)-\(feePeriodId ?? 0)-\(busRouteId ?? 0)-\(hostelId ?? 0)"
    }

    var isPaid: Bool { outstanding <= 0.0001 }

    // Phase 58: mirrors Android's `MobileFeeLineItemDto.canPrint`. Paid via
    // the mobile app AND we have a receiptId to fetch — otherwise the Print
    // button is hidden.
    var canPrint: Bool {
        isPaid && receiptId != nil && (createdBy ?? "") == "Mobile App"
    }

    private enum CodingKeys: String, CodingKey {
        case feeTypeId, feeTypeName
        case termId, termName
        case feePeriodId, feePeriodLabel
        case busRouteId, routeName
        case hostelId, hostelName
        case amount = "structureAmount"
        case paidAmount, outstanding, concessionAmount
        case receiptId, createdBy
    }
}

// MARK: - Razorpay order lifecycle

struct MobileCreateOrderRequest: Encodable {
    let academicYearId: Int
    let feeTypeCategory: String     // FeeTypeCategory.rawValue
    let items: [MobileFeeOrderItem]
}

struct MobileFeeOrderItem: Encodable {
    let feeTypeId: Int?
    let termId: Int?
    let feePeriodId: Int?
    let busRouteId: Int?
    let hostelId: Int?
    let amount: Double
    let concessionAmount: Double
}

struct MobileOrderResponse: Decodable, Identifiable {
    let gatewayOrderId: Int
    let externalOrderId: String       // Razorpay order_id
    let keyId: String                  // Razorpay key_id (public, safe to embed in checkout)
    let amountInPaise: Int
    let currency: String
    let gatewayName: String?

    var id: Int { gatewayOrderId }
}

struct MobileVerifyRequest: Encodable {
    let gatewayOrderId: Int
    let paymentId: String              // Razorpay razorpay_payment_id
    let orderId: String                // Razorpay razorpay_order_id
    let signature: String              // Razorpay razorpay_signature (HMAC)
}

struct MobilePaymentResultDto: Decodable {
    let receiptId: Int?
    let receiptNo: String?
    let amount: Double?
    let message: String?
}

// Phase 58 (Android parity): full receipt payload returned by
// GET /mobile/fees/receipts/{id}. Validated server-side to belong to the
// selected child via `student_unique_id`. Used to render the printable
// receipt sheet (WKWebView + UIPrintInteractionController).
struct MobileReceiptDto: Decodable, Identifiable {
    let receiptId: Int
    let receiptNo: String?
    let receiptDate: String?
    let studentName: String?
    let admissionNo: String?
    let className: String?
    let sectionName: String?
    let academicYear: String?
    let paymentMode: String?
    let totalAmount: Double
    let status: String?
    let createdBy: String?
    let remarks: String?
    let items: [MobileReceiptItemDto]

    var id: Int { receiptId }
}

struct MobileReceiptItemDto: Decodable, Identifiable {
    let feeTypeName: String?
    let termName: String?
    let feePeriodLabel: String?
    let routeName: String?
    let hostelName: String?
    let amount: Double
    let concessionAmount: Double
    let netAmount: Double

    // Synthesized id — items don't have a stable server id in the mobile projection.
    var id: String {
        "\(feeTypeName ?? "")|\(termName ?? "")|\(feePeriodLabel ?? "")|\(routeName ?? "")|\(hostelName ?? "")|\(netAmount)"
    }
}

// MARK: - Gateway config (read-only key_id for checkout)

struct GatewayConfigDto: Decodable {
    let keyId: String?
    let gatewayName: String?
    let isActive: Bool?
}

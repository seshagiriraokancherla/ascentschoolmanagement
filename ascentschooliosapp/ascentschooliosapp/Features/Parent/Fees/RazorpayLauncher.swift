import Foundation
import UIKit
import Razorpay

// Async wrapper around the Razorpay iOS SDK.
//
// We use the Obj-C `RazorpayCheckout` class directly (not the Swift wrapper
// `RazorpaySwift`) for two reasons:
//   1. `RazorpaySwift.open(withPayload:)` doesn't expose `displayController:`,
//      and the SDK's auto-detection of `UIApplication.shared.keyWindow` doesn't
//      work on iOS 13+ scene-based apps — Razorpay's modal silently fails to
//      present, our delegate is never called, our `withCheckedContinuation` is
//      never resumed, and the user sees nothing on Pay-now tap.
//   2. Going through the Obj-C class lets us pass the resolved top view
//      controller explicitly via `open(_:displayController:)`.
//
// The class inherits from NSObject to satisfy the `@objc` requirements of
// `RazorpayPaymentCompletionProtocolWithData`. Delegate methods are
// `nonisolated` because Razorpay invokes them via the Obj-C runtime; they
// hop to MainActor before touching `continuation`.
final class RazorpayLauncher: NSObject, RazorpayPaymentCompletionProtocolWithData {

    enum Result {
        case success(paymentId: String, orderId: String, signature: String)
        case cancelled
        case failure(message: String)
    }

    // SDK keeps a strong reference to its delegate, so the singleton-ish
    // pattern below isn't strictly required, but holding `razorpay` ensures
    // we don't reinitialise it on repeat launches.
    private var razorpay: RazorpayCheckout?
    private var continuation: CheckedContinuation<Result, Never>?

    override init() {
        super.init()
    }

    func launch(order: MobileOrderResponse) async -> Result {
        DebugLogger.log(.network, "RazorpayLauncher.launch begin — gatewayOrderId=\(order.gatewayOrderId)")

        let result = await withCheckedContinuation { (cont: CheckedContinuation<Result, Never>) in
            continuation = cont
            present(order: order)
        }

        DebugLogger.log(.network, "RazorpayLauncher.launch end — \(result)")
        return result
    }

    // MARK: - Present

    private func present(order: MobileOrderResponse) {
        let options: [AnyHashable: Any] = [
            "order_id":    order.externalOrderId,
            "amount":      order.amountInPaise,
            "currency":    order.currency,
            "name":        AppInfo.displayName,
            "description": "School fee payment",
            "prefill":     ["name": KeychainTokenStore.shared.studentName ?? ""],
            "theme":       ["color": "#1E3A8A"],
        ]

        if razorpay == nil {
            razorpay = RazorpayCheckout.initWithKey(order.keyId, andDelegateWithData: self)
        }

        guard let razorpay else {
            DebugLogger.log(.network, "Razorpay: SDK failed to initialise")
            finish(.failure(message: "Couldn't initialise the payment gateway."))
            return
        }

        if let presenter = Self.topViewController() {
            DebugLogger.log(.network, "Razorpay: presenting on \(type(of: presenter))")
            razorpay.open(options, displayController: presenter)
        } else {
            DebugLogger.log(.network, "Razorpay: no presenter found, falling back to open(options)")
            razorpay.open(options)
        }
    }

    private static func topViewController() -> UIViewController? {
        // Scene-based lookup (iOS 13+) — UIApplication.shared.keyWindow is
        // deprecated and returns nil in scene-based apps like ours.
        let scenes = UIApplication.shared.connectedScenes
            .compactMap { $0 as? UIWindowScene }
        let scene = scenes.first(where: { $0.activationState == .foregroundActive })
            ?? scenes.first
        guard let scene else { return nil }

        let window = scene.windows.first(where: { $0.isKeyWindow }) ?? scene.windows.first
        guard let window else { return nil }

        var top = window.rootViewController
        while let presented = top?.presentedViewController {
            top = presented
        }
        return top
    }

    // MARK: - Continuation

    private func finish(_ result: Result) {
        continuation?.resume(returning: result)
        continuation = nil
    }

    // MARK: - RazorpayPaymentCompletionProtocolWithData
    // Razorpay calls these from the Obj-C runtime; mark `nonisolated` and hop
    // back to MainActor before touching the continuation.

    nonisolated func onPaymentSuccess(_ payment_id: String, andData response: [AnyHashable: Any]?) {
        let orderId   = (response?["razorpay_order_id"]  as? String) ?? ""
        let signature = (response?["razorpay_signature"] as? String) ?? ""
        Task { @MainActor [weak self] in
            self?.finish(.success(paymentId: payment_id, orderId: orderId, signature: signature))
        }
    }

    nonisolated func onPaymentError(_ code: Int32, description str: String, andData response: [AnyHashable: Any]?) {
        // Razorpay error code 2 == user cancelled the payment sheet.
        let result: Result = (code == 2) ? .cancelled : .failure(message: str)
        Task { @MainActor [weak self] in
            self?.finish(result)
        }
    }
}

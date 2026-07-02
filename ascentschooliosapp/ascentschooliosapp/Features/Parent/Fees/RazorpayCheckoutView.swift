import SwiftUI
import UIKit

// Bridge between SwiftUI and the Razorpay checkout flow.
//
// Phase iOS-8a (current): the host controller shows a placeholder
// UIAlertController with the order details and a "Simulate success" button so
// the rest of the flow (server verify, receipt alert, list reload) can be
// exercised before the SDK is linked.
//
// Phase iOS-8b (after `razorpay-pod` SwiftPM package is added):
//   - Add `import Razorpay`
//   - Replace the `present(alert, …)` call with:
//       let razorpay = RazorpayCheckout.initWithKey(order.keyId,
//                                                   andDelegate: context.coordinator)
//       razorpay.open([
//           "order_id":   order.externalOrderId,
//           "amount":     order.amountInPaise,
//           "currency":   order.currency,
//           "name":       AppInfo.displayName,
//           "description":"School fee payment",
//           "prefill":    ["name": KeychainTokenStore.shared.studentName ?? ""],
//           "theme":      ["color": "#1E3A8A"]
//       ])
//   - Make `HostController` conform to `RazorpayPaymentCompletionProtocolWithData`:
//       func onPaymentSuccess(paymentId: String, andData response: [AnyHashable: Any]?) {
//           let orderId   = response?["razorpay_order_id"]   as? String ?? ""
//           let signature = response?["razorpay_signature"]  as? String ?? ""
//           onSuccess(paymentId, orderId, signature)
//       }
//       func onPaymentError(_ code: Int32, description str: String,
//                           andData response: [AnyHashable: Any]?) {
//           onFailure(str)
//       }
struct RazorpayCheckoutView: UIViewControllerRepresentable {

    let order: MobileOrderResponse
    let onSuccess: (_ paymentId: String, _ orderId: String, _ signature: String) -> Void
    let onCancel: () -> Void
    let onFailure: (_ message: String) -> Void

    func makeUIViewController(context: Context) -> UIViewController {
        HostController(
            order: order,
            onSuccess: onSuccess,
            onCancel: onCancel,
            onFailure: onFailure
        )
    }

    func updateUIViewController(_ uiViewController: UIViewController, context: Context) {}

    // MARK: - Host

    final class HostController: UIViewController {
        private let order: MobileOrderResponse
        private let onSuccess: (String, String, String) -> Void
        private let onCancel: () -> Void
        private let onFailure: (String) -> Void
        private var hasPresented = false

        init(
            order: MobileOrderResponse,
            onSuccess: @escaping (String, String, String) -> Void,
            onCancel: @escaping () -> Void,
            onFailure: @escaping (String) -> Void
        ) {
            self.order = order
            self.onSuccess = onSuccess
            self.onCancel = onCancel
            self.onFailure = onFailure
            super.init(nibName: nil, bundle: nil)
        }

        required init?(coder: NSCoder) { fatalError("init(coder:) not supported") }

        override func viewDidLoad() {
            super.viewDidLoad()
            view.backgroundColor = .systemBackground
        }

        override func viewDidAppear(_ animated: Bool) {
            super.viewDidAppear(animated)
            guard !hasPresented else { return }
            hasPresented = true
            presentStub()
        }

        private func presentStub() {
            let amount = Double(order.amountInPaise) / 100.0
            let alert = UIAlertController(
                title: "Razorpay stub",
                message: """
                Order created on the server:

                Order id: \(order.externalOrderId)
                Amount:   ₹\(String(format: "%.2f", amount))
                Key:      \(order.keyId)

                Add the razorpay-pod Swift package and Phase iOS-8b will swap this dialog for the real checkout.
                """,
                preferredStyle: .alert
            )
            alert.addAction(.init(title: "Cancel", style: .cancel) { [weak self] _ in
                self?.onCancel()
            })
            alert.addAction(.init(title: "Simulate success", style: .default) { [weak self] _ in
                guard let self else { return }
                // Server will reject the fake signature — that's expected; this lets us
                // exercise the verify endpoint plumbing without the real SDK.
                self.onSuccess("pay_stub_id", self.order.externalOrderId, "sig_stub_pending_real_sdk")
            })
            present(alert, animated: true)
        }
    }
}

import Foundation
import Observation

@Observable
final class FeesViewModel {

    enum LoadState {
        case idle
        case loading
        case success(CrossYearFeeSummaryDto)
        case failure(String)
    }

    // Category tab
    var selectedCategory: FeeTypeCategory = .school

    // Outstanding data
    var loadState: LoadState = .idle

    // Selection — scoped to one academic year at a time (Android decision #69).
    // Switching to an item from another year clears the prior selection.
    var selectedIds: Set<String> = []
    var selectedYearId: Int?

    // Payment lifecycle
    var isInitiatingOrder: Bool = false
    var isVerifying: Bool = false
    var paymentResult: MobilePaymentResultDto?
    var paymentError: String?

    // Phase 58: receipt print/view lifecycle. `isFetchingReceipt` toggles a
    // spinner overlay while /mobile/fees/receipts/{id} loads; `receiptToPrint`
    // is set when the fetch succeeds so the view can hand it to
    // `ReceiptPrinter.present` (which shows the system print sheet — the
    // preview inside doubles as a viewer).
    var isFetchingReceipt: Bool = false
    var receiptToPrint: MobileReceiptDto?
    var receiptError: String?

    private let razorpay = RazorpayLauncher()

    // MARK: - Derived

    var hasSelection: Bool { !selectedIds.isEmpty }

    var selectedTotal: Double {
        guard let summary = currentSummary, let yearId = selectedYearId,
              let year = summary.years.first(where: { $0.academicYearId == yearId }) else {
            return 0
        }
        return year.lineItems
            .filter { selectedIds.contains($0.id) }
            .reduce(0) { $0 + $1.outstanding }
    }

    private var currentSummary: CrossYearFeeSummaryDto? {
        if case .success(let s) = loadState { return s }
        return nil
    }

    // MARK: - Data loading

    func loadFees() async {
        loadState = .loading
        do {
            let summary = try await APIClient.shared.feeOutstanding(category: selectedCategory)
            loadState = .success(summary)
        } catch {
            loadState = .failure((error as? APIError)?.errorDescription ?? error.localizedDescription)
        }
    }

    func selectCategory(_ category: FeeTypeCategory) async {
        guard category != selectedCategory else { return }
        selectedCategory = category
        clearSelection()
        await loadFees()
    }

    // MARK: - Selection

    func toggle(item: MobileFeeLineItemDto, in year: MobileFeeSummaryDto) {
        // Switching year clears prior selection.
        if let current = selectedYearId, current != year.academicYearId {
            selectedIds.removeAll()
        }
        selectedYearId = year.academicYearId

        if selectedIds.contains(item.id) {
            selectedIds.remove(item.id)
        } else {
            selectedIds.insert(item.id)
        }

        if selectedIds.isEmpty {
            selectedYearId = nil
        }
    }

    func selectAll(in year: MobileFeeSummaryDto) {
        if selectedYearId != year.academicYearId {
            selectedIds.removeAll()
        }
        selectedYearId = year.academicYearId

        let pendingIds = year.lineItems.filter { !$0.isPaid }.map(\.id)
        let pendingSet = Set(pendingIds)

        // Toggle: if all pending are already selected, deselect; otherwise select all.
        if pendingSet.isSubset(of: selectedIds) {
            selectedIds.subtract(pendingSet)
        } else {
            selectedIds.formUnion(pendingSet)
        }

        if selectedIds.isEmpty {
            selectedYearId = nil
        }
    }

    func isYearFullySelected(_ year: MobileFeeSummaryDto) -> Bool {
        let pendingIds = Set(year.lineItems.filter { !$0.isPaid }.map(\.id))
        guard !pendingIds.isEmpty else { return false }
        return pendingIds.isSubset(of: selectedIds) && selectedYearId == year.academicYearId
    }

    func clearSelection() {
        selectedIds.removeAll()
        selectedYearId = nil
    }

    // MARK: - Payment

    func initiatePayment() async {
        guard let yearId = selectedYearId,
              let summary = currentSummary,
              let year = summary.years.first(where: { $0.academicYearId == yearId }) else {
            paymentError = "Select at least one item to pay."
            return
        }

        let items: [MobileFeeOrderItem] = year.lineItems
            .filter { selectedIds.contains($0.id) }
            .map { item in
                MobileFeeOrderItem(
                    feeTypeId: item.feeTypeId,
                    termId: item.termId,
                    feePeriodId: item.feePeriodId,
                    busRouteId: item.busRouteId,
                    hostelId: item.hostelId,
                    // `outstanding` already nets out concessions server-side
                    // (matches parent-portal contract — Android #62).
                    amount: item.outstanding,
                    concessionAmount: 0
                )
            }

        guard !items.isEmpty else {
            paymentError = "Select at least one item to pay."
            return
        }

        paymentError = nil

        // 1. POST create-order
        isInitiatingOrder = true
        let order: MobileOrderResponse
        do {
            let request = MobileCreateOrderRequest(
                academicYearId: yearId,
                feeTypeCategory: selectedCategory.rawValue,
                items: items
            )
            order = try await APIClient.shared.createPaymentOrder(request)
        } catch {
            paymentError = (error as? APIError)?.errorDescription ?? error.localizedDescription
            isInitiatingOrder = false
            return
        }
        isInitiatingOrder = false

        // 2. Open Razorpay (async — resumes when user finishes / cancels / errors)
        let result = await razorpay.launch(order: order)

        // 3. Branch on outcome
        switch result {
        case .success(let paymentId, let orderId, let signature):
            await verifyPayment(
                gatewayOrderId: order.gatewayOrderId,
                paymentId: paymentId,
                orderId: orderId,
                signature: signature
            )
        case .cancelled:
            // User dismissed the sheet — silently return; selection stays so they can retry.
            break
        case .failure(let message):
            paymentError = message
        }
    }

    private func verifyPayment(
        gatewayOrderId: Int,
        paymentId: String,
        orderId: String,
        signature: String
    ) async {
        isVerifying = true
        defer { isVerifying = false }

        do {
            let result = try await APIClient.shared.verifyPayment(
                gatewayOrderId: gatewayOrderId,
                request: MobileVerifyRequest(
                    gatewayOrderId: gatewayOrderId,
                    paymentId: paymentId,
                    orderId: orderId,
                    signature: signature
                )
            )
            paymentResult = result
            clearSelection()
            await loadFees()
        } catch {
            paymentError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }

    func dismissPaymentResult() {
        paymentResult = nil
    }

    func dismissPaymentError() {
        paymentError = nil
    }

    // MARK: - Receipt print (Phase 58)

    // Fetches the full receipt and stashes it on `receiptToPrint`. The view
    // observes that field, hands the receipt to ReceiptPrinter, then clears
    // the field via `dismissReceipt` so re-tapping Print re-fetches (which
    // catches a receipt that was cancelled between taps).
    func fetchReceipt(id: Int) async {
        isFetchingReceipt = true
        defer { isFetchingReceipt = false }
        do {
            let receipt = try await APIClient.shared.mobileReceipt(id: id)
            receiptToPrint = receipt
        } catch {
            receiptError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }

    func dismissReceipt() {
        receiptToPrint = nil
    }

    func dismissReceiptError() {
        receiptError = nil
    }
}

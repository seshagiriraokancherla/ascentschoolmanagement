import UIKit

// Phase 58 (Android parity): in-app receipt print. Android uses an offscreen
// WebView + PrintManager; iOS collapses those to UIPrintInteractionController
// with a UIMarkupTextPrintFormatter, which shows the system print sheet (also
// doubles as a preview + "Save to PDF" affordance) with zero WKWebView setup.
//
// Called from FeesView after the view model finishes fetching the receipt
// detail. Presentation must happen on the main actor from a live UIWindow —
// SwiftUI doesn't own the print controller, so we grab the topmost window ourselves.
@MainActor
enum ReceiptPrinter {

    static func present(receipt: MobileReceiptDto) {
        let html = renderHTML(receipt: receipt)

        let info = UIPrintInfo.printInfo()
        info.outputType = .general
        info.jobName = "Receipt \(receipt.receiptNo ?? String(receipt.receiptId))"
        info.orientation = .portrait

        let controller = UIPrintInteractionController.shared
        controller.printInfo = info
        controller.printFormatter = UIMarkupTextPrintFormatter(markupText: html)
        controller.showsNumberOfCopies = true

        guard let window = topWindow() else {
            controller.present(animated: true, completionHandler: nil)
            return
        }

        // iPad requires a source rect/bar button for the popover presentation;
        // iPhone doesn't care. Pass a centred zero-rect on iPad so the sheet
        // anchors reliably even without a tapped-button reference.
        if UIDevice.current.userInterfaceIdiom == .pad, let root = window.rootViewController {
            let sourceRect = CGRect(
                x: root.view.bounds.midX,
                y: root.view.bounds.midY,
                width: 0,
                height: 0
            )
            controller.present(from: sourceRect, in: root.view, animated: true, completionHandler: nil)
        } else {
            controller.present(animated: true, completionHandler: nil)
        }
    }

    // MARK: - HTML

    private static func renderHTML(receipt: MobileReceiptDto) -> String {
        let brand = AppInfo.displayName.htmlEscaped
        let receiptNo = (receipt.receiptNo ?? "—").htmlEscaped
        let receiptDate = formatDate(receipt.receiptDate)
        let studentName = (receipt.studentName ?? "—").htmlEscaped
        let admission = (receipt.admissionNo ?? "—").htmlEscaped
        let classSec = classSection(className: receipt.className, sectionName: receipt.sectionName)
        let year = (receipt.academicYear ?? "").htmlEscaped
        let mode = (receipt.paymentMode ?? "—").htmlEscaped
        let collectedBy = (receipt.createdBy ?? "—").htmlEscaped
        let isCancelled = (receipt.status ?? "").caseInsensitiveCompare("Cancelled") == .orderedSame

        let itemRows = receipt.items.map { item in
            let feeType = firstNonEmpty(item.feeTypeName, item.routeName, item.hostelName) ?? "—"
            let term = firstNonEmpty(item.termName, item.feePeriodLabel) ?? "—"
            return """
            <tr>
              <td>\(feeType.htmlEscaped)</td>
              <td>\(term.htmlEscaped)</td>
              <td class="num">\(rupees(item.amount))</td>
              <td class="num">\(rupees(item.concessionAmount))</td>
              <td class="num">\(rupees(item.netAmount))</td>
            </tr>
            """
        }.joined(separator: "\n")

        let cancelledBanner = isCancelled
            ? """
              <div class="cancelled">CANCELLED\(receipt.remarks.map { " · \($0.htmlEscaped)" } ?? "")</div>
              """
            : ""

        return """
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset="utf-8" />
          <title>Fee Receipt</title>
          <style>
            body { font-family: -apple-system, "Helvetica Neue", Helvetica, Arial, sans-serif;
                   color: #0F172A; margin: 24px; }
            .header { text-align: center; margin-bottom: 16px; }
            .school { font-size: 20px; font-weight: 700; margin: 0; }
            .title { font-size: 14px; letter-spacing: 2px; color: #64748B;
                     margin-top: 6px; text-transform: uppercase; }
            .meta { width: 100%; margin-top: 16px; font-size: 12px; }
            .meta td { padding: 4px 0; vertical-align: top; }
            .meta .label { color: #64748B; padding-right: 12px; width: 130px; }
            table.items { width: 100%; border-collapse: collapse; margin-top: 18px; font-size: 12px; }
            table.items th, table.items td { border-bottom: 1px solid #E2E8F0; padding: 8px 6px;
                                             text-align: left; }
            table.items th { background: #F1F5F9; color: #1E3A8A; font-weight: 600; }
            table.items td.num, table.items th.num { text-align: right; }
            .total-row td { border-top: 2px solid #1E3A8A; border-bottom: none;
                            font-weight: 700; font-size: 13px; padding-top: 10px; }
            .cancelled { color: #B91C1C; border: 2px solid #B91C1C; padding: 8px 12px;
                         font-weight: 700; margin-top: 14px; text-align: center; letter-spacing: 1px; }
            .footer { margin-top: 24px; font-size: 11px; color: #64748B; text-align: center; }
          </style>
        </head>
        <body>
          <div class="header">
            <p class="school">\(brand)</p>
            <p class="title">Fee Receipt</p>
          </div>

          \(cancelledBanner)

          <table class="meta">
            <tr><td class="label">Receipt No.</td><td>\(receiptNo)</td>
                <td class="label">Date</td><td>\(receiptDate)</td></tr>
            <tr><td class="label">Student</td><td>\(studentName)</td>
                <td class="label">Admission No.</td><td>\(admission)</td></tr>
            <tr><td class="label">Class</td><td>\(classSec)</td>
                <td class="label">Academic Year</td><td>\(year)</td></tr>
            <tr><td class="label">Payment Mode</td><td>\(mode)</td>
                <td class="label">Collected By</td><td>\(collectedBy)</td></tr>
          </table>

          <table class="items">
            <thead>
              <tr>
                <th>Fee Type</th>
                <th>Term / Period</th>
                <th class="num">Amount</th>
                <th class="num">Concession</th>
                <th class="num">Net</th>
              </tr>
            </thead>
            <tbody>
              \(itemRows)
              <tr class="total-row">
                <td colspan="4">Total</td>
                <td class="num">\(rupees(receipt.totalAmount))</td>
              </tr>
            </tbody>
          </table>

          <div class="footer">Generated in-app on \(currentTimestamp()) · \(brand)</div>
        </body>
        </html>
        """
    }

    // MARK: - Helpers

    private static func rupees(_ value: Double) -> String {
        let f = NumberFormatter()
        f.numberStyle = .decimal
        f.minimumFractionDigits = 2
        f.maximumFractionDigits = 2
        let text = f.string(from: NSNumber(value: value)) ?? String(format: "%.2f", value)
        return "₹" + text
    }

    private static func formatDate(_ iso: String?) -> String {
        guard let iso, !iso.isEmpty else { return "—" }
        let display = DateFormatter()
        display.dateFormat = "d MMM yyyy"

        let isoFull = ISO8601DateFormatter()
        isoFull.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        if let date = isoFull.date(from: iso) { return display.string(from: date) }

        let isoNoFraction = ISO8601DateFormatter()
        isoNoFraction.formatOptions = [.withInternetDateTime]
        if let date = isoNoFraction.date(from: iso) { return display.string(from: date) }

        let dateOnly = DateFormatter()
        dateOnly.dateFormat = "yyyy-MM-dd"
        if let date = dateOnly.date(from: iso) { return display.string(from: date) }

        return iso
    }

    private static func currentTimestamp() -> String {
        let f = DateFormatter()
        f.dateFormat = "d MMM yyyy, h:mm a"
        return f.string(from: Date())
    }

    private static func classSection(className: String?, sectionName: String?) -> String {
        let base = (className ?? "").trimmingCharacters(in: .whitespaces)
        let sec = (sectionName ?? "").trimmingCharacters(in: .whitespaces)
        if base.isEmpty && sec.isEmpty { return "—" }
        if sec.isEmpty { return base.htmlEscaped }
        return "\(base) · \(sec)".htmlEscaped
    }

    private static func firstNonEmpty(_ values: String?...) -> String? {
        for v in values {
            if let v, !v.trimmingCharacters(in: .whitespaces).isEmpty { return v }
        }
        return nil
    }

    private static func topWindow() -> UIWindow? {
        let scenes = UIApplication.shared.connectedScenes
            .compactMap { $0 as? UIWindowScene }
        return scenes
            .flatMap { $0.windows }
            .first(where: { $0.isKeyWindow }) ?? scenes.flatMap { $0.windows }.first
    }
}

// Minimal HTML escaper — avoids pulling in a third-party dependency for the
// four characters that actually matter in receipt fields.
private extension String {
    var htmlEscaped: String {
        var s = self
        s = s.replacingOccurrences(of: "&", with: "&amp;")
        s = s.replacingOccurrences(of: "<", with: "&lt;")
        s = s.replacingOccurrences(of: ">", with: "&gt;")
        s = s.replacingOccurrences(of: "\"", with: "&quot;")
        s = s.replacingOccurrences(of: "'", with: "&#39;")
        return s
    }
}

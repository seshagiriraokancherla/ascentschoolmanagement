import SwiftUI

struct FeesView: View {
    @State private var viewModel = FeesViewModel()

    var body: some View {
        VStack(spacing: 0) {
            categoryPicker

            ZStack(alignment: .bottom) {
                ScrollView {
                    content
                        .padding(.horizontal, 16)
                        .padding(.top, 14)
                        .padding(.bottom, viewModel.hasSelection ? 96 : 14)
                        .frame(maxWidth: .infinity)
                }
                .refreshable {
                    await viewModel.loadFees()
                }
                .background(AppTheme.Palette.appBackground)

                if viewModel.hasSelection {
                    payBar
                }
            }
        }
        .task {
            if case .idle = viewModel.loadState {
                await viewModel.loadFees()
            }
        }
        // Payment success alert.
        .alert("Payment received", isPresented: paymentResultBinding, presenting: viewModel.paymentResult) { result in
            // Phase 58: offer immediate print/view from the success dialog.
            // Only shown when the server returned a receiptId (it always
            // does for verified payments, but guard anyway).
            if let id = result.receiptId {
                Button("View / Print Receipt") {
                    viewModel.dismissPaymentResult()
                    Task { await viewModel.fetchReceipt(id: id) }
                }
            }
            Button("OK", role: .cancel) { viewModel.dismissPaymentResult() }
        } message: { result in
            let receipt = result.receiptNo ?? "—"
            let amount = result.amount.map { String(format: "₹%.2f", $0) } ?? "—"
            Text("Receipt \(receipt) · \(amount)\n\(result.message ?? "")")
        }
        // Generic error alert.
        .alert("Couldn't complete payment", isPresented: paymentErrorBinding) {
            Button("OK", role: .cancel) { viewModel.dismissPaymentError() }
        } message: {
            Text(viewModel.paymentError ?? "")
        }
        // Phase 58: receipt fetch failed.
        .alert("Couldn't load receipt", isPresented: receiptErrorBinding) {
            Button("OK", role: .cancel) { viewModel.dismissReceiptError() }
        } message: {
            Text(viewModel.receiptError ?? "")
        }
        // Phase 58: when the fetch completes, hand the receipt to the printer.
        // System print sheet handles preview + Save-to-PDF; there's no separate
        // "view" surface to build.
        .onChange(of: viewModel.receiptToPrint?.receiptId) { _, newValue in
            guard newValue != nil, let receipt = viewModel.receiptToPrint else { return }
            ReceiptPrinter.present(receipt: receipt)
            // Clear so re-tapping Print re-fetches (catches server-side cancels).
            viewModel.dismissReceipt()
        }
        // Loading overlays.
        .overlay {
            if viewModel.isInitiatingOrder || viewModel.isVerifying {
                loadingOverlay(viewModel.isInitiatingOrder ? "Creating order…" : "Verifying payment…")
            } else if viewModel.isFetchingReceipt {
                loadingOverlay("Loading receipt…")
            }
        }
    }

    // MARK: - Top bar

    private var categoryPicker: some View {
        Picker("Category", selection: categoryBinding) {
            ForEach(FeeTypeCategory.allCases) { category in
                Text(category.displayName).tag(category)
            }
        }
        .pickerStyle(.segmented)
        .padding(.horizontal, 16)
        .padding(.vertical, 10)
        .background(AppTheme.Palette.navyBlue)
    }

    private var categoryBinding: Binding<FeeTypeCategory> {
        Binding(
            get: { viewModel.selectedCategory },
            set: { newValue in
                Task { await viewModel.selectCategory(newValue) }
            }
        )
    }

    // MARK: - Content

    @ViewBuilder
    private var content: some View {
        switch viewModel.loadState {
        case .idle, .loading:
            VStack(spacing: 14) {
                LoadingCard()
                LoadingCard()
                LoadingCard()
            }
        case .success(let summary):
            if summary.years.isEmpty {
                EmptyState(
                    systemImage: "checkmark.seal",
                    title: "No dues",
                    message: "Nothing to pay under \(viewModel.selectedCategory.displayName) right now."
                )
                .frame(minHeight: 280)
            } else {
                VStack(spacing: 18) {
                    ForEach(summary.years) { year in
                        yearSection(year)
                    }
                }
            }
        case .failure(let message):
            ErrorView(message: message) {
                Task { await viewModel.loadFees() }
            }
            .frame(minHeight: 280)
        }
    }

    // MARK: - Year section

    private func yearSection(_ year: MobileFeeSummaryDto) -> some View {
        let pending = year.lineItems.filter { !$0.isPaid }
        let paid = year.lineItems.filter { $0.isPaid }

        return VStack(alignment: .leading, spacing: 10) {
            yearHeader(year, pendingCount: pending.count)

            if pending.isEmpty {
                Text("No pending items for this year.")
                    .font(.appBodySmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
                    .padding(12)
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 12))
            } else {
                VStack(spacing: 8) {
                    ForEach(pending) { item in
                        itemRow(item, in: year, isPaid: false)
                    }
                }
            }

            if !paid.isEmpty {
                Text("Already paid")
                    .font(.appLabelSmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
                    .padding(.top, 4)
                VStack(spacing: 6) {
                    ForEach(paid) { item in
                        itemRow(item, in: year, isPaid: true)
                    }
                }
            }
        }
    }

    private func yearHeader(_ year: MobileFeeSummaryDto, pendingCount: Int) -> some View {
        HStack {
            VStack(alignment: .leading, spacing: 2) {
                Text(year.academicYear ?? "Academic year")
                    .font(.appTitleMedium)
                    .foregroundStyle(AppTheme.Palette.textPrimary)
                Text("Outstanding ₹\(amount(year.outstandingAmount)) · Paid ₹\(amount(year.paidAmount))")
                    .font(.appLabelSmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
            }
            Spacer()
            if pendingCount > 0 {
                Button {
                    viewModel.selectAll(in: year)
                } label: {
                    Text(viewModel.isYearFullySelected(year) ? "Clear" : "Select all")
                        .font(.appLabelSmall.bold())
                        .padding(.horizontal, 10)
                        .padding(.vertical, 5)
                        .background(AppTheme.Palette.navyContainer, in: Capsule())
                        .foregroundStyle(AppTheme.Palette.onNavyContainer)
                }
            }
        }
    }

    // MARK: - Item row

    private func itemRow(_ item: MobileFeeLineItemDto, in year: MobileFeeSummaryDto, isPaid: Bool) -> some View {
        let isSelected = viewModel.selectedIds.contains(item.id)
        let isFromActiveYear = viewModel.selectedYearId == nil || viewModel.selectedYearId == year.academicYearId

        // Not wrapped in a Button: a nested Button (the Print icon on paid rows,
        // Phase 58) inside a `.disabled` outer Button would inherit the disabled
        // state and never fire. Using `.onTapGesture` on the container keeps the
        // toggle behaviour while letting the inner Print button intercept its
        // own taps normally.
        return HStack(spacing: 12) {
                if !isPaid {
                    Image(systemName: isSelected ? "checkmark.square.fill" : "square")
                        .imageScale(.large)
                        .foregroundStyle(
                            isSelected
                                ? AppTheme.Palette.navyBlue
                                : (isFromActiveYear ? AppTheme.Palette.textSecondary : AppTheme.Palette.textSecondary.opacity(0.4))
                        )
                }

                VStack(alignment: .leading, spacing: 4) {
                    Text(itemTitle(for: item))
                        .font(.appBodyMedium)
                        .foregroundStyle(isPaid ? AppTheme.Palette.textSecondary : AppTheme.Palette.textPrimary)
                    if let subtitle = itemSubtitle(for: item) {
                        Text(subtitle)
                            .font(.appLabelSmall)
                            .foregroundStyle(AppTheme.Palette.textSecondary)
                    }
                    if item.concessionAmount > 0 {
                        Text("Concession ₹\(amount(item.concessionAmount))")
                            .font(.appLabelSmall)
                            .foregroundStyle(AppTheme.Palette.navyBlue)
                    }
                }

                Spacer()

                VStack(alignment: .trailing, spacing: 2) {
                    if isPaid {
                        Text("Paid")
                            .font(.appLabelSmall.bold())
                            .foregroundStyle(AppTheme.Palette.present)
                    } else {
                        Text("₹\(amount(item.outstanding))")
                            .font(.appLabelLarge)
                            .foregroundStyle(AppTheme.Palette.textPrimary)
                    }
                    Text("of ₹\(amount(item.amount))")
                        .font(.appLabelSmall)
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                }

                // Phase 58: in-line print action for items paid via this app
                // (server flags `createdBy == "Mobile App"` — payments from the
                // parent web portal or offline receipts hide the button).
                if item.canPrint, let receiptId = item.receiptId {
                    Button {
                        Task { await viewModel.fetchReceipt(id: receiptId) }
                    } label: {
                        Image(systemName: "printer")
                            .imageScale(.medium)
                            .padding(8)
                            .background(AppTheme.Palette.navyContainer, in: Circle())
                            .foregroundStyle(AppTheme.Palette.onNavyContainer)
                    }
                    .buttonStyle(.plain)
                    .accessibilityLabel("Print receipt")
                }
        }
        .padding(12)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 12))
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(
                    isSelected ? AppTheme.Palette.navyBlue : AppTheme.Palette.surfaceVariant,
                    lineWidth: isSelected ? 1.5 : 1
                )
        )
        .opacity(isPaid ? 0.7 : 1.0)
        .contentShape(Rectangle())
        .onTapGesture {
            guard !isPaid else { return }
            viewModel.toggle(item: item, in: year)
        }
    }

    private func itemTitle(for item: MobileFeeLineItemDto) -> String {
        if let route = item.routeName, !route.isEmpty { return route }
        if let hostel = item.hostelName, !hostel.isEmpty { return hostel }
        return item.feeTypeName ?? "Fee item"
    }

    private func itemSubtitle(for item: MobileFeeLineItemDto) -> String? {
        var pieces: [String] = []
        if let term = item.termName, !term.isEmpty { pieces.append(term) }
        if let period = item.feePeriodLabel, !period.isEmpty { pieces.append(period) }
        return pieces.isEmpty ? nil : pieces.joined(separator: " · ")
    }

    // MARK: - Pay bar

    private var payBar: some View {
        HStack {
            VStack(alignment: .leading, spacing: 2) {
                Text("\(viewModel.selectedIds.count) selected")
                    .font(.appLabelSmall)
                    .foregroundStyle(.white.opacity(0.85))
                Text("₹\(amount(viewModel.selectedTotal))")
                    .font(.appTitleMedium.bold())
                    .foregroundStyle(.white)
            }

            Spacer()

            Button {
                Task { await viewModel.initiatePayment() }
            } label: {
                HStack(spacing: 6) {
                    if viewModel.isInitiatingOrder {
                        ProgressView().progressViewStyle(.circular).tint(.white)
                    }
                    Text(viewModel.isInitiatingOrder ? "Creating order…" : "Pay now")
                        .font(.appLabelLarge.bold())
                }
                .padding(.horizontal, 16)
                .padding(.vertical, 10)
                .background(AppTheme.Palette.gold, in: Capsule())
                .foregroundStyle(.white)
            }
            .disabled(viewModel.isInitiatingOrder)
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 14)
        .background(AppTheme.Palette.navyBlue)
        .clipShape(UnevenRoundedRectangle(topLeadingRadius: 18, topTrailingRadius: 18))
        .shadow(color: .black.opacity(0.2), radius: 10, y: -4)
    }

    private func loadingOverlay(_ message: String) -> some View {
        ZStack {
            Color.black.opacity(0.15).ignoresSafeArea()
            VStack(spacing: 10) {
                ProgressView().progressViewStyle(.circular).tint(AppTheme.Palette.navyBlue)
                Text(message)
                    .font(.appLabelMedium)
                    .foregroundStyle(AppTheme.Palette.textPrimary)
            }
            .padding(20)
            .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 14))
        }
    }

    // MARK: - Bindings

    private var paymentResultBinding: Binding<Bool> {
        Binding(
            get: { viewModel.paymentResult != nil },
            set: { if !$0 { viewModel.dismissPaymentResult() } }
        )
    }

    private var paymentErrorBinding: Binding<Bool> {
        Binding(
            get: { viewModel.paymentError != nil },
            set: { if !$0 { viewModel.dismissPaymentError() } }
        )
    }

    private var receiptErrorBinding: Binding<Bool> {
        Binding(
            get: { viewModel.receiptError != nil },
            set: { if !$0 { viewModel.dismissReceiptError() } }
        )
    }

    // MARK: - Helpers

    private func amount(_ value: Double) -> String {
        // Indian-style grouping is a nice-to-have; use a plain "comma + 2dp" for now.
        let formatter = NumberFormatter()
        formatter.numberStyle = .decimal
        formatter.minimumFractionDigits = 2
        formatter.maximumFractionDigits = 2
        return formatter.string(from: NSNumber(value: value)) ?? String(format: "%.2f", value)
    }
}

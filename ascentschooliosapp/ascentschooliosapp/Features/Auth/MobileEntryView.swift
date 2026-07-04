import SwiftUI

struct MobileEntryView: View {
    @Bindable var viewModel: AuthViewModel
    @FocusState private var focused: Bool

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text("Parent login")
                .font(.appTitleLarge)
                .foregroundStyle(AppTheme.Palette.textPrimary)

            Text("Enter the mobile number registered with the school office. We'll text you a 6-digit code.")
                .font(.appBodySmall)
                .foregroundStyle(AppTheme.Palette.textSecondary)

            HStack(spacing: 8) {
                Text("+91")
                    .font(.appBodyLarge)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
                    .padding(.leading, 4)
                TextField("10-digit mobile", text: $viewModel.mobile)
                    .keyboardType(.numberPad)
                    .textContentType(.telephoneNumber)
                    .focused($focused)
                    .submitLabel(.send)
                    .onSubmit(submit)
            }
            .padding(.vertical, 12)
            .padding(.horizontal, 14)
            .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 12))
            .overlay(
                RoundedRectangle(cornerRadius: 12)
                    .stroke(AppTheme.Palette.surfaceVariant, lineWidth: 1)
            )

            Button(action: submit) {
                HStack {
                    if viewModel.isLoading {
                        ProgressView()
                            .progressViewStyle(.circular)
                            .tint(.white)
                    }
                    Text(viewModel.isLoading ? "Sending OTP…" : "Send OTP")
                        .font(.appLabelLarge)
                }
                .frame(maxWidth: .infinity)
                .padding(.vertical, 12)
            }
            .buttonStyle(.borderedProminent)
            .tint(AppTheme.Palette.navyBlue)
            .disabled(viewModel.isLoading || viewModel.mobile.filter(\.isNumber).count != 10)
        }
        .padding(24)
        .onAppear { focused = true }
    }

    private func submit() {
        Task { await viewModel.requestOtp() }
    }
}

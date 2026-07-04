import SwiftUI

struct OTPEntryView: View {
    @Bindable var viewModel: AuthViewModel
    let mobile: String

    var body: some View {
        VStack(spacing: 16) {
            VStack(spacing: 4) {
                Text("Enter the 6-digit code")
                    .font(.appTitleLarge)
                    .foregroundStyle(AppTheme.Palette.textPrimary)
                Text("Sent to +91 \(mobile)")
                    .font(.appBodySmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
            }
            .frame(maxWidth: .infinity)

            OTPField(code: $viewModel.otp)
                .padding(.vertical, 4)

            Button(action: submit) {
                HStack {
                    if viewModel.isLoading {
                        ProgressView()
                            .progressViewStyle(.circular)
                            .tint(.white)
                    }
                    Text(viewModel.isLoading ? "Verifying…" : "Verify")
                        .font(.appLabelLarge)
                }
                .frame(maxWidth: .infinity)
                .padding(.vertical, 12)
            }
            .buttonStyle(.borderedProminent)
            .tint(AppTheme.Palette.navyBlue)
            .disabled(viewModel.isLoading || viewModel.otp.count != 6)

            HStack(spacing: 8) {
                if viewModel.resendCountdown > 0 {
                    Text("Resend in \(viewModel.resendCountdown)s")
                        .font(.appLabelMedium)
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                } else {
                    Button("Resend OTP") {
                        Task { await viewModel.requestOtp() }
                    }
                    .font(.appLabelMedium)
                    .tint(AppTheme.Palette.navyBlue)
                    .disabled(viewModel.isLoading)
                }

                Spacer()

                Button("Change number") {
                    viewModel.backToMobile()
                }
                .font(.appLabelMedium)
                .tint(AppTheme.Palette.textSecondary)
                .disabled(viewModel.isLoading)
            }
        }
        .padding(24)
        .onChange(of: viewModel.otp) { _, newValue in
            if newValue.count == 6 && !viewModel.isLoading {
                submit()
            }
        }
    }

    private func submit() {
        Task { await viewModel.verifyOtp() }
    }
}

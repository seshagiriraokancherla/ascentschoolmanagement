import SwiftUI

// Phase 44 (Android parity: SchoolCodeScreen.kt) — first screen the generic
// "CHAK IN" build shows on cold start when no school code is stored. Baked
// flavors never reach this view.
//
// Flow: enter code → resolve() → show confirmation card ("This is your school:
// {name}") → confirm() persists the subdomain + branding → RootView drops the
// gate on next observation cycle.
struct SchoolCodeView: View {
    @State private var viewModel = SchoolCodeViewModel()
    @FocusState private var focused: Bool

    var body: some View {
        ZStack {
            AppTheme.loginGradient.ignoresSafeArea()

            ScrollView {
                VStack(spacing: 22) {
                    header
                    card
                }
                .padding(.horizontal, 20)
                .padding(.top, 60)
                .padding(.bottom, 32)
            }
        }
        .preferredColorScheme(.light)
        .alert("Couldn't continue", isPresented: errorBinding) {
            Button("OK", role: .cancel) { viewModel.clearError() }
        } message: {
            Text(currentErrorMessage ?? "")
        }
    }

    // MARK: - Pieces

    private var header: some View {
        VStack(spacing: 10) {
            // Generic-flavor logo uses the app icon (matching Android's
            // school-code screen). If a real logo asset ships later it can
            // slot in via AppInfo.loginLogo.
            Image(systemName: "graduationcap.circle.fill")
                .font(.system(size: 72))
                .foregroundStyle(.white)
                .shadow(color: .black.opacity(0.2), radius: 8, y: 4)
            Text("CHAK IN")
                .font(.appHeadlineSmall)
                .foregroundStyle(.white)
            Text("Powered by Ascent Schools")
                .font(.appLabelSmall)
                .foregroundStyle(.white.opacity(0.75))
        }
    }

    @ViewBuilder
    private var card: some View {
        VStack(alignment: .leading, spacing: 16) {
            switch viewModel.state {
            case .idle, .resolving, .error:
                codeEntry
            case .confirm(let school):
                confirmation(school: school)
            case .done:
                // Transient — RootView is already re-routing.
                ProgressView().progressViewStyle(.circular)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 30)
            }
        }
        .padding(20)
        .frame(maxWidth: .infinity)
        .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 24))
        .overlay(
            RoundedRectangle(cornerRadius: 24)
                .stroke(.white.opacity(0.25), lineWidth: 1)
        )
        .shadow(color: .black.opacity(0.15), radius: 18, y: 8)
    }

    private var codeEntry: some View {
        VStack(alignment: .leading, spacing: 14) {
            Text("Enter school code")
                .font(.appTitleLarge)
                .foregroundStyle(AppTheme.Palette.textPrimary)
            Text("Your school office will share a short code (typically 4 digits). Enter it below to continue.")
                .font(.appBodySmall)
                .foregroundStyle(AppTheme.Palette.textSecondary)

            TextField("e.g. 1000", text: $viewModel.code)
                .keyboardType(.numberPad)
                .textContentType(.oneTimeCode)
                .focused($focused)
                .font(.system(size: 22, weight: .semibold, design: .rounded))
                .padding(.vertical, 12)
                .padding(.horizontal, 14)
                .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 12))
                .overlay(
                    RoundedRectangle(cornerRadius: 12)
                        .stroke(AppTheme.Palette.surfaceVariant, lineWidth: 1)
                )
                .onSubmit(submit)
                .onAppear { focused = true }

            Button(action: submit) {
                HStack {
                    if viewModel.isBusy {
                        ProgressView()
                            .progressViewStyle(.circular)
                            .tint(.white)
                    }
                    Text(viewModel.isBusy ? "Looking up…" : "Continue")
                        .font(.appLabelLarge)
                }
                .frame(maxWidth: .infinity)
                .padding(.vertical, 12)
            }
            .buttonStyle(.borderedProminent)
            .tint(AppTheme.Palette.navyBlue)
            .disabled(viewModel.isBusy || viewModel.code.filter(\.isNumber).count < 3)
        }
    }

    private func confirmation(school: SchoolByCodeDto) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Is this your school?")
                .font(.appTitleLarge)
                .foregroundStyle(AppTheme.Palette.textPrimary)

            HStack(spacing: 12) {
                Circle()
                    .fill(AppTheme.Palette.navyContainer)
                    .frame(width: 44, height: 44)
                    .overlay(
                        Image(systemName: "graduationcap.fill")
                            .foregroundStyle(AppTheme.Palette.onNavyContainer)
                    )
                VStack(alignment: .leading, spacing: 2) {
                    Text(school.name ?? "School")
                        .font(.appTitleMedium)
                        .foregroundStyle(AppTheme.Palette.textPrimary)
                    Text(school.schoolCode)
                        .font(.appLabelSmall)
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                }
                Spacer()
            }
            .padding(12)
            .background(AppTheme.Palette.appBackground, in: RoundedRectangle(cornerRadius: 12))

            HStack(spacing: 10) {
                Button(action: { viewModel.reject() }) {
                    Text("Change")
                        .font(.appLabelLarge)
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 12)
                }
                .buttonStyle(.bordered)
                .tint(AppTheme.Palette.navyBlue)
                .disabled(viewModel.isBusy)

                Button {
                    Task { await viewModel.confirm(school) }
                } label: {
                    HStack {
                        if viewModel.isBusy {
                            ProgressView()
                                .progressViewStyle(.circular)
                                .tint(.white)
                        }
                        Text(viewModel.isBusy ? "Saving…" : "Yes, continue")
                            .font(.appLabelLarge)
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
                }
                .buttonStyle(.borderedProminent)
                .tint(AppTheme.Palette.navyBlue)
                .disabled(viewModel.isBusy)
            }
        }
    }

    // MARK: - Actions / bindings

    private func submit() {
        Task { await viewModel.resolve() }
    }

    private var currentErrorMessage: String? {
        if case .error(let msg) = viewModel.state { return msg }
        return nil
    }

    private var errorBinding: Binding<Bool> {
        Binding(
            get: { currentErrorMessage != nil },
            set: { if !$0 { viewModel.clearError() } }
        )
    }
}

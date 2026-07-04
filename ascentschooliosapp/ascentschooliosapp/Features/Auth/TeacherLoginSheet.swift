import SwiftUI

struct TeacherLoginSheet: View {
    let viewModel: AuthViewModel
    @Environment(\.dismiss) private var dismiss

    @State private var username: String = ""
    @State private var password: String = ""
    @FocusState private var focused: Field?

    private enum Field { case username, password }

    var body: some View {
        NavigationStack {
            VStack(alignment: .leading, spacing: 16) {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Staff login")
                        .font(.appHeadlineSmall)
                        .foregroundStyle(AppTheme.Palette.textPrimary)
                    Text("Sign in with the username and password issued by your school admin.")
                        .font(.appBodySmall)
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                }

                field("Username", text: $username, focus: .username, contentType: .username)
                field("Password", text: $password, focus: .password, contentType: .password, secure: true)

                Button(action: submit) {
                    HStack {
                        if viewModel.isLoading {
                            ProgressView().progressViewStyle(.circular).tint(.white)
                        }
                        Text(viewModel.isLoading ? "Signing in…" : "Sign in")
                            .font(.appLabelLarge)
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
                }
                .buttonStyle(.borderedProminent)
                .tint(AppTheme.Palette.navyBlue)
                .disabled(viewModel.isLoading || username.isEmpty || password.isEmpty)

                Spacer()
            }
            .padding(24)
            .background(AppTheme.Palette.appBackground)
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Cancel") { dismiss() }
                }
            }
            .navigationBarTitleDisplayMode(.inline)
            .onAppear { focused = .username }
        }
        .presentationDetents([.medium, .large])
        .presentationDragIndicator(.visible)
    }

    private func field(
        _ placeholder: String,
        text: Binding<String>,
        focus: Field,
        contentType: UITextContentType,
        secure: Bool = false
    ) -> some View {
        Group {
            if secure {
                SecureField(placeholder, text: text)
            } else {
                TextField(placeholder, text: text)
                    .textInputAutocapitalization(.never)
                    .autocorrectionDisabled()
            }
        }
        .textContentType(contentType)
        .focused($focused, equals: focus)
        .submitLabel(focus == .username ? .next : .go)
        .onSubmit {
            if focus == .username { focused = .password } else { submit() }
        }
        .padding(.vertical, 12)
        .padding(.horizontal, 14)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 12))
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(AppTheme.Palette.surfaceVariant, lineWidth: 1)
        )
    }

    private func submit() {
        Task { await viewModel.loginTeacher(username: username, password: password) }
    }
}

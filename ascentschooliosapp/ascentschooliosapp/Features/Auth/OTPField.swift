import SwiftUI

// 6-cell OTP picker. A single hidden TextField captures input (so iOS auto-fills
// from the incoming SMS via `.textContentType(.oneTimeCode)`); the visible grid
// just renders the typed digits.
struct OTPField: View {
    @Binding var code: String
    var length: Int = 6
    @FocusState private var isFocused: Bool

    var body: some View {
        ZStack {
            // Invisible TextField — handles keyboard input + SMS autofill.
            TextField("", text: $code)
                .keyboardType(.numberPad)
                .textContentType(.oneTimeCode)
                .focused($isFocused)
                .foregroundStyle(.clear)
                .accentColor(.clear)
                .frame(width: 1, height: 1)
                .opacity(0.001) // not 0 — iOS can ignore zero-opacity fields for first responder
                .onChange(of: code) { _, newValue in
                    let digits = newValue.filter(\.isNumber)
                    let trimmed = String(digits.prefix(length))
                    if trimmed != newValue { code = trimmed }
                }

            // Visible cell grid (mirrors `code`).
            HStack(spacing: 10) {
                ForEach(0..<length, id: \.self) { index in
                    cell(at: index)
                }
            }
            .contentShape(Rectangle())
            .onTapGesture { isFocused = true }
        }
        .onAppear { isFocused = true }
    }

    private func cell(at index: Int) -> some View {
        let char: String = {
            guard index < code.count else { return "" }
            return String(code[code.index(code.startIndex, offsetBy: index)])
        }()
        let isCurrent = index == code.count
        let highlight = isCurrent && isFocused

        return Text(char.isEmpty ? "·" : char)
            .font(.system(size: 26, weight: .semibold, design: .rounded))
            .frame(width: 44, height: 56)
            .foregroundStyle(
                char.isEmpty
                    ? AppTheme.Palette.textSecondary.opacity(0.4)
                    : AppTheme.Palette.textPrimary
            )
            .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 10))
            .overlay(
                RoundedRectangle(cornerRadius: 10)
                    .stroke(
                        highlight ? AppTheme.Palette.navyBlue : AppTheme.Palette.surfaceVariant,
                        lineWidth: highlight ? 2 : 1
                    )
            )
    }
}

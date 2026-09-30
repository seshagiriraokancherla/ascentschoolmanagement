import SwiftUI

// Phase 92: one chat bubble, shared by the parent and teacher chat screens.
// `isMine` right-aligns + navy-fills the bubble; the other side is left-aligned
// on a surface fill with the sender's name above. Long-press → Report (UGC).
struct MessageBubble: View {
    let message: MessageDto
    let isMine: Bool
    let onReport: (Int) -> Void

    var body: some View {
        HStack {
            if isMine { Spacer(minLength: 40) }

            VStack(alignment: isMine ? .trailing : .leading, spacing: 3) {
                if !isMine, let name = message.senderName, !name.isEmpty {
                    Text(name)
                        .font(.appLabelSmall.bold())
                        .foregroundStyle(AppTheme.Palette.navyBlue)
                }

                Text(bubbleText)
                    .font(.appBodyMedium)
                    .italic(message.isRemoved)
                    .foregroundStyle(textColor)
                    .fixedSize(horizontal: false, vertical: true)

                if let ts = message.createdAt?.friendlyDate(style: "d MMM, h:mm a") {
                    Text(ts)
                        .font(.appLabelSmall)
                        .foregroundStyle(isMine ? .white.opacity(0.7) : AppTheme.Palette.textSecondary)
                }
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 8)
            .background(bubbleColor, in: RoundedRectangle(cornerRadius: 14))
            .contextMenu {
                // Removed messages can't be reported again.
                if !message.isRemoved {
                    Button {
                        onReport(message.messageId)
                    } label: {
                        Label("Report", systemImage: "flag")
                    }
                }
            }

            if !isMine { Spacer(minLength: 40) }
        }
    }

    private var bubbleText: String {
        message.isRemoved ? "This message was removed." : message.body
    }

    private var bubbleColor: Color {
        if message.isRemoved { return AppTheme.Palette.surfaceVariant }
        return isMine ? AppTheme.Palette.navyBlue : AppTheme.Palette.appSurface
    }

    private var textColor: Color {
        if message.isRemoved { return AppTheme.Palette.textSecondary }
        return isMine ? .white : AppTheme.Palette.textPrimary
    }
}

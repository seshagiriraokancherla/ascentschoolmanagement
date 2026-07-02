import SwiftUI

// Counterpart of Android's `Shimmer.kt`: a diagonal gradient sweeps across the
// content forever, masked by the content itself so only opaque pixels shimmer.
// Apply via `.shimmer()` on any view.
struct ShimmerModifier: ViewModifier {
    @State private var phase: CGFloat = -1

    func body(content: Content) -> some View {
        content
            .overlay(
                GeometryReader { geometry in
                    LinearGradient(
                        gradient: Gradient(stops: [
                            .init(color: .white.opacity(0.0), location: 0.30),
                            .init(color: .white.opacity(0.55), location: 0.50),
                            .init(color: .white.opacity(0.0), location: 0.70),
                        ]),
                        startPoint: .topLeading,
                        endPoint: .bottomTrailing
                    )
                    .blendMode(.plusLighter)
                    .offset(x: geometry.size.width * phase)
                    .animation(
                        .linear(duration: 1.4).repeatForever(autoreverses: false),
                        value: phase
                    )
                }
                .mask(content)
            )
            .onAppear {
                phase = 1.5
            }
    }
}

extension View {
    func shimmer() -> some View {
        modifier(ShimmerModifier())
    }
}

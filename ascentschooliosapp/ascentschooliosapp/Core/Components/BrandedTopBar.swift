import SwiftUI

// Navy `ToolbarContent` block that replaces a stock navigation bar.
// Usage:
//   NavigationStack {
//       SomeScreen()
//           .toolbar { BrandedTopBar(title: "Attendance", onLogout: viewModel.logout) }
//           .toolbarBackground(AppTheme.Palette.navyBlue, for: .navigationBar)
//           .toolbarBackground(.visible, for: .navigationBar)
//           .toolbarColorScheme(.dark, for: .navigationBar)
//   }
struct BrandedTopBar: ToolbarContent {
    let title: String
    var onLogout: (() -> Void)? = nil
    // Phase 40 (Android parity): if this callback is supplied AND the parent
    // has more than one linked child, an overflow (⋮) menu replaces the plain
    // logout button. Passing `nil` (or a single-child list) keeps the old
    // single-icon layout — teachers and single-child parents don't see it.
    var onSwitchChild: (() -> Void)? = nil
    // Phase 46 (Android parity): "Change School" menu item, only supplied by
    // the generic-flavor parent home (baked flavors have their school hard-
    // coded and never expose the option). Adding this callback forces the
    // overflow menu on even for single-child parents.
    var onChangeSchool: (() -> Void)? = nil
    // Phase 65 pt2/3 (Android parity): global Refresh — reloads the current
    // screen's data AND re-runs the app-version check. Shown as a separate
    // leading-of-the-menu icon when supplied.
    var onRefresh: (() -> Void)? = nil

    var body: some ToolbarContent {
        ToolbarItem(placement: .principal) {
            Text(title)
                .font(.appTitleLarge)
                .foregroundStyle(.white)
        }
        if let onRefresh {
            ToolbarItem(placement: .topBarLeading) {
                Button(action: onRefresh) {
                    Image(systemName: "arrow.clockwise")
                        .imageScale(.large)
                }
                .tint(.white)
                .accessibilityLabel("Refresh")
            }
        }
        if onLogout != nil {
            ToolbarItem(placement: .topBarTrailing) {
                if useOverflowMenu, let onLogout {
                    Menu {
                        if let onSwitchChild {
                            Button {
                                onSwitchChild()
                            } label: {
                                Label("Switch Child", systemImage: "person.2")
                            }
                        }
                        if let onChangeSchool {
                            Button {
                                onChangeSchool()
                            } label: {
                                Label("Change School", systemImage: "building.columns")
                            }
                        }
                        Divider()
                        Button(role: .destructive) {
                            onLogout()
                        } label: {
                            Label("Log out", systemImage: "rectangle.portrait.and.arrow.right")
                        }
                    } label: {
                        Image(systemName: "ellipsis.circle")
                            .imageScale(.large)
                    }
                    .tint(.white)
                    .accessibilityLabel("More options")
                } else if let onLogout {
                    Button(action: onLogout) {
                        Image(systemName: "rectangle.portrait.and.arrow.right")
                            .imageScale(.large)
                    }
                    .tint(.white)
                    .accessibilityLabel("Log out")
                }
            }
        }
    }

    private var useOverflowMenu: Bool {
        onSwitchChild != nil || onChangeSchool != nil
    }
}

import SwiftUI

// Parent bottom-tab container. 8 tabs total — iOS 17 shows the first four
// directly and tucks the remaining four behind an auto-generated "More" tab.
// Order favours the most-frequent screens (Home / Attendance / Marks / Fees)
// up front so they're always one tap away.
struct ParentHomeView: View {

    enum Tab: Hashable {
        case home, attendance, marks, fees, homework, announcements, events, messages, profile
    }

    @State private var selectedTab: Tab = .home

    // Phase 40 (Android parity): children loaded once on appear so the ⋮ menu
    // can show "Switch Child" only when >1 child is linked. Each successful
    // switch bumps `childEpoch`, which is used as `.id(childEpoch)` on every
    // tab body — SwiftUI treats a changed id as a new identity and rebuilds
    // the subtree, recreating each tab's @State (viewmodel) so all screens
    // reload with the newly-selected child's data. Matches Android's
    // `key(childEpoch) { ... }` in HomeScreen.kt.
    @State private var children: [ChildDto] = []
    @State private var childEpoch: Int = 0
    @State private var showChildSheet: Bool = false
    // Phase 65 pt2: bumped by the global Refresh button; combined with childEpoch
    // into each tab's `.id`, so a tap rebuilds every tab (reloading its data).
    @State private var refreshEpoch: Int = 0

    private var store: KeychainTokenStore { KeychainTokenStore.shared }

    var body: some View {
        TabView(selection: $selectedTab) {
            tab(HomeOverviewView(), title: "Home", tab: .home, systemImage: "house.fill")
            tab(AttendanceView(), title: "Attendance", tab: .attendance, systemImage: "calendar")
            tab(MarksView(), title: "Marks", tab: .marks, systemImage: "chart.bar.fill")
            tab(FeesView(), title: "Fees", tab: .fees, systemImage: "indianrupeesign.circle")
            tab(HomeworkView(), title: "Homework", tab: .homework, systemImage: "book.closed")
            tab(AnnouncementsView(), title: "Notices", tab: .announcements, systemImage: "megaphone")
            tab(EventsView(), title: "Events", tab: .events, systemImage: "photo.on.rectangle.angled")
            tab(MessagesView(), title: "Messages", tab: .messages, systemImage: "bubble.left.and.bubble.right")
            tab(ProfileView(), title: "Profile", tab: .profile, systemImage: "person.circle")
        }
        .tint(AppTheme.Palette.navyBlue)
        .task {
            // Load children once per app session. Failures are silent — the
            // menu simply falls back to logout-only if the list can't be
            // fetched, matching Android's behaviour.
            if children.isEmpty {
                await loadChildren()
            }
        }
        .sheet(isPresented: $showChildSheet) {
            ChildSwitchSheet(
                children: children,
                currentStudentId: store.studentId,
                onSelect: handleChildSelection
            )
        }
    }

    @ViewBuilder
    private func tab<Content: View>(
        _ content: Content,
        title: String,
        tab: Tab,
        systemImage: String
    ) -> some View {
        // Hoist the optional callbacks out of the BrandedTopBar init to keep
        // Swift's type-checker happy — inline `condition ? { … } : nil`
        // ternaries in a ToolbarContent init defeat inference on newer Swift
        // versions ("Failed to produce diagnostic for expression").
        let switchChildAction: (() -> Void)? = children.count > 1
            ? { showChildSheet = true }
            : nil
        let changeSchoolAction: (() -> Void)? = AppInfo.isGenericApp
            ? { changeSchool() }
            : nil

        NavigationStack {
            content
                .navigationTitle(title)
                .navigationBarTitleDisplayMode(.inline)
                .toolbar {
                    BrandedTopBar(
                        title: title,
                        onLogout: logout,
                        onSwitchChild: switchChildAction,
                        onChangeSchool: changeSchoolAction,
                        onRefresh: globalRefresh
                    )
                }
                .toolbarBackground(AppTheme.Palette.navyBlue, for: .navigationBar)
                .toolbarBackground(.visible, for: .navigationBar)
                .toolbarColorScheme(.dark, for: .navigationBar)
        }
        // Rebuilds this tab's subtree (and therefore its @State-owned view model)
        // whenever a different child is selected OR the global Refresh is tapped.
        .id("\(childEpoch)-\(refreshEpoch)")
        .tabItem {
            Label(title, systemImage: systemImage)
        }
        .tag(tab)
    }

    // MARK: - Actions

    private func loadChildren() async {
        do {
            children = try await APIClient.shared.parentChildren()
        } catch {
            // Non-fatal: the menu just won't offer Switch Child. Logging left
            // out because APIClient already logs the failure at the network layer.
        }
    }

    private func handleChildSelection(linkId: Int) {
        Task {
            do {
                let auth = try await APIClient.shared.selectChild(linkId: linkId)
                KeychainTokenStore.shared.saveParentAuth(auth)
                KeychainTokenStore.shared.saveChildLinkId(linkId)
                // Trigger reload of every tab's view model.
                childEpoch &+= 1
            } catch {
                // Silently fail — the current child stays active. A real error
                // UI could be added later, but Android currently swallows too.
            }
        }
    }

    // Phase 65 pt2/3 (Android parity): reload every tab's data (via the epoch
    // bump → tab `.id` change) AND re-run the app-version check. A long-lived
    // 180-day session may never cold-start, so this is the mid-session path for
    // a forced update to surface (RootView observes AppConfigStore.isForced).
    private func globalRefresh() {
        refreshEpoch &+= 1
        Task { await AppConfigStore.shared.refresh() }
    }

    private func logout() {
        Task {
            try? await APIClient.shared.logoutParent()
            KeychainTokenStore.shared.clear()
            CookiePersistence.clear()
        }
    }

    // Phase 46 (Android parity): ends the current session AND wipes the
    // school selection so RootView routes back to SchoolCodeView on the next
    // observation cycle. No confirmation dialog — matches Android's decision.
    private func changeSchool() {
        Task {
            try? await APIClient.shared.logoutParent()
            KeychainTokenStore.shared.clear()
            KeychainTokenStore.shared.clearSchool()
            CookiePersistence.clear()
        }
    }
}

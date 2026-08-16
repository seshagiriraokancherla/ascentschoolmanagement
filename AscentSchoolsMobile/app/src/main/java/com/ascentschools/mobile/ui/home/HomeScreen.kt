package com.ascentschools.mobile.ui.home

import android.os.Build
import android.widget.Toast
import androidx.activity.compose.BackHandler
import androidx.annotation.RequiresApi
import androidx.compose.animation.*
import androidx.compose.animation.core.tween
import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.background
import androidx.compose.foundation.basicMarquee
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import com.ascentschools.mobile.data.api.ChildDto
import com.ascentschools.mobile.data.api.MobileFeeLineItemDto
import com.ascentschools.mobile.data.local.TokenStore
import com.ascentschools.mobile.data.repository.AuthRepository
import com.ascentschools.mobile.data.repository.StudentRepository
import kotlinx.coroutines.launch
import com.ascentschools.mobile.ui.announcements.AnnouncementsScreen
import com.ascentschools.mobile.ui.announcements.AnnouncementsUiState
import com.ascentschools.mobile.ui.announcements.AnnouncementsViewModel
import com.ascentschools.mobile.ui.calendar.CalendarScreen
import com.ascentschools.mobile.ui.calendar.CalendarViewModel
import com.ascentschools.mobile.ui.components.PullToRefresh
import com.ascentschools.mobile.ui.attendance.AttendanceScreen
import com.ascentschools.mobile.ui.attendance.AttendanceViewModel
import com.ascentschools.mobile.ui.events.EventsScreen
import com.ascentschools.mobile.ui.events.EventsUiState
import com.ascentschools.mobile.ui.events.EventsViewModel
import com.ascentschools.mobile.ui.fee.FeeScreen
import com.ascentschools.mobile.ui.fee.FeeUiState
import com.ascentschools.mobile.ui.fee.FeeViewModel
import com.ascentschools.mobile.ui.homework.HomeworkScreen
import com.ascentschools.mobile.ui.homework.HomeworkUiState
import com.ascentschools.mobile.ui.homework.HomeworkViewModel
import com.ascentschools.mobile.ui.marks.MarksScreen
import com.ascentschools.mobile.ui.marks.MarksViewModel
import com.ascentschools.mobile.ui.messages.MessagesScreen
import com.ascentschools.mobile.ui.messages.MessagesViewModel
import com.ascentschools.mobile.ui.profile.ProfileScreen
import com.ascentschools.mobile.ui.profile.ProfileViewModel
import com.ascentschools.mobile.ui.theme.NavyBlue

// Single source of truth for the 8 parent destinations. Both the bottom tab bar and
// the tile dashboard iterate this list, so a feature is defined in exactly one place.
private data class Feature(
    val label    : String,
    val icon     : ImageVector,
    val index    : Int,
    val gradient : Pair<Color, Color>   // fills the tile's icon square
)

private val features = listOf(
    Feature("Profile",    Icons.Default.Person,        0, Color(0xFF1E3A8A) to Color(0xFF3B82F6)),
    Feature("Attendance", Icons.Default.CalendarToday, 1, Color(0xFF0E7490) to Color(0xFF06B6D4)),
    Feature("Marks",      Icons.Default.Grade,         2, Color(0xFF4F46E5) to Color(0xFF6366F1)),
    Feature("Fees",       Icons.Default.Payments,      3, Color(0xFFB45309) to Color(0xFFF59E0B)),
    Feature("Homework",   Icons.Default.MenuBook,      4, Color(0xFF047857) to Color(0xFF10B981)),
    Feature("Notices",    Icons.Default.Notifications, 5, Color(0xFFBE123C) to Color(0xFFF43F5E)),
    Feature("Events",     Icons.Default.PhotoLibrary,  6, Color(0xFF6D28D9) to Color(0xFF8B5CF6)),
    Feature("Calendar",   Icons.Default.DateRange,     8, Color(0xFF0F766E) to Color(0xFF14B8A6)),
    Feature("Messages",   Icons.Default.Chat,          7, Color(0xFF0369A1) to Color(0xFF0EA5E9))
)

@RequiresApi(Build.VERSION_CODES.O)
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreen(
    repo              : StudentRepository,
    feeVm             : FeeViewModel,
    tokenStore        : TokenStore,
    authRepo          : AuthRepository,
    onInitiatePayment : (items: List<MobileFeeLineItemDto>, academicYearId: Int, feeTypeCategory: String) -> Unit,
    onChildSwitched   : () -> Unit,
    onChangeSchool    : (() -> Unit)? = null,   // generic single-app build only
    onLogout          : () -> Unit
) {
    var selectedTab   by remember { mutableIntStateOf(0) }
    var tilesView     by remember { mutableStateOf(tokenStore.tilesView) }
    var openedFeature by remember { mutableStateOf<Int?>(null) }   // tiles mode: which feature is open

    val scope   = rememberCoroutineScope()
    val context = LocalContext.current

    // Child switching (overflow menu). Children loaded once; menu item only shown
    // when the parent has more than one linked child.
    var menuExpanded by remember { mutableStateOf(false) }
    var showSwitch   by remember { mutableStateOf(false) }
    var switching    by remember { mutableStateOf(false) }
    var children     by remember { mutableStateOf<List<ChildDto>>(emptyList()) }

    // Branding for the tile dashboard band + tint (color hex, logo). Loaded once —
    // works for baked flavors too since /branding resolves from the school code.
    var brandColorHex by remember { mutableStateOf(tokenStore.brandingPrimaryColor) }
    var logoUrl       by remember { mutableStateOf(tokenStore.brandingLogoUrl) }

    LaunchedEffect(Unit) {
        authRepo.getChildren().onSuccess { children = it }
        authRepo.loadBranding().onSuccess {
            brandColorHex = tokenStore.brandingPrimaryColor
            logoUrl       = tokenStore.brandingLogoUrl
        }
    }

    val bandColor = parseBrandColor(brandColorHex, NavyBlue)
    val logoModel: Any = remember(logoUrl) {
        if (!logoUrl.isNullOrBlank()) logoUrl!!
        else context.packageManager.getApplicationIcon(context.packageName)
    }

    val profileVm       = remember { ProfileViewModel(repo) }
    val attendanceVm    = remember { AttendanceViewModel(repo) }
    val marksVm         = remember { MarksViewModel(repo) }
    val homeworkVm      = remember { HomeworkViewModel(repo) }
    val announcementsVm = remember { AnnouncementsViewModel(repo) }
    val eventsVm        = remember { EventsViewModel(repo) }
    val calendarVm      = remember { CalendarViewModel(repo) }
    val messagesVm      = remember { MessagesViewModel(repo) }

    // ── Live badges (tiles mode) — derived from already-loaded VM state ──────────
    val feeState by feeVm.uiState.collectAsState()
    val hwState  by homeworkVm.uiState.collectAsState()
    val annState by announcementsVm.uiState.collectAsState()
    val evState  by eventsVm.uiState.collectAsState()

    fun badgeFor(index: Int): Pair<String?, Boolean> = when (index) {
        3 -> {  // Fees — total outstanding for the loaded category; red = money owed
            val due = (feeState as? FeeUiState.Success)?.years?.sumOf { it.outstandingAmount } ?: 0.0
            if (due > 0) ("₹${due.toLong()}" to true) else (null to false)
        }
        4 -> ((hwState  as? HomeworkUiState.Success)?.homework?.size?.takeIf { it > 0 }?.toString() to false)
        5 -> ((annState as? AnnouncementsUiState.Success)?.announcements?.size?.takeIf { it > 0 }?.toString() to false)
        6 -> ((evState  as? EventsUiState.Success)?.events?.size?.takeIf { it > 0 }?.toString() to false)
        else -> null to false
    }

    // The one place each feature's screen is wired — reused by tabs AND tiles.
    @Composable
    fun FeatureContent(tab: Int) {
        when (tab) {
            0 -> PullToRefresh(onRefresh = { profileVm.load() })       { ProfileScreen(viewModel = profileVm) }
            1 -> PullToRefresh(onRefresh = { attendanceVm.load() })    { AttendanceScreen(viewModel = attendanceVm) }
            2 -> PullToRefresh(onRefresh = { marksVm.load() })         { MarksScreen(viewModel = marksVm) }
            3 -> PullToRefresh(onRefresh = { feeVm.loadFees() })       { FeeScreen(viewModel = feeVm, onInitiatePayment = onInitiatePayment) }
            4 -> PullToRefresh(onRefresh = { homeworkVm.load() })      { HomeworkScreen(viewModel = homeworkVm) }
            5 -> PullToRefresh(onRefresh = { announcementsVm.load() }) { AnnouncementsScreen(viewModel = announcementsVm) }
            6 -> PullToRefresh(onRefresh = { eventsVm.load() })        { EventsScreen(viewModel = eventsVm) }
            8 -> PullToRefresh(onRefresh = { calendarVm.load() })      { CalendarScreen(viewModel = calendarVm) }
            7 -> MessagesScreen(viewModel = messagesVm)
        }
    }

    // In tiles mode, the system back button returns to the grid instead of exiting.
    BackHandler(enabled = tilesView && openedFeature != null) { openedFeature = null }

    val inFeature = tilesView && openedFeature != null

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        if (inFeature) features.first { it.index == openedFeature }.label
                        else tokenStore.studentName ?: "Ascent Schools",
                        style = MaterialTheme.typography.titleMedium
                    )
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor          = if (tilesView) bandColor else NavyBlue,
                    titleContentColor       = Color.White,
                    navigationIconContentColor = Color.White,
                    actionIconContentColor  = Color.White
                ),
                navigationIcon = {
                    if (inFeature) {
                        IconButton(onClick = { openedFeature = null }) {
                            Icon(Icons.Default.ArrowBack, contentDescription = "Back")
                        }
                    }
                },
                actions = {
                    IconButton(onClick = {
                        // Global refresh — reload every tab's data.
                        profileVm.load(); attendanceVm.load(); marksVm.load()
                        homeworkVm.load(); announcementsVm.load(); eventsVm.load()
                        messagesVm.load(); feeVm.loadFees()
                    }) {
                        Icon(Icons.Default.Refresh, contentDescription = "Refresh")
                    }
                    IconButton(onClick = { menuExpanded = true }) {
                        Icon(Icons.Default.MoreVert, contentDescription = "Menu")
                    }
                    DropdownMenu(
                        expanded         = menuExpanded,
                        onDismissRequest = { menuExpanded = false }
                    ) {
                        DropdownMenuItem(
                            text        = { Text(if (tilesView) "Tab view" else "Tile view") },
                            leadingIcon = {
                                Icon(
                                    if (tilesView) Icons.Default.ViewList else Icons.Default.GridView,
                                    contentDescription = null
                                )
                            },
                            onClick = {
                                menuExpanded  = false
                                tilesView     = !tilesView
                                tokenStore.tilesView = tilesView
                                openedFeature = null
                            }
                        )
                        if (children.size > 1) {
                            DropdownMenuItem(
                                text        = { Text("Switch Child") },
                                leadingIcon = { Icon(Icons.Default.SwapHoriz, contentDescription = null) },
                                onClick     = { menuExpanded = false; showSwitch = true }
                            )
                        }
                        if (onChangeSchool != null) {
                            DropdownMenuItem(
                                text        = { Text("Change School") },
                                leadingIcon = { Icon(Icons.Default.School, contentDescription = null) },
                                onClick     = { menuExpanded = false; onChangeSchool() }
                            )
                        }
                        DropdownMenuItem(
                            text        = { Text("Logout") },
                            leadingIcon = { Icon(Icons.Default.Logout, contentDescription = null) },
                            onClick     = { menuExpanded = false; onLogout() }
                        )
                    }
                }
            )
        },
        bottomBar = {
            if (!tilesView) {
                NavigationBar(
                    containerColor = MaterialTheme.colorScheme.surface
                ) {
                    features.forEach { item ->
                        NavigationBarItem(
                            selected = selectedTab == item.index,
                            onClick  = { selectedTab = item.index },
                            icon     = { Icon(item.icon, contentDescription = item.label) },
                            label    = { Text(item.label, style = MaterialTheme.typography.labelSmall) },
                            colors   = NavigationBarItemDefaults.colors(
                                selectedIconColor   = NavyBlue,
                                selectedTextColor   = NavyBlue,
                                indicatorColor      = NavyBlue.copy(alpha = 0.12f),
                                unselectedIconColor = MaterialTheme.colorScheme.onSurfaceVariant,
                                unselectedTextColor = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        )
                    }
                }
            }
        }
    ) { innerPadding ->
        if (tilesView) {
            if (openedFeature == null) {
                // Ticker shows the latest two announcements as "date: notice" (createdAt is
                // IST — Phase 104). Not date-filtered, so a notice posted for a future day
                // (e.g. tomorrow's holiday) still shows. The Notices tab shows the full list.
                val ticker = (annState as? AnnouncementsUiState.Success)?.announcements
                    ?.sortedByDescending { it.createdAt ?: "" }
                    ?.take(2)
                    ?.mapNotNull { a ->
                        a.title?.takeIf { it.isNotBlank() }?.let { "${tickerDate(a.createdAt)}: $it" }
                    }
                    ?.joinToString("        •        ")
                    ?.takeIf { it.isNotBlank() }
                    ?: "Welcome to ${tokenStore.brandingName ?: "our school"}."
                val bgBrush = Brush.verticalGradient(
                    listOf(bandColor.copy(alpha = 0.10f), Color(0xFFFFFFFF))
                )

                Column(Modifier.fillMaxSize().padding(innerPadding)) {
                    LogoBand(bandColor, logoModel, tokenStore.brandingName ?: (tokenStore.studentName ?: ""))
                    NewsTicker(ticker, bandColor) { openedFeature = 5 }
                    Box(Modifier.weight(1f).fillMaxWidth().background(bgBrush)) {
                        LazyVerticalGrid(
                            columns               = GridCells.Fixed(3),
                            modifier              = Modifier.fillMaxSize(),
                            contentPadding        = PaddingValues(16.dp),
                            horizontalArrangement = Arrangement.spacedBy(12.dp),
                            verticalArrangement   = Arrangement.spacedBy(18.dp)
                        ) {
                            items(features) { f ->
                                val (badge, alert) = badgeFor(f.index)
                                IconTile(
                                    label      = f.label,
                                    icon       = f.icon,
                                    gradient   = f.gradient,
                                    badge      = badge,
                                    badgeAlert = alert,
                                    modifier   = Modifier.fillMaxWidth(),
                                    onClick    = { openedFeature = f.index }
                                )
                            }
                        }
                    }
                    PoweredByFooter()
                }
            } else {
                Box(Modifier.fillMaxSize().padding(innerPadding)) {
                    FeatureContent(openedFeature!!)
                }
            }
        } else {
            AnimatedContent(
                targetState   = selectedTab,
                transitionSpec = {
                    val forward = targetState > initialState
                    (fadeIn(tween(220)) + slideInHorizontally(tween(220)) { if (forward) it / 8 else -it / 8 }) togetherWith
                    (fadeOut(tween(150)) + slideOutHorizontally(tween(150)) { if (forward) -it / 8 else it / 8 })
                },
                label  = "tabContent",
                modifier = Modifier.padding(innerPadding)
            ) { tab ->
                FeatureContent(tab)
            }
        }
    }

    // ── Switch Child dialog ─────────────────────────────────────────────────
    if (showSwitch) {
        AlertDialog(
            onDismissRequest = { if (!switching) showSwitch = false },
            title = { Text("Switch Child") },
            text  = {
                Column {
                    children.forEach { child ->
                        val isCurrent = child.studentId == tokenStore.studentId
                        Card(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(vertical = 4.dp)
                                .clickable(enabled = !switching && !isCurrent) {
                                    switching = true
                                    scope.launch {
                                        authRepo.selectChild(child.linkId)
                                            .onSuccess {
                                                switching  = false
                                                showSwitch = false
                                                onChildSwitched()
                                            }
                                            .onFailure {
                                                switching = false
                                                Toast.makeText(
                                                    context,
                                                    it.message ?: "Failed to switch child",
                                                    Toast.LENGTH_SHORT
                                                ).show()
                                            }
                                    }
                                }
                        ) {
                            Row(
                                modifier = Modifier.padding(12.dp),
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Column(modifier = Modifier.weight(1f)) {
                                    Text(child.studentName, style = MaterialTheme.typography.titleSmall)
                                    Text(
                                        "${child.className} · ${child.admissionNo}",
                                        style = MaterialTheme.typography.bodySmall,
                                        color = MaterialTheme.colorScheme.onSurfaceVariant
                                    )
                                }
                                if (isCurrent) {
                                    Icon(
                                        Icons.Default.Check,
                                        contentDescription = "Current",
                                        tint = NavyBlue
                                    )
                                }
                            }
                        }
                    }
                    if (switching) {
                        Spacer(Modifier.height(12.dp))
                        CircularProgressIndicator(modifier = Modifier.align(Alignment.CenterHorizontally))
                    }
                }
            },
            confirmButton = {},
            dismissButton = {
                TextButton(onClick = { showSwitch = false }, enabled = !switching) {
                    Text("Cancel")
                }
            }
        )
    }
}

// ── Tile-dashboard chrome ──────────────────────────────────────────────────────

/** Colored band carrying the school logo + name (the "hero" without a photo). */
@Composable
private fun LogoBand(bandColor: Color, logoModel: Any, name: String) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .background(bandColor)
            .padding(vertical = 18.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Surface(shape = CircleShape, color = Color.White, modifier = Modifier.size(72.dp)) {
            AsyncImage(
                model              = logoModel,
                contentDescription = null,
                contentScale       = ContentScale.Fit,
                modifier           = Modifier.padding(8.dp)
            )
        }
        if (name.isNotBlank()) {
            Spacer(Modifier.height(8.dp))
            Text(
                name,
                color      = Color.White,
                fontWeight = FontWeight.Bold,
                style      = MaterialTheme.typography.titleMedium,
                textAlign  = TextAlign.Center
            )
        }
    }
}

/** Formats an ISO createdAt string (e.g. "2026-07-28T09:15:00") as dd-MM-yyyy for the ticker. */
private fun tickerDate(raw: String?): String {
    val d = raw?.take(10) ?: return ""          // yyyy-MM-dd
    val p = d.split("-")
    return if (p.size == 3) "${p[2]}-${p[1]}-${p[0]}" else d
}

/** A red "News" chip + an auto-scrolling latest-announcements line; tap opens Notices. */
@OptIn(ExperimentalFoundationApi::class)
@Composable
private fun NewsTicker(text: String, bandColor: Color, onClick: () -> Unit) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .height(40.dp)
            .background(bandColor.copy(alpha = 0.85f))
            .clickable { onClick() },
        verticalAlignment = Alignment.CenterVertically
    ) {
        Surface(color = Color(0xFFE53935), shape = RoundedCornerShape(topEnd = 6.dp, bottomEnd = 6.dp)) {
            Text(
                "News",
                color      = Color.White,
                fontWeight = FontWeight.Bold,
                fontSize   = 13.sp,
                modifier   = Modifier.padding(horizontal = 14.dp, vertical = 9.dp)
            )
        }
        Spacer(Modifier.width(12.dp))
        Text(
            text,
            color    = Color.White,
            fontSize = 13.sp,
            maxLines = 1,
            modifier = Modifier.weight(1f).basicMarquee()
        )
        Spacer(Modifier.width(12.dp))
    }
}

@Composable
private fun PoweredByFooter() {
    Text(
        "Powered by Ascent Info Solutions",
        modifier   = Modifier.fillMaxWidth().padding(vertical = 10.dp),
        textAlign  = TextAlign.Center,
        fontSize   = 11.sp,
        color      = MaterialTheme.colorScheme.onSurfaceVariant
    )
}

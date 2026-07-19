package com.ascentschools.mobile.ui.home

import android.os.Build
import android.widget.Toast
import androidx.annotation.RequiresApi
import androidx.compose.animation.*
import androidx.compose.animation.core.tween
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import com.ascentschools.mobile.data.api.ChildDto
import com.ascentschools.mobile.data.api.MobileFeeLineItemDto
import com.ascentschools.mobile.data.local.TokenStore
import com.ascentschools.mobile.data.repository.AuthRepository
import com.ascentschools.mobile.data.repository.StudentRepository
import kotlinx.coroutines.launch
import com.ascentschools.mobile.ui.announcements.AnnouncementsScreen
import com.ascentschools.mobile.ui.announcements.AnnouncementsViewModel
import com.ascentschools.mobile.ui.components.PullToRefresh
import com.ascentschools.mobile.ui.attendance.AttendanceScreen
import com.ascentschools.mobile.ui.attendance.AttendanceViewModel
import com.ascentschools.mobile.ui.events.EventsScreen
import com.ascentschools.mobile.ui.events.EventsViewModel
import com.ascentschools.mobile.ui.fee.FeeScreen
import com.ascentschools.mobile.ui.fee.FeeViewModel
import com.ascentschools.mobile.ui.homework.HomeworkScreen
import com.ascentschools.mobile.ui.homework.HomeworkViewModel
import com.ascentschools.mobile.ui.marks.MarksScreen
import com.ascentschools.mobile.ui.marks.MarksViewModel
import com.ascentschools.mobile.ui.messages.MessagesScreen
import com.ascentschools.mobile.ui.messages.MessagesViewModel
import com.ascentschools.mobile.ui.profile.ProfileScreen
import com.ascentschools.mobile.ui.profile.ProfileViewModel
import com.ascentschools.mobile.ui.theme.NavyBlue

private data class BottomNavItem(val label: String, val icon: ImageVector, val index: Int)

private val navItems = listOf(
    BottomNavItem("Home",       Icons.Default.Home,          0),
    BottomNavItem("Attendance", Icons.Default.CalendarToday, 1),
    BottomNavItem("Marks",      Icons.Default.Grade,         2),
    BottomNavItem("Fees",       Icons.Default.Payments,      3),
    BottomNavItem("Homework",   Icons.Default.MenuBook,      4),
    BottomNavItem("Notices",    Icons.Default.Notifications, 5),
    BottomNavItem("Events",     Icons.Default.PhotoLibrary,  6),
    BottomNavItem("Messages",   Icons.Default.Chat,          7)
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
    var selectedTab by remember { mutableIntStateOf(0) }

    val scope   = rememberCoroutineScope()
    val context = LocalContext.current

    // Child switching (overflow menu). Children loaded once; menu item only shown
    // when the parent has more than one linked child.
    var menuExpanded by remember { mutableStateOf(false) }
    var showSwitch   by remember { mutableStateOf(false) }
    var switching    by remember { mutableStateOf(false) }
    var children     by remember { mutableStateOf<List<ChildDto>>(emptyList()) }

    LaunchedEffect(Unit) {
        authRepo.getChildren().onSuccess { children = it }
    }

    val profileVm       = remember { ProfileViewModel(repo) }
    val attendanceVm    = remember { AttendanceViewModel(repo) }
    val marksVm         = remember { MarksViewModel(repo) }
    val homeworkVm      = remember { HomeworkViewModel(repo) }
    val announcementsVm = remember { AnnouncementsViewModel(repo) }
    val eventsVm        = remember { EventsViewModel(repo) }
    val messagesVm      = remember { MessagesViewModel(repo) }

    Scaffold(
        topBar = {
            TopAppBar(
                title  = {
                    Text(
                        tokenStore.studentName ?: "Ascent Schools",
                        style = MaterialTheme.typography.titleMedium
                    )
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor         = NavyBlue,
                    titleContentColor      = Color.White,
                    actionIconContentColor = Color.White
                ),
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
            NavigationBar(
                containerColor = MaterialTheme.colorScheme.surface
            ) {
                navItems.forEach { item ->
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
    ) { innerPadding ->
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
            when (tab) {
                0 -> PullToRefresh(onRefresh = { profileVm.load() })       { ProfileScreen(viewModel = profileVm) }
                1 -> PullToRefresh(onRefresh = { attendanceVm.load() })    { AttendanceScreen(viewModel = attendanceVm) }
                2 -> PullToRefresh(onRefresh = { marksVm.load() })         { MarksScreen(viewModel = marksVm) }
                3 -> PullToRefresh(onRefresh = { feeVm.loadFees() })       { FeeScreen(viewModel = feeVm, onInitiatePayment = onInitiatePayment) }
                4 -> PullToRefresh(onRefresh = { homeworkVm.load() })      { HomeworkScreen(viewModel = homeworkVm) }
                5 -> PullToRefresh(onRefresh = { announcementsVm.load() }) { AnnouncementsScreen(viewModel = announcementsVm) }
                6 -> PullToRefresh(onRefresh = { eventsVm.load() })        { EventsScreen(viewModel = eventsVm) }
                7 -> MessagesScreen(viewModel = messagesVm)
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

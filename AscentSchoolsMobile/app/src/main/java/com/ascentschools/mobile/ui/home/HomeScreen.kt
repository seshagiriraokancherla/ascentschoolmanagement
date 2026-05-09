package com.ascentschools.mobile.ui.home

import android.os.Build
import androidx.annotation.RequiresApi
import androidx.compose.animation.*
import androidx.compose.animation.core.tween
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import com.ascentschools.mobile.data.api.MobileFeeLineItemDto
import com.ascentschools.mobile.data.local.TokenStore
import com.ascentschools.mobile.data.repository.StudentRepository
import com.ascentschools.mobile.ui.announcements.AnnouncementsScreen
import com.ascentschools.mobile.ui.announcements.AnnouncementsViewModel
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
    BottomNavItem("Events",     Icons.Default.PhotoLibrary,  6)
)

@RequiresApi(Build.VERSION_CODES.O)
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreen(
    repo              : StudentRepository,
    feeVm             : FeeViewModel,
    tokenStore        : TokenStore,
    onInitiatePayment : (items: List<MobileFeeLineItemDto>, academicYearId: Int, paymentModeId: Int) -> Unit,
    onLogout          : () -> Unit
) {
    var selectedTab by remember { mutableIntStateOf(0) }

    val profileVm       = remember { ProfileViewModel(repo) }
    val attendanceVm    = remember { AttendanceViewModel(repo) }
    val marksVm         = remember { MarksViewModel(repo) }
    val homeworkVm      = remember { HomeworkViewModel(repo) }
    val announcementsVm = remember { AnnouncementsViewModel(repo) }
    val eventsVm        = remember { EventsViewModel(repo) }

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
                    IconButton(onClick = onLogout) {
                        Icon(Icons.Default.Logout, contentDescription = "Logout")
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
                0 -> ProfileScreen(viewModel = profileVm)
                1 -> AttendanceScreen(viewModel = attendanceVm)
                2 -> MarksScreen(viewModel = marksVm)
                3 -> FeeScreen(viewModel = feeVm, onInitiatePayment = onInitiatePayment)
                4 -> HomeworkScreen(viewModel = homeworkVm)
                5 -> AnnouncementsScreen(viewModel = announcementsVm)
                6 -> EventsScreen(viewModel = eventsVm)
            }
        }
    }
}

package com.ascentschools.mobile.ui.teacher

import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import com.ascentschools.mobile.ui.events.EventsScreen

// Read-only — same events gallery parents see (school-wide + this class's, if a class
// was selected on the teacher home screen). Teachers don't create events here; that
// stays a school-web-app action (Events Gallery management page).
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TeacherEventsScreen(
    classId  : Int?,
    viewModel: TeacherViewModel,
    onBack   : () -> Unit
) {
    val uiState by viewModel.eventsState.collectAsState()

    LaunchedEffect(classId) { viewModel.loadEvents(classId) }

    Scaffold(
        topBar = {
            TopAppBar(
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "Back") }
                },
                title = { Text("Events", fontWeight = FontWeight.Bold) }
            )
        }
    ) { padding ->
        EventsScreen(
            uiState  = uiState,
            onRetry  = { viewModel.loadEvents(classId) },
            modifier = Modifier.padding(padding)
        )
    }
}

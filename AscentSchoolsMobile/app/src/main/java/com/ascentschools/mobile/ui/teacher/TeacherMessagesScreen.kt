package com.ascentschools.mobile.ui.teacher

import android.os.Build
import androidx.annotation.RequiresApi
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.MessageThreadDto
import com.ascentschools.mobile.ui.theme.NavyBlue
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter

/**
 * Teacher's message inbox — one row per child whose parent has written in.
 * Only threads for the classes this teacher is assigned to appear (the server
 * filters by class_teacher_assignments).
 */
@RequiresApi(Build.VERSION_CODES.O)
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TeacherMessagesScreen(
    viewModel : TeacherViewModel,
    onOpen    : (threadId: Int) -> Unit,
    onBack    : () -> Unit
) {
    val threads   by viewModel.threads.collectAsState()
    val isLoading by viewModel.isLoading.collectAsState()
    val uiState   by viewModel.uiState.collectAsState()

    val snackbarHost = remember { SnackbarHostState() }

    LaunchedEffect(Unit) { viewModel.loadThreads() }

    LaunchedEffect(uiState) {
        when (val s = uiState) {
            is TeacherUiState.Error   -> { snackbarHost.showSnackbar(s.message); viewModel.clearUiState() }
            is TeacherUiState.Success -> { snackbarHost.showSnackbar(s.message); viewModel.clearUiState() }
            else -> {}
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHost) },
        topBar = {
            TopAppBar(
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
                title = { Text("Messages", fontWeight = FontWeight.Bold) },
                actions = {
                    IconButton(onClick = { viewModel.loadThreads() }) {
                        Icon(Icons.Default.Refresh, contentDescription = "Refresh")
                    }
                }
            )
        }
    ) { padding ->
        Box(Modifier.padding(padding).fillMaxSize()) {
            when {
                isLoading && threads.isEmpty() ->
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator()
                    }

                threads.isEmpty() ->
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Icon(
                                Icons.Default.ChatBubbleOutline, contentDescription = null,
                                modifier = Modifier.size(48.dp),
                                tint = MaterialTheme.colorScheme.onBackground.copy(alpha = 0.3f)
                            )
                            Spacer(Modifier.height(12.dp))
                            Text(
                                "No messages",
                                color = MaterialTheme.colorScheme.onBackground.copy(alpha = 0.5f)
                            )
                            Spacer(Modifier.height(4.dp))
                            Text(
                                "Parents of your classes can message you here.",
                                fontSize = 12.sp,
                                color = MaterialTheme.colorScheme.onBackground.copy(alpha = 0.4f)
                            )
                        }
                    }

                else -> LazyColumn(
                    modifier            = Modifier.fillMaxSize(),
                    contentPadding      = PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    items(threads, key = { it.threadId }) { t ->
                        ThreadRow(t) { onOpen(t.threadId) }
                    }
                }
            }
        }
    }
}

@RequiresApi(Build.VERSION_CODES.O)
@Composable
private fun ThreadRow(thread: MessageThreadDto, onClick: () -> Unit) {
    val when_ = runCatching {
        thread.lastMessageAt?.let {
            LocalDateTime.parse(it.take(19)).format(DateTimeFormatter.ofPattern("dd MMM"))
        } ?: ""
    }.getOrDefault(thread.lastMessageAt?.take(10) ?: "")

    Card(
        modifier = Modifier.fillMaxWidth().clickable(onClick = onClick),
        colors   = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
        Row(
            Modifier.padding(14.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(Modifier.weight(1f)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        thread.studentName ?: "—",
                        fontWeight = FontWeight.SemiBold,
                        fontSize   = 15.sp
                    )
                    if (thread.status == "Blocked") {
                        Spacer(Modifier.width(6.dp))
                        Icon(
                            Icons.Default.Block, contentDescription = "Blocked",
                            modifier = Modifier.size(14.dp),
                            tint = MaterialTheme.colorScheme.error.copy(alpha = 0.7f)
                        )
                    }
                }
                Text(
                    listOfNotNull(thread.className, thread.sectionName).joinToString(" · "),
                    fontSize = 11.sp,
                    color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f)
                )
                Spacer(Modifier.height(6.dp))
                Text(
                    thread.lastMessageBody ?: "",
                    fontSize = 13.sp,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                    color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.7f)
                )
            }
            Spacer(Modifier.width(10.dp))
            Column(horizontalAlignment = Alignment.End) {
                Text(
                    when_,
                    fontSize = 10.sp,
                    color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.45f)
                )
                if (thread.unreadCount > 0) {
                    Spacer(Modifier.height(6.dp))
                    Surface(shape = MaterialTheme.shapes.large, color = NavyBlue) {
                        Text(
                            thread.unreadCount.toString(),
                            modifier = Modifier.padding(horizontal = 7.dp, vertical = 2.dp),
                            fontSize = 10.sp,
                            fontWeight = FontWeight.Bold,
                            color = MaterialTheme.colorScheme.onPrimary
                        )
                    }
                }
            }
        }
    }
}

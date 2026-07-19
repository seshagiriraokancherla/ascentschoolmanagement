package com.ascentschools.mobile.ui.teacher

import android.os.Build
import androidx.annotation.RequiresApi
import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.combinedClickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.Send
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.MessageDto
import com.ascentschools.mobile.ui.theme.NavyBlue
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter

/**
 * Teacher's side of one conversation. Several teachers may share a class, so a
 * reply here is from whoever is logged in — the thread is common to all of them.
 */
@RequiresApi(Build.VERSION_CODES.O)
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TeacherChatScreen(
    threadId  : Int,
    viewModel : TeacherViewModel,
    onBack    : () -> Unit
) {
    val detail    by viewModel.thread.collectAsState()
    val isLoading by viewModel.isLoading.collectAsState()
    val uiState   by viewModel.uiState.collectAsState()

    val snackbarHost = remember { SnackbarHostState() }
    val listState    = rememberLazyListState()
    var menuExpanded by remember { mutableStateOf(false) }
    var reportTarget by remember { mutableStateOf<MessageDto?>(null) }
    var text         by remember { mutableStateOf("") }

    LaunchedEffect(threadId) {
        viewModel.clearThread()
        viewModel.loadThread(threadId)
    }

    LaunchedEffect(uiState) {
        when (val s = uiState) {
            is TeacherUiState.Error   -> { snackbarHost.showSnackbar(s.message); viewModel.clearUiState() }
            is TeacherUiState.Success -> { snackbarHost.showSnackbar(s.message); viewModel.clearUiState() }
            else -> {}
        }
    }

    val messages  = detail?.messages ?: emptyList()
    val thread    = detail?.thread
    val isBlocked = thread?.status == "Blocked"
    // A teacher may lift a school-side block but not one the parent set.
    val blockedByParent = isBlocked && thread?.blockedByType == "parent"

    LaunchedEffect(messages.size) {
        if (messages.isNotEmpty()) listState.animateScrollToItem(messages.size - 1)
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
                title = {
                    Column {
                        Text(thread?.studentName ?: "Conversation", fontWeight = FontWeight.Bold)
                        Text(
                            listOfNotNull(thread?.className, thread?.sectionName).joinToString(" · "),
                            fontSize = 12.sp,
                            color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
                        )
                    }
                },
                actions = {
                    IconButton(onClick = { menuExpanded = true }) {
                        Icon(Icons.Default.MoreVert, contentDescription = "Menu")
                    }
                    DropdownMenu(expanded = menuExpanded, onDismissRequest = { menuExpanded = false }) {
                        if (!isBlocked) {
                            DropdownMenuItem(
                                text        = { Text("Block conversation") },
                                leadingIcon = { Icon(Icons.Default.Block, contentDescription = null) },
                                onClick     = { menuExpanded = false; viewModel.setThreadBlocked(threadId, true) }
                            )
                        } else if (!blockedByParent) {
                            DropdownMenuItem(
                                text        = { Text("Unblock conversation") },
                                leadingIcon = { Icon(Icons.Default.LockOpen, contentDescription = null) },
                                onClick     = { menuExpanded = false; viewModel.setThreadBlocked(threadId, false) }
                            )
                        }
                    }
                }
            )
        }
    ) { padding ->
        // imePadding lifts the reply box above the soft keyboard (edge-to-edge app:
        // adjustResize alone doesn't resize the content, so the IME inset is consumed here).
        Column(Modifier.padding(padding).fillMaxSize().imePadding()) {

            if (isLoading && messages.isEmpty()) {
                Box(Modifier.weight(1f).fillMaxWidth(), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator()
                }
            } else if (messages.isEmpty()) {
                Box(Modifier.weight(1f).fillMaxWidth(), contentAlignment = Alignment.Center) {
                    Text(
                        "No messages yet",
                        color = MaterialTheme.colorScheme.onBackground.copy(alpha = 0.5f)
                    )
                }
            } else {
                LazyColumn(
                    state               = listState,
                    modifier            = Modifier.weight(1f).fillMaxWidth(),
                    contentPadding      = PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    items(messages, key = { it.messageId }) { m ->
                        ChatBubble(
                            message  = m,
                            isMine   = m.senderType == "teacher",
                            onReport = { reportTarget = m }
                        )
                    }
                }
            }

            HorizontalDivider()

            if (isBlocked) {
                Row(
                    Modifier.fillMaxWidth().padding(16.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Icon(
                        Icons.Default.Block, contentDescription = null,
                        modifier = Modifier.size(18.dp),
                        tint = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f)
                    )
                    Spacer(Modifier.width(8.dp))
                    Text(
                        if (blockedByParent) "The parent blocked this conversation."
                        else "This conversation is blocked.",
                        fontSize = 12.sp,
                        color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
                    )
                }
            } else {
                Row(
                    Modifier.fillMaxWidth().padding(horizontal = 12.dp, vertical = 8.dp),
                    verticalAlignment = Alignment.Bottom
                ) {
                    OutlinedTextField(
                        value         = text,
                        onValueChange = { if (it.length <= 2000) text = it },
                        placeholder   = { Text("Reply", fontSize = 14.sp) },
                        modifier      = Modifier.weight(1f),
                        maxLines      = 4,
                        shape         = MaterialTheme.shapes.large
                    )
                    Spacer(Modifier.width(8.dp))
                    FilledIconButton(
                        onClick = { viewModel.reply(threadId, text); text = "" },
                        enabled = text.isNotBlank() && !isLoading,
                        colors  = IconButtonDefaults.filledIconButtonColors(containerColor = NavyBlue)
                    ) {
                        if (isLoading) {
                            CircularProgressIndicator(
                                modifier = Modifier.size(18.dp),
                                strokeWidth = 2.dp,
                                color = MaterialTheme.colorScheme.onPrimary
                            )
                        } else {
                            Icon(Icons.AutoMirrored.Filled.Send, contentDescription = "Send")
                        }
                    }
                }
            }
        }
    }

    reportTarget?.let { msg ->
        var reason by remember(msg.messageId) { mutableStateOf("") }
        AlertDialog(
            onDismissRequest = { reportTarget = null },
            title = { Text("Report message") },
            text = {
                Column {
                    Text(
                        "The school will review this message.",
                        fontSize = 13.sp,
                        color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.7f)
                    )
                    Spacer(Modifier.height(12.dp))
                    OutlinedTextField(
                        value         = reason,
                        onValueChange = { if (it.length <= 500) reason = it },
                        label         = { Text("Reason (optional)") },
                        modifier      = Modifier.fillMaxWidth(),
                        maxLines      = 3
                    )
                }
            },
            confirmButton = {
                TextButton(onClick = {
                    viewModel.reportMessage(threadId, msg.messageId, reason.ifBlank { null })
                    reportTarget = null
                }) { Text("Report") }
            },
            dismissButton = { TextButton(onClick = { reportTarget = null }) { Text("Cancel") } }
        )
    }
}

@RequiresApi(Build.VERSION_CODES.O)
@OptIn(ExperimentalFoundationApi::class)
@Composable
private fun ChatBubble(
    message  : MessageDto,
    isMine   : Boolean,
    onReport : () -> Unit
) {
    val removed = message.status == "Removed"
    var showMenu by remember { mutableStateOf(false) }

    val time = runCatching {
        LocalDateTime.parse(message.createdAt.take(19)).format(DateTimeFormatter.ofPattern("dd MMM, HH:mm"))
    }.getOrDefault(message.createdAt.take(16).replace('T', ' '))

    Row(
        Modifier.fillMaxWidth(),
        horizontalArrangement = if (isMine) Arrangement.End else Arrangement.Start
    ) {
        Box {
            Surface(
                shape = MaterialTheme.shapes.medium,
                color = when {
                    removed -> MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f)
                    isMine  -> NavyBlue.copy(alpha = 0.12f)
                    else    -> MaterialTheme.colorScheme.surfaceVariant
                },
                modifier = Modifier
                    .widthIn(max = 280.dp)
                    .then(
                        if (!isMine && !removed)
                            Modifier.combinedClickable(
                                onClick     = { },
                                onLongClick = { showMenu = true }
                            )
                        else Modifier
                    )
            ) {
                Column(Modifier.padding(horizontal = 12.dp, vertical = 8.dp)) {
                    if (!message.senderName.isNullOrBlank()) {
                        Text(
                            message.senderName,
                            fontSize   = 11.sp,
                            fontWeight = FontWeight.SemiBold,
                            color      = NavyBlue
                        )
                        Spacer(Modifier.height(2.dp))
                    }
                    if (removed) {
                        Text(
                            "This message was removed.",
                            fontSize  = 13.sp,
                            fontStyle = FontStyle.Italic,
                            color     = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f)
                        )
                    } else {
                        Text(message.body, fontSize = 14.sp)
                    }
                    Spacer(Modifier.height(4.dp))
                    Text(
                        time,
                        fontSize = 10.sp,
                        color    = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.45f)
                    )
                }
            }

            DropdownMenu(expanded = showMenu, onDismissRequest = { showMenu = false }) {
                DropdownMenuItem(
                    text        = { Text("Report message") },
                    leadingIcon = { Icon(Icons.Default.Flag, contentDescription = null) },
                    onClick     = { showMenu = false; onReport() }
                )
            }
        }
    }
}

package com.ascentschools.mobile.ui.messages

import android.os.Build
import androidx.annotation.RequiresApi
import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.combinedClickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.material.icons.Icons
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
import com.ascentschools.mobile.data.api.ParentThreadViewDto
import com.ascentschools.mobile.ui.theme.NavyBlue
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter

/**
 * Parent chat with the child's class teacher(s). One continuous conversation —
 * a parent has exactly one thread per child, so there's no thread list here.
 * When no teacher is assigned to the child's class, canMessage is false and the
 * server's reason is shown instead of the composer.
 */
@RequiresApi(Build.VERSION_CODES.O)
@Composable
fun MessagesScreen(
    viewModel: MessagesViewModel,
    modifier : Modifier = Modifier
) {
    val uiState by viewModel.uiState.collectAsState()
    val isBusy  by viewModel.isBusy.collectAsState()
    val toast   by viewModel.toast.collectAsState()

    val snackbarHost = remember { SnackbarHostState() }
    var reportTarget by remember { mutableStateOf<MessageDto?>(null) }

    LaunchedEffect(toast) {
        toast?.let { snackbarHost.showSnackbar(it); viewModel.clearToast() }
    }

    // imePadding lifts the composer above the soft keyboard. Needed because the app
    // is edge-to-edge, where windowSoftInputMode=adjustResize no longer resizes the
    // content on its own — the IME inset has to be consumed in Compose.
    Box(modifier.fillMaxSize().imePadding()) {
        when (val s = uiState) {
            is MessagesUiState.Loading -> Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                CircularProgressIndicator()
            }
            is MessagesUiState.Error -> Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                ErrorState(s.message) { viewModel.load() }
            }
            is MessagesUiState.Success -> ChatContent(
                view      = s.view,
                isBusy    = isBusy,
                onSend    = { viewModel.send(it) },
                onReport  = { reportTarget = it },
                onBlock   = { viewModel.setBlocked(it) }
            )
        }

        SnackbarHost(snackbarHost, modifier = Modifier.align(Alignment.BottomCenter))
    }

    reportTarget?.let { msg ->
        ReportDialog(
            message    = msg,
            onDismiss  = { reportTarget = null },
            onConfirm  = { reason -> viewModel.report(msg.messageId, reason); reportTarget = null }
        )
    }
}

@RequiresApi(Build.VERSION_CODES.O)
@Composable
private fun ChatContent(
    view     : ParentThreadViewDto,
    isBusy   : Boolean,
    onSend   : (String) -> Unit,
    onReport : (MessageDto) -> Unit,
    onBlock  : (Boolean) -> Unit
) {
    val messages = view.messages ?: emptyList()
    val listState = rememberLazyListState()
    val isBlocked = view.status == "Blocked"
    val blockedByMe = isBlocked && view.blockedByType == "parent"

    // Keep the newest message in view as the conversation grows.
    LaunchedEffect(messages.size) {
        if (messages.isNotEmpty()) listState.animateScrollToItem(messages.size - 1)
    }

    Column(Modifier.fillMaxSize()) {

        RecipientHeader(view.teachers ?: emptyList(), isBlocked, blockedByMe, onBlock)

        if (messages.isEmpty()) {
            Box(Modifier.weight(1f).fillMaxWidth(), contentAlignment = Alignment.Center) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Icon(
                        Icons.Default.ChatBubbleOutline, contentDescription = null,
                        modifier = Modifier.size(48.dp),
                        tint = MaterialTheme.colorScheme.onBackground.copy(alpha = 0.3f)
                    )
                    Spacer(Modifier.height(12.dp))
                    Text(
                        "No messages yet",
                        color = MaterialTheme.colorScheme.onBackground.copy(alpha = 0.5f)
                    )
                    if (view.canMessage) {
                        Spacer(Modifier.height(4.dp))
                        Text(
                            "Send a message to your child's class teacher.",
                            fontSize = 12.sp,
                            color = MaterialTheme.colorScheme.onBackground.copy(alpha = 0.4f)
                        )
                    }
                }
            }
        } else {
            LazyColumn(
                state               = listState,
                modifier            = Modifier.weight(1f).fillMaxWidth(),
                contentPadding      = PaddingValues(16.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                items(messages, key = { it.messageId }) { m ->
                    MessageBubble(
                        message  = m,
                        isMine   = m.senderType == "parent",
                        onReport = { onReport(m) }
                    )
                }
            }
        }

        Composer(
            canMessage = view.canMessage,
            reason     = view.reason,
            isBusy     = isBusy,
            onSend     = onSend
        )
    }
}

@Composable
private fun RecipientHeader(
    teachers    : List<String>,
    isBlocked   : Boolean,
    blockedByMe : Boolean,
    onBlock     : (Boolean) -> Unit
) {
    Surface(
        color = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.4f),
        modifier = Modifier.fillMaxWidth()
    ) {
        Row(
            Modifier.padding(horizontal = 16.dp, vertical = 10.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(Modifier.weight(1f)) {
                Text(
                    if (teachers.isEmpty()) "Class teacher" else "To: " + teachers.joinToString(", "),
                    fontSize   = 13.sp,
                    fontWeight = FontWeight.Medium
                )
                if (teachers.size > 1) {
                    Text(
                        "Any of them may reply",
                        fontSize = 11.sp,
                        color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f)
                    )
                }
            }
            // Block is a Play Store requirement for in-app messaging.
            if (!isBlocked) {
                TextButton(onClick = { onBlock(true) }) { Text("Block", fontSize = 12.sp) }
            } else if (blockedByMe) {
                TextButton(onClick = { onBlock(false) }) { Text("Unblock", fontSize = 12.sp) }
            }
        }
    }
    HorizontalDivider()
}

@RequiresApi(Build.VERSION_CODES.O)
@OptIn(ExperimentalFoundationApi::class)
@Composable
private fun MessageBubble(
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
                    // Long-press to report — the other side's messages only.
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
                    if (!isMine && !message.senderName.isNullOrBlank()) {
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
                            fontSize   = 13.sp,
                            fontStyle  = FontStyle.Italic,
                            color      = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f)
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

@Composable
private fun Composer(
    canMessage : Boolean,
    reason     : String?,
    isBusy     : Boolean,
    onSend     : (String) -> Unit
) {
    if (!canMessage) {
        HorizontalDivider()
        Row(
            Modifier.fillMaxWidth().padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(
                Icons.Default.Info, contentDescription = null,
                modifier = Modifier.size(18.dp),
                tint = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f)
            )
            Spacer(Modifier.width(8.dp))
            Text(
                reason ?: "Messaging is unavailable.",
                fontSize = 12.sp,
                color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
            )
        }
        return
    }

    var text by remember { mutableStateOf("") }

    HorizontalDivider()
    Row(
        Modifier.fillMaxWidth().padding(horizontal = 12.dp, vertical = 8.dp),
        verticalAlignment = Alignment.Bottom
    ) {
        OutlinedTextField(
            value         = text,
            onValueChange = { if (it.length <= 2000) text = it },
            placeholder   = { Text("Message", fontSize = 14.sp) },
            modifier      = Modifier.weight(1f),
            maxLines      = 4,
            shape         = MaterialTheme.shapes.large
        )
        Spacer(Modifier.width(8.dp))
        FilledIconButton(
            onClick = { onSend(text); text = "" },
            enabled = text.isNotBlank() && !isBusy,
            colors  = IconButtonDefaults.filledIconButtonColors(containerColor = NavyBlue)
        ) {
            if (isBusy) {
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

@Composable
private fun ReportDialog(
    message   : MessageDto,
    onDismiss : () -> Unit,
    onConfirm : (String?) -> Unit
) {
    var reason by remember { mutableStateOf("") }

    AlertDialog(
        onDismissRequest = onDismiss,
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
            TextButton(onClick = { onConfirm(reason.ifBlank { null }) }) { Text("Report") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Cancel") } }
    )
}

@Composable
private fun ErrorState(message: String, onRetry: () -> Unit) {
    Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.padding(16.dp)) {
        Text(message, color = MaterialTheme.colorScheme.error)
        Spacer(Modifier.height(12.dp))
        Button(onClick = onRetry) { Text("Retry") }
    }
}

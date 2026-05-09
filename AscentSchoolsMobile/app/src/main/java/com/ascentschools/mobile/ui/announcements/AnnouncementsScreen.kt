package com.ascentschools.mobile.ui.announcements

import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AttachFile
import androidx.compose.material.icons.filled.NotificationImportant
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.AnnouncementDto

@Composable
fun AnnouncementsScreen(
    viewModel: AnnouncementsViewModel,
    modifier : Modifier = Modifier
) {
    val uiState by viewModel.uiState.collectAsState()

    Box(modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        when (val s = uiState) {
            is AnnouncementsUiState.Loading -> CircularProgressIndicator()
            is AnnouncementsUiState.Error   -> ErrorState(s.message) { viewModel.load() }
            is AnnouncementsUiState.Success -> AnnouncementsContent(s.announcements)
        }
    }
}

@Composable
private fun AnnouncementsContent(announcements: List<AnnouncementDto>) {
    if (announcements.isEmpty()) {
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            Text("No announcements",
                color = MaterialTheme.colorScheme.onBackground.copy(alpha = 0.5f))
        }
        return
    }

    LazyColumn(
        modifier            = Modifier.fillMaxSize(),
        contentPadding      = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp)
    ) {
        items(announcements) { ann -> AnnouncementCard(ann) }
    }
}

@Composable
private fun AnnouncementCard(ann: AnnouncementDto) {
    val pinned  = ann.isPinned
    val context = LocalContext.current
    Card(
        shape    = RoundedCornerShape(12.dp),
        modifier = Modifier.fillMaxWidth(),
        colors   = CardDefaults.cardColors(
            containerColor = if (pinned)
                MaterialTheme.colorScheme.primaryContainer.copy(alpha = 0.3f)
            else
                MaterialTheme.colorScheme.surface
        )
    ) {
        Column(Modifier.padding(14.dp)) {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment     = Alignment.CenterVertically
            ) {
                Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.weight(1f)) {
                    if (pinned) {
                        Icon(Icons.Default.NotificationImportant, contentDescription = "Pinned",
                            modifier = Modifier.size(14.dp),
                            tint     = MaterialTheme.colorScheme.primary)
                        Spacer(Modifier.width(4.dp))
                    }
                    Text(ann.title ?: "", fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                }
                ScopeChip(ann.scope)
            }

            if (!ann.description.isNullOrBlank()) {
                Spacer(Modifier.height(6.dp))
                Text(ann.description, fontSize = 13.sp, lineHeight = 18.sp)
            }

            if (!ann.attachmentUrl.isNullOrBlank()) {
                Spacer(Modifier.height(6.dp))
                OutlinedButton(
                    onClick  = {
                        context.startActivity(
                            Intent(Intent.ACTION_VIEW, Uri.parse(ann.attachmentUrl))
                        )
                    },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Icon(Icons.Default.AttachFile, contentDescription = null,
                        modifier = Modifier.size(16.dp))
                    Spacer(Modifier.width(6.dp))
                    Text("Download / View Document", fontSize = 13.sp)
                }
            }

            Spacer(Modifier.height(8.dp))
            Text(ann.createdAt ?: "", fontSize = 11.sp,
                color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.4f))
        }
    }
}

@Composable
private fun ScopeChip(scope: String?) {
    val (label, color) = when (scope) {
        "Class"  -> "Class"  to Color(0xFF1677FF)
        "School" -> "School" to Color(0xFF722ED1)
        else     -> "All"    to Color(0xFF52C41A)
    }
    Surface(shape = RoundedCornerShape(12.dp), color = color.copy(alpha = 0.12f)) {
        Text(label, fontSize = 11.sp, color = color,
            modifier = Modifier.padding(horizontal = 8.dp, vertical = 3.dp))
    }
}

@Composable
private fun ErrorState(message: String, onRetry: () -> Unit) {
    Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.padding(16.dp)) {
        Text(message, color = MaterialTheme.colorScheme.error)
        Spacer(Modifier.height(12.dp))
        Button(onClick = onRetry) { Text("Retry") }
    }
}

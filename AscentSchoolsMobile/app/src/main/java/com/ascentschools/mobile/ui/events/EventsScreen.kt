package com.ascentschools.mobile.ui.events

import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AttachFile
import androidx.compose.material.icons.filled.NotificationImportant
import androidx.compose.material.icons.filled.OpenInNew
import androidx.compose.material.icons.filled.Photo
import androidx.compose.material.icons.filled.PlayCircle
import androidx.compose.material.icons.filled.SmartDisplay
import androidx.compose.foundation.background
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import com.ascentschools.mobile.data.api.SchoolEventDto

@Composable
fun EventsScreen(
    viewModel: EventsViewModel,
    modifier : Modifier = Modifier
) {
    val uiState by viewModel.uiState.collectAsState()

    Box(modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        when (val s = uiState) {
            is EventsUiState.Loading -> CircularProgressIndicator()
            is EventsUiState.Error   -> ErrorState(s.message) { viewModel.load() }
            is EventsUiState.Success -> EventsContent(s.events)
        }
    }
}

@Composable
private fun EventsContent(events: List<SchoolEventDto>) {
    if (events.isEmpty()) {
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            Text(
                "No events yet",
                color = MaterialTheme.colorScheme.onBackground.copy(alpha = 0.5f)
            )
        }
        return
    }

    LazyColumn(
        modifier            = Modifier.fillMaxSize(),
        contentPadding      = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        item {
            Text(
                "School Events (${events.size})",
                fontWeight = FontWeight.SemiBold,
                fontSize   = 15.sp,
                modifier   = Modifier.padding(bottom = 4.dp)
            )
        }
        items(events) { event -> EventCard(event) }
    }
}

@Composable
private fun EventCard(event: SchoolEventDto) {
    val context  = LocalContext.current
    val isVideo  = event.mediaType == "video"
    val pinned   = event.isPinned

    // Determine the best thumbnail URL:
    // - explicit thumbnail_url if set
    // - for images: media_url itself
    // - for YouTube: extract video ID and use YouTube thumbnail API
    val displayThumbnail = when {
        !event.thumbnailUrl.isNullOrBlank() -> event.thumbnailUrl
        !isVideo                             -> event.mediaUrl
        else                                 -> youtubeThumbnail(event.mediaUrl)
    }

    Card(
        shape    = RoundedCornerShape(12.dp),
        modifier = Modifier
            .fillMaxWidth()
            .clickable {
                val url = event.mediaUrl ?: return@clickable
                context.startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(url)))
            },
        colors = CardDefaults.cardColors(
            containerColor = if (pinned)
                MaterialTheme.colorScheme.primaryContainer.copy(alpha = 0.25f)
            else
                MaterialTheme.colorScheme.surface
        )
    ) {
        Column {
            // Thumbnail
            if (!displayThumbnail.isNullOrBlank()) {
                Box {
                    AsyncImage(
                        model             = displayThumbnail,
                        contentDescription = event.title,
                        contentScale      = ContentScale.Crop,
                        modifier          = Modifier
                            .fillMaxWidth()
                            .height(180.dp)
                    )
                    // Video overlay — dark scrim + large play icon + "YouTube" label
                    if (isVideo) {
                        Box(
                            modifier = Modifier
                                .matchParentSize()
                                .background(Color.Black.copy(alpha = 0.45f))
                        )
                        Column(
                            modifier            = Modifier.align(Alignment.Center),
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Icon(
                                imageVector        = Icons.Default.SmartDisplay,
                                contentDescription = "Play video",
                                tint               = Color.White,
                                modifier           = Modifier.size(64.dp)
                            )
                            Spacer(Modifier.height(4.dp))
                            Surface(
                                shape = RoundedCornerShape(4.dp),
                                color = Color(0xFFFF0000)
                            ) {
                                Text(
                                    "YouTube",
                                    color    = Color.White,
                                    fontSize = 11.sp,
                                    fontWeight = FontWeight.Bold,
                                    modifier = Modifier.padding(horizontal = 8.dp, vertical = 3.dp)
                                )
                            }
                        }
                    }
                }
            } else {
                // Placeholder when no thumbnail available
                Box(
                    Modifier
                        .fillMaxWidth()
                        .height(100.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Icon(
                        imageVector        = if (isVideo) Icons.Default.PlayCircle else Icons.Default.Photo,
                        contentDescription = null,
                        tint               = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.3f),
                        modifier           = Modifier.size(48.dp)
                    )
                }
            }

            // Text content
            Column(Modifier.padding(12.dp)) {
                Row(
                    Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment     = Alignment.CenterVertically
                ) {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        modifier          = Modifier.weight(1f)
                    ) {
                        if (pinned) {
                            Icon(
                                Icons.Default.NotificationImportant,
                                contentDescription = "Pinned",
                                modifier           = Modifier.size(14.dp),
                                tint               = MaterialTheme.colorScheme.primary
                            )
                            Spacer(Modifier.width(4.dp))
                        }
                        Text(
                            event.title ?: "",
                            fontWeight = FontWeight.SemiBold,
                            fontSize   = 14.sp
                        )
                    }
                    MediaTypeBadge(isVideo)
                }

                if (!event.description.isNullOrBlank()) {
                    Spacer(Modifier.height(4.dp))
                    Text(
                        event.description,
                        fontSize   = 12.sp,
                        lineHeight = 18.sp,
                        color      = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.7f)
                    )
                }

                // Attachment download button
                if (!event.attachmentUrl.isNullOrBlank()) {
                    Spacer(Modifier.height(6.dp))
                    OutlinedButton(
                        onClick  = {
                            context.startActivity(
                                Intent(Intent.ACTION_VIEW, Uri.parse(event.attachmentUrl))
                            )
                        },
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Icon(
                            Icons.Default.AttachFile,
                            contentDescription = null,
                            modifier           = Modifier.size(16.dp)
                        )
                        Spacer(Modifier.width(6.dp))
                        Text("Download / View Document", fontSize = 13.sp)
                    }
                }

                if (event.media?.isNotEmpty() == true) {
                    Spacer(Modifier.height(8.dp))
                    com.ascentschools.mobile.ui.components.MediaAttachments(event.media)
                }

                Spacer(Modifier.height(8.dp))
                Row(
                    Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment     = Alignment.CenterVertically
                ) {
                    Text(
                        event.eventDate ?: "",
                        fontSize = 11.sp,
                        color    = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.45f)
                    )
                    Icon(
                        Icons.Default.OpenInNew,
                        contentDescription = "Open",
                        modifier           = Modifier.size(14.dp),
                        tint               = MaterialTheme.colorScheme.primary
                    )
                }
            }
        }
    }
}

@Composable
private fun MediaTypeBadge(isVideo: Boolean) {
    val (label, color) = if (isVideo)
        "Video" to Color(0xFFFF4D4F)
    else
        "Image" to Color(0xFF1677FF)

    Surface(
        shape = RoundedCornerShape(10.dp),
        color = color.copy(alpha = 0.12f)
    ) {
        Text(
            label,
            fontSize = 10.sp,
            color    = color,
            modifier = Modifier.padding(horizontal = 7.dp, vertical = 2.dp)
        )
    }
}

@Composable
private fun ErrorState(message: String, onRetry: () -> Unit) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        modifier            = Modifier.padding(16.dp)
    ) {
        Text(message, color = MaterialTheme.colorScheme.error)
        Spacer(Modifier.height(12.dp))
        Button(onClick = onRetry) { Text("Retry") }
    }
}

/** Extract a YouTube video thumbnail URL from a YouTube link. */
private fun youtubeThumbnail(url: String?): String? {
    if (url.isNullOrBlank()) return null
    // Handles youtu.be/ID and youtube.com/watch?v=ID
    val id = Regex("""(?:youtu\.be/|[?&]v=)([A-Za-z0-9_-]{11})""")
        .find(url)?.groupValues?.get(1) ?: return null
    return "https://img.youtube.com/vi/$id/hqdefault.jpg"
}

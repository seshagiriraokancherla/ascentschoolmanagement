package com.ascentschools.mobile.ui.components

import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AttachFile
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material3.Icon
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import com.ascentschools.mobile.data.api.MediaUploadDto

/** Renders R2-uploaded attachments: images inline (Coil), everything else as a tappable button. */
@Composable
fun MediaAttachments(media: List<MediaUploadDto>?, modifier: Modifier = Modifier) {
    if (media.isNullOrEmpty()) return
    val context = LocalContext.current
    Column(modifier) {
        media.forEach { m ->
            val url = m.fileUrl ?: return@forEach
            if (m.fileType == "image") {
                AsyncImage(
                    model = url,
                    contentDescription = m.fileName,
                    contentScale = ContentScale.Crop,
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(180.dp)
                        .padding(vertical = 4.dp)
                        .clip(RoundedCornerShape(8.dp))
                        .clickable { context.startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(url))) }
                )
            } else {
                OutlinedButton(
                    onClick = { context.startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(url))) },
                    modifier = Modifier.fillMaxWidth().padding(vertical = 2.dp)
                ) {
                    Icon(
                        if (m.fileType == "video") Icons.Default.PlayArrow else Icons.Default.AttachFile,
                        contentDescription = null, modifier = Modifier.size(15.dp)
                    )
                    Spacer(Modifier.width(6.dp))
                    Text(m.fileName ?: "Attachment", fontSize = 12.sp, maxLines = 1)
                }
            }
        }
    }
}

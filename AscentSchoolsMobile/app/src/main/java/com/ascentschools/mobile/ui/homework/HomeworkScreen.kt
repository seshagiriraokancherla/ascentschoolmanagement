package com.ascentschools.mobile.ui.homework

import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AttachFile
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.HomeworkDto

@Composable
fun HomeworkScreen(
    viewModel: HomeworkViewModel,
    modifier : Modifier = Modifier
) {
    val uiState by viewModel.uiState.collectAsState()

    Box(modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        when (val s = uiState) {
            is HomeworkUiState.Loading -> CircularProgressIndicator()
            is HomeworkUiState.Error   -> ErrorState(s.message) { viewModel.load() }
            is HomeworkUiState.Success -> HomeworkContent(s.homework)
        }
    }
}

@Composable
private fun HomeworkContent(homework: List<HomeworkDto>) {
    if (homework.isEmpty()) {
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            Text("No homework assigned",
                color = MaterialTheme.colorScheme.onBackground.copy(alpha = 0.5f))
        }
        return
    }

    LazyColumn(
        modifier            = Modifier.fillMaxSize(),
        contentPadding      = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp)
    ) {
        item {
            Text("Homework (${homework.size})", fontWeight = FontWeight.SemiBold,
                fontSize = 15.sp, modifier = Modifier.padding(bottom = 4.dp))
        }
        items(homework) { hw -> HomeworkCard(hw) }
    }
}

@Composable
private fun HomeworkCard(hw: HomeworkDto) {
    val context = LocalContext.current
    Card(
        shape    = RoundedCornerShape(12.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(Modifier.padding(14.dp)) {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment     = Alignment.Top
            ) {
                Column(Modifier.weight(1f)) {
                    Text(hw.title ?: "", fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                    if (!hw.subjectName.isNullOrBlank()) {
                        Text(hw.subjectName, fontSize = 12.sp,
                            color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f))
                    }
                }
                Text("Due: ${hw.dueDate ?: ""}", fontSize = 12.sp,
                    color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f))
            }

            if (!hw.description.isNullOrBlank()) {
                Spacer(Modifier.height(8.dp))
                Text(hw.description, fontSize = 13.sp)
            }

            if (!hw.attachmentUrl.isNullOrBlank()) {
                Spacer(Modifier.height(8.dp))
                OutlinedButton(
                    onClick  = {
                        context.startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(hw.attachmentUrl)))
                    },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Icon(Icons.Default.AttachFile, contentDescription = null,
                        modifier = Modifier.size(15.dp))
                    Spacer(Modifier.width(6.dp))
                    Text("Download / View Document", fontSize = 12.sp)
                }
            }

            if (hw.attachments?.isNotEmpty() == true) {
                Spacer(Modifier.height(8.dp))
                hw.attachments!!.forEach { att ->
                    OutlinedButton(
                        onClick  = {
                            val url = att.filePath ?: return@OutlinedButton
                            context.startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(url)))
                        },
                        modifier = Modifier.fillMaxWidth().padding(vertical = 2.dp)
                    ) {
                        Icon(Icons.Default.AttachFile, contentDescription = null,
                            modifier = Modifier.size(15.dp))
                        Spacer(Modifier.width(6.dp))
                        Text(att.fileName, fontSize = 12.sp, maxLines = 1)
                    }
                }
            }

            Spacer(Modifier.height(6.dp))
            Text("Assigned: ${hw.assignedDate ?: ""}", fontSize = 11.sp,
                color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.4f))
        }
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

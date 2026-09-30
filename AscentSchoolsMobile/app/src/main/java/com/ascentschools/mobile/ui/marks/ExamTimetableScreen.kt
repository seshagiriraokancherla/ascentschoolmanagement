package com.ascentschools.mobile.ui.marks

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.ExamTimetableGroupDto
import com.ascentschools.mobile.data.api.ExamTimetableSubjectDto
import com.ascentschools.mobile.ui.theme.NavyBlue
import java.time.LocalDate
import java.time.LocalTime
import java.time.format.DateTimeFormatter
import java.time.format.TextStyle
import java.util.Locale

// Read-only exam schedule — one card per exam type, subjects listed by date. Sourced from
// Master Data → Exam Master's exam_date on the web app; nothing here lets a viewer edit it.
// Shared by the parent Marks-screen tab AND the teacher's own screen (both pass this same
// uiState shape so the rendering logic lives in exactly one place).

sealed class ExamTimetableUiState {
    object Loading : ExamTimetableUiState()
    data class Success(val groups: List<ExamTimetableGroupDto>) : ExamTimetableUiState()
    data class Error(val message: String) : ExamTimetableUiState()
}

@Composable
fun ExamTimetableScreen(
    uiState: ExamTimetableUiState,
    onRetry: () -> Unit,
    modifier: Modifier = Modifier
) {
    Box(modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        when (val s = uiState) {
            is ExamTimetableUiState.Loading -> CircularProgressIndicator()
            is ExamTimetableUiState.Error   -> ErrorState(s.message, onRetry)
            is ExamTimetableUiState.Success -> ExamTimetableContent(s.groups)
        }
    }
}

@Composable
private fun ExamTimetableContent(groups: List<ExamTimetableGroupDto>) {
    if (groups.isEmpty()) {
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.padding(24.dp)) {
                Text(
                    "Exam timetable not published yet",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }
        return
    }

    LazyColumn(
        modifier            = Modifier.fillMaxSize(),
        contentPadding      = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        items(groups, key = { it.examTypeId }) { group -> ExamTypeCard(group) }
    }
}

@Composable
private fun ExamTypeCard(group: ExamTimetableGroupDto) {
    Card(
        shape    = RoundedCornerShape(16.dp),
        colors   = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(Modifier.padding(16.dp)) {
            Text(
                group.examTypeName,
                style      = MaterialTheme.typography.titleSmall,
                fontWeight = FontWeight.SemiBold,
                color      = NavyBlue
            )
            Spacer(Modifier.height(10.dp))
            group.subjects.forEachIndexed { index, subject ->
                SubjectDateRow(subject)
                if (index != group.subjects.lastIndex) {
                    HorizontalDivider(modifier = Modifier.padding(vertical = 8.dp))
                }
            }
        }
    }
}

@Composable
private fun SubjectDateRow(subject: ExamTimetableSubjectDto) {
    val date = remember(subject.examDate) { runCatching { LocalDate.parse(subject.examDate) }.getOrNull() }
    val time = remember(subject.examTime) {
        subject.examTime?.let { runCatching { LocalTime.parse(it) }.getOrNull() }
    }

    Column(Modifier.fillMaxWidth()) {
        Row(
            Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment     = Alignment.CenterVertically
        ) {
            Text(
                subject.subjectName,
                style    = MaterialTheme.typography.bodyMedium,
                modifier = Modifier.weight(1f)
            )
            Column(horizontalAlignment = Alignment.End) {
                Text(
                    date?.format(DateTimeFormatter.ofPattern("dd MMM yyyy")) ?: subject.examDate,
                    style      = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.Medium
                )
                Row {
                    if (date != null) {
                        Text(
                            date.dayOfWeek.getDisplayName(TextStyle.FULL, Locale.getDefault()),
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            fontSize = 11.sp
                        )
                    }
                    if (time != null) {
                        Text(
                            " · ${time.format(DateTimeFormatter.ofPattern("h:mm a"))}",
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            fontSize = 11.sp
                        )
                    }
                }
            }
        }
        if (!subject.examRemarks.isNullOrBlank()) {
            Text(
                subject.examRemarks,
                style    = MaterialTheme.typography.labelSmall,
                color    = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(top = 3.dp)
            )
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

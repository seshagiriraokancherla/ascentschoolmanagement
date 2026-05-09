package com.ascentschools.mobile.ui.marks

import androidx.compose.animation.*
import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.ExamResultDto
import com.ascentschools.mobile.data.api.MarksResultDto
import com.ascentschools.mobile.data.api.SubjectMarkDto
import com.ascentschools.mobile.ui.theme.MarksShimmer
import com.ascentschools.mobile.ui.theme.NavyBlue

@Composable
fun MarksScreen(
    viewModel: MarksViewModel,
    modifier : Modifier = Modifier
) {
    val uiState by viewModel.uiState.collectAsState()

    AnimatedContent(
        targetState  = uiState,
        transitionSpec = { fadeIn(tween(250)) togetherWith fadeOut(tween(150)) },
        label        = "marksContent",
        modifier     = modifier.fillMaxSize()
    ) { state ->
        when (state) {
            is MarksUiState.Loading -> MarksShimmer()
            is MarksUiState.Error   -> ErrorState(state.message) { viewModel.load() }
            is MarksUiState.Success -> MarksContent(state.results)
        }
    }
}

@Composable
private fun MarksContent(results: List<MarksResultDto>) {
    if (results.isEmpty()) {
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            Text(
                "No marks available",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        return
    }

    LazyColumn(
        modifier            = Modifier.fillMaxSize(),
        contentPadding      = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        results.forEach { yearResult ->
            item {
                Text(
                    yearResult.academicYearName ?: "Academic Year",
                    style    = MaterialTheme.typography.labelLarge,
                    color    = NavyBlue,
                    modifier = Modifier.padding(bottom = 4.dp, top = 4.dp)
                )
            }
            itemsIndexed(yearResult.exams) { index, exam ->
                var visible by remember { mutableStateOf(false) }
                LaunchedEffect(Unit) { visible = true }

                AnimatedVisibility(
                    visible      = visible,
                    enter        = fadeIn(tween(300, delayMillis = index * 80)) +
                                   slideInVertically(tween(300, delayMillis = index * 80)) { it / 3 }
                ) {
                    ExamCard(exam)
                }
            }
        }
    }
}

// ── Exam card with bar charts ─────────────────────────────────────────────────

@Composable
private fun ExamCard(exam: ExamResultDto) {
    Card(
        shape  = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(Modifier.padding(16.dp)) {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment     = Alignment.CenterVertically
            ) {
                Text(exam.examTypeName, style = MaterialTheme.typography.titleSmall)
                if (exam.totalMax > 0) {
                    ScoreBadge(exam.totalObtained, exam.totalMax)
                }
            }

            Spacer(Modifier.height(16.dp))

            exam.marks.forEach { subject ->
                SubjectBarRow(subject)
                Spacer(Modifier.height(10.dp))
            }
        }
    }
}

@Composable
private fun ScoreBadge(obtained: Double, total: Double) {
    val pct   = if (total > 0) (obtained / total * 100).toInt() else 0
    val color = scoreColor(obtained, total)
    Box(
        modifier = Modifier
            .clip(RoundedCornerShape(20.dp))
            .background(color.copy(alpha = 0.12f))
            .padding(horizontal = 10.dp, vertical = 4.dp)
    ) {
        Text(
            "$pct%",
            style      = MaterialTheme.typography.labelMedium,
            fontWeight = FontWeight.Bold,
            color      = color
        )
    }
}

// ── Subject bar row ───────────────────────────────────────────────────────────

@Composable
private fun SubjectBarRow(subject: SubjectMarkDto) {
    if (subject.isAbsent == true) {
        Row(
            Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment     = Alignment.CenterVertically
        ) {
            Text(subject.subjectName, style = MaterialTheme.typography.bodySmall)
            Box(
                modifier = Modifier
                    .clip(RoundedCornerShape(4.dp))
                    .background(Color(0xFFEF4444).copy(alpha = 0.12f))
                    .padding(horizontal = 8.dp, vertical = 2.dp)
            ) {
                Text("Absent", style = MaterialTheme.typography.labelSmall, color = Color(0xFFEF4444))
            }
        }
        return
    }

    val obtained = subject.marksObtained ?: 0.0
    val max      = subject.maxMarks ?: 0.0
    val fraction = if (max > 0) (obtained / max).coerceIn(0.0, 1.0).toFloat() else 0f
    val color    = scoreColor(obtained, max)

    Column {
        Row(
            Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text(
                subject.subjectName,
                style    = MaterialTheme.typography.bodySmall,
                modifier = Modifier.weight(1f)
            )
            Text(
                "${obtained.toInt()} / ${max.toInt()}",
                style      = MaterialTheme.typography.labelMedium,
                fontWeight = FontWeight.SemiBold,
                color      = color
            )
        }
        Spacer(Modifier.height(4.dp))

        // Animated bar
        var animFraction by remember { mutableFloatStateOf(0f) }
        val animatedFraction by animateFloatAsState(
            targetValue   = animFraction,
            animationSpec = tween(600, easing = FastOutSlowInEasing),
            label         = "barAnim"
        )
        LaunchedEffect(fraction) { animFraction = fraction }

        Box(
            modifier = Modifier
                .fillMaxWidth()
                .height(8.dp)
                .clip(RoundedCornerShape(4.dp))
                .background(MaterialTheme.colorScheme.surfaceVariant)
        ) {
            Box(
                modifier = Modifier
                    .fillMaxWidth(animatedFraction)
                    .fillMaxHeight()
                    .clip(RoundedCornerShape(4.dp))
                    .background(color)
            )
        }
    }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

private fun scoreColor(obtained: Double, total: Double): Color {
    if (total == 0.0) return Color(0xFF64748B)
    return when {
        obtained / total >= 0.75 -> Color(0xFF22C55E)
        obtained / total >= 0.50 -> Color(0xFFF97316)
        else                     -> Color(0xFFEF4444)
    }
}

@Composable
private fun ErrorState(message: String, onRetry: () -> Unit) {
    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.padding(16.dp)) {
            Text(message, color = MaterialTheme.colorScheme.error)
            Spacer(Modifier.height(12.dp))
            Button(onClick = onRetry) { Text("Retry") }
        }
    }
}

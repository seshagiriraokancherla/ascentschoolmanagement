package com.ascentschools.mobile.ui.attendance

import android.os.Build
import androidx.annotation.RequiresApi
import androidx.compose.animation.*
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ChevronLeft
import androidx.compose.material.icons.filled.ChevronRight
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.AttendanceRecordDto
import com.ascentschools.mobile.data.api.AttendanceSummaryDto
import com.ascentschools.mobile.ui.theme.AttendanceShimmer
import com.ascentschools.mobile.ui.theme.NavyBlue
import java.time.LocalDate
import java.time.Month
import java.time.YearMonth
import java.time.format.TextStyle
import java.util.Locale

private val Present  = Color(0xFF22C55E)
private val Absent   = Color(0xFFEF4444)
private val Late     = Color(0xFFF97316)
private val HalfDay  = Color(0xFFFBBF24)

@RequiresApi(Build.VERSION_CODES.O)
@Composable
fun AttendanceScreen(
    viewModel: AttendanceViewModel,
    modifier : Modifier = Modifier
) {
    val uiState       by viewModel.uiState.collectAsState()
    val selectedMonth by viewModel.selectedMonth.collectAsState()
    val selectedYear  by viewModel.selectedYear.collectAsState()

    Column(modifier.fillMaxSize()) {
        MonthNavigator(
            month  = selectedMonth,
            year   = selectedYear,
            onPrev = {
                if (selectedMonth == 1) viewModel.setMonthYear(12, selectedYear - 1)
                else                    viewModel.setMonthYear(selectedMonth - 1, selectedYear)
            },
            onNext = {
                if (selectedMonth == 12) viewModel.setMonthYear(1, selectedYear + 1)
                else                     viewModel.setMonthYear(selectedMonth + 1, selectedYear)
            }
        )

        AnimatedContent(
            targetState  = uiState,
            transitionSpec = {
                fadeIn(tween(250)) togetherWith fadeOut(tween(150))
            },
            label = "attendanceContent"
        ) { state ->
            when (state) {
                is AttendanceUiState.Loading -> AttendanceShimmer()
                is AttendanceUiState.Error   -> ErrorState(state.message) { viewModel.load() }
                is AttendanceUiState.Idle    -> {}
                is AttendanceUiState.Success -> AttendanceContent(
                    summary = state.summary,
                    month   = selectedMonth,
                    year    = selectedYear
                )
            }
        }
    }
}

@RequiresApi(Build.VERSION_CODES.O)
@Composable
private fun MonthNavigator(month: Int, year: Int, onPrev: () -> Unit, onNext: () -> Unit) {
    val monthName = Month.of(month).getDisplayName(TextStyle.FULL, Locale.getDefault())
    Surface(
        color     = NavyBlue,
        modifier  = Modifier.fillMaxWidth()
    ) {
        Row(
            modifier              = Modifier
                .fillMaxWidth()
                .padding(horizontal = 8.dp, vertical = 12.dp),
            verticalAlignment     = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            IconButton(onClick = onPrev) {
                Icon(Icons.Default.ChevronLeft, contentDescription = "Previous month", tint = Color.White)
            }
            Text(
                "$monthName $year",
                fontWeight = FontWeight.SemiBold,
                fontSize   = 16.sp,
                color      = Color.White
            )
            IconButton(onClick = onNext) {
                Icon(Icons.Default.ChevronRight, contentDescription = "Next month", tint = Color.White)
            }
        }
    }
}

@RequiresApi(Build.VERSION_CODES.O)
@Composable
private fun AttendanceContent(
    summary : AttendanceSummaryDto,
    month   : Int,
    year    : Int
) {
    LazyColumn(
        modifier       = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        item { SummaryCard(summary) }

        item {
            Card(
                shape  = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(Modifier.padding(16.dp)) {
                    Text(
                        "Monthly Calendar",
                        style = MaterialTheme.typography.titleSmall,
                        color = MaterialTheme.colorScheme.onSurface
                    )
                    Spacer(Modifier.height(12.dp))
                    CalendarGrid(year = year, month = month, records = summary.records)
                    Spacer(Modifier.height(12.dp))
                    AttendanceLegend()
                }
            }
        }
    }
}

// ── Summary card ──────────────────────────────────────────────────────────────

@Composable
private fun SummaryCard(summary: AttendanceSummaryDto) {
    val pct = if (summary.totalDays > 0)
        ((summary.presentDays + 0.5f * summary.halfDayDays) / summary.totalDays * 100).toInt() else 0
    val barColor = when {
        pct >= 75 -> Present
        pct >= 60 -> Late
        else      -> Absent
    }

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
                Text("Attendance Summary", style = MaterialTheme.typography.titleSmall)
                Text(
                    "$pct%",
                    fontWeight = FontWeight.Bold,
                    fontSize   = 28.sp,
                    color      = barColor
                )
            }
            Spacer(Modifier.height(12.dp))
            LinearProgressIndicator(
                progress     = { pct / 100f },
                modifier     = Modifier.fillMaxWidth().height(10.dp).clip(RoundedCornerShape(5.dp)),
                color        = barColor,
                trackColor   = barColor.copy(alpha = 0.15f)
            )
            Spacer(Modifier.height(16.dp))
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceEvenly) {
                StatPill("Present", summary.presentDays.toString(), Present)
                StatPill("Absent",  summary.absentDays.toString(),  Absent)
                StatPill("Late",    summary.lateDays.toString(),    Late)
                StatPill("Half Day", summary.halfDayDays.toString(), HalfDay)
                StatPill("Total",   summary.totalDays.toString(),   NavyBlue)
            }
        }
    }
}

@Composable
private fun StatPill(label: String, value: String, color: Color) {
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Box(
            modifier         = Modifier
                .size(48.dp)
                .clip(CircleShape)
                .background(color.copy(alpha = 0.12f)),
            contentAlignment = Alignment.Center
        ) {
            Text(value, fontWeight = FontWeight.Bold, color = color, fontSize = 16.sp)
        }
        Spacer(Modifier.height(4.dp))
        Text(label, style = MaterialTheme.typography.labelSmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
}

// ── Calendar grid ─────────────────────────────────────────────────────────────

@RequiresApi(Build.VERSION_CODES.O)
@Composable
private fun CalendarGrid(year: Int, month: Int, records: List<AttendanceRecordDto>) {
    val yearMonth   = YearMonth.of(year, month)
    val firstDay    = LocalDate.of(year, month, 1)
    val startOffset = (firstDay.dayOfWeek.value - 1) % 7   // Monday = 0
    val daysInMonth = yearMonth.lengthOfMonth()

    val dayStatusMap = records.mapNotNull { record ->
        try { LocalDate.parse(record.date).dayOfMonth to record.status }
        catch (_: Exception) { null }
    }.toMap()

    // Day-of-week headers
    Row(Modifier.fillMaxWidth()) {
        listOf("M", "T", "W", "T", "F", "S", "S").forEach { d ->
            Text(
                d,
                modifier   = Modifier.weight(1f),
                textAlign  = TextAlign.Center,
                style      = MaterialTheme.typography.labelSmall,
                color      = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    }
    Spacer(Modifier.height(6.dp))

    val rows = ((startOffset + daysInMonth) + 6) / 7
    repeat(rows) { row ->
        Row(
            modifier = Modifier.fillMaxWidth().padding(vertical = 3.dp),
        ) {
            repeat(7) { col ->
                val dayNum = row * 7 + col - startOffset + 1
                Box(Modifier.weight(1f), contentAlignment = Alignment.Center) {
                    if (dayNum in 1..daysInMonth) {
                        DayCell(dayNum, dayStatusMap[dayNum])
                    }
                }
            }
        }
    }
}

@Composable
private fun DayCell(day: Int, status: String?) {
    val (bg, fg) = when (status) {
        "Present"  -> Present  to Color.White
        "Absent"   -> Absent   to Color.White
        "Late"     -> Late     to Color.White
        "HalfDay"  -> HalfDay  to Color.White
        else       -> Color(0xFFE2E8F0) to Color(0xFF94A3B8)
    }
    Box(
        modifier         = Modifier
            .size(34.dp)
            .clip(CircleShape)
            .background(bg),
        contentAlignment = Alignment.Center
    ) {
        Text(
            day.toString(),
            style      = MaterialTheme.typography.labelMedium,
            fontWeight = if (status != null) FontWeight.Bold else FontWeight.Normal,
            color      = fg
        )
    }
}

@Composable
private fun AttendanceLegend() {
    Row(
        modifier              = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        listOf("Present" to Present, "Absent" to Absent, "Late" to Late, "Half Day" to HalfDay).forEach { (label, color) ->
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                Box(Modifier.size(10.dp).clip(CircleShape).background(color))
                Text(label, style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
    }
}

// ── Error state ───────────────────────────────────────────────────────────────

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

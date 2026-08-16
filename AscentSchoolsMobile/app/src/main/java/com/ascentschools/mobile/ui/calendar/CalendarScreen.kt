package com.ascentschools.mobile.ui.calendar

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ChevronLeft
import androidx.compose.material.icons.filled.ChevronRight
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.CalendarEventDto
import java.time.DayOfWeek
import java.time.LocalDate
import java.time.format.TextStyle
import java.util.Locale

private val HolidayRed  = Color(0xFFD4380D)
private val HolidayTint = Color(0x14F5222D)   // light red row background

@Composable
fun CalendarScreen(
    viewModel: CalendarViewModel,
    modifier : Modifier = Modifier
) {
    val uiState    by viewModel.uiState.collectAsState()
    val monthLabel by viewModel.monthLabel.collectAsState()

    Column(modifier.fillMaxSize()) {
        // Month navigation header
        Row(
            Modifier
                .fillMaxWidth()
                .padding(horizontal = 12.dp, vertical = 8.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment     = Alignment.CenterVertically
        ) {
            IconButton(onClick = { viewModel.prevMonth() }) {
                Icon(Icons.Default.ChevronLeft, contentDescription = "Previous month")
            }
            Text(monthLabel, fontWeight = FontWeight.SemiBold, fontSize = 16.sp)
            IconButton(onClick = { viewModel.nextMonth() }) {
                Icon(Icons.Default.ChevronRight, contentDescription = "Next month")
            }
        }
        HorizontalDivider()

        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            when (val s = uiState) {
                is CalendarUiState.Loading -> CircularProgressIndicator()
                is CalendarUiState.Error   -> ErrorState(s.message) { viewModel.load() }
                is CalendarUiState.Success -> {
                    val days = remember(s) { buildMonth(s.year, s.month, s.events) }
                    val workingDays = days.count { !it.isHoliday }
                    LazyColumn(Modifier.fillMaxSize()) {
                        items(days) { d -> DayRow(d) }
                        item {
                            HorizontalDivider()
                            Text(
                                "Total Working Days : $workingDays",
                                fontWeight = FontWeight.Bold,
                                fontSize   = 14.sp,
                                modifier   = Modifier
                                    .fillMaxWidth()
                                    .padding(16.dp),
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun DayRow(d: DayCell) {
    Row(
        Modifier
            .fillMaxWidth()
            .then(if (d.isHoliday) Modifier.background(HolidayTint) else Modifier)
            .padding(horizontal = 16.dp, vertical = 10.dp),
        verticalAlignment = Alignment.Top
    ) {
        Text(
            d.day.toString(),
            fontWeight = FontWeight.SemiBold,
            fontSize   = 14.sp,
            color      = if (d.isHoliday) HolidayRed else MaterialTheme.colorScheme.onSurface,
            modifier   = Modifier.width(28.dp)
        )
        Text(
            d.weekday,
            fontSize = 14.sp,
            fontWeight = if (d.isHoliday) FontWeight.SemiBold else FontWeight.Normal,
            color = if (d.isHoliday) HolidayRed else MaterialTheme.colorScheme.onSurface,
            modifier = Modifier.width(96.dp)
        )
        Text("-", fontSize = 14.sp,
            color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.4f),
            modifier = Modifier.padding(end = 10.dp))
        Text(
            d.description,
            fontSize   = 14.sp,
            fontWeight = if (d.isHoliday) FontWeight.SemiBold else FontWeight.Normal,
            color = when {
                d.isHoliday -> HolidayRed
                d.isEvent   -> MaterialTheme.colorScheme.onSurface
                else        -> MaterialTheme.colorScheme.onSurface.copy(alpha = 0.45f)  // Working Day
            },
            modifier = Modifier.weight(1f)
        )
    }
    HorizontalDivider(color = MaterialTheme.colorScheme.outline.copy(alpha = 0.15f))
}

// ── Month model ────────────────────────────────────────────────────────────────

private data class DayCell(
    val day        : Int,
    val weekday    : String,
    val description: String,
    val isHoliday  : Boolean,   // Sunday OR a Holiday-category event → red, excluded from working days
    val isEvent    : Boolean
)

// Builds one row per day of the month, applying: event(s) on a date → shown;
// Sunday with no event → "Holiday"; anything else → "Working Day".
private fun buildMonth(year: Int, month: Int, events: List<CalendarEventDto>): List<DayCell> {
    val first = LocalDate.of(year, month, 1)
    return (1..first.lengthOfMonth()).map { day ->
        val date    = LocalDate.of(year, month, day)
        val isSun   = date.dayOfWeek == DayOfWeek.SUNDAY
        val onDay   = events.filter { containsDate(it, date) }
        val hasHolidayEvent = onDay.any { it.category.equals("Holiday", ignoreCase = true) }
        val weekday = date.dayOfWeek.getDisplayName(TextStyle.FULL, Locale.getDefault())

        val description = when {
            onDay.isNotEmpty() -> onDay.joinToString(" · ") { it.title?.takeIf { t -> t.isNotBlank() } ?: (it.category ?: "Event") }
            isSun              -> "Holiday"
            else               -> "Working Day"
        }
        DayCell(
            day         = day,
            weekday     = weekday,
            description = description,
            isHoliday   = isSun || hasHolidayEvent,
            isEvent     = onDay.isNotEmpty()
        )
    }
}

private fun containsDate(ev: CalendarEventDto, date: LocalDate): Boolean {
    val start = parseIso(ev.startDate) ?: return false
    val end   = parseIso(ev.endDate) ?: start
    return !date.isBefore(start) && !date.isAfter(end)
}

private fun parseIso(s: String?): LocalDate? =
    try { if (s.isNullOrBlank()) null else LocalDate.parse(s.take(10)) } catch (e: Exception) { null }

@Composable
private fun ErrorState(message: String, onRetry: () -> Unit) {
    Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.padding(16.dp)) {
        Text(message, color = MaterialTheme.colorScheme.error)
        Spacer(Modifier.height(12.dp))
        Button(onClick = onRetry) { Text("Retry") }
    }
}

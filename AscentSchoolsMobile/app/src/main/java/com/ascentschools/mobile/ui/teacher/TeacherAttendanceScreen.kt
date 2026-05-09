package com.ascentschools.mobile.ui.teacher

import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.combinedClickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import java.time.format.FormatStyle

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TeacherAttendanceScreen(
    classId   : Int,
    sectionId : Int,
    viewModel : TeacherViewModel,
    onBack    : () -> Unit
) {
    val students          by viewModel.attendanceStudents.collectAsState()
    val className         by viewModel.className.collectAsState()
    val selectedDate      by viewModel.selectedDate.collectAsState()
    val isLoading         by viewModel.isLoading.collectAsState()
    val uiState           by viewModel.uiState.collectAsState()
    val alreadySaved      by viewModel.isAttendanceAlreadySaved.collectAsState()
    val snackbarHost       = remember { SnackbarHostState() }

    // Student whose status sheet is open
    var bottomSheetStudent by remember { mutableStateOf<StudentAttendanceState?>(null) }

    // Date picker
    var showDatePicker by remember { mutableStateOf(false) }
    val datePickerState = rememberDatePickerState(
        initialSelectedDateMillis = runCatching {
            LocalDate.parse(selectedDate).toEpochDay() * 86400_000L
        }.getOrDefault(System.currentTimeMillis())
    )

    // Load on first composition and when date changes
    LaunchedEffect(selectedDate) {
        viewModel.loadAttendance(classId, sectionId)
    }

    // Show snackbar for success / error
    LaunchedEffect(uiState) {
        when (val s = uiState) {
            is TeacherUiState.Success -> {
                snackbarHost.showSnackbar(s.message)
                viewModel.clearUiState()
            }
            is TeacherUiState.Error -> {
                snackbarHost.showSnackbar(s.message)
                viewModel.clearUiState()
            }
            else -> {}
        }
    }

    // Counts (recomputed on each recompose — fast for typical class sizes)
    val presentCount = students.count { it.status == AttendanceStatus.Present }
    val absentCount  = students.count { it.status == AttendanceStatus.Absent  }
    val lateCount    = students.count { it.status == AttendanceStatus.Late    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHost) },
        topBar = {
            TopAppBar(
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "Back") }
                },
                title = {
                    Column {
                        Text(className.ifBlank { "Attendance" }, fontWeight = FontWeight.Bold)
                        val displayDate = runCatching {
                            LocalDate.parse(selectedDate)
                                .format(DateTimeFormatter.ofLocalizedDate(FormatStyle.MEDIUM))
                        }.getOrDefault(selectedDate)
                        Text(displayDate, fontSize = 12.sp,
                            color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f))
                    }
                },
                actions = {
                    IconButton(onClick = { showDatePicker = true }) {
                        Icon(Icons.Default.Schedule, "Change date")
                    }
                }
            )
        },
        bottomBar = {
            Column {
                Divider()
                Button(
                    onClick  = { viewModel.saveAttendance(classId, sectionId) },
                    enabled  = students.isNotEmpty() && !isLoading,
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 12.dp)
                        .height(50.dp)
                ) {
                    if (isLoading) {
                        CircularProgressIndicator(
                            color       = MaterialTheme.colorScheme.onPrimary,
                            modifier    = Modifier.size(20.dp),
                            strokeWidth = 2.dp
                        )
                    } else {
                        val label = if (alreadySaved) "Update Attendance (${students.size})"
                                    else "Save Attendance (${students.size} students)"
                        Text(label, fontWeight = FontWeight.SemiBold)
                    }
                }
            }
        }
    ) { padding ->
        if (isLoading && students.isEmpty()) {
            Box(Modifier.fillMaxSize().padding(padding), contentAlignment = Alignment.Center) {
                CircularProgressIndicator()
            }
        } else {
            LazyColumn(
                modifier            = Modifier.fillMaxSize().padding(padding),
                contentPadding      = PaddingValues(bottom = 8.dp)
            ) {
                // Summary + Mark All button
                item {
                    Row(
                        modifier              = Modifier
                            .fillMaxWidth()
                            .background(MaterialTheme.colorScheme.surfaceVariant)
                            .padding(horizontal = 16.dp, vertical = 10.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment     = Alignment.CenterVertically
                    ) {
                        Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                            SummaryChip("$presentCount P", Color(0xFF52C41A))
                            SummaryChip("$absentCount A", Color(0xFFFF4D4F))
                            if (lateCount > 0) SummaryChip("$lateCount L", Color(0xFFFA8C16))
                        }
                        TextButton(
                            onClick = { viewModel.markAllPresent() },
                            enabled = absentCount > 0 || lateCount > 0
                        ) {
                            Text("All Present", fontSize = 12.sp)
                        }
                    }
                }

                if (alreadySaved) {
                    item {
                        Text(
                            "Attendance already saved · tap to edit",
                            fontSize = 11.sp,
                            color    = MaterialTheme.colorScheme.primary,
                            modifier = Modifier.padding(horizontal = 16.dp, vertical = 4.dp)
                        )
                    }
                }

                // Student rows
                itemsIndexed(students, key = { _, s -> s.studentId }) { index, student ->
                    StudentRow(
                        index   = index + 1,
                        student = student,
                        onTap   = { viewModel.toggleStudentStatus(student.studentId) },
                        onLongPress = { bottomSheetStudent = student }
                    )
                    Divider(color = MaterialTheme.colorScheme.outlineVariant, thickness = 0.5.dp)
                }
            }
        }
    }

    // ── Date picker dialog ────────────────────────────────────────────────────
    if (showDatePicker) {
        DatePickerDialog(
            onDismissRequest = { showDatePicker = false },
            confirmButton = {
                TextButton(onClick = {
                    datePickerState.selectedDateMillis?.let { millis ->
                        val date = LocalDate.ofEpochDay(millis / 86400_000L)
                            .format(DateTimeFormatter.ISO_LOCAL_DATE)
                        viewModel.setDate(date)
                    }
                    showDatePicker = false
                }) { Text("OK") }
            },
            dismissButton = {
                TextButton(onClick = { showDatePicker = false }) { Text("Cancel") }
            }
        ) {
            DatePicker(state = datePickerState)
        }
    }

    // ── Status picker bottom sheet ────────────────────────────────────────────
    val sheetStudent = bottomSheetStudent
    if (sheetStudent != null) {
        ModalBottomSheet(onDismissRequest = { bottomSheetStudent = null }) {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 24.dp)
                    .padding(bottom = 32.dp),
                verticalArrangement = Arrangement.spacedBy(4.dp)
            ) {
                Text(sheetStudent.studentName, fontWeight = FontWeight.Bold, fontSize = 16.sp)
                Text("Choose attendance status",
                    color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f),
                    fontSize = 13.sp)
                Spacer(Modifier.height(12.dp))

                listOf(
                    Triple(AttendanceStatus.Present, "Present",  Color(0xFF52C41A)),
                    Triple(AttendanceStatus.Absent,  "Absent",   Color(0xFFFF4D4F)),
                    Triple(AttendanceStatus.Late,    "Late",     Color(0xFFFA8C16)),
                ).forEach { (status, label, color) ->
                    val isSelected = sheetStudent.status == status
                    OutlinedButton(
                        onClick = {
                            viewModel.setStudentStatus(sheetStudent.studentId, status)
                            bottomSheetStudent = null
                        },
                        modifier = Modifier.fillMaxWidth(),
                        colors   = if (isSelected)
                            ButtonDefaults.outlinedButtonColors(containerColor = color.copy(alpha = 0.15f))
                        else ButtonDefaults.outlinedButtonColors()
                    ) {
                        Text(label, color = if (isSelected) color else MaterialTheme.colorScheme.onSurface,
                            fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Normal)
                    }
                }
            }
        }
    }
}

// ── Student row ───────────────────────────────────────────────────────────────

@OptIn(ExperimentalFoundationApi::class)
@Composable
private fun StudentRow(
    index      : Int,
    student    : StudentAttendanceState,
    onTap      : () -> Unit,
    onLongPress: () -> Unit
) {
    val statusColor = when (student.status) {
        AttendanceStatus.Present -> Color(0xFF52C41A)
        AttendanceStatus.Absent  -> Color(0xFFFF4D4F)
        AttendanceStatus.Late    -> Color(0xFFFA8C16)
    }
    val statusIcon = when (student.status) {
        AttendanceStatus.Present -> Icons.Default.Check
        AttendanceStatus.Absent  -> Icons.Default.Close
        AttendanceStatus.Late    -> Icons.Default.Schedule
    }

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .combinedClickable(onClick = onTap, onLongClick = onLongPress)
            .padding(horizontal = 16.dp, vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(14.dp)
    ) {
        // Status icon circle
        Box(
            modifier = Modifier
                .size(40.dp)
                .clip(CircleShape)
                .background(statusColor.copy(alpha = 0.15f)),
            contentAlignment = Alignment.Center
        ) {
            Icon(statusIcon, contentDescription = student.status.name,
                tint = statusColor, modifier = Modifier.size(20.dp))
        }

        // Name + admission
        Column(Modifier.weight(1f)) {
            Text(student.studentName, fontWeight = FontWeight.Medium, fontSize = 14.sp)
            Text(student.admissionNo, fontSize = 12.sp,
                color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f))
        }

        // Status badge (only for non-present)
        if (student.status != AttendanceStatus.Present) {
            Text(
                text     = student.status.name.uppercase(),
                color    = statusColor,
                fontSize = 11.sp,
                fontWeight = FontWeight.Bold,
                modifier = Modifier
                    .clip(RoundedCornerShape(4.dp))
                    .background(statusColor.copy(alpha = 0.1f))
                    .padding(horizontal = 6.dp, vertical = 2.dp)
            )
        }
    }
}

@Composable
private fun SummaryChip(text: String, color: Color) {
    Text(
        text     = text,
        color    = color,
        fontSize = 13.sp,
        fontWeight = FontWeight.Bold,
        modifier = Modifier
            .clip(RoundedCornerShape(6.dp))
            .background(color.copy(alpha = 0.12f))
            .padding(horizontal = 8.dp, vertical = 4.dp)
    )
}


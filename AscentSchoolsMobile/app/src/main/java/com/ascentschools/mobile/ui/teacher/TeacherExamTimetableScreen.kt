package com.ascentschools.mobile.ui.teacher

import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.ui.marks.ExamTimetableScreen

// Read-only — same exam schedule students see for the class (Master Data → Exam Master's
// exam_date). Teachers don't set exam dates here; that stays a school-web-app action.
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TeacherExamTimetableScreen(
    classId  : Int,
    className: String,
    viewModel: TeacherViewModel,
    onBack   : () -> Unit
) {
    val uiState by viewModel.examTimetableState.collectAsState()

    LaunchedEffect(classId) { viewModel.loadExamTimetable(classId) }

    Scaffold(
        topBar = {
            TopAppBar(
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "Back") }
                },
                title = {
                    Column {
                        Text("Exam Timetable", fontWeight = FontWeight.Bold)
                        Text(className, fontSize = 12.sp,
                            color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f))
                    }
                }
            )
        }
    ) { padding ->
        ExamTimetableScreen(
            uiState  = uiState,
            onRetry  = { viewModel.loadExamTimetable(classId) },
            modifier = Modifier.padding(padding)
        )
    }
}

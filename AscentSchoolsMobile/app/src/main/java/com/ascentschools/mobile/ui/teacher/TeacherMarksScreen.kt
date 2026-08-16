package com.ascentschools.mobile.ui.teacher

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.TeacherExamTypeDto
import com.ascentschools.mobile.data.api.TeacherMarksSubjectDto

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TeacherMarksScreen(
    classId   : Int,
    sectionId : Int,
    className : String,
    viewModel : TeacherViewModel,
    onBack    : () -> Unit
) {
    val examTypes    by viewModel.examTypes.collectAsState()
    val subjects     by viewModel.marksSubjects.collectAsState()
    val header       by viewModel.marksHeader.collectAsState()
    val students     by viewModel.markStudents.collectAsState()
    val isLoading    by viewModel.isLoading.collectAsState()
    val uiState      by viewModel.uiState.collectAsState()
    val snackbarHost  = remember { SnackbarHostState() }

    var selectedExam    by remember { mutableStateOf<TeacherExamTypeDto?>(null) }
    var selectedSubject by remember { mutableStateOf<TeacherMarksSubjectDto?>(null) }
    var examExpanded    by remember { mutableStateOf(false) }
    var subjectExpanded by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        viewModel.loadExamTypes()
        viewModel.loadMarksSubjects(classId)
    }

    // Load (or clear) the student list whenever exam + subject selection changes.
    LaunchedEffect(selectedExam, selectedSubject) {
        val e = selectedExam; val s = selectedSubject
        if (e != null && s != null) viewModel.loadMarks(classId, sectionId, e.examTypeId, s.subjectId)
        else viewModel.clearMarks()
    }

    LaunchedEffect(uiState) {
        when (val st = uiState) {
            is TeacherUiState.Success -> { snackbarHost.showSnackbar(st.message); viewModel.clearUiState() }
            is TeacherUiState.Error   -> { snackbarHost.showSnackbar(st.message); viewModel.clearUiState() }
            else -> {}
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHost) },
        topBar = {
            TopAppBar(
                navigationIcon = { IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "Back") } },
                title = {
                    Column {
                        Text(className.ifBlank { "Marks Entry" }, fontWeight = FontWeight.Bold)
                        Text("Marks Entry", fontSize = 12.sp,
                            color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f))
                    }
                }
            )
        },
        bottomBar = {
            if (header != null && students.isNotEmpty()) {
                Column {
                    Divider()
                    Button(
                        onClick  = {
                            val e = selectedExam
                            if (e != null) viewModel.saveMarks(classId, sectionId, e.examTypeId)
                        },
                        enabled  = !isLoading,
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(horizontal = 16.dp, vertical = 12.dp)
                            .height(50.dp)
                    ) {
                        if (isLoading) CircularProgressIndicator(
                            modifier = Modifier.size(22.dp), color = MaterialTheme.colorScheme.onPrimary)
                        else Text("Save Marks", fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
                    }
                }
            }
        }
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(padding)) {

            // ── Exam + Subject filters ──────────────────────────────────────────
            Column(
                Modifier.fillMaxWidth().padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                ExposedDropdownMenuBox(
                    expanded = examExpanded,
                    onExpandedChange = { examExpanded = it }
                ) {
                    OutlinedTextField(
                        value = selectedExam?.examTypeName ?: "",
                        onValueChange = {}, readOnly = true,
                        label = { Text("Exam") },
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(examExpanded) },
                        modifier = Modifier.fillMaxWidth().menuAnchor()
                    )
                    ExposedDropdownMenu(expanded = examExpanded, onDismissRequest = { examExpanded = false }) {
                        examTypes.forEach { ex ->
                            DropdownMenuItem(
                                text = { Text(ex.examTypeName) },
                                onClick = { selectedExam = ex; examExpanded = false }
                            )
                        }
                    }
                }

                ExposedDropdownMenuBox(
                    expanded = subjectExpanded && subjects.isNotEmpty(),
                    onExpandedChange = { subjectExpanded = it }
                ) {
                    OutlinedTextField(
                        value = selectedSubject?.subjectName ?: "",
                        onValueChange = {}, readOnly = true,
                        label = { Text("Subject") },
                        placeholder = {
                            Text(if (subjects.isEmpty()) "No subjects mapped to this class" else "Select subject")
                        },
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(subjectExpanded) },
                        enabled = subjects.isNotEmpty(),
                        modifier = Modifier.fillMaxWidth().menuAnchor()
                    )
                    ExposedDropdownMenu(expanded = subjectExpanded, onDismissRequest = { subjectExpanded = false }) {
                        subjects.forEach { sub ->
                            DropdownMenuItem(
                                text = { Text(sub.subjectName) },
                                onClick = { selectedSubject = sub; subjectExpanded = false }
                            )
                        }
                    }
                }

                header?.let { h ->
                    Text(
                        buildString {
                            append("Max marks: ${fmtD(h.maxMarks)}")
                            if (h.hasActivity) append("  ·  Activity max: ${fmtD(h.activityMaxMarks ?: 0.0)}")
                        },
                        fontSize = 12.sp,
                        color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
                    )
                }
            }
            Divider()

            // ── Student list ────────────────────────────────────────────────────
            Box(Modifier.fillMaxSize()) {
                when {
                    isLoading && students.isEmpty() ->
                        CircularProgressIndicator(Modifier.align(Alignment.Center))
                    header == null ->
                        Text(
                            "Select an exam and subject to enter marks.",
                            Modifier.align(Alignment.Center).padding(24.dp),
                            color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
                        )
                    students.isEmpty() ->
                        Text(
                            "No students found for this class and section.",
                            Modifier.align(Alignment.Center).padding(24.dp),
                            color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
                        )
                    else -> LazyColumn(
                        Modifier.fillMaxSize(),
                        contentPadding = PaddingValues(16.dp),
                        verticalArrangement = Arrangement.spacedBy(10.dp)
                    ) {
                        items(students, key = { it.studentId }) { s ->
                            MarkRow(
                                state       = s,
                                hasActivity = header?.hasActivity == true,
                                onMarks     = { viewModel.setMark(s.studentId, it) },
                                onActivity  = { viewModel.setActivity(s.studentId, it) },
                                onAbsent    = { viewModel.toggleMarkAbsent(s.studentId) }
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun MarkRow(
    state       : StudentMarkState,
    hasActivity : Boolean,
    onMarks     : (String) -> Unit,
    onActivity  : (String) -> Unit,
    onAbsent    : () -> Unit
) {
    Card(shape = RoundedCornerShape(12.dp), modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Column(Modifier.weight(1f)) {
                    Text(state.studentName, fontWeight = FontWeight.SemiBold, fontSize = 15.sp)
                    Text(state.admissionNo, fontSize = 12.sp,
                        color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f))
                }
                FilterChip(
                    selected = state.absent,
                    onClick  = onAbsent,
                    label    = { Text("Absent") }
                )
            }
            Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                if (hasActivity) {
                    NumberField(
                        value = state.activity, label = "Activity", enabled = !state.absent,
                        onChange = onActivity, modifier = Modifier.weight(1f)
                    )
                }
                NumberField(
                    value = state.marks, label = "Marks", enabled = !state.absent,
                    onChange = onMarks, modifier = Modifier.weight(1f)
                )
            }
        }
    }
}

@Composable
private fun NumberField(
    value    : String,
    label    : String,
    enabled  : Boolean,
    onChange : (String) -> Unit,
    modifier : Modifier = Modifier
) {
    OutlinedTextField(
        value = value,
        onValueChange = { t -> if (t.all { it.isDigit() || it == '.' }) onChange(t) },
        label = { Text(label) },
        singleLine = true,
        enabled = enabled,
        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
        modifier = modifier
    )
}

private fun fmtD(d: Double): String =
    if (d == d.toLong().toDouble()) d.toLong().toString() else d.toString()

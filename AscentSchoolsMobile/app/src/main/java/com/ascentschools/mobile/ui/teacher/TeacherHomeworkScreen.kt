package com.ascentschools.mobile.ui.teacher

import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.layout.*
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Book
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.TeacherHomeworkDto
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import java.time.format.FormatStyle

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TeacherHomeworkScreen(
    classId   : Int,
    className : String,
    viewModel : TeacherViewModel,
    onBack    : () -> Unit
) {
    val homework    by viewModel.homework.collectAsState()
    val isLoading   by viewModel.isLoading.collectAsState()
    val uiState     by viewModel.uiState.collectAsState()
    val snackbarHost = remember { SnackbarHostState() }

    var showCreateSheet by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) { viewModel.loadHomework(classId) }

    LaunchedEffect(uiState) {
        when (val s = uiState) {
            is TeacherUiState.Success -> {
                snackbarHost.showSnackbar(s.message)
                viewModel.clearUiState()
                showCreateSheet = false
            }
            is TeacherUiState.Error -> {
                snackbarHost.showSnackbar(s.message)
                viewModel.clearUiState()
            }
            else -> {}
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHost) },
        topBar = {
            TopAppBar(
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "Back") }
                },
                title = {
                    Column {
                        Text("Homework", fontWeight = FontWeight.Bold)
                        Text(className, fontSize = 12.sp,
                            color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f))
                    }
                }
            )
        },
        floatingActionButton = {
            FloatingActionButton(onClick = { showCreateSheet = true }) {
                Icon(Icons.Default.Add, "Add homework")
            }
        }
    ) { padding ->
        if (isLoading && homework.isEmpty()) {
            Box(Modifier.fillMaxSize().padding(padding), contentAlignment = Alignment.Center) {
                CircularProgressIndicator()
            }
        } else if (homework.isEmpty()) {
            Box(Modifier.fillMaxSize().padding(padding), contentAlignment = Alignment.Center) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Icon(Icons.Default.Book, null,
                        modifier = Modifier.size(48.dp),
                        tint = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.3f))
                    Spacer(Modifier.height(8.dp))
                    Text("No homework yet", color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f))
                    Text("Tap + to add homework", fontSize = 12.sp,
                        color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.4f))
                }
            }
        } else {
            LazyColumn(
                modifier       = Modifier.fillMaxSize().padding(padding),
                contentPadding = PaddingValues(16.dp),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                items(homework, key = { it.homeworkId }) { hw ->
                    HomeworkCard(hw)
                }
            }
        }
    }

    if (showCreateSheet) {
        CreateHomeworkSheet(
            onDismiss = { showCreateSheet = false },
            onSave    = { title, desc, dueDate ->
                viewModel.createHomework(classId, title, desc, dueDate)
            },
            isLoading = isLoading
        )
    }
}

@Composable
private fun HomeworkCard(hw: TeacherHomeworkDto) {
    val dueDisplay = runCatching {
        LocalDate.parse(hw.dueDate.take(10))
            .format(DateTimeFormatter.ofLocalizedDate(FormatStyle.MEDIUM))
    }.getOrDefault(hw.dueDate.take(10))

    val isOverdue = runCatching {
        LocalDate.parse(hw.dueDate.take(10)).isBefore(LocalDate.now())
    }.getOrDefault(false)

    Card(
        shape  = RoundedCornerShape(10.dp),
        colors = CardDefaults.cardColors()
    ) {
        Column(Modifier.padding(14.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
            Row(
                modifier              = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment     = Alignment.CenterVertically
            ) {
                Text(hw.title, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
                Text(
                    "Due: $dueDisplay",
                    fontSize = 11.sp,
                    color = if (isOverdue) MaterialTheme.colorScheme.error
                            else MaterialTheme.colorScheme.primary
                )
            }
            if (!hw.description.isNullOrBlank()) {
                Text(
                    text     = hw.description,
                    fontSize = 13.sp,
                    maxLines = 2,
                    color    = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.7f)
                )
            }
            if (!hw.subjectName.isNullOrBlank()) {
                Text("📚 ${hw.subjectName}", fontSize = 12.sp,
                    color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f))
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CreateHomeworkSheet(
    onDismiss : () -> Unit,
    onSave    : (title: String, description: String?, dueDate: String) -> Unit,
    isLoading : Boolean
) {
    var title       by remember { mutableStateOf("") }
    var description by remember { mutableStateOf("") }
    var dueDate     by remember { mutableStateOf("") }
    var showDatePicker by remember { mutableStateOf(false) }

    val datePickerState = rememberDatePickerState(
        initialSelectedDateMillis = System.currentTimeMillis()
    )

    ModalBottomSheet(onDismissRequest = onDismiss) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 24.dp)
                .padding(bottom = 32.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text("Add Homework", fontWeight = FontWeight.Bold, fontSize = 18.sp)

            OutlinedTextField(
                value         = title,
                onValueChange = { title = it },
                label         = { Text("Title *") },
                modifier      = Modifier.fillMaxWidth(),
                singleLine    = true
            )

            OutlinedTextField(
                value         = description,
                onValueChange = { description = it },
                label         = { Text("Description (optional)") },
                modifier      = Modifier.fillMaxWidth(),
                minLines      = 2,
                maxLines      = 4
            )

            OutlinedTextField(
                value         = dueDate.ifBlank { "" },
                onValueChange = {},
                readOnly      = true,
                label         = { Text("Due Date *") },
                placeholder   = { Text("Tap to select") },
                modifier      = Modifier
                    .fillMaxWidth()
                    .clickable { showDatePicker = true },
                enabled       = false,
                colors        = OutlinedTextFieldDefaults.colors(
                    disabledTextColor         = MaterialTheme.colorScheme.onSurface,
                    disabledBorderColor       = MaterialTheme.colorScheme.outline,
                    disabledLabelColor        = MaterialTheme.colorScheme.onSurfaceVariant,
                    disabledPlaceholderColor  = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            )

            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedButton(
                    onClick  = onDismiss,
                    modifier = Modifier.weight(1f)
                ) { Text("Cancel") }

                Button(
                    onClick  = { onSave(title, description.takeIf { it.isNotBlank() }, dueDate) },
                    enabled  = title.isNotBlank() && dueDate.isNotBlank() && !isLoading,
                    modifier = Modifier.weight(1f)
                ) {
                    if (isLoading) CircularProgressIndicator(
                        modifier    = Modifier.size(18.dp),
                        strokeWidth = 2.dp,
                        color       = MaterialTheme.colorScheme.onPrimary
                    )
                    else Text("Save")
                }
            }
        }
    }

    if (showDatePicker) {
        DatePickerDialog(
            onDismissRequest = { showDatePicker = false },
            confirmButton = {
                TextButton(onClick = {
                    datePickerState.selectedDateMillis?.let { millis ->
                        dueDate = java.time.LocalDate.ofEpochDay(millis / 86400_000L)
                            .format(DateTimeFormatter.ISO_LOCAL_DATE)
                    }
                    showDatePicker = false
                }) { Text("OK") }
            },
            dismissButton = {
                TextButton(onClick = { showDatePicker = false }) { Text("Cancel") }
            }
        ) { DatePicker(state = datePickerState) }
    }
}

private fun Modifier.clickable(onClick: () -> Unit) =
    this.pointerInput(onClick) { detectTapGestures(onTap = { onClick() }) }

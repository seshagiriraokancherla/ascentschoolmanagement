package com.ascentschools.mobile.ui.teacher

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Person
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.MessagingClassDto
import com.ascentschools.mobile.data.api.MessagingStudentDto

// Start a new conversation with any currently-enrolled student — no class assignment
// required. Class → Student, then opening a student hands the resolved/created
// threadId to the caller, which navigates into the normal TeacherChatScreen (reused
// as-is; sending a message still goes through the ordinary reply flow there).
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TeacherNewMessageScreen(
    viewModel: TeacherViewModel,
    onOpened : (threadId: Int) -> Unit,
    onBack   : () -> Unit
) {
    val classes             by viewModel.messagingClasses.collectAsState()
    val students            by viewModel.messagingStudents.collectAsState()
    val isLoading           by viewModel.isLoading.collectAsState()
    val isOpeningConversation by viewModel.isOpeningConversation.collectAsState()

    var selectedClass  by remember { mutableStateOf<MessagingClassDto?>(null) }
    var classExpanded  by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) { viewModel.loadMessagingClasses() }

    Scaffold(
        topBar = {
            TopAppBar(
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "Back") }
                },
                title = { Text("New Message", fontWeight = FontWeight.Bold) }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(20.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            Text("Select a class, then a student", fontWeight = FontWeight.SemiBold, fontSize = 15.sp)

            ExposedDropdownMenuBox(
                expanded = classExpanded,
                onExpandedChange = { classExpanded = it }
            ) {
                OutlinedTextField(
                    value         = selectedClass?.className ?: "",
                    onValueChange = {},
                    readOnly      = true,
                    label         = { Text("Class") },
                    trailingIcon  = { ExposedDropdownMenuDefaults.TrailingIcon(classExpanded) },
                    modifier      = Modifier
                        .fillMaxWidth()
                        .menuAnchor()
                )
                ExposedDropdownMenu(
                    expanded = classExpanded,
                    onDismissRequest = { classExpanded = false }
                ) {
                    classes.forEach { cls ->
                        DropdownMenuItem(
                            text    = { Text(cls.className) },
                            onClick = {
                                selectedClass = cls
                                classExpanded = false
                                viewModel.loadMessagingStudents(cls.classId)
                            }
                        )
                    }
                }
            }

            if (isLoading || isOpeningConversation) {
                Box(Modifier.fillMaxWidth(), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator(modifier = Modifier.size(24.dp))
                }
            }

            when {
                selectedClass == null -> {
                    Text(
                        "Pick a class to see its students.",
                        fontSize = 13.sp,
                        color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
                    )
                }
                !isLoading && students.isEmpty() -> {
                    Text(
                        "No students found for this class.",
                        fontSize = 13.sp,
                        color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
                    )
                }
                else -> {
                    LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        items(students, key = { it.studentUniqueId }) { student ->
                            StudentRow(
                                student = student,
                                enabled = !isOpeningConversation,
                                onClick = {
                                    viewModel.openConversation(student.studentUniqueId) { threadId ->
                                        onOpened(threadId)
                                    }
                                }
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun StudentRow(
    student: MessagingStudentDto,
    enabled: Boolean,
    onClick: () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(10.dp))
            .background(MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.4f))
            .clickable(enabled = enabled, onClick = onClick)
            .padding(horizontal = 14.dp, vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Icon(
            Icons.Default.Person,
            contentDescription = null,
            tint = MaterialTheme.colorScheme.primary
        )
        Column(Modifier.weight(1f)) {
            Text(student.studentName ?: "", fontWeight = FontWeight.Medium, fontSize = 14.sp)
            Text(
                listOfNotNull(student.admissionNo, student.sectionName?.let { "Section $it" })
                    .joinToString(" · "),
                fontSize = 12.sp,
                color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
            )
        }
    }
}

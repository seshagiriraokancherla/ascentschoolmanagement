package com.ascentschools.mobile.ui.teacher

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Cake
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.BirthdayStudentDto
import java.time.LocalDate

// School-wide by default ("All Classes"), narrowed by the dropdown — same "no
// restriction" access model as Events/Messages/New Message (any teacher sees any
// student). Read-only; nothing is created here.
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TeacherBirthdaysScreen(
    viewModel: TeacherViewModel,
    onBack   : () -> Unit
) {
    val classes    by viewModel.classes.collectAsState()
    val birthdays  by viewModel.birthdays.collectAsState()
    val isLoading  by viewModel.isLoadingBirthdays.collectAsState()
    val error      by viewModel.birthdaysError.collectAsState()

    var selectedClassId   by remember { mutableStateOf<Int?>(null) }
    var selectedClassName by remember { mutableStateOf("All Classes") }
    var expanded           by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        viewModel.loadClasses()
        viewModel.loadBirthdays(null)
    }

    Scaffold(
        topBar = {
            TopAppBar(
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "Back") }
                },
                title = { Text("Birthdays Today", fontWeight = FontWeight.Bold) }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            ExposedDropdownMenuBox(
                expanded = expanded,
                onExpandedChange = { expanded = it }
            ) {
                OutlinedTextField(
                    value         = selectedClassName,
                    onValueChange = {},
                    readOnly      = true,
                    label         = { Text("Class") },
                    trailingIcon  = { ExposedDropdownMenuDefaults.TrailingIcon(expanded) },
                    modifier      = Modifier
                        .fillMaxWidth()
                        .menuAnchor()
                )
                ExposedDropdownMenu(
                    expanded = expanded,
                    onDismissRequest = { expanded = false }
                ) {
                    DropdownMenuItem(
                        text    = { Text("All Classes") },
                        onClick = {
                            selectedClassId   = null
                            selectedClassName = "All Classes"
                            expanded          = false
                            viewModel.loadBirthdays(null)
                        }
                    )
                    classes.forEach { cls ->
                        DropdownMenuItem(
                            text    = { Text(cls.className) },
                            onClick = {
                                selectedClassId   = cls.classId
                                selectedClassName = cls.className
                                expanded          = false
                                viewModel.loadBirthdays(cls.classId)
                            }
                        )
                    }
                }
            }

            when {
                isLoading -> {
                    Box(Modifier.fillMaxWidth().padding(top = 40.dp), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator()
                    }
                }
                error != null -> {
                    Column(
                        modifier = Modifier.fillMaxWidth().padding(top = 40.dp),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text(error ?: "Failed to load birthdays", color = MaterialTheme.colorScheme.error)
                        Spacer(Modifier.height(12.dp))
                        Button(onClick = { viewModel.loadBirthdays(selectedClassId) }) { Text("Retry") }
                    }
                }
                birthdays.isEmpty() -> {
                    Column(
                        modifier = Modifier.fillMaxWidth().padding(top = 40.dp),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Icon(
                            Icons.Default.Cake,
                            contentDescription = null,
                            modifier = Modifier.size(40.dp),
                            tint = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.3f)
                        )
                        Spacer(Modifier.height(8.dp))
                        Text(
                            "No birthdays today",
                            fontSize = 13.sp,
                            color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
                        )
                    }
                }
                else -> {
                    LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        items(birthdays, key = { it.studentId }) { student ->
                            BirthdayRow(student)
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun BirthdayRow(student: BirthdayStudentDto) {
    val age = remember(student.dateOfBirth) {
        student.dateOfBirth?.let {
            runCatching { LocalDate.now().year - LocalDate.parse(it).year }.getOrNull()
        }
    }

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .background(Color(0xFFFFF1F2), RoundedCornerShape(10.dp))
            .padding(horizontal = 14.dp, vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Box(
            modifier = Modifier
                .size(36.dp)
                .background(Color(0xFFFB7185), CircleShape),
            contentAlignment = Alignment.Center
        ) {
            Icon(Icons.Default.Cake, contentDescription = null, tint = Color.White, modifier = Modifier.size(18.dp))
        }
        Column(Modifier.weight(1f)) {
            Text(student.studentName ?: "", fontWeight = FontWeight.Medium, fontSize = 14.sp)
            Text(
                listOfNotNull(
                    listOfNotNull(student.className, student.sectionName).joinToString(" · ").takeIf { it.isNotBlank() },
                    student.admissionNo
                ).joinToString(" · "),
                fontSize = 12.sp,
                color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
            )
            if (age != null && age > 0) {
                Text("Turning $age today 🎉", fontSize = 12.sp, color = Color(0xFFBE123C))
            }
        }
    }
}

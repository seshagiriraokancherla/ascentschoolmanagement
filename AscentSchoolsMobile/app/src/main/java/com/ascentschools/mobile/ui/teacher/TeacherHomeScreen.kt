package com.ascentschools.mobile.ui.teacher

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarToday
import androidx.compose.material.icons.filled.Book
import androidx.compose.material.icons.filled.Logout
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.TeacherClassDto
import com.ascentschools.mobile.data.api.TeacherSectionDto

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TeacherHomeScreen(
    teacherName    : String,
    viewModel      : TeacherViewModel,
    onAttendance   : (classId: Int, sectionId: Int) -> Unit,
    onHomework     : (classId: Int) -> Unit,
    onLogout       : () -> Unit
) {
    val classes  by viewModel.classes.collectAsState()
    val sections by viewModel.sections.collectAsState()
    val isLoading by viewModel.isLoading.collectAsState()

    var selectedClass   by remember { mutableStateOf<TeacherClassDto?>(null) }
    var selectedSection by remember { mutableStateOf<TeacherSectionDto?>(null) }
    var classExpanded   by remember { mutableStateOf(false) }
    var sectionExpanded by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) { viewModel.loadClasses() }

    LaunchedEffect(selectedClass) {
        selectedSection = null
        selectedClass?.let { viewModel.loadSections(it.classId) }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text("Teacher Portal", fontWeight = FontWeight.Bold)
                        Text(teacherName, fontSize = 12.sp,
                            color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f))
                    }
                },
                actions = {
                    IconButton(onClick = onLogout) {
                        Icon(Icons.Default.Logout, contentDescription = "Logout")
                    }
                }
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
            Text("Select Class & Section", fontWeight = FontWeight.SemiBold, fontSize = 15.sp)

            // Class dropdown
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
                            text     = { Text(cls.className) },
                            onClick  = { selectedClass = cls; classExpanded = false }
                        )
                    }
                }
            }

            // Section dropdown (enabled only after class selected)
            ExposedDropdownMenuBox(
                expanded = sectionExpanded && sections.isNotEmpty(),
                onExpandedChange = { if (selectedClass != null) sectionExpanded = it }
            ) {
                OutlinedTextField(
                    value         = selectedSection?.sectionName ?: "",
                    onValueChange = {},
                    readOnly      = true,
                    label         = { Text("Section") },
                    placeholder   = { Text(if (selectedClass == null) "Select class first" else "Select section") },
                    trailingIcon  = { ExposedDropdownMenuDefaults.TrailingIcon(sectionExpanded) },
                    enabled       = selectedClass != null && sections.isNotEmpty(),
                    modifier      = Modifier
                        .fillMaxWidth()
                        .menuAnchor()
                )
                ExposedDropdownMenu(
                    expanded = sectionExpanded,
                    onDismissRequest = { sectionExpanded = false }
                ) {
                    sections.forEach { sec ->
                        DropdownMenuItem(
                            text    = { Text("Section ${sec.sectionName}") },
                            onClick = { selectedSection = sec; sectionExpanded = false }
                        )
                    }
                }
            }

            if (isLoading) {
                Box(Modifier.fillMaxWidth(), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator(modifier = Modifier.size(24.dp))
                }
            }

            Spacer(Modifier.height(8.dp))

            // Action cards — shown only when selection is complete
            val cls = selectedClass
            val sec = selectedSection

            if (cls != null) {
                // Attendance needs section; homework needs only class
                ActionCard(
                    title       = "Take Attendance",
                    subtitle    = if (sec != null) "${cls.className} · Section ${sec.sectionName}" else "Select a section to continue",
                    icon        = Icons.Default.CalendarToday,
                    enabled     = sec != null,
                    onClick     = { onAttendance(cls.classId, sec!!.sectionId) }
                )

                ActionCard(
                    title   = "Homework",
                    subtitle = cls.className,
                    icon    = Icons.Default.Book,
                    enabled = true,
                    onClick = { onHomework(cls.classId) }
                )
            }
        }
    }
}

@Composable
private fun ActionCard(
    title   : String,
    subtitle: String,
    icon    : androidx.compose.ui.graphics.vector.ImageVector,
    enabled : Boolean,
    onClick : () -> Unit
) {
    Card(
        onClick  = onClick,
        enabled  = enabled,
        shape    = RoundedCornerShape(12.dp),
        modifier = Modifier.fillMaxWidth(),
        colors   = CardDefaults.cardColors(
            containerColor = if (enabled) MaterialTheme.colorScheme.primaryContainer
                             else MaterialTheme.colorScheme.surfaceVariant
        )
    ) {
        Row(
            modifier            = Modifier.padding(20.dp),
            verticalAlignment   = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            Icon(
                imageVector = icon,
                contentDescription = null,
                tint = if (enabled) MaterialTheme.colorScheme.primary
                       else MaterialTheme.colorScheme.onSurface.copy(alpha = 0.4f),
                modifier = Modifier.size(32.dp)
            )
            Column {
                Text(title, fontWeight = FontWeight.SemiBold, fontSize = 16.sp,
                    color = if (enabled) MaterialTheme.colorScheme.onPrimaryContainer
                            else MaterialTheme.colorScheme.onSurface.copy(alpha = 0.4f))
                Text(subtitle, fontSize = 12.sp,
                    color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f))
            }
        }
    }
}

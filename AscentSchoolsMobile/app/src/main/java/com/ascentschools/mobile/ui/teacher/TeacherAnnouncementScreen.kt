package com.ascentschools.mobile.ui.teacher

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Campaign
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.TeacherAnnouncementDto
import com.ascentschools.mobile.data.api.TeacherSectionDto
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import java.time.format.FormatStyle

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TeacherAnnouncementScreen(
    classId   : Int,
    className : String,
    viewModel : TeacherViewModel,
    onBack    : () -> Unit
) {
    val announcements by viewModel.announcements.collectAsState()
    val sections      by viewModel.sections.collectAsState()
    val isLoading     by viewModel.isLoading.collectAsState()
    val uiState       by viewModel.uiState.collectAsState()
    val snackbarHost = remember { SnackbarHostState() }

    var showCreateSheet by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        viewModel.loadAnnouncements(classId)
        viewModel.loadSections(classId)
    }

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
                        Text("Announcements", fontWeight = FontWeight.Bold)
                        Text(className, fontSize = 12.sp,
                            color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f))
                    }
                }
            )
        },
        floatingActionButton = {
            FloatingActionButton(onClick = { showCreateSheet = true }) {
                Icon(Icons.Default.Add, "New announcement")
            }
        }
    ) { padding ->
        if (isLoading && announcements.isEmpty()) {
            Box(Modifier.fillMaxSize().padding(padding), contentAlignment = Alignment.Center) {
                CircularProgressIndicator()
            }
        } else if (announcements.isEmpty()) {
            Box(Modifier.fillMaxSize().padding(padding), contentAlignment = Alignment.Center) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Icon(Icons.Default.Campaign, null,
                        modifier = Modifier.size(48.dp),
                        tint = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.3f))
                    Spacer(Modifier.height(8.dp))
                    Text("No announcements yet", color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f))
                    Text("Tap + to post one", fontSize = 12.sp,
                        color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.4f))
                }
            }
        } else {
            LazyColumn(
                modifier       = Modifier.fillMaxSize().padding(padding),
                contentPadding = PaddingValues(16.dp),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                items(announcements, key = { it.announcementId }) { a ->
                    AnnouncementCard(a)
                }
            }
        }
    }

    if (showCreateSheet) {
        CreateAnnouncementSheet(
            sections  = sections,
            onDismiss = { showCreateSheet = false },
            onSave    = { sectionId, title, desc ->
                viewModel.createAnnouncement(classId, sectionId, title, desc)
            },
            isLoading = isLoading
        )
    }
}

@Composable
private fun AnnouncementCard(a: TeacherAnnouncementDto) {
    val dateDisplay = runCatching {
        LocalDate.parse(a.createdAt.take(10))
            .format(DateTimeFormatter.ofLocalizedDate(FormatStyle.MEDIUM))
    }.getOrDefault(a.createdAt.take(10))

    // School-wide announcements (posted from the web app) show alongside the class ones.
    val target = when {
        a.scope.equals("School", ignoreCase = true) -> "Whole school"
        !a.sectionName.isNullOrBlank()               -> "Section ${a.sectionName}"
        else                                         -> "Whole class"
    }

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
                Text(a.title, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
                Text(dateDisplay, fontSize = 11.sp,
                    color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f))
            }
            if (!a.description.isNullOrBlank()) {
                Text(a.description, fontSize = 13.sp,
                    color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.75f))
            }
            Text(target, fontSize = 11.sp, color = MaterialTheme.colorScheme.primary)
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CreateAnnouncementSheet(
    sections  : List<TeacherSectionDto>,
    onDismiss : () -> Unit,
    onSave    : (sectionId: Int?, title: String, description: String?) -> Unit,
    isLoading : Boolean
) {
    var title       by remember { mutableStateOf("") }
    var description by remember { mutableStateOf("") }
    var selectedSection by remember { mutableStateOf<TeacherSectionDto?>(null) }  // null = All Sections
    var sectionExpanded by remember { mutableStateOf(false) }

    ModalBottomSheet(onDismissRequest = onDismiss) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 24.dp)
                .padding(bottom = 32.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text("New Announcement", fontWeight = FontWeight.Bold, fontSize = 18.sp)

            // Section picker — "All Sections" (whole class) or a specific section.
            ExposedDropdownMenuBox(
                expanded = sectionExpanded,
                onExpandedChange = { sectionExpanded = it }
            ) {
                OutlinedTextField(
                    value         = selectedSection?.let { "Section ${it.sectionName}" } ?: "All Sections (whole class)",
                    onValueChange = {},
                    readOnly      = true,
                    label         = { Text("Send to") },
                    trailingIcon  = { ExposedDropdownMenuDefaults.TrailingIcon(sectionExpanded) },
                    modifier      = Modifier.fillMaxWidth().menuAnchor()
                )
                ExposedDropdownMenu(
                    expanded = sectionExpanded,
                    onDismissRequest = { sectionExpanded = false }
                ) {
                    DropdownMenuItem(
                        text    = { Text("All Sections (whole class)") },
                        onClick = { selectedSection = null; sectionExpanded = false }
                    )
                    sections.forEach { sec ->
                        DropdownMenuItem(
                            text    = { Text("Section ${sec.sectionName}") },
                            onClick = { selectedSection = sec; sectionExpanded = false }
                        )
                    }
                }
            }

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
                label         = { Text("Message (optional)") },
                modifier      = Modifier.fillMaxWidth(),
                minLines      = 3,
                maxLines      = 6
            )

            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedButton(
                    onClick  = onDismiss,
                    modifier = Modifier.weight(1f)
                ) { Text("Cancel") }

                Button(
                    onClick  = { onSave(selectedSection?.sectionId, title, description.takeIf { it.isNotBlank() }) },
                    enabled  = title.isNotBlank() && !isLoading,
                    modifier = Modifier.weight(1f)
                ) {
                    if (isLoading) CircularProgressIndicator(
                        modifier    = Modifier.size(18.dp),
                        strokeWidth = 2.dp,
                        color       = MaterialTheme.colorScheme.onPrimary
                    )
                    else Text("Post")
                }
            }
        }
    }
}

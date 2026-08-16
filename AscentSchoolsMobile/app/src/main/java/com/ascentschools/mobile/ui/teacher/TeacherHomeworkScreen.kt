package com.ascentschools.mobile.ui.teacher

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
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
import com.ascentschools.mobile.data.api.TeacherSectionDto

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TeacherHomeworkScreen(
    classId     : Int,
    sectionId   : Int?,
    className   : String,
    viewModel   : TeacherViewModel,
    onBack      : () -> Unit
) {
    val homework      by viewModel.homework.collectAsState()
    val total         by viewModel.homeworkTotal.collectAsState()
    val sections      by viewModel.sections.collectAsState()
    val isLoading     by viewModel.isLoading.collectAsState()
    val isLoadingMore by viewModel.isLoadingMoreHomework.collectAsState()
    val uiState       by viewModel.uiState.collectAsState()
    val snackbarHost = remember { SnackbarHostState() }
    val listState    = rememberLazyListState()

    var showCreateSheet by remember { mutableStateOf(false) }

    // Section filter. Starts on whatever the home screen was showing (null = the
    // teacher picked a class but no section there), and can be changed here so a
    // teacher doesn't have to go back just to look at another section.
    var filterSectionId by remember { mutableStateOf(sectionId) }

    LaunchedEffect(Unit) {
        viewModel.loadSections(classId)   // feeds both the filter row and the create sheet
    }

    // Keyed on the filter, so picking a chip reloads page 1 for that section.
    LaunchedEffect(filterSectionId) {
        viewModel.loadHomework(classId, filterSectionId)
    }

    // Infinite scroll — fetch the next page as the user nears the end of the list.
    LaunchedEffect(listState) {
        snapshotFlow {
            val info = listState.layoutInfo
            (info.visibleItemsInfo.lastOrNull()?.index ?: 0) to info.totalItemsCount
        }.collect { (lastVisible, totalItems) ->
            if (totalItems > 0 && lastVisible >= totalItems - 3) viewModel.loadMoreHomework()
        }
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
        Column(Modifier.fillMaxSize().padding(padding)) {

            // Filter row sits ABOVE the loading/empty branches on purpose: filtering to
            // a section with no homework must still leave a way back to "All sections".
            if (sections.isNotEmpty()) {
                LazyRow(
                    contentPadding = PaddingValues(horizontal = 16.dp, vertical = 8.dp),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    item {
                        FilterChip(
                            selected = filterSectionId == null,
                            onClick  = { filterSectionId = null },
                            label    = { Text("All sections") }
                        )
                    }
                    items(sections, key = { it.sectionId }) { sec ->
                        FilterChip(
                            selected = filterSectionId == sec.sectionId,
                            onClick  = { filterSectionId = sec.sectionId },
                            label    = { Text(sec.sectionName) }
                        )
                    }
                }
            }

            when {
                isLoading && homework.isEmpty() ->
                    Box(Modifier.fillMaxWidth().weight(1f), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator()
                    }

                homework.isEmpty() ->
                    Box(Modifier.fillMaxWidth().weight(1f), contentAlignment = Alignment.Center) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Icon(Icons.Default.Book, null,
                                modifier = Modifier.size(48.dp),
                                tint = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.3f))
                            Spacer(Modifier.height(8.dp))
                            Text(
                                if (filterSectionId == null) "No homework yet"
                                else "No homework for this section",
                                color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f)
                            )
                            Text("Tap + to add homework", fontSize = 12.sp,
                                color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.4f))
                        }
                    }

                else -> {
                    Text(
                        // A section filter also returns homework posted to the whole
                        // class — say so, or the extra rows look like the filter failed.
                        if (filterSectionId == null) "Showing ${homework.size} of $total"
                        else "Showing ${homework.size} of $total · includes whole-class homework",
                        fontSize = 12.sp,
                        color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f),
                        modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp)
                    )
                    LazyColumn(
                        state          = listState,
                        modifier       = Modifier.fillMaxWidth().weight(1f),
                        contentPadding = PaddingValues(start = 16.dp, end = 16.dp, bottom = 16.dp),
                        verticalArrangement = Arrangement.spacedBy(10.dp)
                    ) {
                        items(homework, key = { it.homeworkId }) { hw ->
                            HomeworkCard(hw)
                        }
                        if (isLoadingMore) {
                            item {
                                Box(Modifier.fillMaxWidth().padding(12.dp), contentAlignment = Alignment.Center) {
                                    CircularProgressIndicator(Modifier.size(24.dp), strokeWidth = 2.dp)
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    if (showCreateSheet) {
        CreateHomeworkSheet(
            sections        = sections,
            // Default "Post to" to whatever section is being viewed, not the one the
            // home screen opened with.
            initialSectionId = filterSectionId,
            onDismiss       = { showCreateSheet = false },
            onSave          = { chosenSectionId, title, desc ->
                viewModel.createHomework(classId, chosenSectionId, title, desc)
            },
            isLoading       = isLoading
        )
    }
}

@Composable
private fun HomeworkCard(hw: TeacherHomeworkDto) {
    Card(
        shape  = RoundedCornerShape(10.dp),
        colors = CardDefaults.cardColors()
    ) {
        Column(Modifier.padding(14.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
            Text(hw.title, fontWeight = FontWeight.SemiBold, modifier = Modifier.fillMaxWidth())
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
            Row(
                horizontalArrangement = Arrangement.spacedBy(12.dp),
                verticalAlignment     = Alignment.CenterVertically
            ) {
                Text("📅 ${formatHomeworkDate(hw.assignedDate)}", fontSize = 12.sp,
                    color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f))
                Text("🏫 ${hw.sectionName?.takeIf { it.isNotBlank() } ?: "All sections"}",
                    fontSize = 12.sp,
                    color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f))
            }
        }
    }
}

/** Formats the ISO assigned-date string (e.g. "2026-07-28T00:00:00") as dd-MM-yyyy. */
private fun formatHomeworkDate(raw: String): String {
    val datePart = raw.take(10)               // yyyy-MM-dd
    val parts = datePart.split("-")
    return if (parts.size == 3) "${parts[2]}-${parts[1]}-${parts[0]}" else datePart
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CreateHomeworkSheet(
    sections         : List<TeacherSectionDto>,
    initialSectionId : Int?,
    onDismiss        : () -> Unit,
    onSave           : (sectionId: Int?, title: String, description: String?) -> Unit,
    isLoading        : Boolean
) {
    var title       by remember { mutableStateOf("") }
    var description by remember { mutableStateOf("") }
    var sectionId   by remember { mutableStateOf(initialSectionId) }
    var sectionMenuOpen by remember { mutableStateOf(false) }

    val WHOLE_CLASS = "Whole class (all sections)"
    val sectionLabel = sections.find { it.sectionId == sectionId }
        ?.let { "Section ${it.sectionName}" } ?: WHOLE_CLASS

    ModalBottomSheet(onDismissRequest = onDismiss) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 24.dp)
                .padding(bottom = 32.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text("Add Homework", fontWeight = FontWeight.Bold, fontSize = 18.sp)

            ExposedDropdownMenuBox(
                expanded = sectionMenuOpen,
                onExpandedChange = { sectionMenuOpen = it }
            ) {
                OutlinedTextField(
                    value         = sectionLabel,
                    onValueChange = {},
                    readOnly      = true,
                    label         = { Text("Post to") },
                    trailingIcon  = { ExposedDropdownMenuDefaults.TrailingIcon(sectionMenuOpen) },
                    modifier      = Modifier.menuAnchor().fillMaxWidth()
                )
                ExposedDropdownMenu(
                    expanded = sectionMenuOpen,
                    onDismissRequest = { sectionMenuOpen = false }
                ) {
                    DropdownMenuItem(
                        text = { Text(WHOLE_CLASS) },
                        onClick = { sectionId = null; sectionMenuOpen = false }
                    )
                    sections.forEach { sec ->
                        DropdownMenuItem(
                            text = { Text("Section ${sec.sectionName}") },
                            onClick = { sectionId = sec.sectionId; sectionMenuOpen = false }
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
                label         = { Text("Description (optional)") },
                modifier      = Modifier.fillMaxWidth(),
                minLines      = 2,
                maxLines      = 4
            )

            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedButton(
                    onClick  = onDismiss,
                    modifier = Modifier.weight(1f)
                ) { Text("Cancel") }

                Button(
                    onClick  = { onSave(sectionId, title, description.takeIf { it.isNotBlank() }) },
                    enabled  = title.isNotBlank() && !isLoading,
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
}

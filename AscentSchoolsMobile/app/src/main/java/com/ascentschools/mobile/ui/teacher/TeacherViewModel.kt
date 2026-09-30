package com.ascentschools.mobile.ui.teacher

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ascentschools.mobile.data.api.*
import com.ascentschools.mobile.data.repository.TeacherRepository
import com.ascentschools.mobile.ui.events.EventsUiState
import com.ascentschools.mobile.ui.marks.ExamTimetableUiState
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import java.time.LocalDate
import java.time.format.DateTimeFormatter

enum class AttendanceStatus { Present, Absent, Late, HalfDay }

data class StudentAttendanceState(
    val studentId   : Long,
    val studentName : String,
    val admissionNo : String,
    val status      : AttendanceStatus = AttendanceStatus.Present
)

data class StudentMarkState(
    val studentId   : Long,
    val studentName : String,
    val admissionNo : String,
    val marks       : String = "",   // text so the field can be blank
    val activity    : String = "",
    val absent      : Boolean = false
)

sealed class TeacherUiState {
    object Idle    : TeacherUiState()
    object Loading : TeacherUiState()
    data class Error(val message: String) : TeacherUiState()
    data class Success(val message: String) : TeacherUiState()
}

class TeacherViewModel(private val repo: TeacherRepository) : ViewModel() {

    // ── Class / Section selection ─────────────────────────────────────────────

    private val _classes = MutableStateFlow<List<TeacherClassDto>>(emptyList())
    val classes = _classes.asStateFlow()

    private val _sections = MutableStateFlow<List<TeacherSectionDto>>(emptyList())
    val sections = _sections.asStateFlow()

    // ── Attendance ────────────────────────────────────────────────────────────

    private val _attendanceStudents = MutableStateFlow<List<StudentAttendanceState>>(emptyList())
    val attendanceStudents = _attendanceStudents.asStateFlow()

    private val _selectedDate = MutableStateFlow(LocalDate.now().format(DateTimeFormatter.ISO_LOCAL_DATE))
    val selectedDate = _selectedDate.asStateFlow()

    private val _className = MutableStateFlow("")
    val className = _className.asStateFlow()

    private val _isAttendanceAlreadySaved = MutableStateFlow(false)
    val isAttendanceAlreadySaved = _isAttendanceAlreadySaved.asStateFlow()

    // ── Homework ──────────────────────────────────────────────────────────────

    private val _homework = MutableStateFlow<List<TeacherHomeworkDto>>(emptyList())
    val homework = _homework.asStateFlow()

    // Pagination — accumulate pages via infinite scroll; bounds the payload as history grows.
    private val _isLoadingMoreHomework = MutableStateFlow(false)
    val isLoadingMoreHomework = _isLoadingMoreHomework.asStateFlow()
    private val _homeworkTotal = MutableStateFlow(0)   // total matching rows on the server
    val homeworkTotal = _homeworkTotal.asStateFlow()
    private var homeworkClassId   = 0
    private var homeworkSectionId : Int? = null
    private var homeworkPage      = 0

    // ── Announcements ─────────────────────────────────────────────────────────

    private val _announcements = MutableStateFlow<List<TeacherAnnouncementDto>>(emptyList())
    val announcements = _announcements.asStateFlow()

    // ── Events (read-only — same gallery parents see, reuses EventsScreen) ─────

    private val _eventsState = MutableStateFlow<EventsUiState>(EventsUiState.Loading)
    val eventsState = _eventsState.asStateFlow()

    // ── Birthdays (school-wide by default; classId narrows it) ─────────────────

    private val _birthdays = MutableStateFlow<List<BirthdayStudentDto>>(emptyList())
    val birthdays = _birthdays.asStateFlow()
    private val _isLoadingBirthdays = MutableStateFlow(false)
    val isLoadingBirthdays = _isLoadingBirthdays.asStateFlow()
    private val _birthdaysError = MutableStateFlow<String?>(null)
    val birthdaysError = _birthdaysError.asStateFlow()

    fun loadBirthdays(classId: Int?) {
        _isLoadingBirthdays.value = true
        _birthdaysError.value = null
        viewModelScope.launch {
            repo.getBirthdays(classId)
                .onSuccess { _birthdays.value = it }
                .onFailure { _birthdaysError.value = it.message ?: "Failed to load birthdays" }
            _isLoadingBirthdays.value = false
        }
    }

    // ── Marks ─────────────────────────────────────────────────────────────────

    private val _examTypes = MutableStateFlow<List<TeacherExamTypeDto>>(emptyList())
    val examTypes = _examTypes.asStateFlow()

    private val _marksSubjects = MutableStateFlow<List<TeacherMarksSubjectDto>>(emptyList())
    val marksSubjects = _marksSubjects.asStateFlow()

    private val _marksHeader = MutableStateFlow<TeacherSubjectMarksDto?>(null)
    val marksHeader = _marksHeader.asStateFlow()

    private val _markStudents = MutableStateFlow<List<StudentMarkState>>(emptyList())
    val markStudents = _markStudents.asStateFlow()

    // ── Messaging ─────────────────────────────────────────────────────────────

    private val _threads = MutableStateFlow<List<MessageThreadDto>>(emptyList())
    val threads = _threads.asStateFlow()

    private val _thread = MutableStateFlow<MessageThreadDetailDto?>(null)
    val thread = _thread.asStateFlow()

    // Start-a-new-conversation picker — any student, no class assignment required.
    private val _messagingClasses = MutableStateFlow<List<MessagingClassDto>>(emptyList())
    val messagingClasses = _messagingClasses.asStateFlow()

    private val _messagingStudents = MutableStateFlow<List<MessagingStudentDto>>(emptyList())
    val messagingStudents = _messagingStudents.asStateFlow()

    private val _isOpeningConversation = MutableStateFlow(false)
    val isOpeningConversation = _isOpeningConversation.asStateFlow()

    // ── Shared UI state ───────────────────────────────────────────────────────

    private val _uiState = MutableStateFlow<TeacherUiState>(TeacherUiState.Idle)
    val uiState = _uiState.asStateFlow()

    private val _isLoading = MutableStateFlow(false)
    val isLoading = _isLoading.asStateFlow()

    // ── Actions ───────────────────────────────────────────────────────────────

    fun loadClasses() {
        viewModelScope.launch {
            _isLoading.value = true
            repo.getClasses()
                .onSuccess { _classes.value = it }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed to load classes") }
            _isLoading.value = false
        }
    }

    fun loadSections(classId: Int) {
        _sections.value = emptyList()
        viewModelScope.launch {
            repo.getSections(classId)
                .onSuccess { _sections.value = it }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed to load sections") }
        }
    }

    fun setDate(date: String) {
        _selectedDate.value = date
    }

    fun loadAttendance(classId: Int, sectionId: Int) {
        viewModelScope.launch {
            _isLoading.value = true
            repo.getAttendance(classId, sectionId, _selectedDate.value)
                .onSuccess { grid ->
                    _className.value = grid.className
                    _isAttendanceAlreadySaved.value = grid.isMarked
                    _attendanceStudents.value = grid.students.map { s ->
                        StudentAttendanceState(
                            studentId   = s.studentId,
                            studentName = s.studentName,
                            admissionNo = s.admissionNo,
                            // Default to Present if not yet marked; use saved value if already marked
                            status = when (s.status?.lowercase()) {
                                "absent"  -> AttendanceStatus.Absent
                                "late"    -> AttendanceStatus.Late
                                "halfday" -> AttendanceStatus.HalfDay
                                else      -> AttendanceStatus.Present
                            }
                        )
                    }
                }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed to load attendance") }
            _isLoading.value = false
        }
    }

    fun toggleStudentStatus(studentId: Long) {
        _attendanceStudents.value = _attendanceStudents.value.map { s ->
            if (s.studentId == studentId) {
                // Quick tap toggles Present <-> Absent; Late/HalfDay are set via long-press sheet.
                s.copy(status = when (s.status) {
                    AttendanceStatus.Present -> AttendanceStatus.Absent
                    AttendanceStatus.Absent  -> AttendanceStatus.Present
                    AttendanceStatus.Late    -> AttendanceStatus.Present
                    AttendanceStatus.HalfDay -> AttendanceStatus.Present
                })
            } else s
        }
    }

    fun setStudentStatus(studentId: Long, status: AttendanceStatus) {
        _attendanceStudents.value = _attendanceStudents.value.map { s ->
            if (s.studentId == studentId) s.copy(status = status) else s
        }
    }

    fun markAllPresent() {
        _attendanceStudents.value = _attendanceStudents.value.map { it.copy(status = AttendanceStatus.Present) }
    }

    fun saveAttendance(classId: Int, sectionId: Int) {
        val students = _attendanceStudents.value
        if (students.isEmpty()) return

        viewModelScope.launch {
            _isLoading.value = true
            val request = TeacherSaveAttendanceRequest(
                classId   = classId,
                sectionId = sectionId,
                date      = _selectedDate.value,
                entries   = students.map { s ->
                    TeacherAttendanceEntry(
                        studentId = s.studentId,
                        status    = s.status.name  // "Present" / "Absent" / "Late" / "HalfDay"
                    )
                }
            )
            repo.saveAttendance(request)
                .onSuccess {
                    _isAttendanceAlreadySaved.value = true
                    _uiState.value = TeacherUiState.Success("Attendance saved for ${students.size} students.")
                }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Save failed") }
            _isLoading.value = false
        }
    }

    fun loadHomework(classId: Int, sectionId: Int? = null) {
        homeworkClassId   = classId
        homeworkSectionId = sectionId
        viewModelScope.launch {
            _isLoading.value = true
            _homework.value = emptyList()
            homeworkPage         = 0
            _homeworkTotal.value = 0
            repo.getHomework(classId, sectionId, 1, HOMEWORK_PAGE_SIZE)
                .onSuccess {
                    _homework.value      = it.items
                    _homeworkTotal.value = it.total
                    homeworkPage         = 1
                }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed to load homework") }
            _isLoading.value = false
        }
    }

    /** Fetches the next page and appends it. No-op when a load is in flight or all rows are loaded. */
    fun loadMoreHomework() {
        if (_isLoading.value || _isLoadingMoreHomework.value) return
        if (homeworkPage == 0 || _homework.value.size >= _homeworkTotal.value) return
        val next = homeworkPage + 1
        viewModelScope.launch {
            _isLoadingMoreHomework.value = true
            repo.getHomework(homeworkClassId, homeworkSectionId, next, HOMEWORK_PAGE_SIZE)
                .onSuccess {
                    _homework.value      = _homework.value + it.items
                    _homeworkTotal.value = it.total
                    homeworkPage         = next
                }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed to load more homework") }
            _isLoadingMoreHomework.value = false
        }
    }

    fun createHomework(classId: Int, sectionId: Int?, title: String, description: String?) {
        if (title.isBlank()) { _uiState.value = TeacherUiState.Error("Title is required"); return }

        viewModelScope.launch {
            _isLoading.value = true
            val today = LocalDate.now().format(DateTimeFormatter.ISO_LOCAL_DATE)
            repo.createHomework(TeacherCreateHomeworkRequest(
                classId     = classId,
                sectionId   = sectionId,
                title       = title,
                description = description?.takeIf { it.isNotBlank() },
                assignedDate = today
            ))
                .onSuccess {
                    _uiState.value = TeacherUiState.Success("Homework created.")
                    loadHomework(classId, homeworkSectionId)
                }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Create failed") }
            _isLoading.value = false
        }
    }

    fun loadAnnouncements(classId: Int) {
        viewModelScope.launch {
            _isLoading.value = true
            repo.getAnnouncements(classId)
                .onSuccess { _announcements.value = it }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed to load announcements") }
            _isLoading.value = false
        }
    }

    fun createAnnouncement(classId: Int, sectionId: Int?, title: String, description: String?) {
        if (title.isBlank()) { _uiState.value = TeacherUiState.Error("Title is required"); return }

        viewModelScope.launch {
            _isLoading.value = true
            repo.createAnnouncement(TeacherCreateAnnouncementRequest(
                classId     = classId,
                sectionId   = sectionId,
                title       = title.trim(),
                description = description?.takeIf { it.isNotBlank() }
            ))
                .onSuccess {
                    _uiState.value = TeacherUiState.Success("Announcement posted.")
                    loadAnnouncements(classId)
                }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Post failed") }
            _isLoading.value = false
        }
    }

    fun loadEvents(classId: Int?) {
        _eventsState.value = EventsUiState.Loading
        viewModelScope.launch {
            repo.getEvents(classId)
                .onSuccess { _eventsState.value = EventsUiState.Success(it) }
                .onFailure { _eventsState.value = EventsUiState.Error(it.message ?: "Failed to load events") }
        }
    }

    // ── Exam timetable (read-only — same schedule students see) ────────────────

    private val _examTimetableState = MutableStateFlow<ExamTimetableUiState>(ExamTimetableUiState.Loading)
    val examTimetableState = _examTimetableState.asStateFlow()

    fun loadExamTimetable(classId: Int) {
        _examTimetableState.value = ExamTimetableUiState.Loading
        viewModelScope.launch {
            repo.getExamTimetable(classId)
                .onSuccess { _examTimetableState.value = ExamTimetableUiState.Success(it) }
                .onFailure { _examTimetableState.value = ExamTimetableUiState.Error(it.message ?: "Failed to load exam timetable") }
        }
    }

    // ── Marks ─────────────────────────────────────────────────────────────────

    fun loadExamTypes() {
        viewModelScope.launch {
            repo.getExamTypes()
                .onSuccess { _examTypes.value = it }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed to load exams") }
        }
    }

    fun loadMarksSubjects(classId: Int) {
        _marksSubjects.value = emptyList()
        viewModelScope.launch {
            repo.getMarksSubjects(classId)
                .onSuccess { _marksSubjects.value = it }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed to load subjects") }
        }
    }

    /** Clears the loaded student list — call when exam/subject changes so stale marks don't linger. */
    fun clearMarks() {
        _marksHeader.value = null
        _markStudents.value = emptyList()
    }

    fun loadMarks(classId: Int, sectionId: Int, examTypeId: Int, subjectId: Int) {
        viewModelScope.launch {
            _isLoading.value = true
            repo.getMarks(classId, sectionId, examTypeId, subjectId)
                .onSuccess { h ->
                    _marksHeader.value = h
                    _markStudents.value = h.students.map { s ->
                        StudentMarkState(
                            studentId   = s.studentId,
                            studentName = s.studentName,
                            admissionNo = s.admissionNo,
                            marks       = s.marksObtained?.let { fmt(it) } ?: "",
                            activity    = s.activityMarks?.let { fmt(it) } ?: "",
                            absent      = s.isAbsent
                        )
                    }
                }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed to load marks") }
            _isLoading.value = false
        }
    }

    fun setMark(studentId: Long, value: String) {
        _markStudents.value = _markStudents.value.map {
            if (it.studentId == studentId) it.copy(marks = value) else it
        }
    }

    fun setActivity(studentId: Long, value: String) {
        _markStudents.value = _markStudents.value.map {
            if (it.studentId == studentId) it.copy(activity = value) else it
        }
    }

    fun toggleMarkAbsent(studentId: Long) {
        _markStudents.value = _markStudents.value.map {
            if (it.studentId == studentId) {
                val a = !it.absent
                if (a) it.copy(absent = true, marks = "", activity = "") else it.copy(absent = false)
            } else it
        }
    }

    fun saveMarks(classId: Int, sectionId: Int, examTypeId: Int) {
        val header = _marksHeader.value ?: return
        val students = _markStudents.value
        // Only send rows with something entered (or marked absent) — no junk 0 rows.
        val entries = students.mapNotNull { s ->
            val hasMark = s.marks.isNotBlank()
            val hasAct  = s.activity.isNotBlank()
            if (!s.absent && !hasMark && !hasAct) null
            else TeacherMarkEntry(
                studentId     = s.studentId,
                marksObtained = if (s.absent) 0.0 else s.marks.toDoubleOrNull() ?: 0.0,
                activityMarks = if (s.absent || !header.hasActivity || !hasAct) null else s.activity.toDoubleOrNull(),
                isAbsent      = s.absent
            )
        }
        if (entries.isEmpty()) { _uiState.value = TeacherUiState.Error("Enter at least one mark."); return }

        viewModelScope.launch {
            _isLoading.value = true
            repo.saveMarks(TeacherSaveMarksRequest(
                classId          = classId,
                sectionId        = sectionId,
                examTypeId       = examTypeId,
                subjectId        = header.subjectId,
                examId           = header.examId,
                maxMarks         = header.maxMarks,
                activityMaxMarks = header.activityMaxMarks,
                entries          = entries
            ))
                .onSuccess { _uiState.value = TeacherUiState.Success("Marks saved for ${entries.size} students.") }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Save failed") }
            _isLoading.value = false
        }
    }

    // Trim a trailing ".0" so whole numbers show as "45" not "45.0".
    private fun fmt(d: Double): String =
        if (d == d.toLong().toDouble()) d.toLong().toString() else d.toString()

    // ── Messaging ─────────────────────────────────────────────────────────────

    fun loadThreads() {
        viewModelScope.launch {
            _isLoading.value = true
            repo.getThreads()
                .onSuccess { _threads.value = it }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed to load messages") }
            _isLoading.value = false
        }
    }

    // ── Start a new conversation ────────────────────────────────────────────

    fun loadMessagingClasses() {
        viewModelScope.launch {
            _isLoading.value = true
            repo.getMessagingClasses()
                .onSuccess { _messagingClasses.value = it }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed to load classes") }
            _isLoading.value = false
        }
    }

    fun loadMessagingStudents(classId: Int) {
        _messagingStudents.value = emptyList()
        viewModelScope.launch {
            _isLoading.value = true
            repo.getMessagingStudents(classId)
                .onSuccess { _messagingStudents.value = it }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed to load students") }
            _isLoading.value = false
        }
    }

    /** Resolves/creates the thread for a student, then hands the threadId to onOpened. */
    fun openConversation(studentUniqueId: Int, onOpened: (threadId: Int) -> Unit) {
        viewModelScope.launch {
            _isOpeningConversation.value = true
            repo.openConversation(studentUniqueId)
                .onSuccess { onOpened(it) }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Couldn't open conversation") }
            _isOpeningConversation.value = false
        }
    }

    fun loadThread(threadId: Int) {
        viewModelScope.launch {
            _isLoading.value = true
            repo.getThread(threadId)
                .onSuccess {
                    _thread.value = it
                    // Opening the conversation marks the parent's messages read.
                    repo.markThreadRead(threadId)
                }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed to load conversation") }
            _isLoading.value = false
        }
    }

    /** Refresh without the spinner — used after a reply so the chat doesn't flash. */
    private fun reloadThread(threadId: Int) {
        viewModelScope.launch {
            repo.getThread(threadId).onSuccess { _thread.value = it }
        }
    }

    fun reply(threadId: Int, text: String) {
        if (text.isBlank()) return
        viewModelScope.launch {
            _isLoading.value = true
            repo.reply(threadId, text.trim())
                .onSuccess { reloadThread(threadId) }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Reply failed") }
            _isLoading.value = false
        }
    }

    fun reportMessage(threadId: Int, messageId: Int, reason: String?) {
        viewModelScope.launch {
            _isLoading.value = true
            repo.reportMessage(threadId, messageId, reason)
                .onSuccess { _uiState.value = TeacherUiState.Success("Reported. The school will review this message.") }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Report failed") }
            _isLoading.value = false
        }
    }

    fun setThreadBlocked(threadId: Int, blocked: Boolean) {
        viewModelScope.launch {
            _isLoading.value = true
            val result = if (blocked) repo.blockThread(threadId) else repo.unblockThread(threadId)
            result
                .onSuccess {
                    _uiState.value = TeacherUiState.Success(
                        if (blocked) "Conversation blocked." else "Conversation unblocked.")
                    reloadThread(threadId)
                }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed") }
            _isLoading.value = false
        }
    }

    /** Clears the open conversation so a stale one never flashes on the next open. */
    fun clearThread() { _thread.value = null }

    fun clearUiState() { _uiState.value = TeacherUiState.Idle }

    // ── Derived counts ────────────────────────────────────────────────────────

    val presentCount get() = _attendanceStudents.value.count { it.status == AttendanceStatus.Present }
    val absentCount  get() = _attendanceStudents.value.count { it.status == AttendanceStatus.Absent  }
    val lateCount    get() = _attendanceStudents.value.count { it.status == AttendanceStatus.Late    }

    companion object {
        private const val HOMEWORK_PAGE_SIZE = 20
    }
}

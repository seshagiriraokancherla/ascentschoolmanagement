package com.ascentschools.mobile.ui.teacher

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ascentschools.mobile.data.api.*
import com.ascentschools.mobile.data.repository.TeacherRepository
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import java.time.LocalDate
import java.time.format.DateTimeFormatter

enum class AttendanceStatus { Present, Absent, Late }

data class StudentAttendanceState(
    val studentId   : Long,
    val studentName : String,
    val admissionNo : String,
    val status      : AttendanceStatus = AttendanceStatus.Present
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
                s.copy(status = when (s.status) {
                    AttendanceStatus.Present -> AttendanceStatus.Absent
                    AttendanceStatus.Absent  -> AttendanceStatus.Present
                    AttendanceStatus.Late    -> AttendanceStatus.Present
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
                        status    = s.status.name  // "Present" / "Absent" / "Late"
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

    fun loadHomework(classId: Int) {
        viewModelScope.launch {
            _isLoading.value = true
            repo.getHomework(classId)
                .onSuccess { _homework.value = it }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Failed to load homework") }
            _isLoading.value = false
        }
    }

    fun createHomework(classId: Int, title: String, description: String?, dueDate: String) {
        if (title.isBlank()) { _uiState.value = TeacherUiState.Error("Title is required"); return }
        if (dueDate.isBlank()) { _uiState.value = TeacherUiState.Error("Due date is required"); return }

        viewModelScope.launch {
            _isLoading.value = true
            val today = LocalDate.now().format(DateTimeFormatter.ISO_LOCAL_DATE)
            repo.createHomework(TeacherCreateHomeworkRequest(
                classId     = classId,
                title       = title,
                description = description?.takeIf { it.isNotBlank() },
                assignedDate = today,
                dueDate     = dueDate
            ))
                .onSuccess {
                    _uiState.value = TeacherUiState.Success("Homework created.")
                    loadHomework(classId)
                }
                .onFailure { _uiState.value = TeacherUiState.Error(it.message ?: "Create failed") }
            _isLoading.value = false
        }
    }

    fun clearUiState() { _uiState.value = TeacherUiState.Idle }

    // ── Derived counts ────────────────────────────────────────────────────────

    val presentCount get() = _attendanceStudents.value.count { it.status == AttendanceStatus.Present }
    val absentCount  get() = _attendanceStudents.value.count { it.status == AttendanceStatus.Absent  }
    val lateCount    get() = _attendanceStudents.value.count { it.status == AttendanceStatus.Late    }
}

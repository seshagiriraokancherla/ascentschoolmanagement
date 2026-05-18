Attribute VB_Name = "AscentAPI"
'==============================================================================
' AscentAPI.bas  —  VB6 Integration Module for Ascent Schools API
'==============================================================================
' HOW TO USE
'   1. Add this .bas file to your VB6 project (Project → Add Module → Existing)
'   2. Set API_KEY below to the key generated for your school (see setup steps)
'   3. All public functions return JSON strings. Use the Json* helpers to read
'      individual fields from those strings.
'
' SETUP (run once in SSMS against ascent_master)
'   -- Generate a key for school_id = 1:
'   UPDATE school_settings
'   SET    integration_api_key = LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
'   WHERE  school_id = 1;
'
'   -- Copy the key into API_KEY below:
'   SELECT integration_api_key FROM school_settings WHERE school_id = 1;
'
' REQUIREMENTS
'   Windows with WinHttp (included on all Windows versions from XP onwards).
'   No extra DLLs or references needed — uses late binding throughout.
'==============================================================================
Option Explicit

' ── Configuration — edit these two lines ─────────────────────────────────────
Private Const API_BASE_URL As String = "https://edu-care.in/api/"
Private Const API_KEY      As String = "PASTE-YOUR-KEY-HERE"

'==============================================================================
' SECTION 1 — CORE HTTP
'==============================================================================

' Generic HTTP call. Returns the raw response JSON string.
' On network/server error returns {"success":false,"message":"<error>"}.
Private Function ApiCall(sMethod As String, sEndpoint As String, _
                         Optional sBody As String = "") As String
    On Error GoTo ErrHandler

    Dim oHttp As Object
    Set oHttp = CreateObject("WinHttp.WinHttpRequest.5.1")

    oHttp.Open sMethod, API_BASE_URL & sEndpoint, False   ' False = synchronous

    oHttp.SetRequestHeader "Content-Type", "application/json"
    oHttp.SetRequestHeader "Accept",       "application/json"
    oHttp.SetRequestHeader "X-Api-Key",    API_KEY

    If Len(sBody) > 0 Then
        oHttp.Send sBody
    Else
        oHttp.Send
    End If

    ApiCall = oHttp.ResponseText
    Set oHttp = Nothing
    Exit Function

ErrHandler:
    ApiCall = "{""success"":false,""message"":""HTTP error: " & _
              Replace(Err.Description, """", "'") & """}"
End Function

Private Function ApiGet(sEndpoint As String) As String
    ApiGet = ApiCall("GET", sEndpoint)
End Function

Private Function ApiPost(sEndpoint As String, sBody As String) As String
    ApiPost = ApiCall("POST", sEndpoint, sBody)
End Function

Private Function ApiPut(sEndpoint As String, sBody As String) As String
    ApiPut = ApiCall("PUT", sEndpoint, sBody)
End Function

'==============================================================================
' SECTION 2 — JSON HELPERS
' All functions work on the raw JSON strings returned by the API.
'==============================================================================

' Extract a string value for a key.
' e.g. JsonStr("{""name"":""Ravi""}", "name")  →  "Ravi"
' Handles escaped characters (\", \\, \n, \r, \t).
Public Function JsonStr(sJson As String, sKey As String) As String
    Dim sPattern As String
    Dim nPos     As Long
    Dim i        As Long
    Dim sChar    As String
    Dim sResult  As String

    sPattern = """" & sKey & """:"""
    nPos = InStr(sJson, sPattern)
    If nPos = 0 Then Exit Function

    i = nPos + Len(sPattern)
    sResult = ""

    Do While i <= Len(sJson)
        sChar = Mid(sJson, i, 1)
        If sChar = "\" Then
            i = i + 1
            If i <= Len(sJson) Then
                sChar = Mid(sJson, i, 1)
                Select Case sChar
                    Case """":  sResult = sResult & """"
                    Case "\": sResult = sResult & "\"
                    Case "n":   sResult = sResult & Chr(10)
                    Case "r":   sResult = sResult & Chr(13)
                    Case "t":   sResult = sResult & Chr(9)
                    Case Else:  sResult = sResult & "\" & sChar
                End Select
            End If
        ElseIf sChar = """" Then
            Exit Do
        Else
            sResult = sResult & sChar
        End If
        i = i + 1
    Loop

    JsonStr = sResult
End Function

' Extract a numeric value for a key. Returns as String — convert with CLng/CDbl.
' e.g. JsonNum("{""id"":42}", "id")  →  "42"
' Returns "" if key not found or value is null.
Public Function JsonNum(sJson As String, sKey As String) As String
    Dim sPattern As String
    Dim nPos     As Long
    Dim sChar    As String
    Dim sResult  As String

    sPattern = """" & sKey & """:"
    nPos = InStr(sJson, sPattern)
    If nPos = 0 Then Exit Function

    nPos = nPos + Len(sPattern)

    ' Skip whitespace
    Do While nPos <= Len(sJson) And Mid(sJson, nPos, 1) = " "
        nPos = nPos + 1
    Loop

    ' null → return ""
    If Mid(sJson, nPos, 4) = "null" Then Exit Function

    ' Read all numeric characters
    sResult = ""
    Do While nPos <= Len(sJson)
        sChar = Mid(sJson, nPos, 1)
        If InStr("0123456789.-+eE", sChar) > 0 Then
            sResult = sResult & sChar
            nPos = nPos + 1
        Else
            Exit Do
        End If
    Loop

    JsonNum = sResult
End Function

' Extract a boolean value. Returns True/False.
' e.g. JsonBool("{""success"":true}", "success")  →  True
Public Function JsonBool(sJson As String, sKey As String) As Boolean
    Dim sPattern As String
    Dim nPos     As Long

    sPattern = """" & sKey & """:"
    nPos = InStr(sJson, sPattern)
    If nPos = 0 Then Exit Function

    nPos = nPos + Len(sPattern)
    Do While nPos <= Len(sJson) And Mid(sJson, nPos, 1) = " "
        nPos = nPos + 1
    Loop

    JsonBool = (Mid(sJson, nPos, 4) = "true")
End Function

' Check whether an API response indicates success.
' e.g. JsonSuccess("{""success"":true,...}")  →  True
Public Function JsonSuccess(sJson As String) As Boolean
    JsonSuccess = JsonBool(sJson, "success")
End Function

' Extract the error/info message from an API response.
Public Function JsonMessage(sJson As String) As String
    JsonMessage = JsonStr(sJson, "message")
End Function

' Extract the "data" object or array from an API response envelope.
' e.g. for {"success":true,"data":{...}}  returns  "{...}"
' e.g. for {"success":true,"data":[...]}  returns  "[...]"
' Returns "" if data is null or missing.
Public Function JsonData(sJson As String) As String
    Dim sPattern As String
    Dim nPos     As Long
    Dim nStart   As Long
    Dim nDepth   As Long
    Dim i        As Long
    Dim sChar    As String
    Dim bInStr   As Boolean

    sPattern = """data"":"
    nPos = InStr(sJson, sPattern)
    If nPos = 0 Then Exit Function

    nPos = nPos + Len(sPattern)

    ' Skip whitespace
    Do While nPos <= Len(sJson) And Mid(sJson, nPos, 1) = " "
        nPos = nPos + 1
    Loop

    sChar = Mid(sJson, nPos, 1)
    If sChar = "n" Then Exit Function          ' null
    If sChar <> "{" And sChar <> "[" Then Exit Function  ' primitive

    nStart  = nPos
    nDepth  = 0
    bInStr  = False
    i       = nPos

    Do While i <= Len(sJson)
        sChar = Mid(sJson, i, 1)
        If bInStr Then
            If sChar = "\" Then
                i = i + 1   ' skip escaped char
            ElseIf sChar = """" Then
                bInStr = False
            End If
        Else
            Select Case sChar
                Case """":        bInStr = True
                Case "{", "[":    nDepth = nDepth + 1
                Case "}", "]":
                    nDepth = nDepth - 1
                    If nDepth = 0 Then
                        JsonData = Mid(sJson, nStart, i - nStart + 1)
                        Exit Function
                    End If
            End Select
        End If
        i = i + 1
    Loop
End Function

' Split a JSON array "[{...},{...},...]" into an array of item strings.
' Each element in the returned array is one complete JSON object string.
' Returns an empty array (UBound = -1) if the array is empty or not found.
'
' Usage:
'   Dim aItems() As String
'   Dim i As Long
'   aItems = JsonSplit(sJsonArray)
'   For i = 0 To UBound(aItems)
'       Debug.Print JsonStr(aItems(i), "studentName")
'   Next i
Public Function JsonSplit(sJsonArray As String) As String()
    Dim aEmpty()  As String
    Dim nBracket  As Long
    Dim nEnd      As Long
    Dim nDepth    As Long
    Dim i         As Long
    Dim sChar     As String
    Dim bInStr    As Boolean
    Dim sContent  As String
    Dim aItems()  As String
    Dim nCount    As Long
    Dim nItemStart As Long
    Dim j         As Long
    Dim aResult() As String

    ReDim aEmpty(-1 To -1)   ' empty array — UBound = -1 signals "nothing"

    ' Find the opening bracket
    nBracket = InStr(sJsonArray, "[")
    If nBracket = 0 Then
        JsonSplit = aEmpty
        Exit Function
    End If

    ' Find matching closing bracket
    nDepth = 0
    bInStr = False
    nEnd   = 0
    i      = nBracket

    Do While i <= Len(sJsonArray)
        sChar = Mid(sJsonArray, i, 1)
        If bInStr Then
            If sChar = "\" Then i = i + 1
            If i <= Len(sJsonArray) Then
                If Mid(sJsonArray, i, 1) = """" Then bInStr = False
            End If
        Else
            Select Case sChar
                Case """":        bInStr = True
                Case "[", "{":    nDepth = nDepth + 1
                Case "]", "}":
                    nDepth = nDepth - 1
                    If nDepth = 0 Then nEnd = i : i = Len(sJsonArray) + 1
            End Select
        End If
        i = i + 1
    Loop

    If nEnd = 0 Then
        JsonSplit = aEmpty
        Exit Function
    End If

    sContent = Trim(Mid(sJsonArray, nBracket + 1, nEnd - nBracket - 1))
    If Len(sContent) = 0 Then
        JsonSplit = aEmpty
        Exit Function
    End If

    ' Walk the content, splitting at top-level object boundaries
    ReDim aItems(200)
    nCount     = 0
    nDepth     = 0
    bInStr     = False
    nItemStart = 1
    i          = 1

    Do While i <= Len(sContent)
        sChar = Mid(sContent, i, 1)
        If bInStr Then
            If sChar = "\" Then i = i + 1
            If i <= Len(sContent) Then
                If Mid(sContent, i, 1) = """" Then bInStr = False
            End If
        Else
            Select Case sChar
                Case """":        bInStr = True
                Case "{", "[":    nDepth = nDepth + 1
                Case "}", "]":
                    nDepth = nDepth - 1
                    If nDepth = 0 Then
                        aItems(nCount) = Trim(Mid(sContent, nItemStart, i - nItemStart + 1))
                        nCount = nCount + 1
                        ' Skip the comma and any whitespace before next item
                        Do While i < Len(sContent)
                            Dim sNext As String
                            sNext = Mid(sContent, i + 1, 1)
                            If sNext = "," Or sNext = " " Or _
                               sNext = Chr(13) Or sNext = Chr(10) Then
                                i = i + 1
                            Else
                                Exit Do
                            End If
                        Loop
                        nItemStart = i + 1
                    End If
            End Select
        End If
        i = i + 1
    Loop

    If nCount = 0 Then
        JsonSplit = aEmpty
        Exit Function
    End If

    ReDim aResult(nCount - 1)
    For j = 0 To nCount - 1
        aResult(j) = aItems(j)
    Next j

    JsonSplit = aResult
End Function

' Build a JSON object string from paired key–value arguments.
' String values are automatically quoted and escaped.
' Numeric/Boolean/Null values are written unquoted.
'
' Usage:
'   Dim sJson As String
'   sJson = BuildObj("AdmissionNo", "2025001", _
'                    "StudentName", "Ravi Kumar", _
'                    "AcademicYearId", 3, _
'                    "ClassId", 5)
'   ' → {"AdmissionNo":"2025001","StudentName":"Ravi Kumar","AcademicYearId":3,"ClassId":5}
Public Function BuildObj(ParamArray kv() As Variant) As String
    Dim sResult As String
    Dim i       As Long
    Dim sKey    As String
    Dim vVal    As Variant
    Dim sVal    As String
    Dim bFirst  As Boolean

    sResult = "{"
    bFirst  = True
    i       = LBound(kv)

    Do While i <= UBound(kv) - 1
        sKey = CStr(kv(i))
        vVal = kv(i + 1)

        If Not bFirst Then sResult = sResult & ","
        bFirst = False

        sResult = sResult & """" & sKey & """:"

        Select Case VarType(vVal)
            Case vbString
                sResult = sResult & """" & EscStr(CStr(vVal)) & """"
            Case vbBoolean
                If CBool(vVal) Then
                    sResult = sResult & "true"
                Else
                    sResult = sResult & "false"
                End If
            Case vbNull, vbEmpty
                sResult = sResult & "null"
            Case Else
                ' Numeric types — write as-is
                sResult = sResult & CStr(vVal)
        End Select

        i = i + 2
    Loop

    sResult = sResult & "}"
    BuildObj = sResult
End Function

' Escape a string for safe inclusion in JSON.
Private Function EscStr(s As String) As String
    Dim sR As String
    sR = s
    sR = Replace(sR, "\",  "\\")
    sR = Replace(sR, """", "\""")
    sR = Replace(sR, Chr(13), "\r")
    sR = Replace(sR, Chr(10), "\n")
    sR = Replace(sR, Chr(9),  "\t")
    EscStr = sR
End Function

'==============================================================================
' SECTION 3 — CONNECTION TEST
'==============================================================================

' Returns True if the API key is valid and the server is reachable.
' Call this when the VB app starts to detect config problems early.
Public Function TestConnection() As Boolean
    Dim sResp As String
    sResp = ApiGet("school/master/academic-years")
    TestConnection = JsonSuccess(sResp)
End Function

'==============================================================================
' SECTION 4 — MASTER DATA
' Call these once on startup and cache the results in global variables or
' a module-level Collection/Dictionary so you don't repeat the network call.
'==============================================================================

' Returns JSON array of academic years.
' Fields per item: academicYearId, yearName, startDate, endDate, isCurrent
Public Function GetAcademicYears() As String
    GetAcademicYears = JsonData(ApiGet("school/master/academic-years"))
End Function

' Returns JSON array of all classes.
' Fields per item: classId, className, classGroupId, classGroupName
Public Function GetClasses() As String
    GetClasses = JsonData(ApiGet("school/master/classes"))
End Function

' Returns JSON array of sections for one class.
' Fields per item: sectionId, sectionName, classId
Public Function GetSections(nClassId As Long) As String
    GetSections = JsonData(ApiGet("school/master/sections?classId=" & nClassId))
End Function

' Returns JSON array of fee types.
' Fields per item: feeTypeId, feeTypeName, description
Public Function GetFeeTypes() As String
    GetFeeTypes = JsonData(ApiGet("school/master/fee-types"))
End Function

' Returns JSON array of terms.
' Fields per item: termId, termName, academicYearId
Public Function GetTerms() As String
    GetTerms = JsonData(ApiGet("school/master/terms"))
End Function

' Returns JSON array of payment modes.
' Fields per item: paymentModeId, modeName, isOnline
Public Function GetPaymentModes() As String
    GetPaymentModes = JsonData(ApiGet("school/master/payment-modes"))
End Function

' Returns JSON array of fee categories.
' Fields per item: feeCategoryId, categoryName
Public Function GetFeeCategories() As String
    GetFeeCategories = JsonData(ApiGet("school/master/fee-categories"))
End Function

'==============================================================================
' SECTION 5 — STUDENTS
'==============================================================================

' Get a list of students. All parameters are optional (pass 0 or "" to skip).
' sStatus: "Active", "Inactive", "Blocked" — pass "" for all
' Returns JSON array. Fields per item:
'   studentId, studentUniqueId, admissionNo, studentName, className,
'   sectionName, fatherName, fatherMobile, status, dateOfBirth, gender
Public Function GetStudents(Optional nAcademicYearId As Long = 0, _
                            Optional nClassId As Long = 0, _
                            Optional nSectionId As Long = 0, _
                            Optional sSearch As String = "", _
                            Optional sStatus As String = "") As String
    Dim sQuery As String
    sQuery = "school/students?"

    If nAcademicYearId > 0 Then sQuery = sQuery & "academicYearId=" & nAcademicYearId & "&"
    If nClassId > 0        Then sQuery = sQuery & "classId="        & nClassId        & "&"
    If nSectionId > 0      Then sQuery = sQuery & "sectionId="      & nSectionId      & "&"
    If Len(sSearch) > 0    Then sQuery = sQuery & "search="         & sSearch         & "&"
    If Len(sStatus) > 0    Then sQuery = sQuery & "status="         & sStatus         & "&"

    ' Trim trailing ? or &
    If Right(sQuery, 1) = "&" Or Right(sQuery, 1) = "?" Then
        sQuery = Left(sQuery, Len(sQuery) - 1)
    End If

    GetStudents = JsonData(ApiGet(sQuery))
End Function

' Get full details of one student by studentId.
' Returns a single JSON object with all student fields.
Public Function GetStudent(nStudentId As Long) As String
    GetStudent = JsonData(ApiGet("school/students/" & nStudentId))
End Function

' Create a new student.
' sJsonBody: build with BuildObj() — see examples in AscentAPI_Examples.bas
' Returns the new studentId on success (0 on failure).
' sError is set to the server message on failure.
'
' Required fields: AdmissionNo, StudentName, AcademicYearId, ClassId, SectionId, FeeCategoryId
' Optional fields: DateOfBirth, Gender, FatherName, FatherMobile, MotherName,
'                  MotherMobile, Address, BloodGroup, Religion, Caste, AadharNo
Public Function CreateStudent(sJsonBody As String, sError As String) As Long
    Dim sResp As String
    sResp  = ApiPost("school/students", sJsonBody)
    sError = ""

    If Not JsonSuccess(sResp) Then
        sError = JsonMessage(sResp)
        CreateStudent = 0
        Exit Function
    End If

    CreateStudent = CLng(JsonNum(JsonData(sResp), "studentId"))
End Function

' Update an existing student.
' sJsonBody: build with BuildObj() — only include fields you want to change.
' Returns True on success. sError is set on failure.
Public Function UpdateStudent(nStudentId As Long, sJsonBody As String, _
                              sError As String) As Boolean
    Dim sResp As String
    sResp  = ApiPut("school/students/" & nStudentId, sJsonBody)
    sError = ""

    If Not JsonSuccess(sResp) Then
        sError = JsonMessage(sResp)
        UpdateStudent = False
    Else
        UpdateStudent = True
    End If
End Function

'==============================================================================
' SECTION 6 — FEES
'==============================================================================

' Get fee summary + line items for a student (current-year student_id).
' Returns a JSON object with:
'   totalAmount, paidAmount, outstandingAmount,
'   lineItems: [{feeTypeId, feeTypeName, termName, amount,
'                paidAmount, concessionAmount, outstanding, isPaid}]
Public Function GetStudentFees(nStudentId As Long) As String
    GetStudentFees = JsonData(ApiGet("school/fees/student/" & nStudentId))
End Function

' Collect fee for a student (offline — cash/cheque/bank transfer).
' nPaymentModeId : from GetPaymentModes() — use the id for Cash/Cheque/etc.
' sItemsJson     : JSON array of fee line items — build with BuildFeeItems()
' sRemarks       : optional note (e.g. cheque number)
' Returns receipt JSON object {receiptId, receiptNo, totalAmount} or "" on failure.
' sError is set to the server message on failure.
Public Function CollectFee(sError As String, _
                           nStudentId As Long, _
                           nAcademicYearId As Long, _
                           nPaymentModeId As Long, _
                           sItemsJson As String, _
                           Optional sRemarks As String = "") As String
    Dim sBody As String
    Dim sResp As String

    sBody = "{" & _
        """StudentId"":" & nStudentId & "," & _
        """AcademicYearId"":" & nAcademicYearId & "," & _
        """PaymentModeId"":" & nPaymentModeId & "," & _
        """Remarks"":""" & EscStr(sRemarks) & """," & _
        """Items"":" & sItemsJson & _
        "}"

    sResp  = ApiPost("school/fees/collect", sBody)
    sError = ""

    If Not JsonSuccess(sResp) Then
        sError = JsonMessage(sResp)
        CollectFee = ""
    Else
        CollectFee = JsonData(sResp)
    End If
End Function

' Helper — build the fee items JSON array for CollectFee.
' Call once per fee line item, accumulate the results, then join them.
'
' Usage:
'   Dim sItems As String
'   sItems = "[" & _
'       BuildFeeItem(2, 1, 0, 0, 5000, 0) & "," & _   ' feeTypeId=2, termId=1
'       BuildFeeItem(3, 1, 0, 0, 2000, 0) & _           ' feeTypeId=3, termId=1
'       "]"
Public Function BuildFeeItem(nFeeTypeId As Long, _
                             nTermId As Long, _
                             nFeePeriodId As Long, _
                             nBusRouteId As Long, _
                             dAmount As Double, _
                             dConcession As Double) As String
    Dim sTermId     As String
    Dim sPeriodId   As String
    Dim sBusRouteId As String

    If nTermId > 0      Then sTermId     = CStr(nTermId)     Else sTermId     = "null"
    If nFeePeriodId > 0 Then sPeriodId   = CStr(nFeePeriodId) Else sPeriodId   = "null"
    If nBusRouteId > 0  Then sBusRouteId = CStr(nBusRouteId)  Else sBusRouteId = "null"

    BuildFeeItem = "{" & _
        """FeeTypeId"":" & nFeeTypeId & "," & _
        """TermId"":"    & sTermId    & "," & _
        """FeePeriodId"":" & sPeriodId & "," & _
        """BusRouteId"":" & sBusRouteId & "," & _
        """Amount"":"    & CStr(dAmount)     & "," & _
        """ConcessionAmount"":" & CStr(dConcession) & _
        "}"
End Function

' Get list of receipts. Pass "" to skip date filters.
' sDateFrom / sDateTo: "YYYY-MM-DD"
' Returns JSON array. Fields per item:
'   receiptId, receiptNo, studentName, admissionNo,
'   totalAmount, paymentMode, createdAt, status
Public Function GetReceipts(Optional sDateFrom As String = "", _
                            Optional sDateTo As String = "", _
                            Optional sSearch As String = "") As String
    Dim sQuery As String
    sQuery = "school/fees/receipts?"

    If Len(sDateFrom) > 0 Then sQuery = sQuery & "dateFrom=" & sDateFrom & "&"
    If Len(sDateTo)   > 0 Then sQuery = sQuery & "dateTo="   & sDateTo   & "&"
    If Len(sSearch)   > 0 Then sQuery = sQuery & "search="   & sSearch   & "&"

    If Right(sQuery, 1) = "&" Or Right(sQuery, 1) = "?" Then
        sQuery = Left(sQuery, Len(sQuery) - 1)
    End If

    GetReceipts = JsonData(ApiGet(sQuery))
End Function

' Get full detail of one receipt including all line items.
' Returns a JSON object with header + items[].
Public Function GetReceipt(nReceiptId As Long) As String
    GetReceipt = JsonData(ApiGet("school/fees/receipts/" & nReceiptId))
End Function

'==============================================================================
' SECTION 7 — ATTENDANCE
'==============================================================================

' Get the attendance grid for a class on a specific date.
' sDate format: "YYYY-MM-DD"
' Returns JSON array. Fields per item:
'   studentId, studentName, admissionNo, status (P/A/Late/HalfDay/OnLeave)
Public Function GetAttendance(nClassId As Long, nSectionId As Long, _
                              sDate As String) As String
    GetAttendance = JsonData(ApiGet( _
        "school/attendance?classId=" & nClassId & _
        "&sectionId=" & nSectionId & _
        "&date=" & sDate))
End Function

'==============================================================================
' SECTION 8 — BULK STUDENT IMPORT
' Use this to push multiple students in one call (max 500 per batch).
'==============================================================================

' Bulk create students from a JSON array string.
' Build the array by calling BuildObj() for each student and joining with commas.
' Returns a result JSON object:
'   {total, imported, failed, errors:[{row, admissionNo, reason}]}
'
' Usage: see AscentAPI_Examples.bas → BulkImportStudentsExample()
Public Function BulkImportStudents(sJsonArray As String, sError As String) As String
    Dim sBody As String
    Dim sResp As String

    sBody = "{""Rows"":" & sJsonArray & "}"
    sResp  = ApiPost("school/students/bulk", sBody)
    sError = ""

    If Not JsonSuccess(sResp) Then
        sError = JsonMessage(sResp)
        BulkImportStudents = ""
    Else
        BulkImportStudents = JsonData(sResp)
    End If
End Function

'==============================================================================
' SECTION 9 — LEGACY RECEIPT IMPORT
' Use this to push historical receipts in bulk (max 1000 rows per call).
'==============================================================================

' Bulk import legacy fee receipts.
' sJsonArray: array of receipt row objects.
' Each row: {LegacyReceiptNo, AdmissionNo, AcademicYear, FeeTypeName, TermName,
'            Amount, ConcessionAmount, PaymentDate, PaymentMode, Remarks}
' Rows with the same LegacyReceiptNo are grouped into one receipt.
' Returns result JSON: {total, imported, failed, errors:[...]}
Public Function BulkImportReceipts(sJsonArray As String, sError As String) As String
    Dim sBody As String
    Dim sResp As String

    sBody = "{""Rows"":" & sJsonArray & "}"
    sResp  = ApiPost("school/fees/receipts/bulk", sBody)
    sError = ""

    If Not JsonSuccess(sResp) Then
        sError = JsonMessage(sResp)
        BulkImportReceipts = ""
    Else
        BulkImportReceipts = JsonData(sResp)
    End If
End Function

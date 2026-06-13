Attribute VB_Name = "AscentAPI_Examples"
'==============================================================================
' AscentAPI_Examples.bas  —  Practical VB6 usage examples
'==============================================================================
' Add this file to the same VB6 project as AscentAPI.bas.
' These are self-contained Sub/Function examples — copy the relevant block into
' your form or module and wire it to a button or startup event.
'
' BEFORE RUNNING
'   1. AscentAPI.bas must be added to the project and API_KEY must be set.
'   2. Replace the hard-coded IDs (academic year, class, section etc.) with the
'      real IDs from your database — use the master data examples first to print
'      them to the Immediate Window.
'==============================================================================
Option Explicit

'==============================================================================
' EXAMPLE 1 — STARTUP / CONNECTION TEST
' Call this from Form_Load. Populates module-level globals used by later examples.
'==============================================================================

' Module-level cache — loaded once at startup, shared by all examples below.
Private g_nAcademicYearId As Long   ' current academic year id
Private g_nCashModeId     As Long   ' payment_mode_id for Cash
Private g_nChequeModeId   As Long   ' payment_mode_id for Cheque

Public Sub AppStartup()
    '--- 1. Verify connection ---------------------------------------------------
    If Not TestConnection() Then
        MsgBox "Cannot reach the Ascent API. Check your network and API key.", _
               vbCritical, "Ascent Integration"
        End
    End If

    '--- 2. Load master data into module globals --------------------------------
    LoadAcademicYear
    LoadPaymentModes

    MsgBox "Connected. Academic year " & g_nAcademicYearId & " loaded.", _
           vbInformation, "Ascent Integration"
End Sub

Private Sub LoadAcademicYear()
    Dim aYears() As String
    Dim i        As Long
    aYears = JsonSplit(GetAcademicYears())
    For i = 0 To UBound(aYears)
        If JsonBool(aYears(i), "isCurrent") Then
            g_nAcademicYearId = CLng(JsonNum(aYears(i), "academicYearId"))
            Exit For
        End If
    Next i
End Sub

Private Sub LoadPaymentModes()
    Dim aModes() As String
    Dim i        As Long
    Dim sName    As String
    aModes = JsonSplit(GetPaymentModes())
    For i = 0 To UBound(aModes)
        sName = LCase(JsonStr(aModes(i), "modeName"))
        If InStr(sName, "cash") > 0 Then g_nCashModeId = CLng(JsonNum(aModes(i), "paymentModeId"))
        If InStr(sName, "cheque") > 0 Then g_nChequeModeId = CLng(JsonNum(aModes(i), "paymentModeId"))
    Next i
End Sub

'==============================================================================
' EXAMPLE 2 — PRINT ALL MASTER DATA TO IMMEDIATE WINDOW
' Run from a "Debug master data" button to discover the IDs you need.
'==============================================================================

Public Sub PrintMasterData()
    Dim aItems() As String
    Dim i        As Long

    Debug.Print "=== ACADEMIC YEARS ==="
    aItems = JsonSplit(GetAcademicYears())
    For i = 0 To UBound(aItems)
        Debug.Print "  id=" & JsonNum(aItems(i), "academicYearId") & _
                    "  name=" & JsonStr(aItems(i), "yearName") & _
                    "  current=" & JsonBool(aItems(i), "isCurrent")
    Next i

    Debug.Print "=== CLASSES ==="
    aItems = JsonSplit(GetClasses())
    For i = 0 To UBound(aItems)
        Debug.Print "  id=" & JsonNum(aItems(i), "classId") & _
                    "  name=" & JsonStr(aItems(i), "className") & _
                    "  group=" & JsonStr(aItems(i), "classGroupName")
    Next i

    Debug.Print "=== FEE TYPES ==="
    aItems = JsonSplit(GetFeeTypes())
    For i = 0 To UBound(aItems)
        Debug.Print "  id=" & JsonNum(aItems(i), "feeTypeId") & _
                    "  name=" & JsonStr(aItems(i), "feeTypeName") & _
                    "  desc=" & JsonStr(aItems(i), "description")
    Next i

    Debug.Print "=== TERMS ==="
    aItems = JsonSplit(GetTerms())
    For i = 0 To UBound(aItems)
        Debug.Print "  id=" & JsonNum(aItems(i), "termId") & _
                    "  name=" & JsonStr(aItems(i), "termName") & _
                    "  year=" & JsonNum(aItems(i), "academicYearId")
    Next i

    Debug.Print "=== PAYMENT MODES ==="
    aItems = JsonSplit(GetPaymentModes())
    For i = 0 To UBound(aItems)
        Debug.Print "  id=" & JsonNum(aItems(i), "paymentModeId") & _
                    "  name=" & JsonStr(aItems(i), "modeName") & _
                    "  online=" & JsonBool(aItems(i), "isOnline")
    Next i

    Debug.Print "=== FEE CATEGORIES ==="
    aItems = JsonSplit(GetFeeCategories())
    For i = 0 To UBound(aItems)
        Debug.Print "  id=" & JsonNum(aItems(i), "feeCategoryId") & _
                    "  name=" & JsonStr(aItems(i), "categoryName")
    Next i

    Debug.Print "Done."
End Sub

'==============================================================================
' EXAMPLE 3 — LIST STUDENTS IN A CLASS
' Wire to a "Load Students" button; fill lstStudents listbox.
'==============================================================================

Public Sub LoadStudentsIntoListbox(lstStudents As ListBox, _
                                   nClassId As Long, _
                                   nSectionId As Long)
    Dim sData    As String
    Dim aStudents() As String
    Dim i        As Long

    lstStudents.Clear

    sData = GetStudents(nAcademicYearId:=g_nAcademicYearId, _
                        nClassId:=nClassId, _
                        nSectionId:=nSectionId, _
                        sStatus:="Active")

    If Len(sData) = 0 Or sData = "null" Then
        MsgBox "No students found.", vbInformation
        Exit Sub
    End If

    aStudents = JsonSplit(sData)
    For i = 0 To UBound(aStudents)
        ' ListBox shows "AdmNo - Name"; ItemData stores studentId
        Dim sLine As String
        sLine = JsonStr(aStudents(i), "admissionNo") & " - " & _
                JsonStr(aStudents(i), "studentName")
        lstStudents.AddItem sLine
        lstStudents.ItemData(lstStudents.NewIndex) = _
            CLng(JsonNum(aStudents(i), "studentId"))
    Next i
End Sub

'==============================================================================
' EXAMPLE 4 — SYNC ONE STUDENT FROM YOUR LOCAL DB → API
' Assumes you have opened a local Recordset (rs) with one student row.
' Tries UpdateStudent first; if student doesn't exist yet, calls CreateStudent.
'==============================================================================

Public Sub SyncStudentFromLocalDB(rs As Object)
    ' rs is an ADO or DAO Recordset with columns matching field names below.
    Dim sBody  As String
    Dim sError As String
    Dim nNewId As Long
    Dim nExistingStudentId As Long

    ' Build the common fields JSON body
    sBody = BuildObj( _
        "AdmissionNo", CStr(rs.Fields("AdmissionNo").Value), _
        "StudentName", CStr(rs.Fields("StudentName").Value), _
        "AcademicYearId", g_nAcademicYearId, _
        "ClassId", CLng(rs.Fields("ClassId").Value), _
        "SectionId", CLng(rs.Fields("SectionId").Value), _
        "FeeCategoryId", CLng(rs.Fields("FeeCategoryId").Value), _
        "DateOfBirth", CStr(rs.Fields("DateOfBirth").Value), _
        "Gender", CStr(rs.Fields("Gender").Value), _
        "FatherName", CStr(rs.Fields("FatherName").Value), _
        "FatherMobile", CStr(rs.Fields("FatherMobile").Value), _
        "MotherName", CStr(rs.Fields("MotherName").Value), _
        "MotherMobile", CStr(rs.Fields("MotherMobile").Value), _
        "Address", CStr(rs.Fields("Address").Value), _
        "BloodGroup", CStr(rs.Fields("BloodGroup").Value), _
        "Religion", CStr(rs.Fields("Religion").Value), _
        "Caste", CStr(rs.Fields("Caste").Value), _
        "AadharNo", CStr(rs.Fields("AadharNo").Value) _
    )

    nExistingStudentId = CLng(rs.Fields("ApiStudentId").Value)  ' your local mapped id column

    If nExistingStudentId > 0 Then
        ' Student was previously synced — update
        If UpdateStudent(nExistingStudentId, sBody, sError) Then
            Debug.Print "Updated student " & nExistingStudentId
        Else
            Debug.Print "Update failed: " & sError
        End If
    Else
        ' First sync — create
        nNewId = CreateStudent(sBody, sError)
        If nNewId > 0 Then
            ' Write the new API student_id back to your local DB so next sync can update
            rs.Fields("ApiStudentId").Value = nNewId
            rs.Update
            Debug.Print "Created student → api id " & nNewId
        Else
            Debug.Print "Create failed: " & sError
        End If
    End If
End Sub

'==============================================================================
' EXAMPLE 5 — DISPLAY FEE OUTSTANDING IN A LISTVIEW / GRID
' Shows how to parse the fee summary and populate a UI control.
'==============================================================================

Public Sub ShowStudentFees(nStudentId As Long, lvFees As Object)
    ' lvFees: a ListView with columns:
    '   Fee Type | Term | Amount | Paid | Concession | Outstanding | Status
    Dim sFeeData  As String
    Dim sItems    As String
    Dim aItems()  As String
    Dim i         As Long
    Dim oItem     As Object

    sFeeData = GetStudentFees(nStudentId)

    If Len(sFeeData) = 0 Then
        MsgBox "Student not found or no fee data.", vbExclamation
        Exit Sub
    End If

    Debug.Print "Total outstanding: " & JsonNum(sFeeData, "outstandingAmount")

    ' Extract the lineItems array from the fee summary object
    sItems = JsonData("{""data"":" & sFeeData & "}")     ' wrap so JsonData can strip
    ' Actually lineItems is nested — extract it differently:
    sItems = ExtractLineItems(sFeeData)

    lvFees.ListItems.Clear

    If Len(sItems) = 0 Then Exit Sub

    aItems = JsonSplit(sItems)
    For i = 0 To UBound(aItems)
        Set oItem = lvFees.ListItems.Add(, , JsonStr(aItems(i), "feeTypeName"))
        oItem.SubItems(1) = JsonStr(aItems(i), "termName")
        oItem.SubItems(2) = JsonNum(aItems(i), "amount")
        oItem.SubItems(3) = JsonNum(aItems(i), "paidAmount")
        oItem.SubItems(4) = JsonNum(aItems(i), "concessionAmount")
        oItem.SubItems(5) = JsonNum(aItems(i), "outstanding")
        oItem.SubItems(6) = IIf(JsonBool(aItems(i), "isPaid"), "Paid", "Due")
    Next i
End Sub

' Helper: extract the lineItems array from a fee summary JSON object.
Private Function ExtractLineItems(sJson As String) As String
    Dim sPattern As String
    Dim nPos     As Long
    sPattern = """lineItems"":"
    nPos = InStr(sJson, sPattern)
    If nPos = 0 Then Exit Function
    ExtractLineItems = JsonData("{""data"":" & Mid(sJson, nPos + Len(sPattern)) & "}")
End Function

'==============================================================================
' EXAMPLE 6 — COLLECT CASH FEE PAYMENT (single term, single fee type)
'==============================================================================

' Wire to a "Collect Cash" button on your fee collection form.
' Replace the IDs with real values from PrintMasterData().
'
' nStudentId     : from student record
' nFeeTypeId     : e.g. 2 = "Tuition Fee"
' nTermId        : e.g. 1 = "Term 1"
' dAmount        : amount the student is paying now
' dConcession    : concession already recorded (0 if none)
'
Public Sub CollectCashPaymentExample(nStudentId As Long, _
                                     nFeeTypeId As Long, _
                                     nTermId As Long, _
                                     dAmount As Double, _
                                     dConcession As Double)
    Dim sItems   As String
    Dim sReceipt As String
    Dim sError   As String

    ' Build the items array (just one item here; add more with & "," & BuildFeeItem(...)
    sItems = "[" & BuildFeeItem(nFeeTypeId, nTermId, 0, 0, dAmount, dConcession) & "]"

    sReceipt = CollectFee(sError, nStudentId, g_nAcademicYearId, _
                          g_nCashModeId, sItems, "Cash payment")

    If Len(sError) > 0 Then
        MsgBox "Fee collection failed: " & sError, vbCritical
    Else
        MsgBox "Receipt created: " & JsonStr(sReceipt, "receiptNo") & Chr(13) & _
               "Total: Rs." & JsonNum(sReceipt, "totalAmount"), vbInformation
    End If
End Sub

'==============================================================================
' EXAMPLE 7 — COLLECT CHEQUE FEE PAYMENT (multiple terms in one receipt)
'==============================================================================

Public Sub CollectChequeMultiTermExample(nStudentId As Long)
    Dim sItems   As String
    Dim sReceipt As String
    Dim sError   As String
    Dim sChequeNo As String

    sChequeNo = InputBox("Enter cheque number:", "Cheque Payment")
    If Len(sChequeNo) = 0 Then Exit Sub

    ' Two line items: Term 1 Tuition + Term 1 Annual Fee
    ' Replace IDs with real values from your DB.
    sItems = "[" & _
        BuildFeeItem(2, 1, 0, 0, 5000, 0) & "," & _
        BuildFeeItem(4, 1, 0, 0, 1200, 200) & _
        "]"
        
' Tuition, Term 1, Rs.5000, no concession
' Annual, Term 1, Rs.1200 - Rs.200 concession


    sReceipt = CollectFee(sError, nStudentId, g_nAcademicYearId, _
                          g_nChequeModeId, sItems, _
                          "Chq No: " & sChequeNo)

    If Len(sError) > 0 Then
        MsgBox "Collection failed: " & sError, vbCritical
    Else
        MsgBox "Receipt: " & JsonStr(sReceipt, "receiptNo") & Chr(13) & _
               "Total: Rs." & JsonNum(sReceipt, "totalAmount"), vbInformation
    End If
End Sub

'==============================================================================
' EXAMPLE 8 — BULK IMPORT STUDENTS FROM LOCAL ACCESS DB TABLE
'==============================================================================
' Assumes a table "Students" in the current database with columns matching VB field names.
' Run from a "Push All Students to Web" button. Processes in batches of 100.
'
' Usage:
'   BulkImportStudentsExample CurrentDb.OpenRecordset("SELECT * FROM Students WHERE Synced=0")
'
Public Sub BulkImportStudentsExample(rs As Object, _
                                     nClassId As Long, _
                                     nSectionId As Long, _
                                     nFeeCategoryId As Long)
    Const BATCH_SIZE As Integer = 100

    Dim nTotal    As Long
    Dim nImported As Long
    Dim nFailed   As Long
    Dim sBatch    As String
    Dim nCount    As Integer
    Dim sRow      As String
    Dim sResult   As String
    Dim sError    As String
    Dim bFirst    As Boolean

    nTotal = 0
    nImported = 0
    nFailed = 0
    nCount = 0
    bFirst = True
    sBatch = "["

    Do While Not rs.EOF
        sRow = BuildObj( _
            "AdmissionNo", CStr(rs.Fields("AdmNo").Value), _
            "StudentName", CStr(rs.Fields("StuName").Value), _
            "AcademicYearId", g_nAcademicYearId, _
            "ClassId", nClassId, _
            "SectionId", nSectionId, _
            "FeeCategoryId", nFeeCategoryId, _
            "DateOfBirth", CStr(rs.Fields("DOB").Value), _
            "Gender", CStr(rs.Fields("Gender").Value), _
            "FatherName", CStr(rs.Fields("FatherName").Value), _
            "FatherMobile", CStr(rs.Fields("FatherMobile").Value), _
            "MotherName", CStr(rs.Fields("MotherName").Value), _
            "Address", CStr(rs.Fields("Address").Value) _
        )

        If Not bFirst Then sBatch = sBatch & ","
        bFirst = False
        sBatch = sBatch & sRow

        nCount = nCount + 1
        nTotal = nTotal + 1

        rs.MoveNext

        ' Flush when batch is full or at end of recordset
        If nCount >= BATCH_SIZE Or rs.EOF Then
            sBatch = sBatch & "]"
            sResult = BulkImportStudents(sBatch, sError)

            If Len(sError) > 0 Then
                MsgBox "Batch error: " & sError, vbExclamation
            Else
                nImported = nImported + CLng(JsonNum(sResult, "imported"))
                nFailed = nFailed + CLng(JsonNum(sResult, "failed"))

                ' Print per-row errors to Immediate Window
                Dim aErrors() As String
                aErrors = JsonSplit(JsonData("{""data"":" & sResult & "}"))
                If UBound(aErrors) >= 0 Then
                    Dim e As Long
                    For e = 0 To UBound(aErrors)
                        Debug.Print "Row " & JsonNum(aErrors(e), "row") & _
                                    " (" & JsonStr(aErrors(e), "admissionNo") & "): " & _
                                    JsonStr(aErrors(e), "reason")
                    Next e
                End If
            End If

            ' Reset for next batch
            sBatch = "["
            nCount = 0
            bFirst = True
        End If
    Loop

    rs.Close
    MsgBox "Sync complete." & Chr(13) & _
           "Total: " & nTotal & Chr(13) & _
           "Imported: " & nImported & Chr(13) & _
           "Failed: " & nFailed, vbInformation, "Bulk Import Result"
End Sub

'==============================================================================
' EXAMPLE 9 — BULK IMPORT HISTORICAL LEGACY RECEIPTS (one-off migration)
' Reads historical fee payments from SAS_FeeReceipts and pushes to the API.
' LegacyReceiptNo (= FeeReciptID) is used as receipt_no directly.
' Re-running is safe — already-imported receipts are skipped automatically.
'==============================================================================
'
' rs assumed: SELECT * FROM SAS_FeeReceipts ORDER BY FeeReciptID, Id_new
' Process in batches of 200 rows (not 200 receipts — each receipt may have
' multiple rows, one per fee type/term line item).
'
Public Sub BulkImportReceiptsExample(rs As Object)
    Const BATCH_SIZE As Integer = 200

    Dim nTotal    As Long
    Dim nImported As Long
    Dim nSkipped  As Long
    Dim nFailed   As Long
    Dim sBatch    As String
    Dim nCount    As Integer
    Dim sRow      As String
    Dim sResult   As String
    Dim sError    As String
    Dim bFirst    As Boolean
    Dim sDate     As String
    Dim sStat     As String

    nTotal = 0
    nImported = 0
    nSkipped = 0
    nFailed = 0
    nCount = 0
    bFirst = True
    sBatch = "["

    Do While Not rs.EOF
        sDate = Format(rs.Fields("FeeDat").Value, "YYYY-MM-DD")
        sStat = CStr(rs.Fields("FeeReceiptStat").Value)   ' "A" or "D"

        sRow = BuildReceiptRow( _
            CStr(rs.Fields("FeeReciptID").Value), _
            CStr(rs.Fields("StuAdmnNo").Value), _
            Trim(CStr(rs.Fields("AcdYear").Value)), _
            CStr(rs.Fields("FeeTyp").Value), _
            CStr(rs.Fields("PaymentTyp").Value), _
            CDbl(rs.Fields("FeeAmt").Value), _
            sDate, _
            Trim(CStr(rs.Fields("PymntTyp").Value)), _
            Trim(CStr(rs.Fields("RefNo").Value)), _
            Trim(CStr(rs.Fields("RemarksDet").Value)), _
            sStat, _
            Trim(CStr(rs.Fields("DeletResn").Value)) _
        )
        
        
'        sRow = BuildReceiptRow( _
'            CStr(rs.Fields("FeeReciptID").Value), _      ' LegacyReceiptNo → receipt_no
'            CStr(rs.Fields("StuAdmnNo").Value), _        ' AdmissionNo
'            Trim(CStr(rs.Fields("AcdYear").Value)), _    ' AcademicYear
'            CStr(rs.Fields("FeeTyp").Value), _           ' FeeTypeName
'            CStr(rs.Fields("PaymentTyp").Value), _       ' TermName
'            CDbl(rs.Fields("FeeAmt").Value), _           ' PaidAmount
'            sDate, _                                      ' PaymentDate
'            Trim(CStr(rs.Fields("PymntTyp").Value)), _   ' PaymentMode
'            Trim(CStr(rs.Fields("RefNo").Value)), _      ' ChequeNo
'            Trim(CStr(rs.Fields("RemarksDet").Value)), _ ' Remarks
'            sStat, _                                      ' Status: A/D
'            Trim(CStr(rs.Fields("DeletResn").Value)) _   ' CancelReason (when D)
'        )



        If Not bFirst Then sBatch = sBatch & ","
        bFirst = False
        sBatch = sBatch & sRow

        nCount = nCount + 1
        nTotal = nTotal + 1

        rs.MoveNext

        If nCount >= BATCH_SIZE Or rs.EOF Then
            sBatch = sBatch & "]"
            sResult = BulkImportReceipts(sBatch, sError)

            If Len(sError) > 0 Then
                Debug.Print "Batch error: " & sError
            Else
                nImported = nImported + CLng(JsonNum(sResult, "imported"))
                nSkipped = nSkipped + CLng(JsonNum(sResult, "skipped"))
                nFailed = nFailed + CLng(JsonNum(sResult, "failed"))

                Dim aErrors() As String
                aErrors = JsonSplit(JsonData("{""data"":" & sResult & "}"))
                If UBound(aErrors) >= 0 Then
                    Dim e As Long
                    For e = 0 To UBound(aErrors)
                        Debug.Print "Row " & JsonNum(aErrors(e), "row") & ": " & _
                                    JsonStr(aErrors(e), "reason")
                    Next e
                End If
            End If

            sBatch = "["
            nCount = 0
            bFirst = True
        End If
    Loop

    rs.Close
    MsgBox "Import complete." & Chr(13) & _
           "Total rows: " & nTotal & Chr(13) & _
           "Receipts imported: " & nImported & Chr(13) & _
           "Skipped (already exist): " & nSkipped & Chr(13) & _
           "Failed: " & nFailed, vbInformation, "Receipt Import"
End Sub

'==============================================================================
' EXAMPLE 10 — END-OF-DAY RECEIPT PULL (sync new receipts to local DB)
' Pulls receipts created after a stored timestamp, loops through each,
' and writes them to a local Access/Jet table for the legacy reports system.
'==============================================================================
'
' sLastSync : ISO 8601 datetime string from your local "LastSyncAt" setting
'             e.g. "2025-06-01T08:00:00"
' db        : your Access CurrentDb (DAO.Database)
'
Public Sub PullNewReceiptsExample(sLastSync As String, db As Object)
    Dim sQuery  As String
    Dim sData   As String
    Dim aList() As String
    Dim i       As Long
    Dim sRec    As String
    Dim sDetail As String
    Dim nRecId  As Long
    Dim qdf     As Object
    Dim sql     As String

    ' source=webapp ensures we only pull receipts created in the web/parent portal
    ' — never returns receipts we already pushed from VB6 (no sync loop).
    sQuery = "school/fees/receipts?createdAfter=" & sLastSync & "&source=webapp"
    sData = JsonData(ApiGet(sQuery))

    If Len(sData) = 0 Or sData = "null" Then
        MsgBox "No new receipts since " & sLastSync, vbInformation
        Exit Sub
    End If

    aList = JsonSplit(sData)
    Debug.Print "Pulling " & (UBound(aList) + 1) & " new receipts..."

    For i = 0 To UBound(aList)
        nRecId = CLng(JsonNum(aList(i), "receiptId"))

        ' Get full detail with line items
        sDetail = GetReceipt(nRecId)
        If Len(sDetail) = 0 Then GoTo NextReceipt

        ' Insert header into local FeeReceipts table
        sql = "INSERT INTO FeeReceipts (ApiReceiptId, ReceiptNo, StudentName, " & _
              "AdmissionNo, TotalAmount, PaymentMode, CreatedAt, Status) VALUES (" & _
              nRecId & ", """ & JsonStr(sDetail, "receiptNo") & """, " & _
              """" & JsonStr(sDetail, "studentName") & """, " & _
              """" & JsonStr(sDetail, "admissionNo") & """, " & _
              JsonNum(sDetail, "totalAmount") & ", " & _
              """" & JsonStr(sDetail, "paymentMode") & """, " & _
              """" & JsonStr(sDetail, "createdAt") & """, " & _
              """" & JsonStr(sDetail, "status") & """)"
        db.Execute sql

        Debug.Print "  Receipt " & JsonStr(sDetail, "receiptNo") & _
                    " - Rs." & JsonNum(sDetail, "totalAmount")

NextReceipt:
    Next i

    ' Update your stored sync timestamp in the local settings table
    db.Execute "UPDATE AppSettings SET SettingValue = '" & _
               Format(Now, "YYYY-MM-DDTHH:MM:SS") & "' WHERE SettingKey = 'LastSyncAt'"

    MsgBox "Pulled " & (UBound(aList) + 1) & " receipts.", vbInformation
End Sub

'==============================================================================
' EXAMPLE 11 — ATTENDANCE: DISPLAY TODAY'S GRID IN A LISTVIEW
'==============================================================================

Public Sub ShowAttendanceExample(nClassId As Long, nSectionId As Long, _
                                  lvAttend As Object)
    Dim sDate  As String
    Dim sData  As String
    Dim aItems() As String
    Dim i      As Long
    Dim oItem  As Object

    sDate = Format(Now, "YYYY-MM-DD")
    sData = GetAttendance(nClassId, nSectionId, sDate)

    If Len(sData) = 0 Or sData = "null" Then
        MsgBox "No attendance data for " & sDate, vbInformation
        Exit Sub
    End If

    lvAttend.ListItems.Clear
    aItems = JsonSplit(sData)

    For i = 0 To UBound(aItems)
        Set oItem = lvAttend.ListItems.Add(, , JsonStr(aItems(i), "admissionNo"))
        oItem.SubItems(1) = JsonStr(aItems(i), "studentName")
        oItem.SubItems(2) = JsonStr(aItems(i), "status")   ' P / A / Late / HalfDay
    Next i

    Debug.Print "Loaded " & (UBound(aItems) + 1) & " students for " & sDate
End Sub

'==============================================================================
' EXAMPLE 12 — SECTIONS LOOKUP (populate a ComboBox from a class selection)
'==============================================================================

Public Sub FillSectionsCombo(cboSection As ComboBox, nClassId As Long)
    Dim sData    As String
    Dim aSecs()  As String
    Dim i        As Long

    cboSection.Clear
    sData = GetSections(nClassId)

    If Len(sData) = 0 Or sData = "null" Then Exit Sub

    aSecs = JsonSplit(sData)
    For i = 0 To UBound(aSecs)
        cboSection.AddItem JsonStr(aSecs(i), "sectionName")
        ' ItemData stores sectionId so you can pass it to GetStudents
        cboSection.ItemData(cboSection.NewIndex) = CLng(JsonNum(aSecs(i), "sectionId"))
    Next i

    If cboSection.ListCount > 0 Then cboSection.ListIndex = 0
End Sub

'==============================================================================
' EXAMPLE 14 — EOD SYNC: PUSH TODAY'S LEGACY RECEIPTS TO NEW APP
' Run at end-of-day from an "EOD Sync" button.
' Pushes receipts collected in the legacy system today to the new app DB.
' Re-running is safe — duplicates are skipped automatically.
'
' db       : DAO.Database (CurrentDb in Access, or open connection in VB6)
' sAcadYear: current academic year string e.g. "2024-25"
'==============================================================================

Public Sub EodPushReceiptsExample(db As Object, sAcadYear As String)
    Const BATCH_SIZE As Integer = 200

    Dim rs        As Object
    Dim sBatch    As String
    Dim sRow      As String
    Dim sResult   As String
    Dim sError    As String
    Dim nCount    As Integer
    Dim nTotal    As Long
    Dim nImported As Long
    Dim nSkipped  As Long
    Dim nFailed   As Long
    Dim bFirst    As Boolean
    Dim sDate     As String
    Dim sStat     As String
    Dim sSql      As String

    ' Pull today's receipts from legacy — all rows, ordered so line items group correctly
    sSql = "SELECT * FROM SAS_FeeReceipts " & _
           "WHERE Format(FeeDat, 'YYYY-MM-DD') = Format(Date(), 'YYYY-MM-DD') " & _
           "AND   LTRIM(RTRIM(AcdYear)) = '" & sAcadYear & "' " & _
           "ORDER BY FeeReciptID, Id_new"

    Set rs = db.OpenRecordset(sSql)

    If rs.EOF Then
        MsgBox "No receipts to sync for today.", vbInformation, "EOD Sync"
        rs.Close
        Exit Sub
    End If

    nTotal = 0
    nImported = 0
    nSkipped = 0
    nFailed = 0
    nCount = 0
    bFirst = True
    sBatch = "["

    Do While Not rs.EOF
        sDate = Format(rs.Fields("FeeDat").Value, "YYYY-MM-DD")
        sStat = Trim(CStr(rs.Fields("FeeReceiptStat").Value))

        sRow = BuildReceiptRow( _
            Trim(CStr(rs.Fields("FeeReciptID").Value)), _
            Trim(CStr(rs.Fields("StuAdmnNo").Value)), _
            sAcadYear, _
            Trim(CStr(rs.Fields("FeeTyp").Value)), _
            Trim(CStr(rs.Fields("PaymentTyp").Value)), _
            CDbl(rs.Fields("FeeAmt").Value), _
            sDate, _
            Trim(CStr(rs.Fields("PymntTyp").Value)), _
            Trim(CStr(rs.Fields("RefNo").Value)), _
            Trim(CStr(rs.Fields("RemarksDet").Value)), _
            sStat, _
            Trim(CStr(rs.Fields("DeletResn").Value)) _
        )

        If Not bFirst Then sBatch = sBatch & ","
        bFirst = False
        sBatch = sBatch & sRow
        nCount = nCount + 1
        nTotal = nTotal + 1

        rs.MoveNext

        If nCount >= BATCH_SIZE Or rs.EOF Then
            sBatch = sBatch & "]"
            sResult = BulkImportReceipts(sBatch, sError)

            If Len(sError) > 0 Then
                Debug.Print "EOD batch error: " & sError
            Else
                nImported = nImported + CLng(JsonNum(sResult, "imported"))
                nSkipped = nSkipped + CLng(JsonNum(sResult, "skipped"))
                nFailed = nFailed + CLng(JsonNum(sResult, "failed"))
            End If

            sBatch = "["
            nCount = 0
            bFirst = True
        End If
    Loop

    rs.Close

    MsgBox "EOD Push complete." & Chr(13) & _
           "Rows processed : " & nTotal & Chr(13) & _
           "Imported       : " & nImported & Chr(13) & _
           "Skipped (dup)  : " & nSkipped & Chr(13) & _
           "Failed         : " & nFailed, _
           vbInformation, "EOD Sync"
End Sub

'==============================================================================
' EXAMPLE 13 — MINIMAL INTEGRATION TEST (run from Immediate Window)
' Paste all 5 lines into the VB6 Immediate Window to confirm end-to-end.
'==============================================================================
' TestConnection   → should print True
' Debug.Print GetAcademicYears()
' Debug.Print GetClasses()
' Debug.Print GetStudents(3, 1, 0, "", "Active")   ' year=3, class=1
' Debug.Print GetStudentFees(12345)                  ' replace 12345 with a real studentId

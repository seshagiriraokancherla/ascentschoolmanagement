Attribute VB_Name = "General"
Dim j As Integer
Dim k As Integer
Public db As New ADODB.Connection
Public Con As New ADODB.Connection
Public Cn As New ADODB.Connection
Public Con1 As New ADODB.Connection
Public items(10) As String
Public Names(25) As String
Public USBSecurity(100) As String
Public rs As New ADODB.Recordset
Public r As New ADODB.Recordset
Public Res As New ADODB.Recordset
Public Rgs As New ADODB.Recordset
Public Pth As String
Public serverPath As String
Public StuImgPath As String
Public StfImgPath As String
Public OfficePath As String
Public SchName As String
Public SchAddr As String
Public SchPh As String
Public SchTyp As String
Public SchShortNam As String
Public SysMacAd As String
Public SysIPAddr As String
Public SysNam As String
Public FeeReceiptStat As String
Public NofReceipts As Integer
Public UsrID As String
Public UsrAth As String
Public UsrNam As String
Public StartDat As Date
Public EndDat As Date
Public traDate As Date
Public transactionsStart As Date
Public FinalRepStat As String
Public branchID  As String
Public branchID_REs As String, branchid_Fin As String
Public shiftName As String
Public titleLodgePanel As Integer, submenuChk As Integer
Public Declare Sub Sleep Lib "kernel32" (ByVal dwMilliseconds As Long)
Public repMenuChk As Integer
Public Lang As String
Public PrntrPort As String
Public PrntrMod As String
Public PrntMod As String ' Other Receipts Print Model
Public FinStart As Date
Public FinEnd As Date
Public AcdStatDat As Date
Public AcdEndDat As Date
Public AcdYear As String
Public SmsPortNo As String
Public SmsTitl As String
Public SmsStat As String
Public SmsUrlStr As String
Public SmsTyp As String
Public AdminSmsMobil As String
Public SmsPrvdr As String
Public SmsTmplatID As String
Public SmsTmplatFooter As String
Public FineStat As String
Public FtTyp As String ' Fee Type like Monthly / Terms
Public TrnsInclud As String ' Transport Fee Includes Regular Fees
Public Const MAX_WSADescription As Long = 256
Public Const MAX_WSASYSStatus As Long = 128
Public Const ERROR_SUCCESS       As Long = 0
Public Const WS_VERSION_REQD     As Long = &H101
Public Const WS_VERSION_MAJOR    As Long = WS_VERSION_REQD \ &H100 And &HFF&
Public Const WS_VERSION_MINOR    As Long = WS_VERSION_REQD And &HFF&
Public Const MIN_SOCKETS_REQD    As Long = 1
Public Const SOCKET_ERROR        As Long = -1
Public dbCheck As Integer
Public Acslck As Integer
Public TINno As String
Public PurInwardLock As Integer
Public StrAPIkey As String

Public BilNoStatTyp As String

Public Type HOSTENT
   hNAME      As Long
   hAliases   As Long
   hAddrType  As Integer
   hLen       As Integer
   hAddrList  As Long
End Type
Public Declare Function gethostname Lib "wsock32" _
  (ByVal szHost As String, _
   ByVal dwHostLen As Long) As Long
Public Declare Function WSAGetLastError Lib "wsock32" () As Long
Public Declare Function gethostbyname Lib "wsock32" _
  (ByVal szHost As String) As Long
  
  Public Declare Sub CopyMemory Lib "kernel32" _
   Alias "RtlMoveMemory" _
  (hpvDest As Any, _
   ByVal hpvSource As Long, _
   ByVal cbCopy As Long)
   
   Public Type WSADATA
   wVersion      As Integer
   wHighVersion  As Integer
   szDescription(0 To MAX_WSADescription)   As Byte
   szSystemStatus(0 To MAX_WSASYSStatus)    As Byte
   wMaxSockets   As Integer
   wMaxUDPDG     As Integer
   dwVendorInfo  As Long
End Type


Public Declare Function WSAStartup Lib "wsock32" _
  (ByVal wVersionRequired As Long, _
   lpWSADATA As WSADATA) As Long
   
   
   'Voice
   Public voice As New SpeechLib.SpVoice
   
   '----------------------------------------
   
   
   '-----------Web Cam ---------------------
   Public CameraName As String
   Public crx As New CRAXDRT.Application
   '----------------------------------------
   
Public Declare Function WSACleanup Lib "wsock32" () As Long

Public Sub ConOpen()
On Error GoTo ErrHandler
Dim DbNam As String
Dim SrvrNam As String
Dim LogNam As String
Dim SrvrPwd As String


'dbCheck = 1 'SQL
'dbCheck = 0 'MS Access

Pth = "D:\SchoolAutomationSolution\SASsupport.mdb"
Cn.Open "provider=Microsoft.jet.oledb.4.0; data source=" & Pth & ""



'Con1.Open "Provider=Microsoft Jet 4.0 OLE DB Provider;Data Source=" & App.Path & "\AscentSchoolDB.int; Jet OLEDB:Database Password=23644"


 

Dim i As Integer
i = 1
Open App.Path & "\DBConfig.txt" For Input As #1
Do Until EOF(1)
Line Input #1, tmpvar
' /* Do something here with line 5 of the file */
    If i = 1 Then
        SrvrNam = tmpvar
    ElseIf i = 2 Then
        DbNam = tmpvar
    ElseIf i = 3 Then
        LogNam = tmpvar
    ElseIf i = 4 Then
        SrvrPwd = tmpvar
    ElseIf i = 5 Then
        If tmpvar = "MS-Access" Then
            dbCheck = 0  'MS Access
        Else
            dbCheck = 1 'SQL
        End If
    ElseIf i = 6 Then
        StuImgPath = tmpvar
    ElseIf i = 7 Then
        StfImgPath = tmpvar
    End If
    i = i + 1
Loop
Close #1

If dbCheck = 1 Then
    Con.Open "Provider=SQLOLEDB.1;Persist Security Info=False;User ID=" & LogNam & ";password=" & SrvrPwd & ";Data Source=" & SrvrNam & ";Initial Catalog=" & DbNam & "" '
'    Con.CursorLocation = adUseClient
'    Con.Open "Provider=MSDASQL; DRIVER=Sql Server; SERVER=" & SrvrNam & "; DATABASE=" & DbNam & "; UID=" & LogNam & "; PWD=" & SrvrPwd & ";"
Else
    Con.Open "Provider=Microsoft Jet 4.0 OLE DB Provider;Data Source=" & App.Path & "\EducareDB.mdb; Jet OLEDB:Database Password=mushkin@outlook.in"
End If
  
 
'VIET - BR150032
'VTEC - BR150033
 


Exit Sub
ErrHandler:
'Adm_ServrConfig.Show
End Sub

Public Sub USBSecurt()
USBSecurity(0) = "20051738130F1D905BFB" 'Royal Srinivas,VSKP
'USBSecurity(0) = "4C532007410218110042" 'ChakraPani,BZA
'USBSecurity(0) = "9788R9O8" ' USB Devise ID

'BR120004 -- SFS Gajuwaka
 End Sub

Public Sub Regs()
If Rgs.State = 1 Then Rgs.Close
Rgs.CursorLocation = adUseClient
Rgs.Open "select * from SAS_LicenceDet", Con, adOpenKeyset, adLockReadOnly
    If Rgs.RecordCount > 0 Then
        SchName = Rgs("FirmName") ' Name
        SchAddr = Rgs("Addr") & "," & Rgs("City") 'Address
        SchPh = Rgs("Phs") 'PH
        SchTyp = Rgs("InstituteType")
        SchShortNam = Rgs("ShortNam")
        Names(1) = Rgs("ContactNam")
        Names(2) = Rgs("Addr")
        Names(3) = Rgs("City") 'City
        Names(5) = Rgs("ShortNam")   'TIN / ShortNam
        Names(6) = Rgs("mailID") 'District
        Names(7) = ""  ' receipt print
        Names(8) = Rgs("InstituteType") 'School / College
        Names(9) = Rgs("FeeTyp") 'Fee Receipt Type -- Ex : Terms/Monthly
        Names(10) = ""
        Names(18) = "SMS"  '"NoSMS"
        Names(19) = "SQL-Server" '"MS-Access"
Else
    Names(0) = "Ascent Info Solutions" ' Name
    Names(1) = "East Anadh Bagh,"
    Names(2) = "Malkajgiri," 'Address
    Names(3) = "Hyderabad." 'City
    Names(4) = "9392123644" 'PH
    Names(5) = "" 'TIN
    Names(6) = ""
    Names(7) = ""  ' receipt print
    Names(8) = ""
    Names(9) = ""
    Names(10) = ""
    Names(18) = "SMS"  '"NoSMS"
    Names(19) = "MS-Access"
    End If
Rgs.Close

End Sub

Public Sub FrmPrts(X As Form)
'x.BackColor = &HF5C7A7
'    Shp.Width = x.Width + 2700
'    Shp1.Width = x.Width + 2700
X.Icon = LoadPicture(App.Path & "\Images\AscentIcon.ico")
'x.Picture
End Sub
Public Function fixstring(s As String, X As Integer) As String
'tHIS function returns a string after padding the given string with required spaces
Dim ss As String
If X > Len(s) Then
ss = s & Space(X - Len(s))
Else
ss = Mid(s, 1, X)
End If
fixstring = ss
End Function

Public Function fixLeftString(s As String, n As Integer) As String
If Len(s) >= n Then
    fixLeftString = s
Else
    fixLeftString = Space(n - Len(s)) & s
End If
End Function
Public Function fixCenterString(s As String, X As Integer) As String
'This function returns a center padding string
Dim ss As String
If X > Len(s) Then
ss = Space((X - Len(s)) / 2) & s
Else
ss = Mid(s, 1, X)
End If
fixCenterString = ss

End Function
Public Sub dosPrint(X As ListBox)
Dim tt As Integer
tt = MsgBox("Prints Data", vbOKCancel)
If tt = vbOK Then
Dim i As Integer, j As Integer, k As Integer
k = 1
i = 0
Open App.Path & "\rer.001" For Output As #1
While i <= X.ListCount - 1
    Print #1, fixstring(items(0), 60) & fixLeftString("Page No", 12) & k
    Print #1, items(1)
    Print #1, items(2)
    Print #1, lin
    For j = 0 To 55
        Print #1, X.List(i)
        i = i + 1
        If i = X.ListCount Then
            GoTo 10
        End If
    Next j
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    k = k + 1
Wend
10
i = i + (6 * k)
i = i Mod 55
For j = i To 55
Print #1, ""
Next j
Close #1
Open App.Path & "\fpt.bat" For Output As #2
    Print #2, "type rer.001 >> prn"
Close #2
Shell App.Path & "\fpt.bat", vbHide
End If
End Sub

'Public Sub windowsPrint(x As ListBox)
'
''
''Dim tt As Integer
''con.Close
''Call dataCon
''
''con.Execute "delete from temptable"
''
''Dim R As New ADODB.Recordset
''R.Open "temptable", con, adOpenDynamic, adLockPessimistic
''Dim i As Integer, j As Integer, k As Integer
''k = 1
''i = 0
''While i <= X.ListCount - 1
''
''R.AddNew
''    R.Fields(0) = IIf(Trim(X.List(i)) = "", " ", Trim(X.List(i)))
''R.Update
''i = i + 1
''Wend
''R.Close
''con.Close
'''DataEnvironment1.Connection1.Close
'''DataEnvironment1.Connection1.DefaultDatabase = App.path & "\iprocess1.234"
'''DataEnvironment1.dataConn1.ConnectionString = App.path & "\iprocess1.234"
''
'''DataReport1.Show vbModal
''Shell App.Path & "\ReportProj.exe", vbMaximizedFocus
''Call dataCon
''
'''Shell App.Path & "\dataRep.exe"
'''finalReport.Show vbModal
'End Sub
Public Function Uper(tx As TextBox) As String
tx = UCase(tx)
End Function

Private Function unitStr(X As Integer) As String
Dim s As String
Select Case X
    Case 0
        s = ""
    Case 1
        s = "one "
    Case 2
        s = "two "
    Case 3
        s = "three "
    Case 4
        s = "four "
    Case 5
        s = "five "
    Case 6
        s = "six "
    Case 7
        s = "seven "
    Case 8
        s = "eight "
    Case 9
        s = "nine "
End Select
unitStr = s
End Function

Private Function tenStr(X As Integer) As String
Dim s As String
Select Case X
    Case 0
        s = ""
    Case 2
        s = "twenty "
    Case 3
        s = "thirty "
    Case 4
        s = "forty "
    Case 5
        s = "fifty "
    Case 6
        s = "sixty "
    Case 7
        s = "seventy "
    Case 8
        s = "eighty "
    Case 9
        s = "ninty "
End Select
tenStr = s
End Function

Private Function teenStr(X As Integer)
Dim s As String
Select Case X
    Case 10
        s = "ten "
    Case 11
        s = "eleven "
    Case 12
        s = "twelve "
    Case 13
        s = "thirteen "
    Case 14
        s = "fourteen "
    Case 15
        s = "fifteen "
    Case 16
        s = "sixteen "
    Case 17
        s = "seventeen "
    Case 18
        s = "eighteen "
    Case 19
        s = "ninteen "
End Select
teenStr = s
End Function

Public Function numWord(p As Long) As String

Dim s As String, n As Integer, ss As String, X As Long
Dim A As Integer, b As Integer
X = p
s = ""
ss = ""




n = Int(X / 10000000)
If n >= 10 Then
    If n < 20 Then
        s = teenStr(n)
    Else
        b = n Mod 10
        A = Int(n / 10)
        s = s & tenStr(A) & unitStr(b)
    End If
Else
    If n < 10 Then
        s = unitStr(n)
    End If
End If
If n > 0 Then
    If n = 1 Then
    ss = ss & s & "crore "
    Else
    ss = ss & s & "crores "
    End If
    X = X Mod 10000000
End If

s = ""
n = Int(X / 100000)
If n >= 10 Then
    If n < 20 Then
        s = teenStr(n)
    Else
        b = n Mod 10
        A = Int(n / 10)
        s = s & tenStr(A) & unitStr(b)
    End If
Else
    If n < 10 Then
        s = unitStr(n)
    End If
End If
If n > 0 Then
    If n = 1 Then
    ss = ss & s & "lakh "
    Else
    ss = ss & s & "lakhs "
    End If
    X = X Mod 100000
End If



s = ""
n = Int(X / 1000)
If n >= 10 Then
    If n < 20 Then
        s = teenStr(n)
    Else
        b = n Mod 10
        A = Int(n / 10)
        s = s & tenStr(A) & unitStr(b)
    End If
Else
    If n < 10 Then
        s = unitStr(n)
    End If
End If
If n > 0 Then
    If n = 1 Then
    ss = ss & s & "thousand " 'thousands
    Else
    ss = ss & s & "thousand "
    End If
    X = X Mod 1000
End If


s = ""
n = Int(X / 100)
If n >= 10 Then
    If n < 20 Then
        s = teenStr(n)
    Else
        b = n Mod 10
        A = Int(n / 10)
        s = s & tenStr(A) & unitStr(b)
    End If
Else
    If n < 10 Then
        s = unitStr(n)
    End If
End If
If n > 0 Then
    X = X Mod 100
    If X > 0 Then
    ss = ss & s & "hundred and "
    Else
    ss = ss & s & "hundred "
    End If
Else
    X = X Mod 100
    If X > 0 Then
    End If
End If

s = ""
n = Int(X Mod 100)
If n >= 10 Then
    If n < 20 Then
        s = teenStr(n)
    Else
        b = n Mod 10
        A = Int(n / 10)
        s = s & tenStr(A) & unitStr(b)
    End If
Else
    If n < 10 Then
        s = unitStr(n)
    End If
End If
If n > 0 Then
    ss = ss & s
End If

'ss = ss & "rupees only"
s = Mid(ss, 1, 1)
s = UCase(s)
ss = s & Mid(ss, 2, Len(ss))
numWord = "Rupees: " & ss & "Only"
End Function
Public Function numD(X As Double) As Integer
'Retuns number of digits in a number
Dim s As String
s = X
numD = Len(s)
End Function

Public Function fixCur(X As Double, n As Integer) As String
Dim X1 As Double, X2 As Double
Dim s As String
X2 = Round(X, n)
X1 = Int(X)
If InStr(1, Str(X), ".") = 0 Then
    s = ""
Else
    s = Trim(Mid(Str(X), InStr(1, Str(X), ".") + 1, n))
End If
s = s & zeroPad(n - Len(Trim(s)))
fixCur = Trim(Str(X1) & "." & s)
End Function

Public Function intCheck(X As Integer) As Integer
Dim t As Integer
If X < 48 Or X > 57 Then
    If X <> 8 Then
        t = 0
    Else
        t = 8
    End If
Else
       t = X
End If
If X = 13 Then t = 13
intCheck = t
End Function



Public Function makeLine(X As Integer) As String
Dim i As Integer, s As String
s = ""
For i = 1 To X
    s = s & "_"
Next i

makeLine = s
End Function
Public Function makCur(X As String) As String
Dim L As Integer, s As String
L = Len(X)
If L <= 6 Then
s = X
ElseIf L <= 8 Then
s = Mid(X, 1, Len(X) - 6) & "," & Right(X, 6)
ElseIf L <= 10 Then
s = Mid(X, 1, Len(X) - 8) & "," & Mid(X, Len(X) - 7, 2) & "," & Right(X, 6)
Else
s = Mid(X, 1, Len(X) - 10) & "," & Mid(X, Len(X) - 9, 2) & "," & Mid(X, Len(X) - 7, 2) & "," & Right(X, 6)
End If
makCur = s
End Function

Public Sub windowsPrint(X As ListBox)
Dim tt As Integer
tt = MsgBox("Prints Data", vbOKCancel)
If tt = vbOK Then
Printer.FontName = "Courier New"
Printer.FontSize = 10
Printer.FontBold = False
Printer.FontItalic = False
Dim i As Integer, j As Integer, k As Integer
k = 1
i = 0
While i <= X.ListCount - 1
'    Printer.Print ""
 '   Printer.Print ""
  '  Printer.Print ""
    
    Printer.Print Space(8) & fixstring("", 60) '& fixLeftString("Page No", 12) & k
 '   Printer.Print Space(8)
   ' Printer.Print Space(8)
 '   Printer.Print Space(8) & lin
 '   Printer.Print Space(8) & ""
 
    For j = 0 To 60
        Printer.Print X.List(i)
        i = i + 1
        If i = X.ListCount Then
        
            GoTo 10
        End If
    Next j
    Printer.Print Space(8) & ""
    Printer.Print Space(70) & "Contd.,"
    Printer.NewPage
    k = k + 1
Wend
10
i = i + (6 * k)
i = i Mod 60
For j = i To 60
Printer.Print ""
Next j
Printer.EndDoc
End If

End Sub

Public Sub ItmPrts(cmb As ComboBox)
cmb.AddItem "Style"
cmb.AddItem "Fabric"
cmb.AddItem "Design"
cmb.AddItem "Size"
cmb.AddItem "Color"
cmb.AddItem "Brand"
cmb.AddItem "Gender"
End Sub

Public Function begTra(da As Date) As Date
Dim s As String
s = "1-apr-"
If Month(da) < 4 Then
s = s & Year(da) - 1
Else
s = s & Year(da)
End If
begTra = CDate(s)
End Function
Public Function cloTra(da As Date) As Date
Dim s As String
s = "31-mar-"
If Month(da) < 4 Then
s = s & Year(da)
Else
s = s & Year(da) + 1
End If
cloTra = CDate(s)
End Function

Public Function strDatConv(dd As Date) As String
    Dim s As String
    s = dayDou(Day(dd)) & "-" & Mid(MonthName(Month(dd)), 1, 3) & "-" & Year(dd)
    strDatConv = s
End Function

Public Function dayDou(X As Integer) As String
Dim s As String
If X < 10 Then
s = 0 & X
Else
s = X
End If
dayDou = s
End Function
Private Function linEq() As String
Dim i As Integer, s As String
For i = 1 To 78
    s = s & "="
Next i
lineEq = s
End Function
Public Function fixDatConv(dd As Date) As String
    Dim s As String
    s = dayDou(Day(dd)) & "/" & dayDou(Month(dd)) & "/" & Year(dd)
    fixDatConv = s
End Function
Public Sub commonItems()
items(0) = "Hotel Nagamani"
items(1) = "NTR Colony"
items(2) = "Vijayawada"
items(3) = "Srinivasa Nagar"
items(4) = "Vijayawada"
items(5) = "VJAIP000"
End Sub
Public Function weekStart(da As Date) As Date
Dim d As Date
d = da - Weekday(da) + 1
weekStart = d
End Function
Public Function weekEnd(da As Date) As Date
Dim d As Date
d = weekStart(da) + 6
weekEnd = d
End Function
Public Function alphaCheck(X As Integer) As Integer
Dim n As Integer
If X >= 97 And X <= 122 Then
    n = X
ElseIf X >= 65 And X <= 90 Then
    n = X
ElseIf X = 8 Then
    n = X
Else
    n = 0
End If
alphaCheck = n
End Function
Public Function alphaNumericCheck(X As Integer) As Integer
Dim n As Integer
If X >= 97 And X <= 122 Then
    n = X
ElseIf X >= 65 And X <= 90 Then
    n = X
ElseIf X >= 48 And X <= 57 Then
    n = X
ElseIf X = 8 Then
    n = X
Else
    n = 0
End If
alphaNumericCheck = n
End Function
Public Function numberCheck(X As Integer, m As TextBox) As Integer
If InStr(m.text, ".") = 0 Then k = 0
If X = 46 Then k = k + 1
If X < 48 Or X > 57 Then
If X <> 8 And X <> 46 Then
X = 0
End If
End If
If X = 46 And k > 1 Then
X = 0
End If
numberCheck = X
End Function


Public Function triNum(X As Integer) As String
Dim s As String

    If X < 10 Then
        s = "00" & X
    ElseIf X < 100 Then
        s = "0" & X
    Else
        s = X
    End If

triNum = s
End Function
Public Function changeChar(s As String, s1 As String, s2 As String) As String
Dim str1 As String, str2 As String
Dim i As Integer
str2 = ""
For i = 1 To Len(s)
    str1 = Mid(s, i, 1)
    If str1 = s1 Then
        str1 = s2
    End If
    str2 = str2 & str1
Next i
changeChar = str2
End Function

Public Sub dosFullPrint(X As ListBox)
Dim tt As Integer
tt = MsgBox("Prints Data", vbOKCancel)
If tt = vbOK Then
Dim i As Integer, j As Integer, k As Integer
k = 1
i = 0
Open App.Path & "\rer.001" For Output As #1
While i <= X.ListCount - 1
    Print #1, fixstring(items(0), 60) & fixLeftString("Page No", 12) & k
    Print #1, items(1)
    Print #1, items(2)
    Print #1, lin
    For j = 0 To 55
        Print #1, X.List(i)
        i = i + 1
        If i = X.ListCount Then
            GoTo 10
        End If
    Next j
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, ""
    k = k + 1
Wend
10
i = i + (6 * k)
i = i Mod 70
For j = i To 70
Print #1, ""
Next j
Close #1
Open App.Path & "\fpt.bat" For Output As #2
    'Print #2, "type rer.001 >> prn"
    Print #2, "print rer.001"
Close #2
Shell App.Path & "\fpt.bat", vbHide
End If

End Sub

Private Function lin() As String
Dim i As Integer, s As String
For i = 1 To 78
    s = s & "-"
Next i
lin = s
End Function

Public Function numDat(dd As Date) As String
Dim s As String
s = dayDou(Day(dd)) & "-" & dayDou(Month(dd)) & "-" & Year(dd)
numDat = s
End Function


Public Function monthStart(X As Date) As Date
Dim s As String, dt As Date
s = "1-"
s = s & Mid(MonthName(Month(X)), 1, 3) & "-" & Year(X)
dt = CDate(s)
monthStart = dt
End Function
Public Function monthEnd(X As Date) As Date
Dim s As String, dt As Date
s = "1-"
If Month(X) < 12 Then
s = s & Mid(MonthName(Month(X) + 1), 1, 3) & "-" & Year(X)
Else
s = s & Mid(MonthName(1), 1, 3) & "-" & Year(X)
End If
dt = CDate(s) - 1
monthEnd = dt
End Function
Public Function comboCheck(X As ComboBox) As Boolean
Dim i As Integer
For i = 0 To X.ListCount - 1
    If Trim(X.List(i)) = Trim(X.text) Then
        comboCheck = True
        Exit Function
    End If
Next i
comboCheck = False
End Function
Public Function listCheck(X As ListBox, s As String) As Boolean
Dim i As Integer
For i = 0 To X.ListCount - 1
    If Trim(X.List(i)) = Trim(s) Then
        listCheck = True
        Exit Function
    End If
Next i
listCheck = False
End Function

Public Sub htmlShow(X As ListBox, ftr As String)
Dim tt As Integer
Dim i As Integer, j As Integer, k As Integer
k = 1
i = 0


While i <= X.ListCount - 1
preView.List1.AddItem fixstring(items(0), 60) & fixLeftString("Page No", 12) & k
    preView.List1.AddItem items(1)
    preView.List1.AddItem items(2)

    preView.List1.AddItem lin
    For j = 0 To 62
        preView.List1.AddItem X.List(i)
        i = i + 1
        If i = X.ListCount Then
            GoTo 10
        End If
    Next j
    preView.List1.AddItem ""
    preView.List1.AddItem ftr
    preView.List1.AddItem ""
    preView.List1.AddItem ""
    k = k + 1
Wend
10
i = i + (6 * k)
i = i Mod 70
For j = i To 70
preView.List1.AddItem ""
Next j
Close #1
'preView.Show vbModal
End Sub
Public Sub makewindowPageNos()

'Dim i As Integer, n As Integer
'n = wPrint.vsP1.PageCount
'For i = 2 To n
'wPrint.vsP1.FontName = "Arial"
'wPrint.vsP1.FontBold = False
'wPrint.vsP1.FontSize = 8
'
'    Call wPrint.vsP1.StartOverlay(i)
'    wPrint.vsP1.CurrentX = "6.500in"
'    wPrint.vsP1.CurrentY = "0.600in"
'    wPrint.vsP1.Text = "Page " & i & " of " & n
'    Call wPrint.vsP1.EndOverlay
'Next i
End Sub
 
Public Sub makeSpool(X As ListBox, fileNam As String)
Dim tt As Integer
FileName = fileNam



Dim i As Integer, j As Integer, k As Integer
k = 1
i = 0

Open FileName For Output As #1

While i <= X.ListCount - 1
Print #1, fixstring(items(0), 60) & fixLeftString("Page No", 12) & k
Print #1, items(1)
Print #1, items(2)
Print #1, lin
    For j = 0 To 64
        Print #1, X.List(i)
        i = i + 1
        If i = X.ListCount Then
            GoTo 10
        End If
    Next j
    Print #1, ""
    Print #1, ""
    k = k + 1
Wend
10
i = i + (6 * k)
i = i Mod 70
For j = i To 70
Print #1, ""
Next j
Close #1

End Sub

Public Function fixHtmlString(s As String, X As Integer) As String
Dim ss As String
If X > Len(s) Then
ss = s & htmlsPace(X - Len(s))
Else
ss = Mid(s, 1, X)
End If
fixHtmlString = ss
End Function

Private Function htmlsPace(n As Integer)
Dim ss As String
ss = ""
For i = 1 To n
    ss = ss & "&nbsp;"
Next i

htmlsPace = s & ss

End Function

'Public Function makeLine(x As Integer) As String
'Dim i As Integer, s As String
's = ""
'For i = 1 To x
'    s = s & "_"
'Next i
'
'makeLine = s
'End Function
Public Function fixstringLine(s As String, X As Integer)
Dim ss As String
If X > Len(s) Then
ss = s & makeLineo(X - Len(s))
Else
ss = Mid(s, 1, X)
End If
fixstringLine = ss
End Function

Public Function makeLineo(X As Integer) As String
Dim i As Integer, s As String
s = ""
For i = 1 To X
    s = s & "-"
Next i

makeLineo = s
End Function

Public Sub checkTrial()
On Error GoTo ErrHandler
Dim r As New ADODB.Recordset
r.Open "select max(dotra),min(dotra) from finexec", Con, adOpenDynamic, adLockPessimistic
If Abs(r.Fields(0) - r.Fields(1)) > 30 Then
    MsgBox "SystemHTF.ini file missed"
    End
End If
Exit Sub
ErrHandler:
End Sub
Public Function zeroMake(X As Long, Y As Integer) As String
Dim s As String, i As Integer
s = Trim(Str(X))
i = Y - Len(s)
s = zeroPad(i) & s
zeroMake = s
End Function
Private Function zeroPad(X As Integer) As String
Dim i As Integer, s As String
s = ""
For i = 1 To X
    s = s & "0"
Next i
zeroPad = s
End Function

Public Function strDate(d As Date) As String
strDate = dayDou(Day(d)) & "-" & Mid(MonthName(Month(d)), 1, 3) & "-" & Right(Year(d), 2)
End Function
Public Function newStrDate(d As Date) As String
If dbCheck = 0 Then
    newStrDate = "cdate('" & strDate(d) & "')"
Else
    newStrDate = "'" & strDate(d) & "'"
End If
End Function
Public Sub myOwnProperties(X As Form)
On Error GoTo ErrHandler
X.Icon = LoadPicture(App.Path & "\myicoRes.ico")
'Sets icon of Form
'x.Left = mMenu.Width / 2 - x.Width / 2
'x.Top = mMenu.Height / 2 - x.Height / 2

X.t1.Caption = items(0)
X.t2.Caption = items(1) & ", " & items(2)  'subUserNo & ", " & subUserName

'To set Customer details on form
'x.t1.Width = x.Width
'x.t2.Width = x.Width
'x.t1.Left = 0
'x.t2.Left = 0

X.datCaption.Caption = X.Caption & " on " & strDate(traDate)
X.datCaption.Width = X.Width
ErrHandler:
End Sub

Public Function removeSQ(X As Integer) As Integer
If X = 39 Then
removeSQ = 0
Else
removeSQ = X
End If
End Function
Private Sub makeServerPath()
On Error GoTo ErrHandler
Dim d As New ADODB.Connection
d.Open "provider=microsoft.jet.oledb.4.0;data source = " & App.Path & "\Temp.mdb"
r.Open "ApplPath", d, adOpenDynamic, adLockPessimistic
If Right(r.Fields(0), 1) = "\" Then
serverPath = r.Fields(0)
Else
serverPath = r.Fields(0) & "\"
End If
r.Close
d.Close
Exit Sub
ErrHandler:
If Len(App.Path) > 3 Then
serverPath = App.Path & "\"
Else
serverPath = App.Path
End If

End Sub

Public Function financialStart(d As Date) As Date
Dim s As String
s = "1-Apr-"
If Month(d) >= 4 Then
s = s & zeroMake(Val(Right(Year(d), 2)), 2)
Else
s = s & zeroMake(Val(Right(Year(d), 2)) - 1, 2)
End If
financialStart = CDate(s)
End Function
Public Function financialClosing(d As Date) As Date
Dim s As String
s = "31-Mar-"
If Month(d) >= 4 Then
s = s & zeroMake(Val(Right(Year(d), 2)) + 1, 2)
Else
s = s & zeroMake(Val(Right(Year(d), 2)), 2)
End If

financialClosing = CDate(s)
End Function
Public Function removeEnter(s As String) As String
Dim Str As String, i As Integer
Str = ""

For i = 1 To Len(s)
    If Asc(Mid(s, i, 1)) = 13 Or Asc(Mid(s, i, 1)) = 10 Then
    If i <> 1 Then
        If Mid(Str, i - 1, 1) <> "," Then
        Str = Str & ", "
        End If
    End If
    Else
        Str = Str & Mid(s, i, 1)
    End If
    
Next i
removeEnter = Str

End Function

Public Function removeEnterOnly(s As String) As String
Dim Str As String, i As Integer
Str = ""

For i = 1 To Len(s)
    If Asc(Mid(s, i, 1)) = 13 Or Asc(Mid(s, i, 1)) = 10 Then
    If i <> 1 Then
        If Mid(Str, i - 1, 1) <> "," Then
            Str = Str
        End If
    End If
    Else
        Str = Str & Mid(s, i, 1)
    End If
    
Next i
removeEnterOnly = Str

End Function
Public Sub makePropertiesRes(s As String)
branchID_REs = s
Dim r As New ADODB.Recordset
r.Open "select * from admCompanyProfile where propertyCode = '" & s & "'", Con, adOpenDynamic, adLockPessimistic
If r.BOF = True And r.EOF = True Then
    Exit Sub
Else
    items(3) = r.Fields(1)
    items(4) = r.Fields(5)
End If
r.Close
End Sub
Public Sub makePropertiesFin(s As String)
branchid_Fin = s
Dim r As New ADODB.Recordset
r.Open "select * from admCompanyProfile where propertyCode = '" & s & "'", Con, adOpenDynamic, adLockPessimistic
If r.BOF = True And r.EOF = True Then
    Exit Sub
Else
    items(3) = r.Fields(1)
    items(4) = r.Fields(5)
End If
r.Close
End Sub

Public Sub listTotalCle(lBox As ListBox)
Dim i As Integer
For i = 0 To lBox.ListCount - 1
    lBox.Selected(i) = False
Next i
End Sub

Public Function makeUpperRound(X As Double) As Double
Dim Y As Long
Y = X
If Y = X Then
makeUpperRound = X
Else
    If Y > X Then
        makeUpperRound = Y
    Else
        makeUpperRound = Y + 1
    End If
End If
End Function
Public Function makeRoundNearBy(X As Double) As Double
Dim Y As Integer

Y = X
makeRoundNearBy = Y
'x = x - y
'If x >= ".51" Then
'    makeRoundNearBy = y + 1
'Else
'    makeRoundNearBy = y
'End If
 
End Function
Public Sub makeSingleOkListview(X As ListView, n As Integer)
Dim i As Integer
For i = 1 To X.ListItems.Count
    If i = n Then
        X.ListItems(i).Checked = True
    Else
        X.ListItems(i).Checked = False
    End If
Next i
End Sub

Public Function verifyCombo(s As String, cmb As ComboBox) As Integer
Dim i As Integer
For i = 0 To cmb.ListCount - 1
    If cmb.List(i) = s Then
        verifyCombo = i
        Exit Function
    End If
Next i
verifyCombo = -1
End Function

Public Function makeUpper(X As Integer) As Integer
If X >= 97 And X <= 122 Then
    makeUpper = X - 32
Else
    makeUpper = X
End If
End Function
Public Function clinetVerifyChk() As Integer
Dim d As New ADODB.Connection, r As New ADODB.Recordset
d.Open "provider=microsoft.jet.oledb.4.0;data source = " & App.Path & "\serverpath.mdb"
r.Open "clientVerify", d, adOpenDynamic, adLockPessimistic
clinetVerifyChk = r.Fields(0)
r.Close
End Function
Public Function financialStringMake()
Dim s As String
s = Right(Year(financialStart(Date)), 2) & Right(Year(financialClosing(Date)), 2)
financialStringMake = s

End Function
Public Function removeHtmlEnter(s As String) As String
Dim Str As String, i As Integer
Str = ""

For i = 1 To Len(s)
    If Asc(Mid(s, i, 1)) = 13 Or Asc(Mid(s, i, 1)) = 10 Then
    If i <> 1 Then
        If Mid(Str, i - 1, 4) <> "<br>" Then
        Str = Str & "<br>"
        End If
    End If
    Else
        Str = Str & Mid(s, i, 1)
    End If
    
Next i
removeHtmlEnter = Str

End Function

Public Function ColNameforExcel(ColNo As Integer) As String
    If ColNo = 1 Then ColNameforExcel = "A"
    If ColNo = 2 Then ColNameforExcel = "B"
    If ColNo = 3 Then ColNameforExcel = "C"
    If ColNo = 4 Then ColNameforExcel = "D"
    If ColNo = 5 Then ColNameforExcel = "E"
    If ColNo = 6 Then ColNameforExcel = "F"
    If ColNo = 7 Then ColNameforExcel = "G"
    If ColNo = 8 Then ColNameforExcel = "H"
    If ColNo = 9 Then ColNameforExcel = "I"
    If ColNo = 10 Then ColNameforExcel = "J"
    If ColNo = 11 Then ColNameforExcel = "K"
    If ColNo = 12 Then ColNameforExcel = "L"
    If ColNo = 13 Then ColNameforExcel = "M"
    If ColNo = 14 Then ColNameforExcel = "N"
    If ColNo = 15 Then ColNameforExcel = "O"
    If ColNo = 16 Then ColNameforExcel = "P"
    If ColNo = 17 Then ColNameforExcel = "Q"
    If ColNo = 18 Then ColNameforExcel = "R"
    If ColNo = 19 Then ColNameforExcel = "S"
    If ColNo = 20 Then ColNameforExcel = "T"
    If ColNo = 21 Then ColNameforExcel = "U"
    If ColNo = 22 Then ColNameforExcel = "V"
    If ColNo = 23 Then ColNameforExcel = "W"
    If ColNo = 24 Then ColNameforExcel = "X"
    If ColNo = 25 Then ColNameforExcel = "Y"
    If ColNo = 26 Then ColNameforExcel = "Z"
    If ColNo = 27 Then ColNameforExcel = "AA"
    If ColNo = 28 Then ColNameforExcel = "AB"
    If ColNo = 29 Then ColNameforExcel = "AC"
    If ColNo = 30 Then ColNameforExcel = "AD"
    If ColNo = 31 Then ColNameforExcel = "AE"
    If ColNo = 32 Then ColNameforExcel = "AF"
    If ColNo = 33 Then ColNameforExcel = "AG"
    If ColNo = 34 Then ColNameforExcel = "AH"
    If ColNo = 35 Then ColNameforExcel = "AI"
    If ColNo = 36 Then ColNameforExcel = "AJ"
    If ColNo = 37 Then ColNameforExcel = "AK"
    If ColNo = 38 Then ColNameforExcel = "AL"
    If ColNo = 39 Then ColNameforExcel = "AM"
    If ColNo = 40 Then ColNameforExcel = "AN"
    If ColNo = 41 Then ColNameforExcel = "AO"
    If ColNo = 42 Then ColNameforExcel = "AP"
    If ColNo = 43 Then ColNameforExcel = "AQ"
    If ColNo = 44 Then ColNameforExcel = "AR"
    If ColNo = 45 Then ColNameforExcel = "AS"
    If ColNo = 46 Then ColNameforExcel = "AT"
    If ColNo = 47 Then ColNameforExcel = "AU"
    If ColNo = 48 Then ColNameforExcel = "AV"
    If ColNo = 49 Then ColNameforExcel = "AW"
    If ColNo = 50 Then ColNameforExcel = "AX"
    If ColNo = 51 Then ColNameforExcel = "AY"
    If ColNo = 52 Then ColNameforExcel = "AZ"
    If ColNo = 53 Then ColNameforExcel = "BA"
    If ColNo = 54 Then ColNameforExcel = "BB"
    If ColNo = 55 Then ColNameforExcel = "BC"
    If ColNo = 56 Then ColNameforExcel = "BD"
    If ColNo = 57 Then ColNameforExcel = "BE"
    If ColNo = 58 Then ColNameforExcel = "BF"
    If ColNo = 59 Then ColNameforExcel = "BG"
    If ColNo = 60 Then ColNameforExcel = "BH"
    If ColNo = 61 Then ColNameforExcel = "BI"
    If ColNo = 62 Then ColNameforExcel = "BJ"
    If ColNo = 63 Then ColNameforExcel = "BK"
    If ColNo = 64 Then ColNameforExcel = "BL"
    If ColNo = 65 Then ColNameforExcel = "BM"
    If ColNo = 66 Then ColNameforExcel = "BN"
    If ColNo = 67 Then ColNameforExcel = "BO"
    If ColNo = 68 Then ColNameforExcel = "BP"
    If ColNo = 69 Then ColNameforExcel = "BQ"
    If ColNo = 70 Then ColNameforExcel = "BR"
    If ColNo = 71 Then ColNameforExcel = "BS"
    If ColNo = 72 Then ColNameforExcel = "BT"
    If ColNo = 73 Then ColNameforExcel = "BU"
    If ColNo = 74 Then ColNameforExcel = "BV"
    If ColNo = 75 Then ColNameforExcel = "BW"
    If ColNo = 76 Then ColNameforExcel = "BX"
    If ColNo = 77 Then ColNameforExcel = "BY"
    If ColNo = 78 Then ColNameforExcel = "BZ"
End Function


Public Function numCheck(X As Integer, m As TextBox, n As Integer) As Integer
'The aim of this function is to return the keyascii of one key after checking
'whether given text control satisifies a reasonable number or not

'X argument
'Receives keyascii from function

'm argument
'A text box in which number is to be checked

'n argument
'After how many digits the number is to be stopped

If InStr(m.text, ".") = 0 Then j = 0
'If text does not contain decimal we can go for any number of digits


If X = 46 Then j = j + 1
'The moment when we press decimal flag j will be increased by 1

If X < 48 Or X > 57 Then
' Checking whether given one is digits range or not i.e. 0-9

If X <> 8 And X <> 46 Then
' if keyascii is neither back space not decimal key
'then it becomes a null charecter
X = 0
End If
End If


If X = 46 And j > 1 Then
'If decimal is already existed and we are forcing another decimal charecter
'thenit becomes a null charecter
X = 0
End If

If InStr(m.text, ".") <> 0 And Len(m.text) - InStr(1, m.text, ".") >= n Then
' If text contains a decimal charecter and we are forcing more digits after
'required decimal position then if chaected is not back space then it
'becomes null charecter
If X <> 8 Then
X = 0
End If
End If
numCheck = X
End Function

 

Public Function GetIPAddress() As String

   Dim sHostName    As String * 256
   Dim lpHost    As Long
   Dim HOST      As HOSTENT
   
   Dim dwIPAddr  As Long
   Dim tmpIPAddr() As Byte
   Dim i         As Integer
   Dim sIPAddr  As String
   
   If Not SocketsInitialize() Then
      GetIPAddress = ""
      Exit Function
   End If

   If gethostname(sHostName, 256) = SOCKET_ERROR Then
      GetIPAddress = ""
      MsgBox "Windows Sockets error " & Str$(WSAGetLastError()) & _
              " has occurred. Unable to successfully get Host Name."
      SocketsCleanup
      Exit Function
   End If
   
   sHostName = Trim$(sHostName)
   lpHost = gethostbyname(sHostName)
    
   If lpHost = 0 Then
      GetIPAddress = ""
      MsgBox "Windows Sockets are not responding. " & _
              "Unable to successfully get Host Name."
      SocketsCleanup
      Exit Function
   End If

   CopyMemory HOST, lpHost, Len(HOST)
   CopyMemory dwIPAddr, HOST.hAddrList, 4
   
   ReDim tmpIPAddr(1 To HOST.hLen)
   CopyMemory tmpIPAddr(1), dwIPAddr, HOST.hLen
   
   For i = 1 To HOST.hLen
      sIPAddr = sIPAddr & tmpIPAddr(i) & "."
   Next
  
   GetIPAddress = Mid$(sIPAddr, 1, Len(sIPAddr) - 1)
   
   SocketsCleanup
    
End Function
Public Function SocketsInitialize() As Boolean

   Dim WSAD As WSADATA
   Dim sLoByte As String
   Dim sHiByte As String
   
   If WSAStartup(WS_VERSION_REQD, WSAD) <> ERROR_SUCCESS Then
      MsgBox "The 32-bit Windows Socket is not responding."
      SocketsInitialize = False
      Exit Function
   End If
   
   
   If WSAD.wMaxSockets < MIN_SOCKETS_REQD Then
        MsgBox "This application requires a minimum of " & _
                CStr(MIN_SOCKETS_REQD) & " supported sockets."
        
        SocketsInitialize = False
        Exit Function
    End If
   
   
   If LoByte(WSAD.wVersion) < WS_VERSION_MAJOR Or _
     (LoByte(WSAD.wVersion) = WS_VERSION_MAJOR And _
      HiByte(WSAD.wVersion) < WS_VERSION_MINOR) Then
      
      sHiByte = CStr(HiByte(WSAD.wVersion))
      sLoByte = CStr(LoByte(WSAD.wVersion))
      
      MsgBox "Sockets version " & sLoByte & "." & sHiByte & _
             " is not supported by 32-bit Windows Sockets."
      
      SocketsInitialize = False
      Exit Function
      
   End If
    
   SocketsInitialize = True
        
End Function

Public Sub SocketsCleanup()
    If WSACleanup() <> ERROR_SUCCESS Then
        MsgBox "Socket error occurred in Cleanup."
    End If
End Sub

Public Function LoByte(ByVal wParam As Integer) As Byte
   LoByte = wParam And &HFF&
End Function


Public Function HiByte(ByVal wParam As Integer) As Byte
   HiByte = (wParam And &HFF00&) \ (&H100)
End Function


Public Sub yCOUNT(narr As String, s As String, s1 As String)
'RowCount = RowCount + 1
'
'wPrint.vsP1.AddTable s, "", s1, vbCrLf
'
'If RowCount = pageAvailableLines Then
'    wPrint.footerMake (items(0))
'
'    RowCount = 1
'    wPrint.vsP1.NewPage
'    Call wPrint.headerMAke(narr, pageNO)
'
'    wPrint.vsP1.CurrentX = "1.400in"
'    wPrint.vsP1.CurrentY = "1.400in"
'
'
'    pageNO = pageNO + 1
'End If
'


End Sub

Public Function fixCenString(s As String, n As Integer) As String
Dim i As Integer
If Len(s) >= n Then
    fixCenString = s
Else
    i = (n / 2) - (Len(s) / 2)
    fixCenString = Space(i) & s
End If
End Function

Public Function makeCurrency(s As String, tx As TextBox) As String
Dim ss As String

ss = RemoveCurrency(s)

'If Right(ss, 1) <> "." And Left(Right(ss, 2), 1) <> "." Then
If InStr(1, ss, ".") > 0 Then
    ss = makCur(fixCur(Val(ss), 2))
Else
    ss = makCur(fixCur(Val(ss), 2))
    If Right(ss, 3) = ".00" Then
        ss = Mid(ss, 1, Len(ss) - 3)
    End If
End If
   
'End If
If Left(Right(ss, 3), 1) = "." And Right(ss, 2) = "00" Then
    ss = Mid(ss, 1, Len(ss) - 2)
ElseIf Left(Right(ss, 3), 1) = "." And Left(Right(ss, 2), 1) <> "0" And Right(ss, 1) = "0" Then
    tx.SelStart = Len(tx.text) - 1
    tx.SelLength = 1
If Left(Right(ss, 2), 1) <> "." Then
   ss = Mid(ss, 1, Len(ss) - 1)
   tx.SelStart = Len(tx.text)
    tx.SelLength = 0
End If
Else
    tx.SelStart = Len(tx.text)
    tx.SelLength = 0
End If
makeCurrency = ss
End Function
Public Function RemoveCurrency(s As String) As String
Dim ss As String, i As Integer, s1 As String
ss = ""
For i = 1 To Len(s)
    s1 = Mid(s, i, 1)
    If s1 <> "," Then
        ss = ss & s1
    End If
Next i
RemoveCurrency = ss
End Function
Public Function removeKama(s As String) As String
Dim i As Integer, ss As String
ss = ""
For i = 1 To Len(s)
    If Mid(s, i, 1) <> "," Then
        ss = ss & Mid(s, i, 1)
    End If
Next i
removeKama = ss
End Function

Public Function RemovSingQuot(s As String) As String
Dim i As Integer, ss As String
ss = ""
For i = 1 To Len(s)
    If Mid(s, i, 1) <> "'" Then
        ss = ss & Mid(s, i, 1)
    End If
Next i
RemovSingQuot = ss
End Function

Public Function numWords(p As Long) As String

Dim s As String, n As Integer, ss As String, X As Long
Dim A As Integer, b As Integer
X = p
s = ""
ss = ""




n = Int(X / 10000000)
If n >= 10 Then
    If n < 20 Then
        s = teenStr(n)
    Else
        b = n Mod 10
        A = Int(n / 10)
        s = s & tenStr(A) & unitStr(b)
    End If
Else
    If n < 10 Then
        s = unitStr(n)
    End If
End If
If n > 0 Then
    If n = 1 Then
    ss = ss & s & "crore "
    Else
    ss = ss & s & "crores "
    End If
    X = X Mod 10000000
End If

s = ""
n = Int(X / 100000)
If n >= 10 Then
    If n < 20 Then
        s = teenStr(n)
    Else
        b = n Mod 10
        A = Int(n / 10)
        s = s & tenStr(A) & unitStr(b)
    End If
Else
    If n < 10 Then
        s = unitStr(n)
    End If
End If
If n > 0 Then
    If n = 1 Then
    ss = ss & s & "lakh "
    Else
    ss = ss & s & "lakhs "
    End If
    X = X Mod 100000
End If



s = ""
n = Int(X / 1000)
If n >= 10 Then
    If n < 20 Then
        s = teenStr(n)
    Else
        b = n Mod 10
        A = Int(n / 10)
        s = s & tenStr(A) & unitStr(b)
    End If
Else
    If n < 10 Then
        s = unitStr(n)
    End If
End If
If n > 0 Then
    If n = 1 Then
    ss = ss & s & "thousand " ' thousands
    Else
    ss = ss & s & "thousand "
    End If
    X = X Mod 1000
End If


s = ""
n = Int(X / 100)
If n >= 10 Then
    If n < 20 Then
        s = teenStr(n)
    Else
        b = n Mod 10
        A = Int(n / 10)
        s = s & tenStr(A) & unitStr(b)
    End If
Else
    If n < 10 Then
        s = unitStr(n)
    End If
End If
If n > 0 Then
    X = X Mod 100
    If X > 0 Then
    ss = ss & s & "hundred and "
    Else
    ss = ss & s & "hundred "
    End If
Else
    X = X Mod 100
    If X > 0 Then
    End If
End If

s = ""
n = Int(X Mod 100)
If n >= 10 Then
    If n < 20 Then
        s = teenStr(n)
    Else
        b = n Mod 10
        A = Int(n / 10)
        s = s & tenStr(A) & unitStr(b)
    End If
Else
    If n < 10 Then
        s = unitStr(n)
    End If
End If
If n > 0 Then
    ss = ss & s
End If

'ss = ss & "rupees only"
s = Mid(ss, 1, 1)
s = UCase(s)
ss = s & Mid(ss, 2, Len(ss))
numWords = ss
End Function

Public Function DaysInMonth(ByVal dDate As Date) As Integer
    DaysInMonth = Day(DateAdd("m", 1, dDate - Day(dDate) + 1) - 1)
End Function
Public Sub Sendkeys(text As Variant, Optional wait As Boolean = False)
   Dim WshShell As Object
   Set WshShell = CreateObject("wscript.shell")
   WshShell.Sendkeys CStr(text), wait
       Set WshShell = Nothing
End Sub

Public Function FormatINR(ByVal Amount As Currency, _
                   Optional ByVal DigitsAfterDot As Byte = 2) As String
   '-- Convert Amount to Indian Currency format: ##,##,##,##,##,##,##0.00
   FormatINR = Format(Amount, Replace(Space((Len(Str(Fix(Amount))) - 3) \ 2), " ", "##\,") _
               & "##0" & String(-(DigitsAfterDot > 0), ".") & String(DigitsAfterDot, "0"))
End Function



Public Function StripOut(From As String, What As String) As String
Dim i As Integer
StripOut = From
For i = 1 To Len(What)
StripOut = Replace(StripOut, Mid$(What, i, 1), "")
Next i
End Function



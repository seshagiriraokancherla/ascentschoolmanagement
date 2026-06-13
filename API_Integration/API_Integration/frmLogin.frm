VERSION 5.00
Object = "{86CF1D34-0C5F-11D2-A9FC-0000F8754DA1}#2.0#0"; "MSCOMCT2.OCX"
Begin VB.Form frmLogin 
   BackColor       =   &H00EECEC1&
   BorderStyle     =   3  'Fixed Dialog
   Caption         =   "Login"
   ClientHeight    =   4905
   ClientLeft      =   2835
   ClientTop       =   3480
   ClientWidth     =   7140
   Icon            =   "frmLogin.frx":0000
   LinkTopic       =   "Form1"
   MaxButton       =   0   'False
   MinButton       =   0   'False
   ScaleHeight     =   2898.036
   ScaleMode       =   0  'User
   ScaleWidth      =   6704.073
   ShowInTaskbar   =   0   'False
   StartUpPosition =   2  'CenterScreen
   Begin VB.ComboBox CmbAcdYear 
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   9.75
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   360
      Left            =   2790
      Style           =   2  'Dropdown List
      TabIndex        =   1
      Top             =   1920
      Width           =   2355
   End
   Begin MSComCtl2.DTPicker DTPicker1 
      Height          =   375
      Left            =   240
      TabIndex        =   14
      Top             =   3480
      Visible         =   0   'False
      Width           =   1545
      _ExtentX        =   2725
      _ExtentY        =   661
      _Version        =   393216
      Format          =   131072001
      CurrentDate     =   41262
   End
   Begin VB.ListBox List1 
      Appearance      =   0  'Flat
      BackColor       =   &H80000003&
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   9.75
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   990
      ItemData        =   "frmLogin.frx":030A
      Left            =   5190
      List            =   "frmLogin.frx":030C
      TabIndex        =   12
      Top             =   2010
      Visible         =   0   'False
      Width           =   1785
   End
   Begin VB.ComboBox CmbBranchs 
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   9.75
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   360
      Left            =   2790
      Style           =   2  'Dropdown List
      TabIndex        =   0
      Top             =   1470
      Width           =   2355
   End
   Begin VB.TextBox txtPassword 
      Appearance      =   0  'Flat
      BeginProperty Font 
         Name            =   "Arial Black"
         Size            =   12
         Charset         =   0
         Weight          =   900
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   465
      IMEMode         =   3  'DISABLE
      Left            =   2790
      PasswordChar    =   "#"
      TabIndex        =   3
      Top             =   2850
      Width           =   2325
   End
   Begin VB.CommandButton cmdCancel 
      BackColor       =   &H00FF8080&
      Cancel          =   -1  'True
      Caption         =   "Cancel"
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   9.75
         Charset         =   0
         Weight          =   700
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   480
      Left            =   4020
      Style           =   1  'Graphical
      TabIndex        =   5
      Top             =   3420
      Width           =   1080
   End
   Begin VB.CommandButton cmdOK 
      BackColor       =   &H00FF8080&
      Caption         =   "OK"
      Default         =   -1  'True
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   9.75
         Charset         =   0
         Weight          =   700
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   480
      Left            =   2790
      Style           =   1  'Graphical
      TabIndex        =   4
      Top             =   3420
      Width           =   1080
   End
   Begin VB.TextBox txtUserName 
      Appearance      =   0  'Flat
      BeginProperty Font 
         Name            =   "Georgia"
         Size            =   12
         Charset         =   0
         Weight          =   700
         Underline       =   0   'False
         Italic          =   -1  'True
         Strikethrough   =   0   'False
      EndProperty
      Height          =   390
      Left            =   2790
      TabIndex        =   2
      Top             =   2370
      Width           =   2325
   End
   Begin VB.PictureBox Picture1 
      Appearance      =   0  'Flat
      BackColor       =   &H80000005&
      BorderStyle     =   0  'None
      ForeColor       =   &H80000008&
      Height          =   1365
      Left            =   0
      Picture         =   "frmLogin.frx":030E
      ScaleHeight     =   1365
      ScaleWidth      =   7155
      TabIndex        =   6
      Top             =   0
      Width           =   7155
      Begin VB.PictureBox MyLock 
         Appearance      =   0  'Flat
         AutoSize        =   -1  'True
         BackColor       =   &H80000005&
         BorderStyle     =   0  'None
         DragMode        =   1  'Automatic
         ForeColor       =   &H80000008&
         Height          =   660
         Left            =   5610
         Picture         =   "frmLogin.frx":20602
         ScaleHeight     =   660
         ScaleWidth      =   690
         TabIndex        =   9
         Top             =   240
         Width           =   690
      End
   End
   Begin MSComCtl2.DTPicker DTPicker2 
      Height          =   375
      Left            =   210
      TabIndex        =   15
      Top             =   3150
      Visible         =   0   'False
      Width           =   1545
      _ExtentX        =   2725
      _ExtentY        =   661
      _Version        =   393216
      Format          =   130744321
      CurrentDate     =   41262
   End
   Begin VB.Line Line2 
      BorderColor     =   &H00FFFFFF&
      X1              =   0
      X2              =   6675.905
      Y1              =   2676.473
      Y2              =   2676.473
   End
   Begin VB.Label LblExpirMsg 
      Alignment       =   2  'Center
      AutoSize        =   -1  'True
      BackStyle       =   0  'Transparent
      Caption         =   "Label1"
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   9.75
         Charset         =   0
         Weight          =   700
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   240
      Left            =   120
      TabIndex        =   21
      Top             =   4620
      Width           =   6870
   End
   Begin VB.Line Line1 
      BorderColor     =   &H00FFFFFF&
      X1              =   0
      X2              =   6675.905
      Y1              =   0
      Y2              =   0
   End
   Begin VB.Label Label1 
      AutoSize        =   -1  'True
      BackStyle       =   0  'Transparent
      Caption         =   "Label1"
      Height          =   195
      Left            =   5340
      TabIndex        =   20
      Top             =   1590
      Visible         =   0   'False
      Width           =   480
   End
   Begin VB.Label LblMsg 
      Alignment       =   2  'Center
      BackColor       =   &H00FF8080&
      Caption         =   "Label1"
      BeginProperty Font 
         Name            =   "Arial Black"
         Size            =   20.25
         Charset         =   0
         Weight          =   900
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      ForeColor       =   &H00FFFFFF&
      Height          =   525
      Left            =   0
      TabIndex        =   19
      Top             =   3990
      Width           =   7125
   End
   Begin VB.Label lblLabels 
      BackStyle       =   0  'Transparent
      Caption         =   "Acd. Year"
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   9.75
         Charset         =   0
         Weight          =   700
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   270
      Index           =   3
      Left            =   1530
      TabIndex        =   18
      Top             =   1965
      Width           =   1080
   End
   Begin VB.Label networkipaddresslBl 
      AutoSize        =   -1  'True
      BackStyle       =   0  'Transparent
      Caption         =   "0"
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   8.25
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   195
      Left            =   900
      TabIndex        =   17
      Top             =   1860
      Visible         =   0   'False
      Width           =   105
   End
   Begin VB.Label LblSys 
      AutoSize        =   -1  'True
      BackStyle       =   0  'Transparent
      Caption         =   "0"
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   8.25
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   195
      Left            =   60
      TabIndex        =   16
      Top             =   1470
      Visible         =   0   'False
      Width           =   105
   End
   Begin VB.Label LblMac 
      AutoSize        =   -1  'True
      BackStyle       =   0  'Transparent
      Caption         =   "0"
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   8.25
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   195
      Left            =   90
      TabIndex        =   13
      Top             =   3330
      Visible         =   0   'False
      Width           =   105
   End
   Begin VB.Label LblBranchID 
      AutoSize        =   -1  'True
      BackStyle       =   0  'Transparent
      Caption         =   "0"
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   8.25
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   195
      Left            =   90
      TabIndex        =   11
      Top             =   3540
      Visible         =   0   'False
      Width           =   105
   End
   Begin VB.Label lblLabels 
      AutoSize        =   -1  'True
      BackStyle       =   0  'Transparent
      Caption         =   "Institute"
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   9.75
         Charset         =   0
         Weight          =   700
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   240
      Index           =   2
      Left            =   1530
      TabIndex        =   10
      Top             =   1530
      Width           =   945
   End
   Begin VB.Label lblLabels 
      BackStyle       =   0  'Transparent
      Caption         =   "&Password:"
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   9.75
         Charset         =   0
         Weight          =   700
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   270
      Index           =   1
      Left            =   1530
      TabIndex        =   8
      Top             =   2910
      Width           =   1080
   End
   Begin VB.Label lblLabels 
      AutoSize        =   -1  'True
      BackStyle       =   0  'Transparent
      Caption         =   "&User Name"
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   9.75
         Charset         =   0
         Weight          =   700
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   240
      Index           =   0
      Left            =   1530
      TabIndex        =   7
      Top             =   2415
      Width           =   1185
   End
End
Attribute VB_Name = "frmLogin"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit
Dim GetDetails As New MyClass
Public LoginSucceeded As Boolean
Dim GetUSBSec As String
Dim Clsdata As New MyClass

Dim PurDat As Date
Dim ExpirDat As Date
Dim RnwalDys As Integer


Private Sub CmbAcdYear_KeyPress(KeyAscii As Integer)
If KeyAscii = 13 Then
    Sendkeys vbTab
End If
End Sub

Private Sub cmdCancel_Click()
    'set the global var to false
    'to denote a failed login
    LoginSucceeded = False
    Unload Me
End Sub

Private Sub cmdOk_Click()

 
'
'    txtUserName = "a"
'    txtPassword = "a"
'On Error GoTo errHandler

If InputVald = True Then
Label1.Caption = "InputVald"

        If LoginAuth = True Then
        Label1.Caption = "LoginAuth"
            If r.State = 1 Then r.Close
            r.CursorLocation = adUseClient
            r.Open "select * from SAS_UserMaster where UserName='" & txtUserName.text & "' and Userpwd='" & txtPassword.text & "' and UsrStatus='A' and BranchID='" & branchID & "'", Con, adOpenDynamic, adLockReadOnly
                
                If r.RecordCount > 0 Then
                
                    LoginSucceeded = True
                    'Call Regs
                    UsrID = r("userid")
                    UsrAth = r("AccessTyp")
'                    branchID = r("branchid")
                    UsrNam = txtUserName.text
                    DTPicker1 = Date
                    DTPicker2 = Date
                    'Call Regs
                    
                    DTPicker1.Day = 1
                    DTPicker1.Month = 1
                    DTPicker2.Month = 12
                    DTPicker2.Day = 31
                    
                    StartDat = DTPicker1
                    EndDat = DTPicker2
                
                    Con.Execute "update SAS_Acdyear set AcdStatus='D' where AcademicYear <> '" & CmbAcdYear.text & "'"
                    Con.Execute "update SAS_Acdyear set AcdStatus='A' where AcademicYear = '" & CmbAcdYear.text & "'"
                
                    If r.State = 1 Then r.Close
                    r.CursorLocation = adUseClient
                    r.Open "select * from SAS_Acdyear where AcdStatus='A' and AcademicYear='" & CmbAcdYear.text & "' ", Con, adOpenDynamic, adLockReadOnly
                        If r.RecordCount > 0 Then
                            AcdStatDat = r("StartMonth")
                            AcdEndDat = r("EndMonth")
                            AcdYear = r("AcademicYear")
                        End If
                    r.Close
                
                                         
                    If Res.State = 1 Then Res.Close
                    Res.CursorLocation = adUseClient
                    Res.Open "Select * from ClientSoftwareSettings", Cn, adOpenDynamic, adLockReadOnly
                        If Res.RecordCount > 0 Then
                            Acslck = Res("FeeReceiptsPrints")
                            SmsPortNo = Res("SmsPort")
                            SmsStat = Res("SmsStat")
                            SmsTyp = Res("SmsTyp")
                        End If
                    Res.Close
                
                    If Res.State = 1 Then Res.Close
                    Res.CursorLocation = adUseClient
                    Res.Open "Select * from SAS_SoftwareSettings", Con, adOpenDynamic, adLockReadOnly
                        If Res.RecordCount > 0 Then
                            FeeReceiptStat = Res("FeeReceiptPrint")
                            NofReceipts = Res("RecptNofPrnts")
                            PrntrMod = Res("PrintMod")
                            FineStat = Res("FineStat")
                            SmsPrvdr = Res("Smsprovdr")
                            TrnsInclud = Res("TransFeeInclud")
                            BilNoStatTyp = Res("BilNosStrts")
                            CameraName = Res("WebCamNam")
                            AdminSmsMobil = Res("TestngMobil")
                            PrntMod = Res("MiscPrntModl")
                            SaveWebCamSettings
                        End If
                    Res.Close
                
                    
                    If Res.State = 1 Then Res.Close
                    Res.CursorLocation = adUseClient
                    Res.Open "Select * from SAS_SMSGatewayInfo where TraStatus='A'", Con, adOpenDynamic, adLockReadOnly
                        If Res.RecordCount > 0 Then
                            SmsUrlStr = Res("SmsAPILink")
                            SmsTitl = Res("SmsSndrID")
                        End If
                    Res.Close
                    
                    
'                    MsgBox Now & " Application Date : " & traDate
                    Call UserAdd
                    
                    Unload Me
                    Mst_Main.Show
                Else
                    MsgBox "Invalid Password, try again!", , "Login"
                    txtPassword.SetFocus
                    'SendKeys "{Home}+{End}"
                End If
                
            End If
    End If

'End If
Exit Sub
ErrHandler:
MsgBox Err.Description
Resume Next

End Sub

Private Sub Form_Activate()
    txtUserName.SetFocus
End Sub

Private Sub Form_Load()

'Call ConOpen
'GetUSBDet

'Dim AccesDat As Date
'AccesDat = "25-May-19"
'If Date >= AccesDat Then
'    Con.Execute "Delete from SAS_AcdYear"
'End If

LblSys.Caption = SchName
networkipaddresslBl.Caption = GetIPAddress()
LblMac.Caption = GetMACs_AdaptInfo()
LblMac.Caption = ""
LblMac.Caption = MachineID
SysNam = LblSys.Caption
GetBranchDet
Label1.Caption = "GetBranchDet"

traDate = Date

LblBranchID.Caption = GetDetails.GetItmPrts("sas_licencedet", "BranchID", "FirmName='" & Replace(CmbBranchs.text, "'", "''") & "'")
branchID = LblBranchID.Caption
ExpirDat = Clsdata.GetFeildID("SAS_LicenceDet", "ExpDat", " BranchID='" & branchID & "'")

RnwalDys = DateDiff("d", Date, ExpirDat)
LblExpirMsg.Caption = "License : " & RnwalDys & " days remaining"

'traDate = ClsData.GetFeildID("SAS_TraDate", "TraDate", " TraStatus='A' and BranchID='" & branchID & "'")
Label1.Caption = "LblBranchID.Caption"
If r.State = 1 Then r.Close
r.CursorLocation = adUseClient
r.Open "Select TraDate from SAS_TraDate where TraStatus='A' and BranchID='" & branchID & "'", Con, adOpenDynamic, adLockReadOnly
    If r.RecordCount > 0 Then
        traDate = Format(r(0), "dd-MMM-yy")
    Else
        traDate = Date
        MsgBox "Please Make Day Close Again . . ."
    End If
r.Close
Label1.Caption = "SAS_TraDate"

CmbAcdYear.AddItem Clsdata.GetComboItems("SAS_AcdYear", "AcademicYear", " AcdStatus <>'C'", CmbAcdYear)
Label1.Caption = "CmbAcdYear.AddItem "
CmbAcdYear.text = Clsdata.GetFeildID("SAS_AcdYear", "AcademicYear", " AcdStatus='A'")
Label1.Caption = "CmbAcdYear.text "

 
LblMsg.Caption = Weekday(traDate)
 

Select Case DatePart("w", traDate)
    Case 1
        LblMsg.Caption = "Sunday  " & traDate
    Case 2
        LblMsg.Caption = "Monday  " & traDate
    Case 3
        LblMsg.Caption = "Tuesday  " & traDate
    Case 4
        LblMsg.Caption = "Wednesday  " & traDate
    Case 5
        LblMsg.Caption = "Thursday  " & traDate
    Case 6
        LblMsg.Caption = "Friday  " & traDate
    Case 7
        LblMsg.Caption = "Saturday  " & traDate
End Select


Label1.Caption = "LblMsg.Caption "
End Sub


Private Sub UserAdd()
Label1.Caption = "UserAdd"
Con.Execute "Insert Into SAS_Clicks values('" & UsrID & "','" & Now & "','" & MachineID & "')"
'If rs.State = 1 Then rs.Close
' rs.CursorLocation = adUseClient
' rs.Open "SAS_Clicks", Con, adOpenKeyset, adLockPessimistic
'     rs.AddNew
'         rs(0) = UsrID
'         rs(1) = Now
'         rs(2) = MachineID
'     rs.Update
' rs.Close
End Sub
Private Function LoginAuth() As Boolean
Dim BilngStat As String
Dim BilngAuth As String
            If r.State = 1 Then r.Close
            r.CursorLocation = adUseClient
            r.Open "select count(*) from SAS_Clicks", Con, adOpenDynamic, adLockReadOnly
            If Val(r.Fields(0)) > 10000 Then
                MsgBox "Access Denied, Please Cont: Ascent Info Solutions,9392123644"
                LoginAuth = False
                End
                Exit Function
            End If
'            If R.State = 1 Then R.Close
'            R.CursorLocation = adUseClient
'            R.Open "select DISTINCT(TRADAT) from DailyTrasactions", Con, adOpenDynamic, adLockPessimistic
'            If Val(R.RecordCount) > 365 Then
'                MsgBox "Access Denied, Please Cont: CAL-ON INSTRUEMNTS, 040 27261888,27260203"
'                LoginAuth = False
'                End
'                Exit Function
'            End If
'            If R.State = 1 Then R.Close
'            R.CursorLocation = adUseClient
'            R.Open "select *  from DailyTrasactions where TRADAT=cdate('15-Sep-11')", Con, adOpenDynamic, adLockPessimistic
'            If R.RecordCount > 0 Then
'                MsgBox "Access Denied, Please Cont: CAL-ON INSTRUEMNTS, 040 27261888,27260203"
'                LoginAuth = False
'                End
'                Exit Function
'            End If


If rs.State = 1 Then rs.Close
rs.CursorLocation = adUseClient
rs.Open "select * from SAS_SoftwareSettings", Con, adOpenDynamic, adLockReadOnly
    If rs.RecordCount > 0 Then
        BilngStat = rs("BillingStat")
    Else
        BilngStat = "Auto"
    End If
rs.Close

If BilngStat = "Manual" Then
    If r.State = 1 Then r.Close
    r.CursorLocation = adUseClient
    r.Open "Select * from SAS_TraDate where TraDate='" & traDate & "' and TraStatus='A'", Con, adOpenDynamic, adLockOptimistic
        If r.RecordCount > 0 Then
            BilngAuth = r("RemrksData")
        End If
    r.Close
End If

If r.State = 1 Then r.Close
r.CursorLocation = adUseClient
r.Open "select * from SAS_UserMaster where UserName='" & txtUserName.text & "' and Userpwd='" & txtPassword.text & "' and UsrStatus='A' and BranchID='" & branchID & "'", Con, adOpenDynamic, adLockReadOnly
    If r.RecordCount > 0 Then
        UsrID = r("userid")
        UsrAth = r("AccessTyp")
    End If
r.Close
            
            
If UsrAth = "Billing User" Then
    If BilngAuth = "No" Then
        MsgBox "Access Denied, Please Cont: Administrator"
        LoginAuth = False
        Exit Function
    End If
End If
LoginAuth = True
End Function

 

Private Sub txtPassword_KeyPress(KeyAscii As Integer)
If KeyAscii = 13 Then
    Sendkeys vbTab
End If
End Sub


Private Sub txtUserName_KeyPress(KeyAscii As Integer)
If KeyAscii = 13 Then
    txtPassword.SetFocus
End If
End Sub

Private Function InputVald() As Boolean
'GetUSBDet
If txtUserName.text = "" Then
    MsgBox "Enter User Name"
    txtUserName.SetFocus
    InputVald = False
    Exit Function
End If

If CmbAcdYear.text = "" Then
    MsgBox "Select Academic Year"
    CmbAcdYear.SetFocus
    InputVald = False
    Exit Function
End If

If txtPassword.text = "" Then
    txtPassword.SetFocus
    InputVald = False
    Exit Function
End If

'Call USBSecurt
'Dim i As Integer
'For i = 0 To List1.ListCount
 '   If USBSecurity(0) = List1.List(i) Then
  '      InputVald = True
   '     GetUSBSec = "Yes"
    '    Exit For
    'Else
     '   GetUSBSec = "No"
    'End If
'Next i

'If GetUSBSec = "No" Then
'    MsgBox "Plz Insert CPBS USB Security Device"
'    Exit Function
'    InputVald = False
'End If

'Call Regs

''If LblMac.Caption <> USBSecurity(0) Then
''    MsgBox "Authentication Failed, Please Contact Vendor", vbCritical
''    InputVald = False
''    Exit Function
''End If

If RnwalDys <= 0 Then
    MsgBox "Access Denied, Please Cont: your Vendor- 9392123644 or skmushkin@gmail.com"
'        SendMail
    InputVald = False
    Exit Function
End If
    
If CDate(Date) <> CDate(traDate) Then
'    MsgBox "System Date Not Matched with Fee Software Billing Date, Please Check it Once . . . ?", vbCritical
End If
LblBranchID.Caption = GetDetails.GetItmPrts("sas_licencedet", "BranchID", "FirmName='" & Replace(CmbBranchs.text, "'", "''") & "'")
branchID = LblBranchID.Caption
InputVald = True
End Function

Private Sub GetUserDet()
If rs.State = 1 Then rs.Close
rs.CursorLocation = adUseClient
rs.Open "select * from CustomerDet", Con, adOpenDynamic, adLockPessimistic
    If rs.RecordCount > 0 Then
        Names(0) = rs(0)
    End If
rs.Close
End Sub

Private Sub GetUSBDet()
List1.Clear
Dim obj, objs, buf, PnPID
Dim UsbNo As String
' Get the PnPDevice ID
Set objs = GetObject("winmgmts:").InstancesOf("Win32_DiskDrive")
For Each obj In objs
     If obj.InterfaceType = "USB" Then
          'buf = "Model: " & obj.Model & vbcr
          'buf = buf & "PnP Device ID: " & obj.PnPDeviceID
            PnPID = obj.PnPDeviceID
            UsbNo = Split(PnPID, "\")(2)
            UsbNo = Split(UsbNo, "&")(0)
            
            List1.AddItem UsbNo
     End If
Next

' fix up the PnPDevice ID to make it suitable for comparing against the Association
PnPID = Replace(PnPID, "\", "\\") & Chr(34)

' Use WMI associations to pair up USBContoller and PnPEntity
Set objs = GetObject("winmgmts:").InstancesOf("Win32_USBControllerDevice")
For Each obj In objs
     If Right(obj.Dependent, Len(PnPID)) = PnPID Then
          'MsgBox (PnPID & vbCr & obj.Dependent & vbCr & obj.Antecedent)
     End If
Next
End Sub


Private Sub GetBranchDet()
CmbBranchs.Clear
If r.State = 1 Then r.Close
r.CursorLocation = adUseClient
r.Open "select * from SAS_LicenceDet", Con, adOpenDynamic, adLockReadOnly
If r.RecordCount > 0 Then
    FtTyp = r("FeeTyp") 'FeeTyp like  Monthly / Terms
    StrAPIkey = ""
    While r.EOF = False
        'CmbBranchs.AddItem Trim(r("FirmName"))
        CmbBranchs.AddItem r("FirmName")
        r.MoveNext
    Wend
End If
r.Close
CmbBranchs.ListIndex = 0
End Sub

Private Sub SaveWebCamSettings()
    Dim F As Integer
    Dim c As Integer
    
    F = FreeFile(0)
    Open "Settings.txt" For Output As #F
    Write #F, "0"
    Write #F, "" & CameraName & ""

    Close #F
End Sub


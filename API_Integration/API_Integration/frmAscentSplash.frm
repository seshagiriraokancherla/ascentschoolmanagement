VERSION 5.00
Begin VB.Form frmAscentSplash 
   Appearance      =   0  'Flat
   BackColor       =   &H80000005&
   BorderStyle     =   0  'None
   Caption         =   " "
   ClientHeight    =   4245
   ClientLeft      =   210
   ClientTop       =   1365
   ClientWidth     =   7380
   ClipControls    =   0   'False
   ControlBox      =   0   'False
   Icon            =   "frmAscentSplash.frx":0000
   KeyPreview      =   -1  'True
   LinkTopic       =   "Form2"
   MaxButton       =   0   'False
   MinButton       =   0   'False
   ScaleHeight     =   4245
   ScaleWidth      =   7380
   ShowInTaskbar   =   0   'False
   StartUpPosition =   2  'CenterScreen
   Begin VB.Frame Frame1 
      Height          =   4050
      Left            =   150
      TabIndex        =   0
      Top             =   60
      Width           =   7080
      Begin VB.Timer Timer2 
         Enabled         =   0   'False
         Interval        =   20
         Left            =   1890
         Top             =   1230
      End
      Begin VB.Timer Timer1 
         Interval        =   2000
         Left            =   750
         Top             =   870
      End
      Begin VB.Image imgLogo 
         Height          =   1815
         Left            =   330
         Picture         =   "frmAscentSplash.frx":000C
         Stretch         =   -1  'True
         Top             =   1800
         Width           =   2985
      End
      Begin VB.Label lblCopyright 
         BackStyle       =   0  'Transparent
         Caption         =   "Copyright"
         BeginProperty Font 
            Name            =   "Arial"
            Size            =   8.25
            Charset         =   0
            Weight          =   400
            Underline       =   0   'False
            Italic          =   0   'False
            Strikethrough   =   0   'False
         EndProperty
         Height          =   255
         Left            =   4560
         TabIndex        =   4
         Top             =   3060
         Visible         =   0   'False
         Width           =   2415
      End
      Begin VB.Label lblCompany 
         Alignment       =   1  'Right Justify
         BackStyle       =   0  'Transparent
         Caption         =   "Company"
         BeginProperty Font 
            Name            =   "Verdana"
            Size            =   9.75
            Charset         =   0
            Weight          =   700
            Underline       =   0   'False
            Italic          =   0   'False
            Strikethrough   =   0   'False
         EndProperty
         ForeColor       =   &H0000C000&
         Height          =   255
         Left            =   1830
         TabIndex        =   3
         Top             =   390
         Width           =   5175
      End
      Begin VB.Label lblWarning 
         AutoSize        =   -1  'True
         BackStyle       =   0  'Transparent
         Caption         =   "Warning"
         BeginProperty Font 
            Name            =   "Comic Sans MS"
            Size            =   8.25
            Charset         =   0
            Weight          =   400
            Underline       =   0   'False
            Italic          =   0   'False
            Strikethrough   =   0   'False
         EndProperty
         ForeColor       =   &H000000C0&
         Height          =   225
         Left            =   150
         TabIndex        =   2
         Top             =   3660
         Width           =   675
      End
      Begin VB.Label lblVersion 
         Alignment       =   1  'Right Justify
         AutoSize        =   -1  'True
         BackStyle       =   0  'Transparent
         Caption         =   "Version"
         BeginProperty Font 
            Name            =   "Arial"
            Size            =   9
            Charset         =   0
            Weight          =   700
            Underline       =   0   'False
            Italic          =   0   'False
            Strikethrough   =   0   'False
         EndProperty
         Height          =   225
         Left            =   6315
         TabIndex        =   5
         Top             =   3660
         Width           =   660
      End
      Begin VB.Label lblProductName 
         Alignment       =   2  'Center
         AutoSize        =   -1  'True
         BackStyle       =   0  'Transparent
         Caption         =   "Product"
         BeginProperty Font 
            Name            =   "Haettenschweiler"
            Size            =   32.25
            Charset         =   0
            Weight          =   400
            Underline       =   0   'False
            Italic          =   0   'False
            Strikethrough   =   0   'False
         EndProperty
         ForeColor       =   &H000040C0&
         Height          =   660
         Left            =   60
         TabIndex        =   7
         Top             =   1080
         Width           =   6960
      End
      Begin VB.Label lblLicenseTo 
         Alignment       =   1  'Right Justify
         BackStyle       =   0  'Transparent
         Caption         =   "LicenseTo"
         BeginProperty Font 
            Name            =   "Arial"
            Size            =   9
            Charset         =   0
            Weight          =   400
            Underline       =   0   'False
            Italic          =   0   'False
            Strikethrough   =   0   'False
         EndProperty
         Height          =   255
         Left            =   120
         TabIndex        =   1
         Top             =   180
         Width           =   6855
      End
      Begin VB.Label lblCompanyProduct 
         Alignment       =   2  'Center
         AutoSize        =   -1  'True
         BackStyle       =   0  'Transparent
         Caption         =   "ASCENT INFO SOLUTIONS"
         BeginProperty Font 
            Name            =   "Franklin Gothic Medium"
            Size            =   18
            Charset         =   0
            Weight          =   700
            Underline       =   0   'False
            Italic          =   0   'False
            Strikethrough   =   0   'False
         EndProperty
         ForeColor       =   &H00C00000&
         Height          =   450
         Left            =   90
         TabIndex        =   6
         Top             =   720
         Width           =   6945
      End
   End
End
Attribute VB_Name = "frmAscentSplash"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False


Option Explicit



Private Declare Function Desktop_Show_Hide Lib "WinLockDll.dll" (ByVal bSByValhowHide As Boolean) As Integer
Private Declare Function StartButton_Show_Hide Lib "WinLockDll.dll" (bShowHide As Boolean) As Integer
Private Declare Function Taskbar_Show_Hide Lib "WinLockDll.dll" (ByVal bShowHide As Boolean) As Integer
Private Declare Function Clock_Show_Hide Lib "WinLockDll.dll" (ByVal bShowHide As Boolean) As Integer
Private Declare Function Process_Desktop Lib "WinLockDll.dll" (ByVal szDesktopName As String, ByVal szPath As String) As Integer
Private Declare Function Keys_Enable_Disable Lib "WinLockDll.dll" (ByVal bEnableDisable As Boolean) As Integer
Private Declare Function AltTab1_Enable_Disable Lib "WinLockDll.dll" (ByVal bEnableDisable As Boolean) As Integer
Private Declare Function AltTab2_Enable_Disable Lib "WinLockDll.dll" (ByVal hwnd As Long, ByVal bEnableDisable As Boolean) As Integer
Private Declare Function TaskSwitching_Enable_Disable Lib "WinLockDll.dll" (ByVal bEnableDisable As Boolean) As Integer
Private Declare Function TaskManager_Enable_Disable Lib "WinLockDll.dll" (ByVal bEnableDisable As Boolean) As Integer
Private Declare Function CtrlAltDel_Enable_Disable Lib "WinLockDll.dll" (ByVal bEnableDisable As Boolean) As Integer

Private Sub Form_KeyPress(KeyAscii As Integer)
frmLogin.Show
    Unload Me
End Sub
Private Sub Form_Load()
Call ConOpen
Call Regs
'Taskbar_Show_Hide (False)
Frame1.BackColor = Me.BackColor
    lblVersion.Caption = "Version " & App.Major & "." & App.Minor & "." & App.Revision
    lblProductName.Caption = App.Title
lblWarning.Caption = "This product is licensed to " & Names(0) & ". Any illegal usage is restricted"
    lblCompany.Caption = SchName
'TaskSwitching_Enable_Disable (False)
End Sub

Private Sub Frame1_Click()
    Unload Me
End Sub

Private Sub Timer1_Timer()
Timer1.Enabled = False
Timer2.Enabled = True
End Sub

Private Sub Timer2_Timer()
Me.Width = Me.Width - 300
If Me.Width <= 1000 Then
    Timer2.Enabled = False
    Unload Me
   frmLogin.Show
End If
End Sub

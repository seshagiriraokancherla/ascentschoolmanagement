VERSION 5.00
Object = "{5E9E78A0-531B-11CF-91F6-C2863C385E30}#1.0#0"; "MSFLXGRD.OCX"
Object = "{86CF1D34-0C5F-11D2-A9FC-0000F8754DA1}#2.0#0"; "MSCOMCT2.OCX"
Object = "{00025600-0000-0000-C000-000000000046}#5.2#0"; "Crystl32.OCX"
Begin VB.Form Tra_ImportReceipts 
   Caption         =   "Import Fee Receipts . . ."
   ClientHeight    =   12495
   ClientLeft      =   60
   ClientTop       =   405
   ClientWidth     =   22920
   BeginProperty Font 
      Name            =   "Arial"
      Size            =   12
      Charset         =   0
      Weight          =   400
      Underline       =   0   'False
      Italic          =   0   'False
      Strikethrough   =   0   'False
   EndProperty
   LinkTopic       =   "Form1"
   MDIChild        =   -1  'True
   ScaleHeight     =   12495
   ScaleWidth      =   22920
   WindowState     =   2  'Maximized
   Begin VB.Frame Frame1 
      Caption         =   "Date Range"
      Height          =   855
      Left            =   930
      TabIndex        =   4
      Top             =   780
      Width           =   4605
      Begin MSComCtl2.DTPicker DTPfrm 
         Height          =   405
         Left            =   750
         TabIndex        =   5
         Top             =   300
         Width           =   1575
         _ExtentX        =   2778
         _ExtentY        =   714
         _Version        =   393216
         BeginProperty Font {0BE35203-8F91-11CE-9DE3-00AA004BB851} 
            Name            =   "Arial"
            Size            =   12
            Charset         =   0
            Weight          =   400
            Underline       =   0   'False
            Italic          =   0   'False
            Strikethrough   =   0   'False
         EndProperty
         CustomFormat    =   "dd-MMM-yy"
         Format          =   260112387
         CurrentDate     =   41622
      End
      Begin MSComCtl2.DTPicker DTPTo 
         Height          =   405
         Left            =   2940
         TabIndex        =   6
         Top             =   300
         Width           =   1545
         _ExtentX        =   2725
         _ExtentY        =   714
         _Version        =   393216
         BeginProperty Font {0BE35203-8F91-11CE-9DE3-00AA004BB851} 
            Name            =   "Arial"
            Size            =   12
            Charset         =   0
            Weight          =   400
            Underline       =   0   'False
            Italic          =   0   'False
            Strikethrough   =   0   'False
         EndProperty
         CustomFormat    =   "dd-MMM-yy"
         Format          =   260112387
         CurrentDate     =   41622
      End
      Begin VB.Label Label2 
         AutoSize        =   -1  'True
         BackStyle       =   0  'Transparent
         Caption         =   "To"
         BeginProperty Font 
            Name            =   "Arial"
            Size            =   11.25
            Charset         =   0
            Weight          =   400
            Underline       =   0   'False
            Italic          =   0   'False
            Strikethrough   =   0   'False
         EndProperty
         Height          =   255
         Left            =   2430
         TabIndex        =   8
         Top             =   360
         Width           =   255
      End
      Begin VB.Label Label1 
         AutoSize        =   -1  'True
         BackStyle       =   0  'Transparent
         Caption         =   "From"
         BeginProperty Font 
            Name            =   "Arial"
            Size            =   11.25
            Charset         =   0
            Weight          =   400
            Underline       =   0   'False
            Italic          =   0   'False
            Strikethrough   =   0   'False
         EndProperty
         Height          =   255
         Left            =   150
         TabIndex        =   7
         Top             =   360
         Width           =   525
      End
   End
   Begin VB.CommandButton BtnShow 
      BackColor       =   &H00FFFFFF&
      Caption         =   "&Show"
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   9
         Charset         =   0
         Weight          =   700
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   1155
      Left            =   5160
      Picture         =   "Tra_ImportReceipts.frx":0000
      Style           =   1  'Graphical
      TabIndex        =   3
      Top             =   6480
      Width           =   1125
   End
   Begin VB.CommandButton Btnprint 
      BackColor       =   &H00FFFFFF&
      Caption         =   "S &A V E"
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   9
         Charset         =   0
         Weight          =   700
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   1185
      Left            =   3300
      Picture         =   "Tra_ImportReceipts.frx":067E
      Style           =   1  'Graphical
      TabIndex        =   2
      Top             =   6690
      Width           =   1125
   End
   Begin VB.CommandButton BtnBack 
      BackColor       =   &H00FFFFFF&
      Cancel          =   -1  'True
      Caption         =   "&Back"
      BeginProperty Font 
         Name            =   "Verdana"
         Size            =   9
         Charset         =   0
         Weight          =   700
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   1185
      Left            =   8040
      MaskColor       =   &H00C0E0FF&
      Picture         =   "Tra_ImportReceipts.frx":0E87
      Style           =   1  'Graphical
      TabIndex        =   1
      Top             =   0
      Width           =   1125
   End
   Begin VB.ComboBox CmbAcdYear 
      Height          =   390
      Left            =   5640
      Style           =   2  'Dropdown List
      TabIndex        =   0
      Top             =   1170
      Visible         =   0   'False
      Width           =   1995
   End
   Begin MSFlexGridLib.MSFlexGrid MSFlexGrid1 
      Height          =   5355
      Left            =   900
      TabIndex        =   9
      Top             =   1680
      Width           =   12375
      _ExtentX        =   21828
      _ExtentY        =   9446
      _Version        =   393216
      RowHeightMin    =   450
      BackColorFixed  =   16761024
      BackColorBkg    =   16777215
      FocusRect       =   0
      Appearance      =   0
      BeginProperty Font {0BE35203-8F91-11CE-9DE3-00AA004BB851} 
         Name            =   "Arial"
         Size            =   12
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
   End
   Begin Crystal.CrystalReport CrystalReport1 
      Left            =   0
      Top             =   30
      _ExtentX        =   741
      _ExtentY        =   741
      _Version        =   348160
      PrintFileLinesPerPage=   60
   End
   Begin VB.Label LblMsg 
      AutoSize        =   -1  'True
      BackStyle       =   0  'Transparent
      BeginProperty Font 
         Name            =   "Arial"
         Size            =   20.25
         Charset         =   0
         Weight          =   700
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      ForeColor       =   &H000000FF&
      Height          =   480
      Left            =   1080
      TabIndex        =   11
      Top             =   120
      Width           =   120
   End
   Begin VB.Label Label5 
      AutoSize        =   -1  'True
      BackStyle       =   0  'Transparent
      Caption         =   "Academic Year"
      BeginProperty Font 
         Name            =   "Arial"
         Size            =   12
         Charset         =   0
         Weight          =   700
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   285
      Left            =   5670
      TabIndex        =   10
      Top             =   870
      Visible         =   0   'False
      Width           =   1695
   End
End
Attribute VB_Name = "Tra_ImportReceipts"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Dim Clsdata As New MyClass
Private Sub BtnBack_Click()
    Unload Me
End Sub

Private Sub Form_Load()
Frame1.BackColor = Mst_Main.BackColor
DTPfrm = Date
DTPTo = Date
DTPfrm.Day = 1
DTPTo = DateAdd("M", 1, DTPfrm)
DTPTo = DateAdd("d", -1, DTPTo)
LoadButtons
MshPos
End Sub

Private Sub LoadButtons()
BtnShow.Top = 7300
BtnShow.Left = 1500

Btnprint.Top = 7300
Btnprint.Left = 6500

BtnBack.Top = 7300
BtnBack.Left = 11580
End Sub


Private Sub GetData()

End Sub

Private Sub MshPos()
With MSFlexGrid1
    .Clear
    .Rows = 2
    .Cols = 10
    .TextMatrix(0, 0) = "Sl No"
    .TextMatrix(0, 1) = "Date"
    .TextMatrix(0, 2) = "Bill No"
    .TextMatrix(0, 3) = "Admin No"
    .TextMatrix(0, 4) = "Student Name"
    .TextMatrix(0, 5) = "Class"
    .TextMatrix(0, 6) = "Sec"
    .TextMatrix(0, 7) = "Term"
    .TextMatrix(0, 8) = "Amount"
    .TextMatrix(0, 9) = "Ref No"
End With
End Sub



VERSION 5.00
Begin VB.UserControl addrCntl 
   ClientHeight    =   2325
   ClientLeft      =   0
   ClientTop       =   0
   ClientWidth     =   5370
   ScaleHeight     =   2325
   ScaleWidth      =   5370
   Begin VB.TextBox a4 
      Appearance      =   0  'Flat
      Height          =   300
      Left            =   735
      MaxLength       =   50
      TabIndex        =   3
      Top             =   1920
      Width           =   2115
   End
   Begin VB.TextBox a9 
      Appearance      =   0  'Flat
      Height          =   300
      Left            =   3495
      MaxLength       =   50
      TabIndex        =   8
      Top             =   1920
      Width           =   1815
   End
   Begin VB.TextBox a8 
      Appearance      =   0  'Flat
      Height          =   300
      Left            =   3495
      MaxLength       =   50
      TabIndex        =   7
      Top             =   1470
      Width           =   1815
   End
   Begin VB.TextBox a7 
      Appearance      =   0  'Flat
      Height          =   300
      Left            =   3495
      MaxLength       =   50
      TabIndex        =   6
      Top             =   1020
      Width           =   1815
   End
   Begin VB.TextBox a6 
      Appearance      =   0  'Flat
      Height          =   300
      Left            =   3480
      MaxLength       =   50
      TabIndex        =   5
      Top             =   570
      Width           =   1815
   End
   Begin VB.TextBox a5 
      Appearance      =   0  'Flat
      Height          =   300
      Left            =   3495
      MaxLength       =   50
      TabIndex        =   4
      Top             =   120
      Width           =   1815
   End
   Begin VB.TextBox a1 
      Appearance      =   0  'Flat
      BeginProperty Font 
         Name            =   "Arial"
         Size            =   8.25
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   735
      Left            =   735
      MaxLength       =   250
      MultiLine       =   -1  'True
      TabIndex        =   0
      Top             =   120
      Width           =   2115
   End
   Begin VB.ComboBox a2 
      Appearance      =   0  'Flat
      Height          =   315
      Left            =   735
      Sorted          =   -1  'True
      Style           =   2  'Dropdown List
      TabIndex        =   1
      Top             =   960
      Width           =   2175
   End
   Begin VB.ComboBox a3 
      Appearance      =   0  'Flat
      Height          =   315
      Left            =   735
      Sorted          =   -1  'True
      Style           =   2  'Dropdown List
      TabIndex        =   2
      Top             =   1440
      Width           =   2175
   End
   Begin VB.Label Label1 
      AutoSize        =   -1  'True
      Caption         =   "web"
      Height          =   195
      Index           =   11
      Left            =   3165
      TabIndex        =   17
      Top             =   1980
      Width           =   300
   End
   Begin VB.Label Label1 
      AutoSize        =   -1  'True
      Caption         =   "State"
      Height          =   195
      Index           =   9
      Left            =   330
      TabIndex        =   16
      Top             =   1020
      Width           =   375
   End
   Begin VB.Label Label1 
      AutoSize        =   -1  'True
      Caption         =   "email"
      Height          =   195
      Index           =   8
      Left            =   3105
      TabIndex        =   15
      Top             =   1530
      Width           =   360
   End
   Begin VB.Label Label1 
      AutoSize        =   -1  'True
      Caption         =   "District"
      Height          =   195
      Index           =   7
      Left            =   225
      TabIndex        =   14
      Top             =   1500
      Width           =   480
   End
   Begin VB.Label Label1 
      AutoSize        =   -1  'True
      Caption         =   "City"
      Height          =   195
      Index           =   6
      Left            =   450
      TabIndex        =   13
      Top             =   1980
      Width           =   255
   End
   Begin VB.Label Label1 
      AutoSize        =   -1  'True
      Caption         =   "Fax"
      Height          =   195
      Index           =   5
      Left            =   3210
      TabIndex        =   12
      Top             =   1080
      Width           =   255
   End
   Begin VB.Label Label1 
      AutoSize        =   -1  'True
      Caption         =   "Tel"
      Height          =   195
      Index           =   4
      Left            =   3240
      TabIndex        =   11
      Top             =   630
      Width           =   225
   End
   Begin VB.Label Label1 
      AutoSize        =   -1  'True
      Caption         =   "ZIP"
      Height          =   195
      Index           =   3
      Left            =   3210
      TabIndex        =   10
      Top             =   180
      Width           =   255
   End
   Begin VB.Label Label1 
      AutoSize        =   -1  'True
      Caption         =   "Address"
      Height          =   195
      Index           =   2
      Left            =   135
      TabIndex        =   9
      Top             =   120
      Width           =   570
   End
End
Attribute VB_Name = "addrCntl"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
Private Sub a1_KeyPress(KeyAscii As Integer)
x = KeyAscii
If x = 13 And enterChk(KeyAscii) = 0 Then a2.SetFocus
KeyAscii = enterChk(KeyAscii)
KeyAscii = removeSQ(KeyAscii)
End Sub
Private Sub a2_click()
On Error GoTo errHandler
a3.Clear
Dim r As New ADODB.Recordset
r.Open "select * from misstatemaster where cntname = '" & Trim(a2.Text) & "'", db, adOpenDynamic, adLockPessimistic
While r.EOF = False
    a3.AddItem r.Fields(1)
    r.MoveNext
Wend

Exit Sub
errHandler:
a3.Clear
End Sub

Private Sub a2_KeyPress(KeyAscii As Integer)
If KeyAscii = 13 Then a3.SetFocus
End Sub
Private Sub a3_KeyPress(KeyAscii As Integer)
If KeyAscii = 13 Then a4.SetFocus
End Sub
Private Sub a4_KeyPress(KeyAscii As Integer)
If KeyAscii = 13 Then a5.SetFocus
End Sub
Private Sub a5_KeyPress(KeyAscii As Integer)
If KeyAscii = 13 Then a6.SetFocus
End Sub
Private Sub a6_KeyPress(KeyAscii As Integer)
If KeyAscii = 13 Then a7.SetFocus
End Sub
Private Sub a7_KeyPress(KeyAscii As Integer)
If KeyAscii = 13 Then a8.SetFocus
End Sub
Private Sub a8_KeyPress(KeyAscii As Integer)
If KeyAscii = 13 Then a9.SetFocus
End Sub


Private Sub UserControl_Initialize()
On Error GoTo errHandler
UserControl.Width = 5370
UserControl.Height = 2325
Call countryADD

errHandler:
End Sub

Private Sub UserControl_Resize()
UserControl.Width = 5370
UserControl.Height = 2325
End Sub
Public Function address() As String
address = IIf(Trim(a1.Text) = "", " ", Trim(a1.Text))
End Function
Public Function country() As String
country = IIf(Trim(a2.Text) = "", " ", Trim(a2.Text))
End Function
Public Function state() As String
state = IIf(Trim(a3.Text) = "", " ", Trim(a3.Text))
End Function
Public Function city() As String
city = IIf(Trim(a4.Text) = "", " ", Trim(a4.Text))
End Function


Public Function pinCode() As String
pinCode = IIf(Trim(a5.Text) = "", " ", Trim(a5.Text))
End Function
Public Function telePhone() As String
telePhone = IIf(Trim(a6.Text) = "", " ", Trim(a6.Text))
End Function
Public Function fax() As String
fax = IIf(Trim(a7.Text) = "", " ", Trim(a7.Text))
End Function
Public Function emailID() As String
emailID = IIf(Trim(a8.Text) = "", " ", Trim(a8.Text))
End Function
Public Function webID() As String
webID = IIf(Trim(a9.Text) = "", " ", Trim(a9.Text))
End Function

Public Sub addrCle()
a1.Text = ""
a2.ListIndex = -1
a3.ListIndex = -1
a4.Text = ""
a5.Text = ""
a6.Text = ""
a7.Text = ""
a8.Text = ""
a9.Text = ""
End Sub
Private Function enterChk(x As Integer) As Integer
On Error GoTo errHandler
Dim i As Integer
Dim n As Integer
n = 0
If x <> 13 Then
    enterChk = x
Else
    For i = 1 To Len(a1.Text)
        If Asc(Mid(a1.Text, i, 1)) = 13 Then
            n = n + 1
        End If
    Next i
    
    If n >= 2 Then
        enterChk = 0
    Else
        enterChk = x
    End If
End If
Exit Function
errHandler:
enterChk = x
End Function
Public Sub setAddress(s1 As String, s2 As String, s3 As String, s4 As String, s5 As String, s6 As String, s7 As String, s8 As String, s9 As String)
On Error GoTo errHandler
a1.Text = Trim(s1)
If Trim(s2) <> "" Then
    a2.Text = Trim(s2)
Else
    a2.ListIndex = -1
End If
If Trim(s3) <> "" Then
    a3.Text = Trim(s3)
Else
    a3.ListIndex = -1
End If
a4.Text = Trim(s4)
a5.Text = Trim(s5)
a6.Text = Trim(s6)
a7.Text = Trim(s7)
a8.Text = Trim(s8)
a9.Text = Trim(s9)
errHandler:
End Sub
Public Sub backCol(x As ColorConstants)
UserControl.BackColor = x
Label1(2).BackColor = x
Label1(3).BackColor = x
Label1(4).BackColor = x
Label1(5).BackColor = x
Label1(8).BackColor = x
Label1(9).BackColor = x
Label1(6).BackColor = x
Label1(7).BackColor = x
Label1(11).BackColor = x
End Sub
Public Sub countryADD()
Dim r As New ADODB.Recordset
a2.Clear
a3.Clear
r.Open "miscountrymaster", db, adOpenDynamic, adLockPessimistic
While r.EOF = False
    a2.AddItem r.Fields(0)
    r.MoveNext
Wend
End Sub
Public Sub makeRefresh()
Call countryADD
End Sub

Public Sub forecol(x As ColorConstants)
Label1(2).ForeColor = x
Label1(3).ForeColor = x
Label1(4).ForeColor = x
Label1(5).ForeColor = x
Label1(8).ForeColor = x
Label1(9).ForeColor = x
Label1(6).ForeColor = x
Label1(7).ForeColor = x
Label1(11).ForeColor = x
End Sub
Public Sub setFocu()
a1.SetFocus
End Sub

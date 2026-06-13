VERSION 5.00
Object = "{831FDD16-0C5C-11D2-A9FC-0000F8754DA1}#2.0#0"; "mscomctl.ocx"
Begin VB.MDIForm Mst_Main 
   BackColor       =   &H8000000C&
   Caption         =   "Customised Reports . . ."
   ClientHeight    =   10335
   ClientLeft      =   165
   ClientTop       =   510
   ClientWidth     =   19080
   Icon            =   "Mst_Main.frx":0000
   LinkTopic       =   "MDIForm1"
   StartUpPosition =   3  'Windows Default
   WindowState     =   2  'Maximized
   Begin MSComctlLib.ImageList ImageList1 
      Left            =   1980
      Top             =   3660
      _ExtentX        =   1005
      _ExtentY        =   1005
      BackColor       =   -2147483643
      ImageWidth      =   48
      ImageHeight     =   48
      MaskColor       =   12632256
      _Version        =   393216
      BeginProperty Images {2C247F25-8591-11D1-B16A-00C0F0283628} 
         NumListImages   =   7
         BeginProperty ListImage1 {2C247F27-8591-11D1-B16A-00C0F0283628} 
            Picture         =   "Mst_Main.frx":0ECA
            Key             =   ""
         EndProperty
         BeginProperty ListImage2 {2C247F27-8591-11D1-B16A-00C0F0283628} 
            Picture         =   "Mst_Main.frx":1629
            Key             =   ""
         EndProperty
         BeginProperty ListImage3 {2C247F27-8591-11D1-B16A-00C0F0283628} 
            Picture         =   "Mst_Main.frx":1CF3
            Key             =   ""
         EndProperty
         BeginProperty ListImage4 {2C247F27-8591-11D1-B16A-00C0F0283628} 
            Picture         =   "Mst_Main.frx":23E2
            Key             =   ""
         EndProperty
         BeginProperty ListImage5 {2C247F27-8591-11D1-B16A-00C0F0283628} 
            Picture         =   "Mst_Main.frx":49BC
            Key             =   ""
         EndProperty
         BeginProperty ListImage6 {2C247F27-8591-11D1-B16A-00C0F0283628} 
            Picture         =   "Mst_Main.frx":50DD
            Key             =   ""
         EndProperty
         BeginProperty ListImage7 {2C247F27-8591-11D1-B16A-00C0F0283628} 
            Picture         =   "Mst_Main.frx":58F6
            Key             =   ""
         EndProperty
      EndProperty
   End
   Begin EduCareReports.ACPRibbon ACPRibbon1 
      Align           =   1  'Align Top
      Height          =   1740
      Left            =   0
      TabIndex        =   0
      Top             =   0
      Width           =   19080
      _ExtentX        =   33655
      _ExtentY        =   3069
      BackColor       =   4210752
      ForeColor       =   -2147483630
   End
End
Attribute VB_Name = "Mst_Main"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
    
    
Dim Theme As Integer

Private Sub ACPRibbon1_ButtonClick(ByVal ID As String, ByVal Caption As String)
If ID = 1 Then
    '# Open a new Child Form
'    Set fchild = New ChildMDI
    
    
'    fchild.Show
'
'    '# Set Theme for new Child Form
'    fchild.Picture = ACPRibbon1.LoadBackground
'    fchild.BackColor = ACPRibbon1.BackColor
    '
    'Set fchild = New Mst_Ins_Items
    Tra_ImportReceipts.Show
    Tra_ImportReceipts.Picture = ACPRibbon1.LoadBackground
    Tra_ImportReceipts.BackColor = ACPRibbon1.BackColor
ElseIf ID = 4 Then
    
ElseIf ID = 8 Then
    
ElseIf ID = 9 Then
    
ElseIf ID = 10 Then
    End
ElseIf ID = 11 Then
    
End If
End Sub

Private Sub MDIForm_Load()
Call ConOpen
Call Regs
 Mst_Main.Caption = SchName
    
    'Mst_Main.Picture = LoadPicture(App.Path & "\WallPaper\2015-2016.jpg", 0, 0, picCapture.Width, picCapture.Height)

    Theme = 1
    
    '# SET Theme
    
    ACPRibbon1.Theme = Theme    ' 0 - Black
                                ' 1 - Blue
                                ' 2 - Silver
                            
    
    '# OPTIONAL - Load Background for Form.
    'Mst_Main.Picture = ACPRibbon1.LoadBackground
    
    '# OPTIONAL - Load Background for Form
    Mst_Main.BackColor = ACPRibbon1.BackColor
      Mst_Main.Caption = "Edu Care -- " & SchName & " -- Academic Year : " & AcdYear & " -- Date : " & Format(traDate, "dd-MMM-yy")
    '# Set ImageList to use for icons
    ACPRibbon1.ImageList = ImageList1
    
    '# Set Buttons on Center verticaly    (True = Center, False(Default) = Align on Top)
    ACPRibbon1.ButtonCenter = False
    
    '# Add Tabs ---   ID - Caption
    ACPRibbon1.AddTab "1", "Customised Reports"
     
    '# Add Cats ---   ID - Tab Caption - ShowDialogButton
    ACPRibbon1.AddCat "1", "1", " ", False
'    ACPRibbon1.AddCat "2", "1", "Monthly Reports ", False
    ACPRibbon1.AddCat "3", "1", "", False
    ACPRibbon1.AddCat "4", "1", "Close", False
    
    '# Add Button ---    ID - Cat - Capt. - Icons -   More Arrow   - ToolTip
    ACPRibbon1.AddButton "1", "1", "Import" & vbNewLine & "Receipts", 5
    ACPRibbon1.AddButton "2", "1", "Exports" & vbNewLine & "Receipts", 6
    ACPRibbon1.AddButton "3", "3", "Update" & vbNewLine & "Students", 7
    
'    ACPRibbon1.AddButton "5", "2", "Pre-Primary", 1
'    ACPRibbon1.AddButton "6", "2", "Building Fund", 1
'    ACPRibbon1.AddButton "7", "2", "Transport", 1
     
    ACPRibbon1.AddButton "10", "4", " ", 3
 
    '# Repaint Ribbon
    ACPRibbon1.Refresh

End Sub

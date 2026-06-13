Attribute VB_Name = "GetComPorts"
'// API Declarations
Public Declare Function CreateFile Lib "kernel32" Alias "CreateFileA" (ByVal lpFileName As String, ByVal dwDesiredAccess As Long, ByVal dwShareMode As Long, lpSecurityAttributes As SECURITY_ATTRIBUTES, ByVal dwCreationDisposition As Long, ByVal dwFlagsAndAttributes As Long, ByVal hTemplateFile As Long) As Long
Public Declare Function CloseHandle Lib "kernel32" (ByVal hObject As Long) As Long

'// API Structures
Public Type SECURITY_ATTRIBUTES
    nLength As Long
    lpSecurityDescriptor As Long
    bInheritHandle As Long
End Type

'// API constants
Public Const FILE_SHARE_READ = &H1
Public Const FILE_SHARE_WRITE = &H2
Public Const OPEN_EXISTING = 3
Public Const FILE_ATTRIBUTE_NORMAL = &H80
 

'// Return TRUE if the COM exists, FALSE if the COM does not exist
Public Function COMAvailable(COMNum As Integer) As Boolean
    Dim hCOM As Long
    Dim ret As Long
    Dim sec As SECURITY_ATTRIBUTES

    '// try to open the COM port
    hCOM = CreateFile("\.\COM" & COMNum & "", 0, FILE_SHARE_READ + _
        FILE_SHARE_WRITE, sec, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0)
    If hCOM = -1 Then
        COMAvailable = False
    Else
        COMAvailable = True
        '// close the COM port
        ret = CloseHandle(hCOM)
    End If
End Function




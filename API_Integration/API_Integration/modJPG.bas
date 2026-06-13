Attribute VB_Name = "modJPG"
' Module1 (Module1.bas)
Option Explicit

Private Type BITMAPINFOHEADER
    biSize As Long
    biWidth As Long
    biHeight As Long
    biPlanes As Integer
    biBitCount As Integer
    biCompression As Long
    biSizeImage As Long
    biXPelsPerMeter As Long
    biYPelsPerMeter As Long
    biClrUsed As Long
    biClrImportant As Long
End Type

Private Declare Function CreateCompatibleDC Lib "gdi32" (ByVal hDC As Long) As Long
Private Declare Function CreateDIBSection Lib "gdi32" (ByVal hDC As Long, pBitmapInfo As BITMAPINFOHEADER, ByVal un As Long, lplpVoid As Long, ByVal Handle As Long, ByVal dw As Long) As Long

Public Declare Function SelectObject Lib "gdi32" (ByVal hDC As Long, ByVal hObject As Long) As Long
Public Declare Function DeleteObject Lib "gdi32" (ByVal hObject As Long) As Long
Public Declare Function DeleteDC Lib "gdi32" (ByVal hDC As Long) As Long
''--------------------------------------------------------------------------------

Public Declare Sub CopyMemory Lib "kernel32" Alias "RtlMoveMemory" (lpvDest As Any, lpvSource As Any, ByVal cbCopy As Long)
Public Declare Function BitBlt Lib "gdi32" (ByVal hDestDC As Long, ByVal X As Long, ByVal Y As Long, ByVal nWidth As Long, ByVal nHeight As Long, ByVal hSrcDC As Long, ByVal xSrc As Long, ByVal ySrc As Long, ByVal dwRop As Long) As Long


Public PadBytes As Long
Public BytesPerScanLine As Long
Public m_hDIb As Long, m_hBmpOld As Long
Public m_hDC As Long, DIBPtr As Long

Public picWidth As Long, picHeight As Long


Public Sub SETBMI()
Dim SBI As BITMAPINFOHEADER
   With SBI
      .biSize = 40
      .biWidth = picWidth
      .biHeight = picHeight
      .biPlanes = 1
      .biBitCount = 32 '24
      .biCompression = 0
   
      BytesPerScanLine = (((.biWidth * .biBitCount) + 31) \ 32) * 4
      PadBytes = BytesPerScanLine - (((.biWidth * .biBitCount) + 7) \ 8)
      .biSizeImage = BytesPerScanLine * Abs(.biHeight)
      
      .biXPelsPerMeter = 0
      .biYPelsPerMeter = 0
      .biClrUsed = 0
      .biClrImportant = 0
   End With
   
   m_hDC = CreateCompatibleDC(0)
   m_hDIb = CreateDIBSection(m_hDC, SBI, 0, DIBPtr, 0, 0)
   m_hBmpOld = SelectObject(m_hDC, m_hDIb)
End Sub

 

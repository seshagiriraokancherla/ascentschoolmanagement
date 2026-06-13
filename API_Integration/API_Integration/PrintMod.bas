Attribute VB_Name = "PrintMod"
Public Sub dosPrintwithFooter(x As ListBox, ftr As String)
Dim tt As Integer
tt = MsgBox("Prints Data", vbOKCancel)
If tt = vbOK Then
Dim i As Integer, j As Integer, k As Integer
k = 1
i = 0
Open App.Path & "\rer1.001" For Output As #1
While i <= x.ListCount - 1
    Print #1, fixstring(items(0), 60) & fixLeftString("Page No     " & k, 18)
    Print #1, items(1)
    Print #1, items(2)
    Print #1, lin
    For j = 0 To 55
        Print #1, x.List(i)
        i = i + 1
        If i = x.ListCount Then
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
    Print #1, ftr
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
    Print #2, "print rer1.001"
Close #2
Shell App.Path & "\fpt.bat", vbHide
End If

End Sub



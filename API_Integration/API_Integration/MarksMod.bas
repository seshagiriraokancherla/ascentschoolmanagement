Attribute VB_Name = "MarksMod"
Public IndvSubjPont As Integer
Public Function GetGradeData(Subct As String, SbjMrks As Double) As String
GetGradeData = ""
IndvSubjPont = 0

If Subct = "Hin" Then
    If Val(SbjMrks) >= 90 Then
        GetGradeData = "A1"
        IndvSubjPont = 10
    ElseIf Val(SbjMrks) >= 80 And Val(SbjMrks) <= 89.9 Then
        GetGradeData = "A2"
        IndvSubjPont = 9
    ElseIf Val(SbjMrks) >= 70 And Val(SbjMrks) <= 79.9 Then
        GetGradeData = "B1"
        IndvSubjPont = 8
    ElseIf Val(SbjMrks) >= 60 And Val(SbjMrks) <= 69.9 Then
        GetGradeData = "B2"
        IndvSubjPont = 7
    ElseIf Val(SbjMrks) >= 50 And Val(SbjMrks) <= 59.9 Then
        GetGradeData = "C1"
        IndvSubjPont = 6
    ElseIf Val(SbjMrks) >= 40 And Val(SbjMrks) <= 49.9 Then
        GetGradeData = "C2"
        IndvSubjPont = 5
    ElseIf Val(SbjMrks) >= 30 And Val(SbjMrks) <= 39.9 Then
        GetGradeData = "D1"
        IndvSubjPont = 4
    ElseIf Val(SbjMrks) >= 20 And Val(SbjMrks) <= 29.9 Then
        GetGradeData = "D2"
        IndvSubjPont = 3
    ElseIf Val(SbjMrks) >= 19 And Val(SbjMrks) <= 0 Then
        GetGradeData = "E"
        IndvSubjPont = 0
    End If
Else
    If Val(SbjMrks) >= 92 Then
        GetGradeData = "A1"
        IndvSubjPont = 10
    ElseIf Val(SbjMrks) >= 83 And Val(SbjMrks) <= 91.9 Then
        GetGradeData = "A2"
        IndvSubjPont = 9
    ElseIf Val(SbjMrks) >= 75 And Val(SbjMrks) <= 82.9 Then
        GetGradeData = "B1"
        IndvSubjPont = 8
    ElseIf Val(SbjMrks) >= 67 And Val(SbjMrks) <= 74.9 Then
        GetGradeData = "B2"
        IndvSubjPont = 7
    ElseIf Val(SbjMrks) >= 59 And Val(SbjMrks) <= 66.9 Then
        GetGradeData = "C1"
        IndvSubjPont = 6
    ElseIf Val(SbjMrks) >= 51 And Val(SbjMrks) <= 58.9 Then
        GetGradeData = "C2"
        IndvSubjPont = 5
    ElseIf Val(SbjMrks) >= 43 And Val(SbjMrks) <= 50.9 Then
        GetGradeData = "D1"
        IndvSubjPont = 4
    ElseIf Val(SbjMrks) >= 35 And Val(SbjMrks) <= 42.9 Then
        GetGradeData = "D2"
        IndvSubjPont = 3
    ElseIf Val(SbjMrks) >= 34 And Val(SbjMrks) <= 0 Then
        GetGradeData = "E"
        IndvSubjPont = 0
    End If
End If

End Function


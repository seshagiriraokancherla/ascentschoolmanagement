Attribute VB_Name = "StuValdt"
Public Function GetAgeValidation(StuDob As Date, StuJoinClas As String) As String
Dim StuAge As Double
Dim TraAgeDat As Date
TraAgeDat = "31-Aug-" & Year(Date)
StuAge = DateDiff("d", StuDob, TraAgeDat)
StuAge = Val(StuAge) / 365
 StuAge = Format(StuAge, "###.##")
 
If StuJoinClas = "Nursery" Then
    If Val(2) >= Val(StuAge) Then
        GetAgeValidation = "Student Age not Eligible with Class Name . . . Present Age : " & StuAge & " Years, Required Age : 2 Years"
    Else
        GetAgeValidation = "Student Present Age : " & StuAge & " Years"
    End If
ElseIf StuJoinClas = "LKG" Then
    If Val(3) >= Val(StuAge) Then
        GetAgeValidation = "Student Age not Eligible with Class Name . . . Present Age : " & StuAge & " Years, Required Age : 3 Years"
    Else
        GetAgeValidation = "Student Present Age : " & StuAge & " Years"
    End If
ElseIf StuJoinClas = "UKG" Then
    If Val(4) >= Val(StuAge) Then
        GetAgeValidation = "Student Age not Eligible with Class Name . . . Present Age : " & StuAge & " Years, Required Age : 4 Years"
    Else
        GetAgeValidation = "Student Present Age : " & StuAge & " Years"
    End If
ElseIf StuJoinClas = "1 Class" Or StuJoinClas = "1 class" Then
    If Val(5) >= Val(StuAge) Then
        GetAgeValidation = "Student Age not Eligible with Class Name . . . Present Age : " & StuAge & " Years, Required Age : 5 Years"
    Else
        GetAgeValidation = "Student Present Age : " & StuAge & " Years"
    End If
ElseIf StuJoinClas = "2 Class" Or StuJoinClas = "2 class" Then
    If Val(6) >= Val(StuAge) Then
        GetAgeValidation = "Student Age not Eligible with Class Name . . . Present Age : " & StuAge & " Years, Required Age : 6 Years"
    Else
        GetAgeValidation = "Student Present Age : " & StuAge & " Years"
    End If
ElseIf StuJoinClas = "3 Class" Or StuJoinClas = "3 Class" Then
    If Val(7) >= Val(StuAge) Then
        GetAgeValidation = "Student Age not Eligible with Class Name . . . Present Age : " & StuAge & " Years, Required Age : 7 Years"
    Else
        GetAgeValidation = "Student Present Age : " & StuAge & " Years"
    End If
ElseIf StuJoinClas = "4 class" Or StuJoinClas = "4 Class" Then
    If Val(8) >= Val(StuAge) Then
        GetAgeValidation = "Student Age not Eligible with Class Name . . . Present Age : " & StuAge & " Years, Required Age : 8 Years"
    Else
        GetAgeValidation = "Student Present Age : " & StuAge & " Years"
    End If
ElseIf StuJoinClas = "5 class" Or StuJoinClas = "5 Class" Then
    If Val(9) >= Val(StuAge) Then
        GetAgeValidation = "Student Age not Eligible with Class Name . . . Present Age : " & StuAge & " Years, Required Age : 9 Years"
    Else
        GetAgeValidation = "Student Present Age : " & StuAge & " Years"
    End If
ElseIf StuJoinClas = "6 class" Or StuJoinClas = "6 Class" Then
    If Val(10) >= Val(StuAge) Then
        GetAgeValidation = "Student Age Not Eligible with Class Name . . . Present Age : " & StuAge & " Years, Required Age : 10 Years"
    Else
        GetAgeValidation = "Student Present Age : " & StuAge & " Years"
    End If
ElseIf StuJoinClas = "7 class" Or StuJoinClas = "7 Class" Then
    If Val(11) >= Val(StuAge) Then
        GetAgeValidation = "Student Age not Eligible with Class Name . . . Present Age : " & StuAge & " Years, Required Age : 11 Years"
    Else
        GetAgeValidation = "Student Present Age : " & StuAge & " Years"
    End If
ElseIf StuJoinClas = "8 class" Or StuJoinClas = "8 Class" Then
    If Val(12) >= Val(StuAge) Then
        GetAgeValidation = "Student Age not Eligible with Class Name . . . Present Age : " & StuAge & " Years, Required Age : 12 Years"
    Else
        GetAgeValidation = "Student Present Age : " & StuAge & " Years"
    End If
ElseIf StuJoinClas = "9 class" Or StuJoinClas = "9 Class" Then
    If Val(13) >= Val(StuAge) Then
        GetAgeValidation = "Student Age not Eligible with Class Name . . . Present Age : " & StuAge & " Years, Required Age : 13 Years"
    Else
        GetAgeValidation = "Student Present Age : " & StuAge & " Years"
    End If
ElseIf StuJoinClas = "10 Class" Or StuJoinClas = "10 Class" Then
    If Val(14) >= Val(StuAge) Then
        GetAgeValidation = "Student Age not Eligible with Class Name . . . Present Age : " & StuAge & " Years, Required Age : 14 Years"
    Else
        GetAgeValidation = "Student Present Age : " & StuAge & " Years"
    End If
End If


End Function
 

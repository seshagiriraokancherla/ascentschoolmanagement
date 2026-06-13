VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "MyClass"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
Public ItmPrt As New ADODB.Recordset
Public Prts As New ADODB.Recordset
Public GetRs As New ADODB.Recordset
Public MRs As New ADODB.Recordset
Public Function TransMaxID(ByVal TBNam As String, ByVal FldNam As String, ByVal TBCond As String) As String
On Error GoTo errHandler
    If Prts.state = 1 Then Prts.Close
    Prts.CursorLocation = adUseClient
    Prts.Open "select max(" & FldNam & ") from " & TBNam & " where " & TBCond & " ", Con, adOpenDynamic, adLockPessimistic
    If Prts.RecordCount > 0 Then
        TransMaxID = Prts(0) + 1
    End If
    Prts.Close
    Exit Function
errHandler:
TransMaxID = 1
End Function
Public Function TransMaxCount(ByVal TBNam As String, ByVal FldNam As String, ByVal TBCond As String) As String
On Error GoTo errHandler
    If Prts.state = 1 Then Prts.Close
    Prts.CursorLocation = adUseClient
    Prts.Open "select max(" & FldNam & ") from " & TBNam & " where " & TBCond & " ", Con, adOpenDynamic, adLockPessimistic
    If Prts.RecordCount > 0 Then
        TransMaxCount = Prts(0)
    End If
    Prts.Close
    Exit Function
errHandler:
TransMaxCount = 0
End Function


Public Function FldID(ByVal TBNam As String, ByVal FldNam As String, ByVal TBCond As String) As String
If Rs.state = 1 Then Rs.Close
Rs.CursorLocation = adUseClient
Rs.Open "select " & FldNam & " from " & TBNam & " ", Con, adOpenDynamic, adLockPessimistic
If Rs.RecordCount > 0 Then
    FldID = Rs(0)
End If
End Function



Public Function GetItmPrts(ByVal TBNam As String, ByVal FldNam As String, ByVal Whr As String) As String
If ItmPrt.state = 1 Then ItmPrt.Close
ItmPrt.CursorLocation = adUseClient
ItmPrt.Open "select " & FldNam & " from " & TBNam & " where " & Whr & " order by " & FldNam & "", Con, adOpenDynamic, adLockPessimistic
    If ItmPrt.RecordCount > 0 Then
        GetItmPrts = ItmPrt(0)
    End If
ItmPrt.Close
End Function

Public Function GetProperties(ByVal TBNam As String, ByVal FldNam As String, ByVal Whr As String, CmbPrt As ComboBox) As String
If Prts.state = 1 Then Prts.Close
Prts.CursorLocation = adUseClient
Prts.Open "select " & FldNam & " from " & TBNam & " where itmprttyp= '" & Whr & "' and Status='A' order by " & FldNam & "", Con, adOpenDynamic, adLockPessimistic
    While Prts.EOF = False
        CmbPrt.AddItem Prts(0)
        Prts.MoveNext
    Wend
Prts.Close
End Function

Public Function GetItmStrng(ByVal TBNam As String, ByVal FldNam As String, ByVal Whr As String) As String

If Prts.state = 1 Then Prts.Close
Prts.CursorLocation = adUseClient
Prts.Open "select Distinct(" & FldNam & ") from " & TBNam & " where  " & Whr & " order by " & FldNam & "", Con, adOpenDynamic, adLockPessimistic
    While Prts.EOF = False
        GetItmStrng = GetItmStrng & "," & Prts(0)
        Prts.MoveNext
    Wend
Prts.Close

End Function

Public Function GetAcs(ByVal TBNam As String, ByVal FldNam As String, ByVal fldnam2 As String, ByVal Whr As String, CmbPrt As ComboBox) As String
If Prts.state = 1 Then Prts.Close
Prts.CursorLocation = adUseClient
Prts.Open "select " & FldNam & " , " & fldnam2 & " from " & TBNam & " where actype= '" & Whr & "' order by " & FldNam & "", Con, adOpenDynamic, adLockPessimistic
    While Prts.EOF = False
        CmbPrt.AddItem Prts(0) & "-" & Prts(1)
        Prts.MoveNext
    Wend
Prts.Close
End Function

Public Function GetCmbDet(ByVal TBNam As String, ByVal FldNam As String, ByVal Whr As String, CmbPrt As ComboBox) As String
CmbPrt.Clear
If Prts.state = 1 Then Prts.Close
Prts.CursorLocation = adUseClient
Prts.Open "select Distinct(" & FldNam & ") from " & TBNam & " order by " & FldNam & "", Con, adOpenDynamic, adLockPessimistic
    While Prts.EOF = False
        CmbPrt.AddItem Prts(0)
        Prts.MoveNext
    Wend
Prts.Close
End Function


Public Function GetItems(ByVal TBNam As String, ByVal FldNam As String, CmbPrt As ComboBox) As String
CmbPrt.Clear
If Prts.state = 1 Then Prts.Close
Prts.CursorLocation = adUseClient
Prts.Open "select " & FldNam & " from " & TBNam & " where Status='A' order by " & FldNam & "", Con, adOpenDynamic, adLockPessimistic
    While Prts.EOF = False
        CmbPrt.AddItem Prts(0)
        Prts.MoveNext
    Wend
Prts.Close
End Function
Public Function GetAllItems(ByVal TBNam As String, ByVal FldNam As String, CmbPrt As ComboBox) As String
If Prts.state = 1 Then Prts.Close
Prts.CursorLocation = adUseClient
Prts.Open "select " & FldNam & " from " & TBNam & " order by " & FldNam & "", Con, adOpenDynamic, adLockPessimistic
    While Prts.EOF = False
        CmbPrt.AddItem Prts(0)
        Prts.MoveNext
    Wend
Prts.Close
End Function
Public Function GetPFxID(ByVal TBNam As String, ByVal FldNam As String, ByVal Pfx As String) As String
On Error GoTo errHandler
If GetRs.state = 1 Then GetRs.Close
GetRs.CursorLocation = adUseClient
GetRs.Open "select max(mid(" & FldNam & ",3,6)) from " & TBNam & " ", Con, adOpenDynamic, adLockPessimistic
    If GetRs.RecordCount > 0 Then
        If Val(GetRs(0)) < 9 Then
            GetPFxID = Pfx & "00000" & GetRs(0) + 1
        ElseIf Val(GetRs(0)) < 99 Then
            GetPFxID = Pfx & "0000" & GetRs(0) + 1
        ElseIf Val(GetRs(0)) < 999 Then
            GetPFxID = Pfx & "000" & GetRs(0) + 1
        ElseIf Val(GetRs(0)) < 9999 Then
            GetPFxID = Pfx & "00" & GetRs(0) + 1
        Else
            GetPFxID = Pfx & "0" & GetRs(0) + 1
        End If
    End If

GetRs.Close
Exit Function
errHandler:
GetPFxID = Pfx & "000001"
End Function

Public Function GetItmID(ByVal TBNam As String, ByVal FldNam As String, ByVal Pfx As String) As String
On Error GoTo errHandler
If GetRs.state = 1 Then GetRs.Close
GetRs.CursorLocation = adUseClient
GetRs.Open "select max(mid(" & FldNam & ",3,6)) from " & TBNam & " ", Con, adOpenDynamic, adLockPessimistic
    If GetRs.RecordCount > 0 Then
        If Val(GetRs(0)) < 9 Then
            GetItmID = Pfx & "00000" & GetRs(0) + 1
        ElseIf Val(GetRs(0)) < 99 Then
            GetItmID = Pfx & "0000" & GetRs(0) + 1
        ElseIf Val(GetRs(0)) < 999 Then
            GetItmID = Pfx & "000" & GetRs(0) + 1
        ElseIf Val(GetRs(0)) < 9999 Then
            GetItmID = Pfx & "00" & GetRs(0) + 1
        Else
            GetItmID = Pfx & "0" & GetRs(0) + 1
        End If
    End If
GetRs.Close
Exit Function
errHandler:
GetItmID = Pfx & "000001"
End Function

Public Function GetItmIDMonthly(ByVal TBNam As String, ByVal FldNam As String, ByVal Pfx As String, ByVal Whr As String) As String
On Error GoTo errHandler
If MRs.state = 1 Then MRs.Close
MRs.CursorLocation = adUseClient
MRs.Open "select max(mid(" & FldNam & ",3,6)) from " & TBNam & " where " & Whr & " ", Con, adOpenDynamic, adLockPessimistic
    If MRs.RecordCount > 0 Then
        If Val(MRs(0)) < 9 Then
            GetItmIDMonthly = Pfx & "00000" & MRs(0) + 1
        ElseIf Val(MRs(0)) < 99 Then
            GetItmIDMonthly = Pfx & "0000" & MRs(0) + 1
        ElseIf Val(MRs(0)) < 999 Then
            GetItmIDMonthly = Pfx & "000" & MRs(0) + 1
        ElseIf Val(MRs(0)) < 9999 Then
            GetItmIDMonthly = Pfx & "00" & MRs(0) + 1
        Else
            GetItmIDMonthly = Pfx & "0" & MRs(0) + 1
        End If
    End If
MRs.Close
Exit Function
errHandler:
GetItmIDMonthly = Pfx & "000001"
End Function
Public Function GetAllAcs(ByVal TBNam As String, ByVal FldNam As String, ByVal Whr As String, CmbPrt As ComboBox) As String
CmbPrt.Clear
If Prts.state = 1 Then Prts.Close
Prts.CursorLocation = adUseClient
Prts.Open "select " & FldNam & "  from " & TBNam & " where  " & Whr & " order by " & FldNam & "", Con, adOpenDynamic, adLockPessimistic
    While Prts.EOF = False
        CmbPrt.AddItem Prts(0)
        Prts.MoveNext
    Wend
Prts.Close
End Function

Public Function GetSum(ByVal TBNam As String, ByVal FldNam As String, ByVal Whr As String) As String

If GetRs.state = 1 Then GetRs.Close
GetRs.CursorLocation = adUseClient
GetRs.Open "select sum(" & FldNam & ") from " & TBNam & "  where  " & Whr & "", Con, adOpenDynamic, adLockPessimistic
    If IsNull(GetRs(0)) = True Then
        GetSum = 0
    Else
        GetSum = GetRs(0)
    End If
GetRs.Close

End Function
Public Function GetComboItems(ByVal TBNam As String, ByVal FldNam As String, ByVal Whr As String, CmbPrt As ComboBox) As String
CmbPrt.Clear
If Prts.state = 1 Then Prts.Close
Prts.CursorLocation = adUseClient
Prts.Open "select Distinct(" & FldNam & ") from " & TBNam & "  where " & Whr & " order by " & FldNam & "", Con, adOpenDynamic, adLockPessimistic
    While Prts.EOF = False
        CmbPrt.AddItem Prts(0)
        Prts.MoveNext
    Wend
Prts.Close
End Function

Public Function GetNewTraID(ByVal TBNam As String, ByVal FldNam As String, ByVal Pfx As String, ByVal Whr As String) As String
On Error GoTo errHandler
If MRs.state = 1 Then MRs.Close
MRs.CursorLocation = adUseClient
If dbCheck = 1 Then
    MRs.Open "select max(substring(" & FldNam & ",5,6)) from " & TBNam & " where " & Whr & " ", Con, adOpenDynamic, adLockPessimistic
Else
    MRs.Open "select max(mid(" & FldNam & ",5,6)) from " & TBNam & " where " & Whr & " ", Con, adOpenDynamic, adLockPessimistic
End If
    If MRs.RecordCount > 0 Then
    If IsNull(MRs(0)) = True Then
        GetNewTraID = "0001"
    Else
        If Val(MRs(0)) < 9 Then
            GetNewTraID = "000" & MRs(0) + 1
        ElseIf Val(MRs(0)) < 99 Then
            GetNewTraID = "00" & MRs(0) + 1
        ElseIf Val(MRs(0)) < 999 Then
            GetNewTraID = "0" & MRs(0) + 1
        Else
            GetNewTraID = MRs(0) + 1
        End If
    End If
    End If
MRs.Close
Exit Function
errHandler:
GetNewTraID = "0001"
End Function
Public Function GetFeildID(ByVal TBNam As String, ByVal FldNam As String, ByVal Whr As String) As String
If ItmPrt.state = 1 Then ItmPrt.Close
ItmPrt.CursorLocation = adUseClient
ItmPrt.Open "select " & FldNam & " from " & TBNam & " where " & Whr & "", Con, adOpenDynamic, adLockPessimistic
    If ItmPrt.RecordCount > 0 Then
        GetFeildID = ItmPrt(0)
    Else
        GetFeildID = ""
    End If
ItmPrt.Close
End Function


Public Function GetFeeReceiptID(ByVal TBNam As String, ByVal FldNam As String, ByVal Pfx As String, ByVal Whr As String) As String
On Error GoTo errHandler
If MRs.state = 1 Then MRs.Close
MRs.CursorLocation = adUseClient
If dbCheck = 1 Then
    MRs.Open "select max(substring(" & FldNam & ",6,5)) from " & TBNam & " where " & Whr & " ", Con, adOpenDynamic, adLockPessimistic
Else
    MRs.Open "select max(mid(" & FldNam & ",6,5)) from " & TBNam & " where " & Whr & " ", Con, adOpenDynamic, adLockPessimistic
End If
    If MRs.RecordCount > 0 Then
    If IsNull(MRs(0)) = True Then
        GetFeeReceiptID = "00001"
    Else
        If Val(MRs(0)) < 9 Then
            GetFeeReceiptID = "0000" & MRs(0) + 1
        ElseIf Val(MRs(0)) < 99 Then
            GetFeeReceiptID = "000" & MRs(0) + 1
        ElseIf Val(MRs(0)) < 999 Then
            GetFeeReceiptID = "00" & MRs(0) + 1
        ElseIf Val(MRs(0)) < 9999 Then
            GetFeeReceiptID = "0" & MRs(0) + 1
        
        Else
            GetFeeReceiptID = MRs(0) + 1
        End If
    End If
    End If
MRs.Close
Exit Function
errHandler:
GetFeeReceiptID = "00001"
End Function


Public Function GetCont(ByVal TBNam As String, ByVal FldNam As String, ByVal Whr As String) As String

If GetRs.state = 1 Then GetRs.Close
GetRs.CursorLocation = adUseClient
GetRs.Open "select count(" & FldNam & ") from " & TBNam & "  where  " & Whr & "", Con, adOpenDynamic, adLockPessimistic
    If IsNull(GetRs(0)) = True Then
        GetCont = 0
    Else
        GetCont = GetRs(0)
    End If
GetRs.Close

End Function

Public Function GetNewCount(ByVal TBNam As String, ByVal FldNam As String, ByVal Whr As String) As String

If GetRs.state = 1 Then GetRs.Close
GetRs.CursorLocation = adUseClient
GetRs.Open "select count(" & FldNam & ") from " & TBNam & "  where  " & Whr & "", Con, adOpenDynamic, adLockPessimistic
If GetRs.RecordCount > 0 Then
    If IsNull(GetRs(0)) = True Then
        GetNewCount = 0
    Else
        GetNewCount = GetRs.RecordCount
    End If
Else
  GetNewCount = 0
End If
GetRs.Close

End Function


Public Function GetFeildIDWithZero(ByVal TBNam As String, ByVal FldNam As String, ByVal Whr As String) As String
If ItmPrt.state = 1 Then ItmPrt.Close
ItmPrt.CursorLocation = adUseClient
ItmPrt.Open "select " & FldNam & " from " & TBNam & " where " & Whr & "", Con, adOpenDynamic, adLockPessimistic
    If ItmPrt.RecordCount > 0 Then
        GetFeildIDWithZero = ItmPrt(0)
    Else
        GetFeildIDWithZero = 0
    End If
ItmPrt.Close
End Function

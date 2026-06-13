Attribute VB_Name = "miscellaneous"
Public Sub menuVerification()
Exit Sub
End Sub

Public Function findItemPosition(menuC As Integer, itemC As Integer) As Integer
findItemPosition = 1
Exit Function
Dim r As New ADODB.Recordset, s As String
r.Open "select * from admLodUsrDetails where usrname = '" & UsrNam & "'", Con, adOpenDynamic, adLockPessimistic
If r.BOF = True And r.EOF = True Then
    findItemPosition = 1
    Exit Function
Else
    If Trim(r.Fields(2)) = "" Then
        findItemPosition = 1
        Exit Function
    Else
        s = r.Fields(2)
    End If
End If
r.Close
r.Open "select * from admRolesLod where rolename = '" & s & "' and menuc = " & menuC & " and itemc = " & itemC, Con, adOpenDynamic, adLockPessimistic
    findItemPosition = r.Fields(3)
r.Close



End Function

Public Sub misCountryAdd(t1 As TreeView)
Dim i As Integer, r As New ADODB.Recordset
i = 1
t1.Nodes.Clear
r.Open "misCountryMaster", Con, adOpenDynamic, adLockPessimistic

While r.EOF = False
    t1.Nodes.Add , , "a" & i, r.Fields(0)
    i = i + 1
    r.MoveNext
Wend
r.Close
End Sub
Public Sub misStateAdd(t1 As TreeView)
Dim i As Integer, j As Integer
Dim r As New ADODB.Recordset
Call misCountryAdd(t1)
For i = 1 To t1.Nodes.Count
    r.Open "select * from misStateMaster where cntname = '" & t1.Nodes(i).Text & "'", Con, adOpenDynamic, adLockPessimistic
    j = 1
    While r.EOF = False
        t1.Nodes.Add t1.Nodes(i).Key, tvwChild, "x" & j & t1.Nodes(i).Key, r.Fields(1)
        j = j + 1
        r.MoveNext
    Wend
    r.Close
Next i
End Sub

Public Function menuEnableChk(menuC As Integer, itemC As Integer) As Boolean
Dim s As String
s = usrName
If UCase(s) = "ADMIN" Then
    menuEnableChk = True
    Exit Function
End If

Dim r As New ADODB.Recordset

r.Open "select * from admRoles where rolename = '" & s & "' and menuc = " & menuC & " and itemC = " & itemC, Con, adOpenDynamic, adLockPessimistic
menuEnableChk = Abs(r.Fields(3))
r.Close
End Function


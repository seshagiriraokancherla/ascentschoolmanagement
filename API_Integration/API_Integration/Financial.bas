Attribute VB_Name = "Financial"
Public financialStaDate As Date
Public financialLastDate As Date
Public financialGrpChk As Integer
Public financialTraDate As Date
Public financialNarrChk As Integer
Public finAccounctCreChk As Integer


Public Sub FintreeAdd(t1 As TreeView)
t1.Nodes.Clear
Dim r As New ADODB.Recordset
t1.Nodes.Add , , "a", "ASSETS"
t1.Nodes.Add , , "b", UCase("Liabilities")
t1.Nodes.Add , , "c", UCase("Incomes")
t1.Nodes.Add , , "d", UCase("Expenditure")
'The above 4 are primary groupw which are irrevalent to data base
'Set r = db.OpenRecordset("fingroups")
r.Open "select * from finGroups where branchid = '" & branchID & "' order by seq", db, adOpenDynamic, adLockPessimistic
While r.EOF = False
    t1.Nodes.Add Trim(r.Fields(1)), tvwChild, Trim(r.Fields(2)), Trim(r.Fields(0))
    r.MoveNext
Wend
'to add one by one sub group to its location

Call makeColor(t1)
End Sub
Private Sub makeColor(t1 As TreeView)
Dim i As Integer
For i = 1 To t1.Nodes.Count
    If Mid(t1.Nodes(i).Key, 1, 2) = "a0" Then
        t1.Nodes(i).ForeColor = RGB(0, 255, 255)
    ElseIf Mid(t1.Nodes(i).Key, 1, 2) = "a1" Or Mid(t1.Nodes(i).Key, 1, 2) = "b2" Then
        t1.Nodes(i).ForeColor = RGB(255, 0, 0)      'Banks
    ElseIf Mid(t1.Nodes(i).Key, 1, 2) = "b1" Then
        t1.Nodes(i).ForeColor = RGB(255, 128, 0)    'Suppliers
    ElseIf Mid(t1.Nodes(i).Key, 1, 2) = "d1" Then
        t1.Nodes(i).ForeColor = RGB(255, 0, 255)    'Purchases
    ElseIf Mid(t1.Nodes(i).Key, 1, 2) = "a2" Then
        t1.Nodes(i).ForeColor = RGB(0, 0, 255)      'Customers
    ElseIf Mid(t1.Nodes(i).Key, 1, 2) = "c1" Then
        t1.Nodes(i).ForeColor = RGB(0, 128, 128)    'Sales
    ElseIf Mid(t1.Nodes(i).Key, 1, 2) = "d2" Then
        t1.Nodes(i).ForeColor = RGB(0, 128, 0)      'Manufacturin Expenses
    ElseIf Mid(t1.Nodes(i).Key, 1, 2) = "d3" Then
        t1.Nodes(i).ForeColor = RGB(143, 80, 134)      'Taxes
        't1.Nodes(i).BackColor = vbBlack
    Else
        t1.Nodes(i).ForeColor = vbBlack             'Financial
    End If
       
Next i
End Sub
Public Function globalMinAmountCheck(aName As String, Amt As Double, X As Date) As Boolean
Dim r As New ADODB.Recordset, b As Boolean
r.Open "Select * from finOtherFactors where branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
If r.BOF = True And r.EOF = True Then
    b = False
Else
    If r.Fields(0) = 1 Then
        b = True
    Else
        b = False
    End If
End If
r.Close
If b = True Then
    globalMinAmountCheck = True
    Exit Function
Else

Dim n1 As Double, n2 As Double, n3 As Double
Dim dddd As Date
Dim Y As Date, n As Date
Dim r1 As New ADODB.Recordset, r2 As New ADODB.Recordset
n1 = 0 - (nameWiseOpeningBalance(aName, X) + nameWiseDayTransactions(aName, X))
'Closing balance at the end of given date

n2 = n1 '- nameWiseDayTransactions(aName, x)
'Closing balance at the end of given date

r1.Open "select * from finexec where dat > " & newStrDate(X) & " and dat <= " & newStrDate(financialClosing(X)) & " and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic

If r1.BOF = False Or r1.EOF = False Then
    r2.Open "select max(dat) from finexec where dat > " & newStrDate(X) & " and dat <= " & newStrDate(financialClosing(X)) & " and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
    Y = r2.Fields(0)
    r2.Close
    For n = X + 1 To Y
        n2 = n2 - nameWiseDayTransactions(aName, n)
        If n2 < n1 Then
            n1 = n2
        End If
    Next n
End If
r1.Close

If n1 >= Amt Then
    globalMinAmountCheck = True
Else
    MsgBox "Negative Balances can not be maintained even in further Date", vbCritical
    globalMinAmountCheck = False
End If
End If
End Function
Public Function globalMinAmount(aName As String, X As Date) As Double

'This function helps to find minimum cash balance at the end of day after
'giving date

Dim n1 As Double, n2 As Double, n3 As Double
Dim dddd As Date
Dim Y As Date, n As Date
Dim r1 As New ADODB.Recordset, r2 As New ADODB.Recordset
n1 = 0 - (nameWiseOpeningBalance(aName, X) + nameWiseDayTransactions(aName, X))
'Closing balance at the end of given date

n2 = n1 - nameWiseDayTransactions(aName, X)
'Closing balance at the end of given date

r1.Open "select * from finexec where dat > " & newStrDate(X) & " and dat <= " & newStrDate(financialClosing(X)) & " and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
If r1.BOF = True And r1.EOF = True Then
    globalMinAmount = n1
    Exit Function
Else
    r2.Open "select max(dat) from finexec where dat > " & newStrDate(X) & " and dat <= " & newStrDate(financialClosing(X)) & " and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
    Y = r2.Fields(0)
    'to find maximum date of transactions
    
    'the purpose of followed loop is to check from given date to maximum date
    'where the closing balance is less than current closing balance
    'the moment when closing balance of any date is less than miniumu closing
    'Balance at instance then minimum balance is replaced with current closing balance
    'Thus we can find the least balanced maintained at the end of date
    For n = X + 1 To Y
        n2 = n2 - nameWiseDayTransactions(aName, n)
        If n2 < n1 Then
            n1 = n2
        End If
    Next n
End If

globalMinAmount = n1
End Function
Public Function dailyCashFlow(X As Date) As Double
'The purpose of this function is to return
'effective cash flow in given day
On Error GoTo errHandler
Dim r As New ADODB.Recordset
Dim n As Double
n = 0
r.Open "select * from finaccountCre where actype = 'CAS' and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
While r.EOF = False
    n = n + nameWiseDayTransactions(r.Fields(1), X)
    r.MoveNext
Wend
r.Close
dailyCashFlow = n
Exit Function
errHandler:
dailyCashFlow = 0
End Function

Public Function dailyBankFlow(X As Date) As Double
'The purpose of this function is to return
'effective cash flow in given day
On Error GoTo errHandler
Dim r As New ADODB.Recordset
Dim n As Double
n = 0
r.Open "select * from finaccountCre where actype = 'B' and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
While r.EOF = False
    n = n + nameWiseDayTransactions(r.Fields(1), X)
    r.MoveNext
Wend
r.Close
dailyBankFlow = n
Exit Function
errHandler:
dailyBankFlow = 0
End Function
Public Sub findFinancialStaDate(BCode As String)
On Error GoTo errHandler
Dim r As New ADODB.Recordset
r.Open "select * from finstadate where branchid = '" & BCode & "'", db, adOpenDynamic, adLockPessimistic
financialStaDate = r.Fields(1)
financialLastDate = r.Fields(2)
r.Close
Exit Sub
errHandler:
End Sub
Public Function openingCashBalance(d As Date)
On Error GoTo errHandler
Dim r As New ADODB.Recordset
Dim n As Double
n = 0
r.Open "select * from finaccountCre where actype = 'CAS' and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
While r.EOF = False
    n = n + nameWiseOpeningBalance(r.Fields(1), d)
    r.MoveNext
Wend
r.Close
openingCashBalance = n
Exit Function
errHandler:
openingCashBalance = 0
End Function
Public Function nameWiseOpeningBalance(s As String, d As Date) As Double
On Error GoTo errHandler
Dim r As New ADODB.Recordset
Dim dd As Date
dd = financialStart(d)
Dim n As Double
n = 0

'On Error GoTo errH
r.Open "select * from finOpeningBalances where aname = '" & Trim(s) & "' and dat = " & newStrDate(dd) & " and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
If r.BOF = True And r.EOF = True Then
n = 0
Else
n = r.Fields(2) - r.Fields(3)
End If
r.Close
'errH:



'r.Open "select * from finexec where dat < " & newStrDate(d) & " and aname = '" & Trim(s) & "' and dat>= " & newStrDate(dd) & " and branchid = '" & branchid & "'"

'--------------------------------------------------------------------------------
r.Open "select sum(cre-deb) from finexec where dat < " & newStrDate(d) & " and aname = '" & Trim(s) & "' and dat>= " & newStrDate(dd) & " and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
'above query i changed on 1-Apr-13
'------------------------------------------------------------------------------------

'r.Open "select sum(cre-deb) from finexec where dat <= " & newStrDate(d) & " and aname = '" & Trim(s) & "' and dat>= " & newStrDate(dd) & " and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic

While r.EOF = False
    'n = n + r.Fields(2) - r.Fields(3)
    If IsNull(r.Fields(0)) = False Then
    n = n + r.Fields(0)
    End If
    r.MoveNext
Wend
r.Close

nameWiseOpeningBalance = n
Exit Function
errHandler:
nameWiseOpeningBalance = 10
End Function
Public Function nameWiseDayTransactions(aName As String, Dat As Date) As Double
On Error GoTo errHandler
Dim r As New ADODB.Recordset
r.Open "select sum(cre)-sum(deb) from finexec where dat = " & newStrDate(Dat) & " and aname = '" & aName & "' and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
nameWiseDayTransactions = r.Fields(0)
r.Close
Exit Function
errHandler:
nameWiseDayTransactions = 0
End Function
Public Function openingBalancesdiff(Dat As Date) As Double
On Error GoTo errHandler
Dim r As New ADODB.Recordset
Dim n As Double
n = 0
Dim d As Date
d = financialStart(Dat)

r.Open "select sum(cre)-sum(deb) from finOpeningBalances where dat = " & newStrDate(d) & " and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
n = r.Fields(0)
r.Close
openingBalancesdiff = n
Exit Function
errHandler:
openingBalancesdiff = 0
End Function
Public Function openingBankBalance(X As Date) As Double
On Error GoTo errHandler
Dim r As New ADODB.Recordset
Dim n As Double
n = 0
r.Open "select * from finaccountCre where actype = 'B' and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
While r.EOF = False
    n = n + nameWiseOpeningBalance(r.Fields(1), X)
    r.MoveNext
Wend
r.Close
openingBankBalance = n
Exit Function
errHandler:
openingBankBalance = 0
End Function
Public Function totalIncomes(X As Date) As Double
On Error GoTo errHandler
Dim n As Double, r As New ADODB.Recordset, r1 As New ADODB.Recordset
n = 0
r1.Open "select * from fingroups where branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
While r1.EOF = False
    If Mid(r1.Fields(1), 1, 1) = "c" Then
        r.Open "select * from finaccountcre where grp = '" & Trim(r1.Fields(0)) & "' and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
        While r.EOF = False
            n = n + nameWiseOpeningBalance(r.Fields(1), X - 1) + nameWiseDayTransactions(r.Fields(1), X - 1)
            r.MoveNext
        Wend
        r.Close
    End If
    r1.MoveNext
Wend
r1.Close
totalIncomes = Round(n, 2)
Exit Function
errHandler:
totalIncomes = 0

End Function
Public Function totalExpenses(X As Date) As Double
On Error GoTo errHandler
Dim n As Double, r As New ADODB.Recordset, r1 As New ADODB.Recordset
n = 0
r1.Open "select * from fingroups where branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
While r1.EOF = False
    If Mid(r1.Fields(1), 1, 1) = "d" Then
        r.Open "select * from finaccountcre where grp = '" & Trim(r1.Fields(0)) & "' and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
        While r.EOF = False
            n = n + nameWiseOpeningBalance(r.Fields(1), X - 1) + nameWiseDayTransactions(r.Fields(1), X - 1)
            r.MoveNext
        Wend
        r.Close
    End If
    r1.MoveNext
Wend
r1.Close
n = 0 - n
totalExpenses = Round(n, 2)
Exit Function
errHandler:
totalExpenses = 0
End Function

Public Function accountWiseCashFlow(aName As String, frmDate As Date, toDate As Date) As Double
On Error GoTo errHandler
Dim r As New ADODB.Recordset
r.Open "select sum(cre)-sum(deb) from finexec where aname = '" & Trim(aName) & "' and dat >= " & newStrDate(frmDate) & " and dat <= " & newStrDate(toDate) & " and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
accountWiseCashFlow = r.Fields(0)
Exit Function
errHandler:
accountWiseCashFlow = 0
End Function

Public Function totalIncomesExpensesFlow(d1 As Date, d2 As Date, chk As Integer) As Double
Dim r As New ADODB.Recordset, r1 As New ADODB.Recordset
Dim Tot As Double
Tot = 0
r.Open "select * from fingroups where branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic

While r.EOF = False
If Mid(r.Fields(2), 1, 1) = "c" Or Mid(r.Fields(2), 1, 1) = "d" Then
r1.Open "select * from finaccountcre where grp = '" & Trim(r.Fields(0)) & "' and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
While r1.EOF = False
    Tot = Tot + findTotalAmount(r1.Fields(1), d1, d2, chk)
    r1.MoveNext
Wend

r1.Close
    
End If
r.MoveNext
Wend
r.Close
totalIncomesExpensesFlow = Tot
End Function
Private Function findTotalAmount(aName As String, d1 As Date, d2 As Date, chk As Integer) As Double
On Error GoTo errHandler
Dim r As New ADODB.Recordset
If chk = 1 Then
r.Open "select sum(cre) from finexec where aname = '" & Trim(aName) & "' and dat >= " & newStrDate(d1) & " and dat <= " & newStrDate(d2) & " and cre > 0 and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
Else
r.Open "select sum(deb) from finexec where aname = '" & Trim(aName) & "' and dat >= " & newStrDate(d1) & " and dat <= " & newStrDate(d2) & " and deb > 0 and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
End If
findTotalAmount = r.Fields(0)

Exit Function
errHandler:
findTotalAmount = 0

End Function
Public Sub vendorTreeAdd(t1 As TreeView)
t1.Nodes.Clear
Dim r As New ADODB.Recordset

t1.Nodes.Add , , "b1", UCase("Sundry Creditors")



'Set r = db.OpenRecordset("fingroups")
r.Open "select * from finGroups where branchid = '" & branchID & "' order by seq", db, adOpenDynamic, adLockPessimistic
While r.EOF = False
If Mid(r.Fields(2), 1, 3) = "b1_" Then
    t1.Nodes.Add Trim(r.Fields(1)), tvwChild, Trim(r.Fields(2)), Trim(r.Fields(0))
End If
    r.MoveNext
Wend
'to add one by one sub group to its location
End Sub

Public Function groupWiseOpeningBalance(grpName As String, Dat As Date) As Double
On Error GoTo errHandler
Dim r As New ADODB.Recordset, n As Double
n = 0
r.Open "select * from finaccountcre where grp = '" & Trim(grpName) & "' and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
While r.EOF = False
    n = n + nameWiseOpeningBalance(r.Fields(1), Dat)
    r.MoveNext
Wend
groupWiseOpeningBalance = n
Exit Function
errHandler:
groupWiseOpeningBalance = 0
End Function
Public Function groupWiseDailyTransactions(grpName As String, Dat As Date) As Double
On Error GoTo errHandler
Dim r As New ADODB.Recordset, n As Double
n = 0
r.Open "select * from finaccountcre where grp = '" & Trim(grpName) & "' and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
While r.EOF = False
    n = n + nameWiseDayTransactions(r.Fields(1), Dat)
    r.MoveNext
Wend
groupWiseDailyTransactions = n
Exit Function
errHandler:
groupWiseDailyTransactions = 0

End Function
Public Function findIDNO(da As Date) As Integer
On Error GoTo errHandler
Dim r As New ADODB.Recordset
r.Open "select max(idno) from finexec where branchid = '" & branchID & "' and dat = " & newStrDate(da) & "", db, adOpenDynamic, adLockPessimistic
findIDNO = r.Fields(0) + 1
r.Close
Exit Function
errHandler:
findIDNO = 1
End Function
Public Function findGroupNo(s As String, da As Date) As Integer
On Error GoTo errHandler
Dim r As New ADODB.Recordset
r.Open "select max(groupCode) from finexec where branchid = '" & branchID & "' and dat = " & newStrDate(da) & " and tratype = '" & s & "'", db, adOpenDynamic, adLockPessimistic
findGroupNo = r.Fields(0) + 1
r.Close
Exit Function
errHandler:
findGroupNo = 1
End Function
Public Function findTransactionID(Dat As Date) As String
Dim s As String
s = Right(Trim(str(Year(Dat))), 2) & Trim(zeroMake(Month(Dat), 2)) & Trim(zeroMake(Day(Dat), 2)) & " "
On Error GoTo errHandler
Dim r As New ADODB.Recordset
r.Open "Select max(y) from finexec where dat = " & newStrDate(Dat) & " and branchid = '" & branchID & "' and y like '" & s & "%'", db, adOpenDynamic, adLockPessimistic
If r.BOF = True And r.EOF = True Then
    n = 0
Else
    If IsNull(r.Fields(0)) Then
        n = 0
    Else
        n = Val(Trim(Right(r.Fields(0), 3)))
    End If
End If
r.Close
n = n + 1
s = s & zeroMake(Val(Trim(str(n))), 3)
findTransactionID = s
Exit Function
errHandler:
s = s & zeroMake(100, 3)
findTransactionID = s
End Function
Public Function transactionCheck(Dat As Date) As Boolean
On Error GoTo errHandler
Dim st As Date, ct As Date
st = financialStart(financialStaDate)
ct = financialClosing(financialStaDate)

If Dat >= st And Dat <= ct Then
    transactionCheck = True
Else
    MsgBox "Your transaction should be from " & strDate(st) & " to " & strDate(ct)
    transactionCheck = False
End If
errHandler:
End Function


Public Sub dosVoucherMAke(Dat As Date, rectNo As String, traType As String, chk As Integer)
Dim n As Double, n1 As Double
Open App.Path & "\tt.003" For Output As #1



Print #1, fixstring(" ", 9) & Chr(14) & items(0) & Chr(17)
Print #1, fixCenString(items(1) & ", " & items(2), 67)
Print #1, ""
Print #1, ""

If chk = 1 Then
    Print #1, fixstring(" ", 15) & Chr(14) & "VOUCHER" & Chr(17)
    Print #1, fixstring(" ", 15) & Chr(14) & "-------" & Chr(17)
Else
    Print #1, fixstring(" ", 15) & Chr(14) & "RECEIPT" & Chr(17)
    Print #1, fixstring(" ", 15) & Chr(14) & "-------" & Chr(17)
End If
Print #1, ""
Print #1, ""

Dim r As New ADODB.Recordset
r.Open "Select * from finexec where y = '" & rectNo & "' and tratype = '" & traType & "' and branchid = '" & branchID & "' and x <> ' '", db, adOpenDynamic, adLockPessimistic
If r.BOF = False Or r.EOF = False Then
    Print #1, fixstring("Tra Id  : " & rectNo, 25) & fixLeftString("Date : " & strDate(Dat), 42)
    Print #1, ""
    Print #1, ""
    If chk = 1 Then
        Print #1, fixstring("Paid      Rs:", 20) & numWord(Val(r.Fields(3))) & " only"
        Print #1, ""
        Print #1, fixstring("To Mr / Mrs / M/s", 20) & r.Fields(1)
    Else
        Print #1, fixstring("Received  Rs:", 20) & numWord(Val(r.Fields(2))) & " only"
        Print #1, "with thanks"
        Print #1, fixstring("from Mr / Mrs / M/s", 20) & r.Fields(1)
    End If
    
    
    
    Print #1, "Towards " & r.Fields(4)
    Print #1, ""
    Print #1, ""
    If chk = 1 Then
        Print #1, "Rs: " & Chr(14) & fixCur(Val(r.Fields(3)), 2) & "/-" & Chr(17)
    Else
        Print #1, "Rs: " & Chr(14) & fixCur(Val(r.Fields(2)), 2) & "/-" & Chr(17)
    End If
    
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, fixstring("Consignee", 27) & fixLeftString("Cashier / Receiptionist", 40)
    Print #1, ""
    Print #1, ""
    Print #1, ""
    Print #1, fixLeftString("For " & items(0), 67)
End If
r.Close
Close #1
Open App.Path & "\fprt.bat" For Output As #2
    Print #2, "type tt.003 >> prn"
Close #2
Shell App.Path & "\fprt.bat", vbHide


End Sub
Public Sub setTempFinancialYear(Dat As Date)
financialStaDate = financialStart(Dat)
End Sub

Public Sub windowsVoucherMake(Dat As Date, reptNo As String, traType As String, chk As Integer)
Dim n As Double, n1 As Double
Printer.Font.Size = 11
Printer.Font.Name = "Arial"
Printer.Font.Bold = True


Printer.Print Space(12) & items(0)
Printer.Font.Size = 9
Printer.Font.Name = "Arial"
Printer.Font.Bold = True

Printer.Print fixCenString(items(1) & ", " & items(2), 67)
Printer.Print ""
Printer.Print ""

Printer.Font.Size = 11
Printer.Font.Name = "Arial"
Printer.Font.Bold = True


If chk = 1 Then
    Printer.Print fixstring(" ", 15) & Chr(14) & "VOUCHER" & Chr(17)
    Printer.Print fixstring(" ", 15) & Chr(14) & "-------" & Chr(17)
Else
    Printer.Print fixstring(" ", 15) & Chr(14) & "RECEIPT" & Chr(17)
    Printer.Print fixstring(" ", 15) & Chr(14) & "-------" & Chr(17)
End If
Printer.Print ""
Printer.Print ""
Printer.Font.Size = 9
Printer.Font.Name = "Arial"
Printer.Font.Bold = False

Dim r As New ADODB.Recordset
r.Open "Select * from finexec where y = '" & rectNo & "' and tratype = '" & traType & "' and branchid = '" & branchID & "' and x <> ' '", db, adOpenDynamic, adLockPessimistic
If r.BOF = False Or r.EOF = False Then
    Printer.Print fixstring("Tra Id  : " & rectNo, 25) & fixLeftString("Date : " & strDate(Dat), 42)
    Printer.Print ""
    Printer.Print ""
    If chk = 1 Then
        Printer.Print fixstring("Paid      Rs:", 20) & numWord(Val(r.Fields(3))) & " only"
        Printer.Print ""
        Printer.FontBold = True
        Printer.Print fixstring("from Mr / Mrs / M/s", 20) & r.Fields(1)
        Printer.FontBold = False
    Else
        Printer.Print fixstring("Received  Rs:", 20) & numWord(Val(r.Fields(2))) & " only"
        Printer.Print "with thanks"
        Printer.FontBold = True
        Printer.Print fixstring("from Mr / Mrs / M/s", 20) & r.Fields(1)
        Printer.FontBold = False
    End If
    
    
    
    Printer.Print "Towards " & r.Fields(4)
    Printer.Print ""
    Printer.Print ""
    Printer.FontSize = 10
    Printer.FontBold = True
    If chk = 1 Then
        Printer.Print "Rs: " & Chr(14) & fixCur(Val(r.Fields(3)), 2) & "/-" & Chr(17)
    Else
        Printer.Print "Rs: " & Chr(14) & fixCur(Val(r.Fields(2)), 2) & "/-" & Chr(17)
    End If
    Printer.FontSize = 8
    Printer.FontBold = False
    Printer.Print ""
    Printer.Print ""
    Printer.Print ""
    Printer.Print fixstring("Consignee", 27) & fixLeftString("Cashier / Receiptionist", 40)
    Printer.Print ""
    Printer.Print ""
    Printer.Print ""
    Printer.Print fixLeftString("For " & items(0), 67)
End If
r.Close
Printer.EndDoc


End Sub
 

'create new Method for Bank Reconselation Statement on 2-Apr-13
Public Function nameWiseClosingBalance(s As String, d As Date) As Double 'for BRS
On Error GoTo errHandler
Dim r As New ADODB.Recordset
Dim dd As Date
dd = financialStart(d)
Dim n As Double
n = 0

'On Error GoTo errH
r.Open "select * from finOpeningBalances where aname = '" & Trim(s) & "' and dat = " & newStrDate(dd) & " and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
If r.BOF = True And r.EOF = True Then
n = 0
Else
n = r.Fields(2) - r.Fields(3)
End If
r.Close
'errH:



'r.Open "select * from finexec where dat < " & newStrDate(d) & " and aname = '" & Trim(s) & "' and dat>= " & newStrDate(dd) & " and branchid = '" & branchid & "'"

'--------------------------------------------------------------------------------
'r.Open "select sum(cre-deb) from finexec where dat < " & newStrDate(d) & " and aname = '" & Trim(s) & "' and dat>= " & newStrDate(dd) & " and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic
'above query i changed on 1-Apr-13
'------------------------------------------------------------------------------------

r.Open "select sum(cre-deb) from finexec where dat <= " & newStrDate(d) & " and aname = '" & Trim(s) & "' and dat>= " & newStrDate(dd) & " and branchid = '" & branchID & "'", db, adOpenDynamic, adLockPessimistic

While r.EOF = False
    'n = n + r.Fields(2) - r.Fields(3)
    If IsNull(r.Fields(0)) = False Then
    n = n + r.Fields(0)
    End If
    r.MoveNext
Wend
r.Close

nameWiseClosingBalance = n
Exit Function
errHandler:
nameWiseClosingBalance = 10
End Function



'Cash Outward                               COW
'Cash inward                                CIW
'Cash withdrawls                            CWD
'Cash Deposits                              CDP
'Cash to cash                               C2C

'Cheque Issues                              BOW
'Cheque Receipts                            BIW
'Group DD Transactions                      BDD
'Bank to Bank Transactions                  B2B

'Journals                                   JOT

'Cash Purchases                             CPU
'Bank Purchases                             BPU
'Credit Purchaes                            JPU

'Cash Purchase Returns                      CPR
'Bank Purchase Returns                      BPR
'Credit Purchae Returns                     JPR

'Cash Sales                                 CSA
'Bank Sales                                 BSA
'Credit Sales                               JSA


'Cash Sale Returns                          CSR
'Bank Sale Returns                          BSR
'Credit Sale Returns                        JSR



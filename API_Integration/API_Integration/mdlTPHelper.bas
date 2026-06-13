Attribute VB_Name = "mdlTPHelper"
Option Explicit
'###########################################
'# mdlTPHelper                             #
'# Author: Jonas Wolz                      #
'# This module contains utility            #
'# functions for use with clsTablePrint.   #
'# This module is not needed by the        #
'# class !                                 #
'# --------------------------------------- #
'# Function list:                          #
'# Sub ImportFlexGrid( clsTP As _          #
'#   clsTablePrint, flxGrd As MSFlexGrid): #
'#   This function reads the               #
'#   data from flxGrd into clsTP.          #
'###########################################



Private fntOld As StdFont

'ImportFlexGrid:
' This Sub reads the FlexGrid specified by flxGrd into clsTP.
Sub ImportFlexGrid(clsTP As clsTablePrint, flxGrd As MSFlexGrid, Optional ByVal sngDesiredWidth As Single = -1)
    Dim lRow As Long, lCol As Long
    Dim sngFXGGesWidth As Single
    
    clsTP.Rows = flxGrd.Rows - flxGrd.FixedRows
    clsTP.Cols = flxGrd.Cols
    clsTP.HeaderRows = flxGrd.FixedRows
    clsTP.HasFooter = False
    clsTP.LineThickness = flxGrd.GridLineWidth
    'Use double line width
    clsTP.HeaderLineThickness = 2 * clsTP.LineThickness

    'Set the row height
    clsTP.RowHeightMin = flxGrd.RowHeightMin
    clsTP.FooterRowHeightMin = clsTP.RowHeightMin
    clsTP.HeaderRowHeightMin = clsTP.RowHeightMin
    
    'Use some reasonable default values:
    clsTP.CellXOffset = 60
    clsTP.CellYOffset = 30
    clsTP.CenterMergedHeader = False
    clsTP.ResizeCellsToPicHeight = True
    clsTP.PrintHeaderOnEveryPage = True
    
    Set fntOld = New StdFont
    With flxGrd
        sngFXGGesWidth = 0
        For lRow = 0 To .FixedRows - 1
            For lCol = 0 To .Cols - 1
                .Col = lCol
                .Row = lRow '+ .FixedRows
                Set clsTP.HeaderFont(lRow, lCol) = GetGridFont(flxGrd)
                If (lRow = 0) Then
                    Select Case .FixedAlignment(lCol) '.CellAlignment
                    Case flexAlignLeftTop, flexAlignLeftBottom, flexAlignLeftCenter
                        clsTP.ColAlignment(lCol) = eLeft
                    Case flexAlignRightTop, flexAlignRightBottom, flexAlignRightCenter
                        clsTP.ColAlignment(lCol) = eRight
                    Case flexAlignCenterTop, flexAlignCenterBottom, flexAlignCenterCenter
                        clsTP.ColAlignment(lCol) = eCenter
                    Case flexAlignGeneral 'Always Left here
                        clsTP.ColAlignment(lCol) = eLeft
                    End Select
                    sngFXGGesWidth = sngFXGGesWidth + .ColWidth(lCol)
                End If
                clsTP.HeaderText(lRow, lCol) = .Text
            Next
            clsTP.MergeHeaderRow(lRow) = .MergeRow(lRow)
        Next
        For lCol = 0 To .Cols - 1
            For lRow = 0 To .Rows - .FixedRows - 1
                .Col = lCol
                .Row = lRow + .FixedRows
                Set clsTP.FontMatrix(lRow, lCol) = GetGridFont(flxGrd)
                If Not (.CellPicture Is Nothing) Then
                    If .CellPicture.Handle <> 0 Then
                        Set clsTP.PictureMatrix(lRow, lCol) = .CellPicture
                    End If
                End If
                clsTP.TextMatrix(lRow, lCol) = .Text
                If (lCol = 0) Then
                    clsTP.MergeRow(lRow) = .MergeRow(lRow)
                End If
            Next
            If sngDesiredWidth > 0 Then
                clsTP.ColWidth(lCol) = (.ColWidth(lCol) / sngFXGGesWidth) * sngDesiredWidth
            Else
                clsTP.ColWidth(lCol) = .ColWidth(lCol)
            End If
            clsTP.MergeCol(lCol) = .MergeCol(lCol)
            clsTP.MergeHeaderCol(lCol) = .MergeCol(lCol)
        Next
    End With
End Sub

'Helper Function for ImportFlexGrid()
Private Function GetGridFont(flxGrd As MSFlexGrid) As StdFont
    Dim bDiff As Boolean
    
    If fntOld Is Nothing Then bDiff = True: GoTo DiffCheck
    'Font styles:
    bDiff = bDiff Or (flxGrd.CellFontBold <> fntOld.Bold) Or _
            (flxGrd.CellFontItalic <> fntOld.Italic) Or (flxGrd.CellFontUnderline <> fntOld.Underline) Or _
            (flxGrd.CellFontStrikeThrough <> fntOld.Strikethrough)
    'Name:
    bDiff = bDiff Or (flxGrd.CellFontName <> fntOld.Name)
    'Size:
    bDiff = bDiff Or (flxGrd.CellFontSize <> fntOld.Size)
DiffCheck:
    If bDiff Then
        Set fntOld = New StdFont
        fntOld.Name = flxGrd.CellFontName
        fntOld.Size = flxGrd.CellFontSize
        fntOld.Bold = flxGrd.CellFontBold
        fntOld.Italic = flxGrd.CellFontItalic
        fntOld.Underline = flxGrd.CellFontUnderline
        fntOld.Strikethrough = flxGrd.CellFontStrikeThrough
    End If
    Set GetGridFont = fntOld
End Function



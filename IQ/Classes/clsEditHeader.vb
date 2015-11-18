'Option Strict On

Imports dataAccess
Public Class clsEditHeader

    Public path As String
    Public matrix As clsScreen
    Public Filters As Dictionary(Of clsField, Dictionary(Of clsFilter, String)) = Nothing  'This a a dictionary of the ACTIVE filters, per field - each field can have more than one filter applied at once
    Public sorts As Dictionary(Of Integer, clsPriorityDirection)
    Public ColWidth As Dictionary(Of clsField, Single)
    Public Fromindex As Integer
    Public PerPage As Integer
    'Public divID As String 'Panel
    Public translationLanguage As clsLanguage
    Public VW As DataView
    Public DT As DataTable

    'If Not iq.SeshContains(bi.lid, "matrix." & bi.path) Then
    '            iq.sesh(bi.lid, "sorts." & bi.path$) = matrix.DefaultSorts  'when the matrix button is pressed.. set the default sort orders and filters (per the DefaultFilter and Defautl sort Properties of the clsFields within the clsScreen (aka matrix)
    '            iq.sesh(bi.lid, "colstate." & bi.path) = New Dictionary(Of clsField, enumColState) 'holds the expanded/collapsed info
    '            iq.sesh(bi.lid, "filters." & bi.path) = New Dictionary(Of clsField, Dictionary(Of clsFilter, String))  'matrix.DefaultFilters
    '            iq.sesh(bi.lid, "matrix." & bi.path) = matrix.ID 'True
    '        End If

    Public Sub New(Path As String, dicOrObj As Object, matrix As clsScreen, buyeraccount As clsAccount, Language As clsLanguage, ByRef errormessages As List(Of String), lid As UInt64)

        If matrix Is Nothing Then Stop

        Me.path = Path
        Me.matrix = matrix
        Me.Filters = New Dictionary(Of clsField, Dictionary(Of clsFilter, String))  'TODO  default filters
        Me.sorts = matrix.DefaultSorts
        Me.ColWidth = New Dictionary(Of clsField, Single)
        '   Me.divID = divid 'panel
        Me.translationLanguage = Language

        If IsDictionary(dicOrObj) Then
            Me.DT = MakeStubDataTable(dicOrObj)
            Me.VW = New DataView(DT)
            addMissingColumns(dicOrObj, buyeraccount, Language, New HashSet(Of String), errormessages, lid)
        End If
    End Sub

    Public Function RowIndex(id As Integer) As Integer

        'Returns -1 if a there is no row in the VIEW with an ID column containing ID
        'NB: The underlying tdatatable *may* contain such a row - bit the view might be filtering it

        Dim keep As String = VW.Sort
        Me.VW.Sort = "ID"
        RowIndex = VW.Find(id)
        Me.VW.Sort = keep

    End Function

    Private Function MakeStubDataTable(dic As Object) As DataTable

        Dim col As DataColumn
        col = New DataColumn("ID", GetType(Int32))

        Dim dt As DataTable = New DataTable
        dt.Columns.Add(col)

        dt.PrimaryKey = New DataColumn() {col} 'Set the PK on the datatable so we can FIND (primarily to delete)

        Dim c(0) As Object 'ID column in the data table
        For Each k In dic.Keys
            c(0) = k
            dt.Rows.Add(c)
        Next

        Return dt

    End Function

    Public Sub addRow(lid As UInt64, instance As Object, screen As clsScreen, buyerAccount As clsAccount, language As clsLanguage, ByRef errormessages As List(Of String))

        'Adds a row, containing the essential columns (for the dataview - which is providing filtering and sorting on the dictionary we're editing)

        Dim row As List(Of Object) = New List(Of Object)  'this is an array of fields that will form the new row
        For Each c As DataColumn In DT.Columns

            'find the field which fills this column (in the datatable)
            Dim ff = From f As clsField In screen.Fields.Values Where f.propertyName = c.ColumnName
            If ff.Any Then
                Dim fld As clsField = ff.First
                row.Add(fld.CellValue(instance, fld.propertyName, buyerAccount, language, "", Nothing, False, Nothing, Nothing, New HashSet(Of String), errormessages, lid))
            End If
        Next

        DT.Rows.Add(row.ToArray)

    End Sub

    Public Sub fillCol(f As clsField, col As DataColumn, dic As Object, buyeraccount As clsAccount, language As clsLanguage, foci As hashset(Of String), ByRef errormessages As List(Of String), lid As UInt64)

        'This is specifically for the the editor - Dic is allways a dictionary of integer>clsSomething
        Dim r As Integer = 0
        For Each id In dic.Keys

            DT.Rows(r).Item(col) = f.CellValue(dic(id), f.propertyName, buyeraccount, language, "", Nothing, False, Nothing, Nothing, foci, errormessages, lid)
            '            DT.Rows(r).Item(col) = f.CellValue(dic(id), prop, buyeraccount, language, Nothing, False, Nothing, Nothing, foci, errormessages)
            r += 1
        Next

    End Sub

    Public Sub addMissingColumns(dic As Object, buyeraccount As clsAccount, Language As clsLanguage, foci As hashset(Of String), ByRef errormessages As List(Of String), lid As UInt64)

        'dic is always a dictionary of integer>someCls  (clsUser,clsAccount,clsField,ClsWhatever)

        Dim col As DataColumn
        For Each fld In Me.Filters.Keys
            If Not DT.Columns.Contains(fld.propertyName) Then
                If fld.InputType.code = "string" Then
                    col = New DataColumn(fld.propertyName, GetType(String)) 'Single))
                Else
                    col = New DataColumn(fld.propertyName, GetType(Int64)) 'Single))
                End If

                DT.Columns.Add(col)
                Me.fillCol(fld, col, dic, buyeraccount, Language, New HashSet(Of String), errormessages, lid)
            End If
        Next

        'do the same for sorts (add a column to the underlying datatable for everything we're sorting on)
        For Each so In sorts.Values
            If Not DT.Columns.Contains(so.column.propertyName) Then 'Dont add the same column twice (we may already have added it for filtering)


                If so.column.InputType.code = "string" Then
                    col = New DataColumn(so.column.propertyName, GetType(String)) 'Single))
                Else
                    col = New DataColumn(so.column.propertyName, GetType(Int64)) 'Single))
                End If

                DT.Columns.Add(col)
                Me.fillCol(so.column, col, dic, buyeraccount, Language, New HashSet(Of String), errormessages, lid)
            End If
        Next

        Dim ft As String = ConstructFilter()

        Me.VW.RowFilter = ft


    End Sub

    Public Sub setColWidth(fld As clsField, width As Single)
        ColWidth(fld) = width
    End Sub

    ''' <summary>Used for debugging - displays a textual representatiom of the current filters applied by this matrixHeader</summary>
    Public Function currentFilters() As String

        currentFilters = ""
        For Each fld In Filters.Keys
            currentFilters &= (fld.displayName(English) & ":")
            For Each fltVp In Filters(fld)
                currentFilters &= fltVp.Key.DisplayText.text(English) & "|" & fltVp.Value & " "
            Next
        Next

    End Function

    Public Sub UpdateSorts(NewPriority As clsPriorityDirection)

        'gives us a Field, priority and direction to update
        If Not Me.sorts.ContainsKey(NewPriority.Priority) Then
            Me.sorts.Add(NewPriority.Priority, NewPriority)
        Else
            Me.sorts(NewPriority.Priority) = NewPriority
        End If
    End Sub

    Public Function SortsString() As String

        Return ""

    End Function

    Public Sub RemoveFilter(toRemove As String)

        'to remove contains the fieldID^Filter ID

        Dim p() As String = Split(toRemove, "|")
        Me.Filters(iq.Fields(CInt(p(0)))).Remove(iq.i_Filters_Code(p(1)))

        Me.VW.RowFilter = ConstructFilter()

    End Sub

    Public Sub UpdateFilters(fieldID As Integer, filterCode As String, Value As String) 'TextFrag As String)

        'Uses the changefilter parameter which
        'looks like 738|GE|1.02
        'Field ID, Filter Code (operator), New operand (value)

        'Dim c$()
        'c$ = Split(changefilter, "|")

        'If UBound(c) <> 2 Then
        '    Beep()
        'End If

        Dim fld As clsField
        Dim flt As clsFilter
        fld = iq.Fields(fieldID)
        flt = iq.i_Filters_Code(filterCode)  'Greater than, Equal, Less than like etc..

        With Me.Filters
            If Not .ContainsKey(fld) Then .Add(fld, New Dictionary(Of clsFilter, String))
            If Not Me.Filters(fld).ContainsKey(flt) Then Me.Filters(fld).Add(flt, "")
            Me.Filters(fld)(flt) = Value 'sortValue(TextFrag) 'may contain an integer            
        End With

    End Sub

    Private Function ConstructFilter() As String

        'Turns the current filters dictionary -  into something actually usable by the dataview
        'note, the operator segment generaly contains [filterValue] and [col] placeholders - which is replaced with the value segment and column name
        'thus  738|SW|4
        'becomes
        '[Displayname]=LIKE 'home*' AND [years]=4

        ConstructFilter = ""
        If Filters Is Nothing Then Exit Function

        For Each fld In Filters.Keys
            For Each flt In Filters(fld).Keys
                'Where we have a string value - it needs to be 'quoted' (form factors)

                Dim colname$
                Dim qp$

                qp$ = Replace(flt.Filter, "[filterValue]", Me.Filters(fld)(flt))  'grab the template fo rthis filter criterea and replace the current value

                colname$ = "[" & fld.propertyName & "]"
                qp$ = Replace(qp$, "[col]", colname$)
                ConstructFilter &= qp$ & " AND "
            Next
        Next

        If ConstructFilter <> "" Then ConstructFilter = Left(ConstructFilter, Len(ConstructFilter) - 5) 'Take the last AND off

    End Function

    Public Function UI(buyeraccount As clsAccount, language As clsLanguage, ByRef errorMessages As List(Of String), inpanel As Panel) As Panel

        'this uses the path of the branch being displayed as a matrix - which IS NOT the same as the matrix path - the actual matrix in used may be defined much higher (eg. at the 'servers' level')..
        'But a set of filters, sorts, and collapsed columns is maintained for each branch displaying its children as a matrix (for example each server family branch)
        'This is difficult to grasp - but it's vital you understand before messing with it (or you will create some horrible bugs)

        'Returns the user facing UI for filtering and sorting this matirx
        UI = New Panel
        UI.CssClass = "editHeader"
        Dim drpLanguage As DropDownList = New DropDownList()
        drpLanguage.DataSource = iq.ActiveLanguages.Values
        drpLanguage.DataTextField = "LocalName"
        drpLanguage.DataValueField = "ID"
        'drpLanguage.ID = Me.divID & "_lang"
        drpLanguage.ID = Me.path & "_lang"
        drpLanguage.SelectedValue = translationLanguage.ID
        drpLanguage.DataBind()
        ' Dim dropdownScript As String = "var ddl = document.getElementById('" & Me.divID & "_lang" & "'); var spd=ddl.options[ddl.selectedIndex].value;"
        Dim dropdownScript As String = "var ddl = document.getElementById('" & Me.path & "_lang" & "'); var spd=ddl.options[ddl.selectedIndex].value;"
        'dropdownScript &= "embed('../editor/editor.aspx?cmd=language&path=" & path$ & "&value='+spd" & ",'" & Me.divID & "',false,false);return false;"
        dropdownScript &= "embed('../editor/editor.aspx?cmd=language&path=" & path$ & "&value='+spd" & ",'" & Me.path & "',true,false);return false;"
        drpLanguage.Attributes("onchange") = dropdownScript
        UI.Controls.Add(drpLanguage)
        UI.Controls.Add(Me.matrix.EditTitles(Me, language))
        UI.Controls.Add(Me.FiltersUI(language, inpanel.ID, errorMessages)) '.MatrixPath, bi.lid, bi.AgentAccount.Language)) ' todo - reinstate
        UI.Controls.Add(Me.SortsUI)
        UI.Controls.Add(Me.ControlsUI)


    End Function

    Private Function ControlsUI() As Panel
        'Adds the widen,narrow,promote,demote, show and hide buttons for a column

        ControlsUI = New Panel

        ControlsUI.Controls.Add(NewLit("<div class='efcSpacer'></div>"))

        For Each f In From v In Me.matrix.Fields.Values Order By v.order  'iterate the fields in order of their Order property

            Dim col As Panel = f.emptyCell(Not f.visibleList, True)
            ControlsUI.Controls.Add(col)

            Dim fid As String = Trim(f.ID.ToString)

            If Not f.visibleList Then
                'Hidden columns Only have a 'show' button
                'col.Width = New Unit(1, ut)
                col.Controls.Add(editor.MakeButton(True, "S", Xlt("Show this column", Me.translationLanguage) & "(" & f.propertyName & ")", editor.EmbedScript(path$, path$, "show," & fid, False, True)))
                'btn = ControlButton("S", 
                'col.Controls.Add(btn)
                'btn.Attributes("onmousedown") = 
            Else

                col.Controls.Add(editor.MakeButton(True, "+", Xlt("widen this column", Me.translationLanguage), editor.EmbedScript(path$, path$, "widen," & fid, False, True)))

                If f.width > 1 Then  'don't let them narrow a column to nothing !!
                    col.Controls.Add(editor.MakeButton(True, "-", Xlt("Narrow this column", Me.translationLanguage), editor.EmbedScript(path$, path$, "narrow," & fid, False, True)))
                End If

                col.Controls.Add(editor.MakeButton(True, "H", Xlt("Hide this column", Me.translationLanguage), editor.EmbedScript(path$, path$, "hide," & fid, False, True)))
                col.Controls.Add(editor.MakeButton(True, "<", Xlt("Promote this column", Me.translationLanguage), editor.EmbedScript(path$, path$, "promote," & fid, False, True)))
                col.Controls.Add(editor.MakeButton(True, ">", Xlt("Demote this column", Me.translationLanguage), editor.EmbedScript(path$, path$, "demote," & fid, False, True)))

            End If
        Next f

    End Function

    Public Sub changeLayout(cmd As String, fld As clsField, ByRef errorMessages As List(Of String))

        Select Case cmd

            Case Is = "widen"

                fld.width += 1 : fld.update(errorMessages)

                'If Me.ColWidth(fld) > 100 Then
                '    errorMessages.Add("You can't widen this column any further")
                'Else
                '    'Me.ColWidth(fld) += 1
                'End If

            Case Is = "narrow"

                fld.width -= 1
                fld.update(errorMessages)


                'If Me.ColWidth(fld) > 1 Then
                '    Me.ColWidth(fld) -= 1
                'Else
                '    errorMessages.Add("You can't narrow this column any further - you can hide it if you like")
                'End If
            Case Is = "promote"
                fld.promote(errorMessages) 'hmm this means the only have one, global position - the editHeader should probably contain a sorted list to make this per user
            Case Is = "demote"
                errorMessages.Add("Demote is not supported")
                '                fld.demote(errorMessages) 'hmm this means the only have one, global position - the editHeader should probably contain a sorted list to make this per user
            Case Is = "demote"
                fld.demote() 'hmm this means the only have one, global position - the editHeader should probably contain a sorted list to make this per user
            Case Is = "show"
                fld.visibleList = True
            Case Is = "hide"
                fld.visibleList = False
            Case Else
                errorMessages.Add("Unrecognised changelayout cmd " & cmd)

        End Select

        fld.update(errorMessages) 'persist the changes in the DB (expensive)

    End Sub

    Private Function FiltersUI(language As clsLanguage, holdingRowID As String, ByRef errormessages As List(Of String)) As Panel

        'returns a panel containing Filtering UI for the EDITOR - A full set of columns

        Dim pnl As Panel = New Panel
        Dim col As Panel

        ' pnl.Controls.Add(ContrastSpacer) 'make room for the 'contrast' checkboxes

        pnl.CssClass = "editFilters"
        'pnl.Attributes("style") = "display:inline-block;"
        'for each column in the matrix we can have a value for each filter - so we can say >5 AND <10
        Dim AreSome As Boolean = False

        pnl.Controls.Add(NewLit("<div class='efcSpacer'></div>"))
        For Each fld In From v In Me.matrix.Fields.Values Order By v.order  'iterate the fields in order of their Order property

            col = fld.emptyCell(Not fld.visibleList, True) 'Me.ColIsCollapsed(fld))
            pnl.Controls.Add(col)

            If fld.visibleList Then

                ' Dim ddl As DropDownList = fld.OperatorDDL() - this *was* the operator DDL (Deprecated)
                ' ddl.ID = "ops." & holdingRowID & "." & fld.ID 'give this DDL a uniqueID
                ' col.Controls.Add(ddl)

                Dim pnlTxt As Panel = New Panel
                col.Controls.Add(pnlTxt)

                Dim valueControlID As String
                If fld.InputType.code = "one" Then
                    'One type fields get an autoSuggest

                    Dim currentindex As Int64 = 0  'This is an FK/Pointer
                    If Not Me.Filters Is Nothing Then
                        If Me.Filters.ContainsKey(fld) Then
                            currentindex = CInt(Me.Filters(fld).Values(0))
                        End If
                    End If

                    Dim selectedtarget As Object
                    If currentindex = 0 Then
                        selectedtarget = Nothing
                    Else
                        selectedtarget = Reflection.WalkPropertyValue(iq, fld.lookupOf, errormessages)(currentindex) 'this object is the 'target' of the foregin key - and contains the selected value 
                    End If

                    valueControlID = "f_" & Me.path & "f" & fld.ID
                    Dim suggest As Panel = FilledDDL(fld, selectedtarget, language, valueControlID, True, Nothing, 0, errormessages) 'Obj2 carries the ID
                    'for 'one' type fields this is a autocomplete DDL - which yields the FK to macth on
                    pnlTxt.Controls.Add(suggest)

                ElseIf fld.InputType.code = "translate" Then
                    'One type fields get an autoSuggest

                    Dim currentindex As Int64 = 0  'This is an FK/Pointer
                    If Not Me.Filters Is Nothing Then
                        If Me.Filters.ContainsKey(fld) Then
                            currentindex = CInt(Me.Filters(fld).Values(0))
                        End If
                    End If

                    Dim selectedTranslation As clsTranslation
                    If currentindex = 0 Then
                        selectedTranslation = Nothing
                    Else
                        selectedTranslation = iq.Translations(currentindex) 'this object is the 'target' of the foregin key - and contains the selected value 
                    End If

                    valueControlID = "f_" & Me.path & "f" & fld.ID
                    Dim suggest As Panel = FilledTranslation(fld, selectedTranslation, language, valueControlID, True, Nothing, 0, errormessages) 'Obj2 carries the ID
                    'for 'one' type fields this is a autocomplete DDL - which yields the FK to macth on
                    pnlTxt.Controls.Add(suggest)

                Else

                    Dim ft As TextBox = New TextBox  'Filtert tex 'EG dl38'
                    ft.ID = "ft." & holdingRowID & "." & fld.ID
                    ft.CssClass = "editFilterTextBox"
                    ' ft.Attributes("style") &= "width:" & CStr(fld.width - 1.5) & "em;"
                    pnlTxt.Controls.Add(ft)
                    valueControlID = ft.ID

                    'populate with any exisitng (filter) value
                    If Me.Filters.ContainsKey(fld) Then
                        If Me.Filters(fld).Count Then
                            If fld.InputType.code = "string" Then
                                ft.Text = Me.Filters(fld)(iq.i_Filters_Code("CN"))  'populate the textbox with the CONTAINS substring filter (for strings) 
                            Else
                                ft.Text = Me.Filters(fld)(iq.i_Filters_Code("EQ"))  'populate the textbox with the 
                            End If
                        End If
                    End If
                End If

                'unfinished
                'CMD is the cmd parameter of the Ajax callback 
                'Dim cmd$ = "changefilter," & fld.ID & ",'"  '<<-IMPORTANT SINGLE QUOTE - breaks out of the JS literal string and back into JS    
                'cmd$ &= "+getElementById('" & ddl.ID & "').value +','+getElementById('" & valueControlID & "').value " '  'this is the filters code
                'cmd$ &= "+'" 'back into the literal string

                Dim op As String
                If fld.InputType.code = "string" Then
                    op = "CN"
                Else
                    op = "EQ"
                End If

                Dim cmd$ = "changefilter," & fld.ID & "," & op & ",'+getElementById('" & valueControlID & "').value +'" 'Some very importanst 's  in here breaking in and out of JS
                pnlTxt.Controls.Add(editor.MakeButton(True, "go", "apply this filter", editor.EmbedScript(Me.path, Me.path, cmd$, False, True)))

                Dim cancelButtons As Panel = New Panel
                cancelButtons.CssClass = "editFilterCancel"
                pnlTxt.Controls.Add(cancelButtons)

                'remove' buttons for the (currently applied) filters

                If Not Me.Filters Is Nothing Then
                    If Me.Filters.ContainsKey(fld) Then
                        For Each flt In Me.Filters(fld).Keys  'ME.filters are those 'active' filters in this header 
                            cancelButtons.Controls.Add( _
                            editor.MakeButton(True, "x", "Remove this filter " & fld.propertyName & " " & flt.DisplayText.text(language) & " " & Me.Filters(fld)(flt), _
                            editor.EmbedScript(Me.path, Me.path, "removeFilter," & Trim$(fld.ID.ToString) & "|" & Trim$(flt.Code), False, True)))
                            AreSome = True
                        Next
                    End If
                End If
            End If
        Next

        ' pnl.CssClass = "filtersUI"

        ' If AreSome Then pnl.Attributes("style") &= "height:" & collapsedColumnWidth & "em;"

        Return pnl

    End Function

    Public Function SortsUI() As Panel

        SortsUI = New Panel
        SortsUI.CssClass = "editSorts"


        SortsUI.Controls.Add(NewLit("<div class='efcSpacer'></div>"))
        Dim col As Panel

        For Each f In From v In Me.matrix.Fields.Values Order By v.order  'iterate the fields in order of their Order property

            col = f.emptyCell(Not f.visibleList, True)  'Me.ColIsCollapsed(f))
            col.ID = path$ & ".F" & f.ID

            SortsUI.Controls.Add(col)

            If f.visibleList Then

                Dim currentValue As String = "-"

                'ptb = f.SortPriorityTextBox(Me.sorts$, currentValue)
                'occ$ = "sortPath='" & path$ & "';sortFieldID=" & f.ID & ";showSortPriorityPicker('" & col.ID & "','" & currentValue & "');"
                'ptb.Attributes("onfocus") = occ$
                ' ptb.ToolTip = "Set the priority and direction of this sorting for this column"
                ' col.Controls.Add(ptb)

            End If
        Next f

    End Function

End Class

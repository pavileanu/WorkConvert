Imports System.Globalization
Imports System.IO

<Serializable> Public Class clsMatrixHeader

    Public path As String
    Public matrix As clsScreen
    Public Filters As Dictionary(Of clsField, Dictionary(Of clsFilter, List(Of Int64))) = Nothing
    Public sorts As Dictionary(Of Integer, clsPriorityDirection) 'The priority is the key (makes sense, honest)
    Public ColState As Dictionary(Of clsField, enumColState)

    Public Vw As System.Data.DataView
    Public DT As System.Data.DataTable

    Public QuickFiltersVisible As Boolean
    Private dicTrans As Dictionary(Of clsField, Dictionary(Of clsTranslation, Integer)) 'Holds the surviving count (distinct) translations for this Quick filter - populated my addmissingcolumns
    Private dicBands As Dictionary(Of clsField, List(Of clsMatrixHeader.clsBand)) 'QuickFilter fields of type BANDS a
    'Private dicAttrib As Dictionary(Of clsfiled, Dictionary(clsattribute,integer))  'holds the count of
    Private dicNums As Dictionary(Of clsField, Dictionary(Of Int64, Integer)) 'All the DISTINCT numeric values (and the survivor counts thereof)
    Private dicUnits As Dictionary(Of clsField, clsUnit) 'for each (numeric) field detect and validate the UNITS
    Private lid As UInt64
    Private _FieldResultSet As Dictionary(Of clsField, clsAccountScreenField)
    Public ReadOnly Property FieldResultSet As Dictionary(Of clsField, clsAccountScreenField)
        Get
            If _FieldResultSet Is Nothing Then
                Dim l As clsAccount = iq.sesh(lid, "BuyerAccount")
                Dim errormessages As List(Of String) = New List(Of String)
                'TODO add default display unit here
                Dim asa As IEnumerable(Of clsScreenOverride) = iq.ScreenOverrides.Where(Function(so) so.AccountID = l.ID And so.ScreenID = Me.matrix.ID And so.Path = Me.path).Select(Function(dd) dd)
                Dim screenOverrideObjects =
                    From s In iq.Screens(Me.matrix.ID).Fields.Values
                        Group Join a In asa On s.ID Equals a.FieldId Into lr = Group From m In lr.DefaultIfEmpty()
                                Select New With {
                                    .a = New clsAccountScreenField With {.AccountID = l.ID, .ScreenID = Me.matrix.ID, .Path = path, .FieldId = s.ID, .Visibility = If(m Is Nothing OrElse m.ForceVisibilityTo Is Nothing, s.visibleList, m.ForceVisibilityTo), .Order = If(m Is Nothing OrElse m.ForceOrderTo Is Nothing, s.order, m.ForceOrderTo), .Width = If(m Is Nothing OrElse m.ForceWidthTo Is Nothing, If(s.width = 0, Nothing, s.width), m.ForceWidthTo), .Description = s.labelText, .DisplayUnit = If(m Is Nothing OrElse m.DisplayUnit Is Nothing, Nothing, m.DisplayUnit)},
                                    .b = s}

                If AccountHasRight(lid, "GLOBALADM") Then
                    _FieldResultSet = screenOverrideObjects.ToDictionary(Function(a) a.b, Function(a) a.a)
                Else
                    _FieldResultSet = screenOverrideObjects.Where(Function(f) f.b.CanUserSelect).ToDictionary(Function(a) a.b, Function(a) a.a)
                End If
            End If
            Return _FieldResultSet
        End Get
    End Property

    Public ReadOnly Property AllAvailableFields() As List(Of clsAccountScreenField)
        Get
            'Get a list of all fields from the screen and populate with any overrides for this user
            Return FieldResultSet.Select(Function(a) a.Value).ToList()
        End Get
    End Property
    Public ReadOnly Property EffectiveFields() As List(Of clsField)
        Get
            'Get a list of all fields from the screen and populate with any overrides for this user
            Return FieldResultSet.Where(Function(a) a.Value.Visibility Or iq.Fields.Values.Where(Function(al) If(al.LinkedFieldID IsNot Nothing, al.LinkedFieldID, -1) = a.Key.ID).Count > 0).OrderBy(Function(d) d.Value.Order).Select(Function(a) a.Key).ToList()
        End Get
    End Property
    Public ReadOnly Property EffectiveFieldsWithFilters As List(Of clsField)
        Get
            'Get a list of all fields from the screen and populate with any overrides for this user
            Return FieldResultSet.Where(Function(a) a.Value.Visibility Or a.Key.QuickFilterGroup IsNot Nothing Or iq.Fields.Values.Where(Function(al) If(al.LinkedFieldID IsNot Nothing, al.LinkedFieldID, -1) = a.Key.ID).Count > 0).OrderBy(Function(d) d.Value.Order).Select(Function(a) a.Key).ToList()
        End Get
    End Property

    ''' <summary>Export the grid as CSV - respecting the filters and sorts</summary>
    ''' <remarks>Column visibility (collapsedosity) and ordering are not yet respected </remarks>
    Public Function exportCSV(lid As UInt64, descendants As Dictionary(Of clsBranch, clsVisibility), buyeraccount As clsAccount, _
                           l As clsLanguage, foci As HashSet(Of String), ByRef errorMessages As List(Of String)) As String

        Randomize()
        Dim fn$ = System.IO.Path.GetTempPath & "\export-" & buyeraccount.ID & Rnd(1).ToString & ".csv" 'this isn't terribly robust 

        iq.sesh(lid, "tostream") = fn$
        iq.sesh(lid, "streamcontent-type") = "text/csv;charset=UTF-8""  "
        iq.sesh(lid, "DeleteStreamed") = True

        Try
            ' A 'normal' filestream removed the frist CRLF (on the header row WTF ?)
            Dim sw As StreamWriter = New StreamWriter(File.Create(fn$), System.Text.Encoding.UTF8)


            'Write the quoted names of the columns (in the agent's languae)
            sw.WriteLine(Me.headerRow(l))


            For i As Integer = 0 To Me.Vw.Count - 1

                Dim bid As Integer = CInt(Me.Vw(i).Item("ID"))
                Dim branch As clsBranch = iq.Branches(bid)
                Dim vis As clsVisibility = descendants(branch)
                Me.exportRow(lid, vis, sw, buyeraccount, l, foci, errorMessages)
            Next

            sw.Close()


        Catch ex As System.Exception
            errorMessages.Add(ex.Message.ToString & " (Could not create the file ? )" & fn$)

        End Try

    End Function

    Public Function headerRow(language) As String

        Dim qc As List(Of String) = New List(Of String)
        For Each field In Me.matrix.Fields.Values
            If field.visibleList Then
                qc.Add(Utility.CSV(field.labelText.text(language)))
            End If
        Next

        Return Join(qc.ToArray, ",")

    End Function


    'clone method should copy the filters and sorts of - and use the datatable for columns that exist  - and 
    Private Function MakeStubDataTable(buyeraccount As clsAccount, showall As Boolean, dic As Object, ByRef errors As List(Of String)) As DataTable

        Dim culture As String = buyeraccount.BuyerChannel.Region.Culture

        Dim dt As DataTable = New DataTable() 'PathName(bi.path)) 'give it a pretty name (for debugging)
        Dim ci As CultureInfo = Nothing
        Try
            ci = New CultureInfo(culture)
        Catch
            ci = New CultureInfo("EN-gb")
            Beep()
        End Try

        dt.Locale = ci

        Dim col As DataColumn
        col = New DataColumn("ID", GetType(Int32))
        dt.Columns.Add(col)

        Dim c(0) As Object 'ID column in the data table
        For Each vs In dic.values

            ' If vs.branch.product Is Nothing Then Stop
            '  If Not vs.branch.hassku Then Stop

            If vs.hideReasonList.Count = 0 Or showall = True Then
                c(0) = vs.branch.id
                dt.Rows.Add(c)
            End If
        Next

        Return dt

    End Function


    Public Sub exportRow(lid As UInt64, row As clsVisibility, sw As StreamWriter, buyeraccount As clsAccount, language As clsLanguage, _
                         foci As HashSet(Of String), ByRef errorMessages As List(Of String))

        Dim cols As List(Of String) = New List(Of String)
        For Each f In Me.matrix.Fields.Values
            If f.visibleList Then
                cols.Add(f.CSV(row.branch, row.path, buyeraccount, language, Me, False, foci, errorMessages, lid, 0))
            End If
        Next

        sw.WriteLine(Join(cols.ToArray, ","))

    End Sub

    ''' <summary>
    ''' </summary>
    ''' <param name="bi"></param>
    ''' <param name="descendants"></param>
    ''' <param name="errormessages"></param>
    ''' <param name="copyFiltersFrom">Copy the filter values from an existing (usually ancestor) matrix.. used from the 'family finder'</param>
    ''' <remarks></remarks>
    Public Sub New(bi As clsBranchInfo, descendants As Dictionary(Of clsBranch, clsVisibility), quickFiltersVisible As Boolean, ByRef errormessages As List(Of String))

        'If descendants.Count = 0 Then Stop

        'We hold a reference to each matrixheader in the users session (keyed by their login ID)
        Dim matrixHeaders As Dictionary(Of String, clsMatrixHeader) = CType(iq.sesh(bi.lid, "matrixHeaders"), Dictionary(Of String, clsMatrixHeader))
        If matrixHeaders.ContainsKey(bi.path) Then
            matrixHeaders.Remove(bi.path)
        End If

        matrixHeaders.Add(bi.path, Me)

        Me.path = bi.path
        Me.matrix = MatrixAbove(Me.path) 'NOTE: this is the ONLY place we find out what TYPE of matrixHeader we need to instance
        Me.lid = bi.lid

        Dim mha = matrixHeaderAbove(lid, path$, errormessages)
        'If Not mha Is Nothing Then
        '    If mha.DT IsNot Nothing Then Me.DT = mha.Vw.ToTable()
        '    Me.Vw = New DataView(DT)
        'End If
        Me.setup(bi, descendants, errormessages) 'calls addmissingColums - populating any column we're sorting of filtering by

        'Me.setup(bi, descendants, errormessages) 'calls addmissingColums - populating any column we're sorting of filtering by

        Me.Vw.RowFilter = Me.ConstructFilter
        Me.QuickFiltersVisible = quickFiltersVisible

    End Sub

    Public Sub rebuild(bi As clsBranchInfo, descendants As Dictionary(Of clsBranch, clsVisibility), ByRef errormessages As List(Of String))

        'ML - todo why dont we just invalidate this?
        'NA - don' tknow - sounds logical

        '        Me.setup(bi, descendants, errormessages) 'calls addmissingColums - populating any column we're sorting of filtering by
        Me.DT = MakeStubDataTable(bi.buyerAccount, bi.showAll, descendants, errormessages)
        Me.addMissingColumns(descendants, Me.DT, bi.buyerAccount, bi.agentAccount.Language, bi.foci, errormessages, bi.lid)
        'fills the datable (which underlies the dataview we will be returning)


    End Sub

    Public Sub New()
        Stop
    End Sub

    Public Sub New(lid As UInt64, path As String, screen As clsScreen)

        ' Stop

        'special' constructor - used for the root level bootstrap instance
        Me.matrix = screen
        Me.path = path
        Me.ColState = New Dictionary(Of clsField, enumColState)
        Me.DT = Nothing  'these will be populated JIT *if* we switch the root level to a grid
        Me.Vw = Nothing


        'This dictionary of the active filters, sorts and datasets (held in a clsMatrixHeader) - is persisted in each users session (every user has different ones!)
        Dim matrixHeaders As Dictionary(Of String, clsMatrixHeader) = New Dictionary(Of String, clsMatrixHeader)
        matrixHeaders.Add(Me.path, Me)
        iq.sesh(lid, "matrixHeaders") = matrixHeaders

        Me.lid = lid

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

                Dim qp$ = String.Empty
                For Each filt In Me.Filters(fld)(flt)
                    qp$ &= "(" & Replace(flt.Filter, "[filterValue]", filt)  'grab the template fo rthis filter criterea and replace the current value 
                    'Change for GE LE
                    If flt.Code = "LE" AndAlso Filters(fld).ContainsKey(iq.i_Filters_Code("GE")) Then
                        qp$ &= " AND "
                        qp$ &= Replace(iq.i_Filters_Code("GE").Filter, "[filterValue]", Filters(fld)(iq.i_Filters_Code("GE"))(Me.Filters(fld)(flt).IndexOf(filt)))
                    End If
                    qp$ &= ") OR "
                Next
                If qp$ <> "" Then qp$ = Left(qp$, Len(qp$) - 4) 'Take the last AND off

                colname$ = "[" & fld.propertyName & "]"
                qp$ = Replace(qp$, "[col]", colname$)
                ConstructFilter &= "(" & qp$ & ") AND "
            Next
        Next

        If ConstructFilter <> "" Then ConstructFilter = Left(ConstructFilter, Len(ConstructFilter) - 5) 'Take the last AND off

    End Function


    Public Sub setup(bi As clsBranchInfo, descendants As Dictionary(Of clsBranch, clsVisibility), ByRef errormessages As List(Of String))

        Me.Filters = New Dictionary(Of clsField, Dictionary(Of clsFilter, List(Of Int64)))  'TODO  default filters
        Me.sorts = Me.matrix.DefaultSorts  'each field has a defaultsort priority and directon - eg 1A 3D
        Me.ColState = New Dictionary(Of clsField, enumColState)
        Me.dicTrans = New Dictionary(Of clsField, Dictionary(Of clsTranslation, Integer))
        Me.dicBands = New Dictionary(Of clsField, List(Of clsMatrixHeader.clsBand))
        Me.dicNums = New Dictionary(Of clsField, Dictionary(Of Int64, Integer))
        Me.dicUnits = New Dictionary(Of clsField, clsUnit)

        'Me.DT = New DataTable
        'Me.Vw = New DataView(DT)
        '        Me.FillDataTable(bi, descendants, errormessages)

        If Me.DT Is Nothing Then  'this is a very important speed up - we only want to do this ONCE - not every postabck !
            Me.DT = MakeStubDataTable(bi.buyerAccount, bi.showAll, descendants, errormessages)
            'fills the datable (which underlies the dataview we will be returning)
            Me.Vw = New DataView(DT) 'creates a new view onto the 'stub' datatable (wich just contains ID's)
        End If

        If Me.DT.Rows.Count = 0 Then
            errormessages.Add("datatable was empty in FillDatatTable") : Exit Sub
        End If

        Dim flt$
        ' If bi.MatrixHeader IsNot Nothing Then
        'AddmissingColumns populates those columns in the datatable we want to sort or filter by - just in time, and Once ony using reflection
        'all subsequent operations use this pre-loaded data
        addMissingColumns(descendants, DT, bi.buyerAccount, bi.agentAccount.Language, bi.foci, errormessages, bi.lid)  'adds (and populates) any columns we're wanting to sort or filter by to the datatable
        flt = ConstructFilter() 'Me.matrix, bi.MatrixHeader.Filters)

        If flt$ <> "" Then  'don't ever set the filter property to nothing - everything dissapears !
            Try
                Me.Vw.RowFilter = flt$
            Catch

                errormessages.Add("invalid filters " & flt$)
            End Try
        End If

        '       Dim sorts As String
        '        If bi.MatrixHeader IsNot Nothing Then
        '        sorts = bi.MatrixHeader.sortsString
        '  Else
        '  sorts = bi.EditHeader.SortsString
        '  End If

        Dim sorts$ = Me.sortsString
        Try
            Vw.Sort = sorts$
        Catch ex As Exception
            errormessages.Add("invalid sorts " & sorts)
        End Try

    End Sub



    Public Sub addMissingColumns(dic As Dictionary(Of clsBranch, clsVisibility), ByRef dt As DataTable, buyeraccount As clsAccount, language As clsLanguage, foci As HashSet(Of String), ByRef errormessages As List(Of String), lid As UInt64)

        Dim col As DataColumn
        Dim uncol As DataColumn

        'we shouldnt have to do this - add the missing column to the view/datatable and get it from there
        For Each fld In Me.EffectiveFieldsWithFilters
            Select Case fld.QuickFilterUItype
                Case Is = "TKEY", "BANDS", "NUMBANDS", "NUMS", "CHECK"
                    ' If Not Me.dicTrans.ContainsKey(fld) Then

                    If Not dt.Columns.Contains(fld.propertyName) Then
                        col = New DataColumn(fld.propertyName, GetType(Int64)) 'Single))
                        dt.Columns.Add(col)
                        uncol = New DataColumn(fld.propertyName & "UNIT", GetType(Int16))
                        dt.Columns.Add(uncol)
                        Me.fillCol(dt, fld, col, dic, buyeraccount, language, foci, errormessages, lid) 'also populates dicTrans,dicBands  and dicNums
                    End If
                    'End If
                Case Is = ""

                Case Else
                    errormessages.Add("unrecognised filter UI type :" & fld.QuickFilterUItype)
            End Select
        Next

        For Each fld In Me.Filters.Keys
            If Not dt.Columns.Contains(fld.propertyName) Then
                col = New DataColumn(fld.propertyName, GetType(Int64)) 'Single))

                '    If fld.propertyName.Contains("CP_SVC") Then Stop
                dt.Columns.Add(col)
                uncol = New DataColumn(fld.propertyName & "UNIT", GetType(Int16))
                dt.Columns.Add(uncol)
                Me.fillCol(dt, fld, col, dic, buyeraccount, language, foci, errormessages, lid)
            End If
        Next

        'do the same for sorts (add a column to the underlying datatable for everything we're sorting on)
        For Each so In sorts.Values
            If Not dt.Columns.Contains(so.column.propertyName) Then 'Dont add the same column twice (we may already have added it for filtering)
                col = New DataColumn(so.column.propertyName, GetType(Int64)) 'Single))

                dt.Columns.Add(col)
                uncol = New DataColumn(so.column.propertyName & "UNIT", GetType(Int16))
                dt.Columns.Add(uncol)
                Me.fillCol(dt, so.column, col, dic, buyeraccount, language, foci, errormessages, lid)
            End If
        Next

    End Sub

    Private Sub AddFieldToQFDic(f As clsField, ByRef errormessages As List(Of String))
        Select Case f.QuickFilterUItype
            Case Is = "TKEY"
                If Not dicTrans.ContainsKey(f) Then
                    dicTrans.Add(f, New Dictionary(Of clsTranslation, Integer))
                End If
            Case Is = "BANDS", "NUMBANDS"
                If Not Me.dicBands.ContainsKey(f) Then
                    dicBands.Add(f, New List(Of clsBand))
                End If
            Case Is = "NUMS", "CHECK"

                If Not dicNums.ContainsKey(f) Then
                    dicNums.Add(f, New Dictionary(Of Int64, Integer))  'counts of distinct VALUES (by field)
                End If
                ' Case Is = "CHECK"
            Case Is = ""

            Case Else
                errormessages.Add("unknown quickFilterUItype:" & f.QuickFilterUItype)
        End Select

    End Sub

    ''' <summary>
    ''' Fills the specified column on the datatable with numeric values retrieved for the field F
    ''' </summary>
    ''' <remarks>Also Populates dicNums,dicTrans and dicBands - used for the QuickFilters</remarks>
    Private Sub fillCol(dt As DataTable, f As clsField, col As DataColumn, dic As Dictionary(Of clsBranch, clsVisibility), buyeraccount As clsAccount, language As clsLanguage, foci As HashSet(Of String), ByRef errormessages As List(Of String), lid As UInt64)

        Dim translation As clsTranslation = Nothing

        AddFieldToQFDic(f, errormessages) 'The QFdics store the distinct values, (and their 'survivor' counts)

        Dim numericvalue As Int64
        Dim values As List(Of Int64) = New List(Of Int64)

        Dim branch As clsBranch = Nothing

        'index the Visbilities - by branch (for fast access to the  PATHs we'll need

        Dim vbb As New Dictionary(Of clsBranch, clsVisibility)
        For Each v In dic.Values
            If Not vbb.ContainsKey(v.branch) Then vbb.Add(v.branch, v)
        Next

        For Each row As DataRow In dt.Rows    'we're working with the entire unfiltered datatable here always) - no view involved

            branch = iq.Branches(CType(row("id"), Int32))

            'The cellvalue returns Numeric Value (if present), or translation.Sortvalue (which IS translation .order from non-zero orders, or an 'alphabetical' sort otherwise
            Dim unit As clsUnit = Nothing
            If vbb.ContainsKey(branch) Then  'this check shouldn't be required but JIT carepacks has some issue
                numericvalue = f.CellValue(branch, vbb(branch).path, buyeraccount, language, "", Nothing, False, translation, unit, foci, errormessages, lid)
            End If

            ' If numericvalue <> Int64.MinValue And numericvalue <> 0 Then Stop

            'This remembers the units for this column - which are needed for the labels in numeric quickFilters
            If unit IsNot Nothing Then
                row.Item(col.ColumnName + "UNIT") = unit.ID
                If dicUnits.ContainsKey(f) Then
                    If unit IsNot dicUnits(f) Then 'Ut oh, mismatched (or mixed units in a column)
                        errormessages.Add("Mismatched unit " & unit.Code)
                    End If
                Else
                    dicUnits(f) = unit

                End If
            End If

            If f.QuickFilterUItype = "TKEY" Then
                If translation IsNot Nothing Then
                    row.Item(col) = translation.SortValue(language)

                    If Not dicTrans(f).ContainsKey(translation) Then
                        dicTrans(f).Add(translation, 1)
                    Else
                        dicTrans(f)(translation) += 1
                    End If
                Else
                    row.Item(col) = numericvalue 'translation.SortValue(language)
                End If
            Else

                'any numeric value on 'check' type fields is treated as a 1
                If f.QuickFilterUItype = "CHECK" And numericvalue > Int64.MinValue Then numericvalue = 1

                row.Item(col) = numericvalue  '<<<THIS is 

                If f.QuickFilterUItype = "BANDS" Or f.QuickFilterUItype = "NUMBANDS" Then
                    If numericvalue > Int64.MinValue Then
                        values.Add(numericvalue)
                    End If

                ElseIf f.QuickFilterUItype = "NUMS" Or f.QuickFilterUItype = "CHECK" Then

                    If numericvalue > Int64.MinValue Then
                        'DONT change this - we WANT to add a 1 
                        If Not dicNums(f).ContainsKey(numericvalue) Then
                            dicNums(f).Add(numericvalue, 1)
                        Else
                            dicNums(f)(numericvalue) += 1
                        End If
                    End If
                End If

            End If
        Next

        'make a set of bands for the set of values we just retrieved - Each band will have (approximately the same NUMBER of values)
        If f.QuickFilterUItype = "BANDS" Or f.QuickFilterUItype = "NUMBANDS" Then
            If values.Count Then
                dicBands(f) = MakeBands(values)
            End If
        End If


    End Sub


    'Builds the bands such that each contains the same number of results - Eg, for 5 bands and 100 results, each band would have 20 results
    'NOTE: - This means that the min and max VALUES of the bands have not particular relationship to the range 
    'However - it's more useful to be able to fitler to the 'top 20% of laptops) rather than some arbitray value based bands

    Private Function MakeBands(values As List(Of Long)) As List(Of clsBand)

        'build the bands - such that each contains the same number of results 

        MakeBands = New List(Of clsBand)

        Dim numBands As Integer = 5
        Dim sortedValues = From j In values Order By j Distinct Select j
        Dim bottom As Int64 = Int64.MinValue
        Dim top As Int64 = 0
        Dim chunk As Integer = values.Count \ numBands
        Dim band As clsBand


        If sortedValues.Count < 5 Then

            band = New clsBand(sortedValues.First, sortedValues.Last, values.Count)
            MakeBands.Add(band)

        Else
            bottom = sortedValues.First
            For i = 1 To numBands - 1
                Dim skip As Integer = CType(i / numBands * sortedValues.Count, Integer)

                top = (From z In sortedValues.Skip(skip)).First
                band = New clsBand(bottom, top, chunk)
                MakeBands.Add(band)
                bottom = top
            Next

            'need to make the last band (i think)
            top = sortedValues.Last
            band = New clsBand(bottom, top, chunk)
            MakeBands.Add(band)

            'Round and overlap the bands
            For Each band In MakeBands
                'band.Stretch()
            Next
        End If


    End Function


    Public Function sortsString() As String

        'return the sorts in a format suitable for the sort propert of a dataview

        Dim s$ = ""
        For Each v In From j In Me.sorts.Values Order By j.Priority
            Dim sd As String = "DESC"
            If v.Direction <> "D" Then sd = ""
            s$ &= " [" & v.column.propertyName & "] " & sd & "," 'Note there are some pretty ciritcal spaces in here - mess with it at your peril
        Next

        If s.Length > 0 Then
            s = Left(s, Len(s) - 1)
        End If

        Return s$

    End Function

    Public Sub UpdateSorts(lid As UInt64, NewPriority As clsPriorityDirection, descendants As Dictionary(Of clsBranch, clsVisibility), buyeraccount As clsAccount, language As clsLanguage, foci As HashSet(Of String), ByRef errormessages As List(Of String))

        'gives us a Field, priority and direction to update

        Dim j As IEnumerable(Of clsPriorityDirection) = From v In Me.sorts.Values Where v.column Is NewPriority.column

        If j.Any Then 'we're changing an existing sort on this column (de-priortising it) - (by selecting it as a later sort than a pre-existing one)
            Me.sorts.Remove(j.First.Priority)
        End If

        If Not Me.sorts.ContainsKey(NewPriority.Priority) Then
            Me.sorts.Add(NewPriority.Priority, NewPriority)
        Else
            Me.sorts(NewPriority.Priority) = NewPriority
        End If

        Me.reNumberSorts()

        Me.addMissingColumns(descendants, Me.DT, buyeraccount, language, foci, errormessages, lid)


        Me.Vw.Sort = Me.sortsString

    End Sub


    Public Function key(buyeraccount As clsAccount) As String

        'the columns preset are key (we may need to add one if they've added a filter)
        key = Me.path & "-" & buyeraccount.SellerChannel.Code & "-" & buyeraccount.BuyerChannel.Code

    End Function

    Public Sub setColState(fld As clsField, state As enumColState)
        ColState(fld) = state 'expaned/collapsed (etc)
    End Sub

    ' ''' <summary>Used for debugging - displays a textual representatiom of the current filters applied by this matrixHeader</summary>
    'Public Function currentFilters() As String

    '    currentFilters = ""
    '    For Each fld In Filters.Keys
    '        currentFilters &= (fld.displayName(English) & ":")
    '        For Each fltVp In Filters(fld)
    '            currentFilters &= fltVp.Key.DisplayText.text(English) & "|" & fltVp.Value & " "
    '        Next
    '    Next
    'End Function


    Private Sub reNumberSorts()

        'pulling a sort out of the middle of the stack (e.g. delete sort '2 of 3' causes misnumbering 

        Dim j = From s In Me.sorts.Values.ToList Order By s.Priority

        Me.sorts.Clear()
        Dim p As Integer = 1
        For Each i In j
            i.Priority = p
            Me.sorts.Add(p, i)
            p += 1
        Next

    End Sub
    Public Sub RemoveSort(fldid As Integer)

        Me.sorts.Remove(fldid)
        Me.reNumberSorts()

        Me.Vw.Sort = Me.sortsString

    End Sub

    Public Sub removeFilters()

        Me.Filters.Clear()
        Me.Vw.RowFilter = Nothing

    End Sub
    Public Sub RemoveFilter(toRemove As String, ByRef errormessages As List(Of String))

        'To remove contains the fieldID|FilterCODE|Filtercode|filtercode

        Dim p() As String = Split(toRemove, "|")
        Dim fld As clsField = iq.Fields(CInt(p(0)))
        Dim filter As clsFilter

        If Me.Filters.ContainsKey(fld) Then
            For i = 1 To UBound(p)
                If iq.i_Filters_Code.ContainsKey(p(i)) Then
                    filter = iq.i_Filters_Code(p(i))
                    Me.Filters(fld).Remove(filter)  'each field can have more than one filter applied simultaeneously
                Else
                    errormessages.Add("Can't remove filter with the CODE " & p(1))
                End If
            Next i
        Else
            errormessages.Add("No filter present for field " & p(0))
        End If

        Me.Vw.RowFilter = ConstructFilter()

    End Sub

    Public Sub UpdateFilters(changefilter$)

        'Uses the changefilter parameter which
        'looks like 738|GE|1.02
        'Field ID, Filter Code (operator), New operand (value)

        'have extended to allow - ie, multiple Filter-value pairs on the same field
        'fldID|GE|2000|LE|4000

        Dim p() As String
        p = Split(changefilter, "|")

        Dim fld As clsField
        Dim flt As clsFilter
        fld = iq.Fields(CInt(p(0)))

        For i = 1 To UBound(p) - 1 Step 2
            flt = iq.i_Filters_Code(p(i))

            With Me.Filters
                If Not .ContainsKey(fld) Then .Add(fld, New Dictionary(Of clsFilter, List(Of Int64)))
                If Me.Filters(fld).ContainsKey(flt) Then
                    If Me.Filters(fld)(flt).Contains(CType(p(i + 1), Int64)) Then
                        If Me.Filters(fld)(flt).Count = 1 Then
                            Me.Filters(fld).Remove(flt)
                        Else
                            Me.Filters(fld)(flt).Remove(CType(p(i + 1), Int64))
                        End If
                    Else
                        Me.Filters(fld)(flt).Add(CType(p(i + 1), Int64))
                    End If
                Else
                    Me.Filters(fld).Add(flt, New List(Of Int64))
                    Me.Filters(fld)(flt).Add(CType(p(i + 1), Int64))
                End If
            End With
        Next i

        ' Me.addMissingColumns(descendants, Me.DT, buyeraccount, language, foci, errormessages, lid) ML TODO READD

        Me.Vw.RowFilter = Me.ConstructFilter

    End Sub


    Private Function ExpandCollapseColumnButtons(language As clsLanguage) As Panel

        Dim panel As Panel = New Panel
        panel.ID = "HeaderExpandRow"
        panel.CssClass = "oneRow"
        panel.Attributes("style") = "height:1em;"

        ' Dim cols As IEnumerable = (From f In Me.EffectiveFields Order By f.order)
        panel.Controls.Add(NewLit("<div class='LeftPad' style='width:4.25em;display:inline-block;'>&nbsp;</div>"))
        Dim x As Single = 0
        For Each f As clsField In Me.EffectiveFields

            'Dim d As Panel = f.emptyCell(f.isCollapsed(headerPath$, session))
            'img = New Image
            'img.Attributes("style") = "width:1.5em;height:1.5em;"
            'panel.Controls.Add(img)
            'panel.Controls.Add(d)
            ' d.Controls.Add(img)

            Dim p As Panel = New Panel()
            p.Style("display") = "inline-block"
            If Me.ColIsCollapsed(f) Then
                p.Width = New Unit(collapsedColumnWidth, UnitType.Em)
                p.Controls.Add(MakeRoundButton("expandColumn.png", "Show Column", "getBranches('path=" & Me.path & "&cmd=expandColumn&fieldid=" & Trim$(f.ID.ToString) & "');return false;", "", "position:relative;", language, f.ID))

                x = x + collapsedColumnWidth
            Else
                ' img.ImageUrl = "/images/navigation/collapseColumn.png"
                ' img.Attributes("onmousedown") = "getBranches('" & headerPath & "','collapseColumn=" & Trim$(f.ID) & "');return false;"
                ' img.ToolTip = "hide " & f.labelText & " column"

                p.Width = New Unit(FieldResultSet(f).GrownWidth, UnitType.Em)
                p.Controls.Add(MakeRoundButton("collapseColumn.png", "Hide Column", "getBranches('path=" & Me.path & "&cmd=collapseColumn&fieldid=" & Trim$(f.ID.ToString) & "');return false;", "", "position:relative;", language, f.ID))
                x = x + FieldResultSet(f).GrownWidth 'in ems
            End If
            panel.Controls.Add(p)
        Next

        '' this has been moved onto the containg div - matrixHeader.CssClass = "matrixHeader"
        Return panel

    End Function


    Public Function hasQuickFilters() As Boolean

        hasQuickFilters = False
        For Each fld In Me.EffectiveFields
            If fld.QuickFilterUItype <> "" Then
                hasQuickFilters = True
                Exit For
            End If
        Next

    End Function

    ''' <summary>
    ''' A 'quickfilter' is a set of checkboxes containing the distinct values in one field (column)
    ''' The act of rendering the quickfilters UI also (pre) scans the survivors (determined by the filter on the view) to enable/disable the correct options
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function FiltersUI(bi As clsBranchInfo, ByRef errorMessages As List(Of String)) As Panel


        FiltersUI = New Panel
        FiltersUI.Attributes("class") = "quickFilterGroupHolder"
        Dim ButtonPanel As Panel = New Panel
        ButtonPanel.CssClass = "FilterButtonContainer"

        If Me.QuickFiltersVisible Then
            Dim lit As Literal = New Literal
            Dim bid As String = "hmcb." & bi.path
            lit.Text = Replace("<div id=|" & bid & "| class=|QF hpBlueButton hideQF| onclick=|DisableElementsByClassName('FF');getBranches('cmd=hideQuickFilters&path=" & bi.path & "')|>Hide filters</div>", "|", Chr(34))
            ButtonPanel.Controls.Add(lit)
        End If
        'If bi.bs IsNot Nothing And Not bi.Branch.HasSKU AndAlso bi.PathLevel <> 1 Then
        If Not bi.branch.HasSKU AndAlso bi.PathLevel <> 1 Then
            'this is an 'open','category' branch (eligible for quick filters)

            'this is the 'show filters' button the hide filters is in the matrixheader itself
            If bi.MatrixHeader Is Nothing OrElse bi.MatrixHeader.QuickFiltersVisible = False AndAlso Me.matrix.Fields.ToList().Where(Function(f) f.Value.QuickFilterGroup IsNot Nothing).Count() > 0 Then

                If bi.MoreThanXskus(5) AndAlso bi.branch.Translation.Group <> "OL3" AndAlso Me.hasQuickFilters() Then 'need to include parent filters in this!
                    Dim bid As String = "hmcb." & bi.path  'just needs a unique DIV id (serves no other purpose)
                    Dim lit As New Literal
                    lit.Text = Replace("<div id=|" & bid & "| class=|hmc hpBlueButton ib showHMC| onclick=|getBranches('cmd=quickFilter&path=" & bi.path & "');return false|> " & If(Me.Filters.ToList().Where(Function(f) f.Value.Count() > 0).Count > 0, Xlt("Filtered", bi.buyerAccount.Language), Xlt("Filter", bi.buyerAccount.Language)) & "</div>", "|", Chr(34)) ' ML Removed <div class='clear'></div> as this was adding a huge space in scenaro, add quote click on earlier breadcrumb
                    ButtonPanel.Controls.Add(lit)
                End If
            End If
            Dim pth As String = ""
            If bi.PathLevel() < 6 Then
                Dim mh As Dictionary(Of String, clsMatrixHeader) = iq.sesh(bi.lid, "matrixHeaders")
                For Each seg In Split(bi.path, ".")
                    pth &= seg
                    If mh.ContainsKey(pth) AndAlso mh(pth).Filters IsNot Nothing Then
                        For Each f In mh(pth).Filters
                            Dim rf = String.Join("|", f.Value.Select(Function(fil) String.Join("|", fil.Value.Select(Function(fd) f.Key.ID.ToString() + "|" + fil.Key.Code + "|" + fd.ToString()))))
                            If f.Value.Count > 0 Then ButtonPanel.Controls.Add(NewLit("<div class=""FilterButton"" title=""Remove: " + String.Join(",", f.Value.Select(Function(d) d.Key.DisplayText.text(English) + String.Join(",", d.Value))) + """ onclick=""" + bi.branch.ButtonScript("cmd=removeFilter&filterParams=" + rf + "&path=" + bi.path & "&into=" + bi.path + "&filterPath=" + pth) + """><span>" + iq.Branches(Split(pth, ".").Last()).DisplayName(English) + " - </span><span>" + f.Key.labelText.text(bi.agentAccount.Language) + "</span></div>"))
                        Next 'd.Value.Select(Function(g) iq.Translations(CInt(g)).text(English))
                        ' If mh(pth).Vw IsNot Nothing Then
                        ' If mh(pth).Vw.RowFilter <> "" Then
                        ' FiltersUI.Controls.Add(NewLit("<span class=""FilterButton"" onclick=""" + bi.Branch.ButtonScript("cmd=removeFilters&path=" + bi.path & "&into=" + pth + "&filterPath=" & If(bi.RootPath Is Nothing, bi.path, bi.RootPath)) + """>Rack Mount</span>"))
                        ' End If
                    End If

                    pth &= "."
                Next
                'For Each f In Me.Filters
                ' FiltersUI.Controls.Add(NewLit("<span class=""FilterButton"" onclick=""" + bi.Branch.ButtonScript("cmd=removeFilters&path=" + bi.path & "&into=" + bi.path + "&filterPath=" & If(bi.RootPath Is Nothing, bi.path, bi.RootPath)) + """>Rack Mount</span>"))
                ' Next
            End If
        End If
        FiltersUI.Controls.Add(ButtonPanel)

        If Me.QuickFiltersVisible Then
            Me.EnableQuickFilters(bi, errorMessages)

            'Dim tKeyDic As Dictionary(Of clsField, List(Of clsTranslation)) = Me.quickfilterTextKeys  

            Dim EQfilter As clsFilter = iq.i_Filters_Code("EQ")

            Dim FilterControlArray As Dictionary(Of Panel, Integer) = New Dictionary(Of Panel, Integer)()
            Dim dicPanels As Dictionary(Of String, Panel) = New Dictionary(Of String, Panel)
            Dim pnl As Panel
            For Each fld In Me.FieldResultSet.Keys

                If fld.QuickFilterGroup IsNot Nothing Then

                    If Not dicPanels.ContainsKey(fld.QuickFilterGroup.text(English)) Then
                        pnl = New Panel
                        pnl.CssClass = "quickFilterGroupPanel"
                        dicPanels.Add(fld.QuickFilterGroup.text(English), pnl)
                        'Dim title As Label = New Label
                        'title.Text = fld.QuickFilterGroup.text(bi.AgentAccount.Language)
                        pnl.Controls.Add(NewLit("<span class='quickFilterGroupTitle'>" & fld.QuickFilterGroup.text(bi.agentAccount.Language) & "</span>"))
                        FilterControlArray.Add(pnl, fld.QuickFilterGroup.Order)
                    End If
                    pnl = dicPanels(fld.QuickFilterGroup.text(English))

                    Dim ip As Panel = New Panel 'inner panel (one for each UI element in the group - tend to be arranged in a column)
                    pnl.Controls.Add(ip)

                    Select Case fld.QuickFilterUItype
                        Case "CHECK"  'the field must evaluate to a productattribute - checking the box will fitler (by productattribute) to those with true (1) values

                            'check the boxes to match the current filters
                            Dim value As Boolean = False
                            If Filters IsNot Nothing Then
                                If Filters.ContainsKey(fld) Then
                                    Dim eq As clsFilter = iq.i_Filters_Code("EQ") 'EQ is the Equals filter 
                                    If Filters(fld).ContainsKey(eq) Then
                                        If Filters(fld)(eq).Contains(1) Then
                                            value = True
                                        End If
                                    End If
                                End If
                            End If

                            Dim chkBox As Literal = New Literal  'You *cant* use actual checkboxes - don't try - .NET has a nasty habbit of wrapping them in spans - and then attatching any script to the span (not the checkbox) - literals are the answer.
                            Dim occ$ 'EQ id the code for the 'Equals" filter (we look for Non-zero values)
                            occ$ = "filterfield=" & fld.ID & ";DisableElementsByClassName('FF');if(this.checked){getBranches('cmd=changeFilter&path=" & Me.path & "&filterParams=" & fld.ID & "|EQ|1')}else{getBranches('cmd=changeFilter&path=" & path & "&filterParams=" & fld.ID & "|EQ|1')};"

                            Dim count As Integer = 0
                            If dicNums.ContainsKey(fld) Then
                                If dicNums(fld).ContainsKey(1) Then
                                    count = dicNums(fld)(1) '1 is the 'true' value - and the key to the count
                                End If
                            End If

                            chkBox.Text = "<input class='FF' type='checkbox' " & CType(IIf(value, "checked", ""), String) & " " & CType(IIf(count = 0, "disabled='disabled'", ""), String) & "  onclick=" & Chr(34) & occ$ & Chr(34) & "/>"

                            'The quickfilterUI Interacts with the filters

                            ip.Controls.Add(chkBox)

                            Dim lbl As Label = New Label
                            lbl.Text = fld.labelText.text(bi.agentAccount.Language)
                            Dim dvs = dicNums(fld).Count
                            lbl.Text &= "(" & count & ")"  'add the count of True values (non true values are not present)

                            lbl.CssClass = "quickFilterLabel"
                            If count = 0 Then lbl.CssClass &= " disabled" : lbl.ToolTip = Xlt("This option isn't availble (in combination with your other selections)", bi.buyerAccount.Language)
                            ip.Controls.Add(lbl)

                        Case "BANDS", "NUMBANDS"
                            'break numerics/prices into bands

                            If dicBands.ContainsKey(fld) Then
                                For Each band In dicBands(fld)

                                    Dim chkBox As Literal = New Literal
                                    Dim occ$

                                    'NB this ChangeFilter chanegs/remove TWO filters at once
                                    occ$ = "filterfield=" & fld.ID & ";DisableElementsByClassName('FF');if(this.checked){getBranches('cmd=changeFilter&path=" & Me.path & "&filterParams=" & fld.ID.ToString & "|GE|" & band.min & "|LE|" & band.max & "')}else{getBranches('cmd=changeFilter&path=" & Me.path & "&filterParams=" & fld.ID & "|GE|" & band.min & "|LE|" & band.max & "')};"

                                    Dim selected As Boolean = band.isSelected(fld, Me.Filters) 'Compare to the currently selected GE/LE filters on this field
                                    chkBox.Text = "<input class='FF' type='checkbox' " & CType(IIf(selected, "checked='checked'", ""), String) & " " & CType(IIf(band.Survivors = 0, "disabled='disabled'", ""), String) & "  onclick=" & Chr(34) & occ$ & Chr(34) & ">"
                                    If fld.QuickFilterUItype = "NUMBANDS" Then
                                        'Numeric bands (no currency symbol) and NOT * 100
                                        chkBox.Text &= CType(band.min, Decimal).ToString
                                        chkBox.Text &= " - "
                                        chkBox.Text &= CType(band.max, Decimal).ToString
                                    Else
                                        'normal' PRICE bands
                                        chkBox.Text &= bi.buyerAccount.Currency.format(CType(band.min / 100, Decimal), bi.buyerAccount.BuyerChannel.Region.Culture, errorMessages, 0)
                                        chkBox.Text &= " - "
                                        chkBox.Text &= bi.buyerAccount.Currency.format(CType(band.max / 100, Decimal), bi.buyerAccount.BuyerChannel.Region.Culture, errorMessages, 0)
                                    End If

                                    chkBox.Text &= "&nbsp;(" & band.Survivors & ")"  'dicBands(fld)(band)

                                    chkBox.Text &= "</input><br/>"
                                    ip.Controls.Add(chkBox)

                                Next
                            End If


                            ' If Me.Filters.ContainsKey(fld) Then
                            'If Me.Filters(fld).Count > 0 Then
                            'ip.Controls.Add(fld.NoPreferenceRadioButton(path, Me.Filters(fld).Keys.ToList)) 'ML - no longer needed now we have check boxes rather than options
                            ' End If
                            'End If

                        Case "TKEY"
                            'this is a single field containing one of a set of transltions
                            'they currently present as radio buttons - but would be easy enough to switch to a set of checkboxes giving 'OR' functionality

                            Dim nm$ = "f_" & fld.ID & "." & bi.path
                            If dicTrans.ContainsKey(fld) Then 'it SHOULD always be there - but if you've put somehting stupid in the screens flds propertyname - it wont be
                                For Each t In dicTrans(fld).Keys
                                    If t IsNot Nothing Then
                                        Dim sv As Int64 = t.SortValue(bi.agentAccount.Language)
                                        Dim rb As Literal = New Literal
                                        rb.Text = "<input class=~FF~ type=~checkbox~ name=~" & nm$ & "~"
                                        rb.Text &= " onclick=~{DisableElementsByClassName('FF');getBranches('path=" & Me.path & "&cmd=changeFilter&filterParams=" & fld.ID & "|EQ|" & sv.ToString & "')}~"
                                        If Filters.ContainsKey(fld) Then
                                            For Each activefilter In Filters(fld)
                                                If activefilter.Key Is EQfilter Then  'The quickFilters do a strict equals
                                                    If activefilter.Value.Contains(t.SortValue(bi.agentAccount.Language)) Then
                                                        rb.Text &= " CHECKED=~CHECKED~"
                                                        Exit For
                                                    End If
                                                End If
                                            Next
                                        End If

                                        If dicTrans(fld)(t) = 0 Then  'There are no survivors
                                            rb.Text &= " disabled=~disabled~"
                                        End If

                                        rb.Text &= ">" & t.text(bi.agentAccount.Language) & " (" & dicTrans(fld)(t) & ")</input><br/>"
                                        rb.Text = rb.Text.Replace("~", Chr(34))

                                        ip.Controls.Add(rb)
                                    End If
                                Next
                            End If


                            If Me.Filters.ContainsKey(fld) Then
                                If Me.Filters(fld).Count > 0 Then
                                    'ip.Controls.Add(fld.NoPreferenceRadioButton(path, Me.Filters(fld).Keys.ToList)) ' ML - No longer needed now we have check boxes rather than options
                                End If
                            End If

                        Case "NUMS"

                            Dim nm$ = "f_" & fld.ID & "." & bi.path
                            If dicNums.Keys.Count = 0 Then
                                errorMessages.Add("DicNums was not populated")
                            Else
                                If Not dicUnits.ContainsKey(fld) Then
                                    errorMessages.Add("no units present for values in " & fld.propertyName)
                                Else
                                    'distinct Values of the number
                                    For Each v In (From j In dicNums(fld).Keys Order By j)
                                        Dim rb As Literal = New Literal
                                        rb.Text = "<input class=~FF~ type=~checkbox~ name=~" & nm$ & "~"
                                        rb.Text &= " onclick=~{DisableElementsByClassName('FF');getBranches('path=" & Me.path & "&cmd=changeFilter&filterParams=" & fld.ID & "|EQ|" & v.ToString & "')}~"
                                        If Filters.ContainsKey(fld) Then
                                            For Each activefilter In Filters(fld)
                                                If activefilter.Key Is EQfilter Then
                                                    If activefilter.Value.Contains(v) Then
                                                        rb.Text &= " CHECKED=~CHECKED~"
                                                        Exit For
                                                    End If
                                                End If
                                            Next
                                        End If

                                        If dicNums(fld)(v) = 0 Then  'There are no survivors
                                            rb.Text &= " disabled=~disabled~"
                                        End If

                                        rb.Text &= ">" & v.ToString & " " & dicUnits(fld).Symbol & " (" & dicNums(fld)(v) & ")</input><br/>"
                                        rb.Text = rb.Text.Replace("~", Chr(34))
                                        ip.Controls.Add(rb)
                                    Next

                                    If Me.Filters.ContainsKey(fld) Then
                                        If Me.Filters(fld).Keys.Count > 0 Then
                                            'no preference
                                            'ip.Controls.Add(fld.NoPreferenceRadioButton(path, Me.Filters(fld).Keys.ToList)) 'ML - No longer needed now we have check boxes instead of options 
                                        End If
                                    End If
                                End If
                            End If
                        Case Else
                            errorMessages.Add("Unrecognised quickFilterUIType:'" & fld.QuickFilterUItype & "'")

                    End Select

                    For Each p As Panel In FilterControlArray.OrderBy(Function(a) a.Value).Select(Function(a) a.Key)
                        FiltersUI.Controls.Add(p)
                    Next
                End If
            Next

            'Else
        End If
        'Quick filters


        '  End If

        'End If



    End Function

    ''' <summary>
    ''' Scans the 'surviving' (filtered) rows - to enable ony radiobuttons/checkboxes with suriving options
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub EnableQuickFilters(bi As clsBranchInfo, ByRef errorMessages As List(Of String)) ' As List(Of clsField)

        'NOTE uses ME.

        Dim descendants As Dictionary(Of clsBranch, clsVisibility) = New Dictionary(Of clsBranch, clsVisibility) 'Of String, clsBranch)
        Dim pruned As Integer = 0

        'If pbi.RenderChildrenAs = bt.matrixRows Then
        'Get the extended list - of ALL child products (recursing through the placeholder branches) - This isn't ALL descendants - becuase we only recurse down to the next product
        'The key to the dictionary is the (full) path .. the last segment thereof being the branch ID

        Dim branch As clsBranch = iq.Branches(CInt(Split(Me.path, ".").Last))


        Dim numericvalue As Int64 'Object
        Dim translation As clsTranslation = Nothing

        Dim holdIt As Dictionary(Of clsFilter, List(Of Int64)) = Nothing
        For Each fld In Me.EffectiveFields

            holdIt = Nothing
            If Me.Filters IsNot Nothing Then
                If Me.Filters.ContainsKey(fld) Then
                    'Change the filter on the view (for each quickfilter) to drop out this field - so that fields (for example price bands are not 'self-excluding')
                    holdIt = Me.Filters(fld)
                    Me.Filters.Remove(fld)
                End If
            End If

            Vw.RowFilter = Me.ConstructFilter '(Me.matrix, Me.Filters)

            'ZERO (but do not remove) the counts

            Select Case fld.QuickFilterUItype
                Case Is = "BANDS", "NUMBANDS"
                    If Not dicBands.ContainsKey(fld) Then
                        errorMessages.Add("Band was not intialised for " & fld.propertyName & " No values ?")
                    Else
                        For Each band In dicBands(fld)
                            band.Survivors = 0
                        Next
                    End If

                Case Is = "TKEY"
                    ' If dicTrans(fld).Keys.Count = 0 Then errorMessages.Add("TKEY dictionary was not initialised for " & fld.propertyName) - this will happen if there's no data in the column
                    If dicTrans.ContainsKey(fld) Then 'vegas - remove could mask problems
                        For Each k In dicTrans(fld).Keys.ToList  'This tolist IS VITAL - as it you iterate over the keys themselves (rather than a copy thereof) you get a 'collection cannot be modified' error
                            dicTrans(fld)(k) = 0
                        Next
                    End If

                Case Is = "NUMS", "CHECK"

                    'If dicNums(fld).Keys.Count = 0 Then errorMessages.Add("NUMS dictionary was not initialised for " & fld.propertyName)
                    If dicNums.ContainsKey(fld) Then  'vegas

                        'dicnums contains, an entry (a count) for every distinct value of a field - for example - for the 'Weight' field - i might contain 1KG (5), 2KG (10)
                        'the Distinct values are the KEYS in the outer dictionary, the VALUES in the dictionary are the COUNT of 'survivors' 'HAVING' that attribute.
                        'in the case of 'check' fields - there is ONLY ONE distinct value ('true')
                        For Each k In dicNums(fld).Keys.ToList
                            dicNums(fld)(k) = 0
                        Next
                    End If

            End Select

            If Not DT.Columns.Contains(fld.propertyName) Then
                ' errorMessages.Add("Invalid column specified:'" & fld.propertyName & "' (Check cAsE)")
            Else


                For i = 0 To Vw.Count - 1  'This view is a (filtered) subset of the datatable (which is a cache of all the nuericvalues of all the fields)

                    If fld.QuickFilterUItype <> "" Then

                        If TypeOf (Vw.Item(i)(fld.propertyName)) Is DBNull Then
                            numericvalue = Int64.MaxValue
                        Else
                            numericvalue = CType(Vw.Item(i)(fld.propertyName), Int64)  '.DataView.Item()
                        End If


                        ' If LCase(fld.propertyName) = "product.i_attributes_code(tch)(0)" Then Stop

                        Select Case fld.QuickFilterUItype

                            Case Is = "CHECK"

                                'any NON minvalue (ie, present) value qwill so and is stored under the value 1
                                If CType(numericvalue, Int64) <> Int64.MinValue Then   'int64.minvalue is the default numeric value and means its 'not there'

                                    If Not dicNums.ContainsKey(fld) Then dicNums.Add(fld, New Dictionary(Of Long, Integer))
                                    If Not dicNums(fld).ContainsKey(1) Then dicNums(fld).Add(1, 0)
                                    dicNums(fld)(1) += 1
                                    '            Exit For 'once we've found a single value (in the current filtered view)  - we know this is a valid option - so this gives a big speedup
                                End If


                            Case Is = "TKEY"

                                'this is not very elegant - although it should be fast enough as its a small list (distinct trasnlations within 1 field)
                                Dim found As Boolean = False
                                If dicTrans.ContainsKey(fld) Then 'vegas - remove - could mask problems
                                    For Each t In dicTrans(fld).Keys.ToList
                                        If t.SortValue(bi.agentAccount.Language) = numericvalue Then
                                            dicTrans(fld)(t) += 1
                                            found = True : Exit For
                                        End If
                                    Next
                                    If dicTrans(fld).Count > 0 Then
                                        ' If Not found Then errorMessages.Add("TKEY not found for " & fld.propertyName)
                                    End If
                                End If

                            Case Is = "BANDS", "NUMBANDS"

                                If Me.dicBands.ContainsKey(fld) Then
                                    For Each band In Me.dicBands(fld) 'this is a list of bands for this field
                                        If band.contains(numericvalue) Then band.Survivors += 1
                                    Next
                                End If


                            Case Is = "NUMS"
                                Dim flddic As Dictionary(Of Int64, Integer) = dicNums(fld)
                                If flddic.ContainsKey(numericvalue) Then 'UGLY - deals with missing values - eg a machine with no figure for installed mem
                                    flddic(numericvalue) += 1  'increment the count of occurances of this NumericValue
                                End If

                            Case Is = ""

                            Case Else
                                errorMessages.Add("EnableQuickFilters - Unknown quickFilterUI type" & fld.QuickFilterUItype)
                        End Select
                    End If
                Next

            End If

            If holdIt IsNot Nothing Then
                Me.Filters.Add(fld, holdIt)  'Put back the filter for this column
            End If

        Next

        bi.survivors = Vw.Count

    End Sub

    Public Function UI(bi As clsBranchInfo, ByRef errormessages As List(Of String), lid As UInt64) As PlaceHolder

        Me.path = bi.path
        Me.matrix = MatrixAbove(Me.path) 'NOTE: this is the ONLY place we find out what TYPE of matrixHeader we need to instance


        'Returns the user facing UI for filtering and sorting this matirx
        UI = New PlaceHolder
        If AccountHasRight(lid, "DIAGVIEW") Then UI.Controls.Add(NewLit(Me.matrix.ID))

        Dim b = Me.FieldResultSet.ToList().Where(Function(f) f.Key.QuickFilterGroup IsNot Nothing)
        If Me.FieldResultSet.ToList().Where(Function(f) f.Key.QuickFilterGroup IsNot Nothing).Count() > 0 Then  ' remove blank filters

            UI.Controls.Add(Me.FiltersUI(bi, errormessages))
        End If

        Dim mh As Panel = New Panel
        mh.CssClass = "matrixHeader"
        UI.Controls.Add(mh)  'MH is a div wich we put the diagonal lables (chart control) plus the filter/sort UI - so it all aligns



        ' mh.Controls.Add(pnlHeadSquares)

        OutputErrors(mh.Controls, errormessages, lid, True)

        If clsBranchState.getbranchstate(bi.lid, Me.path).rca = enumBt.gridrow Then
            Dim bAccount As clsAccount = iq.sesh(lid, "BuyerAccount")
            If AccountHasRight(lid, "GLOBALADM") Then mh.Controls.Add(NewLit(String.Format("<button class=""CustomizeColumns hmc hpBlueButton ib showHMC"" onclick=""$('#FieldFilter').draggable();$('#FieldFilter').show('slide right');$.ajax({{ url: '../Data/GetAvailableFields', data: JSON.stringify({{lid:'{0}', BranchPath:'{1}'}}),type: 'POST', contentType: 'application/JSON',success: GetAvailableFields_Success }});return false;"">" & Xlt("Customise Columns", bi.buyerAccount.Language) & "</button>", bi.lid, Me.path)))
            If bi.branch.Translation.Group = "OL3" Then 'simple screen for OL3, basically a list
                'mh.Controls.Add(Me.matrix.MatrixHeaders(Me))  'renders the header with diagonal lables
                mh.Controls.Add(Me.RemoveFiltersUI(bi.buyerAccount.Language)) '.MatrixPath, bi.lid, bi.AgentAccount.Language)) ' todo - reinstate
                Exit Function
            End If


            mh.Controls.Add(Me.SortDropDownsUI(bi.agentAccount.Language)) 'DropDowns
            mh.Controls.Add(Me.matrix.MatrixHeaders(Me, bi.agentAccount.Language))  'renders the header with diagonal lables
            Dim blit As Literal = New Literal


            mh.Controls.Add(Me.ExpandCollapseColumnButtons(bi.agentAccount.Language))
            mh.Controls.Add(Me.RemoveFiltersUI(bi.buyerAccount.Language)) '.MatrixPath, bi.lid, bi.AgentAccount.Language)) ' todo - reinstate

            mh.Controls.Add(Me.SortDirectionsUI(bi.buyerAccount.Language)) 'row of arrows

            blit.Text = "<div style='height:1px;clear:both;'>&nbsp;</div>"
            UI.Controls.Add(blit)

            If bi.survivors > 0 Then
                'Number matching (current filters)
                Dim lblmatches As Label
                lblmatches = New Label
                lblmatches.Text = bi.survivors.ToString
                lblmatches.Attributes("class") = "matchingLabel"

                UI.Controls.Add(lblmatches) ' a refrence to this is returned - so it can be populated later (becuase we render the headers early on - before we have fetched the data, so we don't have a count yet)

            End If
        End If



    End Function

    Private Function RemoveFiltersUI(language As clsLanguage) As Panel

        'returns a panel containing *customer facing*  UI for filtering (NOT for editor)

        Dim pnl As Panel = New Panel
        Dim occ$
        Dim col As Panel

        ' pnl.Controls.Add(ContrastSpacer) 'make room for the 'contrast' checkboxes

        pnl.CssClass = "oneRow"
        pnl.Attributes("style") = "display:inline-block;"

        pnl.Controls.Add(NewLit("<div class='LeftPad' style='width:4.25em;display:inline-block;'>&nbsp;</div>"))
        'for each column in the matrix we can have a value for each filter - so we can say >5 AND <10
        Dim AreSome As Boolean = False

        For Each fld In From v In Me.EffectiveFields  'iterate the fields in order of their Order property
            ' If fld.visibleList Then

            col = fld.emptyCell(Me.ColIsCollapsed(fld), False)
            '   col.CssClass = "removeFilter" 'get rid of the matrixCell class

            pnl.Controls.Add(col)

            '    Dim ib As ImageButton
            If Not Me.Filters Is Nothing Then
                If Me.Filters.ContainsKey(fld) Then
                    For Each flt In Me.Filters(fld).Keys
                        'ib = New ImageButton
                        'ib.ImageUrl = "/images/navigation/close.png"

                        'Special case for a greater than and less than
                        If flt.Code = "LE" AndAlso Me.Filters(fld).ToList().Where(Function(f) f.Key.Code = "GE").Count() > 0 Then Continue For

                        Dim rb As Panel = New Panel
                        rb.CssClass = "removeButton"
                        rb.ToolTip = Xlt("Remove the filter:- ", language) & fld.labelText.text(language) & " " & flt.DisplayText.text(language) & " " & String.Join(",", Me.Filters(fld)(flt)) 'ML - todo , add all values to this tip
                        occ$ = "getBranches('path=" & Me.path & "&cmd=removeFilter&filterPath=" & Me.path & "&filterParams=" & Trim$(fld.ID.ToString) & "|" & flt.Code & "');return false;"

                        AreSome = True
                        rb.Attributes("onclick") = occ$

                        col.Controls.Add(rb)
                        col.Width = New Unit(Me.FieldResultSet(fld).GrownWidth, UnitType.Em)
                    Next
                End If
            End If
            '  End If
        Next

        ' pnl.CssClass = "filtersUI"

        If AreSome Then pnl.Attributes("style") &= "height:" & collapsedColumnWidth & "em;"

        Return pnl

    End Function


    Public Function ColIsCollapsed(fld As clsField) As Boolean

        'If Me.ColState IsNot Nothing Then
        If Me.ColState.ContainsKey(fld) Then
            If Me.ColState(fld) = enumColState.SoftCollapsed Or Me.ColState(fld) = enumColState.HardCollapsed Then ColIsCollapsed = True
        End If
        'End If
    End Function


    Public Function SortDirectionsUI(language As clsLanguage) As Panel

        'formerly sortUI - now just provided the direction arrows

        SortDirectionsUI = New Panel
        SortDirectionsUI.ID = "sortDirections." & Me.path

        'Dim nl As Literal = New Literal
        'nl.Text = "<br/>"
        'SortsUI.Controls.Add(nl)
        SortDirectionsUI.Controls.Add(NewLit("<div class='LeftPad' style='width:4.25em;display:inline-block;'>&nbsp;</div>"))
        Dim col As Panel

        'SortsUI.Controls.Add(ContrastSpacer)

        SortDirectionsUI.CssClass = "sortsRow"


        Dim btnReSort As Literal = New Literal

        For Each f In From v In Me.EffectiveFields   'iterate the fields in order of their Order property

            '   If f.visibleList Then
            col = f.emptyCell(Me.ColIsCollapsed(f), Me.FieldResultSet(f).GrownWidth, False)
            col.ID = path$ & ".F" & f.ID

            Dim j As IEnumerable(Of clsPriorityDirection) = Nothing
            j = From v In Me.sorts.Values Where v.column Is f 'Pull out the sort info pertaining to this column
            If j.Any Then

                Dim pd As clsPriorityDirection = j.First

                'Dim currentValue As String = "-"
                'ptb = f.SortPriorityTextBox(Me.sorts$, currentValue)
                col.Controls.Add(pd.UI(path, language))

                'occ$ = "sortPath='" & path$ & "';sortFieldID=" & f.ID & ";showSortPriorityPicker('" & col.ID & "','" & currentValue & "');"

                ' ptb.Attributes("onfocus") = occ$
                ' ptb.ToolTip = "Set the priority and direction of this sorting for this column"
                ' col.Controls.Add(ptb)

                If LCase(f.propertyName) = "stock" Or LCase(f.propertyName) = "customerprice" Then

                    'todo - check if we're actually sorting by either of these !

                    'If InStr(Me.sorts$, f.propertyName) > 0 Then  ' a little crude - but good enough
                    btnReSort = New Literal
                    btnReSort.Text = "<div ID=|resort." & path$ & f.propertyName & "| class=|re-sort| style=|display:none| onmousedown=|getBranches('path=" & path & "&cmd=invalidate');return false;|" & "> &nbsp;</div>"
                    btnReSort.Text = Replace(btnReSort.Text, "|", Chr(34))

                    'btnReSort.ToolTip = "Click to re-sort based on updated stock/price)"
                    'btnReSort.CssClass = "re-sort"
                    'btnReSort.Attributes("style") = "display:none;"  'initially hidden - shown by js fillPrices()
                    'btnReSort.OnClientClick = occ$
                    'btnReSort.ID = "resort." & path$ & f.propertyName 'the need *an* ID (to be made visible - becuase the JS fethces them by class but show()s them by ID)
                    col.Controls.Add(btnReSort)
                    'End If
                End If
            End If

            SortDirectionsUI.Controls.Add(col)
            '    End If
        Next f
        ' SortsUI.Style("clear") = "both"

    End Function


    Public Function SortDropDownsUI(language As clsLanguage) As Panel

        'returns a set of dropdowns, one for each sort order in force - along with their remove buttons, and a single 'add sort' dropdown (cotaining other available columns to sort by)

        Dim ui As Panel = New Panel
        ui.ID = "sorts." & Me.path
        ui.CssClass = "sortsDropDowns"

        Dim sortOrders = From v In Me.sorts.Values Order By v.Priority

        For Each sortOrder In sortOrders
            'ui.Controls.Add(NewLit(sortOrder.Priority))
            ui.Controls.Add(Me.MakeDDL(sortOrder.column, sortOrder.Direction.First, sortOrder.Priority, language))
        Next

        If sortOrders.Count < 2 Then
            ui.Controls.Add(Me.MakeDDL(Nothing, "D", Me.sorts.Count + 1, language)) 'this 'special case' version contains a 'Add another' sort
        End If


        Return ui

    End Function

    ''' <summary>
    ''' Returns a panel containing the dropdown list and remove button for a single sort order (for this Grid)
    ''' </summary>
    Private Function MakeDDL(SelectedColumn As clsField, direction As String, priority As Integer, language As clsLanguage) As Panel 'sortorder As clsMatrixHeader.clsPriorityDirection) As Panel

        MakeDDL = New Panel

        Dim ddl As DropDownList = New DropDownList
        MakeDDL.Controls.Add(ddl)
        ddl.ID = "Sort_" & Me.path$ & priority 'the DDL needs a unique ID (note, many grides can be present in the made simultaeneously
        MakeDDL.CssClass = "aSortDropDown"

        If SelectedColumn Is Nothing Then
            Dim i As ListItem = New ListItem(Xlt("Add a sort", language), "add") 'You can't actually select this, so it's value is unimportant (The onchange will fire on this DDL, adding a new sort)
            ddl.Items.Add(i)
            ddl.SelectedValue = "add"
        End If

        For Each f In Me.EffectiveFields
            If f.visibleList Then
                Dim i As ListItem = New ListItem(f.labelText.text(language), f.ID.ToString & "," & priority & direction)
                ddl.Items.Add(i)
                If f Is SelectedColumn Then
                    ddl.SelectedValue = i.Value 'Select the right column in this dropdown (these is one dropdown for each active sort order)
                End If
            End If
            'UI.Attributes("onclick") = "getBranches('" & path & "', 'priority=" & Me.column.ID & "," & Me.Priority & "D');" ' + sortFieldID + ',' + v);"
        Next

        ddl.Attributes("onchange") = "DisableElementsByClassName('FF');var ddl=document.getElementById('" & ddl.ID & "');var spd=ddl.options[ddl.selectedIndex].value;getBranches('path=" & Me.path & "&cmd=sort&value='+spd);"

        If SelectedColumn IsNot Nothing Then
            Dim killButton As Panel = New Panel
            killButton.CssClass = "removeButton"
            Dim lit As Literal = New Literal
            lit.Text = "&nbsp;"
            killButton.Controls.Add(lit)
            killButton.Attributes("onclick") = "DisableElementsByClassName('FF');getBranches('cmd=removeSort&path=" & Me.path$ & "&priority=" & priority.ToString.Trim & "');"  'Removesort works by the priority
            MakeDDL.Controls.Add(killButton)
        End If

    End Function

    Public Sub CollapseColumns(emsAvailable As Single, ByRef errormessages As List(Of String))

        'Dynamically collapses the lowest priority columns in a matrix, based on the available width and which columns have been actively opened or closed

        'Dim ColState As Dictionary(Of clsField, enumColState) = Nothing
        'If iq.SeshContains(lid, "colstate." & path$) Then
        ' ColState = iq.sesh(lid, "colstate." & path$)
        ' End If

        're-expand all soft collapsed columns (we will re-collapse as many as necessary  below)
        For Each k In ColState.Keys.ToList
            If ColState(k) = enumColState.SoftCollapsed Then
                ColState(k) = enumColState.SoftExpanded
            End If
        Next

        Dim w As Single = 0
        w = Me.currentWidth(errormessages) ' get the current width (all columns minus the hard collapsed ones)

        'Now collapse columns in descending order of priority until the total width is less than the space available
        Dim pass As Integer
        Dim done As Boolean = False
        For pass = 1 To 2  'on the first pass we collapse the 'soft' columns - on the second.. second hard
            For Each fld In From v In Me.EffectiveFields Order By v.priority Descending 'iterate the fields in descending order of their priority - ie. knock out the highest numbers (least important columns) first - Priority '1' is the most important column
                'If fld.visibleList Then
                If w < emsAvailable Then done = True
                If done Then Exit For
                If (ColState(fld) = enumColState.SoftExpanded And pass = 1) Or (ColState(fld) = enumColState.HardExpanded And pass = 2) Then
                    If fld.priority <> 1 Then
                        w = w - (FieldResultSet(fld).Width - collapsedColumnWidth) 'this is what we 'gain' by collpasing this column
                        Me.ColState(fld) = enumColState.SoftCollapsed
                    End If
                End If
                ' End If
            Next fld
            If done Then Exit For
        Next pass


        'Add some small ones back  (most important first)
        For Each fld In From v In Me.EffectiveFields Order By v.priority
            ' If fld.visibleList Then
            If (ColState(fld) = enumColState.SoftCollapsed) Then
                If w + FieldResultSet(fld).Width < emsAvailable Then
                    Me.ColState(fld) = enumColState.SoftExpanded
                    w += FieldResultSet(fld).Width
                End If
            End If
            ' End If
        Next

        'Mop up and grow
        Dim growField = Me.EffectiveFields.Where(Function(ef) ef.Grow).FirstOrDefault()
        If growField IsNot Nothing AndAlso emsAvailable > w AndAlso Me.ColState(growField) <> enumColState.SoftCollapsed AndAlso (emsAvailable - w) > Me.FieldResultSet(growField).Width Then
            'growField.width = emsAvailable - w
            Me.FieldResultSet(growField).GrownWidth = emsAvailable - w
            w = emsAvailable
        Else
            If growField IsNot Nothing Then Me.FieldResultSet(growField).GrownWidth = Nothing
        End If

    End Sub

    Private Function currentWidth(ByRef errormessages As List(Of String)) As Single

        'this is the total width of all the visible columns (taking in to account wether they're collapsed or not) 

        Dim w As Single
        w = 0


        For Each fld In Me.EffectiveFields
            ' If fld.visibleList Then
            If Me.ColState.ContainsKey(fld) Then
                Select Case Me.ColState(fld)
                    Case enumColState.SoftCollapsed
                        Me.ColState(fld) = enumColState.SoftExpanded  'we re-expand any soft collapsed column
                        w = w + FieldResultSet(fld).Width
                    Case enumColState.HardCollapsed
                        w = w + collapsedColumnWidth  'this is a hard collapse (the user explicily collapsed the column)
                    Case enumColState.SoftExpanded, enumColState.HardExpanded
                        w = w + FieldResultSet(fld).Width
                    Case Else
                        errormessages.Add("Unexpected column state")
                End Select
            Else
                ColState.Add(fld, enumColState.SoftExpanded) 'first pass - set all colups to soft expanded
                w = w + FieldResultSet(fld).Width

            End If
            ' End If
        Next

        Return w


    End Function

    Public Function Clone() As clsMatrixHeader
        Return DirectCast(Me.MemberwiseClone(), clsMatrixHeader)
    End Function

    Public Sub InvalidateFields()
        _FieldResultSet = Nothing
    End Sub

    Public Sub SetDefaultFilterOn()
        'Clear current filters
        Filters.Clear()

        'Fetch defaults
        For Each f In EffectiveFields
            If f.DefaultFilterValues IsNot Nothing Then
                UpdateFilters(String.Format("{0}|{1}", f.ID, f.DefaultFilterValues))
            End If
        Next
    End Sub


End Class

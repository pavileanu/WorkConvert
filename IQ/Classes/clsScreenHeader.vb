Imports System.Globalization
Imports IQ.clsScreenHeader
Imports System.IO
Imports System.Threading


Public Class clsScreenHeader
    Public Property screen As clsScreen

    Public Property United As Boolean
    Public Filters As Dictionary(Of clsField, Dictionary(Of clsFilter, List(Of Int64))) = Nothing
    Public sorts As Dictionary(Of Integer, clsPriorityDirection) 'The priority is the key (makes sense, honest)
    Public ColState As Dictionary(Of clsField, enumColState)
    Public Vw As System.Data.DataView
    Public QuickFiltersVisible As Boolean
    Private lid As UInt64
    Public Path As String

    Private dicTrans As Dictionary(Of clsField, Dictionary(Of clsTranslation, Integer)) 'Holds the surviving count (distinct) translations for this Quick filter - populated my addmissingcolumns
    Private dicBands As Dictionary(Of clsField, List(Of clsBand)) 'QuickFilter fields of type BANDS a
    Private dicNums As Dictionary(Of clsField, Dictionary(Of Int64, Integer)) 'All the DISTINCT numeric values (and the survivor counts thereof)
    Private dicUnits As Dictionary(Of clsField, clsUnit) 'for each (numeric) field detect and validate the UNITS

    Private descendants As Dictionary(Of clsBranch, clsVisibility)

    Private _FieldResultSet As Dictionary(Of clsField, clsAccountScreenField)
    Public ReadOnly Property FieldResultSet As Dictionary(Of clsField, clsAccountScreenField)
        Get
            If _FieldResultSet Is Nothing Then
                Dim pml As clsPromo = Nothing
                Dim otherPromo As List(Of String) = New List(Of String)
                If iq.seshDic(lid).ContainsKey("promoinforce") AndAlso Not {"K", "I"}.Contains(iq.Branches(Split(Me.Path, ".").Last).rca) Then
                    pml = iq.Promos(iq.sesh(lid, "promoinforce"))
                End If
                Dim l As clsAccount = iq.sesh(lid, "BuyerAccount")

                If iq.i_PromoRegions.ContainsKey(l.BuyerChannel.Region) Then
                    Dim t As String = ""
                    For Each promo In iq.i_PromoRegions(l.BuyerChannel.Region)
                        otherPromo.Add(promo.FieldProperty_Filter)
                    Next
                    

                End If

                If pml IsNot Nothing Then otherPromo.Remove(pml.FieldProperty_Filter)


                Dim errormessages As List(Of String) = New List(Of String)
                'TODO add default display unit here
                Dim asa As IEnumerable(Of clsScreenOverride) = iq.ScreenOverrides.Where(Function(so) so.AccountID = l.ID And so.ScreenID = Me.screen.ID And so.Path = Me.Path).Select(Function(dd) dd)
                Dim screenOverrideObjects =
                    From s In iq.Screens(Me.screen.ID).Fields.Values.Where(Function(fld) (pml IsNot Nothing AndAlso fld.propertyName = pml.FieldProperty_Filter) OrElse (otherPromo IsNot Nothing AndAlso otherPromo.Contains(fld.propertyName)) OrElse fld.ValidRegions.Count = 0 OrElse fld.ValidRegions.Where(Function(vr) vr.Value.Encompasses(l.BuyerChannel.Region)).Count > 0)
                        Group Join a In asa On s.ID Equals a.FieldId Into lr = Group From m In lr.DefaultIfEmpty()
                                Select New With {
                                    .a = New clsAccountScreenField With {.AccountID = l.ID, .ScreenID = Me.screen.ID, .Path = Path, .FieldId = s.ID, .Visibility = If((pml IsNot Nothing AndAlso s.propertyName = pml.FieldProperty_Filter) OrElse (otherPromo IsNot Nothing AndAlso otherPromo.Contains(s.propertyName)), True, If(m Is Nothing OrElse m.ForceVisibilityTo Is Nothing, s.visibleList, m.ForceVisibilityTo)), .Order = If(m Is Nothing OrElse m.ForceOrderTo Is Nothing, s.order, m.ForceOrderTo), .Width = If(m Is Nothing OrElse m.ForceWidthTo Is Nothing, If(s.width = 0, Nothing, s.width), m.ForceWidthTo), .Description = s.labelText, .DisplayUnit = If(m Is Nothing OrElse m.DisplayUnit Is Nothing, Nothing, m.DisplayUnit), .PromoColumn = If(pml IsNot Nothing AndAlso s.propertyName = pml.FieldProperty_Filter, True, False)},
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

    Public Sub New(bi As clsBranchInfo, descendants As Dictionary(Of clsBranch, clsVisibility), quickFiltersVisible As Boolean)
        Me.QuickFiltersVisible = quickFiltersVisible
        Me.descendants = descendants
        Dim screenHeaders As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(bi.lid, "screenHeaders"), Dictionary(Of String, clsScreenHeader))
        If bi.branch.ID = 118499 Then
            Dim a = 9
        End If
        'Remove the previous header if there was one here, do we want to do this?
        If screenHeaders.ContainsKey(bi.path) Then
            screenHeaders.Remove(bi.path)
        End If
        screenHeaders.Add(bi.path, Me)

        Me.Path = bi.path
        Me.lid = bi.lid
        Me.screen = MatrixAbove(lid, Me.Path) 'NOTE: this is the ONLY place we find out what TYPE of screen definition we need to instance

        If Me.screen Is Nothing Then Exit Sub

        Me.Filters = New Dictionary(Of clsField, Dictionary(Of clsFilter, List(Of Int64)))  'TODO  default filters
        Me.sorts = Me.screen.DefaultSorts  'each field has a defaultsort priority and directon - eg 1A 3D
        Me.ColState = New Dictionary(Of clsField, enumColState)

        Me.dicTrans = New Dictionary(Of clsField, Dictionary(Of clsTranslation, Integer))
        Me.dicBands = New Dictionary(Of clsField, List(Of clsBand))
        Me.dicNums = New Dictionary(Of clsField, Dictionary(Of Int64, Integer))
        Me.dicUnits = New Dictionary(Of clsField, clsUnit)

        'Get or create the lid's root data table

        'Dim seshDT As DataTable = bi.agentAccount.SellerChannel.AttributeDataTable(bi.buyerAccount.BuyerChannel)
        Dim seshDT As DataTable = iq.sesh(lid, "dataTable")
        If seshDT Is Nothing Then
            seshDT = New DataTable() With {.Locale = New CultureInfo(If(bi.buyerAccount.Culture IsNot Nothing, bi.buyerAccount.Culture.Code, "en-us"))}
            iq.seshDic(lid).Add("dataTable", seshDT)
            Dim col As DataColumn
            col = New DataColumn("ID", GetType(Int32))
            seshDT.Columns.Add(col)

        End If

        Me.Vw = New DataView(seshDT)

        'Populate it with all id's in descendants
        Dim c(0) As Object 'ID column in the data table
        Dim dv = seshDT.AsDataView()
        dv.Sort = "[ID]"
        Dim ids = descendants.Values.Where(Function(dic) (dic.hideReasonList.Count = 0 Or bi.showAll) AndAlso dic.branch.HasSKU).Select(Function(desc) desc.branch.ID).ToList().Except(seshDT.AsEnumerable().Select(Function(tab) CInt(tab("Id"))).ToList()).ToList()

        For Each vs In ids
            seshDT.Rows.Add(vs)
        Next


        'For Each vs In descendants.Values
        '    If dv.Find(vs.branch.ID) = -1 AndAlso (vs.hideReasonList.Count = 0 Or bi.showAll = True) AndAlso vs.branch.HasSKU Then
        '        c(0) = vs.branch.ID
        '        seshDT.Rows.Add(c)
        '    End If
        'Next


        If Not iq.seshDic(lid).ContainsKey("pathDataLoaded") Then iq.seshDic(lid).Add("pathDataLoaded", New List(Of String)) 'visiblechildren is heavy, dont keep using it, this is NOT perfect and needs more work once this way is proven to be ok... ML
        Dim pdl As List(Of String) = iq.sesh(lid, "pathDataLoaded")

        If Not pdl.Contains(Me.Path) Then
            populateData(descendants, seshDT, bi, lid)
            iq.sesh(lid, "pathDataLoaded").Add(Me.Path)
        End If
        Vw.Sort = sortsString()
        'If Not bi.agentAccount.SellerChannel.DataPathLoaded(bi.buyerAccount.BuyerChannel, Me.Path) Then
        '    populateData(descendants, seshDT, bi, lid)
        'End If

        Dim aa = 0
        Me.Vw.RowFilter = Me.ConstructFilter(True)
        Me.QuickFiltersVisible = quickFiltersVisible

    End Sub

    Public Sub populateData(dic As Dictionary(Of clsBranch, clsVisibility), ByRef dt As DataTable, bi As clsBranchInfo, lid As UInt64)

        'ML Note, not looked at scenario when a column is added and rows already exist without this data?
        Dim col As DataColumn
        Dim uncol As DataColumn
        'we shouldnt have to do this - add the missing column to the view/datatable and get it from there


        For Each fld In Me.EffectiveFieldsWithFilters
            AddFieldToQFDic(fld) 'The QFdics store the distinct values, (and their 'survivor' counts)

            '   Select Case fld.QuickFilterUItype
            '  Case Is = "TKEY", "BANDS", "NUMBANDS", "NUMS", "CHECK"
            If Not dt.Columns.Contains(fld.propertyName) Then
                If fld.InputType.code.ToUpper = "STRING" And fld.QuickFilterUItype = "BANDS" Then

                    col = New DataColumn(fld.propertyName, GetType(String)) 'Single))
                    dt.Columns.Add(col)
                    uncol = New DataColumn(fld.propertyName & "UNIT", GetType(Int16))
                    dt.Columns.Add(uncol)

                Else
                    If fld.propertyName.Contains("DMR_ISS") Then
                        Dim a = 1
                    End If
                    col = New DataColumn(fld.propertyName, GetType(Int64)) 'Single))
                    dt.Columns.Add(col)
                    uncol = New DataColumn(fld.propertyName & "UNIT", GetType(Int16))
                    dt.Columns.Add(uncol)
                End If
                Me.fillDTCol(dt, fld, col, dic, bi.buyerAccount, bi.agentAccount.Language, bi.foci, lid) 'also populates dicTrans,dicBands  and dicNums
            Else
                'Do we have any more nodes to add to the table?
                col = dt.Columns(fld.propertyName)
                Dim newdic = New Dictionary(Of clsBranch, clsVisibility)

                Dim dv = dt.AsDataView()
                dv.Sort = "[ID]"
                For Each c In dic
                    Dim d = dv.Find(c.Key.ID)
                    If d >= 0 AndAlso IsDBNull(dv(d)(fld.propertyName)) Then newdic.Add(c.Key, c.Value)
                Next
                Me.fillDTCol(dt, fld, col, newdic, bi.buyerAccount, bi.agentAccount.Language, bi.foci, lid) 'also populates dicTrans,dicBands  and dicNums

            End If
            '       Case Is = ""
            'No need to populate
            '         Case Else
            '      AuditLog.Instance.Add(AuditType.Error, "unrecognised filter UI type :" & fld.QuickFilterUItype, "clsScreenHeader", lid)
            '      End Select
        Next

        For Each fld In Me.Filters.Keys
            AddFieldToQFDic(fld) 'The QFdics store the distinct values, (and their 'survivor' counts)

            If Not dt.Columns.Contains(fld.propertyName) Then
                col = New DataColumn(fld.propertyName, GetType(Int64)) 'Single))

                '    If fld.propertyName.Contains("CP_SVC") Then Stop
                dt.Columns.Add(col)
                uncol = New DataColumn(fld.propertyName & "UNIT", GetType(Int16))
                dt.Columns.Add(uncol)
                Me.fillDTCol(dt, fld, col, dic, bi.buyerAccount, bi.agentAccount.Language, bi.foci, lid)
            Else
                col = dt.Columns(fld.propertyName)
                Dim newdic = New Dictionary(Of clsBranch, clsVisibility)
                Dim dv = dt.AsDataView()
                dv.Sort = "[ID]"
                For Each c In dic
                    Dim d = dv.Find(c.Key.ID)
                    If d >= 0 AndAlso IsDBNull(dv(dv.Find(c.Key.ID))(fld.propertyName)) Then newdic.Add(c.Key, c.Value)
                Next
                Me.fillDTCol(dt, fld, col, newdic, bi.buyerAccount, bi.agentAccount.Language, bi.foci, lid) 'also populates dicTrans,dicBands  and dicNums
            End If
        Next

        'do the same for sorts (add a column to the underlying datatable for everything we're sorting on)
        For Each so In sorts.Values
            AddFieldToQFDic(so.column) 'The QFdics store the distinct values, (and their 'survivor' counts)

            If Not dt.Columns.Contains(so.column.propertyName) Then 'Dont add the same column twice (we may already have added it for filtering)
                col = New DataColumn(so.column.propertyName, GetType(Int64)) 'Single))

                dt.Columns.Add(col)
                uncol = New DataColumn(so.column.propertyName & "UNIT", GetType(Int16))
                dt.Columns.Add(uncol)
                Me.fillDTCol(dt, so.column, col, dic, bi.buyerAccount, bi.agentAccount.Language, bi.foci, lid)
            Else
                col = dt.Columns(so.column.propertyName)
                Dim newdic = New Dictionary(Of clsBranch, clsVisibility)
                Dim dv = dt.AsDataView()
                dv.Sort = "[ID]"
                For Each c In dic
                    Dim d = dv.Find(c.Key.ID)
                    If d >= 0 AndAlso IsDBNull(dv(dv.Find(c.Key.ID))(col.ColumnName)) Then newdic.Add(c.Key, c.Value)
                Next
                Me.fillDTCol(dt, so.column, col, newdic, bi.buyerAccount, bi.agentAccount.Language, bi.foci, lid) 'also populates dicTrans,dicBands  and dicNums
            End If
        Next
        '  indexData()

    End Sub

    Sub RefreshViewFilter()
        Vw.RowFilter = Me.ConstructFilter(True)
    End Sub
    Sub indexData()
        Vw.RowFilter = Me.ConstructFilter(True)
        dicTrans.Clear()
        dicNums.Clear()
        dicBands.Clear()

        For Each fld In Me.EffectiveFieldsWithFilters
            '   If fld.propertyName <> "Product.SKU" Then
            Me.indexData(fld)
            'End If
        Next
    End Sub

    Sub indexData(f As clsField)


        'AddFieldToQFDic(f)
     
        Dim values As List(Of Int64) = New List(Of Int64)
        Dim branch As clsBranch = Nothing

        Dim so = Vw.Sort
        Dim origFilter = Me.ConstructFilter(False)
        Vw.RowFilter = origFilter

        Vw.Sort = "[ID]"

        'ML Testing here, remove if broken

        'Dim bandValues As Dictionary(Of clsField, Dictionary(Of Int64, Int32)) = New Dictionary(Of clsField, Dictionary(Of Long, Integer))()


        Dim did = descendants.Select(Function(d) d.Key.ID)
        'If f.InputType.code.ToUpper = "STRING" And f.QuickFilterUItype = "BANDS" Then
        '    rows = Vw.Table.AsEnumerable().Where(Function(r) did.Contains(r("id")) AndAlso Not IsDBNull(r(f.propertyName)))
        '    headers = rows.GroupBy(f.propertyName)

        'Else
        Dim rows As System.Data.EnumerableRowCollection(Of System.Data.DataRow)
        If f.propertyName = "Product.SKU" Then
            rows = Vw.Table.AsEnumerable().Where(Function(r) did.Contains(r("id")) AndAlso Not IsDBNull(r(f.propertyName)))
            '  Dim headers = rows.GroupBy(Function(dh) Int64.Parse(dh("id")))

        Else
            rows = Vw.Table.AsEnumerable().Where(Function(r) did.Contains(r("id")) AndAlso Not IsDBNull(r(f.propertyName)) AndAlso r(f.propertyName) > Int64.MinValue)

        End If

        Dim headers = rows.GroupBy(Function(dh) Int64.Parse(dh(f.propertyName)))




        If rows.Count > 0 AndAlso Not IsDBNull(rows(0)(f.propertyName & "UNIT")) AndAlso iq.Units.ContainsKey(CInt(rows(0)(f.propertyName & "UNIT"))) Then dicUnits(f) = iq.Units(CInt(rows(0)(f.propertyName & "UNIT"))) ' What if multiple units?  need to think about this one?

        'Dim tbl As DataTable = rows(0).Table
        'For Each r In rows
        '    tbl.Rows.Add(r)
        'Next


        Dim CombFilters As Dictionary(Of clsField, Dictionary(Of clsFilter, System.Collections.Generic.KeyValuePair(Of List(Of Long), List(Of Long)))) = New Dictionary(Of clsField, Dictionary(Of clsFilter, Generic.KeyValuePair(Of List(Of Long), List(Of Long))))()
        Me.CombinedFilters(True, CombFilters)

        Dim dfdf = Vw.ToTable.AsEnumerable().Where(Function(r) did.Contains(r("id")) AndAlso Not IsDBNull(r(f.propertyName)) AndAlso r(f.propertyName) > Int64.MinValue)
        For Each filt In CombFilters
            dfdf = dfdf.Where(Function(dd)
                                  If filt.Key Is f Then
                                      Select Case f.QuickFilterUItype
                                          Case "TKEY", "NUMS", "CHECK", "BOOL"
                                              filt.Value(iq.i_Filters_Code("EQ")).Key.Add(dd(filt.Key.propertyName))
                                          Case "BANDS", "NUMBANDS"
                                              filt.Value(iq.i_Filters_Code("LEGE")).Key.Add(dd(filt.Key.propertyName))
                                              filt.Value(iq.i_Filters_Code("LEGE")).Value.Add(dd(filt.Key.propertyName))
                                      End Select
                                  End If

                                  Return filt.Value.Where(Function(fd) fd.Key.compare(dd(filt.Key.propertyName), fd.Value.Key, fd.Value.Value)).Count > 0
                              End Function)

        Next
        Dim ds As Dictionary(Of Object, Integer)
        If f.propertyName <> "Product.SKU" Then
            ds = dfdf.GroupBy(Function(rw) rw(f.propertyName)).ToDictionary(Function(fa) fa.Key, Function(fa) fa.Count)
        Else
            ' Dim a As DataTable = dfdf.CopyToDataTable
            ' Dim a = dfdf.GroupBy(Function(rw) rw(f.propertyName))
            ' Dim b = a.ToList

        End If
        Dim tInt As Int32
        Select Case f.QuickFilterUItype.ToUpper
            Case "TKEY"
                Dim headerDic As Dictionary(Of clsTranslation, Integer) = New Dictionary(Of clsTranslation, Integer)

                dicTrans.Add(f, headers.ToDictionary(Function(td) If(Int32.TryParse(td.Key, tInt) AndAlso iq.Translations.ContainsKey(td.Key), iq.Translations(td.Key), iq.AddTranslation("Other", English, "", 0, Nothing, 0, True)), Function(ddh) If(ds.ContainsKey(ddh.Key), ds(ddh.Key), 0)))
            Case "NUMS", "CHECK", "BOOL"
                dicNums.Add(f, headers.ToDictionary(Function(td) If(f.QuickFilterUItype = "CHECK", If(td.Key > Int64.MinValue, 1, 0), If(f.QuickFilterUItype = "BOOL", If(td.Key = 1, 1, 0), Int64.Parse(td.Key))), Function(ddh) If(ds.ContainsKey(ddh.Key), ds(ddh.Key), 0)))
            Case "BANDS", "NUMBANDS"
                If f.propertyName <> "Product.SKU" Then
                    values = dfdf.Select(Function(ff) Int64.Parse(ff(f.propertyName))).ToList
                Else

                End If
        End Select

        If f.QuickFilterUItype = "BANDS" Or f.QuickFilterUItype = "NUMBANDS" Then
            If f.propertyName <> "Product.SKU" AndAlso headers.Count Then
                dicBands(f) = MakeBands(headers.SelectMany(Function(fff) Enumerable.Repeat(fff.Key, fff.Count)).ToList, values, f.QuickFilterUItype)
            End If
        End If


        'ML End testing

        'For Each row As DataRow In Vw.Table.Rows    'we're working with the entire unfiltered datatable here always) - no view involved
        '    branch = iq.Branches(CType(row("id"), Int32))

        '    If Not descendants.ContainsKey(branch) Then Continue For
        '    If dicUnits.ContainsKey(f) Then 'ML toDO need to replace this?
        '        'If UnitId IsNot dicUnits(f) Then 'Ut oh, mismatched (or mixed units in a column)
        '        ' AuditLog.Instance.Add(AuditType.Error, "Mismatched unit " & Unit.Code, "clsScreenHeader", lid)
        '        'End If
        '    Else
        '        If row(f.propertyName & "UNIT") IsNot DBNull.Value Then
        '            Dim UnitID = CInt(row(f.propertyName & "UNIT"))
        '            If iq.Units.ContainsKey(UnitID) Then
        '                dicUnits(f) = iq.Units(UnitID)
        '            End If
        '        End If
        '    End If
        '    If Not IsDBNull(row(f.propertyName)) Then
        '        Dim numericvalue = CDbl(row(f.propertyName))


        '        If f.QuickFilterUItype = "CHECK" And numericvalue > Int64.MinValue Then numericvalue = 1
        '        Select Case f.QuickFilterUItype
        '            Case "TKEY"
        '                Dim tID = CType(row(f.propertyName), Int64)
        '                'Dim tInt As Int32

        '                If Int32.TryParse(tID, tInt) Then
        '                    If iq.Translations.ContainsKey(tID) Then
        '                        Dim r As Regex = New Regex("(\[" & f.propertyName.Replace("(", "\(").Replace(")", "\)").Replace(".", "\.") & "\]=[0-9]+)")
        '                        Dim match = r.Match(origFilter)
        '                        If match.Groups.Count > 1 Then
        '                            Dim fi = origFilter.Replace(match.Groups(1).Value, "").Replace("()", "").Replace("()", "").Replace("( OR ", "(").Replace("( AND ", "(").Trim(" AND ".ToArray).Trim(" OR ".ToArray)
        '                            Vw.RowFilter = fi & If(String.IsNullOrEmpty(fi) AndAlso Not fi.EndsWith("AND "), "", " AND ") & "[" & f.propertyName & "]=" & tID
        '                        End If
        '                        If Not dicTrans(f).ContainsKey(iq.Translations(tID)) Then
        '                            dicTrans(f).Add(iq.Translations(tID), 0)
        '                        End If
        '                        If Vw.Find(row("id")) > -1 Then dicTrans(f)(iq.Translations(tID)) += 1
        '                    Else
        '                        'Add error here...
        '                    End If
        '                End If
        '            Case "Check", "NUMS"
        '                If numericvalue > Int64.MinValue Then
        '                    Dim r As Regex = New Regex("(\[" & f.propertyName.Replace("(", "\(").Replace(")", "\)").Replace(".", "\.") & "\]=[0-9]+)")
        '                    Dim match = r.Match(origFilter)
        '                    If match.Groups.Count > 1 Then
        '                        Dim fi = origFilter.Replace(match.Groups(1).Value, "").Replace("()", "").Replace("()", "").Replace("( OR ", "(").Replace("( AND ", "(").Trim(" AND ".ToArray).Trim(" OR ".ToArray)
        '                        Vw.RowFilter = fi & If(String.IsNullOrEmpty(fi) AndAlso Not fi.EndsWith("AND "), "", " AND ") & "[" & f.propertyName & "]=" & numericvalue
        '                    End If
        '                    'DONT change this - we WANT to add a 1 
        '                    If Not dicNums(f).ContainsKey(numericvalue) Then
        '                        dicNums(f).Add(numericvalue, 0)
        '                    End If
        '                    If Vw.Find(row("id")) > -1 Then dicNums(f)(numericvalue) += 1
        '                End If
        '            Case "BANDS", "NUMBANDS"
        '                    If numericvalue > Int64.MinValue Then
        '                    'Vw.RowFilter = origFilter.Replace(f.propertyName, "%") & If(String.IsNullOrEmpty(origFilter), "", " AND ") & "[" & f.propertyName & "]=" & numericvalue
        '                    'Vw.RowFilter = Vw.RowFilter.Replace("AND", "OR")
        '                        values.Add(numericvalue)
        '                    End If
        '        End Select

        '    End If
        'Next

        Vw.Sort = so
    End Sub

    ''' <summary>
    ''' Fills the specified column on the datatable with numeric values retrieved for the field F
    ''' </summary>
    ''' <remarks>Also Populates dicNums,dicTrans and dicBands - used for the QuickFilters</remarks>
    Private Sub fillDTCol(dt As DataTable, f As clsField, col As DataColumn, dic As Dictionary(Of clsBranch, clsVisibility), buyeraccount As clsAccount, language As clsLanguage, foci As HashSet(Of String), lid As UInt64)

        If dic.Count = 0 Then Exit Sub

        'TODO - this needs to fill dictrans etc or use something else based on the view?
        Dim translation As clsTranslation = Nothing

        If dic.Count = 0 Then Exit Sub

        Dim numericvalue As Int64
        Dim values As List(Of Int64) = New List(Of Int64)
        Dim branch As clsBranch = Nothing

        'index the Visbilities - by branch (for fast access to the  PATHs we'll need

        Dim vbb As New Dictionary(Of clsBranch, clsVisibility)
        For Each v In dic.Values
            If Not vbb.ContainsKey(v.branch) Then vbb.Add(v.branch, v)
        Next
        Dim errorMessages As List(Of String) = New List(Of String)()

        'Dim dv As DataView = dt.AsDataView() - iteraating over a view (instead of the datatable) didn't ahelp
        'dv.Sort = "ID"

        'For Each row As DataRow In dt.Rows     'we're working with the entire unfiltered datatable here always) - no view involved
        For i = 0 To dt.Rows.Count - 1
            Dim row As DataRow = dt.Rows(i)


            Dim id As Integer = row("id")
            branch = iq.Branches(id)
            Dim cellVal
            'The cellvalue returns Numeric Value (if present), or translation.Sortvalue (which IS translation .order from non-zero orders, or an 'alphabetical' sort otherwise
            Dim unit As clsUnit = Nothing
            If vbb.ContainsKey(branch) Then  'this check shouldn't be required but JIT carepacks has some issue
                cellVal = f.CellValue(branch, vbb(branch).path, buyeraccount, language, "", Nothing, False, translation, unit, foci, errorMessages, lid)
                If IsNumeric(cellVal) Then
                    numericvalue = cellVal
                End If
            Else
                Continue For
            End If

            'This remembers the units for this column - which are needed for the labels in numeric quickFilters
            If unit IsNot Nothing Then
                row.Item(col.ColumnName + "UNIT") = unit.ID
            End If

            If f.QuickFilterUItype = "TKEY" Then
                If translation IsNot Nothing Then
                    row(col.ColumnName) = translation.Key
                Else
                    If numericvalue <> Int64.MinValue AndAlso numericvalue <> Int64.MaxValue Then
                        row.Item(col) = iq.AddTranslation(numericvalue.ToString, English, "fv", 0, Nothing, 0, False).Key
                    Else
                        row.Item(col) = numericvalue
                    End If
                End If
            Else

                'any numeric value on 'check' type fields is treated as a 1
                ' If f.QuickFilterUItype = "CHECK" And numericvalue > Int64.MinValue Then numericvalue = 1
                If numericvalue = 0 AndAlso IsNumeric(cellVal) = False Then
                    row.Item(col) = cellVal
                Else
                    row.Item(col) = numericvalue
                End If
                '<<<THIS is 
                '
                '                If f.QuickFilterUItype = "BANDS" Or f.QuickFilterUItype = "NUMBANDS" Then
                ' If numericvalue > Int64.MinValue Then
                ' values.Add(numericvalue)
                'End If

                'ElseIf f.QuickFilterUItype = "NUMS" Or f.QuickFilterUItype = "CHECK" Then

                'If numericvalue > Int64.MinValue Then
                '    'DONT change this - we WANT to add a 1 
                '    If Not dicNums(f).ContainsKey(numericvalue) Then
                '        dicNums(f).Add(numericvalue, 1)
                '    Else
                '        dicNums(f)(numericvalue) += 1
                '    End If
                'End If
                'End If

            End If
        Next

        'make a set of bands for the set of values we just retrieved - Each band will have (approximately the same NUMBER of values)
        'If f.QuickFilterUItype = "BANDS" Or f.QuickFilterUItype = "NUMBANDS" Then
        '    If values.Count Then
        '        dicBands(f) = MakeBands(values)
        '    End If
        'End If


    End Sub
    Private Sub AddFieldToQFDic(f As clsField)
        Select Case f.QuickFilterUItype
            Case Is = "TKEY"
                If Not dicTrans.ContainsKey(f) Then
                    dicTrans.Add(f, New Dictionary(Of clsTranslation, Integer))
                End If
            Case Is = "BANDS", "NUMBANDS"
                If Not Me.dicBands.ContainsKey(f) Then
                    dicBands.Add(f, New List(Of clsBand))
                End If
            Case Is = "NUMS", "CHECK", "BOOL"

                If Not dicNums.ContainsKey(f) Then
                    dicNums.Add(f, New Dictionary(Of Int64, Integer))  'counts of distinct VALUES (by field)
                End If
                ' Case Is = "CHECK"
            Case Is = ""

            Case Else
                AuditLog.Instance.Add(AuditType.Error, "unknown quickFilterUItype:" & f.QuickFilterUItype, "clsScreenHeader", lid)
        End Select
    End Sub

    'Builds the bands such that each contains the same number of results - Eg, for 5 bands and 100 results, each band would have 20 results
    'NOTE: - This means that the min and max VALUES of the bands have not particular relationship to the range 
    'However - it's more useful to be able to fitler to the 'top 20% of laptops) rather than some arbitray value based bands

    Private Function MakeBands(values As List(Of Long), filteredValues As List(Of Long), BandType As String) As List(Of clsBand)

        'build the bands - such that each contains the same number of results 

        MakeBands = New List(Of clsBand)

        Dim numBands As Integer = 5
        Dim sortedValues = From j In values Order By j Select j
        Dim bottom As Int64 = Int64.MinValue
        Dim top As Int64 = 0
        Dim chunk As Integer = values.Count \ numBands
        Dim band As clsBand


        If sortedValues.Distinct.Count < 5 Then

            band = New clsBand(sortedValues.First, sortedValues.Last, values.Count)
            MakeBands.Add(band)

        Else
            bottom = sortedValues.First
            Dim i = 0
            While bottom <> sortedValues.Last And i < 4
                i = i + 1
                Dim skip As Integer = CType(i / numBands * sortedValues.Count, Integer)

                top = Math.Ceiling((From z In sortedValues.Skip(skip)).First)
                band = New clsBand(bottom, top, filteredValues.Where(Function(f) f >= bottom AndAlso f <= top).Count)
                If bottom > top Then
                    bottom = sortedValues.Where(Function(sv) sv > top).First
                    Continue While
                End If

                If Not MakeBands.Contains(band) Then MakeBands.Add(band)
                If sortedValues.Where(Function(sv) sv > top).Count = 0 Then Exit While
                bottom = sortedValues.Where(Function(sv) sv > top).First

            End While

            'need to make the last band (i think)
            top = sortedValues.Last
            band = New clsBand(bottom, top, filteredValues.Where(Function(f) f >= bottom AndAlso f <= top).Count)
            MakeBands.Add(band)

            'Round and overlap the bands
            'For Each band In MakeBands
            '    ' band.Stretch()
            'Next
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
    Public Sub CombinedFilters(includeSelf As Boolean, ByRef combFilters As Dictionary(Of clsField, Dictionary(Of clsFilter, System.Collections.Generic.KeyValuePair(Of List(Of Long), List(Of Long)))))

        For Each f In Filters
            For Each d In f.Value
                For Each g In d.Value
                    If Not combFilters.ContainsKey(f.Key) Then combFilters.Add(f.Key, New Dictionary(Of clsFilter, System.Collections.Generic.KeyValuePair(Of List(Of Long), List(Of Long))))
                    If Not combFilters(f.Key).ContainsKey(d.Key) Then combFilters(f.Key).Add(d.Key, New System.Collections.Generic.KeyValuePair(Of List(Of Long), List(Of Long))(New List(Of Long), New List(Of Long)))
                    If Not combFilters(f.Key)(d.Key).Key.Contains(g) Then combFilters(f.Key)(d.Key).Key.Add(g)
                Next
                If d.Key.Code = "LE" AndAlso combFilters(f.Key).ContainsKey(iq.i_Filters_Code("GE")) OrElse d.Key.Code = "GE" AndAlso combFilters(f.Key).ContainsKey(iq.i_Filters_Code("LE")) Then
                    combFilters(f.Key).Add(iq.i_Filters_Code("LEGE"), New System.Collections.Generic.KeyValuePair(Of List(Of Long), List(Of Long))(combFilters(f.Key)(iq.i_Filters_Code("LE")).Key, combFilters(f.Key)(iq.i_Filters_Code("GE")).Key))
                    combFilters(f.Key).Remove(iq.i_Filters_Code("GE"))
                    combFilters(f.Key).Remove(iq.i_Filters_Code("LE"))
                End If

            Next
        Next

    End Sub

    Private Function ConstructFilter(includeSelf As Boolean) As String

        'Turns the current filters dictionary -  into something actually usable by the dataview
        'note, the operator segment generaly contains [filterValue] and [col] placeholders - which is replaced with the value segment and column name
        'thus  738|SW|4
        'becomes
        '[Displayname]=LIKE 'home*' AND [years]=4


        Dim pth = Path.Substring(0, Path.Length - Path.Split(".").Last.Length - 1) 'Get the parent path
        Dim sh = CType(iq.sesh(lid, "screenHeaders"), Dictionary(Of String, clsScreenHeader)) 'Get the parents screenHeader, if it exists
        If sh.ContainsKey(pth) Then ConstructFilter = sh(pth).ConstructFilter(True) Else ConstructFilter = "" 'Add the parents filter to this one

        'Add any promo filters (which are forced in)

        If iq.seshDic(lid).ContainsKey("promoinforce") AndAlso Me.Path.Split(".").Length <= 4 Then 'no, no, no
            Dim pmo As clsPromo = iq.Promos(CInt(iq.sesh(lid, "promoinforce")))
            If Me.screen.i_field_property.ContainsKey(pmo.FieldProperty_Filter) Then
                If Not ConstructFilter = "" Then ConstructFilter &= " AND "
                ConstructFilter &= " ([" & pmo.FieldProperty_Filter & "]=" & pmo.FieldProperty_Value & ")"

                'Dim fid = Me.screen.i_field_property(pmo.FieldProperty_Filter)

                'If Not Filters.ContainsKey(fid) Then
                '    .Add(fid, New Dictionary(Of clsFilter, System.Collections.Generic.KeyValuePair(Of List(Of Long), List(Of Long))))
                'End If
                'combFilters(fid).Add(iq.i_Filters_Code("EQ"), New System.Collections.Generic.KeyValuePair(Of List(Of Long), List(Of Long))(New List(Of Long) From {pmo.FieldProperty_Value}, New List(Of Long)))
            End If
        End If

        If Filters Is Nothing OrElse Not includeSelf Then Return ConstructFilter

        If ConstructFilter <> "" Then ConstructFilter &= " AND "

        For Each fld In Filters.Keys
            For Each flt In Filters(fld).Keys
                'Where we have a string value - it needs to be 'quoted' (form factors)

                Dim colname$

                Dim qp$ = String.Empty
                For Each filt In Me.Filters(fld)(flt)
                    qp$ &= "(" & Replace(flt.Filter, "[filterValue]", filt)  'grab the template for this filter criterea and replace the current value 
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

                If Not String.IsNullOrEmpty(qp$) Then
                    ConstructFilter &= "(" & qp$ & ") AND "
                End If
            Next
        Next

        If ConstructFilter <> "" Then ConstructFilter = Left(ConstructFilter, Len(ConstructFilter) - 5) 'Take the last AND off

        If ConstructFilter.Trim() = "()" Then ConstructFilter = ""

    End Function


    Public Sub UpdateSorts(NewPriority As clsPriorityDirection)

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

        ' Me.populateData(descendants, Me.DT, buyeraccount, language, foci, errormessages, lid)


        Me.Vw.Sort = Me.sortsString

    End Sub

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
                    errormessages.Add("* Can't remove filter with the CODE " & p(1))
                End If
            Next i
        Else
            errormessages.Add("* No filter present for field " & p(0))
        End If

        Me.Vw.RowFilter = ConstructFilter(True)

    End Sub

    Public Sub ClearFilter(filterID As String)

        Dim filterField As clsField = iq.Fields(CInt(filterID))

        If Me.Filters.ContainsKey(filterField) Then
            Me.Filters(filterField).Clear()
        End If

    End Sub

    Public Sub ClearGroupFilter(filterID As String)

        Dim filterField As clsField = iq.Fields(CInt(filterID))

        ' Clear all filters belonging to the same group
        For Each f In Me.Filters
            If f.Key.QuickFilterGroup.Key = filterField.QuickFilterGroup.Key Then
                f.Value.Clear()
            End If
        Next

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
                    If fld.HMC_MutuallyExclusive OrElse fld.QuickFilterUItype = "BOOL" Then
                        Me.Filters(fld)(flt).Clear()
                    End If
                    If String.IsNullOrEmpty(p(i + 1)) Then
                        Me.Filters(fld)(flt).Clear()
                    Else
                        If Me.Filters(fld)(flt).Contains(CType(p(i + 1), Int64)) Then
                            If Me.Filters(fld)(flt).Count = 1 Then
                                Me.Filters(fld).Remove(flt)
                            Else
                                Me.Filters(fld)(flt).Remove(CType(p(i + 1), Int64))
                            End If
                        Else
                            Me.Filters(fld)(flt).Add(CType(p(i + 1), Int64))
                        End If
                    End If
                Else
                    Me.Filters(fld).Add(flt, New List(Of Int64))
                    Me.Filters(fld)(flt).Add(CType(p(i + 1), Int64))
                End If
            End With
        Next i

        ' Me.addMissingColumns(descendants, Me.DT, buyeraccount, language, foci, errormessages, lid) ML TODO READD

        Me.Vw.RowFilter = Me.ConstructFilter(True)
       

    End Sub

    Public Sub setColState(fld As clsField, state As enumColState)
        ColState(fld) = state 'expaned/collapsed (etc)
    End Sub

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

    Public Sub InvalidateFields()
        _FieldResultSet = Nothing
    End Sub

    Public Sub SetDefaultFilterOn(Optional ByRef filterNotFound As String = "")

        'Clear current filters
        Filters.Clear()

        'Fetch defaults
        For Each f In EffectiveFields
            If f.DefaultFilterValues IsNot Nothing Then
                UpdateFilters(String.Format("{0}|{1}", f.ID, f.DefaultFilterValues))
            End If
        Next
        Dim stringB As StringBuilder = New StringBuilder()
        For Each dcol As DataColumn In Me.Vw.Table.Columns

            stringB.Append(dcol.ColumnName & ",")
        Next
        'Log4NetMessage(stringB.ToString())
        'stringB = New StringBuilder()
        'For Each dRow As DataRow In Me.Vw.Table.Rows
        '    For Each dcol As DataColumn In Me.Vw.Table.Columns

        '        stringB.Append(dRow(dcol.ColumnName) & ",")
        '    Next
        '    Log4NetMessage(stringB.ToString())
        '    stringB = New StringBuilder()
        'Next

        If Me.Vw.Count = 0 Then
            Dim newFilterString As List(Of String) = New List(Of String)
            Dim filterString() As String = Split(Me.Vw.RowFilter, "AND")
            For intloop = 0 To filterString.Length - 1
                Me.Vw.RowFilter = filterString(intloop)
                If Me.Vw.Count > 0 Then
                    newFilterString.Add(filterString(intloop))
                Else
                    filterNotFound = filterString(intloop)
                    Dim intstart As Integer = InStr(filterNotFound, "[")
                    Dim intend As Integer = InStr(filterNotFound, "]")
                    Dim fieldProperty As String = Mid(filterNotFound, intstart + 1, intend - intstart - 1)

                    Dim a = From f In EffectiveFields Where f.propertyName = fieldProperty
                    If a.Count = 1 Then
                        filterNotFound = a.First.ID
                    End If

                End If
            Next
            Dim updateFilterString As String = String.Join(" AND ", newFilterString.ToArray())
            Me.Vw.RowFilter = updateFilterString
            '     filterNotFound = updateFilterString

        End If

    End Sub


    Public Function ColIsCollapsed(fld As clsField) As Boolean

        'If Me.ColState IsNot Nothing Then
        If Me.ColState.ContainsKey(fld) Then
            If Me.ColState(fld) = enumColState.SoftCollapsed Or Me.ColState(fld) = enumColState.HardCollapsed Then ColIsCollapsed = True
        End If
        'End If
    End Function

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
                        errormessages.Add("* Unexpected column state")
                End Select
            Else
                ColState.Add(fld, enumColState.SoftExpanded) 'first pass - set all colups to soft expanded
                w = w + FieldResultSet(fld).Width

            End If
            ' End If
        Next

        Return w


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

        Dim branch As clsBranch = iq.Branches(CInt(Split(Me.Path, ".").Last))


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

            Vw.RowFilter = Me.ConstructFilter(True) '(Me.matrix, Me.Filters)

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

                Case Is = "NUMS", "CHECK", "BOOL"

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

            ''   If Not DT.Columns.Contains(fld.propertyName) Then
            ' errorMessages.Add("Invalid column specified:'" & fld.propertyName & "' (Check cAsE)")
            ' Else


            For i = 0 To Vw.Count - 1  'This view is a (filtered) subset of the datatable (which is a cache of all the nuericvalues of all the fields)

                If fld.QuickFilterUItype <> "" Then

                    If TypeOf (Vw.Item(i)(fld.propertyName)) Is DBNull Then
                        numericvalue = Int64.MaxValue
                    Else
                        numericvalue = CType(Vw.Item(i)(fld.propertyName), Int64)  '.DataView.Item()
                    End If


                    ' If LCase(fld.propertyName) = "product.i_attributes_code(tch)(0)" Then Stop

                    Select Case fld.QuickFilterUItype

                        Case Is = "CHECK", "BOOL"

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

            'End If

            If holdIt IsNot Nothing Then
                Me.Filters.Add(fld, holdIt)  'Put back the filter for this column
            End If

        Next

        bi.survivors = Vw.Count

    End Sub



    Public Function UI(bi As clsBranchInfo, ByRef errormessages As List(Of String), lid As UInt64) As PlaceHolder

        'Me.Path = bi.path
        ' Me.matrix = MatrixAbove(Me.path) 'NOTE: this is the ONLY place we find out what TYPE of matrixHeader we need to instance


        'Returns the user facing UI for filtering and sorting this matirx
        UI = New PlaceHolder
        'If AccountHasRight(lid, "DIAGVIEW") Then UI.Controls.Add(NewLit(Me.screen.ID))

        Dim b = Me.FieldResultSet.ToList().Where(Function(f) f.Key.QuickFilterGroup IsNot Nothing)
        If Me.FieldResultSet.ToList().Where(Function(f) f.Key.QuickFilterGroup IsNot Nothing AndAlso f.Value.Visibility AndAlso Not f.Value.PromoColumn).Count() > 0 Then  ' remove blank filters
            If CType(iq.sesh(lid, "Paradigm"), enumParadigm) = enumParadigm.configuringSystem AndAlso Not isSystemAbove(lid, Path) Then
            Else
                Dim panelFilter As Panel = Me.FiltersUI(bi, errormessages)
                If panelFilter Is Nothing Then panelFilter = Me.FiltersUI(bi, errormessages)
                If panelFilter IsNot Nothing Then UI.Controls.Add(panelFilter) 'only show filters above or at system level when not in configure mode
            End If
        End If

        Dim mh As Panel = New Panel
        mh.CssClass = "matrixHeader"
        mh.ID = screen.ID
        UI.Controls.Add(mh)  'MH is a div wich we put the diagonal lables (chart control) plus the filter/sort UI - so it all aligns


        ' mh.Controls.Add(pnlHeadSquares)

        OutputErrors(mh.Controls, errormessages, lid, True)
        If clsBranchState.getbranchstate(bi.lid, Me.Path) IsNot Nothing Then
            If clsBranchState.getbranchstate(bi.lid, Me.Path).rca = enumBt.gridrow Then
                Dim bAccount As clsAccount = iq.sesh(lid, "BuyerAccount")

                ' Add a Help Me Choose button for Microsoft (OS) branches
                If bi.branch.Translation.text(English).Contains("Microsoft OS") Then
                    Dim sysPath As String = Nothing
                    Dim sysBranch As clsBranch = bi.branch.FindSystemAbove(bi.path, sysPath)
                    Dim toPath As String = Nothing
                    sysBranch.FindBranchByNameBelow("Operating System", "", True, 12, toPath)
                    If Not String.IsNullOrEmpty(toPath) Then
                        toPath = sysPath.Substring(0, sysPath.LastIndexOf(".")) + toPath
                        If Not String.IsNullOrEmpty(toPath) Then
                            mh.Controls.Add(NewLit("<button class='hmc hpBlueButton ib showHMC' onclick='getBranches(""cmd=defFilterOn&path=" + sysPath + "&to=" + toPath + "&into=tree"");return false;'>" & Xlt("Help Me Choose", bi.buyerAccount.Language) & "</button>&nbsp;&nbsp;"))
                        End If
                    End If
                End If

                If AccountHasRight(lid, "GLOBALADM") Then mh.Controls.Add(NewLit(String.Format("<button class=""CustomizeColumns hmc hpBlueButton ib showHMC"" onclick=""$('#FieldFilter').draggable();$('#FieldFilter').show('slide right');$.ajax({{ url: '../Data/GetAvailableFields', data: JSON.stringify({{lid:'{0}', BranchPath:'{1}'}}),type: 'POST', contentType: 'application/JSON',success: GetAvailableFields_Success }});return false;"">" & Xlt("Customise Columns", bi.buyerAccount.Language) & "</button>", bi.lid, Me.Path)))

                If bi.branch.Translation.Group = "OL3" Then 'simple screen for OL3, basically a list
                    'mh.Controls.Add(Me.matrix.MatrixHeaders(Me))  'renders the header with diagonal lables
                    mh.Controls.Add(Me.RemoveFiltersUI(bi.buyerAccount.Language)) '.MatrixPath, bi.lid, bi.AgentAccount.Language)) ' todo - reinstate
                    Exit Function
                End If

                mh.Controls.Add(Me.SortDropDownsUI(bi.agentAccount.Language)) 'DropDowns
                mh.Controls.Add(Me.screen.MatrixHeaders(Me, bi, bi.agentAccount.Language))  'renders the header with diagonal lables
                Dim blit As Literal = New Literal

                mh.Controls.Add(Me.ExpandCollapseColumnButtons(bi, bi.agentAccount.Language))
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

                        If Me.Filters(fld)(flt).Any Then
                            Dim filterValues As New List(Of String)
                            If fld.QuickFilterUItype = "TKEY" Then
                                'If we're filtering against tranlsations values - we need to be able to display the text
                                Dim wi As IEnumerable(Of String) = Nothing
                                wi = Me.Filters(fld)(flt).Select(Function(fi) iq.Translations(fi).text(language))
                                If Not wi Is Nothing AndAlso wi.Count > 0 Then
                                    filterValues = wi.ToList
                                End If

                            Else
                                'otherwise we display the 'raw' 64 bit numbers
                                Dim wi = Filters(fld)(flt).Select(Function(fi) fi.ToString)
                                filterValues = wi.ToList
                            End If

                            rb.ToolTip = Xlt("Remove the filter:- ", language) & fld.labelText.text(language) & " " & flt.DisplayText.text(language) & " "

                            rb.ToolTip &= Join(filterValues.ToArray, ",")
                        End If

                        'rb.ToolTip = Xlt("Remove the filter:- ", language) & fld.labelText.text(language) & " " & flt.DisplayText.text(language) & " " & String.Join(",", If(fld.QuickFilterUItype = "TKEY", Me.Filters(fld)(flt).Select(Function(fi) iq.Translations(fi).text(language)).ToArray, Me.Filters(fld)(flt))) 'ML - todo , add all values to this tip
                        occ$ = "getBranches('path=" & Me.Path & "&cmd=removeFilter&filterPath=" & Me.Path & "&filterParams=" & Trim$(fld.ID.ToString) & "|" & flt.Code & "');return false;"

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

    Public Function SortDirectionsUI(language As clsLanguage) As Panel

        'formerly sortUI - now just provided the direction arrows

        SortDirectionsUI = New Panel
        SortDirectionsUI.ID = "sortDirections." & Me.Path

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
            col.ID = Path$ & ".F" & f.ID

            Dim j As IEnumerable(Of clsPriorityDirection) = Nothing
            j = From v In Me.sorts.Values Where v.column Is f 'Pull out the sort info pertaining to this column
            If j.Any Then

                Dim pd As clsPriorityDirection = j.First

                'Dim currentValue As String = "-"
                'ptb = f.SortPriorityTextBox(Me.sorts$, currentValue)
                col.Controls.Add(pd.UI(Path, language))

                'occ$ = "sortPath='" & path$ & "';sortFieldID=" & f.ID & ";showSortPriorityPicker('" & col.ID & "','" & currentValue & "');"

                ' ptb.Attributes("onfocus") = occ$
                ' ptb.ToolTip = "Set the priority and direction of this sorting for this column"
                ' col.Controls.Add(ptb)

                If LCase(f.propertyName) = "stock" Or LCase(f.propertyName) = "customerprice" Then

                    'todo - check if we're actually sorting by either of these !

                    'If InStr(Me.sorts$, f.propertyName) > 0 Then  ' a little crude - but good enough
                    btnReSort = New Literal
                    btnReSort.Text = "<div ID=|resort." & Path$ & f.propertyName & "| class=|re-sort| style=|display:none| onmousedown=|getBranches('path=" & Path & "&cmd=invalidate');return false;|" & "> &nbsp;</div>"
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
        ui.ID = "sorts." & Me.Path
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
        ddl.ID = "Sort_" & Me.Path$ & priority 'the DDL needs a unique ID (note, many grides can be present in the made simultaeneously
        MakeDDL.CssClass = "aSortDropDown"

        If SelectedColumn Is Nothing Then
            Dim i As ListItem = New ListItem(Xlt("Add a sort", language), "add") 'You can't actually select this, so it's value is unimportant (The onchange will fire on this DDL, adding a new sort)
            ddl.Items.Add(i)
            ddl.SelectedValue = "add"
        End If

        For Each f In Me.EffectiveFields
            If f.visibleList Then
                If Not ColIsCollapsed(f) Then
                    Dim i As ListItem = New ListItem(f.labelText.text(language), f.ID.ToString & "," & priority & direction)
                    If f.labelText.text(language) Is Nothing Or f.labelText.text(language) = "" Then
                        Continue For
                    End If
                    ddl.Items.Add(i)
                    If f Is SelectedColumn Then
                        ddl.SelectedValue = i.Value 'Select the right column in this dropdown (these is one dropdown for each active sort order)
                    End If
                End If
            End If
            'UI.Attributes("onclick") = "getBranches('" & path & "', 'priority=" & Me.column.ID & "," & Me.Priority & "D');" ' + sortFieldID + ',' + v);"
        Next

        ddl.Attributes("onchange") = "DisableElementsByClassName('FF','" & Path & "');var ddl=document.getElementById('" & ddl.ID & "');var spd=ddl.options[ddl.selectedIndex].value;getBranches('path=" & Me.Path & "&cmd=sort&value='+spd);"

        If SelectedColumn IsNot Nothing Then
            Dim killButton As Panel = New Panel
            killButton.CssClass = "removeButton"
            Dim lit As Literal = New Literal
            lit.Text = "&nbsp;"
            killButton.Controls.Add(lit)
            killButton.Attributes("onclick") = "DisableElementsByClassName('FF','" & Path & "');getBranches('cmd=removeSort&path=" & Me.Path$ & "&priority=" & priority.ToString.Trim & "');"  'Removesort works by the priority
            MakeDDL.Controls.Add(killButton)
        End If

    End Function


    ''' <summary>
    ''' A 'quickfilter' is a set of checkboxes containing the distinct values in one field (column)
    ''' The act of rendering the quickfilters UI also (pre) scans the survivors (determined by the filter on the view) to enable/disable the correct options
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function FiltersUI(bi As clsBranchInfo, ByRef errorMessages As List(Of String)) As Panel
        Dim FilterDataNotFound As Boolean = False

        FiltersUI = New Panel
        FiltersUI.Attributes("class") = "quickFilterGroupHolder"
        Dim ButtonPanel As Panel = New Panel
        ButtonPanel.CssClass = "FilterButtonContainer"

        'Dim filterDisplay As Literal
        'If Not String.IsNullOrEmpty(Me.Vw.RowFilter) Then
        '    filterDisplay = New Literal
        '    filterDisplay.Text = String.Format("<div>Filter: {0}</div><br/>", Me.Vw.RowFilter)
        'End If

        If Me.QuickFiltersVisible Then
            Dim lit As Literal = New Literal
            Dim bid As String = "hmcb." & bi.path
            lit.Text = Replace("<div id=|" & bid & "| class=|QF hpBlueButton hideQF| onclick=|DisableElementsByClassName('FF','" & Path & "');getBranches('cmd=hideQuickFilters&path=" & bi.path & "')|>Hide filters</div>", "|", Chr(34))
            ButtonPanel.Controls.Add(lit)
        End If
        'If bi.bs IsNot Nothing And Not bi.Branch.HasSKU AndAlso bi.PathLevel <> 1 Then
        If Not bi.branch.HasSKU AndAlso bi.PathLevel <> 1 Then
            'this is an 'open','category' branch (eligible for quick filters)

            'this is the 'show filters' button the hide filters is in the matrixheader itself
            If Me.QuickFiltersVisible = False AndAlso screen.Fields.ToList().Where(Function(f) f.Value.QuickFilterGroup IsNot Nothing AndAlso f.Value.FilterVisible).Count() > 0 Then

                If bi.MoreThanXskus(5) AndAlso bi.branch.Translation.Group <> "OL3" AndAlso Me.hasQuickFilters() Then 'need to include parent filters in this!
                    Dim bid As String = "hmcb." & bi.path  'just needs a unique DIV id (serves no other purpose)
                    Dim lit As New Literal
                    lit.Text = Replace("<div id=|" & bid & "| class=|hmc hpBlueButton ib showHMC| onclick=|getBranches('cmd=quickFilter&path=" & bi.path & "');return false|> " & If(Me.Filters.ToList().Where(Function(f) f.Value.Count() > 0).Count > 0, Xlt("Filtered", bi.buyerAccount.Language), Xlt("Filter", bi.buyerAccount.Language)) & "</div>", "|", Chr(34)) ' ML Removed <div class='clear'></div> as this was adding a huge space in scenaro, add quote click on earlier breadcrumb
                    ButtonPanel.Controls.Add(lit)
                End If
            End If
            Dim pth As String = ""
            If bi.PathLevel() < 6 Then
                Dim mh As Dictionary(Of String, clsScreenHeader) = iq.sesh(bi.lid, "screenHeaders")
                If iq.seshDic(lid).ContainsKey("promoinforce") Then
                    Dim pif = iq.Promos(CInt(iq.sesh(lid, "promoinforce")))
                    ButtonPanel.Controls.Add(NewLit("<div class=""FilterButton"" onclick=""burstBubble(event);getBranches('cmd=removePromoLink');"" title=""Promo""><span>" & pif.displayName(bi.buyerAccount.Language) & "</span></div>"))
                End If
                For Each seg In Split(bi.path, ".")
                    pth &= seg
                    If mh.ContainsKey(pth) AndAlso mh(pth).Filters IsNot Nothing Then
                        For Each f In mh(pth).Filters
                            Dim rf = String.Join("|", f.Value.Select(Function(fil) String.Join("|", fil.Value.Select(Function(fd) f.Key.ID.ToString() + "|" + fil.Key.Code + "|" + fd.ToString()))))
                            Dim tlate As List(Of String) = New List(Of String)()
                            If f.Key.InputType.code.ToLower = "translate" Then
                                For Each va In f.Value.Values
                                    tlate.Add(iq.Translations(CInt(va(0).ToString)).text(bi.buyerAccount.Language))
                                Next
                            End If

                            If f.Value.Count > 0 Then ButtonPanel.Controls.Add(NewLit("<div class=""FilterButton"" title=""Remove: " + String.Join(",", f.Value.Select(Function(d) d.Key.DisplayText.text(English) & " " & If(f.Key.InputType.code.ToLower = "translate", String.Join(",", tlate), String.Join(",", d.Value)))) + """ onclick=""" + bi.branch.ButtonScript("cmd=removeFilter&filterParams=" + rf + "&path=" + bi.path & "&into=" + bi.path + "&filterPath=" + pth) + """><span>" + iq.Branches(Split(pth, ".").Last()).DisplayName(English) + " - </span><span>" + f.Key.labelText.text(bi.agentAccount.Language) + "</span></div>"))
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
            Me.indexData()

            'Dim tKeyDic As Dictionary(Of clsField, List(Of clsTranslation)) = Me.quickfilterTextKeys  

            Dim EQfilter As clsFilter = iq.i_Filters_Code("EQ")

            Dim FilterControlArray As Dictionary(Of Panel, Integer) = New Dictionary(Of Panel, Integer)()
            Dim dicPanels As Dictionary(Of String, Panel) = New Dictionary(Of String, Panel)
            Dim pnl As Panel

            ' Look for grouped sets of BOOL fields so we can render No Preference buttons after the last one
            ' - could be extended to other field types
            Dim noPrefGroupings As New Dictionary(Of Integer, Integer)
            For Each fld In Me.FieldResultSet
                If fld.Key.QuickFilterUItype = "BOOL" Then
                    Dim groupId = fld.Key.QuickFilterGroup.Key
                    If noPrefGroupings.ContainsKey(groupId) Then
                        noPrefGroupings(groupId) += 1
                    Else
                        noPrefGroupings.Add(groupId, 1)
                    End If
                End If
            Next
            Dim noPrefCounts As New Dictionary(Of Integer, Integer)
            For Each group In noPrefGroupings.Keys
                noPrefCounts.Add(group, 0)
            Next
            '  Dim stringLookup = "SVR SCRHPN STORAGE"
            For Each fld In Me.FieldResultSet.ToList().Where(Function(ord) ord.Key.QuickFilterGroup IsNot Nothing AndAlso ord.Key.FilterVisible AndAlso Not ord.Value.PromoColumn).OrderBy(Function(ord) ord.Key.QuickFilterGroup.Order).Select(Function(ord) ord.Key)
            
                Dim filterDataFound As Boolean = False
                ' If fld.QuickFilterGroup IsNot Nothing Then
                '   If Not (fld.propertyName = "CustomerPrice" And stringLookup.Contains(fld.Screen.code.ToUpper())) Then
                If Filters IsNot Nothing AndAlso Not Filters.ContainsKey(fld) Then
                    filterDataFound = True
                End If

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
                    Case "CHECK", "BOOL"  'the field must evaluate to a productattribute - checking the box will fitler (by productattribute) to those with true (1) values

                        'check the boxes to match the current filters
                        Dim value As Boolean? = Nothing
                        If Filters IsNot Nothing Then
                            If Filters.ContainsKey(fld) Then
                                Dim eq As clsFilter = iq.i_Filters_Code("EQ") 'EQ is the Equals filter 
                                If Filters(fld).ContainsKey(eq) Then
                                    If Filters(fld)(eq).Contains(1) Then
                                        value = True
                                    End If
                                    If Filters(fld)(eq).Contains(0) Then
                                        value = False
                                    End If
                                End If
                            End If
                        End If

                        Dim chkBox As Literal = New Literal  'You *can't* use actual checkboxes - don't try - .NET has a nasty habit of wrapping them in spans - and then attaching any script to the span (not the checkbox) - literals are the answer.
                        Dim filterClicKHandler As String
                        filterClicKHandler = "filterfield=" & fld.ID & ";DisableElementsByClassName('FF','" & Path & "');if(this.checked){getBranches('cmd=changeFilter&path=" & Me.Path & "&filterParams=" & fld.ID & "|EQ|1')}else{getBranches('cmd=changeFilter&path=" & Path & "&filterParams=" & fld.ID & "|EQ|')};"

                        Dim count As Integer = 0
                        If dicNums.ContainsKey(fld) Then
                            If dicNums(fld).ContainsKey(1) Then
                                count = dicNums(fld)(1) '1 is the 'true' value - and the key to the count
                            End If
                        End If

                        ' Render the checkbox
                        Dim disabled = IIf(count = 0, "disabled ", String.Empty)

                        Dim checked = CType(IIf(value.HasValue AndAlso value.Value, "checked", ""), String)
                        If checked = "checked" Then filterDataFound = True
                        chkBox.Text = String.Format("<input class='FF' type='checkbox' {0} {1} onclick= {2}/>", disabled, checked, Chr(34) & filterClicKHandler & Chr(34))

                        'The quickfilterUI Interacts with the filters

                        ip.Controls.Add(chkBox)



                        Dim lbl As Label = New Label
                        lbl.Text = fld.labelText.text(bi.agentAccount.Language) 'Xlt("Yes", bi.agentAccount.Language) '
                        Dim dvs = dicNums(fld).Count
                        lbl.Text &= " (" & count & ")"  'add the count of True values (non true values are not present)

                        lbl.CssClass = "quickFilterLabel"
                        'If count = 0 Then lbl.CssClass &= " disabled" : lbl.ToolTip = Xlt("This option isn't availble (in combination with your other selections)", bi.buyerAccount.Language)
                        ip.Controls.Add(lbl)


                        ' If this is the last in the list of grouped fields, optionally render a No Preferences button for the whole group
                        Dim groupId = fld.QuickFilterGroup.Key
                        If noPrefCounts.ContainsKey(groupId) Then
                            noPrefCounts(groupId) += 1
                            If noPrefCounts(groupId) = noPrefGroupings(groupId) Then

                                ' Add a No Preference button on Help Me Choose screens
                                If (Me.screen.code.StartsWith("hmc") Or Me.screen.code.StartsWith("optCPK")) AndAlso noPrefGroupings(groupId) > 1 Then
                                    ip.Controls.Add(BuildNoPreferenceButton(fld, bi, True))
                                End If

                            End If
                        End If

                    Case "BANDS", "NUMBANDS"
                        'break numerics/prices into bands

                        If dicBands.ContainsKey(fld) Then
                            For Each band In If(fld.InvertFilterOrder, dicBands(fld).OrderByDescending(Function(fb) fb.min), dicBands(fld).OrderBy(Function(fb) fb.min))

                                Dim chkBox As Literal = New Literal
                                Dim occ$

                                'NB this ChangeFilter chanegs/remove TWO filters at once
                                occ$ = "filterfield=" & fld.ID & ";DisableElementsByClassName('FF','" & Path & "');if(this.checked){getBranches('cmd=changeFilter&path=" & Me.Path & "&filterParams=" & fld.ID.ToString & "|GE|" & band.min & "|LE|" & band.max & "')}else{getBranches('cmd=changeFilter&path=" & Me.Path & "&filterParams=" & fld.ID & "|GE|" & band.min & "|LE|" & band.max & "')};"

                                Dim selected As Boolean = band.isSelected(fld, Me.Filters) 'Compare to the currently selected GE/LE filters on this field
                                If selected Then filterDataFound = True
                                chkBox.Text = "<input class='FF' type='checkbox' " & CType(IIf(selected, "checked='checked'", ""), String) & " " & CType(IIf(band.Survivors = 0, "disabled='disabled'", ""), String) & "  onclick=" & Chr(34) & occ$ & Chr(34) & ">"
                                If fld.QuickFilterUItype = "NUMBANDS" Then
                                    'Numeric bands (no currency symbol) and NOT * 100
                                    If band.min = band.max Then
                                        chkBox.Text &= CType(band.min, Decimal).ToString
                                    Else
                                        chkBox.Text &= CType(band.min, Decimal).ToString
                                        chkBox.Text &= " - "
                                        chkBox.Text &= CType(band.max, Decimal).ToString
                                    End If
                                Else

                                    'normal' PRICE bands
                                    chkBox.Text &= bi.buyerAccount.Currency.format(Math.Floor(band.min / 100), bi.buyerAccount.Culture.Code, errorMessages, 0)
                                    chkBox.Text &= " - "
                                    chkBox.Text &= bi.buyerAccount.Currency.format(Math.Ceiling(band.max / 100), bi.buyerAccount.Culture.Code, errorMessages, 0)
                                End If

                                chkBox.Text &= "&nbsp;(" & band.Survivors & ")"  'dicBands(fld)(band)

                                chkBox.Text &= "</input><br/>"
                                If Not (band.min = 0 And band.max = 0) Then
                                    ip.Controls.Add(chkBox)
                                End If

                            Next

                        End If
                        If ip.Controls.Count = 0 Then
                            FilterControlArray.Remove(pnl)
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
                            Dim optionCount As Integer = 0
                            For Each t In dicTrans(fld).Keys.OrderByDescending(Function(tr) tr.Order)

                                If t IsNot Nothing Then
                                    'Dim sv As Int64 = t.SortValue(bi.agentAccount.Language)
                                    Dim rb As Literal = New Literal
                                    rb.Text = "<input class=~FF~ type=~checkbox~ name=~" & nm$ & "~"
                                    rb.Text &= " onclick=~{DisableElementsByClassName('FF','" & Path & "');getBranches('path=" & Me.Path & "&cmd=changeFilter&filterParams=" & fld.ID & "|EQ|" & t.Key.ToString & "')}~"
                                    If Filters.ContainsKey(fld) Then
                                        For Each activefilter In Filters(fld)
                                            If activefilter.Key Is EQfilter Then  'The quickFilters do a strict equals
                                                If activefilter.Value.Contains(t.Key) Then
                                                    filterDataFound = True
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

                                    If optionCount = 0 Then

                                    End If

                                    optionCount += 1
                                End If
                            Next

                            ' Add a No Preference button on Help Me Choose screens
                            If (Me.screen.code.StartsWith("hmc") Or Me.screen.code.StartsWith("optCPK")) AndAlso optionCount > 0 Then
                                ip.Controls.Add(BuildNoPreferenceButton(fld, bi))
                            End If
                        End If

                        'If Me.Filters.ContainsKey(fld) Then
                        '    If Me.Filters(fld).Count > 0 Then
                        '        'ip.Controls.Add(fld.NoPreferenceRadioButton(path, Me.Filters(fld).Keys.ToList)) ' ML - No longer needed now we have check boxes rather than options
                        '    End If
                        'End If

                    Case "NUMS"

                        Dim nm$ = "f_" & fld.ID & "." & bi.path
                        If dicNums.Keys.Count = 0 Then
                            errorMessages.Add("DicNums was not populated")
                        Else
                            If Not dicUnits.ContainsKey(fld) Then
                                errorMessages.Add("no units present for values in " & fld.propertyName)
                            Else
                                'distinct Values of the number
                                For Each v In If(fld.InvertFilterOrder, dicNums(fld).Keys.OrderByDescending(Function(f) f), dicNums(fld).Keys.OrderByDescending(Function(f) f))
                                    Dim rb As Literal = New Literal
                                    rb.Text = "<input class=~FF~ type=~checkbox~ name=~" & nm$ & "~"
                                    rb.Text &= " onclick=~{DisableElementsByClassName('FF','" & Path & "');getBranches('path=" & Me.Path & "&cmd=changeFilter&filterParams=" & fld.ID & "|EQ|" & v.ToString & "')}~"
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

                                If (Me.screen.code.StartsWith("hmc") Or Me.screen.code.StartsWith("optCPK")) AndAlso ip.Controls.Count > 0 Then
                                    ip.Controls.Add(BuildNoPreferenceButton(fld, bi))
                                End If

                                'If Me.Filters.ContainsKey(fld) Then
                                'If Me.Filters(fld).Keys.Count > 0 Then
                                'no preference
                                ' ROK - add a No Preference button
                                'ip.Controls.Add(fld.NoPreferenceRadioButton(path, Me.Filters(fld).Keys.ToList)) 'ML - No longer needed now we have check boxes instead of options 
                                'End If
                                'End If
                            End If
                        End If
                    Case Else
                        errorMessages.Add("Unrecognised quickFilterUIType:'" & fld.QuickFilterUItype & "'")

                End Select

                If Not filterDataFound Then
                    FilterDataNotFound = True
                    Filters.Remove(fld)
                End If
                '  End If
            Next

            If FilterDataNotFound Then             
                Return Nothing
            End If

            For Each p As Panel In FilterControlArray.OrderBy(Function(a) a.Value).Select(Function(a) a.Key)
                FiltersUI.Controls.Add(p)
            Next

            'If Not filterDisplay Is Nothing Then
            '    FiltersUI.Controls.Add(filterDisplay)
            'End If

            'Else
        End If





    End Function

    Private Function BuildNoPreferenceButton(field As clsField, branchInfo As clsBranchInfo, Optional groupedField As Boolean = False) As Literal

        Dim id = String.Format("clearFilter.{0}", Me.Path)
        Dim command As String = If(groupedField, "clearGroupFilter", "clearFilter")
        Dim clickHandler = String.Format("{{DisableElementsByClassName('FF','{0}');getBranches('path={0}&cmd={1}&filterId={2}');}}", Me.Path, command, field.ID)
        Dim text = Xlt("No preference", branchInfo.agentAccount.Language)

        BuildNoPreferenceButton = New Literal
        BuildNoPreferenceButton.Text = String.Format("<div class=""hpGreyButton smallfont"" style=""width:60px"" id=""{0}"" onclick=""{1}"">{2}</div><br/>", field.ID, clickHandler, text)

    End Function

    Private Function ExpandCollapseColumnButtons(bi As clsBranchInfo, language As clsLanguage) As Panel

        Dim panel As Panel = New Panel
        panel.ID = "HeaderExpandRow"
        panel.CssClass = "oneRow"
        panel.Attributes("style") = "height:1em;"

        ' Dim cols As IEnumerable = (From f In Me.EffectiveFields Order By f.order)

        If Not UserIsAdmin(bi.lid) AndAlso (bi.branch.rca.StartsWith("GTB") Or bi.branch.rca.Equals("G")) Then
            ' Start further left on the options screens in basic user mode as no expand/collapse control is displayed
            panel.Controls.Add(NewLit("<div class='LeftPad' style='width:1.25em;display:inline-block;'>&nbsp;</div>"))
        Else
            panel.Controls.Add(NewLit("<div class='LeftPad' style='width:4.25em;display:inline-block;'>&nbsp;</div>"))
        End If

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
                p.Controls.Add(MakeRoundButton("expandColumn.png", "Show Column", "getBranches('path=" & Me.Path & "&cmd=expandColumn&fieldid=" & Trim$(f.ID.ToString) & "');return false;", "", "position:relative;", language, f.ID))
                x = x + collapsedColumnWidth
            Else
                ' img.ImageUrl = "/images/navigation/collapseColumn.png"
                ' img.Attributes("onmousedown") = "getBranches('" & headerPath & "','collapseColumn=" & Trim$(f.ID) & "');return false;"
                ' img.ToolTip = "hide " & f.labelText & " column"



                p.Width = New Unit(FieldResultSet(f).GrownWidth, UnitType.Em)
                If f.labelText.text(English) <> "" Then
                    p.Controls.Add(MakeRoundButton("collapseColumn.png", "Hide Column", "getBranches('path=" & Me.Path & "&cmd=collapseColumn&fieldid=" & Trim$(f.ID.ToString) & "');return false;", "", "position:relative;", language, f.ID))
                End If
                x = x + FieldResultSet(f).GrownWidth 'in ems
            End If
            panel.Controls.Add(p)
        Next

        '' this has been moved onto the containg div - matrixHeader.CssClass = "matrixHeader"
        Return panel


    End Function
    Public Sub regenerateFieldDefs()
        _FieldResultSet = Nothing
    End Sub

#Region "Exports"

    ''' <summary>Export the grid as CSV - respecting the filters and sorts</summary>
    ''' <remarks>Column visibility (collapsedosity) and ordering are not yet respected </remarks>
    Public Function exportCSV(lid As UInt64, descendants As Dictionary(Of clsBranch, clsVisibility), buyeraccount As clsAccount, _
                           l As clsLanguage, foci As HashSet(Of String), ByRef errorMessages As List(Of String), toolsExport As Boolean, Optional export As Boolean = False) As String

        Randomize()
        Dim fn As String = System.IO.Path.GetTempFileName
        fn = System.IO.Path.ChangeExtension(fn, "csv")

        iq.sesh(lid, "tostream") = fn
        iq.sesh(lid, "streamcontent-type") = "text/csv;charset=UTF-8""  "
        iq.sesh(lid, "DeleteStreamed") = True

        Dim sw As StreamWriter = Nothing
        Try
            ' A 'normal' filestream removed the frist CRLF (on the header row WTF ?)
            sw = New StreamWriter(File.Create(fn), System.Text.Encoding.UTF8)

            'Define separator for excel to understand
            'sw.WriteLine("sep=,")
            'Write the quoted names of the columns (in the agent's languae)
            Dim fieldIdListOnScreen As List(Of Integer) = New List(Of Integer)
            Dim fieldTextListOnScreen As List(Of String) = New List(Of String)
            GetFieldListsOnScreen(fieldTextListOnScreen, fieldIdListOnScreen, l, toolsExport)

            sw.WriteLine(Me.headerRow(l, fieldIdListOnScreen, fieldTextListOnScreen))

            'Me.exportRow(lid, sw, buyeraccount, l, foci, errorMessages, fieldIdListOnScreen, fieldTextListOnScreen)
            'For i As Integer = 0 To descendants.Count - 1 - ML changed due to new screenHeader structure
            Dim s As String = Vw.Sort
            Vw.Sort = "[ID]"

            For Each d In descendants
                If Vw.Find(d.Key.ID) > -1 Or d.Key.Product.isSystem() Then
                    Me.exportRow(lid, d.Value, sw, buyeraccount, l, foci, errorMessages, fieldIdListOnScreen, fieldTextListOnScreen, export)
                End If

                'Dim bid As Integer = CInt(Me.Vw(i).Item("ID")) 
                'Dim branch As clsBranch = iq.Branches(bid)
                'Dim vis As clsVisibility = descendants(branch)

            Next
            Vw.Sort = s

        Catch ex As System.Exception
            errorMessages.Add("*" & ex.Message.ToString & " (Could not create the file ? )" & fn$)
        Finally
            If sw IsNot Nothing Then sw.Close()
        End Try
        Return String.Empty

    End Function

    Public Function headerRow(language As Object, fieldIdListOnScreen As List(Of Integer), fieldTextListOnScreen As List(Of String)) As String

        Dim qc As List(Of String) = New List(Of String)

        For Each field In Me.screen.Fields.Values
            '' Some fields are set to 'Not Displayed' on the screen. This is followed through to exports using fieldIdListOnScreen.
            If (field.visibleList) And (fieldIdListOnScreen.Contains(field.ID)) Then
                qc.Add(Utility.CSV(field.labelText.text(language)))
            End If
        Next

        'For Each id In fieldIdListOnScreen
        '    Dim fld As clsField = iq.Fields(id)
        '    qc.Add(Utility.CSV(fld.labelText.text(language)))
        'Next

        Dim orderedQc As List(Of String) = New List(Of String)

        orderedQc = OrderFields(qc, fieldTextListOnScreen, qc)

        Return Join(orderedQc.ToArray, ",")

    End Function


    Private Function OrderFields(inputs As List(Of String), fieldTextListOnScreen As List(Of String), qc As List(Of String)) As List(Of String)

        Dim outputs As List(Of String) = New List(Of String)

        '' Take the inputs List String and order it so that it matches 
        '' the order of the field text lists on the screen.
        ''
        '' QC is the export list headings
        '' The inputs may also be the export list headings or
        '' actual data in rows.
        For j As Integer = 0 To fieldTextListOnScreen.Count - 1
            For i As Integer = 0 To qc.Count - 1
                If fieldTextListOnScreen(j) = qc(i).Replace("""", "") Then
                    outputs.Add(inputs(i))
                    Exit For
                End If
            Next
        Next
        Return outputs
    End Function

    Private Sub GetFieldListsOnScreen(ByRef fieldTextListOnScreen As List(Of String), ByRef fieldIdListOnScreen As List(Of Integer), language As clsLanguage, toolsExport As Boolean)

        Dim result As List(Of clsField)
        '' This gets fields displayed on the screen.
        If toolsExport Then
            'ExCSV
            result = (From f In iq.i_screens_code("ExCSV").Fields.Values Where f.visibleList = True Order By f.order).ToList()
        Else
            result = FieldResultSet.Where(Function(a) a.Value.Visibility Or iq.Fields.Values.Where(Function(al) If(al.LinkedFieldID IsNot Nothing, al.LinkedFieldID, -1) = a.Key.ID).Count > 0).OrderBy(Function(d) d.Value.Order).Select(Function(a) a.Key).ToList()
            '  result = FieldResultSet.OrderBy(Function(d) d.Value.Order).Select(Function(a) a.Key).ToList()
        End If

        'result =  
        '  Dim listofFields = iq.i_screens_code("ExCSV").Fields.Values.ToList()

        'For Each x In result
        '    Debug.WriteLine(x.displayName(English))
        'Next
        Dim order As Integer = 0

        For Each r As clsField In result
            If r.visibleList = False Then r.visibleList = True

            fieldIdListOnScreen.Add(r.ID)
            fieldTextListOnScreen.Add(r.labelText.text(language))
        Next


    End Sub

    Public Sub exportRow(lid As UInt64, row As clsVisibility, sw As StreamWriter, buyeraccount As clsAccount, language As clsLanguage, _
                         foci As HashSet(Of String), ByRef errorMessages As List(Of String), fieldIdListOnScreen As List(Of Integer), fieldTextListOnScreen As List(Of String), Optional export As Boolean = False)

        Dim qc As List(Of String) = New List(Of String)

        Dim cols As List(Of String) = New List(Of String)
        For Each field In Me.screen.Fields.Values
            '' Some fields are set to 'Not Displayed' on the screen. This is followed through to exports using fieldIdListOnScreen.
            If (field.visibleList) And (fieldIdListOnScreen.Contains(field.ID)) Then

                'If f.visibleList Then
                qc.Add(Utility.CSV(field.labelText.text(language)))

                Dim fv As String = field.CSV(row.branch, row.path, buyeraccount, language, Me, False, foci, errorMessages, lid, 0, export)
                cols.Add(fv)
            End If
        Next

        Dim orderedCols As List(Of String) = New List(Of String)

        orderedCols = OrderFields(cols, fieldTextListOnScreen, qc)

        sw.WriteLine(Join(orderedCols.ToArray, ","))

    End Sub
#End Region


End Class

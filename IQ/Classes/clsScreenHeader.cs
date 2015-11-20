using System.Globalization;
using IQ.clsScreenHeader;
using System.IO;
using System.Threading;



public class clsScreenHeader
{
    public clsScreen screen { get; set; }

    public bool United { get; set; }
    public Dictionary<clsField, Dictionary<clsFilter, List<long>>> Filters = null;
    public Dictionary<int, clsPriorityDirection> sorts; //The priority is the key (makes sense, honest)
    public Dictionary<clsField, enumColState> ColState;
    public System.Data.DataView Vw;
    public bool QuickFiltersVisible;
    private UInt64 lid;
    public string Path;

    private Dictionary<clsField, Dictionary<clsTranslation, int>> dicTrans; //Holds the surviving count (distinct) translations for this Quick filter - populated my addmissingcolumns
    private Dictionary<clsField, List<clsBand>> dicBands; //QuickFilter fields of type BANDS a
    private Dictionary<clsField, Dictionary<long, int>> dicNums; //All the DISTINCT numeric values (and the survivor counts thereof)
    private Dictionary<clsField, clsUnit> dicUnits; //for each (numeric) field detect and validate the UNITS

    private Dictionary<clsBranch, clsVisibility> descendants;

    private Dictionary<clsField, clsAccountScreenField> _FieldResultSet;
    public Dictionary<clsField, clsAccountScreenField> FieldResultSet
    {
        get
		{
			if (_FieldResultSet == null)
			{
				clsPromo pml = null;
				List<string> otherPromo = new List<string>();
                string[] pathLetters = { "K", "I" };
				if (iq.seshDic(lid).ContainsKey("promoinforce") && !pathLetters.Contains(iq.Branches(this.Path.Split('.').Last).rca))
				{
					pml = iq.Promos(iq.sesh(lid, "promoinforce"));
				}
				clsAccount l = iq.sesh(lid, "BuyerAccount");
				
				if (iq.i_PromoRegions.ContainsKey(l.BuyerChannel.Region))
				{
					string t = "";
					foreach (var promo in iq.i_PromoRegions(l.BuyerChannel.Region))
					{
						otherPromo.Add(promo.FieldProperty_Filter);
					}
					
					
				}
				
				if (pml != null)
				{
					otherPromo.Remove(pml.FieldProperty_Filter);
				}
				
				
				List<string> errormessages = new List<string>();
				//TODO add default display unit here
				IEnumerable<clsScreenOverride> asa = iq.ScreenOverrides.Where(so => so.AccountID == l.ID && so.ScreenID == this.screen.ID && so.Path == this.Path).Select(dd => dd);
				System.Boolean screenOverrideObjects = from s in iq.Screens(this.screen.ID).Fields.Values.Where(fld => (pml != null && fld.propertyName == pml.FieldProperty_Filter) || (otherPromo != null && otherPromo.Contains(fld.propertyName)) || fld.ValidRegions.Count == 0 || fld.ValidRegions.Where(vr => vr.Value.Encompasses(l.BuyerChannel.Region)).Count() > 0)
					join a in asa on s.ID equals a.FieldId into g let lr = g.ToArray() from m in lr.DefaultIfEmpty()
					select new {
						a = new clsAccountScreenField {AccountID = l.ID, ScreenID = this.screen.ID, Path = Path, FieldId = s.ID, Visibility = (((pml != null && s.propertyName = pml.FieldProperty_Filter) || (otherPromo != null && otherPromo.Contains(s.propertyName))) ? true : (m == null || m.ForceVisibilityTo == null ? s.visibleList : m.ForceVisibilityTo)), Order = (m == null || m.ForceOrderTo == null ? s.order : m.ForceOrderTo), Width = (m == null || m.ForceWidthTo == null ? (s.width = 0 ? null : s.width) : m.ForceWidthTo), Description = s.labelText, DisplayUnit = (m == null || m.DisplayUnit == null ? null : m.DisplayUnit), PromoColumn = (pml != null && s.propertyName = pml.FieldProperty_Filter ? true : false) },
						b = s};
					
					
					if (AccountHasRight(lid, "GLOBALADM"))
					{
						_FieldResultSet = screenOverrideObjects.ToDictionary(a => a.b, a => a.a);
					}
					else
					{
						_FieldResultSet = screenOverrideObjects.Where(f => f.b.CanUserSelect).ToDictionary(a => a.b, a => a.a);
					}
				}
				return _FieldResultSet;
			}
    }

    public List<clsAccountScreenField> AllAvailableFields
    {
        get
        {
            //Get a list of all fields from the screen and populate with any overrides for this user
            return FieldResultSet.Select(a => a.Value).ToList();
        }
    }
    public List<clsField> EffectiveFields
    {
        get
        {
            //Get a list of all fields from the screen and populate with any overrides for this user
            return FieldResultSet.Where(a => a.Value.Visibility || iq.Fields.Values.Where(al => (al.LinkedFieldID != null ? al.LinkedFieldID : -1) == a.Key.ID).Count() > 0).OrderBy(d => d.Value.Order).Select(a => a.Key).ToList();
        }
    }
    public List<clsField> EffectiveFieldsWithFilters
    {
        get
        {
            //Get a list of all fields from the screen and populate with any overrides for this user
            return FieldResultSet.Where(a => a.Value.Visibility || a.Key.QuickFilterGroup != null || iq.Fields.Values.Where(al => (al.LinkedFieldID != null ? al.LinkedFieldID : -1) == a.Key.ID).Count() > 0).OrderBy(d => d.Value.Order).Select(a => a.Key).ToList();
        }
    }

    public clsScreenHeader(clsBranchInfo bi, Dictionary<clsBranch, clsVisibility> descendants, bool quickFiltersVisible)
    {
        this.QuickFiltersVisible = quickFiltersVisible;
        this.descendants = descendants;
        Dictionary<string, clsScreenHeader> screenHeaders = (Dictionary<string, clsScreenHeader>)(iq.sesh(bi.lid, "screenHeaders"));
        if (bi.branch.ID == 118499)
        {
            int a = 9;
        }
        //Remove the previous header if there was one here, do we want to do this
        if (screenHeaders.ContainsKey(bi.path))
        {
            screenHeaders.Remove(bi.path);
        }
        screenHeaders.Add(bi.path, this);

        this.Path = System.Convert.ToString(bi.path);
        this.lid = bi.lid;
        this.screen = MatrixAbove(lid, this.Path); //NOTE: this is the ONLY place we find out what TYPE of screen definition we need to instance

        if (this.screen == null)
        {
            return;
        }

        this.Filters = new Dictionary<clsField, Dictionary<clsFilter, List<long>>>(); //TODO  default filters
        this.sorts = this.screen.DefaultSorts; //each field has a defaultsort priority and directon - eg 1A 3D
        this.ColState = new Dictionary<clsField, enumColState>();

        this.dicTrans = new Dictionary<clsField, Dictionary<clsTranslation, int>>();
        this.dicBands = new Dictionary<clsField, List<clsBand>>();
        this.dicNums = new Dictionary<clsField, Dictionary<long, int>>();
        this.dicUnits = new Dictionary<clsField, clsUnit>();

        //Get or create the lid's root data table

        //Dim seshDT As DataTable = bi.agentAccount.SellerChannel.AttributeDataTable(bi.buyerAccount.BuyerChannel)
        DataTable seshDT = iq.sesh(lid, "dataTable");
        if (seshDT == null)
        {
            seshDT = new DataTable() { Locale = new CultureInfo(System.Convert.ToString(bi.buyerAccount.Culture != null ? bi.buyerAccount.Culture.Code : "en-us")) };
            iq.seshDic(lid).Add("dataTable", seshDT);
            DataColumn col = default(DataColumn);
            col = new DataColumn("ID", typeof(int));
            seshDT.Columns.Add(col);

        }

        this.Vw = new DataView(seshDT);

        //Populate it with all id's in descendants
        object[] c = new object[1]; //ID column in the data table
        object dv = seshDT.AsDataView();
        dv.Sort = "[ID]";
        object ids = descendants.Values.Where(dic => (dic.hideReasonList.Count == 0 || bi.showAll) && dic.branch.HasSKU).Select(desc => desc.branch.ID).ToList().Except(seshDT.AsEnumerable().Select(tab => System.Convert.ToInt32(TAB(short.Parse("Id")))).ToList()).ToList();

        foreach (var vs in ids)
        {
            seshDT.Rows.Add(vs);
        }


        //For Each vs In descendants.Values
        //    If dv.Find(vs.branch.ID) = -1 AndAlso (vs.hideReasonList.Count = 0 Or bi.showAll = True) AndAlso vs.branch.HasSKU Then
        //        c(0) = vs.branch.ID
        //        seshDT.Rows.Add(c)
        //    End If
        //Next


        if (!iq.seshDic(lid).ContainsKey("pathDataLoaded"))
        {
            iq.seshDic(lid).Add("pathDataLoaded", new List<string>()); //visiblechildren is heavy, dont keep using it, this is NOT perfect and needs more work once this way is proven to be ok... ML
        }
        List<string> pdl = iq.sesh(lid, "pathDataLoaded");

        if (!pdl.Contains(this.Path))
        {
            populateData(descendants, seshDT, bi, lid);
            iq.sesh(lid, "pathDataLoaded").Add(this.Path);
        }
        Vw.Sort = sortsString();
        //If Not bi.agentAccount.SellerChannel.DataPathLoaded(bi.buyerAccount.BuyerChannel, Me.Path) Then
        //    populateData(descendants, seshDT, bi, lid)
        //End If

        int aa = 0;
        this.Vw.RowFilter = this.ConstructFilter(true);
        this.QuickFiltersVisible = quickFiltersVisible;

    }

    public void populateData(Dictionary<clsBranch, clsVisibility> dic, DataTable dt, clsBranchInfo bi, UInt64 lid)
    {

        //ML Note, not looked at scenario when a column is added and rows already exist without this data
        DataColumn col = default(DataColumn);
        DataColumn uncol = default(DataColumn);
        //we shouldnt have to do this - add the missing column to the view/datatable and get it from there


        foreach (var fld in this.EffectiveFieldsWithFilters)
        {
            AddFieldToQFDic(fld); //The QFdics store the distinct values, (and their 'survivor' counts)

            //   Select Case fld.QuickFilterUItype
            //  Case Is = "TKEY", "BANDS", "NUMBANDS", "NUMS", "CHECK"
            if (!dt.Columns.Contains(fld.propertyName))
            {
                if (fld.InputType.code.ToUpper == "STRING" && fld.QuickFilterUItype == "BANDS")
                {

                    col = new DataColumn(fld.propertyName, typeof(string)); //Single))
                    dt.Columns.Add(col);
                    uncol = new DataColumn(fld.propertyName + "UNIT", typeof(short));
                    dt.Columns.Add(uncol);

                }
                else
                {
                    if (fld.propertyName.Contains("DMR_ISS"))
                    {
                        int a = 1;
                    }
                    col = new DataColumn(fld.propertyName, typeof(long)); //Single))
                    dt.Columns.Add(col);
                    uncol = new DataColumn(fld.propertyName + "UNIT", typeof(short));
                    dt.Columns.Add(uncol);
                }
                this.fillDTCol(dt, fld, col, dic, bi.buyerAccount, bi.agentAccount.Language, bi.foci, lid); //also populates dicTrans,dicBands  and dicNums
            }
            else
            {
                //Do we have any more nodes to add to the table
                col = dt.Columns(fld.propertyName);
                Dictionary<clsBranch, clsVisibility> newdic = new Dictionary<clsBranch, clsVisibility>();

                object dv = dt.AsDataView();
                dv.Sort = "[ID]";
                foreach (var c in dic)
                {
                    object d = dv.Find(c.Key.ID);
                    if (d >= 0 && Information.IsDBNull(dv(d)[fld.propertyName]))
                    {
                        newdic.Add(c.Key, c.Value);
                    }
                }
                this.fillDTCol(dt, fld, col, newdic, bi.buyerAccount, bi.agentAccount.Language, bi.foci, lid); //also populates dicTrans,dicBands  and dicNums

            }
            //       Case Is = ""
            //No need to populate
            //         Case Else
            //      AuditLog.Instance.Add(AuditType.Error, "unrecognised filter UI type :" & fld.QuickFilterUItype, "clsScreenHeader", lid)
            //      End Select
        }

        foreach (var fld in this.Filters.Keys)
        {
            AddFieldToQFDic(fld); //The QFdics store the distinct values, (and their 'survivor' counts)

            if (!dt.Columns.Contains(fld.propertyName))
            {
                col = new DataColumn(fld.propertyName, typeof(long)); //Single))

                //    If fld.propertyName.Contains("CP_SVC") Then Stop
                dt.Columns.Add(col);
                uncol = new DataColumn(fld.propertyName + "UNIT", typeof(short));
                dt.Columns.Add(uncol);
                this.fillDTCol(dt, fld, col, dic, bi.buyerAccount, bi.agentAccount.Language, bi.foci, lid);
            }
            else
            {
                col = dt.Columns(fld.propertyName);
                Dictionary<clsBranch, clsVisibility> newdic = new Dictionary<clsBranch, clsVisibility>();
                object dv = dt.AsDataView();
                dv.Sort = "[ID]";
                foreach (var c in dic)
                {
                    object d = dv.Find(c.Key.ID);
                    if (d >= 0 && Information.IsDBNull(dv(dv.Find(c.Key.ID))[fld.propertyName]))
                    {
                        newdic.Add(c.Key, c.Value);
                    }
                }
                this.fillDTCol(dt, fld, col, newdic, bi.buyerAccount, bi.agentAccount.Language, bi.foci, lid); //also populates dicTrans,dicBands  and dicNums
            }
        }

        //do the same for sorts (add a column to the underlying datatable for everything we're sorting on)
        foreach (var so in sorts.Values)
        {
            AddFieldToQFDic(so.column); //The QFdics store the distinct values, (and their 'survivor' counts)

            if (!dt.Columns.Contains(so.column.propertyName)) //Dont add the same column twice (we may already have added it for filtering)
            {
                col = new DataColumn(so.column.propertyName, typeof(long)); //Single))

                dt.Columns.Add(col);
                uncol = new DataColumn(so.column.propertyName + "UNIT", typeof(short));
                dt.Columns.Add(uncol);
                this.fillDTCol(dt, so.column, col, dic, bi.buyerAccount, bi.agentAccount.Language, bi.foci, lid);
            }
            else
            {
                col = dt.Columns(so.column.propertyName);
                Dictionary<clsBranch, clsVisibility> newdic = new Dictionary<clsBranch, clsVisibility>();
                object dv = dt.AsDataView();
                dv.Sort = "[ID]";
                foreach (var c in dic)
                {
                    object d = dv.Find(c.Key.ID);
                    if (d >= 0 && Information.IsDBNull(dv(dv.Find(c.Key.ID))[col.ColumnName]))
                    {
                        newdic.Add(c.Key, c.Value);
                    }
                }
                this.fillDTCol(dt, so.column, col, newdic, bi.buyerAccount, bi.agentAccount.Language, bi.foci, lid); //also populates dicTrans,dicBands  and dicNums
            }
        }
        //  indexData()

    }

    public void RefreshViewFilter()
    {
        Vw.RowFilter = this.ConstructFilter(true);
    }
    public void indexData()
    {
        Vw.RowFilter = this.ConstructFilter(true);
        dicTrans.Clear();
        dicNums.Clear();
        dicBands.Clear();

        foreach (var fld in this.EffectiveFieldsWithFilters)
        {
            //   If fld.propertyName <> "Product.SKU" Then
            this.indexData(fld);
            //End If
        }
    }

    public void indexData(clsField f)
    {


        //AddFieldToQFDic(f)

        List<long> values = new List<long>();
        clsBranch branch = null;

        object so = Vw.Sort;
        object origFilter = this.ConstructFilter(false);
        Vw.RowFilter = origFilter;

        Vw.Sort = "[ID]";

        //ML Testing here, remove if broken

        //Dim bandValues As Dictionary(Of clsField, Dictionary(Of Int64, Int32)) = New Dictionary(Of clsField, Dictionary(Of Long, Integer))()


        object did = descendants.Select(d => d.Key.ID);
        //If f.InputType.code.ToUpper = "STRING" And f.QuickFilterUItype = "BANDS" Then
        //    rows = Vw.Table.AsEnumerable().Where(Function(r) did.Contains(r("id")) AndAlso Not IsDBNull(r(f.propertyName)))
        //    headers = rows.GroupBy(f.propertyName)

        //Else
        System.Data.EnumerableRowCollection<System.Data.DataRow> rows = default(System.Data.EnumerableRowCollection<System.Data.DataRow>);
        if (f.propertyName == "Product.SKU")
        {
            rows = Vw.Table.AsEnumerable().Where(r => did.Contains(r("id")) && !Information.IsDBNull(r(f.propertyName)));
            //  Dim headers = rows.GroupBy(Function(dh) Int64.Parse(dh("id")))

        }
        else
        {
            rows = Vw.Table.AsEnumerable().Where(r => did.Contains(r("id")) && !Information.IsDBNull(r(f.propertyName)) && r(f.propertyName) > long.MinValue);

        }

        object headers = rows.GroupBy(dh => long.Parse(System.Convert.ToString(dh(f.propertyName))));




        if (rows.Count > 0 && !Information.IsDBNull(rows[0][f.propertyName + "UNIT"]) && iq.Units.ContainsKey(System.Convert.ToInt32(rows[0][f.propertyName + "UNIT"])))
        {
            dicUnits[f] = iq.Units(System.Convert.ToInt32(rows[0][f.propertyName + "UNIT"])); // What if multiple units?  need to think about this one
        }

        //Dim tbl As DataTable = rows(0).Table
        //For Each r In rows
        //    tbl.Rows.Add(r)
        //Next


        Dictionary<clsField, Dictionary<clsFilter, System.Collections.Generic.KeyValuePair<List<long>, List<long>>>> CombFilters = new Dictionary<clsField, Dictionary<clsFilter, Generic.KeyValuePair<List<long>, List<long>>>>();
        this.CombinedFilters(true, CombFilters);

        object dfdf = Vw.ToTable.AsEnumerable().Where(r => did.Contains(r("id")) && !Information.IsDBNull(r(f.propertyName)) && r(f.propertyName) > long.MinValue);
        foreach (var filt in CombFilters)
        {
            dfdf = System.Convert.ToBoolean(dfdf.Where(dd =>
            {
                if (filt.Key == f)
                {
                    switch (f.QuickFilterUItype)
                    {
                        case "TKEY":
                        case "NUMS":
                        case "CHECK":
                        case "BOOL":
                            filt.Value(iq.i_Filters_Code("EQ")).Key.Add(dd(filt.Key.propertyName));
                        case "BANDS":
                        case "NUMBANDS":
                            filt.Value(iq.i_Filters_Code("LEGE")).Key.Add(dd(filt.Key.propertyName));
                            filt.Value(iq.i_Filters_Code("LEGE")).Value.Add(dd(filt.Key.propertyName));
                    }
                }

                return filt.Value.Where(fd => fd.Key.compare(dd(filt.Key.propertyName), fd.Value.Key, fd.Value.Value)).Count() > 0;
            }));

        }
        Dictionary<object, int> ds = default(Dictionary<object, int>);
        if (f.propertyName != "Product.SKU")
        {
            ds = dfdf.GroupBy(rw => rw(f.propertyName)).ToDictionary(fa => fa.Key, fa => fa.Count);
        }
        else
        {
            // Dim a As DataTable = dfdf.CopyToDataTable
            // Dim a = dfdf.GroupBy(Function(rw) rw(f.propertyName))
            // Dim b = a.ToList

        }
        int tInt;
        if ((string)f.QuickFilterUItype.ToUpper == "TKEY")
        {
            Dictionary<clsTranslation, int> headerDic = new Dictionary<clsTranslation, int>();

            dicTrans.Add(f, headers.ToDictionary(td => ((int.TryParse(System.Convert.ToString(td.Key), out tInt) && iq.Translations.ContainsKey(td.Key)) ? (iq.Translations(td.Key)) : (iq.AddTranslation("Other", English, "", 0, null, 0, true))), ddh => ((ds.ContainsKey(ddh.Key)) ? (ds[ddh.Key]) : 0)));
        }
        else if ((((string)f.QuickFilterUItype.ToUpper == "NUMS") || ((string)f.QuickFilterUItype.ToUpper == "CHECK")) || ((string)f.QuickFilterUItype.ToUpper == "BOOL"))
        {
            dicNums.Add(f, headers.ToDictionary(td => (f.QuickFilterUItype == "CHECK" ? (td.Key > long.MinValue ? 1 : 0) : (f.QuickFilterUItype == "BOOL" ? (td.Key == 1 ? 1 : 0) : (long.Parse(System.Convert.ToString(td.Key))))), ddh => ((ds.ContainsKey(ddh.Key)) ? (ds[ddh.Key]) : 0)));
        }
        else if (((string)f.QuickFilterUItype.ToUpper == "BANDS") || ((string)f.QuickFilterUItype.ToUpper == "NUMBANDS"))
        {
            if (f.propertyName != "Product.SKU")
            {
                values = dfdf.Select(ff => long.Parse(System.Convert.ToString(ff(f.propertyName)))).ToList;
            }
            else
            {

            }
        }

        if (f.QuickFilterUItype == "BANDS" || f.QuickFilterUItype == "NUMBANDS")
        {
            if (f.propertyName != "Product.SKU" && headers.Count)
            {
                dicBands[f] = MakeBands(headers.SelectMany(fff => Enumerable.Repeat(fff.Key, fff.Count)).ToList, values, System.Convert.ToString(f.QuickFilterUItype));
            }
        }


        //ML End testing

        //For Each row As DataRow In Vw.Table.Rows    'we're working with the entire unfiltered datatable here always) - no view involved
        //    branch = iq.Branches(CType(row("id"), Int32))

        //    If Not descendants.ContainsKey(branch) Then Continue For
        //    If dicUnits.ContainsKey(f) Then 'ML toDO need to replace this
        //        'If UnitId IsNot dicUnits(f) Then 'Ut oh, mismatched (or mixed units in a column)
        //        ' AuditLog.Instance.Add(AuditType.Error, "Mismatched unit " & Unit.Code, "clsScreenHeader", lid)
        //        'End If
        //    Else
        //        If row(f.propertyName & "UNIT") IsNot DBNull.Value Then
        //            Dim UnitID = CInt(row(f.propertyName & "UNIT"))
        //            If iq.Units.ContainsKey(UnitID) Then
        //                dicUnits(f) = iq.Units(UnitID)
        //            End If
        //        End If
        //    End If
        //    If Not IsDBNull(row(f.propertyName)) Then
        //        Dim numericvalue = CDbl(row(f.propertyName))


        //        If f.QuickFilterUItype = "CHECK" And numericvalue > Int64.MinValue Then numericvalue = 1
        //        Select Case f.QuickFilterUItype
        //            Case "TKEY"
        //                Dim tID = CType(row(f.propertyName), Int64)
        //                'Dim tInt As Int32

        //                If Int32.TryParse(tID, tInt) Then
        //                    If iq.Translations.ContainsKey(tID) Then
        //                        Dim r As Regex = New Regex("(\[" & f.propertyName.Replace("(", "\(").Replace(")", "\)").Replace(".", "\.") & "\]=[0-9]+)")
        //                        Dim match = r.Match(origFilter)
        //                        If match.Groups.Count > 1 Then
        //                            Dim fi = origFilter.Replace(match.Groups(1).Value, "").Replace("()", "").Replace("()", "").Replace("( OR ", "(").Replace("( AND ", "(").Trim(" AND ".ToArray).Trim(" OR ".ToArray)
        //                            Vw.RowFilter = fi & If(String.IsNullOrEmpty(fi) AndAlso Not fi.EndsWith("AND "), "", " AND ") & "[" & f.propertyName & "]=" & tID
        //                        End If
        //                        If Not dicTrans(f).ContainsKey(iq.Translations(tID)) Then
        //                            dicTrans(f).Add(iq.Translations(tID), 0)
        //                        End If
        //                        If Vw.Find(row("id")) > -1 Then dicTrans(f)(iq.Translations(tID)) += 1
        //                    Else
        //                        'Add error here...
        //                    End If
        //                End If
        //            Case "Check", "NUMS"
        //                If numericvalue > Int64.MinValue Then
        //                    Dim r As Regex = New Regex("(\[" & f.propertyName.Replace("(", "\(").Replace(")", "\)").Replace(".", "\.") & "\]=[0-9]+)")
        //                    Dim match = r.Match(origFilter)
        //                    If match.Groups.Count > 1 Then
        //                        Dim fi = origFilter.Replace(match.Groups(1).Value, "").Replace("()", "").Replace("()", "").Replace("( OR ", "(").Replace("( AND ", "(").Trim(" AND ".ToArray).Trim(" OR ".ToArray)
        //                        Vw.RowFilter = fi & If(String.IsNullOrEmpty(fi) AndAlso Not fi.EndsWith("AND "), "", " AND ") & "[" & f.propertyName & "]=" & numericvalue
        //                    End If
        //                    'DONT change this - we WANT to add a 1
        //                    If Not dicNums(f).ContainsKey(numericvalue) Then
        //                        dicNums(f).Add(numericvalue, 0)
        //                    End If
        //                    If Vw.Find(row("id")) > -1 Then dicNums(f)(numericvalue) += 1
        //                End If
        //            Case "BANDS", "NUMBANDS"
        //                    If numericvalue > Int64.MinValue Then
        //                    'Vw.RowFilter = origFilter.Replace(f.propertyName, "%") & If(String.IsNullOrEmpty(origFilter), "", " AND ") & "[" & f.propertyName & "]=" & numericvalue
        //                    'Vw.RowFilter = Vw.RowFilter.Replace("AND", "OR")
        //                        values.Add(numericvalue)
        //                    End If
        //        End Select

        //    End If
        //Next

        Vw.Sort = so;
    }

    /// <summary>
    /// Fills the specified column on the datatable with numeric values retrieved for the field F
    /// </summary>
    /// <remarks>Also Populates dicNums,dicTrans and dicBands - used for the QuickFilters</remarks>
    private void fillDTCol(DataTable dt, clsField f, DataColumn col, Dictionary<clsBranch, clsVisibility> dic, clsAccount buyeraccount, clsLanguage language, HashSet<string> foci, UInt64 lid)
    {

        if (dic.Count == 0)
        {
            return;
        }

        //TODO - this needs to fill dictrans etc or use something else based on the view
        clsTranslation translation = null;

        if (dic.Count == 0)
        {
            return;
        }

        long numericvalue = 0;
        List<long> values = new List<long>();
        clsBranch branch = null;

        //index the Visbilities - by branch (for fast access to the  PATHs we'll need

        Dictionary<clsBranch, clsVisibility> vbb = new Dictionary<clsBranch, clsVisibility>();
        foreach (var v in dic.Values)
        {
            if (!vbb.ContainsKey(v.branch))
            {
                vbb.Add(v.branch, v);
            }
        }
        List<string> errorMessages = new List<string>();

        //Dim dv As DataView = dt.AsDataView() - iteraating over a view (instead of the datatable) didn't ahelp
        //dv.Sort = "ID"

        //For Each row As DataRow In dt.Rows     'we're working with the entire unfiltered datatable here always) - no view involved
        for (i = 0; i <= dt.Rows.Count - 1; i++)
        {
            DataRow row = dt.Rows(i);


            int id = System.Convert.ToInt32(row["id"]);
            branch = iq.Branches(id);
            object cellVal = null;
            //The cellvalue returns Numeric Value (if present), or translation.Sortvalue (which IS translation .order from non-zero orders, or an 'alphabetical' sort otherwise
            clsUnit unit = null;
            if (vbb.ContainsKey(branch)) //this check shouldn't be required but JIT carepacks has some issue
            {
                cellVal = f.CellValue(branch, vbb[branch].path, buyeraccount, language, "", null, false, translation, unit, foci, errorMessages, lid);
                if (Information.IsNumeric(cellVal))
                {
                    numericvalue = System.Convert.ToInt64(cellVal);
                }
            }
            else
            {
                continue;
            }

            //This remembers the units for this column - which are needed for the labels in numeric quickFilters
            if (unit != null)
            {
                row.Item(col.ColumnName + "UNIT") = unit.ID;
            }

            if (f.QuickFilterUItype == "TKEY")
            {
                if (translation != null)
                {
                    row[col.ColumnName] = translation.Key;
                }
                else
                {
                    if (numericvalue != long.MinValue && numericvalue != long.MaxValue)
                    {
                        row.Item(col) = iq.AddTranslation(numericvalue.ToString(), English, "fv", 0, null, 0, false).Key;
                    }
                    else
                    {
                        row.Item(col) = numericvalue;
                    }
                }
            }
            else
            {

                //any numeric value on 'check' type fields is treated as a 1
                // If f.QuickFilterUItype = "CHECK" And numericvalue > Int64.MinValue Then numericvalue = 1
                if (numericvalue == 0 && Information.IsNumeric(cellVal) == false)
                {
                    row.Item(col) = cellVal;
                }
                else
                {
                    row.Item(col) = numericvalue;
                }
                //<<<THIS is
                //
                //                If f.QuickFilterUItype = "BANDS" Or f.QuickFilterUItype = "NUMBANDS" Then
                // If numericvalue > Int64.MinValue Then
                // values.Add(numericvalue)
                //End If

                //ElseIf f.QuickFilterUItype = "NUMS" Or f.QuickFilterUItype = "CHECK" Then

                //If numericvalue > Int64.MinValue Then
                //    'DONT change this - we WANT to add a 1
                //    If Not dicNums(f).ContainsKey(numericvalue) Then
                //        dicNums(f).Add(numericvalue, 1)
                //    Else
                //        dicNums(f)(numericvalue) += 1
                //    End If
                //End If
                //End If

            }
        }

        //make a set of bands for the set of values we just retrieved - Each band will have (approximately the same NUMBER of values)
        //If f.QuickFilterUItype = "BANDS" Or f.QuickFilterUItype = "NUMBANDS" Then
        //    If values.Count Then
        //        dicBands(f) = MakeBands(values)
        //    End If
        //End If


    }
    private void AddFieldToQFDic(clsField f)
    {
        if ((string)f.QuickFilterUItype == "TKEY")
        {
            if (!dicTrans.ContainsKey(f))
            {
                dicTrans.Add(f, new Dictionary<clsTranslation, int>());
            }
        }
        else if (((string)f.QuickFilterUItype == "BANDS") || ((string)f.QuickFilterUItype == "NUMBANDS"))
        {
            if (!this.dicBands.ContainsKey(f))
            {
                dicBands.Add(f, new List<clsBand>());
            }
        }
        else if ((((string)f.QuickFilterUItype == "NUMS") || ((string)f.QuickFilterUItype == "CHECK")) || ((string)f.QuickFilterUItype == "BOOL"))
        {

            if (!dicNums.ContainsKey(f))
            {
                dicNums.Add(f, new Dictionary<long, int>()); //counts of distinct VALUES (by field)
            }
            // Case Is = "CHECK"
        }
        else if ((string)f.QuickFilterUItype == "")
        {
        }
        else
        {
            AuditLog.Instance.Add(AuditType.Error, "unknown quickFilterUItype:" + f.QuickFilterUItype, "clsScreenHeader", lid);
        }
    }

    //Builds the bands such that each contains the same number of results - Eg, for 5 bands and 100 results, each band would have 20 results
    //NOTE: - This means that the min and max VALUES of the bands have not particular relationship to the range
    //However - it's more useful to be able to fitler to the 'top 20% of laptops) rather than some arbitray value based bands

    private List<clsBand> MakeBands(List<long> values, List<long> filteredValues, string BandType)
    {
        List<clsBand> returnValue = default(List<clsBand>);

        //build the bands - such that each contains the same number of results

        returnValue = new List<clsBand>();

        int numBands = 5;
        System.Object sortedValues = from j in values orderby j select j;
        long bottom = long.MinValue;
        long top = 0;
        int chunk = System.Convert.ToInt32(values.Count / numBands);
        clsBand band = default(clsBand);


        if (sortedValues.Distinct.Count < 5)
        {

            band = new clsBand(sortedValues.First, sortedValues.Last, values.Count);
            returnValue.Add(band);

        }
        else
        {
            bottom = System.Convert.ToInt64(sortedValues.First);
            int i = 0;
            while (bottom != sortedValues.Last && i < 4)
            {
                i++;
                int skip = (double)i / numBands * sortedValues.Count;

                top = System.Convert.ToInt64(Math.Ceiling((from z in sortedValues.Skip(skip) select z).First));
                band = new clsBand(bottom, top, filteredValues.Where(f => f >= bottom && f <= top).Count());
                if (bottom > top)
                {
                    bottom = sortedValues.Where(sv => sv > top).First;
                    continue;
                }

                if (!returnValue.Contains(band))
                {
                    returnValue.Add(band);
                }
                if (sortedValues.Where(sv => sv > top).Count() == 0)
                {
                    break;
                }
                bottom = sortedValues.Where(sv => sv > top).First;

            }

            //need to make the last band (i think)
            top = System.Convert.ToInt64(sortedValues.Last);
            band = new clsBand(bottom, top, filteredValues.Where(f => f >= bottom && f <= top).Count());
            returnValue.Add(band);

            //Round and overlap the bands
            //For Each band In MakeBands
            //    ' band.Stretch()
            //Next
        }


        return returnValue;
    }


    public string sortsString()
    {

        //return the sorts in a format suitable for the sort propert of a dataview

        string s = "";
        foreach (var v in from j in this.sorts.Values orderby j.Priority select j)
        {
            string sd = "DESC";
            if (v.Direction != "D")
            {
                sd = "";
            }
            s += " [" + v.column.propertyName + "] " + sd + ","; //Note there are some pretty ciritcal spaces in here - mess with it at your peril
        }

        if (s.Length > 0)
        {
            s = s.Substring(0, s.Length - 1);
        }

        return s;

    }
    public void CombinedFilters(bool includeSelf, Dictionary<clsField, Dictionary<clsFilter, System.Collections.Generic.KeyValuePair<List<long>, List<long>>>> combFilters)
    {

        foreach (var f in Filters)
        {
            foreach (var d in f.Value)
            {
                foreach (var g in d.Value)
                {
                    if (!combFilters.ContainsKey(f.Key))
                    {
                        combFilters.Add(f.Key, new Dictionary<clsFilter, System.Collections.Generic.KeyValuePair<List<long>, List<long>>>());
                    }
                    if (!combFilters(f.Key).ContainsKey(d.Key))
                    {
                        combFilters(f.Key).Add(d.Key, new System.Collections.Generic.KeyValuePair<List<long>, List<long>>(new List<long>(), new List<long>()));
                    }
                    if (!combFilters(f.Key)[d.Key].Key.Contains(g))
                    {
                        combFilters(f.Key)[d.Key].Key.Add(g);
                    }
                }
                if (d.Key.Code == "LE" && combFilters(f.Key).ContainsKey(iq.i_Filters_Code("GE")) || d.Key.Code == "GE" && combFilters(f.Key).ContainsKey(iq.i_Filters_Code("LE")))
                {
                    combFilters(f.Key).Add(iq.i_Filters_Code("LEGE"), new System.Collections.Generic.KeyValuePair<List<long>, List<long>>(combFilters(f.Key)[iq.i_Filters_Code("LE")].Key, combFilters(f.Key)[iq.i_Filters_Code("GE")].Key));
                    combFilters(f.Key).Remove(iq.i_Filters_Code("GE"));
                    combFilters(f.Key).Remove(iq.i_Filters_Code("LE"));
                }

            }
        }

    }

    private string ConstructFilter(bool includeSelf)
    {
        string returnValue = "";

        //Turns the current filters dictionary -  into something actually usable by the dataview
        //note, the operator segment generaly contains [filterValue] and [col] placeholders - which is replaced with the value segment and column name
        //thus  738|SW|4
        //becomes
        //[Displayname]=LIKE 'home*' AND [years]=4


        System.Char pth = Path.Substring(0, Path.Length - Path.Split('.').Last.Length - 1); //Get the parent path
        System.Object sh = (Dictionary<string, clsScreenHeader>)(iq.sesh(lid, "screenHeaders")); //Get the parents screenHeader, if it exists
        if (sh.ContainsKey(pth))
        {
            returnValue = System.Convert.ToString(sh[pth].ConstructFilter(true));
        }
        else
        {
            returnValue = ""; //Add the parents filter to this one
        }

        //Add any promo filters (which are forced in)

        if (iq.seshDic(lid).ContainsKey("promoinforce") && this.Path.Split('.').Length <= 4) //no, no, no
        {
            clsPromo pmo = iq.Promos(System.Convert.ToInt32(iq.sesh(lid, "promoinforce")));
            if (this.screen.i_field_property.ContainsKey(pmo.FieldProperty_Filter))
            {
                if (!(returnValue == ""))
                {
                    returnValue += " AND ";
                }
                returnValue += " ([" + pmo.FieldProperty_Filter + "]=" + pmo.FieldProperty_Value + ")";

                //Dim fid = Me.screen.i_field_property(pmo.FieldProperty_Filter)

                //If Not Filters.ContainsKey(fid) Then
                //    .Add(fid, New Dictionary(Of clsFilter, System.Collections.Generic.KeyValuePair(Of List(Of Long), List(Of Long))))
                //End If
                //combFilters(fid).Add(iq.i_Filters_Code("EQ"), New System.Collections.Generic.KeyValuePair(Of List(Of Long), List(Of Long))(New List(Of Long) From {pmo.FieldProperty_Value}, New List(Of Long)))
            }
        }

        if (Filters == null || !includeSelf)
        {
            return returnValue;
        }

        if (returnValue != "")
        {
            returnValue += " AND ";
        }

        foreach (var fld in Filters.Keys)
        {
            foreach (var flt in Filters[fld].Keys)
            {
                //Where we have a string value - it needs to be 'quoted' (form factors)

                object colname = null;

                System.String qp = string.Empty;
                foreach (var filt in this.Filters[fld][flt])
                {
                    qp += "(" + Strings.Replace(System.Convert.ToString(flt.Filter), "[filterValue]", System.Convert.ToString(filt)); //grab the template for this filter criterea and replace the current value
                    //Change for GE LE
                    if (flt.Code == "LE" && Filters[fld].ContainsKey(iq.i_Filters_Code("GE")))
                    {
                        qp += " AND ";
                        qp += Strings.Replace(System.Convert.ToString(iq.i_Filters_Code("GE").Filter), "[filterValue]", System.Convert.ToString(Filters[fld][iq.i_Filters_Code("GE")](this.Filters[fld][flt].IndexOf(filt))));
                    }
                    qp += ") OR ";
                }
                if (!string.IsNullOrEmpty(qp))
                {
                    qp = qp.Substring(0, qp.Length - 4); //Take the last AND off
                }

                colname = "[" + fld.propertyName + "]";
                qp = qp.Replace("[col]", colname);

                if (!string.IsNullOrEmpty(qp))
                {
                    returnValue += "(" + qp + ") AND ";
                }
            }
        }

        if (returnValue != "")
        {
            returnValue = Strings.Left(System.Convert.ToString(returnValue), Strings.Len(returnValue) - 5); //Take the last AND off
        }

        if (returnValue.Trim() == "()")
        {
            returnValue = "";
        }

        return returnValue;
    }


    public void UpdateSorts(clsPriorityDirection NewPriority)
    {

        //gives us a Field, priority and direction to update

        IEnumerable<clsPriorityDirection> j = from v in this.sorts.Values where v.column == NewPriority.column select v;

        if (j.Any) //we're changing an existing sort on this column (de-priortising it) - (by selecting it as a later sort than a pre-existing one)
        {
            this.sorts.Remove(j.First.Priority);
        }

        if (!this.sorts.ContainsKey(NewPriority.Priority))
        {
            this.sorts.Add(NewPriority.Priority, NewPriority);
        }
        else
        {
            this.sorts[NewPriority.Priority] = NewPriority;
        }

        this.reNumberSorts();

        // Me.populateData(descendants, Me.DT, buyeraccount, language, foci, errormessages, lid)


        this.Vw.Sort = this.sortsString();

    }

    private void reNumberSorts()
    {

        //pulling a sort out of the middle of the stack (e.g. delete sort '2 of 3' causes misnumbering

        System.Object j = from s in this.sorts.Values.ToList orderby s.Priority select s;

        this.sorts.Clear();
        int p = 1;
        foreach (var i in j)
        {
            i.Priority = p;
            this.sorts.Add(p, i);
            p++;
        }

    }
    public void RemoveSort(int fldid)
    {

        this.sorts.Remove(fldid);
        this.reNumberSorts();

        this.Vw.Sort = this.sortsString();

    }

    public void removeFilters()
    {

        this.Filters.Clear();
        this.Vw.RowFilter = null;

    }
    public void RemoveFilter(string toRemove, List<string> errormessages)
    {

        //To remove contains the fieldID|FilterCODE|Filtercode|filtercode

        string[] p = toRemove.Split('|');
        clsField fld = iq.Fields(int.Parse(p[0]));
        clsFilter filter = default(clsFilter);

        if (this.Filters.ContainsKey(fld))
        {
            for (i = 1; i <= (p.Length - 1); i++)
            {
                if (iq.i_Filters_Code.ContainsKey(p[i]))
                {
                    filter = iq.i_Filters_Code(p[i]);
                    this.Filters[fld].Remove(filter); //each field can have more than one filter applied simultaeneously
                }
                else
                {
                    errormessages.Add("* Can\'t remove filter with the CODE " + p[1]);
                }
            }
        }
        else
        {
            errormessages.Add("* No filter present for field " + p[0]);
        }

        this.Vw.RowFilter = ConstructFilter(true);

    }

    public void ClearFilter(string filterID)
    {

        clsField filterField = iq.Fields(int.Parse(filterID));

        if (this.Filters.ContainsKey(filterField))
        {
            this.Filters[filterField].Clear();
        }

    }

    public void ClearGroupFilter(string filterID)
    {

        clsField filterField = iq.Fields(int.Parse(filterID));

        // Clear all filters belonging to the same group
        foreach (var f in this.Filters)
        {
            if (f.Key.QuickFilterGroup.Key == filterField.QuickFilterGroup.Key)
            {
                f.Value.Clear();
            }
        }

    }

    public void UpdateFilters(object changefilter)
    {

        //Uses the changefilter parameter which
        //looks like 738|GE|1.02
        //Field ID, Filter Code (operator), New operand (value)

        //have extended to allow - ie, multiple Filter-value pairs on the same field
        //fldID|GE|2000|LE|4000

        string[] p = null;
        p = Strings.Split(System.Convert.ToString(changefilter), "|");

        clsField fld = default(clsField);
        clsFilter flt = default(clsFilter);
        fld = iq.Fields(int.Parse(p[0]));

        for (i = 1; i <= (p.Length - 1) - 1; i += 2)
        {
            flt = iq.i_Filters_Code(p[i]);

            if (!this.Filters.ContainsKey(fld))
            {
                this.Filters.Add(fld, new Dictionary<clsFilter, List<long>>());
            }
            if (this.Filters[fld].ContainsKey(flt))
            {
                if (fld.HMC_MutuallyExclusive || fld.QuickFilterUItype == "BOOL")
                {
                    this.Filters[fld][flt].Clear();
                }
                if (string.IsNullOrEmpty(p[i + 1]))
                {
                    this.Filters[fld][flt].Clear();
                }
                else
                {
                    if (this.Filters[fld][flt].Contains(System.Convert.ToInt64(p[i + 1])))
                    {
                        if (this.Filters[fld][flt].Count() == 1)
                        {
                            this.Filters[fld].Remove(flt);
                        }
                        else
                        {
                            this.Filters[fld][flt].Remove(System.Convert.ToInt64(p[i + 1]));
                        }
                    }
                    else
                    {
                        this.Filters[fld][flt].Add(System.Convert.ToInt64(p[i + 1]));
                    }
                }
            }
            else
            {
                this.Filters[fld].Add(flt, new List<long>());
                this.Filters[fld][flt].Add(System.Convert.ToInt64(p[i + 1]));
            }
        }

        // Me.addMissingColumns(descendants, Me.DT, buyeraccount, language, foci, errormessages, lid) ML TODO READD

        this.Vw.RowFilter = this.ConstructFilter(true);


    }

    public void setColState(clsField fld, enumColState state)
    {
        ColState[fld] = state; //expaned/collapsed (etc)
    }

    public void CollapseColumns(float emsAvailable, ref List<string> errormessages)
    {

        //Dynamically collapses the lowest priority columns in a matrix, based on the available width and which columns have been actively opened or closed

        //Dim ColState As Dictionary(Of clsField, enumColState) = Nothing
        //If iq.SeshContains(lid, "colstate." & path$) Then
        // ColState = iq.sesh(lid, "colstate." & path$)
        // End If

        //re-expand all soft collapsed columns (we will re-collapse as many as necessary  below)
        foreach (var k in ColState.Keys.ToList)
        {
            if (ColState[k] == enumColState.SoftCollapsed)
            {
                ColState[k] = enumColState.SoftExpanded;
            }
        }

        float w = 0;
        w = this.currentWidth(errormessages); // get the current width (all columns minus the hard collapsed ones)

        //Now collapse columns in descending order of priority until the total width is less than the space available
        int pass = 0;
        bool done = false;
        for (pass = 1; pass <= 2; pass++) //on the first pass we collapse the 'soft' columns - on the second.. second hard
        {
            foreach (var fld in from v in this.EffectiveFields orderby v.priority descending select v) //iterate the fields in descending order of their priority - ie. knock out the highest numbers (least important columns) first - Priority '1' is the most important column
            {
                //If fld.visibleList Then
                if (w < emsAvailable)
                {
                    done = true;
                }
                if (done)
                {
                    break;
                }
                if ((ColState[fld] == enumColState.SoftExpanded && pass == 1) || (ColState[fld] == enumColState.HardExpanded && pass == 2))
                {
                    if (fld.priority != 1)
                    {
                        w = w - (FieldResultSet(fld).Width - collapsedColumnWidth); //this is what we 'gain' by collpasing this column
                        this.ColState[fld] = enumColState.SoftCollapsed;
                    }
                }
                // End If
            }
            if (done)
            {
                break;
            }
        }


        //Add some small ones back  (most important first)
        foreach (var fld in from v in this.EffectiveFields orderby v.priority select v)
        {
            // If fld.visibleList Then
            if (ColState[fld] == enumColState.SoftCollapsed)
            {
                if (w + FieldResultSet(fld).Width < emsAvailable)
                {
                    this.ColState[fld] = enumColState.SoftExpanded;
                    w += System.Convert.ToSingle(FieldResultSet(fld).Width);
                }
            }
            // End If
        }

        //Mop up and grow
        object growField = this.EffectiveFields.Where(ef => ef.Grow).FirstOrDefault();
        if (growField != null && emsAvailable > w && this.ColState[growField] != enumColState.SoftCollapsed && (emsAvailable - w) > this.FieldResultSet(growField).Width)
        {
            //growField.width = emsAvailable - w
            this.FieldResultSet(growField).GrownWidth = emsAvailable - w;
            w = emsAvailable;
        }
        else
        {
            if (growField != null)
            {
                this.FieldResultSet(growField).GrownWidth = null;
            }
        }

    }

    public void InvalidateFields()
    {
        _FieldResultSet = null;
    }

    public void SetDefaultFilterOn(ref string filterNotFound)
    {

        //Clear current filters
        Filters.Clear();

        //Fetch defaults
        foreach (var f in EffectiveFields)
        {
            if (f.DefaultFilterValues != null)
            {
                UpdateFilters(string.Format("{0}|{1}", f.ID, f.DefaultFilterValues));
            }
        }
        StringBuilder stringB = new StringBuilder();
        foreach (DataColumn dcol in this.Vw.Table.Columns)
        {

            stringB.Append(dcol.ColumnName + ",");
        }
        //Log4NetMessage(stringB.ToString())
        //stringB = New StringBuilder()
        //For Each dRow As DataRow In Me.Vw.Table.Rows
        //    For Each dcol As DataColumn In Me.Vw.Table.Columns

        //        stringB.Append(dRow(dcol.ColumnName) & ",")
        //    Next
        //    Log4NetMessage(stringB.ToString())
        //    stringB = New StringBuilder()
        //Next

        if (this.Vw.Count == 0)
        {
            List<string> newFilterString = new List<string>();
            string[] filterString = Strings.Split(System.Convert.ToString(this.Vw.RowFilter), "AND");
            for (intloop = 0; intloop <= filterString.Length - 1; intloop++)
            {
                this.Vw.RowFilter = filterString[intloop];
                if (this.Vw.Count > 0)
                {
                    newFilterString.Add(filterString[intloop]);
                }
                else
                {
                    filterNotFound = filterString[intloop];
                    int intstart = filterNotFound.IndexOf("[") + 1;
                    int intend = filterNotFound.IndexOf("]") + 1;
                    string fieldProperty = filterNotFound.Substring(intstart + 1 - 1, intend - intstart - 1);

                    System.Boolean a = from f in EffectiveFields where f.propertyName == fieldProperty select f;
                    if (a.Count == 1)
                    {
                        filterNotFound = System.Convert.ToString(a.First.ID);
                    }

                }
            }
            string updateFilterString = string.Join(" AND ", newFilterString.ToArray());
            this.Vw.RowFilter = updateFilterString;
            //     filterNotFound = updateFilterString

        }

    }


    public bool ColIsCollapsed(clsField fld)
    {
        bool returnValue = false;

        //If Me.ColState IsNot Nothing Then
        if (this.ColState.ContainsKey(fld))
        {
            if (this.ColState[fld] == enumColState.SoftCollapsed || this.ColState[fld] == enumColState.HardCollapsed)
            {
                returnValue = true;
            }
        }
        //End If
        return returnValue;
    }

    private float currentWidth(List<string> errormessages)
    {

        //this is the total width of all the visible columns (taking in to account wether they're collapsed or not)

        float w = 0;
        w = 0;


        foreach (var fld in this.EffectiveFields)
        {
            // If fld.visibleList Then
            if (this.ColState.ContainsKey(fld))
            {
                if (this.ColState[fld] == enumColState.SoftCollapsed)
                {
                    this.ColState[fld] = enumColState.SoftExpanded; //we re-expand any soft collapsed column
                    w = w + FieldResultSet(fld).Width;
                }
                else if (this.ColState[fld] == enumColState.HardCollapsed)
                {
                    w = w + collapsedColumnWidth; //this is a hard collapse (the user explicily collapsed the column)
                }
                else if ((this.ColState[fld] == enumColState.SoftExpanded) || (this.ColState[fld] == enumColState.HardExpanded))
                {
                    w = w + FieldResultSet(fld).Width;
                }
                else
                {
                    errormessages.Add("* Unexpected column state");
                }
            }
            else
            {
                ColState.Add(fld, enumColState.SoftExpanded); //first pass - set all colups to soft expanded
                w = w + FieldResultSet(fld).Width;

            }
            // End If
        }

        return w;


    }

    public bool hasQuickFilters()
    {
        bool returnValue = false;

        returnValue = false;
        foreach (var fld in this.EffectiveFields)
        {
            if (fld.QuickFilterUItype != "")
            {
                returnValue = true;
                break;
            }
        }

        return returnValue;
    }

    /// <summary>
    /// Scans the 'surviving' (filtered) rows - to enable ony radiobuttons/checkboxes with suriving options
    /// </summary>
    /// <remarks></remarks>
    private void EnableQuickFilters(clsBranchInfo bi, List<string> errorMessages) // As List(Of clsField)
    {

        //NOTE uses ME.

        Dictionary<clsBranch, clsVisibility> descendants = new Dictionary<clsBranch, clsVisibility>(); //Of String, clsBranch)
        int pruned = 0;

        //If pbi.RenderChildrenAs = bt.matrixRows Then
        //Get the extended list - of ALL child products (recursing through the placeholder branches) - This isn't ALL descendants - becuase we only recurse down to the next product
        //The key to the dictionary is the (full) path .. the last segment thereof being the branch ID

        clsBranch branch = iq.Branches(System.Convert.ToInt32(this.Path.Split('.').Last));


        long numericvalue = 0; //Object
        clsTranslation translation = null;

        Dictionary<clsFilter, List<long>> holdIt = null;
        foreach (var fld in this.EffectiveFields)
        {

            holdIt = null;
            if (this.Filters != null)
            {
                if (this.Filters.ContainsKey(fld))
                {
                    //Change the filter on the view (for each quickfilter) to drop out this field - so that fields (for example price bands are not 'self-excluding')
                    holdIt = this.Filters[fld];
                    this.Filters.Remove(fld);
                }
            }

            Vw.RowFilter = this.ConstructFilter(true); //(Me.matrix, Me.Filters)

            //ZERO (but do not remove) the counts

            if (((string)fld.QuickFilterUItype == "BANDS") || ((string)fld.QuickFilterUItype == "NUMBANDS"))
            {
                if (!dicBands.ContainsKey(fld))
                {
                    errorMessages.Add("Band was not intialised for " + fld.propertyName + " No values ?");
                }
                else
                {
                    foreach (var band in dicBands[fld])
                    {
                        band.Survivors = 0;
                    }
                }
            }
            else if ((string)fld.QuickFilterUItype == "TKEY")
            {
                // If dicTrans(fld).Keys.Count = 0 Then errorMessages.Add("TKEY dictionary was not initialised for " & fld.propertyName) - this will happen if there's no data in the column
                if (dicTrans.ContainsKey(fld)) //vegas - remove could mask problems
                {
                    foreach (var k in dicTrans[fld].Keys.ToList) //This tolist IS VITAL - as it you iterate over the keys themselves (rather than a copy thereof) you get a 'collection cannot be modified' error
                    {
                        dicTrans[fld][k] = 0;
                    }
                }
            }
            else if ((((string)fld.QuickFilterUItype == "NUMS") || ((string)fld.QuickFilterUItype == "CHECK")) || ((string)fld.QuickFilterUItype == "BOOL"))
            {

                //If dicNums(fld).Keys.Count = 0 Then errorMessages.Add("NUMS dictionary was not initialised for " & fld.propertyName)
                if (dicNums.ContainsKey(fld)) //vegas
                {

                    //dicnums contains, an entry (a count) for every distinct value of a field - for example - for the 'Weight' field - i might contain 1KG (5), 2KG (10)
                    //the Distinct values are the KEYS in the outer dictionary, the VALUES in the dictionary are the COUNT of 'survivors' 'HAVING' that attribute.
                    //in the case of 'check' fields - there is ONLY ONE distinct value ('true')
                    foreach (var k in dicNums[fld].Keys.ToList)
                    {
                        dicNums[fld][k] = 0;
                    }
                }
            }

            //'   If Not DT.Columns.Contains(fld.propertyName) Then
            // errorMessages.Add("Invalid column specified:'" & fld.propertyName & "' (Check cAsE)")
            // Else


            for (i = 0; i <= Vw.Count - 1; i++) //This view is a (filtered) subset of the datatable (which is a cache of all the nuericvalues of all the fields)
            {

                if (fld.QuickFilterUItype != "")
                {

                    if ((Vw.Item(i)[fld.propertyName]) is DBNull)
                    {
                        numericvalue = long.MaxValue;
                    }
                    else
                    {
                        numericvalue = System.Convert.ToInt64(Vw.Item(i)[fld.propertyName]); //.DataView.Item()
                    }


                    // If LCase(fld.propertyName) = "product.i_attributes_code(tch)(0)" Then Stop

                    if (((string)fld.QuickFilterUItype == "CHECK") || ((string)fld.QuickFilterUItype == "BOOL"))
                    {

                        //any NON minvalue (ie, present) value qwill so and is stored under the value 1
                        if (System.Convert.ToInt64(numericvalue) != long.MinValue) //int64.minvalue is the default numeric value and means its 'not there'
                        {

                            if (!dicNums.ContainsKey(fld))
                            {
                                dicNums.Add(fld, new Dictionary<long, int>());
                            }
                            if (!dicNums[fld].ContainsKey(1))
                            {
                                dicNums[fld].Add(1, 0);
                            }
                            dicNums[fld][1] += 1;
                            //            Exit For 'once we've found a single value (in the current filtered view)  - we know this is a valid option - so this gives a big speedup
                        }

                    }
                    else if ((string)fld.QuickFilterUItype == "TKEY")
                    {

                        //this is not very elegant - although it should be fast enough as its a small list (distinct trasnlations within 1 field)
                        bool found = false;
                        if (dicTrans.ContainsKey(fld)) //vegas - remove - could mask problems
                        {
                            foreach (var t in dicTrans[fld].Keys.ToList)
                            {
                                if (t.SortValue(bi.agentAccount.Language) == numericvalue)
                                {
                                    dicTrans[fld][t] += 1;
                                    found = true;
                                    break;
                                }
                            }
                            if (dicTrans[fld].Count() > 0)
                            {
                                // If Not found Then errorMessages.Add("TKEY not found for " & fld.propertyName)
                            }
                        }
                    }
                    else if (((string)fld.QuickFilterUItype == "BANDS") || ((string)fld.QuickFilterUItype == "NUMBANDS"))
                    {

                        if (this.dicBands.ContainsKey(fld))
                        {
                            foreach (var band in this.dicBands[fld]) //this is a list of bands for this field
                            {
                                if (band.contains(numericvalue))
                                {
                                    band.Survivors += 1;
                                }
                            }
                        }

                    }
                    else if ((string)fld.QuickFilterUItype == "NUMS")
                    {
                        Dictionary<long, int> flddic = dicNums[fld];
                        if (flddic.ContainsKey(numericvalue)) //UGLY - deals with missing values - eg a machine with no figure for installed mem
                        {
                            flddic[numericvalue]++; //increment the count of occurances of this NumericValue
                        }
                    }
                    else if ((string)fld.QuickFilterUItype == "")
                    {
                    }
                    else
                    {
                        errorMessages.Add("EnableQuickFilters - Unknown quickFilterUI type" + fld.QuickFilterUItype);
                    }
                }
            }

            //End If

            if (holdIt != null)
            {
                this.Filters.Add(fld, holdIt); //Put back the filter for this column
            }

        }

        bi.survivors = Vw.Count;

    }



    public PlaceHolder UI(clsBranchInfo bi, ref List<string> errormessages, UInt64 lid)
    {
        PlaceHolder returnValue = default(PlaceHolder);

        //Me.Path = bi.path
        // Me.matrix = MatrixAbove(Me.path) 'NOTE: this is the ONLY place we find out what TYPE of matrixHeader we need to instance


        //Returns the user facing UI for filtering and sorting this matirx
        returnValue = new PlaceHolder();
        //If AccountHasRight(lid, "DIAGVIEW") Then UI.Controls.Add(NewLit(Me.screen.ID))

        object b = this.FieldResultSet.ToList().Where(f => f.Key.QuickFilterGroup != null);
        if (this.FieldResultSet.ToList().Where(f => f.Key.QuickFilterGroup != null && f.Value.Visibility && !f.Value.PromoColumn).Count() > 0) // remove blank filters
        {
            if (((enumParadigm)(iq.sesh(lid, "Paradigm"))) == enumParadigm.configuringSystem && !isSystemAbove(lid, Path))
            {
            }
            else
            {
                Panel panelFilter = this.FiltersUI(bi, errormessages);
                if (panelFilter == null)
                {
                    panelFilter = this.FiltersUI(bi, errormessages);
                }
                if (panelFilter != null)
                {
                    returnValue.Controls.Add(panelFilter); //only show filters above or at system level when not in configure mode
                }
            }
        }

        Panel mh = new Panel();
        mh.CssClass = "matrixHeader";
        mh.ID = screen.ID;
        returnValue.Controls.Add(mh); //MH is a div wich we put the diagonal lables (chart control) plus the filter/sort UI - so it all aligns


        // mh.Controls.Add(pnlHeadSquares)

        OutputErrors(mh.Controls, errormessages, lid, true);
        if (clsBranchState.getbranchstate(bi.lid, this.Path) != null)
        {
            if (clsBranchState.getbranchstate(bi.lid, this.Path).rca == enumBt.gridrow)
            {
                clsAccount bAccount = iq.sesh(lid, "BuyerAccount");

                // Add a Help Me Choose button for Microsoft (OS) branches
                if (bi.branch.Translation.text(English).Contains("Microsoft OS"))
                {
                    string sysPath = null;
                    clsBranch sysBranch = bi.branch.FindSystemAbove(bi.path, sysPath);
                    string toPath = null;
                    sysBranch.FindBranchByNameBelow("Operating System", "", true, 12, toPath);
                    if (!string.IsNullOrEmpty(toPath))
                    {
                        toPath = sysPath.Substring(0, sysPath.LastIndexOf(".")) + toPath;
                        if (!string.IsNullOrEmpty(toPath))
                        {
                            mh.Controls.Add(NewLit("<button class=\'hmc hpBlueButton ib showHMC\' onclick=\'getBranches(\"cmd=defFilterOn&path=" + sysPath + "&to=" + toPath + "&into=tree\");return false;\'>" + Xlt("Help Me Choose", bi.buyerAccount.Language) + "</button>&nbsp;&nbsp;"));
                        }
                    }
                }

                if (AccountHasRight(lid, "GLOBALADM"))
                {
                    mh.Controls.Add(NewLit(string.Format("<button class=\"CustomizeColumns hmc hpBlueButton ib showHMC\" onclick=\"$(\'#FieldFilter\').draggable();$(\'#FieldFilter\').show(\'slide right\');$.ajax({{ url: \'../Data/GetAvailableFields\', data: JSON.stringify({{lid:\'{0}\', BranchPath:\'{1}\'}}),type: \'POST\', contentType: \'application/JSON\',success: GetAvailableFields_Success }});return false;\">" + Xlt("Customise Columns", bi.buyerAccount.Language) + "</button>", bi.lid, this.Path)));
                }

                if (bi.branch.Translation.Group == "OL3") //simple screen for OL3, basically a list
                {
                    //mh.Controls.Add(Me.matrix.MatrixHeaders(Me))  'renders the header with diagonal lables
                    mh.Controls.Add(this.RemoveFiltersUI(bi.buyerAccount.Language)); //.MatrixPath, bi.lid, bi.AgentAccount.Language)) ' todo - reinstate
                    return returnValue;
                }

                mh.Controls.Add(this.SortDropDownsUI(bi.agentAccount.Language)); //DropDowns
                mh.Controls.Add(this.screen.MatrixHeaders(this, bi, bi.agentAccount.Language)); //renders the header with diagonal lables
                Literal blit = new Literal();

                mh.Controls.Add(this.ExpandCollapseColumnButtons(bi, bi.agentAccount.Language));
                mh.Controls.Add(this.RemoveFiltersUI(bi.buyerAccount.Language)); //.MatrixPath, bi.lid, bi.AgentAccount.Language)) ' todo - reinstate

                mh.Controls.Add(this.SortDirectionsUI(bi.buyerAccount.Language)); //row of arrows

                blit.Text = "<div style=\'height:1px;clear:both;\'>&nbsp;</div>";
                returnValue.Controls.Add(blit);

                if (bi.survivors > 0)
                {
                    //Number matching (current filters)
                    Label lblmatches = default(Label);
                    lblmatches = new Label();
                    lblmatches.Text = bi.survivors.ToString();
                    lblmatches.Attributes("class") = "matchingLabel";

                    returnValue.Controls.Add(lblmatches); // a refrence to this is returned - so it can be populated later (becuase we render the headers early on - before we have fetched the data, so we don't have a count yet)

                }
            }
        }


        return returnValue;
    }
    private Panel RemoveFiltersUI(clsLanguage language)
    {

        //returns a panel containing *customer facing*  UI for filtering (NOT for editor)

        Panel pnl = new Panel();
        object occ = null;
        Panel col = default(Panel);

        // pnl.Controls.Add(ContrastSpacer) 'make room for the 'contrast' checkboxes

        pnl.CssClass = "oneRow";
        pnl.Attributes("style") = "display:inline-block;";

        pnl.Controls.Add(NewLit("<div class=\'LeftPad\' style=\'width:4.25em;display:inline-block;\'>&nbsp;</div>"));
        //for each column in the matrix we can have a value for each filter - so we can say >5 AND <10
        bool AreSome = false;

        foreach (var fld in from v in this.EffectiveFields select v) //iterate the fields in order of their Order property
        {
            // If fld.visibleList Then

            col = fld.emptyCell(this.ColIsCollapsed(fld), false);
            //   col.CssClass = "removeFilter" 'get rid of the matrixCell class

            pnl.Controls.Add(col);

            //    Dim ib As ImageButton
            if (!(this.Filters == null))
            {
                if (this.Filters.ContainsKey(fld))
                {
                    foreach (var flt in this.Filters[fld].Keys)
                    {
                        //ib = New ImageButton
                        //ib.ImageUrl = "/images/navigation/close.png"

                        //Special case for a greater than and less than
                        if (flt.Code == "LE" && this.Filters[fld].ToList().Where(f => f.Key.Code == "GE").Count() > 0)
                        {
                            continue;
                        }

                        Panel rb = new Panel();
                        rb.CssClass = "removeButton";

                        if (this.Filters[fld][flt].Any)
                        {
                            List<string> filterValues = new List<string>();
                            if (fld.QuickFilterUItype == "TKEY")
                            {
                                //If we're filtering against tranlsations values - we need to be able to display the text
                                IEnumerable<string> wi = null;
                                wi = this.Filters[fld][flt].Select(fi => iq.Translations(fi).text(language));
                                if (!(wi == null) && wi.Count > 0)
                                {
                                    filterValues = wi.ToList;
                                }

                            }
                            else
                            {
                                //otherwise we display the 'raw' 64 bit numbers
                                System.Object wi = Filters[fld][flt].Select(fi => fi.ToString());
                                filterValues = wi.ToList;
                            }

                            rb.ToolTip = Xlt("Remove the filter:- ", language) + fld.labelText.text(language) + " " + flt.DisplayText.text(language) + " ";

                            rb.ToolTip += string.Join(",", filterValues.ToArray);
                        }

                        //rb.ToolTip = Xlt("Remove the filter:- ", language) & fld.labelText.text(language) & " " & flt.DisplayText.text(language) & " " & String.Join(",", If(fld.QuickFilterUItype = "TKEY", Me.Filters(fld)(flt).Select(Function(fi) iq.Translations(fi).text(language)).ToArray, Me.Filters(fld)(flt))) 'ML - todo , add all values to this tip
                        occ = "getBranches(\'path=" + this.Path + "&cmd=removeFilter&filterPath=" + this.Path + "&filterParams=" + Strings.Trim(System.Convert.ToString(fld.ID.ToString())) + "|" + flt.Code + "\');return false;";

                        AreSome = true;
                        rb.Attributes("onclick") = occ;

                        col.Controls.Add(rb);
                        col.Width = new Unit(this.FieldResultSet(fld).GrownWidth, UnitType.Em);
                    }
                }
            }
            //  End If
        }

        // pnl.CssClass = "filtersUI"

        if (AreSome)
        {
            pnl.Attributes("style") += "height:" + collapsedColumnWidth + "em;";
        }

        return pnl;

    }

    public Panel SortDirectionsUI(clsLanguage language)
    {
        Panel returnValue = default(Panel);

        //formerly sortUI - now just provided the direction arrows

        returnValue = new Panel();
        returnValue.ID = "sortDirections." + this.Path;

        //Dim nl As Literal = New Literal
        //nl.Text = "<br/>"
        //SortsUI.Controls.Add(nl)
        returnValue.Controls.Add(NewLit("<div class=\'LeftPad\' style=\'width:4.25em;display:inline-block;\'>&nbsp;</div>"));
        Panel col = default(Panel);

        //SortsUI.Controls.Add(ContrastSpacer)

        returnValue.CssClass = "sortsRow";


        Literal btnReSort = new Literal();

        foreach (var f in from v in this.EffectiveFields select v) //iterate the fields in order of their Order property
        {

            //   If f.visibleList Then
            col = f.emptyCell(this.ColIsCollapsed(f), this.FieldResultSet(f).GrownWidth, false);
            col.ID = Path + ".F" + f.ID;

            IEnumerable<clsPriorityDirection> j = null;
            j = from v in this.sorts.Values where v.column == f select v; //Pull out the sort info pertaining to this column
            if (j.Any)
            {

                clsPriorityDirection pd = j.First;

                //Dim currentValue As String = "-"
                //ptb = f.SortPriorityTextBox(Me.sorts$, currentValue)
                col.Controls.Add(pd.UI(Path, language));

                //occ$ = "sortPath='" & path$ & "';sortFieldID=" & f.ID & ";showSortPriorityPicker('" & col.ID & "','" & currentValue & "');"

                // ptb.Attributes("onfocus") = occ$
                // ptb.ToolTip = "Set the priority and direction of this sorting for this column"
                // col.Controls.Add(ptb)

                if (Strings.LCase(System.Convert.ToString(f.propertyName)) == "stock" || Strings.LCase(System.Convert.ToString(f.propertyName)) == "customerprice")
                {

                    //todo - check if we're actually sorting by either of these !

                    //If InStr(Me.sorts$, f.propertyName) > 0 Then  ' a little crude - but good enough
                    btnReSort = new Literal();
                    btnReSort.Text = "<div ID=|resort." + Path + f.propertyName + "| class=|re-sort| style=|display:none| onmousedown=|getBranches(\'path=" + Path + "&cmd=invalidate\');return false;|" + "> &nbsp;</div>";
                    btnReSort.Text = Strings.Replace(System.Convert.ToString(btnReSort.Text), "|", System.Convert.ToString('\u0022'));

                    //btnReSort.ToolTip = "Click to re-sort based on updated stock/price)"
                    //btnReSort.CssClass = "re-sort"
                    //btnReSort.Attributes("style") = "display:none;"  'initially hidden - shown by js fillPrices()
                    //btnReSort.OnClientClick = occ$
                    //btnReSort.ID = "resort." & path$ & f.propertyName 'the need *an* ID (to be made visible - becuase the JS fethces them by class but show()s them by ID)
                    col.Controls.Add(btnReSort);
                    //End If
                }
            }

            returnValue.Controls.Add(col);
            //    End If
        }
        // SortsUI.Style("clear") = "both"

        return returnValue;
    }


    public Panel SortDropDownsUI(clsLanguage language)
    {

        //returns a set of dropdowns, one for each sort order in force - along with their remove buttons, and a single 'add sort' dropdown (cotaining other available columns to sort by)

        Panel ui = new Panel();
        ui.ID = "sorts." + this.Path;
        ui.CssClass = "sortsDropDowns";

        System.Object sortOrders = from v in this.sorts.Values orderby v.Priority select v;

        foreach (var sortOrder in sortOrders)
        {
            //ui.Controls.Add(NewLit(sortOrder.Priority))
            ui.Controls.Add(this.MakeDDL(sortOrder.column, System.Convert.ToString(sortOrder.Direction.First), System.Convert.ToInt32(sortOrder.Priority), language));
        }

        if (sortOrders.Count < 2)
        {
            ui.Controls.Add(this.MakeDDL(null, "D", System.Convert.ToInt32(this.sorts.Count + 1), language)); //this 'special case' version contains a 'Add another' sort
        }


        return ui;

    }

    /// <summary>
    /// Returns a panel containing the dropdown list and remove button for a single sort order (for this Grid)
    /// </summary>
    private Panel MakeDDL(clsField SelectedColumn, string direction, int priority, clsLanguage language) //sortorder As clsMatrixHeader.clsPriorityDirection) As Panel
    {
        Panel returnValue = default(Panel);

        returnValue = new Panel();

        DropDownList ddl = new DropDownList();
        returnValue.Controls.Add(ddl);
        ddl.ID = "Sort_" + this.Path + System.Convert.ToString(priority); //the DDL needs a unique ID (note, many grides can be present in the made simultaeneously
        returnValue.CssClass = "aSortDropDown";

        if (SelectedColumn == null)
        {
            ListItem i = new ListItem(Xlt("Add a sort", language), "add"); //You can't actually select this, so it's value is unimportant (The onchange will fire on this DDL, adding a new sort)
            ddl.Items.Add(i);
            ddl.SelectedValue = "add";
        }

        foreach (var f in this.EffectiveFields)
        {
            if (f.visibleList)
            {
                if (!ColIsCollapsed(f))
                {
                    ListItem i = new ListItem(f.labelText.text(language), f.ID.ToString() + "," + System.Convert.ToString(priority) + direction);
                    if (f.labelText.text(language) == null || f.labelText.text(language) == "")
                    {
                        continue;
                    }
                    ddl.Items.Add(i);
                    if (f == SelectedColumn)
                    {
                        ddl.SelectedValue = i.Value; //Select the right column in this dropdown (these is one dropdown for each active sort order)
                    }
                }
            }
            //UI.Attributes("onclick") = "getBranches('" & path & "', 'priority=" & Me.column.ID & "," & Me.Priority & "D');" ' + sortFieldID + ',' + v);"
        }

        ddl.Attributes("onchange") = "DisableElementsByClassName(\'FF\',\'" + Path + "\');var ddl=document.getElementById(\'" + ddl.ID + "\');var spd=ddl.options[ddl.selectedIndex].value;getBranches(\'path=" + this.Path + "&cmd=sort&value=\'+spd);";

        if (SelectedColumn != null)
        {
            Panel killButton = new Panel();
            killButton.CssClass = "removeButton";
            Literal lit = new Literal();
            lit.Text = "&nbsp;";
            killButton.Controls.Add(lit);
            killButton.Attributes("onclick") = "DisableElementsByClassName(\'FF\',\'" + Path + "\');getBranches(\'cmd=removeSort&path=" + this.Path + "&priority=" + priority.ToString().Trim() + "\');"; //Removesort works by the priority
            returnValue.Controls.Add(killButton);
        }

        return returnValue;
    }


    /// <summary>
    /// A 'quickfilter' is a set of checkboxes containing the distinct values in one field (column)
    /// The act of rendering the quickfilters UI also (pre) scans the survivors (determined by the filter on the view) to enable/disable the correct options
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
    private Panel FiltersUI(clsBranchInfo bi, List<string> errorMessages)
    {
        Panel returnValue = default(Panel);
        bool FilterDataNotFound = false;

        returnValue = new Panel();
        returnValue.Attributes("class") = "quickFilterGroupHolder";
        Panel ButtonPanel = new Panel();
        ButtonPanel.CssClass = "FilterButtonContainer";

        //Dim filterDisplay As Literal
        //If Not String.IsNullOrEmpty(Me.Vw.RowFilter) Then
        //    filterDisplay = New Literal
        //    filterDisplay.Text = String.Format("<div>Filter: {0}</div><br/>", Me.Vw.RowFilter)
        //End If

        if (this.QuickFiltersVisible)
        {
            Literal lit = new Literal();
            string bid = "hmcb." + bi.path;
            lit.Text = "<div id=|" + bid + "| class=|QF hpBlueButton hideQF| onclick=|DisableElementsByClassName(\'FF\',\'" + Path + "\');getBranches(\'cmd=hideQuickFilters&path=" + bi.path + "\')|>Hide filters</div>".Replace("|", '\u0022');
            ButtonPanel.Controls.Add(lit);
        }
        //If bi.bs IsNot Nothing And Not bi.Branch.HasSKU AndAlso bi.PathLevel <> 1 Then
        if (!bi.branch.HasSKU && bi.PathLevel != 1)
        {
            //this is an 'open','category' branch (eligible for quick filters)

            //this is the 'show filters' button the hide filters is in the matrixheader itself
            if (this.QuickFiltersVisible == false && screen.Fields.ToList().Where(f => f.Value.QuickFilterGroup != null && f.Value.FilterVisible).Count() > 0)
            {

                if (bi.MoreThanXskus(5) && bi.branch.Translation.Group != "OL3" && this.hasQuickFilters()) //need to include parent filters in this!
                {
                    string bid = "hmcb." + bi.path; //just needs a unique DIV id (serves no other purpose)
                    Literal lit = new Literal();
                    lit.Text = "<div id=|" + bid + "| class=|hmc hpBlueButton ib showHMC| onclick=|getBranches(\'cmd=quickFilter&path=" + bi.path + "\');return false|> " + System.Convert.ToString((this.Filters.ToList().Where(f => f.Value.Count() > 0).Count() > 0) ? (Xlt("Filtered", bi.buyerAccount.Language)) : (Xlt("Filter", bi.buyerAccount.Language))) + "</div>".Replace("|", '\u0022'); // ML Removed <div class='clear'></div> as this was adding a huge space in scenaro, add quote click on earlier breadcrumb
                    ButtonPanel.Controls.Add(lit);
                }
            }
            string pth = "";
            if (bi.PathLevel() < 6)
            {
                Dictionary<string, clsScreenHeader> mh = iq.sesh(bi.lid, "screenHeaders");
                if (iq.seshDic(lid).ContainsKey("promoinforce"))
                {
                    object pif = iq.Promos(System.Convert.ToInt32(iq.sesh(lid, "promoinforce")));
                    ButtonPanel.Controls.Add(NewLit("<div class=\"FilterButton\" onclick=\"burstBubble(event);getBranches(\'cmd=removePromoLink\');\" title=\"Promo\"><span>" + pif.displayName(bi.buyerAccount.Language) + "</span></div>"));
                }
                foreach (var seg in Strings.Split(System.Convert.ToString(bi.path), "."))
                {
                    pth += System.Convert.ToString(seg);
                    if (mh.ContainsKey(pth) && mh[pth].Filters != null)
                    {
                        foreach (var f in mh[pth].Filters)
                        {
                            System.Char rf = string.Join("|", f.Value.Select(fil => string.Join("|", fil.Value.Select(fd => f.Key.ID.ToString() + "|" + fil.Key.Code + "|" + fd.ToString()))));
                            List<string> tlate = new List<string>();
                            if (f.Key.InputType.code.ToLower == "translate")
                            {
                                foreach (var va in f.Value.Values)
                                {
                                    tlate.Add(iq.Translations(int.Parse(va(0).ToString())).text(bi.buyerAccount.Language));
                                }
                            }

                            if (f.Value.Count > 0)
                            {
                                ButtonPanel.Controls.Add(NewLit("<div class=\"FilterButton\" title=\"Remove: " + string.Join(",", f.Value.Select(d => d.Key.DisplayText.text(English) + " " + System.Convert.ToString(f.Key.InputType.code.ToLower == "translate" ? (string.Join(",", tlate)) : (string.Join(",", d.Value))))) + "\" onclick=\"" + bi.branch.ButtonScript("cmd=removeFilter&filterParams=" + rf + "&path=" + bi.path + ("&into=" + bi.path + "&filterPath=" + pth)) + "\"><span>" + iq.Branches(pth.Split('.').Last()).DisplayName(English) + " - </span><span>" + f.Key.labelText.text(bi.agentAccount.Language) + "</span></div>"));
                            }
                        } //d.Value.Select(Function(g) iq.Translations(CInt(g)).text(English))
                        // If mh(pth).Vw IsNot Nothing Then
                        // If mh(pth).Vw.RowFilter <> "" Then
                        // FiltersUI.Controls.Add(NewLit("<span class=""FilterButton"" onclick=""" + bi.Branch.ButtonScript("cmd=removeFilters&path=" + bi.path & "&into=" + pth + "&filterPath=" & If(bi.RootPath Is Nothing, bi.path, bi.RootPath)) + """>Rack Mount</span>"))
                        // End If
                    }

                    pth += ".";
                }
                //For Each f In Me.Filters
                // FiltersUI.Controls.Add(NewLit("<span class=""FilterButton"" onclick=""" + bi.Branch.ButtonScript("cmd=removeFilters&path=" + bi.path & "&into=" + bi.path + "&filterPath=" & If(bi.RootPath Is Nothing, bi.path, bi.RootPath)) + """>Rack Mount</span>"))
                // Next
            }
        }
        returnValue.Controls.Add(ButtonPanel);

        if (this.QuickFiltersVisible)
        {
            this.indexData();

            //Dim tKeyDic As Dictionary(Of clsField, List(Of clsTranslation)) = Me.quickfilterTextKeys

            clsFilter EQfilter = iq.i_Filters_Code("EQ");

            Dictionary<Panel, int> FilterControlArray = new Dictionary<Panel, int>();
            Dictionary<string, Panel> dicPanels = new Dictionary<string, Panel>();
            Panel pnl = default(Panel);

            // Look for grouped sets of BOOL fields so we can render No Preference buttons after the last one
            // - could be extended to other field types
            Dictionary<int, int> noPrefGroupings = new Dictionary<int, int>();
            foreach (var fld in this.FieldResultSet)
            {
                if (fld.Key.QuickFilterUItype == "BOOL")
                {
                    object groupId = fld.Key.QuickFilterGroup.Key;
                    if (noPrefGroupings.ContainsKey(groupId))
                    {
                        noPrefGroupings[groupId]++;
                    }
                    else
                    {
                        noPrefGroupings.Add(groupId, 1);
                    }
                }
            }
            Dictionary<int, int> noPrefCounts = new Dictionary<int, int>();
            foreach (var group in noPrefGroupings.Keys)
            {
                noPrefCounts.Add(group, 0);
            }
            //  Dim stringLookup = "SVR SCRHPN STORAGE"
            foreach (var fld in this.FieldResultSet.ToList().Where(ord => ord.Key.QuickFilterGroup != null && ord.Key.FilterVisible && !ord.Value.PromoColumn).OrderBy(ord => ord.Key.QuickFilterGroup.Order).Select(ord => ord.Key))
            {

                bool filterDataFound = false;
                // If fld.QuickFilterGroup IsNot Nothing Then
                //   If Not (fld.propertyName = "CustomerPrice" And stringLookup.Contains(fld.Screen.code.ToUpper())) Then
                if (Filters != null && !Filters.ContainsKey(fld))
                {
                    filterDataFound = true;
                }

                if (!dicPanels.ContainsKey(fld.QuickFilterGroup.text(English)))
                {
                    pnl = new Panel();
                    pnl.CssClass = "quickFilterGroupPanel";
                    dicPanels.Add(fld.QuickFilterGroup.text(English), pnl);
                    //Dim title As Label = New Label
                    //title.Text = fld.QuickFilterGroup.text(bi.AgentAccount.Language)
                    pnl.Controls.Add(NewLit("<span class=\'quickFilterGroupTitle\'>" + fld.QuickFilterGroup.text(bi.agentAccount.Language) + "</span>"));
                    FilterControlArray.Add(pnl, fld.QuickFilterGroup.Order);
                }
                pnl = dicPanels[fld.QuickFilterGroup.text(English)];

                Panel ip = new Panel(); //inner panel (one for each UI element in the group - tend to be arranged in a column)
                pnl.Controls.Add(ip);

                if (((string)fld.QuickFilterUItype == "CHECK") || ((string)fld.QuickFilterUItype == "BOOL")) //the field must evaluate to a productattribute - checking the box will fitler (by productattribute) to those with true (1) values
                {

                    //check the boxes to match the current filters
                    bool? value = null;
                    if (Filters != null)
                    {
                        if (Filters.ContainsKey(fld))
                        {
                            clsFilter eq = iq.i_Filters_Code("EQ"); //EQ is the Equals filter
                            if (Filters[fld].ContainsKey(eq))
                            {
                                if (Filters[fld][eq].Contains(1))
                                {
                                    value = true;
                                }
                                if (Filters[fld][eq].Contains(0))
                                {
                                    value = false;
                                }
                            }
                        }
                    }

                    Literal chkBox = new Literal(); //You *can't* use actual checkboxes - don't try - .NET has a nasty habit of wrapping them in spans - and then attaching any script to the span (not the checkbox) - literals are the answer.
                    string filterClicKHandler = "";
                    filterClicKHandler = "filterfield=" + fld.ID + ";DisableElementsByClassName(\'FF\',\'" + Path + "\');if(this.checked){getBranches(\'cmd=changeFilter&path=" + this.Path + "&filterParams=" + fld.ID + "|EQ|1\')}else{getBranches(\'cmd=changeFilter&path=" + Path + "&filterParams=" + fld.ID + "|EQ|\')};";

                    int count = 0;
                    if (dicNums.ContainsKey(fld))
                    {
                        if (dicNums[fld].ContainsKey(1))
                        {
                            count = System.Convert.ToInt32(dicNums[fld][1]); //1 is the 'true' value - and the key to the count
                        }
                    }

                    // Render the checkbox
                    System.Object disabled = count == 0 ? "disabled " : string.Empty;

                    string @checked = System.Convert.ToString(value.HasValue && value.Value ? "checked" : "");
                    if (@checked == "checked")
                    {
                        filterDataFound = true;
                    }
                    chkBox.Text = string.Format("<input class=\'FF\' type=\'checkbox\' {0} {1} onclick= {2}/>", disabled, @checked, '\u0022' + filterClicKHandler + "\u0022");

                    //The quickfilterUI Interacts with the filters

                    ip.Controls.Add(chkBox);



                    Label lbl = new Label();
                    lbl.Text = fld.labelText.text(bi.agentAccount.Language); //Xlt("Yes", bi.agentAccount.Language) '
                    System.Object dvs = dicNums[fld].Count;
                    lbl.Text += " (" + System.Convert.ToString(count) + ")"; //add the count of True values (non true values are not present)

                    lbl.CssClass = "quickFilterLabel";
                    //If count = 0 Then lbl.CssClass &= " disabled" : lbl.ToolTip = Xlt("This option isn't availble (in combination with your other selections)", bi.buyerAccount.Language)
                    ip.Controls.Add(lbl);


                    // If this is the last in the list of grouped fields, optionally render a No Preferences button for the whole group
                    object groupId = fld.QuickFilterGroup.Key;
                    if (noPrefCounts.ContainsKey(groupId))
                    {
                        noPrefCounts[groupId]++;
                        if (noPrefCounts[groupId] == noPrefGroupings[groupId])
                        {

                            // Add a No Preference button on Help Me Choose screens
                            if ((this.screen.code.StartsWith("hmc") || this.screen.code.StartsWith("optCPK")) && noPrefGroupings[groupId] > 1)
                            {
                                ip.Controls.Add(BuildNoPreferenceButton(fld, bi, true));
                            }

                        }
                    }
                }
                else if (((string)fld.QuickFilterUItype == "BANDS") || ((string)fld.QuickFilterUItype == "NUMBANDS"))
                {
                    //break numerics/prices into bands

                    if (dicBands.ContainsKey(fld))
                    {
                        foreach (var band in (fld.InvertFilterOrder ? (dicBands[fld].OrderByDescending(fb => fb.min)) : (dicBands[fld].OrderBy(fb => fb.min))))
                        {

                            Literal chkBox = new Literal();
                            object occ = null;

                            //NB this ChangeFilter chanegs/remove TWO filters at once
                            occ = "filterfield=" + fld.ID + ";DisableElementsByClassName(\'FF\',\'" + Path + "\');if(this.checked){getBranches(\'cmd=changeFilter&path=" + this.Path + "&filterParams=" + fld.ID.ToString() + "|GE|" + band.min + "|LE|" + band.max + "\')}else{getBranches(\'cmd=changeFilter&path=" + this.Path + "&filterParams=" + fld.ID + "|GE|" + band.min + "|LE|" + band.max + "\')};";

                            bool selected = System.Convert.ToBoolean(band.isSelected(fld, this.Filters)); //Compare to the currently selected GE/LE filters on this field
                            if (selected)
                            {
                                filterDataFound = true;
                            }
                            chkBox.Text = "<input class=\'FF\' type=\'checkbox\' " + System.Convert.ToString(selected ? "checked=\'checked\'" : "") + " " + System.Convert.ToString(band.Survivors == 0 ? "disabled=\'disabled\'" : "") + "  onclick=" + "\u0022" + System.Convert.ToString(occ) + "\u0022" + ">";
                            if (fld.QuickFilterUItype == "NUMBANDS")
                            {
                                //Numeric bands (no currency symbol) and NOT * 100
                                if (band.min == band.max)
                                {
                                    chkBox.Text += System.Convert.ToDecimal(band.min).ToString();
                                }
                                else
                                {
                                    chkBox.Text += System.Convert.ToDecimal(band.min).ToString();
                                    chkBox.Text += " - ";
                                    chkBox.Text += System.Convert.ToDecimal(band.max).ToString();
                                }
                            }
                            else
                            {

                                //normal' PRICE bands
                                chkBox.Text += bi.buyerAccount.Currency.format(Math.Floor(band.min / 100), bi.buyerAccount.Culture.Code, errorMessages, 0);
                                chkBox.Text += " - ";
                                chkBox.Text += bi.buyerAccount.Currency.format(Math.Ceiling(band.max / 100), bi.buyerAccount.Culture.Code, errorMessages, 0);
                            }

                            chkBox.Text += "&nbsp;(" + band.Survivors + ")"; //dicBands(fld)(band)

                            chkBox.Text += "</input><br/>";
                            if (!(band.min == 0 && band.max == 0))
                            {
                                ip.Controls.Add(chkBox);
                            }

                        }

                    }
                    if (ip.Controls.Count == 0)
                    {
                        FilterControlArray.Remove(pnl);
                    }
                    // If Me.Filters.ContainsKey(fld) Then
                    //If Me.Filters(fld).Count > 0 Then
                    //ip.Controls.Add(fld.NoPreferenceRadioButton(path, Me.Filters(fld).Keys.ToList)) 'ML - no longer needed now we have check boxes rather than options
                    // End If
                    //End If
                }
                else if ((string)fld.QuickFilterUItype == "TKEY")
                {
                    //this is a single field containing one of a set of transltions
                    //they currently present as radio buttons - but would be easy enough to switch to a set of checkboxes giving 'OR' functionality

                    string nm = "f_" + fld.ID + "." + bi.path;
                    if (dicTrans.ContainsKey(fld)) //it SHOULD always be there - but if you've put somehting stupid in the screens flds propertyname - it wont be
                    {
                        int optionCount = 0;
                        foreach (var t in dicTrans[fld].Keys.OrderByDescending(tr => tr.Order))
                        {

                            if (t != null)
                            {
                                //Dim sv As Int64 = t.SortValue(bi.agentAccount.Language)
                                Literal rb = new Literal();
                                rb.Text = "<input class=~FF~ type=~checkbox~ name=~" + nm + "~";
                                rb.Text += " onclick=~{DisableElementsByClassName(\'FF\',\'" + Path + "\');getBranches(\'path=" + this.Path + "&cmd=changeFilter&filterParams=" + fld.ID + "|EQ|" + t.Key.ToString() + "\')}~";
                                if (Filters.ContainsKey(fld))
                                {
                                    foreach (var activefilter in Filters[fld])
                                    {
                                        if (activefilter.Key == EQfilter) //The quickFilters do a strict equals
                                        {
                                            if (activefilter.Value.Contains(t.Key))
                                            {
                                                filterDataFound = true;
                                                rb.Text += " CHECKED=~CHECKED~";
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (dicTrans[fld][t] == 0) //There are no survivors
                                {
                                    rb.Text += " disabled=~disabled~";
                                }

                                rb.Text += ">" + t.text(bi.agentAccount.Language) + " (" + dicTrans[fld][t] + ")</input><br/>";
                                rb.Text = rb.Text.Replace("~", '\u0022');

                                ip.Controls.Add(rb);

                                if (optionCount == 0)
                                {

                                }

                                optionCount++;
                            }
                        }

                        // Add a No Preference button on Help Me Choose screens
                        if ((this.screen.code.StartsWith("hmc") || this.screen.code.StartsWith("optCPK")) && optionCount > 0)
                        {
                            ip.Controls.Add(BuildNoPreferenceButton(fld, bi));
                        }
                    }

                    //If Me.Filters.ContainsKey(fld) Then
                    //    If Me.Filters(fld).Count > 0 Then
                    //        'ip.Controls.Add(fld.NoPreferenceRadioButton(path, Me.Filters(fld).Keys.ToList)) ' ML - No longer needed now we have check boxes rather than options
                    //    End If
                    //End If
                }
                else if ((string)fld.QuickFilterUItype == "NUMS")
                {

                    string nm = "f_" + fld.ID + "." + bi.path;
                    if (dicNums.Keys.Count == 0)
                    {
                        errorMessages.Add("DicNums was not populated");
                    }
                    else
                    {
                        if (!dicUnits.ContainsKey(fld))
                        {
                            errorMessages.Add("no units present for values in " + fld.propertyName);
                        }
                        else
                        {
                            //distinct Values of the number
                            foreach (var v in (fld.InvertFilterOrder ? (dicNums[fld].Keys.OrderByDescending(f => f)) : (dicNums[fld].Keys.OrderByDescending(f => f))))
                            {
                                Literal rb = new Literal();
                                rb.Text = "<input class=~FF~ type=~checkbox~ name=~" + nm + "~";
                                rb.Text += " onclick=~{DisableElementsByClassName(\'FF\',\'" + Path + "\');getBranches(\'path=" + this.Path + "&cmd=changeFilter&filterParams=" + fld.ID + "|EQ|" + v.ToString() + "\')}~";
                                if (Filters.ContainsKey(fld))
                                {
                                    foreach (var activefilter in Filters[fld])
                                    {
                                        if (activefilter.Key == EQfilter)
                                        {
                                            if (activefilter.Value.Contains(v))
                                            {

                                                rb.Text += " CHECKED=~CHECKED~";
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (dicNums[fld][v] == 0) //There are no survivors
                                {
                                    rb.Text += " disabled=~disabled~";
                                }

                                rb.Text += ">" + v.ToString() + " " + dicUnits[fld].Symbol + " (" + dicNums[fld][v] + ")</input><br/>";
                                rb.Text = rb.Text.Replace("~", '\u0022');
                                ip.Controls.Add(rb);
                            }

                            if ((this.screen.code.StartsWith("hmc") || this.screen.code.StartsWith("optCPK")) && ip.Controls.Count > 0)
                            {
                                ip.Controls.Add(BuildNoPreferenceButton(fld, bi));
                            }

                            //If Me.Filters.ContainsKey(fld) Then
                            //If Me.Filters(fld).Keys.Count > 0 Then
                            //no preference
                            // ROK - add a No Preference button
                            //ip.Controls.Add(fld.NoPreferenceRadioButton(path, Me.Filters(fld).Keys.ToList)) 'ML - No longer needed now we have check boxes instead of options
                            //End If
                            //End If
                        }
                    }
                }
                else
                {
                    errorMessages.Add("Unrecognised quickFilterUIType:\'" + fld.QuickFilterUItype + "\'");
                }

                if (!filterDataFound)
                {
                    FilterDataNotFound = true;
                    Filters.Remove(fld);
                }
                //  End If
            }

            if (FilterDataNotFound)
            {
                return null;
            }

            foreach (Panel p in FilterControlArray.OrderBy(a => a.Value).Select(a => a.Key))
            {
                returnValue.Controls.Add(p);
            }

            //If Not filterDisplay Is Nothing Then
            //    FiltersUI.Controls.Add(filterDisplay)
            //End If

            //Else
        }





        return returnValue;
    }

    private Literal BuildNoPreferenceButton(clsField field, clsBranchInfo branchInfo, bool groupedField = false)
    {
        Literal returnValue = default(Literal);

        System.Char id = string.Format("clearFilter.{0}", this.Path);
        string command = System.Convert.ToString(groupedField ? "clearGroupFilter" : "clearFilter");
        System.Char clickHandler = string.Format("{{DisableElementsByClassName(\'FF\',\'{0}\');getBranches(\'path={0}&cmd={1}&filterId={2}\');}}", this.Path, command, field.ID);
        object text = Xlt("No preference", branchInfo.agentAccount.Language);

        returnValue = new Literal();
        returnValue.Text = string.Format("<div class=\"hpGreyButton smallfont\" style=\"width:60px\" id=\"{0}\" onclick=\"{1}\">{2}</div><br/>", field.ID, clickHandler, text);

        return returnValue;
    }

    private Panel ExpandCollapseColumnButtons(clsBranchInfo bi, clsLanguage language)
    {

        Panel panel = new Panel();
        panel.ID = "HeaderExpandRow";
        panel.CssClass = "oneRow";
        panel.Attributes("style") = "height:1em;";

        // Dim cols As IEnumerable = (From f In Me.EffectiveFields Order By f.order)

        if (!UserIsAdmin(bi.lid) && (bi.branch.rca.StartsWith("GTB") || bi.branch.rca.Equals("G")))
        {
            // Start further left on the options screens in basic user mode as no expand/collapse control is displayed
            panel.Controls.Add(NewLit("<div class=\'LeftPad\' style=\'width:1.25em;display:inline-block;\'>&nbsp;</div>"));
        }
        else
        {
            panel.Controls.Add(NewLit("<div class=\'LeftPad\' style=\'width:4.25em;display:inline-block;\'>&nbsp;</div>"));
        }

        float x = 0;
        foreach (clsField f in this.EffectiveFields)
        {

            //Dim d As Panel = f.emptyCell(f.isCollapsed(headerPath$, session))
            //img = New Image
            //img.Attributes("style") = "width:1.5em;height:1.5em;"
            //panel.Controls.Add(img)
            //panel.Controls.Add(d)
            // d.Controls.Add(img)

            panel p = new Panel();
            p.Style("display") = "inline-block";
            if (this.ColIsCollapsed(f))
            {
                p.Width = new Unit(collapsedColumnWidth, UnitType.Em);
                p.Controls.Add(MakeRoundButton("expandColumn.png", "Show Column", "getBranches(\'path=" + this.Path + "&cmd=expandColumn&fieldid=" + Strings.Trim(System.Convert.ToString(f.ID.ToString())) + "\');return false;", "", "position:relative;", language, f.ID));
                x = x + collapsedColumnWidth;
            }
            else
            {
                // img.ImageUrl = "/images/navigation/collapseColumn.png"
                // img.Attributes("onmousedown") = "getBranches('" & headerPath & "','collapseColumn=" & Trim$(f.ID) & "');return false;"
                // img.ToolTip = "hide " & f.labelText & " column"



                p.Width = new Unit(FieldResultSet(f).GrownWidth, UnitType.Em);
                if (f.labelText.text(English) != "")
                {
                    p.Controls.Add(MakeRoundButton("collapseColumn.png", "Hide Column", "getBranches(\'path=" + this.Path + "&cmd=collapseColumn&fieldid=" + Strings.Trim(System.Convert.ToString(f.ID.ToString())) + "\');return false;", "", "position:relative;", language, f.ID));
                }
                x = x + FieldResultSet(f).GrownWidth; //in ems
            }
            panel.Controls.Add(p);
        }

        //' this has been moved onto the containg div - matrixHeader.CssClass = "matrixHeader"
        return panel;


    }
    public void regenerateFieldDefs()
    {
        _FieldResultSet = null;
    }

    #region Exports

    /// <summary>Export the grid as CSV - respecting the filters and sorts</summary>
    /// <remarks>Column visibility (collapsedosity) and ordering are not yet respected </remarks>
    public string exportCSV(UInt64 lid, Dictionary<clsBranch, clsVisibility> descendants, clsAccount buyeraccount, clsLanguage l, HashSet<string> foci, ref List<string> errorMessages, bool toolsExport, bool export = false)
    {

        VBMath.Randomize();
        string fn = System.Convert.ToString(System.IO.Path.GetTempFileName);
        fn = System.Convert.ToString(System.IO.Path.ChangeExtension(fn, "csv"));

        iq.sesh(lid, "tostream") = fn;
        iq.sesh(lid, "streamcontent-type") = "text/csv;charset=UTF-8\"  ";
        iq.sesh(lid, "DeleteStreamed") = true;

        StreamWriter sw = null;
        try
        {
            // A 'normal' filestream removed the frist CRLF (on the header row WTF ?)
            sw = new StreamWriter(File.Create(fn), System.Text.Encoding.UTF8);

            //Define separator for excel to understand
            //sw.WriteLine("sep=,")
            //Write the quoted names of the columns (in the agent's languae)
            List<int> fieldIdListOnScreen = new List<int>();
            List<string> fieldTextListOnScreen = new List<string>();
            GetFieldListsOnScreen(fieldTextListOnScreen, fieldIdListOnScreen, l, toolsExport);

            sw.WriteLine(this.headerRow(l, fieldIdListOnScreen, fieldTextListOnScreen));

            //Me.exportRow(lid, sw, buyeraccount, l, foci, errorMessages, fieldIdListOnScreen, fieldTextListOnScreen)
            //For i As Integer = 0 To descendants.Count - 1 - ML changed due to new screenHeader structure
            string s = System.Convert.ToString(Vw.Sort);
            Vw.Sort = "[ID]";

            foreach (var d in descendants)
            {
                if (Vw.Find(d.Key.ID) > -1 || d.Key.Product.isSystem())
                {
                    this.exportRow(lid, d.Value, sw, buyeraccount, l, foci, errorMessages, fieldIdListOnScreen, fieldTextListOnScreen, export);
                }

                //Dim bid As Integer = CInt(Me.Vw(i).Item("ID"))
                //Dim branch As clsBranch = iq.Branches(bid)
                //Dim vis As clsVisibility = descendants(branch)

            }
            Vw.Sort = s;

        }
        catch (System.Exception ex)
        {
            errorMessages.Add("*" + ex.Message.ToString() + " (Could not create the file ? )" + fn);
        }
        finally
        {
            if (sw != null)
            {
                sw.Close();
            }
        }
        return string.Empty;

    }

    public string headerRow(object language, List<int> fieldIdListOnScreen, List<string> fieldTextListOnScreen)
    {

        List<string> qc = new List<string>();

        foreach (var field in this.screen.Fields.Values)
        {
            //' Some fields are set to 'Not Displayed' on the screen. This is followed through to exports using fieldIdListOnScreen.
            if ((field.visibleList) && (fieldIdListOnScreen.Contains(field.ID)))
            {
                qc.Add(Utility.CSV(field.labelText.text(language)));
            }
        }

        //For Each id In fieldIdListOnScreen
        //    Dim fld As clsField = iq.Fields(id)
        //    qc.Add(Utility.CSV(fld.labelText.text(language)))
        //Next

        List<string> orderedQc = new List<string>();

        orderedQc = OrderFields(qc, fieldTextListOnScreen, qc);

        return string.Join(",", orderedQc.ToArray);

    }


    private List<string> OrderFields(List<string> inputs, List<string> fieldTextListOnScreen, List<string> qc)
    {

        List<string> outputs = new List<string>();

        //' Take the inputs List String and order it so that it matches
        //' the order of the field text lists on the screen.
        //'
        //' QC is the export list headings
        //' The inputs may also be the export list headings or
        //' actual data in rows.
        for (int j = 0; j <= fieldTextListOnScreen.Count - 1; j++)
        {
            for (int i = 0; i <= qc.Count - 1; i++)
            {
                if (fieldTextListOnScreen(j) == qc(i).Replace("\"", ""))
                {
                    outputs.Add(inputs(i));
                    break;
                }
            }
        }
        return outputs;
    }

    private void GetFieldListsOnScreen(List<string> fieldTextListOnScreen, List<int> fieldIdListOnScreen, clsLanguage language, bool toolsExport)
    {

        List<clsField> result = default(List<clsField>);
        //' This gets fields displayed on the screen.
        if (toolsExport)
        {
            //ExCSV
            result = (from f in iq.i_screens_code("ExCSV").Fields.Values where f.visibleList == true orderby f.order select f).ToList();
        }
        else
        {
            result = FieldResultSet.Where(a => a.Value.Visibility || iq.Fields.Values.Where(al => (al.LinkedFieldID != null ? al.LinkedFieldID : -1) == a.Key.ID).Count() > 0).OrderBy(d => d.Value.Order).Select(a => a.Key).ToList();
            //  result = FieldResultSet.OrderBy(Function(d) d.Value.Order).Select(Function(a) a.Key).ToList()
        }

        //result =
        //  Dim listofFields = iq.i_screens_code("ExCSV").Fields.Values.ToList()

        //For Each x In result
        //    Debug.WriteLine(x.displayName(English))
        //Next
        int order = 0;

        foreach (clsField r in result)
        {
            if (r.visibleList == false)
            {
                r.visibleList = true;
            }

            fieldIdListOnScreen.Add(r.ID);
            fieldTextListOnScreen.Add(r.labelText.text(language));
        }


    }

    public void exportRow(UInt64 lid, clsVisibility row, StreamWriter sw, clsAccount buyeraccount, clsLanguage language, HashSet<string> foci, List<string> errorMessages, List<int> fieldIdListOnScreen, List<string> fieldTextListOnScreen, bool export = false)
    {

        List<string> qc = new List<string>();

        List<string> cols = new List<string>();
        foreach (var field in this.screen.Fields.Values)
        {
            //' Some fields are set to 'Not Displayed' on the screen. This is followed through to exports using fieldIdListOnScreen.
            if ((field.visibleList) && (fieldIdListOnScreen.Contains(field.ID)))
            {

                //If f.visibleList Then
                qc.Add(Utility.CSV(field.labelText.text(language)));

                string fv = System.Convert.ToString(field.CSV(row.branch, row.path, buyeraccount, language, this, false, foci, errorMessages, lid, 0, export));
                cols.Add(fv);
            }
        }

        List<string> orderedCols = new List<string>();

        orderedCols = OrderFields(cols, fieldTextListOnScreen, qc);

        sw.WriteLine(string.Join(",", orderedCols.ToArray));

    }
    #endregion


}
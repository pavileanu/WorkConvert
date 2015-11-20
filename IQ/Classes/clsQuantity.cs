using dataAccess;


//Quantities define AutoAdds, preferred increments, and pre-installed options (the same thing)

public class clsQuantity : i_Editable
{
    public int ID { get; set; }
    public clsRegion Region { get; set; } //Certain things (carepacks, warranties O/S's are only added in certain countries) - furthermore - certain systems are only available in certain countries
    public string Path { get; set; } //OPTIONAL specific tree node (because of grafts - a branch id is not unique)
    public clsBranch Branch { get; set; }

    public int NumPreInstalled { get; set; } //number of this component fitted by default - in this context
    public int MinIncrement { get; set; }
    public int PreferredIncrement { get; set; }
    public bool FOC { get; set; } //Free of charge (or not)

    public clsBranch oBranch; // holds a reference the the branch this quantity was originally on when it was created (for editing).. such that the quantity can be 'reparented'
    public clsRegion oRegion; //similar to the above..
    public string oPath;
    public bool deleted;

    //   Public i_Quantities As Dictionary(Of clsRegion, Dictionary(Of String, clsBranch))

    public clsQuantity()
    {

    }

    public bool IsAutoAdd
    {
        get
        {
            return NumPreInstalled > 0;
        }
    }
    public clsQuantity clone(object newpath)
    {
        clsQuantity returnValue = default(clsQuantity);

        returnValue = new clsQuantity(this.Region, System.Convert.ToString(newpath), this.Branch, this.NumPreInstalled, this.MinIncrement, this.PreferredIncrement, this.FOC);

        return returnValue;
    }
    public string displayName(clsLanguage language)
    {
        return "rgn:" + this.Region.Code + " path:" + this.Path + " preInst:" + System.Convert.ToString(this.NumPreInstalled) + " minIncr:" + System.Convert.ToString(this.MinIncrement) + " prfIncr:" + System.Convert.ToString(this.PreferredIncrement) + " foc:" + System.Convert.ToString(this.FOC);
    }
    public string XML()
    {

        clsQuantity with_1 = this;
        return string.Format("<quantity id=\'{0}\' region=\'{1}\' path=\'{2}\' number=\'{3}\' minIncr=\'{4}\' prefIncr=\'{5}\' freeOfCharge=\'{6}\'/>",
            with_1.ID, with_1.Region.Code, with_1.Path, with_1.NumPreInstalled, MinIncrement, PreferredIncrement, FOC);

    }

    public dynamic Insert(ref List<string> errorMessages)
    {

        //Return New clsQuantity(Me.Region, Me.Path, Me.Branch, Me.SKUVariant, Me.NumPreInstalled, Me.MinIncrement, Me.PreferredIncrement, Me.FOC)
        return new clsQuantity(this.Region, this.Path, this.Branch, this.NumPreInstalled, this.MinIncrement, this.PreferredIncrement, this.FOC);
    }

    public TableRow adminTableRow(clsBranchInfo bi)
    {

        TableRow tr = default(TableRow);
        TableCell td = default(TableCell);

        bool isPruned = false;
        tr = new TableRow();

        if (!string.IsNullOrWhiteSpace(this.Path))
        {
            if (this.Branch.PruneInForce(this.Path, bi.buyerAccount.SellerChannel) > 0)
            {
                isPruned = true;
            }
        }

        if (isPruned)
        {
            tr.Attributes.Add("style", "text-decoration:line-through;");
        }
        tr.Attributes.Add("title", "the quanity sits on a pruned bracnh in this context - and is not in force");

        if (this.deleted)
        {
            tr.CssClass += " deletedRow";
        }

        td = new TableCell();
        td.Controls.Add(MakeRoundButton("pencil.png", Xlt("Edit this quantity", bi.agentAccount.Language), "window.open(\'edit.aspx?path=Quantities(" + System.Convert.ToString(this.ID) + ")&TreePath=" + bi.path + "&lid=" + bi.lid.ToString() + "\');return(false);", "", "width:25px;height:25px;", bi.buyerAccount.Language));
        tr.Controls.Add(td);


        td = new TableCell();
        tr.Controls.Add(td);
        td.Text = this.Path;
        tr.Controls.Add(td);
        td.ToolTip = Utility.PathName(this.Path);

        td = new TableCell();
        tr.Controls.Add(td);
        td.Text = this.NumPreInstalled.ToString().Trim();
        tr.Controls.Add(td);

        td = new TableCell();
        tr.Controls.Add(td);
        td.Text = this.MinIncrement.ToString().Trim();
        tr.Controls.Add(td);

        td = new TableCell();
        tr.Controls.Add(td);
        td.Text = this.PreferredIncrement.ToString().Trim();
        tr.Controls.Add(td);

        // td = New TableCell
        // td.Text = Me.Branch.Product.DisplayName(bi.buyerAccount.Language) & " (" & Me.Branch.Product.ID & ")"
        // tr.Controls.Add(td)

        td = new TableCell();
        td.Text = this.Branch.Product.ProductType.DisplayName(bi.buyerAccount.Language);
        td.ToolTip = this.Branch.Product.DisplayName(bi.buyerAccount.Language) + " (" + this.Branch.Product.ID + ")";
        tr.Controls.Add(td);

        td = new TableCell();
        td.Text = this.FOC.ToString();
        tr.Controls.Add(td);

        td = new TableCell();
        tr.Controls.Add(td);
        td.Text = this.Region.Code;
        tr.Controls.Add(td);

        td = new TableCell();
        tr.Controls.Add(td);
        if (!string.IsNullOrWhiteSpace(this.Path))
        {
            td.Text = this.Branch.PruneInForce(this.Path, bi.buyerAccount.SellerChannel);
        }
        tr.Controls.Add(td);

        td = new TableCell();
        tr.Controls.Add(td);
        if (this.deleted)
        {
            Literal lt = FunctionButton(bi.path, this.ID, "unDeleteQuantity", "unDEL", "unDelete this quantity");
            td.Controls.Add(lt);

        }
        else
        {

            Literal lt2 = FunctionButton(bi.path, this.ID, "deleteQuantity", "DEL", "Delete this quantity");
            td.Controls.Add(lt2);

        }

        return tr;

    }


    public void update(ref List<string> errorMessages)
    {

        object sql = null;
        sql = "UPDATE [Quantity] SET ";
        sql += " Preinstalled=" + System.Convert.ToString(this.NumPreInstalled);
        sql += ",MinIncrement=" + System.Convert.ToString(this.MinIncrement);
        sql += ",PreferredIncrement=" + System.Convert.ToString(this.PreferredIncrement);
        sql += ",FK_Region_ID=" + this.Region.ID;
        // sql$ &= ",FK_Variant_ID=" & Me.SKUVariant.ID
        sql += ",Foc=" + System.Convert.ToString(this.FOC ? 1 : 0);
        sql += ",deleted =" + System.Convert.ToString(this.deleted ? 1 : 0);

        sql += " WHERE ID= " + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql, false);

        this.oBranch.Quantities.Remove(this.ID);
        this.Branch.Quantities.Add(this.ID, this);
        //Me.oBranch.i_Quantities(Me.oRegion).Remove(Me.oPath)
        //Me.Branch.i_Quantities(Me.Region).Add(Me.Path, Me)

        oBranch = this.Branch;
        oRegion = this.Region;
        oPath = this.Path;

        //Return Me

    }

    public void delete(ref List<string> errorMessages)
    {

        object sql = null;
        sql = "DELETE FROM [Quantity] WHERE ID=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql, false);

        this.oBranch.Quantities.Remove(this.ID);
        //Me.oBranch.i_Quantities(Me.oCountry).Remove(Me.oPath)
        iq.Quantities.Remove(this.ID);

    }
    //Public Function compoundKey() As String
    //    'used to make a lookup in the branches slot sorteddictionary - having them in a sorted dictionary means they can be presented in a sensible order
    //    Return Me.Country.ID & "_" & Me.Path
    //End Function

    //   Public Sub New(region As clsRegion, ByVal Path As String, ByVal Branch As clsBranch, SKUvariant As clsVariant, ByVal numPreInstalled As Integer, ByVal MinIncrement As Integer, ByVal PreferredIncrement As Integer, freeOfCharge As Boolean, Optional Writecache As DataTable = Nothing)
    public clsQuantity(clsRegion region, string Path, clsBranch branch, int numPreInstalled, int MinIncrement, int PreferredIncrement, bool freeOfCharge, DataTable Writecache = null)
    {

        //Note:- Quanitites do not have a clsVariant - the 'best' variant to autoadd/preinstall is determined from the system, warehouse and localisation

        // If numPreInstalled > 0 And FOC = False Then Stop 'this shold be a carepack or OS.. but not an FIO

        this.Path = Path;
        this.Branch = branch; // iq.Branches(Split(Path, ".").Last)

        this.NumPreInstalled = numPreInstalled;
        this.Region = region;
        this.FOC = freeOfCharge;

        if (numPreInstalled < 0)
        {
            Debugger.Break();
        }

        this.MinIncrement = MinIncrement;
        this.PreferredIncrement = PreferredIncrement;

        // If Branch.i_Quantities Is Nothing Then Branch.i_Quantities = New Dictionary(Of clsRegion, Dictionary(Of String, clsQuantity))

        //        DeDupeCountryPath()

        if (Writecache == null)
        {
            object sql = null;
            sql = "INSERT INTO [Quantity] (fk_branch_id,fk_region_id,path,Preinstalled,MinIncrement,PreferredIncrement,foc) ";
            sql += "VALUES (" + Branch.ID + "," + Region.ID + "," + da.SqlEncode(Path) + "," + System.Convert.ToString(numPreInstalled) + "," + System.Convert.ToString(MinIncrement) + "," + System.Convert.ToString(PreferredIncrement) + "," + System.Convert.ToString(this.FOC ? "1" : "0") + ");";

            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));

            if (Branch.Quantities == null)
            {
                Branch.Quantities = new Dictionary<int, clsQuantity>();
            }
            if (!Branch.Quantities.ContainsKey(this.ID))
            {
                Branch.Quantities.Add(this.ID, this);
            }
            iq.Quantities.Add(this.ID, this);

        }
        else
        {

            System.Data.DataRow row = default(System.Data.DataRow);
            row = Writecache.NewRow();
            row["FK_Branch_ID"] = Branch.ID;
            row["FK_Region_id"] = Region.ID;
            row["Path"] = Path;
            row["Preinstalled"] = numPreInstalled;
            row["Minincrement"] = MinIncrement;
            row["PreferredIncrement"] = PreferredIncrement;
            row["FOC"] = FOC ? 1 : 0; //free of charge
            row["deleted"] = 0;


            Writecache.Rows.Add(row);

        }

        //    If Not branch.i_Quantities.ContainsKey(region) Then branch.i_Quantities.Add(region, New Dictionary(Of String, clsQuantity))
        //If Not Branch.i_Quantities.ContainsKey(country) Then Branch.i_Quantities.Add(country, New Dictionary(Of String, clsQuantity))
        //Branch.i_Quantities(country).Add(Path, Me)


        oBranch = branch;
        oRegion = region;
        oPath = Path;

    }

    //Public Sub DeDupeCountryPath()

    //    'Tweeks the ME variables to make sure we're not inserting a duplicate
    //    'If Not Me.Branch.i_Quantities.ContainsKey(Me.Country) Then Exit Sub ' no conflict

    //    Dim dic As Dictionary(Of String, clsQuantity)
    //    dic = Me.Branch.i_Quantities(Me.Country) 'get the dictionary of paths>quanities (by country)
    //    If Not dic.ContainsKey(Me.Path) Then Exit Sub

    //    If Me.Path <> "" Then Me.Path = "" 'a local, specific quantity already existed - try a global
    //    If Not dic.ContainsKey(Me.Path) Then Exit Sub ' resolved.. we'll use a global

    //    For Each C In iq.Countries.Values
    //        If Not Me.Branch.i_Quantities.ContainsKey(C) Then Me.Country = C : Exit Sub 'resolved (using a different country)
    //    Next

    //End Sub

    //Public Sub New(ByVal id As Integer, Region As clsRegion, ByVal Path As String, ByVal Branch As clsBranch, skuvariant As clsVariant, ByVal numPreinstalled As Integer, ByVal MinIncrement As Integer, ByVal PreferredIncrement As Integer, freeOfCharge As Boolean)
    public clsQuantity(int id, clsRegion Region, string Path, clsBranch Branch, int numPreinstalled, int MinIncrement, int PreferredIncrement, bool freeOfCharge)
    {

        this.ID = id;
        this.Region = Region;
        this.Path = Path;
        this.Branch = Branch;
        // Me.SKUVariant = skuvariant
        this.NumPreInstalled = numPreinstalled;
        this.MinIncrement = MinIncrement;
        this.PreferredIncrement = PreferredIncrement;
        this.FOC = freeOfCharge;

        //iq.Quantities.Add(Path, Me)
        //If Branch.i_Quantities Is Nothing Then Branch.i_Quantities = New Dictionary(Of clsRegion, Dictionary(Of String, clsQuantity))
        //If Not Branch.i_Quantities.ContainsKey(country) Then Branch.i_Quantities.Add(country, New Dictionary(Of String, clsQuantity))

        //If Not Branch.i_Quantities(country).ContainsKey(Path) Then  'Remove this IF - i jsut had some screwy data
        // Branch.i_Quantities(country).Add(Path, Me)
        // End If

        if (Branch.Quantities == null)
        {
            Branch.Quantities = new Dictionary<int, clsQuantity>();
        }
        //If Not Branch.Quantities.ContainsKey(Me.ID) Then Branch.Quantities.Add(Me.ID, Me)
        Branch.Quantities.Add(this.ID, this);
        iq.Quantities.Add(this.ID, this);

        oBranch = Branch;
        oRegion = Region;
        oPath = Path;

    }

}
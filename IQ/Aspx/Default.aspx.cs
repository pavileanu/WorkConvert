using System.Text;
using dataAccess;
using System.IO;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Data.OleDb;


public class _Default : System.Web.UI.Page
{


	private void  // ERROR: Handles clauses are not supported in C#
Page_Init(object sender, System.EventArgs e)
	{
	}

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		UInt64 lid = 0;
		UInt64.TryParse(Request.QueryString("lid"), lid);
		if (Request.QueryString("lid") == null || iq.SeshAlive(lid) == false || !AccountHasRight(Request.QueryString("lid"), "DEFPAGE")) {
			if (LCase(Request("Key")) != "m4ster") {
				Response.Redirect("signin.aspx");
			}
		}


		//If Request("Key") <> "M4Ster" Then

		//End If
		if (!clsIQ.IsLoaded)
			return;
		//Yield to the masterpage
		// If iq Is Nothing Then

		//     HttpContext.Current.Session.Timeout = 525600  'The First/Master session (which holds the IQ object) will last for 1 year (which is the maximum permissable)

		//iq = New clsIQ

		//Dim errormessages As List(Of String) = New List(Of String)
		//   Panel1.Controls.Add(iq.load(errormessages))  'This loads the entire object model from the database and returns the status text/timings

		//    Panel1.Controls.Add(OutputErrors(errormessages, 0, True))

		//This is obsolete - we no longer 'self host the service' it's served by/from IIS - see \services\PnA.svc
		//StockWebservice = StartWebservice()  'returns a reference to it (to keep it in scope!)
		//Dim mylit As Literal
		//mylit = New Literal
		//With StockWebservice
		//    mylit.Text = "<p>Stock and price Webservice Started on " & .BaseAddresses(0).AbsoluteUri & " port " & .BaseAddresses(0).Port.ToString & " state is:" & .State.ToString & "</p>"
		//End With
		//Panel1.Controls.Add(mylit)

		//   End If

		if (iq.Screens.Count) {
			Button btnEdit = new Button();
			btnEdit.Text = "Edit Channels";
			object occ = "embed('/editor/editor.aspx?path=Channels&panelID=ctl00_MainContent_EditPanel','ctl00_MainContent_EditPanel',false,false);return false;";
			btnEdit.OnClientClick = occ;
			EditPanel.Controls.Add(btnEdit);
		}

		LblProducts.Text = iq.Products.Count();

		// FillTree()
		// Upd.Update()

		//Dim dt As DataTable = getTranslationGroups()

		//For Each row As DataRow In dt.Rows
		//    Dim lst As ListItem = New ListItem()
		//    If Trim(row(0)) = "" Then
		//        lst.Text = " Blank (" & row(1) & ")"
		//        lst.Value = ""
		//    Else
		//        lst.Text = row(0) & "(" & row(1) & ")"
		//        lst.Value = row(0)
		//    End If

		//    dropGroup.Items.Add(lst)
		//Next
	}


	//Private Sub FillTree()

	//    'Upd.Controls.Clear()
	//    Dim top As New Panel

	//    'To open and close branches without refreshing the page, we need to know which branch was clicked *before* its event fires
	//    'Its even cannot fire unless it has already been added to the control hierarchy - but a branch that is already open and has had its
	//    'collapse' button pressed - we would load the open children (and their descendants)... (and then be left with the problem of removing them)
	//    'by 'pre-emptively' knowing which expand/collapse button was pressed to submit the form - we can toggle that branch whilst filling the tree

	//    'Dim ClickedBranch As String
	//    'ClickedBranch = Request.Form("__EVENTTARGET")
	//    'Dim p() = Split(ClickedBranch, "$")
	//    Dim ToggleBranch As String
	//    ToggleBranch = ""
	//    'If UBound(p) Then
	//    '    ToggleBranch = Replace(p(UBound(p)), "Exp", "")
	//    'End If

	//    Upd.ContentTemplateContainer.Controls.Add(top)
	//    'recursively add all open branches from the root downwards - closing(or opening) togglebranch
	//    AddBranchTotree(iq.Root, top, 1, ToggleBranch, "") ', Upd.Triggers)

	//End Sub

	private string Removeitem(@in, r)
	{

		//Removes the item R$ from the comma delimited list in$
		//returns the shortened list

		object ret = "";
		string[] j;
		j = Split(@in, ",");

		foreach ( i in j) {
			if (i != r)
				ret += i + ",";
		}

		return ret;

	}


	private object LastSegment(string Path)
	{
		string[] b;
		b = Split(Path, ".");
		LastSegment = b(UBound(b));

	}

	//Private Sub FixVariants(HostID As String)

	//    'delete all variants, prices and stock
	//    'for each HostMfrPartnum in h3.iq.products.pricelistmaster - make a new variant

	//    Dim Sql$ = "SELECT MfrPartnum,hostmfrpartnum FROM h3.iq.products.pricelistmaster "
	//    Sql$ &= " WHERE hostID='" & HostID & "' and priceBand is null"

	//    Dim dic As Dictionary(Of String, String) = da.BuildDic(Sql$, "hostmfrpartnum", "mfrpartnum")

	//    Dim channel As clsChannel = iq.i_channel_code(HostID)
	//    Dim v As clsVariant
	//    For Each k In dic.Keys
	//        If iq.i_SKU.ContainsKey(dic(k)) Then
	//            v = New clsVariant("", iq.i_SKU(dic(k)), channel, k, "", "", "", Nothing)
	//        End If
	//    Next

	//End Sub




	protected override void Finalize()
	{
		base.Finalize();
	}


	public _Default()
	{
	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnImport_Click(object sender, EventArgs e)
	{

		Logit("Starting import", true, false);

		Import.everything();



	}




	//                       MEM            mem-pc2-600         dl360
	// optTypeCheck(rdr.Item("opttype"), rdr.Item("optfamily"), family) Then

	public bool OptTypeCheck(string category, string Optfamily, string Sysfamily, Dictionary<string, Dictionary<string, string>> dicSysFamDefs)
	{

		//check that this options optfamily value matches the sysfamilydefinition(category)

		//category is MEM,HDD,OPT,FAN etc.
		//type is mem-pc2-600
		//family is .. the family

		if (Optfamily == dicSysFamDefs(Sysfamily)(Optfamily))
			OptTypeCheck = true;
		else
			OptTypeCheck = false;

	}


	private void  // ERROR: Handles clauses are not supported in C#
BtnSerialize_Click(object sender, System.EventArgs e)
	{
		StreamWriter sw = new StreamWriter("c:\\temp\\iq.txt");
		Reflection.Serialize(iq, sw, 1);

		sw.Close();

	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnLogin_Click(object sender, EventArgs e)
	{

	}



	//'make a product for each type of primary storage (bay eg. 5.25 SFF hot swapppable)

	//    rdr = da.dbexecuteReader(con, "select distinct familypristor from iq.products.sysfamilydefinitions")
	//'Make a product to fit on the branch that will group them
	//Dim objProduct_PriStoreTypes As New clsProduct
	//Dim j As New clsProductAttribute(objProduct_PriStoreTypes, iq.Attributes("Name"), 0, iq.Units("txt"), New clsText(iq.AddText("Primary storage types", s_lang)))
	//    j = Nothing

	//' make the placeholder branch (grouping) branch for all types of primary storage (bay/Form Factor)
	//Dim objBranch_PriStoreTypes As New clsBranch(objProduct_PriStoreTypes, Nothing)

	//Dim atype As clsProduct
	//Dim dicBranches_PriStore As New Dictionary(Of String, clsBranch)

	//    While rdr.Read
	//        If Not IsDBNull(rdr.Item("familyPristor")) Then
	//            atype = New clsProduct(CStr(rdr.Item("Familypristor")), False) 'just making one will add it to the OM's Prodcuts Collection
	//            dicBranches_PriStore.Add(Trim$(rdr.Item("familypristor")), New clsBranch(atype, objBranch_PriStoreTypes))
	//        End If
	//    End While

	//    rdr.Close()

	//'find all the options that are of the known primary storage types  (eg. 5.25 SFF hot swapppable)
	//'.. make a product for each, add it to that PrimaryStorage type branch branch

	//    rdr = da.dbexecuteReader(con, "SELECT optfamily,optSKU,ccdescription FROM iq.products.options_svr AS o JOIN iq.products.hierarchyIQ AS h ON o.optSKU=h.upcnum; ")
	//Dim objProduct_Drive As clsProduct

	//Dim dicBranch_drives As New Dictionary(Of String, clsBranch) 'SKU > branch
	//Dim sku As clsProductAttribute

	//Dim objAttribute_SKU As clsAttribute
	//    objAttribute_SKU = iq.Attributes("MfrSKU")

	//Dim drives As Integer

	//    While rdr.Read

	//        If dicBranches_PriStore.ContainsKey(Trim$(rdr.Item("optfamily"))) Then

	//            objProduct_Drive = New clsProduct 'just making one will add it to the OM's Products Collection
	//            sku = New clsProductAttribute(objProduct_Drive, objAttribute_SKU, 0, iq.Units("txt"), New clsText(iq.AddText(rdr.Item("optsku"), s_lang)))

	//Dim drivedesc As String
	//            drivedesc = rdr.Item("ccdescription")
	//            j = New clsProductAttribute(objProduct_Drive, iq.Attributes("Desc"), 0, iq.Units("txt"), New clsText(iq.AddText(drivedesc, s_lang)))

	//            dicBranch_drives.Add(Trim$(rdr.Item("OptSku")), New clsBranch(objProduct_Drive, dicBranches_PriStore(Trim$(rdr.Item("optfamily")))))

	//'If dicBranches_PriStore(rdr.Item("optfamily")).Branches.Count > 1 Then
	//' Stop
	//' End If

	//            drives += 1
	//        End If
	//    End While

	//    rdr.Close()

	//'now fetch descriptions for all the drives (from ProductHeirachy.. based on the OptSKU's in the drives dictionary

	//'For every System.. graft on (a reference to) the right branch of primary storage based on its family's FamilyPriStor

	//    sql$ = "select d.FamilyPriStor,s.modelSKU from iq.products.systems_svr s join iq.products.sysfamilydefinitions d "
	//    sql$ &= "on s.familycode = d.sysfamily" ' where s.systype='svr'"

	//    rdr = da.dbexecuteReader(con, sql$)

	//    While rdr.Read
	//        If Not IsDBNull(rdr.Item("familypristor")) Then
	//            If dicSystems.ContainsKey(Trim$(rdr.Item("modelsku"))) Then
	//'abranch = New clsBranch(dicBranches_PriStore(rdr.Item("FamilyPriStor")).Product, systems(rdr.Item("modelsku")))
	//                iq.Graft(dicBranches_PriStore(Trim$(rdr.Item("FamilyPriStor"))), dicSystems(Trim$(rdr.Item("modelsku"))))
	//            Else
	//                Stop
	//            End If
	//        End If
	//    End While

	//    rdr.Close()


	//Protected Sub BtnImportSlots_Click(sender As Object, e As EventArgs) Handles BtnImportSlots.Click

	//    'Create an account for techdata
	//    'FROM is a LINQ query

	//    'http://www.experts-exchange.com/Software/Server_Software/Application_Servers/.Net/Q_24706814.html

	//    Dim role As clsRole = (From r In iq.Roles.Values Where r.Code = "admin").First
	//    Dim lang As clsLanguage = (From l In iq.Languages.Values Where l.Code = "EN").First
	//    Dim channel As clsChannel = (From j In iq.Channels.Values Where j.Name.Contains("Computer 2000")).First
	//    Dim currency As clsCurrency = (From c In iq.Currencies.Values Where c.Code = "GBP").First
	//    Dim team As clsTeam = Nothing
	//    Dim User As clsUser = iq.i_user_email("nickax@gmail.com")
	//    Dim password As String = Shuffle(md5("password"))

	//    Beep()
	//    'make the new account
	//    'Dim ac As New clsAccount(User, password, channel, role, team, lang, currency)

	//End Sub



	protected void  // ERROR: Handles clauses are not supported in C#
BtnImportListPrices_Click(object sender, EventArgs e)
	{
		//Imports/updates 

		SqlClient.SqlConnection con;
		con = da.OpenDatabase();
		object l;
		l = Import.listprices(con, LPCountry.Text);
		con.Close();
		con.Dispose();

		Form.Controls.Add(NewLit(l));
		Form.Controls.Add(ErrorDymo(LPCountry.Text + " ONLY!"));
		Form.Controls.Add(NewLit("<p>see c:\\temp\\import.log for details</p>"));

		//Dim MyConnection As System.Data.OleDb.OleDbConnection
		//Dim DtSet As System.Data.DataSet
		//Dim MyCommand As System.Data.OleDb.OleDbDataAdapter
		//MyConnection = New System.Data.OleDb.OleDbConnection("provider=Microsoft.Jet.OLEDB.4.0;Data Source='c:\temp\hplistprices.xlsx';Extended Properties=Excel 8.0;")

		//MyConnection.Open()

		//MyCommand = New System.Data.OleDb.OleDbDataAdapter("select * from [HPSD$]", MyConnection)
		//    'MyCommand.TableMappings.Add("Table", "Net-informations.com")
		//    DtSet = New System.Data.DataSet
		//Try
		//    MyCommand.SelectCommand.Connection = MyConnection
		//    MyCommand.Fill(DtSet)
		//Catch ex As System.Exception

		//    Beep()

		//End Try

		//    Dim sheetreader As System.Data.DataTableReader
		//    sheetreader = DtSet.CreateDataReader()

		//Dim ob() As Object = Nothing
		//ReDim ob(35)

		//sheetreader.Read() 'Countries (from column 9 - FR)

		//sheetreader.Read() 'currencies

		//'Conversions for HP's dodgy internal currency codes
		//Dim dichpcurr As Dictionary(Of String, String)
		//dichpcurr = New Dictionary(Of String, String)
		//dichpcurr.Add("EC", "EUR")
		//dicHpcurr.add("BP", "GBP")
		//dicHpcurr.add("SK", "SEK")
		//dicHpcurr.add("NK", "NOK")
		//dicHpcurr.add("DK", "DKK")
		//dicHpcurr.add("SF", "SFR")
		//dicHpcurr.add("PZ", "PLN")
		//dicHpcurr.add("CK", "CZK")
		//'dicHpcurr.add "HF","CZK"  'Hungarian forint - not implimented
		//dicHpcurr.add("RR", "RUB")
		//dicHpcurr.add("RD", "SAR") 'rand

		//sheetreader.Read() 'Countries (from column 9 - FR)
		//sheetreader.Read() 'Countries (from column 9 - FR)
		//sheetreader.Read() 'Countries (from column 9 - FR)


		//    Do
		//        While sheetreader.Read
		//        sheetreader.GetValues(ob)

		//            For i = 0 To UBound(ob)
		//                Debug.Print(sheetreader.GetName(i) & ob(i))
		//            Next

		//        End While

		//    Loop While sheetreader.NextResult() 'move to the next result set (sheet)

		//    sheetreader.Close()
		//    MyCommand = Nothing


		//MyConnection.Close()

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button2_Click(object sender, EventArgs e)
	{
		int count;
		count = IndexPaths();
		Response.Write("Wrote " + count + " [Path] rows");

	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnMakeScreens_Click(object sender, EventArgs e)
	{
		PruneOffNonCompatableFamilyMinorSLotTypes();
		//Import.EnableSASLicense()

		//Import.Incremental(New List(Of String) From {"726660-B21"})

		//Dim errormessages As List(Of String) = New List(Of String)

		//da.DBExecutesql("DELETE FROM [field]")
		//da.DBExecutesql("DELETE FROM [InputType]")
		//da.DBExecutesql("DELETE FROM Screen")

		//Dim atype As clsInputType
		//iq.InputTypes.Clear()
		//iq.i_inputType_code.Clear()

		//atype = New clsInputType("single", "Single")
		//atype = New clsInputType("string", "Text")
		//atype = New clsInputType("int32", "Integer")
		//atype = New clsInputType("date", "Date")
		//atype = New clsInputType("many", "Many")  'dictionaries of other (editable) things
		//atype = New clsInputType("one", "One")
		//atype = New clsInputType("boolean", "Boolean")
		//atype = New clsInputType("translate", "Translation")
		//atype = New clsInputType("nullstring", "Nullable String")
		//atype = New clsInputType("nullint", "Nullable Integer")
		//atype = New clsInputType("customerprice", "customer specific price")
		//atype = New clsInputType("nullprice", "nullable price")
		//atype = New clsInputType("icon", "icon")  'an icon is a special field renders a productattribute - It's shown (as an icon) against a product if it's present - its (numeric) value becomes a tooltip - it *should not* have a translation
		//atype = New clsInputType("xNote", "xNote")  'a note is a special field - it's a product attribute with an image based ont he numeric value, - It also has a translation and it's visibility can be controlled by the HideF and ShowF (show and hide under family) attributes

		//iq.Screens.Clear()
		//iq.i_screens_title.Clear()
		//iq.Fields.Clear()

		//'make the screen for managing screens
		//Dim screen As clsScreen
		//screen = MakeScreen("Screen", "scrn", GetType(clsScreen), Nothing, errormessages)

		//'     screen = MakeScreen("Channel", GetType(clsChannel), Nothing)
		//' screen = MakeScreen("Product", GetType(clsProduct), Nothing)
		//' screen = MakeScreen("Thread", GetType(clsThread), Nothing)
		//' screen = MakeScreen("State", GetType(clsState), Nothing)


		//'load up what we just created
		//Dim con As SqlClient.SqlConnection
		//Dim rdr As SqlClient.SqlDataReader = Nothing
		//con = da.OpenDatabase()
		//iq.LoadScreens(con, rdr)
		//con.Close()
		//con.Dispose()

		//Response.Redirect("default.aspx") ' refresh the page to remake the edit button 


	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnImportUsers_Click(object sender, EventArgs e)
	{
		iq.Users.Clear();

		SqlClient.SqlConnection con = da.OpenDatabase();

		da.DBExecutesql(con, "delete from [user]");

		//USERS, ACCOUNTS and TEAMS about 21 seconds
		Dictionary<string, clsChannel> dicChannels = loadDic(con, iq.Channels, "channel");
		Dictionary<string, clsAccount> dicAccounts = loadDic(con, iq.Accounts, "account");
		Dictionary<string, clsTeam> dicTeams = loadDic(con, iq.Teams, "team");
		Dictionary<string, clsUser> dicUsers;
		// = loadDic(con, iq.Users, "user", EventDicLoad)

		dicUsers = new Dictionary<string, clsUser>();
		dicAccounts = new Dictionary<string, clsAccount>();

		Import.users(con, dicChannels, dicAccounts, dicTeams, dicUsers);

		saveDic(con, dicAccounts, "account");
		saveDic(con, dicTeams, "team");
		saveDic(con, dicUsers, "user");

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button3_Click(object sender, EventArgs e)
	{

		SqlClient.SqlConnection con;
		con = da.OpenDatabase();
		Import.Regions(con);
		con.Close();
		con.Dispose();


	}

	//OBSOLETED by variants

	//Protected Sub BtnImportChannelSKUs_Click(sender As Object, e As EventArgs) Handles BtnImportChannelSKUs.Click

	//    Dim con As SqlClient.SqlConnection
	//    con = da.opendatabase()

	//    da.dbexecutesql("DELETE FROM CHANNELSKU")


	//    Dim dt As DataTable = da.MakeWriteCacheFor(con, "ChannelSKU")

	//    Dim sql$
	//    sql$ = "SELECT distinct HOSTID,hostpartnum,hostmfrpartnum from iq.products.pricelistmaster Where hostpartnum is not null or hostmfrpartnum is not null"

	//    Dim rdr As SqlClient.SqlDataReader
	//    Dim row As System.Data.DataRow

	//    Dim rows As Integer = 0
	//    Dim failed As Integer = 0

	//    Dim hmpn As String

	//    rdr = da.dbexecuteReader(con, sql$)
	//    While rdr.Read

	//        If Not IsDBNull(rdr.Item("hostmfrpartnum")) Then
	//            hmpn = Split(rdr.Item("hostmfrpartnum"), "#")(0)

	//            If iq.i_SKU.ContainsKey(hmpn) Then
	//                If iq.i_channel_code.ContainsKey(rdr.Item("HOSTID")) Then
	//                    row = dt.NewRow()
	//                    row("fk_channel_id") = iq.i_channel_code(rdr.Item("hostid")).ID
	//                    row("fk_product_id") = iq.i_SKU(hmpn).ID
	//                    row("fk_variant_id") = iq.StandardVariant.ID

	//                    If IsDBNull(rdr.Item("HostPartnum")) Then   'westcoast (for example) don't provide a host partnum
	//                        row("channelSKU") = rdr.Item("hostmfrpartnum") 'use thier HostMfrParNum (complete with any #code)
	//                    Else
	//                        row("channelSKU") = rdr.Item("hostpartnum")
	//                    End If
	//                    row("channelMfrSKU") = rdr.Item("HostMfrPartNum")

	//                    dt.Rows.Add(row)
	//                    rows += 1
	//                Else

	//                    failed += 1
	//                End If

	//            Else
	//                failed += 1
	//            End If
	//        Else
	//            failed += 1
	//        End If

	//    End While
	//    rdr.Close()

	//    da.bulkwrite(con, dt, "ChannelSKU")

	//    con.Close()
	//    con.Dispose()

	//    Response.Write("Imported " & rows)

	//End Sub


	protected void  // ERROR: Handles clauses are not supported in C#
BtnImportFormFactors_Click(object sender, EventArgs e)
	{
		SqlClient.SqlConnection con;
		con = da.OpenDatabase();

		Dictionary<string, clsTranslation> dicFF;
		dicFF = Import.FormFactors(con);

		SqlClient.SqlDataReader rdr;

		DataTable writecache = da.MakeWriteCacheFor(con, "ProductAttribute");

		object sql;

		sql = "delete from translation where [group]='ff'";
		iq.LoadTranslations(con);


		sql = "Delete from iquote2.productAttribute where fk_attribute_id=" + iq.i_attribute_code("formFactor").ID;
		//sql$ = "select modelsku,sf.InstFormFactor as ff from IQ.products.Systems s join IQ.products.SysFamilyDefinitions sf on s.FamilyCode = sf.SysFamily join iq.dbo.abbreviations a on a.code=ff"
		sql = "select modelsku,a.Translation,sf.InstFormFactor as ff from IQ.products.Systems s join IQ.products.SysFamilyDefinitions sf on s.FamilyCode = sf.SysFamily join iq.dbo.abbreviations a on a.code=sf.InstFormFactor";
		rdr = da.DBExecuteReader(con, sql);


		clsProduct system;
		clsProductAttribute formfactor;

		int count = 0;
		int rows = 0;

		clsTranslation tl;
		while (rdr.Read) {
			if (iq.i_SKU.ContainsKey(rdr.Item("MODELSKU"))) {
				system = iq.i_SKU(rdr.Item("modelsku"));
				if (system.isSystem) {
					if (dicFF.ContainsKey(rdr.Item("translation"))) {
						tl = dicFF(rdr.Item("translation"));
						formfactor = new clsProductAttribute(system, iq.i_attribute_code("formFactor"), 0, iq.i_unit_code("txt"), tl, writecache);
						count += 1;
					} else {
						Beep();
					}
				}
				rows += 1;
			}
		}
		rdr.Close();

		da.BulkWrite(con, writecache, "ProductAttribute");

		con.Close();
		con.Dispose();

	}
	private int alphaval(l)
	{

		//return a numeric value from the first 4 chars of string

		alphaval = 0;

		int p = 1;
		for (int i = 1; i <= 4; i++) {
			alphaval += Asc(Mid(l, i, 1));
			p = p * 256;
		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnAvalanche_Click(object sender, EventArgs e)
	{
		SqlClient.SqlConnection con = da.OpenDatabase();

		Import.RefCodes(con);
		Import.Avalanche(con);

		con.Close();
		con.Dispose();

	}

	protected void  // ERROR: Handles clauses are not supported in C#
BtnImportBundles_Click(object sender, EventArgs e)
	{
		SqlClient.SqlConnection con = da.OpenDatabase();

		// Import.Bundles(con)
		con.Close();
		con.Dispose();

	}


	protected void  // ERROR: Handles clauses are not supported in C#
SetScreen_Click(object sender, EventArgs e)
	{
		clsScreen hdds = (from r in iq.Screens.Valueswhere r.title == "HDDs").First;
		clsScreen mem = (from r in iq.Screens.Valueswhere r.title == "Memory").First;
		clsScreen laptops = (from r in iq.Screens.Valueswhere r.title == "Laptops").First;
		clsScreen desktops = (from r in iq.Screens.Valueswhere r.title == "Desktops").First;
		clsScreen servers = (from r in iq.Screens.Valueswhere r.title == "Servers").First;
		clsScreen carePacks = (from r in iq.Screens.Valueswhere r.title == "CarePack").First;

		if (hdds == null)
			System.Diagnostics.Debugger.Break();
		if (mem == null)
			System.Diagnostics.Debugger.Break();

		List<string> errormessages = new List<string>();
		iq.RootBranch.Matrix = servers;
		iq.RootBranch.Update(errormessages);

		foreach ( branch in iq.Branches.Values) {
			object bn = LCase(branch.Translation.text(English));

			if (InStr(bn, "hard disk") | LCase(branch.Translation.text(English)) == "storage") {
				branch.Matrix = hdds;
				branch.Update(errormessages);
			//strictly equal !
			} else if (bn == "memory") {
				branch.Matrix = mem;
				branch.Update(errormessages);
			//strictly equal !
			} else if (bn == "laptops") {
				branch.Matrix = laptops;
				branch.Update(errormessages);
			//strictly equal !
			} else if (bn == "servers") {
				branch.Matrix = servers;
				branch.Update(errormessages);
			//strictly equal !
			} else if (bn == "desktops") {
				branch.Matrix = desktops;
				branch.Update(errormessages);
			//strictly equal !
			} else if (bn == "hp networking") {
				branch.Matrix = servers;
				branch.Update(errormessages);
			//strictly equal !
			} else if (bn == "hp storage") {
				branch.Matrix = servers;
				branch.Update(errormessages);

			// a placeholder branch
			} else if (branch.Product == null) {
				// If branch.Product.isSystem Then
				if (!object.ReferenceEquals(branch, iq.RootBranch)) {
					switch (bn) {
						case "smart buy":
						case "regular models":

						case "top value":

						case "cpus":
						case "warranty":
						case "services":
							branch.Matrix = carePacks;

							branch.Update(errormessages);
						default:
							branch.Matrix = null;
							//iq.Screens(719)

							branch.Update(errormessages);
					}
				}
			}

			//            If LCase(branch.Translation.text(English)) = "performance" Then branch.Picture = "tab" : branch.Update()

		}

	}

	//Protected Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click

	//    Response.Write(Import.HostPrices(TxtHostID.Text, "325009"))



	//End Sub


	protected void  // ERROR: Handles clauses are not supported in C#
PtnPower_Click(object sender, EventArgs e)
	{
		SqlClient.SqlConnection con;
		con = da.OpenDatabase();
		object l;
		l = Import.PowerSizing(con);
		con.Close();
		con.Dispose();

		Response.Clear();
		Response.Write("<p>" + l + "</p>");

	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnExtText_Click(object sender, EventArgs e)
	{
		System.Diagnostics.Debugger.Break();
		//Response.Write(Import.ExtText())

	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnSlotAdds_Click(object sender, EventArgs e)
	{
		SqlClient.SqlConnection con = da.OpenDatabase();
		Response.Write(Import.slotAdds(con));
		con.Close();
		con.Dispose();

	}



	protected void  // ERROR: Handles clauses are not supported in C#
Button6_Click(object sender, EventArgs e)
	{
		clsVariant chassisvariant;


		object chassis = from j in iq.Products.Valueswhere object.ReferenceEquals(j.ProductType, iq.i_ProductType_Code("CHAS"));
		foreach ( cp in chassis) {
			chassisvariant = new clsVariant("", cp, HP, "", "", "", "", r_worldwide, 0);
			//Every product needs a variant - so it can be stored in a QuoteItem
		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button7_Click(object sender, EventArgs e)
	{
		// FixVariants("DAZRG248NE")

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button8_Click(object sender, EventArgs e)
	{

		SqlClient.SqlConnection con = da.OpenDatabase();


		Dictionary<string, clsBranch> dicSystems = loadDic(con, iq.Branches, "system");
		Dictionary<string, clsRegion> dicRegions = loadDic(con, iq.Regions, "region");


		//around 18 seconds
		Dictionary<string, clsProduct> dicAutoAdds;
		dicAutoAdds = loadDic(con, iq.Products, "autoAdd");

		dicAutoAdds = new Dictionary<string, clsProduct>();
		//remove this to to a delta import

		Response.Write(Import.autoadds(con, dicAutoAdds, dicSystems, dicRegions));
		Response.Write("see c:\\temp\\import.log  and c:\\temp\\autoadds.txt");
		saveDic(con, dicAutoAdds, "autoadd");


		con.Close();



	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button9_Click(object sender, EventArgs e)
	{
		//order branches

		List<string> errormessages = new List<string>();

		Dictionary<string, int> otr = new Dictionary<string, int>();
		Dictionary<string, int> otp = new Dictionary<string, int>();

		object sql;
		SqlClient.SqlConnection con = da.OpenDatabase();

		SqlClient.SqlDataReader rdr;

		sql = "SELECT optTypeCode,optTypeName,optTypeRank FROM iq.products.optTypes";
		rdr = da.DBExecuteReader(con, sql);
		while (rdr.Read) {
			if (object.ReferenceEquals(rdr.Item("optTypeRank"), DBNull.Value)) {
				otr.Add(rdr.Item("optTypeName"), 100);
			} else {
				if (!otr.ContainsKey(rdr.Item("optTypeName"))) {
					otr.Add(rdr.Item("optTypeName"), rdr.Item("optTypeRank"));
				}
			}
		}
		rdr.Close();

		sql = "SELECT optTypeParent,parentRank FROM iq.products.optTypeParents";
		rdr = da.DBExecuteReader(con, sql);
		while (rdr.Read) {
			if (object.ReferenceEquals(rdr.Item("parentRank"), DBNull.Value)) {
				otp.Add(rdr.Item("optTypeParent"), 100);
			} else {
				otp.Add(rdr.Item("optTypeParent"), rdr.Item("parentRank"));
			}
		}

		rdr.Close();
		con.Close();

		foreach ( branch in iq.Branches.Values) {
			if (branch.Product == null) {
				//it's a category
				if (branch.Parent == null) {
					if (otp.ContainsKey(branch.EnglishName)) {
						branch.order = otp(branch.EnglishName);
						branch.Update(errormessages);
					}
				} else {
					if (otr.ContainsKey(branch.EnglishName)) {
						branch.order = otr(branch.EnglishName);
						branch.Update(errormessages);
					}
				}
			}


		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button10_Click(object sender, EventArgs e)
	{
		Import.CarePackProperties();

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button11_Click(object sender, EventArgs e)
	{
		//Import loyalty points

		Import.LoyaltyPoints();

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button12_Click(object sender, EventArgs e)
	{

		List<string> errormessages = new List<string>();
		Import.Receta(errormessages);
		OutputErrors(Form.Controls, errormessages, 0, true);

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button13_Click(object sender, EventArgs e)
	{
		Import.FlexOPGs();

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button14_Click(object sender, EventArgs e)
	{
		List<string> errorMessages = new List<string>();
		SqlClient.SqlConnection con = da.OpenDatabase();
		Logit("cpuImport", true);
		Import.CPUs(con, errorMessages);
		con.Close();
		OutputErrors(Form.Controls, errorMessages, 0, true);

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button2_Click1(object sender, EventArgs e)
	{

		List<string> errormessages = new List<string>();

		foreach ( branch in iq.Branches.Values) {
			if (InStr(LCase(branch.Picture), "/iq/images") > 0) {
				branch.Picture = Replace(branch.Picture, "/iq/images", "/images/iq");
				branch.Update(errormessages);
			}
		}

	}

	protected void  // ERROR: Handles clauses are not supported in C#
Button15_Click(object sender, EventArgs e)
	{
		Import.TopRecommendations();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
Button16_Click(object sender, EventArgs e)
	{
		Import.HighPerformance();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
Button17_Click(object sender, EventArgs e)
	{
		Import.energyStar();
	}



	protected void  // ERROR: Handles clauses are not supported in C#
Button18_Click(object sender, EventArgs e)
	{
		//read through all the source code . .
		//get (in advance of it every displaying) a copy of every 'key'

		StreamWriter sw = new StreamWriter("C:\\Download\\xlt\\xlt.txt");

		string sqlString = "SELECT *  FROM [WWW3.CHANNELCENTRAL.NET\\CHARLIE,8484].[iQ].[dbo].[Language_Store] ls1 ";
		SqlClient.SqlDataReader rdr;
		SqlClient.SqlConnection con = da.OpenDatabase();
		rdr = da.DBExecuteReader(con, sqlString);

		DataTable dt = new DataTable();
		dt.Load(rdr);
		foreach ( Folder in My.Computer.FileSystem.GetDirectories("c:\\sites\\iq\\iq")) {

			foreach ( File in My.Computer.FileSystem.GetFiles(Folder)) {
				StreamReader sr = new StreamReader(File);
				string[] l = sr.ReadToEnd.Split(vbCrLf);
				sr.Close();

				foreach ( ln in l) {
					if (ln.ToLower.Contains("xlt(")) {
						int txtpos = InStr(ln.ToLower(), "xlt(");
						int txtpos1 = InStr(txtpos + 5, ln, Chr(34), CompareMethod.Text);
						if (txtpos1 > txtpos) {
							// sw.WriteLine(Mid(ln, txtpos + 5, txtpos1 - (txtpos + 5)))
							//Dim txtpos2 As Integer = InStr(txtpos1 + 1, ln, Chr(34))
							//If txtpos2 > txtpos1 Then
							//    sw.WriteLine(Mid(ln, txtpos1, txtpos2 - txtpos1))
							//End If

							AddNewTranslation(Mid(ln, txtpos + 5, txtpos1 - (txtpos + 5)), sw, dt);

						}
						// sw.WriteLine(ln)  'needs some more parseing
					}
					if (ln.ToLower.Contains("<asp:")) {
						if (ln.ToLower.Contains(" text")) {
							int txtpos = InStr(ln.ToLower(), " text");
							int txtpos1 = InStr(txtpos + 1, ln, Chr(34), CompareMethod.Text);
							if (txtpos1 > txtpos) {
								int txtpos2 = InStr(txtpos1 + 1, ln, Chr(34));
								if (txtpos2 > txtpos1) {
									// sw.WriteLine(Mid(ln, txtpos1 + 1, txtpos2 - (txtpos1 + 1)))

									AddNewTranslation(Mid(ln, txtpos1 + 1, txtpos2 - (txtpos1 + 1)), sw, dt);
								}
							}
						} else if (ln.ToLower.Contains("<asp:label")) {
							int txtpos = InStr(ln, ">");
							int txtpos1 = InStr(txtpos + 1, ln, "<", CompareMethod.Text);
							if (txtpos1 > txtpos) {
								// sw.WriteLine(Mid(ln, txtpos + 1, txtpos1 - (txtpos + 1)))
								object t = Mid(ln, txtpos + 1, txtpos1 - (txtpos + 1));
								AddNewTranslation(t, sw, dt);
							}
							//  sw.WriteLine(ln)
						}
					}
					//same for any .caption = "
					//or .text =
					//(etc... see The form controls translations in Masterpage)
				}
			}
		}

		sw.Close();

		//Make another streamreader here - and read back all the translations - making a KY one, and a EN one
		//If you create them all with a GROUP of 'UI' - we can delete them all together (for testing/refreshing)
		//we can also make french german spanish etc - for anything we already have an exact translation for in IQ1

	}

	private void AddNewTranslation(string p1, ref StreamWriter sw, ref DataTable dt)
	{
		clsTranslation importTranslation;
		clsLanguage kyLanguage = (from l in iq.Languages.Valueswhere l.Code == "KY").First;
		if (p1.Contains("'")) {
			p1 = p1.Replace("'", "~");
			p1 = p1.Replace("~", "''");
		}

		string queryString = "Translation = '" + p1 + "'";
		clsLanguage otherLanguage;
		DataRow[] result = dt.Select(queryString);

		string rowLanguage;

		if (result.Length > 0) {
			DataRow item = result(0);
			if (!iq.KYIndex.ContainsKey(p1)) {
				importTranslation = new clsTranslation(kyLanguage, p1);

				queryString = "textID =" + item("textID").ToString();
				DataRow[] rows = dt.Select(queryString);
				foreach (DataRow row in rows) {
					rowLanguage = row("lang");
					otherLanguage = iq.i_language_Code(rowLanguage.ToUpper);
					// (From l In iq.Languages.Values Where l.Code = rowLanguage.ToUpper).First
					importTranslation.addLanguage(otherLanguage, row("Translation"), dt);
					importTranslation.Update(otherLanguage);
				}
			}
		} else {
			sw.WriteLine(p1);

		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button19_Click(object sender, EventArgs e)
	{
		Import.fixProductFamilies();


	}

	protected void  // ERROR: Handles clauses are not supported in C#
Button20_Click(object sender, EventArgs e)
	{
		Import.defaultWarranty();

	}



	protected void  // ERROR: Handles clauses are not supported in C#
Button22_Click(object sender, EventArgs e)
	{
		Literal lit = new Literal();

		List<string> errormessages = new List<string>();
		lit.Text = "<p>" + GetVariants(txtHostCode.Text, "", errormessages) + "</p>";
		Form.Controls.Add(lit);


	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button23_Click(object sender, EventArgs e)
	{

		SqlClient.SqlConnection con = da.OpenDatabase;
		Import.quoteStates(con);
		con.Close();


	}


	public void  // ERROR: Handles clauses are not supported in C#
Button24_Click(object sender, EventArgs e)
	{
		List<string> errormessages = new List<string>();

		SetRCAs(errormessages);

		OutputErrors(Form.Controls, errormessages, 0, true);
		Form.Controls.Add(NewLit("Done"));

	}

	protected void  // ERROR: Handles clauses are not supported in C#
btnSBSO_Click(object sender, EventArgs e)
	{
		Page.Controls.Add(NewLit("<p>" + Import.Hierarchy() + "</p>"));
		//  Dim output As String = IQDrive.uploadFile()
	}


	private static void SetRCAs(ref List<string> errorMessages)
	{
		Dictionary<string, List<int>> how = new Dictionary<string, List<int>>();
		iq.RootBranch.setRCA(1, iq.RootBranch, how, errorMessages);


		foreach ( h in how.Keys) {
			int done = 0;
			bool alldone = false;
			do {
				object lump = (from v in how(h)(string)v).Skip(done).Take(5000);
				if (lump.Count < 5000)
					alldone = true;
				string[] ids = lump.ToArray;
				da.DBExecutesql("UPDATE branch set rca ='" + h + "' WHERE ID IN (" + Join(ids, ",") + ");");
				done += 5000;
			} while (!(alldone));

		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button25_Click(object sender, EventArgs e)
	{
		SqlClient.SqlConnection con = da.OpenDatabase;
		Page.Controls.Add(NewLit(quoteImport.all(con)));
		con.Close();

	}

	protected void  // ERROR: Handles clauses are not supported in C#
btnPreInstalled_Click(object sender, EventArgs e)
	{
		Import.preInstalledParts();

	}



	protected void  // ERROR: Handles clauses are not supported in C#
btnImpcrePacks_Click(object sender, EventArgs e)
	{
		//Import.CarePacks()
		Import.MLCarePacks();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
cmdQuickSpecs_Click(object sender, EventArgs e)
	{
		Import.ImportQuickSpecs();
	}


	protected void  // ERROR: Handles clauses are not supported in C#
cmdExtras_Click(object sender, EventArgs e)
	{
		Import.Extras();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
btnGraphics_Click(object sender, EventArgs e)
	{
		Import.Graphics();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
cmdNetworking_Click(object sender, EventArgs e)
	{
		Import.Networking();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
cmdPSU_Click(object sender, EventArgs e)
	{
		Import.ImportPSU();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
cmdOS_Click(object sender, EventArgs e)
	{
		Import.OSs();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
btnGenerateLang_Click(object sender, EventArgs e)
	{
		//Use this set to export KY index
		//Using writer As StreamWriter = New StreamWriter("C:\translationexport.txt")

		//    writer.WriteLine("KY|EN" & IIf(txtLang.Text <> "EN", "|" & txtLang.Text, ""))


		//    Dim list = From j In iq.Translations.Values Where j.Group <> ""
		//    For Each translation As clsTranslation In list
		//        Dim outputString As String
		//        outputString = translation.textTranslation(iq.i_language_Code("KY")) & "|" & translation.textTranslation(iq.i_language_Code("EN")) & IIf(txtLang.Text <> "EN", "|" & translation.textTranslation(iq.i_language_Code(txtLang.Text)), "")

		//        writer.WriteLine(outputString)
		//    Next
		//End Using


		//Use this to export all 
		using (StreamWriter writer = new StreamWriter("C:\\translationexport.txt")) {

			writer.WriteLine("EN|KeyCode|Group" + IIf(txtLang.Text != "EN", "|" + txtLang.Text, ""));


			object list = from j in iq.Translations.Values;
			foreach (clsTranslation translation in list) {
				string outputString;
				outputString = Replace(translation.textTranslation(iq.i_language_Code("EN")), vbTab, "") + "|" + translation.Key + "|" + Replace(translation.Group, vbTab, "") + IIf(txtLang.Text != "EN", "|" + translation.textTranslation(iq.i_language_Code(txtLang.Text)), "");

				writer.WriteLine(outputString);
			}
		}




	}

	protected void  // ERROR: Handles clauses are not supported in C#
btnImportTranslation_Click(object sender, EventArgs e)
	{
		//File format will be | deliminated column headers to be language id or language name, left column is ALWAYS the key used
		string savePath = Path.GetTempPath();
		string line = null;
		bool firstrow = false;
		int itemColumns = 0;
		clsLanguage transLang = null;
		if (uploadTranslations.HasFile) {
			savePath += uploadTranslations.FileName;
			uploadTranslations.SaveAs(savePath);

			object lines = File.ReadAllLines(savePath, Encoding.Unicode);
			List<clsLanguage> CSVLanguages = new List<clsLanguage>();
			object sourceLanguage = new Dictionary<string, List<clsTranslation>>();

			List<string> errors = new List<string>();
			bool haveHeaders = false;
			foreach ( line in lines) {
				object columns = line.Split("|");
				if (!haveHeaders) {
					//Do we have enough
					if (columns.Count < 2)
						throw new Exception("Not enough columns detected");

					foreach ( col in columns) {
						object l = findLanguage(col);
						if (CSVLanguages.Count == 0 && !{
							"KY",
							"EN"
						}.Contains(l.Code))
							throw new Exception("Can only convert KY or English for now!");
						if (CSVLanguages.Count == 0) {
							sourceLanguage = iq.Translations.GroupBy(ei => ei.Value.text(l).ToLower).ToDictionary(ei => ei.Key, ei => ei.Select(ei2 => ei2.Value).ToList());
						}
						if (l == null)
							throw new Exception("Cannot convert language");
						CSVLanguages.Add(l);
					}
					haveHeaders = true;
				} else {
					//Normal data line - for now we can only deal with English, ID or KY as the key
					switch (CSVLanguages(0).Code) {
						case  // ERROR: Case labels with binary operators are unsupported : Equality
"EN":
							if (sourceLanguage.ContainsKey(columns(0).ToString.ToLower)) {
								foreach ( t in sourceLanguage(columns(0).ToString.ToLower)) {
									t.addLanguage(CSVLanguages(1), columns(1), null);
								}
							} else {
								errors.Add("Could not find english for:" + columns(0));
							}
						case  // ERROR: Case labels with binary operators are unsupported : Equality
"KY":
							if (iq.KYIndex.ContainsKey(columns(0))) {
								iq.KYIndex(columns(0).ToString).addLanguage(CSVLanguages(1), columns(1), null);
							} else {
								errors.Add("Could not find english for:" + columns(0));
							}
					}
				}

			}


			//Using reader As StreamReader = New StreamReader(savePath)
			//    line = reader.ReadLine
			//    Do While line IsNot Nothing
			//        Dim stringItems() As String = Split(line, "|")
			//        If firstrow = False Then
			//            itemColumns = stringItems.Count
			//            If itemColumns > 3 Then
			//                transLang = iq.i_language_Code(stringItems(3))
			//            End If
			//            firstrow = True
			//        Else
			//            If stringItems.Count > 0 And transLang IsNot Nothing And IsNumeric(stringItems(1)) Then

			//                Dim trans As clsTranslation = iq.Translations(CInt(stringItems(1)))
			//                trans.addLanguage(transLang, stringItems(3), Nothing)
			//            End If
			//        End If
			//        line = reader.ReadLine
			//    Loop
			//End Using
			foreach ( er in errors) {
				AuditLog.Instance.Add(AuditType.Warning, er, "ImportTran", 0);
			}

		}
	}
	private clsLanguage findLanguage(object lk)
	{
		findLanguage = null;
		int id2;
		//do we recognise the language?
		if (int.TryParse(lk.ToString, id2) && iq.Languages.ContainsKey((int)lk)) {
			findLanguage = iq.Languages((int)lk);
		} else {
			if (iq.i_language_Code.ContainsKey(lk))
				findLanguage = iq.i_language_Code(lk);
		}


	}
	private void ProcessTranslations(HttpPostedFile file)
	{
		System.IO.Stream myStream;
		Int32 fileLen;
		StringBuilder displayString = new StringBuilder();

		// Get the length of the file.
		fileLen = uploadTranslations.PostedFile.ContentLength;

		// Create a byte array to hold the contents of the file.
		byte[] Input = new byte[fileLen - 1];
		// Initialize the stream to read the uploaded file.
		myStream = uploadTranslations.FileContent;

		// Read the file into the byte array.
		myStream.Read(Input, 0, fileLen);

		//Copy the byte array to a string

		for (int loop1 = 0; loop1 <= loop1 < fileLen; loop1++) {
			displayString.Append(Input(loop1).ToString());
		}


	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button26_Click(object sender, EventArgs e)
	{
		//standalone new options import

		SqlClient.SqlConnection con = da.OpenDatabase();
		Dictionary<string, clsBranch> dicfamilies = loadDic(con, iq.Branches, "family");

		Logit("building PLcode lookup dictionary");
		Dictionary<string, string> dicplcode;
		dicplcode = LoadPLCodes(con);
		//generates a dictionary of SKU>Plcode

		Dictionary<string, clsUnit> dicUnits = new Dictionary<string, clsUnit>();
		// = loadDic(con, iq.Units, "unit", AnEvent)
		Import.units(con, dicUnits);
		//        saveDic(con, dicUnits, "unit", anEvent)

		Dictionary<clsProduct, List<clsRegion>> dicOptLocalisation = new Dictionary<clsProduct, List<clsRegion>>();
		Dictionary<string, clsBranch> opts = Import.options2(con, dicplcode, dicUnits, dicOptLocalisation, clsRegion.containment);

		Response.Write("Loaded " + opts.Count + " options");
		Response.Write("Options root is" + opts("ALL").ID);


		con.Close();
		con = da.OpenDatabase;

		Dictionary<string, clsBranch> dicSystems = loadDic(con, iq.Branches, "system");

		con = da.OpenDatabase();
		Import.Buildtree2(con, opts, dicfamilies, dicSystems, dicOptLocalisation);

		con.Close();

	}



	protected void  // ERROR: Handles clauses are not supported in C#
Button27_Click(object sender, EventArgs e)
	{

		List<string> errormessages = new List<string>();
		iq.RootBranch.OrderFamilies(1, errormessages);


	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button28_Click(object sender, EventArgs e)
	{
		//prunes off options which are not listed as comatible with their family

		Import.DoPrunes();


	}



	protected void Button29_Click(object sender, EventArgs e)
	{
		SaveUserStates();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
Button30_Click(object sender, EventArgs e)
	{
		Import.SoftwareSlots();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
Button31_Click(object sender, EventArgs e)
	{
		Import.chassisMemSlots();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
Button32_Click(object sender, EventArgs e)
	{
		Import.DoPrunes();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
Button33_Click(object sender, EventArgs e)
	{
		Import.fixFamMinor();

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button34_Click(object sender, EventArgs e)
	{
		Import.fixPci();

	}

	protected void ButtonOP_Click(object sender, EventArgs e)
	{
		Import.InterfaceSlots();
	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button35_Click(object sender, EventArgs e)
	{

		clsVariant avariant;
		clsPrice aprice;

		DataTable vwc;
		DataTable pwc;

		SqlClient.SqlConnection con;
		con = da.OpenDatabase;

		clsChannel chn = iq.i_channel_code("ACHCM72YN");

		da.DBExecutesql("delete from price where fk_variant_id in (select Id from variant where fk_channel_id_seller= " + chn.ID + ")");
		da.DBExecutesql("delete from stock where fk_variant_id in (select Id from variant where fk_channel_id_seller= " + chn.ID + ")");
		da.DBExecutesql("delete from variant where fk_channel_id_seller= " + chn.ID);

		int nvid;
		int npid;
		vwc = da.MakeWriteCacheFor(con, "variant", nvid, true);
		pwc = da.MakeWriteCacheFor(con, "price", npid, true);

		chn.priceConfig = 5;
		//Take off the 'webservice' Bit
		List<string> errormessages = new List<string>();
		chn.Update(errormessages);

		foreach ( sku in iq.i_SKU.Keys) {
			clsProduct product = iq.i_SKU(sku);
			avariant = new clsVariant("AAV", product, chn, "AA-" + sku, "", "", "", r_worldwide, 0, vwc,
			nvid);
			//  aprice = New clsPrice(avariant, iq.getPriceBand(""), New NullablePrice(CSng(1), iq.i_currency_code("GBP"), False), "FAKE", pwc, npid)
		}

		da.BulkWrite(con, vwc, "Variant", , true);
		da.BulkWrite(con, pwc, "Price", , true);

		con.Close();

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button36_Click(object sender, EventArgs e)
	{
		//Adds a focus attribute to factory installed options sush that they *wont* appear (for an channld that's not focussing on FIOs)
		Import.FIOfocus();

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button37_Click(object sender, EventArgs e)
	{
		Import.Legal();
	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button38_Click(object sender, EventArgs e)
	{
		clsVariant avariant;

		clsChannel unhosted = iq.i_channel_code("UNHOSTED");
		int nvid = 0;
		int npid = 0;

		SqlClient.SqlConnection con = da.OpenDatabase();
		DataTable vwc = da.MakeWriteCacheFor(con, "Variant", nvid, true);
		DataTable pwc = da.MakeWriteCacheFor(con, "Price", npid, true);

		clsRegion xw = iq.i_region_code("XW");

		int c = 1;
		foreach ( product in iq.Products.Values) {
			avariant = new clsVariant("TST", product, unhosted, "FAKE" + c, "*Test variant*", "CCC", "#UK", xw, false, vwc,
			nvid);

			NullablePrice pr = new NullablePrice((float)100, iq.i_currency_code("GBP"), false);
			//Dim prc As clsPrice = New clsPrice(avariant, iq.getPriceBand(""), pr, "fake", pwc, npid)
			c += 1;
		}

		da.BulkWrite(con, vwc, "Variant");
		da.BulkWrite(con, pwc, "Price");

		con.Close();

		Beep();

	}

	protected void  // ERROR: Handles clauses are not supported in C#
btnImportQuotes_Click(object sender, EventArgs e)
	{
		SqlClient.SqlConnection con = da.OpenDatabase;
		List<string> errorMessages = new List<string>();
		quoteImport.QuotesByHostID(con, txtHostID2.Text, errorMessages);
		if (errorMessages.Count > 0) {
			Label9.Text = Join(errorMessages.ToArray(), "###");
		}
	}


	protected void btnIncImport_Click(object sender, EventArgs e)
	{
	}

	protected void  // ERROR: Handles clauses are not supported in C#
btnSpecificImport_Click(object sender, EventArgs e)
	{
		string retString = "";
		switch (txtSpecificImport.Text) {
			case "SoftwareSlots":
				SqlClient.SqlDataReader rdr;
				object sql__1 = "SELECT sysType,modelSKU,cpu,cpuqty,ram,ramqty,pristor,pristorqty,secstor,secstorqty,terstor,terstorqty,raid,raidcache,psu,psuqty,iloLicense,iloHardware,iceIncluded,";
				sql__1 += "WLAN,WWAN,[controllers],extras,software,discreteGraphics,options";
				sql__1 += " FROM h3.[iq].products.union_systems ";

				rdr = da.DBExecuteReader(da.OpenDatabase(), sql__1);

				while ((rdr.Read)) {
					if (iq.i_SKU.ContainsKey(rdr("modelsku"))) {
						object sys = iq.i_SKU(rdr("modelsku"));
						foreach ( one in Split("software,extras,iceIncluded,options", ",")) {
							if (!IsDBNull(rdr.Item(one))) {
								foreach ( s in Split(rdr.Item(one), ",")) {
									if (s == "727258-B21") {
										object a = 9;
									}
									//.Values 'assuming only one path...
									foreach ( sysb in sys.Branches) {
										string resultPath = "";
										if (sysb.AllPaths.Count > 0) {
											object childb = sysb.findChildBySKU2(sysb.AllPaths.First, s, resultPath);
											if (childb != null) {
												if (childb.Quantities.Where(q => (q.Value.Path == "" || q.Value.Path == resultPath) && q.Value.FOC == true && q.Value.NumPreInstalled == 1).Count == 0) {
													object q = new clsQuantity(iq.i_region_code("XW"), resultPath, childb, 1, 1, 1, true);
													retString += "Adding For: " + s + "-" + childb.ID + "-" + resultPath + Environment.NewLine;
												} else {
													retString += "Already exists for: " + s + "-" + childb.ID + "-" + resultPath + Environment.NewLine;
												}
											} else {
												retString += "Part Missing: " + s + Environment.NewLine;
											}
										}
									}
								}
							}
						}
					}

				}
			case "Incomat":
				//Check IQ1 incompatible field has come across for everything and then prune based on this

				//Make sure we have all the attributes...
				object sql__1 = "select optsku,incompatible from h3.iq.products.options where incompatible is not null";
				object rdr = da.DBExecuteReader(da.OpenDatabase(), sql__1);
				while ((rdr.Read)) {
					if (iq.i_SKU.ContainsKey(rdr("optsku"))) {
						if (!iq.i_SKU(rdr("optsku")).i_Attributes_Code.ContainsKey("incompat")) {
							object n = new clsProductAttribute(iq.i_SKU(rdr("optsku")), iq.i_attribute_code("incompat"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr("incompatible").ToString(), English, "", 0, null, 0, false));
						}
					}
				}


				foreach ( p in iq.Products.Values) {
					if (p.i_Attributes_Code.ContainsKey("incompat")) {
						//We have a product with incompatibility
						//.Values
						foreach ( b in p.Branches) {
							foreach ( path__2 in b.AllPaths) {
								string fm = "";
								object fbs = findFamily(path__2, fm, true, false);
								List<string> incompat = new List<string>();
								foreach ( il in p.i_Attributes_Code("incompat").Select(aa => aa.Translation.text(English))) {
									incompat.AddRange(Split(il, ","));
								}
								//contains 
								if (incompat.Where(ic => ic.ToLower == fm.ToLower || ic.ToLower == fbs.ToLower).Count > 0) {
									//We have an incompat path
									if (b.Prunes.Where(pr => pr.Value.Path == path__2).Count == 0) {
										object pr = new clsPrune(path__2, new NullableInt(), "IncompatDef");
									}
								}
							}
						}
					}
				}

			case "CheckFamSlots":
				List<string> errorMessages__3 = new List<string>();
				Dictionary<string, Dictionary<string, string>> ofDic = Import.FamilyOptTypeToOptFamily();

				if (iq.i_slotType_Code.ContainsKey("VGA") || iq.i_slotType_Code("VGA").ContainsKey("VIDEO_DISPLAYS")) {
					object ff = new clsSlotType("VGA", "VIDEO_DISPLAYS", iq.AddTranslation("Video Display", English, "", 0, null, 0, false));
				}
				if (iq.i_slotType_Code.ContainsKey("VGA") || iq.i_slotType_Code("VGA").ContainsKey("VIDEO_DEVICES_SERVERS")) {
					object ff = new clsSlotType("VGA", "VIDEO_DEVICES_SERVERS", iq.AddTranslation("Video Devices Servers", English, "", 0, null, 0, false));
				}

				if (iq.i_slotType_Code.ContainsKey("VGA") || iq.i_slotType_Code("VGA").ContainsKey("VIDEO_ADAPTERS")) {
					object ff = new clsSlotType("VGA", "VIDEO_ADAPTERS", iq.AddTranslation("Video Adapter", English, "", 0, null, 0, false));
				}
				if (iq.i_slotType_Code.ContainsKey("FAN") || iq.i_slotType_Code("FAN").ContainsKey("NHP_FAN")) {
					object ff = new clsSlotType("FAN", "NHP_FAN", iq.AddTranslation("Non-HotPlug FAN", English, "", 0, null, 0, false));
				}
				if (iq.i_slotType_Code.ContainsKey("FAN") || iq.i_slotType_Code("FAN").ContainsKey("FAN_NHP")) {
					object ff = new clsSlotType("FAN", "FAN_NHP", iq.AddTranslation("Non-HotPlug FAN", English, "", 0, null, 0, false));
				}
				if (iq.i_slotType_Code.ContainsKey("PSU") || iq.i_slotType_Code("PSU").ContainsKey("SAN_POWER")) {
					object ff = new clsSlotType("PSU", "SAN_POWER", iq.AddTranslation("SAN Power Supply", English, "", 0, null, 0, false));
				}


				object dt = da.FilledDataTable(da.OpenDatabase(), "select * from h3.iq.products.optionlimits");
				foreach ( b in iq.Branches.Values) {
					if (b.Product != null && b.Product.isSystem) {
						if (b.Product.SKU == "J8Z43EA") {
							object f = 8;
						}

						foreach ( Path__4 in b.AllPaths) {
							string fm = "";
							findFamily(Path__4, fm, true, true);
							object rs = dt.Select("SysFamily = '" + fm + "'");
							foreach ( r in rs) {
								if (!iq.i_slotType_Code.ContainsKey(r("OptTYpe")))
									continue;
								object relevantslots = b.slots.Where(sl => sl.Value.Type.MajorCode.ToLower == r("OptType").ToString.ToLower && (sl.Value.path == "" || sl.Value.path == Path__4)).ToList();
								//Add chassis slots
								clsBranch cb;
								foreach ( c in b.childBranches.Values) {
									if (c.Translation.text(English).Contains(" chassis")) {
										cb = c;
										relevantslots.AddRange(c.slots.Where(sl => sl.Value.Type.MajorCode.ToLower == r("OptType").ToString.ToLower && (sl.Value.path == "" || sl.Value.path == Path__4)));
									}
								}
								if (relevantslots.Count == 0 && (int)r("QtyMax") > 0) {
									//Add then slot
									object minT = "";
									if (!ofDic(fm).ContainsKey(r("OptType").ToString)) {
										if (iq.i_slotType_Code(r("opttype")).Count == 1) {

											if (!iq.i_slotType_Code(r("OptType")).ContainsKey("GEN")) {
												minT = iq.i_slotType_Code(r("opttype")).First.Value.MinorCode;

											}

										} else {
											object st2 = new clsSlotType(r("OptType"), "GEN", iq.AddTranslation(r("opttype"), English, "", 0, null, 0, false));
											minT = "GEN";
										}

									} else {
										minT = ofDic(fm)(r("OptType").ToString);
									}
									//we dont want fan and memory is on the CPU in some cases so its too dangerous here
									if (iq.i_slotType_Code.ContainsKey(r("opttype").ToString()) && r("opttype") != "FAN" && r("opttype") != "MEM") {
										if (minT == "POWER_NBK" | minT == "POWER_PBK") {
											if (!iq.i_slotType_Code("PSUm").ContainsKey(ofDic(fm)(r("OptType").ToString))) {
												object sl = new clsSlotType("PSUm", ofDic(fm)(r("OptType").ToString), iq.AddTranslation("ProBook Power Supply", English, "", 0, null, 0, false));
											}
											object slott = new clsSlot(iq.i_slotType_Code("PSUm")(ofDic(fm)(r("OptType").ToString)), cb, "", (int)r("QtyMax"), null, new NullableInt(), r("Incr_min"), IsDBNull(r("incr_pref")) ? 0 : (int)r("incr_pref"));
											retString += "Added: PSUm -" + ofDic(fm)(r("OptType").ToString) + "-" + b.Product.SKU + Environment.NewLine;
										} else {
											object slot = new clsSlot(iq.i_slotType_Code(r("opttype").ToString())(minT), cb, "", (int)r("QtyMax"), null, new NullableInt(), r("Incr_min"), IsDBNull(r("incr_pref")) ? 0 : (int)r("incr_pref"));
											retString += "Added: " + r("opttype").ToString() + "-" + minT + "-" + b.Product.SKU + Environment.NewLine;
										}

									}
								}
								if (relevantslots.Count > 0 && (int)r("QtyMax") == 0) {
									//Remove
									if ({
										"POEP",
										"OPT",
										"SFPP"
									}.Contains(r("opttype"))) {
										relevantslots.First.Value.delete(errorMessages__3);
										retString += "DELETE: " + b.Product.SKU + Environment.NewLine;
									}
								}
								//Work on diff numbers...

							}
						}
					}


				}

			case "OptionSlots":
				List<string> errorMessages__3 = new List<string>();
				object sql__1 = "select optsku,slots,opttype,optfamily from h3.iq.products.options";
				object rdr = da.DBExecuteReader(da.OpenDatabase(), sql__1);
				while ((rdr.Read)) {
					if (iq.i_SKU.ContainsKey(rdr("optsku"))) {
						if (iq.i_slotType_Code.ContainsKey(rdr("opttype")) && iq.i_slotType_Code(rdr("opttype")).ContainsKey(rdr("optfamily"))) {
							object prod = iq.i_SKU(rdr("optsku"));
							//.Values
							foreach ( branch in prod.Branches) {
								object sl = branch.slots.Values.Where(br => br.Type.MajorCode.ToLower == rdr("opttype").ToString.ToLower);
								// removed AndAlso br.Type.MinorCode.ToLower = rdr("optfamily").ToString.ToLower for CPU's mainly
								object slcount = sl.Sum(d => d.numSlots);
								if ((int)rdr("slots") > 0 & slcount == 0) {
									//Need to add
									object newslot = new clsSlot(iq.i_slotType_Code(rdr("opttype"))(rdr("optfamily")), branch, "", -rdr("slots"), null, new NullableInt(), 0, 0, null);
									retString += "Adding: " + rdr("optsku") + "-" + rdr("opttype") + "." + rdr("optfamily") + Environment.NewLine;
								} else if ((int)rdr("slots") == 0 & slcount > 0) {
									foreach ( s in sl) {
										s.delete(errorMessages__3);
									}
									retString += "Removed: " + rdr("optsku") + "-" + rdr("opttype") + "." + rdr("optfamily") + Environment.NewLine;
								//Remove
								} else if (-(int)rdr("slots") != slcount) {
									//Need to amend
									///''How?
									object a = 9;
									retString += "Altered: " + rdr("optsku") + "-" + rdr("opttype") + "." + rdr("optfamily") + Environment.NewLine;
								}


							}
						} else {
							//Slot doesnt exist
							retString += "SlotType: " + rdr("opttype") + "." + rdr("optfamily") + Environment.NewLine;
						}
					}

				}
			case "FixLocalisation":

				object sql__1 = "select optsku,aaonly,localisation from h3.iq.products.options where localisation is not null";
				object rdr = da.DBExecuteReader(da.OpenDatabase(), sql__1);
				while ((rdr.Read)) {
					if (iq.i_SKU.ContainsKey(rdr("optsku"))) {
						object optionproduct__5 = iq.i_SKU(rdr("optsku"));

						string rgns = "";
						if (!IsDBNull(rdr.Item("localisation")))
							rgns = rdr.Item("localisation");

						if (rdr.Item("aaonly") != 0) {
							rgns += ",AA";
						}

						List<clsRegion> regions = new List<clsRegion>();
						if (rgns != "") {

							if (optionproduct__5 != null) {

								List<string> cs = Split(rgns, ",").ToList;

								//Anything paul has localized 'worldwide' needs no restriction
								if (!cs.Contains("XW")) {

									cleanRegions(cs, clsRegion.containment);

									foreach ( c in cs) {
										if (c == "UCSA")
											c = "USCA";
										//fix a typo
										if (iq.i_region_code.ContainsKey(c)) {
											regions.Add(iq.i_region_code(c));
										} else {
											Logit("invalid region " + c + " (in products.options.localisation)");
											//    Stop
										}
									}
								}
							}
						}
						//Create qty
						foreach ( branch in optionproduct__5.Branches) {
							if (branch.Quantities.Count == 0) {
								//For Each Path In branch.AllPaths
								foreach ( reg in regions) {
									object q = new clsQuantity(reg, "", branch, 0, 1, 0, false, null);
								}
								//Next
							}
						}

					}
				}
			case "FixProductLocalisation":

				object sql__1 = "select ModelSKU,ActiveSites,AAOnly from h3.iq.products.systems where ActiveSites is not null and modelsku='J8Q75EA'";
				object rdr = da.DBExecuteReader(da.OpenDatabase(), sql__1);


				while ((rdr.Read)) {

					if (iq.i_SKU.ContainsKey(rdr("ModelSKU"))) {
						object optionProduct__6 = iq.i_SKU(rdr("ModelSKU"));
						string rgns = rdr.Item("ActiveSites");

						if (rdr.Item("AAOnly") != 0) {
							rgns += ",AA";
						}

						List<clsRegion> regions = new List<clsRegion>();
						if (rgns != "") {

							if (optionProduct__6 != null) {
								List<string> regionList = Split(rgns, ",").ToList;

								cleanRegions(regionList, clsRegion.containment);

								foreach ( r in regionList) {
									if (r == "UCSA")
										r = "USCA";
									//fix a typo
									if (iq.i_region_code.ContainsKey(r)) {
										regions.Add(iq.i_region_code(r));
									} else {
										Logit("invalid region " + r + " (in products.systems.ActiveSites)");
										//    Stop
									}

								}
							}
						}

						//Create qty
						foreach ( branch in optionProduct__6.Branches) {
							if (branch.Quantities.Count == 0) {
								//For Each Path In branch.AllPaths
								foreach ( reg in regions) {
									object q = new clsQuantity(reg, "", branch, 0, 1, 0, false, null);
								}
								//Next
							}
						}

					}
				}
			case "ReorderL3s":
				//We are only interested in rack and power options, turns out we arent, we are interested in ALL L3's
				object con = da.OpenDatabase();

				List<string> SQLtoRun__7 = new List<string>();
				List<clsBranch> toRemove = new List<clsBranch>();

				List<string> errorMessages__3 = new List<string>();
				object dr = da.DBExecuteReader(con, "SELECT * FROM h3.iq.dbo.V2_OPtionCatsml WHERE L3 is not null");
				// = 'Rack &amp; Power'
				while (dr.Read) {
					if (Trim(dr("l3real")).ToLower == "data cables") {
						object a = 9;
					}
					if (iq.i_SKU.ContainsKey(dr("optsku"))) {
						clsProduct optionProduct__6 = iq.i_SKU(dr("optsku"));

						foreach ( branch in optionProduct__6.Branches) {
							//Make sure we dont move the system branches which are also options (swicthes etc)
							if (branch.AllPaths.Count > 0 && !optionProduct__6.isSystem(branch.AllPaths.First)) {
								bool success = false;

								//Ok here we are look at the branch above
								//Determine which level the L3 branch falls
								clsBranch l2Branch = null;
								clsBranch l3Branch = null;
								switch ("All Options") {
									case branch.Parent.Parent.Translation.text(English):
									//Its hung off an L1 branch, somethig wrong...!
									case branch.Parent.Parent.Parent != null ? branch.Parent.Parent.Parent.Translation.text(English) : "":
										//L2 so we need to move to L3
										l2Branch = branch.Parent;
									case branch.Parent.Parent.Parent != null && branch.Parent.Parent.Parent.Parent != null ? branch.Parent.Parent.Parent.Parent.Translation.text(English) : "":
										//L3 so check its the correct category
										l2Branch = branch.Parent.Parent;
										l3Branch = branch.Parent;
								}

								if (l2Branch != null && (l3Branch == null || l3Branch.Translation.text(English) != dr("L3real"))) {
									//We have a miscategorisation
									//Find the correct parent...

									foreach ( child in l2Branch.childBranches) {
										if (child.Value.Translation.text(English) == dr("L3real")) {
											//Reparent here
											branch.Parent = child.Value;
											//branch.Value.Update(errorMessages)
											SQLtoRun__7.Add("UPDATE branch SET FK_Branch_ID_Parent = " + child.Value.ID + " WHERE id=" + branch.ID);
											success = true;
											break; // TODO: might not be correct. Was : Exit For
										}
									}
									if (!success) {
										//Need to create one (no cache as it needs to be real time)
										object b = new clsBranch(null, l2Branch, iq.AddTranslation(dr("L3real"), English, "l3", 0, null, 0, false), "", iq.AddTranslation(dr("L3real"), English, "l3", 0, null, 0, false), iq.AddTranslation(dr("L3real"), English, "l3", 0, null, 0, false), null, 0, 0, "G");
										branch.Parent = b;
										//branch.Value.Update(errorMessages)
										SQLtoRun__7.Add("UPDATE branch SET FK_Branch_ID_Parent = " + b.ID + " WHERE id=" + branch.ID);
										success = true;
									}
									if (l3Branch != null)
										toRemove.Add(l3Branch);
								}
							}
						}
					}
				}

				da.DBExecutesql(con, string.Join(";", SQLtoRun__7));

				foreach ( r in toRemove) {
					if (r.childBranches.Count == 0) {
						r.delete(errorMessages__3);
					}
				}

			case "De-dupSlots":
				//Look through chassis and make sure we dont have duplicates of EXACT slots as that really shouldnt be.  so check slottype maj and min and sign of slot -/+
				List<string> errorMessages__3 = new List<string>();
				foreach ( p in iq.Products.Values) {
					if (p.isSystem) {
						foreach ( branch in p.Branches) {
							Dictionary<string, clsSlot> currentSlots = new Dictionary<string, clsSlot>();
							List<clsSlot> deleteSlots = new List<clsSlot>();
							foreach ( slot in branch.slots.Values) {
								if (!currentSlots.ContainsKey(slot.Type.MajorCode + "^" + slot.Type.MinorCode + "^" + Math.Sign(slot.numSlots) + "^" + slot.slotNum.value)) {
									currentSlots.Add(slot.Type.MajorCode + "^" + slot.Type.MinorCode + "^" + Math.Sign(slot.numSlots) + "^" + slot.slotNum.value, slot);
								} else {
									//Mark for deletion
									if (slot.Type.MajorCode != "PCI")
										deleteSlots.Add(slot);
								}
							}
							foreach ( child in branch.childBranches.Values) {
								if (child.Translation.text(English).Contains(" chassis")) {
									foreach ( slot in child.slots.Values) {
										if (!currentSlots.ContainsKey(slot.Type.MajorCode + "^" + slot.Type.MinorCode + "^" + Math.Sign(slot.numSlots) + "^" + slot.slotNum.value)) {
											currentSlots.Add(slot.Type.MajorCode + "^" + slot.Type.MinorCode + "^" + Math.Sign(slot.numSlots) + "^" + slot.slotNum.value, slot);
										} else {
											//Mark for deletion
											if (slot.Type.MajorCode != "PCI")
												deleteSlots.Add(slot);
										}
									}
								}
							}

							foreach ( slot in deleteSlots) {
								slot.delete(errorMessages__3);
							}
						}
					}
				}

			case "FixPCIMinorSlots":
				object Sql2 = "SELECT familyname, dedisku from h3.[iq].products.SysFamilyPCIslots where dedicated=1";
				object rdr2 = da.DBExecuteReader(da.OpenDatabase(), Sql2);
				Dictionary<string, List<string>> ded = new Dictionary<string, List<string>>();
				while ((rdr2.Read)) {
					if (!IsDBNull(rdr2("dedisku"))) {
						object s = Split(rdr2("dedisku"), ",");
						foreach ( sku in s) {
							if (!ded.ContainsKey(rdr2("familyname")))
								ded.Add(rdr2("familyname"), new List<string>());
							ded(rdr2("familyname")).Add(sku);
						}
					}
				}

				//
				object Sql__8 = "SELECT [SKU], [Code] ,[Notes]  FROM h3.[iQ].[products].[OptPCIcodes]";
				IDataReader rdr = da.DBExecuteReader(da.OpenDatabase(), Sql__8);
				List<string> errorMessages__3 = new List<string>();
				while (rdr.Read) {
					if (iq.i_SKU.ContainsKey(rdr("SKU"))) {
						if (rdr("sku") == "631670-B21") {
							object a = 9;
						}
						object product = iq.i_SKU(rdr("SKU"));
						string minCode = rdr("Code");

						minCode = Import.fixPci(minCode);

						foreach ( branch in product.Branches) {
							if (branch.AllPaths.Count > 0) {
								object fam = findFamily(branch.AllPaths(0));
								if (fam != "") {
									object minCode2 = minCode + "_" + ded.ContainsKey(fam) && ded(fam).Contains(rdr("sku")) ? 1 : 0;
									foreach ( slot in branch.slots.Values) {
										if (slot.Type.MajorCode.ToUpper.StartsWith(Import.ConvertPCIMinorToMajor(minCode2).ToUpper)) {
											if (!iq.i_slotType_Code(slot.Type.MajorCode).ContainsKey(minCode2)) {
												object st = new clsSlotType(slot.Type.MajorCode, minCode2, iq.AddTranslation(Import.ExpandPCI(minCode2).fullText, English, "PCIST", 0, null, 0, false));
											}

											slot.Type = iq.i_slotType_Code(slot.Type.MajorCode)(minCode2);
											slot.update(errorMessages__3);
										}
									}
								}
							}
						}
					}

				}
			case "Fallbacks":
				//probably need to consider dedicated slots more carefully
				object sql__1 = "delete from altslottype";
				object s = da.DBExecutesql(da.OpenDatabase(), sql__1);
				pciStruct ospec;
				pciStruct ispec;
				foreach ( ko in iq.SlotTypes.Keys) {
					if (iq.SlotTypes(ko).MinorCode == "PCIE_C8B8_HXHY_G2_0") {
						object a = -9;
					}
					if (iq.SlotTypes(ko).Fallback != null)
						iq.SlotTypes(ko).Fallback.Clear();
					//necessary (had to hack it in during an import)
					if (UBound(Split(iq.SlotTypes(ko).MinorCode, "_")) == 4) {
						ospec = ExpandPCI(iq.SlotTypes(ko).MinorCode);
						if (!ospec.dedicated) {
							foreach ( ki in iq.SlotTypes.Keys) {
								if (iq.SlotTypes(ki).MinorCode == "PCIE_C16B8_HXFY_G3_0") {
									object a = -9;
								}
								if (!ko == ki) {
									if (Left(iq.SlotTypes(ko).MajorCode, 3) == "PCI" && Left(iq.SlotTypes(ki).MajorCode, 3) == "PCI" && iq.SlotTypes(ki).MinorCode == "GEN") {
										object kos = 0;
										if (iq.SlotTypes(ko).MajorCode.Length > 3)
											kos = Asc(Right(iq.SlotTypes(ko).MajorCode, 1));
										else
											kos = 0;
										object kis = 0;
										if (iq.SlotTypes(ki).MajorCode.Length > 3)
											kis = Asc(Right(iq.SlotTypes(ki).MajorCode, 1));
										else
											kis = 0;

										if (kis >= kos)
											iq.SlotTypes(ko).AddFallback(iq.SlotTypes(ko).Fallback.Count, iq.SlotTypes(ki));
									} else {
										//necessary (had to hack it in during an import)
										if (UBound(Split(iq.SlotTypes(ki).MinorCode, "_")) == 4) {
											//If ki > ko Then
											ispec = ExpandPCI(iq.SlotTypes(ki).MinorCode);

											//same 'technology' (PCIe/x/BLcm
											if (ispec.tech == ospec.tech | (Left(ispec.tech, 3) == "PCI" && Left(ospec.tech, 3) == "PCI")) {
												//slotform 1,8,16 - only add higher connector width slots
												if (ispec.connector >= ospec.connector) {
													//Speed 4x 8x 16x - only use higher speed slots
													if (ispec.speed >= ospec.speed) {
														//only an alternative if same or wider AND same or higher
														if (ispec.w >= ospec.w & ispec.h >= ospec.h) {
															 // ERROR: Not supported in C#: WithStatement

														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}

			case "ScanFIOs":

				object toUpdate__9 = {
					"PSU",
					"RAM",
					"CPU"
				};

				string ck = "";
				Dictionary<string, clsBranch> lDic = new Dictionary<string, clsBranch>();
				foreach ( branch in iq.RootBranch.childBranches) {
					if (branch.Value.Translation.text(English).ToLower == "accessories and services") {
						branch.Value.BuildPathDic("", lDic, false);
						break; // TODO: might not be correct. Was : Exit For
					}
				}

				object con = da.OpenDatabase();
				object failed = new List<string>();
				Dictionary<clsProduct, List<clsRegion>> dicOptLocalization = new Dictionary<clsProduct, List<clsRegion>>();
				Import.LoadAbbreviations(con);

				List<string> missing = new List<string>();
				object sql__1 = "select modelsku,psu,psuqty,ram,ramqty,cpu,cpuqty from h3.iq.products.systems where psu is not null or ram is not null or cpu is not null";
				object rdr = da.DBExecuteReader(da.OpenDatabase, sql__1);
				List<string> errorMEssages__10 = new List<string>();
				while (rdr.Read) {
					if (iq.i_SKU.ContainsKey(rdr("modelsku"))) {
						object system_product = iq.i_SKU(rdr("modelsku"));

						foreach ( opttype in toUpdate__9) {
							if (IsDBNull(rdr(opttype)) || rdr(opttype) == "EXT")
								continue;
							if (rdr(opttype) == "###MSM525_AP_IOC") {
								object a = 9;
							}

							if (!iq.i_SKU.ContainsKey(rdr(opttype).trim())) {
								//Product missing totally!
								//Create it?
								//Find the alloptions branch?

								missing.Add(string.Format("Model: {0} missing {1}{2}", rdr("ModelSKU"), rdr(opttype), Environment.NewLine));

								string column = opttype;
								//for clarity !! - will be psu ram ir cpu
								if (failed.Contains(rdr(column)))
									continue;

								List<string> imp = new List<string>();
								//import the missing CPU/RAM/PSU
								imp.Add(rdr.Item(column).trim);

								//These are NOT the system options (it's the accessories catalogue)
								if (Import.optionsIncremental(da.OpenDatabase(), imp, iq.i_unit_code, lDic, dicOptLocalization, null, true) == null) {
									failed.Add(rdr(opttype));
									continue;
								}

								if (!iq.i_SKU.ContainsKey(rdr(opttype))) {
									failed.Add(rdr(opttype));
									missing.Add(string.Format("Model: {0} failed to create {1}{2}", rdr("ModelSKU"), rdr(opttype).trim(), Environment.NewLine));
									continue;
								}

								missing.Add(string.Format("Model: {0} added {1}{2}", rdr("ModelSKU"), rdr(opttype), Environment.NewLine));
							}
							object psu_product = iq.i_SKU(rdr(opttype).trim());

							foreach ( branch in system_product.Branches) {
								foreach ( Path__4 in branch.AllPaths) {
									string newpath = "";
									if (object.ReferenceEquals(branch.FindSystemAbove(Path__4, newpath), branch)) {
										//Ok, lets find the quantity record...

										Dictionary<clsBranch, List<string>> validPaths = new Dictionary<clsBranch, List<string>>();


										foreach ( optBranch in psu_product.Branches) {
											foreach ( optPAth in optBranch.AllPaths) {
												if (optPAth.Contains(Path__4)) {
													//This is in here because there appear to be more than one branch for the same SKU under FIO's in [some] cases
													if (!validPaths.ContainsKey(optBranch))
														validPaths.Add(optBranch, new List<string>());
													validPaths(optBranch).Add(optPAth);

												}
											}
										}
										if (validPaths.Count == 0) {
											//Not mapped under this tree
											//Create a branch for it in all options / FIOs
											object ao = branch.ChildNamed("All Options");
											object fios = ao.ChildNamed("FIOs");
											clsBranch nb = new clsBranch(psu_product, fios, iq.AddTranslation(psu_product.SKU, English, "SKU", 0, null, 0, true), "", iq.AddTranslation(psu_product.SKU, English, "SKU", 0, null, 0, true), iq.AddTranslation(psu_product.SKU, English, "SKU", 0, null, 0, true), null, 0, false, "B",
											null, 0);
											validPaths.Add(nb, new List<string> { Path__4 + "." + ao.ID + "." + fios.ID + "." + nb.ID });
											missing.Add(string.Format("Model: {0} added fio branch {1}{2}", rdr("ModelSKU"), rdr(opttype), Environment.NewLine));
										}
										if (validPaths.Keys.SelectMany(vp => vp.Quantities).Where(qr => validPaths(qr.Value.Branch).Contains(qr.Value.Path) | string.IsNullOrEmpty(qr.Value.Path) & qr.Value.NumPreInstalled == IsDBNull(rdr(opttype + "qty")) ? 1 : (int)rdr(opttype + "qty")).Count == 0) {
											//Create it
											//Brutal now, if there is another preinstalled of a different sku and the same type remove it (has to be really as IQ1 only has one field for this)
											//Get the holder for this item
											object bb = iq.Branches(validPaths.First.Value.First.Split(".")(validPaths.First.Value.First.Split(".").Length - 1));
											object qtys = bb.childBranches.Values.SelectMany(cb => cb.Quantities).Where(qr => validPaths(qr.Value.Branch).Contains(qr.Value.Path) | string.IsNullOrEmpty(qr.Value.Path) & qr.Value.NumPreInstalled > 0);
											foreach ( qtr in qtys) {
												qtr.Value.Delete(errorMEssages__10);
												missing.Add(string.Format("Model: {0} deleted quantity record for {1}{2}", rdr("ModelSKU"), rdr(opttype), Environment.NewLine));
											}
											missing.Add(string.Format("Model: {0} added quantity record for {1}{2}", rdr("ModelSKU"), rdr(opttype), Environment.NewLine));
											object q = new clsQuantity(iq.i_region_code("XW"), validPaths.First.Value.First, validPaths.First.Key, IsDBNull(rdr(opttype + "qty")) ? 1 : (int)rdr(opttype + "qty"), 0, 0, true, null);
										}
									}
								}
							}
						}
					}
				}
				retString = Join(missing.ToArray, "".ToArray);
			case "RemoveGENs":
				List<string> errorMessages__3 = new List<string>();
				foreach ( branch in iq.Branches.Values) {
					//Dim branch = iq.Branches(2593)
					if (branch.Product != null && branch.Product.isSystem) {
						if (branch.AllPaths.Count > 0) {
							object slots = branch.slots.ToList();
							clsBranch cb;
							foreach ( child in branch.childBranches.Values) {
								if (child.Translation.text(English).Contains(" chassis")) {
									slots.AddRange(child.slots.ToList());
									break; // TODO: might not be correct. Was : Exit For
								}
							}


							object todelete__11 = new List<clsSlot>();
							foreach ( path__2 in branch.AllPaths) {
								foreach ( slot in slots) {
									if (slot.Value.Type.MajorCode.StartsWith("PCI")) {
										object a = 9;
									}
									object mc = Import.ConvertPCIMinorToMajor(slot.Value.Type.MinorCode);
									if ((slot.Value.path.Contains(path__2) || slot.Value.path == "") && ((slot.Value.Type.MajorCode.StartsWith("PCI") && mc == slot.Value.Type.MinorCode) | !slot.Value.Type.MajorCode.StartsWith("PCI"))) {
										if (slots.Where(st => (st.Value.path == "" || st.Value.path.Contains(path__2)) && st.Value.Type.MajorCode == slot.Value.Type.MajorCode && st.Value.Type.MinorCode != slot.Value.Type.MinorCode).Sum(st => st.Value.numSlots) == slot.Value.numSlots) {
											retString += "Branch (SKU): " + branch.ID + "(" + branch.Product.SKU + ") Removed Slot " + slot.Value.displayName(English) + " x " + slot.Value.numSlots + Environment.NewLine;
											todelete__11.Add(slot.Value);
										}
									}
								}
							}
							foreach ( sl in todelete__11) {
								sl.delete(errorMessages__3);
							}
						}
					}
				}

			case "SoftwareSpec":
				object nextTKey = 0;
				object con = da.OpenDatabase();
				object pawc = da.MakeWriteCacheFor(con, "ProductAttribute");
				object twc = da.MakeWriteCacheFor(con, "Translation", nextTKey, true);

				object Sql__8 = "SELECT modelsku,isnull(translation,software) as software FROM h3.iq.products.systems left outer join h3.iq.dbo.Abbreviations a on a.code=systems.software where software is not null";
				IDataReader rdr = da.DBExecuteReader(da.OpenDatabase(), Sql__8);
				List<string> errorMessages__3 = new List<string>();
				while (rdr.Read) {
					if (iq.i_SKU.ContainsKey(rdr("modelsku"))) {
						object prod = iq.i_SKU(rdr("modelsku"));
						if (!prod.i_Attributes_Code.ContainsKey("software")) {
							object at = new clsProductAttribute(prod, iq.i_attribute_code("software"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr("software"), English, "sw", 0, twc, nextTKey, true), pawc);
						} else {
							prod.i_Attributes_Code("software").First.Translation.text(English) = rdr("software");
							prod.i_Attributes_Code("software").First.Translation.Update(English);
						}
					}
				}

				da.BulkWrite(con, twc, "Translation");
				da.BulkWrite(con, pawc, "ProductAttribute");
			case "Expand OtherFeatures":
				object Sql__8 = "SELECT code,translation from h3.iq.dbo.Abbreviations";
				IDataReader rdr = da.DBExecuteReader(da.OpenDatabase(), Sql__8);
				List<string> errorMessages__3 = new List<string>();
				object Abrs = new Dictionary<string, string>();
				while (rdr.Read) {
					Abrs.Add(rdr("code"), rdr("translation"));
				}

				foreach ( prod in iq.Products.Values) {
					if (prod.i_Attributes_Code.ContainsKey("options")) {
						object s = Split(prod.i_Attributes_Code("options").First.Translation.text(English), ",");
						object nw = new List<string>();
						foreach ( element in s) {
							if (Abrs.ContainsKey(element)) {
								nw.Add(Abrs(element));
							} else {
								nw.Add(element);
							}
						}
						prod.i_Attributes_Code("options").First.Translation.text(English) = Join(nw.ToArray, ",");
						prod.i_Attributes_Code("options").First.Translation.Update(English);
					}
				}

			case "Split HK Support Screens":
				//this one is a rather specific one off for HMC...
				List<string> errorMEssages__10 = new List<string>();
				foreach ( branch in iq.Branches.Values) {
					if (branch.Translation.text(English) == "HW Support") {
						foreach ( Path__4 in branch.AllPaths) {
							object newpath = "";
							object sysunit = branch.FindSystemAbove(Path__4, newpath);
							if (sysunit != null) {
								if ({
									"DTO",
									"NBK"
								}.Contains(sysunit.Product.ProductType.Code)) {
									branch.Matrix = iq.Screens(1812);
									branch.Update(errorMEssages__10);
								}

							}
						}

					}
				}

			case "RemoveGENPCIs":
				object errormessages__12 = new List<string>();
				foreach ( branch in iq.Branches.Values) {
					List<clsSlot> todelete__11 = new List<clsSlot>();
					if (branch.Product != null && branch.Product.isSystem) {
						clsBranch chassisBranch;
						foreach ( child in branch.childBranches.Values) {
							if (child.Translation.text(English).Contains(" chassis")) {
								chassisBranch = child;
								break; // TODO: might not be correct. Was : Exit For
							}
						}
						foreach ( Path__4 in branch.AllPaths) {
							foreach ( slot in chassisBranch.slots.Values) {
								if ((string.IsNullOrEmpty(slot.path) || slot.path == Path__4)) {
									if (slot.Type.MajorCode.StartsWith("PCI") && Import.ConvertPCIMinorToMajor(slot.Type.MinorCode) == slot.Type.MinorCode) {
										if (branch.slots.Values.Where(bs => (string.IsNullOrEmpty(bs.path) || bs.path == Path__4) && bs.Type.MajorCode.StartsWith("PCI")).Count > 0) {
											todelete__11.Add(slot);
										}
									}
								}
								if ((string.IsNullOrEmpty(slot.path) || slot.path == Path__4) && slot.Type.MajorCode == "CPU") {
									todelete__11.Add(slot);
									// Don't want any CPU's on the chassis!
								}
							}
						}
					}
					foreach ( std in todelete__11) {
						std.delete(errormessages__12);
					}
				}

			case "CheckAutoAdds":
				object sql__1 = "SELECT [CountryCode],a.[ModelSKU],[AddSKU],[OptType],s.FamilyCode,a.ranking from h3.[iq].[Products].[AutoAdds] a inner join h3.iq.products.Systems s on a.modelsku=s.ModelSKU where opttype<>'WTY' and ranking = 1";
				IDataReader rdr = da.DBExecuteReader(da.OpenDatabase(), sql__1);
				List<string> errorMessages__3 = new List<string>();
				while (rdr.Read) {
					if (iq.i_SKU.ContainsKey(rdr("modelsku"))) {
						clsProduct system = iq.i_SKU(rdr("modelsku"));
						foreach ( systemBranch in system.Branches) {
							foreach ( Path__4 in systemBranch.AllPaths) {
								object partPAth = "";
								object part = systemBranch.findChildBySKU2(Path__4, rdr("addsku"), partPAth);
								if (part != null) {
									if (part.Quantities.Values.Where(q => (string.IsNullOrEmpty(q.Path) || q.Path == partPAth) && q.NumPreInstalled > 0 && q.Region.Encompasses(iq.i_region_code(rdr("CountryCode").replace("UK", "GB"))) && q.FOC == false).Count == 0) {
										object q = new clsQuantity(iq.i_region_code(rdr("countrycode").replace("UK", "GB")), partPAth, part, 1, 0, 0, false, null);
									}
								}
							}
						}
					}
				}
			case "RemoveCPQs":
				object con = da.OpenDatabase();
				List<string> errorMessages__3 = new List<string>();
				object ToDelete__13 = new List<Int32>();
				List<Int32> torun = new List<Int32>();
				foreach ( branch in iq.i_SpecialBranches("cpqroot").childBranches) {
					//Grafts and prunes


					ToDelete__13.Add(branch.Value.ID);
					torun.Add(branch.Value.ID);
				}

				//Split it!



				if (torun.Count > 0) {

					da.DBExecutesql(con, "DELETE FROM graft WHERE FK_Branch_ID_Source IN (" + Join(ToDelete__13.Select(f => f.ToString()).ToArray, ",") + ") OR FK_Branch_ID_Target IN (" + Join(ToDelete__13.Select(f => f.ToString()).ToArray, ",") + ")");
					while (torun.Count > 0) {
						object thischunk = torun.Take(10);
						da.DBExecutesql(con, "DELETE FROM prune WHERE CALC_BRANCH_ID in (" + Join(thischunk.Select(f => f.ToString()).ToArray, ",") + ")");
						da.DBExecutesql(con, "DELETE pa FROM productattribute pa inner join branch on branch.fk_product_id=pa.fk_product_id WHERE branch.id in (" + Join(thischunk.Select(f => f.ToString()).ToArray, ",") + ")");

						da.DBExecutesql(con, "DELETE FROM slot WHERE fk_branch_id in (" + Join(thischunk.Select(f => f.ToString()).ToArray, ",") + ")");
						da.DBExecutesql(con, "SELECT p.id INTO #tmp FROM product p inner join branch b on b.fk_product_id=p.id WHERE b.id in (" + Join(thischunk.Select(f => f.ToString()).ToArray, ",") + ")" + "delete from quoteitem where fk_branch_id in (" + Join(thischunk.Select(f => f.ToString()).ToArray, ",") + ");" + "delete from quantity where FK_branch_ID in (" + Join(thischunk.Select(f => f.ToString()).ToArray, ",") + ");" + "delete b FROM branch b WHERE b.id in (" + Join(thischunk.Select(f => f.ToString()).ToArray, ",") + ");" + "delete s from stock s inner join variant on fk_variant_id=variant.id where fk_product_id in (select id from #tmp);" + "delete from variant where FK_Product_ID in (select id from #tmp);" + "delete from product where id in (select id from #tmp);" + "drop table #tmp");
						//da.DBExecutesql(con, "DELETE FROM branch WHERE id in (" & Join(thischunk.Select(Function(f) f.ToString()).ToArray, ",") & ")")

						torun.RemoveAll(m => thischunk.Contains(m));
					}

				}
			//For Each td In ToDelete
			//    Dim todelattr = New List(Of clsProductAttribute)
			//    For Each pa In iq.Branches(td).Product.Attributes.Values
			//        todelattr.Add(pa)
			//    Next
			//    For Each tdp In todelattr
			//        tdp.delete(errorMessages)
			//    Next
			//    iq.Branches(td).Product.delete(errorMessages)
			//    Dim todelslot = New List(Of clsSlot)
			//    For Each slot In iq.Branches(td).slots.Values
			//        todelslot.Add(slot)
			//    Next
			//    For Each tds In todelslot
			//        tds.delete(errorMessages)
			//    Next
			//    iq.Branches(td).delete(errorMessages)
			//Next
			case "FixSystemasOptionL3ReorderCockup":
				object con = da.OpenDatabase();
				object sqltorun__14 = new List<string>();
				object dr = da.DBExecuteReader(con, "SELECT * FROM h3.iq.dbo.V2_OPtionCatsml WHERE L3 is not null");
				// = 'Rack &amp; Power'
				while (dr.Read) {
					if (iq.i_SKU.ContainsKey(dr("optsku"))) {
						object prod = iq.i_SKU(dr("optsku"));
						foreach ( branch in prod.Branches) {
							if (branch.Product != null) {
								foreach ( Path__4 in branch.AllPaths) {
									if (branch.Product.isSystem(Path__4)) {
										//We have one here which we shouldnt have moved!
										//Find where it should be...
										object cat = branch.Parent.Parent;
										//HP Networking or the like
										foreach ( childbranch in cat.childBranches.Values) {
											if (childbranch.Product != null && childbranch.Product.i_Attributes_Code.ContainsKey("FamMajor")) {

												if (prod.i_Attributes_Code.ContainsKey("FamMajor")) {
													if (childbranch.Product.i_Attributes_Code("FamMajor").First.Translation.text(English) == prod.i_Attributes_Code("FamMajor").First.Translation.text(English)) {
														//Found it, reparent
														branch.Parent = childbranch;
														sqltorun__14.Add("UPDATE branch SET FK_Branch_ID_Parent = " + childbranch.ID + " WHERE id=" + branch.ID);
													}
												} else {
													if (prod.i_Attributes_Code.ContainsKey("FamMinor")) {
														if (childbranch.childBranches.Count > 0 && childbranch.childBranches.Values.First.Product.i_Attributes_Code("FamMinor").First.Translation.text(English) == prod.i_Attributes_Code("FamMinor").First.Translation.text(English)) {
															//Found it, reparent
															branch.Parent = childbranch;
															sqltorun__14.Add("UPDATE branch SET FK_Branch_ID_Parent = " + childbranch.ID + " WHERE id=" + branch.ID);
														} else {
															retString += " Cannot place: " + prod.SKU + Environment.NewLine;
														}
													}
												}
											}
										}

									}
								}
							}
						}
					}
				}
				if (sqltorun__14.Count > 0)
					da.DBExecutesql(con, string.Join(";", sqltorun__14));
			case "AddFIODependantSlotsToChassis":
				//Scan the system to see if there are any FIO's which cause instant slot validations (shouldn't happen EVER)
				object errorMessages__3 = new List<string>();
				foreach ( branch in iq.Branches.Values) {
					if (branch.Product != null && branch.Product.isSystem) {
						clsBranch cb;
						foreach ( childbranch in branch.childBranches.Values) {
							if (childbranch.Translation.text(English).Contains(" chassis")) {
								cb = childbranch;
								break; // TODO: might not be correct. Was : Exit For
							}
						}

						foreach ( Path__4 in branch.AllPaths) {
							object sl = branch.slotsInForce(Path__4).ToDictionary(sif => sif, sif => 1);
							Dictionary<clsSlot, Int32> sl2 = new Dictionary<clsSlot, int>();
							foreach ( fio in branch.GetPreInstalledRecursive(iq.i_region_code("XW"), Path__4, errorMessages__3)) {
								foreach ( slot in fio.Branch.slots.Values) {
									if (string.IsNullOrEmpty(slot.path) || slot.path.Contains(Path__4)) {
										//Check for weird ones (like systems with systems)
										if (!sl.ContainsKey(slot) & !sl2.ContainsKey(slot)) {
											sl2.Add(slot, fio.NumPreInstalled);
										} else {
											retString += "Unable to add all slots, odd config for: " + branch.Translation.text(English) + Environment.NewLine;
										}
									}
								}
							}
							foreach ( slot in sl) {
								if (slot.Key.Type.MajorCode != "PWR" && (sl.Where(inf => object.ReferenceEquals(inf.Key.NonStrictType, slot.Key.NonStrictType)).Sum(inf => inf.Key.numSlots) + sl2.Where(inf => object.ReferenceEquals(inf.Key.NonStrictType, slot.Key.NonStrictType)).Sum(inf => inf.Key.numSlots)) < 0) {
									//Is it validated, do we care?
									object vi = iq.ValidationInclusions.Where(vai => vai.Value.MajorCode.ToLower == slot.Key.Type.MajorCode.ToLower).FirstOrDefault;
									if (vi.Value != null && vi.Value.InclusionType == enumInclusionType.Validated) {
										object a = 9;
										object sq = -sl.Where(inf => object.ReferenceEquals(inf.Key.NonStrictType, slot.Key.NonStrictType)).Sum(inf => inf.Key.numSlots);
										object slo = new clsSlot(slot.Key.Type, cb, "", sq, null, new NullableInt(), 0, 0, null);
										sl2.Add(slo, 1);
										retString += "Added slot for: " + branch.Translation.text(English) + " of type: " + slot.Key.Type.MajorCode + " qty " + sq + Environment.NewLine;
									}
								}

							}
						}


					}
				}

			case "ScanForUnattachedQuantites":
				object errormessages__12 = new List<string>();
				foreach ( branch in iq.Branches.Values) {
					object todelete__11 = new List<clsQuantity>();
					object toupdate__15 = new List<clsQuantity>();
					foreach ( quantity in branch.Quantities.Values) {
						if (!string.IsNullOrEmpty(quantity.Path)) {
							if (!branch.AllPaths.Contains(quantity.Path)) {
								//Whoa orphaned quantity then... what to do
								//Well, check we still have the info on its... replacement?  how do we know where its replacement is?

								if (branch.Product.ProductType.Code.ToLower == "wty") {
									todelete__11.Add(quantity);
								} else {
									//Check last but one bit
									object segs = Split(quantity.Path, ".");
									object holdingseg = segs(segs.Length - 2);
									object newpath = "";
									foreach ( Path__4 in branch.AllPaths) {
										object bpsegs = Split(Path__4, ".");
										for (i = 0; i <= bpsegs.Length; i++) {
											if (bpsegs(i) != segs(i)) {
												if (i == bpsegs.Length - 2) {
													//We have a possible l3 change...
													if (bpsegs.Last == segs.Last) {
														newpath = Path__4;
														//Left(Path, Path.IndexOf(bpsegs(i))) & segs(i) & "." & segs.Last
														break; // TODO: might not be correct. Was : Exit For
													}
												} else {
													break; // TODO: might not be correct. Was : Exit For
												}
											}
										}
										if (newpath != "") {
											if (branch.Quantities.Values.Where(q => q.Path == newpath).Count > 0) {
												object a = 9;
											}
											quantity.Path = newpath;
											toupdate__15.Add(quantity);

											//Update?
											break; // TODO: might not be correct. Was : Exit For
										}
									}
									if (newpath == "") {
										//Not found a replacement
										object a = 9;
									}
								}
							}
						}
					}
					foreach ( u in toupdate__15) {
						u.update(errormessages__12);
					}
					foreach ( t in todelete__11) {
						t.Delete(errormessages__12);
					}
				}

			case "AddCarePackSlots":
				foreach ( branch in iq.Branches.Values) {
					if (branch.Product != null && branch.Product.isSystem) {
						if ({
							"SWD",
							"HPN"
						}.Contains(branch.Product.ProductType.Code)) {
							clsBranch cb;
							foreach ( childbranch in branch.childBranches.Values) {
								if (childbranch.Translation.text(English).Contains(" chassis")) {
									cb = childbranch;
									break; // TODO: might not be correct. Was : Exit For
								}
							}
							if (cb.slots.Values.Where(sl => sl.Type.MajorCode.ToUpper == "WTY").Count == 0) {
								object slt = new clsSlot(iq.i_slotType_Code("WTY")("CAREPACK"), cb, "", 3, null, new NullableInt(), 0, 0);
							}
						}
					}
				}

			case "RemoveMedSlots":
				object errormessages__12 = new List<string>();
				foreach ( b in iq.Branches.Values) {
					if (b.Product != null && !b.Product.isSystem) {
						if (b.Product.ProductType.Code == "MED") {
							if (b.i_Slots.ContainsKey("MED")) {
								object hasSlots = new List<clsSlotType>();
								object todelete__11 = new List<clsSlot>();
								foreach ( sl in b.slots.Values) {
									if (sl.numSlots > 0) {
										todelete__11.Add(sl);
									} else {
										if (hasSlots.Contains(sl.Type)) {
											todelete__11.Add(sl);
										} else {
											hasSlots.Add(sl.Type);
										}
									}
								}
								foreach ( d in todelete__11) {
									d.delete(errormessages__12);
								}
							}
						}
					}
				}

		}

		txtOutput.Text = retString;
	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button39_Click(object sender, EventArgs e)
	{
		List<string> ErrorMessages = new List<string>();
		Response.Write("DONE:" + Import.Tokens(ErrorMessages));
		OutputErrors(Form.Controls, ErrorMessages, 0, true);

	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnSetCur_Click(object sender, EventArgs e)
	{
		List<string> errs = new List<string>();


		clsCurrency c;
		if (iq.i_currency_code.ContainsKey(TxtCurr.Text)) {
			c = iq.i_currency_code(TxtCurr.Text);

			if (txtHost4Curr.Text != "") {
				Import.fixCurrencies(txtHost4Curr.Text, c, errs);

			} else {
				errs.Add("Please enter a hostid");
			}


		} else {
			errs.Add("No such currency");
		}

		OutputErrors(Panel2.Controls, errs, 0);
	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button40_Click(object sender, EventArgs e)
	{
		List<string> errormessages = new List<string>();
		Import.clones(errormessages);

		foreach ( Er in errormessages) {
			Form.Controls.Add(ErrorDymo(Er));
		}


	}

	protected void  // ERROR: Handles clauses are not supported in C#
btnExpScreen_Click(object sender, EventArgs e)
	{
		List<string> err;
		clsScreen screen = iq.i_screens_code("HDD").copy(err);
		screen.title = "Export CSV";
		screen.code = "ExCSV";
		clsField screenfield = new clsField(screen, "Product.ProductType.Code", "", iq.AddTranslation("Product Type Code", English, "FLDLBL", 1, null, 0, false), "Product Type Code", null, iq.i_inputType_code("string"), 10, 1, 10,
		10, "", true, false, "", "", 1, null, "", false,
		null, true);
		clsField screenfield2 = new clsField(screen, "Product.ProductType.Translation", "", iq.AddTranslation("Product Type Name", English, "FLDLBL", 1, null, 0, false), "Product Type Name", null, iq.i_inputType_code("string"), 10, 1, 10,
		10, "", true, false, "", "", 1, null, "", false,
		null, true);
		screen.Update(err);
		string strfieldsnotrequired = "Product.i_Attributes_Code(capacity)(0)\tProduct.i_Attributes_Code(speed)(0)\tParent.Translation\tParent.Parent.Translation";

		foreach ( fld in screen.Fields.Values) {
			if (strfieldsnotrequired.Contains(fld.displayName(English))) {
				fld.visibleList = false;
			} else {
				switch (fld.displayName(English)) {
					case "Product.ProductType.Code":
						fld.order = 1;
					case "Product.ProductType.Translation":
						fld.order = 10;
					case "Product.i_Attributes_Code(MfrSKU)(0)":
						fld.order = 15;
					case "Product.i_Attributes_Code(Desc)(0)":
						fld.order = 20;
					case "Stock":
						fld.order = 25;
					case "CustomerPrice":
						fld.order = 30;
				}
			}
			fld.update(err);

		}

	}

	protected void  // ERROR: Handles clauses are not supported in C#
Button41_Click(object sender, EventArgs e)
	{
		List<clsProduct> psu = (from p in iq.Products.Valueswhere p.ProductType.Code.ToUpper == "PSU").ToList();

		string stringPsu;
		foreach ( psuUnit in psu) {
			if (psuUnit.i_Attributes_Code.ContainsKey("Desc")) {
				if (psuUnit.i_Attributes_Code.ContainsKey("capacity")) {
					stringPsu = stringPsu + psuUnit.SKU + " : TRUE :" + psuUnit.i_Attributes_Code("Desc")(0).Translation.text(English) + ": " + psuUnit.i_Attributes_Code("capacity")(0).NumericValue + " <br/>";

				} else {
					stringPsu = stringPsu + psuUnit.SKU + " : FALSE :" + psuUnit.i_Attributes_Code("Desc")(0).Translation.text(English) + "<br/>";
				}
			} else {
				stringPsu = stringPsu + psuUnit.SKU + "<br/>";
			}


		}
		Literal1.Text = "<br/>" + stringPsu;

	}

	protected void  // ERROR: Handles clauses are not supported in C#
btnFixOS_Click(object sender, EventArgs e)
	{
		UInt64 lid = 0;
		UInt64.TryParse(Request.QueryString("lid"), lid);
		OSAutoAdd.fixAutoAdds(lid);
	}



	protected void  // ERROR: Handles clauses are not supported in C#
btnCpkReport_Click(object sender, EventArgs e)
	{
		UInt64 lid = 0;
		UInt64.TryParse(Request.QueryString("lid"), lid);
		List<clsSysCarePack> sysList = OSAutoAdd.CarePackReports(lid);
		Response.Clear();
		Response.Buffer = true;
		Response.AddHeader("content-disposition", "attachment;filename=CarePAckAutoAddReport.csv");
		Response.Charset = "";
		Response.ContentType = "application/text";

		StringBuilder sb = new StringBuilder();
		//For k As Integer = 0 To sysList.
		//    'add separator
		//    sb.Append(dt.Columns(k).ColumnName + ","c)
		//Next
		sb.Append("System SKU, System Desc, AutoAdd Carepack Sku, Carepack Desc");
		//append new line
		sb.Append(vbCr + vbLf);
		foreach ( sys in sysList) {
			//add separator
			sb.Append(sys.sysSkus + "," + sys.sysDesc + "," + sys.carepackSku + "," + sys.carePackDesc);
			//append new line
			sb.Append(vbNewLine);
		}
		Response.Output.Write(sb.ToString());
		Response.Flush();
		Response.End();
	}
	protected void  // ERROR: Handles clauses are not supported in C#
cmdFixCarePack_Click(object sender, EventArgs e)
	{
		UInt64 lid = 0;
		UInt64.TryParse(Request.QueryString("lid"), lid);
		OSAutoAdd.FixCarePacks(lid);
	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnManufacturer_Click(object sender, EventArgs e)
	{
		Import.Manufacturer();

	}

	protected void  // ERROR: Handles clauses are not supported in C#
Button42_Click(object sender, EventArgs e)
	{
		fixTranslations();


	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button43_Click(object sender, EventArgs e)
	{
		fixFilterDefaults();
	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button44_Click(object sender, EventArgs e)
	{
		StreamWriter sw = new StreamWriter("c:\\temp\\mem.txt");
		foreach ( branch in iq.Branches.Values) {
			if (branch.Product != null) {
				if (branch.Product.isSystem) {
					clsProduct product = branch.Product;

					int cpus = 0;
					int mem = 0;
					foreach ( slot in branch.slots.Values) {
						if (slot.Type.MajorCode == "CPU") {
							if (slot.numSlots < 1 | slot.numSlots > 4)
								System.Diagnostics.Debugger.Break();
							foreach ( cb in branch.childBranches.Values) {
								if (branch.Translation.text(English).ToLower.Contains("chassis")) {
									foreach ( cs in cb.slots.Values) {
										if (cs.Type.MajorCode == "MEM") {
											Beep();
										}
									}
								}
							}
						}
						cpus += slot.numSlots;
						if (slot.Type.MajorCode == "MEM")
							mem += slot.numSlots;
					}

					if (mem > 0 & cpus > 0) {
						sw.WriteLine(product.SKU + " " + product.i_Attributes_Code("desc")(0).Translation.text(English) + " has " + mem + " mem & " & " & cpus & " + cpus);
					}

					//Return Me.Type.MajorCode & "_" & Me.Type.MinorCode & "_" & Me.path & "_" & Math.Sign(Me.numSlots) & "_" & Me.slotNum.sqlvalue

				}
			}
		}

		sw.Close();

	}

	protected void  // ERROR: Handles clauses are not supported in C#
Button45_Click(object sender, EventArgs e)
	{
		string filename = MapPath("CarePackTablesv3test.xls");
		string connectionString = string.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0}; Extended Properties=Excel 12.0;", filename);
		DataTable dtSheets;
		using (OleDbConnection objConn = new OleDbConnection(connectionString)) {
			objConn.Open();
			dtSheets = objConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] {
				null,
				null,
				null,
				"TABLE"
			});
		}
		DataSet ds = new DataSet();
		foreach ( row in dtSheets.Rows) {
			OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT * FROM [" + row("TABLE_NAME").ToString() + "]", connectionString);
			adapter.Fill(ds, row("TABLE_NAME").ToString());

		}
		DataTable dtServiceLevelsMap = ds.Tables("ServiceLevelsMap$");
		DataTable dtServiceType = ds.Tables("ServiceType$");
		DataTable dtResponse = ds.Tables("Response$");
		DataTable dtTROAA = ds.Tables("TROAA$");
		DataTable dtServiceOptions = ds.Tables("ServiceOptions$");
		SqlClient.SqlConnection con;
		con = da.OpenDatabase();
		da.DBExecutesql(con, "Truncate Table TROAA");
		da.DBExecutesql(con, "DELETE FROM ServiceLevelMap");
		da.DBExecutesql(con, "DELETE FROM ServiceType");
		da.DBExecutesql(con, "DELETE FROM Response");
		da.DBExecutesql(con, "DBCC CHECKIDENT ('ServiceType',RESEED, 0)");
		da.DBExecutesql(con, "DBCC CHECKIDENT ('Response',RESEED, 0)");
		da.DBExecutesql(con, "DBCC CHECKIDENT ('ServiceLevelMap',RESEED, 0)");
		da.DBExecutesql(con, "DBCC CHECKIDENT ('TROAA',RESEED, 0)");

		using (SqlDataAdapter adapterServiceType = new SqlDataAdapter("select * from ServiceType", con)) {
			DataTable dtiqServiceType = new DataTable();
			adapterServiceType.Fill(dtiqServiceType);
			foreach ( row in dtServiceType.Rows) {
				DataRow rowIQ = dtiqServiceType.NewRow();
				if (row("DisplayOrder") != null) {
					int order = Convert.ToInt32(row("DisplayOrder"));
					rowIQ("mfrCode") = row("Mfr_Code");
					if (row("Title") != null)
						rowIQ("FK_Translation_Key_Title") = iq.AddTranslation(row("Title"), English, "CPQ", order, null, 0, false).Key;
					if (row("LongDescription") != null & !IsDBNull(row("LongDescription")))
						rowIQ("FK_Translation_Key_Description") = iq.AddTranslation(row("LongDescription"), English, "CPQ", order, null, 0, false).Key;
					rowIQ("serviceTypeDefault") = Convert.ToBoolean(row("Default"));
					rowIQ("HPID") = row("ID");
					dtiqServiceType.Rows.Add(rowIQ);
				}
			}
			SqlCommandBuilder ESCBuilder = new SqlCommandBuilder(adapterServiceType);
			adapterServiceType.UpdateCommand = ESCBuilder.GetUpdateCommand();
			adapterServiceType.Update(dtiqServiceType);


		}
		using (SqlDataAdapter adapterResponse = new SqlDataAdapter("select * from Response", con)) {
			DataTable dtiqResponse = new DataTable();
			adapterResponse.Fill(dtiqResponse);
			foreach ( row in dtResponse.Rows) {
				DataRow rowIQ = dtiqResponse.NewRow();
				if (row("DisplayOrder") != null) {
					int order = Convert.ToInt32(row("DisplayOrder"));
					rowIQ("mfrCode") = row("Mfr_Code");
					if (row("Title") != null)
						rowIQ("FK_Translation_Key_Title") = iq.AddTranslation(row("Title"), English, "CPQ", order, null, 0, false).Key;
					if (row("LongDescription") != null & !IsDBNull(row("LongDescription")))
						rowIQ("FK_Translation_Key_Description") = iq.AddTranslation(row("LongDescription"), English, "CPQ", order, null, 0, false).Key;
					rowIQ("ResponseDefault") = Convert.ToBoolean(row("Default"));
					rowIQ("HPID") = row("ID");
					dtiqResponse.Rows.Add(rowIQ);
				}
			}
			SqlCommandBuilder ESCBuilder = new SqlCommandBuilder(adapterResponse);
			adapterResponse.UpdateCommand = ESCBuilder.GetUpdateCommand();
			adapterResponse.Update(dtiqResponse);


		}

		using (SqlDataAdapter adapterServiceLevelMap = new SqlDataAdapter("select * from ServiceLevelMap", con)) {
			DataTable dtiqServiceType = new DataTable();
			DataTable dtiqResponse = new DataTable();
			using (SqlDataAdapter adapterServiceType = new SqlDataAdapter("select * from ServiceType", con)) {
				adapterServiceType.Fill(dtiqServiceType);
			}
			using (SqlDataAdapter adapterResponse = new SqlDataAdapter("select * from Response", con)) {
				adapterResponse.Fill(dtiqResponse);
			}

			DataTable dtiqServiceLevelMap = new DataTable();
			adapterServiceLevelMap.Fill(dtiqServiceLevelMap);
			foreach ( row in dtServiceLevelsMap.Rows) {
				DataRow rowIQ = dtiqServiceLevelMap.NewRow();

				if (row("Mfr_Code") != null) {
					rowIQ("mfrCode") = row("Mfr_Code");
					rowIQ("ServiceLevel") = Convert.ToInt32(row("ServiceLevel"));
					rowIQ("ServiceLevelGroup") = row("ServiceLevelGroup");
					rowIQ("SuperGroup") = row("SuperGroup");
					if (row("Description") != null & !IsDBNull(row("Description")))
						rowIQ("FK_Translation_Key_Description") = iq.AddTranslation(row("Description"), English, "CPQ", 1, null, 0, false).Key;

					rowIQ("Duration") = Convert.ToInt32(row("Duration"));

					if (row("WarrantyType") == "S")
						rowIQ("PostWarranty") = false;
					else
						rowIQ("PostWarranty") = true;
					rowIQ("Disabled") = Convert.ToBoolean(row("Supress"));

					if (row("Fk_ServiceType_ID") != null & !IsDBNull(row("Fk_ServiceType_ID"))) {
						DataRow[] foundrows;
						foundrows = dtiqServiceType.Select("HPID = '" + Trim(row("Fk_ServiceType_ID")) + "'");
						if (foundrows.Count == 1) {
							DataRow selectedRow = foundrows(0);
							rowIQ("Fk_ServiceType_ID") = Convert.ToInt32(selectedRow("ID"));
						}
					}

					if (row("Fk_Response_ID") != null & !IsDBNull(row("Fk_Response_ID"))) {
						DataRow[] foundrows;
						foundrows = dtiqResponse.Select("HPID = '" + Trim(row("Fk_Response_ID")) + "'");
						if (foundrows.Count == 1) {
							DataRow selectedRow = foundrows(0);
							rowIQ("Fk_Response_ID") = Convert.ToInt32(selectedRow("ID"));
						}
					}

					if (row("HPE_DMR") != null & !IsDBNull(row("HPE_DMR")))
						rowIQ("hpeDMR") = Convert.ToBoolean(row("HPE_DMR"));
					if (row("HPE_CDMR") != null & !IsDBNull(row("HPE_CDMR")))
						rowIQ("hpeCDMR") = Convert.ToBoolean(row("HPE_CDMR"));
					if (row("HPI_ADP") != null & !IsDBNull(row("HPI_ADP")))
						rowIQ("hpiADP") = Convert.ToBoolean(row("HPI_ADP"));
					if (row("HPI_DMR") != null & !IsDBNull(row("HPI_DMR")))
						rowIQ("hpiDMR") = Convert.ToBoolean(row("HPI_DMR"));
					if (row("HPI_Travel") != null & !IsDBNull(row("HPI_Travel")))
						rowIQ("hpiTravel") = Convert.ToBoolean(row("HPI_Travel"));
					if (row("HPI_Traceing") != null & !IsDBNull(row("HPI_Traceing")))
						rowIQ("hpiTracing") = Convert.ToBoolean(row("HPI_Traceing"));
					if (row("HPI_Theft") != null & !IsDBNull(row("HPI_Theft")))
						rowIQ("hpiTheft") = Convert.ToBoolean(row("HPI_Theft"));

					dtiqServiceLevelMap.Rows.Add(rowIQ);
				}
			}
			SqlCommandBuilder ESCBuilder = new SqlCommandBuilder(adapterServiceLevelMap);
			adapterServiceLevelMap.UpdateCommand = ESCBuilder.GetUpdateCommand();
			adapterServiceLevelMap.Update(dtiqServiceLevelMap);



		}

		using (SqlDataAdapter adapterTROAA = new SqlDataAdapter("select * from TROAA", con)) {
			DataTable dtiqServiceLevelMap = new DataTable();
			using (SqlDataAdapter adapterServiceLevelMap = new SqlDataAdapter("select * from ServiceLevelMap", con)) {
				adapterServiceLevelMap.Fill(dtiqServiceLevelMap);
			}
			DataTable dtiqTROAA = new DataTable();
			adapterTROAA.Fill(dtiqTROAA);
			foreach ( row in dtTROAA.Rows) {
				DataRow rowIQ = dtiqTROAA.NewRow();
				if (row("DisplayOrder") != null) {
					int order = Convert.ToInt32(row("DisplayOrder"));
					rowIQ("SysFamily") = row("SysFamily");
					rowIQ("SlotTypeCode") = Convert.ToInt32(row("SlotTypeCode"));
					rowIQ("ServiceLevel") = Convert.ToInt32(row("ServiceLevel"));
					rowIQ("DisplayOrder") = Convert.ToInt32(row("DisplayOrder"));
					DataRow[] foundrows;
					foundrows = dtiqServiceLevelMap.Select("ServiceLevel = " + Trim(row("ServiceLevel")));
					if (foundrows.Count == 1) {
						DataRow selectedRow = foundrows(0);
						rowIQ("FK_ServiceLevelMap_ID") = Convert.ToInt32(selectedRow("ID"));
					}
					if (iq.i_region_code.ContainsKey(row("Region"))) {
						rowIQ("FK_Region_ID") = iq.i_region_code(row("Region")).ID;
					}

					dtiqTROAA.Rows.Add(rowIQ);
				}
			}
			SqlCommandBuilder ESCBuilder = new SqlCommandBuilder(adapterTROAA);
			adapterTROAA.UpdateCommand = ESCBuilder.GetUpdateCommand();
			adapterTROAA.Update(dtiqTROAA);


		}


	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button46_Click(object sender, EventArgs e)
	{

		Import.sweepFIos();


	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button47_Click(object sender, EventArgs e)
	{
		SqlClient.SqlConnection con = da.OpenDatabase;
		Import.FIOs(con);

		con.Close();

	}

	protected void  // ERROR: Handles clauses are not supported in C#
Button48_Click(object sender, EventArgs e)
	{
		Import.TestFastFind();


	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button1_Click(object sender, EventArgs e)
	{
	}

	protected void  // ERROR: Handles clauses are not supported in C#
BtnOptionsPerSystem_Click(object sender, EventArgs e)
	{
		Utility.OptionsPerSystem();
	}


	protected void  // ERROR: Handles clauses are not supported in C#
CheckFamilies_Click(object sender, EventArgs e)
	{
		Import.checkfamilies();

	}


	private void  // ERROR: Handles clauses are not supported in C#
FixMSBranches()
	{
		List<string> errorMessages = new List<string>();


		foreach ( branch in iq.Branches.Values) {
			// Find each "Microsoft" branch we need to fix

			if (branch.Translation.text(English) == "Microsoft" && branch.childBranches.Count > 0) {
				object microsoftBranch = branch;

				// Look for a sibling "Microsoft OS" branch
				clsBranch microsoftOsBranch = microsoftBranch.Parent.FindBranchByNameBelow("Microsoft OS", "", true, 12);
				if (microsoftOsBranch == null)
					continue;

				// Look for the "Operating System" and "Client & Device Licencing" branches under the "Microsoft" branch
				clsBranch osBranch = null;
				clsBranch calBranch = null;

				foreach ( b in microsoftBranch.childBranches.Values) {
					if (b.Translation.text(English) == "Operating System" | b.Translation.text(English) == "Operating Systems") {
						osBranch = b;
					}

					if (b.Translation.text(English) == "Client & Device Licencing") {
						calBranch = b;
					}

				}


				if (osBranch != null && calBranch != null) {
					FixMSSubbranch(osBranch, calBranch, errorMessages, microsoftOsBranch, "sof1");
					FixMSSubbranch(calBranch, osBranch, errorMessages, microsoftOsBranch, "sof4");

				}

			}

		}

	}


	private void FixMSSubbranch(clsBranch targetBranch, clsBranch siblingTargetBranch, List<string> errorMessages, clsBranch microsoftOsBranch, string sof)
	{
		// targetBranch is either the "Operating System" or the "Client & Device Licencing" subbranch of the Software/Microsoft branch - 
		// this routine moves it to Software/Microsoft OS and populates it from the data held directly under Software/Microsoft OS

		// Give the branch its new parent
		targetBranch.Parent = microsoftOsBranch;
		targetBranch.Update(errorMessages);

		// Delete any existing children - we'll take the list under the "Microsoft OS" branch as more reliable

		foreach ( b in targetBranch.childBranches.Values) {
			b.deleted = true;
			b.Update(errorMessages);

		}

		// Move each sof1/sof4 product from directly under the "Microsoft OS" branch to the target subbranch

		foreach ( b in microsoftOsBranch.childBranches.Values) {

			if (!b.ID == targetBranch.ID && !b.ID == siblingTargetBranch.ID) {

				if (b.Product != null) {

					if (string.Equals(b.Product.ProductType.Code, sof, StringComparison.InvariantCultureIgnoreCase)) {
						b.Parent = targetBranch;
						b.Update(errorMessages);

					}

				}

			}

		}

	}

	protected void  // ERROR: Handles clauses are not supported in C#
Button49_Click(object sender, EventArgs e)
	{
		foreach ( branch in iq.Branches.Values) {

			if (branch.Product != null && branch.Product.isSystem) {
				if (branch.slots.Values.Where(sl => sl.Type.MajorCode.ToUpper == "WTY").Count == 0) {
					object slt = new clsSlot(iq.i_slotType_Code("WTY")("CAREPACK"), branch, "", 3, null, new NullableInt(), 0, 0);
				}
			}

		}
	}

	protected void  // ERROR: Handles clauses are not supported in C#
btnCullBadOptions_Click(object sender, EventArgs e)
	{
		Import.CullBadOptions();


	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnFixQuantities_Click(object sender, EventArgs e)
	{
		HashSet<string> todel = new HashSet<string>();
		int DC = 0;
		string sysSKU = txtSysSku.Text;
		//  If sysSKU <> "" Then

		Dictionary<string, Dictionary<string, int>> dicfios = new Dictionary<string, Dictionary<string, int>>();

		SqlClient.SqlConnection con = da.OpenDatabase;
		dicfios = Import.FIOs(con, "", null);
		//"'" & sysSKU & "'")

		object pth = "";
		//Dim systembranch As clsBranch = RootBranch.findChildBySKU2("tree.1", sysSKU, pth, False)

		Dictionary<string, clsBranch> sysLocs = RootBranch.findSystemBranches("tree.1");


		foreach ( Path in sysLocs.Keys) {
			Dictionary<string, HashSet<clsQuantity>> qdic = new Dictionary<string, HashSet<clsQuantity>>();
			clsBranch systembranch = sysLocs(Path);

			// If systembranch.Product.SKU = "788096-425" Then Stop

			systembranch.descendantQuantities(qdic);

			//  If qdic.ContainsKey("EJ013B") Then
			// Dim a = 0
			// End If

			foreach ( optionsku in qdic.Keys) {
				if (optionsku != systembranch.Product.SKU) {
					if (iq.i_SKU(optionsku).i_Attributes_Code.ContainsKey("opttype")) {
						string ot = iq.i_SKU(optionsku).i_Attributes_Code("opttype")(0).Translation.text(English).ToUpper;
						if (ot == "TAP") {
							if (dicfios.ContainsKey(systembranch.Product.SKU)) {
								if (!dicfios(systembranch.Product.SKU).ContainsKey(optionsku)) {
									int qc = qdic(optionsku).Count;
									foreach ( q in qdic(optionsku)) {
										object dbg = "removed " + PathName(Path);
										todel.Add(q.ID);
										q.deleted = true;
										DC += 1;
									}
								}
							} else {
								//        Stop
							}
						}
					}
				}
			}
		}
		con.Close();

		da.DBExecutesql("UPDATE quantity SET deleted=1 WHERE id in ('" + Join(todel.ToArray, "','") + "');");

		Debug.Print(DC);
		// End If
	}

	protected void  // ERROR: Handles clauses are not supported in C#
btnFixMissingMemory_Click(object sender, EventArgs e)
	{
		Import.FixMissingMemory();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
Button50_Click(object sender, EventArgs e)
	{
		Import.checkoptions();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
btnDeleteCarepacks_Click(object sender, EventArgs e)
	{
		CarePackModule.DeleteAllCarePacks();
	}

	protected void  // ERROR: Handles clauses are not supported in C#
cmdAddAll_Click(object sender, EventArgs e)
	{
		UInt64 lid = 0;
		UInt64.TryParse(Request.QueryString("lid"), lid);
		CarePackModule.AddAllCarePacks(lid);
	}
}

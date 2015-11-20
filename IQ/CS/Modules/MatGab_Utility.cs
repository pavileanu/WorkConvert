
using dataAccess;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Net.Mail;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Globalization;
using System.Linq;
using System.Drawing;
using System.Web.UI.DataVisualization.Charting;
using System.Security.Cryptography;
using System.Web.UI.WebControls;

using System.Threading;
using System.Threading.Tasks;

using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using log4net;
using log4net.Config;


class Utility
{

	public const string imagebase = "http://www.channelcentral.net/";
	//Public Const imagebase = "http://iquote2.channelcentral.net/sandbox/daisyimages/"


	public  eim = "../editor/images/";
	public clsState ev_Warning;
	public clsState ev_Critical;
	public clsState ev_Info;
	private ILog log = LogManager.GetLogger("IQUtility");
	private object switchAccountLock = new object();

	private object logLock = new object();
	/// <summary>Wraps the string in "quotes" - escaping and quotes, singles quores and \'s therein for an excel compatible CSV format</summary>
	/// <remarks>Attempts to comply with http://tools.ietf.org/html/rfc4180 which appears to be the closest thing there is to a standard </remarks>
	public string CSV(l)
	{

		object r;
		r = Replace(l, Chr(34), Chr(34) + Chr(34));
		//    r$ = Replace(r$, "\", "\\")  'there is no mention of this in 'the standard'
		//    r$ = Replace(r$, "'", "''")
		//finally - wrap in quotes
		r = Chr(34) + r + Chr(34);

		return r;

	}
	/// <summary>
	/// Creates delete button pass path id and command in. 
	/// </summary>
	/// <param name="path">An String object that represents the path of the item</param>
	/// <param name="id">A integer value that prepresnts the id of item.</param>
	/// <param name="cmd">An string object that represent the cmd to run.</param>
	/// <returns>An instance of Literal Control.</returns>
	/// <remarks></remarks>
	public Literal FunctionButton(string path, int id, string cmd, string caption, string tooltip)
	{
		Literal lt = new Literal();
		lt.Text = string.Format("{0}{1}{2}{3}{4}{5}{6}", "<div onclick='burstBubble();getBranches(|path=", path, "&cmd=", cmd, "&id=", id, "|);return false;'");
		lt.Text = Replace(lt.Text, "|", Chr(34));
		lt.Text += "style='background-color:red;color:white;display:inline-block;cursor:pointer;z-index:20;'";
		lt.Text += "title='" + tooltip + "'>";
		lt.Text += caption + "</div>";
		return lt;

	}

	public object writeSystemsBelow(clsBranch b)
	{

		//compile a list of all systems by family
		HashSet<clsProduct> systems = new HashSet<clsProduct>();
		b.systemsBelow(systems);
		//recurses finding all systems (by sku)

		Dictionary<string, string> scs;
		scs = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
		scs.Add("HP Renew", "R");
		scs.Add("Promotional", "P");
		scs.Add("Regular models", "A");
		scs.Add("Smart Buy", "SB");
		scs.Add("Top Value", "TV");

		StreamWriter sw = new StreamWriter("c:\\temp\\systems.txt");


		object c = "Data Source=www.channelcentral.net,8484; user id=editor;Initial Catalog=datastore; password=wainwright; connection timeout=35; MultipleActiveResultSets=true;";
		SqlClient.SqlConnection con = da.OpenDatabase(c);


		//INSERT H1.DataStore.admin.Compare_Systems
		//(Version, SysType, SysFamilyName, SystemSKU, SystemDesc, SystemSupplyChain)

		DataTable wc = da.MakeWriteCacheFor(con, "admin.compare_systems");


		DateTime ts = Now;

		int ss;
		foreach ( s in systems) {
			//SysType SysFamilyName SystemSKU SystemSupplyChain SystemDesc
			//SWD TAPE_DRIVE AG576B A HP StoreEver 3U SAS Rackmount Kit

			if (s.Active & s.activeFrom < Now & Now < DateAdd(DateInterval.Month, 3, s.activeTo) & s.Publish) {
				string fm = "";
				//Somes Microservers have no family etc.
				if (s.i_Attributes_Code.ContainsKey("fammajor")) {
					if (s.i_Attributes_Code.ContainsKey("fammajor"))
						fm = s.i_Attributes_Code("fammajor")(0).Translation.text(English);
					string sc = s.i_Attributes_Code("SC")(0).Translation.text(English);
					ss += 1;
					object desc = s.i_Attributes_Code("desc")(0).Translation.text(English);
					sw.WriteLine("IQ2^" + s.ProductType.Code + "^" + fm + "^" + s.SKU + "^" + scs(sc) + "^" + desc);

					if (s.SKU.Length < 40) {
						if (s.SKU.Length > 40)
							System.Diagnostics.Debugger.Break();
						if (desc.Length > 400)
							System.Diagnostics.Debugger.Break();
						if (s.ProductType.Code.Length > 3)
							System.Diagnostics.Debugger.Break();
						if (fm.Length > 20)
							System.Diagnostics.Debugger.Break();
						//If sc.Length > 4 Then Stop

						DataRow r = wc.NewRow;
						r.Item("sysType") = s.ProductType.Code;
						r.Item("sysFamilyName") = fm;
						r.Item("systemSKU") = s.SKU;
						r.Item("systemDesc") = desc;
						r.Item("systemSupplyChain") = scs(sc);
						r.Item("version") = "IQ2";
						r.Item("reportTime") = ts;

						wc.Rows.Add(r);
					}
				}
			}
		}
		sw.Close();


		da.BulkWrite(con, wc, "admin.compare_systems");
		con.Close();


	}

	public object OptionsPerSystem()
	{

		HashSet<string> opts = new HashSet<string>();

		int prunes;
		int dupes;
		HashSet<string> inskus = new HashSet<string>();

		//For Each sku In Split("AP838B,AW568B,E1Z55LT,AP838B,AW568B,E1Z55LT,F1K76LA,F1P36EA,J6D94UA,470065-743,470065-861,769503-291,WM448ET,WM698EA,F1K76LA,F1P36EA,J6D94UA,470065-743,470065-861,769503-291,WM448ET,WM698EA", ",")
		//    inskus.Add(sku)
		//Next

		bool jjj = iq.i_SKU.ContainsKey("QK765A");

		prunes = 0;
		dupes = 0;
		StreamWriter sw = new StreamWriter("c:\\temp\\dupedOptions.txt");
		iq.RootBranch.OptionsPersystem("", opts, "tree." + iq.RootBranch.ID, prunes, dupes, sw, inskus);
		sw.Close();

		object c = "Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=datastore; password=wainwright; connection timeout=35; MultipleActiveResultSets=true;";
		SqlClient.SqlConnection con = da.OpenDatabase(c);

		//INSERT H1.DataStore.admin.Compare_Systems
		//(Version, SysType, SysFamilyName, SystemSKU, SystemDesc, SystemSupplyChain)

		da.DBExecutesql(con, "truncate table datastore.admin.compare_optionsPerSystem");

		DataTable wc = da.MakeWriteCacheFor(con, "datastore.admin.compare_optionsPerSystem");

		foreach ( o in opts) {
			string[] bits = Split(o, "^");
			DataRow r = wc.NewRow;
			//If bits(0) = "QK765A" Then Stop
			r.Item("systemSKU") = bits(0);
			r.Item("optionSKU") = bits(1);
			wc.Rows.Add(r);
		}

		da.BulkWrite(con, wc, "admin.compare_optionsPerSystem");
		con.Close();


	}

	public object writeOptionsBelow(clsBranch b)
	{

		object c = "Data Source=www.channelcentral.net,8484; user id=editor;Initial Catalog=datastore; password=wainwright; connection timeout=35; MultipleActiveResultSets=true;";
		SqlClient.SqlConnection con = da.OpenDatabase(c);

		//INSERT H1.DataStore.admin.Compare_Systems
		//(Version, SysType, SysFamilyName, SystemSKU, SystemDesc, SystemSupplyChain)

		DataTable wc = da.MakeWriteCacheFor(con, "admin.compare_options");

		DateTime ts = Now;
		StreamWriter sw;
		//CK - SKU^sysfamcode
		//Dim ck$ = ty$ & "^" & fam$ & "^" & Me.Product.i_Attributes_Code("MfrSKU")(0).Translation.text(English)
		Dictionary<string, clsProduct> opts = new Dictionary<string, clsProduct>();
		b.DistinctOptionsRecursive("", "", opts);


		sw = new StreamWriter("c:\\temp\\options.txt");
		foreach ( kvp in opts) {
			string ot = "";
			if (kvp.Value.i_Attributes_Code.ContainsKey("optType")) {
				ot = kvp.Value.i_Attributes_Code("opttype")(0).Translation.text(English);
			}

			clsProduct o = kvp.Value;


			if (o.Active & o.activeFrom < Now & Now < DateAdd(DateInterval.Month, 3, o.activeTo) & o.Publish) {
				//don't write 'carepacks' for .. now
				if (!("WTY,SUP,HWSW,SVC").Contains(ot.ToUpper)) {
					string dsc = "";
					if (kvp.Value.i_Attributes_Code.ContainsKey("desc")) {
						dsc = kvp.Value.i_Attributes_Code("desc")(0).Translation.text(English);
					}

					//Dim sku$ = ""
					//If kvp.Value.i_Attributes_Code.ContainsKey("mfrsku") Then
					// sku = kvp.Value.i_Attributes_Code("mfrsku")(0).Translation.text(English)
					// End If

					string[] bits = Split(kvp.Key, "^");
					string skuType = bits(0);
					string fam = bits(1);
					string sku = bits(2);

					sw.WriteLine("IQ2^" + skuType + "^" + fam + "^" + ot + "^" + sku + "^" + dsc);

					DataRow r = wc.NewRow;
					if (Len(ot) > 5)
						ot = "HDD";
					//HORRIBLE HACK FIX
					r.Item("optType") = ot;
					r.Item("sysFamilyName") = fam;
					r.Item("optsku") = sku;
					r.Item("optdesc") = dsc;
					r.Item("version") = "IQ2";
					r.Item("reportTime") = ts;
					r.Item("sysType") = skuType;

					wc.Rows.Add(r);

				}
			}

		}
		sw.Close();

		da.BulkWrite(con, wc, "admin.compare_options");
		con.Close();


	}


	public string systemPath(path)
	{

		string[] segs;
		segs = Split(path, ".");

		clsProduct p;
		string pth = "tree";

		foreach ( seg in segs) {
			if (LCase(seg) != "tree") {
				pth += "." + seg;
				p = iq.Branches((int)seg).Product;
				if (p != null && p.isSystem)
					return pth;
			}
		}

		return "";

	}

	public bool PictureOnPath(Path, string picture)
	{

		PictureOnPath = false;
		string[] seg;
		seg = Split(Path, ".");

		foreach ( s in seg) {
			if (LCase(s) != "tree") {
				if (LCase(iq.Branches((int)s).Picture) == LCase(picture))
					return true;
			}
		}

	}

	public UInt64 simpleHash(l)
	{

		SHA1 j;
		j = new SHA1Cng();

		int i = 0;

		// SK - I can't see the point of the fixed (and very short) salt value in the following line. Each salt value
		// should be unique (preferably randomly generated, e.g. via RNGCryptoServiceProvider) and stored. A fixed salt
		// value isn't really adding much security...
		byte[] bytes = Encoding.UTF8.GetBytes(l + "s34dog");
		//salt http://en.wikipedia.org/wiki/Salt_(cryptography)

		byte[] hashbytes;

		hashbytes = j.ComputeHash(bytes);
		// And UInt64.MaxValue

		simpleHash = BitConverter.ToUInt64(hashbytes, 0);

	}

	public class Ra
	{
		public int Required;

		public int Available;
		public Ra(int r, int a)
		{
			this.Required = r;
			this.Available = a;
		}

	}

	public class clsRange
	{
		public string UnitText;
		public Int64 min;
		public Int64 max;
		public bool IsMixed = false;

		public string TextRepresentation;
		//min As Int64, max As Int64)
		public clsRange()
		{
			this.min = Int64.MaxValue;
			//this is a little counterinuitive - but its right... the first update 'stretc' will set min and max
			this.max = Int64.MinValue;
		}

		public bool stretch(Int64 v)
		{
			//Updates the min and max based on some new value - returns true if the range was extended

			stretch = false;
			if (v < this.min){this.min = v;stretch = true;}
			if (v > this.max){this.max = v;stretch = true;}

			if (this.min < 0 & this.min != Int64.MinValue)
				this.min = 0;
			//Never show negatives...
		}


	}
	public class clsMinMaxTotalUsed
	{
		public int Min;
		public int Max;
		public int Total;
		public int Used;

		public bool optionalRule;
		public clsMinMaxTotalUsed(int min, int max, int total, int Used, bool optionalRule)
		{
			this.Min = min;
			this.Max = max;
			this.Total = total;
			this.Used = Used;
			this.optionalRule = optionalRule;
		}

		public bool stretch(int v)
		{
			//Updates the min and max based on some new value - returns true if the range was extended

			stretch = false;
			if (v < this.Min){this.Min = v;stretch = true;}
			if (v > this.Max){this.Max = v;stretch = true;}

		}

	}



	public string ReplaceSegment(path, int find, int replace)
	{

		string[] segs = Split(path, ".");

		//we go from 1 becuas the 0th seg is 'tree.'
		for (i = 1; i <= UBound(segs); i++) {
			if ((int)segs(i) == find)
				segs(i) = Trim((string)replace);
		}

		ReplaceSegment = Join(segs, ".");

	}
	public int BitCount(int b)
	{
		//Returns the number of bits set in the byte
		int c;
		int m = 1;
		//mask
		//maxbits is the number of bits to check
		for (i = 0; i <= 7; i++) {
			if (b & m)
				c += 1;
			m = m + m;
		}

		return c;

	}

	public clsPrice LowestPrice(List<clsPrice> prices)
	{

		object nonTestPrices = from p in priceswhere p.SKUVariant.Code.ToUpper != "TST"orderby p.Price.value ascending;

		if (nonTestPrices.Count > 0) {
			LowestPrice = nonTestPrices.First;
		} else {
			LowestPrice = (from p in pricesorderby p.Price.value ascending).First;
		}

		//LowestPrice = If((From p In lp Where p.SKUVariant.Code.ToUpper <> "TST" Order By p.Price.value Ascending).Count > 0, (From p In lp Where p.SKUVariant.Code.ToUpper <> "TST" Order By p.Price.value Ascending).First, (From p In lp Order By p.Price.value Ascending).First)

	}

	/// <summary>'return the .Price property of supplied clsPrice instance - unless it's Nothing in which case it returns an empty price of the specified currency</summary>
	public NullablePrice NullPrice(clsPrice price, clsCurrency currency)
	{

		if (price == null) {
			return new NullablePrice(currency);
		} else {
			return price.Price;
		}

	}

	public clsLanguage s_lang()
	{
		s_lang = English;
	}


	public void FillDDL(ref DropDownList ddl, object values)
	{
		foreach ( v in values) {
			ddl.Items.Add(new ListItem(v.displayname(s_lang), v.id));
		}

	}

	public string GetParenthesisValue(l)
	{

		//returns a value enclosed in parantheses from a strring

		if (InStr(l, "(")) {
			string j = Split(l, "(")(1);
			int cp = InStr(j, ")");
			if (cp == 0) {
				System.Diagnostics.Debugger.Break();
				//missing close parentehise
			}
			j = Left(j, cp - 1);
			return j;
		} else {
			return "";
		}

	}

	//Public Function ChannelSKU(product As clsProduct, skuvariant As clsVariant, sellerChannel As clsChannel) As String

	//    'this really needs to be cached in a dictionary - as it could be called several hundred times in the opening of a single branch

	//    If skuvariant Is Nothing Then skuvariant = iq.StandardVariant

	//    ChannelSKU = ""

	//    Dim con As SqlClient.SqlConnection
	//    Dim rdr As SqlClient.SqlDataReader
	//    con = da.opendatabase()
	//    Dim sql$
	//    sql$ = "SELECT ChannelMFRSKU FROM channelSKU where fk_product_id=" & product.ID & " AND FK_Variant_ID=" & skuvariant.ID & " AND FK_Channel_ID=" & sellerChannel.ID

	//    rdr = da.dbexecuteReader(con, sql$)

	//    If rdr.HasRows Then
	//        rdr.Read()
	//        ChannelSKU = rdr.Item("channelmfrsku")
	//    End If

	//    rdr.Close()
	//    con.Close()

	//End Function


	/// <summary>
	/// Makes the UI for the 'specification section of a quote item (mostly information on slot utilisations)    ''' 
	/// </summary>
	/// <param name="dicslots">The consolidated information of slot usage within this system</param>
	/// <param name="slottypes"></param>
	/// <param name="Open">If open - it returns a richer panel, with a close button</param>
	/// <returns></returns>
	/// <remarks></remarks>
	public Panel SpecUI(UInt64 lid, clsQuoteItem i, Dictionary<clsSlotType, clsSlotSummary> dicslots, List<string> slottypes, bool Open, clsLanguage language)
	{

		//it would be nicer if this as a member of the clsQuoteItem - but the fact that this is based on a precompiled dictionary complicated thigs
		//dicslots is  my 'minor' type - slot types is the list of 'categories' we're validating by W,MEM,HDD etc
		SpecUI = new Panel();
		SpecUI.CssClass = "panelOuter";


		//        Dim totalavail, used, unused As Integer

		Panel SpecHeader = new Panel();
		SpecHeader.CssClass = "panelHeader";
		SpecUI.Controls.Add(SpecHeader);
		string script = "";
		//passed byref and POPULATES the script (which goes onto the WHOLE panel (not just the button)
		SpecHeader.Controls.Add(i.PanelButton(panelEnum.Spec, Open, script));
		SpecHeader.Attributes("onmousedown") = script;

		Literal title = new Literal();
		title.Text = string.Format("<div class='panelTitle'>{0}</div>", Xlt("Specification", i.quote.AgentAccount.Language));
		SpecHeader.Controls.Add(title);

		Panel specRollup = new Panel();
		specRollup.CssClass = "specRollup";
		SpecHeader.Controls.Add(specRollup);


		HtmlGenericControl list = new HtmlGenericControl("UL");

		//For Each st In dicslots.Keys
		//    If slottypes.Contains(st.MajorCode) Then 'consolidate by major code
		//        With dicslots(st)
		//            'unused = dicslots(st).Given - dicslots(st).taken
		//            'totalAvail = dicslots(st).Given
		//            'used = totalavail - unused

		//            Dim lit As Literal = New Literal
		//            If st.MajorCode = "CPU" Then
		//                lit.Text = "<div class='specRollupItem'>" & st.MajorCode & ":" & .taken & " x " & .TotalCapacity / .taken & "&nbsp;"
		//            ElseIf st.MajorCode = "PWR" Then
		//                lit.Text = "<div class='specRollupItem'>" & st.MinorCode & ":" & .taken & "&nbsp;"
		//            Else
		//                lit.Text = "<div class='specRollupItem'>" & st.MajorCode & ":" & .TotalCapacity & "&nbsp;"
		//            End If

		//            If dicslots(st).CapacityUnit IsNot Nothing Then
		//                lit.Text &= dicslots(st).CapacityUnit.Translation.text(English) & "&nbsp;"
		//            End If
		//            lit.Text &= "</div>"
		//            specRollup.Controls.Add(lit)

		//            If Open Then
		//                'open - verbose version

		//                'we ONLY want to present the amber light for Watts ('AKA Power Sizing')
		//                Dim cu As String = ""
		//                If dicslots(st).CapacityUnit IsNot Nothing Then
		//                    cu = dicslots(st).CapacityUnit.Code
		//                End If

		//                Dim li As HtmlGenericControl = New HtmlGenericControl("LI")
		//                list.Controls.Add(li)

		//                If .taken > .Given Then
		//                    li.Attributes("Style") &= " traffic redLight"
		//                ElseIf .taken >= .Given * 0.75 And cu = "W" Then   ' Greg didn't like this (other than for PSU)
		//                    li.Attributes("Style") &= " traffic amberlight"
		//                Else
		//                    li.Attributes("Style") &= " traffic greenLight"
		//                End If

		//                lit = New Literal
		//                'lit.Text &= translateable(st.Translation.text(English), lid) & " used " & .taken & " of " & .Given & " available"

		//                'stopgap until we get round to translating the minor slot types above - to things like 'Small Drive Bay'
		//                lit.Text &= .taken & " " & fullMajor(st.MajorCode) & " of " & .Given '& " available"

		//                If dicslots(st).CapacityUnit IsNot Nothing Then
		//                    If st.MajorCode = "CPU" Then
		//                        lit.Text &= "&nbsp;totaling " & dicslots(st).taken & " x " & dicslots(st).TotalCapacity / dicslots(st).taken & "&nbsp;" & dicslots(st).CapacityUnit.Translation.text(English)
		//                    ElseIf st.MinorCode = "W" Then
		//                        'lit.Text &= "&nbsp;totaling " & dicslots(st).TotalCapacity & "&nbsp;" & dicslots(st).CapacityUnit.Translation.text(English)
		//                    Else
		//                        lit.Text &= "&nbsp;totaling " & dicslots(st).TotalCapacity & "&nbsp;" & dicslots(st).CapacityUnit.Translation.text(English)
		//                    End If

		//                End If
		//                li.Controls.Add(lit)

		//            End If
		//        End With
		//    End If
		//Next

		//SpecUI.Controls.Add(list)

		object maxPower = 0;
		foreach ( p in i.Children.GroupBy(c => c.Branch.Product.ProductType.Code.ToLower())) {
			object dis = dicslots.Where(ds => ds.Key.MajorCode.ToLower() == p.Key).FirstOrDefault;
			if (i.Msgs.Where(msg => msg.slotTypeMajor != null && msg.slotTypeMajor.ToLower() == p.Key).Count == 0) {
				if (p.Sum(f => f.validate ? f.Quantity : 0) > 0) {
					if (slottypes.Contains(p.Key.ToUpper)) {
						object text = "";
						object totalCapacity = p.Count > 0 && p.First.Branch.Product.i_Attributes_Code.ContainsKey("capacity") ? p.Sum(dd => dd.Branch.Product.i_Attributes_Code.ContainsKey("capacity") ? dd.validate ? dd.Quantity : 0 * dd.Branch.Product.i_Attributes_Code("capacity").First.NumericValue : 0) : "";
						if (dis.Value != null)
							text = string.Format("{0} {1}{2} ({3} slots of {4}) {5}", p.First.Branch.Product.ProductType.Translation.text(language), dis.Value.TotalCapacity > 0 ? dis.Value.TotalCapacity.ToString() : "", dis.Value.CapacityUnit != null ? dis.Value.CapacityUnit.Code : "", dis.Value.taken.ToString(), dis.Value.Given.ToString(), p.First.Branch.Product.ProductType.Code.ToLower() == "psu" ? " - (" + dicslots.Where(ds => ds.Key.MajorCode.ToLower == "pwr").Sum(ds => ds.Value.taken) <= dis.Value.TotalRedundantCapacity ? Xlt("Redundant", language) : Xlt("Non Redundant", language) + ")" : "");

						if (Open)
							list.Controls.Add(NewLit("<li>" + text + "</li>"));

						if ((i.Branch.Product.ProductType.Code == "SVR" && {
							"cpu",
							"mem",
							"pwr"
						}.Contains(p.Key)) | (i.Branch.Product.ProductType.Code == "HPN" && {
							"upconnectivity",
							"priconnectivity"
						}.Contains(p.Key))) {

							if (slottypes.Contains(p.Key.ToUpper)) {
								specRollup.Controls.Add(NewLit(string.Format("<div class='specRollupItem'><span style='font-weight: bold;'>{0}</span> : {1}{2}</div>", p.Key.ToUpperInvariant, dis.Value != null ? dis.Value.TotalCapacity : totalCapacity.ToString(), dis.Value != null && dis.Value.CapacityUnit != null ? dis.Value.CapacityUnit.Code : "")));

								//specRollup.Controls.Add(NewLit("<span>" & p.Key & ":" & p.Sum(Function(f) If(f.validate, f.Quantity, 0)) & "</span>"))
							}
						}
					}
				}
			}

		}


		foreach ( p in dicslots) {
			if (!slottypes.Contains(p.Key.MajorCode.ToUpper()))
				continue;
			if (i.Branch.Product.ProductType.Code == "SVR" && { "PWR" }.Contains(p.Key.MajorCode.ToUpper()) && i.Msgs.Where(msg => msg.slotTypeMajor != null && msg.slotTypeMajor.ToLower() == p.Key.MajorCode.ToLower()).Count == 0) {
				string text = string.Format("{0} {1}{2} ({3} slots of {4})", p.Key.TranslationShort != null ? p.Key.TranslationShort.text(language) : p.Key.Translation.text(language), p.Value.TotalCapacity.ToString(), p.Value.CapacityUnit != null ? p.Value.CapacityUnit.Code : "", p.Value.taken.ToString(), p.Value.Given.ToString());
				//Here we are in hackly land again, can't use slot total as soldered parts dont take a slot so we need to equate slottype or producttype and total the preinstalled of that product type....

				//Once again power is a special case....
				if (p.Key.MajorCode.ToUpper() == "PWR") {
					text = string.Format("{0} {1}W of {2}W", Xlt("Power Consumption", language), p.Value.taken.ToString(), p.Value.Given.ToString());
				}
				if (Open)
					list.Controls.Add(NewLit("<li>" + text + "</li>"));

				string midt = "";
				if (p.Key.MajorCode.ToUpper() == "PWR")
					midt = "W";
				specRollup.Controls.Add(NewLit("<div class='specRollupItem'><span style='font-weight: bold;'>" + p.Key.MajorCode + "</span> : " + p.Value.taken + midt + "/" + p.Value.Given + midt + "</div>"));
			}
		}

		SpecUI.Controls.Add(list);



	}

	public string translateable(i, UInt64 lid)
	{

		//can link from here to the editor to edit specific translations

		//translateable = "<span style='color:blue'>" & i$ & "</span>"
		translateable = "<span>" + i + "</span>";

	}

	private string fullMajor(string code)
	{

		switch (UCase(code)) {
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"HDD":
				fullMajor = "Drive bays";
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"MEM":
				fullMajor = "Memory Slots";
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"OPT":
				fullMajor = "Optical Drive bays";
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"FAN":
				fullMajor = "Fan Bays";
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"CPU":
				fullMajor = "CPU Slots";
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"PWR":
				fullMajor = "Watts";
			default:
				return code;
		}
	}



	public int IndexPaths()
	{

		DataTable WC = new DataTable();
		DataTable segcache = new DataTable();

		da.DBExecutesql("DROP INDEX [Nick] ON [dbo].[PathSegment] WITH ( ONLINE = OFF )");

		SqlClient.SqlConnection con;
		con = da.OpenDatabase();
		WC = da.MakeWriteCacheFor(con, "Path");
		segcache = da.MakeWriteCacheFor(con, "PathSegment");

		da.DBExecutesql("truncate table Path");
		da.DBExecutesql("DBCC CHECKIDENT('[Path]', RESEED, 1)");

		da.DBExecutesql("truncate table PathSegment");
		da.DBExecutesql("DBCC CHECKIDENT('[PathSegment]', RESEED, 1)");

		iq.RootBranch.IndexPaths(con, "tree", "", WC, segcache, 1, 0);

		//these are now just the final bulk writes (as we write in blocks of 50k rows)
		da.BulkWrite(con, segcache, "PathSegment");
		segcache = null;

		//takes around 22 seconds (for circa 1.5 million rows, on my laptop)
		da.BulkWrite(con, WC, "Path");

		object sql;

		Beep();
		Beep();
		Beep();
		sql = "CREATE NONCLUSTERED INDEX [Nick] ON [dbo].[PathSegment] ([fk_branch_id] ASC,[fk_path_id] Asc) ";
		sql += "WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY];";
		da.DBExecutesql(sql);
		Beep();
		Beep();
		Beep();

		IndexPaths = WC.Rows.Count;

		WC = null;

	}

	//Public Function FormatPrice(amount As Single, region As clsRegion) As String

	//    Dim ci As CultureInfo = Nothing
	//    Try
	//        If region.Culture Is Nothing Then
	//            ci = New CultureInfo("en-us")

	//        Else
	//            ci = New CultureInfo(region.Culture.Code) ' & "-" & region.Code))
	//        End If
	//    Catch
	//        Err.Raise(100, Nothing, "The culture code " & region.Culture.Code & " for country " & region.Name.text(s_lang) & " is probably wrong.")
	//    End Try

	//    Return amount.ToString("C", ci)

	//    'this UN converts it !
	//    'If Single.TryParse(pr, NumberStyles.Any, ci.NumberFormat, p!) Then

	//End Function
	public string FormatPrice(float amount, clsCulture culture)
	{

		CultureInfo ci = null;
		try {
			if (culture == null) {
				ci = new CultureInfo("en-us");

			} else {
				ci = new CultureInfo(culture.Code);
				// & "-" & region.Code))
			}
		} catch {
			Err.Raise(100, null, "The culture code " + culture.Code + "is probably wrong.");
		}

		return amount.ToString("C", ci);

		//this UN converts it !
		//If Single.TryParse(pr, NumberStyles.Any, ci.NumberFormat, p!) Then

	}

	//Public Sub WriteRows(ByRef DT As DataTable) - OBSOLETED by SQLBulkCopy (which doesnt require a dedicated SP and Table Type variable definition) - see Make WriteCacheFor() and da.bulkwrite()

	//    'Writes the accumlated SKUIndex rows via an SP
	//    '(and EMPTIES the dataTable) - ready for the next batch

	//    Dim con As SqlClient.SqlConnection = da.opendatabase()

	//    Dim params As Dictionary(Of String, Object)
	//    params = New Dictionary(Of String, Object)
	//    params.Add("tvp", DT)

	//    ExecuteSP(con, "SKUIndexInsert", params, Nothing)
	//    con.Close()

	//    'IMPORTANT !
	//    DT.Rows.Clear()

	//End Sub

	public object cloneQuoteItemRecursive(clsQuoteItem originalItem, clsQuote ontoQuote, clsQuoteItem newParentItem)
	{

		//Clones a quote item and all its children recursively - returning a new item - with now children (on the specified quote)

			//this is the final return value
			//virtual' root item




		 // ERROR: Not supported in C#: WithStatement


		clsQuoteItem ci;
		// was Me.Children
		foreach ( child in originalItem.Children) {
			ci = cloneQuoteItemRecursive(child, ontoQuote, cloneQuoteItemRecursive);
		}

	}




	public string SaveXML(XmlDocument doc, string filename, ref string Message)
	{

		//returns the physical path to the file created
		//find the virtual, and from that the physical path
		object vPath = HttpContext.Current.Request.ApplicationPath;
		object pPath = HttpContext.Current.Request.MapPath(vPath);

		SaveXML = pPath + filename;

		try {
			doc.Save(pPath + filename);
			Message = "Saved Successfully.";
		} catch (Exception ex) {
			Message = "ERROR saving " + ex.Message.ToString;
			SaveXML = "FAIL";
		}

	}



	public void LoadEnglish()
	{
		SqlClient.SqlConnection con = da.OpenDatabase();
		SqlClient.SqlDataReader r = da.DBExecuteReader(con, "Select id,code,localname,rtl,live,active from language where code= 'en' ");

		if (r.HasRows) {
			r.Read();
			English = new clsLanguage((int)r.Item("ID"), Trim(r.Item("Code")), r.Item("Localname"), (bool)r.Item("rtl"), (bool)r.Item("live"), (bool)r.Item("active"));
		} else {
			English = new clsLanguage("EN", "English", false, true, true);
		}

		r.Close();
		con.Close();

	}



	public void bootstrap()
	{
		//things we cannot even iq.Load() - without . .
		BootStrapTranslations();
		LoadEnglish();
		// loadWorldWide()
		// r_worldwide = clsRegion.getOrMake(Nothing, "XW", "Worldwide", False)


	}

	//Private Function loadWorldWide()


	//    Dim con As SqlClient.SqlConnection

	//    con = da.OpenDatabase()
	//    Dim r As SqlClient.SqlDataReader = da.DBExecuteReader(con, "SELECT Id,[fk_region_id_parent],code,[fk_translation_key_name],isCountry,culture FROM [Region] where code='XW'")

	//    '   LoadTranslation(r.Item("fk_translation_key_name"))

	//    Dim aRegion As clsRegion
	//    If r.HasRows Then
	//        r.Read()
	//        aRegion = New clsRegion(CInt(r.Item("id")), Nothing, r.Item("code").ToString(), _
	//                                iq.Translations(CInt(r.Item("fk_translation_key_name"))), _
	//                                CBool(r.Item("isCountry")), r.Item("culture").ToString())

	//    End If

	//    r.Close()
	//    con.Close()




	//End Function


	public void BootStrapTranslations()
	{
		//we need at least 1 translation to exist for addtranslation to work


		SqlClient.SqlConnection con = da.OpenDatabase();

		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, "select top 1 * from translation");

		bool addOne;
		if (!rdr.HasRows) {
			addOne = true;
		}
		rdr.Close();
		con.Close();


		if (addOne)
			da.DBExecutesql("INSERT INTO [translation] ([key],[text],fk_language_id,[group],[order]) VALUES (1,'bootstrap',1,'',0)");



	}

	public void checkEssentials()
	{
		//Creates (once) a bunch of standard systemwide stuff - such as some base units, translations etc. - IF they don't exist (weren't LOADed)
		//Generally the root objects (RootBranch,RootEvent, RootThread and RootChannel are set during iq.Load - to the first object instanced (see the constructors)

		Reflection.setupClassList();
		//required prior to makescreen() calls

		//If Not iq.i_state_GroupCode.ContainsKey("EV-Info") Then

		//    ev_Info = New clsState("EV", "Info", iq.AddTranslation("Info", English, ), 1, "#0000a0")
		//    ev_Warning = New clsState("EV", "Warn", iq.AddTranslation("Warning", English), 1, "#a0a060")
		//    ev_Critical = New clsState("EV", "Crit", iq.AddTranslation("Critical", English), 1, "#FF0000")
		//Else
		//    ev_Info = iq.i_state_GroupCode("EV-Info")
		//    ev_Warning = iq.i_state_GroupCode("EV-Warn")
		//    ev_Critical = iq.i_state_GroupCode("EV-Crit")
		//End If

		if (iq.Channels.Count) {
			string uu = "unknown@unknown.com";
			if (!iq.i_user_email.ContainsKey(uu))
				clsUser aUser = new clsUser(iq.Channels.Values.First, uu, "System use", new nullableString(), new nullableString());
			UnknownUser = iq.i_user_email(uu);
		}

		r_worldwide = clsRegion.getOrMake(null, "XW", "Worldwide", false, false, "");
		//- see Bootstrap()
		r_RestOfWorld = clsRegion.getOrMake(r_worldwide, "ROW", "Rest Of World", false, false, "");
		r_Americas = clsRegion.getOrMake(r_worldwide, "AMS", "The Americas", false, true, "");
		r_USCA = clsRegion.getOrMake(r_Americas, "USCA", "The United States and Canada", false, true, "");
		r_EMEA = clsRegion.getOrMake(r_worldwide, "EMEA", "Europe, Middle East and Africa", false, false, "");
		r_GWE = clsRegion.getOrMake(r_EMEA, "GWE", "Greater and Western Europe", false, false, "");
		r_UKIE = clsRegion.getOrMake(r_GWE, "UKIE", "United Kingdom & Ireland", false, false, "");
		r_GB = clsRegion.getOrMake(r_UKIE, "GB", "United Kingdom", true, false, "");
		//!!!! GB !!!
		r_IE = clsRegion.getOrMake(r_UKIE, "IE", "Ireland", true, false, "");
		r_MEMA = clsRegion.getOrMake(r_EMEA, "MEMA", "Middle East, Mediterranean and Africa", false, false, "");
		r_CEE = clsRegion.getOrMake(r_EMEA, "CEE", "Central and Eastern Europe", false, false, "");

		if (!iq.i_attribute_code.ContainsKey("FamMajor")) {
			object fmaj = new clsAttribute("FamMajor", iq.AddTranslation("Major Family", English, "attribs", 0, null, 0, 0), 0);
		}

		if (!iq.i_attribute_code.ContainsKey("FamMinor")) {
			object famMin = new clsAttribute("FamMinor", iq.AddTranslation("Minor Family", English, "attribs", 0, null, 0, 0), 0);
		}

		if (!iq.i_attribute_code.ContainsKey("FamDisp")) {
			object famDisp = new clsAttribute("FamDisp", iq.AddTranslation("Family name (for display)", English, "attribs", 0, null, 0, 0), 0);
		}

		if (!iq.i_channel_code.ContainsKey("Root"))
			clsChannel achannel = new clsChannel(null, "All channels", "", "", "Root", r_worldwide, new nullableString(), new nullableString(), new nullableString(), 15,
			"tree.1", "", 0, 0, "R", "", "", null, false, "",
			"", "");
		RootChannel = iq.i_channel_code("Root");

		if (!iq.i_channel_code.ContainsKey("HP"))
			clsChannel achannel = new clsChannel(null, "HP", "Hewlett Packard", "Hewlett-Packard Company,3000 Hanover(Street),Palo Alto, CA,94304-1185,USA", "HP", r_worldwide, new nullableString(), new nullableString(), new nullableString("http://www.hp.com"), 2,
			"tree.1", "", 0, 0, "R", "", "", iq.i_currency_code("USD"), false, "",
			"", "");
		HP = iq.i_channel_code("HP");

		if (iq.Branches.Count == 0)
			clsBranch aBranch = new clsBranch(null, null, iq.AddTranslation("All HP Products", English, "UI", 0, null, 0, false), "", iq.AddTranslation("Sectors", English, "collect", 0, null, 0, false), iq.AddTranslation("Sector", English, "collect", 0, null, 0, false), iq.i_screens_code("Servers"), 100, false, "S");
		RootBranch = iq.Branches(1);


		clsState Status;
		if (iq.i_state_GroupCode.ContainsKey("TH-InProg")) {
			Status = iq.i_state_GroupCode("TH-InProg");
		} else {
			Status = new clsState("TH", "InProg", iq.AddTranslation("In progress", English, "ticks", 0, null, 0, false), 10, "#a08040");
		}

		clsState normal;
		if (iq.i_state_GroupCode.ContainsKey("PR-Normal")) {
			normal = iq.i_state_GroupCode("PR-Normal");
		} else {
			normal = new clsState("PR", "Normal", iq.AddTranslation("Normal", English, "Priority", true, null, 0, false), 50, "#30a040");
			//green
		}

		//If iq.Threads.Count = 0 Then
		//    Dim aThread As clsThread = New clsThread(sysAdmin, sysAdmin, Nothing, normal, Status, 100, "All Threads", New nullableString("This is the root of all threads - do not delete it ! (although that should be impossible)"), Nothing, Now, Now, True)
		//End If

		bool reset;

		if (iq.Products.Count == 0)
			reset = true;
		clsAttribute j;
		//this is a 'throwaway' variable be use to create instances.. they are added (internally) to the IQ.Attributes 'master' list - holding a reference to them so they are not destroyed

		if (!iq.i_attribute_code.ContainsKey("name"))
			j = new clsAttribute("Name", iq.AddTranslation("Name", English, "UI", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("desc"))
			j = new clsAttribute("desc", iq.AddTranslation("Description", English, "UI", 0, null, 0, false), 0);

		if (!iq.i_attribute_code.ContainsKey("cores"))
			j = new clsAttribute("cores", iq.AddTranslation("Cores", English, "UI", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("threads"))
			j = new clsAttribute("threads", iq.AddTranslation("Threads", English, "UI", 0, null, 0, false), 0);


		if (!iq.i_attribute_code.ContainsKey("mfrSKU"))
			j = new clsAttribute("mfrSKU", iq.AddTranslation("MfrSKU", English, "UI", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("PLcode"))
			j = new clsAttribute("PLcode", iq.AddTranslation("PLcode", English, "UI", 0, null, 0, false), 0);

		if (!iq.i_attribute_code.ContainsKey("mass"))
			j = new clsAttribute("mass", iq.AddTranslation("Mass", English, "UI", 0, null, 0, false), 0);

		if (!iq.i_attribute_code.ContainsKey("optFamily"))
			j = new clsAttribute("optFamily", iq.AddTranslation("Option Family (legacy/import)", English, "UI", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("optType"))
			j = new clsAttribute("optType", iq.AddTranslation("Option type (legacy/import)", English, "UI", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("famMinor"))
			j = new clsAttribute("famMinor", iq.AddTranslation("Minor family (legacy/import)", English, "UI", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("famMajor"))
			j = new clsAttribute("famMajor", iq.AddTranslation("System family (legacy/import)", English, "UI", 0, null, 0, false), 0);

		if (!iq.i_attribute_code.ContainsKey("alsoHost"))
			j = new clsAttribute("alsoHost", iq.AddTranslation("Additional host codes (comma seperated) - overrides usuaual geographic restrictions)", English, "UI", 0, null, 0, false), 0);


		if (!iq.i_attribute_code.ContainsKey("incompat"))
			j = new clsAttribute("incompat", iq.AddTranslation("Incompatible with subafamilies:(legacy/import)", English, "UI", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("altSKU"))
			j = new clsAttribute("altSKU", iq.AddTranslation("Alternative Part:(legacy/import)", English, "UI", 0, null, 0, false), 0);

		if (!iq.i_attribute_code.ContainsKey("speed"))
			j = new clsAttribute("speed", iq.AddTranslation("Speed", English, "UI", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("capacity"))
			j = new clsAttribute("capacity", iq.AddTranslation("Capacity", English, "UI", 0, null, 0, false), 0);

		if (!iq.i_attribute_code.ContainsKey("cpuSKU"))
			j = new clsAttribute("cpuSKU", iq.AddTranslation("CPU Part number (legacy/import)", English, "UI", 0, null, 0, false), 0);

		if (!iq.i_attribute_code.ContainsKey("lifeCycle"))
			j = new clsAttribute("lifeCycle", iq.AddTranslation("Life Cycle (months)", English, "UI", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("management"))
			j = new clsAttribute("management", iq.AddTranslation("Management", English, "UI", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("security"))
			j = new clsAttribute("security", iq.AddTranslation("Security", English, "UI", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("subTitle"))
			j = new clsAttribute("subTitle", iq.AddTranslation("Sub Title", English, "UI", 0, null, 0, false), 0);

		if (!iq.i_attribute_code.ContainsKey("displaySize"))
			j = new clsAttribute("displaySize", iq.AddTranslation("Display Size", English, "UI", 0, null, 0, false), 0);
		//for laptops - so we can filter by size independent of resolution

		if (!iq.i_attribute_code.ContainsKey("SC"))
			j = new clsAttribute("SC", iq.AddTranslation("Supply Chain", English, "UI", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("focus"))
			j = new clsAttribute("focus", iq.AddTranslation("Focus", English, "UI", 0, null, 0, false), 0);


		if (!iq.Measures.ContainsKey(0)) {
			iq.Measures.Add(0, "None");
			object sql;
			sql = "SET IDENTITY_INSERT Measure ON;INSERT INTO [Measure] (ID,MeasureName) values (0,'None');SET IDENTITY_INSERT Measure OFF";
			da.DBExecutesql(sql, true);
		}

		if (!iq.i_unit_code.ContainsKey("txt")) {
			clsUnit Txt = new clsUnit("txt", iq.AddTranslation("Text", English, "UNITS", 0, null, 0, false), "*", 0);
		}

		if (!iq.i_unit_code.ContainsKey("num")) {
			clsUnit num = new clsUnit("num", iq.AddTranslation("number", English, "UNITS", 0, null, 0, false), "", 0);
		}

		if (!iq.i_unit_code.ContainsKey("year")) {
			clsUnit num = new clsUnit("year", iq.AddTranslation("year", English, "UNITS", 0, null, 0, false), "", 0);
		}


		if (!iq.i_unit_code.ContainsKey("hour")) {
			clsUnit num = new clsUnit("hour", iq.AddTranslation("hour", English, "UNITS", 0, null, 0, false), "", 0);
		}


		if (!iq.i_unit_code.ContainsKey("U")) {
			clsUnit U = new clsUnit("U", iq.AddTranslation("U", English, "UNITS", 0, null, 0, false), "U", 0);
		}

		if (!iq.i_unit_code.ContainsKey("Feet")) {
			clsUnit ft = new clsUnit("Feet", iq.AddTranslation("Feet", English, "UNITS", 0, null, 0, false), "ft", 0);
		}

		if (!iq.i_unit_code.ContainsKey("Inch")) {
			clsUnit Inch = new clsUnit("Inch", iq.AddTranslation("Inch", English, "UNITS", 0, null, 0, false), "in", 0);
		}

		if (!iq.i_unit_code.ContainsKey("mm")) {
			clsUnit Milimeter = new clsUnit("mm", iq.AddTranslation("Milimeters", English, "UNITS", 0, null, 0, false), "mm", 0);
		}

		if (!iq.i_unit_code.ContainsKey("cm")) {
			clsUnit Centimeter = new clsUnit("cm", iq.AddTranslation("Centimeter", English, "UNITS", 0, null, 0, false), "cm", 0);
		}

		if (!iq.i_unit_code.ContainsKey("W")) {
			clsUnit Watt = new clsUnit("W", iq.AddTranslation("Watts", English, "UNITS", 0, null, 0, false), "W", 0);
		}

		if (!iq.i_unit_code.ContainsKey("kW")) {
			clsUnit KW = new clsUnit("kW", iq.AddTranslation("KiloWatt", English, "UNITS", 0, null, 0, false), "kW", 0);
		}

		if (!iq.i_unit_code.ContainsKey("kg")) {
			clsUnit Int = new clsUnit("kg", iq.AddTranslation("kg", English, "UNITS", 0, null, 0, false), "kg", 0);
		}

		if (!iq.i_unit_code.ContainsKey("lb")) {
			clsUnit lb = new clsUnit("lb", iq.AddTranslation("lb", English, "UNITS", 0, null, 0, false), "lb", 0);
		}

		if (!iq.i_unit_code.ContainsKey("C")) {
			clsUnit c = new clsUnit("C", iq.AddTranslation("Celcius", English, "UNITS", 0, null, 0, false), "°C", 0);
		}

		if (!iq.i_unit_code.ContainsKey("F")) {
			clsUnit f = new clsUnit("F", iq.AddTranslation("Farenheit", English, "UNITS", 0, null, 0, false), "°F", 0);
		}

		if (!iq.i_unit_code.ContainsKey("RPM")) {
			clsUnit rpm = new clsUnit("RPM", iq.AddTranslation("Revolutions per minute", English, "UNITS", 0, null, 0, false), "r/min", 0);
		}

		if (!iq.i_unit_code.ContainsKey("Gbyte")) {
			clsUnit rpm = new clsUnit("Gbyte", iq.AddTranslation("Gigabytes", English, "UNITS", 0, null, 0, false), "GB", 0);
		}

		if (!iq.i_unit_code.ContainsKey("Gbit")) {
			clsUnit rpm = new clsUnit("Gbit", iq.AddTranslation("Gigabits", English, "UNITS", 0, null, 0, false), "Gb", 0);
		}

		if (!iq.i_ProductType_Code.ContainsKey("none")) {
			clsProductType npt = new clsProductType("none", iq.AddTranslation("none", English, "Prod", 0, null, 0, false), 0);
		}



		if (!iq.i_slotType_Code.ContainsKey("none")) {
			clsSlotType nst = new clsSlotType("none", "none", iq.AddTranslation("none", English, "slot", 0, null, 0, false));
		}

		if (!iq.i_slotType_Code.ContainsKey("CPU") && !iq.i_slotType_Code.ContainsKey("GEN_CPU ")) {
			clsSlotType nst = new clsSlotType("CPU", "GEN_CPU", iq.AddTranslation("CPU", English, "slot", 0, null, 0, false));
		}

		//   If Not iq.Variants.ContainsKey(-1) Then
		// Dim npt As clsProduct = iq.Products.Where(Function(f) f.Value.sku = "none").Select(Function(f) f.Value).FirstOrDefault
		// If npt Is Nothing Then
		// npt = New clsProduct("none", False, iq.i_sector_code("NoSector"), iq.i_ProductType_Code("none"), New Date(2014, 1, 1), New Date(2080, 1, 1), True, False, True)
		// End If

		//Dim a = New clsVariant("none", npt, HP, "1234", "None", "", "", iq.i_region_code("XW"), False)
		//     End If

		iq.AddTranslation("Unknown", English, "avail", 0, null, 0, false);
		iq.AddTranslation("Unstocked", English, "avail", 0, null, 0, false);
		iq.AddTranslation("Unstocked Variant", English, "avail", 0, null, 0, false);
		iq.AddTranslation("Hard Drives:", English, "UI", 0, null, 0, false);

		List<string> errormessages = new List<string>();
		if (!iq.i_screens_code.ContainsKey("Base")) {
			clsScreen @base = new clsScreen("branch", "Base", "Base Grid", errormessages);
			clsField fld;
			fld = new clsField(@base, "ID", "", iq.AddTranslation("id", English, "FLDLBL", 1, null, 0, false), "Primary key", null, iq.i_inputType_code("string"), 10, 1, 10,
			10, "", true, false, "", "", 1, null, "", false,
			null, true);
			fld = new clsField(@base, "Product.i_Attributes_Code(MfrSKU)(0)", "", iq.AddTranslation("Part Number", English, "FLDLBL", 1, null, 0, false), "HP Part Number", null, iq.i_inputType_code("string"), 10, 1, 10,
			10, "", true, false, "", "", 1, null, "", false,
			null, true);
		}

		//Add any missing rights
		if (!iq.i_right_Code.ContainsKey("GLOBALADM"))
			object r = new clsRight("GLOBALADM", iq.AddTranslation("Global Administrator", English, "Rights", 0, null, 0, false));
		if (!iq.i_right_Code.ContainsKey("TAKEOVER"))
			object r = new clsRight("TAKEOVER", iq.AddTranslation("Takeover Session ", English, "Rights", 0, null, 0, false));
		if (!iq.i_right_Code.ContainsKey("PWDRESET"))
			object r = new clsRight("PWDRESET", iq.AddTranslation("Password Reset", English, "Rights", 0, null, 0, false));
		if (!iq.i_right_Code.ContainsKey("DISABLEUSR"))
			object r = new clsRight("DISABLEUSR", iq.AddTranslation("Disable User", English, "Rights", 0, null, 0, false));
		if (!iq.i_right_Code.ContainsKey("ENABLEUSR"))
			object r = new clsRight("ENABLEUSR", iq.AddTranslation("Enable User", English, "Rights", 0, null, 0, false));
		if (!iq.i_right_Code.ContainsKey("CREATEUSR"))
			object r = new clsRight("CREATEUSR", iq.AddTranslation("Create User", English, "Rights", 0, null, 0, false));
		if (!iq.i_right_Code.ContainsKey("VIEWALL"))
			object r = new clsRight("VIEWALL", iq.AddTranslation("View All Products", English, "Rights", 0, null, 0, false));
		if (!iq.i_right_Code.ContainsKey("TREEVIEW"))
			object r = new clsRight("TREEVIEW", iq.AddTranslation("Treeview Access", English, "Rights", 0, null, 0, false));
		if (!iq.i_right_Code.ContainsKey("DIAGVIEW"))
			object r = new clsRight("DIAGVIEW", iq.AddTranslation("Diagnositcs View", English, "Rights", 0, null, 0, false));
		if (!iq.i_right_Code.ContainsKey("FULLDIST"))
			object r = new clsRight("FULLDIST", iq.AddTranslation("Full Distributor Access", English, "Rights", 0, null, 0, false));
		if (!iq.i_right_Code.ContainsKey("EDITTREE"))
			object r = new clsRight("EDITTREE", iq.AddTranslation("Edit and Add to Tree Structure", English, "Rights", 0, null, 0, false));
		if (!iq.i_right_Code.ContainsKey("SEETEST"))
			object r = new clsRight("SEETEST", iq.AddTranslation("View additional test Variants (AA - equiveilent)", English, "Rights", 0, null, 0, false));
		if (!iq.i_right_Code.ContainsKey("SHOWERRORS"))
			object r = new clsRight("SHOWERRORS", iq.AddTranslation("Enable show errors", English, "Rights", 0, null, 0, false));
		if (!iq.i_right_Code.ContainsKey("SHOWALL"))
			object r = new clsRight("SHOWALL", iq.AddTranslation("Access show all products", English, "Rights", 0, null, 0, false));
		if (!iq.i_right_Code.ContainsKey("ADMINMENU"))
			object r = new clsRight("ADMINMENU", iq.AddTranslation("Access show admin menu", English, "Rights", 0, null, 0, false));


		//        If Not iq.i_right_Code.ContainsKey("EXPORTGRID") Then Dim r = New clsRight("EXPORTGRID", iq.AddTranslation("Export grid as CSV file", English, "Rights", 0, Nothing, 0, False))

		if (!iq.i_role_Code.ContainsKey("ADMIN"))
			object r = new clsRole("ADMIN", iq.AddTranslation("Administrator", English, "Roles", 0, null, 0, false));
		if (!iq.i_role_Code("ADMIN").i_right_code.ContainsKey("GLOBALADM"))
			iq.i_role_Code("ADMIN").AddRight(iq.i_right_Code("GLOBALADM"));
		if (!iq.i_role_Code("ADMIN").i_right_code.ContainsKey("DIAGVIEW"))
			iq.i_role_Code("ADMIN").AddRight(iq.i_right_Code("DIAGVIEW"));
		if (!iq.i_role_Code("ADMIN").i_right_code.ContainsKey("TREEVIEW"))
			iq.i_role_Code("ADMIN").AddRight(iq.i_right_Code("TREEVIEW"));
		if (!iq.i_role_Code("ADMIN").i_right_code.ContainsKey("TAKEOVER"))
			iq.i_role_Code("ADMIN").AddRight(iq.i_right_Code("TAKEOVER"));
		if (!iq.i_role_Code("ADMIN").i_right_code.ContainsKey("VIEWALL"))
			iq.i_role_Code("ADMIN").AddRight(iq.i_right_Code("VIEWALL"));
		if (!iq.i_role_Code("ADMIN").i_right_code.ContainsKey("SHOWALL"))
			iq.i_role_Code("ADMIN").AddRight(iq.i_right_Code("SHOWALL"));
		if (!iq.i_role_Code("ADMIN").i_right_code.ContainsKey("SHOWERRORS"))
			iq.i_role_Code("ADMIN").AddRight(iq.i_right_Code("SHOWERRORS"));

		if (!iq.i_role_Code.ContainsKey("EDITOR"))
			object r = new clsRole("EDITOR", iq.AddTranslation("Editor", English, "Roles", 0, null, 0, false));
		if (!iq.i_role_Code("EDITOR").i_right_code.ContainsKey("EDITTREE"))
			iq.i_role_Code("EDITOR").AddRight(iq.i_right_Code("EDITTREE"));
		if (!iq.i_role_Code("EDITOR").i_right_code.ContainsKey("VIEWALL"))
			iq.i_role_Code("EDITOR").AddRight(iq.i_right_Code("VIEWALL"));
		if (!iq.i_role_Code("EDITOR").i_right_code.ContainsKey("TREEVIEW"))
			iq.i_role_Code("EDITOR").AddRight(iq.i_right_Code("TREEVIEW"));

		if (!iq.i_role_Code.ContainsKey("USER"))
			object r = new clsRole("USER", iq.AddTranslation("Basic User", English, "Roles", 0, null, 0, false));

		if (!iq.i_role_Code.ContainsKey("DISTADMIN"))
			object r = new clsRole("DISTADMIN", iq.AddTranslation("Distributor Admin", English, "Roles", 0, null, 0, false));
		if (!iq.i_role_Code("DISTADMIN").i_right_code.ContainsKey("FULLDIST"))
			iq.i_role_Code("DISTADMIN").AddRight(iq.i_right_Code("FULLDIST"));
		if (!iq.i_role_Code("DISTADMIN").i_right_code.ContainsKey("DISABLEUSR"))
			iq.i_role_Code("DISTADMIN").AddRight(iq.i_right_Code("DISABLEUSR"));
		if (!iq.i_role_Code("DISTADMIN").i_right_code.ContainsKey("CREATEUSR"))
			iq.i_role_Code("DISTADMIN").AddRight(iq.i_right_Code("CREATEUSR"));
		if (!iq.i_role_Code("DISTADMIN").i_right_code.ContainsKey("PWDRESET"))
			iq.i_role_Code("DISTADMIN").AddRight(iq.i_right_Code("PWDRESET"));
		if (!iq.i_role_Code("DISTADMIN").i_right_code.ContainsKey("ADMINMENU"))
			iq.i_role_Code("DISTADMIN").AddRight(iq.i_right_Code("ADMINMENU"));

		if (!iq.i_role_Code.ContainsKey("SUPPORT"))
			object r = new clsRole("SUPPORT", iq.AddTranslation("Support", English, "Roles", 0, null, 0, false));
		if (!iq.i_role_Code("SUPPORT").i_right_code.ContainsKey("SHOWALL"))
			iq.i_role_Code("SUPPORT").AddRight(iq.i_right_Code("SHOWALL"));
		if (!iq.i_role_Code("SUPPORT").i_right_code.ContainsKey("SHOWERRORS"))
			iq.i_role_Code("SUPPORT").AddRight(iq.i_right_Code("SHOWERRORS"));
		if (!iq.i_role_Code("SUPPORT").i_right_code.ContainsKey("ADMINMENU"))
			iq.i_role_Code("SUPPORT").AddRight(iq.i_right_Code("ADMINMENU"));





		//SpecialBranches - is a crap idea and a stupid name
		//'CPQ branch
		if (!iq.i_SpecialBranches.ContainsKey("cpqroot")) {
			object r = new clsBranch(null, null, iq.AddTranslation("CPQ Root", English, "Root", 0, null, 0, false), "", iq.AddTranslation("Roots", English, "Root", 0, null, 0, true), iq.AddTranslation("Root", English, "Root", 0, null, 0, true), null, 0, true, "B",
			null, 0);
			da.DBExecutesql(da.OpenDatabase(), "INSERT INTO SpecialBranch VALUES ('cpqroot'," + r.ID + ")");
			iq.i_SpecialBranches.Add("cpqroot", r);
		}

	}

	public Literal ErrorDymo(string message, UInt64 lid = 0, bool dismissable = false, string extraStyle = "")
	{
		//Returns a red Dymo-Tape style error message
		Literal lit = new Literal();
		bool displayErrors = true;

		//Dont do this otherwise we can't use the 'force' parameter (on outputerrors) to output critical errors 
		//The whole thing needs a bit of a rethink - with them going into the error log, having a 'severity' - and probably being list of some errrorCls (with timestamp, severity, errornumber, message, callstack - etc)
		//If lid > 0 Then
		// displayErrors = CType(iq.sesh(lid, "ErrorDisplay"), Boolean)
		// End If

		//ML - have removed the screen display on Rob's request for UAT, things will appear in the audit log now with code Dymo.
		// AuditLog.Instance.Add(AuditType.Warning, message, "Dymo", lid)

		if (displayErrors) {
			lit.Text = "<div><span class='errorLabel'";
			if (extraStyle != "")
				lit.Text += " style='" + extraStyle + "'";
			lit.Text += ">" + message;
			if (dismissable) {
				lit.Text += MakeRoundButton("Dismiss.png", "Ignore this error", "this.parentNode.parentNode.style.display='none'", "", "", English).Text;
			}
			lit.Text += "</span></div>";
		}

		return lit;

	}


	public void Logit(l, bool reset = false, bool flush = false)
	{
		//Logs acitivity messages to a file
		//Reset empties the file (and writes the 1 new line) (typicall called at the beginning)
		//Flush forces buffered conents to be written to the file. (typicall called at the end)
		//actually writing to the file (appending) is very slow (well, several milliseconds) - so we have a rotating buffer of 50 lines
		//and only do the acutal write once every 50 lines (or if we explicitly flush)

		//Exit Sub

		AuditLog.Instance.Add(AuditType.Information, l, "", 0);
		return;

		static string[] line = new string[499];
		//static variables keep their values between calls
		static int linepointer;

		line(linepointer) = l;
		linepointer += 1;


		if (reset) {
			object Sw = new StreamWriter("c:\\temp\\import.log", !reset);
			Sw.WriteLine(l);
			Sw.Close();


		} else {

			if (flush | linepointer == 501) {
				object Sw = new StreamWriter("c:\\temp\\import.log", !reset);
				for (int i = 0; i <= linepointer - 1; i++) {
					Sw.WriteLine(line(i));
				}
				Sw.Close();
				linepointer = 0;

			}
		}

	}

	//                             hello                 fr      bonjour
	//Dim xlate As New Dictionary(Of String, Dictionary(Of String, String))

		//see BtnImport_click 
	public Dictionary<string, Dictionary<string, string>> xlate;

	public string XmlEscape(l)
	{

		//replaces some special HTML &thing; markups with their unicode equivilents
		//Removes any remaining ampersands

		l = Replace(l, "&plusmn;", "^#x00B1;", , , Microsoft.VisualBasic.CompareMethod.Text);

		l = Replace(l, "&", "&#038");

		XmlEscape = Replace(l, "^", "&");

	}

	public string Xlt(string ky, clsLanguage language)
	{

		string kyCompositeKey = ky + "^UI";
		if (iq.KYIndex.ContainsKey(kyCompositeKey)) {
			//we have already created a translation  object for this kie
			clsTranslation tlo = iq.KYIndex(kyCompositeKey);
			if (tlo.Group == "") {
				tlo.Group = "UI";
				tlo.Update(language);
				if (KYlanguage != null)
					tlo.Update(KYlanguage);
			}

			//Return UCase(iq.KYIndex(ky).text(language))  ' if the language version is not present - it will return the EN version then the KY version
			if ((object.ReferenceEquals(language, English))) {
				return iq.KYIndex(kyCompositeKey).text(language);
			} else {
				return iq.KYIndex(kyCompositeKey).text(language);
			}
		//Use the ucase version above to SHOUT the things we dont have translations for

		} else {

			if (ky.Trim().Length > 0 & (object.ReferenceEquals(language, KYlanguage) | object.ReferenceEquals(language, English))) {
				// NICK NOBBLED FOR NOR    
				clsTranslation tl = new clsTranslation(KYlanguage, ky, "UI", 0);
				if (object.ReferenceEquals(language, English)) {
					tl = iq.AddTranslation(ky, English, "UI", 0, null, 0, false);
					//New clsTranslation(English, ky, "UI", 0)
				}

				object cl = iq.KYIndex(kyCompositeKey).text(language);
				if (object.ReferenceEquals(language, English)) {
					return cl != null ? iq.KYIndex(kyCompositeKey).text(language) : "Translation for key and group " + kyCompositeKey + " language ID " + language.ID + "is missing";
				} else {
					return cl != null ? iq.KYIndex(kyCompositeKey).text(language) : "Translation for key and group " + kyCompositeKey + " language ID " + language.ID + "is missing";
				}
			} else {
				return ky;
			}

		}
	}
	/// <summary></summary>
	/// <param name="ky"></param>
	/// <param name="text"></param>
	/// <param name="language"></param>
	/// <returns></returns>
	/// <remarks></remarks>

	public bool AddXlt(string ky, string text, clsLanguage language)
	{
		if (iq.KYIndex.ContainsKey(ky)) {
			if (!object.ReferenceEquals(language, KYlanguage)) {
				clsTranslation kyTrans = iq.KYIndex(ky);
				if (kyTrans.textTranslation(language) == "") {
					kyTrans.addLanguage(language, text, null);
				} else {
					if (kyTrans.delete(language)) {
						kyTrans.addLanguage(language, text, null);
					}
				}
			}
			return true;
		} else {
			if (object.ReferenceEquals(language, KYlanguage)) {
				clsTranslation kyTrans = new clsTranslation(language, ky, "UI", 0);
				return true;
			} else if (object.ReferenceEquals(language, English)) {
				clsTranslation kyTrans = new clsTranslation(iq.i_language_Code("KY"), ky, "UI", 0);
				kyTrans.addLanguage(language, text, null);
				return true;
			} else {
				return false;
			}
		}
	}
	public Literal NewLit(l)
	{
		NewLit = new Literal();
		NewLit.Text = l;
	}
	public string NullIt(object o)
	{

		//Returns NULL or a 'quoted' (and encoded) value (for strings) or an unquoted value for non strings - suitable for INSERTing

		NullIt = null;


		if (o == null)
			return "NULL";
		if (IsDBNull(o))
			return "NULL";

		if ((o) is int) {
			return o.ToString;
		}
		if ((o) is string) {
			return da.SqlEncode(o.ToString);
		}
		if ((o) is nullableString) {
			return da.SqlEncode(o.DisplayValue);
		}

		System.Diagnostics.Debugger.Break();

	}



	public void Serialize(object O, filename)
	{
		StreamWriter objStreamWriter = new StreamWriter(filename);
		XmlSerializer x = new XmlSerializer(O.GetType);
		x.Serialize(objStreamWriter, O);
		objStreamWriter.Close();

	}

	public string GeneratePassword()
	{

		Randomize(Math.Sin(Now.Millisecond / 287) * 100);
		//Seeding just with the timer wouldn't be very secure

		object c;

		c = "ghjabcdefkmnpqrstwxy23456878";
		//we don't use U's V's 1's I's or O's

		object pw = "";
		for (i = 1; i <= 8; i++) {
			pw += Mid(c, 1 + Rnd(1) * (Len(c) - 2), 1);
		}
		pw = Left(pw, 4) + "-" + Mid(pw, 5);
		//Split with a dash - for something of the form JK5-LTA
		return pw;

	}


	public void DiscardUnChangedQuote(UInt64 lid)
	{

		clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");


		if (buyerAccount != null) {
			List<clsQuote> todel = new List<clsQuote>();
			clsState state_new = iq.i_state_GroupCode("QT-#NW");

			//discards empty new quotes
			foreach ( q in buyerAccount.Quotes.Values) {
				if (object.ReferenceEquals(q.State, state_new) & q.RootItem.Children.Count == 0) {
					todel.Add(q);
					//we can't directly delete them (becuase we're iteratring the collection we'd be removing them from)
				}
			}

			foreach ( q in todel) {
				if (q.ID == iq.sesh(lid, "QuoteID")) {
					iq.getSeshDic(lid).Remove("QuoteID");
					//We just discarded a quote we'd started (and added nothing to)

				}

				q.delete();
			}

		}

	}

	public string UiTrans(l)
	{

		UiTrans = l;
		//Pre login translatin function


	}
	public string md5(i)
	{

		SqlConnection con;
		con = da.OpenDatabase();
		SqlClient.SqlDataReader rdr;

		rdr = da.DBExecuteReader(con, "select dbo.md5(" + dataAccess.da.SqlEncode(i) + ")");
		rdr.Read();
		md5 = rdr.Item(0);
		rdr.Close();
		con.Close();



	}
	public string Shuffle(i)
	{
		if (Len(i) == 35)
			i = Left(i, 32);
		if (Len(i) < 32)
			return "";
		//Performs a simple (non commutative) 3 way cut/shuffle on a 32 byte MD5 hash
		//so that we're no longer storing 'standard' (easily reversible via dictionary lookup) MD5 hashes.
		//A hacker would now have to have the database, and have and Dissassemble the DLL's to find out passwords

		object r = i;
		r = Mid(i, 10, 10) + Mid(i, 1, 10) + Mid(i, 21);

		return r;

	}




	public void ShowError(Panel ph, string text)
	{
		Label em;
		em = new Label();
		ph.Controls.Add(em);
		em.BackColor = Drawing.Color.Red;
		em.ForeColor = Drawing.Color.White;
		em.Text = text;

	}

	public string TimeSince(ref double LastMilestone)
	{

		//Uses the system.diagnositic.stopwatch to return the elapsed time in Milliseconds (formatted to two decimal places) since LastMilestone
		//IMPORTANT: - this UPDTAES last milestone (to the current time.. so you can call is many times to measure the time between stages)

		double TimeNow;
		TimeNow = Stopwatch.GetTimestamp();
		TimeSince = ((TimeNow - LastMilestone) / Stopwatch.Frequency * 1000).ToString("0.00") + "ms";
		LastMilestone = TimeNow;

	}

	//Public Function FindProductPaths(id As Integer) As Dictionary(Of String, String)

	//    'fetches the paths to all occurances of the SKU
	//    'returns a dictionary of Paths:PathNames

	//    FindProductPaths = New Dictionary(Of String, String)

	//    Dim con As SqlClient.SqlConnection
	//    con = da.opendatabase()
	//    Dim rdr As SqlClient.SqlDataReader
	//    Dim path$

	//    rdr = da.dbexecuteReader(con, "SELECT [path] FROM [ProductPath] WHERE fk_product_id=" & id & ";")
	//    If rdr.HasRows Then
	//        While rdr.Read
	//            path$ = rdr.Item("Path")
	//            FindProductPaths.Add(path$, PathName(path$, English))
	//        End While
	//    Else
	//        FindProductPaths.Add("", "[ProductPath] table is not populated - see default.aspx Index SKU's button")
	//    End If
	//    rdr.Close()
	//    con.Close()

	//End Function

	public Panel KWbreadcrumbs(UInt64 lid, path, clsLanguage language, bool isoptionsSearch, bool greyed, string rfh, bool isDiagView)
	{

		//returns a panel containing clickable divs to every segment in the path 

		KWbreadcrumbs = new Panel();
		KWbreadcrumbs.CssClass = "KWbreadcrumbs";

		[] p;
		p = Split(path, ".");

		clsBranch branch;
		int lowerbound = 1;
		object pth = p(0);
		string sysPath = "";
		//We present slightly modified breadcrumbs - only from the system.. down to the option(s)
		if (isoptionsSearch & !isDiagView) {
			lowerbound = UBound(p) - 2;
			for (i = 1; i <= lowerbound - 1; i++) {
				pth += "." + p(i);
				if (iq.Branches(p(i)).Product != null && iq.Branches(p(i)).Product.isSystem)
					sysPath = pth;
			}
		} else {
			lowerbound = 2;
			//nick
		}

		//The 0'th item is 'tree'  (from tree.2.7.910.2005 etc) - the 1st is "Root Branch"
		for (i = lowerbound; i <= UBound(p); i++) {
			Panel seg = new Panel();

			if (greyed) {
				seg.CssClass = "disabledKWcrumb";
				//Inline-block
			} else {
				seg.CssClass = "KWcrumb";
				//Inline-block
			}

			KWbreadcrumbs.Controls.Add(seg);
			pth += "." + p(i);

			string pdesc = "";

			branch = iq.Branches(p(i));
			if (!branch.Product == null) {
				if (branch.Product.i_Attributes_Code.ContainsKey("desc")) {
					clsProductAttribute pa = branch.Product.i_Attributes_Code("desc")(0);
					pdesc = pa.Translation.text(language).Replace("[mfr]", branch.Product.mfrCode);
				}
			}

			if (greyed) {
				seg.ToolTip = "Unavailable product (" + rfh + ")";
			} else {
				seg.ToolTip = pdesc;
			}


			string root = iq.sesh(lid, "Root");
			//seg.Attributes("onclick") = "hideKeywordSearchResults();getBranches('path=" & pth & "&cmd=open&into=tree');return false;"

			//The difference between these and 'normal' breadcrumbs is that the branches are not yet open.
			//See - proccesCommand() in showchildren.aspx

			string pathToSeg = Join(p.Take(i + 1).ToArray, ".");

			//disabled (greyed) lines have no onclick events (because navigating to things not in the portfolio yields a host of problems)
			//we remain in the configuringSystem paradigm
			if (isoptionsSearch) {
				seg.Attributes("onclick") = string.Format("hideKeywordSearchResults();getBranches('cmd=open&path={0}&to={1}&into=tree');return false;", sysPath, pathToSeg);
			} else {
				if (iq.Branches(Split(path, ".").Last).Product != null && iq.Branches(Split(path, ".").Last).Product.isSystem && pathToSeg == path) {
					//clicked on a system (this MUST BE) a systems search
					seg.Attributes("onclick") = string.Format("hideKeywordSearchResults();getBranches('cmd=open&path=tree.1&to={0}&Paradigm=B&into=tree&showOnly={1}');return false;", pathToSeg, p.Last);
				// Breadcrumbs root item
				} else if (i == lowerbound) {
					seg.Attributes("onclick") = string.Format("hideKeywordSearchResults();getBranches('cmd=open&path={0}&configuration=0&Paradigm=B&into=tree');return false;", pathToSeg);
				} else {
					//clicked on something that isn't a system - whilst in a system search
					//we need to change paradigm to enumparadigm.addingsystem (aka Browsing)
					seg.Attributes("onclick") = string.Format("hideKeywordSearchResults();getBranches('cmd=open&Paradigm=B&path=tree.1&to={0}&into=tree&configuration=0');return false;", pathToSeg);
				}
			}

			seg.Controls.Add(NewLit(" ▶" + branch.DisplayName(language)));

			if (isDiagView) {
				if (branch.Product != null && branch.Product.i_Attributes_Code.ContainsKey("FamMinor")) {
					seg.Controls.Add(NewLit(" (" + branch.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English) + ")"));
				}
			}

			//seg.Attributes("title") = pathToSeg

			if (p(i) == p.Last) {
				object LINETEXT = pdesc;
				if (LINETEXT != "") {
					int MAXLEN = 150;
					if (Len(LINETEXT) > MAXLEN)
						LINETEXT = Left(LINETEXT, MAXLEN) + "..";
					//this is the light grey description 'result' (which is set earlier into the tooltip on the last segment)
					seg.Controls.Add(NewLit("<div CLASS='searchLine'>" + LINETEXT + "</div>"));
				}
			}

		}

	}

	public string FQP(path, clsBranch branch)
	{

		//returns the fully qualified path from a specified branch and 'address'
		//For example .. from tree.123.345  will return tree.123
		//at present only simple .. and ... operations are supported


		if (path == ".." | path == "...") {
			string[] p;
			p = Split(path, ".");

			object r = "";
			for (i = 0; i <= UBound(p) - (Len(path) - 1); i++) {
				r += p(i) + ".";
			}

			r = Left(r, Len(r - 1));

			return r;
		} else {
			return path;
		}

	}

	public Panel MakeSpannedHeader(string Styleprefix, l)
	{

		MakeSpannedHeader = new Panel();

		[] p = Split(l, ",");
		string celltext;


		for (int i = 0; i <= UBound(p); i++) {
			celltext = p(i);
			if (Left(celltext, 1) == "!")
				celltext = "";
			//the title is not displayed for any column starting with a ! (however it still has a css class)

			MakeSpannedHeader.Controls.Add(NewLit("<div class='" + Styleprefix + Trim(p(i)) + "'>" + celltext + "</div>"));
		}

	}
	public TableHeaderRow MakeTHR(l, string tooltips, string css)
	{
		//Constructs a table header row from a comma delimited list

		MakeTHR = new TableHeaderRow();

		[] p = Split(l, ",");

		[] t;
		t = Split(tooltips, "|");

		TableCell acell;
		for (i = 0; i <= UBound(p); i++) {
			acell = new TableHeaderCell();
			acell.Text = p(i);

			if (UBound(t) > 0 & i < t.Length - 1) {
				acell.ToolTip = t(i);
			}

			MakeTHR.Cells.Add(acell);

		}

		MakeTHR.CssClass += " " + css;

		//job done - return value is the completed table header row <TH>

	}

	public Panel outputMessages(List<string> msgs)
	{


		outputMessages = new Panel();
		Literal lit;
		foreach ( msg in msgs) {
			lit = new Literal();
			lit.Text = "<div><span class='messageLabel'>" + msg;
			lit.Text += MakeRoundButton("Dismiss.png", "Ignore this error", "this.parentNode.parentNode.style.display='none';return(false);", "", "", English).Text;
			lit.Text += "</span></div>";
			outputMessages.Controls.Add(lit);
		}

	}



	public void OutputErrors(ref ControlCollection cnts, ref List<string> msgs, UInt64 lid, bool Force = false)
	{
		//Anything starting with a * will only appear to users with "ErrorDispay" on (for which they need a right to the button)
		Panel OutputErrors;

		bool displayerrors = false;

		if (lid != 0) {
			displayerrors = (bool)iq.sesh(lid, "ErrorDisplay");
		}

		bool showall = (bool)iq.sesh(lid, "showAll");
		if (showall)
			displayerrors = true;

		OutputErrors = new Panel();
		foreach ( m in msgs) {
			if (!m.StartsWith("*") | displayerrors) {
				cnts.Add(ErrorDymo(m, lid, true));
			}
		}
		cnts.Add(OutputErrors);

		msgs.Clear();

		// End If

	}

	public Panel TreeAddButton(TextBox tb, path, clsBranch branch, clsVariant skuvariant, clsLanguage language, bool enabled, string disabledMessage)
	{
		//PlaceHolder

		//This is the flex up/add button that appears in the product tree
		//NB: there is no corresopnding flex down button as items cannot be flexed down via the product tree (only in the basket - as the basket may contain multiple instances of th eproduct in question))

		//Dim p As New PlaceHolder
		TreeAddButton = new Panel();

		// If adding isn't enabled (e.g. HPI/HPE split), set the style/message and exit before setting up the add to basket script
		if (!enabled) {
			TreeAddButton.CssClass = "treeAddButtonDisabled UI";
			TreeAddButton.Attributes.Add("onmousedown", string.Format("burstBubble(event); displayAddMsg('{0}', '{1}');", tb.ID, disabledMessage));
			return;
		}

		object script;

		TreeAddButton.CssClass = "treeAddButton UI";

		//we pass 'absolute' as false - to say add this *relative* amount to the quote

		//changeQty(boxID, itemID,path,SKUvariantID,absolute) {
		script = "burstBubble(event);hideKeywordSearchResults();setToOneIfBlank('" + tb.ID + "');sourceQty='" + tb.ID + "';";

		string productSystem = "";

		if (branch.Product.isSystem(path)) {
			productSystem = "true";
		} else {
			productSystem = "false";
		}


		script += "changeQty('" + tb.ID + "',0,'" + path + "'," + skuvariant.ID + ",false, " + productSystem + ");blank('" + tb.ID + "');return false;";
		//Automatic 'show in tree' if a system is added (to the basket)
		//If branch.Product.isSystem Then
		//setTimeout (function(){fillPrices('" & path & "'," & requestHandle & ");return false;},3000)"   'fill Prices Calls GetPriceUis.aspx for - 3000 gives the page time to render - any less time and the filling becomes unreliable
		//script$ &= "setTimeout(function(){getBranches('path=" & path & "&cmd=open&into=tree');return false;},200);"
		// End If
		// script$ &= "return false;"

		//tb.Attributes.Add("onKeyUp", "if(keyIs(e){" & script$ & "};") 'For when they press enter (add the quantity in the textbox to the quote)

		tb.Attributes.Add("onkeydown", "if(event.keyCode==13){" + script + "};");
		//For when they press enter (add the quantity in the textbox to the quote)
		//tb.Attributes("onmousedown") = "burstBubble(event);" 'was on mousedown

		//Dim imgBtnFlexUp As New WebControls.Image 'Button
		TreeAddButton.Attributes("onmousedown") = script;
		//was onclick

		//imgBtnFlexUp.ImageUrl = "/images/navigation/plus.png"
		//imgBtnFlexUp.CssClass = "treeAddButton"
		TreeAddButton.ToolTip = Xlt("Add a quantity to the quote", language);
		// Dim lit As Literal = New Literal
		// lit.Text = "&nbsp;"
		// TreeAddButton.Controls.Add(lit)

		//p.Controls.Add(imgBtnFlexUp)

		//Return p

	}

	public string TranslationKey(clsTranslation t)
	{

		if (t == null)
			return "null";
		//If t Is DBNull.Value Then Stop
		return t.Key;

	}



	public void SendEmail(string toAddress, string templatename, Dictionary<string, string> tags, clsLanguage language, ref List<string> errorMessages, bool HighPriority, System.Net.Mail.Attachment attachment = null)
	{

		//Subject - i pulled form the <subject> tag INSIDE the template
		//Reads the email template (currently from the file system - may move to DB)
		//Replaces the [tags] in the dictionary - which are tag,value pairs

		object e = "";

		string ppath = "";
		string vpath = "";

		try {
			vpath = HttpContext.Current.Request.ApplicationPath;
			ppath = HttpContext.Current.Request.MapPath(vpath);

			StreamReader tr = null;

			try {
				tr = new StreamReader(ppath + "/EMT/" + templatename);
				e = tr.ReadToEnd();
				tr.Close();
			} catch {
				tr.Dispose();
			}

			Regex regex = new Regex("\\|[A-z\\ 0-9]+\\|");
			// ML translate anything between |'s
			object matches = regex.Matches(e);
			foreach (Match m in matches) {
				e = Replace(e, m.Value, iq.AddTranslation(m.Value.Trim("|".ToArray()), English, "Export", 0, null, 0, false).text(language));
			}

			foreach ( t in tags.Keys) {
				e = e.Replace("[" + t + "]", tags(t));
				//brackets are easier to deal with in the IDE (which inisist on trying to close <tags>)
			}

		} catch (System.Exception ex) {
			errorMessages.Add(" Error peparing email ");
			errorMessages.Add("* Vpath:" + vpath);
			errorMessages.Add("* Ppath:" + ppath);

			ErrorLog.Add(ex);

		}

		string subject = "HP iQuote";

		int s = InStr(e, "<subject>");
		if (s) {
			//This is pretty ugly - it just pulls the contents out of the <subject></subject> tag
			subject = Mid(e, InStr(s, e, ">") + 1);
			subject = Left(subject, InStr(subject, "<") - 1);
		}

		MailMessage msg = null;
		System.Net.Mail.SmtpClient smtpclient = null;
		try {
			smtpclient = new System.Net.Mail.SmtpClient();

			//you can't just change the from address to hpiquote.NET !
			msg = new MailMessage(iq.Addresses("iQuoteSupportEmail").Translation.text(English), toAddress, subject, e);
			msg.ReplyToList.Add(new MailAddress(iq.Addresses("iQuoteSupportEmail").Translation.text(English)));
			//msg.Bcc.Add(New MailAddress("nick.axworthy@channelcentral.net"))
			msg.IsBodyHtml = true;

			if (attachment != null) {
				msg.Attachments.Add(attachment);
			}
			if (HighPriority) {
				msg.Priority = MailPriority.High;
			}
		} catch (Exception ex) {
			errorMessages.Add("* Error building mail" + ex.Message);
			errorMessages.Add("Unable to send email at this time");
		}

		if (msg != null & smtpclient != null) {
			try {
				smtpclient.ServicePoint.MaxIdleTime = 1;
				smtpclient.Send(msg);
				msg.Dispose();

			} catch (System.Exception ex) {
				errorMessages.Add("Send email failed");
				errorMessages.Add("* " + ex.Message);
				if (ex.InnerException != null) {
					errorMessages.Add(ex.InnerException.Message);
				}

			}
		}

	}

	public bool SimpleEmail(to, Subject, body)
	{

		SimpleEmail = true;
		System.Net.Mail.SmtpClient smtpclient = null;
		MailMessage msg;
		smtpclient = new System.Net.Mail.SmtpClient();

		msg = new MailMessage("support@channelcentral.net", to, Subject, body);
		//      msg.ReplyToList.Add(New MailAddress("dan.mason@channelcentral.net"))
		// msg.CC.Add(New MailAddress("nick.axworthy@channelcentral.net"))
		msg.IsBodyHtml = true;
		msg.Priority = MailPriority.High;

		try {
			smtpclient.ServicePoint.MaxIdleTime = 1;
			smtpclient.Send(msg);
			msg.Dispose();
		} catch (System.Exception ex) {
			SimpleEmail = false;
		}

	}



	public void postpaint(object chart, ChartPaintEventArgs e)
	{

		if ((e.ChartElement) is Chart) {

			System.Drawing.Graphics g = e.ChartGraphics.Graphics;



			//Dim installedFontCollection As New System.Drawing.Text.InstalledFontCollection()

			// Get the array of FontFamily objects.
			// Dim fontFamilies() As FontFamily
			// fontFamilies = installedFontCollection.Families

			Drawing.Font DrawFont = new System.Drawing.Font("HP simplified w04 regular", 8);
			//System.Drawing.Font.'System.Drawing.SystemFonts.CaptionFont

			System.Drawing.Brush drawbrush = Drawing.Brushes.Black;

			//// see how big the text will be
			// Dim txtWidth As Integer = g.MeasureString(txt, DrawFont).Width
			// Dim TxtHeight As Integer = g.MeasureString(txt, DrawFont).Height
			//// where to draw

			int x = 5;
			int y = (int)e.Chart.Height.Value - 10;

			g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias;
			g.ScaleTransform(1, 1);

			System.Drawing.Point[] v = new System.Drawing.Point[-1];

			g.ResetTransform();

			g.TranslateTransform(x, y);
			g.RotateTransform(-50);
			//        v(0) = New System.Drawing.Point(20, 0)  'a vector of 20 pixels 'accross' in device space (pixels)
			g.TransformPoints(Drawing2D.CoordinateSpace.World, Drawing2D.CoordinateSpace.Device, v);

			g.DrawString(chart.Attributes("text"), DrawFont, drawbrush, 0, 0);
			//x += 100
		}

	}

	public Panel MakeFilterButtons(clsLanguage language)
	{

		//Makes the full set of every possibly filter button
		//The correct ones are subsequently shown/hidden by the JS showFilterButtons

		MakeFilterButtons = new Panel();
		MakeFilterButtons.ID = "filterButtons";
		//the master page prepends ctl00_   !!!!argh !
		MakeFilterButtons.Attributes("Style") = "display:none;position:relative;z-index:150;width:0px;";
		//was none
		MakeFilterButtons.CssClass = "filterButtons moreFilterButtonStyle";
		//MakeFilterButtons.Attributes("Style") &= ""

		object sc;
		sc = "onSpeechBubble=true;return false;";
		MakeFilterButtons.Attributes("onmouseover") = sc;

		sc = "onSpeechBubble=false;display('ctl00_filterButtons','none');return false;";
		MakeFilterButtons.Attributes("onmouseleave") = sc;

		WebControls.Image sib;
		//(in cell - sample filter button)



		//returns the physical path to the file created
		//find the virtual, and from that the physical path
		object vPath = HttpContext.Current.Request.ApplicationPath;
		object pPath = HttpContext.Current.Request.MapPath(vPath);

		Literal lt = new Literal();

		lt.Text = "<!--bPath " + vPath + "-->";
		MakeFilterButtons.Controls.Add(lt);

		lt = new Literal();
		lt.Text = "<!--PPath " + pPath + "-->";
		MakeFilterButtons.Controls.Add(lt);


		//filterKey In iq.i_Filters_Code.Keys 'filters.Keys
		foreach ( f in iq.Filters.Values) {
			sib = new WebControls.Image();
			//Button

			//@@@

			string ih = "";

			if (My.Computer.FileSystem.FileExists(pPath + "/images/navigation/" + f.Code + ".png")) {
				sib.ImageUrl = "/images/navigation/" + f.Code + ".png";
			} else {
				sib.ImageUrl = "/images/navigation/genericfilter.png";
				//" & f & ".png"
				ih = "(missing " + pPath + "/images/navigation/" + f.Code + ".png)";
			}

			sib.ToolTip = f.DisplayText.text(language) + ih;
			//sib.Attributes.Add("filter",  iq.filters(fi))")
			sib.Attributes.Add("code", f.Code);
			sib.ID = "FIB_" + f.Code;
			sib.CssClass = "FB";
			//used to get all of the buttons in the jscript - see showFilterButtons()

			//filterField,filterPath and FilterVale are global variables in iQuote.js - they are set in the onmouseover scripts of the matrixUI

			object occ;
			occ = "burstBubble(event);getBranches('path='+filterPath+'&cmd=changeFilter&filterParams='+ filterField + '|" + f.Code + "|' + filterValue);onSpeechBubble=false;return false;";

			//sib.ToolTip = occ$
			sib.Attributes("onclick") = occ;
			//sib.Attributes("onclick") = occ$
			sib.Width = new Unit(18, UnitType.Pixel);
			MakeFilterButtons.Controls.Add(sib);

		}

	}

	public object NothingFromNull(object v)
	{

		if ((v) is DBNull)
			return null;
		else
			return v;

	}


	public class clsKwScore
	{
		public int majorMatchBits = 0;
		public int minorMatchbits = 0;
			//the total count of fragment macthes in titles, part numbers etc
		public int MajorMatchCount = 0;
			//the total count of fragment matches in things like descriptions, attribtues etc
		public  MinorMatchCount;

	}

	public void SavePromoBranches(List<clsBranch> branches, clsChannel buyerChannel, string type)
	{
		SqlClient.SqlConnection con = da.OpenDatabase();
		DataTable dt = da.MakeWriteCacheFor(con, "PromoBranch");

		System.Data.DataRow row;
		foreach ( branch in branches) {
			row = dt.NewRow;
			row("FK_Branch_id") = branch.ID;
			row("FK_Channel_ID_Buyer") = buyerChannel.ID;
			row("promoType") = type;
			dt.Rows.Add(row);
		}

		da.BulkWrite(con, dt, "PromoBranch");

		con.Close();

	}

	public DateTime PromoUpdated(clsChannel buyerChannel, string type)
	{

		SqlClient.SqlConnection con = da.OpenDatabase();
		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, "SELECT max(timestamp) from PromoScan WHERE fk_channel_id_buyer=" + buyerChannel.ID + " AND [PromoType]='" + type + "'");

		rdr.Read();
		if (IsDBNull(rdr.Item(0))) {
			PromoUpdated = DateAdd(DateInterval.Day, -100, Now);
		} else {
			PromoUpdated = rdr.Item(0);
		}

		rdr.Close();
		con.Close();

	}


	/// <summary>It is expensive to work out which branches feature a valid (possibly deeply descendant) Promo in realtime - so we create/cache a list (of those branches - per buyerChannel) at logon</summary>
	/// <remarks>We do this per buyerchannel (rather than per region).. becuase some of the promo products may be hidden (from a particular buyer) for other reasons (such as not being in the feed)</remarks>

	public void TagPromoBranches(clsAccount buyeraccount, List<string> errormessages)
	{
		float age;

		Int16 dontCheckWebService = buyeraccount.SellerChannel.priceConfig & !8;

		//Avalance, Bundles and Flex
		foreach ( t in Split("A B F")) {

			if (!iq.PromoBranches.ContainsKey(buyeraccount.BuyerChannel))
				iq.PromoBranches.Add(buyeraccount.BuyerChannel, new Dictionary<string, List<clsBranch>>());
			if (!iq.PromoBranches(buyeraccount.BuyerChannel).ContainsKey(t))
				iq.PromoBranches(buyeraccount.BuyerChannel).Add(t, new List<clsBranch>());

			//Just testing, remove!
			if (da.DatabaseAlive) {

				age = DateDiff(DateInterval.Hour, PromoUpdated(buyeraccount.BuyerChannel, t), Now);


				if (age > 4) {
					List<clsBranch> branches;
					branches = new List<clsBranch>();

					da.DBExecutesql("DELETE FROM PromoBranch WHERE promotype='" + t + "' and fk_channel_id_buyer=" + buyeraccount.BuyerChannel.ID);

					//TODO - note we're not checking focus here - so if we were looking for (for example) avalanche within receta - this wouldn't work
					switch (t) {
						case  // ERROR: Case labels with binary operators are unsupported : Equality
"A":
						//branches = iq.RootBranch.checkAvalanche(Nothing, buyeraccount, New List(Of String), "tree." & Trim$(iq.RootBranch.ID), False, dontCheckWebService)  'Circa 3 seconds
						case  // ERROR: Case labels with binary operators are unsupported : Equality
"B":
						//          branches = iq.RootBranch.CheckBundles(buyeraccount, New List(Of String), "tree." & Trim$(iq.RootBranch.ID), False, dontCheckWebService)
						case  // ERROR: Case labels with binary operators are unsupported : Equality
"F":
							iq.RootBranch.checkFlex(buyeraccount, new HashSet<string>(), "tree." + Trim(iq.RootBranch.ID), dontCheckWebService, branches, errormessages);
					}

					//Bulk writes them to the databse
					if (branches != null) {
						if (branches.Count) {
							SavePromoBranches(branches, buyeraccount.BuyerChannel, t);
							da.DBExecutesql("INSERT INTO PromoScan (fk_channel_id_buyer,promotype,timestamp) VALUES (" + buyeraccount.BuyerChannel.ID + ",'" + t + "',getdate());");
						}
					}
				}

				iq.PromoBranches(buyeraccount.BuyerChannel)(t) = loadPromoBranches(buyeraccount.BuyerChannel, t);
			}
		}

	}

	public List<clsBranch> loadPromoBranches(clsChannel BuyerChannel, string type)
	{

		loadPromoBranches = new List<clsBranch>();

		SqlClient.SqlConnection con = da.OpenDatabase();

		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, "SELECT FK_branch_id FROM [PromoBranch] WHERE fk_channel_id_buyer=" + BuyerChannel.ID + " AND [PromoType]='" + type + "'");

		clsBranch branch;
		while (rdr.Read) {
			int bid = rdr.Item("fk_branch_id");
			branch = iq.Branches(bid);
			loadPromoBranches.Add(iq.Branches(rdr.Item("fk_branch_id")));
		}
		rdr.Close();

		con.Close();

	}

	public string xmlEncode(l)
	{


		xmlEncode = HttpUtility.HtmlEncode(l);

		return;

		//There's probably some intrinsic way to do this - but after 10 minutes of googling i counldn't find it - so here we go
		if (l != "") {
			string[] p;
			//create and iterate a list of the symbol=replacement pairs
			foreach ( sr in Split("&=&amp;|<=&lt;|>=&gt;|'=&apos;|" + Chr(34) + "=&quot;", "|")) {
				p = Split(sr, "=");
				//split the sybmol into p(0), and it's replacement into p(1) at the "="
				l = Replace(l, p(0), p(1));
				//there, that was easy wasn't it  (all the complexities of replacing &'s with &'s are handled for us
			}
		}

		return l;
		//tada

	}

	public string rdus(l)
	{

		//replaces all dots with underscores (for valid control names) based on paths
		return Replace(l, ".", "_");

	}

	public clsBranch FindBranchByName(path, name)
	{

		FindBranchByName = null;

		string[] segs = Split(path, ".");
		clsBranch branch;
		for (i = UBound(segs); i >= 1; i += -1) {
			branch = iq.Branches(segs(i));
			if (branch.Translation != null && branch.Translation.text(English) == name)
				return branch;
		}

		return null;
	}


	public clsBranch FindSystemBranch(path)
	{

		FindSystemBranch = null;

		string[] segs = Split(path, ".");
		clsBranch branch;
		for (i = UBound(segs); i >= 1; i += -1) {
			branch = iq.Branches(segs(i));
			if (branch.Product != null) {
				if (branch.Product.isSystem & !branch.Product.isOption)
					return branch;
				//DONT find tabe drives !
			}
		}

	}

	public clsScreenHeader matrixHeaderAbove(UInt64 lid, path, ref List<string> errormessages)
	{

		Dictionary<string, clsScreenHeader> mh = iq.sesh(lid, "screenHeaders");
		object ppath = path;

		//If ppath = "tree" Or ppath = "" Or ppath = "swift" Then Return New clsScreenHeader(lid, path, iq.Screens(719))

		do {
			clsBranch branch;
			branch = iq.Branches((int)Split(ppath, ".").Last);
			if (branch.Product != null) {
				if (branch.Product.isSystem) {
					if (path == ppath) {
						ppath = oneAbove(ppath);
					} else {
						break; // TODO: might not be correct. Was : Exit Do
						//do NOT cross systems when looking back up through to find the effectivematrix
					}
				}
			}

			if (mh.ContainsKey(ppath)) {
				matrixHeaderAbove = mh(ppath);
				return;
			}
			ppath = oneAbove(ppath);
			if (ppath == "tree" | ppath == "" | ppath == "swift")
				break; // TODO: might not be correct. Was : Exit Do
		} while (true);

		//errormessages.Add("couldn't locate matrixHeaderAbove" & path$)
		// Return Nothing
		return null;
		//New clsScreenHeader(path, iq.Screens(719))

	}


	public clsScreen MatrixAbove(UInt64 lid, path)
	{

		//returns the first ClsScreen referenced by a branch above the path 

		int i;
		clsScreen matrix = null;
		clsBranch branch = null;
		object pth = path;
		object sysabove = isSystemAbove(lid, path);
		string[] segs = path.Split(".");
		//note.. seg(0) is 'tree'
		for (i = segs.Count - 1; i >= 1; i += -1) {
			pth = Left(pth, Len(pth) - Len(segs(i)) - 1);
			if (pth == "tree" || isSystemAbove(lid, pth) != sysabove)
				break; // TODO: might not be correct. Was : Exit For
			branch = iq.Branches(Val(segs(i)));
			object bn = branch.Translation.text(English);
			matrix = branch.Matrix;
			if (matrix != null) {
				break; // TODO: might not be correct. Was : Exit For
			}
		}
		if (matrix == null)
			matrix = iq.i_screens_code("Servers");

		return matrix;

	}
	// ''' <summary>
	// ''' Works out the available width in ems from the state of every branch (above this one) in the path - (most DIVs will subtract 2ems which comes from the treeindent class - however breadcrumbs don't
	// ''' </summary>
	// ''' <param name="lid">handle to the users sesh variables</param>
	// ''' <param name="path"></param>
	// ''' <returns></returns>
	// ''' <remarks></remarks>
	//Public Function emsAvailable(lid As UInt64, ByVal path$, ByVal treewidth As Single) As Single
	//    'note params are passed byval (are copies not references) so we don't mess up the originals

	//    emsAvailable = treewidth

	//    Dim branchStates As Dictionary(Of String, clsBranchState) = iq.sesh(lid, "branchStates")
	//    If InStr(path$, ".") = 0 Then Exit Function 'Swift 'paths' have no .'s  


	//    'terrible hack
	//    If UBound(Split(path, ".")) > 4 Then
	//        emsAvailable = treewidth - 8
	//    Else
	//        emsAvailable = treewidth - 2.5
	//    End If


	//    Exit Function

	//    Do
	//        path$ = Left$(path$, InStrRev(path$, ".") - 1)

	//        If path$ <> "tree" Then 'yuck - but its late and i'm tried TODO
	//            If branchStates.ContainsKey(path) Then  'some placeholding branches are never rendered
	//                If branchStates(path$).renderAs <> bt.BreadCrumb Then
	//                    emsAvailable -= 2.25 ' every indent reduces the available space
	//                End If
	//            End If

	//        End If

	//    Loop Until path$ = "tree" Or path$ = "swift"


	//End Function

	public string oneAbove(path)
	{

		oneAbove = Left(path, InStrRev(path, ".") - 1);

	}


	public string PathName(path)
	{
		//aka FullPath

		PathName = ".";
		string[] seg;
		seg = path.Split(".");
		for (i = 1; i <= UBound(seg); i++) {
			if (iq.Branches.ContainsKey(seg(i))) {
				PathName += "/" + iq.Branches(Val(seg(i))).DisplayName(English);
			} else {
				PathName += "/???" + seg(i) + "???";
			}

		}
	}

	//Public Function RequiringUpdate(branches As Dictionary(Of String, clsBranch), buyeraccount As clsAccount) As Dictionary(Of String, ClsProductVariant)


	//    'Returns a dictionary of DistiSKUs > our ProductVariant
	//    'Our branches carry Product-Variants
	//    'we need get a list of DistiSkus that represent those product-variants (and return a dictionary)

	//    RequiringUpdate = New Dictionary(Of String, ClsProductVariant)


	//    For Each b In branches.Values
	//        If Not b.Product Is Nothing Then


	//            'variant key
	//            Dim vdic As Dictionary(Of clsVariant, String)
	//            vdic = buyeraccount.SellerChannel.ChannelSKUs(b.Product) 'fetch the dictionary of Variants to DistiSKUs
	//            For Each k In vdic.Keys    'This disti can have SKUs for several variants of this (branches) product 
	//                '
	//                Dim prices As List(Of IQ.clsPrice)
	//                prices = b.Product.GetPrices(buyeraccount, 9, k)
	//                If prices.Count <> 1 Then Stop 'but for each variant there should only be one price !

	//                Dim minutesold As Integer = DateDiff(DateInterval.Minute, prices(0).DateStamp, Now)
	//                If minutesold < 0 Then Stop
	//                If minutesold > 60 Or prices(0).Price.valid = False Then  'fetch a new price forall POAs
	//                    If Not RequiringUpdate.ContainsKey(vdic(k)) Then
	//                        RequiringUpdate.Add(vdic(k), New ClsProductVariant(b.Product, k))
	//                    Else
	//                        Beep()
	//                    End If
	//                End If
	//            Next
	//        End If
	//    Next

	//End Function

	//Public Sub AppendDic(ByRef a As Dictionary(Of Object, Object), ByRef b As Dictionary(Of Object, Object))
	//Dictionary(Of Object, Object), ByRef b As Dictionary(Of Object, Object))
	public void AppendDic(ref object a, object b)
	{

		foreach ( k in b.Keys) {
			if (!a.ContainsKey(k)) {
				a.Add(k, b(k));
			} else {
				//     Beep()  '    Stop 'duplicate value
			}
		}

	}

	public WebControls.Image ScriptImage(func)
	{

		WebControls.Image img = new WebControls.Image();
		img.ImageUrl = "/images/navigation/refresh.png";
		//this is just *an* image (to attach the script to - it's not visible)
		img.Width = 1;
		img.Height = 1;
		img.Attributes("style") += "position:absolute";
		//take it out of the flow 
		img.Attributes.Add("onload", func);

		return img;

	}


	public List<clsVariant> additionalUpdates(clsBranch parentBranch, clsAccount buyeraccount, path, ref List<string> errorMessages)
	{

		//returns a dictionary of distis SKUS>ProductVariants

		additionalUpdates = new List<clsVariant>();
		//Dictionary(Of String, ClsProductVariant)
		//if it's a keyword search, the parent (ie. the branch we clicked on in the keyword results) could be out of date.. (for a 'normal' branch opening - it should have already been update by opening it's parent)

		if (!parentBranch.Product == null) {
			if (parentBranch.Product.inFeed(buyeraccount.SellerChannel)) {
				//AppendDic(additionalUpdates, parentBranch.StalePrices(buyeraccount)) 'appends to needupdate()
				additionalUpdates.AddRange(parentBranch.StalePrices(buyeraccount, errorMessages));
				if (parentBranch.Product.isSystem) {
					clsBranch system = parentBranch;
					List<clsQuantity> preinstalled = system.GetPreInstalledRecursive(buyeraccount.SellerChannel.Region, path, errorMessages);

					foreach ( i in preinstalled) {
						if (i.Branch.Product != null) {
							//    If Not i.FOC Then  - *DO* fetch Free of charge (FIO's) .. becuase we're very likely flex them (and second or or asubsequent ones *would* have a price)
							if (i.Branch.Product.inFeed(buyeraccount.SellerChannel)) {
								//AppendDic(additionalUpdates, i.Branch.StalePrices(buyeraccount))
								additionalUpdates.AddRange(i.Branch.StalePrices(buyeraccount, errorMessages));
							}
						} else {
							Beep();
							//  TODO - why is it doing this ?
						}
					}
					Debug.Print("queued reqeusts for prices on " + additionalUpdates.Count + " preinstalled items");
				}
			}
		}

	}



	public int LongSQL(SQL, bool ReturnIdentity = false)
	{
		//Runs aribitrary SQL with a 4 minute timeout (large deletions - during Initial data loads via PNA)

		//Dirty fix for the fact you can't control to connection timeout in dataaccess 
		//and i didn't have the time to do a new build (and get everyone to adopt it)

		//Dim sw As StreamWriter = New StreamWriter("c:\temp\tomlog.txt", True)
		//sw.WriteLine(SQL)
		//sw.Close()

		SqlClient.SqlConnection con;
		con = da.OpenDatabase;

		System.Data.SqlClient.SqlCommand com;
		com = new SqlClient.SqlCommand(SQL, con);
		com.CommandTimeout = 240;

		LongSQL = com.ExecuteNonQuery();
		//returns number of rows affected

		if (ReturnIdentity) {
			com.CommandText = "select @@identity";
			LongSQL = (int)com.ExecuteScalar();
		}


		//sw = New StreamWriter("c:\temp\tomlog.txt", True)
		//sw.WriteLine(LongSQL)
		//sw.Close()



		con.Close();

	}




	public void TidySwiftBranches(UInt64 lid)
	{
		clsBranch b;
		if (iq.SeshContains(lid, "swiftStart")) {
			//swiftEnd is a more negative number than swiftstart
			for (i = iq.sesh(lid, "swiftEnd"); i <= iq.sesh(lid, "swiftStart"); i++) {
				b = iq.Branches(i);
				if (b.Parent != null)
					b.Parent.childBranches.Remove(b.ID);
				iq.Branches.Remove(i);
			}
		}



		// iq.sesh(lid, "SwiftStart") = Nothing

	}


	public WebControls.Image fetcherImage(path, int requestHandle, List<clsVariant> needUpdate)
	{

		//returns an image control with the script attached which will check for prices already requested from unitran

		WebControls.Image img;
		img = new WebControls.Image();
		img.CssClass = "fetcherImage";
		img.ImageUrl = eim + "resort.png";
		// this is just AN arbitrary image - it's not visible
		//img.ImageUrl = "http://www.channelcentral.net/images/cloud_man_sml_focus.jpg"
		img.Width = 1;
		img.Height = 1;

		object script;

		//any Prices_div Beneath the div 'path' will be refreshed by a consolidated call to PriceRefresh.aspx
		script = "setTimeout (function(){fillPrices('" + path + "'," + requestHandle + ");return false;},3000)";
		//fill Prices Calls GetPriceUis.aspx for - 3000 gives the page time to render - any less time and the filling becomes unreliable
		img.Attributes.Add("onload", script);
		img.ToolTip = "Handle:" + requestHandle + " Price/stock was requested for : " + Join((from j in needUpdatej.Product.SKU).ToArray, ",");
		//uses LINQ to assemble a CDlist of SKUS

		return img;

	}




	public string findFamily(path, ref string famminor = "", bool SystemOnly = false, bool includeSelf = true)
	{

		//Returns the family name (from the FamMajor attribute of the (family) branches stub product.

		findFamily = "";
		int i;

		clsBranch branch;
		clsProductAttribute pa;

		string[] seg = Split(path, ".");
		for (i = UBound(seg); i >= 1; i += -1) {
			if (!includeSelf){includeSelf = true;continue;}
			branch = iq.Branches(Val(seg((i))));
			if (branch.Product != null && (!SystemOnly || branch.Product.isSystem)) {
				if (branch.Product.i_Attributes_Code.ContainsKey("FamMinor")) {
					famminor = branch.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English);
				}

				if (branch.Product.i_Attributes_Code.ContainsKey("FamMajor")) {
					pa = branch.Product.i_Attributes_Code("FamMajor")(0);
					findFamily = pa.Translation.text(English);
					return;
				}
			}
		}

	}
	public string SerializeToString(object obj)
	{
		XmlSerializer serializer = new XmlSerializer(obj.GetType());
		object xns = new XmlSerializerNamespaces();
		xns.Add(string.Empty, string.Empty);
		using (StringWriter writer = new StringWriter()) {
			serializer.Serialize(writer, obj, xns);

			return writer.ToString();
		}
	}
	//Public Sub ExportExcelToPDF()
	//    Dim stream As FileStream = New FileStream("client_secrets.json", FileMode.Open, FileAccess.Read)

	//    Dim drive As List(Of String) = New List(Of String)
	//    'Dim localClientSecret As ClientSecrets = New ClientSecrets()
	//    'localClientSecret.ClientId = "520683653454-datlp7gs19vo40sp1lrrbskjtbcaa09h.apps.googleusercontent.com"
	//    'localClientSecret.ClientSecret = "CLIENT_SECRET_HERE"
	//    drive.Add(DriveService.Scope.Drive)
	//    Dim credential As UserCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets, drive, "user", CancellationToken.None).Result

	//    ' Create the service.
	//    Dim serviceInitialiser As BaseClientService.Initializer = New BaseClientService.Initializer()
	//    serviceInitialiser.HttpClientInitializer = credential
	//    serviceInitialiser.ApplicationName = "Drive API Sample"

	//    Dim service = New DriveService(serviceInitialiser)

	//    Dim body As New Google.Apis.Drive.v2.Data.File()
	//    body.Title = "My document"
	//    body.Description = "A test document"
	//    body.MimeType = "text/plain"

	//    Dim byteArray As Byte() = System.IO.File.ReadAllBytes("document.txt")
	//    Dim stream2 As New System.IO.MemoryStream(byteArray)

	//    Dim request As FilesResource.InsertMediaUpload = service.Files.Insert(body, stream2, "text/plain")
	//    request.Upload()

	//    Dim file As Google.Apis.Drive.v2.Data.File = request.ResponseBody
	//    Console.WriteLine("File id: " + file.Id)
	//    Console.WriteLine("Press Enter to end this process.")
	//    Console.ReadLine()



	//End Sub


	public DataTable getQuoteExport(int quoteRootID)
	{
		try {
			SqlClient.SqlConnection con = da.OpenDatabase();
			string sqlQuery = "SELECT  qe.[ID],[FK_Quote_ID],[Type],[TimeStamp] , [Version] FROM [QuoteExport] qe inner join Quote q on [FK_Quote_ID] = q.ID where FK_Quote_ID_Root = " + quoteRootID;
			SqlClient.SqlDataReader r = da.DBExecuteReader(con, sqlQuery);
			DataTable dt = new DataTable();
			dt.Load(r);
			return dt;
		} catch {
		}
	}

	public DataTable getQuoteVersionExports(int QuoteID, clsAccount agentaccount)
	{
		try {
			SqlClient.SqlConnection con = da.OpenDatabase();
			string sqlQuery = "SELECT  qe.[ID],[FK_Quote_ID],[Type],[TimeStamp] , [Version] FROM [QuoteExport] qe inner join Quote q on [FK_Quote_ID] = q.ID where q.id= " + QuoteID + " and q.fk_account_id_agent=" + agentaccount.ID;
			SqlClient.SqlDataReader r = da.DBExecuteReader(con, sqlQuery);
			DataTable dt = new DataTable();
			dt.Load(r);
			return dt;

		} catch {

		}

	}

	public string decodeTrebbleHash(string code)
	{
		//basic, this can be tuned
		code = Right(code, Len(code) - 3);
		code = code.Replace("_", " ");
		code = code.Replace(" Generic ", "");
		code = code.Substring(0, code.Length > 13 ? 13 : code.Length);
		return code;
	}

	private bool isSystemAbove(UInt64 lid, string path)
	{
		foreach ( seg in path.Split(".").Reverse) {
			if (path == "tree")
				return false;
			if (path == "tree.1")
				return false;
			if (iq.Branches(path.Split(".").Last).Product != null && iq.Branches(path.Split(".").Last).Product.isSystem)
				return true;
			path = Left(path, Len(path) - Len(seg) - 1);
		}
		return false;
	}



	public void SwitchAccount(UInt64 lid, clsAccount buyerAccount, clsAccount agentAccount, List<string> errorMessages)
	{

		lock (switchAccountLock) {
			Dictionary<string, object> dic = iq.getSeshDic(lid);

			if (dic.ContainsKey("AgentAccount")) {
				int uid;
				clsAccount ag = dic("AgentAccount");
				string pwh = null;
				string md5 = null;
				string root = null;
				object accountList = null;
				string mfr = null;
				string @base = null;
				bool? viaGatekeeper;
				string gkPriceBand = null;
				string gkBasketUrl = null;
				string gkToken = null;
				string screenName = null;

				if (dic.ContainsKey("passwordHash"))
					pwh = dic("passwordHash");
				if (dic.ContainsKey("passwordMD5"))
					md5 = dic("passwordMD5");
				if (dic.ContainsKey("Root"))
					root = dic("Root");
				if (dic.ContainsKey("UserID"))
					uid = dic("UserID");
				if (dic.ContainsKey("AccountList"))
					accountList = dic("AccountList");
				if (dic.ContainsKey("MFR"))
					mfr = dic("MFR");
				if (dic.ContainsKey("Base"))
					@base = dic("Base");
				if (dic.ContainsKey("viaGatekeeper"))
					viaGatekeeper = dic("viaGatekeeper");
				if (dic.ContainsKey("gk_cPriceBand"))
					gkPriceBand = dic("gk_cPriceBand");
				if (dic.ContainsKey("gk_BasketURL"))
					gkBasketUrl = dic("gk_BasketURL");
				if (dic.ContainsKey("gk_Token"))
					gkToken = dic("gk_Token");
				if (dic.ContainsKey("screenName"))
					screenName = dic("screenName");

				dic.Clear();
				dic.Add("UserID", uid);
				dic.Add("AgentAccount", ag);
				dic.Add("BuyerAccount", ag);
				if (!pwh == null)
					dic.Add("passwordHash", pwh);
				if (!md5 == null)
					dic.Add("passwordMD5", md5);
				if (!root == null)
					dic.Add("Root", root);
				if (!mfr == null)
					dic.Add("MFR", mfr);
				if (!@base == null)
					dic.Add("Base", @base);
				if (!accountList == null)
					dic.Add("AccountList", accountList);
				if (viaGatekeeper.HasValue)
					dic.Add("viaGatekeeper", viaGatekeeper);
				if (!gkPriceBand == null)
					dic.Add("gk_cPriceBand", gkPriceBand);
				if (!gkBasketUrl == null)
					dic.Add("gk_BasketURL", gkBasketUrl);
				if (!gkToken == null)
					dic.Add("gk_Token", gkToken);
				if (!screenName == null)
					dic.Add("screenName", screenName);
			}

			iq.sesh(lid, "AgentAccount") = agentAccount;
			//Initially an agent of themself (might change when they choose a customer)
			iq.sesh(lid, "BuyerAccount") = buyerAccount;
			if ((iq.sesh(lid, "QuoteID") != 0))
				iq.sesh(lid, "QuoteID") = 0;
			iq.sesh(lid, "Paradigm") = enumParadigm.AddingSystem;
			iq.updateLogin(lid, agentAccount);

			buyerAccount.SellerChannel.IsCloneOf.LoadVariants(errorMessages, 1);
			//loads (and refreshes) them if neccessary

			if (!agentAccount.SellerChannel.IsCloneOf.stockLoaded) {
				agentAccount.SellerChannel.IsCloneOf.LoadStock();
			}

			if (!agentAccount.SellerChannel.IsCloneOf.pricesLoadedFor.ContainsKey(agentAccount.Priceband)) {
				agentAccount.SellerChannel.IsCloneOf.LoadPrices(agentAccount.Priceband, errorMessages);
			}

			if (!agentAccount.SellerChannel.IsCloneOf.pricesLoadedFor.ContainsKey(Everyone)) {
				agentAccount.SellerChannel.IsCloneOf.LoadPrices(Everyone, errorMessages);
			}

			clsRegion rgn = agentAccount.SellerChannel.IsCloneOf.Region;
			if (!HP.listPricesLoadedFor.ContainsKey(rgn) || HP.listPricesLoadedFor(rgn) == 0) {
				HP.LoadPrices(Everyone, errorMessages, agentAccount.SellerChannel.IsCloneOf.Region);
			}

			iq.sesh(lid, "Root") = "tree.1";

		}

	}

	public string GetSplitMessage(Manufacturer quoteSplit, clsLanguage language)
	{

		GetSplitMessage = string.Format("Due to the upcoming HP separation into HP Inc and Hewlett Packard Enterprise, PPS and EG products need to be quoted separately. " + );
		"To quote {0} products, first save this quote and then create a new quote.";

		GetSplitMessage = Xlt(GetSplitMessage, language);

	}

	// Attempts to look for a Universal request URL and infer the manufacturer from it.
	public string InferUniversalManufacturer(System.Web.HttpRequest request)
	{

		InferUniversalManufacturer = null;


		if (!request == null && !iq.Addresses == null) {
			object requestHost = request.Url.Host.ToLower();

			if (requestHost.Contains("hpiquote.channelcentral.net") || requestHost.Contains("iquote.hp.com") || (iq.Addresses.ContainsKey("HPIUniversalHost") && requestHost.Contains(iq.Addresses("HPIUniversalHost").Translation.text(English)))) {
				InferUniversalManufacturer = "HPI";
			} else if (requestHost.Contains("hpeiquote.channelcentral.net") || requestHost.Contains("iquote.hpe.com") || (iq.Addresses.ContainsKey("HPEUniversalHost") && requestHost.Contains(iq.Addresses("HPEUniversalHost").Translation.text(English)))) {
				InferUniversalManufacturer = "HPE";
			}

		}

	}


	public void Log4NetMessage(string message)
	{
		if ((!log4net.LogManager.GetRepository().Configured)) {
			XmlConfigurator.Configure();
		}
		log.Info(message);

	}

}

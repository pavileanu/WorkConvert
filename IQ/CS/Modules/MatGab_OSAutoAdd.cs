using dataAccess;
using System.Data.SqlClient;

class OSAutoAdd
{


	public void fixAutoAdds(UInt64 lid)
	{

		SqlClient.SqlConnection con = da.OpenDatabase();
		// Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
		List<string> errormessages = new List<string>();
		Dictionary<string, clsBranch> dicSystems = loadDic(con, iq.Branches, "system");
		List<clsProduct> sysProducts = (from s in iq.Products.Valueswhere s.isSystem).ToList();
		clsBranch sysbranch = new clsBranch();
		// Dim region = buyerAccount.SellerChannel.Region

		string skuPrefix = "";


		foreach ( prod in sysProducts) {
			skuPrefix = "";
			if (dicSystems.ContainsKey(prod.sku)) {
				sysbranch = dicSystems(prod.sku);

				object syspath = "tree." + Trim(iq.RootBranch.ID);
				//root
				syspath += "." + Trim(sysbranch.Parent.Parent.ID);
				//System type
				syspath += "." + Trim(sysbranch.Parent.ID);
				//Family
				syspath += "." + Trim(sysbranch.ID);


				string parentName = sysbranch.Parent.EnglishName;
				switch (true) {
					case parentName.Contains("ML110"):
					case parentName.Contains("MS001"):
					case parentName.Contains("ML10"):
						skuPrefix = "748920";
					case parentName.Contains("ML310"):
						skuPrefix = "748919";
					case parentName.Contains("ML5"):
					case parentName.Contains("DL5"):
					case parentName.Contains("BL"):
						skuPrefix = "748922";
					default:
						Match m = Regex.Match(parentName, "[MD]L3[^1].*", RegexOptions.IgnoreCase);
						if (m.Success) {
							skuPrefix = "748921";
						} else {
							Match n = Regex.Match(parentName, "DL[68].*", RegexOptions.IgnoreCase);
							if (n.Success) {
								skuPrefix = "748921";
							} else if (parentName.Contains("DL1") | parentName.Contains("ML15")) {
								skuPrefix = "748921";
							}


						}
				}

				if (skuPrefix != "") {
					foreach ( region in iq.Regions.Values) {
						string skuSuffix = "021";
						List<clsQuantity> preinstalled;
						preinstalled = sysbranch.GetPreInstalledRecursive(region, syspath, errormessages);
						switch (region.Code.ToLower) {
							case "ru":
							case "pl":
							case "cz":
								skuSuffix = "421";
							case "fr":
							case "gb":
							case "us":
							case "ca":
								skuSuffix = "B21";
							case "jp":
								skuSuffix = "291";
							case "au":
							case "in":

								skuSuffix = "371";
						}
						string ossku = skuPrefix + "-" + skuSuffix;
						foreach ( i in preinstalled) {
							if (i.Branch.Product != null && i.Branch.Product.ProductType.Code.ToLower == "sof1") {
								if (i.Branch.Product.sku == ossku & i.NumPreInstalled == 0) {
									i.NumPreInstalled = 1;
									i.update(errormessages);
								}


							}
						}
					}
				}

			}

		}

	}


	public void FixMissingCarepackAttributes(UInt64 lid)
	{

		SqlClient.SqlConnection con = da.OpenDatabase();
		// Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
		List<string> errormessages = new List<string>();
		Dictionary<string, clsBranch> dicSystems = loadDic(con, iq.Branches, "system");
		List<clsProduct> cpqProducts = (from s in iq.Products.Valueswhere s.ProductType.Code.ToUpper() == "WTY" & (s.i_Attributes_Code.ContainsKey("response") == false | s.i_Attributes_Code.ContainsKey("servicelevel") == false | s.i_Attributes_Code.ContainsKey("DMR_ISS") == false | s.i_Attributes_Code.ContainsKey("desc") == false | s.i_Attributes_Code.ContainsKey("capacity") == false)).ToList();
		clsBranch sysbranch = new clsBranch();

		SqlClient.SqlConnection con2 = new SqlClient.SqlConnection("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35");
		con2.Open();
		object sql2 = "select description,sl1.sLabel as response,sl2.sLabel as ServiceLevel, sl3.sLabel as Options,duration ,opttype,optfamily " + ",options.optsku,case when sl1.sLabel like '%24x7%' then 1 else 0 end as tfs ,travel,tracing,ADP,DMR,CTR,OnSite, OptTypeParent as L1,OptTypeName as L2," + "ISNULL(a.translation, CASE " + "WHEN ot2.OptTypeParent = 'Software' AND ot2.OptTypeName NOT LIKE 'Microsoft OS' THEN ISNULL(Options.Technology, Options.OptFamily)  " + "WHEN ot2.OptTypeParent = 'Services' AND ot2.OptTypeName = 'SW Support' THEN CASE WHEN Options.Technology <> 'SUP' THEN Options.Technology ELSE Options.OptFamily END  " + "END) as L3 " + "                   from h3.iq.products.options " + "left outer join h3.iq.products.[CarePack_Properties]  on options.optsku=[CarePack_Properties].optsku " + "left outer join h3.iq.products.carepack_servicelevels sl1 on sl1.sCode = ResponseCode_ISS " + "left outer join h3.iq.products.carepack_servicelevels sl2 on sl2.sCode = servicelevel_iss " + "left outer join h3.iq.products.carepack_servicelevels sl3 on sl3.sCode = options_iss " + "left outer join h3.iq.products.opttypes ot2 on ot2.OptTypeCode = options.opttype " + "left outer join h3.iq.dbo.Abbreviations a ON a.code =  CASE " + "WHEN ot2.OptTypeParent = 'Software' AND ot2.OptTypeName NOT LIKE 'Microsoft OS' THEN ISNULL(Options.Technology, Options.OptFamily) " + "WHEN ot2.OptTypeParent = 'Services' AND ot2.OptTypeName = 'SW Support' THEN CASE WHEN Options.Technology <> 'SUP' THEN Options.Technology ELSE Options.OptFamily END " + "End " + "WHERE OptTypeName='HW Support' and options.optsku in (";
		string skus = "";

		foreach ( prod2 in cpqProducts) {
			skus = skus + "'" + prod2.sku + "',";
		}
		skus = skus.Substring(0, skus.Length - 1);
		skus = skus + ")";
		//skus = "'U7AT3E')"

		//  If Not prod.i_Attributes_Code.ContainsKey("response") Then
		object sql = sql2 + skus;
		object rdr2 = dataAccess.da.DBExecuteReader(con2, sql);
		while (rdr2.Read) {
			clsProduct prod;
			if (iq.i_SKU.ContainsKey(rdr2("optsku"))) {
				prod = iq.i_SKU(rdr2("optsku"));
				if (!prod.i_Attributes_Code.ContainsKey("response")) {
					if (!IsDBNull(rdr2("response")))
						object b = new clsProductAttribute(prod, iq.i_attribute_code("response"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("response").ToString, English, "CPQ", 0, null, 0, false), null);
				}
				if (!prod.i_Attributes_Code.ContainsKey("servicelevel")) {
					if (!IsDBNull(rdr2("servicelevel")))
						object b = new clsProductAttribute(prod, iq.i_attribute_code("servicelevel"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("servicelevel").ToString, English, "CPQ", 0, null, 0, false), null);
				}
				if (!prod.i_Attributes_Code.ContainsKey("DMR_ISS")) {
					if (!IsDBNull(rdr2("options")))
						object b = new clsProductAttribute(prod, iq.i_attribute_code("DMR_ISS"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("options").ToString, English, "CPQ", 0, null, 0, false), null);
				}
				if (!prod.i_Attributes_Code.ContainsKey("capacity")) {
					if (!IsDBNull(rdr2("duration")))
						object b = new clsProductAttribute(prod, iq.i_attribute_code("capacity"), (int)rdr2("duration"), iq.i_unit_code("year"), null, null);
				}
				if (!prod.i_Attributes_Code.ContainsKey("desc")) {
					if (!IsDBNull(rdr2("description")))
						object b = new clsProductAttribute(prod, iq.i_attribute_code("desc"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("description").ToString, English, "CPQ", 0, null, 0, false), null);
				}
			}
		}
		//End If


		con2.Close();

		// Fix all missing slot issues
		object swc = null;
		//dataAccess.da.MakeWriteCacheFor(iq2con, "Slot")

		cpqProducts = (from s in iq.Products.Valueswhere s.ProductType.Code.ToUpper() == "WTY" & s.Branches.Count > 0).ToList();
		clsBranch cpqBranch = new clsBranch();
		foreach ( carepack in cpqProducts) {
			cpqBranch = carepack.Branches.First;
			if (cpqBranch.slots.Count == 0) {
				object s = new clsSlot(iq.i_slotType_Code("WTY")("CAREPACK"), cpqBranch, "", -1, null, new NullableInt(), 0, 0, swc);

			}
			object qtys = from q in cpqBranch.Quantities.Valueswhere q.Path == "";


			foreach ( qty in qtys) {
				qty.Path = "";

			}

		}
		//remove all post warranty products 
		cpqProducts = (from s in iq.Products.Valueswhere s.ProductType.Code.ToUpper() == "WTY" & s.i_Attributes_Code.ContainsKey("desc")).ToList();
		foreach ( carepack in cpqProducts) {
			// Dim errormessages As List(Of String) = New List(Of String)
			if (carepack.i_Attributes_Code("desc").First.Translation.text(English).ToUpper().Contains("POST")) {
				object a = carepack.i_Attributes_Code("desc").First.Translation.text(English);
				AuditLog.Instance.Add(AuditType.Information, "CarePack SKU " + carepack.sku + " Activeto date changed", errormessages, "");
				carepack.activeTo = Now.AddDays(-30);
				carepack.update(errormessages);
			}
		}


		con.Close();


	}
	//Public Sub FixDuplicateTRO(lid As UInt64)
	//    Dim con As SqlClient.SqlConnection = da.OpenDatabase()
	//    Dim dicSystems As Dictionary(Of String, clsBranch) = loadDic(con, iq.Branches, "system")
	//    Dim sysProducts As List(Of clsProduct) = (From s In iq.Products.Values Where s.isSystem).ToList()
	//    Dim skuPrefix As String = ""
	//    Dim sysbranch As clsBranch = New clsBranch()
	//    Dim agentAccount = iq.seshTyped(Of clsAccount)(lid, "AgentAccount")
	//    For Each prod In sysProducts
	//        skuPrefix = ""
	//        If dicSystems.ContainsKey(prod.sku) Then
	//            sysbranch = dicSystems(prod.sku)

	//            Dim syspath = "tree." & Trim$(iq.RootBranch.ID) 'root
	//            syspath &= "." & Trim$(sysbranch.Parent.Parent.ID) 'System type
	//            syspath &= "." & Trim$(sysbranch.Parent.ID) 'Family
	//            syspath &= "." & Trim$(sysbranch.ID)

	//            Dim troPath As String = ""
	//            Dim troBranch As clsBranch = sysbranch.FindBranchByNameBelow("HP Top Recommended", syspath, False, 12, troPath)
	//            Dim troCPQBranch = troBranch.FindBranchByNameBelow("Care Pack", syspath, False, 12, troPath)
	//            Dim dupChildBranch As List(Of clsBranch) = New List(Of clsBranch)
	//            For Each child As clsBranch In troCPQBranch.childBranches.values
	//                If Not child.isPrunedAt(troPath & "." & child.ID, agentAccount.SellerChannel) Then
	//                    If trocpqs.ContainsKey(child.Product.sku) Then
	//                         If Not hasList.Contains(child.Product.sku) Then
	//                            hasList.Add(child.Product.sku)
	//                        Else ' duplicate tro item
	//                            '  Dim p = New clsPrune(troPath & "." & child.ID, New NullableInt(agentAccount.SellerChannel.ID), "CPQJIT")
	//                            dupChildBranch.Add(child)

	//                        End If


	//                    End If

	//                End If
	//            Next
	//            For Each br In dupChildBranch
	//                Dim priceCount = troCPQBranch.childBranches(br.ID).Quantities.Count
	//                'delete quantities
	//                For i = 0 To priceCount - 1
	//                    Dim qty = troCPQBranch.childBranches(br.ID).Quantities.Values.First
	//                    troCPQBranch.childBranches(br.ID).Quantities(qty.ID).Delete(errorMEssages)
	//                Next
	//                'Delte slots
	//                Dim slotsCount = troCPQBranch.childBranches(br.ID).slots.Count
	//                For i = 0 To slotsCount - 1
	//                    Dim slot = troCPQBranch.childBranches(br.ID).slots.Values.First
	//                    troCPQBranch.childBranches(br.ID).slots(slot.ID).delete(errorMEssages)
	//                Next
	//                troCPQBranch.childBranches(br.ID).delete(errorMEssages)

	//            Next
	//        End If
	//    Next


	//End Sub

	public void FixCarePacks(UInt64 lid)
	{
		FixMissingCarepackAttributes(lid);

		//Adds quantity record and creates new carepack Products
		clsAccount agentAccount = iq.seshTyped<clsAccount>(lid, "AgentAccount");
		SqlClient.SqlConnection con = da.OpenDatabase();
		// Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
		List<string> errormessages = new List<string>();
		Dictionary<string, clsBranch> dicSystems = loadDic(con, iq.Branches, "system");
		List<clsProduct> sysProducts = (from s in iq.Products.Valueswhere s.isSystem).ToList();
		clsBranch sysbranch = new clsBranch();
		// Dim region = buyerAccount.SellerChannel.Region
		clsRegion defaultregion = agentAccount.SellerChannel.Region;
		string skuPrefix = "";
		DateTime startTime = Now;


		foreach ( prod in sysProducts) {
			if (Now.AddMinutes(-5) > startTime) {
				startTime = Now;
				agentAccount = iq.seshTyped<clsAccount>(lid, "AgentAccount");
			}

			skuPrefix = "";
			if (dicSystems.ContainsKey(prod.sku)) {
				sysbranch = dicSystems(prod.sku);

				object syspath = "tree." + Trim(iq.RootBranch.ID);
				//root
				syspath += "." + Trim(sysbranch.Parent.Parent.ID);
				//System type
				syspath += "." + Trim(sysbranch.Parent.ID);
				//Family
				syspath += "." + Trim(sysbranch.ID);

				clsGenericAjaxRequest req = new clsGenericAjaxRequest();
				req.lid = lid;
				req.BranchPath = syspath;
				object a = CarePackModule.CarePackJIT(req, defaultregion);
			}
		}


	}
	public List<clsSysCarePack> CarePackReports(UInt64 lid)
	{
		SqlClient.SqlConnection con = da.OpenDatabase();
		List<string> errormessages = new List<string>();
		Dictionary<string, clsBranch> dicSystems = loadDic(con, iq.Branches, "system");
		List<clsProduct> sysProducts = (from s in iq.Products.Valueswhere s.isSystem & s.activeTo >= Today & s.EOL == false & s.Active & s.Publish).ToList();
		clsBranch sysbranch = new clsBranch();
		// Dim region = buyerAccount.SellerChannel.Region

		string skuPrefix = "";

		List<clsSysCarePack> sysList = new List<clsSysCarePack>();
		foreach ( prod in sysProducts) {
			skuPrefix = "";
			if (dicSystems.ContainsKey(prod.sku)) {
				sysbranch = dicSystems(prod.sku);
				object syspath = "tree." + Trim(iq.RootBranch.ID);
				//root
				syspath += "." + Trim(sysbranch.Parent.Parent.ID);
				//System type
				syspath += "." + Trim(sysbranch.Parent.ID);
				//Family
				syspath += "." + Trim(sysbranch.ID);
				clsRegion region = iq.i_region_code("US");
				object systemBI = new clsBranchInfo(lid, syspath, null, 70, enumParadigm.errorNotSet, errormessages);
				object hideReasons = systemBI.branch.ReasonsForHide(systemBI.buyerAccount, systemBI.foci, syspath, systemBI.buyerAccount.SellerChannel.priceConfig, false, errormessages);

				if (hideReasons.Count == 0) {
					List<clsQuantity> preinstalled;
					preinstalled = sysbranch.GetPreInstalledRecursive(region, syspath, errormessages);
					bool carepackexists = false;
					foreach ( i in preinstalled) {
						if (i.Branch.Product != null && i.Branch.Product.ProductType.Code.ToLower == "wty") {
							if (i.NumPreInstalled == 1) {
								clsSysCarePack sysCarePack = new clsSysCarePack();
								sysCarePack.sysSkus = prod.SKU;
								sysCarePack.carepackSku = i.Branch.Product.SKU;
								sysCarePack.sysDesc = prod.i_Attributes_Code("desc")(0).Translation.text(English);
								if (i.Branch.Product.i_Attributes_Code.ContainsKey("desc")) {
									sysCarePack.carePackDesc = i.Branch.Product.i_Attributes_Code("desc")(0).Translation.text(English);
								} else {
									sysCarePack.carePackDesc = "No Carepack Description available";
								}
								sysList.Add(sysCarePack);
								carepackexists = true;
								break; // TODO: might not be correct. Was : Exit For
							}
						}
					}
					if (!carepackexists) {
						clsSysCarePack notexist = new clsSysCarePack();
						notexist.sysSkus = prod.SKU;
						notexist.sysDesc = prod.i_Attributes_Code("desc")(0).Translation.text(English);
						sysList.Add(notexist);
					}
				}

			}

		}

		return sysList;
	}

	public List<clsSysCarePack> CarePackTROReports(UInt64 lid)
	{
		SqlClient.SqlConnection con = da.OpenDatabase();
		// Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
		List<string> errormessages = new List<string>();
		Dictionary<string, clsBranch> dicSystems = loadDic(con, iq.Branches, "system");
		List<clsProduct> sysProducts = (from s in iq.Products.Valueswhere s.isSystem & s.activeTo >= Today).ToList();
		clsBranch sysbranch = new clsBranch();
		// Dim region = buyerAccount.SellerChannel.Region
		object agentAccount = iq.seshTyped<clsAccount>(lid, "AgentAccount");

		string skuPrefix = "";
		Dictionary<string, clsBranch> trocpqs = new Dictionary<string, clsBranch>();
		List<clsSysCarePack> sysList = new List<clsSysCarePack>();
		foreach ( prod in sysProducts) {
			skuPrefix = "";
			if (dicSystems.ContainsKey(prod.sku)) {
				sysbranch = dicSystems(prod.sku);

				object syspath = "tree." + Trim(iq.RootBranch.ID);
				//root
				syspath += "." + Trim(sysbranch.Parent.Parent.ID);
				//System type
				syspath += "." + Trim(sysbranch.Parent.ID);
				//Family
				syspath += "." + Trim(sysbranch.ID);

				clsRegion region = iq.i_region_code("US");
				string troPath = "";
				clsBranch troBranch = sysbranch.FindBranchByNameBelow("Top Recommended", syspath, false, 12, troPath);
				clsBranch troCPQBranch;
				object hasList = new List<string>();
				bool carepackexists = false;
				if (troBranch != null) {
					troCPQBranch = troBranch.FindBranchByNameBelow("Care Pack", syspath, false, 12, troPath);

					if (troCPQBranch != null) {
						foreach (clsBranch child in troCPQBranch.childBranches.Values) {
							if (!child.PruneInForce(troPath + "." + child.ID, agentAccount.SellerChannel)) {
								if (!hasList.Contains(child.Product.sku)) {
									hasList.Add(child.Product.sku);
									clsSysCarePack sysCarePack = new clsSysCarePack();
									sysCarePack.sysSkus = prod.sku;
									sysCarePack.carepackSku = child.Product.sku;
									sysCarePack.sysDesc = prod.i_Attributes_Code("desc")(0).Translation.text(English);
									sysCarePack.carePackDesc = child.Product.i_Attributes_Code("desc")(0).Translation.text(English);
									sysList.Add(sysCarePack);
									carepackexists = true;
								}
							}
						}
					}
				}
				if (!carepackexists) {
					clsSysCarePack notexist = new clsSysCarePack();
					notexist.sysSkus = prod.sku;
					notexist.sysDesc = prod.i_Attributes_Code("desc")(0).Translation.text(English);
					sysList.Add(notexist);
				}




			}

		}

		return sysList;
	}
}
public class clsSysCarePack
{
	private string sysSkus {
		get { return m_sysSkus; }
		set { m_sysSkus = Value; }
	}
	private string m_sysSkus;
	private string sysDesc {
		get { return m_sysDesc; }
		set { m_sysDesc = Value; }
	}
	private string m_sysDesc;
	private string carepackSku {
		get { return m_carepackSku; }
		set { m_carepackSku = Value; }
	}
	private string m_carepackSku;
	private string carePackDesc {
		get { return m_carePackDesc; }
		set { m_carePackDesc = Value; }
	}
	private string m_carePackDesc;

}

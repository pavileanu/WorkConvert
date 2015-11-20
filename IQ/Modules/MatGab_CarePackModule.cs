using System.Data.SqlClient;
using System.Net.Mail;
//Imports log4net
//Imports log4net.Config
using dataAccess;

class CarePackModule
{
	// Private log As ILog = LogManager.GetLogger("IQ")

	public List<string> CarePackJIT(clsGenericAjaxRequest request, clsRegion importregion = null)
	{
		//Return Nothing

		ulong lid = request.lid;
		object errorMessages = new List<string>();
		//LogMessage("CarePackJIT")
		// Make sure we got a branch path
		if (request.BranchPath == null)
			return null;
		if (request.BranchPath == "tree.1")
			request.BranchPath = iq.sesh(lid, "treecursor");
		if (request.BranchPath == null)
			return null;

		object agentAccount = iq.seshTyped<clsAccount>(lid, "AgentAccount");
		object buyerAccount = iq.seshTyped<clsAccount>(lid, "BuyerAccount");

		clsBranch systemBranch = null;
		object systemPath = string.Empty;
		bool troAmended = false;
		string sku = null;

		// Look for the branch and product
		if (iq.Branches.ContainsKey(Split(request.BranchPath, ".").Last)) {
			systemBranch = iq.Branches(Split(request.BranchPath, ".").Last);
			object path = string.Empty;
			systemBranch = systemBranch.FindSystemAbove(request.BranchPath, path);
		}
		if (systemBranch == null || systemBranch.Product == null || string.IsNullOrEmpty(systemBranch.Product.SKU))
			return null;

		sku = systemBranch.Product.SKU;

		// Don't refresh care packs for this sku if they've been refreshed recently
		bool refresh = true;

		//LogMessage("CarePackJIT : CarePackLastRefresh System SKU " & sku)

		if (iq.CarePackLastRefresh.ContainsKey(sku)) {
			DateTime lastRefresh = iq.CarePackLastRefresh(sku);
			// Refresh system if not refreshed in the previous 24 hours
			if (DateTime.Now < lastRefresh.AddDays(1)) {
				refresh = false;
			}
		}
		if (!refresh)
			return null;
		//LogMessage("CarePackJIT : IsPQWSActive :" & IsPQWSActive())
		// Make sure the PQWS service is installed/responding
		if (!IsPQWSActive())
			return null;

		// Get the system path
		systemPath = Left(request.BranchPath, request.BranchPath.IndexOf(systemBranch.ID) + Len(systemBranch.ID.ToString));

		clsScreen carePackScreen = new clsScreen();
		if (systemBranch.Product.mfrCode.ToUpper() == "HPI") {
			carePackScreen = iq.i_screens_code("optCPKDTO");
		} else {
			carePackScreen = iq.i_screens_code("optCPK");
		}


		// Find the Hardware Support branch - care pack branches get grafted on there
		string hwSupportPath = Left(systemPath, Len(systemPath) - Len(Split(systemPath, ".").Last) - 1);
		clsBranch hwSupportBranch = systemBranch.FindBranchByNameBelow("HW Support", Left(systemPath, Len(systemPath) - Len(Split(systemPath, ".").Last) - 1), true, 12, hwSupportPath);
		if (hwSupportBranch.Matrix == null) {
			hwSupportBranch.Matrix = carePackScreen;
			hwSupportBranch.Update(errorMessages);
		}

		if (hwSupportBranch == null) {
			// Couldn't find the Hardware Support branch - create it under the Services branch
			object servicesBranchPath = string.Empty;
			object servicesBranch = systemBranch.FindBranchByNameBelow("Services", "", true, 12, servicesBranchPath);

			if (servicesBranch == null) {
				// Couldn't find the Services branch; locate via the All Options branch
				object allOptionsBranchPath = string.Empty;
				object allOptionsBranch = systemBranch.FindBranchByNameBelow("All Options", "", true, 12, allOptionsBranchPath);
				servicesBranch = new clsBranch(null, allOptionsBranch, iq.AddTranslation("Services", English, "", 0, null, 0, false), "", iq.AddTranslation("Options", English, "", 0, null, 0, false), iq.AddTranslation("Option", English, "", 0, null, 0, false), null, 40, false, "Y");

			}

			if (!servicesBranch == null) {
				hwSupportBranch = new clsBranch(null, servicesBranch, iq.AddTranslation("HW Support", English, "", 0, null, 0, false), "", iq.AddTranslation("Options", English, "", 0, null, 0, false), iq.AddTranslation("Option", English, "", 0, null, 0, false), carePackScreen, 0, false, "B");
			}

		}
		//LogMessage("CarePackJIT : HwSupportBranch : " & hwSupportBranch.ID)


		if (hwSupportBranch == null)
			return null;

		// Find the Top Recommended Options branch
		object troPath = string.Empty;
		clsBranch troBranch = systemBranch.FindBranchByNameBelow("Top Recommended", systemPath, false, 6, troPath);

		if (troBranch == null) {
			// Couldn't find the Top Recommended Options branch - create it
			troBranch = new clsBranch(null, systemBranch, iq.AddTranslation("Top Recommended", English, "", 0, null, 0, false), "", iq.AddTranslation("Top Recommended", English, "", 0, null, 0, false), iq.AddTranslation("Top Recommended", English, "", 0, null, 0, false), null, 0, false, "H");
			troPath = systemPath + "." + troBranch.ID;

		}

		// Find the Top Recommended Options/Care Pack branch - care pack branches get grafted on there
		clsBranch troCpqBranch = troBranch.FindBranchByNameBelow("Care Pack", troPath, false, 7, troPath);

		if (troCpqBranch == null) {
			// Couldn't find the Top Recommended Options/Care Pack branch - create it
			troCpqBranch = new clsBranch(null, troBranch, iq.AddTranslation("Care Pack", English, "", 0, null, 0, false), "/images/product/category/cat2.png", iq.AddTranslation("Top Recommended", English, "", 0, null, 0, false), iq.AddTranslation("Top Recommended", English, "", 0, null, 0, false), null, 0, false, "I");
			troPath = troPath + "." + troCpqBranch.ID;
		}

		//LogMessage("CarePackJIT : TroPath : " & troPath)

		clsVariant skuVariant;
		// Attempt to refresh the care packs via a call to PQWS
		string autoAddCreatedPath = null;

		try {
			//LogMessage("CarePackJIT : RefreshPQWSCarePacks")
			RefreshPQWSCarePacks(systemBranch.Product, systemPath, hwSupportBranch, troCpqBranch, agentAccount, autoAddCreatedPath, troAmended, lid, hwSupportPath, systemBranch,
			troPath, errorMessages);

			//  CreateCarePackVariants(carePackList, request, agentAccount, buyerAccount)

			// Store the last refresh time for this sku so we don't refresh it constantly
			if (iq.CarePackLastRefresh.ContainsKey(sku)) {
				iq.CarePackLastRefresh(sku) = DateTime.Now;
			} else {
				iq.CarePackLastRefresh.Add(sku, DateTime.Now);
			}

		} catch (Exception ex) {
			ErrorLog.Add(ex);
		}

		// Set up the return
		// Dim results = New List(Of String)()

		//'LogMessage("CarePackJIT : AutoAddPath")
		//If autoAddCreatedPath IsNot Nothing Then
		//    'LogMessage("CarePackJIT : AutoAddPath :" & autoAddCreatedPath)
		//    results.Add("addpart:" & autoAddCreatedPath)
		//End If

		//If troAmended Then
		//    'LogMessage("CarePackJIT : TroChanged :")
		//    results.Add("refreshall")
		//End If

		//If results.Count > 0 Then Return results Else Return Nothing
		return null;

	}

	private bool IsPQWSActive()
	{

		IsPQWSActive = false;

		PQWS.PQWSClient pqws = new PQWS.PQWSClient();

		// False if the endpoint isn't configured
		if (!pqws.Endpoint.Address == null) {

			try {
				object response = pqws.Hello();
				IsPQWSActive = (string.Equals(response, "Hello", StringComparison.InvariantCultureIgnoreCase));
			} catch (Exception ex) {
				ErrorLog.Add(ex);
			}

		}

	}

	private bool RefreshPQWSCarePacks(clsProduct systemProduct, string systemPath, clsBranch hwSupportBranch, clsBranch troBranch, clsAccount agentAccount, ref string autoAddCreatedPath, ref bool troAmended, ulong lid, string hwSupportPath, clsBranch systemBranch,
	string troPath, List<string> errorMessages)
	{

		RefreshPQWSCarePacks = true;

		// Call the PQWS webapi service to retrieve HP care packs
		PQWS.PQWSClient pqws = new PQWS.PQWSClient();
		PQWS.CPCHierarchyCarePackResults hpCarePackResults = null;

		// Retrieve care pack data from HP
		try {
			hpCarePackResults = pqws.HPCarePacks(agentAccount.mfrCode, systemProduct.SKU, agentAccount.SellerChannel.Region.Code, agentAccount.Language.Code);
		//LogMessage("CarePackJIT : CarePack results")
		} catch (Exception ex) {
			//LogMessage("CarePackJIT : CarePack results Exception " & ex.Message)
			RefreshPQWSCarePacks = false;
			ErrorLog.Add(ex);
		}
		if (hpCarePackResults.AllHPCarePacks != null) {
		//LogMessage("CarePackJIT : CarePack results AllHPCarePacks :" & hpCarePackResults.AllHPCarePacks.Count)
		} else {
			//LogMessage("CarePackJIT : CarePack results AllHPCarePacks 0")
		}

		if (hpCarePackResults.RecommendedHPCarePacks != null) {
		//LogMessage("CarePackJIT : CarePack results RecommendedHPCarePacks :" & hpCarePackResults.RecommendedHPCarePacks.Count)
		} else {
			//LogMessage("CarePackJIT : CarePack results RecommendedHPCarePacks 0")
		}
		//      'LogMessage("CarePackJIT : CarePack results RecommendedHPCarePacks" & IIf(hpCarePackResults.RecommendedHPCarePacks IsNot Nothing, hpCarePackResults.RecommendedHPCarePacks.Count, 0))
		// Refresh the care packs
		if (!hpCarePackResults == null) {
			// Dim carePacks = From u In hpCarePackResults.AllHPCarePacks Select u.CarePackProductNumber

			RefreshPQWSCarePacks = RefreshCarePacks(systemProduct, systemPath, hwSupportBranch, troBranch, hpCarePackResults, agentAccount, autoAddCreatedPath, troAmended, lid, hwSupportPath,
			systemBranch, troPath, errorMessages);
		}

	}

	// Creates and assigns IQ2 Care Packs to the owning sku from the passed list of HP care packs
	private bool RefreshCarePacks(clsProduct systemProduct, string systemPath, clsBranch hwSupportBranch, clsBranch troBranch, PQWS.CPCHierarchyCarePackResults hpCarePackResults, clsAccount agentAccount, ref string autoAddCreatedPath, ref bool troAmended, ulong lid, string hwSupportPath,
	clsBranch systemBranch, string troPath, List<string> errorMessages)
	{

		RefreshCarePacks = true;


		if (ValidateHPCarePacks(hpCarePackResults)) {
			object allCarePacks = new List<IQ.PQWS.CPCCarePack>();
			object amendedCarePacks = new List<IQ.PQWS.CPCCarePack>();
			object newCarePacks = new List<IQ.PQWS.CPCCarePack>();
			object deletedCarePacks = new List<string>();
			object newServiceLevels = new List<string>();
			object notAddedCarePacks = new List<IQ.PQWS.CPCCarePack>();
			clsBranch carePackRootBranch = iq.i_SpecialBranches("cpqroot");

			// Apply any fixes to the data
			//  CleanData(hwSupportBranch, troBranch, errorMessages)

			// Form a list of all the care packs by combining the "recommended" and "all" lists
			BuildCarePackList(hpCarePackResults.RecommendedHPCarePacks, hpCarePackResults.AllHPCarePacks, allCarePacks);
			//LogMessage("CarePackJIT : BuildCarePackList")
			// Build a list of unknown service levels
			FindUnknownServiceLevels(allCarePacks, newServiceLevels);
			//LogMessage("CarePackJIT : FindUnknownServiceLevels")
			// Build a list of new/amended care packs (as well as unrecognized service levels to send to support)
			FindNewAndAmendedCarePacks(allCarePacks, amendedCarePacks, newCarePacks, hpCarePackResults.CountryDetails, agentAccount, notAddedCarePacks);
			//LogMessage("CarePackJIT : FindNewAndAmendedCarePacks")
			// Build a list of care packs deleted from the product
			FindDeletedCarePacks(hwSupportBranch, notAddedCarePacks, deletedCarePacks);
			//LogMessage("CarePackJIT : FindDeletedCarePacks")
			// New care packs - these could be completely new to IQ2, or existing care packs newly
			// added to the product

			foreach (PQWS.CPCCarePack hpCarePack in newCarePacks) {
				object carePackSKUCode = hpCarePack.CarePackProductNumber;
				clsServiceLevel serviceLevel = iq.ServiceLevels.Values.FirstOrDefault(sl => (sl.ServiceLevel == hpCarePack.ServiceLevel));
				clsBranch carePackBranch = null;

				// This is a completely new care pack - create the clsProduct and, if not disabled/post-warranty, clsBranch
				carePackBranch = CreateOrAmendCarePack(null, null, hpCarePack, serviceLevel, carePackRootBranch, agentAccount, hpCarePackResults.CountryDetails, lid, errorMessages);


				if (carePackBranch != null && carePackRootBranch.childBranches.ContainsKey(carePackBranch.ID) == false) {
					// Add the care pack to the care pack root branch
					carePackRootBranch.childBranches.Add(carePackBranch.ID, carePackBranch);
					carePackRootBranch.Update(errorMessages);
				}
				// Make sure the care pack branch is grafted onto the Hardware Support branch
				GraftCarePack(carePackSKUCode, carePackBranch, hwSupportBranch, errorMessages, hwSupportPath);



			}
			//LogMessage("CarePackJIT : NewCarePacks")
			// Amended care packs

			foreach (PQWS.CPCCarePack hpCarePack in amendedCarePacks) {
				object carePackSKUCode = hpCarePack.CarePackProductNumber;
				clsServiceLevel serviceLevel = iq.ServiceLevels.Values.FirstOrDefault(sl => (sl.ServiceLevel == hpCarePack.ServiceLevel));
				clsProduct carePack = iq.i_SKU(carePackSKUCode);

				// Make sure the care pack branch is under the care pack root branch
				clsBranch carePackBranch = carePackRootBranch.childBranches.Values.FirstOrDefault(cb => !cb.Product == null && cb.Product.ID == carePack.ID);
				if (carePackBranch == null) {
					// No care pack branch - create and add to the root branch (unless disabled or post-warranty)
					clsTranslation translation = iq.AddTranslation(carePackSKUCode, English, "CPQ", 0, null, 0, false);
					carePackBranch = new clsBranch(carePack, carePackRootBranch, translation, string.Empty, translation, translation, null, 0, false, "B");
				}

				// Update the care pack
				CreateOrAmendCarePack(carePack, carePackBranch, hpCarePack, serviceLevel, carePackRootBranch, agentAccount, hpCarePackResults.CountryDetails, lid, errorMessages);

				if (carePackBranch != null) {
					// Make sure the care pack branch is grafted onto the Hardware Support branch
					GraftCarePack(carePackSKUCode, carePackBranch, hwSupportBranch, errorMessages, hwSupportPath);
				}

			}
			//LogMessage("CarePackJIT : amendedCarePacks")
			// Deleted care packs

			foreach (string sku in deletedCarePacks) {
				// Delete the care pack from the system - care packs remain on the care pack root branch if they're there
				DeleteCarePackBranch(troBranch, sku, errorMessages);
				DeleteCarePackBranch(hwSupportBranch, sku, errorMessages);

			}
			//LogMessage("CarePackJIT : deletedCarePacks")
			object buyerAccount = iq.seshTyped<clsAccount>(lid, "BuyerAccount");
			if (newCarePacks.Count > 0)
				CreateCarePackVariants(newCarePacks, lid, agentAccount, buyerAccount);
			if (amendedCarePacks.Count > 0)
				CreateCarePackVariants(amendedCarePacks, lid, agentAccount, buyerAccount);
			// Assign any Top Recommended Option care packs
			troAmended = SetupTRO(systemProduct, troBranch, allCarePacks, carePackRootBranch, troPath, agentAccount, errorMessages);

			// Set up any Auto Adds
			autoAddCreatedPath = SetUpAutoAdd(systemProduct, allCarePacks, carePackRootBranch, systemPath, agentAccount, errorMessages, systemBranch, hwSupportBranch, buyerAccount);

			// Email support with any unknown service levels encountered
			if (newServiceLevels.Count > 0) {
				AddUnknownServiceLevel(newServiceLevels, systemProduct.mfrCode);
				// SendServiceLevelsEmail(newServiceLevels, errorMessages)
			}

			RefreshCarePacks = true;

		} else {
			RefreshCarePacks = false;
			AuditLog.Instance.Add(AuditType.Error, "Invalid response retrieved from PQWS web service", string.Empty, lid);
		}

	}

	// Ensure the care pack branch is grafted onto the parent branch

	private void GraftCarePack(string carePackSKUCode, clsBranch carePackBranch, clsBranch hwSupportBranch, List<string> errorMessages, string hwSupportPath)
	{
		//  If hwSupportBranch.childBranches.Values.FirstOrDefault(Function(cb) cb.SKU = carePackSKUCode) Is Nothing Then
		// Not hwSupportBranch.childBranches.Values.Contains(carePackBranch) Then
		if (carePackBranch != null && !carePackBranch.GraftedOnAt.Contains(hwSupportPath)) {
			if (hwSupportBranch.Graft(carePackBranch, "RefreshPQWSCarePacks", hwSupportPath, errorMessages, null)) {
				hwSupportBranch.Update(errorMessages);
			}
		}

	}


	private void CleanData(clsBranch hwSupportBranch, clsBranch troBranch, List<string> errorMessages)
	{
		// Some care packs are "native children" of the system rather than branches grafted on from the care pack root branch - 
		// to tidy this up, the "native" branch needs to be deleted and a new care pack root branch created and grafted on in its place

		RemoveNonGraftedBranches(hwSupportBranch, errorMessages);
		RemoveNonGraftedBranches(troBranch, errorMessages);

	}


	private void RemoveNonGraftedBranches(clsBranch parentBranch, List<string> errorMessages)
	{
		object nonGraftedBranchIDs = new List<int>();

		// Build a list of non-grafted branches to remove - these will have the parent
		// branch as their Parent (whereas grafted branches retain their original Parent).
		foreach (clsBranch branch in parentBranch.childBranches.Values) {
			// Parent will be different for grafted branches
			if (branch.Parent == null || branch.Parent.ID == parentBranch.ID) {
				nonGraftedBranchIDs.Add(branch.ID);
			}
		}

		// Delete any non-grafted branches

		foreach ( nonGraftedBranchID in nonGraftedBranchIDs) {
			parentBranch.childBranches(nonGraftedBranchID).Parent = null;
			parentBranch.childBranches(nonGraftedBranchID).Update(errorMessages);

			parentBranch.childBranches.Remove(nonGraftedBranchID);

		}

	}


	private void DeleteCarePackBranch(clsBranch parentBranch, string sku, List<string> errorMessages)
	{
		clsBranch carePackBranch = parentBranch.childBranches.Values.FirstOrDefault(cb => cb.SKU == sku);


		while (!carePackBranch == null) {
			if (carePackBranch.Parent.ID != parentBranch.ID) {
				// The care pack is grafted on - remove the graft
				parentBranch.DeleteGraftedOnBranch(carePackBranch.ID);
			} else {
				// The care pack is an actual child branch - remove its parent
				carePackBranch.Parent = null;
				carePackBranch.Update(errorMessages);
			}

			// Remove the care pack from the object model
			parentBranch.childBranches.Remove(carePackBranch.ID);

			// Any more? Seems there can be duplicates...
			carePackBranch = parentBranch.childBranches.Values.FirstOrDefault(cb => cb.SKU == sku);

		}

	}

	private bool SetupTRO(clsProduct systemProduct, clsBranch troBranch, List<PQWS.CPCCarePack> allCarePacks, clsBranch carePackRootBranch, string troPath, clsAccount agentAccount, List<string> errorMessages)
	{

		string sysFamily = systemProduct.i_Attributes_Code("FamMajor")(0).Translation.text(English);
		int slotTypeCode = 2;
		// 2 is the magic number for TROs on the TROAA table
		object MAXTROS = 2;
		// 2 is also the maximum number of TROs we display
		bool amended = false;

		//LogMessage("CarePackJIT : SetupTRO : ")


		if (systemProduct.mfrCode == "HPE") {
			List<clsTROAA> tros = iq.ServiceLevelTROAA.Values.Where(troaa => string.Equals(sysFamily, troaa.SysFamily, StringComparison.InvariantCultureIgnoreCase) && troaa.SlotTypeCode == slotTypeCode).OrderBy(troaa => troaa.DisplayOrder).ToList();

			if (tros == null)
				return amended;

			int addedCount = 0;
			foreach (clsTROAA tro in tros) {
				//LogMessage("CarePackJIT : Tro" & tros.Count)
				//LogMessage("CarePackJIT : Tro : allCarePacks" & allCarePacks.Count)
				object serviceLevel = tro.ServiceLevel;

				// There should be either none or one service packs matching this service level. We basically assume
				// HP never sends us > 1 with the same level...
				PQWS.CPCCarePack matchingHPCarePack = allCarePacks.FirstOrDefault(cp => cp.ServiceLevel == serviceLevel.ServiceLevel);


				if (matchingHPCarePack != null && matchingHPCarePack.CarePackProductNumber != null) {
					//LogMessage("CarePackJIT : matchingHPCarePack : " & matchingHPCarePack.CarePackProductNumber)
					// Graft this care pack onto the TRO branch
					clsBranch carePackBranch = carePackRootBranch.childBranches.Values.FirstOrDefault(cpb => !cpb.Product == null && string.Equals(cpb.SKU, matchingHPCarePack.CarePackProductNumber, StringComparison.InvariantCultureIgnoreCase));
					//LogMessage("CarePackJIT : carePackBranch : " & carePackRootBranch.childBranches.Values.Count)
					//AndAlso Not troBranch.childBranches.Values.Contains(carePackBranch)
					if (carePackBranch != null && !carePackBranch.GraftedOnAt.Contains(troPath)) {
						if (troBranch.Graft(carePackBranch, "RefreshPQWSTRO", troPath, errorMessages, null)) {
							//LogMessage("CarePackJIT : TRO Added  ")

							troBranch.Update(errorMessages);
							//LogMessage("CarePackJIT : TRO Updated ")
							addedCount += 1;
							amended = true;
						}

					}


				}

				// Don't add more than MAXTROS care packs
				if (addedCount == MAXTROS)
					break; // TODO: might not be correct. Was : Exit For

			}
		} else {
			foreach ( cpk in from c in allCarePackswhere c.OrderOfPreference > 12orderby c.OrderOfPreference) {
				clsBranch carePackBranch = carePackRootBranch.childBranches.Values.FirstOrDefault(cpb => !cpb.Product == null && string.Equals(cpb.SKU, cpk.CarePackProductNumber, StringComparison.InvariantCultureIgnoreCase));
				//LogMessage("CarePackJIT : TRO  SKU : " & cpk.CarePackProductNumber)
				if (carePackBranch != null && !carePackBranch.GraftedOnAt.Contains(troPath)) {
					if (troBranch.Graft(carePackBranch, "RefreshPQWSTRO", troPath, errorMessages, null)) {
						troBranch.Update(errorMessages);
						amended = true;
					}

				}

			}


		}
		//LogMessage("CarePackJIT : TRO Ammended : " & amended)
		return amended;

	}

	private string SetUpAutoAdd(clsProduct systemProduct, List<PQWS.CPCCarePack> allCarePacks, clsBranch carePackRootBranch, string systemPath, clsAccount agentAccount, List<string> errorMessages, clsBranch systemBranch, clsBranch hwSupportBranch, clsAccount buyerAccount)
	{
		string autoAddCreatedPath = null;
		if (systemProduct.mfrCode == "HPE") {
			//LogMessage("CarePackJIT : SetUpAutoAdd : ")
			string sysFamily = systemProduct.i_Attributes_Code("FamMajor")(0).Translation.text(English);
			int slotTypeCode = 1;
			// 1 is the magic number for AutoAdds on the TROAA table
			object MAXAAS = 1;
			// 1 is also the maximum number of AutoAdds

			if (systemBranch.slots.Values.Where(sl => sl.Type.MajorCode.ToUpper == "WTY").Count == 0) {
				//LogMessage("CarePackJIT : SetUpAutoAdd : SlotsCreated ")
				object slt = new clsSlot(iq.i_slotType_Code("WTY")("CAREPACK"), systemBranch, "", 3, null, new NullableInt(), 0, 0);
			}


			List<clsTROAA> aas = iq.ServiceLevelTROAA.Values.Where(troaa => string.Equals(sysFamily, troaa.SysFamily, StringComparison.InvariantCultureIgnoreCase) && troaa.SlotTypeCode == slotTypeCode).OrderBy(troaa => troaa.DisplayOrder).ToList();

			if (aas == null)
				return null;

			int addedCount = 0;

			foreach (clsTROAA aa in aas) {
				object serviceLevel = aa.ServiceLevel;

				// There should be either none or one service packs matching this service level. We basically assume
				// HP never sends us > 1 with the same level...
				PQWS.CPCCarePack matchingHPCarePack = allCarePacks.FirstOrDefault(cp => cp.ServiceLevel == serviceLevel.ServiceLevel);

				if (matchingHPCarePack != null) {
					//LogMessage("CarePackJIT : SetUpAutoAdd : matchingHPCarePack ")
					clsBranch carePackBranch = hwSupportBranch.childBranches.Values.FirstOrDefault(cpb => !cpb.Product == null && string.Equals(cpb.SKU, matchingHPCarePack.CarePackProductNumber, StringComparison.InvariantCultureIgnoreCase));
					//LogMessage("CarePackJIT : SetUpAutoAdd : matchingHPCarePack " & hwSupportBranch.childBranches.Values.Count)
					if (carePackBranch != null) {
						List<clsQuantity> qtyCount = (from q in carePackBranch.Quantities.Valueswhere (string.IsNullOrEmpty(q.Path) || q.Path.Contains(systemPath)) && q.Region.Encompasses(agentAccount.BuyerChannel.Region) && q.NumPreInstalled == 1 && q.FOC == false).ToList();
						//LogMessage("CarePackJIT : SetUpAutoAdd : Quantities " & qtyCount.Count)
						if (qtyCount.Count == 0) {
							//LogMessage("CarePackJIT : SetUpAutoAdd : Creating AutoAdd ")
							object resultPath = string.Empty;
							clsProduct cpkProduct = iq.i_SKU(matchingHPCarePack.CarePackProductNumber);
							systemBranch.findChildBySKU2(systemPath, matchingHPCarePack.CarePackProductNumber, resultPath);
							autoAddCreatedPath = resultPath;
							//LogMessage("CarePackJIT : SetUpAutoAdd : Creating AutoAdd path " & resultPath)
							object quantity = new clsQuantity(agentAccount.SellerChannel.Region, resultPath, carePackBranch, 1, 0, 0, false, null);
						}
						//For Each q In carePackBranch.Quantities.Values
						//    Debug.WriteLine(q.Path & "| " & q.Region.Code & " | " & q.NumPreInstalled & " | " & q.FOC & " | " & q.displayName(English))
						//Next
						addedCount += 1;
					}
					// carePackBranch.Quantities.Clear()

				}

				// Don't add more than MAXTROS care packs
				if (addedCount == MAXAAS)
					break; // TODO: might not be correct. Was : Exit For

			}
		} else {
			foreach ( cpk in from c in allCarePackswhere c.OrderOfPreference == 1) {
				clsBranch carePackBranch = carePackRootBranch.childBranches.Values.FirstOrDefault(cpb => !cpb.Product == null && string.Equals(cpb.SKU, cpk.CarePackProductNumber, StringComparison.InvariantCultureIgnoreCase));
				if (!carePackBranch == null) {
					object qtyCount = from q in carePackBranch.Quantities.Valueswhere (string.IsNullOrEmpty(q.Path) || q.Path.Contains(systemPath)) && q.Region.Encompasses(agentAccount.BuyerChannel.Region) && q.NumPreInstalled == 1 && q.FOC == false;
					//LogMessage("CarePackJIT : SetUpAutoAdd : Quantities " & qtyCount.Count)
					if (qtyCount.Count == 0) {
						//LogMessage("CarePackJIT : SetUpAutoAdd : Creating AutoAdd ")
						object resultPath = string.Empty;
						systemBranch.findChildBySKU2(systemPath, cpk.CarePackProductNumber, resultPath);
						autoAddCreatedPath = resultPath;
						//LogMessage("CarePackJIT : SetUpAutoAdd : Creating AutoAdd path " & resultPath)
						object quantity = new clsQuantity(agentAccount.SellerChannel.Region, resultPath, carePackBranch, 1, 0, 0, false, null);
					}

				}

			}

		}
		//LogMessage("CarePackJIT : SetUpAutoAdd : autoAddCreatedPath " & autoAddCreatedPath)
		return autoAddCreatedPath;

	}


	private void BuildCarePackList(PQWS.CPCCarePack[] recommendedHPCarePacks, PQWS.CPCCarePack[] allHPCarePacks, List<PQWS.CPCCarePack> allCarePacks)
	{
		// The "Recommended" and "All" care pack lists are mutually exclusive so we need to process both
		if (recommendedHPCarePacks != null) {
			allCarePacks.AddRange(recommendedHPCarePacks);
		}

		if (allHPCarePacks != null) {
			allCarePacks.AddRange(allHPCarePacks);
		}

	}


	private void FindUnknownServiceLevels(ref List<PQWS.CPCCarePack> allCarePacks, ref List<string> newServiceLevels)
	{

		foreach ( hpCarePack in allCarePacks) {
			clsServiceLevel serviceLevel = iq.ServiceLevels.Values.FirstOrDefault(sl => (sl.ServiceLevel == hpCarePack.ServiceLevel));


			if (serviceLevel == null) {
				// We got a care pack with a service level we don't recognize
				newServiceLevels.Add(hpCarePack.ServiceLevel);

			}

		}

	}

	private void FindNewAndAmendedCarePacks(ref List<PQWS.CPCCarePack> allCarePacks, ref List<PQWS.CPCCarePack> amendedCarePacks, ref List<PQWS.CPCCarePack> newCarePacks, PQWS.CPCCountry countryDetails, clsAccount agentAccount, ref List<PQWS.CPCCarePack> carepacksNotAdded)
	{

		foreach ( hpCarePack in allCarePacks) {
			clsServiceLevel serviceLevel = iq.ServiceLevels.Values.FirstOrDefault(sl => (sl.ServiceLevel == hpCarePack.ServiceLevel));


			if (!serviceLevel == null && !(serviceLevel.Disabled | serviceLevel.PostWarranty)) {
				// Look for new/amended care packs
				object carePackSKUCode = hpCarePack.CarePackProductNumber;

				if (iq.i_SKU.ContainsKey(carePackSKUCode)) {
					//  If HasCarePackChanged(hpCarePack, iq.i_SKU(carePackSKUCode), countryDetails, agentAccount, serviceLevel) Then
					amendedCarePacks.Add(hpCarePack);
				//End If
				} else {
					newCarePacks.Add(hpCarePack);

				}
			} else {
				//'LogMessage("CarePackJIT : FindNewAndAmendedCarePacks : carepacksNotAdded " & IIf(hpCarePack.CarePackProductNumber IsNot Nothing, hpCarePack.CarePackProductNumber, ""))
				carepacksNotAdded.Add(hpCarePack);
			}
		}
		//LogMessage("CarePackJIT : FindNewAndAmendedCarePacks : allCarePcks: " & allCarePacks.Count & " , amendedCarePacks:  " & amendedCarePacks.Count & " , newCarePacks :" & newCarePacks.Count & " , notAdded :" & carepacksNotAdded.Count)
		foreach ( cpk in carepacksNotAdded) {
			allCarePacks.Remove(cpk);
		}

	}


	private void FindDeletedCarePacks(ref clsBranch hwSupportBranch, ref List<PQWS.CPCCarePack> allCarePacks, ref List<string> deletedCarePacks)
	{
		List<string> cpkSKUs = (from f in hwSupportBranch.childBranches.Valueswhere f.Product != nullf.Product.SKU).ToList();

		List<string> newSKUs = (from n in allCarePacksn.CarePackProductNumber).ToList();

		foreach ( sku in cpkSKUs) {
			if (newSKUs.Contains(sku)) {
				deletedCarePacks.Add(sku);

			}
		}

		//For Each carePackBranch As clsBranch In hwSupportBranch.childBranches.Values

		//    If Not carePackBranch.Product Is Nothing Then

		//        Dim sku As String = carePackBranch.Product.SKU
		//        Dim toDelete As Boolean = True

		//        For Each hpCarePack In allCarePacks
		//            If String.Equals(hpCarePack.CarePackProductNumber, sku, StringComparison.InvariantCultureIgnoreCase) Then
		//                Dim serviceLevel As clsServiceLevel = iq.ServiceLevels.Values.FirstOrDefault(Function(sl) (sl.ServiceLevel = hpCarePack.ServiceLevel))

		//                If Not (serviceLevel.Disabled Or serviceLevel.PostWarranty) Then
		//                    toDelete = False
		//                End If
		//                Exit For
		//            End If
		//        Next

		//        If toDelete Then
		//            If Not deletedCarePacks.Contains(sku) Then
		//                deletedCarePacks.Add(sku)
		//            End If
		//        End If
		//    End If
		//Next

	}

	private bool HasCarePackChanged(PQWS.CPCCarePack hpCarePack, clsProduct carePack, PQWS.CPCCountry countryDetails, clsAccount agentAccount, clsServiceLevel serviceLevel)
	{

		bool changed = false;

		if (carePack.Manufacturer != serviceLevel.Manufacturer)
			changed = true;

		return changed;

	}

	// Creates an IQ2 care pack product from HP care pack details
	private clsBranch CreateOrAmendCarePack(clsProduct carePack, clsBranch carePackBranch, PQWS.CPCCarePack hpCarePack, clsServiceLevel serviceLevel, clsBranch carePackRootBranch, clsAccount agentAccount, PQWS.CPCCountry countryDetails, ulong lid, List<string> errorMessages)
	{

		object carePackSKUCode = hpCarePack.CarePackProductNumber;

		if (carePack == null) {
			carePack = new clsProduct(carePackSKUCode, false, true, iq.i_sector_code("HPBCS"), iq.i_ProductType_Code("wty"), DateTime.Now, DateTime.Now.AddYears(5), true, false, true,
			serviceLevel.MfrCode, null, null);
			AuditLog.Instance.Add(AuditType.Information, string.Format("Care Pack SKU {0} created", carePackSKUCode), errorMessages, lid);
		}

		if (carePackBranch == null) {

			if (!(serviceLevel.PostWarranty | serviceLevel.Disabled)) {
				clsTranslation translation = iq.AddTranslation(carePackSKUCode, English, "CPQ", 0, null, 0, false);
				carePackBranch = new clsBranch(carePack, carePackRootBranch, translation, string.Empty, translation, translation, null, 0, false, "YTGB");

			}
		}

		if (carePack.Manufacturer != serviceLevel.Manufacturer) {
			carePack.mfrCode = serviceLevel.MfrCode;
		}

		// Create and assign new product attributes

		if (!(CreateCarePackAttributes(carePack, hpCarePack, serviceLevel, agentAccount, errorMessages))) {
			return null;
		}

		// Make sure the care pack branch has a slot
		if (!carePackBranch == null) {
			if (carePackBranch.slots.Count == 0) {
				object newSlot = new clsSlot(iq.i_slotType_Code("wty")("carepack"), carePackBranch, string.Empty, -1, null, new NullableInt(), 0, 0);
			} else if (carePackBranch.slots.Count > 1) {
				return null;
				// Shouldn't be more than one slot here
			}
		}

		carePack.update(errorMessages);

		return carePackBranch;

	}


	private void SendServiceLevelsEmail(List<string> newServiceLevels, List<string> errorMessages)
	{
		SmtpClient smtpclient = new SmtpClient();
		string address = iq.Addresses("iQuoteSupportEmail").Translation.text(English);

		StringBuilder sb = new StringBuilder();
		sb.AppendLine("<h2>New HP Service Levels returned by PQWS</h2>");
		foreach ( serviceLevel in newServiceLevels) {
			sb.AppendLine(serviceLevel);
		}

		MailMessage msg = new MailMessage(address, address, "New HP Service Level returned by PQWS", sb.ToString());
		msg.IsBodyHtml = true;
		msg.Priority = MailPriority.High;
		smtpclient.ServicePoint.MaxIdleTime = 1;

		try {
			smtpclient.Send(msg);
		} catch (Exception ex) {
			errorMessages.Add("Unable to send email at this time");
		}

	}

	// Creates IQ2 care pack product attributes from the HP care pack and the service level details held in IQ
	private bool CreateCarePackAttributes(clsProduct carePack, PQWS.CPCCarePack hpCarePack, clsServiceLevel serviceLevel, clsAccount agentAccount, ref List<string> errorMessages)
	{

		CreateCarePackAttributes = false;
		int numericValue = 0;
		if (carePack.Attributes == null)
			carePack.Attributes = new Dictionary<int, clsProductAttribute>();
		if (carePack.i_Attributes_Code == null)
			carePack.i_Attributes_Code = new Dictionary<string, List<clsProductAttribute>>();

		// Clear out any old attributes
		//Dim attributeCodes As New List(Of Integer)
		//For Each pa As clsProductAttribute In carePack.Attributes.Values
		//    attributeCodes.Add(pa.ID)
		//Next
		//For i As Integer = 0 To attributeCodes.Count - 1
		//    carePack.Attributes(attributeCodes(i)).delete(errorMessages)
		//Next

		clsAttribute attribute;
		clsTranslation translation;
		clsProductAttribute productAttribute;

		// Always create a description attribute
		attribute = iq.i_attribute_code("desc");
		clsProductAttribute att = (from at in carePack.Attributes.Valueswhere object.ReferenceEquals(at.Attribute, attribute)).FirstOrDefault;

		translation = iq.AddTranslation(hpCarePack.ServiceDescription, agentAccount.Language, "CPQ", 0, null, 0, false);
		AmmendAttribute(att, carePack, translation, 0, iq.i_unit_code("txt"), agentAccount, attribute, numericValue, errorMessages);

		// Always create a duration attribute
		attribute = iq.i_attribute_code("capacity");
		att = (from at in carePack.Attributes.Valueswhere object.ReferenceEquals(at.Attribute, attribute)).FirstOrDefault;
		int durUnit;
		clsUnit unitCode;

		if (serviceLevel.Duration % 12 > 0) {
			translation = iq.AddTranslation(string.Format("{0} mth", serviceLevel.Duration), English, "CPQ", 0, null, 0, false);
			durUnit = serviceLevel.Duration;
			unitCode = iq.i_unit_code("num");
		} else {
			int years = serviceLevel.Duration / 12;
			translation = iq.AddTranslation(string.Format("{0} yr", years), English, "CPQ", 0, null, 0, false);
			durUnit = years;
			unitCode = iq.i_unit_code("year");
		}
		AmmendAttribute(att, carePack, translation, durUnit, unitCode, agentAccount, attribute, numericValue, errorMessages);

		// Loop through all the ServiceLevel/Attribute mappings looking for further ProductAttributes to add
		foreach (string code in iq.ServiceLevelAttributeMap.Keys) {
			numericValue = 0;
			attribute = null;
			productAttribute = null;

			clsTranslation description = null;
			attribute = iq.ServiceLevelAttributeMap(code);
			switch (code.ToLower()) {

				case "fk_servicetype_id":
					if (!serviceLevel.ServiceType == null) {
						if (serviceLevel.MfrCode == "HPI") {
							attribute = iq.i_attribute_code("servicedelivery");
							//Else
							//    attribute = iq.ServiceLevelAttributeMap(code)
						}


						description = serviceLevel.ServiceType.Title;

					}
				case "fk_response_id":
					if (!serviceLevel.Response == null) {
						//attribute = iq.ServiceLevelAttributeMap(code)
						description = serviceLevel.Response.Title;

					}
				case "hpedmr":
					if (serviceLevel.MfrCode == "HPE") {
						attribute = iq.i_attribute_code("DMR_ISS");
						if (serviceLevel.HpeDmr) {
							description = iq.AddTranslation("DMR", English, "CPQ", 0, null, 0, false);
						} else {
							if (serviceLevel.HpeCdmr) {
								description = iq.AddTranslation("CDMR", English, "CPQ", 0, null, 0, false);
							} else {
								description = iq.AddTranslation("No DMR", English, "CPQ", 0, null, 0, false);
							}

						}
					}
				case "hpiadp":
					//    attribute = iq.i_attribute_code("options")
					if (serviceLevel.HpiAdp) {
						numericValue = 1;
					}
					description = iq.AddTranslation("ADP", English, "CPQ", 0, null, 0, false);
				case "hpidmr":
					//   attribute = iq.i_attribute_code("options")
					if (serviceLevel.HpiDmr) {
						numericValue = 1;
					}
					description = iq.AddTranslation("DMR", English, "CPQ", 0, null, 0, false);
				case "hpitravel":
					//  attribute = iq.i_attribute_code("options")
					if (serviceLevel.HpiTravel) {
						numericValue = 1;
					}
					description = iq.AddTranslation("Travel", English, "CPQ", 0, null, 0, false);
				case "hpitracing":
					// attribute = iq.i_attribute_code("options")
					if (serviceLevel.HpiTracing) {
						numericValue = 1;
					}
					description = iq.AddTranslation("Tracing", English, "CPQ", 0, null, 0, false);
				case "hpitheft":
					//attribute = iq.i_attribute_code("options")
					if (serviceLevel.HpiTheft) {
						numericValue = 1;
					}
					description = iq.AddTranslation("Theft", English, "CPQ", 0, null, 0, false);
			}


			if (attribute != null && description != null) {
				att = (from at in carePack.Attributes.Valueswhere object.ReferenceEquals(at.Attribute, attribute)).FirstOrDefault;

				AmmendAttribute(att, carePack, description, 0, iq.i_unit_code("txt"), agentAccount, attribute, numericValue, errorMessages);
				if (attribute.Code == "DMR_ISS") {
					attribute = iq.i_attribute_code("options");
					att = (from at in carePack.Attributes.Valueswhere object.ReferenceEquals(at.Attribute, attribute)).FirstOrDefault;
					AmmendAttribute(att, carePack, description, 0, iq.i_unit_code("txt"), agentAccount, attribute, numericValue, errorMessages);
					//   productAttribute = New clsProductAttribute(carePack, attribute, 0, iq.i_unit_code("txt"), description)
				}

			}
		}

		CreateCarePackAttributes = true;

	}

	// Vets whether the HP Care Pack passes the quality check
	private bool ValidateHPCarePacks(PQWS.CPCHierarchyCarePackResults hpCarePackResults)
	{

		ValidateHPCarePacks = false;

		// Data is valid if more than a certain number of care packs is returned

		if (!hpCarePackResults == null) {
			int count = 0;

			if (!hpCarePackResults.RecommendedHPCarePacks == null)
				count = hpCarePackResults.RecommendedHPCarePacks.Length;
			if (!hpCarePackResults.AllHPCarePacks == null)
				count += hpCarePackResults.AllHPCarePacks.Length;

			// Ensure we received more than the configured minimum no. of care packs
			int min = 50;
			if (!ConfigurationManager.AppSettings("MinHPCarePacks") == null) {
				min = Convert.ToInt32(ConfigurationManager.AppSettings("MinHPCarePacks"));
			}
			if (count >= min)
				ValidateHPCarePacks = true;

		}

	}

	public List<string> CarePackJIT_Old(clsGenericAjaxRequest request, clsRegion importregion = null)
	{
		try {
			if (request.BranchPath == null)
				return;
			object agentAccount = iq.seshTyped<clsAccount>(request.lid, "AgentAccount");
			object buyerAccount = iq.seshTyped<clsAccount>(request.lid, "BuyerAccount");
			List<string> errorMEssages = new List<string>();
			if (request.BranchPath == "tree.1")
				request.BranchPath = iq.sesh(request.lid, "treecursor");
			if (request.BranchPath == null)
				return;

			bool createdTROBranch = false;
			string autoAddCreated = null;


			if (iq.Branches.ContainsKey(Split(request.BranchPath, ".").Last)) {
				object branch = iq.Branches(Split(request.BranchPath, ".").Last);
				string sysPath = "";
				branch = branch.FindSystemAbove(request.BranchPath, sysPath);
				if (branch == null)
					return;
				sysPath = Left(request.BranchPath, request.BranchPath.IndexOf(branch.ID) + Len(branch.ID.ToString));
				if (branch.Product != null) {
					Dictionary<string, clsBranch> tgtList = new Dictionary<string, clsBranch>();
					//Contains the sku and a reference to the branch int he AllCPQ tree
					Dictionary<string, clsBranch> srcList = new Dictionary<string, clsBranch>();
					//contains the sku and a reference to the branch under the hw support section

					clsBranch CPQRootBranch = iq.i_SpecialBranches("cpqroot");

					//Find HW Support
					string hwsupportpath = Left(sysPath, Len(sysPath) - Len(Split(sysPath, ".").Last) - 1);
					clsBranch hwsupportBranch = branch.FindBranchByNameBelow("HW Support", Left(sysPath, Len(sysPath) - Len(Split(sysPath, ".").Last) - 1), true, 12, hwsupportpath);

					if (hwsupportBranch == null) {
						//Create it
						object svcBranchPath = "";
						object svcbranch = branch.FindBranchByNameBelow("Services", "", true, 12, svcBranchPath);
						if (svcbranch == null) {
							object aoBranchPath = "";
							object aoBranch = branch.FindBranchByNameBelow("All Options", "", true, 12, aoBranchPath);
							svcbranch = new clsBranch(null, aoBranch, iq.AddTranslation("Services", English, "", 0, null, 0, false), "", iq.AddTranslation("Options", English, "", 0, null, 0, false), iq.AddTranslation("Option", English, "", 0, null, 0, false), null, 40, false, "Y");
						}
						hwsupportBranch = new clsBranch(null, svcbranch, iq.AddTranslation("HW Support", English, "", 0, null, 0, false), "", iq.AddTranslation("Options", English, "", 0, null, 0, false), iq.AddTranslation("Option", English, "", 0, null, 0, false), null, 0, false, "B");
					}

					foreach ( bb in hwsupportBranch.childBranches) {
						if (bb.Value.Product != null) {
							if (srcList.ContainsKey(bb.Value.Product.SKU)) {
							//bb.Value.delete(errorMEssages) 'Duplicate, not sure why??
							} else {
								if (agentAccount == null) {
									agentAccount = iq.seshTyped<clsAccount>(request.lid, "AgentAccount");
								}

								if (!bb.Value.PruneInForce(hwsupportpath + "." + bb.Value.ID, agentAccount.SellerChannel)) {
									if (!string.IsNullOrEmpty(bb.Value.Product.SKU)) {
										srcList.Add(bb.Value.Product.SKU, bb.Value);
									}

								}

							}
						}

					}

					//Deal with the TRO's and autoadds here 
					//Server rule time in iq1 parlance
					//Get family major code
					object fm = "";
					if (branch.Product.i_Attributes_Code.ContainsKey("FamMajor")) {
						fm = branch.Product.i_Attributes_Code("FamMajor").First.Translation.text(English);
					}

					object troresp;
					object autoresp = "24x7, 4hr";
					object autoserv = "Foundation Care";
					object troserv;
					object duration = 3;
					object options = "No DMR";

					// Tro checks
					if (fm.StartsWith("ML1") | fm.StartsWith("ML31") | fm.StartsWith("MS0") | fm.StartsWith("DL320e") | fm.StartsWith("DL160")) {
						troresp = "Next Business Day";
						troserv = "Proactive Care";
					} else {
						troresp = "24x7, 4hr";
						troserv = "Proactive Care";
					}

					//AutoAdd checks 
					foreach ( r in {
						"[D|M]L3[0-9]+eG8",
						"DL16[0|5]G[7|8|9]",
						"DL1[6|8]0G9",
						"DL120G[6|7]",
						"ML10",
						"ML110G7",
						"MS001"
					}) {
						Regex rex = new Regex(r);
						if (rex.IsMatch(fm)) {
							autoresp = "Next Business Day";
							break; // TODO: might not be correct. Was : Exit For
						}
					}

					Dictionary<string, clsBranch> trocpqs = new Dictionary<string, clsBranch>();

					//End TRO and autoadd Setup


					List<string> ToCreate = new List<string>();

					//Go get info
					object con = new SqlConnection("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35");
					con.Open();
					Dictionary<string, object> @params = new Dictionary<string, object>();
					Dictionary<string, object> retVals = new Dictionary<string, object>();
					@params.Add("HWsku", branch.Product.SKU);
					@params.Add("countryCode", agentAccount.SellerChannel.Region.Code);
					object rdr = dataAccess.da.ExecuteSP(con, "products.[CarePackFinder]", @params, retVals);
					while (rdr.Read) {
						if (!IsDBNull(rdr("ccDescription"))) {
							//Do we have this care pack in iq2?
							clsBranch cpqBranch = new clsBranch();
							if (iq.i_SKU.ContainsKey(rdr("CPKpartnum")) && iq.i_SKU(rdr("CPKpartnum")).Branches.Count > 0) {
								cpqBranch = iq.i_SKU(rdr("CPKpartnum")).Branches.FirstOrDefault;
								//CPQ should only be on 1 branch and grafted everywhere else...
								if (!tgtList.ContainsKey(rdr("CPKpartnum").ToString.Trim()))
									tgtList.Add(rdr("CPKpartnum").ToString.Trim(), cpqBranch);

							} else {
								//No create it
								ToCreate.Add(dataAccess.da.SqlEncode(rdr("CPKpartnum")));
							}

							if ({
								"DTO",
								"NBK"
							//PPS items DTO and NBK go in here 
							}.Contains(branch.Product.ProductType.Code)) {
								if ((int)rdr("cpkranking") == 1 & cpqBranch.Quantities.Values.Count == 0) {
									//  End If
									//If Not cpqBranch.Quantities.Values.Where(Function(q) (String.IsNullOrEmpty(q.Path) OrElse q.Path.Contains(sysPath)) AndAlso q.Region.Encompasses(agentAccount.BuyerChannel.Region) AndAlso q.NumPreInstalled = 1 AndAlso q.FOC = False).Count > 0 Then
									object resultPath = "";
									branch.findChildBySKU2(sysPath, rdr("CPKpartnum"), resultPath);
									object q = new clsQuantity(agentAccount.BuyerChannel.Region, resultPath, cpqBranch, 1, 0, 0, false, null);
									AuditLog.Instance.Add(AuditType.Information, "CarePack SKU Qty record" + rdr("CPKpartnum") + " Syspath " + sysPath, errorMEssages, "");
									autoAddCreated = resultPath;

								}

							} else {
								//Dim test = From q In cpqBranch.Quantities.Values Where String.IsNullOrEmpty(q.Path) AndAlso q.Region.Encompasses(agentAccount.BuyerChannel.Region) AndAlso q.NumPreInstalled = 1 AndAlso q.FOC = False
								//If cpqBranch.Product.ProductType.Code = "SWD" Then
								//    Dim abc = autoresp
								//    Dim opt = options

								//End If
								//Auto adds
								if (cpqBranch != null && cpqBranch.Product != null && cpqBranch.Product.i_Attributes_Code.ContainsKey("servicelevel") && cpqBranch.Product.i_Attributes_Code("servicelevel").First.Translation.text(English) == autoserv && cpqBranch.Product.i_Attributes_Code.ContainsKey("response") && cpqBranch.Product.i_Attributes_Code("response").First.Translation.text(English) == autoresp && cpqBranch.Product.i_Attributes_Code.ContainsKey("DMR_ISS") && cpqBranch.Product.i_Attributes_Code("DMR_ISS").First.Translation.text(English) == options && cpqBranch.Product.i_Attributes_Code.ContainsKey("capacity") && cpqBranch.Product.i_Attributes_Code("capacity").First.NumericValue == duration) {
									//This is an auto add so check its quantity record
									object x = from z in cpqBranch.Quantities.Valueswhere z.Region.Encompasses(agentAccount.BuyerChannel.Region) && z.NumPreInstalled == 1 && z.FOC == false;

									if (x.Count > 1) {
										AuditLog.Instance.Add(AuditType.Information, "CarePack SKUmultiple qty record for preisntalled" + rdr("CPKpartnum") + " Syspath " + sysPath, errorMEssages, "");
									}

									if (!cpqBranch.Quantities.Values.Where(q => (string.IsNullOrEmpty(q.Path) || q.Path.Contains(sysPath)) && q.Region.Encompasses(agentAccount.BuyerChannel.Region) && q.NumPreInstalled == 1 && q.FOC == false).Count > 0) {
										object resultPath = "";
										branch.findChildBySKU2(sysPath, rdr("CPKpartnum"), resultPath);
										object q = new clsQuantity(agentAccount.BuyerChannel.Region, resultPath, cpqBranch, 1, 0, 0, false, null);
										AuditLog.Instance.Add(AuditType.Information, "CarePack SKU Qty record" + rdr("CPKpartnum") + " Syspath " + sysPath, errorMEssages, "");
										autoAddCreated = resultPath;
									}
								}
							}

							if (branch.Product.ProductType.Code == "SVR") {
								if (cpqBranch != null && cpqBranch.Product != null && cpqBranch.Product.i_Attributes_Code.ContainsKey("servicelevel") && cpqBranch.Product.i_Attributes_Code("servicelevel").First.Translation.text(English) == troserv && cpqBranch.Product.i_Attributes_Code.ContainsKey("response") && cpqBranch.Product.i_Attributes_Code("response").First.Translation.text(English) == troresp && cpqBranch.Product.i_Attributes_Code.ContainsKey("options") && cpqBranch.Product.i_Attributes_Code("options").First.Translation.text(English) == options && cpqBranch.Product.i_Attributes_Code.ContainsKey("capacity") && cpqBranch.Product.i_Attributes_Code("capacity").First.NumericValue == duration)
									trocpqs.Add(cpqBranch.Product.SKU, cpqBranch);
							} else if ({
								"DTO",
								"NBK"
							}.Contains(branch.Product.ProductType.Code)) {
								if (cpqBranch != null && (int)rdr("cpkranking") == 2)
									trocpqs.Add(cpqBranch.Product.SKU, cpqBranch);
							}
						}
					}
					rdr.Close();
					con.Close();
					//Find TRO branch
					string troPath = "";
					clsBranch troBranch = branch.FindBranchByNameBelow("HP Top Recommended", sysPath, false, 12, troPath);
					clsBranch troCPQBranch;
					if (troBranch == null) {
						troBranch = new clsBranch(null, branch, iq.AddTranslation("Top Recommended", English, "", 0, null, 0, false), "", iq.AddTranslation("Top Recommended", English, "", 0, null, 0, false), iq.AddTranslation("Top Recommended", English, "", 0, null, 0, false), null, 0, false, "H");
						troPath = sysPath + "." + troBranch.ID;
						createdTROBranch = true;
					}
					troCPQBranch = branch.FindBranchByNameBelow("Care Pack", sysPath, false, 12, troPath);

					if (troCPQBranch == null) {
						//Create
						troCPQBranch = new clsBranch(null, troBranch, iq.AddTranslation("Care Pack", English, "", 0, null, 0, false), "/images/product/category/cat2.png", iq.AddTranslation("HP Top Recommended", English, "", 0, null, 0, false), iq.AddTranslation("HP Top Recommended", English, "", 0, null, 0, false), null, 0, false, "I");
					}

					object hasList = new List<string>();
					List<clsBranch> dupChildBranch = new List<clsBranch>();
					foreach (clsBranch child in troCPQBranch.childBranches.Values) {
						if (!child.PruneInForce(troPath + "." + child.ID, agentAccount.SellerChannel)) {
							if (!trocpqs.ContainsKey(child.Product.SKU)) {
								//Remove (delete or prune, not sure)
								object p = new clsPrune(troPath + "." + child.ID, new NullableInt(agentAccount.SellerChannel.ID), "CPQJIT");
							} else {
								if (!hasList.Contains(child.Product.SKU)) {
									hasList.Add(child.Product.SKU);
								// duplicate tro item
								} else {
									//  Dim p = New clsPrune(troPath & "." & child.ID, New NullableInt(agentAccount.SellerChannel.ID), "CPQJIT")
									dupChildBranch.Add(child);
								}
							}
						} else {
							if (trocpqs.ContainsKey(child.Product.SKU)) {
								foreach (clsPrune prune in child.Prunes.Values) {
									if (prune.Path == troPath + "." + child.ID && (prune.ChannelID.value == null || IsDBNull(prune.ChannelID.value) || prune.ChannelID.value == agentAccount.SellerChannel.ID))
										prune.delete();
								}
							}
						}
					}

					//For Each br In dupChildBranch
					//    Dim priceCount = troCPQBranch.childBranches(br.ID).Quantities.Count
					//    'delete quantities
					//    For i = 0 To priceCount - 1
					//        Dim qty = troCPQBranch.childBranches(br.ID).Quantities.Values.First
					//        troCPQBranch.childBranches(br.ID).Quantities(qty.ID).Delete(errorMEssages)
					//    Next
					//    'Delte slots
					//    Dim slotsCount = troCPQBranch.childBranches(br.ID).slots.Count
					//    For i = 0 To slotsCount - 1
					//        Dim slot = troCPQBranch.childBranches(br.ID).slots.Values.First
					//        troCPQBranch.childBranches(br.ID).slots(slot.ID).delete(errorMEssages)
					//    Next
					//    troCPQBranch.childBranches(br.ID).delete(errorMEssages)

					//Next

					foreach ( tro in trocpqs.Keys.Except(hasList)) {
						//Dim troitem = New clsBranch(trocpqs(tro).Product, troCPQBranch, iq.AddTranslation(trocpqs(tro).Product.sku,English,"",0,Nothing,0,False),"",iq.AddTranslation("HP Top Recommended", English, "", 0, Nothing, 0, False),iq.AddTranslation("HP Top Recommended", English, "", 0, Nothing, 0, False),Nothing,0,False,"I"))
						foreach ( prne in trocpqs(tro).Prunes.Where(p => !IsDBNull(p.Value.ChannelID.value) && p.Value.ChannelID.value == agentAccount.SellerChannel.ID && (string.IsNullOrEmpty(p.Value.Path) | p.Value.Path == troPath + "." + trocpqs(tro).ID))) {
							prne.Value.delete();
						}
						troCPQBranch.Graft(trocpqs(tro), "TROCPQ", troPath, errorMEssages);
					}

					if (ToCreate.Count > 0) {
						object listSKUs = new List<string>();

						object iq2con = dataAccess.da.OpenDatabase();
						int nextBId = 0;
						object pawc = null;
						// dataAccess.da.MakeWriteCacheFor(iq2con, "ProductAttribute")
						object swc = null;
						//dataAccess.da.MakeWriteCacheFor(iq2con, "Slot")
						object bwc = null;
						//dataAccess.da.MakeWriteCacheFor(iq2con, "Branch", nextBId, True)
						object nextKey = 0;
						//clsTranslation.NextKey
						object twc = null;
						//dataAccess.da.MakeWriteCacheFor(iq2con, "Translation")

						/// Could expand to education etc, only does HW support at the moment ML
						object sql2 = "select description,sl1.sLabel as response,sl2.sLabel as ServiceLevel, sl3.sLabel as Options,duration ,opttype,optfamily " + ",options.optsku,case when sl1.sLabel like '%24x7%' then 1 else 0 end as tfs ,travel,tracing,ADP,DMR,CTR,OnSite, OptTypeParent as L1,OptTypeName as L2," + "ISNULL(a.translation, CASE " + "WHEN ot2.OptTypeParent = 'Software' AND ot2.OptTypeName NOT LIKE 'Microsoft OS' THEN ISNULL(Options.Technology, Options.OptFamily)  " + "WHEN ot2.OptTypeParent = 'Services' AND ot2.OptTypeName = 'SW Support' THEN CASE WHEN Options.Technology <> 'SUP' THEN Options.Technology ELSE Options.OptFamily END  " + "END) as L3 " + "                   from h3.iq.products.options " + "left outer join h3.iq.products.[CarePack_Properties]  on options.optsku=[CarePack_Properties].optsku " + "left outer join h3.iq.products.carepack_servicelevels sl1 on sl1.sCode = ResponseCode_ISS " + "left outer join h3.iq.products.carepack_servicelevels sl2 on sl2.sCode = servicelevel_iss " + "left outer join h3.iq.products.carepack_servicelevels sl3 on sl3.sCode = options_iss " + "left outer join h3.iq.products.opttypes ot2 on ot2.OptTypeCode = options.opttype " + "left outer join h3.iq.dbo.Abbreviations a ON a.code =  CASE " + "WHEN ot2.OptTypeParent = 'Software' AND ot2.OptTypeName NOT LIKE 'Microsoft OS' THEN ISNULL(Options.Technology, Options.OptFamily) " + "WHEN ot2.OptTypeParent = 'Services' AND ot2.OptTypeName = 'SW Support' THEN CASE WHEN Options.Technology <> 'SUP' THEN Options.Technology ELSE Options.OptFamily END " + "End " + "WHERE OptTypeName='HW Support' and options.optsku IN (" + Join(ToCreate.ToArray, ",") + ")";
						if (!iq.i_attribute_code.ContainsKey("Tracing"))
							object d = new clsAttribute("Tracing", iq.AddTranslation("Tracing", English, "", 0, twc, nextKey, true), 0);
						if (!iq.i_attribute_code.ContainsKey("ADP"))
							object d = new clsAttribute("ADP", iq.AddTranslation("ADP", English, "", 0, twc, nextKey, true), 0);
						if (!iq.i_attribute_code.ContainsKey("CTR"))
							object d = new clsAttribute("CTR", iq.AddTranslation("CTR", English, "", 0, twc, nextKey, true), 0);

						object rdr2 = dataAccess.da.DBExecuteReader(con, sql2);
						while (rdr2.Read) {
							clsProduct prod;
							AuditLog.Instance.Add(AuditType.Information, "CarePack SKU created" + rdr2("optsku"), errorMEssages, "");

							if (!iq.i_SKU.ContainsKey(rdr2("optsku"))) {
								prod = new clsProduct(rdr2("optsku").ToString, false, true, iq.i_sector_code("HPBCS"), iq.i_ProductType_Code(rdr2("opttype")), DateTime.Now, DateTime.Now.AddYears(5), true, false, true,
								buyerAccount.mfrCode, "", "");
							} else {
								prod = iq.i_SKU(rdr2("optsku"));
							}

							object cpqBranch = new clsBranch(prod, CPQRootBranch, iq.AddTranslation(rdr2("optsku").ToString, English, "CPQ", 0, twc, nextKey, false), "", iq.AddTranslation("Carepacks", English, "", 0, twc, nextKey, false), iq.AddTranslation("Carepack", English, "", 0, twc, nextKey, false), null, 0, false, "B",
							bwc, nextBId);
							object a = new clsProductAttribute(prod, iq.i_attribute_code("mfrSKU"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("optsku").ToString, English, "CPQ", 0, twc, nextKey, false), pawc);
							if (!IsDBNull(rdr2("description")))
								object b = new clsProductAttribute(prod, iq.i_attribute_code("desc"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("description").ToString, English, "CPQ", 0, twc, nextKey, false), pawc);
							if (!IsDBNull(rdr2("duration")))
								object b = new clsProductAttribute(prod, iq.i_attribute_code("capacity"), (int)rdr2("duration"), iq.i_unit_code("year"), null, pawc);

							if (!IsDBNull(rdr2("servicelevel")))
								object b = new clsProductAttribute(prod, iq.i_attribute_code("servicelevel"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("servicelevel").ToString, English, "CPQ", 0, twc, nextKey, false), pawc);
							if (!IsDBNull(rdr2("response")))
								object b = new clsProductAttribute(prod, iq.i_attribute_code("response"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("response").ToString, English, "CPQ", 0, twc, nextKey, false), pawc);
							//this is ISS (Servers and Storage Device) DMR
							if (!IsDBNull(rdr2("options")))
								object b = new clsProductAttribute(prod, iq.i_attribute_code("DMR_ISS"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("options").ToString, English, "CPQ", 0, twc, nextKey, false), pawc);
							if (!IsDBNull(rdr2("tfs")))
								object b = new clsProductAttribute(prod, iq.i_attribute_code("twentyfourseven"), rdr2("tfs"), iq.i_unit_code("txt"), iq.AddTranslation("24x7", English, "CPQ", 0, twc, nextKey, false), pawc);
							if (!IsDBNull(rdr2("travel")))
								object b = new clsProductAttribute(prod, iq.i_attribute_code("travel"), rdr2("travel"), iq.i_unit_code("txt"), null, pawc);
							if (!IsDBNull(rdr2("Tracing")))
								object b = new clsProductAttribute(prod, iq.i_attribute_code("tracing"), rdr2("Tracing"), iq.i_unit_code("txt"), null, pawc);
							if (!IsDBNull(rdr2("ADP")))
								object b = new clsProductAttribute(prod, iq.i_attribute_code("ADP"), rdr2("ADP"), iq.i_unit_code("txt"), null, pawc);
							//this PPS(Desktop and NoteBook) DMR
							if (!IsDBNull(rdr2("DMR")))
								object b = new clsProductAttribute(prod, iq.i_attribute_code("DMR"), rdr2("DMR"), iq.i_unit_code("txt"), null, pawc);
							if (!IsDBNull(rdr2("OnSite")))
								object b = new clsProductAttribute(prod, iq.i_attribute_code("OnSite"), rdr2("OnSite"), iq.i_unit_code("txt"), null, pawc);
							if (!IsDBNull(rdr2("CTR")))
								object b = new clsProductAttribute(prod, iq.i_attribute_code("CTR"), rdr2("CTR"), iq.i_unit_code("txt"), null, pawc);

							object s = new clsSlot(iq.i_slotType_Code(rdr2("OPTTYPE"))(rdr2("OPTFAMILY")), cpqBranch, "", -1, null, new NullableInt(), 0, 0, swc);


							if (!tgtList.ContainsKey(rdr2("optsku")))
								tgtList.Add(rdr2("optsku"), cpqBranch);
							listSKUs.Add(rdr2("optsku"));
							ToCreate.Remove(dataAccess.da.SqlEncode(rdr2("optsku")));
						}

						foreach ( sku in ToCreate) {
							if (!iq.i_SKU.ContainsKey(sku)) {
								object prod = new clsProduct(sku.Trim("'").Trim(), false, true, iq.i_sector_code("HPBCS"), iq.i_ProductType_Code("WTY"), DateTime.Now, DateTime.Now.AddYears(5), true, false, true,
								buyerAccount.mfrCode, "", "");
								object cpqBranch = new clsBranch(prod, CPQRootBranch, iq.AddTranslation(sku.Trim("'").Trim(), English, "CPQ", 0, twc, nextKey, true), "", iq.AddTranslation("Carepacks", English, "", 0, twc, nextKey, true), iq.AddTranslation("Carepack", English, "", 0, twc, nextKey, true), null, 0, false, "B",
								bwc, nextBId);

								object a = new clsProductAttribute(prod, iq.i_attribute_code("mfrSKU"), 0, iq.i_unit_code("txt"), iq.AddTranslation(sku.Trim("'").Trim(), English, "CPQ", 0, twc, nextKey, false), pawc);

								if (!tgtList.ContainsKey(sku.Trim("'").Trim()))
									tgtList.Add(sku.Trim("'").Trim(), cpqBranch);
							}
						}

						//dataAccess.da.BulkWrite(iq2con, twc, "Translation")
						//dataAccess.da.BulkWrite(iq2con, bwc, "Branch")
						//dataAccess.da.BulkWrite(iq2con, pawc, "ProductAttribute")
						//dataAccess.da.BulkWrite(iq2con, swc, "Slot")


						try {
							wsconsumer.I_UniTranClient cl = new wsconsumer.I_UniTranClient();
							//cl.Endpoint.Binding.OpenTimeout = TimeSpan.FromSeconds(5)
							//cl.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(10)

							//Dim SKUlist() As String = cl.AllProducts(agentAccount.SellerChannel.Code)
							string WSRQKey = agentAccount.Priceband.text;
							if (iq.SeshContains(request.lid, "gk_SessionID")) {
								WSRQKey += ";" + iq.sesh(request.lid, "gk_sessionID");
							}

							object lps = cl.ListPrices(buyerAccount.SellerChannel.Region.Code, buyerAccount.Currency.Code, listSKUs.ToArray);

							object sp = null;
							if (!buyerAccount.SellerChannel.priceConfig & 2) {
								//is there a webservice
								if ((buyerAccount.SellerChannel.priceConfig & 8)) {
									IQ.wsconsumer.clsStockPriceRequest unirequest = new wsconsumer.clsStockPriceRequest();
									unirequest = cl.BuildRequest(buyerAccount.SellerChannel.Code, buyerAccount.Priceband.text, (string)buyerAccount.User.ID, (string)request.lid, buyerAccount.Currency.Code, "", WSRQKey, listSKUs.ToArray, "", buyerAccount.User.Email,
									"iquote2");
									object handle = cl.RequestStockPrices(unirequest);
									if (handle != -1)
										sp = cl.CheckStockPrices(handle, true, 30);
								}
							}


							foreach ( sku in listSKUs) {
								if (iq.i_SKU.ContainsKey(sku)) {
									clsVariant newListVariant = new clsVariant("list", iq.i_SKU(sku), HP, sku, "List Price", "", "", buyerAccount.SellerChannel.Region, false, null,
									-1);
									float price = 0;
									foreach ( lp in lps) {
										if (lp.SKU == sku) {
											price = lp.price;
										}
									}
									object p = new clsPrice(newListVariant, iq.priceBands(""), new NullablePrice(price, buyerAccount.Currency, true), "");

									price = 0;
									if (sp != null) {
										foreach ( itm in sp.items) {
											if (itm.sku == sku) {
												price = itm.ListPrice;
											}
										}

									}
									if (price != 0) {
										clsVariant newChanVariant = new clsVariant("", iq.i_SKU(sku), buyerAccount.SellerChannel, sku, "", "", "", buyerAccount.SellerChannel.Region, false, null,
										-1);
										p = new clsPrice(newChanVariant, buyerAccount.Priceband, new NullablePrice(price, buyerAccount.Currency, false), "");
									}
								}

							}

						} catch (Exception ex) {
							ErrorLog.Add(ex);

						}
						//dont fail on unitran failure...
					}

					bool changed = false;

					if (ToCreate.Count > 0) {
						//Things that shouldn't be there
						foreach ( sku in srcList.Keys.Except(tgtList.Keys)) {
							//Prune at this point?
							changed = true;
							object p = new clsPrune(hwsupportpath + "." + srcList(sku).ID, new NullableInt(agentAccount.SellerChannel.ID), "CPQJIT");
						}


						foreach ( sku in tgtList.Keys.Except(srcList.Keys)) {
							changed = true;
							hwsupportBranch.Graft(tgtList(sku), "CPQJIT", hwsupportpath, errorMEssages, null);
						}

					}

					List<string> toreturn = new List<string>();
					if (changed) {
						object shs = iq.seshTyped<Dictionary<string, clsScreenHeader>>(request.lid, "screenHeaders");
						if (shs != null && shs.ContainsKey(hwsupportpath)) {
							shs.Remove(hwsupportpath);
						}
						if (shs != null && shs.ContainsKey(troPath)) {
							shs.Remove(troPath);
						}

						if (iq.sesh(request.lid, "pathDataLoaded") != null && iq.seshTyped<List<string>>(request.lid, "pathDataLoaded").Contains(hwsupportpath))
							iq.sesh(request.lid, "pathDataLoaded").Remove(hwsupportpath);
						if (iq.sesh(request.lid, "pathDataLoaded") != null && iq.seshTyped<List<string>>(request.lid, "pathDataLoaded").Contains(troPath))
							iq.sesh(request.lid, "pathDataLoaded").Remove(troPath);
						toreturn.Add("cmd=openTab&path=" + troPath);
					}

					if (autoAddCreated != null) {
						toreturn.Add("addpart:" + autoAddCreated);
					}

					if (createdTROBranch) {
						toreturn.Add("refreshall");
					}

					if (toreturn.Count > 0)
						return toreturn;
					else
						return null;



				}
			}

		} catch (Exception ex) {
			ErrorLog.Add(ex);
		}
	}

	private void CreateCarePackVariants(List<PQWS.CPCCarePack> carePackList, ulong lid, clsAccount agentAccount, clsAccount buyerAccount)
	{
		try {
			wsconsumer.I_UniTranClient cl = new wsconsumer.I_UniTranClient();
			string WSRQKey = agentAccount.Priceband.text;
			if (iq.SeshContains(lid, "gk_SessionID")) {
				WSRQKey += ";" + iq.sesh(lid, "gk_sessionID");
			}

			object sp = null;
			// If Not buyerAccount.SellerChannel.priceConfig And 2 Then
			string[] skuArray = (from c in carePackListc.CarePackProductNumber).ToArray();
			//is there a webservice
			if ((buyerAccount.SellerChannel.priceConfig & 8)) {
				IQ.wsconsumer.clsStockPriceRequest unirequest = new wsconsumer.clsStockPriceRequest();
				unirequest = cl.BuildRequest(buyerAccount.SellerChannel.Code, buyerAccount.Priceband.text, (string)buyerAccount.User.ID, (string)lid, buyerAccount.Currency.Code, "", WSRQKey, skuArray, "", buyerAccount.User.Email,
				"iquote2");
				object handle = cl.RequestStockPrices(unirequest);
				if (handle != -1)
					sp = cl.CheckStockPrices(handle, true, 30);
			}
			//End If

			clsProduct carePack;
			foreach ( cpk in carePackList) {
				if (iq.i_SKU.ContainsKey(cpk.CarePackProductNumber)) {
					//carePack = New clsProduct()
					carePack = iq.i_SKU(cpk.CarePackProductNumber);
					float price = cpk.PriceLocalList;
					clsPrice p;
					object SKUvariant = carePack.Variants.Values.Where(v => v.HasListPrice(buyerAccount.SellerChannel.DefaultCurrency));
					if (SKUvariant.Count == 0) {
						clsVariant newListVariant = new clsVariant("list", carePack, HP, carePack.SKU, "List Price", "", "", buyerAccount.SellerChannel.Region, false, null,
						-1);
						p = new clsPrice(newListVariant, iq.priceBands(""), new NullablePrice(price, buyerAccount.Currency, true), "CarePackJIT");
					}
					price = 0;
					if (sp != null) {
						foreach ( itm in sp.items) {
							if (itm.sku == carePack.SKU) {
								price = itm.customerPrice;
								break; // TODO: might not be correct. Was : Exit For
							}
						}
					}
					if (price != 0) {
						object SKUvariant2 = carePack.Variants.Values.Where(v => v.Product.SKU == carePack.SKU & v.sellerChannel.Code == buyerAccount.SellerChannel.Code);
						if (SKUvariant2.Count == 0) {
							clsVariant newChanVariant = new clsVariant("", carePack, buyerAccount.SellerChannel, carePack.SKU, "", "", "", buyerAccount.SellerChannel.Region, false, null,
							-1);
							p = new clsPrice(newChanVariant, buyerAccount.Priceband, new NullablePrice(price, buyerAccount.Currency, false), "CarePackJIT");

						}

					}
				}
			}

		} catch (Exception ex) {
			ErrorLog.Add(ex);

		}
		//dont fail on unitran failure...
	}

	private void AmmendAttribute(clsProductAttribute att, clsProduct carePack, clsTranslation description, int p4, clsUnit clsUnit, clsAccount agentAccount, clsAttribute attribute, int numericValue, List<string> errorMessages)
	{

		try {
			if (att == null || att.Translation == null || att.Translation.text(agentAccount.Language) == null) {
				object productAttribute = new clsProductAttribute(carePack, attribute, numericValue, iq.i_unit_code("txt"), description);
			} else if (att.Translation.text(agentAccount.Language) != description.text(agentAccount.Language)) {
				carePack.Attributes(att.ID).delete(errorMessages);
				object productAttribute = new clsProductAttribute(carePack, attribute, numericValue, iq.i_unit_code("txt"), description);
			}
		} catch (Exception ex) {
			ErrorLog.Add(ex);
		}

	}

	//Private Sub 'LogMessage(message As String)

	//    If (Not log4net.LogManager.GetRepository().Configured) Then
	//        XmlConfigurator.Configure()
	//    End If
	//    log.Info(message)

	//End Sub

	public void DeleteAllCarePacks()
	{
		clsBranch carePackRootBranch = iq.i_SpecialBranches("cpqroot");
		List<string> errors = new List<string>();
		List<string> listOfProducts = new List<string>();
		List<string> listOfBranches = new List<string>();
		foreach ( cpk in carePackRootBranch.childBranches.Values) {
			try {
				Dictionary<string, int> counts = new Dictionary<string, int>();
				//total numbers of records by type affected
				string summary = "";
				if (cpk.HasSKU) {
					//Dim prod As clsProduct = iq.i_SKU(cpk.SKU)
					//prod.isDeleted = True
					//prod.update(errors)
					listOfProducts.Add(cpk.SKU);
				}
				//                cpk.deleted = True
				//               cpk.Update(errors)
				listOfBranches.Add(cpk.ID);
			} catch (Exception ex) {
				//LogMessage(ex.Message)

			}

		}

		string[] otherProds = (from p in iq.Products.Valueswhere p.ProductType.Code == "wty" | p.ProductType.Code == "hwsw" | p.ProductType.Code == "svc" | p.ProductType.Code == "edu"p.SKU).ToArray();

		string prods = "'" + Join(listOfProducts.ToArray(), "','") + "'";

		string branches = Join(listOfBranches.ToArray(), ",");
		if (branches != null) {
			da.DBExecutesql("update branch set deleted=1 where id in (" + branches + ")");

		}
		if (prods.Length > 3) {
			da.DBExecutesql("update Product set deleted=1 where sku in (" + prods + ")");
		}
		prods = "'" + Join(otherProds.ToArray(), "','") + "'";
		da.DBExecutesql("update Product set deleted=1 where sku in (" + prods + ")");
	}

	public void AddAllCarePacks(ulong lid)
	{
		//log = LogManager.GetLogger("IQOffline")
		//LogMessage("AddAllCarePacks : Start")
		System.DateTime startTime = Now;
		List<string> errorMessages = new List<string>();
		List<clsProduct> allProds = (from p in iq.Products.Valueswhere p.isSystem == true & p.isOption == false).ToList();
		object agentAccount = iq.seshTyped<clsAccount>(lid, "AgentAccount");
		object buyerAccount = iq.seshTyped<clsAccount>(lid, "BuyerAccount");
		if (!IsPQWSActive())
			return;
		//LogMessage("AddAllCarePacks : PQWSActive")
		int intLoop = 0;

		foreach ( sysProd in allProds) {

			try {
				//LogMessage("AddAllCarePacks : SysSKU" & sysProd.SKU)


				foreach ( systemBranch in sysProd.Branches) {
					foreach ( sysPath in systemBranch.AllPaths) {
						bool troAmended = false;
						clsScreen carePackScreen = new clsScreen();
						if (systemBranch.Product.mfrCode.ToUpper() == "HPI") {
							carePackScreen = iq.i_screens_code("optCPKDTO");
						} else {
							carePackScreen = iq.i_screens_code("optCPK");
						}
						string hwSupportPath = Left(sysPath, Len(sysPath) - Len(Split(sysPath, ".").Last) - 1);
						clsBranch hwSupportBranch = systemBranch.FindBranchByNameBelow("HW Support", hwSupportPath, true, 12, hwSupportPath);

						if (hwSupportBranch == null) {
							// Couldn't find the Hardware Support branch - create it under the Services branch
							object servicesBranchPath = string.Empty;
							object servicesBranch = systemBranch.FindBranchByNameBelow("Services", "", true, 12, servicesBranchPath);

							if (servicesBranch == null) {
								// Couldn't find the Services branch; locate via the All Options branch
								object allOptionsBranchPath = string.Empty;
								object allOptionsBranch = systemBranch.FindBranchByNameBelow("All Options", "", true, 12, allOptionsBranchPath);
								servicesBranch = new clsBranch(null, allOptionsBranch, iq.AddTranslation("Services", English, "", 0, null, 0, false), "", iq.AddTranslation("Options", English, "", 0, null, 0, false), iq.AddTranslation("Option", English, "", 0, null, 0, false), null, 40, false, "Y");

							}

							if (!servicesBranch == null) {
								hwSupportBranch = new clsBranch(null, servicesBranch, iq.AddTranslation("HW Support", English, "", 0, null, 0, false), "", iq.AddTranslation("Options", English, "", 0, null, 0, false), iq.AddTranslation("Option", English, "", 0, null, 0, false), carePackScreen, 0, false, "B");
							}

						}
						if (hwSupportBranch.Matrix == null) {
							hwSupportBranch.Matrix = carePackScreen;
							hwSupportBranch.Update(errorMessages);
						}

						object troPath = string.Empty;
						clsBranch troBranch = systemBranch.FindBranchByNameBelow("Top Recommended", sysPath, false, 12, troPath);

						if (troBranch == null) {
							// Couldn't find the Top Recommended Options branch - create it
							troBranch = new clsBranch(null, systemBranch, iq.AddTranslation("Top Recommended", English, "", 0, null, 0, false), "", iq.AddTranslation("Top Recommended", English, "", 0, null, 0, false), iq.AddTranslation("Top Recommended", English, "", 0, null, 0, false), null, 0, false, "H");
							troPath = sysPath + "." + troBranch.ID;

						}
						clsBranch troCpqBranch = troBranch.FindBranchByNameBelow("Care Pack", troPath, false, 12, troPath);

						if (troCpqBranch == null) {
							// Couldn't find the Top Recommended Options/Care Pack branch - create it
							troCpqBranch = new clsBranch(null, troBranch, iq.AddTranslation("Care Pack", English, "", 0, null, 0, false), "/images/product/category/cat2.png", iq.AddTranslation("Top Recommended", English, "", 0, null, 0, false), iq.AddTranslation("Top Recommended", English, "", 0, null, 0, false), null, 0, false, "I");
							troPath = troPath + "." + troCpqBranch.ID;
						}
						clsVariant skuVariant;
						string autoAddCreatedPath = null;
						RefreshPQWSCarePacks(systemBranch.Product, sysPath, hwSupportBranch, troCpqBranch, agentAccount, autoAddCreatedPath, troAmended, lid, hwSupportPath, systemBranch,
						troPath, errorMessages);
						if (iq.CarePackLastRefresh.ContainsKey(sysProd.SKU)) {
							iq.CarePackLastRefresh(sysProd.SKU) = DateTime.Now;
						} else {
							iq.CarePackLastRefresh.Add(sysProd.SKU, DateTime.Now);
						}
					}
					//sysPath
				}
				//systemBranch
				intLoop = intLoop + 1;


			} catch (Exception ex) {
			}
		}
		//sysProd
		//LogMessage("AddAllCarePacks : Total Carepacks added " & intLoop)
		TimeSpan ti = Now - startTime;
		//LogMessage("AddAllCarePacks : Total Carepacks added " & ti.ToString("d : hh :mm:ss"))
		//Dim partPAth = ""
		//Dim part = systemBranch.findChildBySKU2(Path, rdr("addsku"), partPAth)
		//If part IsNot Nothing Then
		//    If part.Quantities.Values.Where(Function(q) (String.IsNullOrEmpty(q.Path) OrElse q.Path = partPAth) AndAlso q.NumPreInstalled > 0 AndAlso q.Region.Encompasses(iq.i_region_code(rdr("CountryCode").replace("UK", "GB"))) AndAlso q.FOC = False).Count = 0 Then
		//        Dim q = New clsQuantity(iq.i_region_code(rdr("countrycode").replace("UK", "GB")), partPAth, part, 1, 0, 0, False, Nothing)
		//    End If
		//End If


	}

	public void AddUnknownServiceLevel(List<string> serviceLevelList, string mfrCode)
	{
		string conString = ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString;
		foreach ( newLevel in serviceLevelList) {
			int serviceLevel = Convert.ToInt32(newLevel);
			using (SqlConnection con = new SqlConnection(conString)) {
				con.Open();
				using (SqlCommand command = new SqlCommand()) {
					command.CommandText = "sp_AddNewPQWSServiceLevel";
					command.CommandType = CommandType.StoredProcedure;
					command.Connection = con;
					command.Parameters.Add(new SqlParameter("@serviceLevel", serviceLevel));
					command.Parameters.Add(new SqlParameter("@mfrCode", mfrCode));
					command.ExecuteNonQuery();
				}
			}
		}
	}
}


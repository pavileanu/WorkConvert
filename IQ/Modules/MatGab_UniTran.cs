using System.Collections.Concurrent;

class ModUniTran
{

	public class clsQueuedRequest
	{
		public int RequestID;
		//  Public header As uniTran.clsStockPriceHeader 'gives us access to the request and response timestamps (for housekeeping)
		public clsAccount BuyerAccount;
			//dicSKUs As Dictionary(Of String, ClsProductVariant) 'distiSKU > our Product/Variant
		public List<clsVariant> skuVariants;

		public int tryCount = 0;
		//Dictionary(Of String, ClsProductVariant))
		public clsQueuedRequest(int RequestID, clsAccount BuyerAccount, List<clsVariant> skuVariants)
		{
			this.RequestID = RequestID;
			this.BuyerAccount = BuyerAccount;
			//    Me.header = header
			this.skuVariants = skuVariants;
		}

	}

		//declared at a module level so available to all users - holds the request handle, and a dictionary of the DistiSKUs>ProductVariants - used when handing the responses in getPriceUIs.aspx to update the correct prices
	public ConcurrentDictionary<int, clsQueuedRequest> PendingRequests;



	public void EmbedUpdateRequest(clsBranchInfo pbi, Dictionary<clsBranch, clsVisibility> descendantPaths, Panel childrenPanel, ref List<string> errormessages)
	{
		//Fires off the update request to get stock and price for the Products in descendantpaths
		//add an image in the specified panel with attached script to call the fillprices after 5 seconds (when the Webservice call may be complete)

		List<clsVariant> NeedUpdate;
		//Dictionary(Of String, ClsProductVariant)
		NeedUpdate = new List<clsVariant>();
		//Dictionary(Of String, ClsProductVariant)

		//needupdate will cointain only out of date, visible products - often none !
		//note - the keys are Branches (the values are paths)
		foreach ( vs in descendantPaths.Values) {
			//only fetch prices for things with SKU (ie. NOT for placeholder branches !)
			if (vs.branch.HasSKU) {
				if (vs.hideReasonList.Count == 0) {
					//AppendDic(NeedUpdate, b.StalePrices(buyeraccount)) 
					//appends  ProductVariant (containing the distis sku)
					// for the stale prices for every variant of every descendant branch's product
					NeedUpdate.AddRange(vs.branch.StalePrices(pbi.buyerAccount, errormessages));
				} else {
					// Beep()
					int w = 1;
				}
			}
		}

		//If it's a keyword search, the parent (ie. the branch we clicked on in the keyword results) could be out of date.. (for a 'normal' branch opening - it should have already been update by opening it's parent)
		//IMPORTANT also ques updates for preinstalled items on any server we open 

		//Moved into quote.aspx as Gregs requirment to *not* show children when you open a system totally broke it
		//NeedUpdate.AddRange(additionalUpdates(pbi.Branch, pbi.BuyerAccount, pbi.path$))


		//Bitwise check of the webservice flag
		if ((pbi.buyerAccount.SellerChannel.priceConfig & 8) == 0) {
			int breakpoint = 0;
		}

		Debug.Print(NeedUpdate.Count + " need updating ");
		if (NeedUpdate.Count > 0) {
			int handle;
			//This issues a request to the Universal Translating webservice - and (instantly) returns a handle 
			handle = ModUniTran.DispatchUpdateRequest(pbi.lid, NeedUpdate, "", errormessages);

			//pbi.path$ - tree.1 was pbi.path - but there's no real reason (apart from perhaps swift) not to placeprices across the whole tree
			if (handle == 0) {
				errormessages.Add("Could not dispatch web request (handle was 0) - No skus needed updating ?");
			} else {
				//inserts an image with an onload script which calls the js FillPrices() after 5 seconds
				childrenPanel.Controls.Add(fetcherImage("tree.1", handle, NeedUpdate));
			}
		}

	}

	/// <summary>
	/// Calls the wsconsumer to get update/create the list of variants for the specified host
	/// </summary>
	/// <param name="hostid">Channel.code</param>
	/// <param name="warehouse"></param>
	/// <remarks></remarks>
	public string GetVariants(string hostID, string warehouse, ref List<string> errorMessages)
	{



		//Dim channel As clsChannel
		//If Not iq.i_channel_code.ContainsKey(hostID) Then
		//    Return "Not a valid Host ID (code) - CaSe SeNsitive"
		//Else

		//    channel = iq.i_channel_code(hostID)
		//    If Not channel.variantsLoaded Then channel.LoadVariants(errorMessages)

		//    'Fetch a 'Mini Feed' from the WSConsumer (for this host)
		//    Dim cl As wsconsumer.I_UniTranClient = New wsconsumer.I_UniTranClient
		//    Dim minifeed As List(Of String) 'returns a pipe sperated list of Host|mfr skus|wh

		//    minifeed = cl.AllProducts(hostID, warehouse).ToList

		//    Dim chn As clsChannel = iq.i_channel_code(hostID)

		//    Dim same, added, missing, bad, undeleted As Integer
		//    Dim aVariant As clsVariant

		//    'Compound key of warehouse|distiSKU >MfrPartnum
		//    Dim Feed As Dictionary(Of String, String) = New Dictionary(Of String, String)

		//    Dim bits() As String
		//    For Each line In minifeed  'lines  in the minifeed are MPN|DistiSKu
		//        bits = Split(line, "|")
		//        Dim wh As String = ""
		//        If bits.Count = 3 Then wh = bits(2) 'third element (zero based)
		//        Feed.Add(wh & "|" & bits(1), bits(0))  'WH|HPN>MPN
		//        If bits(0) = "" Or bits(1) = "" Then Stop
		//    Next

		//    'check what's in the Feed - against the varaints that exisit in the OM at the moment
		//    For Each CK In Feed.Keys  ' WH|DistiSku>MfrPartNum
		//        bits = Split(CK, "|")   'the current keys are Warehouse|DistiSKU

		//        ' first dimension is warehouse - 2n'd is DistiSKU (bear in mind there can be more than onve variant (with the same HP part numbers but different HostPN's)
		//        ' chn.i_variant_distisku.Add(bits(0), bits(1))

		//        Dim addit As Boolean = True
		//        Dim fwh As String = bits(0) 'warehouse
		//        Dim FdistiSKU As String = Trim(bits(1)) 'Distisku

		//        If chn.i_variant_distisku.ContainsKey(fwh) Then 'warehouse
		//            If chn.i_variant_distisku(fwh).ContainsKey(FdistiSKU) Then

		//                Dim eSKUvariant As clsVariant = chn.i_variant_distisku(fwh)(FdistiSKU)
		//                'Yes, got that already
		//                If eSKUvariant.Deleted Then
		//                    eSKUvariant.Deleted = False
		//                    undeleted += 1
		//                    eSKUvariant.Update()
		//                Else
		//                    same += 1
		//                    addit = False
		//                End If

		//            End If
		//        End If

		//        If addit Then
		//            'we didnt have a variant (in this warehouse)  with this DistiSKU
		//            Dim mfrpartnum As String = Feed(CK) 'Split(bits(1), "#")(0) 'take of any #ABU type 'variant'
		//            Dim product As clsProduct
		//            If iq.i_SKU.ContainsKey(mfrpartnum) Then
		//                product = iq.i_SKU(mfrpartnum)
		//                aVariant = New clsVariant("wsg", product, chn, FdistiSKU, "", "", "", r_worldwide, 0)
		//                added += 1
		//            Else
		//                bad += 1
		//            End If
		//        End If
		//    Next

		//    'check every variant we have..
		//    'Remove those no longer in this 'feed'

		//    Dim skuVariant As clsVariant
		//    For Each warehouse In chn.i_variant_distisku.Keys.ToArray
		//        For Each distisku In chn.i_variant_distisku(warehouse).Keys.ToArray
		//            If Not Feed.ContainsKey(warehouse & "|" & distisku) Then
		//                skuVariant = chn.i_variant_distisku(warehouse)(distisku)
		//                If Not skuVariant.Deleted Then
		//                    skuVariant.Delete() 'we actively remove those Skus no longer there
		//                    missing += 1
		//                End If
		//            End If
		//        Next
		//    Next

		//    GetVariants = "same:" & same & " Added:" & added & " missing from minifeed (deleted):" & missing & " bad:" & bad & " undeleted:" & undeleted

		//End If


	}


	/// <summary>
	/// Dispatches a request for updated stock and pricing via the UniTran pricing proxy webservice
	/// </summary>
	/// <param name="needingUpdate"></param>
	/// <param name="authkey"></param>
	/// <returns></returns>
	/// <remarks>Creates an instance of the UniTran webservice client - and issues a request for updated stock and pricing for the set of product-variants in 'needingUpdate' Adds that instance, and the request handle to the public PendingRequests dictionary</remarks>
	/// 
	public int DispatchUpdateRequest(UInt64 lid, List<clsVariant> needingUpdate, string authkey, ref List<string> errorMessages)
	{

		clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");

		int quoteid = iq.sesh(lid, "QuoteID");
		long ts = Stopwatch.GetTimestamp;
		string wsCall = null;

		//Pmark("WS_DispatchRequest")

		try {
			//Create an instance of the UnitTran (Univeral Translator) webservice
			wsconsumer.I_UniTranClient Requester = new wsconsumer.I_UniTranClient();

			int handle;
			//each request gets a handle (also appears in the header)
			//Dim request As uniTran.clsStockPriceRequest

			//      If buyeraccount.priceBand = "" Then Stop
			if (buyerAccount.SellerChannel.priceConfig & 8 == 0)
				System.Diagnostics.Debugger.Break();

			//Prepare a request 
			[] skus = (from i in needingUpdatei.DistiSku).ToArray;
			//LINQ
			if (skus.Count == 0) {
				return 0;

			} else {
				Requester.ClientCredentials.Windows.ClientCredential.Password = "iQuoteEXPERT";
				Requester.ClientCredentials.Windows.ClientCredential.UserName = "DSVR016766\\Nick.axworthy";

				//If Trim(buyerAccount.priceBand) = "" Then Beep() : buyerAccount.priceBand = "CHA097"

				//REMOVE
				if (buyerAccount.SellerChannel.IsCloneOf.Code == "DWERG74AH" & buyerAccount.Priceband.text == "")
					buyerAccount.Priceband = iq.getPriceBand("CHA097");
				//If buyerAccount.SellerChannel.Code = "DCORG248NE" And buyerAccount.priceBand = "" Then buyerAccount.priceBand = "325009"
				//If buyerAccount.SellerChannel.Code = "DAZRG248NE" And buyerAccount.priceBand = "" Then buyerAccount.priceBand = "325009"

				if (needingUpdate.Count > 100) {
					errorMessages.Add("* Warning: more than 100 parts were requested");
				}

				wsconsumer.clsStockPriceRequest request;
				string qid = "";
				clsQuote quote;
				if (quoteid != 0) {
					if (iq.Quotes.ContainsKey(quoteid)) {
						quote = iq.Quotes(quoteid);
						qid = quote.RootItem.ID + "-" + quote.Version + "(" + quote.ID + ")";
					}
				}

				string WSRQKey = buyerAccount.Priceband.text;
				if (iq.SeshContains(lid, "gk_SessionID")) {
					WSRQKey += ";" + iq.sesh(lid, "gk_sessionID");
				}
				//Requester.Endpoint.Binding.OpenTimeout = TimeSpan.FromSeconds(1)
				//Requester.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(1)
				//Requester.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromSeconds(1)
				//Requester.Endpoint.Binding.CloseTimeout = TimeSpan.FromSeconds(1)

				wsCall = "BuildRequest";
				request = Requester.BuildRequest(buyerAccount.SellerChannel.IsCloneOf.Code, buyerAccount.Priceband.text, (string)agentAccount.User.ID, (string)lid, agentAccount.Currency.Code, "", WSRQKey, skus, qid, agentAccount.User.Email,
				"iQuote2");
				wsCall = null;

				//catch for a failed build
				if (request.skus.Count == 0)
					return 0;

				//handle = Requester.RequetStockPrices("", buyeraccount.SellerChannel.Code, buyeraccount.priceBand, buyeraccount.Currency.Code, "", skus)
				//Requester.Endpoint.Binding.OpenTimeout = TimeSpan.FromSeconds(1)
				//Requester.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(2)
				//Requester.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromSeconds(5)
				//Requester.Endpoint.Binding.CloseTimeout = TimeSpan.FromSeconds(1)

				wsCall = "RequestStockPrices";
				handle = Requester.RequestStockPrices(request);
				wsCall = null;

				Requester.Close();

				if (PendingRequests == null) {
					PendingRequests = new ConcurrentDictionary<int, clsQueuedRequest>();
				}

				if (handle == 0) {
					errorMessages.Add("* Requester.requestStockprices returned a Handle of 0");
				} else {
					//error
					if (handle != -1) {

						//store the request info - including the dictionary we will use to look up the product variants from the HostsSKUs
						if (PendingRequests.TryAdd(handle, new clsQueuedRequest(handle, buyerAccount, needingUpdate))) {
							string ms = TimeSince(ts);
							if (Val(ms) > 100) {
								errorMessages.Add("* Request for " + needingUpdate.Count.ToString + "parts dispatch took " + ms);
							}
						} else {
							ErrorLog.Add(new Exception("DispatchUpdateRequest - Pending requests already contained the handle " + handle + " (IQ2 Restarted ?)"));
							return -1;
						}

					}
				}

				return handle;
			}


		} catch (Exception ex) {
			ErrorLog.Add(ex);
			errorMessages.Add("*" + ex.Message + !string.IsNullOrEmpty(wsCall) ? " - " + wsCall : string.Empty);
			if (ex.InnerException != null) {
				errorMessages.Add("*" + ex.InnerException.Message);
			}

			return 0;
			//Finally
			Pacc("WS_DispatchUpdateRequest");
		}


	}




}

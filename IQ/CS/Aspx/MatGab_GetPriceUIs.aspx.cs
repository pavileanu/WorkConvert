public class GetPriceUIs : System.Web.UI.Page
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		//This page is called (from js fillprices() - to refresh the price AND STOCK labels after a webservice call is made
		//the sequence of events is.. a branch is opened, the list of products whose prices are out of date is compiled
		//A call is made to the 'UniTran' (universal translation) web service in DispatchUpdateRequest() to unitran.RequestStockPrices() - which will return a Handle (batch id) (and place the request in a global dictionary PendingRequests)
		//an image is rendered into the page with an onLoad script - which has a setTimeout - to execute the javascript fillPrices(path) after 5 seconds.
		//the JS fillPrices() assembles a list of the IDs of DIVs beneath the open branch (path) that contain stock and price UI to be refreshed .. there may be several becuase the user may have opened one or more branches during the 5 seconds
		//rExec calls this page (getPriceUIs.aspx), and upon completion calls back the javascript function PlacePrices()
		//which replaces the DIVs with revised content (which may or may not include the update - which may or may not have arrived yet)
		//if updates are still pending another JS timeout() is set

		//request("divIDs") contains a comma delimited list of the div IDs to refresh which are of the form  P or S (price or stock) _branchID_priceID


		List<string> errorMessages = new List<string>();

		UInt64 lid = (UInt64)Request.QueryString("lid");

		string divIDs = Request("divIDs");

		object ru = Request.RawUrl;
		Literal lit = new Literal();

		if (!IsNumeric(Request("handle"))) {
			errorMessages.Add("* GetpriceUIs invalid handle was " + Request("handle").ToString);
			OutputErrors(this.Controls, errorMessages, lid);
			// these just go to the audit log now
			lit.Text = "]^DONE";

		} else {
			int handle = Request("handle");
			wsconsumer.clsStockPriceResponse response = null;


			if (handle != -1) {
				//make a very fast call to the UniTran webservice to fetch the current status/results for the handle
				//it will return instantly  - with or without result lines
				response = callWsConsumer(handle, errorMessages);
				//update the OM with any completed lines in the response

				OutputErrors(this.Controls, errorMessages, lid);
				// these just go to the audit log now

				//The webservice call can fail 
				if (response != null) {
					updateStockPriceFromResponse(handle, response, errorMessages);
					outputUpdatedPriceUIs(lid, handle, response, divIDs, errorMessages);


					if (PendingRequests.ContainsKey(handle)) {
						if ((response.completed) | PendingRequests(handle).tryCount == 5) {
							if (PendingRequests != null) {
								clsQueuedRequest removed = null;
								PendingRequests.TryRemove(handle, removed);
							}
							lit.Text = "]DONE^";
							//we have completed the fetches for all ID (they are all less than 5 minutes old)

						} else {
							PendingRequests(handle).tryCount += 1;
							//if any of the prices are still pending.. set another timeout
							lit.Text = "]" + Request("Path") + "^" + handle + "^";
							//Some prices are still old - we add the path so a new setTimeout() can be created in the JS PlacePrices()
						}
					}

				}
			}
		}

		Form.Controls.Add(lit);

	}

	private void outputUpdatedPriceUIs(UInt64 lid, int handle, wsconsumer.clsStockPriceResponse response, string divIds, ref List<string> errormessages)
	{


		if (handle != -1) {
			clsAccount buyeraccount;
			clsQueuedRequest queuedRequest = null;


			if (PendingRequests.TryGetValue(handle, queuedRequest)) {
				buyeraccount = queuedRequest.BuyerAccount;
				Literal lit;
				//yey we have results
				if (response.items.Count) {

					//it's possibe we closed the DIV (whilst the request was pending)
					if (divIds != "") {
						[] b;

						Panel pnl;
						//each DIV id is of the form P_priceID (or S_Stockid)
						foreach (string ID in divIds.Split(",")) {

							if (ID != "") {
								lit = new Literal();
								lit.Text = "]" + ID + "^";
								//This ASPX outputs ]DivID^replacamentContent  - which is merged back into the poage by the JS placePrices()
								Form.Controls.Add(lit);

								b = Split(ID, "_");
								if (b(0) == "P") {
									//If b(1) <> "-1" Then  'todo - remove (to do with POA's and temporary variants  see getprices)
									//    minutesOld = 0 ' UI will RETURN the age of the price
									//pnl = iq.Products(b(1)).prices(b(2)).Ui 'Should go green
									if (buyeraccount != null) {
										if (!iq.Prices.ContainsKey(b(1))) {
											int jjj = 99;


										} else {

											object price = iq.Prices(b(1));
											price.lastUpdated = Now;
											pnl = price.Ui(buyeraccount, 1, lid);
											//Should go green


											//prices that have been created by late inserts - need their temporary references cleaned up
											if (price.tempID < 0) {
												if (iq.Prices.ContainsKey(price.tempID)) {
													iq.Prices.Remove(price.tempID);
													price.tempID = 0;
												} else {
													int kkk = 0;
												}
											}
										}

									} else {
										errormessages.Add("* BuyerAccount was nothing in getPriceUIs");
										pnl = new Panel();
									}


									//minutesOld = iq.Prices(b(1)).MinutesOld
									//If minutesOld > 5 Then
									// allDone = False
									// End If

									Form.Controls.Add(pnl);
								// Else
								//     Beep()
								// Else
								//     Beep()
								// End If

								} else if (b(0) == "S") {
									// the placeholder contains a panels (div) - which holds the stock UI  
									PlaceHolder ph = iq.Stock(b(1)).SKUvariant.StockUI(1, string.Empty, buyeraccount.Language, buyeraccount.SellerChannel);
									Form.Controls.Add(ph);
								} else {
									Beep();
								}
							}
						}
					}
				}
			} else {
				// PendingRequests doesn't contain the handle - should never happen
				errormessages.Add(string.Format("* PendingRequests didn't contain expected handle {0}", handle));
			}
		}

	}


	private void updateStockPriceFromResponse(int handle, wsconsumer.clsStockPriceResponse response, ref List<string> errormessages)
	{
		clsAccount buyeraccount;

		if (PendingRequests == null) {
			errormessages.Add("* Pending requests was nothing");
		} else {
			if (!PendingRequests.ContainsKey(handle)) {
				errormessages.Add("* PendingRequests did not contain the handle:" + handle);
			} else {

				foreach ( r in response.items) {

					try {
						IEnumerable<clsVariant> vs = (from rq in PendingRequests(handle).skuVariantswhere rq.DistiSku == r.SKU);
						if (vs.Any) {
							clsVariant v = vs.First;
							buyeraccount = PendingRequests(handle).BuyerAccount;
							updatePriceStock(buyeraccount, v, r);
							if (vs.Count > 1)
								errormessages.Add("* There were " + vs.Count + " rows returend for " + v.DistiSku + " expected 1");

						} else {
							errormessages.Add("* The request contained no SKU:" + r.SKU);
						}
					} catch {
						errormessages.Add("threading/multiple collection problem in updateStockpPiceFromResponse");
					}

				}
			}
		}



	}


	private wsconsumer.clsStockPriceResponse callWsConsumer(int handle, ref List<string> errorMessages)
	{

		wsconsumer.I_UniTranClient requester;
		requester = new wsconsumer.I_UniTranClient();

		requester.ClientCredentials.Windows.ClientCredential.Password = "iQuoteEXPERT";
		requester.ClientCredentials.Windows.ClientCredential.UserName = "DSVR016766\\Nick.axworthy";

		wsconsumer.clsStockPriceResponse response;
		try {
			//requester.Endpoint.Binding.OpenTimeout = TimeSpan.FromSeconds(1)
			//requester.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(1)
			//requester.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromSeconds(5)

			response = requester.CheckStockPrices(handle, false, 0);
			//Parameters : handle, isSyncronus, Timeout. Timeout valid for if isSyncronous set to true. This call is very fast - it returns the results sofar for the specified handle  (which may be an empty list).. and status   - TODO probably want to handle errors gracefully
			requester.Close();
			//new
		} catch (System.Exception ex) {
			errorMessages.Add("*" + ex.Message);
			response = null;
		}

		return response;

	}


	public void updatePriceStock(clsAccount buyeraccount, clsVariant v, wsconsumer.clsStockPriceItem item)
	{
		//Each Variant (product) is Seller-specific (but not Buyer specific) 
		bool found = false;
		//each batch is a dictionary of HostPartnum > ProductVariant (a product-SKUvariant pair)
		//Each product has a dictioanry of sellers>variants>(arrival)dates>ClsStocks  - (containing a quantity, datestamp etc)





			// If item.SKU.Contains("3") Then Stop
			//Important ! (otherwise POA's remain 'invalid' event though they now have a value
			//In case it was a (temporary) list price - (it is'nt now!)


			//do the INSERT

			//Expensive
			//should never happen ! - the webservice created a POA price in advance


			//Recurses through every item in the quote - updating them IF they have this price
		 // ERROR: Not supported in C#: WithStatement


		if (v.shipments == null) {
			clsstock newstockrecord__1 = new clsstock(v, item.stock(0).quantity, item.stock(0).arrival, "New stock record (created by UpdatePriceStock()", true);
		}

		//dates on which shipments of variants arrive

		foreach (wsconsumer.clsShipment shipment in item.stock) {
			//update an exisiting shipment
			if (v.shipments.ContainsKey(shipment.arrival)) {
				 // ERROR: Not supported in C#: WithStatement

			} else {
				//make a new stock record for this shipment (will INSERT to the database and Update product.i_Stock
				//' This wasn't such a good idea - issues with the shipments ID changing when archived -  If shipment.arrival.Date = CDate("01/01/2000").Date Then v.ArchiveCurrentStock() 'removes it from the product.i_stock AND sets the archived flag

				//in this instance, there was no stock record - so there is no stockUI to replace - so the stock doesnt show... we need to refesh the whole branch instead

				object newStockRecord__2 = new clsstock(v, shipment.quantity, shipment.arrival, "WS", shipment.isCurrent);
			}
		}

	}

}

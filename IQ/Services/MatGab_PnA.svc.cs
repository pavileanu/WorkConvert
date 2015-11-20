
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Runtime.Serialization;
using dataAccess;

//Namespace iQuote  'a namespace is a bit like a stub class.. under which we can define a set classes implementing and exposing methods

[ServiceContract()]
public interface i_PnA
{

	[OperationContract()]
	clsStockPriceSvc.clsResult[] SetStock(string SessionID, clsStockPriceSvc.clsStockItem[] items);
	[OperationContract()]
	clsStockPriceSvc.clsResult[] SetPrices(string SessionID, string Currency, clsStockPriceSvc.clsPrice[] prices);
	[OperationContract()]
	clsStockPriceSvc.clsResult[] SetMargins(string SessionID, clsStockPriceSvc.clsMargin[] margins);
	[OperationContract()]
	clsStockPriceSvc.clsResult[] SetVariants(string SessionID, clsStockPriceSvc.clsVariant[] variants);
	[OperationContract()]
	string DeleteVariants(string SessionID);
	[OperationContract()]
	HashSet<string> SKUS(bool systems, bool options);

	//techdata (and others) will requires somethgin along these lines
	//the productinfo class might contain 'Recognised' - regionalisation info, ListPrice info (in currencies), HP descriptions - etc.
	//<OperationContract()>
	//Function ProductInfo(SessionID As String, skus As List(Of String)) As List(Of clsStockPriceService.clsProductInfo)

}

//<AspNetCompatibilityRequirements(RequirementsMode:=AspNetCompatibilityRequirementsMode.Allowed)>

// IQ.clsStockPriceSvc is not attributed with ServiceContractAttribute
//<ServiceContract()>

public class clsStockPriceSvc : i_PnA
{

	//The clsPrice is used (in combination with the hosts Token (which yields their ID) to locate this right variant

	[DataContractAttribute()]
	private class clsPrice
	{

		[DataMemberAttribute()]
			//HP part number (strictly this is redundant but it helps us look up the variant much quciker)
		public string MfrSKU;
		[DataMemberAttribute()]
			//a unique identifier for each distinct variant of the product (most likely thier internal part nmber) - or a compound key of Warehouse/PartnNo
		public string HostSKU;
		[DataMemberAttribute()]
			//The currency and buyer channel are  specified in the setPrices Method
		public decimal Price;
		[DataMemberAttribute()]
			//
		public string Warehouse;
		[DataMemberAttribute()]
			//
		public string PriceBand;

	}

	[DataContractAttribute()]
	private class clsVariant
	{
		[DataMemberAttribute()]
		public string MfrSKU;
		[DataMemberAttribute()]
		public string HostSKU;
		[DataMemberAttribute()]
		public string WareHouse;
		[DataMemberAttribute()]
			//an overriding/variant specific description
		public string DisplayText;

	}

	private class clsMargin
	{

		public string AccountNum;
		public string ProductType;

		public float Margin;
	}

	private class clsstock
	{
		public int Quantity;
		public System.DateTime Arrival;
		public bool isCurrent;
	}

	private class clsStockItem
	{

		public string MfrSKU;
		public string HostSKU;
			//'a unique identifier for each distinct variant of the product (most likely thier internal part nmber) - or a compound key of Warehouse/PartnNo
		public string Warehouse;

		public clsstock[] Shipments;
	}

	[DataContractAttribute()]
	private class clsResult
	{
		[DataMemberAttribute()]
		public bool Success;
		[DataMemberAttribute()]
		public string Message;
		[DataMemberAttribute()]

		public int ErrorCode;
		public clsResult(bool Success, string Message, int errorCode)
		{
			this.Success = Success;
			this.Message = Message;
			this.ErrorCode = errorCode;
		}

	}

	public clsChannel BuyerChannelFrompriceBand(clsPriceBand priceBand)
	{

		object j = from a in iq.Accounts.Valueswhere object.ReferenceEquals(a.Priceband, priceBand);
		if (j.Any)
			return j.First.BuyerChannel;
		else
			return null;

	}

	public clsChannel channelFromToken(string WebToken)
	{

		object j = from c in iq.Channels.Valueswhere UCase(c.WebToken) == UCase(WebToken) | UCase(c.Code) == UCase(WebToken);
		//todo REMOve THE CHECK ON cHANNEL.CODE AND WORK OFF (SECRET) TOKENS

		if (j.Any) {
			return j.First;
		} else {
			return null;
		}

	}

	public clsResult[] i_PnA.SetMargins(string webtoken, clsStockPriceSvc.clsMargin[] Margins)
	{

		clsStockPriceSvc.clsResult[] results;

		if (iq.PNAdown) {
			 // ERROR: Not supported in C#: ReDimStatement

			results(0) = new clsResult(false, "The service is temporarily unavailable (typically only for a few seconds) - please try again", 1);
			return results;
			//<<<<<<<<<< EXIT
		}

		 // ERROR: Not supported in C#: ReDimStatement

		return results;

	}
	public string i_PnA.DeleteVariants(string webtoken)
	{

		//delete all variants (that are not referenced by a quoteitem)

		object rt = "";
		//result text


		try {
			SqlClient.SqlConnection con = da.OpenDatabase();

			clsChannel seller;
			seller = channelFromToken(webtoken);



			if (seller == null){rt = webtoken + " is not valid (no seller channel could be located)";return rt;}

			iq.PNAdown = true;

			object sql = "SELECT id FROM variant WHERE fk_channel_id_seller=" + seller.ID;
			List<string> vlist = new List<string>();
			SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql);
			while (rdr.Read) {
				vlist.Add(rdr.Item("ID").ToString.Trim);
			}
			rdr.Close();

			if (vlist.Count == 0) {
				rt = " There are no variants for " + seller.ID + " (" + webtoken + ")";

			} else {
				rt += vlist.Count + " Variants " + vbCrLf;

				sql = "DELETE FROM PRICE where fk_variant_id in (" + Join(vlist.ToArray, ",") + ")";
				rt += "Deleted " + LongSQL(sql) + " prices " + vbCrLf;

				sql = "DELETE FROM Stock where fk_variant_id in (Select id from variant where fk_channel_id_seller=" + seller.ID + ")";
				rt += "Deleted " + LongSQL(sql) + " stock " + vbCrLf;

				foreach ( s in iq.Stock.Values.ToArray) {
					if (vlist.Contains(s.SKUvariant.ID.ToString.Trim)) {
						iq.Stock.Remove(s.ID);
					}
				}

				//prices live 'in' the products - actually variants live in the products - prices  line under the variants

				//For Each product In iq.Products.Values
				//    For Each ba In product.

				//    Next
				//Next

				//These are the variants referenced by quoteitems (we will not be able to delete)
				sql = "SELECT v.id as ID FROM quote q ";
				sql += "JOIN quoteitem qi ON qi.fk_quote_id=q.id ";
				sql += "JOIN variant v ON qi.fk_variant_id=v.id ";
				sql += "JOIN account buyerAccount on buyeraccount.id=q.fk_account_id_buyer ";
				sql += "WHERE buyeraccount.fk_channel_id_seller = " + seller.ID;

				rdr = da.DBExecuteReader(con, sql);
				int refed = 0;
				while (rdr.Read) {
					vlist.Remove((string)rdr.Item("id"));
					refed += 1;
				}
				rdr.Close();
				con.Close();

				rt += refed + " variants are referenced by quote items and cannot be (physically) deleted" + vbCrLf;

				int removed = 0;
				foreach ( v in iq.Variants.Values.ToArray) {
					if (vlist.Contains(v.ID.ToString.Trim)) {
						iq.Variants.Remove(v.ID);
						removed += 1;
					}
				}
				rt += removed + " variants were removed from the OM (iq.variants)." + vbCrLf;

				removed = 0;
				int sremoved = 0;
				//remove the variants (holding the prices) from the products
				foreach ( p in iq.Products.Values.ToArray) {
					if (p.i_Variants.ContainsKey(seller)) {
						p.i_Variants.Remove(seller);
						//each product has an index of variants by seller channel - we need to remove those references - Public i_Variants As Dictionary(Of clsChannel, List(Of clsVariant))
						sremoved += 1;
					}

					foreach ( v in p.Variants.Values.ToArray) {
						if (vlist.Contains(v.ID.ToString.Trim)) {
							if (!p.Variants.ContainsKey(v.ID))
								System.Diagnostics.Debugger.Break();

							p.Variants.Remove(v.ID);
							removed += 1;
						}
					}
				}

				rt += removed + " variants were removed from products.variants" + vbCrLf;
				rt += sremoved + " products had the seller (variant set) removed" + vbCrLf;


				//Break into chunks of 1000 or the deletions time out
				List<string> chunk = new List<string>();
				foreach ( vid in vlist) {
					chunk.Add(vid);
					if (chunk.Count == 200 | vid == vlist.Last) {
						sql = "DELETE FROM VARIANT where id in (" + Join(chunk.ToArray, ",") + ")";
						rt += LongSQL(sql) + " variants DELETED from the database";
						chunk = new List<string>();
					}
				}
			}

			return rt;


		} catch (Exception ex) {
			return "An error coccured " + ex.Message + vbCrLf + rt;
			ErrorLog.Add(ex);


		} finally {
			iq.PNAdown = false;

		}

	}


	public clsStockPriceSvc.clsResult[] i_PnA.SetVariants(string webtoken, clsStockPriceSvc.clsVariant[] variants)
	{

		//Called by the feedReaded to push variants into IQ2 - should be obsoleted by the JIT call to AllProducts

		//For each HP sku a disti can have multiple variants 
		//prices (and stock) then apply to these variants (for specific customers)


		try {
			clsStockPriceSvc.clsResult[] Results = new clsStockPriceSvc.clsResult[variants.Count - 2];

			if (iq == null) {
				 // ERROR: Not supported in C#: ReDimStatement

				Results(0) = new clsResult(false, "The object model is not currently loaded - please try later", 1);
				return Results;
				//<<<<<<<<<< EXIT
			}

			if (iq.PNAdown) {
				 // ERROR: Not supported in C#: ReDimStatement

				Results(0) = new clsResult(false, "The service is temporarily unavailable (typically only for a few seconds) - please try again", 1);
				return Results;
				//<<<<<<<<<< EXIT
			}

			clsChannel sellerChannel;
			sellerChannel = channelFromToken(webtoken);

			if (sellerChannel == null) {
				 // ERROR: Not supported in C#: ReDimStatement

				Results(0) = new clsResult(false, "Invalid or unrecognised Security Token '" + webtoken + "' your webservice security token is a lowercase 32 Digit, hyphenated, Hexadecimal GUID, supplied by ChannelCentral.net - Contact support@channelcentral.net for assistance", 1);
				return Results;
				//<<<<<<<<<< EXIT
			}

			List<string> errormessages = new List<string>();
			if (sellerChannel.variantsLoadedAt == null) {
				sellerChannel.LoadVariants(errormessages, 0.1);
				//generally this will mean there is 
			}

			IQ.clsVariant skuvariant = null;
			clsResult result = null;

			int i = 0;

			//need a locking mechanism - to make these robust
			iq.PNAdown = true;

			SqlClient.SqlConnection con = da.OpenDatabase();


			//find the next available variant ID (so we can bulk write)

			int nextid;
			DataTable wc = da.MakeWriteCacheFor(con, "variant", nextid, true);
			con.Close();

			// Dim nextID As Integer = (From j In iq.Variants.Values Select j.ID).Max + 1  'LINQ

			 // ERROR: Not supported in C#: ReDimStatement


			foreach ( v in variants) {
				//If v.HostSKU.Contains("#") Then Stop

				if (v.DisplayText == null)
					v.DisplayText = "";

				object bpn = Split(v.MfrSKU, "#")(0);
				if (v.MfrSKU == "") {
					Results(i) = new clsResult(false, "Empty manufacturer SKU for :" + v.HostSKU, 21);
				//Check the part before any hash present
				} else if (!iq.i_SKU.ContainsKey(bpn)) {
					Results(i) = new clsResult(false, "Unrecognised manufacturer part number (in Heirarchy - but not in iQuote) " + v.MfrSKU, 66);

				} else {
					if (!sellerChannel.findVariant(v.HostSKU, v.WareHouse, result, skuvariant)) {
						switch (result.ErrorCode) {
							case 23:
							case 56:
							case 57:
								//No (existing) variant
								//add the variant

								//THIS IS SIGNIFICANT - Products are hashless - sellers have variants wich carry an #SUFFIX (of the version they choose to sell), along with their (internal) part number
								//If a disti pushes as #ABU type variant to us we will make a variant .. (using the same maechanism as warehouses)
								//this allows them to sell multiple versions of the same 
								string basePartNo = Split(v.MfrSKU, "#").First;
								object suffix = "";
								if (v.MfrSKU.Contains("#")) {
									suffix = Split(v.MfrSKU, "#").Last;
									// set the CODE of this variant to any #ABU type suffix in the HostMfrPartNum - 
								}

								//Every variant has a code.. for a 123456#ABU part - it woudl be ABU - generally it's blank/empty

								skuvariant = new IQ.clsVariant(suffix, iq.i_SKU(basePartNo), sellerChannel, v.HostSKU, v.DisplayText, v.WareHouse.Trim, "", r_worldwide, false, wc,
								nextid);
								Results(i) = new clsResult(true, "Added variant " + v.MfrSKU + "/" + v.HostSKU + " " + v.WareHouse, 0);
							default:
								Results(i) = result;
						}
					} else {
						//we found the variant in question

						bool A;
						bool b;

						A = (v.DisplayText != skuvariant.DisplayText);
						//description has changed
						b = (v.HostSKU != skuvariant.DistiSku);

						if (A | b) {
							skuvariant.DisplayText = v.DisplayText;
							skuvariant.DistiSku = v.HostSKU;
							// TODO warehouse, localisations etc
							skuvariant.Update();
						}

						Results(i) = result;
						//OK  (returned by findvariant)

					}

				}
				i += 1;
			}

			if (wc.Rows.Count > 0) {
				con = da.OpenDatabase();
				da.BulkWrite(con, wc, "variant", 1000, true);
				con.Close();
			}

			return Results;


		} catch (System.Exception ex) {
			ErrorLog.Add(ex);

		} finally {
			iq.PNAdown = false;

		}


	}

	/// <summary>Webservice method Called by external applications (feed reader) to directly inject stock levels to the iquote2 database/OM </summary>
	/// <remarks>Does not update the [pricing] database - which in the LONG term will not exist </remarks>
	public clsResult[] i_PnA.SetStock(string webtoken, clsStockItem[] Items)
	{



		try {
			clsStockPriceSvc.clsResult[] results;
			 // ERROR: Not supported in C#: ReDimStatement


			if (iq == null) {
				 // ERROR: Not supported in C#: ReDimStatement

				results(0) = new clsResult(false, "The object model is not currently loaded - please try later", 1);
				return results;
				//<<<<<<<<<< EXIT
			}


			if (iq.PNAdown) {
				 // ERROR: Not supported in C#: ReDimStatement

				results(0) = new clsResult(false, "The service is temporarily unavailable (typically only for a few seconds) - please try again", 1);
				return results;
				//<<<<<<<<<< EXIT
			}

			clsChannel sellerChannel;
			sellerChannel = channelFromToken(webtoken);

			if (sellerChannel == null) {
				 // ERROR: Not supported in C#: ReDimStatement

				results(0) = new clsResult(false, "Invalid or unrecognised Security Token '" + webtoken + "' your webservice security token is a lowercase 32 Digit, hyphenated, Hexadecimal GUID, supplied by ChannelCentral.net - Contact support@channelcentral.net for assistance", 1);
				return results;
				//<<<<<<<<<< EXIT
			}

			if (iq.PNAdown) {
				 // ERROR: Not supported in C#: ReDimStatement

				results(0) = new clsResult(false, "The service is temporarily unavailable (typically only for a few seconds) - please try again", 1);
				return results;
				//<<<<<<<<<< EXIT
			}

			iq.PNAdown = true;

			List<string> errormessages = new List<string>();

			//load up - JIT
			sellerChannel.LoadVariants(errormessages, 0.1);

			if (!sellerChannel.stockLoaded) {
				sellerChannel.LoadStock();
			}

			//        Dim con As SqlClient.SqlConnection
			//        Dim swc As DataTable = da.MakeWriteCacheFor(con,"stock",nsid,

			List<int> DeleteShipments;
			DeleteShipments = new List<int>();
			//Maintain a list of unmatched shipments...ones which are in the database for which the caller is not providing a updated version - they're removed from the OM as we go - but removed from the DB en-masse at the end of the batch for performance reasons

			int i;
			//Products - stock lines (each, potentially containing multiple shipments)
			foreach ( item in Items) {

				IQ.clsVariant skuvariant = null;
				clsResult result = null;


				//DistiSKU|Warehouse form a unique key
				if (!sellerChannel.findVariant(item.HostSKU, item.Warehouse, result, skuvariant)) {
					results(i) = result;
				} else {
					results(i) = new clsResult(true, "OK", 0);

					//Each stock items has many shipments - most of which are in the future
					//the isCurrent flag tells us which is the current stock level indicator (becuase the datestamps of what were once future shipments, may may now be in the past)
					//for each shipment - see if we have an existing match (on variant and date) in the OM - if so, update it (if the quantity has changed)

					IQ.clsstock match;
					IQ.clsStockPriceSvc.clsstock ashipment;
					List<IQ.clsstock> matched;
					// store a list of all the shipments we *have* matched
					matched = new List<IQ.clsstock>();
					foreach (IQ.clsStockPriceSvc.clsstock shipment__1 in item.Shipments) {
						ashipment = shipment__1;
						//apparently we need to do this (rather than look at the iterator)

						if (skuvariant.shipments == null)
							skuvariant.shipments = new SortedDictionary<DateTime, IQ.clsstock>();
						if (skuvariant.shipments.ContainsKey(ashipment.Arrival)) {
							match = skuvariant.shipments(ashipment.Arrival);
							match.LastUpdated = System.DateTime.UtcNow;
							//update the timestamp
							match.quantity = shipment__1.Quantity;
							matched.Add(match);

							if (match.quantity != shipment__1.Quantity) {
							//the (anticipated) shipment quantity has changed

							} else {
							}
						} else {
							//INSERT
							IQ.clsstock newshipment;
							newshipment = new IQ.clsstock(skuvariant, shipment__1.Quantity, shipment__1.Arrival, "PNA webservice", shipment__1.isCurrent);
						}
					}

					//remove unmatched shipments from the OM - and queue them for deletion for the DB
					if (skuvariant.shipments != null) {
						foreach ( shipment__1 in skuvariant.shipments.Values) {
							if (!matched.Contains(shipment__1)) {
								DeleteShipments.Add(shipment__1.ID);
							}
						}

						//Mark all stock shipments of this variant as non-current (in the object model)
						foreach ( shipment__1 in skuvariant.shipments.Values) {
							shipment__1.IsCurrent = false;
						}
					}

					//And do the same in the database (this is more efficient than doing many clsStock.update calls)
					object sql;
					sql = "UPDATE [Stock] SET isCurrent=0 WHERE fk_variant_id=" + skuvariant.ID;
					//The SKUVariant is unique to the seller, product and warehouse - we're 'archving' stock for this variant
					da.DBExecutesql(sql);

					bool gotcurrent = false;
					foreach ( Shipment__2 in item.Shipments) {
						//create the new (and Current) stock shipments for this SKU (which will add them to the product, and INSERT them in the database STOCK table)

						IQ.clsstock stock = new IQ.clsstock(skuvariant, Shipment__2.Quantity, Shipment__2.Arrival, "PNA Webservice", Shipment__2.isCurrent);

						if (Shipment__2.Arrival < DateAdd(DateInterval.Second, 1, DateTime.UtcNow)) {
							if (gotcurrent) {
								results(i) = new clsResult(false, "You have provided more than one current stock level - only one shipment with a date in the past is allowed (which should contain all of your current stock)", 5);
							} else {
								stock.IsCurrent = true;
								gotcurrent = true;
							}
						}
					}
				}

				i += 1;
			}

			iq.PNAdown = false;

			return results;


		} catch (System.Exception ex) {
			ErrorLog.Add(ex);

		} finally {
			iq.PNAdown = false;

		}


	}

	private clsCurrency FindCurrency(string code)
	{

		//uses LINQ to locate the currency by code
		if ((from c in iq.Currencies.Valueswhere c.Code == code).Count > 0) {
			return (from c in iq.Currencies.Valueswhere c.Code == code).First;
		} else {
			return null;
		}

	}

	/// <summary>Returns a list of SKUs known to iQuote2</summary>
	/// <param name="includeSystems">whether to include systems SKU in the list</param>
	/// <param name="includeOptions"></param>

	public HashSet<string> i_PnA.Skus(bool includeSystems, bool includeOptions)
	{

		Skus = new HashSet<string>();
		//hashsets are fast and enforce uniqueness
		foreach ( kvp in iq.i_SKU) {
			if ((kvp.Value.isSystem & includeSystems) | ((!kvp.Value.isSystem) & includeOptions)) {
				Skus.Add(kvp.Key);
			}
		}

	}

	public clsResult[] i_PnA.SetPrices(string webtoken, string CurrencyCode, clsPrice[] prices)
	{


		try {
			List<string> errormessages = new List<string>();

			clsStockPriceSvc.clsResult[] results;
			 // ERROR: Not supported in C#: ReDimStatement


			if (iq == null) {
				 // ERROR: Not supported in C#: ReDimStatement

				results(0) = new clsResult(false, "The system is currently unavailable (the object model is not loaded)", 99);
				return results;
			}

			clsChannel sellerChannel;
			sellerChannel = channelFromToken(webtoken);

			if (sellerChannel == null) {
				 // ERROR: Not supported in C#: ReDimStatement

				results(0) = new clsResult(false, "Invalid or unrecognised Security Token '" + webtoken + "' your webservice security token is a lowercase 32 Digit, hyphenated, Hexadecimal GUID, supplied by ChannelCentral.net - Contact support@channelcentral.net for assistance", 1);
				return results;
				//<<<<<<<<<< EXIT
			}

			if (iq.PNAdown) {
				 // ERROR: Not supported in C#: ReDimStatement

				results(0) = new clsResult(false, "The service is temporarily unavailable (typically only for a few seconds) - please try again", 1);
				return results;
				//<<<<<<<<<< EXIT
			}

			clsChannel buyer = null;

			//If priceBand = "" Then
			//    buyer = Everyone
			//Else
			//    buyer = BuyerChannelFrompriceBand(priceBand) 'channelFromToken(BuyerChannelToken)
			//    If buyer Is Nothing Then
			//        ReDim results(0)
			//        results(0) = New clsResult(False, "Invalid host account number " & priceBand, 2)
			//        Return results '<<<<<<<<<< EXIT
			//    End If
			//End If

			//load/freshen variants - JIT
			sellerChannel.LoadVariants(errormessages, 0.1);

			//load prices - JIT

			int i = 0;
			clsCurrency currency;
			string CurrentCurrencyCode = "";

			if (iq.i_currency_code.ContainsKey(CurrencyCode)) {
				currency = iq.i_currency_code(CurrencyCode);
			} else {
				 // ERROR: Not supported in C#: ReDimStatement

				results(0) = new clsResult(false, "Invalid currency code:" + CurrencyCode, 78);
				return results;
			}

			//results(0) = New clsResult(False, "Hello:" & CurrencyCode, 78)
			// Return results

			//Dim con As SqlClient.SqlConnection
			//con = da.OpenDatabase
			//Dim writecache As DataTable = da.MakeWriteCacheFor(con, "price")
			//con.Close()

			//Dim nextID As Integer = (From j In iq.Prices.Values Select j.ID).Max + 1  'LINQ  - the prices aren't all loaded any more so we have to go back to the DB for the MAX

			//Dim nextid As Integer
			//Dim con As SqlClient.SqlConnection = da.OpenDatabase()
			//Dim reader = da.DBExecuteReader(con, "Select max([price])+1 as c from Price")
			//reader.Read()
			//Nextid = reader.Item(0)
			//reader.Close()
			//con.Close()


			clsPrice aprice;
			foreach ( price in prices) {
				aprice = price;

				clsResult result = null;
				IQ.clsVariant SKUvariant = null;
				if (!sellerChannel.findVariant(price.HostSKU, price.Warehouse, result, SKUvariant)) {
					results(i) = result;
					//Failed to find the variant
				} else {
					//Look at the prices in the price band

					//load them Just in time (the check is trivial)
					if (!sellerChannel.pricesLoadedFor.ContainsKey(iq.getPriceBand(price.PriceBand)) || sellerChannel.pricesLoadedFor(iq.getPriceBand(price.PriceBand)) == 0) {
						sellerChannel.LoadPrices(iq.getPriceBand(price.PriceBand), errormessages);
					}

					List<IQ.clsPrice> pl = SKUvariant.Product.Prices(sellerChannel, buyer, iq.getPriceBand(price.PriceBand), currency, SKUvariant);

					bool AddPrice = false;
					if (pl == null) {
						AddPrice = true;
					} else {
						if (pl.Count == 0)
							AddPrice = true;
					}
					if (AddPrice) {
						IQ.clsPrice newprice;
						newprice = new IQ.clsPrice(SKUvariant, iq.getPriceBand(price.PriceBand), new NullablePrice(price.Price, currency, false), "PNA webservice");
						//match the IQ distiSku with the stockPriceSVC skuvariant 
						results(i) = new clsResult(true, "Added", 0);
					} else if (pl.Count == 1) {
						if (pl(0).Price.NumericValue != price.Price) {
							pl(0).Price = new NullablePrice(price.Price, currency, false);
							pl(0).PriceBand = iq.getPriceBand(price.PriceBand);
							pl(0).Update();
							results(i) = new clsResult(true, "Updated", 0);
						} else {
							results(i) = new clsResult(true, "Unchanged (shouldn't happen)", 0);
						}
					} else {
						results(i) = new clsResult(false, "more than one variant matches " + aprice.HostSKU, 18);
					}
				}
				i += 1;
			}

			// If writecache.Rows.Count > 0 Then
			// con = da.OpenDatabase
			// da.BulkWrite(con, writecache, "Price")
			// con.Close()
			// End If
			iq.PNAdown = false;
			return results;


		} catch (System.Exception ex) {
			ErrorLog.Add(ex);

		} finally {
			iq.PNAdown = false;

		}


	}

}

//End Namespace

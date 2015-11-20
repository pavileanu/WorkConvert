
using dataAccess;


public class clsProduct : i_Editable
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	//For each product - each seller offers prices to each buyer in each currency
	//Public pricekeys As Dictionary(Of Integer, Dictionary(Of Integer, Dictionary(Of Integer, Single)))

	private clsSector Sector {
		get { return m_Sector; }
		set { m_Sector = Value; }
	}
	private clsSector m_Sector;
	private clsProductType ProductType {
		get { return m_ProductType; }
		set { m_ProductType = Value; }
	}
	private clsProductType m_ProductType;
	private HashSet<clsBranch> Branches {
		get { return m_Branches; }
		set { m_Branches = Value; }
	}
	private HashSet<clsBranch> m_Branches;
	//Dictionary(Of Integer, clsBranch) = New Dictionary(Of Integer, clsBranch)

	//this has been  made private so as to force acces to prices via the clearer Baseprice(),NullablePrice() and listprice() functions
	//these are the base prices - the channels (not the accounts) contain margins 
	//                                                     seller                  null/buyer
	//Private i_Prices As Dictionary(Of clsChannel, Dictionary(Of clsChannel, Dictionary(Of clsCurrency, Dictionary(Of clsVariant, clsPrice))))
	//Private i_Prices As Dictionary(Of clsChannel, Dictionary(Of clsVariant, Dictionary(Of clsChannel, Dictionary(Of clsCurrency, clsPrice))))

	//Seller,Variant,ArrivalDate,Stock(contains variant)
	//Public i_Stock As Dictionary(Of clsChannel, Dictionary(Of clsVariant, SortedDictionary(Of Date, clsStock)))

	//  Property stock As Dictionary(Of Integer, clsStock)  'it (still) makes sense to have'global' stock and price held in the product
	//Property prices As Dictionary(Of Integer, clsPrice)
	private DateTime activeFrom {
		get { return m_activeFrom; }
		set { m_activeFrom = Value; }
	}
	private DateTime m_activeFrom;
	//Product will only display between these dates
	private DateTime activeTo {
		get { return m_activeTo; }
		set { m_activeTo = Value; }
	}
	private DateTime m_activeTo;
	//
	private bool Active {
		get { return m_Active; }
		set { m_Active = Value; }
	}
	private bool m_Active;
	//Wether the product shows (at all) or not
	private bool EOL {
		get { return m_EOL; }
		set { m_EOL = Value; }
	}
	private bool m_EOL;
	//End of life - product will only show if it has stock
	private bool Publish {
		get { return m_Publish; }
		set { m_Publish = Value; }
	}
	private bool m_Publish;
	//Only admin users see unpublished products
	private Dictionary<int, ClsAvalancheOPG> AvalancheOPGs {
		get { return m_AvalancheOPGs; }
		set { m_AvalancheOPGs = Value; }
	}
	private Dictionary<int, ClsAvalancheOPG> m_AvalancheOPGs;
	private Dictionary<int, clsFlexLine> OPGflexLines {
		get { return m_OPGflexLines; }
		set { m_OPGflexLines = Value; }
	}
	private Dictionary<int, clsFlexLine> m_OPGflexLines;
	private Dictionary<int, clsVariant> Variants {
		get { return m_Variants; }
		set { m_Variants = Value; }
	}
	private Dictionary<int, clsVariant> m_Variants;
	//a flat dictionary referencing the variants - to allow editing (would be nicer if the generic edito could evaluate more complex paths and edit lists (aswell as dictionaries)
		//this isn't a property - so it's not exposed in the editor we edit iq.bundles
	public Dictionary<int, clsBundle> Bundles;
		// how many points does this product have under each scheme
	public Dictionary<clsScheme, int> Points;
	private Dictionary<string, List<clsRegion>> Promos {
		get { return m_Promos; }
		set { m_Promos = Value; }
	}
	private Dictionary<string, List<clsRegion>> m_Promos;

	//New for HP Split
	private string mfrCode {
		get { return m_mfrCode; }
		set { m_mfrCode = Value; }
	}
	private string m_mfrCode;
	private string plCode {
		get { return m_plCode; }
		set { m_plCode = Value; }
	}
	private string m_plCode;
	private string buCode {
		get { return m_buCode; }
		set { m_buCode = Value; }
	}
	private string m_buCode;
	private string SKU {
		get { return m_SKU; }
		set { m_SKU = Value; }
	}
	private string m_SKU;


	//                                           seller                           
	//' <summary>Provides access to a List of sellerChannel specific variants of the product (which in turn have prices) </summary>

	public Dictionary<clsChannel, List<clsVariant>> i_Variants;
	//Geographical visibility is controlled by Quantity records - which relate products to regions... see also

	private Dictionary<int, clsProductAttribute> Attributes {
		get { return m_Attributes; }
		set { m_Attributes = Value; }
	}
	private Dictionary<int, clsProductAttribute> m_Attributes;
	// this is a 'flattened' dictionary for the editor (a product can have more than one attribute of the same type - i_Attributes_code groups them by type and makes thank indexable (e.g. SKU(0)) 
	//This MUST be a property as we use reflection to access productattributes in clsFields defined in a clsScreen (for the editor/matrix views)
	private Dictionary<string, List<clsProductAttribute>> i_Attributes_Code {
		get { return m_i_Attributes_Code; }
		set { m_i_Attributes_Code = Value; }
	}
	private Dictionary<string, List<clsProductAttribute>> m_i_Attributes_Code;
	//An index of the attributes by code, - NOTE: because we can have more than one attribute of the same type (eg, more than one xText, or description) this is an index to a LIST of attributes 
	private bool _isSystem;
	private bool _isOption;
	private bool isDeleted {
		get { return m_isDeleted; }
		set { m_isDeleted = Value; }
	}
	private bool m_isDeleted;

	/// <summary>Returns the HP/Everyone Price of the Variant that matches the buyerAccounts Region and Currency</summary>
	public clsPrice ListPrice(clsAccount buyeraccount)
	{

		ListPrice = null;
		clsPriceBand hplist = iq.getPriceBand("");
		//Hplist -dosnt need a 'special' band - becuase it's the 'everyone' band on the HP sellerChannel

		if (this.i_Variants != null) {
			if (this.i_Variants.ContainsKey(hp)) {
				//Not wildly happy with this - walking over (say) 50 list prices pre product could be slow
				foreach ( v in this.i_Variants(HP)) {
					//Making i_variants a compound key SellerChannel|Region would be much faster
					if (v.Region.Code != "US") {
						object a = 0;
					}
					//Base LIST PRICES on SELLERS REGION (not buyers) - ultimately the account should have a region
					if (object.ReferenceEquals(v.Region, buyeraccount.SellerChannel.Region)) {
						if (v.i_prices.ContainsKey(hplist)) {
							if (v.i_prices(hplist).ContainsKey(buyeraccount.Currency)) {
								ListPrice = v.i_prices(hplist)(buyeraccount.Currency);
								return;
							}
						}
					}
				}
			}
		}
		//ListPrice = Me.prices(Everyone)(buyeraccount.Currency)

	}

	public object clone(string newsku)
	{

		 // ERROR: Not supported in C#: WithStatement


	}

	public string FirstAttributeEnglishText(code)
	{

		if (this.i_Attributes_Code.ContainsKey(code)) {
			return this.i_Attributes_Code(code)(0).Translation.text(English);
		}

		return "";

	}

	public bool isFIO {

		//NOT to be confused with preinstalled parts !!

		//FIOs (Factory Installed Options) - ahve part numbers - but can't be 'bought' - and therefore be flexed
		//(in practise there is probably an equevilent (often identical) part - but it has a different (and unknwon to us) SKU

		get {
			if (this.i_Attributes_Code.ContainsKey("focus")) {
				foreach ( f in this.i_Attributes_Code("focus")) {
					if (f.Translation.text(English) == ("FIO")) {
						return true;
					}
				}
			}
		}
	}


	private bool isSystem {
		get {
			if (path != "" & _isSystem) {
				if (Split(path, ".").Length < 6) {
					return _isSystem;
				} else {
					return false;
				}
			} else {
				return _isSystem;
			}

		}
		set { _isSystem = value; }
	}

	private bool isOption {
		get { return _isOption; }
		set { _isOption = value; }
	}

	public Manufacturer Manufacturer {


		get {
			Manufacturer = Manufacturer.Unknown;


			if (string.IsNullOrEmpty(this.mfrCode)) {
				// No mfrCode found - make the decsion based on product types
				if (this.isSystem) {
					if (this.ProductType.Code == "DTO" || this.ProductType.Code == "NBK") {
						Manufacturer = Manufacturer.HPI;
					} else {
						Manufacturer = Manufacturer.HPE;
					}
				}


			} else {
				// mfrCode set up - use it to work out the manufacturer
				if (string.Equals(this.mfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase)) {
					Manufacturer = Manufacturer.HPI;
				} else if (string.Equals(this.mfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase)) {
					Manufacturer = Manufacturer.HPE;
				}

			}

		}
	}


	//Public ReadOnly Property sku() As String
	//    Get
	//        If Me.i_Attributes_Code.ContainsKey("MfrSKU") Then
	//            Return Me.i_Attributes_Code("MfrSKU")(0).displayName(English)
	//        Else
	//            Return ""
	//        End If
	//    End Get
	//End Property

	public bool inFeed(clsChannel sellerchannel)
	{

		if (Left(sellerchannel.Code, 3) == "MHP")
			return true;
		//Temporary hack becuase otherwise you cannot flex up in Universal

		if (this.i_Variants == null)
			return false;
		//*Nobody* stocks (or has a price for me)
		if (!this.i_Variants.ContainsKey(sellerchannel))
			return false;
		return true;

	}


	public clsVariant MatchingVariant(clsVariant MatchWith, clsChannel sellerchannel)
	{

		//looks thtough all the variants on this Product ie. me
		//for the one which most closely matches 'matchwith'

		//    Product.matchingvariant(quoteItem.SKUVariant, buyerAccount.SellerChannel)

		//niave - but clear - hopefully

		MatchingVariant = null;

		int bestscore = 0;
		int score;
		clsChannel channel;


		if (this.i_Variants != null) {
			if (this.i_Variants.ContainsKey(sellerchannel))
				channel = sellerchannel;
			else
				channel = HP;

			if (this.i_Variants.ContainsKey(channel)) {
				foreach ( v in this.i_Variants(channel)) {
					score = 0;
					if (v.Warehouse == MatchWith.Warehouse)
						score = score + 1;
					if (v.Localisation == MatchWith.Localisation)
						score = score + 1;
					if (v.Code == MatchWith.Code)
						score = score + 1;
					if (object.ReferenceEquals(v.Region, MatchWith.Region))
						score += 1;
					if (score > bestscore){MatchingVariant = v;bestscore = score;}
				}
			}

		}

	}


	public clsProduct clone()
	{

		clone = new clsProduct(this.SKU, this.isSystem, this.isOption, this.Sector, this.ProductType, this.activeFrom, this.activeTo, this.Active, this.EOL, this.Publish,
		this.mfrCode, this.buCode, this.plCode);

		//clone the HP variant(s)  (which internally clone the prices)
		clsVariant cv;

		if (this.i_Variants.ContainsKey(HP)) {
			foreach ( v in this.i_Variants(HP)) {
				cv = v.clone(clone);
			}
		}

		clsProductAttribute pa;
		foreach ( pa in this.Attributes.Values) {
			pa.Clone(clone);
			//clone my product attributes onto the new (cloned) product
		}


	}

	public bool anyStock(clsChannel SellerChannel)
	{

		//returns wether there is any present or future stock for any variant
		anyStock = false;

		//Each variant will have many shipments - any before 'now' are absolute stock values at that date and should have current = false
		//there should only be 1 after 'now' who's 'current=true' - others after now are (relative) shipments
		if (this.i_Variants.ContainsKey(SellerChannel)) {
			foreach ( v in this.i_Variants(SellerChannel)) {
				foreach ( s in v.shipments.Values) {
					if (s.IsCurrent | s.Arrival > Now) {
						if (s.quantity > 0)
							return true;
						//yey ! - some stock
					}
				}
			}
		}

	}


	public clsProduct()
	{
		AvalancheOPGs = new Dictionary<int, ClsAvalancheOPG>();
		OPGflexLines = new Dictionary<int, clsFlexLine>();
		this.i_Variants = new Dictionary<clsChannel, List<clsVariant>>();
		this.Variants = new Dictionary<int, clsVariant>();
		this.Points = new Dictionary<clsScheme, int>();
		//number of points this product is worth under each scheme
		this.Promos = new Dictionary<string, List<clsRegion>>();
		this.i_Variants = new Dictionary<clsChannel, List<clsVariant>>();
	}

	public object i_Editable.insert(ref List<string> errorMessages)
	{

		return new clsProduct(this.SKU, this.isSystem, this.isOption, this.Sector, this.ProductType, this.activeFrom, this.activeTo, this.Active, this.EOL, this.Publish,
		this.mfrCode, this.buCode, this.plCode);

	}


	public void i_Editable.update(ref List<string> errorMessages)
	{
		if (this.ID == 0)
			System.Diagnostics.Debugger.Break();

		object sql;
		sql = "UPDATE [Product] SET sku='" + this.SKU + "', isSystem=" + IIf(this.isSystem, 1, 0) + ", isOption=" + IIf(this.isOption, 1, 0) + ",fk_producttype_id=" + this.ProductType.ID + ",fk_sector_id=" + this.Sector.ID;
		sql += ",activefrom=" + da.UniversalDate(this.activeFrom) + ",activeTo=" + da.UniversalDate(this.activeTo) + ",active=" + IIf(this.Active, 1, 0) + ",eol=" + IIf(this.EOL, 1, 0) + ",publish=" + IIf(this.Publish, 1, 0);
		sql += ",mfrCode=" + da.SqlEncode(this.mfrCode) + ",buCode=" + da.SqlEncode(this.buCode) + ", deleted=" + IIf(this.isDeleted, 1, 0) + ",plCode=" + da.SqlEncode(this.plCode);
		sql += " WHERE ID=" + this.ID;
		da.DBExecutesql(sql);

	}


	public void i_Editable.delete(ref List<string> errorMessages)
	{
		try {
			da.DBExecutesql("DELETE  FROM PRODUCT WHERE ID=" + this.ID);
			//will often fail due to RI (expose this error through the editor)
			iq.i_SKU.Remove(this.sku);
			iq.Products.Remove(this.ID);

		} catch (Exception ex) {
			errorMessages.Add(ex.Message.ToString);

		}

	}

	public string DisplayName(clsLanguage clsLanguage, bool StripSKU = false)
	{
		//Show the description - falling back to SKU (if absent)

		DisplayName = this.sku;

		if (this.i_Attributes_Code.ContainsKey("desc")) {
			DisplayName = !StripSKU ? this.sku + " - " : "" + this.i_Attributes_Code("desc")(0).Translation.text(English);

		}
	}
	public string i_Editable.displayName(clsLanguage clsLanguage)
	{

		return DisplayName(clsLanguage, false);

	}


	public bool hasSKU()
	{

		//Returns true if the product has a 'real' (non ### SKU)

		hasSKU = false;
		if (this.SKU != "")
			return true;

		return false;

		//this is obsolete as product will have their sku set at load time (it if is empty),
		// from the product attribute if it is present.

		if (this.SKU != "") {
			object sku;
			sku = this.SKU;
			//If Left$(sku$, 3) <> "###" Then
			//End If
			return true;
		}

	}
	//Public Function SKU() As String

	//    If Not Me.i_Attributes Is Nothing Then
	//        If Me.i_attributes_code.containskey("MfrSKU") Then
	//            SKU = Me.i_attributes_code("MfrSKU").Translation.Text(s_lang)
	//            If Left$(SKU$, 3) = "###" Then
	//                SKU$ = "-"
	//            End If
	//        End If
	//    End If

	//End Function

	/// <summary>Returns the 'HP','Everyone' price for the variant with the specified Region and Currency (which will be a list price)
	/// 
	/// </summary>
	/// <param name="skuvariant"></param>
	/// <returns></returns>
	/// <remarks></remarks>

	private List<clsPrice> GetPrices(clsAccount Buyeraccount, int PriceConfig, clsVariant SKUvariant, ref List<string> errormessages, bool callWebservice)
	{

		GetPrices = new List<clsPrice>();
		//fail safe (return SOMETHING)
		if (SKUvariant == null){errormessages.Add("* SkuVariant was Nothing in getprices) - Use iq.allvariant");return;
}

		//Called form StalePrices()
		//Returns a list of prices for the buyer account based on the PriceConfig of the sellerchannel 
		//Pricing may be customer specific,margin based, may queue a webservice based price/stock update or may be HP list price for the buyeraccounts region.
		//Pricing will only be of one type for a single call - ie. it will not return List and CustomerSpecific prices in the same list

		//an empty list is returned if the are no prices - getPrices never returns Nothing

		//PriceConfig  AND 8 Inclide customer specific prices
		//PriceConfig  AND 4 Use Price bands
		//PriceConfig  AND 2 Show List price (in the absence of any other price)
		//PriceConfig  AND 1 Show POA (for products for which we have no price (as opposed to not showing the product at all if there is no price at all)

		object sku;
		sku = this.SKU;

		List<clsPrice> SpecificPrices = new List<clsPrice>();
		List<clsPrice> BasePrices = new List<clsPrice>();
		List<clsPrice> ListPrices = new List<clsPrice>();

		//returns customer specific prices - Includng potenitally outstanding web request  POA 'Requesting prices' prices
		//also does margin based pricing ! - note, you can't add a margin to a webservice price
		SpecificPrices = this.Prices(Buyeraccount, SKUvariant);

		//This IS DELIBERATE - DON'T undo it         \/-------------------------------\/ - the base price is the sellers price to themself
		//BasePrices = Me.Prices(Buyeraccount.SellerChannel, Buyeraccount.SellerChannel, Buyeraccount.Currency, SKUvariant)

		//With Buyeraccount.SellerChannel

		//if we're using a webserive - we return the (last known) specific price
		//8 = Customer specific prices

		//If (PriceConfig And 8) And SpecificPrices.Count > 0 Then
		//    'Some prices are loaded with 0's - invalidate them (so the basket will update correctly)
		//    If SpecificPrices(0).Price.NumericValue = 0 And SpecificPrices(0).Price.valid = True Then
		//        SpecificPrices(0).Price.valid = False
		//        SpecificPrices(0).Price.Message = "Was 0.. checking with webservice"
		//    End If
		//    Return SpecificPrices
		//End If

		//ListPrices = New List(Of clsPrice)
		//If Me.ListPrice(Buyeraccount) IsNot Nothing Then
		//    ListPrices.Add(Me.ListPrice(Buyeraccount))
		//Return ListPrices
		//End If

		//mask off the webservice BIT on univeral instances
		if (Left(Buyeraccount.SellerChannel.Code, 3) == "MHP")
			PriceConfig = PriceConfig & !8;
		//HP (universal instances)  dont have a webservice (temporary hack)

		//And (PriceConfig And 16) Then
		if ((PriceConfig & 8) & callWebservice) {
			//we're using a webservice.. and there was no specific price YET..
			//IF this poduct is in the feed (we have a variant) return a 'pending' price (and DO show the product)


			//BRAZIL (see clsAccount.warehousefilter)
			if (Buyeraccount.wareHouseFilter.ToUpper != "NONE") {
				//ChannelSKU(Me, SKUvariant, Buyeraccount.SellerChannel) <> "" Then
				if (this.inFeed(Buyeraccount.SellerChannel.IsCloneOf)) {
					List<clsPrice> poa;
					poa = new List<clsPrice>();

					//will make a new price (for every variant!) - A COPY of the HP list price  ''storing a POA, until the webservice returns a real price (for the first time)

					clsPrice aPrice;
					clsstock aStock;
					// Stock is really just a collection of all stock positions

					//these are the disti variants - they're never going to be HP list price ones
					//tolist is new (was getting a collection has been modified enumeration may not execute)
					foreach ( v in this.i_Variants(Buyeraccount.SellerChannel.IsCloneOf).ToList) {
						if (Buyeraccount.wareHouseFilter == "" | string.Equals(Buyeraccount.wareHouseFilter, v.Code, StringComparison.CurrentCultureIgnoreCase)) {

							if (!v.Deleted) {
								//need to see if there is a customer specific price first - if not, clone the list price if these is one,
								//otherwise make a POA


								if (object.ReferenceEquals(v, SKUvariant) | object.ReferenceEquals(SKUvariant, iq.AllVariants)) {
									aPrice = v.priceFor(Buyeraccount.Priceband, Buyeraccount.Currency);

									if (aPrice == null) {
										//we have no price customer specific price

										clsPrice lp = ListPrice(Buyeraccount);

										if (lp == null) {
											aPrice = new clsPrice(v, Buyeraccount.Priceband, new NullablePrice(Buyeraccount.Currency), "Requesting price.." + Format(Now, "ddd hh:nn"));

										//aPrice.Price = New NullablePrice(Buyeraccount.Currency)
										//aPrice.SKUVariant = v
										//aPrice.PriceBand = Buyeraccount.Priceband
										//aPrice.Price.isValid = False
										} else {
											//Clone the HP list PRICE (not variant) into a new customer specific record PRICE
											//(attached to the exsiting disti Variant)
											//prior to calling the webservice for a 'real' (Updated, customer specific) price

											aPrice = new clsPrice(v, Buyeraccount.Priceband, lp.Price, "LP clone");
											aPrice.Price.isList = true;
											//force it into the stale list
											aPrice.lastRequested = DateAdd(DateInterval.Minute, -100, Now);

										}
									}

									if (v.shipments.Count == 0) {
										//similarly, we need to make a stock record - so we can render a DIV with a valid S_Id - to be replaced when the webservice returns - without this wee see 'X'  stock when first using the system
										//If aPrice.ID <> -1 Then 'not a POA
										aStock = new clsstock(v, -1, Now, "initial", true);
										// End If
									}
									//End If
									poa.Add(aPrice);
								}

							}
						}
					}

					return poa;
				} else {
					//Changes to Display CarePack Blank Prices
					if (this.ListPrice(Buyeraccount) != null) {
						//fetch HPs list price according to the buyers region and currency - NB list pricing is per country at the moment (ie. there is no EMEA list price)
						ListPrices.Add(this.ListPrice(Buyeraccount));
						return ListPrices;
					} else {
						// Stop
					}
					//not in their feed
					// return an EMPTY list - which supresses display (feed only skus)
					return new List<clsPrice>();
					//return a list of 0 prices

				}
			}


			//ElseIf (PriceConfig And 8) And Not callWebservice Then
			//    'this is the code path for checeks of whether a product *could* have a price (from the webservice)
			//    'these tend to be the big, expensige resurvice ones for working out whether to display squares etc.
			//    Dim couldhave As List(Of clsPrice) = New List(Of clsPrice)
			//    If Me.inFeed(Buyeraccount.SellerChannel) Then 'ChannelSKU(Me, SKUvariant, Buyeraccount.SellerChannel) <> "" Then
			//        couldhave.Add(Nothing) 'this is a POA
			//    End If
			//    Return couldhave
		}

		//If Not Me.inFeed(Buyeraccount.SellerChannel) Then Return New List(Of clsPrice) 'return a list of 0 prices

		//Everyone' IS the Base Channel - this is loosely equivilent to the IQ1 'external' price - for implementations with a webservice
		//use price bands
		if ((PriceConfig & 4)) {
			BasePrices = this.Prices(Buyeraccount, SKUvariant);
			//.Priceband, Everyone, Buyeraccount.Currency) ', SKUvariant)
			if (BasePrices.Count > 0)
				return BasePrices;
		}

		//   If PriceConfig And 2 Then
		// Buyeraccount.Currency = iq.i_currency_code("USD")


		if (this.ListPrice(Buyeraccount) != null) {
			//fetch HPs list price according to the buyers region and currency - NB list pricing is per country at the moment (ie. there is no EMEA list price)
			ListPrices.Add(this.ListPrice(Buyeraccount));
			return ListPrices;
		} else {
			// Stop
		}




		//    End If

		//'this is effectively a 'show everything' flag - becuase it will make a price on products that are geographically out of scope

		if ((PriceConfig & 1) != 0) {
			List<clsPrice> poa;
			poa = new List<clsPrice>();

			//If Me.i_Variants(sellerchannel).
			//Dim aprice As clsPrice
			//aprice = New clsPrice(Buyeraccount, New clsVariant(-1, "POA", Me, Buyeraccount.SellerChannel, "", "", "", "", r_worldwide, False, False))

			poa.Add(null);
			//return a list containing a single nothing ' This is a POA
			return poa;
		}

		//if ALL else fails - we wont show the product  return an EMPTY list
		return new List<clsPrice>();
		//return a list of 0 prices

	}


	public clsProduct(string SKu, string Name, bool isSystem, bool isOption, clsSector sector, clsProductType productType, DateTime activeFrom, DateTime ActiveTo, bool Active, bool EOL,

	bool Publish, string mfrCode, string buCode, string plCode)
	{
		this.New(SKu, isSystem, isOption, sector, productType, activeFrom, ActiveTo, Active, EOL, Publish,
		mfrCode, buCode, plCode);
		//call the 'normal' constructor to make an instance and populate me.id

		//This is a 'quick' method to create a product with a single attribute (of Name).. and to fill that attribute with a new text object in English carrying the description
		//you *generally* dont want to be doing that - but it's useful for creating some of the metadata

		if (SKu != "") {
			iq.i_SKU.Add(SKu, this);
		}

		clsProductAttribute desc;
		desc = new clsProductAttribute(this, iq.i_attribute_code("Name"), 0, iq.i_unit_code("txt"), iq.AddTranslation(Name, s_lang, null, 0, null, 0, true));

		AvalancheOPGs = new Dictionary<int, ClsAvalancheOPG>();
		OPGflexLines = new Dictionary<int, clsFlexLine>();
		this.i_Variants = new Dictionary<clsChannel, List<clsVariant>>();
		this.Variants = new Dictionary<int, clsVariant>();
		this.i_Attributes_Code = new Dictionary<string, List<clsProductAttribute>>(StringComparer.CurrentCultureIgnoreCase);
		this.Promos = new Dictionary<string, List<clsRegion>>();

	}
	public clsProduct(string sku, bool IsSystem, bool IsOption, clsSector Sector, clsProductType ProductType, DateTime activeFrom, DateTime ActiveTo, bool Active, bool EOL, bool Publish,

	string mfrCode, string buCode, string plCode, DataTable wc = null, ref int nextid = -1, bool InMemoryOnly = false)
	{
		this.Attributes = new Dictionary<int, clsProductAttribute>();
		this.i_Attributes_Code = new Dictionary<string, List<clsProductAttribute>>(StringComparer.CurrentCultureIgnoreCase);


		if (sku == "" & (IsSystem | IsOption))
			System.Diagnostics.Debugger.Break();

		if (IsSystem == 0 & IsOption == 0) {
			object jjj = 0;
		}


		this.SKU = sku;
		this.isSystem = IsSystem;
		this.isOption = IsOption;
		this.Sector = Sector;
		this.ProductType = ProductType;
		this.activeFrom = activeFrom;
		this.activeTo = ActiveTo;
		this.Active = Active;
		this.EOL = EOL;
		this.Publish = Publish;

		AvalancheOPGs = new Dictionary<int, ClsAvalancheOPG>();
		OPGflexLines = new Dictionary<int, clsFlexLine>();
		this.i_Variants = new Dictionary<clsChannel, List<clsVariant>>();
		this.Variants = new Dictionary<int, clsVariant>();
		this.Points = new Dictionary<clsScheme, int>();
		//number of points this product is worth under each scheme
		this.Promos = new Dictionary<string, List<clsRegion>>();

		this.mfrCode = mfrCode;
		//broad
		this.buCode = buCode;
		//narrow
		this.plCode = plCode;
		//narrower

		if (!InMemoryOnly) {
			if (sku != "") {
				iq.i_SKU.Add(sku, this);
			}

			//add myself to the master list
			if (wc != null) {
				this.ID = nextid;
				nextid += 1;

				System.Data.DataRow row;
				row = wc.NewRow();
				row("ID") = this.ID;
				//- we EXPLICITLY set ids ?
				row("sku") = this.SKU;
				row("issystem") = this.isSystem;
				row("isoption") = this.isOption;
				row("fk_producttype_id") = this.ProductType.ID;
				row("fk_sector_id") = this.Sector.ID;
				row("activefrom") = this.activeFrom;
				row("activeTo") = this.activeTo;
				row("active") = this.Active;
				row("eol") = this.EOL;
				row("publish") = this.Publish;
				row("mfrCode") = this.mfrCode;
				row("buCode") = this.buCode;
				row("plCode") = this.plCode;
				row("deleted") = false;

				wc.Rows.Add(row);


			} else {
				object sql;
				sql = "INSERT INTO PRODUCT (sku,issystem,isoption,fk_producttype_id,fk_sector_id,activefrom,activeto,active,eol,publish,mfrCode,buCode,plCode )";
				sql += " VALUES (" + da.SqlEncode(sku) + "," + IIf(IsSystem, 1, 0) + "," + IIf(IsOption, 1, 0) + "," + ProductType.ID + "," + Sector.ID + "," + da.UniversalDate(activeFrom) + "," + da.UniversalDate(ActiveTo) + "," + IIf(Active, 1, 0) + "," + IIf(EOL, 1, 0) + "," + IIf(Publish, 1, 0) + ",";
				sql += da.SqlEncode(mfrCode) + "," + da.SqlEncode(buCode) + "," + da.SqlEncode(plCode) + ");";

				this.ID = da.DBExecutesql(sql, true);
			}

			iq.Products.Add(this.ID, this);
		}

		//Prices = New Dictionary(Of clsChannel, Dictionary(Of clsBuyerGroup, Dictionary(Of clsCurrency, clsPrice)))
		//i_Prices = New Dictionary(Of clsChannel, Dictionary(Of clsChannel, Dictionary(Of clsCurrency, Dictionary(Of clsVariant, clsPrice))))
		//i_Prices = New Dictionary(Of clsChannel, Dictionary(Of clsVariant, Dictionary(Of clsChannel, Dictionary(Of clsCurrency, clsPrice))))
		//i_Stock = New Dictionary(Of clsChannel, Dictionary(Of clsVariant, SortedDictionary(Of Date, clsStock)))
		// Stock = New Dictionary(Of Integer, clsStock)
		// Prices = New Dictionary(Of Integer, clsPrice)

	}


	public clsProduct(int id, string sku, bool isSystem, bool isOption, clsSector sector, clsProductType ProductType, DateTime activeFrom, DateTime ActiveTo, bool Active, bool EOL,

	bool Publish, string mfrCode, string buCode, string plCode)
	{
		//If sku <> "" And (isSystem Or isOption) Then

		this.ID = id;

		this.SKU = sku;
		this.Attributes = new Dictionary<int, clsProductAttribute>();
		this.i_Attributes_Code = new Dictionary<string, List<clsProductAttribute>>(StringComparer.CurrentCultureIgnoreCase);
		this.Sector = sector;
		this.ProductType = ProductType;

		if (sku != "") {
			iq.i_SKU.Add(sku, this);
		}

		//i_Prices = New Dictionary(Of clsChannel, Dictionary(Of clsChannel, Dictionary(Of clsCurrency, Dictionary(Of clsVariant, clsPrice))))
		//i_Prices = New Dictionary(Of clsChannel, Dictionary(Of clsVariant, Dictionary(Of clsChannel, Dictionary(Of clsCurrency, clsPrice))))
		//i_Stock = New Dictionary(Of clsChannel, Dictionary(Of clsVariant, SortedDictionary(Of Date, clsStock)))
		// Stock = New Dictionary(Of Integer, clsStock)
		// Prices = New Dictionary(Of Integer, clsPrice)
		this.AvalancheOPGs = new Dictionary<int, ClsAvalancheOPG>();
		this.OPGflexLines = new Dictionary<int, clsFlexLine>();

		this.i_Variants = new Dictionary<clsChannel, List<clsVariant>>();
		this.Variants = new Dictionary<int, clsVariant>();
		this.Points = new Dictionary<clsScheme, int>();
		//number of points this product is worth under each scheme

		this.activeFrom = activeFrom;
		this.activeTo = ActiveTo;
		this.Active = Active;
		this.EOL = EOL;
		this.Publish = Publish;
		this.isSystem = isSystem;
		this.isOption = isOption;
		this.Promos = new Dictionary<string, List<clsRegion>>();

		this.mfrCode = mfrCode;
		this.buCode = buCode;
		this.plCode = plCode;
		//End If

	}

	//Public Function HasPrices() As Boolean

	//    HasPrices = Me.i_Prices.Count > 0

	//End Function

	//Public Function VariantPrice(BuyerAccount As clsAccount, SKUvariant As clsVariant, ByRef errorMessages As List(Of String)) As NullablePrice

	//    'fetches the single price for a speficied product variant  for a specified account
	//    'It will be either specific, margin based, list or POA.. all dependent upon the seller channels [priceConfig]

	//    Dim OnePrice As List(Of clsPrice)
	//    OnePrice = Me.Prices(BuyerAccount, SKUvariant) 'get the one matching price
	//    If OnePrice Is Nothing Then
	//        Return New NullablePrice(BuyerAccount.Currency)
	//    ElseIf OnePrice.Count = 1 Then
	//        Return OnePrice(0).Price
	//    Else
	//        Return OnePrice(0).Price
	//        errorMessages.Add("* more then one price for the same variant !")
	//    End If
	//End Function


	/// <summary>
	/// Returns a string, representing the amount of (current) stock of the specified variant - or the total stock of all variants if skuvariant is ommitted
	/// POPULATES the NumericValue supplied - with 
	/// </summary>
	public string CurrentStock(clsAccount buyeraccount, ref int numericValue, clsVariant whichVariant, ref List<string> errorMessages)
	{

		if (whichVariant == null) {
			errorMessages.Add("* Whichvariant was Nothing for CurrentStock()");
			numericValue = -10;
			CurrentStock = "-";

		} else {
			//NB: We fetch stock from the sellerchannels 'isCloneOf' - which for non-clones, is itself.
			if (this.i_Variants.ContainsKey(buyeraccount.SellerChannel.IsCloneOf)) {
				CurrentStock = "-";
				//iq.EnglishIndex("Unknown").text(buyeraccount.Language) 'should be agentaccount.language really
				foreach ( sv in this.i_Variants(buyeraccount.SellerChannel.IsCloneOf)) {
					//If Me.i_Stock(buyeraccount.SellerChannel.IsCloneOf).ContainsKey(skuVariant) Then
					if (object.ReferenceEquals(sv, whichVariant) | object.ReferenceEquals(whichVariant, iq.AllVariants)) {
						foreach ( shipment in sv.shipments.Values) {
							if (shipment.IsCurrent & shipment.Arrival < Now) {
								numericValue += shipment.quantity;
								if (whichVariant != null){CurrentStock = numericValue.ToString;return;
}
								//we're done
							}
						}
						CurrentStock = numericValue.ToString;
						return;
						//we're done (totaled for all skuvariants)
					} else {
						CurrentStock = "-";
						//  iq.EnglishIndex("Unstocked Variant").text(buyeraccount.Language)
						numericValue = -1;
					}
				}
			} else {
				CurrentStock = "no record";
				//iq.EnglishIndex("Unstocked").text(buyeraccount.Language)
				numericValue = -2;
			}

			if (numericValue < 0)
				CurrentStock = numericValue.ToString;
		}

	}
	private List<clsPrice> BasePrices(clsAccount buyerAccount, clsVariant whichVariant)
	{

		//Returns all of a seller channels prices for this product in the buyerAccounts currency - there may be many variants 
		//If SKUVariant is is suppled - only that variant is returned

		BasePrices = null;
		clsChannel sellerChannel = buyerAccount.SellerChannel;

		if (this.i_Variants.ContainsKey(sellerChannel)) {
			foreach ( v in this.i_Variants(sellerChannel)) {
				if (object.ReferenceEquals(v, whichVariant) | object.ReferenceEquals(whichVariant, iq.AllVariants)) {
					if (BasePrices == null)
						BasePrices = new List<clsPrice>();
					BasePrices.Add(v.i_prices(iq.getPriceBand(""))(buyerAccount.Currency));
				}
			}
		}

	}

	//Public Function listPrice(country As clsRegion) 'Currency As clsCurrency, Optional SKUVariant As clsVariant = Nothing) As List(Of clsPrice)

	//    'returns the HP list price for every SKUvariant of the product available in the specified currency
	//    'optionally returns List Price for only One (specified) variant
	//    listPrices = Nothing

	//    Dim hp As clsChannel
	//    hp = iq.i_channel_code("HP")
	//    If Me.i_variants IsNot Nothing Then
	//        If Me.i_variants.ContainsKey(hp) Then  'the first dimension of the product.i_variants is the seller channel
	//            For Each sv In Me.i_variants(hp) 'each of those contains a LIST of clsVariant
	//                If SKUVariant Is Nothing Or sv Is SKUVariant Then
	//                    If sv.prices.ContainsKey(Everyone) Then
	//                        If sv.prices(Everyone).ContainsKey(Currency) Then
	//                            If listPrices Is Nothing Then listPrices = New List(Of clsPrice)
	//                            listPrices.Add(sv.prices(Everyone)(Currency))
	//                        End If
	//                    End If
	//                End If
	//            Next
	//        End If
	//    End If

	//End Function


	//Public Function ListPrice(Currency As clsCurrency) As clsPrice

	//    'list pricing is the price of HP (the seller) to the everyone channel - for the first variant (there shoudl only be one!)

	//    Dim hp As clsChannel
	//    hp = iq.i_channel_code("HP") 'If this is missing - it's because you havent imported list prices (see Default.aspx !)

	//    'ListPrice = New clsPrice() : ListPrice.Seller = hp : ListPrice.Buyer = Everyone : ListPrice.Price = New nullablePrice(Currency) : ListPrice.ID = -1 : ListPrice.DateStamp = Now : ListPrice.SKUVariant = skuvariant : ListPrice.Source = ""
	//    '    Property Variants As Dictionary(Of clsChannel, Dictionary(Of Integer, clsVariant))

	//    If Me.i_variants.ContainsKey(hp) Then
	//        If Me.i_variants(hp).ContainsKey(Everyone) Then
	//            If Me.i_variants(hp)(Everyone).prices.ContainsKey(Currency) Then
	//            End If
	//        End If
	//        If Not Me.i_Prices Is Nothing Then
	//            If Me.i_Prices.ContainsKey(hp) Then
	//                If Me.i_Prices(hp).Count = 1 Then
	//                    'the first (0'th) variant
	//                    If Me.i_Prices(hp).Values(0).ContainsKey(Everyone) Then  'is there a price in this accounts currency
	//                        If Me.i_Prices(hp).Values(0)(Everyone).ContainsKey(Currency) Then
	//                            ListPrice = i_Prices(hp).Values(0)(Everyone)(Currency) '.Price
	//                            ListPrice.Price.Message = "List price"
	//                        Else
	//                            Dim aprice As clsPrice = New clsPrice(Me.i_variants(hp).Values(0), Everyone, New nullablePrice(Currency), "No list price available in the currency")
	//                            aprice.Price.valid = False
	//                            Return aprice
	//                        End If
	//                    Else
	//                        Stop
	//                    End If
	//                Else
	//                    'ut oh.. more than one list price variant !
	//                    Stop
	//                End If
	//            End If
	//        End If
	//End Function

	public List<clsPrice> Prices(clsAccount buyeraccount, clsVariant whichVariant)
	{

			//call the other (more granular) overload
		 // ERROR: Not supported in C#: WithStatement


	}

	public List<clsPrice> Prices(clsChannel sellerchannel, clsChannel buyerchannel, clsPriceBand priceband, clsCurrency currency, clsVariant whichvariant)
	{

		//returns The Prices for all (or the specified) SKUVariant(s) for the specified buyer, - at the correct margin/multiplier for the buyer/seller combo
		//will return an empty list if there is no price (POA)

		List<clsPrice> ret = new List<clsPrice>();
		clsMargin margin;

		//NB: We fetch pricing from the sellerchannels 'isCloneOf' - which for non-clones, is itself.
		clsChannel sourceChannel = sellerchannel.IsCloneOf;

		//If we're getting prices for a clone..
		//There's an important distinction between the SourceChannel and the SellerChannel - the sellerchannel (which may be a clone) carries the margin for the buyer..
		//whereas the sourceChannel (the clones 'parent') has the base price

		List<clsVariant> done;
		done = new List<clsVariant>();

		//First get SPECIFIC (overriding)prices
		if (this.i_Variants != null) {
			//does the SOURCE channel seller have a (base) price for this product            
			if (this.i_Variants.ContainsKey(sourceChannel)) {
				//The variants are *not* buyer specific - there are a small number of variants (and a large number of prices)
				foreach ( v in this.i_Variants(sourceChannel)) {
					if (object.ReferenceEquals(v, whichvariant) | object.ReferenceEquals(whichvariant, iq.AllVariants)) {
						//there are *some* specific prices for this product - for this buyer (perhaps not for the right variants or currencies
						if (v.i_prices.ContainsKey(priceband)) {
							if (v.i_prices(priceband).ContainsKey(currency)) {
								ret.Add(v.i_prices(priceband)(currency));
								//we've found a specifc price - to which we should NOT apply margin !
								done.Add(v);
							}
						}
					}
				}
			}

			//for any remaining unpriced - do margin based pricing
			if (buyerchannel != null) {
				//does this seller have ANY margin specified for this buyer
				if (sellerchannel.Margin.ContainsKey(buyerchannel)) {
					//is there a margin specified for products within this sector (BU) .. PSG/ISS . . .
					if (sellerchannel.Margin(buyerchannel).ContainsKey(this.Sector)) {
						if (this.i_Variants.ContainsKey(sourceChannel)) {
							List<clsVariant> vpc = this.i_Variants(sourceChannel);
							//seller'  
							if (vpc.Count > 30) {
								object a = 0;
							}
							foreach ( SV in vpc) {
								if (!done.Contains(SV)) {
									//  If SV Is WhichVariant Or WhichVariant Is iq.AllVariants Then  'was variantmatch
									margin = sellerchannel.Margin(buyerchannel)(this.Sector);
									//baseprice is their price for themselves (or maybe 'everyone' - check)
									if (SV.i_prices.ContainsKey(iq.getPriceBand(margin.PriceBand))) {
										IQ.clsPrice baseprice;
										baseprice = SV.i_prices(iq.getPriceBand(margin.PriceBand))(currency);
										ret.Add(new IQ.clsPrice(baseprice, margin.Factor));
										//Return the Factored price from the SOURCE channel
										done.Add(SV);
									} else {
										// errormessages.Add("There is no (" & margin.PriceBand & ") price for " & sku)
									}
									//End If
								}
							}
						}
					}
				}
			}
		}

		return (ret);

	}


	//Public Function PriceVariants(sellerchannel As clsChannel, buyerchannel As clsChannel, currency As clsCurrency, Optional SKUVariant As clsVariant = Nothing) As List(Of clsPrice)

	//    'returns The Prices for all (or the specified) SKUVariant(s) the specified buyer, - at the correct margin/multiplier for the buyer/seller combo
	//    'will return an empty list if there is no price (POA)

	//    PriceVariants = New List(Of clsPrice)
	//    Dim margin As Single

	//    'NB: We fetch pricing from the sellerchannels 'isCloneOf' - which for non-clones, is itself.
	//    Dim sourceChannel As clsChannel = sellerchannel.IsCloneOf

	//    'If we're getting prices for a clone..
	//    'There's an important distinction between the SourceChannel and the SellerChannel - the sellerchannel (which may be a clone) carries the margin for the buyer..
	//    'whereas the sourceChannel (the clones 'parent') has the base price

	//    Dim done As List(Of clsVariant)
	//    done = New List(Of clsVariant)

	//    'First get SPECIFIC (overriding)prices
	//    If i_Prices.ContainsKey(sourceChannel) Then 'does the SOURCE channel seller have a (base) price for this product            
	//        For Each v In i_Prices(sourceChannel).Keys 'variants
	//            If v Is SKUVariant Or SKUVariant Is Nothing Then
	//                'there are *some* specific prices for this product - for this buyer (perhaps not for the right variants or currencies
	//                If i_Prices(sourceChannel)(v).ContainsKey(buyerchannel) Then
	//                    If i_Prices(sourceChannel)(v)(buyerchannel).ContainsKey(currency) Then
	//                        PriceVariants.Add(i_Prices(sourceChannel)(v)(buyerchannel)(currency))  'we've found a specifc price - to which we should NOT apply margin !
	//                        done.Add(v)
	//                    End If
	//                End If
	//            End If
	//        Next
	//    End If

	//    'for any remaining unpriced - do margin based pricing
	//    If sellerchannel.Margin.ContainsKey(buyerchannel) Then 'does this seller have ANY margin specified for this buyer
	//        If sellerchannel.Margin(buyerchannel).ContainsKey(Me.Sector) Then 'is there a margin specified for products within this sector (BU) .. PSG/ISS . . .
	//            If sellerchannel.Margin(buyerchannel)(Me.Sector).ContainsKey(Me.ProductType) Then  'does this seller have a margin specified for this products productType (within this sector)
	//                For Each SV In i_Prices(sourceChannel).Keys
	//                    If Not done.Contains(SV) Then
	//                        If SV Is SKUVariant Or SKUVariant Is Nothing Then  'was variantmatch
	//                            margin = sellerchannel.Margin(buyerchannel)(Me.Sector)(Me.ProductType).Factor
	//                            PriceVariants.Add(New clsPrice(i_Prices(sourceChannel)(SV)(sellerchannel)(currency), margin))  'Return the Factored price from the SOURCE channel
	//                            done.Add(SV)
	//                        End If
	//                    End If
	//                Next
	//            End If
	//        End If
	//    End If

	//    If PriceVariants.Count = 0 Then PriceVariants = Nothing

	//End Function

	//Private Function VariantMatch(skuvariant As clsVariant, matchWith As clsVariant) As Boolean

	//    VariantMatch = False
	//    If matchWith Is Nothing Then Return True
	//    If skuvariant Is matchWith Then Return True
	//End Function

	public List<ClsValidationMessage> getXtext(path, List<string> Acknowledged)
	{
		if (this.isFIO)
			return new List<ClsValidationMessage>();
		//Dont include messages from Pre-installed

		getXtext = new List<ClsValidationMessage>();

		if (this.i_Attributes_Code.ContainsKey("xText")) {
			int i;
			clsProductAttribute xtext__1;
			string showOnlyInFamilies = "";
			string hideInFamilies = "";

			string myfamily = LCase(Trim(findFamily(path)));

			for (i = 0; i <= this.i_Attributes_Code("xText").Count - 1; i++) {
				xtext__1 = this.i_Attributes_Code("xText")(i);

				if (this.i_Attributes_Code.ContainsKey("ShowF")) {
					showOnlyInFamilies = Trim(LCase(this.i_Attributes_Code("ShowF")(i).Translation.text(English)));
				}

				if (this.i_Attributes_Code.ContainsKey("HideF")) {
					hideInFamilies = Trim(LCase(this.i_Attributes_Code("HideF")(i).Translation.text(English)));
				}


				//Exit if this external text should NOT be visible
				if (showOnlyInFamilies != "" && !Split(showOnlyInFamilies, ",").Contains(myfamily))
					continue;
				if (hideInFamilies != "" && Split(hideInFamilies, ",").Contains(myfamily))
					continue;

				ImageButton ib = new ImageButton();
				ClsValidationMessage msg;

				msg = new ClsValidationMessage(enumValidationMessageType.Validation, Acknowledged != null && Acknowledged.Contains(path + "." + i) ? EnumValidationSeverity.greenTick : (EnumValidationSeverity)XText.NumericValue, iq.AddTranslation(this.sku + ":" + XText.Translation.text(English), English, "ISSU", 0, null, 0, false), iq.AddTranslation("Important Information", English, "ISSU", 0, null, 0, false), "", 0, 0, Split(""), "", path + "." + i);
				if (Acknowledged != null && Acknowledged.Contains(path + "." + i))
					msg.Acknowledged = true;
				getXtext.Add(msg);
			}

		}

	}
	public class clsSpecTableEntry
	{



		private string _title;
		public string Title {
			get {
				if ((Code != "hdd" & Code != "opt")) {
					if (Code2 != null && (Code2.Contains("SATA") | Code2.Contains("SAS"))) {
						return "Disk Controller";
					} else {
						return _title;
					}
				} else {
					return _title;
				}

				//   Return If(Code <> "hdd" AndAlso Code2 IsNot Nothing AndAlso (Code2.Contains("SATA") Or Code2.Contains("SAS")), "Disk Controller", _title)
			}
			set { _title = value; }
		}
		public clsTranslation Value {
			get { return m_Value; }
			set { m_Value = Value; }
		}
		private clsTranslation m_Value;
		public string Extra {
			get { return m_Extra; }
			set { m_Extra = Value; }
		}
		private string m_Extra;
		public string Code {
			get { return m_Code; }
			set { m_Code = Value; }
		}
		private string m_Code;
		public string Code2 {
			get { return m_Code2; }
			set { m_Code2 = Value; }
		}
		private string m_Code2;
		public int Max {
			get { return m_Max; }
			set { m_Max = Value; }
		}
		private int m_Max;
		public string Type {
			get { return m_Type; }
			set { m_Type = Value; }
		}
		private string m_Type;
		public string ProdType {
			get { return m_ProdType; }
			set { m_ProdType = Value; }
		}
		private string m_ProdType;
		public string[] Params {
			get { return m_Params; }
			set { m_Params = Value; }
		}
		private string[] m_Params;
		public Int32 Order {
			//This gives the combined order for preinstalled, attributes, etc
			get {
				switch (ProdType) {
					case "SVR":
					case "DTO":
					case "NBK":
						switch (Code.ToLower()) {
							case "mfrsku":
								return 10;
							case "formfactor":
								return 20;
							case "cpu":
								return Type == "pre" ? 30 : 0;
							case "mem":
								return Type != "slot" ? 40 : 0;
							case "graphics":
								return 50;
							case "display":
								return 60;
							case "networking":
								return 70;
							case "hdd":
								return 80;
							case "raid":
								return Type == "pre" ? 90 : 0;
							case "opt":
								return 100;
							case "pci":
								return 110;
							case "psu":
								return Type != "slot" ? 120 : 0;
							case "mgt":
								return 130;
							case "warrantycode":
								return 140;
							case "man3":
								return 150;
							case "software":
								return 155;
							case "document links":
								return 190;
							case "also included":
								return 160;
							case "options":
								return 180;
							default:
								return 0;
						}
					case "SWD":
						switch (Code.ToLower) {
							case "mfrsku":
								return 10;
							case "formfactor":
								return 20;
							case "ioc":
							case "cpu":
								if (Type == "pre")
									return Code2 != null && (Code2.Contains("SATA") | Code2.Contains("SAS")) ? 90 : 30;
								else
									return 0;
							case "mem":
								return Type != "slot" ? 40 : 0;
							case "networking":
								return 45;
							case "priconnectivity":
								return 50;
							case "management":
								return 40;
							case "poe":
								return 60;
							case "poepower":
								return 70;
							case "hdd":
								return 80;
							case "raid":
								return Type == "pre" ? 90 : 0;
							case "opt":
								return 100;
							case "pci":
								return 110;
							case "mgt":
								return 130;
							case "psu":
								return Type != "slot" ? 120 : 0;
							case "man3":
								return 150;
							case "warrantycode":
								return 140;
							case "software":
								return 155;
							case "document links":
								return 190;
							case "also included":
								return 160;
							case "options":
								return 180;
							default:
								return 0;
						}
					case "HPN":
						switch (Code.ToLower) {
							case "mfrsku":
								return 10;
							case "formfactor":
								return 20;
							case "ioc":
							case "cpu":
								if (Type == "pre")
									return Code2 != null && (Code2.Contains("SATA") | Code2.Contains("SAS")) ? 90 : 30;
								else
									return 0;
							case "mem":
								return Type != "slot" ? 40 : 0;
							case "priconnectivity":
								return 50;
							case "management":
								return 40;
							case "poe":
								return 60;
							case "poepower":
								return 70;
							case "upconnectivity":
								return 80;
							case "psu":
								return Type == "pre" ? 100 : 0;
							case "mgt":
								return 130;
							case "warrantycode":
								return 140;
							case "document links":
								return 190;
							case "also included":
								return 160;
							case "options":
								return 180;
							default:
								return 0;
						}
				}
			}
		}

		public clsSpecTableEntry()
		{
			Params = {
				
			};
		}
	}

	public Panel Spectable(clsLanguage language, List<clsQuantity> preinstalled, clsBranch branch, string sysPath, bool showall)
	{

		object SpectablePanel = new Panel();
		SpectablePanel.CssClass = "specTable";

		string familyName = string.Empty;
		if (branch.Parent != null)
			familyName = branch.Parent.Translation.text(English);

		List<clsSpecTableEntry> specTableProps = new List<clsSpecTableEntry>();

		IEnumerable<clsProductAttribute> orderedAttributeList = from v in this.Attributes.Valuesorderby v.Attribute.Order;

		//For Each a In Me.Attributes.Values
		//    If a.Attribute.Code.ToLower = "also included" Then
		//        Beep()
		//    End If
		//Next


		//This summarises the gives and takes slots accrooss the system, chassis, FIOs and all Preinstalled componentry -
		//so that we can render the 'Max 8' type slot info in the spec table
		allslots = branch.slots.Values.ToList;

		//Martins OLD code
		//allslots = branch.slots.values.Union(branch.childBranches.
		// SelectMany(Function(s) s.Value.slots.Values.
		//   Select(Function(ppp) New clsSlot() With {.path = ppp.path, .numSlots = ppp.numSlots, .Type = ppp.Type})))
		//.Union(preinstalled.SelectMany(Function(p) p.Branch.slots.
		//Select(Function(ppp) New clsSlot() With {.path = ppp.Value.path, .numSlots = p.NumPreInstalled * ppp.Value.numSlots, .Type = ppp.Value.Type}
		//).Where(Function(sl) String.IsNullOrEmpty(sl.path) OrElse sl.path.Contains(path)))) 'Need to consider path in this stuff? yes you do ML


		//The systems child branches includde the chassis branch (which carries slots accross many common systems) 
		//and the FIOs branch which carries a number of 'fake' parts
		foreach ( b in branch.childBranches.Values) {
			if (!b.deleted) {
				if (b.EnglishName.ToLower.Contains("fios"))
					System.Diagnostics.Debugger.Break();
				//contains the ###iO controlers, ###CPUS etc
				if (b.EnglishName.ToLower.Contains("chassis") & b.Hidden) {
					foreach ( slot in b.slots.Values) {
						if (!slot.deleted) {
							//Presinstalled contains some quantities which DO NOT apply (wrong paths)
							if (slot.path.Contains(sysPath) | slot.path == "") {
								allslots.Add(slot);
							}
						}
					}
					break; // TODO: might not be correct. Was : Exit For
					//there should be only 1 chassis branch
				}
			}
		}

		//preinstalled component (clsQuantities)
		foreach ( pic in preinstalled) {
			if (pic.Path == "" || pic.Path.StartsWith(sysPath)) {
				//Dim bn As String = pic.Branch.DisplayName(English)
				//Dim picProd As String = pic.Branch.Product.i_Attributes_Code("desc")(0).Translation.text(English)
				//Dim sp As String = PathName(path) 'sys path
				//Dim p As String = PathName(pic.Path)
				foreach ( picSlot in pic.Branch.slots.Values) {
					if (!picSlot.deleted) {
						//  If picSlot.Type.MajorCode = "MEM" Then Stop
						//Presinstalled contains some quantities which DO NOT apply (wrong paths)
						if (picSlot.path.StartsWith(sysPath) | picSlot.path == "") {
							clsSlot slt = new clsSlot();
							slt.path = pic.Path;
							slt.numSlots = pic.NumPreInstalled * picSlot.numSlots;
							slt.Type = picSlot.Type;
							allslots.Add(slt);
						}
					}
				}
				//Else
				//Beep()
			}
		}
		/// end of 'allslots' creation


		//Add all attributes
		//specTableProps.AddRange(orderedAttributeList.Where(Function(oal) oal.Attribute.Order And oal.deleted = False > 0).Select(Function(v) New clsSpecTableEntry() With {.ProdType = Me.ProductType.Code, .Type = "atr", .Code = v.Attribute.Code, .Title = v.Attribute.displayName(language), .Value = iq.AddTranslation(If(v.Attribute.Code.ToLower() = "formfactor" AndAlso orderedAttributeList.Where(Function(at) at.Attribute.Code = "U").Count() > 0, v.displayNameNoCode(language) + " (" + orderedAttributeList.Where(Function(at) at.Attribute.Code = "U").First().NumericValue.ToString() + "U)", v.displayNameNoCode(language)), English, "", 0, Nothing, 0, False)})) ' horrid hack to put U after form factor
		specTableProps.AddRange(orderedAttributeList.Where(oal => oal.deleted == false).Select(v => new clsSpecTableEntry {
			ProdType = this.ProductType.Code,
			Type = "atr",
			Code = v.Attribute.Code,
			Title = v.Attribute.displayName(language),
			Value = iq.AddTranslation(v.Attribute.Code.ToLower() == "formfactor" && orderedAttributeList.Where(at => at.Attribute.Code == "U").Count() > 0 ? v.displayNameNoCode(language) + " (" + orderedAttributeList.Where(at => at.Attribute.Code == "U").First().NumericValue.ToString() + "U)" : v.displayNameNoCode(language), English, "", 0, null, 0, false)
		}));
		// horrid hack to put U after form factor

		//Add all preinstalled products
		foreach ( p in preinstalled.Where(pi => pi.FOC).GroupBy(pi => pi.Branch)) {
			string productDisplay = string.Empty;
			if (p.Key.Product.i_Attributes_Code.ContainsKey("Name")) {
				object productName = p.Key.Product.i_Attributes_Code("Name")(0).Translation.text(s_lang);
				productDisplay = productName;
			} else if (p.Key.Product.i_Attributes_Code.ContainsKey("Description")) {
				productDisplay = p.Key.Product.i_Attributes_Code("Description")(0).Translation.text(s_lang);
			} else if (p.Key.Product.i_Attributes_Code.ContainsKey("Desc")) {
				productDisplay = p.Key.Product.i_Attributes_Code("Desc")(0).Translation.text(s_lang);
			} else {
				productDisplay = p.Key.Translation.text(s_lang);
			}
			//do we have a max slot?
			// Dim test =AllSlots.Where(Function(x) x.slotNum)
			object slo = allslots.Where(als => als.deleted == false && p.Key.Product.ProductType.Code.ToLower() == als.Type.MajorCode.ToLower() && als.numSlots > 0).Sum(s => s.numSlots);

			//Horrrid if statement below as Raid controllers are embeded devices, this is going to change according to Paul so this is temp, yay!
			clsSpecTableEntry specEntry = new clsSpecTableEntry();

			specEntry.ProdType = this.ProductType.Code;

			if (p.Key.Product.i_Attributes_Code.ContainsKey("technology")) {
				specEntry.Code2 = p.Key.Product.i_Attributes_Code("technology").First.Translation.text(English);
			} else {
				specEntry.Code2 = null;
			}

			specEntry.Type = "pre";

			if (p.Key.Product.i_Attributes_Code.ContainsKey("optFamily") && p.Key.Product.i_Attributes_Code("optFamily")(0).Translation.text(English).ToUpper == "RAID_CONTROLLERS") {
				specEntry.Code = "RAID";
				specEntry.Title = "RAID Controller";
			} else {
				specEntry.Code = (p.Key.Product.ProductType.Code);
				specEntry.Title = p.Key.Product.ProductType.Translation.text(language);
			}


			specEntry.Value = iq.AddTranslation("{0} x {1}", English, "SpecValues", 0, null, 0, false);
			if (specEntry.Code != "ioc") {
				specEntry.Max = slo;
			}
			specEntry.Params = {
				p.Sum(pp => pp.NumPreInstalled).ToString,
				productDisplay
			};

			//   specTableProps.Add(New clsSpecTableEntry() With {.ProdType = Me.ProductType.Code, .Code2 = If(p.Key.Product.i_Attributes_Code.ContainsKey("technology"), p.Key.Product.i_Attributes_Code("technology").First.Translation.text(English), Nothing), .Type = "pre", .Code = If(p.Key.Product.i_Attributes_Code.ContainsKey("optFamily") AndAlso p.Key.Product.i_Attributes_Code("optFamily")(0).Translation.text(English).ToUpper = "RAID_CONTROLLERS", "RAID", p.Key.Product.ProductType.Code), .Title = If(p.Key.Product.i_Attributes_Code.ContainsKey("optFamily") AndAlso p.Key.Product.i_Attributes_Code("optFamily")(0).Translation.text(English).ToUpper = "RAID_CONTROLLERS", "RAID Controller", p.Key.Product.ProductType.Translation.text(language)), .Value = iq.AddTranslation("{0} x {1}", English, "SpecValues", 0, Nothing, 0, False), .Max = slo, .Params = {p.Sum(Function(pp) pp.NumPreInstalled).ToString, productDisplay}})
			specTableProps.Add(specEntry);
		}

		if ({
			"SVR",
			"SWD"
		}.Contains(this.ProductType.Code)) {
			//Populate Management row, do we have an insight or oneview licence here?
			string t2 = "No OneView or Insight Control";
			string t1 = "No Licence";

			if (preinstalled.Where(pi => pi.Branch.Product.ProductType.Code.ToLower() == "man1").Count() > 0) {
				object pr = preinstalled.Where(pi => pi.Branch.Product.ProductType.Code.ToLower() == "man1").FirstOrDefault();
				if (pr.IsAutoAdd) {
					t2 = pr.Branch.Product.i_Attributes_Code("desc")(0).Translation.text(language);
					string includedMajorCode = "ILO MAN1";
					if (pr.Branch.slots.Where(sl => includedMajorCode.Contains(sl.Value.Type.MajorCode)).Count() > 0)
						t1 = "Advanced";
				}

			}
			string sts = this.i_Attributes_Code.ContainsKey("ILOHARDWARE") ? "{0} ({1}) / {2}" : "No Management";
			clsSpecTableEntry specEntry = new clsSpecTableEntry();


			 // ERROR: Not supported in C#: WithStatement

			specTableProps.Add(new clsSpecTableEntry {
				ProdType = this.ProductType.Code,
				Type = "pre",
				Code = "MGT",
				Title = "Management",
				Value = iq.AddTranslation(sts, English, "", 0, null, 0, false),
				Params = this.i_Attributes_Code.ContainsKey("ILOHARDWARE") ? {
					this.i_Attributes_Code("ILOHARDWARE")(0).Translation.text(language),
					t1,
					t2
				} : {
					
				}
			});
		}

		if ({
			"SVR",
			"NBK",
			"DTO",
			"SWD"
		}.Contains(this.ProductType.Code)) {
			//Special case for HDD and OPT as we need a line when they are NOT present
			object allHDDslots = from p in preinstalledwhere p.Branch.Product.ProductType.Code.ToUpper() == "HDD";
			if (allHDDslots.Count > 0) {
				object preinstalledHDD = from p in allHDDslotswhere p.NumPreInstalled > 0;
				if (preinstalledHDD.Count == 0) {
					specTableProps.Add(new clsSpecTableEntry {
						ProdType = this.ProductType.Code,
						Type = "pre",
						Code = "HDD",
						Title = "Hard Disk Drive",
						Value = iq.AddTranslation("None Installed", English, "", 0, null, 0, false),
						Max = allslots.Where(als => als.deleted == false && "hdd" == als.Type.MajorCode.ToLower() && als.numSlots > 0).Sum(s => s.numSlots)
					});
				}
			}

			if (preinstalled.Where(p => p.Branch.Product.ProductType.Code.ToUpper() == "HDD").Count() == 0)
				specTableProps.Add(new clsSpecTableEntry {
					ProdType = this.ProductType.Code,
					Type = "pre",
					Code = "HDD",
					Title = "Hard Disk Drive",
					Value = iq.AddTranslation("None Installed", English, "", 0, null, 0, false),
					Max = allslots.Where(als => "hdd" == als.Type.MajorCode.ToLower() && als.deleted == false && als.numSlots > 0).Sum(s => s.numSlots)
				});
			if (preinstalled.Where(p => p.Branch.Product.ProductType.Code.ToUpper() == "OPT").Count() == 0)
				specTableProps.Add(new clsSpecTableEntry {
					ProdType = this.ProductType.Code,
					Type = "pre",
					Code = "OPT",
					Title = "Optical Drive",
					Value = iq.AddTranslation("None Installed", English, "", 0, null, 0, false),
					Max = allslots.Where(als => "opt" == als.Type.MajorCode.ToLower() && als.deleted == false && als.numSlots > 0).Sum(s => s.numSlots)
				});
		}

		//Add a slot summary for PCI, grouped by the short description of the slot type
		object listofInterfaceCards = {
			"PCIF",
			"PCIC",
			"PCID",
			"PCIE",
			"PCIG",
			"PCIX",
			"PCI",
			"MODA",
			"MODL",
			"MODI",
			"MODB",
			"RISER",
			"MODE",
			"MODM"
		};
		object excludefromsummary = {
			"MODM",
			"MODE",
			"MODL",
			"MODI",
			"RISER"
		};
		object d = string.Join("<br>", branch.slots.Values.Union(branch.childBranches.SelectMany(s => s.Value.slots.Values)).Where(s => listofInterfaceCards.Contains(s.Type.MajorCode.ToUpper) & s.numSlots > 0).OrderBy(sl => IIf(sl.HasSlotNum, sl.slotNum.value, 200)).Select(s => string.Format("{0}: {1}", s.HasSlotNum ? Chr(64 + s.slotNum.value) : "", s.Type.MajorCode.ToUpper.StartsWith("MOD") ? s.Type.shortDisplayName(language) : s.Type.Translation.text(language))).ToList());


		//branch.slots.Values.Union(branch.childBranches.SelectMany(Function(s) s.Value.slots.Values)). _
		//Where(Function(s) listofInterfaceCards.Contains(s.Type.MajorCode.ToUpper) _
		//          And s.numSlots > 0 _
		//          AndAlso s.slotNum.value IsNot Nothing _
		//          AndAlso Not IsDBNull(s.slotNum.value)). _
		//          OrderBy( _
		//              Function(sl) _
		//              IIf(sl.slotNum.value Is Nothing OrElse IsDBNull(sl.slotNum.value), 200, sl.slotNum.value) _
		//              ) _
		//          .Select(Function(s) String.Format("{0}: {1}", _
		//                IIf(s.slotNum IsNot Nothing AndAlso s.slotNum.value IsNot Nothing AndAlso Not IsDBNull(s.slotNum.value), _
		//                    Chr(64 + s.slotNum.value), ""), _
		//                    IIf(s.Type.MajorCode.ToUpper.StartsWith("MOD"), _
		//                        s.Type.shortDisplayName(language), _
		//                        s.Type.Translation.text(language) _
		//    ))).ToList())




		object cn = 0;
		string op = "";
		List<string> @params = new List<string>();

		branch.slots.Values.Union(branch.childBranches.SelectMany(s => s.Value.slots.Values)).Where(s => listofInterfaceCards.Contains(s.Type.MajorCode) && !excludefromsummary.Contains(s.Type.MajorCode) && s.numSlots > 0).GroupBy(s => s.Type.shortDisplayName(language)).Select(s =>
		{
			op += " {" + cn + "}: {" + cn + 1 + "}";
			cn = cn + 2;
			@params.Add(s.Key);
			@params.Add(s.Sum(sss => sss.numSlots));
		}).ToList();

		if (@params.Count > 0)
			specTableProps.Add(new clsSpecTableEntry {
				ProdType = this.ProductType.Code,
				Type = "pre",
				Code = "PCI",
				Title = "Interface Slots",
				Value = iq.AddTranslation(op, English, "", 0, null, 0, false),
				Extra = d,
				Params = @params.ToArray
			});

		//Add any information pertinent to slots, so anything which gives a slot to the system and isn't otherwise already added
		foreach ( slot in branch.slots.Values.Union(branch.childBranches.SelectMany(s => s.Value.slots.Values)).Where(s => !listofInterfaceCards.Contains(s.Type.MajorCode) & s.numSlots > 0)) {
			if (slot.path == sysPath | slot.path == "") {
				if (slot.Type.MajorCode == "RJ45") {
					object spt = specTableProps.Where(stp => stp.Type == "atr" && stp.Code == "PriConnectivity").FirstOrDefault();
					if (spt != null) {
						spt.Max = slot.numSlots;
					}
				}
				specTableProps.Add(new clsSpecTableEntry {
					ProdType = this.ProductType.Code,
					Type = "slot",
					Code = slot.Type.MajorCode,
					Title = Xlt(slotMajorTranslations(slot.Type.MajorCode), language),
					Value = slot.Type.Translation
				});
			}
		}

		//Render all of the above in a predefined order, nasty way of ordering needs to go in the db somewhere but for now hardcoded in the object

		foreach ( sp in specTableProps.OrderBy(s => s.Order)) {
			if (sp.Order > 0 | showall) {
				object l = null;
				if (sp.Code.ToUpper() == "PCI") {
					l = NewLit("<div style='display:none;' id='" + sysPath + "." + "ttPCIslots'>" + sp.Extra + "</div><span onclick=\"TagToTip('" + sysPath + "." + "ttPCIslots', TITLE, 'Interface Card Slots', CLICKSTICKY, true, CLICKCLOSE, false, CLOSEBTN, true, COPYCONTENT, false, DELAY, 400, BORDERWIDTH, 1, BORDERCOLOR, '#2F7BD1', PADDING, 2, FIX, [624, 372])\" style='cursor:help;float:right;height:15px;width:15px;'><img src='../images/Navigation/ICON_CIRCLE_info.png'/></span>");
				}
				//Ok another hacky bit, would like to put a param in the DB to say if the field might contain a part number
				//Scan for Part Numbers
				if (sp.Code.ToUpper != "MFRSKU") {
					string newValue = "";
					@params = new List<string>();
					object vc = 0;
					foreach ( spl in sp.Value.text(English).Split(",")) {
						if (iq.i_SKU.ContainsKey(spl)) {
							newValue += "{" + vc + "} , ";
							@params.Add(iq.i_SKU(spl).DisplayName(language));
							vc = vc + 1;
						}
					}
					if (!string.IsNullOrEmpty(newValue)) {
						sp.Value = iq.AddTranslation(Left(newValue, Len(newValue) - 2), English, "", 0, null, 0, false);
						sp.Params = @params.ToArray;
					}

				}

				specTableRow(IIf(sp.Order == 0, "HIDDEN ", "") + Xlt(sp.Title, language), Replace(Replace(string.Format(sp.Value.text(language), sp.Params), familyName + " ", ""), "HP ", ""), SpectablePanel, sp.Max, l, language);

			} else {
				string ss = sp.Title + ":" + sp.Value.text(language);

			}


		}

		return SpectablePanel;
	}
	private void specTableRow(string headerText, string valueText, ref Panel specTable, int max, Literal xtra, clsLanguage language)
	{
		Panel p = new Panel();
		p.CssClass = "specRow";
		specTable.Controls.Add(p);

		object panel = new Panel();
		panel.CssClass = "specLeft";
		Literal lbl = new Literal();
		lbl.Mode = LiteralMode.Transform;
		lbl.Text = headerText;
		panel.Controls.Add(lbl);
		p.Controls.Add(panel);

		panel = new Panel();
		panel.CssClass = "specRight";
		lbl = new Literal();
		lbl.Mode = LiteralMode.Transform;
		lbl.Text = valueText != null ? valueText.Replace("&NBSP;", "&nbsp;") : "";
		panel.Controls.Add(lbl);
		if (max != 0) {
			object panel2 = new Panel();
			panel2.CssClass = "specRightMax";
			object lbl2 = new Literal();
			lbl2.Text = Xlt("max", language) + ": " + max.ToString();
			panel2.Controls.Add(lbl2);
			panel.Controls.Add(panel2);
		}

		if (xtra != null)
			panel.Controls.Add(xtra);

		p.Controls.Add(panel);

		panel = new Panel();
		panel.CssClass = "specBreak";
		p.Controls.Add(panel);
		lbl = new Literal();
		lbl.Text = "&nbsp;";
	}

	private string slotMajorTranslations(string type)
	{
		switch (type) {
			case "HDD":
				return "Disk Storage Backplane";
			case "PCI":
				return "Interface Card Slots";
			case "FAN":
				return "Fan Slots";
			case "MEM":
				return "Memory Slots";
			case "OPT":
				return "Optical Slots";
			case "PSU":
				return "Power Supply Slots";
		}
		return type;
	}

	protected override void Finalize()
	{
		base.Finalize();
	}

	public bool hasPromo(string promoCode, clsRegion region)
	{
		if (!Promos.ContainsKey(promoCode))
			return false;
		foreach ( p in Promos(promoCode)) {
			if (p.Encompasses(region))
				return true;
		}
		return false;
	}

	// Returns whether this product has a valid list price
	public bool HasListPrice(clsAccount buyerAccount)
	{

		return ListPrice(buyerAccount) != null;

	}

	public bool isFakePart()
	{
		if (!this.hasSKU)
			return true;
		if (this.SKU.StartsWith("###"))
			return true;
		return false;
	}
}


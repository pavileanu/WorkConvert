using dataAccess;



public class clsProduct : i_Editable
{


    public int ID { get; set; }
    //For each product - each seller offers prices to each buyer in each currency
    //Public pricekeys As Dictionary(Of Integer, Dictionary(Of Integer, Dictionary(Of Integer, Single)))

    public clsSector Sector { get; set; }
    public clsProductType ProductType { get; set; }
    public HashSet<clsBranch> Branches { get; set; } //Dictionary(Of Integer, clsBranch) = New Dictionary(Of Integer, clsBranch)

    //this has been  made private so as to force acces to prices via the clearer Baseprice(),NullablePrice() and listprice() functions
    //these are the base prices - the channels (not the accounts) contain margins
    //                                                     seller                  null/buyer
    //Private i_Prices As Dictionary(Of clsChannel, Dictionary(Of clsChannel, Dictionary(Of clsCurrency, Dictionary(Of clsVariant, clsPrice))))
    //Private i_Prices As Dictionary(Of clsChannel, Dictionary(Of clsVariant, Dictionary(Of clsChannel, Dictionary(Of clsCurrency, clsPrice))))

    //Seller,Variant,ArrivalDate,Stock(contains variant)
    //Public i_Stock As Dictionary(Of clsChannel, Dictionary(Of clsVariant, SortedDictionary(Of Date, clsStock)))

    //  Property stock As Dictionary(Of Integer, clsStock)  'it (still) makes sense to have'global' stock and price held in the product
    //Property prices As Dictionary(Of Integer, clsPrice)
    public DateTime activeFrom { get; set; } //Product will only display between these dates
    public DateTime activeTo { get; set; } //
    public bool Active { get; set; } //Wether the product shows (at all) or not
    public bool EOL { get; set; } //End of life - product will only show if it has stock
    public bool Publish { get; set; } //Only admin users see unpublished products
    public Dictionary<int, ClsAvalancheOPG> AvalancheOPGs { get; set; }
    public Dictionary<int, clsFlexLine> OPGflexLines { get; set; }
    public Dictionary<int, clsVariant> Variants { get; set; } //a flat dictionary referencing the variants - to allow editing (would be nicer if the generic edito could evaluate more complex paths and edit lists (aswell as dictionaries)
    public Dictionary<int, clsBundle> Bundles; //this isn't a property - so it's not exposed in the editor we edit iq.bundles
    public Dictionary<clsScheme, int> Points; // how many points does this product have under each scheme
    public Dictionary<string, List<clsRegion>> Promos { get; set; }

    //New for HP Split
    public string mfrCode { get; set; }
    public string plCode { get; set; }
    public string buCode { get; set; }
    public string SKU { get; set; }


    //                                           seller
    //' <summary>Provides access to a List of sellerChannel specific variants of the product (which in turn have prices) </summary>
    public Dictionary<clsChannel, List<clsVariant>> i_Variants;

    //Geographical visibility is controlled by Quantity records - which relate products to regions... see also

    public Dictionary<int, clsProductAttribute> Attributes { get; set; } // this is a 'flattened' dictionary for the editor (a product can have more than one attribute of the same type - i_Attributes_code groups them by type and makes thank indexable (e.g. SKU(0))
    //This MUST be a property as we use reflection to access productattributes in clsFields defined in a clsScreen (for the editor/matrix views)
    public Dictionary<string, List<clsProductAttribute>> i_Attributes_Code { get; set; } //An index of the attributes by code, - NOTE: because we can have more than one attribute of the same type (eg, more than one xText, or description) this is an index to a LIST of attributes
    private bool _isSystem;
    private bool _isOption;
    public bool isDeleted { get; set; }

    /// <summary>Returns the HP/Everyone Price of the Variant that matches the buyerAccounts Region and Currency</summary>
    public clsPrice ListPrice(clsAccount buyeraccount)
    {
        clsPrice returnValue = default(clsPrice);

        returnValue = null;
        clsPriceBand hplist = iq.getPriceBand(""); //Hplist -dosnt need a 'special' band - becuase it's the 'everyone' band on the HP sellerChannel

        if (this.i_Variants != null)
        {
            if (this.i_Variants.ContainsKey(hp))
            {
                foreach (var v in this.i_Variants[HP]) //Not wildly happy with this - walking over (say) 50 list prices pre product could be slow
                {
                    //Making i_variants a compound key SellerChannel|Region would be much faster
                    if (v.Region.Code != "US")
                    {
                        int a = 0;
                    }
                    if (v.Region == buyeraccount.SellerChannel.Region) //Base LIST PRICES on SELLERS REGION (not buyers) - ultimately the account should have a region
                    {
                        if (v.i_prices.ContainsKey(hplist))
                        {
                            if (v.i_prices(hplist).ContainsKey(buyeraccount.Currency))
                            {
                                returnValue = v.i_prices(hplist)[buyeraccount.Currency];
                                return returnValue;
                            }
                        }
                    }
                }
            }
        }
        //ListPrice = Me.prices(Everyone)(buyeraccount.Currency)

        return returnValue;
    }

    public dynamic clone(string newsku)
    {

        clsProduct with_1 = this;
        return new clsProduct(newsku, with_1.get_isSystem(), with_1.isOption, with_1.Sector, with_1.ProductType, with_1.activeFrom, with_1.activeTo, with_1.Active, with_1.EOL, with_1.Publish, with_1.mfrCode, with_1.buCode, with_1.plCode, null, -1);

    }

    public string FirstAttributeEnglishText(object code)
    {

        if (this.i_Attributes_Code.ContainsKey(code))
        {
            return this.i_Attributes_Code(code)[0].Translation.text(English);
        }

        return "";

    }

    public bool isFIO
    {

        //NOT to be confused with preinstalled parts !!

        //FIOs (Factory Installed Options) - ahve part numbers - but can't be 'bought' - and therefore be flexed
        //(in practise there is probably an equevilent (often identical) part - but it has a different (and unknwon to us) SKU

        get
        {
            if (this.i_Attributes_Code.ContainsKey("focus"))
            {
                foreach (var f in this.i_Attributes_Code("focus"))
                {
                    if (f.Translation.text(English) == ("FIO"))
                    {
                        return true;
                    }
                }
            }
        }

    }

    public bool get_isSystem(string path = "")
    {
        if (path != "" && _isSystem)
        {
            if (path.Split('.').Length < 6)
            {
                return _isSystem;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return _isSystem;
        }

    }
    public void set_isSystem(string path, bool value)
    {
        _isSystem = value;
    }

    public bool isOption
    {
        get
        {
            return _isOption;
        }
        set
        {
            _isOption = value;
        }
    }

    public Manufacturer Manufacturer
    {

        get
        {
            Manufacturer returnValue = default(Manufacturer);

            returnValue = returnValue.Unknown;

            if (string.IsNullOrEmpty(this.mfrCode))
            {

                // No mfrCode found - make the decsion based on product types
                if (this.get_isSystem())
                {
                    if (this.ProductType.Code == "DTO" || this.ProductType.Code == "NBK")
                    {
                        returnValue = returnValue.HPI;
                    }
                    else
                    {
                        returnValue = returnValue.HPE;
                    }
                }

            }
            else
            {

                // mfrCode set up - use it to work out the manufacturer
                if (string.Equals(this.mfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase))
                {
                    returnValue = returnValue.HPI;
                }
                else if (string.Equals(this.mfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase))
                {
                    returnValue = returnValue.HPE;
                }

            }

            return returnValue;
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

        if (Strings.Left(System.Convert.ToString(sellerchannel.Code), 3) == "MHP")
        {
            return true; //Temporary hack becuase otherwise you cannot flex up in Universal
        }

        if (this.i_Variants == null)
        {
            return false; //*Nobody* stocks (or has a price for me)
        }
        if (!this.i_Variants.ContainsKey(sellerchannel))
        {
            return false;
        }
        return true;

    }


    public clsVariant MatchingVariant(clsVariant MatchWith, clsChannel sellerchannel)
    {
        clsVariant returnValue = default(clsVariant);

        //looks thtough all the variants on this Product ie. me
        //for the one which most closely matches 'matchwith'

        //    Product.matchingvariant(quoteItem.SKUVariant, buyerAccount.SellerChannel)

        //niave - but clear - hopefully

        returnValue = null;

        int bestscore = 0;
        int score = 0;
        clsChannel channel = default(clsChannel);

        if (this.i_Variants != null)
        {

            if (this.i_Variants.ContainsKey(sellerchannel))
            {
                channel = sellerchannel;
            }
            else
            {
                channel = HP;
            }

            if (this.i_Variants.ContainsKey(channel))
            {
                foreach (var v in this.i_Variants[channel])
                {
                    score = 0;
                    if (v.Warehouse == MatchWith.Warehouse)
                    {
                        score++;
                    }
                    if (v.Localisation == MatchWith.Localisation)
                    {
                        score++;
                    }
                    if (v.Code == MatchWith.Code)
                    {
                        score++;
                    }
                    if (v.Region == MatchWith.Region)
                    {
                        score++;
                    }
                    if (score > bestscore)
                    {
                        returnValue = v;
                    }
                    bestscore = score;
                }
            }

        }

        return returnValue;
    }


    public clsProduct clone()
    {
        clsProduct returnValue = default(clsProduct);

        returnValue = new clsProduct(this.SKU, this.get_isSystem(), this.isOption, this.Sector, this.ProductType, this.activeFrom, this.activeTo, this.Active, this.EOL, this.Publish, this.mfrCode, this.buCode, this.plCode, null, -1);

        //clone the HP variant(s)  (which internally clone the prices)
        clsVariant cv;

        if (this.i_Variants.ContainsKey(HP))
        {
            foreach (var v in this.i_Variants[HP])
            {
                cv = v.clone(returnValue);
            }
        }

        clsProductAttribute pa = default(clsProductAttribute);
        foreach (clsProductAttribute tempLoopVar_pa in this.Attributes.Values)
        {
            pa = tempLoopVar_pa;
            pa.Clone(returnValue); //clone my product attributes onto the new (cloned) product
        }


        return returnValue;
    }

    public bool anyStock(clsChannel SellerChannel)
    {
        bool returnValue = false;

        //returns wether there is any present or future stock for any variant
        returnValue = false;

        //Each variant will have many shipments - any before 'now' are absolute stock values at that date and should have current = false
        //there should only be 1 after 'now' who's 'current=true' - others after now are (relative) shipments
        if (this.i_Variants.ContainsKey(SellerChannel))
        {
            foreach (var v in this.i_Variants[SellerChannel])
            {
                foreach (var s in v.shipments.Values)
                {
                    if (s.IsCurrent || s.Arrival > DateTime.Now)
                    {
                        if (s.quantity > 0)
                        {
                            return true; //yey ! - some stock
                        }
                    }
                }
            }
        }

        return returnValue;
    }

    public clsProduct()
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        Branches = new HashSet<clsBranch>();


        AvalancheOPGs = new Dictionary<int, ClsAvalancheOPG>();
        OPGflexLines = new Dictionary<int, clsFlexLine>();
        this.i_Variants = new Dictionary<clsChannel, List<clsVariant>>();
        this.Variants = new Dictionary<int, clsVariant>();
        this.Points = new Dictionary<clsScheme, int>(); //number of points this product is worth under each scheme
        this.Promos = new Dictionary<string, List<clsRegion>>();
        this.i_Variants = new Dictionary<clsChannel, List<clsVariant>>();
    }

    public dynamic Insert(ref List<string> errorMessages)
    {

        return new clsProduct(this.SKU, this.get_isSystem(), this.isOption, this.Sector, this.ProductType, this.activeFrom, this.activeTo, this.Active, this.EOL, this.Publish, this.mfrCode, this.buCode, this.plCode, null, -1);

    }

    public void update(ref List<string> errorMessages)
    {

        if (this.ID == 0)
        {
            Debugger.Break();
        }

        object sql = null;
        sql = "UPDATE [Product] SET sku=\'" + this.SKU + "\', isSystem=" + System.Convert.ToString(this.get_isSystem() ? 1 : 0) + ", isOption=" + System.Convert.ToString(this.isOption ? 1 : 0) + ",fk_producttype_id=" + this.ProductType.ID + ",fk_sector_id=" + this.Sector.ID;
        sql += ",activefrom=" + da.UniversalDate(this.activeFrom) + ",activeTo=" + da.UniversalDate(this.activeTo) + ",active=" + System.Convert.ToString(this.Active ? 1 : 0) + ",eol=" + System.Convert.ToString(this.EOL ? 1 : 0) + ",publish=" + System.Convert.ToString(this.Publish ? 1 : 0);
        sql += ",mfrCode=" + da.SqlEncode(this.mfrCode) + ",buCode=" + da.SqlEncode(this.buCode) + ", deleted=" + System.Convert.ToString(this.isDeleted ? 1 : 0) + ",plCode=" + da.SqlEncode(this.plCode);
        sql += " WHERE ID=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql);

    }

    public void delete(ref List<string> errorMessages)
    {

        try
        {
            da.DBExecutesql("DELETE  FROM PRODUCT WHERE ID=" + System.Convert.ToString(this.ID)); //will often fail due to RI (expose this error through the editor)
            iq.i_SKU.Remove(this.SKU);
            iq.Products.Remove(this.ID);

        }
        catch (Exception ex)
        {
            errorMessages.Add(ex.Message.ToString());

        }

    }

    public string DisplayName(clsLanguage clsLanguage, bool StripSKU = false)
    {
        string returnValue = "";
        //Show the description - falling back to SKU (if absent)

        returnValue = this.SKU;

        if (this.i_Attributes_Code.ContainsKey("desc"))
        {
            returnValue = (!StripSKU ? this.SKU + " - " : "") + this.i_Attributes_Code("desc")[0].Translation.text(English);

        }
        return returnValue;
    }
    public string displayName(clsLanguage clsLanguage)
    {

        return DisplayName(clsLanguage, false);

    }


    public bool hasSKU()
    {
        bool returnValue = false;

        //Returns true if the product has a 'real' (non ### SKU)

        returnValue = false;
        if (this.SKU != "")
        {
            return true;
        }

        return false;

        //this is obsolete as product will have their sku set at load time (it if is empty),
        // from the product attribute if it is present.

        //		if (this.SKU != "")
        //		{
        //			object sku;
        //			sku = this.SKU;
        //If Left$(sku$, 3) <> "###" Then
        //End If
        //			return true;
        //			}

        return returnValue;
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

    public List<clsPrice> GetPrices(clsAccount Buyeraccount, int PriceConfig, clsVariant SKUvariant, List<string> errormessages, bool callWebservice)
    {
        List<clsPrice> returnValue = default(List<clsPrice>);

        returnValue = new List<clsPrice>(); //fail safe (return SOMETHING)
        if (SKUvariant == null)
        {
            errormessages.Add("* SkuVariant was Nothing in getprices) - Use iq.allvariant");
        }
        return returnValue;

        //Called form StalePrices()
        //Returns a list of prices for the buyer account based on the PriceConfig of the sellerchannel
        //Pricing may be customer specific,margin based, may queue a webservice based price/stock update or may be HP list price for the buyeraccounts region.
        //Pricing will only be of one type for a single call - ie. it will not return List and CustomerSpecific prices in the same list

        //an empty list is returned if the are no prices - getPrices never returns Nothing

        //PriceConfig  AND 8 Inclide customer specific prices
        //PriceConfig  AND 4 Use Price bands
        //PriceConfig  AND 2 Show List price (in the absence of any other price)
        //PriceConfig  AND 1 Show POA (for products for which we have no price (as opposed to not showing the product at all if there is no price at all)

        //			object sku;
        //			sku = this.SKU;

        //			List<clsPrice> SpecificPrices = new List<clsPrice>();
        //			List<clsPrice> BasePrices = new List<clsPrice>();
        //			List<clsPrice> ListPrices = new List<clsPrice>();

        //returns customer specific prices - Includng potenitally outstanding web request  POA 'Requesting prices' prices
        //also does margin based pricing ! - note, you can't add a margin to a webservice price
        //			SpecificPrices = this.Prices(Buyeraccount, SKUvariant);

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
        //			if (Strings.Left(System.Convert.ToString(Buyeraccount.SellerChannel.Code), 3) == "MHP")
        //			{
        //				PriceConfig = PriceConfig & !8 != 0; //HP (universal instances)  dont have a webservice (temporary hack)
        //				}

        //				if ((PriceConfig & 8) && callWebservice) //And (PriceConfig And 16) Then
        //				{
        //we're using a webservice.. and there was no specific price YET..
        //IF this poduct is in the feed (we have a variant) return a 'pending' price (and DO show the product)


        //BRAZIL (see clsAccount.warehousefilter)
        //					if (Buyeraccount.wareHouseFilter.ToUpper != "NONE")
        //					{
        //						if (this.inFeed(Buyeraccount.SellerChannel.IsCloneOf)) //ChannelSKU(Me, SKUvariant, Buyeraccount.SellerChannel) <> "" Then
        //						{
        //							List<clsPrice> poa = default(List<clsPrice>);
        //							poa = new List<clsPrice>();

        //will make a new price (for every variant!) - A COPY of the HP list price  ''storing a POA, until the webservice returns a real price (for the first time)

        //							clsPrice aPrice = default(clsPrice);
        //							clsstock aStock; // Stock is really just a collection of all stock positions

        //these are the disti variants - they're never going to be HP list price ones
        //							foreach (var v in this.i_Variants[Buyeraccount.SellerChannel.IsCloneOf].ToList) //tolist is new (was getting a collection has been modified enumeration may not execute)
        //							{
        //								if (Buyeraccount.wareHouseFilter == "" || string.Equals(System.Convert.ToString(Buyeraccount.wareHouseFilter), System.Convert.ToString(v.Code), StringComparison.CurrentCultureIgnoreCase))
        //								{
        //									if (!v.Deleted)
        //									{

        //need to see if there is a customer specific price first - if not, clone the list price if these is one,
        //otherwise make a POA

        //										if (v == SKUvariant || SKUvariant == iq.AllVariants)
        //										{

        //											aPrice = v.priceFor(Buyeraccount.Priceband, Buyeraccount.Currency);

        //											if (aPrice == null)
        //											{
        //we have no price customer specific price

        //												clsPrice lp = ListPrice(Buyeraccount);
        //												if (lp == null)
        //												{

        //													aPrice = new clsPrice(v, Buyeraccount.Priceband, new NullablePrice(Buyeraccount.Currency), "Requesting price.." + Strings.Format(DateTime.Now, "ddd hh:nn"));

        //aPrice.Price = New NullablePrice(Buyeraccount.Currency)
        //aPrice.SKUVariant = v
        //aPrice.PriceBand = Buyeraccount.Priceband
        //aPrice.Price.isValid = False
        //													}
        //													else
        //													{
        //Clone the HP list PRICE (not variant) into a new customer specific record PRICE
        //(attached to the exsiting disti Variant)
        //prior to calling the webservice for a 'real' (Updated, customer specific) price

        //														aPrice = new clsPrice(v, Buyeraccount.Priceband, lp.Price, "LP clone");
        //														aPrice.Price.isList = true;
        //force it into the stale list
        //														aPrice.lastRequested = DateAndTime.DateAdd(DateInterval.Minute, -100, DateTime.Now);

        //														}
        //														}

        //														if (v.shipments.Count == 0)
        //														{
        //similarly, we need to make a stock record - so we can render a DIV with a valid S_Id - to be replaced when the webservice returns - without this wee see 'X'  stock when first using the system
        //If aPrice.ID <> -1 Then 'not a POA
        //															aStock = new clsstock(v, -1, DateTime.Now, "initial", true);
        // End If
        //															}
        //End If
        //															poa.Add(aPrice);
        //															}

        //															}
        //															}
        //															}

        //															return poa;
        //															}
        //															else
        //															{
        //Changes to Display CarePack Blank Prices
        //																if (this.ListPrice(Buyeraccount) != null)
        //																{
        //fetch HPs list price according to the buyers region and currency - NB list pricing is per country at the moment (ie. there is no EMEA list price)
        //																	ListPrices.Add(this.ListPrice(Buyeraccount));
        //																	return ListPrices;
        //																	}
        //																	else
        //																	{
        // Stop
        //																		}
        //not in their feed
        // return an EMPTY list - which supresses display (feed only skus)
        //																		return new List<clsPrice>(); //return a list of 0 prices

        //																		}
        //																		}


        //ElseIf (PriceConfig And 8) And Not callWebservice Then
        //    'this is the code path for checeks of whether a product *could* have a price (from the webservice)
        //    'these tend to be the big, expensige resurvice ones for working out whether to display squares etc.
        //    Dim couldhave As List(Of clsPrice) = New List(Of clsPrice)
        //    If Me.inFeed(Buyeraccount.SellerChannel) Then 'ChannelSKU(Me, SKUvariant, Buyeraccount.SellerChannel) <> "" Then
        //        couldhave.Add(Nothing) 'this is a POA
        //    End If
        //    Return couldhave
        //																		}

        //If Not Me.inFeed(Buyeraccount.SellerChannel) Then Return New List(Of clsPrice) 'return a list of 0 prices

        //Everyone' IS the Base Channel - this is loosely equivilent to the IQ1 'external' price - for implementations with a webservice
        //																		if (PriceConfig & 4) //use price bands
        //																		{
        //																			BasePrices = this.Prices(Buyeraccount, SKUvariant); //.Priceband, Everyone, Buyeraccount.Currency) ', SKUvariant)
        //																			if (BasePrices.Count > 0)
        //																			{
        //																				return BasePrices;
        //																				}
        //																				}

        //   If PriceConfig And 2 Then
        // Buyeraccount.Currency = iq.i_currency_code("USD")


        //																				if (this.ListPrice(Buyeraccount) != null)
        //																				{
        //fetch HPs list price according to the buyers region and currency - NB list pricing is per country at the moment (ie. there is no EMEA list price)
        //																					ListPrices.Add(this.ListPrice(Buyeraccount));
        //																					return ListPrices;
        //																					}
        //																					else
        //																					{
        // Stop
        //																						}




        //    End If

        //'this is effectively a 'show everything' flag - becuase it will make a price on products that are geographically out of scope
        //																						if ((PriceConfig & 1) != 0)
        //																						{

        //																							List<clsPrice> poa = default(List<clsPrice>);
        //																							poa = new List<clsPrice>();

        //If Me.i_Variants(sellerchannel).
        //Dim aprice As clsPrice
        //aprice = New clsPrice(Buyeraccount, New clsVariant(-1, "POA", Me, Buyeraccount.SellerChannel, "", "", "", "", r_worldwide, False, False))

        //																							poa.Add(null); //return a list containing a single nothing ' This is a POA
        //																							return poa;
        //																							}

        //if ALL else fails - we wont show the product  return an EMPTY list
        //																							return new List<clsPrice>(); //return a list of 0 prices

    }


    public clsProduct(string SKu, string Name, bool isSystem, bool isOption, clsSector sector, clsProductType productType, DateTime activeFrom, DateTime ActiveTo, bool Active, bool EOL, bool Publish, string mfrCode, string buCode, string plCode)
        : this(SKu, isSystem, isOption, sector, productType, activeFrom, ActiveTo, Active, EOL, Publish, mfrCode, buCode, plCode, null, -1)
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        Branches = new HashSet<clsBranch>();



        //This is a 'quick' method to create a product with a single attribute (of Name).. and to fill that attribute with a new text object in English carrying the description
        //you *generally* dont want to be doing that - but it's useful for creating some of the metadata

        if (SKu != "")
        {
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
    public clsProduct(string sku, bool IsSystem, bool IsOption, clsSector Sector, clsProductType ProductType, DateTime activeFrom, DateTime ActiveTo, bool Active, bool EOL, bool Publish, string mfrCode, string buCode, string plCode, DataTable wc, ref int nextid, bool InMemoryOnly = false)
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        Branches = new HashSet<clsBranch>();


        this.Attributes = new Dictionary<int, clsProductAttribute>();
        this.i_Attributes_Code = new Dictionary<string, List<clsProductAttribute>>(StringComparer.CurrentCultureIgnoreCase);


        if (sku == "" && (IsSystem || IsOption))
        {
            Debugger.Break();
        }

        if (IsSystem == 0 && IsOption == 0)
        {
            int jjj = 0;
        }


        this.SKU = sku;
        set_this.isSystem(IsSystem);
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
        this.Points = new Dictionary<clsScheme, int>(); //number of points this product is worth under each scheme
        this.Promos = new Dictionary<string, List<clsRegion>>();

        this.mfrCode = mfrCode; //broad
        this.buCode = buCode; //narrow
        this.plCode = plCode; //narrower

        if (!InMemoryOnly)
        {
            if (sku != "")
            {
                iq.i_SKU.Add(sku, this);
            }

            //add myself to the master list
            if (wc != null)
            {
                this.ID = nextid;
                nextid++;

                System.Data.DataRow row = default(System.Data.DataRow);
                row = wc.NewRow();
                row["ID"] = this.ID; //- we EXPLICITLY set ids
                row["sku"] = this.SKU;
                row["issystem"] = this.get_isSystem();
                row["isoption"] = this.isOption;
                row["fk_producttype_id"] = this.ProductType.ID;
                row["fk_sector_id"] = this.Sector.ID;
                row["activefrom"] = this.activeFrom;
                row["activeTo"] = this.activeTo;
                row["active"] = this.Active;
                row["eol"] = this.EOL;
                row["publish"] = this.Publish;
                row["mfrCode"] = this.mfrCode;
                row["buCode"] = this.buCode;
                row["plCode"] = this.plCode;
                row["deleted"] = false;

                wc.Rows.Add(row);

            }
            else
            {

                object sql = null;
                sql = "INSERT INTO PRODUCT (sku,issystem,isoption,fk_producttype_id,fk_sector_id,activefrom,activeto,active,eol,publish,mfrCode,buCode,plCode )";
                sql += " VALUES (" + da.SqlEncode(sku) + "," + System.Convert.ToString(IsSystem ? 1 : 0) + "," + System.Convert.ToString(IsOption ? 1 : 0) + "," + ProductType.ID + "," + Sector.ID + "," + da.UniversalDate(activeFrom) + "," + da.UniversalDate(ActiveTo) + "," + System.Convert.ToString(Active ? 1 : 0) + "," + System.Convert.ToString(EOL ? 1 : 0) + "," + System.Convert.ToString(Publish ? 1 : 0) + ",";
                sql += da.SqlEncode(mfrCode) + "," + da.SqlEncode(buCode) + "," + da.SqlEncode(plCode) + ");";

                this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
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


    public clsProduct(int id, string sku, bool isSystem, bool isOption, clsSector sector, clsProductType ProductType, DateTime activeFrom, DateTime ActiveTo, bool Active, bool EOL, bool Publish, string mfrCode, string buCode, string plCode)
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        Branches = new HashSet<clsBranch>();


        //If sku <> "" And (isSystem Or isOption) Then

        this.ID = id;

        this.SKU = sku;
        this.Attributes = new Dictionary<int, clsProductAttribute>();
        this.i_Attributes_Code = new Dictionary<string, List<clsProductAttribute>>(StringComparer.CurrentCultureIgnoreCase);
        this.Sector = sector;
        this.ProductType = ProductType;

        if (sku != "")
        {
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
        this.Points = new Dictionary<clsScheme, int>(); //number of points this product is worth under each scheme

        this.activeFrom = activeFrom;
        this.activeTo = ActiveTo;
        this.Active = Active;
        this.EOL = EOL;
        this.Publish = Publish;
        set_this.isSystem(isSystem);
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
    public string CurrentStock(clsAccount buyeraccount, ref int numericValue, clsVariant whichVariant, List<string> errorMessages)
    {
        string returnValue = "";

        if (whichVariant == null)
        {
            errorMessages.Add("* Whichvariant was Nothing for CurrentStock()");
            numericValue = -10;
            returnValue = "-";
        }
        else
        {

            //NB: We fetch stock from the sellerchannels 'isCloneOf' - which for non-clones, is itself.
            if (this.i_Variants.ContainsKey(buyeraccount.SellerChannel.IsCloneOf))
            {
                returnValue = "-"; //iq.EnglishIndex("Unknown").text(buyeraccount.Language) 'should be agentaccount.language really
                foreach (var sv in this.i_Variants[buyeraccount.SellerChannel.IsCloneOf])
                {
                    //If Me.i_Stock(buyeraccount.SellerChannel.IsCloneOf).ContainsKey(skuVariant) Then
                    if (sv == whichVariant || whichVariant == iq.AllVariants)
                    {
                        foreach (var shipment in sv.shipments.Values)
                        {
                            if (shipment.IsCurrent && shipment.Arrival < DateTime.Now)
                            {
                                numericValue += System.Convert.ToInt32(shipment.quantity);
                                if (whichVariant != null)
                                {
                                    returnValue = numericValue.ToString();
                                }
                                return returnValue; //we're done
                            }
                        }
                        returnValue = numericValue.ToString();
                        return returnValue; //we're done (totaled for all skuvariants)
                    }
                    else
                    {
                        returnValue = "-"; //  iq.EnglishIndex("Unstocked Variant").text(buyeraccount.Language)
                        numericValue = -1;
                    }
                }
            }
            else
            {
                returnValue = "no record"; //iq.EnglishIndex("Unstocked").text(buyeraccount.Language)
                numericValue = -2;
            }

            if (numericValue < 0)
            {
                returnValue = numericValue.ToString();
            }
        }

        return returnValue;
    }
    public List<clsPrice> BasePrices(clsAccount buyerAccount, clsVariant whichVariant)
    {
        List<clsPrice> returnValue = default(List<clsPrice>);

        //Returns all of a seller channels prices for this product in the buyerAccounts currency - there may be many variants
        //If SKUVariant is is suppled - only that variant is returned

        returnValue = null;
        clsChannel sellerChannel = buyerAccount.SellerChannel;

        if (this.i_Variants.ContainsKey(sellerChannel))
        {
            foreach (var v in this.i_Variants[sellerChannel])
            {
                if (v == whichVariant || whichVariant == iq.AllVariants)
                {
                    if (returnValue == null)
                    {
                        returnValue = new List<clsPrice>();
                    }
                    returnValue.Add(v.i_prices(iq.getPriceBand(""))[buyerAccount.Currency]);
                }
            }
        }

        return returnValue;
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

        clsAccount with_1 = buyeraccount;
        //call the other (more granular) overload
        return Prices(with_1.SellerChannel, with_1.BuyerChannel, with_1.Priceband, with_1.Currency, whichVariant);

    }

    public List<clsPrice> Prices(clsChannel sellerchannel, clsChannel buyerchannel, clsPriceBand priceband, clsCurrency currency, clsVariant whichvariant)
    {

        //returns The Prices for all (or the specified) SKUVariant(s) for the specified buyer, - at the correct margin/multiplier for the buyer/seller combo
        //will return an empty list if there is no price (POA)

        List<clsPrice> ret = new List<clsPrice>();
        clsMargin margin = default(clsMargin);

        //NB: We fetch pricing from the sellerchannels 'isCloneOf' - which for non-clones, is itself.
        clsChannel sourceChannel = sellerchannel.IsCloneOf;

        //If we're getting prices for a clone..
        //There's an important distinction between the SourceChannel and the SellerChannel - the sellerchannel (which may be a clone) carries the margin for the buyer..
        //whereas the sourceChannel (the clones 'parent') has the base price

        List<clsVariant> done = default(List<clsVariant>);
        done = new List<clsVariant>();

        //First get SPECIFIC (overriding)prices
        if (this.i_Variants != null)
        {
            if (this.i_Variants.ContainsKey(sourceChannel)) //does the SOURCE channel seller have a (base) price for this product
            {
                foreach (var v in this.i_Variants[sourceChannel]) //The variants are *not* buyer specific - there are a small number of variants (and a large number of prices)
                {
                    if (v == whichvariant || whichvariant == iq.AllVariants)
                    {
                        //there are *some* specific prices for this product - for this buyer (perhaps not for the right variants or currencies
                        if (v.i_prices.ContainsKey(priceband))
                        {
                            if (v.i_prices(priceband).ContainsKey(currency))
                            {
                                ret.Add(v.i_prices(priceband)[currency]); //we've found a specifc price - to which we should NOT apply margin !
                                done.Add(v);
                            }
                        }
                    }
                }
            }

            //for any remaining unpriced - do margin based pricing
            if (buyerchannel != null)
            {
                if (sellerchannel.Margin.ContainsKey(buyerchannel)) //does this seller have ANY margin specified for this buyer
                {
                    if (sellerchannel.Margin(buyerchannel).ContainsKey(this.Sector)) //is there a margin specified for products within this sector (BU) .. PSG/ISS . . .
                    {
                        if (this.i_Variants.ContainsKey(sourceChannel))
                        {
                            List<clsVariant> vpc = this.i_Variants[sourceChannel]; //seller'
                            if (vpc.Count > 30)
                            {
                                int a = 0;
                            }
                            foreach (var SV in vpc)
                            {
                                if (!done.Contains(SV))
                                {
                                    //  If SV Is WhichVariant Or WhichVariant Is iq.AllVariants Then  'was variantmatch
                                    margin = sellerchannel.Margin(buyerchannel)[this.Sector];
                                    //baseprice is their price for themselves (or maybe 'everyone' - check)
                                    if (SV.i_prices.ContainsKey(iq.getPriceBand(margin.PriceBand)))
                                    {
                                        IQ.clsPrice baseprice = default(IQ.clsPrice);
                                        baseprice = SV.i_prices(iq.getPriceBand(margin.PriceBand))[currency];
                                        ret.Add(new IQ.clsPrice(baseprice, margin.Factor)); //Return the Factored price from the SOURCE channel
                                        done.Add(SV);
                                    }
                                    else
                                    {
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

    public List<ClsValidationMessage> getXtext(object path, List<string> Acknowledged)
    {
        List<ClsValidationMessage> returnValue = default(List<ClsValidationMessage>);
        if (this.isFIO)
        {
            return new List<ClsValidationMessage>(); //Dont include messages from Pre-installed
        }

        returnValue = new List<ClsValidationMessage>();
        if (this.i_Attributes_Code.ContainsKey("xText"))
        {

            int i = 0;
            clsProductAttribute xtext = default(clsProductAttribute);
            string showOnlyInFamilies = "";
            string hideInFamilies = "";

            string myfamily = Strings.Trim(System.Convert.ToString(findFamily(path))).ToLower();
            for (i = 0; i <= this.i_Attributes_Code("xText").Count() - 1; i++)
            {

                xtext = this.i_Attributes_Code("xText")[i];

                if (this.i_Attributes_Code.ContainsKey("ShowF"))
                {
                    showOnlyInFamilies = Strings.LCase(System.Convert.ToString(this.i_Attributes_Code("ShowF")[i].Translation.text(English))).Trim();
                }

                if (this.i_Attributes_Code.ContainsKey("HideF"))
                {
                    hideInFamilies = Strings.LCase(System.Convert.ToString(this.i_Attributes_Code("HideF")[i].Translation.text(English))).Trim();
                }


                //Exit if this external text should NOT be visible
                if (!string.IsNullOrEmpty(showOnlyInFamilies) && !showOnlyInFamilies.Split(',').Contains(myfamily))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(hideInFamilies) && hideInFamilies.Split(',').Contains(myfamily))
                {
                    continue;
                }

                ImageButton ib = new ImageButton();
                ClsValidationMessage msg = default(ClsValidationMessage);

                msg = new ClsValidationMessage(enumValidationMessageType.Validation, (Acknowledged != null && Acknowledged.Contains(path + "." + System.Convert.ToString(i))) ? EnumValidationSeverity.greenTick : ((EnumValidationSeverity)xtext.NumericValue), iq.AddTranslation(this.SKU + ":" + xtext.Translation.text(English), English, "ISSU", 0, null, 0, false), iq.AddTranslation("Important Information", English, "ISSU", 0, null, 0, false), "", 0, 0, Strings.Split(""), "", path + "." + System.Convert.ToString(i));
                if (Acknowledged != null && Acknowledged.Contains(path + "." + System.Convert.ToString(i)))
                {
                    msg.Acknowledged = true;
                }
                returnValue.Add(msg);
            }

        }

        return returnValue;
    }
    public class clsSpecTableEntry
    {



        private string _title;
        public string Title
        {
            get
            {
                if (Code != "hdd" && Code != "opt")
                {
                    if (Code2 != null && (Code2.Contains("SATA") || Code2.Contains("SAS")))
                    {
                        return "Disk Controller";
                    }
                    else
                    {
                        return _title;
                    }
                }
                else
                {
                    return _title;
                }

                //   Return If(Code <> "hdd" AndAlso Code2 IsNot Nothing AndAlso (Code2.Contains("SATA") Or Code2.Contains("SAS")), "Disk Controller", _title)
            }
            set
            {
                _title = value;
            }
        }
        public clsTranslation Value { get; set; }
        public string Extra { get; set; }
        public string Code { get; set; }
        public string Code2 { get; set; }
        public int Max { get; set; }
        public string Type { get; set; }
        public string ProdType { get; set; }
        public string[] Params { get; set; }
        public int Order //This gives the combined order for preinstalled, attributes, etc
        {
            get
            {
                switch (ProdType)
                {
                    case "SVR":
                    case "DTO":
                    case "NBK":
                        switch (Code.ToLower())
                        {
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
                        break;
                    case "SWD":
                        switch (Code.ToLower())
                        {
                            case "mfrsku":
                                return 10;
                            case "formfactor":
                                return 20;
                            case "ioc":
                            case "cpu":
                                if (Type == "pre")
                                {
                                    return ((Code2 != null && (Code2.Contains("SATA") || Code2.Contains("SAS"))) ? 90 : 30);
                                }
                                else
                                {
                                    return 0;
                                }
                                break;
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
                        break;
                    case "HPN":
                        switch (Code.ToLower())
                        {
                            case "mfrsku":
                                return 10;
                            case "formfactor":
                                return 20;
                            case "ioc":
                            case "cpu":
                                if (Type == "pre")
                                {
                                    return ((Code2 != null && (Code2.Contains("SATA") || Code2.Contains("SAS"))) ? 90 : 30);
                                }
                                else
                                {
                                    return 0;
                                }
                                break;
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
                        break;
                }
            }
        }

        public clsSpecTableEntry()
        {
            @Params = new[] { };
        }
    }

    public Panel Spectable(clsLanguage language, List<clsQuantity> preinstalled, clsBranch branch, string sysPath, bool showall)
																						{
																							
																							Panel SpectablePanel = new Panel();
																							SpectablePanel.CssClass = "specTable";
																							
																							string familyName = string.Empty;
																							if (branch.Parent != null)
																							{
																								familyName = System.Convert.ToString(branch.Parent.Translation.text(English));
																							}
																							
																							List<clsSpecTableEntry> specTableProps = new List<clsSpecTableEntry>();
																							
																							IEnumerable<clsProductAttribute> orderedAttributeList = from v in this.Attributes.Values orderby v.Attribute.Order select v;
																							
																							//For Each a In Me.Attributes.Values
																							//    If a.Attribute.Code.ToLower = "also included" Then
																							//        Beep()
																							//    End If
																							//Next
																							
																							
																							//This summarises the gives and takes slots accrooss the system, chassis, FIOs and all Preinstalled componentry -
																							//so that we can render the 'Max 8' type slot info in the spec table
																							List<clsSlot> allslots = default(List<clsSlot>);
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
																							foreach (var b in branch.childBranches.Values)
																							{
																								if (!b.deleted)
																								{
																									if (b.EnglishName.ToLower.Contains("fios"))
																									{
																										Debugger.Break(); //contains the ###iO controlers, ###CPUS etc
																									}
																									if (b.EnglishName.ToLower.Contains("chassis") && b.Hidden)
																									{
																										foreach (var slot in b.slots.Values)
																										{
																											if (!slot.deleted)
																											{
																												if (slot.path.Contains(sysPath) || slot.path == "") //Presinstalled contains some quantities which DO NOT apply (wrong paths)
																												{
																													allslots.Add(slot);
																												}
																											}
																										}
																										break; //there should be only 1 chassis branch
																									}
																								}
																							}
																							
																							foreach (var pic in preinstalled) //preinstalled component (clsQuantities)
																							{
																								if (pic.Path == "" || pic.Path.StartsWith(sysPath))
																								{
																									//Dim bn As String = pic.Branch.DisplayName(English)
																									//Dim picProd As String = pic.Branch.Product.i_Attributes_Code("desc")(0).Translation.text(English)
																									//Dim sp As String = PathName(path) 'sys path
																									//Dim p As String = PathName(pic.Path)
																									foreach (var picSlot in pic.Branch.slots.Values)
																									{
																										if (!picSlot.deleted)
																										{
																											//  If picSlot.Type.MajorCode = "MEM" Then Stop
																											if (picSlot.path.StartsWith(sysPath) || picSlot.path == "") //Presinstalled contains some quantities which DO NOT apply (wrong paths)
																											{
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
																							specTableProps.AddRange(orderedAttributeList.Where(oal => oal.deleted == false).Select(v => new clsSpecTableEntry() {ProdType = this.ProductType.Code, Type = "atr", Code = v.Attribute.Code, Title = v.Attribute.displayName(language), Value = iq.AddTranslation((v.Attribute.Code.ToLower() = "formfactor" && orderedAttributeList.Where(at => at.Attribute.Code = "U").Count() > 0) ? (v.displayNameNoCode(language) + " (" + orderedAttributeList.Where(at => at.Attribute.Code = "U").First().NumericValue.ToString() + "U)") : (v.displayNameNoCode(language)), English, "", 0, null, 0, false)})); // horrid hack to put U after form factor
																							
																							//Add all preinstalled products
																							foreach (var p in preinstalled.Where(pi => pi.FOC).GroupBy(pi => pi.Branch))
																							{
																								string productDisplay = string.Empty;
																								if (p.Key.Product.i_Attributes_Code.ContainsKey("Name"))
																								{
																									object productName = p.Key.Product.i_Attributes_Code("Name")[0].Translation.text(s_lang);
																									productDisplay = System.Convert.ToString(productName);
																								}
																								else if (p.Key.Product.i_Attributes_Code.ContainsKey("Description"))
																								{
																									productDisplay = System.Convert.ToString(p.Key.Product.i_Attributes_Code("Description")[0].Translation.text(s_lang));
																								}
																								else if (p.Key.Product.i_Attributes_Code.ContainsKey("Desc"))
																								{
																									productDisplay = System.Convert.ToString(p.Key.Product.i_Attributes_Code("Desc")[0].Translation.text(s_lang));
																								}
																								else
																								{
																									productDisplay = System.Convert.ToString(p.Key.Translation.text(s_lang));
																								}
																								//do we have a max slot
																								// Dim test =AllSlots.Where(Function(x) x.slotNum)
																								object slo = allslots.Where(als => als.deleted == false && p.Key.Product.ProductType.Code.ToLower() == als.Type.MajorCode.ToLower() && als.numSlots > 0).Sum(s => s.numSlots);
																								
																								//Horrrid if statement below as Raid controllers are embeded devices, this is going to change according to Paul so this is temp, yay!
																								clsSpecTableEntry specEntry = new clsSpecTableEntry();
																								
																								specEntry.ProdType = System.Convert.ToString(this.ProductType.Code);
																								if (p.Key.Product.i_Attributes_Code.ContainsKey("technology"))
																								{
																									
																									specEntry.Code2 = System.Convert.ToString(p.Key.Product.i_Attributes_Code("technology").First.Translation.text(English));
																								}
																								else
																								{
																									specEntry.Code2 = null;
																								}
																								
																								specEntry.Type = "pre";
																								
																								if (p.Key.Product.i_Attributes_Code.ContainsKey("optFamily") && p.Key.Product.i_Attributes_Code("optFamily")[0].Translation.text(English).ToUpper == "RAID_CONTROLLERS")
																								{
																									specEntry.Code = "RAID";
																									specEntry.Title = "RAID Controller";
																								}
																								else
																								{
																									specEntry.Code = System.Convert.ToString(p.Key.Product.ProductType.Code);
																									specEntry.Title = System.Convert.ToString(p.Key.Product.ProductType.Translation.text(language));
																								}
																								
																								
																								specEntry.Value = iq.AddTranslation("{0} x {1}", English, "SpecValues", 0, null, 0, false);
																								if (specEntry.Code != "ioc")
																								{
																									specEntry.Max = slo;
																								}
																								specEntry.Params = new[] {p.Sum(pp => pp.NumPreInstalled).ToString(), productDisplay};
																								
																								//   specTableProps.Add(New clsSpecTableEntry() With {.ProdType = Me.ProductType.Code, .Code2 = If(p.Key.Product.i_Attributes_Code.ContainsKey("technology"), p.Key.Product.i_Attributes_Code("technology").First.Translation.text(English), Nothing), .Type = "pre", .Code = If(p.Key.Product.i_Attributes_Code.ContainsKey("optFamily") AndAlso p.Key.Product.i_Attributes_Code("optFamily")(0).Translation.text(English).ToUpper = "RAID_CONTROLLERS", "RAID", p.Key.Product.ProductType.Code), .Title = If(p.Key.Product.i_Attributes_Code.ContainsKey("optFamily") AndAlso p.Key.Product.i_Attributes_Code("optFamily")(0).Translation.text(English).ToUpper = "RAID_CONTROLLERS", "RAID Controller", p.Key.Product.ProductType.Translation.text(language)), .Value = iq.AddTranslation("{0} x {1}", English, "SpecValues", 0, Nothing, 0, False), .Max = slo, .Params = {p.Sum(Function(pp) pp.NumPreInstalled).ToString, productDisplay}})
																								specTableProps.Add(specEntry);
																							}
																							
																							if (new[] {"SVR", "SWD"}.Contains(this.ProductType.Code))
																							{
																								//Populate Management row, do we have an insight or oneview licence here
																								string t2 = "No OneView or Insight Control";
																								string t1 = "No Licence";
																								
																								if (preinstalled.Where(pi => pi.Branch.Product.ProductType.Code.ToLower() == "man1").Count() > 0)
																								{
																									object pr = preinstalled.Where(pi => pi.Branch.Product.ProductType.Code.ToLower() == "man1").FirstOrDefault();
																									if (pr.IsAutoAdd)
																									{
																										t2 = System.Convert.ToString(pr.Branch.Product.i_Attributes_Code("desc")[0].Translation.text(language));
																										string includedMajorCode = "ILO MAN1";
																										if (pr.Branch.slots.Where(sl => includedMajorCode.Contains(System.Convert.ToString(sl.Value.Type.MajorCode))).Count() > 0)
																										{
																											t1 = "Advanced";
																										}
																									}
																									
																								}
																								string sts = System.Convert.ToString((this.i_Attributes_Code.ContainsKey("ILOHARDWARE")) ? "{0} ({1}) / {2}" : "No Management");
																								clsSpecTableEntry specEntry = new clsSpecTableEntry();
																								specEntry.ProdType = System.Convert.ToString(this.ProductType.Code);
																								specEntry.Type = "pre";
																								specEntry.Code = "MGT";
																								specEntry.Title = "Management";
																								specEntry.Value = iq.AddTranslation(sts, English, "", 0, null, 0, false);
																								if (this.i_Attributes_Code.ContainsKey("ILOHARDWARE"))
																								{
																									specEntry.Params = new[] {this.i_Attributes_Code("ILOHARDWARE")[0].Translation.text(language), t1, t2};
																								}
																								else
																								{
																									specEntry.Params = new[] {};
																								}
																								
																								
																								specTableProps.Add(new clsSpecTableEntry() {ProdType = this.ProductType.Code, Type = "pre", Code = "MGT", Title = "Management", Value = iq.AddTranslation(sts, English, "", 0, null, 0, false), Params = ((this.i_Attributes_Code.ContainsKey("ILOHARDWARE")) ? (new[] {this.i_Attributes_Code("ILOHARDWARE")[0].Translation.text(language), t1, t2}) : {})});
																							}
																							
																							if (new[] {"SVR", "NBK", "DTO", "SWD"}.Contains(this.ProductType.Code))
																							{
																								//Special case for HDD and OPT as we need a line when they are NOT present
																								System.Boolean allHDDslots = from p in preinstalled where p.branch.Product.ProductType.Code.ToUpper() == "HDD" select p;
																								if (allHDDslots.Count > 0)
																								{
																									System.Boolean preinstalledHDD = from p in allHDDslots where p.NumPreInstalled > 0 select p;
																									if (preinstalledHDD.Count == 0)
																									{
																										specTableProps.Add(new clsSpecTableEntry() {ProdType = this.ProductType.Code, Type = "pre", Code = "HDD", Title = "Hard Disk Drive", Value = iq.AddTranslation("None Installed", English, "", 0, null, 0, false), Max = allslots.Where(als => als.deleted = false && "hdd" = als.Type.MajorCode.ToLower() && als.numSlots > 0).Sum(s => s.numSlots)});
																									}
																								}
																								
																								if (preinstalled.Where(p => p.branch.Product.ProductType.Code.ToUpper() == "HDD").Count() == 0)
																								{
																									specTableProps.Add(new clsSpecTableEntry() {ProdType = this.ProductType.Code, Type = "pre", Code = "HDD", Title = "Hard Disk Drive", Value = iq.AddTranslation("None Installed", English, "", 0, null, 0, false), Max = allslots.Where(als => "hdd" = als.Type.MajorCode.ToLower() && als.deleted = false && als.numSlots > 0).Sum(s => s.numSlots)});
																								}
																								if (preinstalled.Where(p => p.branch.Product.ProductType.Code.ToUpper() == "OPT").Count() == 0)
																								{
																									specTableProps.Add(new clsSpecTableEntry() {ProdType = this.ProductType.Code, Type = "pre", Code = "OPT", Title = "Optical Drive", Value = iq.AddTranslation("None Installed", English, "", 0, null, 0, false), Max = allslots.Where(als => "opt" = als.Type.MajorCode.ToLower() && als.deleted = false && als.numSlots > 0).Sum(s => s.numSlots)});
																								}
																							}
																							
																							//Add a slot summary for PCI, grouped by the short description of the slot type
																							System.Object listofInterfaceCards = new[] {"PCIF", "PCIC", "PCID", "PCIE", "PCIG", "PCIX", "PCI", "MODA", "MODL", "MODI", "MODB", "RISER", "MODE", "MODM"};
																							System.Object excludefromsummary = new[] {"MODM", "MODE", "MODL", "MODI", "RISER"};
																							System.Char d = string.Join("<br>", branch.slots.Values.Union(branch.childBranches.SelectMany(s => s.Value.slots.Values)).Where(s => listofInterfaceCards.Contains(s.Type.MajorCode.ToUpper) && s.numSlots > 0).OrderBy(sl => (sl.HasSlotNum ? sl.slotNum.value : 200)).Select(s => string.Format("{0}: {1}", s.HasSlotNum ? (Strings.Chr(System.Convert.ToInt32(64 + s.slotNum.value))) : "", (s.Type.MajorCode.ToUpper.StartsWith("MOD")) ? (s.Type.shortDisplayName(language)) : (s.Type.Translation.text(language)))).ToList());
																							
																							
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
																							
																							
																							
																							
																							int cn = 0;
																							string op = "";
																							List<string> @params = new List<string>();
																							
																							branch.slots.Values.Union(branch.childBranches.SelectMany(s => s.Value.slots.Values)).Where(s => listofInterfaceCards.Contains(s.Type.MajorCode) && !excludefromsummary.Contains(s.Type.MajorCode) && s.numSlots > 0).GroupBy(s => s.Type.shortDisplayName(language)).Select(s=>
																							{
																								op += " {" + cn + "}: {" + (cn + 1) + "}";
																								cn = cn + 2;
																								@params.Add(s.Key);
																								@params.Add(s.Sum(sss => sss.numSlots));
																							}).ToList();
																							
																							if (@params.Count > 0)
																							{
																								specTableProps.Add(new clsSpecTableEntry() {ProdType = this.ProductType.Code, Type = "pre", Code = "PCI", Title = "Interface Slots", Value = iq.AddTranslation(op, English, "", 0, null, 0, false), Extra = d, Params = @params.ToArray});
																							}
																							
																							//Add any information pertinent to slots, so anything which gives a slot to the system and isn't otherwise already added
																							foreach (var slot in branch.slots.Values.Union(branch.childBranches.SelectMany(s => s.Value.slots.Values)).Where(s => !listofInterfaceCards.Contains(s.Type.MajorCode) && s.numSlots > 0))
																							{
																								if (slot.path == sysPath || slot.path == "")
																								{
																									if (slot.Type.MajorCode == "RJ45")
																									{
																										object spt = specTableProps.Where(stp => stp.Type == "atr" && stp.Code == "PriConnectivity").FirstOrDefault();
																										if (spt != null)
																										{
																											spt.Max = slot.numSlots;
																										}
																									}
																									specTableProps.Add(new clsSpecTableEntry() {ProdType = this.ProductType.Code, Type = "slot", Code = slot.Type.MajorCode, Title = Xlt(slotMajorTranslations(System.Convert.ToString(slot.Type.MajorCode)), language), Value = slot.Type.Translation});
																								}
																							}
																							
																							//Render all of the above in a predefined order, nasty way of ordering needs to go in the db somewhere but for now hardcoded in the object
																							foreach (var sp in specTableProps.OrderBy(s => s.Order))
																							{
																								
																								if (sp.Order > 0 || showall)
																								{
																									System.Object l = null;
																									if (sp.Code.ToUpper() == "PCI")
																									{
																										l = NewLit("<div style=\'display:none;\' id=\'" + sysPath +"." + "ttPCIslots\'>" + sp.Extra + "</div><span onclick=\"TagToTip(\'" + sysPath +"." + "ttPCIslots\', TITLE, \'Interface Card Slots\', CLICKSTICKY, true, CLICKCLOSE, false, CLOSEBTN, true, COPYCONTENT, false, DELAY, 400, BORDERWIDTH, 1, BORDERCOLOR, \'#2F7BD1\', PADDING, 2, FIX, [624, 372])\" style=\'cursor:help;float:right;height:15px;width:15px;\'><img src=\'../images/Navigation/ICON_CIRCLE_info.png\'/></span>");
																									}
																									//Ok another hacky bit, would like to put a param in the DB to say if the field might contain a part number
																									//Scan for Part Numbers
																									if (sp.Code.ToUpper != "MFRSKU")
																									{
																										string newValue = "";
																										@params = new List<string>();
																										int vc = 0;
																										foreach (var spl in sp.Value.text(English).Split(","))
																										{
																											if (iq.i_SKU.ContainsKey(spl))
																											{
																												newValue += "{" + System.Convert.ToString(vc) + "} , ";
																												@params.Add(iq.i_SKU(spl).DisplayName(language));
																												vc++;
																											}
																										}
																										if (!string.IsNullOrEmpty(newValue))
																										{
																											sp.Value = iq.AddTranslation(newValue.Substring(0, newValue.Length - 2), English, "", 0, null, 0, false);
																											sp.Params = @params.ToArray;
																										}
																										
																									}
																									
																									specTableRow(System.Convert.ToString((sp.Order == 0 ? "HIDDEN " : "") + Xlt(sp.Title, language)), string.Format(System.Convert.ToString(sp.Value.text(language)), sp.Params).Replace(familyName + " ", "").Replace("HP ", ""), SpectablePanel, System.Convert.ToInt32(sp.Max), l, language);
																								}
																								else
																								{
																									
																									string ss = sp.Title + ":" + sp.Value.text(language);
																									
																								}
																								
																								
																							}
																							
																							return SpectablePanel;
																						}
    public void specTableRow(string headerText, string valueText, Panel specTable, int max, Literal xtra, clsLanguage language)
    {
        Panel p = new Panel();
        p.CssClass = "specRow";
        specTable.Controls.Add(p);

        Panel panel = new Panel();
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
        lbl.Text = valueText != null ? (valueText.Replace("&NBSP;", "&nbsp;")) : "";
        panel.Controls.Add(lbl);
        if (max != 0)
        {
            Panel panel2 = new Panel();
            panel2.CssClass = "specRightMax";
            Literal lbl2 = new Literal();
            lbl2.Text = Xlt("max", language) + ": " + max.ToString();
            panel2.Controls.Add(lbl2);
            panel.Controls.Add(panel2);
        }

        if (xtra != null)
        {
            panel.Controls.Add(xtra);
        }

        p.Controls.Add(panel);

        panel = new Panel();
        panel.CssClass = "specBreak";
        p.Controls.Add(panel);
        lbl = new Literal();
        lbl.Text = "&nbsp;";
    }

    public string slotMajorTranslations(string type)
    {
        switch (type)
        {
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

    ~clsProduct()
    {
        //base.Finalize();
    }

    public bool hasPromo(string promoCode, clsRegion region)
    {
        if (!Promos.ContainsKey(promoCode))
        {
            return false;
        }
        foreach (var p in Promos(promoCode))
        {
            if (p.Encompasses(region))
            {
                return true;
            }
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
        if (!this.hasSKU())
        {
            return true;
        }
        if (this.SKU.StartsWith("###"))
        {
            return true;
        }
        return false;
    }
}
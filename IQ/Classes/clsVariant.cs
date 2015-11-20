using dataAccess;


public class clsVariant
{

    //Every product has one or more variants
    //These are the disti specific versions of the the product and carry the disti Skus (hostPartNums) - amongst other things
    //Variants allow a product to have one or more stock level(s) and/or price(s) (although neither is required)
    //They're also used to allow (list) prices to be per region(country)
    //There must be a HP varant for each region (country)  for which there is a list price - as it is the variants that link Products, Prices and Regions.

    public int ID { get; set; } // there are at least as many variants as parts
    public clsChannel sellerChannel { get; set; } //Seller channel (disti)
    public string DistiSku { get; set; } //Distis internal part number - can be different for different variants of the same (HP) part (by localisation,  warehouse, code etc)
    public clsProduct Product { get; set; }
    public string Code { get; set; } //Human friendly code such as 'b-grade'
    public string DisplayText { get; set; } //Optional overriding text
    public string Warehouse { get; set; } //disti warehouse code (3 chars, Human readable ?) – could be extended to an object – with GPS cords, and customer-warehouse restrictions
    public string Localisation { get; set; } //#ABU, #ABA etc - this is just a text string - no fucntionality is driven from it
    public clsRegion Region { get; set; } //Mostly so list prices can be defined per country - but potentially useful for international distis - Can be nothing.
    public bool Deleted { get; set; }

    //prices *can* be empty - the actual [Variant] merely joins a Seller, Product, Warehouse and DistiSKU

    //i_Prices (which is an index on THIS variants (Unique Disti wharehouse/product/part) ..Is indexed by PriceBand - eg. 'A' or a host AccountNumber - which can be thought of as a very 'narrow band'
    public Dictionary<clsPriceBand, Dictionary<clsCurrency, clsPrice>> i_prices; //how to edit this (ie.. index by the objects).. would allow the iq.variants to be removed and the direct editing of prices
    public Dictionary<int, clsPrice> prices { get; set; }

    //listprices are under the 'everyone' buyer channel and are blended in in getprices()
    //Listprices are per currency/region

    public SortedDictionary<DateTime, clsStock> shipments { get; set; }
    string Ocode;


    public clsVariant clone(clsProduct Product)
    {
        clsVariant returnValue = default(clsVariant);

        returnValue = new clsVariant(this.Code, Product, this.sellerChannel, this.DistiSku, this.DisplayText, this.Warehouse, this.Localisation, this.Region, this.Deleted, null, -1);

        if (this.i_prices.ContainsKey(iq.getPriceBand(""))) // blank band is the 'everyone' price
        {
            foreach (var p in this.i_prices[Everyone].Values)
            {
                p.clone(returnValue); //clones the price list onto the newly cloned variant
            }
        }

        return returnValue;
    }

    public clsPrice priceFor(clsPriceBand PriceBand, clsCurrency currency)
    {
        clsPrice returnValue = default(clsPrice);

        returnValue = null;

        if (i_prices.ContainsKey(PriceBand))
        {
            if (i_prices[PriceBand].ContainsKey(currency))
            {
                returnValue = i_prices[PriceBand][currency];
            }
        }

        return returnValue;
    }

    /// <summary>Returns the unique 'compound key' for this variant - which is the distiSKU|warehouse -OR HPSKU|region.code</summary>
    /// <returns></returns>
    /// <remarks>The compound key is much like a database index - and is used with the channel.i_variantCK to ensure uniqueness - and for efficient access to Variants - note, warehouse is often (generally) blank</remarks>
    public string CK()
    {

        //For most Disti Channels - variants are indexed by (i.e. the unique key is DistiSKU|Warehouse
        //For HP - the compound key (and hp.i_variantCK) indexes ListPrice variants by Region.code
        //note... there can be more than one price for the same variant.. in different currencies

        if (this.sellerChannel == HP)
        {
            return this.DistiSku + "|" + this.Region.Code;
        }
        else
        {
            return this.DistiSku + "|" + this.Warehouse;
        }

    }


    /// <summary>returns UI for up to MaxShipment shipments of this productVariant</summary>
    public PlaceHolder StockUI(int MaxShipments, string style, clsLanguage language, clsChannel channel) //Panel
    {
        PlaceHolder returnValue = default(PlaceHolder);

        Literal lit = default(Literal);
        Label lbl = new Label();

        returnValue = new PlaceHolder(); //Panel
        //StockUI.Attributes("style") &= "display:inline-block"
        // StockUI.Attributes("style") &= style

        //        If Me.shipments IsNot Nothing Then

        int c = 0;
        //If Me.shipments.Count > 0 Then

        foreach (var s in (from x in this.shipments.Values where x.IsCurrent select x).ToList())
        {

            Panel shipmentUI = new Panel();
            shipmentUI.ID = "S_" + s.ID; //Shipment ID (for this arrival of stock of this product variant)
            shipmentUI.CssClass = "S_" + s.ID + " Refresh";
            shipmentUI.Attributes("style") += "display:inline-block";

            c++;
            lbl = new Label();
            lbl.Text = getStock(channel, System.Convert.ToInt32(s.quantity), language);
            lbl.ToolTip = getLabelTooltipForStock(System.Convert.ToBoolean(s.IsCurrent), channel, System.Convert.ToDateTime(s.LastUpdated), System.Convert.ToDateTime(s.Arrival), System.Convert.ToInt32(s.quantity), language);
            shipmentUI.Controls.Add(lbl);
            returnValue.Controls.Add(shipmentUI);
            if (c == MaxShipments)
            {
                break;
            }
            lit = new Literal();
            lit.Text = "<br/>";
            returnValue.Controls.Add(lit);
        }
        //Else
        //'we have a bootstrap problem here when there is no stock record . . .
        //lbl = New Label
        //lbl.Text = "X" 'there are no shipments
        //lbl.ToolTip = Xlt("Unknown", language)
        //StockUI.Controls.Add(lbl)
        //End If
        //' End If

        return returnValue;
    }
    private string getLabelTooltipForStock(bool sIsCurrent, clsChannel channel, DateTime sLastupdated, DateTime sArrival, int sQuantity, clsLanguage language)
    {
        string result = string.Empty;
        if (sIsCurrent)
        {
            if (channel.BinaryStock && sQuantity > 0)
            {
                result = InStock.text(language) + Xlt(" (at ", language) + sLastupdated.ToString() + ")";
            }
            else if (channel.BinaryStock && sQuantity <= 0)
            {
                result = Xlt("arriving ", language) + sArrival;
            }
            else
            {
                result = sQuantity + Xlt(" in stock (at ", language) + sLastupdated.ToString() + ")";
            }
        }
        else
        {
            result = Xlt("arriving ", language) + sArrival;
        }
        return result;
    }
    /// <summary>
    /// Gets stock quantity or message in stock or out of stock for binarystock channels.
    /// </summary>
    /// <param name="channel">an instance of clsChannel.</param>
    /// <param name="value">An integer value that represents the quantity of stock.</param>
    /// <param name="language">an instance of clsLanguage.</param>
    /// <returns>A string object that represents the text or number to display.</returns>
    /// <remarks></remarks>
    private string getStock(clsChannel channel, int value, clsLanguage language)
    {
        string result = string.Empty;
        if (channel.BinaryStock && value > 0)
        {
            result = System.Convert.ToString(InStock.text(language));
        }
        else if (channel.BinaryStock && value <= 0)
        {
            result = System.Convert.ToString(OutOfStock.text(language));
        }
        else if (!channel.BinaryStock && value > 0)
        {
            result = value.ToString();
        }
        else if (!channel.BinaryStock && value <= 0)
        {
            result = "0";
        }
        return result;
    }

    public NullablePrice Price(clsAccount Buyeraccount) //clsPrice
    {
        NullablePrice returnValue = default(NullablePrice);

        returnValue = new NullablePrice(Buyeraccount.Currency);

        if (i_prices.ContainsKey(Buyeraccount.Priceband))
        {
            if (i_prices[Buyeraccount.Priceband].ContainsKey(Buyeraccount.Currency))
            {
                returnValue = i_prices[Buyeraccount.Priceband][Buyeraccount.Currency].Price;
            }
        }

        return returnValue;
    }

    //Public Function listPrice(currency As clsCurrency) As nullablePrice

    //    'we (should) already be working with the HP specific variant

    //    listPrice = New nullablePrice(currency)

    //    If Me.prices.ContainsKey(Everyone) Then
    //        If Me.prices(Everyone).ContainsKey(currency) Then
    //            listPrice = Me.prices(Everyone)(currency).Price
    //        End If
    //    End If

    //End Function


    public string get_displayName(clsLanguage language)
    {
        if (this.DisplayText == "")
        {
            return this.Warehouse + " " + this.DistiSku + " " + this.Localisation; // & " " & Me.OPG Me.Code & " " &
        }
        else
        {
            return this.DisplayText;
        }

    }

    public clsVariant()
    {
        //                            priceband/priceBand
        this.i_prices = new Dictionary<clsPriceBand, Dictionary<clsCurrency, clsPrice>>();
        this.prices = new Dictionary<int, clsPrice>();
        this.shipments = new SortedDictionary<DateTime, clsstock>();
        // Me.sellerChannel.i_variant_distisku.Add(Me.DistiSku, Me)

    }

    public void ArchiveCurrentStock()
    {

        //Removes the current stock record from the product.i_stock AND sets clears the 'current' Flag
        //There should only ever be one current stock record for each variant - it's arrival may be in the past
        //There may be many (historical) records (in the pas but isCurrent = false) .. these are absolute stock levels at their DateStamp.. (and could be used to graph stock over time)..

        DateTime toDel = default(DateTime);
        int IDtoArchive = 0;

        foreach (var s in this.shipments.Values)
        {
            if (s.IsCurrent) //And kvp.Value.Arrival.Date = Dzero Then
            {
                if (IDtoArchive != 0)
                {
                    Debugger.Break(); // more than one current stock  'TODO remove
                }
                toDel = System.Convert.ToDateTime(s.Arrival);
                IDtoArchive = System.Convert.ToInt32(s.ID);
            }
        }

        if (IDtoArchive == 0)
        {
            Debugger.Break(); //no current stock
        }
        this.shipments.Remove(toDel);

        //Remove it
        iq.Stock.Remove(IDtoArchive);

        //Remove them from the database (so they're not loaded next time)
        da.DBExecutesql("UPDATE stock SET [isCurrent]=0 WHERE ID =" + System.Convert.ToString(IDtoArchive));

    }

    public clsVariant(int ID, string Code, clsProduct Product, clsChannel sellerChannel, string DistiSku, string DisplayText, string Warehouse, string Localisation, clsRegion Region, bool deleted, bool CreateIndex) //, OPG As String)
    {

        this.ID = ID;
        this.Code = Code;
        this.Product = Product;
        this.sellerChannel = sellerChannel;
        this.DistiSku = DistiSku;
        this.DisplayText = DisplayText;
        this.Warehouse = Warehouse;
        this.Localisation = Localisation; //code like #ABU
        this.Region = Region;
        this.Deleted = deleted;

        //        Me.OPG = OPG

        //If Me.Product.i_Variants Is Nothing Then Me.Product.i_Variants = New Dictionary(Of clsChannel, List(Of clsVariant))
        if (!this.Product.i_Variants.ContainsKey(this.sellerChannel))
        {
            this.Product.i_Variants.Add(this.sellerChannel, new List<clsVariant>());
        }

        if (!this.Product.i_Variants(this.sellerChannel).Contains(this))
        {
            this.Product.i_Variants(this.sellerChannel).Add(this);
        }

        if (!Product.Variants.ContainsKey(this.ID)) //During the import, where we reload variants - it can already be in the product
        {
            this.Product.Variants.Add(this.ID, this);
        }

        this.i_prices = new Dictionary<clsPriceBand, Dictionary<clsCurrency, clsPrice>>(); //note, the variants don't have a list price - only the products do
        this.prices = new Dictionary<int, clsPrice>();

        //a variant can exist with an empty set of shipments (but it will be populated as soon as a stock record is made)
        this.shipments = new SortedDictionary<DateTime, clsstock>();

        if (ID != -1)
        {
            if (!iq.Variants.ContainsKey(this.ID)) //Check is required beacuse a webservice call can have loaded variants
            {
                iq.Variants.Add(this.ID, this);
            }
        }

        Ocode = this.Code;

        if (CreateIndex)
        {
            this.sellerChannel.indexVariant(this);
        }


    }

    public clsVariant Insert()
    {

        return new clsVariant(this.Code, this.Product, this.sellerChannel, this.DistiSku, this.DisplayText, this.Warehouse, this.Localisation, this.Region, false, null, -1); //, Me.OPG)

    }

    public void Update()
    {

        //this assumes we're not changing the product or the seller channel

        string rid = "null";
        if (this.Region != null)
        {
            rid = System.Convert.ToString(this.Region.ID);
        }

        object sql = null;
        sql = "UPDATE [variant] set code=" + da.SqlEncode(this.Code) + ",displaytext=" + da.SqlEncode(this.DisplayText) + ",Warehouse=" + da.SqlEncode(this.Warehouse) + ",localisation=" + da.SqlEncode(this.Localisation) + ",fk_region_id=" + rid + ",Deleted=" + System.Convert.ToString(this.Deleted ? "1" : "0") + " WHERE ID=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql);

    }

    public void Delete(List<string> errorMessages)
    {

        //QuoteItems reference Variants - so we cannot delete them per se

        object sql = null;
        sql = "DELETE FROM stock WHERE fk_variant_id=" + System.Convert.ToString(this.ID); //Deletes all stock (inlcuding archived)
        da.DBExecutesql(sql);

        sql = "DELETE FROM price WHERE fk_variant_id=" + System.Convert.ToString(this.ID); //Deletes all prices (inlcuding archived)
        da.DBExecutesql(sql);

        sql = "UPDATE [VARIANT] set deleted = 1 where ID=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql);

        this.Product.i_Variants(this.sellerChannel).Remove(this);
        if (this.Product.i_Variants(this.sellerChannel).Count() == 0)
        {
            this.Product.i_Variants.Remove(this.sellerChannel);
        }
        this.sellerChannel.deIndexVariant(this, errorMessages); //we remove the REFERENCE from the index


    }

    public bool PriceExists(clsPriceBand Priceband, clsCurrency currency)
    {
        bool returnValue = false;

        returnValue = false;

        if (this.i_prices.ContainsKey(Priceband))
        {
            if (this.i_prices[Priceband].ContainsKey(currency))
            {
                returnValue = true;
            }
        }

        return returnValue;
    }

    public NullablePrice BasePrice(clsCurrency currency)
    {
        NullablePrice returnValue = default(NullablePrice);

        //Returns a raw base price - NOT factored by any margin - typically this would be a cost price (it might just occasionally be a list price)

        returnValue = new NullablePrice(currency);
        if (this.i_prices.ContainsKey(Everyone))
        {
            if (this.i_prices[Everyone].ContainsKey(currency))
            {
                returnValue = this.i_prices[Everyone][currency].Price;
            }
        }

        return returnValue;
    }

    public clsVariant(string code, clsProduct Product, clsChannel sellerChannel, string DistiSku, string DisplayText, string Warehouse, string Localisation, clsRegion Region, bool deleted, DataTable WriteCache, ref int nextId)
    {

        if (WriteCache == null)
        {
            string sql = "INSERT INTO [Variant] (code,distisku,fk_channel_id_seller,fk_product_id,displayText,warehouse,localisation,fk_region_id,deleted) VALUES(";
            string rid = "";
            if (Region == null)
            {
                rid = "null";
            }
            else
            {
                rid = System.Convert.ToString(Region.ID);
            }
            sql += da.SqlEncode(code) + "," + da.SqlEncode(DistiSku) + "," + sellerChannel.ID + "," + Product.ID + "," + da.SqlEncode(DisplayText) + "," + da.SqlEncode(Warehouse) + "," + da.SqlEncode(Localisation) + "," + rid + ",0);";
            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        }
        else
        {

            System.Data.DataRow row = default(System.Data.DataRow);
            row = WriteCache.NewRow();

            row["code"] = code;
            row["fk_product_id"] = Product.ID;
            row["fk_channel_id_seller"] = sellerChannel.ID;
            row["distisku"] = DistiSku;
            row["displaytext"] = DisplayText;
            row["warehouse"] = Warehouse;
            row["localisation"] = Localisation;
            if (Region == null)
            {
                row["fk_region_id"] = DBNull.Value;
            }
            else
            {
                row["fk_region_id"] = Region.ID;
            }
            row["deleted"] = deleted;

            //row("opg") = opg
            if (nextId != -1)
            {
                this.ID = nextId;
                row["id"] = nextId;
                nextId++;
            }

            WriteCache.Rows.Add(row);
        }

        this.Code = code;
        this.DistiSku = DistiSku;
        this.DisplayText = DisplayText;
        this.Warehouse = Warehouse;
        this.Localisation = Localisation;
        //Me.OPG = opg
        this.Product = Product;
        this.sellerChannel = sellerChannel;
        this.Region = Region;
        this.Deleted = deleted;

        //                                                                                                              seller

        if (this.Product.i_Variants == null)
        {
            this.Product.i_Variants = new Dictionary<clsChannel, List<clsVariant>>();
        }
        if (!this.Product.i_Variants.ContainsKey(this.sellerChannel))
        {
            this.Product.i_Variants.Add(this.sellerChannel, new List<clsVariant>());
        }

        this.Product.i_Variants(this.sellerChannel).Add(this);
        this.Product.Variants.Add(this.ID, this);

        this.i_prices = new Dictionary<clsPriceBand, Dictionary<clsCurrency, clsPrice>>();
        this.prices = new Dictionary<int, clsPrice>();

        this.shipments = new SortedDictionary<DateTime, clsstock>();

        //iq.Variants.Add(ID, Me)
        //iq.i_variant_code.Add(Me.Code, Me)
        if (this.ID > 0 && !iq.Variants.ContainsKey(this.ID))
        {
            iq.Variants.Add(this.ID, this);
        }

        this.sellerChannel.indexVariant(this);



    }

    public bool HasListPrice(clsCurrency currency)
    {
        bool returnValue = false;

        returnValue = false;

        if (this.i_prices.ContainsKey(Everyone))
        {
            if (this.i_prices[Everyone].ContainsKey(currency))
            {
                return true;
            }
        }

        return returnValue;
    }

}
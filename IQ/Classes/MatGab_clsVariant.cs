using dataAccess;

public class clsVariant
{

	//Every product has one or more variants 
	//These are the disti specific versions of the the product and carry the disti Skus (hostPartNums) - amongst other things
	//Variants allow a product to have one or more stock level(s) and/or price(s) (although neither is required)
	//They're also used to allow (list) prices to be per region(country)
	//There must be a HP varant for each region (country)  for which there is a list price - as it is the variants that link Products, Prices and Regions.

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	// there are at least as many variants as parts
	private clsChannel sellerChannel {
		get { return m_sellerChannel; }
		set { m_sellerChannel = Value; }
	}
	private clsChannel m_sellerChannel;
	//Seller channel (disti)
	private string DistiSku {
		get { return m_DistiSku; }
		set { m_DistiSku = Value; }
	}
	private string m_DistiSku;
	//Distis internal part number - can be different for different variants of the same (HP) part (by localisation,  warehouse, code etc)
	private clsProduct Product {
		get { return m_Product; }
		set { m_Product = Value; }
	}
	private clsProduct m_Product;
	private string Code {
		get { return m_Code; }
		set { m_Code = Value; }
	}
	private string m_Code;
	//Human friendly code such as 'b-grade'
	private string DisplayText {
		get { return m_DisplayText; }
		set { m_DisplayText = Value; }
	}
	private string m_DisplayText;
	//Optional overriding text
	private string Warehouse {
		get { return m_Warehouse; }
		set { m_Warehouse = Value; }
	}
	private string m_Warehouse;
	//disti warehouse code (3 chars, Human readable ?) – could be extended to an object – with GPS cords, and customer-warehouse restrictions
	private string Localisation {
		get { return m_Localisation; }
		set { m_Localisation = Value; }
	}
	private string m_Localisation;
	//#ABU, #ABA etc - this is just a text string - no fucntionality is driven from it
	private clsRegion Region {
		get { return m_Region; }
		set { m_Region = Value; }
	}
	private clsRegion m_Region;
	//Mostly so list prices can be defined per country - but potentially useful for international distis - Can be nothing.
	private bool Deleted {
		get { return m_Deleted; }
		set { m_Deleted = Value; }
	}
	private bool m_Deleted;

	//prices *can* be empty - the actual [Variant] merely joins a Seller, Product, Warehouse and DistiSKU

	//i_Prices (which is an index on THIS variants (Unique Disti wharehouse/product/part) ..Is indexed by PriceBand - eg. 'A' or a host AccountNumber - which can be thought of as a very 'narrow band'
		//how to edit this (ie.. index by the objects).. would allow the iq.variants to be removed and the direct editing of prices
	public Dictionary<clsPriceBand, Dictionary<clsCurrency, clsPrice>> i_prices;
	private Dictionary<int, clsPrice> prices {
		get { return m_prices; }
		set { m_prices = Value; }
	}
	private Dictionary<int, clsPrice> m_prices;

	//listprices are under the 'everyone' buyer channel and are blended in in getprices()
	//Listprices are per currency/region

	private SortedDictionary<DateTime, clsStock> shipments {
		get { return m_shipments; }
		set { m_shipments = Value; }
	}
	private SortedDictionary<DateTime, clsStock> m_shipments;

	string Ocode;

	public clsVariant clone(clsProduct Product)
	{

		clone = new clsVariant(this.Code, Product, this.sellerChannel, this.DistiSku, this.DisplayText, this.Warehouse, this.Localisation, this.Region, this.Deleted);

		// blank band is the 'everyone' price
		if (this.i_prices.ContainsKey(iq.getPriceBand(""))) {
			foreach ( p in this.i_prices(Everyone).Values) {
				p.clone(clone);
				//clones the price list onto the newly cloned variant
			}
		}

	}

	public clsPrice priceFor(clsPriceBand PriceBand, clsCurrency currency)
	{

		priceFor = null;

		if (i_prices.ContainsKey(PriceBand)) {
			if (i_prices(PriceBand).ContainsKey(currency)) {
				priceFor = i_prices(PriceBand)(currency);
			}
		}

	}

	/// <summary>Returns the unique 'compound key' for this variant - which is the distiSKU|warehouse -OR HPSKU|region.code</summary>
	/// <returns></returns>
	/// <remarks>The compound key is much like a database index - and is used with the channel.i_variantCK to ensure uniqueness - and for efficient access to Variants - note, warehouse is often (generally) blank</remarks>
	public string CK()
	{

		//For most Disti Channels - variants are indexed by (i.e. the unique key is DistiSKU|Warehouse
		//For HP - the compound key (and hp.i_variantCK) indexes ListPrice variants by Region.code
		//note... there can be more than one price for the same variant.. in different currencies

		if (object.ReferenceEquals(this.sellerChannel, HP)) {
			return this.DistiSku + "|" + this.Region.Code;
		} else {
			return this.DistiSku + "|" + this.Warehouse;
		}

	}


	/// <summary>returns UI for up to MaxShipment shipments of this productVariant</summary>
	public PlaceHolder StockUI(int MaxShipments, string style, clsLanguage language, clsChannel channel)
	{
		//Panel

		Literal lit;
		Label lbl = new Label();

		StockUI = new PlaceHolder();
		//Panel
		//StockUI.Attributes("style") &= "display:inline-block"
		// StockUI.Attributes("style") &= style

		//        If Me.shipments IsNot Nothing Then

		int c = 0;
		//If Me.shipments.Count > 0 Then


		foreach ( s in (from x in this.shipments.Valueswhere x.IsCurrent).ToList()) {
			Panel shipmentUI = new Panel();
			shipmentUI.ID = "S_" + s.ID;
			//Shipment ID (for this arrival of stock of this product variant)
			shipmentUI.CssClass = "S_" + s.ID + " Refresh";
			shipmentUI.Attributes("style") += "display:inline-block";

			c = c + 1;
			lbl = new Label();
			lbl.Text = getStock(channel, s.quantity, language);
			lbl.ToolTip = getLabelTooltipForStock(s.IsCurrent, channel, s.LastUpdated, s.Arrival, s.quantity, language);
			shipmentUI.Controls.Add(lbl);
			StockUI.Controls.Add(shipmentUI);
			if (c == MaxShipments)
				break; // TODO: might not be correct. Was : Exit For
			lit = new Literal();
			lit.Text = "<br/>";
			StockUI.Controls.Add(lit);
		}
		//Else
		//'we have a bootstrap problem here when there is no stock record . . .
		//lbl = New Label
		//lbl.Text = "X" 'there are no shipments 
		//lbl.ToolTip = Xlt("Unknown", language)
		//StockUI.Controls.Add(lbl)
		//End If
		//' End If

	}
	private string getLabelTooltipForStock(bool sIsCurrent, clsChannel channel, System.DateTime sLastupdated, System.DateTime sArrival, int sQuantity, clsLanguage language)
	{
		string result = string.Empty;
		if (sIsCurrent) {
			if (channel.BinaryStock & sQuantity > 0) {
				result = InStock.text(language) + Xlt(" (at ", language) + sLastupdated.ToString + ")";
			} else if (channel.BinaryStock & sQuantity <= 0) {
				result = Xlt("arriving ", language) + sArrival;
			} else {
				result = sQuantity + Xlt(" in stock (at ", language) + sLastupdated.ToString + ")";
			}
		} else {
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
		if (channel.BinaryStock & value > 0) {
			result = InStock.text(language);
		} else if (channel.BinaryStock & value <= 0) {
			result = OutOfStock.text(language);
		} else if (!channel.BinaryStock & value > 0) {
			result = value.ToString;
		} else if (!channel.BinaryStock & value <= 0) {
			result = "0";
		}
		return result;
	}

	public NullablePrice Price(clsAccount Buyeraccount)
	{
		//clsPrice

		Price = new NullablePrice(Buyeraccount.Currency);

		if (i_prices.ContainsKey(Buyeraccount.Priceband)) {
			if (i_prices(Buyeraccount.Priceband).ContainsKey(Buyeraccount.Currency)) {
				Price = i_prices(Buyeraccount.Priceband)(Buyeraccount.Currency).Price;
			}
		}

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

	private string displayName {

		get {
			if (this.DisplayText == "") {
				return this.Warehouse + " " + this.DistiSku + " " + this.Localisation;
				// & " " & Me.OPG Me.Code & " " &
			} else {
				return this.DisplayText;
			}

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

		System.DateTime toDel;
		int IDtoArchive = 0;

		foreach ( s in this.shipments.Values) {
			//And kvp.Value.Arrival.Date = Dzero Then
			if (s.IsCurrent) {
				if (IDtoArchive != 0)
					System.Diagnostics.Debugger.Break();
				// more than one current stock  'TODO remove
				toDel = s.Arrival;
				IDtoArchive = s.ID;
			}
		}

		if (IDtoArchive == 0)
			System.Diagnostics.Debugger.Break();
		//no current stock
		this.shipments.Remove(toDel);

		//Remove it
		iq.Stock.Remove(IDtoArchive);

		//Remove them from the database (so they're not loaded next time)
		da.DBExecutesql("UPDATE stock SET [isCurrent]=0 WHERE ID =" + IDtoArchive);

	}

	public clsVariant(int ID, string Code, clsProduct Product, clsChannel sellerChannel, string DistiSku, string DisplayText, string Warehouse, string Localisation, clsRegion Region, bool deleted,
	//, OPG As String)
	bool CreateIndex)
	{

		this.ID = ID;
		this.Code = Code;
		this.Product = Product;
		this.sellerChannel = sellerChannel;
		this.DistiSku = DistiSku;
		this.DisplayText = DisplayText;
		this.Warehouse = Warehouse;
		this.Localisation = Localisation;
		//code like #ABU
		this.Region = Region;
		this.Deleted = deleted;

		//        Me.OPG = OPG

		//If Me.Product.i_Variants Is Nothing Then Me.Product.i_Variants = New Dictionary(Of clsChannel, List(Of clsVariant))
		if (!this.Product.i_Variants.ContainsKey(this.sellerChannel))
			this.Product.i_Variants.Add(this.sellerChannel, new List<clsVariant>());

		if (!this.Product.i_Variants(this.sellerChannel).Contains(this)) {
			this.Product.i_Variants(this.sellerChannel).Add(this);
		}

		//During the import, where we reload variants - it can already be in the product
		if (!Product.Variants.ContainsKey(this.ID)) {
			this.Product.Variants.Add(this.ID, this);
		}

		this.i_prices = new Dictionary<clsPriceBand, Dictionary<clsCurrency, clsPrice>>();
		//note, the variants don't have a list price - only the products do
		this.prices = new Dictionary<int, clsPrice>();

		//a variant can exist with an empty set of shipments (but it will be populated as soon as a stock record is made)
		this.shipments = new SortedDictionary<DateTime, clsstock>();

		if (ID != -1) {
			//Check is required beacuse a webservice call can have loaded variants 
			if (!iq.Variants.ContainsKey(this.ID)) {
				iq.Variants.Add(this.ID, this);
			}
		}

		Ocode = this.Code;

		if (CreateIndex)
			this.sellerChannel.indexVariant(this);


	}

	public clsVariant Insert()
	{

		return new clsVariant(this.Code, this.Product, this.sellerChannel, this.DistiSku, this.DisplayText, this.Warehouse, this.Localisation, this.Region, 0);
		//, Me.OPG)

	}


	public void Update()
	{
		//this assumes we're not changing the product or the seller channel

		string rid = "null";
		if (this.Region != null)
			rid = this.Region.ID;

		object sql;
		sql = "UPDATE [variant] set code=" + da.SqlEncode(this.Code) + ",displaytext=" + da.SqlEncode(this.DisplayText) + ",Warehouse=" + da.SqlEncode(this.Warehouse) + ",localisation=" + da.SqlEncode(this.Localisation) + ",fk_region_id=" + rid + ",Deleted=" + IIf(this.Deleted, "1", "0") + " WHERE ID=" + this.ID;
		da.DBExecutesql(sql);

	}


	public void Delete(List<string> errorMessages)
	{
		//QuoteItems reference Variants - so we cannot delete them per se

		object sql;
		sql = "DELETE FROM stock WHERE fk_variant_id=" + this.ID;
		//Deletes all stock (inlcuding archived)
		da.DBExecutesql(sql);

		sql = "DELETE FROM price WHERE fk_variant_id=" + this.ID;
		//Deletes all prices (inlcuding archived)
		da.DBExecutesql(sql);

		sql = "UPDATE [VARIANT] set deleted = 1 where ID=" + this.ID;
		da.DBExecutesql(sql);

		this.Product.i_Variants(this.sellerChannel).Remove(this);
		if (this.Product.i_Variants(this.sellerChannel).Count == 0)
			this.Product.i_Variants.Remove(this.sellerChannel);
		this.sellerChannel.deIndexVariant(this, errorMessages);
		//we remove the REFERENCE from the index


	}

	public bool PriceExists(clsPriceBand Priceband, clsCurrency currency)
	{

		PriceExists = false;

		if (this.i_prices.ContainsKey(Priceband)) {
			if (this.i_prices(Priceband).ContainsKey(currency)) {
				PriceExists = true;
			}
		}

	}

	public NullablePrice BasePrice(clsCurrency currency)
	{

		//Returns a raw base price - NOT factored by any margin - typically this would be a cost price (it might just occasionally be a list price)

		BasePrice = new NullablePrice(currency);
		if (this.i_prices.ContainsKey(Everyone)) {
			if (this.i_prices(Everyone).ContainsKey(currency)) {
				BasePrice = this.i_prices(Everyone)(currency).Price;
			}
		}

	}

	public clsVariant(string code, clsProduct Product, clsChannel sellerChannel, string DistiSku, string DisplayText, string Warehouse, string Localisation, clsRegion Region, bool deleted, ref DataTable WriteCache = null,

	ref int nextId = -1)
	{
		if (WriteCache == null) {
			object sql = "INSERT INTO [Variant] (code,distisku,fk_channel_id_seller,fk_product_id,displayText,warehouse,localisation,fk_region_id,deleted) VALUES(";
			string rid;
			if (Region == null)
				rid = "null";
			else
				rid = Region.ID;
			sql += da.SqlEncode(code) + "," + da.SqlEncode(DistiSku) + "," + sellerChannel.ID + "," + Product.ID + "," + da.SqlEncode(DisplayText) + "," + da.SqlEncode(Warehouse) + "," + da.SqlEncode(Localisation) + "," + rid + ",0);";
			this.ID = da.DBExecutesql(sql, true);

		} else {
			System.Data.DataRow row;
			row = WriteCache.NewRow();

			row("code") = code;
			row("fk_product_id") = Product.ID;
			row("fk_channel_id_seller") = sellerChannel.ID;
			row("distisku") = DistiSku;
			row("displaytext") = DisplayText;
			row("warehouse") = Warehouse;
			row("localisation") = Localisation;
			if (Region == null) {
				row("fk_region_id") = DBNull.Value;
			} else {
				row("fk_region_id") = Region.ID;
			}
			row("deleted") = deleted;

			//row("opg") = opg
			if (nextId != -1) {
				this.ID = nextId;
				row("id") = nextId;
				nextId += 1;
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
			this.Product.i_Variants = new Dictionary<clsChannel, List<clsVariant>>();
		if (!this.Product.i_Variants.ContainsKey(this.sellerChannel))
			this.Product.i_Variants.Add(this.sellerChannel, new List<clsVariant>());

		this.Product.i_Variants(this.sellerChannel).Add(this);
		this.Product.Variants.Add(this.ID, this);

		this.i_prices = new Dictionary<clsPriceBand, Dictionary<clsCurrency, clsPrice>>();
		this.prices = new Dictionary<int, clsPrice>();

		this.shipments = new SortedDictionary<DateTime, clsstock>();

		//iq.Variants.Add(ID, Me)
		//iq.i_variant_code.Add(Me.Code, Me)
		if (this.ID > 0 && !iq.Variants.ContainsKey(this.ID)) {
			iq.Variants.Add(this.ID, this);
		}

		this.sellerChannel.indexVariant(this);



	}

	public bool HasListPrice(clsCurrency currency)
	{

		HasListPrice = false;

		if (this.i_prices.ContainsKey(Everyone)) {
			if (this.i_prices(Everyone).ContainsKey(currency)) {
				return true;
			}
		}

	}

}

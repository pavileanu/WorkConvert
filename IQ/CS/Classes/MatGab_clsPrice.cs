using dataAccess;

public class clsPrice
{

	//A price links a Buyer and a SkuVariant (which is a Seller, product, warehouse combo)

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	// Public Product As clsProduct            
	//    Property Seller As clsChannel 'note - there is no seller, op product here.. they come from the variant
	private clsVariant SKUVariant {
		get { return m_SKUVariant; }
		set { m_SKUVariant = Value; }
	}
	private clsVariant m_SKUVariant;
	//    Property Buyer As clsChannel        'can be 'Everyone' (for list prices)
	private clsPriceBand PriceBand {
		get { return m_PriceBand; }
		set { m_PriceBand = Value; }
	}
	private clsPriceBand m_PriceBand;
	private nullablePrice Price {
		get { return m_Price; }
		set { m_Price = Value; }
	}
	private nullablePrice m_Price;
	//contains the currency, and message
	private Dictionary<int, clsOffer> Offers {
		get { return m_Offers; }
		set { m_Offers = Value; }
	}
	private Dictionary<int, clsOffer> m_Offers;
	//Not implemented (a generalised offer framework to encompass Flex, Avalanche, Bundles and more)

	private System.DateTime lastRequested {
		get { return m_lastRequested; }
		set { m_lastRequested = Value; }
	}
	private System.DateTime m_lastRequested;
	//when it was last requested (webservice request fired off)
	private System.DateTime lastUpdated {
		get { return m_lastUpdated; }
		set { m_lastUpdated = Value; }
	}
	private System.DateTime m_lastUpdated;
	//When it was last successfully updated (webservice result/update)

	private string Source {
		get { return m_Source; }
		set { m_Source = Value; }
	}
	private string m_Source;

		//temporary negative ID used to allow late INSERTS (when the webservice returns a price)
	public int tempID;

	public clsPrice insert()
	{

		return new clsPrice(this.SKUVariant, this.PriceBand, this.Price, this.Source);

	}

	public clsPrice clone(clsVariant SkuVariant)
	{

		clone = new clsPrice(SkuVariant, this.PriceBand, this.Price, "cloned");

	}


	public clsPrice(clsAccount buyeraccount, clsVariant SKUvariant)
	{
		//POA - NB: Does not add the price to the Product, or Insert to the database

		this.ID = -1;
		//Me.Product = Product
		this.SKUVariant = SKUvariant;
		this.Price = new nullablePrice(buyeraccount.Currency);
		this.priceband = buyeraccount.Priceband;
		//       Me.Seller = buyeraccount.SellerChannel
		this.Source = "Contact the seller for current pricing";
		this.lastRequested = DateAdd(DateInterval.Day, -1, Now);
		this.lastUpdated = this.lastRequested;
		this.Offers = new Dictionary<int, clsOffer>();

		if (this.SKUVariant != null) {
			this.SKUVariant.i_prices.Add(buyeraccount.Priceband, new Dictionary<clsCurrency, clsPrice>());
			this.SKUVariant.i_prices(buyeraccount.Priceband).Add(buyeraccount.Currency, this);
			this.SKUVariant.prices.Add(this.ID, this);
		}

	}



	public clsPrice(clsPrice BasePrice, float Factor)
	{
		//creates a new price - from an exsisting (typically list) price - multiplied by the specified factor (margin based pricing

		this.ID = -1;
		this.SKUVariant = BasePrice.SKUVariant;
		this.Price = new NullablePrice(BasePrice.Price.value, BasePrice.Price.currency, false);
		//Note - we do not preserve isList - as multiplying a list price by a factor... makes it no longer a list price ! (OK.. unless it's 1 - smart arse)

		this.PriceBand = BasePrice.PriceBand;
		this.Source = "Margin based price";
		this.Price.Message = "Estimated (Margin based) price";

		this.Offers = new Dictionary<int, clsOffer>();
		//                                                                                                   buyer
		//the vairant holds a dictionary of prices ..  Property prices As Dictionary(Of clsChannel, Dictionary(Of clsCurrency, clsPrice))  'TODO fill ??

		//this is a special constructore - we don't want to add marging based prices to the editable dictionary

		//     Me.SKUVariant.i_prices(Me.Buyer).Add(BasePrice.Price.currency, Me)
		//     Me.SKUVariant.prices.Add(Me.ID, Me)

	}


	public clsPrice()
	{
		this.Offers = new Dictionary<int, clsOffer>();

	}


	//Public Sub New(product As clsProduct, SKUvariant As clsVariant, buyeraccount As clsAccount, source As String)

	//    'this particular oveload is *just* to make the temporary 'POA' ones


	//    Dim POAprice As clsPrice
	//    POAprice = New clsPrice(product, SKUvariant, buyeraccount.SellerChannel, buyeraccount.BuyerChannel, New nullablePrice(buyeraccount.Currency), "Webservice")
	//    Return POAprice

	//    'Me.ID = -1
	//    'Me.Product = product
	//    'Me.SKUVariant = SKUvariant
	//    'Me.Seller = buyeraccount.SellerChannel
	//    'Me.Price = New nullablePrice(buyeraccount.Currency)
	//    'Me.Buyer = Buyer
	//    'Me.Source = "Requesting Price . ."
	//    'Me.Price.Message = ""

	//    'Me.Offers = New Dictionary(Of Integer, clsOffer)

	//End Sub



	public static object temporaryID()
	{

		//assigned the next avialble (and negative) temporary ID for a (in memory only for now) clsPrice
		//INSERTs which would normally yield an ID are very slow - and we don't actually want to persist a record until it has a price (back from the webserivce) anyway


		object @lock = new object();

		lock (@lock) {
			static int countdown;
			countdown -= 1;

			temporaryID = countdown;
		}



	}


	public clsPrice(clsVariant SKUvariant, clsPriceBand Priceband, NullablePrice price, string source, ref DataTable writecache = null, ref int nextID = -1)
	{
		//If buyer Is Nothing Then Stop

		Pmark("Price_NEW (db)");

		if (writecache == null) {
			object sql;


			if (price.sqlvalue == 0) {
				this.ID = temporaryID();

			} else {
				sql = "INSERT INTO PRICE(fk_variant_id,priceband,price,fk_currency_id,datestamp,source) VALUES ";
				sql += "(" + SKUvariant.ID + "," + da.SqlEncode(Priceband.text) + "," + price.sqlvalue + "," + price.currency.ID + ",getdate()," + da.SqlEncode(source) + ");";

				try {
					this.ID = da.DBExecutesql(sql, true);


				} catch (System.Exception ex) {
					Logit(ex.Message.ToString);
					if (ex.InnerException != null) {
						Logit(ex.InnerException.Message.ToString);
					}
					Logit("In clsPrice_New " + sql);
					//Stop

					//Beep()

				}
			}



		} else {
			this.ID = nextID;
			//
			nextID += 1;
			System.Data.DataRow row;
			row = writecache.NewRow();
			//  row("FK_product_id") = product.ID
			//  row("FK_Channel_id_seller") = seller.ID

			if (nextID != -1)
				row("ID") = nextID;
			//NEW

			row("FK_variant_id") = SKUvariant.ID;

			row("Priceband") = Priceband.text;
			//If Buyer Is Nothing Then
			//    row("FK_Channel_id_buyer") = DBNull.Value
			//Else
			//    row("FK_Channel_id_buyer") = buyer.ID
			//End If

			row("price") = price.value;
			row("fk_currency_id") = price.currency.ID;
			row("fk_variant_id") = SKUvariant.ID;
			row("datestamp") = Now;
			row("source") = source + "(bulk)";

			writecache.Rows.Add(row);

		}

		this.Price = price;
		this.SKUVariant = SKUvariant;
		this.lastUpdated = Now;
		this.lastRequested = Now;

		this.Source = source;
		this.PriceBand = Priceband;

		//add myself to the master price list
		//  iq.Prices.Add(Me.ID, Me)

		//add into the products i_prices  - Obsoleted as the Products.Variants(sellerchannel) now provides a natural index
		//SKUvariant.Product.AddPrice(Me)

		this.Offers = new Dictionary<int, clsOffer>();
		if (!this.SKUVariant.i_prices.ContainsKey(Priceband)) {
			this.SKUVariant.i_prices.Add(this.PriceBand, new Dictionary<clsCurrency, clsPrice>());
		}


		if (this.SKUVariant.i_prices(this.PriceBand).ContainsKey(this.Price.currency)) {
			this.SKUVariant.i_prices(this.PriceBand)(this.Price.currency) = this;
		} else {
			this.SKUVariant.i_prices(this.PriceBand).Add(this.Price.currency, this);
		}

		if (this.ID == 0)
			throw new Exception("attempted to add a price with an ID of 0");

		//for an insert WITHOUt the write cache - me.ID will be the ID generated by the SQL INSERT
		if (this.ID != -1) {
			this.SKUVariant.prices.Add(this.ID, this);
		}

		//TODO prices rely on DB
		if (this.ID != -1 & da.DatabaseAlive) {
			iq.Prices.Add(this.ID, this);
		}

		Pacc("Price_NEW (db)");

	}

	public Panel Ui(clsAccount buyeraccount, float margin, UInt64 lid)
	{

		List<string> errorMessages = new List<string>();

		Ui = new Panel();

		//If Me.SKUVariant Is Nothing Or Me.SKUVariant.Product Is Nothing Then
		//    lbl.Text = "POA"
		//    Ui.BackColor = Drawing.Color.Red
		//    lbl.ToolTip = "Price " & Me.ID & " has no Product "
		//Else

		Ui.ID = "P_" + this.ID;
		Ui.CssClass = "P_" + this.ID;
		Ui.CssClass += " Refresh";
		//this class doesn't exist - but is used by the script to identify those elements to refresh - so DONT REMOVE IT !!!
		Ui.CssClass += " PriceUI";

		if (this.MinutesOld > 60)
			Ui.CssClass += " unconfirmed";
		else
			Ui.CssClass += " upToDate";

		NullablePrice priceIncludingMargin = this.Price * margin;
		//lbl.Text = Me.Price * margin.DisplayPrice(buyeraccount, errorMessages).Text

		Panel pp = priceIncludingMargin.DisplayPrice(buyeraccount, errorMessages);

		//'REINSTATE for price source info
		if (false) {
			pp.ToolTip = this.Source + vbCrLf;
			pp.Attributes("Style") += "background-color:blue;padding:5px;";


			 // ERROR: Not supported in C#: WithStatement

		}

		//Brazil Changes
		if (buyeraccount.BuyerChannel.Region.Code == "BR") {
			pp.ToolTip = iq.AddTranslation("For actual prices and stock levels, please set the Customer Context", English, "", 1, null, 0, false).text(buyeraccount.Language);
		}

		// End If

		Ui.Controls.Add(pp);
		//price panel
		OutputErrors(Ui.Controls, errorMessages, lid);

	}
	public int MinutesOld()
	{
		return DateDiff(DateInterval.Minute, this.lastUpdated, Now);
		//how old is it at the moment (when was it last *requested* ) 
	}

	public clsPrice Update()
	{

		//If Me.ID = -1 Then Stop 'TODO REMOVE
		//If LCase(Me.Price.sqlvalue) = "null" Then Stop

		this.lastUpdated = Now;

		object sql;
		sql = "UPDATE PRICE";
		sql += " SET price=" + this.Price.sqlvalue + ",";
		sql += " datestamp=" + da.UniversalDate(Now);
		if (this.Source != null) {
			sql += ",source=" + da.SqlEncode(this.Source);
		}
		sql += " WHERE ID=" + this.ID;

		da.DBExecutesql(sql);

		return this;

	}


	public clsPrice(int id, clsVariant SKUvariant, clsPriceBand priceband, decimal price, clsCurrency currency, System.DateTime datestamp, string source)
	{
		//If buyer Is Nothing Then Stop

		this.ID = id;
		this.SKUVariant = SKUvariant;
		// Me.Buyer = buyer

		bool islistPrice = (object.ReferenceEquals(SKUvariant.sellerChannel, HP)) & priceband.text == "";
		//=(buyer Is Everyone)

		this.Price = new NullablePrice(price, currency, islistPrice);
		this.lastUpdated = datestamp;
		//NOT Now (otherwise datestamps are not restored from the database properly)
		this.lastRequested = datestamp;
		this.Source = source;

		this.Offers = new Dictionary<int, clsOffer>();

		//add myself to the master price list
		iq.Prices.Add(this.ID, this);
		this.PriceBand = priceband;


		this.SKUVariant.prices.Add(this.ID, this);
		if (!this.SKUVariant.i_prices.ContainsKey(this.PriceBand))
			this.SKUVariant.i_prices.Add(this.PriceBand, new Dictionary<clsCurrency, clsPrice>());

		if (this.SKUVariant.i_prices(this.PriceBand).ContainsKey(currency)) {
			// Beep() 'ut oh - we already had a price for that buyer in that currency
			bool a = false;
		} else {
			this.SKUVariant.i_prices(this.PriceBand).Add(currency, this);
		}

	}

}

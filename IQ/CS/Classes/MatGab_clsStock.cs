using dataAccess;

public class clsstock
{
	//Should really be called 'shipments' - many can exist for a single variant (future shipments) 

	//Shipments are (real or predicted) absolute stock positions 
	//ONE shipment is flagged as current

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	//Public Product As clsProduct  'We save memory by *not* having there (at the expense of a more difficult lookup from the Stock Instance)
	// Property Seller As clsChannel
	private clsVariant SKUvariant {
		get { return m_SKUvariant; }
		set { m_SKUvariant = Value; }
	}
	private clsVariant m_SKUvariant;
	private int quantity {
		get { return m_quantity; }
		set { m_quantity = Value; }
	}
	private int m_quantity;
	private DateTime Arrival {
		get { return m_Arrival; }
		set { m_Arrival = Value; }
	}
	private DateTime m_Arrival;
	//The point in time at which this (total) quantity will be available
	private System.DateTime LastUpdated {
		get { return m_LastUpdated; }
		set { m_LastUpdated = Value; }
	}
	private System.DateTime m_LastUpdated;
	//timestamp of the record
	private string Source {
		get { return m_Source; }
		set { m_Source = Value; }
	}
	private string m_Source;
	private bool IsCurrent {
		get { return m_IsCurrent; }
		set { m_IsCurrent = Value; }
	}
	private bool m_IsCurrent;
	//only one stock record per product/variant/seller channel can be current (enforced by a unique index)

	//    Dim oSeller As clsChannel
	//    Dim oProduct As clsProduct
	clsVariant oSKUVariant;

	System.DateTime oArrival;

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


	public clsstock(clsVariant SKUvariant, int Quantity, System.DateTime Arrival, string Source, bool iscurrent)
	{
		//If Arrival.Date = CDate("01/01/2000").Date Then
		// Me.IsCurrent = True
		// End If

		//If Me.IsCurrent Then Arrival = CDate("01/01/2000").Date

		//    Me.Product = Product
		//    Me.Seller = Seller
		this.SKUvariant = SKUvariant;
		this.quantity = Quantity;
		this.Arrival = Arrival;
		this.LastUpdated = Now;
		this.IsCurrent = iscurrent;


			//Because some rows are duplictaed in the feeds

			//stock is no longer in the products - but is global
			//If Not .Product.i_Stock.ContainsKey(.sellerChannel) Then .Product.i_Stock.Add(.sellerChannel, New Dictionary(Of clsVariant, SortedDictionary(Of Date, clsstock)))
			//If Not .Product.i_Stock(.sellerChannel).ContainsKey(SKUvariant) Then .Product.i_Stock(.sellerChannel)(SKUvariant) = New SortedDictionary(Of Date, clsStock)
			//.Product.i_Stock(.sellerChannel)(SKUvariant).Add(Arrival, Me)
			//  iq.Stock.Add(Me.ID, Me)

			//            oProduct = Product 'keep a record of the 'original' versions - so that we can maintain the index when things are opdated
			//            oSeller = Seller
		 // ERROR: Not supported in C#: WithStatement



		if (Quantity == -1) {
			this.ID = temporaryID();
		} else {
			object sql;
			sql = "INSERT INTO STOCK(FK_variant_ID, quantity,Arrival,datestamp,isCurrent,source) VALUES ";
			sql += "(" + SKUvariant.ID + "," + Quantity + "," + da.UniversalDate(Arrival) + ",getdate()," + IIf(iscurrent, "1", "0") + "," + da.SqlEncode(Source) + ");";

			this.ID = da.DBExecutesql(sql, true);

		}

		iq.Stock.Add(this.ID, this);



		oSKUVariant = SKUvariant;
		oArrival = Arrival;

	}

	public clsstock Insert()
	{

		return new clsstock(this.SKUvariant, this.quantity, this.Arrival, this.Source, this.IsCurrent);

	}


	public clsstock(int ID, clsVariant SKUVariant, int Quantity, System.DateTime Arrival, System.DateTime datestamp, bool isCurrent, ref List<string> errormessages)
	{
		this.ID = ID;
		//Me.Product = product
		// Me.Seller = Seller

		this.SKUvariant = SKUVariant;
		this.quantity = Quantity;
		this.Arrival = Arrival;
		this.LastUpdated = datestamp;
		this.IsCurrent = isCurrent;



		if (iq.Stock.ContainsKey(this.ID)) {
			errormessages.Add("* Duplicate stockID & me.id !");

		} else {
			iq.Stock.Add(this.ID, this);


				// errormessages.Add("* Duplicate shipment date ! SVID:" & SKUVariant.ID & " (" & Arrival.ToString & ") SID:" & Me.ID)

			 // ERROR: Not supported in C#: WithStatement

		}

	}

	public clsstock update()
	{

		object sql;
		sql = "UPDATE [Stock] SET quantity =" + this.quantity + ",arrival=" + da.UniversalDate(this.Arrival) + ",dateStamp=" + da.UniversalDate(this.LastUpdated) + ",isCurrent=" + IIf(this.IsCurrent, "1", "0");
		sql += " WHERE ID = " + this.ID;
		da.DBExecutesql(sql);

		oSKUVariant.shipments.Remove(oArrival);
		SKUvariant.shipments(Arrival) = this;

		return this;

	}


	public void Delete()
	{
		object sql;
		sql = "DELETE FROM [Stock] WHERE ID=" + this.ID;
		da.DBExecutesql(sql);

		oSKUVariant.shipments.Remove(oArrival);

	}



}

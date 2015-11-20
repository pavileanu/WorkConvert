using dataAccess;

public class clsPool
{

	private int id {
		get { return m_id; }
		set { m_id = Value; }
	}
	private int m_id;

	public Dictionary<int, clsProduct> products;

	public clsPool()
	{
		this.products = new Dictionary<int, clsProduct>();


	}


	public clsPool(int id)
	{
		this.id = id;
		this.products = new Dictionary<int, clsProduct>();

	}

	public clsPool insert()
	{

		clsPool apool = new clsPool(this.id);

		//iq.pools.add(apool.id, apool)

		return apool;

	}

}


public class clsOffer
{

	//an offer changes the price of a product.. one offer can
	//20% of HDD's in a certain machine (if you buy 4)

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private clsTranslation name {
		get { return m_name; }
		set { m_name = Value; }
	}
	private clsTranslation m_name;
	//FK offer_id
	private clsPrice Price {
		get { return m_Price; }
		set { m_Price = Value; }
	}
	private clsPrice m_Price;
	private int qtyMin {
		get { return m_qtyMin; }
		set { m_qtyMin = Value; }
	}
	private int m_qtyMin;
	private int qtyMax {
		get { return m_qtyMax; }
		set { m_qtyMax = Value; }
	}
	private int m_qtyMax;
	private float absoluteDiscount {
		get { return m_absoluteDiscount; }
		set { m_absoluteDiscount = Value; }
	}
	private float m_absoluteDiscount;
	private float percentDiscount {
		get { return m_percentDiscount; }
		set { m_percentDiscount = Value; }
	}
	private float m_percentDiscount;
	private clsPrice DiscountFromPrice {
		get { return m_DiscountFromPrice; }
		set { m_DiscountFromPrice = Value; }
	}
	private clsPrice m_DiscountFromPrice;
	private clsPool Pool {
		get { return m_Pool; }
		set { m_Pool = Value; }
	}
	private clsPool m_Pool;
	//which pool of products does this offer relate to
	private int poolQtyRequired {
		get { return m_poolQtyRequired; }
		set { m_poolQtyRequired = Value; }
	}
	private int m_poolQtyRequired;
	//how many 
	private int poolDistinctRequired {
		get { return m_poolDistinctRequired; }
		set { m_poolDistinctRequired = Value; }
	}
	private int m_poolDistinctRequired;
	private clsPool MustHaveOneFrom {
		get { return m_MustHaveOneFrom; }
		set { m_MustHaveOneFrom = Value; }
	}
	private clsPool m_MustHaveOneFrom;
	private DateTime ValidFrom {
		get { return m_ValidFrom; }
		set { m_ValidFrom = Value; }
	}
	private DateTime m_ValidFrom;
	private DateTime ValidTo {
		get { return m_ValidTo; }
		set { m_ValidTo = Value; }
	}
	private DateTime m_ValidTo;


	private string DisplayName {
		get { DisplayName = name.text(language); }
	}
	public clsOffer Insert()
	{

		clsOffer anOffer = new clsOffer(this.name, this.Price, this.qtyMin, this.qtyMin, this.absoluteDiscount, this.percentDiscount, this.DiscountFromPrice, this.Pool, this.poolQtyRequired, this.poolDistinctRequired,
		this.MustHaveOneFrom, this.ValidFrom, this.ValidTo);

		this.Price.Offers.Add(this.ID, this);

		return anOffer;

	}


	public void Update()
	{
		object sql;
		sql = "UPDATE OFFER SET ";
		sql += "name=" + this.name.Key + ",";
		sql += "FK_Price_ID=" + this.Price.ID + ",";
		sql += "qtymin=" + this.qtyMin + ",";
		sql += "qtymax=" + this.qtyMax + ",";
		sql += "absoluteDiscount=" + this.absoluteDiscount + ",";
		sql += "percentDiscount =" + this.percentDiscount + ",";
		sql += "fk_price_id_discountFrom=" + this.DiscountFromPrice.ID + ",";
		sql += "fk_pool_id" + this.Pool.id + ",";
		sql += "PoolQtyRequired=" + this.poolQtyRequired + ",";
		sql += "PoolDistinctRequired=" + this.poolDistinctRequired + ",";
		sql += "fk_pool_id_mustahaveone= " + this.MustHaveOneFrom.id + ",";
		sql += "validfrom= " + da.universaldate(this.ValidFrom) + ",";
		sql += "validto =  " + da.universaldate(this.ValidTo);

		sql += " WHERE ID = " + this.ID;

		da.DBExecutesql(sql);

	}

	public clsOffer(clsTranslation Name, clsPrice Price, int qtyMin, int qtyMax, float absoluteDiscount, int percentDiscount, clsPrice DiscountFromPrice, clsPool Pool, int poolQtyRequired, int poolDistinctRequired,

	clsPool MustHaveOneFrom, DateTime validFrom, DateTime validTo, DataTable writecache = null)
	{
		this.name = Name;
		this.Price = Price;
		this.qtyMin = qtyMin;
		this.qtyMax = qtyMax;
		this.absoluteDiscount = absoluteDiscount;
		this.percentDiscount = percentDiscount;
		this.DiscountFromPrice = DiscountFromPrice;
		this.Pool = Pool;
		this.poolQtyRequired = poolQtyRequired;
		this.poolDistinctRequired = poolDistinctRequired;
		this.MustHaveOneFrom = MustHaveOneFrom;


		if (writecache == null) {
			object sql;
			sql = "INSERT INTO Offer (fk_translation_key_name,fk_price_id,qtyMin,qtyMax,absoluteDiscount,percentDiscount,fk_price_id_discountFrom,fk_Pool_id,poolQtyRequired,PoolDistinctRequired,fk_pool_id_mustHaveOne,validFrom,validTo) ";
			sql += "VALUES (" + Name.Key + "," + Price.ID + "," + qtyMin + "," + qtyMax + "," + absoluteDiscount + "," + percentDiscount + "," + DiscountFromPrice.ID + "," + Pool.id + "," + poolQtyRequired + "," + MustHaveOneFrom.id + "," + da.universaldate(validFrom) + "," + da.universaldate(validTo) + ")";

			this.ID = da.DBExecutesql(sql, true);

		} else {
			System.Data.DataRow row;
			row = writecache.NewRow();

			row("fk_translation_key_name") = this.name.Key;
			row("fk_price_id") = this.Price.ID;
			row("qtymin") = this.qtyMin;
			row("qtymax") = this.qtyMax;
			row("absoluteDiscount") = this.absoluteDiscount;
			row("percentDiscount") = this.percentDiscount;
			row("fk_price_id_discountfrom") = this.DiscountFromPrice.ID;
			row("fk_pool_id") = this.Pool.id;
			row("poolQtyRequired") = this.poolQtyRequired;
			row("poolDistinctRequired") = this.poolDistinctRequired;
			row("FK_pool_id_mustHaveOne") = this.MustHaveOneFrom;
			//You must have one product from this pool
			row("validFrom") = this.ValidFrom;
			row("validTo") = this.ValidFrom;

			writecache.Rows.Add(row);

		}

		Price.Offers.Add(this.ID, this);

		//If a system has a descendant price with  offer which points to a pool contaiining the system



	}

	public clsOffer()
	{
		this.ID = -1;

	}


	public clsOffer(int ID, clsTranslation Name, clsPrice Price, int qtyMin, int qtyMax, float absoluteDiscount, int percentDiscount, clsPrice DiscountFromPrice, clsPool Pool, int poolQtyRequired,

	int poolDistinctRequired, clsPool MustHaveOneFrom, DateTime validfrom, DateTime validto, DataTable writecache = null)
	{
		this.ID = ID;

		this.name = Name;
		this.Price = Price;
		this.qtyMin = qtyMin;
		this.qtyMax = qtyMax;
		this.absoluteDiscount = absoluteDiscount;
		this.percentDiscount = percentDiscount;
		this.DiscountFromPrice = DiscountFromPrice;
		this.Pool = Pool;
		this.poolQtyRequired = poolQtyRequired;
		this.poolDistinctRequired = poolDistinctRequired;
		this.MustHaveOneFrom = MustHaveOneFrom;
		this.ValidFrom = validfrom;
		this.ValidTo = validto;

		this.Price.Offers.Add(this.ID, this);

	}

}

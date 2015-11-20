using dataAccess;


public class clsPool
{

    public int id { get; set; }
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

    public int ID { get; set; }
    public clsTranslation name { get; set; } //FK offer_id
    public clsPrice Price { get; set; }
    public int qtyMin { get; set; }
    public int qtyMax { get; set; }
    public float absoluteDiscount { get; set; }
    public float percentDiscount { get; set; }
    public clsPrice DiscountFromPrice { get; set; }
    public clsPool Pool { get; set; } //which pool of products does this offer relate to
    public int poolQtyRequired { get; set; } //how many
    public int poolDistinctRequired { get; set; }
    public clsPool MustHaveOneFrom { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }


    public string get_DisplayName(clsLanguage language)
    {
        string returnValue = "";
        returnValue = System.Convert.ToString(name.text(language));
        return returnValue;
    }
    public clsOffer Insert()
    {

        clsOffer anOffer = new clsOffer(this.name, this.Price, this.qtyMin, this.qtyMin, this.absoluteDiscount, (int)this.percentDiscount, this.DiscountFromPrice, this.Pool, this.poolQtyRequired, this.poolDistinctRequired, this.MustHaveOneFrom, this.ValidFrom, this.ValidTo);

        this.Price.Offers.Add(this.ID, this);

        return anOffer;

    }

    public void Update()
    {

        object sql = null;
        sql = "UPDATE OFFER SET ";
        sql += "name=" + this.name.Key + ",";
        sql += "FK_Price_ID=" + this.Price.ID + ",";
        sql += "qtymin=" + System.Convert.ToString(this.qtyMin) + ",";
        sql += "qtymax=" + System.Convert.ToString(this.qtyMax) + ",";
        sql += "absoluteDiscount=" + System.Convert.ToString(this.absoluteDiscount) + ",";
        sql += "percentDiscount =" + System.Convert.ToString(this.percentDiscount) + ",";
        sql += "fk_price_id_discountFrom=" + this.DiscountFromPrice.ID + ",";
        sql += "fk_pool_id" + System.Convert.ToString(this.Pool.id) + ",";
        sql += "PoolQtyRequired=" + System.Convert.ToString(this.poolQtyRequired) + ",";
        sql += "PoolDistinctRequired=" + System.Convert.ToString(this.poolDistinctRequired) + ",";
        sql += "fk_pool_id_mustahaveone= " + System.Convert.ToString(this.MustHaveOneFrom.id) + ",";
        sql += "validfrom= " + da.universaldate(this.ValidFrom) + ",";
        sql += "validto =  " + da.universaldate(this.ValidTo);

        sql += " WHERE ID = " + System.Convert.ToString(this.ID);

        da.DBExecutesql(sql);

    }

    public clsOffer(clsTranslation Name, clsPrice Price, int qtyMin, int qtyMax, float absoluteDiscount, int percentDiscount, clsPrice DiscountFromPrice, clsPool Pool, int poolQtyRequired, int poolDistinctRequired, clsPool MustHaveOneFrom, DateTime validFrom, DateTime validTo, DataTable writecache = null)
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

        if (writecache == null)
        {

            object sql = null;
            sql = "INSERT INTO Offer (fk_translation_key_name,fk_price_id,qtyMin,qtyMax,absoluteDiscount,percentDiscount,fk_price_id_discountFrom,fk_Pool_id,poolQtyRequired,PoolDistinctRequired,fk_pool_id_mustHaveOne,validFrom,validTo) ";
            sql += "VALUES (" + name.Key + "," + Price.ID + "," + System.Convert.ToString(qtyMin) + "," + System.Convert.ToString(qtyMax) + "," + System.Convert.ToString(absoluteDiscount) + "," + System.Convert.ToString(percentDiscount) + "," + DiscountFromPrice.ID + "," + System.Convert.ToString(Pool.id) + "," + System.Convert.ToString(poolQtyRequired) + "," + System.Convert.ToString(MustHaveOneFrom.id) + "," + da.universaldate(validFrom) + "," + da.universaldate(validTo) + ")";

            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        }
        else
        {

            System.Data.DataRow row = default(System.Data.DataRow);
            row = writecache.NewRow();

            row["fk_translation_key_name"] = this.name.Key;
            row["fk_price_id"] = this.Price.ID;
            row["qtymin"] = this.qtyMin;
            row["qtymax"] = this.qtyMax;
            row["absoluteDiscount"] = this.absoluteDiscount;
            row["percentDiscount"] = this.percentDiscount;
            row["fk_price_id_discountfrom"] = this.DiscountFromPrice.ID;
            row["fk_pool_id"] = this.Pool.id;
            row["poolQtyRequired"] = this.poolQtyRequired;
            row["poolDistinctRequired"] = this.poolDistinctRequired;
            row["FK_pool_id_mustHaveOne"] = this.MustHaveOneFrom; //You must have one product from this pool
            row["validFrom"] = this.ValidFrom;
            row["validTo"] = this.ValidFrom;

            writecache.Rows.Add(row);

        }

        Price.Offers.Add(this.ID, this);

        //If a system has a descendant price with  offer which points to a pool contaiining the system



    }
    public clsOffer()
    {

        this.ID = -1;

    }


    public clsOffer(int ID, clsTranslation Name, clsPrice Price, int qtyMin, int qtyMax, float absoluteDiscount, int percentDiscount, clsPrice DiscountFromPrice, clsPool Pool, int poolQtyRequired, int poolDistinctRequired, clsPool MustHaveOneFrom, DateTime validfrom, DateTime validto, DataTable writecache = null)
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
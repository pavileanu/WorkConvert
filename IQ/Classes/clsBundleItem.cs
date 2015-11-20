using dataAccess;

public class clsBundleItem
{

    public int ID { get; set; }
    public clsBundle Bundle { get; set; }
    public int qtyMin { get; set; }
    public nullablePrice Price { get; set; } //encapsulates currency
    public float Rebate { get; set; }
    public clsProduct Product { get; set; }

    public clsBundleItem(clsBundle bundle, clsProduct product, nullablePrice price, float rebate, int qytMin, DataTable WriteCache = null)
    {

        //price is some absolute overriding price on this bundle item (guaranteed by HP to be less then anyones buy price)
        //Rebate - is relative discount of their normal 'buy price' - rebate is subtracted - so use positive numbers



        if (WriteCache == null)
        {
            object sql = null;
            sql = "INSERT INTO BundleItem(fk_Bundle_id,fk_product_id,price,rebate,fk_currency_id,qtymin) VALUES (" + Bundle.ID + "," + Product.ID + "," + Price.sqlvalue + "," + System.Convert.ToString(rebate) + "," + Price.currency.ID + "," + System.Convert.ToString(qtyMin) + ")";
            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql));
            Bundle.Items.Add(this.ID, this);
        }
        else
        {
            this.ID = -1;

            System.Data.DataRow row = default(System.Data.DataRow);
            row = WriteCache.NewRow();

            row["fk_bundle_id"] = Bundle.ID;
            row["fk_product_id"] = Product.ID;
            row["price"] = Price.sqlvalue == "null" ? DBNull.Value : Price.NumericValue;
            row["rebate"] = rebate;
            row["fk_currency_id"] = Price.currency.ID;
            row["qtymin"] = qtyMin;

            WriteCache.Rows.Add(row);

        }

        this.Bundle = bundle;
        this.Product = product;
        this.Price = price;
        this.qtyMin = qtyMin;


    }

    public clsBundleItem(int ID, clsBundle bundle, clsProduct product, nullablePrice price, float rebate, int qytMin)
    {

        this.ID = ID;
        Bundle.Items.Add(this.ID, this);
        this.Bundle = bundle;
        this.Product = product;
        this.Price = price;
        this.Rebate = rebate;
        this.qtyMin = qtyMin;


    }


}
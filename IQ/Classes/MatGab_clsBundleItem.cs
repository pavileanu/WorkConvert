using dataAccess;
public class clsBundleItem
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private clsBundle Bundle {
		get { return m_Bundle; }
		set { m_Bundle = Value; }
	}
	private clsBundle m_Bundle;
	private int qtyMin {
		get { return m_qtyMin; }
		set { m_qtyMin = Value; }
	}
	private int m_qtyMin;
	private nullablePrice Price {
		get { return m_Price; }
		set { m_Price = Value; }
	}
	private nullablePrice m_Price;
	//encapsulates currency
	private float Rebate {
		get { return m_Rebate; }
		set { m_Rebate = Value; }
	}
	private float m_Rebate;
	private clsProduct Product {
		get { return m_Product; }
		set { m_Product = Value; }
	}
	private clsProduct m_Product;


	public clsBundleItem(clsBundle bundle, clsProduct product, nullablePrice price, float rebate, int qytMin, DataTable WriteCache = null)
	{
		//price is some absolute overriding price on this bundle item (guaranteed by HP to be less then anyones buy price)
		//Rebate - is relative discount of their normal 'buy price' - rebate is subtracted - so use positive numbers



		if (WriteCache == null) {
			object sql;
			sql = "INSERT INTO BundleItem(fk_Bundle_id,fk_product_id,price,rebate,fk_currency_id,qtymin) VALUES (" + bundle.ID + "," + product.ID + "," + price.sqlvalue + "," + rebate + "," + price.currency.ID + "," + qtyMin + ")";
			this.ID = da.DBExecutesql(sql);
			bundle.Items.Add(this.ID, this);
		} else {
			this.ID = -1;

			System.Data.DataRow row;
			row = WriteCache.NewRow();

			row("fk_bundle_id") = bundle.ID;
			row("fk_product_id") = product.ID;
			row("price") = IIf(price.sqlvalue == "null", DBNull.Value, price.NumericValue);
			row("rebate") = rebate;
			row("fk_currency_id") = price.currency.ID;
			row("qtymin") = qtyMin;

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
		bundle.Items.Add(this.ID, this);
		this.Bundle = bundle;
		this.Product = product;
		this.Price = price;
		this.Rebate = rebate;
		this.qtyMin = qtyMin;


	}


}

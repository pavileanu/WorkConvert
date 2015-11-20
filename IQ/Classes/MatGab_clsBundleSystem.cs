using dataAccess;

public class clsBundleSystem
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
	private clsProduct System {
		get { return m_System; }
		set { m_System = Value; }
	}
	private clsProduct m_System;
	private float rebate {
		get { return m_rebate; }
		set { m_rebate = Value; }
	}
	private float m_rebate;


	public clsBundleSystem(clsBundle bundle, clsProduct system, float rebate, DataTable writecache = null)
	{
		//price is some absolute overriding price on this bundle item (guaranteed by HP to be less then anyones buy price)
		//Rebate - is relative discount of their normal 'buy price' - rebate is subtracted - so use positive numbers

		if (writecache == null) {
			object sql;
			sql = "INSERT INTO BundleSystem(fk_Bundle_id,fk_product_id_system,rebate) VALUES (" + bundle.ID + "," + bundle.ID + "," + system.ID + "," + rebate + ")";
			this.ID = da.DBExecutesql(sql);
			if (system.Bundles == null)
				system.Bundles = new Dictionary<int, clsBundle>();
			system.Bundles.Add(bundle.ID, bundle);

		} else {
			this.ID = -1;

			System.Data.DataRow row;
			row = writecache.NewRow();

			row("fk_bundle_id") = bundle.ID;
			row("fk_product_id_system") = system.ID;
			row("rebate") = rebate;
			writecache.Rows.Add(row);

		}

		this.Bundle = bundle;
		this.System = system;
		this.rebate = rebate;

	}


	public clsBundleSystem(int ID, clsBundle bundle, clsProduct system, float rebate)
	{
		this.ID = ID;
		this.Bundle = bundle;
		this.System = system;
		this.rebate = rebate;

		if (system.Bundles == null)
			system.Bundles = new Dictionary<int, clsBundle>();
		system.Bundles.Add(bundle.ID, bundle);


	}


}

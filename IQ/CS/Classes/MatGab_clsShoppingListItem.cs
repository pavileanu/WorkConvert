

public class clsShoppingListItem
{

	private string QtyPartNo {
		get { return m_QtyPartNo; }
		set { m_QtyPartNo = Value; }
	}
	private string m_QtyPartNo;
	private string PartNo {
		get { return m_PartNo; }
		set { m_PartNo = Value; }
	}
	private string m_PartNo;
	private int Quantity {
		get { return m_Quantity; }
		set { m_Quantity = Value; }
	}
	private int m_Quantity;
	private clsProduct Product {
		get { return m_Product; }
		set { m_Product = Value; }
	}
	private clsProduct m_Product;


	public clsShoppingListItem(string qtyPartNo, string partNo, int quantity, clsProduct product)
	{
		this.QtyPartNo = qtyPartNo;
		this.PartNo = partNo;
		this.Quantity = quantity;
		this.Product = product;

	}

}

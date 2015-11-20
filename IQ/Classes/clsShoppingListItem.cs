public class clsShoppingListItem
{

    public string QtyPartNo { get; set; }
    public string PartNo { get; set; }
    public int Quantity { get; set; }
    public clsProduct Product { get; set; }

    public clsShoppingListItem(string qtyPartNo, string partNo, int quantity, clsProduct product)
    {

        this.QtyPartNo = qtyPartNo;
        this.PartNo = partNo;
        this.Quantity = quantity;
        this.Product = product;

    }

}
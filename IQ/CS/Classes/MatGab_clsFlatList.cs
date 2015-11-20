using System.Linq;
public class clsFlatList
{
	public List<clsFlatListItem> items;
	/// <summary>
	/// To check if item has been already been clsQuoteItem with IsPreinstalled to false.
	/// </summary>
	/// <param name="quoteitem">An instance of clsQuoteItem.</param>
	/// <returns>A boolean value true/ false.</returns>
	/// <remarks></remarks>
	public bool DoesNoneInstalledExist(clsQuoteItem quoteitem)
	{

		bool result = false;


		result = items.Where(x => x.QuoteItem.IsPreInstalled == false & x.QuoteItem.Path == quoteitem.Path).Select(x => x).Count > 0;
		return result;
	}

	public clsFlatListItem PSV(clsProduct Product, clsVariant SKUVariant)
	{

		//becuase more than one variant can exist on a branch
		//AND a variant can be on many branches 
		//we're wanting to group by distinct Variant/Path
		//returns the the item with the Product and SKU Variant as specified (or nothing it no item exists in the flatlist)

		PSV = null;
		foreach ( i in items) {
			if (object.ReferenceEquals(i.QuoteItem.Branch.Product, Product) & object.ReferenceEquals(i.QuoteItem.SKUVariant, SKUVariant)) {
				return i;
			}
		}

	}

	public clsFlatList()
	{
		items = new List<clsFlatListItem>();
	}

}


public class clsCustomerContext
{
	private string icmsLocation;
	private string taxType;

	private string warhouseLocation;
	public string Location {
		get { return icmsLocation; }
		set { icmsLocation = value; }
	}

	public string Tax {
		get { return taxType; }
		set { taxType = value; }
	}

	public string WareHouse {
		get { return warhouseLocation; }
		set { warhouseLocation = value; }
	}

}

/// <summary>
/// Bit of a stub - but makes code more readable (as we can index things by clsWarehouse)
/// </summary>
/// <remarks></remarks>
public class clsWarehouse
{
	private string Code {
		get { return m_Code; }
		set { m_Code = Value; }
	}
	private string m_Code;


	public clsWarehouse(string code)
	{
		this.Code = code;


	}

}

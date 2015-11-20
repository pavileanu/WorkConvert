using System.Web.Http;
using System.Net.Http;

public class clsFilterSetRequest : HttpRequestMessage
{

	public UInt64 lid;
	public int ScreenID;
	public List<clsFieldSetRequestDetail> Fields;
}
public class clsFieldSetRequestDetail : HttpRequestMessage
{

	public int FieldId;
	public string DefaultFilter;
	public string TranslationGroup;
	public string FilterType;
	public int Order;
	public bool Enabled;
}


using System.Web.Http;
using System.Net.Http;

public class clsGenericAjaxRequest : HttpRequestMessage
{

	public int ScreenId;
	public UInt64 lid;
	public UInt64 elid;
	public string BranchPath;
	public string ScreenTitle;
	public int ActionId;
	public int SourceFieldId;
	public int DestinationFieldId;
	public string SysType;
	public Int32 QuoteId;
	public Int32 ParentId;
}

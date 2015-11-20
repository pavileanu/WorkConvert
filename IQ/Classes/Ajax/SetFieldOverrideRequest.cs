using System.Web.Http;
using System.Net.Http;

public class SetFieldOverrideRequest : HttpRequestMessage
{
	public UInt64 lid;
	public string BranchPath;
	public int ScreenId;
	public int FieldId;
	public bool? ForceVisibilityTo;
	public int? ForceOrderTo;
	public double? ForceWidthTo;
	public string ForceSortTo;
	public string ForceFilterTo;
}

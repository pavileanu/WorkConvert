using System.Web.Http;
using System.Net.Http;

public class CloneTargetsRequest : HttpRequestMessage
{

	public int ScreenId;
	public string Path;
	public List<string> Targets;
	public UInt64 lid;
	public string Level;

	public string LevelValue;
}

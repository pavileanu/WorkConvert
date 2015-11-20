using System.ServiceModel;

// NOTE: You can use the "Rename" command on the context menu to change the interface name "IOneTimeToken" in both code and config file together.
[ServiceContract(Namespace = "channelcentral.net/oneTimeToken")]
public interface IOneTimeToken
{

	[OperationContract()]
	clsToken GetToken(string HostID, string HostPassword, List<clsNameValuePair> Pairs);
	[OperationContract()]
	List<clsName> Help();
	[OperationContract()]
	clsGKAccount GetUserDetails(string HostCode, string UserEmail, string PriceBand);

}

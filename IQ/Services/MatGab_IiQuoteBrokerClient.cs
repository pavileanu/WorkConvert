using System.ServiceModel;

// NOTE: You can use the "Rename" command on the context menu to change the interface name "IiQuoteBrokerClient" in both code and config file together.
[ServiceContract()]
public interface IiQuoteBrokerClient
{

	[OperationContract()]
	object ObjectUpdated(Guid Id, string Path, List<Tuple<string, string, object, Int32>> Properties);

	void PassAllTypes(nullableString a);
}

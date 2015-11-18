Imports System.ServiceModel

' NOTE: You can use the "Rename" command on the context menu to change the interface name "IOneTimeToken" in both code and config file together.
<ServiceContract(Namespace:="channelcentral.net/oneTimeToken")> _
Public Interface IOneTimeToken

    <OperationContract()>
    Function GetToken(HostID As String, HostPassword As String, Pairs As List(Of clsNameValuePair)) As clsToken
    <OperationContract()>
    Function Help() As List(Of clsName)
    <OperationContract()>
    Function GetUserDetails(HostCode As String, UserEmail As String, PriceBand As String) As clsGKAccount

End Interface

Imports System.ServiceModel

' NOTE: You can use the "Rename" command on the context menu to change the interface name "IiQuoteBrokerClient" in both code and config file together.
<ServiceContract()>
Public Interface IiQuoteBrokerClient

    <OperationContract()>
    Function ObjectUpdated(Id As Guid, Path As String, Properties As List(Of Tuple(Of String, String, Object, Int32?)), UpdateTime As Date) As Boolean

    Sub PassAllTypes(a As nullableString)
End Interface

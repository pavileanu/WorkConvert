Imports System.ServiceModel

Public Class clsBrokerManager
    Public Shared Id As Guid = Guid.Empty

    Public Shared Sub Update(Path As String, Properties As List(Of Tuple(Of String, String, Object, Int32?)))
        'Try
        Using client As iQuoteBrokerService.IiQuoteBrokerServiceClient = New iQuoteBrokerService.IiQuoteBrokerServiceClient()
            If Not client.UpdateObject(Id, Path, Properties, DateTime.Now) Then
                'Check we are registered
                Register()
                If Not client.UpdateObject(Id, Path, Properties, DateTime.Now) Then
                    'Error state
                    Throw New Exception("Broker Down")
                End If
            End If
        End Using
        'Catch ex As System.Exception
        'End Try
    End Sub

    Public Shared Sub Register()
        If System.Configuration.ConfigurationManager.AppSettings("Guid") IsNot Nothing Then Id = Guid.Parse(System.Configuration.ConfigurationManager.AppSettings("Guid"))
        If Id = Guid.Empty Then
            Id = Guid.NewGuid()
            Dim config = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~")
            Dim appSettings As KeyValueConfigurationCollection = config.AppSettings.Settings
            appSettings.Add("Guid", Id.ToString)
            ' appSettings("Guid").Value = Id.ToString()
            config.Save(ConfigurationSaveMode.Modified)

        End If
        Using client As iQuoteBrokerService.IiQuoteBrokerServiceClient = New iQuoteBrokerService.IiQuoteBrokerServiceClient()
            client.RegisterParticipant(Id, Environment.MachineName, "http://localhost:8021/services/iQuoteBrokerClient.svc", False, False)
        End Using
    End Sub


End Class

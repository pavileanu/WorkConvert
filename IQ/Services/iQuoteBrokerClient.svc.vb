Imports System.Reflection

' NOTE: You can use the "Rename" command on the context menu to change the class name "iQuoteBrokerClient" in code, svc and config file together.
' NOTE: In order to launch WCF Test Client for testing this service, please select iQuoteBrokerClient.svc or iQuoteBrokerClient.svc.vb at the Solution Explorer and start debugging.
Public Class iQuoteBrokerClient
    Implements IiQuoteBrokerClient


    Public Function ObjectUpdated(Id As Guid, Path As String, Properties As List(Of Tuple(Of String, String, Object, Int32?)), UpdateTime As Date) As Boolean Implements IiQuoteBrokerClient.ObjectUpdated
        'Lets go reflect...
        Dim errorMessages As List(Of String) = New List(Of String)()

        Dim Obj As Object = iq
        Dim ParentObj As Object = Nothing

        ParsePath(Path$, Obj, ParentObj, errorMessages)

        If Obj IsNot Nothing Then
            For Each t In Properties
                Dim pi As PropertyInfo = Obj.GetType.GetProperty(t.Item2)
                If Not pi.PropertyType.IsSealed Then
                    Dim val = t.Item3
                    If pi.PropertyType.Name.Contains("nullable") Then
                        'Special case for nullables
                        val = Activator.CreateInstance(pi.PropertyType)
                        val.v = t.Item3
                    End If

                    'Find object in model
                    Dim pi2 = Nothing
                    For Each ty In iq.GetType.GetProperties()
                        If ty.PropertyType.FullName.ToLower.Contains("dictionary") AndAlso ty.PropertyType.FullName.ToLower.Contains(pi.PropertyType.Name.ToLower) Then
                            pi2 = ty
                        End If
                    Next

                    If pi2 IsNot Nothing Then
                        Dim dict = pi2.GetValue(iq)

                        Reflection.setProperty(Obj, t.Item2, If(val Is Nothing, Nothing, dict(val)), t.Item4, errorMessages, True)
                    Else
                        Reflection.setProperty(Obj, t.Item2, val, t.Item4, errorMessages, True)
                    End If
                Else
                    Reflection.setProperty(Obj, t.Item2, t.Item3, t.Item4, errorMessages, True)
                End If
            Next
        End If

        Return True
    End Function

    Public Sub PassAllTypes(a As nullableString) Implements IiQuoteBrokerClient.PassAllTypes

    End Sub
End Class

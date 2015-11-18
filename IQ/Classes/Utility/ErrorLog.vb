Public Class ErrorLog
    Private Shared ErrorList As LoggingList(Of String) = New LoggingList(Of String)()
    Public Shared Sub Add(ex As Exception)
        If ex IsNot Nothing Then
            ErrorList.Add(String.Format("{0}\r\n{1}\r\n{2}", ex.Message, If(ex.InnerException IsNot Nothing, ex.InnerException.Message, String.Empty), ex.StackTrace))
            Try
                dataAccess.da.DBExecutesql(String.Format("INSERT INTO ErrorLog (DateTime,Message,StackTrace,InnerException) VALUES ({0},{1},{2},{3})", dataAccess.da.UniversalDate(DateTime.Now), dataAccess.da.SqlEncode(ex.Message), dataAccess.da.SqlEncode(ex.StackTrace), dataAccess.da.SqlEncode(If(ex.InnerException IsNot Nothing, ex.InnerException.Message, String.Empty))))
            Catch exe As Exception
                'Dont crash, thats what we are trying to avoid!!
            End Try
        End If

    End Sub


End Class

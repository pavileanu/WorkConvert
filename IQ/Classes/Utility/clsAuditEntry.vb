Public Class clsAuditEntry
    Public DateTime As DateTime
    Public lid As UInt64
    Public Action As String
    Public SourcePath As String
    Public TargetPath As String
    Public SecondaryMessage As String
    Public Message As String
    Public PageName As String
    Public SourceURL As String
    Public TimeToLoad As Double
    Public HttpRequestMethod As String
    Public UrlReferrer As String
    Public AuditType As String
    Public ActionUndone As Boolean = False
    Public AuditId As Integer? = Nothing
    Public DBMethod As Integer = 0

    Private Shared ReadOnly lock As New Object()



    Public Function Save() As Boolean


        Try

            'URL
            Dim Url$ = dataAccess.da.SqlEncode(If(String.IsNullOrEmpty(Me.UrlReferrer), "", New Uri(Me.UrlReferrer).PathAndQuery))

            If DBMethod = 0 Then
                'Nick - Added the transaction wrapper as we were getting a table deadlock !
                'refactored onto multiple lines  23/03/15
                'if it recurrs there is a suggestion to 'add an index on the self referencing column' here
                'http://stackoverflow.com/questions/5898743/deadlock-using-self-referential-foreign-key

                ' Serialize calls through this DB call as a possible way of avoiding the deadlock issues
                SyncLock (lock)

                    'Nick removed for now - as nobody ever checks the audi log and i suspect it to be the source of perfromance and stability problems (dbexecuteSQL gailing and not closing connections /deadlock problem(s))

                    'dataAccess.da.DBExecutesql(String.Format( _
                    '"BEGIN TRAN;" & _
                    '"INSERT INTO auditlog (MachineName,DateTime,lid,Action,SourcePath,TargetPath,Messages,SecondaryMessage,PageName,SourceURL,TimeToLoad_MS,HttpRequestMethod,UrlReferrer,ParentId)" & _
                    '" VALUES ({0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}," & _
                    '"(select top 1 id from auditlog AS parent where  parent.SourceURL = " & Url & " and " & lid & "=parent.lid and parent.lid <> 0 order by id desc)" & _
                    '" );" & _
                    '"COMMIT TRAN;", _
                    'dataAccess.da.SqlEncode(Environment.MachineName), _
                    'dataAccess.da.UniversalDate(Me.DateTime), _
                    'Me.lid, dataAccess.da.SqlEncode(Me.Action), _
                    'dataAccess.da.SqlEncode(Me.SourcePath), _
                    'dataAccess.da.SqlEncode(Me.TargetPath), _
                    'dataAccess.da.SqlEncode(Me.Message), _
                    'dataAccess.da.SqlEncode(Me.SecondaryMessage), _
                    'dataAccess.da.SqlEncode(Me.PageName), _
                    'dataAccess.da.SqlEncode(Me.SourceURL), _
                    'Me.TimeToLoad, _
                    'dataAccess.da.SqlEncode(Me.HttpRequestMethod), _
                    'dataAccess.da.SqlEncode(Me.UrlReferrer)))
                End SyncLock
            Else
                dataAccess.da.DBExecutesql(String.Format("UPDATE auditlog SET ActionUndone = " + dataAccess.da.SqlEncode(ActionUndone) + " WHERE id=" + AuditId.ToString()))
            End If
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

End Class

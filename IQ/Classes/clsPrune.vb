Imports dataAccess

Public Class clsPrune

    Property ID As Integer
    Property Path As String
    Property ChannelID As NullableInt 'clsChannel 'the seller channel to wich this prune applices (not yet implimented) - but would handle BU's
    Property Source As String
    Property Created As DateTime

    Public Sub New()

    End Sub

    Public Sub update()

        Dim sql$
        sql$ = "UPDATE [PRUNE] set source=" & da.SqlEncode(Source) & " WHERE ID=" & Me.ID
        da.dbexecutesql(sql)

    End Sub
    Public Sub New(ByVal path As String, ChannelID As NullableInt, Source As String, Optional ByRef writecache As DataTable = Nothing, Optional ByRef nextPruneId As Integer = 0)



        If writecache Is Nothing Then
            Dim sql$
            sql$ = "INSERT INTO [prune] (path,fk_channel_id,Created,source) VALUES (" & da.SqlEncode(path) & "," & ChannelID.sqlvalue & ",getdate()," & da.SqlEncode(Source) & ");"
            Me.ID = da.DBExecutesql(sql, True)
        Else

            Me.ID = nextpruneid   'they will get their true ID's next time they're loaded
            nextpruneid += 1
            Dim row As System.Data.DataRow
            row = writecache.NewRow()
            row("Path") = path
            If ChannelID.sqlvalue = "null" Then
                row("FK_Channel_id") = DBNull.Value
            Else
                row("FK_Channel_id") = ChannelID.sqlvalue
            End If

            row("Created") = Now
            row("Source") = Source
            writecache.Rows.Add(row)

        End If


        Me.ChannelID = ChannelID
        Me.Path = path$
        Dim fp$ = PathName(path$)

        Me.Source = Source
        Me.Created = Now

        Dim branch As clsBranch = iq.Branches(Split(path, ".").Last)

        If branch.Prunes.Count > 5000 Then Stop
        branch.Prunes.Add(Me.ID, Me)

    End Sub

    Public Sub New(id As Integer, ByVal path As String, ChannelID As NullableInt, source As String, created As DateTime)

        Me.ID = id
        Me.ChannelID = ChannelID
        Me.Path = path
        Me.Created = created
        Me.Source = source

        If Not iq.Branches.ContainsKey(Split(path, ".").Last) Then
            Me.delete()
        Else
            Dim BRANCH As clsBranch = iq.Branches(Split(path, ".").Last)
            BRANCH.Prunes.Add(Me.ID, Me)
            If BRANCH.Product IsNot Nothing Then
                If BRANCH.Product.SKU = "AN975A" Then
                    Beep()
                End If
            End If
        End If
    End Sub

    Public Sub delete()

        da.DBExecutesql("DELETE FROM PRUNE WHERE ID=" & Me.ID)

        If iq.Branches.ContainsKey(Split(Path, ".").Last) Then
            If iq.Branches(Split(Path, ".").Last).Prunes.ContainsKey(Me.ID) Then
                iq.Branches(Split(Path, ".").Last).Prunes.Remove(Me.ID)
            End If
        End If

    End Sub

End Class 'clsPrune



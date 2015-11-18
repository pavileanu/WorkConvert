Imports dataAccess

Public Class clsBuyerGroup

    'DEAD/OBSOLETE (may need to be re-implimented if we can get distis to give pricelists or groups of buyers

    Public ID As Integer
    Public name As String
    Public Channels As List(Of clsChannel)
    Public Owner As clsChannel
    Public OwnersID As String

    Public Sub New(id As Integer, name As String, owner As clsChannel, ownersID As String)

        Me.ID = id
        Me.name = name
        Me.Channels = New List(Of clsChannel)
        Me.Owner = owner
        Me.OwnersID = ownersID

        iq.BuyerGroups.Add(Me.ID, Me)
        '        iq.i_buyerGroups.Add(owner.ID & "_" & ownersID, Me)

    End Sub

    Public Sub New(name As String, owner As clsChannel, ownersID As String)

        Dim sql$
        sql$ = "INSERT INTO [BuyerGroup] (name,fk_channel_id_owner,ownersID) VALUES (" & da.SqlEncode(name$) & "," & owner.ID & "," & da.SqlEncode(ownersID) & ");"

        Me.ID = da.DBExecutesql(sql$, True)
        Me.name = name
        Me.Owner = owner
        Me.OwnersID = ownersID

        iq.BuyerGroups.Add(Me.ID, Me)

    End Sub

End Class

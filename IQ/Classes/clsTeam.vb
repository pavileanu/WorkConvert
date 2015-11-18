Imports dataAccess
<Serializable>
Public Class clsTeam
    Implements i_Editable

    Property ID As Integer
    Property Name As String
    Property Members As List(Of clsUser)
    Property Channel As clsChannel


    Public Sub New()

        'this is the 'delayed create' version - called by the generic editor
        'an instance is created - but it is not added to its parent channel unti it is Update()d
        Me.ID = -1
        Me.Channel = Nothing
        Me.Members = New List(Of clsUser)
        Me.Channel = Nothing

    End Sub


    Public Sub New(channel As clsChannel, Name As String)

        Dim sql$
        sql$ = "INSERT INTO Team(Name,FK_Channel_ID) VALUES ('" & Name$ & "'," & channel.ID & ");"

        Me.ID = da.DBExecutesql(sql$, True)
        Me.Name = Name
        Me.Channel = channel

        channel.Teams.Add(Me.ID, Me)
        iq.Teams.Add(Me.ID, Me)

        Me.Members = New List(Of clsUser)


    End Sub

    Public Sub New(id As Integer, channel As clsChannel, Name As String)

        Me.ID = id
        Me.Name = Name

        channel.Teams.Add(Me.ID, Me)
        iq.Teams.Add(Me.ID, Me)

        Me.Members = New List(Of clsUser)
        Me.Channel = channel

    End Sub

    Public Function DisplayName(Language As clsLanguage) As String Implements i_Editable.displayName
        DisplayName = Me.Name '& "(" & Me.Code & ")"
    End Function


    Public Function Insert(ByRef errormessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsTeam(Me.Channel, Me.Name) 'we *now* call the constructor which makes a team and adds it to the approprtiate dictionaries/parent object

    End Function

    Public Sub update(ByRef errormessages As List(Of String)) Implements i_Editable.update

        Dim sql$
        sql$ = "UPDATE [team] set name =" & da.SqlEncode(Me.Name) & " WHERE ID=" & Me.ID
        da.DBExecutesql(sql$, False)

    End Sub

    Public Sub delete(ByRef errormessages As List(Of String)) Implements i_Editable.delete


        Dim sql$
        sql$ = "DELETE FROM [team] WHERE id=" & Me.ID

        Try
            da.DBExecutesql(sql$, False)
        Catch ex As Exception
            errormessages.Add(ex.Message)
        End Try


    End Sub


End Class


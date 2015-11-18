Imports dataAccess

'States are (primarily) for quotes - but are pretty generic so could be extended/re-used for other things (which is what 'group' is for)

Public Class clsState

    Property ID As Integer
    Property code As String
    Property Translation As clsTranslation
    Property group As String
    Property Order As Integer
    Property Colour As String

    Private CompoundKey As String

    Public Sub New()
        ' Me.Translation = iq.AddTranslation("Edit me", English, Nothing, True) - now done in set defauts (for all translations)
    End Sub

    Public Function Insert() As clsState
        Return New clsState(Me.group, Me.code, Me.Translation, Me.Order, Me.Colour)
    End Function

    Public ReadOnly Property Displayname(language As clsLanguage) As String

        Get
            Return Me.Translation.text(language) & " (" & Me.code & ")"
        End Get

    End Property

    Public Sub New(ByVal id As Integer, ByVal group As String, ByVal code As String, translation As clsTranslation, order As Integer, colour As String)

        'This is an overriden constructor - becuase the ID *is* specified - it *knows* we' dont want to do a database insert
        Me.ID = id
        Me.group = group
        Me.code = code
        Me.Translation = translation
        Me.Order = order
        Me.Colour = colour

        iq.States.Add(Me.ID, Me)

        'The states index has a compound key of 'code-group'
        CompoundKey = Me.group & "-" & Me.code
        iq.i_state_GroupCode.Add(CompoundKey, Me)

    End Sub

    Public Sub New(ByVal group As String, ByVal code As String, ByVal translation As clsTranslation, order As Integer, colour As String)

        'Creates a new (instance of the class cls)Language - populates its ID

        Dim sql$
        sql$ = "INSERT INTO [State] ([group],[code],fk_translation_key,[order],colour) "
        sql$ &= "VALUES (" & da.SqlEncode(group) & "," & da.SqlEncode(code) & "," & translation.Key & "," & order & "," & da.SqlEncode(colour) & ");"

        Me.ID = da.DBExecutesql(sql$, True)
        Me.group = group
        Me.code = code
        Me.Translation = translation
        Me.Order = order
        Me.Colour = colour

        iq.States.Add(Me.ID, Me)

        'The states index has a compound key of 'code-group'

        CompoundKey = Me.group & "-" & Me.code
        iq.i_state_GroupCode.Add(CompoundKey, Me)


    End Sub

    Public Sub Update()

        Dim sql$
        sql$ = "UPDATE [State] set "
        sql$ &= "[Group]=" & da.SqlEncode(Me.group) & ","
        sql$ &= "[Code]=" & da.SqlEncode(Me.code) & ","
        sql$ &= "[FK_Translation_Key]=" & Me.Translation.Key & ","
        sql$ &= "[Order]=" & Me.Order & ","
        sql$ &= "[Colour]=" & da.SqlEncode(Me.Colour)
        sql$ &= " WHERE ID=" & Me.ID

        da.DBExecutesql(sql$, False)

        'Update the 'index' object (becuase my 'key' may have changed)
        iq.i_state_GroupCode.Remove(CompoundKey)
        CompoundKey = Me.group & "-" & Me.code
        iq.i_state_GroupCode.Add(CompoundKey, Me)



    End Sub




End Class

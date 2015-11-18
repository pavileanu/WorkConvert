Imports dataAccess

Public Class clsRole

    Property ID As Integer
    Property Code As String
    Property Translation As clsTranslation
    Property Rights As Dictionary(Of Integer, clsRight)
    Property i_right_code As Dictionary(Of String, clsRight)


    ReadOnly Property DisplayName(language As clsLanguage)
        Get
            DisplayName = Me.Translation.text(language)
        End Get
    End Property

    ReadOnly Property EnglishDisplayName
        Get
            EnglishDisplayName = Me.Translation.text(English)
        End Get
    End Property

    Public Sub New()
        Me.ID = -1
        Me.Rights = New Dictionary(Of Integer, clsRight)
        Me.i_right_code = New Dictionary(Of String, clsRight)()

    End Sub
    Public Sub New(Code As String, translation As clsTranslation)

        Dim sql$
        sql$ = "INSERT INTO [Role] (code,fk_Translation_key) "
        sql$ &= " values (" & da.SqlEncode(Code) & "," & translation.Key & ");"

        Me.ID = da.DBExecutesql(sql$, True)
        Me.Code = Code
        Me.Translation = translation
        Me.Rights = New Dictionary(Of Integer, clsRight)
        Me.i_right_code = New Dictionary(Of String, clsRight)()

        iq.i_role_Code.Add(Me.Code, Me)

    End Sub

    Public Sub New(Id As Integer, Code As String, translation As clsTranslation)

        Me.ID = Id
        Me.Code = Code
        Me.Translation = translation
        Me.Rights = New Dictionary(Of Integer, clsRight)
        Me.i_right_code = New Dictionary(Of String, clsRight)()

        iq.i_role_Code.Add(Me.Code, Me)

    End Sub

    Public Sub AddRight(right As clsRight)
        If Rights.ContainsKey(right.ID) Then Exit Sub
        Dim sql$
        sql$ = "INSERT INTO [RoleRight] (fk_Role_Id,fk_right_id) "
        sql$ &= " values (" & Me.ID & "," & right.ID & ");"

        da.DBExecutesql(sql$, True)
        Me.Rights.Add(right.ID, right)
        Me.i_right_code.Add(right.Code, right)
    End Sub

End Class 'clsRole


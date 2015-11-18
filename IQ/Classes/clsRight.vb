Imports dataAccess

Public Class clsRight
    Property ID As Integer
    Property Code As String
    Property Translation As clsTranslation

    Public Sub New(Code As String, translation As clsTranslation)

        Dim sql$
        sql$ = "INSERT INTO [Right] (code,fk_Translation_key) "
        sql$ &= " values (" & da.SqlEncode(Code) & "," & translation.Key & ");"

        Me.ID = da.DBExecutesql(sql$, True)
        Me.Code = Code
        Me.Translation = translation
        iq.i_right_Code.Add(Code, Me)

    End Sub

    Public Sub New()
        Me.ID = -1
    End Sub


    Public Sub New(ID As Integer, Code As String, translation As clsTranslation)

        Me.ID = ID
        Me.Code = Code
        Me.Translation = translation
        iq.i_right_Code.Add(Code, Me)
    End Sub


    Public Function displayName(Language As clsLanguage) As String

        displayName = Me.Translation.text(Language)

    End Function

    Public Function Insert() As clsRight

        Return New clsRight(Me.Code, Me.Translation)

    End Function


    Public Sub update()

        If Me.ID = -1 Then Stop

        Dim sql$
        sql$ = "UPDATE [Right] SET code=" & da.SqlEncode(Me.Code) & ",fk_translation_key=" & Me.Translation.Key & " WHERE ID=" & Me.ID
        da.dbexecutesql(sql$)

    End Sub



End Class




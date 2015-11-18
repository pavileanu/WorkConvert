Imports dataAccess

Public Class clsProductType

    Property ID As Integer
    Property Code As String
    Property Translation As clsTranslation
    Property Order As Short

    Dim oCode As String

    Public Sub New(ByVal code As String, ByVal translation As clsTranslation, order As Short)

        Dim sql$
        sql$ = "INSERT INTO ProductType (code,fk_Translation_key_text,[order]) VALUES ('" & code & "'," & translation.Key & "," & order & ");"
        Me.ID = da.DBExecutesql(sql$, True)
        Me.Code = code
        Me.Translation = translation
        Me.Order = order

        iq.ProductTypes.Add(Me.ID, Me)
        iq.i_ProductType_Code.Add(Me.Code, Me)

        oCode = Me.Code

    End Sub
    Public ReadOnly Property DisplayName(Language As clsLanguage) As String
        Get
            DisplayName = Me.Code & " " & Me.Translation.text(Language)
        End Get
    End Property

    Public Sub New()

    End Sub

    Public Function Insert()

        Return New clsProductType(Me.Code, Me.Translation, 0)

    End Function

    Public Sub Update()

        Dim sql$
        sql$ = "UPDATE [ProductType] SET code=" & da.SqlEncode(Me.Code) & ",fk_translation_key_text=" & Me.Translation.Key & ",[order]=" & Order & " WHERE ID=" & Me.ID

        Try
            iq.i_ProductType_Code.Remove(oCode)
            iq.i_ProductType_Code.Add(Me.Code, Me)
            da.dbexecutesql(sql$)

        Catch ex As System.Exception
            Stop ' probably a duplictae code
        End Try

    End Sub

    Public Sub Delete()

        Dim sql$
        sql$ = "DELETE FROM [ProductType] WHERE ID=" & Me.ID
        da.dbexecutesql(sql$)

    End Sub

    Public Sub New(ByVal ID As Integer, ByVal code As String, ByVal translation As clsTranslation, order As Short)

        Me.ID = ID
        Me.Code = code
        Me.Translation = translation
        Me.Order = order

        iq.ProductTypes.Add(Me.ID, Me)
        iq.i_ProductType_Code.Add(Me.Code, Me)

        oCode = Me.Code

    End Sub


End Class

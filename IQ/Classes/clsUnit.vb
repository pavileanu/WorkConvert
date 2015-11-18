Imports dataAccess

Public Class clsUnit
    Implements i_Editable

    Property ID As Integer
    Property Translation As clsTranslation 'carries the translation.key - and exposes (via an indexed defautl property) the underlying text
    Property Symbol As String
    Property Code As String  'our internal code for referencing these units eg KG (most of the time it will be the same as the name)
    Property MeasureID As Integer

    Dim oCode As String

    Public Function DisplayName(language As clsLanguage) As String Implements i_Editable.displayName
    
        Return Me.Translation.text(language)
    End Function

    Public Sub New()


    End Sub

    Public Sub delete(ByRef errormessages As List(Of String)) Implements i_Editable.delete



        Try
            da.DBExecutesql("DELETE FROM UNIT WHERE ID=" & Me.ID)  'will often fail due to RI (expose this error through the editor)
        Catch ex As Exception
            errormessages.Add(ex.Message.ToString)
        End Try

    End Sub


    Public Function Insert(ByRef errormessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsUnit(Me.Code, Me.Translation, Me.Symbol,Me.MeasureID)

    End Function

    Public Sub Update(ByRef errormessages As List(Of String)) Implements i_Editable.update

        Dim sql$
        sql$ = "UPDATE [Unit] set "
        sql$ &= "code=" & da.SqlEncode(Me.Code) & ","
        sql$ &= "fk_translation_key_name=" & Me.Translation.Key & ","
        sql$ &= "symbol=" & da.SqlEncode(Me.Symbol)
        sql$ &= " WHERE ID=" & Me.ID

        iq.i_unit_code.Remove(oCode)
        iq.i_unit_code.Add(Me.Code, Me)

        oCode = Me.Code

        da.DBExecutesql(sql$)

    End Sub


    Public Sub New(ByVal code As String, ByVal translation As clsTranslation, ByVal Symbol As String, MeasureID As Integer)

        Me.Translation = translation
        Me.Symbol = Symbol
        Me.Code = code
        Me.MeasureID = MeasureID

        Dim sql$
        sql$ = "Insert into [Unit] ([code],[FK_Translation_key_name],[symbol],FK_Measure_ID) values (" & da.SqlEncode(code) & "," & translation.Key & "," & da.SqlEncode(Me.Symbol) & "," & da.SqlEncode(Me.MeasureID) & ");"
        Me.ID = da.DBExecutesql(sql$, True)

        iq.Units.Add(Me.ID, Me)
        If Not iq.i_unit_code.ContainsKey(code) Then  'hmm not sure why this is needed 
            iq.i_unit_code.Add(Me.Code, Me)
        End If

        oCode = Me.Code


    End Sub



    Public Sub New(ByVal id As Integer, ByVal code As String, ByVal translation As clsTranslation, ByVal Symbol As String, MeasureID As Integer)

        Me.ID = id
        Me.Translation = translation
        Me.Symbol = Symbol
        Me.Code = code
        Me.MeasureID = MeasureID

        iq.Units.Add(Me.ID, Me)

        iq.i_unit_code.Add(Me.Code, Me)

        oCode = Me.Code

    End Sub

End Class

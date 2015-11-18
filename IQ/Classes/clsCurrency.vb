Imports dataAccess
Imports System.Globalization

Public Class clsCurrency
    Implements i_Editable

    Property ID As Integer
    Property Code As String
    Property Code_HP As String
    Property translation As clsTranslation  'of the currency name (into other languages) - "Dollars" might be Somethign else in chinese/russian etc.
    Property Symbol As String
    Property Rate As Single
    Property Notes As clsTranslation

    'Moved to clsAccount - Euro and Swiss Franc which may be used in multiple cultures - mean this culture should be per account - giving maximum flexibility (it's defaulted from the buyers region)
    'Property Culture As String '.NET culture code for decimal point, thousands seperator etc. (default is EN)

    Public Function DisplayName(language As clsLanguage) As String Implements i_Editable.displayName
        DisplayName = Me.translation.text(language) & " (" & Me.Code & ") " & Me.Symbol
    End Function


    Public Sub New()

    End Sub

    Public Function format(v As Decimal, culture As String, ByRef errorMessages As List(Of String), Optional decimalPlaces As Integer = 2) As String

        format = "unable to format currency"
        Dim ci As CultureInfo = Nothing
        Try
            ci = New CultureInfo(culture)
            format = Me.Symbol.Trim & v.ToString("N" & decimalPlaces.ToString.Trim, ci).Trim 'Format as a currency.. to the cirrenct number of decimal places
        Catch
            errorMessages.Add("The culture code " & culture & " is probably wrong.")
        End Try

    End Function

    Public Function Insert(ByRef errorMessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsCurrency(Me.Code, Me.Code_HP, Me.translation, Me.Symbol, Me.Rate, Me.Notes)

    End Function

    Public Sub update(ByRef errorMessages As List(Of String)) Implements i_Editable.update

        Dim sql$
        'sql$ = "UPDATE [Currency] set code=" & da.SqlEncode(Me.Code) & ",symbol=" & da.SqlEncode(Me.Symbol) & ",rate=" & Me.Rate & ",fk_translation_key_notes=" & Me.translation.Key & ",culture=" & da.SqlEncode(Me.Culture) & " WHERE ID=" & Me.ID
        sql$ = "UPDATE [Currency] set code=" & da.SqlEncode(Me.Code) & ",symbol=" & da.SqlEncode(Me.Symbol) & ",rate=" & Me.Rate & ",fk_translation_key_notes=" & Me.translation.Key & " WHERE ID=" & Me.ID
        da.DBExecutesql(sql$)

    End Sub

    Public Sub Delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        Dim SQL$
        SQL$ = "DELETE FROM [CURRENCY] WHERE ID=" & Me.ID

        Try
            'there's a good chance this will fail (due to RI)
            da.DBExecutesql(SQL$)
            iq.Currencies.Remove(Me.ID)

        Catch ex As Exception

            errorMessages.Add(ex.Message.ToString)
        End Try

    End Sub

    Public Sub New(ByVal Code As String, ByVal Code_HP As String, translation As clsTranslation, ByVal symbol As String, ByVal rate As Single, Notes As clsTranslation) ', culture As String)

        Dim nk$
        If Notes Is Nothing Then nk = "null" Else nk$ = Notes.Key

        Dim sql$
        'sql$ = "INSERT INTO Currency (Code,Symbol,Rate,fk_translation_key_Name,fk_translation_key_notes,culture) VALUES ("
        sql$ = "INSERT INTO Currency (Code,Code_HP,Symbol,Rate,fk_translation_key_Name,fk_translation_key_notes) VALUES ("
        'sql$ &= SqlEncode(Code) & "," & da.SqlEncode(symbol) & "," & rate & "," & translation.Key & "," & nk & "," & da.SqlEncode(culture) & " );"
        sql$ &= da.SqlEncode(Code) & "," & da.SqlEncode(Code_HP) & "," & da.SqlEncode(symbol) & "," & rate & "," & translation.Key & "," & nk & " );"

        Me.ID = da.DBExecutesql(sql$, True)
        Me.Code = Code
        Me.Code_HP = Code_HP
        Me.Symbol = symbol
        Me.Rate = rate
        Me.Notes = Notes
        Me.translation = translation
        'Me.Culture = culture

        iq.Currencies.Add(Me.ID, Me)
        iq.i_currency_code.Add(Me.Code, Me)

    End Sub

    Public Sub New(ByVal ID As Integer, ByVal Code As String, ByVal Code_HP As String, translation As clsTranslation, ByVal symbol As String, ByVal rate As Single, notes As clsTranslation) ', culture As String)

        Me.ID = ID
        Me.Code = Code
        Me.Code_HP = Code_HP
        Me.Symbol = symbol
        Me.Rate = rate
        Me.Notes = notes
        Me.translation = translation
        '    Me.Culture = culture
        iq.Currencies.Add(Me.ID, Me)
        iq.i_currency_code.Add(Me.Code, Me)

    End Sub


End Class

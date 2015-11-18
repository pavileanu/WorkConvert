Imports System.Runtime.Serialization
Imports dataAccess

<DataContract()>
Public Class clsTranslation

    Public Sub New()

    End Sub
    ' Dim ID As Integer
    Property Key As Integer
    Private iText As Dictionary(Of clsLanguage, String)
    Property ID As Dictionary(Of clsLanguage, Integer)

    Property Group As String 'this translation belongs to a group of options - which would allow attributes to be picked from a list - instead of typed
    Property Order As Integer  'this is the order of this option in that list

    Public Function compoundkey(language As clsLanguage) As String

        'compound key will be text^group^language?
        Return Me.text(language) & "^" & Me.Group & "^" & language.Code & "^" & Me.Order

    End Function

    Public Sub addLanguage(language As clsLanguage, text As String, writecache As DataTable)
        If Not Me.iText.ContainsKey(language) Then
            Me.iText.Add(language, text)
            If writecache Is Nothing Then
                Dim sql$
                sql$ = "INSERT INTO [translation] ([key],[text],fk_language_id,[group],[order]) VALUES ("
                sql$ &= Me.Key & ",N" & da.SqlEncode(text) & "," & language.ID & "," & da.SqlEncode(Group) & "," & Order & ");"
                'INserts the new row - and stores a copy of the ID in a dictioanry under the langauge
                Me.ID.Add(language, da.DBExecutesql(sql$, True))
            Else
                Dim row As System.Data.DataRow
                row = writecache.NewRow()
                row("key") = Me.Key
                row("text") = text
                row("fk_language_id") = language.ID
                row("group") = Group
                row("order") = Order
                writecache.Rows.Add(row)
                'NB: this DOESNT increment the key - as were adding another language

            End If
        Else
            If writecache Is Nothing Then
                Dim sql$
                sql$ = "UPDATE [translation] SET text = " & da.SqlEncode(text) & " WHERE [key] = " & Me.Key & " AND fk_language_id=" & language.ID & " AND [group]=" & da.SqlEncode(Me.Group) & ""
                Me.iText(language) = text
                da.DBExecutesql(sql$, False)
            End If
        End If

        clsIQ.IndexTL(Me, language)
    End Sub

    Public Sub addLanguage(language As clsLanguage, id As Int32, text As String)
        If Not Me.ID.ContainsKey(language) Then
            Me.ID.Add(language, id)
            Me.iText.Add(language, text)
            clsIQ.IndexTL(Me, language)
        End If
    End Sub
    Public Function clone() As clsTranslation

        'todo .. doesn't do translations in other languages
        Return New clsTranslation(Me.iText.Keys(0), "Copy of " & Me.iText.Values(0), Me.Group, Me.Order + 1)

    End Function

    Public Function HTML(language As clsLanguage) As Literal

        HTML = New Literal
        If Me.iText.ContainsKey(language) Then
            HTML.Text = Me.iText(language)
        Else
            HTML.Text = "<span class='missingTranslation'>" & Me.iText(English) & "</span>"
        End If

    End Function

    Public Function SortValue(language As clsLanguage) As Int64

        If Me.Order > 0 Then
            ' If Order <> 1 Then Stop
            SortValue = Order
        Else

            'there isn't reall any use for an alphabetical sort

            'Dim words() As String = Split(Trim(Me.iText(language)))
            'Dim pwr As Int64 = 1
            'SortValue = 0
            'For Each w In words.Take(3)
            '    If w <> "" Then  'phrases with a double space hav cause this to be empty
            '        If Len(w) > 1 Then
            '            SortValue += CSng(Asc(Mid(w, 2, 1))) * pwr : pwr = pwr * 64
            '        End If
            '        SortValue += Asc(Left(w, 1)) * pwr : pwr = pwr * 64
            '        ' SortValue += Asc(Left(w, 1)) * pwr : pwr = pwr * 256 - gets too big (and an exponent)
            '    End If
            'Next

            SortValue = Me.Key

        End If


    End Function

    Public Property text(language As clsLanguage) As String
        Get

            If Me.iText.ContainsKey(language) Then
                If language Is English Then
                    Return (Me.iText(language))
                Else
                    Return Me.iText(language)
                End If

            Else
                If Me.iText.ContainsKey(English) Then
                    Return Me.iText(English)  '"*" & Me.iText(English)
                ElseIf iq.i_language_Code.ContainsKey("KY") AndAlso Me.iText.ContainsKey(iq.i_language_Code("KY")) Then 'ML Added check for KY existence as this was breaking the ?reload=1 on signin, master.aspx must not refer to anything which relies on the OM
                    Return Me.iText(iq.i_language_Code("KY")) '(iq.i_language_Code("KY")) 'Return "**" & Me.iText(iq.i_language_Code("KY"))
                Else
                    Return Nothing ' should never happen
                End If
            End If

        End Get

        Set(value As String)

            Me.iText(language) = value

        End Set

    End Property
    Public Property textTranslation(language As clsLanguage) As String
        Get

            If Me.iText.ContainsKey(language) Then
                Return Me.iText(language)
            Else

                Return ""

            End If

        End Get

        Set(value As String)

            Me.iText(language) = value

        End Set

    End Property
    Public Sub remove(language As clsLanguage)

        Me.iText.Remove(language)

    End Sub

    Public Shared Function NextKey() As Integer

        Dim reader As SqlClient.SqlDataReader
        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase()

        reader = da.DBExecuteReader(con, "Select max([key])+1 as c from translation")
        If reader.HasRows Then
            reader.Read()
            If IsDBNull(reader.Item(0)) Then
                NextKey = 1
            Else
                NextKey = reader.Item(0)
            End If
        Else
            NextKey = 1
        End If

        reader.Close()
        con.Close()

    End Function

    Public Sub New(language As clsLanguage, text As String, Optional group As String = "", Optional order As Integer = 0, Optional writecache As DataTable = Nothing, Optional ByRef nextkey As Integer = 0)

        'Creates a NEW translation
        

        Me.iText = New Dictionary(Of clsLanguage, String)
        Me.ID = New Dictionary(Of clsLanguage, Integer)

        Me.iText.Add(language, text)
        Me.Group = group
        Me.Order = order

        If nextkey <> 0 And writecache Is Nothing Then Stop
        If writecache IsNot Nothing And nextkey = 0 Then Stop

        'Me.Key = iq.NextKey
        If writecache Is Nothing Then
            Dim sql$
            Me.Key = da.DBSelectFirst("select max([key])+1 from translation")
            sql$ = "INSERT INTO [translation] ([key],[text],fk_language_id,[group],[order],deleted) VALUES (" & Me.Key & ","
            sql$ &= da.SqlEncode(text) & "," & language.ID & "," & da.SqlEncode(group) & "," & order & ",0);"
            'INserts the new row - and stores a copy of the ID in a dictioanry under the langauge

            Dim id As Integer = da.DBExecutesql(sql$, True)
            Me.ID.Add(language, id)  'NB: Translations have an array of ID's (one for each language)

            'now select back the ID


        Else

            ' Me.ID = -1
            Me.Key = nextkey
            Dim row As System.Data.DataRow
            row = writecache.NewRow()
            row("key") = Me.Key
            row("text") = text
            row("fk_language_id") = language.ID
            row("group") = group
            row("order") = order
            row("deleted") = False
            writecache.Rows.Add(row)

            'this isn't going to populate the ID's dictionary
            nextkey += 1

        End If

        clsIQ.IndexTL(Me, language)

    End Sub

    Public Sub New(key As Integer, language As clsLanguage, text As String, id As Int32, Optional group As String = "", Optional order As Integer = 0)

        'this constructor is called when reloading from the database.. it's slightly different from most (which have an ID)
        'tranlations have a KEY instead - becuase all the different (language) translations share tehen same KEY but have different ID's

        ' Me.ID = id
        Me.Key = key
        'Me.Language = language
        'Me.Text = text
        If Me.iText Is Nothing Then Me.iText = New Dictionary(Of clsLanguage, String)
        If Me.ID Is Nothing Then Me.ID = New Dictionary(Of clsLanguage, Integer)
        Me.Group = group
        Me.Order = order

        Me.ID.Add(language, id)
        Me.iText.Add(language, text)
        clsIQ.IndexTL(Me, language)

    End Sub

    Public Function delete(language As clsLanguage) As Boolean

        If Me.ID.ContainsKey(language) Then
            Dim sql$
            sql$ = "DELETE FROM translation WHERE id=" & Me.ID(language) & ";"
            Try
                'it may not be possible to remove this translation if it still referenced by another object (RI)
                da.DBExecutesql(sql$)


                clsIQ.deleteTL(Me, language)

                Return True

            Catch
                Return False
            End Try
        End If


    End Function
    Public Sub Update(language As clsLanguage)

        '        Stop

        Dim sql$
        sql$ = "Update translation set text=" & da.SqlEncode(text(language)) & ",[group]=" & da.SqlEncode(Me.Group) & ",[order]=" & Order & " where [key]=" & Me.Key & " and fk_language_id=" & language.ID
        da.DBExecutesql(sql$, False)

    End Sub

End Class 'clsTranslation




Imports dataAccess

Public Class clsScheme

    Implements i_Editable

    Public ID As Integer
    Public Name As clsTranslation
    Public code As String
    Public Region As clsRegion
    Public StartDate As Date
    Public EndDate As Date
    Public Active As Boolean

    Public Sub delete(ByRef errormessages As List(Of String)) Implements i_Editable.delete

        Dim sql$
        sql$ = "delete from Scheme where id=me.id"
        Try
            da.DBExecutesql(sql$)
            iq.Schemes.Remove(Me.ID)

        Catch ex As Exception
            errormessages.Add(ex.Message)
        End Try


    End Sub

    Public Function displayName(Language As clsLanguage) As String Implements i_Editable.displayName

        Return Me.Name.text(Language)

    End Function


    Public Sub New()  'the editor requires a parameterless constructor



    End Sub

    Public Function compoundKey() As String
        Return Me.Region.ID & "^" & Me.StartDate & "^" & Me.EndDate
    End Function
    Public Sub New(id As Integer, code As String, name As clsTranslation, Region As clsRegion, Startdate As Date, Enddate As Date)

        Me.ID = id
        Me.code = code
        Me.Name = name
        Me.Region = Region
        Me.StartDate = Startdate
        Me.EndDate = Enddate

        iq.Schemes.Add(Me.ID, Me)
        If Not iq.i_scheme_code.ContainsKey(Me.code) Then iq.i_scheme_code.Add(Me.code, New List(Of clsScheme))
        iq.i_scheme_code(Me.code).Add(Me)

    End Sub

    Sub New(code As String, name As clsTranslation, Region As clsRegion, Startdate As Date, Enddate As Date, Optional writecache As DataTable = Nothing)

        Me.code = code
        Me.Name = name
        Me.Region = Region
        Me.StartDate = Startdate
        Me.EndDate = Enddate

        If Not iq.i_scheme_code.ContainsKey(Me.code) Then iq.i_scheme_code.Add(Me.code, New List(Of clsScheme))
        iq.i_scheme_code(Me.code).Add(Me)


        If writecache Is Nothing Then
            Dim sql$
            sql$ = "INSERT INTO [Scheme] (code,fk_translation_key_name,StartDate,EndDate,fk_region_id) "
            sql$ &= "VALUES (" & da.SqlEncode(Me.code) & "," & name.Key & "," & da.UniversalDate(Startdate) & "," & da.UniversalDate(Enddate) & "," & Region.ID & ");"
            Me.ID = da.DBExecutesql(sql, True)

            iq.Schemes.Add(Me.ID, Me)

        Else
            Dim row As System.Data.DataRow
            row = writecache.NewRow()
            row("code") = Me.code
            row("fk_translation_key_name") = Me.Name.Key
            row("startdate") = Startdate
            row("enddate") = Enddate
            row("fk_region_id") = Region.ID
            writecache.Rows.Add(row)
        End If

    End Sub

    Public Function Insert(ByRef errormessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsScheme("new", iq.AddTranslation("New Loyalty Scheme", English, "Lschemes", 0, Nothing, 0, True), r_worldwide, Now, DateAdd(DateInterval.Year, 1, Now))

    End Function

    Public Sub update(ByRef errormessages As List(Of String)) Implements i_Editable.update

        Dim sql$
        sql$ = "UPDATE [scheme] set (code=" & da.SqlEncode(Me.code) & ",fk_translation_key_name=" & Me.Name.Key & ",StartDate=" & da.UniversalDate(Me.StartDate) & ",enddate=" & da.UniversalDate(Me.EndDate) & ",fk_region_id=" & Me.Region.ID & " WHERE ID=" & Me.ID
        da.DBExecutesql(sql$, False)

    End Sub

End Class

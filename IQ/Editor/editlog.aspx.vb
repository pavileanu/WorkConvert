Imports dataAccess

Public Class editlog
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        showEditsTable()

    End Sub

    Private Function MakeLogTableRow(rdr As SqlClient.SqlDataReader) As TableRow

        Dim tr As TableRow = New TableRow

        For Each column In Split("id,Action,Path,oldValue,NewValue,timestamp,fk_account_id_agent,comments,undone", ",")
            Dim td As New TableCell
            tr.Controls.Add(td)

            Dim v As String
            If column.ToLower = "id" Then
                v = ""
                Dim btn As Button = New Button
                btn.Text = "undo"
                btn.Attributes("rowID") = rdr.Item("id")
                AddHandler btn.Click, AddressOf undo
                td.Controls.Add(btn)

            ElseIf column.ToLower = "fk_account_id_agent" Then
                v = iq.Accounts(rdr.Item(column)).User.RealName
                td.Text = v
            Else
                v = rdr.Item(column).ToString
                td.Text = v
            End If
        Next

        Return tr

    End Function

    Public Function showEditsTable()


        form1.Controls.Clear()
        form1.Controls.Add(NewLit("<p>Most recent activity is at the top</p><p>"))
        Dim t As Table = New Table
        t.CssClass = "adminTable"
        Form.Controls.Add(t)
        Dim thr As TableHeaderRow = MakeTHR("id,Action,path,oldvalue,newvalue,timestamp,by,comments,undone", "Edit, Upate, Add or Delete|The location (in the object model) of the item changed|previous data value|New (current) data value,When the change was made|Who made the change,Editable Comments|When (if) this change was undone.", "")
        t.Controls.Add(thr)

        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, "Select top 50 * from editlog order by id desc")

        While rdr.Read

            Dim tr As TableRow = MakeLogTableRow(rdr)
            t.Controls.Add(tr)

        End While

        rdr.Close()
        con.Close()

    End Function

    Public Function undo(obj As Object, e As EventArgs)

        Dim btn As Button = obj

        Dim sql$
        sql$ = "SELECT oldvalue,path,action from Editlog where id =" & btn.Attributes("rowID")
        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql$)
        rdr.Read()

        undoLogEntry(rdr.Item("action"), rdr.Item("path"), rdr.Item("oldvalue"))

        sql$ = "update editlog set undone=getdate() where id=" & btn.Attributes("rowID")

        da.DBExecutesql(sql$)

        showEditsTable()

    End Function

    Private Function undoLogEntry(action As String, path As String, oldvalue As String)

        Dim obj As Object
        Dim pobj As Object

        Dim errormessages As New List(Of String)

        Dim bits() As String = Split(path$, ".")
        path$ = Left$(path$, InStrRev(path$, ".") - 1)

        Reflection.ParsePath(path, obj, pobj, errormessages)

        If action = "E" Then
            setProperty(obj, bits.Last, oldvalue, 0, errormessages, False)

        End If

        For Each e In errormessages
            Form.Controls.Add(ErrorDymo(e))
        Next

    End Function

End Class
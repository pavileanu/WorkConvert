Public Class ValidationManager
    Inherits System.Web.UI.Page

    Public ds As Array
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        'Validate user
        Dim lid As UInt64 = CULng(Request.QueryString("lid"))
        If Not iq.SeshAlive(lid) OrElse Not UserIsAdmin(lid) Then Response.Redirect("../signin.aspx")

        If Not IsPostBack Then
            'Grab systemtypes available


            ddSysTypes.DataSource = iq.ProductValidationsAssignment.Keys.ToList()
            ddSysTypes.DataSource.Insert(0, "None")
            ddSysTypes.DataBind()
            If Request("typ") IsNot Nothing Then
                ddSysTypes.SelectedValue = Request("typ")
                dgProdVals.DataSource = iq.ProductValidationsAssignment(ddSysTypes.SelectedValue)
                dgProdVals.DataBind()
            End If
        End If
        If IsPostBack Then
            'Load Prod Vals for this
            If iq.ProductValidationsAssignment.ContainsKey(ddSysTypes.SelectedValue) Then
                dgProdVals.DataSource = iq.ProductValidationsAssignment(ddSysTypes.SelectedValue)
                dgProdVals.DataBind()
            End If

        End If
    End Sub

    Protected Sub dgProdVals_DataBinding(sender As Object, e As DataGridItemEventArgs)
        Dim a As DropDownList = CType(e.Item.FindControl("ddsProdValType"), DropDownList)
        If a IsNot Nothing Then
            a.DataSource = [Enum].GetNames(GetType(enumValidationType))
            a.DataBind()
            a.SelectedValue = CType(e.Item.DataItem, clsProductValidation).ValidationType.ToString()
        End If
        Dim b As DropDownList = CType(e.Item.FindControl("ddsSeverity"), DropDownList)
        If b IsNot Nothing Then
            b.DataSource = [Enum].GetNames(GetType(EnumValidationSeverity))
            b.DataBind()
            b.SelectedValue = CType(e.Item.DataItem, clsProductValidation).Severity.ToString()
        End If
    End Sub

    Protected Sub btnAdd_Click(sender As Object, e As EventArgs)
        Response.Redirect("ValidationEditor.aspx?lid=" + Request.QueryString("lid") + "&vid=new" + "&typ=" + ddSysTypes.SelectedValue.ToString())
    End Sub

    Protected Sub dgProdVals_EditCommand(source As Object, e As DataGridCommandEventArgs)

    End Sub

    Protected Sub dgProdVals_ItemCommand(source As Object, e As DataGridCommandEventArgs)
        If (e.CommandName = "Follow") Then
            Response.Redirect("ValidationEditor.aspx?lid=" + Request.QueryString("lid") + "&vid=" + e.Item.Cells(0).Text + "&typ=" + ddSysTypes.SelectedValue.ToString())
        ElseIf e.CommandName = "Delete" Then
            iq.ProductValidationsAssignment(ddSysTypes.SelectedValue.ToString()).Find(Function(va) va.ID = CInt(e.Item.Cells(0).Text)).Delete(ddSysTypes.SelectedValue.ToString())
        End If
    End Sub
End Class
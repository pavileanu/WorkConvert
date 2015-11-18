Public Class ValidationEditor
    Inherits System.Web.UI.Page

    Public ThisValidation As clsProductValidation
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim lid As UInt64 = Request.QueryString("lid")
        If Not iq.SeshAlive(lid) OrElse Not UserIsAdmin(lid) Then Response.Redirect("../signin.aspx")

        Dim vid As Integer = -1
        If (Request("vid") Is Nothing OrElse Not Integer.TryParse(Request("vid"), vid)) AndAlso Request("vid") <> "new" Then Response.Redirect("../signin.aspx")


        ddlType.DataSource = [Enum].GetNames(GetType(enumValidationType))
        ddlType.DataBind()

        ddlSeverity.DataSource = [Enum].GetNames(GetType(EnumValidationSeverity))
        ddlSeverity.DataBind()

        ddlMessageType.DataSource = [Enum].GetNames(GetType(enumValidationMessageType))
        ddlMessageType.DataBind()

        If Request("vid") <> "new" Then
            ThisValidation = iq.ProductValidationsAssignment(Request("typ")).Where(Function(v) v.ID = vid).FirstOrDefault()
            If ThisValidation Is Nothing Then Exit Sub

            ddlSeverity.SelectedValue = If(ThisValidation IsNot Nothing, ThisValidation.Severity.ToString(), "")
            ddlType.SelectedValue = If(ThisValidation IsNot Nothing, ThisValidation.ValidationType.ToString(), "")
            txtOptType.Text = ThisValidation.RequiredOptType
            txtMessage.Text = ThisValidation.Message.text(English)
            txtCheckAttr.Text = ThisValidation.CheckAttribute
            txtDepOpt.Text = ThisValidation.DependantOptType
            txtDepCheckAttr.Text = ThisValidation.DependantCheckAttribute
            txtDepCheckAttrValue.Text = ThisValidation.DependantCheckAttributeValue
            txtCheckAttrValue.Text = ThisValidation.CheckAttributeValue
            txtOptFamily.Text = ThisValidation.OptionFamily
            txtCorrectMessage.Text = ThisValidation.CorrectMessage.text(English)
            txtQuantity.Text = ThisValidation.RequiredQuantity.ToString
            txtLinkOptType.Text = ThisValidation.LinkOptType
            txtLinkTechnology.Text = ThisValidation.LinkTechnology
            txtLinkOptionFamily.Text = ThisValidation.LinkOptionFamily
            ddlMessageType.SelectedValue = If(ThisValidation IsNot Nothing, ThisValidation.ValidationMessageType.ToString, "")
        End If
    End Sub

    Protected Sub Unnamed_Click(sender As Object, e As EventArgs)

        If Request("vid") = "new" Then
            Dim a = New clsProductValidation(Request.Form("ddlMessageType"), Request.Form("txtOptType"), Request.Form("ddlType"), Request.Form("ddlSeverity"), Request.Form("txtCheckAttr"), Request.Form("txtDepOpt"), Request.Form("txtMessage"), Request.Form("txtDepCheckAttr"), Request.Form("txtReqQty"), Request.Form("txtDepCheckAttrValue"), Request.Form("txtDepCheckvalue"), Request.Form("txtOptFamily"), Request("typ"), Request.Form("txtCorrectMessage"), "", "", "")
        Else
            Dim c As clsProductValidation = iq.ProductValidationsAssignment(Request("typ")).Where(Function(v) v.ID = Request("vid")).FirstOrDefault()
            c.DependantOptType = Request("txtDepOpt")
            c.Message.text(English) = Request("txtMessage")
            c.CorrectMessage.text(English) = Request("txtCorrectMessage")
            c.RequiredOptType = Request("txtOptType")
            c.Severity = [Enum].Parse(GetType(EnumValidationSeverity), Request("ddlSeverity"))
            c.DependantCheckAttribute = Request("txtDepCheckAttr")
            c.DependantCheckAttributeValue = Request("txtDepCheckAttrValue")
            c.CheckAttribute = Request("txtCheckAttr")
            c.CheckAttributeValue = Request("txtCheckAttrValue")
            c.ValidationType = [Enum].Parse(GetType(enumValidationType), Request("ddlType"))
            c.OptionFamily = Request("txtOptFamily")
            c.RequiredQuantity = CInt(Request("txtQuantity"))
            c.ValidationMessageType = [Enum].Parse(GetType(enumValidationMessageType), Request("ddlMessageType"))
            c.LinkOptType = Request("txtLinkOptType")
            c.LinkTechnology = Request("txtLinkTechnology")
            c.LinkOptionFamily = Request("txtLinkOptionFamily")
            c.Update()
        End If

        Response.Redirect("validationmanager.aspx?lid=" + Request.QueryString("lid") + "&typ=" + Request("typ"))
    End Sub
End Class
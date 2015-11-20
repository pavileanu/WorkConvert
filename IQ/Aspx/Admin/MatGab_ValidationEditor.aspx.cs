public class ValidationEditor : System.Web.UI.Page
{

	public clsProductValidation ThisValidation;
	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		UInt64 lid = Request.QueryString("lid");
		if (!iq.SeshAlive(lid) || !UserIsAdmin(lid))
			Response.Redirect("../signin.aspx");

		int vid = -1;
		if ((Request("vid") == null || !int.TryParse(Request("vid"), vid)) && Request("vid") != "new")
			Response.Redirect("../signin.aspx");


		ddlType.DataSource = Enum.GetNames(typeof(enumValidationType));
		ddlType.DataBind();

		ddlSeverity.DataSource = Enum.GetNames(typeof(EnumValidationSeverity));
		ddlSeverity.DataBind();

		ddlMessageType.DataSource = Enum.GetNames(typeof(enumValidationMessageType));
		ddlMessageType.DataBind();

		if (Request("vid") != "new") {
			ThisValidation = iq.ProductValidationsAssignment(Request("typ")).Where(v => v.ID == vid).FirstOrDefault();
			if (ThisValidation == null)
				return;

			ddlSeverity.SelectedValue = ThisValidation != null ? ThisValidation.Severity.ToString() : "";
			ddlType.SelectedValue = ThisValidation != null ? ThisValidation.ValidationType.ToString() : "";
			txtOptType.Text = ThisValidation.RequiredOptType;
			txtMessage.Text = ThisValidation.Message.text(English);
			txtCheckAttr.Text = ThisValidation.CheckAttribute;
			txtDepOpt.Text = ThisValidation.DependantOptType;
			txtDepCheckAttr.Text = ThisValidation.DependantCheckAttribute;
			txtDepCheckAttrValue.Text = ThisValidation.DependantCheckAttributeValue;
			txtCheckAttrValue.Text = ThisValidation.CheckAttributeValue;
			txtOptFamily.Text = ThisValidation.OptionFamily;
			txtCorrectMessage.Text = ThisValidation.CorrectMessage.text(English);
			txtQuantity.Text = ThisValidation.RequiredQuantity.ToString;
			txtLinkOptType.Text = ThisValidation.LinkOptType;
			txtLinkTechnology.Text = ThisValidation.LinkTechnology;
			txtLinkOptionFamily.Text = ThisValidation.LinkOptionFamily;
			ddlMessageType.SelectedValue = ThisValidation != null ? ThisValidation.ValidationMessageType.ToString : "";
		}
	}


	protected void Unnamed_Click(object sender, EventArgs e)
	{
		if (Request("vid") == "new") {
			object a = new clsProductValidation(Request.Form("ddlMessageType"), Request.Form("txtOptType"), Request.Form("ddlType"), Request.Form("ddlSeverity"), Request.Form("txtCheckAttr"), Request.Form("txtDepOpt"), Request.Form("txtMessage"), Request.Form("txtDepCheckAttr"), Request.Form("txtReqQty"), Request.Form("txtDepCheckAttrValue"),
			Request.Form("txtDepCheckvalue"), Request.Form("txtOptFamily"), Request("typ"), Request.Form("txtCorrectMessage"), "", "", "");
		} else {
			clsProductValidation c = iq.ProductValidationsAssignment(Request("typ")).Where(v => v.ID == Request("vid")).FirstOrDefault();
			c.DependantOptType = Request("txtDepOpt");
			c.Message.text(English) = Request("txtMessage");
			c.CorrectMessage.text(English) = Request("txtCorrectMessage");
			c.RequiredOptType = Request("txtOptType");
			c.Severity = Enum.Parse(typeof(EnumValidationSeverity), Request("ddlSeverity"));
			c.DependantCheckAttribute = Request("txtDepCheckAttr");
			c.DependantCheckAttributeValue = Request("txtDepCheckAttrValue");
			c.CheckAttribute = Request("txtCheckAttr");
			c.CheckAttributeValue = Request("txtCheckAttrValue");
			c.ValidationType = Enum.Parse(typeof(enumValidationType), Request("ddlType"));
			c.OptionFamily = Request("txtOptFamily");
			c.RequiredQuantity = (int)Request("txtQuantity");
			c.ValidationMessageType = Enum.Parse(typeof(enumValidationMessageType), Request("ddlMessageType"));
			c.LinkOptType = Request("txtLinkOptType");
			c.LinkTechnology = Request("txtLinkTechnology");
			c.LinkOptionFamily = Request("txtLinkOptionFamily");
			c.Update();
		}

		Response.Redirect("validationmanager.aspx?lid=" + Request.QueryString("lid") + "&typ=" + Request("typ"));
	}
}

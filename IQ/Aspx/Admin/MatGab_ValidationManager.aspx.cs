public class ValidationManager : System.Web.UI.Page
{

	public Array ds;
	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		//Validate user
		UInt64 lid = Request.QueryString("lid");
		if (!iq.SeshAlive(lid) || !UserIsAdmin(lid))
			Response.Redirect("../signin.aspx");

		if (!IsPostBack) {
			//Grab systemtypes available


			ddSysTypes.DataSource = iq.ProductValidationsAssignment.Keys.ToList();
			ddSysTypes.DataSource.Insert(0, "None");
			ddSysTypes.DataBind();
			if (Request("typ") != null) {
				ddSysTypes.SelectedValue = Request("typ");
				dgProdVals.DataSource = iq.ProductValidationsAssignment(ddSysTypes.SelectedValue);
				dgProdVals.DataBind();
			}
		}
		if (IsPostBack) {
			//Load Prod Vals for this
			if (iq.ProductValidationsAssignment.ContainsKey(ddSysTypes.SelectedValue)) {
				dgProdVals.DataSource = iq.ProductValidationsAssignment(ddSysTypes.SelectedValue);
				dgProdVals.DataBind();
			}

		}
	}

	protected void dgProdVals_DataBinding(object sender, DataGridItemEventArgs e)
	{
		DropDownList a = e.Item.FindControl("ddsProdValType");
		if (a != null) {
			a.DataSource = Enum.GetNames(typeof(enumValidationType));
			a.DataBind();
			a.SelectedValue = ((clsProductValidation)e.Item.DataItem).ValidationType.ToString();
		}
		DropDownList b = e.Item.FindControl("ddsSeverity");
		if (b != null) {
			b.DataSource = Enum.GetNames(typeof(EnumValidationSeverity));
			b.DataBind();
			b.SelectedValue = ((clsProductValidation)e.Item.DataItem).Severity.ToString();
		}
	}

	protected void btnAdd_Click(object sender, EventArgs e)
	{
		Response.Redirect("ValidationEditor.aspx?lid=" + Request.QueryString("lid") + "&vid=new" + "&typ=" + ddSysTypes.SelectedValue.ToString());
	}


	protected void dgProdVals_EditCommand(object source, DataGridCommandEventArgs e)
	{
	}

	protected void dgProdVals_ItemCommand(object source, DataGridCommandEventArgs e)
	{
		if ((e.CommandName == "Follow")) {
			Response.Redirect("ValidationEditor.aspx?lid=" + Request.QueryString("lid") + "&vid=" + e.Item.Cells(0).Text + "&typ=" + ddSysTypes.SelectedValue.ToString());
		} else if (e.CommandName == "Delete") {
			iq.ProductValidationsAssignment(ddSysTypes.SelectedValue.ToString()).Find(va => va.ID == (int)e.Item.Cells(0).Text).Delete(ddSysTypes.SelectedValue.ToString());
		}
	}
}

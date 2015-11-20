using System.Globalization;
using dataAccess;

public class HpDefault : System.Web.UI.Page
{

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{

		if (!Page.IsPostBack) {

			List<string> errormessages = new List<string>();

			IEnumerable<clsRegion> regsEnum = from j in iq.Channels.Valueswhere j.Code.StartsWith("MHP") & j.Code.EndsWith("U") & j.Universal == true(j.Region);
			//Dim regs As List(Of clsRegion) = New List(Of clsRegion)(regsEnum)
			ListItem regionItem;
			List<ListItem> allregions = new List<ListItem>();
			foreach (clsRegion reg in regsEnum) {
				regionItem = new ListItem();
				regionItem.Text = reg.Name.text(English);
				if (reg.Culture == null | Trim(reg.Culture.Code) == "") {
					regionItem.Value = "EN|" + reg.Code;
				} else {
					regionItem.Value = reg.Culture.Code + "|" + reg.Code;
				}

				allregions.Add(regionItem);

			}

			List<ListItem> sortedRegions = allregions.OrderBy(x => x.Text).ToList();
			lstcountries.DataSource = sortedRegions;
			lstcountries.DataTextField = "Text";
			lstcountries.DataValueField = "Value";

			lstcountries.DataBind();
		}
		//For Each clsChannel As channel In iq.Channels

		//NextDim channel As clsChannel = (From j In iq.Channels.Values Where j.Name.Contains("Computer 2000")).First

	}
	///<summary>Generates a table  country name, two letter ISO , culture info  & culturinfo ISO</summary>
	private void GenerateCultureandRegionInfo()
	{
		CultureInfo[] cinfos = CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures);
		Response.Write("<table border=\"1\"><tr><th>Country Name</th><th>English Name </th><th>Language-Country code</th><th></th></tr>");
		foreach (CultureInfo cinfo in cinfos) {
			if (!(cinfo.LCID == 127 | cinfo.IsNeutralCulture)) {
				RegionInfo region = new RegionInfo(cinfo.LCID);
				Response.Write("<tr><td>" + region.DisplayName + "</td><td>" + region.TwoLetterISORegionName + "</td><td>" + cinfo.Name + "</td><td>" + cinfo.TwoLetterISOLanguageName + "</td></tr>");
				object sql;
				sql = "UPDATE [Region] set ";

				sql += "[Culture]= '" + UCase(cinfo.TwoLetterISOLanguageName) + "'";
				sql += " WHERE [code] = '" + region.TwoLetterISORegionName + "'";

				da.DBExecutesql(sql, false);

			}

		}
		Response.Write("</table>");
	}

	protected void  // ERROR: Handles clauses are not supported in C#
btnRegister_Click(object sender, EventArgs e)
	{
		Response.Redirect("HPSignup.Aspx?lang=" + lstcountries.SelectedValue);
	}
}

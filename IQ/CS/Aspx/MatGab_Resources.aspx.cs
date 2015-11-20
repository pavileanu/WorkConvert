using System.Net;
using System.IO;

public class Resources : System.Web.UI.Page
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		UInt64 lid;
		clsAccount agentAccount;
		StringBuilder menuHtml = new StringBuilder();

		if (iq.seshDic == null)
			return;
		if (Request.QueryString("lid") == null)
			return;
		lid = (UInt64)Request.QueryString("lid");

		if (!iq.seshDic(lid).ContainsKey("AgentAccount"))
			return;
		agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");

		if (iq.ResourceCategories == null || iq.ResourceCategories.Count == 0)
			return;

		// Display which side of the split we're on
		titleLabel.Text = Xlt("iQuote Resources", agentAccount.Language);


		foreach ( resourceCategory in iq.ResourceCategories.Values.OrderBy(rc => (rc.Order))) {
			// Look for any resource files in this category the user has access to
			object visibleResourceFiles = new List<clsResource>();

			if (!resourceCategory.Resources == null) {

				foreach ( resourceFile in resourceCategory.Resources.OrderBy(r => (r.Order))) {
					bool show = true;

					// Optionally filter by region
					if (!resourceFile.Region == null && !resourceFile.Region.Encompasses(agentAccount.SellerChannel.Region)) {
						show = false;
					}

					// Optionally filter by language
					if (show && !resourceFile.Language == null && resourceFile.Language.Code != agentAccount.Language.Code) {
						show = false;
					}

					// Optionally filter by MFR
					if (show && resourceFile.Manufacturer != Manufacturer.Unknown && resourceFile.Manufacturer != agentAccount.Manufacturer) {
						show = false;
					}

					// Optionally filter by Host
					if (show && !resourceFile.SellerChannel == null && resourceFile.SellerChannel.ID != agentAccount.SellerChannel.ID) {
						show = false;
					}

					if (show) {
						visibleResourceFiles.Add(resourceFile);
					}

				}
			}


			if (visibleResourceFiles.Count > 0) {
				menuHtml.Append(string.Format("<h2>{0}</h2>", resourceCategory.Translation.text(agentAccount.Language)));
				menuHtml.Append("<ul style=\"margin-left:0; padding-left:0;\">");

				foreach (clsResource resourceFile in visibleResourceFiles) {
					if (resourceFile.Embed) {
						menuHtml.Append(string.Format("<li><span class=\"link\" onclick=\"displayData('DocStoc.aspx?type={0}&ref={1}');return false;\"> {2}</span></li>", resourceFile.Type.ToLower(), resourceFile.Code, resourceFile.Title.text(agentAccount.Language)));
					} else {
						switch (resourceFile.Type.ToLower()) {

							case "youtube":

								menuHtml.Append(string.Format("<li><a class='link' target='_blank' href='https://www.youtube.com/watch?v={0}'>{1}</a></li>", resourceFile.Code, resourceFile.Title.text(agentAccount.Language)));
							case "pdf":
							case "pps":

								menuHtml.Append(string.Format("<li><a class='link' target='_blank' href='http://www.docstoc.com/docs/{0}'>{1}</a></li>", resourceFile.Code, resourceFile.Title.text(agentAccount.Language)));
							default:
								// Failover to embedded mode if no external URL has been set up for this resource type

								menuHtml.Append(string.Format("<li><span class=\"link\" onclick=\"displayData('DocStoc.aspx?type={0}&ref={1}');return false;\"> {2}</span></li>", resourceFile.Type.ToLower(), resourceFile.Code, resourceFile.Title.text(agentAccount.Language)));
						}

					}
				}

				menuHtml.Append("</ul>");

			}
		}

		litMenu.Text = menuHtml.ToString();

	}

}

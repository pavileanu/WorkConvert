using System.IO;
using System.Xml;

public class BasketDisplay : System.Web.UI.Page
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{


		string config = Request.Form("configuration");
		string sid = Request.Form("LONGSID");
		string acountNum = Request.Form("cAccountNum");
		string content = Request.Form("CONTENT");
		Response.Write("<b>Default Basket page if the Basket page has not been configured in Gatekeeper</b>");
		Response.Write("configuration" + vbNewLine);
		Response.Write("<XMP>" + config + "</XMP>");
		Response.Write("cAccountNum : " + acountNum);
		Response.Write(vbNewLine);
		Response.Write("LONGSID : " + sid);
		Response.Write(vbNewLine);
		Response.Write("CONTENT : " + content);


		foreach ( sItem in Request.Form) {
			Response.Write(sItem);
			Response.Write(" - [" + Request.Form(sItem) + "]" + vbNewLine);
		}

	}

	private string formatXML(string xmlstring)
	{
		StringWriter sw = new StringWriter();
		XmlTextWriter xw = new XmlTextWriter(sw);
		xw.Formatting = Formatting.Indented;
		xw.Indentation = 4;
		XmlDocument doc = new XmlDocument();
		doc.LoadXml(xmlstring);
		doc.Save(xw);
		return sw.ToString();
	}
}

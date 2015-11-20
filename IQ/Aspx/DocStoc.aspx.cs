public class DocStoc : System.Web.UI.Page
{

	public string FrameHeight;
	public string FrameWidth;
	public string DocVal;
	public string PdfObject;
	public string IFrameStyle;
	public string IFrameSrc;
	public string IFrameObject;
	public string ImageObject;

	public string ImageUrl;

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		// Displays the requested resource file in an embedded viewer

		string type = null;
		string @ref = null;
		string viewMode = null;

		if (Request.QueryString("type") == null || Request.QueryString("ref") == null)
			return;
		type = Request.QueryString("type").ToLower();
		@ref = Request.QueryString("ref");

		PdfObject = "none";
		IFrameObject = "none";
		ImageObject = "none";

		FrameHeight = "600";
		FrameWidth = "709";

		switch (type) {

			case "youtube":

				IFrameObject = "block";
				FrameHeight = "35em";
				FrameWidth = "62.22em";
				IFrameStyle = string.Format("height:{0};width:{1};border:0;", FrameHeight, FrameWidth);

				IFrameSrc = string.Format("https://www.youtube.com/embed/{0}?autoplay=1\" frameborder=\"0\" allowfullscreen=\"allowfullscreen", @ref);
			case "pdf":
			case "pps":

				PdfObject = "block";

				DocVal = string.Format("doc_id={0}&mem_id=18136568&doc_type={1}&fullscreen=0&showrelated=0&showotherdocs=0&showstats=0", @ref, type);
			case "sheet":
			case "spreadsheet":

				IFrameObject = "block";
				FrameHeight = "50em";
				FrameWidth = "85em";
				viewMode = "lv";
				// Alternatives are "ccc" and "pub"
				if (InStr(Request.ServerVariables("HTTP_USER_AGENT"), "MSIE") > 0)
					viewMode = "pub";
				IFrameStyle = string.Format("height: {0}; width: {1}; border:0;", FrameHeight, FrameWidth);

				IFrameSrc = string.Format("https://docs.google.com/spreadsheet/{0}?key={1}&hl=en&output=html&widget=true&rm=minimal&skipauth=true", viewMode, @ref);
		}
	}


}

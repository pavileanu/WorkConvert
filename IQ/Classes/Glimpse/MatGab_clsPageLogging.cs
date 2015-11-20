public class clsPageLogging : Page
{


	internal List<string> errorMessages = new List<string>();

	Stopwatch sw = new Stopwatch();

	public void  // ERROR: Handles clauses are not supported in C#
Page_Init(object sender, System.EventArgs e)
	{
		sw.Start();
		//MyBase.OnInit(e)
	}

	protected override void OnLoadComplete(EventArgs e)
	{
		sw.Stop();

		UInt64 lid = 0;
		try {
			if (Context.Request != null && Context.Request.QueryString.Count > 0)
				lid = Context.Request.QueryString("lid");

			AuditLog.Instance.Add(lid, "PageLoad", "", "", errorMessages, null, this.Context.Request.Path, this.Context.Request.RawUrl, sw.ElapsedMilliseconds, this.Context.Request.HttpMethod,
			this.Context.Request.UrlReferrer != null ? this.Context.Request.UrlReferrer.OriginalString : string.Empty);
		} catch (Exception ex) {
			//Oops, dont crash
		}

		base.OnLoadComplete(e);
	}


}

public class clsPageLogging : Page
{
    public clsPageLogging()
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        errorMessages = new List<string>();
        sw = new Stopwatch();

    }

    internal List<string> errorMessages; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.

    Stopwatch sw; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.

    public void Page_Init(object sender, System.EventArgs e)
    {

        sw.Start();
        //MyBase.OnInit(e)
    }

    protected override void OnLoadComplete(EventArgs e)
    {
        sw.Stop();

        UInt64 lid = 0;
        try
        {
            if (Context.Request != null && Context.Request.QueryString.Count > 0)
            {
                lid = Context.Request.QueryString("lid");
            }

            AuditLog.Instance.Add(
                lid,
                "PageLoad",
                "",
                "",
                errorMessages, null,
                this.Context.Request.Path,
                this.Context.Request.RawUrl,
                sw.ElapsedMilliseconds,
                this.Context.Request.HttpMethod, this.Context.Request.UrlReferrer != null ? this.Context.Request.UrlReferrer.OriginalString : string.Empty);
        }
        catch (Exception)
        {
            //Oops, dont crash
        }

        base.OnLoadComplete(e);
    }


}
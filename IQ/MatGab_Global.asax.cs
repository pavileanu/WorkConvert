using System.Web.SessionState;
using System.Web.Routing;
using System.Web.Http;

public class Global_asax : System.Web.HttpApplication
{


	private static LoggingList<string> ErrorLog = new LoggingList<string>();
	private void Application_Start(object sender, EventArgs e)
	{
		// Fires when the application is started

		//POST
		RouteTable.Routes.MapHttpRoute(name: "SetFieldOverride", routeTemplate: "Data/{action}", defaults: new { controller = "Data" });
		RouteTable.Routes.MapHttpRoute(name: "AdminRoutes", routeTemplate: "AdminCtl/{action}", defaults: new { controller = "Admin" });

		//GET
		RouteTable.Routes.MapHttpRoute(name: "UndoAction", routeTemplate: "{action}/{lid}/{ActionId}", defaults: new {
			action = "UndoAction",
			controller = "Data"
		});
		RouteTable.Routes.MapHttpRoute(name: "GetClonableGroups", routeTemplate: "Data/GetClonableGroups/{lid}/{BranchPath}", defaults: new {
			action = "GetClonableGroups",
			controller = "Data"
		});
		RouteTable.Routes.MapHttpRoute(name: "GetAvailableFields", routeTemplate: "{action}/{lid}/{ScreenId}/{BranchPath}", defaults: new {
			symbol = RouteParameter.Optional,
			controller = "Data"
		});
		RouteTable.Routes.MapHttpRoute(name: "SwitchOverrideFieldOrder", routeTemplate: "{action}/{lid}/{ScreenId}/{BranchPath}/{SourceFieldId}/{DestinationFieldId}", defaults: new {
			symbol = RouteParameter.Optional,
			controller = "Data"
		});
		RouteTable.Routes.MapHttpRoute(name: "GetClonableTargets", routeTemplate: "{action}/{lid}/{BranchPath}", defaults: new {
			symbol = RouteParameter.Optional,
			controller = "Data"
		});
		RouteTable.Routes.MapHttpRoute(name: "SetScreenDefaults", routeTemplate: "{action}/{lid}/{ScreenId}/{BranchPath}", defaults: new {
			symbol = RouteParameter.Optional,
			controller = "Data"
		});
		RouteTable.Routes.MapHttpRoute(name: "GetSystemMaintenanceUpdate", routeTemplate: "{action}", defaults: new {
			symbol = RouteParameter.Optional,
			controller = "Data"
		});
		RouteTable.Routes.MapHttpRoute(name: "GetAvailableUndos", routeTemplate: "{action}/{lid}", defaults: new {
			symbol = RouteParameter.Optional,
			controller = "Data"
		});
		RouteTable.Routes.MapHttpRoute(name: "GetAuditTree", routeTemplate: "GetAuditTree/{lid}/{ParentId}", defaults: new {
			symbol = RouteParameter.Optional,
			controller = "Admin"
		});
		RouteTable.Routes.MapHttpRoute(name: "CreateUniqueVersion", routeTemplate: "{action}/{lid}/{ScreenId}/{BranchPath}/{screenTitle}", defaults: new {
			symbol = RouteParameter.Optional,
			controller = "Data"
		});

	}

	private void Session_Start(object sender, EventArgs e)
	{
		// Fires when the session is started

	}

	private void Application_BeginRequest(object sender, EventArgs e)
	{
		// Fires at the beginning of each request
		//Try
		//    Dim lid As UInt64 = 0

		//    '    Dim ru As String = Request.RawUrl

		//    If Request("lid") <> "" AndAlso UInt64.TryParse(Request("lid"), lid) AndAlso lid <> 0 Then
		//        Dim elid As UInt64 = 0

		//        If Not iq.SeshAlive(lid) Or _
		//            Not UInt64.TryParse(Request("lid"), lid) Or _
		//            (Request("elid") <> "" _
		//             AndAlso Not UInt64.TryParse(Request("elid"), elid)) Then

		//            Response.Redirect("signin.aspx") : Exit Sub 'A safety catch to stop people crashing the whole OM with an invalid URL

		//        End If

		//        If iq.seshDic(lid).ContainsKey("ElevatedKey") AndAlso elid = CType(iq.sesh(lid, "ElevatedKey"), UInt64) Then
		//            If Not iq.seshDic(lid).ContainsKey("Elevated") Then iq.seshDic(lid).Add("Elevated", Request("elid")) Else iq.seshDic(lid)("Elevated") = Request("elid")
		//        Else
		//            If iq.seshDic.ContainsKey(lid) Then iq.seshDic(lid).Remove("Elevated")
		//        End If

		//    End If
		//Catch ex As HttpRequestValidationException
		//    'Hmm just ignore it? not sure we should.
		//    Response.Write("Unfortunately an error occurred, please try again")
		//    Response.End()
		//    'Beep()
		//End Try
	}

	private void Application_AuthenticateRequest(object sender, EventArgs e)
	{
		// Fires upon attempting to authenticate the use
	}


	private void Application_Error(object sender, EventArgs e)
	{
		//'fucking me over .. AGAIN... with silent crashing, Broken DB conections etc.
		// '' Fires when an error occurs
		Exception ex = Server.GetLastError != null && Server.GetLastError.InnerException != null ? Server.GetLastError.InnerException : null;
		if (ex != null) {
			ErrorLog.Add(string.Format("{0}\\r\\n{1}\\r\\n{2}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : string.Empty, ex.StackTrace));
			try {
				dataAccess.da.DBExecutesql(string.Format("INSERT INTO ErrorLog (DateTime,Message,StackTrace,InnerException) VALUES ({0},{1},{2},{3})", dataAccess.da.UniversalDate(DateTime.Now), dataAccess.da.SqlEncode(ex.Message), dataAccess.da.SqlEncode(ex.StackTrace), dataAccess.da.SqlEncode(ex.InnerException != null ? ex.InnerException.Message : string.Empty)));
			} catch (Exception exe) {
				//Dont crash, thats what we are trying to avoid!!
			}
		}


		//'  Dim tr As System.IO.TextReader = New System.IO.StreamReader(Response.OutputStream.)
		//Context.Server.ClearError()
		//Response.StatusCode = 200
		// '' If Not clsIQ.IsLoaded Then Stop
	}

	private void Session_End(object sender, EventArgs e)
	{
		// Fires when the session ends

	}

	private void Application_End(object sender, EventArgs e)
	{
		// Fires when the application ends

		//Unload the object model - removed for now as it seems to do it randomly when you pause execution in the IDE
		//    Application("IQ") = Nothing  'release the reference - bad idea as application end appears to fire whenever there is a 500 erroor
		//    iq = Nothing

		SaveUserStates();

	}

}

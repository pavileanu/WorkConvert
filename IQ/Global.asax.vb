Imports System.Web.SessionState
Imports System.Web.Routing
Imports System.Web.Http

Public Class Global_asax
    Inherits System.Web.HttpApplication

    Private Shared ErrorLog As LoggingList(Of String) = New LoggingList(Of String)()

    Sub Application_Start(ByVal sender As Object, ByVal e As EventArgs)
        ' Fires when the application is started

        'POST
        RouteTable.Routes.MapHttpRoute(name:="SetFieldOverride", routeTemplate:="Data/{action}", defaults:=New With {.controller = "Data"})
        RouteTable.Routes.MapHttpRoute(name:="AdminRoutes", routeTemplate:="AdminCtl/{action}", defaults:=New With {.controller = "Admin"})

        'GET
        RouteTable.Routes.MapHttpRoute(name:="UndoAction", routeTemplate:="{action}/{lid}/{ActionId}", defaults:=New With {.action = "UndoAction", .controller = "Data"})
        RouteTable.Routes.MapHttpRoute(name:="GetClonableGroups", routeTemplate:="Data/GetClonableGroups/{lid}/{BranchPath}", defaults:=New With {.action = "GetClonableGroups", .controller = "Data"})
        RouteTable.Routes.MapHttpRoute(name:="GetAvailableFields", routeTemplate:="{action}/{lid}/{ScreenId}/{BranchPath}", defaults:=New With {.symbol = RouteParameter.Optional, .controller = "Data"})
        RouteTable.Routes.MapHttpRoute(name:="SwitchOverrideFieldOrder", routeTemplate:="{action}/{lid}/{ScreenId}/{BranchPath}/{SourceFieldId}/{DestinationFieldId}", defaults:=New With {.symbol = RouteParameter.Optional, .controller = "Data"})
        RouteTable.Routes.MapHttpRoute(name:="GetClonableTargets", routeTemplate:="{action}/{lid}/{BranchPath}", defaults:=New With {.symbol = RouteParameter.Optional, .controller = "Data"})
        RouteTable.Routes.MapHttpRoute(name:="SetScreenDefaults", routeTemplate:="{action}/{lid}/{ScreenId}/{BranchPath}", defaults:=New With {.symbol = RouteParameter.Optional, .controller = "Data"})
        RouteTable.Routes.MapHttpRoute(name:="GetSystemMaintenanceUpdate", routeTemplate:="{action}", defaults:=New With {.symbol = RouteParameter.Optional, .controller = "Data"})
        RouteTable.Routes.MapHttpRoute(name:="GetAvailableUndos", routeTemplate:="{action}/{lid}", defaults:=New With {.symbol = RouteParameter.Optional, .controller = "Data"})
        RouteTable.Routes.MapHttpRoute(name:="GetAuditTree", routeTemplate:="GetAuditTree/{lid}/{ParentId}", defaults:=New With {.symbol = RouteParameter.Optional, .controller = "Admin"})
        RouteTable.Routes.MapHttpRoute(name:="CreateUniqueVersion", routeTemplate:="{action}/{lid}/{ScreenId}/{BranchPath}/{screenTitle}", defaults:=New With {.symbol = RouteParameter.Optional, .controller = "Data"})

    End Sub

    Sub Session_Start(ByVal sender As Object, ByVal e As EventArgs)
        ' Fires when the session is started

    End Sub

    Sub Application_BeginRequest(ByVal sender As Object, ByVal e As EventArgs)
        ' Fires at the beginning of each request
        'Try
        '    Dim lid As UInt64 = 0

        '    '    Dim ru As String = Request.RawUrl

        '    If Request("lid") <> "" AndAlso UInt64.TryParse(Request("lid"), lid) AndAlso lid <> 0 Then
        '        Dim elid As UInt64 = 0

        '        If Not iq.SeshAlive(lid) Or _
        '            Not UInt64.TryParse(Request("lid"), lid) Or _
        '            (Request("elid") <> "" _
        '             AndAlso Not UInt64.TryParse(Request("elid"), elid)) Then

        '            Response.Redirect("signin.aspx") : Exit Sub 'A safety catch to stop people crashing the whole OM with an invalid URL

        '        End If

        '        If iq.seshDic(lid).ContainsKey("ElevatedKey") AndAlso elid = CType(iq.sesh(lid, "ElevatedKey"), UInt64) Then
        '            If Not iq.seshDic(lid).ContainsKey("Elevated") Then iq.seshDic(lid).Add("Elevated", Request("elid")) Else iq.seshDic(lid)("Elevated") = Request("elid")
        '        Else
        '            If iq.seshDic.ContainsKey(lid) Then iq.seshDic(lid).Remove("Elevated")
        '        End If

        '    End If
        'Catch ex As HttpRequestValidationException
        '    'Hmm just ignore it? not sure we should.
        '    Response.Write("Unfortunately an error occurred, please try again")
        '    Response.End()
        '    'Beep()
        'End Try
    End Sub

    Sub Application_AuthenticateRequest(ByVal sender As Object, ByVal e As EventArgs)
        ' Fires upon attempting to authenticate the use
    End Sub

    Sub Application_Error(ByVal sender As Object, ByVal e As EventArgs)

        ''fucking me over .. AGAIN... with silent crashing, Broken DB conections etc.
        ' '' Fires when an error occurs
        Dim ex As Exception = If(Server.GetLastError IsNot Nothing AndAlso Server.GetLastError.InnerException IsNot Nothing, Server.GetLastError.InnerException, Nothing)
        If ex IsNot Nothing Then
            ErrorLog.Add(String.Format("{0}\r\n{1}\r\n{2}", ex.Message, If(ex.InnerException IsNot Nothing, ex.InnerException.Message, String.Empty), ex.StackTrace))
            Try
                dataAccess.da.DBExecutesql(String.Format("INSERT INTO ErrorLog (DateTime,Message,StackTrace,InnerException) VALUES ({0},{1},{2},{3})", dataAccess.da.UniversalDate(DateTime.Now), dataAccess.da.SqlEncode(ex.Message), dataAccess.da.SqlEncode(ex.StackTrace), dataAccess.da.SqlEncode(If(ex.InnerException IsNot Nothing, ex.InnerException.Message, String.Empty))))
            Catch exe As Exception
                'Dont crash, thats what we are trying to avoid!!
            End Try
        End If


        ''  Dim tr As System.IO.TextReader = New System.IO.StreamReader(Response.OutputStream.)
        'Context.Server.ClearError()
        'Response.StatusCode = 200
        ' '' If Not clsIQ.IsLoaded Then Stop
    End Sub

    Sub Session_End(ByVal sender As Object, ByVal e As EventArgs)
        ' Fires when the session ends

    End Sub

    Sub Application_End(ByVal sender As Object, ByVal e As EventArgs)
        ' Fires when the application ends

        'Unload the object model - removed for now as it seems to do it randomly when you pause execution in the IDE
        '    Application("IQ") = Nothing  'release the reference - bad idea as application end appears to fire whenever there is a 500 erroor
        '    iq = Nothing

        SaveUserStates()

    End Sub

End Class
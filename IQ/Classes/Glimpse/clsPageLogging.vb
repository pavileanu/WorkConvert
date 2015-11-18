Public Class clsPageLogging
    Inherits Page

    Friend errorMessages As List(Of String) = New List(Of String)()

    Dim sw As Stopwatch = New Stopwatch()

    Public Overloads Sub Page_Init(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Init

        sw.Start()
        'MyBase.OnInit(e)
    End Sub

    Protected Overrides Sub OnLoadComplete(e As EventArgs)
        sw.Stop()

        Dim lid As UInt64 = 0
        Try
            If Context.Request IsNot Nothing AndAlso Context.Request.QueryString.Count > 0 Then lid = Context.Request.QueryString("lid")

            AuditLog.Instance.Add(
            lid,
            "PageLoad",
            "",
            "",
            errorMessages,
            Nothing,
            Me.Context.Request.Path,
            Me.Context.Request.RawUrl,
            sw.ElapsedMilliseconds,
            Me.Context.Request.HttpMethod,
            If(Me.Context.Request.UrlReferrer IsNot Nothing, Me.Context.Request.UrlReferrer.OriginalString, String.Empty))
        Catch ex As Exception
            'Oops, dont crash
        End Try

        MyBase.OnLoadComplete(e)
    End Sub


End Class

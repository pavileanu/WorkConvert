Public Class DocStoc
    Inherits System.Web.UI.Page

    Public FrameHeight As String
    Public FrameWidth As String
    Public DocVal As String
    Public PdfObject As String
    Public IFrameStyle As String
    Public IFrameSrc As String
    Public IFrameObject As String
    Public ImageObject As String
    Public ImageUrl As String

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        ' Displays the requested resource file in an embedded viewer

        Dim type As String = Nothing
        Dim ref As String = Nothing
        Dim viewMode As String = Nothing

        If Request.QueryString("type") Is Nothing OrElse Request.QueryString("ref") Is Nothing Then Exit Sub
        type = Request.QueryString("type").ToLower()
        ref = Request.QueryString("ref")

        PdfObject = "none"
        IFrameObject = "none"
        ImageObject = "none"

        FrameHeight = "600"
        FrameWidth = "709"

        Select Case type

            Case "youtube"

                IFrameObject = "block"
                FrameHeight = "35em"
                FrameWidth = "62.22em"
                IFrameStyle = String.Format("height:{0};width:{1};border:0;", FrameHeight, FrameWidth)
                IFrameSrc = String.Format("https://www.youtube.com/embed/{0}?autoplay=1"" frameborder=""0"" allowfullscreen=""allowfullscreen", ref)

            Case "pdf", "pps"

                PdfObject = "block"
                DocVal = String.Format("doc_id={0}&mem_id=18136568&doc_type={1}&fullscreen=0&showrelated=0&showotherdocs=0&showstats=0", ref, type)

            Case "sheet", "spreadsheet"

                IFrameObject = "block"
                FrameHeight = "50em"
                FrameWidth = "85em"
                viewMode = "lv"     ' Alternatives are "ccc" and "pub"
                If InStr(Request.ServerVariables("HTTP_USER_AGENT"), "MSIE") > 0 Then viewMode = "pub"
                IFrameStyle = String.Format("height: {0}; width: {1}; border:0;", FrameHeight, FrameWidth)
                IFrameSrc = String.Format("https://docs.google.com/spreadsheet/{0}?key={1}&hl=en&output=html&widget=true&rm=minimal&skipauth=true", viewMode, ref)

        End Select
    End Sub


End Class
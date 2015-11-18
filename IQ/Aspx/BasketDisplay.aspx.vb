Imports System.IO
Imports System.Xml

Public Class BasketDisplay
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load



        Dim config As String = Request.Form("configuration")
        Dim sid As String = Request.Form("LONGSID")
        Dim acountNum As String = Request.Form("cAccountNum")
        Dim content As String = Request.Form("CONTENT")
        Response.Write("<b>Default Basket page if the Basket page has not been configured in Gatekeeper</b>")
        Response.Write("configuration" & vbNewLine)
        Response.Write("<XMP>" & config & "</XMP>")
        Response.Write("cAccountNum : " & acountNum)
        Response.Write(vbNewLine)
        Response.Write("LONGSID : " & sid)
        Response.Write(vbNewLine)
        Response.Write("CONTENT : " & content)


        For Each sItem In Request.Form
            Response.Write(sItem)
            Response.Write(" - [" & Request.Form(sItem) & "]" & vbNewLine)
        Next

    End Sub

    Private Function formatXML(xmlstring As String) As String
        Dim sw As New StringWriter()
        Dim xw As New XmlTextWriter(sw)
        xw.Formatting = Formatting.Indented
        xw.Indentation = 4
        Dim doc As New XmlDocument
        doc.LoadXml(xmlstring)
        doc.Save(xw)
        Return sw.ToString()
    End Function
End Class
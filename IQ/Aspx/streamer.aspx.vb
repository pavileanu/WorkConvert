
Imports System.Net.Mail

Public Class streamer

    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'iq.sesh(lid,"tostream") = fullpath$
        'Response.Redirect("streamer.aspx")

        Dim lid As UInt64 = Request.QueryString("lid")
        Dim systemSKU As String = iq.sesh(lid, "systemSKU")
        Dim boolToolsCSVExport As Boolean = iq.sesh(lid, "toolsCSVExport")

        Dim fn$
        fn$ = iq.sesh(lid, "tostream")

        If Not My.Computer.FileSystem.FileExists(fn$) Then
            Response.Write("We're sorry - there seemed to be a problem generating that file" & fn$ & " , The support team have been notified and will fix it shortly.")

            Dim smtpclient As New System.Net.Mail.SmtpClient

            Dim msg As MailMessage
            msg = New MailMessage(iq.Addresses("iQuoteSupportEmail").Translation.text(English), "nick.axworthy@channelcentral.net", "It's all gone horribly wrong (well a small bit has anyway)", "The PDF Generator (PDFgen) failed to convert " & fn$ & " in a timely manner.. Please check it is running on the server desktop.")
            msg.ReplyToList.Add(New MailAddress("dan.mason@channelcentral.net"))
            msg.IsBodyHtml = True
            msg.Priority = MailPriority.High
            smtpclient.ServicePoint.MaxIdleTime = 1
            smtpclient.Send(msg)

        Else
            Response.Clear()
            Dim FileName As String = ""
            If (boolToolsCSVExport) Then
                Dim RandomNumber As Integer
                RandomNumber = New Random().Next(999999)
                Dim RandomNumberStr = RandomNumber.ToString("000000")
                FileName = systemSKU & "_" & RandomNumberStr & Right(fn, 4)
                iq.sesh(lid, "toolsCSVExport") = False
            Else
                FileName = "Export" & Now.ToString("yyyyMMddHHmmss") & Right(fn, 4)
            End If

            Response.AppendHeader("Content-Disposition", "attachment;filename=" & FileName)
            Response.ContentType = iq.sesh(lid, "streamcontent-type") ' Request("streamcontent-type") "text/xml"
            '"application/vnd.ms-excel;charset=UTF-8"

            Dim fi As System.IO.FileInfo
            fi = My.Computer.FileSystem.GetFileInfo(fn$)

            Response.AddHeader("Content-Length", fi.Length.ToString())

            Response.Flush()
            Response.WriteFile(fn$)


            Response.End()

            If iq.sesh(lid, "DeleteStreamed") = True Then
                Try
                    My.Computer.FileSystem.DeleteFile(fn$)
                Catch

                End Try
            End If
        End If

    End Sub

End Class
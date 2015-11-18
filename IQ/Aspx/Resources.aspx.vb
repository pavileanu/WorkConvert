Imports System.Net
Imports System.IO

Public Class Resources
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim lid As UInt64
        Dim agentAccount As clsAccount
        Dim menuHtml As New StringBuilder

        If iq.seshDic Is Nothing Then Exit Sub
        If Request.QueryString("lid") Is Nothing Then Exit Sub
        lid = CType(Request.QueryString("lid"), UInt64)

        If Not iq.seshDic(lid).ContainsKey("AgentAccount") Then Exit Sub
        agentAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

        If iq.ResourceCategories Is Nothing OrElse iq.ResourceCategories.Count = 0 Then Exit Sub

        ' Display which side of the split we're on
        titleLabel.Text = Xlt("iQuote Resources", agentAccount.Language)

        For Each resourceCategory In iq.ResourceCategories.Values.OrderBy(Function(rc) (rc.Order))

            ' Look for any resource files in this category the user has access to
            Dim visibleResourceFiles = New List(Of clsResource)

            If Not resourceCategory.Resources Is Nothing Then
                For Each resourceFile In resourceCategory.Resources.OrderBy(Function(r) (r.Order))

                    Dim show As Boolean = True

                    ' Optionally filter by region
                    If Not resourceFile.Region Is Nothing AndAlso Not resourceFile.Region.Encompasses(agentAccount.SellerChannel.Region) Then
                        show = False
                    End If

                    ' Optionally filter by language
                    If show AndAlso Not resourceFile.Language Is Nothing AndAlso resourceFile.Language.Code <> agentAccount.Language.Code Then
                        show = False
                    End If

                    ' Optionally filter by MFR
                    If show AndAlso resourceFile.Manufacturer <> Manufacturer.Unknown AndAlso resourceFile.Manufacturer <> agentAccount.Manufacturer Then
                        show = False
                    End If

                    ' Optionally filter by Host
                    If show AndAlso Not resourceFile.SellerChannel Is Nothing AndAlso resourceFile.SellerChannel.ID <> agentAccount.SellerChannel.ID Then
                        show = False
                    End If

                    If show Then
                        visibleResourceFiles.Add(resourceFile)
                    End If

                Next
            End If

            If visibleResourceFiles.Count > 0 Then

                menuHtml.Append(String.Format("<h2>{0}</h2>", resourceCategory.Translation.text(agentAccount.Language)))
                menuHtml.Append("<ul style=""margin-left:0; padding-left:0;"">")

                For Each resourceFile As clsResource In visibleResourceFiles
                    If resourceFile.Embed Then
                        menuHtml.Append(String.Format("<li><span class=""link"" onclick=""displayData('DocStoc.aspx?type={0}&ref={1}');return false;""> {2}</span></li>", resourceFile.Type.ToLower(), resourceFile.Code, resourceFile.Title.text(agentAccount.Language)))
                    Else
                        Select Case resourceFile.Type.ToLower()

                            Case "youtube"
                                menuHtml.Append(String.Format("<li><a class='link' target='_blank' href='https://www.youtube.com/watch?v={0}'>{1}</a></li>", resourceFile.Code, resourceFile.Title.text(agentAccount.Language)))

                            Case "pdf", "pps"
                                menuHtml.Append(String.Format("<li><a class='link' target='_blank' href='http://www.docstoc.com/docs/{0}'>{1}</a></li>", resourceFile.Code, resourceFile.Title.text(agentAccount.Language)))

                            Case Else
                                ' Failover to embedded mode if no external URL has been set up for this resource type
                                menuHtml.Append(String.Format("<li><span class=""link"" onclick=""displayData('DocStoc.aspx?type={0}&ref={1}');return false;""> {2}</span></li>", resourceFile.Type.ToLower(), resourceFile.Code, resourceFile.Title.text(agentAccount.Language)))

                        End Select

                    End If
                Next

                menuHtml.Append("</ul>")

            End If
        Next

        litMenu.Text = menuHtml.ToString()

    End Sub

End Class
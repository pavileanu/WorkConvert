Public Class margins
    Inherits clsPageLogging

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim lid As UInt64 = CULng(Request.QueryString("lid"))

        'Looks like some kind of standalone page to display a table of margins

        Dim tbl As New Table

        Dim thr As TableHeaderRow
        thr = New TableHeaderRow
        tbl.Controls.Add(thr)

        Dim thc As TableHeaderCell
        For Each opt In iq.ProductTypes.Values
            thc = New TableHeaderCell
            thr.Controls.Add(thc)
            Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
            thc.Text = opt.Translation.text(agentAccount.Language)
        Next

        tbl.Controls.Add(thr)

        Dim tr As TableRow

        Stop '- no such thing as a selleraccountid

        Dim SellerAccount As clsAccount = iq.Accounts(iq.sesh(lid, "sellerAccountId"))
        Dim tc As TableCell
        Dim sector As clsSector

        For Each buyeraccount In SellerAccount.SellerChannel.CustomerAccounts.Values
            tr = New TableRow
            For Each productType In iq.ProductTypes.Values
                For Each k In Split("HPISS,HPPSG", ",")
                    sector = iq.i_sector_code(k)
                    tc = New TableCell
                    If SellerAccount.BuyerChannel.Margin(buyeraccount.BuyerChannel).ContainsKey(sector) Then

                        tc.Text = SellerAccount.BuyerChannel.Margin(buyeraccount.BuyerChannel)(sector).Factor.ToString

                    End If
                Next
            Next
        Next

    End Sub

End Class
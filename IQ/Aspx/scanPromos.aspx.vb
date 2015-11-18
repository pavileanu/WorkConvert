Public Class scanPromos
    Inherits clsPageLogging

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

       
        Dim lid As UInt64 = Convert.ToUInt64(Request.QueryString("lid"))
        Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)

        If buyerAccount IsNot Nothing Then ' THIS NEEDS INVESTIGATING -todo - quick fix for security testing
            TagPromoBranches(buyerAccount, errorMessages)
        End If
        Response.Redirect("WaitMessage.aspx?lid=" & CStr(lid), False) '"WaitMessage.aspx?lid=" & lid, False)
        'Response.Redirect("tree.aspx?lid=" & CStr(lid))

    End Sub

End Class
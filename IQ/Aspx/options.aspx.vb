Public Class options
    Inherits clsPageLogging

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load


        If Not IsPostBack Then
            TreeView1.Nodes.Add(iq.Branches(Request("bid")).treenode)  'The treenode method (called on the root 'worldwide' region) - recursively populates the entire tree
            
        End If



    End Sub

End Class
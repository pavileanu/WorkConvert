Public Class Regions
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
       

        If r_worldwide Is Nothing Then r_worldwide = iq.i_region_code("XW")
        Dim geoRegions As IEnumerable(Of clsRegion) = From j In iq.Regions.Values Where j.Code.StartsWith("G-")
        If Not IsPostBack Then
            TreeView1.Nodes.Add(r_worldwide.treeNode)  'The treenode method (called on the root 'worldwide' region) - recursively populates the entire tree
            'If iq.i_region_code.ContainsKey("NEW") Then
            '    Dim regionToDelete As clsRegion = iq.i_region_code("NEW")
            '    regionToDelete.Remove()
            '    iq.i_region_code.Remove("NEW")

            'End If
            drpGeoRegions.Items.Clear()
            Dim defaultListItem As ListItem = New ListItem()
            defaultListItem.Text = "None"
            defaultListItem.Value = ""
            defaultListItem.Selected = True
            drpGeoRegions.Items.Add(defaultListItem)
            For Each region As clsRegion In geoRegions
                Dim listItem As ListItem = New ListItem()
                listItem.Text = region.Name.text(English)
                listItem.Value = region.ID
                drpGeoRegions.Items.Add(listItem)
            Next
        End If




    End Sub

    Protected Sub TreeView1_SelectedNodeChanged(sender As Object, e As EventArgs) Handles TreeView1.SelectedNodeChanged

        Dim cn As TreeNode = sender.selectednode

        TxtID.Text = cn.Value
        TxtID.Enabled = False
        Dim rgn As clsRegion = iq.Regions(cn.Value)
        TxtName.Text = rgn.Name.text(English)
        TxtCode.Text = rgn.Code
        txtNotes.Text = rgn.Notes

        If rgn.Parent IsNot Nothing Then
            TxtParent.Text = rgn.Parent.Code
        Else
            TxtParent.Text = ""
        End If
        ChkIscountry.Checked = rgn.isCountry
        chkIsPlaceHolder.Checked = rgn.isCountry
        If rgn.geoRegion > 0 Then
            drpGeoRegions.SelectedValue = rgn.geoRegion
        Else
            drpGeoRegions.SelectedValue = ""
        End If

    End Sub

    Protected Sub BtnSave_Click(sender As Object, e As EventArgs) Handles BtnSave.Click

        Dim rgn As clsRegion = iq.Regions(CInt(TxtID.Text))

        Dim selectedNode As TreeNode = flatten(TreeView1.Nodes(0))(rgn.ID)

        Dim PRC As String = "" ' parent region code

        If rgn.Parent IsNot Nothing Then PRC = rgn.Parent.Code
        If TxtParent.Text <> PRC Then
            If iq.i_region_code.ContainsKey(TxtParent.Text) Then
                rgn.Parent.Children.Remove(rgn.ID)
                rgn.Parent = iq.i_region_code(TxtParent.Text)
                rgn.Parent.Children.Add(rgn.ID, rgn)
                'Dim selectedNode As TreeNode = flatten(TreeView1.Nodes(0))(rgn.ID)
                selectedNode.Parent.ChildNodes.Remove(selectedNode)

                Dim npr As clsRegion = iq.i_region_code(TxtParent.Text)
                Dim newParentNode As TreeNode = flatten(TreeView1.Nodes(0))(npr.ID)
                newParentNode.ChildNodes.Add(rgn.treeNode)
            Else
                Page.Controls.Add(ErrorDymo(TxtParent.Text & " is not a valid region code"))
            End If
        End If


        If TxtCode.Text <> rgn.Code Then
            If iq.i_region_code.ContainsKey(TxtCode.Text) Then
                Panel3.Controls.Add(ErrorDymo("The region code " & TxtCode.Text & " is already in use - cannot save"))
            Else
                rgn.Code = TxtCode.Text
                rgn.Name.text(English) = TxtName.Text
                rgn.Name.Update(English)
                rgn.isCountry = ChkIscountry.Checked
                rgn.isPlaceholder = chkIsPlaceHolder.Checked
                rgn.Notes = txtNotes.Text

                rgn.Update()
                selectedNode.Text = rgn.Displayname(English)
            End If
        Else
            rgn.Name.text(English) = TxtName.Text
            rgn.Name.Update(English)
            rgn.isCountry = ChkIscountry.Checked
            rgn.isPlaceholder = chkIsPlaceHolder.Checked
            rgn.Notes = txtNotes.Text
            If drpGeoRegions.SelectedValue <> "" Then
                rgn.geoRegion = CInt(drpGeoRegions.SelectedValue)
            End If
            rgn.Update()
        End If


        


        'TreeView1.Nodes.Clear()
        'TreeView1.Nodes.Add(r_worldwide.treeNode)

    End Sub

    Public Shared Function flatten(node As TreeNode) As Dictionary(Of Integer, TreeNode)

        flatten = New Dictionary(Of Integer, TreeNode)
        flatten.Add(node.Value, node)

        For Each child In node.ChildNodes
            For Each k As Integer In flatten(child).Keys
                flatten.Add(k, flatten(child)(k))
            Next
        Next

    End Function

    Protected Sub BtnAddChild_Click(sender As Object, e As EventArgs) Handles BtnAddChild.Click

        Dim rgn As clsRegion = iq.Regions(CInt(TxtID.Text))

        If iq.i_region_code.ContainsKey("NEW") Then
            Panel3.Controls.Add(ErrorDymo("There is already a region 'NEW' region under construction - Please rename that first"))
        Else

            Dim tl As clsTranslation = iq.AddTranslation("New region", English, "rgns", 0, Nothing, 0, True)
            Dim aregion = New clsRegion(rgn, "NEW", tl, False, iq.i_culture_code("en-gb"), False, "")

            TreeView1.SelectedNode.ChildNodes.Add(aregion.treeNode)
        End If

    End Sub

End Class
public class Regions : System.Web.UI.Page
{

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{


		if (r_worldwide == null)
			r_worldwide = iq.i_region_code("XW");
		IEnumerable<clsRegion> geoRegions = from j in iq.Regions.Valueswhere j.Code.StartsWith("G-");
		if (!IsPostBack) {
			TreeView1.Nodes.Add(r_worldwide.treeNode);
			//The treenode method (called on the root 'worldwide' region) - recursively populates the entire tree
			//If iq.i_region_code.ContainsKey("NEW") Then
			//    Dim regionToDelete As clsRegion = iq.i_region_code("NEW")
			//    regionToDelete.Remove()
			//    iq.i_region_code.Remove("NEW")

			//End If
			drpGeoRegions.Items.Clear();
			ListItem defaultListItem = new ListItem();
			defaultListItem.Text = "None";
			defaultListItem.Value = "";
			defaultListItem.Selected = true;
			drpGeoRegions.Items.Add(defaultListItem);
			foreach (clsRegion region in geoRegions) {
				ListItem listItem = new ListItem();
				listItem.Text = region.Name.text(English);
				listItem.Value = region.ID;
				drpGeoRegions.Items.Add(listItem);
			}
		}




	}


	protected void  // ERROR: Handles clauses are not supported in C#
TreeView1_SelectedNodeChanged(object sender, EventArgs e)
	{
		TreeNode cn = sender.selectednode;

		TxtID.Text = cn.Value;
		TxtID.Enabled = false;
		clsRegion rgn = iq.Regions(cn.Value);
		TxtName.Text = rgn.Name.text(English);
		TxtCode.Text = rgn.Code;
		txtNotes.Text = rgn.Notes;

		if (rgn.Parent != null) {
			TxtParent.Text = rgn.Parent.Code;
		} else {
			TxtParent.Text = "";
		}
		ChkIscountry.Checked = rgn.isCountry;
		chkIsPlaceHolder.Checked = rgn.isCountry;
		if (rgn.geoRegion > 0) {
			drpGeoRegions.SelectedValue = rgn.geoRegion;
		} else {
			drpGeoRegions.SelectedValue = "";
		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnSave_Click(object sender, EventArgs e)
	{
		clsRegion rgn = iq.Regions((int)TxtID.Text);

		TreeNode selectedNode = flatten(TreeView1.Nodes(0))(rgn.ID);

		string PRC = "";
		// parent region code

		if (rgn.Parent != null)
			PRC = rgn.Parent.Code;
		if (TxtParent.Text != PRC) {
			if (iq.i_region_code.ContainsKey(TxtParent.Text)) {
				rgn.Parent.Children.Remove(rgn.ID);
				rgn.Parent = iq.i_region_code(TxtParent.Text);
				rgn.Parent.Children.Add(rgn.ID, rgn);
				//Dim selectedNode As TreeNode = flatten(TreeView1.Nodes(0))(rgn.ID)
				selectedNode.Parent.ChildNodes.Remove(selectedNode);

				clsRegion npr = iq.i_region_code(TxtParent.Text);
				TreeNode newParentNode = flatten(TreeView1.Nodes(0))(npr.ID);
				newParentNode.ChildNodes.Add(rgn.treeNode);
			} else {
				Page.Controls.Add(ErrorDymo(TxtParent.Text + " is not a valid region code"));
			}
		}


		if (TxtCode.Text != rgn.Code) {
			if (iq.i_region_code.ContainsKey(TxtCode.Text)) {
				Panel3.Controls.Add(ErrorDymo("The region code " + TxtCode.Text + " is already in use - cannot save"));
			} else {
				rgn.Code = TxtCode.Text;
				rgn.Name.text(English) = TxtName.Text;
				rgn.Name.Update(English);
				rgn.isCountry = ChkIscountry.Checked;
				rgn.isPlaceholder = chkIsPlaceHolder.Checked;
				rgn.Notes = txtNotes.Text;

				rgn.Update();
				selectedNode.Text = rgn.Displayname(English);
			}
		} else {
			rgn.Name.text(English) = TxtName.Text;
			rgn.Name.Update(English);
			rgn.isCountry = ChkIscountry.Checked;
			rgn.isPlaceholder = chkIsPlaceHolder.Checked;
			rgn.Notes = txtNotes.Text;
			if (drpGeoRegions.SelectedValue != "") {
				rgn.geoRegion = (int)drpGeoRegions.SelectedValue;
			}
			rgn.Update();
		}





		//TreeView1.Nodes.Clear()
		//TreeView1.Nodes.Add(r_worldwide.treeNode)

	}

	public static Dictionary<int, TreeNode> flatten(TreeNode node)
	{

		flatten = new Dictionary<int, TreeNode>();
		flatten.Add(node.Value, node);

		foreach ( child in node.ChildNodes) {
			foreach (int k in flatten(child).Keys) {
				flatten.Add(k, flatten(child)(k));
			}
		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnAddChild_Click(object sender, EventArgs e)
	{
		clsRegion rgn = iq.Regions((int)TxtID.Text);

		if (iq.i_region_code.ContainsKey("NEW")) {
			Panel3.Controls.Add(ErrorDymo("There is already a region 'NEW' region under construction - Please rename that first"));

		} else {
			clsTranslation tl = iq.AddTranslation("New region", English, "rgns", 0, null, 0, true);
			object aregion = new clsRegion(rgn, "NEW", tl, false, iq.i_culture_code("en-gb"), false, "");

			TreeView1.SelectedNode.ChildNodes.Add(aregion.treeNode);
		}

	}

}

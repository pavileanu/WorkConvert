public class options : clsPageLogging
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{

		if (!IsPostBack) {
			TreeView1.Nodes.Add(iq.Branches(Request("bid")).treenode);
			//The treenode method (called on the root 'worldwide' region) - recursively populates the entire tree

		}



	}

}

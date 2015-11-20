public class editChannel : System.Web.UI.Page
{

	private int chanId;
	private TextBox txtChannelName;
		//Accessible throught the ASPX - assigned during page load 
	private clsChannel channel;


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		chanId = Request.QueryString("chanID");
		channel = iq.Channels(chanId);

		Label lblChannelName = new Label();
		form1.Controls.Add(lblChannelName);
		txtChannelName = new TextBox();

		Label lblUsers = new Label();
		lblUsers.Text = "Users";
		form1.Controls.Add(lblUsers);

		if (!IsPostBack) {
			//first show.. populate from the info we have in the OM
			txtChannelName.Text = channel.Name;

		} else {
			//postbacks (saves) 
			//the controls will be filled with the values submitted - then (after the page has loaded) things like the btnSave_click will fire - which is where we commit changes
		}

		Button btnsave = new Button();
		btnsave.Text = "Save";
		form1.Controls.Add(btnsave);
		btnsave.Click += SaveChanges;

	}


	public void saveChanges(Button senders, System.EventArgs e)
	{
		channel.Name = txtChannelName.Text;
		//sets it in the object model

		channel.Update(errormessages);
		//commits it back to the database

	}

}

public class QuoteSummary : clsPageLogging
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		int qid;
		qid = Request("QuoteID");

		uint64 lid = Request.QueryString("lid");
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");

		if (qid == 0) {
			Response.Write("No QuoteID parameter specified");

		} else {
			clsQuote Quote;
			if (UserIsAdmin(lid)) {
				Quote = iq.Quotes(qid);
			} else {
				Quote = agentAccount.Quotes(qid);
			}

			//Only load if no quote item has been loaded
			if (Quote.RootItem.Children.Count == 0) {
				Quote.LoadItems(errorMessages);
				//IMPORTANT !
			}
			bool priceChanges = false;
			form1.Controls.Add(Quote.HtmlSummary(Quote.AgentAccount.Language, false, lid, priceChanges, errorMessages));
			//adds an HTML table summarising the quote

			Button btnload;
			btnload = new Button();

			btnload.OnClientClick = "redirect('listquotes.aspx?quoteid=" + qid + "&lid=" + lid + "');return false;";

			btnload.CssClass = "hpbluebutton";

			Button btnNextVersion = new Button();
			btnNextVersion.Text = Xlt("Create next version", agentAccount.Language);
			btnNextVersion.ToolTip = Xlt("Creates a copy leaving the original quote intact", agentAccount.Language);
			//the createnextVersion will set the iq.sesh(lid,"QuoteID") so we can just redirect to quote.aspx
			btnNextVersion.OnClientClick = " $('#" + btnNextVersion.ClientID + "').hide();rExec('Manipulation.aspx?command=createNextVersion&quoteId=" + Quote.ID + "', gotoTree);return false;";

			Button btnCopy = new Button();
			btnCopy.Text = Xlt("Copy", agentAccount.Language);
			btnCopy.ToolTip = Xlt("Create a new quote using this template", agentAccount.Language);
			//the createnextVersion will set the iq.sesh(lid,"QuoteID") so we can just redirect to quote.aspx
			btnCopy.OnClientClick = "showCopy('manipulation.aspx?command=CopyQuote&QID=" + Quote.ID + "');return false;";


			//Price Change PlaceHolder - this is displayed until and unless you update the quote
			//because the page is never (well, rarely) posted back the performance'cost' is acceptable
			// Dim PCP As PlaceHolder = Quote.HtmlSummary(Quote.AgentAccount.Language, True, lid, priceChanges, errorMessages)
			if (priceChanges == false & Quote.Locked == false) {
				//Prices are unchanged .. procede with quote as normal
				btnload.Text = Xlt("Edit", Quote.BuyerAccount.Language);
				btnload.ToolTip = Xlt("Edits the original quote", Quote.BuyerAccount.Language);
			} else {
				btnload.Text = Xlt("View", Quote.BuyerAccount.Language);
				btnload.ToolTip = Xlt("View the original quote", Quote.BuyerAccount.Language);
			}
			form1.Controls.Add(btnload);

			if (!Quote.Saved) {
				btnNextVersion.Visible = false;
			}
			if (Quote.Locked & Quote.Saved & priceChanges) {
				btnload.Visible = false;
				btnCopy.Visible = false;
				Literal lit = new Literal();
				lit.Text = "<div > Price for the saved quote has changed. Please create a new version. </div>";
				form1.Controls.Add(lit);
			}

			form1.Controls.Add(btnCopy);
			form1.Controls.Add(btnNextVersion);

			if (!object.ReferenceEquals(Quote.AgentAccount.SellerChannel, agentAccount.SellerChannel)) {
				form1.Controls.Add(NewLit("<span color='red'>Warning, loading this quote will change your account to " + Quote.AgentAccount.SellerChannel.Code + "</span>"));
			}

		}

		OutputErrors(form1.Controls, errorMessages, lid);


	}



}

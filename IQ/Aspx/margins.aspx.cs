public class margins : clsPageLogging
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		uint64 lid = Request.QueryString("lid");

		//Looks like some kind of standalone page to display a table of margins

		Table tbl = new Table();

		TableHeaderRow thr;
		thr = new TableHeaderRow();
		tbl.Controls.Add(thr);

		TableHeaderCell thc;
		foreach ( opt in iq.ProductTypes.Values) {
			thc = new TableHeaderCell();
			thr.Controls.Add(thc);
			clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
			thc.Text = opt.Translation.text(agentAccount.Language);
		}

		tbl.Controls.Add(thr);

		TableRow tr;

		System.Diagnostics.Debugger.Break();
		//- no such thing as a selleraccountid

		clsAccount SellerAccount = iq.Accounts(iq.sesh(lid, "sellerAccountId"));
		TableCell tc;
		clsSector sector;

		foreach ( buyeraccount in SellerAccount.SellerChannel.CustomerAccounts.Values) {
			tr = new TableRow();
			foreach ( productType in iq.ProductTypes.Values) {
				foreach ( k in Split("HPISS,HPPSG", ",")) {
					sector = iq.i_sector_code(k);
					tc = new TableCell();

					if (SellerAccount.BuyerChannel.Margin(buyeraccount.BuyerChannel).ContainsKey(sector)) {
						tc.Text = SellerAccount.BuyerChannel.Margin(buyeraccount.BuyerChannel)(sector).Factor.ToString;

					}
				}
			}
		}

	}

}

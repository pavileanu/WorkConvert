public class QuotesTable : clsPageLogging
{

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		UInt64 lid;
		bool searchFailed = false;
		clsAccount agentAccount = null;
		try {
			lid = Request.QueryString("lid");
			agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
		//System.Web.HttpRequestValidationException
		} catch (Exception ex) {
			searchFailed = true;
		}

		litDraft.Text = Xlt("Draft Quotes", agentAccount.Language);
		litSaved.Text = Xlt("Saved Quotes", agentAccount.Language);

		Panel pnl = new Panel();
		pnl.ID = "quoteFilterPanel";

		Form.Controls.Add(pnl);

		Literal LblFilter = new Literal();
		LblFilter.Text = "<span class =\"quotePanel\" style='vertical-align: middle;' >" + Xlt("Search", agentAccount == null ? English : agentAccount.Language) + "</span>";
		pnl.Controls.Add(LblFilter);

		TextBox txtFilter;
		txtFilter = new TextBox();
		txtFilter.ID = "txtFilter";
		txtFilter.CssClass = "quotePanel";

		if (Request("filter") != null) {
			txtFilter.Text = Server.UrlDecode(Request("filter"));
		}

		if (txtFilter.Text != "")
			pnl.CssClass += " filterActive";
		pnl.Controls.Add(txtFilter);


		//NB - Even after the quotesTable (this ASPX) was Ajax'd into the ListquotesASPX (MasterPage based holder'd ListOfQuotes DIV - the DIV survives (becuase it's innerHTML is set in Blow() )
		Literal applyButton = new Literal();
		object omd = "burstBubble(event); var fv;fv=document.getElementById('txtFilter').value; var savedP='false';   var indexP= $('ul li.ui-state-active').index(); if (indexP == 1) { savedP = 'true';}  showQuotes(fv,savedP);".Replace("'", Chr(34));
		applyButton.Text = "<div class='hpOrangeButton' style='display:inline-block;' onclick='" + omd + "'>" + Xlt("Apply", agentAccount.Language) + "</div>";
		pnl.Controls.Add(applyButton);

		Literal cancelButton = new Literal();
		omd = "var savedP= $('#SavedPanel').attr('aria-expanded');showQuotes('',savedP);".Replace("'", Chr(34));
		cancelButton.Text = "<div class='hpGreyButton' style='display:inline-block' onmousedown='" + omd + "'>" + Xlt("Clear", agentAccount.Language) + "</div>";
		pnl.Controls.Add(cancelButton);



		Table tbl = new Table();
		Table tbl2 = new Table();
		tbl.CssClass = "quotesTable";
		tbl2.CssClass = "quotesTable";
		tbl.ID = "DraftPanel";
		tbl2.ID = "SavedPanel";
		// tbl.BorderWidth = 1
		// tbl2.BorderWidth = 1
		string[] CSS;
		object language = agentAccount == null ? English : agentAccount.Language;
		//Dim H$ = Xlt("ID,Ver,Name,Customer,Supplier,Updated,Status,Value", If(agentAccount Is Nothing, English, agentAccount.Language))
		//Dim H$ = "ID,Ver,Name,Customer,Supplier,Systems,Updated,Status,Value"
		//Dim H$ = "ID,Version,Name,Customer,Supplier,Systems,Options,Updated,Status,Value,!Buttons"
		//CSS = Split(H$, ",")
		//For i = 0 To UBound(CSS)
		// CSS(i) = "quotesList1Col-" & CSS(i)
		// Next

		//creates a set of spans - with classes
		object hr = new TableHeaderRow();
		hr.Cells.Add(new TableHeaderCell { Text = Xlt("", language) });
		hr.Cells.Add(new TableHeaderCell {
			Text = Xlt("ID", language),
			CssClass = "quoteTableHeader",
			ColumnSpan = 1
		});
		hr.Cells.Add(new TableHeaderCell {
			Text = Xlt("Version", language),
			CssClass = "quoteTableHeader"
		});
		hr.Cells.Add(new TableHeaderCell {
			Text = Xlt("Name", language),
			CssClass = "quoteTableHeader",
			ColumnSpan = 2
		});
		hr.Cells.Add(new TableHeaderCell {
			Text = Xlt("Updated", language),
			CssClass = "quoteTableHeader"
		});
		hr.Cells.Add(new TableHeaderCell {
			Text = Xlt("Status", language),
			CssClass = "quoteTableHeader"
		});
		hr.Cells.Add(new TableHeaderCell {
			Text = Xlt("Exports", language),
			CssClass = "quoteTableHeader"
		});
		hr.Cells.Add(new TableHeaderCell {
			Text = Xlt("Value", language),
			CssClass = "quoteTableHeader"
		});
		hr.Cells.Add(new TableHeaderCell { Text = Xlt("", language) });
		hr.Cells.Add(new TableHeaderCell { Text = Xlt("", language) });
		hr.Cells.Add(new TableHeaderCell { Text = Xlt("", language) });

		object hr2 = new TableHeaderRow();
		hr2.Cells.Add(new TableHeaderCell { Text = Xlt("", language) });
		hr2.Cells.Add(new TableHeaderCell {
			Text = Xlt("ID", language),
			CssClass = "quoteTableHeader",
			ColumnSpan = 1
		});
		hr2.Cells.Add(new TableHeaderCell {
			Text = Xlt("Version", language),
			CssClass = "quoteTableHeader"
		});
		hr2.Cells.Add(new TableHeaderCell {
			Text = Xlt("Updated", language),
			CssClass = "quoteTableHeader"
		});
		hr2.Cells.Add(new TableHeaderCell {
			Text = Xlt("Value", language),
			CssClass = "quoteTableHeader"
		});
		hr2.Cells.Add(new TableHeaderCell { Text = Xlt("", language) });
		hr2.Cells.Add(new TableHeaderCell { Text = Xlt("", language) });
		hr2.Cells.Add(new TableHeaderCell { Text = Xlt("", language) });
		//Dim hr2 = New TableHeaderRow()
		//Dim ar1() As TableHeaderCell
		//hr.Cells.CopyTo(ar1, 0)
		//For Each ce In ar1
		// If ce.text <> Xlt("Name", language) Then hr2.Cells.Add(ce)
		// Next


		tbl.Rows.Add(hr2);
		//tbl.Controls.Add(FloatNone)
		tbl2.Rows.Add(hr);
		//tbl2.Controls.Add(FloatNone)
		// DiscardUnChangedQuote(Session) 'not a good plan as they may go 'back' to ontinue to work on the next draft of the qoute - instead we simply don't display unchanged quotes
		Int32 quoteCount = 0;


		if (!searchFailed) {
			//filter by the supplied buyerID
			agentAccount.LoadQuotes(Val(Request("buyerID")));

			//'build a dictionary of root quote IDs to latest versions  (this isn't terribly efficient - but in practise won't be an issue)
			//Dim dicLatest As Dictionary(Of clsQuote, clsQuote) = New Dictionary(Of clsQuote, clsQuote)
			//For Each q In agentAccount.Quotes.Values
			//    If Not dicLatest.ContainsKey(q.RootQuote) Then
			//        dicLatest.Add(q.RootQuote, q)
			//    Else
			//        If q.Version > dicLatest(q.RootQuote).Version Then
			//            dicLatest(q.RootQuote) = q
			//        End If
			//    End If
			//Next


			//uses LINQ to sort data from the object model
			object sortedQuotes = from q in agentAccount.Quotes.Valuesorderby (q.RootQuote.ID + q.Version / 100) descending;
			// quiteBy (q.Created) Descending
			if (UserIsAdmin(Request("lid"))) {
				sortedQuotes = from q in iq.Quotes.Valuesorderby (q.RootQuote.ID + q.Version / 100) descending;
				if (IsNumeric(txtFilter.Text) && sortedQuotes.Where(sq => sq.ID == txtFilter.Text).Count == 0) {
					//Ok so how do we find who this quote belongs to?
					object aa;
					object dt = dataAccess.da.FilledDataTable(dataAccess.da.OpenDatabase(), "SELECT FK_Account_ID_Agent FROM quote where id=" + txtFilter.Text);
					if (dt.Rows.Count > 0 && !IsDBNull(dt.Rows(0)("FK_Account_ID_Agent"))) {
						object aga = iq.Accounts((int)dt.Rows(0)("FK_Account_ID_Agent"));
						aga.LoadQuotes(0);
					}

				}
			}
			object maxVersions = sortedQuotes.GroupBy(qu => qu.RootQuote.ID).ToDictionary(qu => qu.Key, qu => qu.Max(qui => qui.Version));


			clsState state_cancelled = iq.i_state_GroupCode("QT-#CX");
			clsState state_new = iq.i_state_GroupCode("QT-#NW");

			TableRow row;

			bool odd = true;
			//toggles for each line

			List<int> Expanded = iq.SeshValue(lid, "expandedQuotes", null);
			//a list of the root expanded quotes


			clsLanguage lang = agentAccount.Language;
			string txt = txtFilter.Text;
			txt = LCase(txtFilter.Text);
			//make case insensitive

			StringComparer comparer = StringComparer.CurrentCultureIgnoreCase;
			//.quotes.Values
			foreach (clsQuote quote in (from q in sortedQuotesqwhere q.Saved == true)) {
				// Dim adic As New Dictionary(Of String, Object)(StringComparer.CurrentCultureIgnoreCase)

					//Apply any filtering . . .



					//If quote.RootQuote.ID = 32 Then Stop

					//show all versions of 'Expanded' quotes, for non expanded ones we only show the latest revision
					//If quote.RootQuoteID = 11051 Then Stop
					//  If Not (quote.State Is state_new And quote.RootItem.Children.Count = 0) Then  'dont show quotes with nothing on them (yet)


					//    If odd Then row.CssClass &= " quotesListOdd" Else row.CssClass &= " quotesListEven"
					//toggle betwen the odd and even classes for a stripey (more read accrossable) list
					//  tbl2.Controls.Add(FloatNone())


				 // ERROR: Not supported in C#: WithStatement

				if (quoteCount > 100)
					break; // TODO: might not be correct. Was : Exit For
			}
			//.quotes.Values
			foreach (clsQuote quote in (from q in sortedQuotesqwhere q.Saved == false)) {
				// Dim adic As New Dictionary(Of String, Object)(StringComparer.CurrentCultureIgnoreCase)

					//Apply any filtering . . .


					//If quote.RootQuote.ID = 32 Then Stop

					//show all versions of 'Expanded' quotes, for non expanded ones we only show the latest revision
					//If quote.RootQuoteID = 11051 Then Stop
					//  If Not (quote.State Is state_new And quote.RootItem.Children.Count = 0) Then  'dont show quotes with nothing on them (yet)

					//    If odd Then row.CssClass &= " quotesListOdd" Else row.CssClass &= " quotesListEven"
					//toggle betwen the odd and even classes for a stripey (more read accrossable) list
					//'    tbl.Controls.Add(FloatNone())


				 // ERROR: Not supported in C#: WithStatement

				if (quoteCount > 100)
					break; // TODO: might not be correct. Was : Exit For
			}
		}
		Literal tabLiteral = new Literal();
		Literal tabLiteral2 = new Literal();
		//tabLiteral.Text = "<div id=""tabs""><ul><li><a href=""#DraftPanel"">Draft Quotes</a></li><li><a href=""#SavedPanel"">Saved Quotes</a></li></ul>"
		tabLiteral2.Text = "</div>";
		//form1.Controls.Add(tabLiteral)

		if (quoteCount == 0 | searchFailed) {
			object trw = new TableRow {
				HorizontalAlign = HorizontalAlign.Center,
				CssClass = "center"
			};
			trw.Cells.Add(new TableCell {
				Text = Xlt("No Quotes Found", language),
				ColumnSpan = 10,
				CssClass = "center",
				HorizontalAlign = HorizontalAlign.Center
			});
			object trw2 = new TableRow {
				HorizontalAlign = HorizontalAlign.Center,
				CssClass = "center"
			};
			trw2.Cells.Add(new TableCell {
				Text = Xlt("No Quotes Found", language),
				ColumnSpan = 10,
				CssClass = "center",
				HorizontalAlign = HorizontalAlign.Center
			});
			tbl.Rows.Add(trw);
			tbl2.Rows.Add(trw2);
		}
		form1.Controls.Add(tbl);
		form1.Controls.Add(tbl2);
		form1.Controls.Add(tabLiteral2);
		OutputErrors(form1.Controls, errorMessages, lid);

		//Dim client As ClientScriptManager = Me.Page.ClientScript
		//If (client.IsClientScriptBlockRegistered(Me.GetType(), "Alert")) Then
		//    client.RegisterClientScriptBlock(Me.GetType(), "Alert", "$(""#tabs"").tabs();", True)
		//End If



	}


	public Literal FloatNone()
	{

		FloatNone = new Literal();
		FloatNone.Text = "<div style='clear:both'></div>";


	}
}


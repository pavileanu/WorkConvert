// VBConversions Note: VB project level imports
using System.Text.RegularExpressions;
using System.Web.UI.HtmlControls;
using System.Diagnostics;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Data;
using System.Collections.Specialized;
using System.Web.Profile;
using Microsoft.VisualBasic;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Collections;
using System;
using System.Web;
using System.Web.UI;
using System.Web.SessionState;
using System.Text;
using System.Web.Caching;
using System.Web.UI.WebControls.WebParts;
using System.Linq;
// End of VB project level imports


namespace IQ
{


public partial class manipulation : clsPageLogging
{

protected void Page_Load(object sender, System.EventArgs e)
{

//this page is typically execute by iQuote2.js

//   rexec("manipulation.aspx?command=graft&source=" + copySourceBranchID + "target=" + targetBranchID);

Exception exception = null;

clsBranch targetBranch = null;
if (Request["TargetBranch"] != null)
{
targetBranch = CoreCode.iq.Utility.branches[Request["TargetBranch"]];
}
clsBranch sourcebranch = null;
if (Request["SourceBranch"] != null)
{
sourcebranch = CoreCode.iq.Utility.branches[Request["SourceBranch"]];
}
string targetPath = string.Empty;
if (Request["TargetPath"] != null)
{
targetPath = Request["targetPath"];
}
string sourcePath = string.Empty;
if (Request["SourcePath"] != null)
{
sourcePath = Request["sourcePath"];
}

UInt64 lid = Request.QueryString["lid"];

if (CoreCode.iq.get_sesh(lid, "UserID") == null)
{
Response.Redirect("Loading.aspx", false);
}
return;

//string username = CoreCode.iq.UserAccountName(System.Convert.ToInt32(CoreCode.iq.get_sesh(lid, "UserID")), System.Convert.ToInt32(CoreCode.iq.get_sesh(lid, "AgentAccount").id));
//clsAccount agentaccount = (clsAccount) (CoreCode.iq.get_sesh(lid, "AgentAccount"));

//clsQuote currentQuote = null;

//if (CoreCode.iq.SeshContains(lid, "QuoteID"))
//{
//if (CoreCode.iq.get_sesh(lid, "QuoteID") != null)
//{
//int qid = System.Convert.ToInt32(CoreCode.iq.get_sesh(lid, "QuoteID"));
//currentQuote = agentaccount.Quotes(qid);
//}
//}

//try
//{

//
//if (Request["command"] == "graft")
//{

//string errormessage = System.Convert.ToString(targetBranch.Graft(sourcebranch, username, "", errorMessages)); //Creates the new graft

//we must delete the cached dataview - otherwise we won't see the change
//CoreCode.wipeCachedDataView(targetPath, lid);

//if the graft fails, we put an error in the response which the JS will place into the tree
//Panel1.Controls.Add(Utility.ErrorDymo(errormessage, lid));

//Case Is = "adopt" '(reparent - many branches )

//    wipeCachedDataView(targetPath, lid)
//    Dim newParent As clsBranch = iq.Branches(Split(targetPath, ".").Last)

//    'the JS compiles a list of sources from the checked branches (their paths)
//    For Each s In Request("sources").Split(",")
//        If s <> "" Then 'the JS untidily leaves an extra comma - but it's easier to deal with here
//            sourcebranch = iq.Branches(s.Split(".").Last)
//            sourcebranch.Parent = newParent
//            sourcebranch.Update(errorMessages)
//        End If
//    Next

//    'slots and quantities (on descendants) will need re pathing
//    'any grafts and prunes in force will need manipulating
//    'some quoteitems paths may be invalidated

//}
//else if (Request["command"] == "clone")
//{

//CoreCode.wipeCachedDataView(Utility.oneAbove(sourcePath), lid); //we need to clear the PARENTs dataview

//int bid = System.Convert.ToInt32(sourcePath.Split('.').Last);
//clsBranch aBranch = CoreCode.iq.Utility.branches[bid].clone(sourcePath, errorMessages);
//}
//else if (Request["command"] == "having")
//{
//just a stub.. allows the branch to be displayed open - with the Ex buttons
//}
//else if (Request["command"] == "exclude")
//{

//rExec('manipulation.aspx?command=exclusivity&bid=' + bid + '&val=' + tb.value, nullFunc);
//object v = DBNull.Value;
//clsBranch branch = null;
//Having and Excludes (request variables) are complete paths - we're only interested in the final branch
//int hvg = System.Convert.ToInt32(sourcePath.Split('.').Last);
//int exc = System.Convert.ToInt32(targetPath.Split('.').Last);
//if (CoreCode.iq.Branches.ContainsKey(hvg))
//{
//clsExclude ex = new clsExclude(CoreCode.iq.Utility.branches[hvg], CoreCode.iq.Utility.branches[exc], "No reason specified");
//}
//}
//else if (Request["command"] == "prune")
//{

//CoreCode.wipeCachedDataView(sourcePath, lid);
//CoreCode.iq.Prune(sourcePath, username);
//}
//else if (Request["command"] == "retract")
//{

//CoreCode.wipeCachedDataView(Utility.oneAbove(sourcePath), lid); //we need to clear the PARENTs dataview
//CoreCode.iq.Retract(CoreCode.iq.Utility.branches[sourcePath.Split('.').Last], username, ref errorMessages);
//}
//else if (Request["command"] == "cursor")
//{
//CoreCode.iq.set_sesh(lid, "treeCursor", sourcePath);
//}
//else if (Request["command"] == "createNextVersion")
//{

//clsQuote Quote = default(clsQuote);
//clsQuote RevisedQuote = default(clsQuote);

//Quote = agentaccount.Quotes(Request["QuoteID"]);
//RevisedQuote = Quote.CreateNextVersion(errorMessages);

//CoreCode.iq.set_sesh(lid, "QuoteID", RevisedQuote.ID);
//CoreCode.iq.set_sesh(lid, "quoteCursor", RevisedQuote.RootItem.Children(0).ID);
//}
//else if ((Request["command"] == "startQuote") || (Request["command"] == "startquote"))
//{

//Utility.DiscardUnChangedQuote(lid);
//CoreCode.iq.get_sesh(lid, "branchStates").clear(); //wipetreestate


//int bid = 0;
//if (Request["buyerid"] == "")
//{
//bid = System.Convert.ToInt32(CoreCode.iq.get_sesh(lid, "AgentAccount"));
//}
//else
//{
//bid = Request["buyerid"];
//}

//           If Not iq.Accounts(bid).SellerChannel Is agentaccount.sellerchannel Then

//this buyer does not have an account with this seller (yet).. so make one
//we'll need a priceBand (amongst other things)


//          End If

//startQuote(bid, lid);
//}
//else if (Request["command"] == "adminON")
//{
//CoreCode.iq.set_sesh(lid, "admin", true);
//}
//else if (Request["command"] == "adminOFF")
//{
//CoreCode.iq.set_sesh(lid, "admin", false);
//}
//else if (Request["command"] == "CreateChannel")
//{

//clsChannel sellerChannel = agentaccount.SellerChannel;
//clsChannel achanel = new clsChannel(sellerChannel, "New Company", "Holding company", "", "NEW1", sellerChannel.Region, new nullableString(), new nullableString(), new nullableString(), 15, "tree.1", "", 0, 0, "R", "", "", CoreCode.iq.i_currency_code["GBP"], sellerChannel.Universal, sellerChannel.orderEmail, "", "", null, -1);

//}
//else if (Request["command"] == "CreateSiblingAccount")
//{
//Creates a new user - and an account for them them
// we may need to migrate the accounts to
//clsAccount acToCopy = CoreCode.iq.Accounts(Request["AccID"]); // this is
//clsUser NewUser = new clsUser(acToCopy.BuyerChannel, DomainPart(acToCopy.User.Email), "", acToCopy.User.tel1, acToCopy.User.tel2, null, -1);
//int null_int = null;
//clsAccount anAccount = new clsAccount(NewUser, "Password", acToCopy.BuyerChannel, acToCopy.Roles, acToCopy.Team, acToCopy.Language, acToCopy.Currency, acToCopy.SellerChannel, acToCopy.Priceband, acToCopy.BuyerChannel.Region.Culture, acToCopy.mfrCode, null, ref null_int);
//anAccount.insert(ref errorMessages); //does the insert and sets the ID
//CoreCode.iq.set_sesh(lid, "BuyerAccount", anAccount.ID);

//}
//else if (Request["command"] == "delNote")
//{
//clsQuoteItem qi = currentQuote.RootItem.FindRecursive(Request["qiid"]);
//qi.Note = new nullableString();
//qi.Update();
//}
//else if (Request["command"] == "addNote")
//{


//clsQuoteItem qi = currentQuote.RootItem.FindRecursive(Request["qiid"]);
//qi.Note = new nullableString("Your note");
//qi.Update();
//}
//else if (Request["command"] == "saveNote")
//{

//int qiid = (int) (Conversion.Val(Strings.Mid(Request["qiid"], 5))); //The qiid parameter is now the element name - with is the QuoteItemID prefixed by 'note'  - e.g. note3458830
//clsQuoteItem qi = currentQuote.RootItem.FindRecursive(qiid);
//if (qi != null) //If the remove an item to which they just added a note - we wont be able to locate/save it
//{
//qi.Note = new nullableString(Request["text"]);
//qi.Update();
//}
//}
//else if (Request["command"] == "CopyQuote")
//{

//clsQuote quote = agentaccount.Quotes(Request["QID"]);

//var newQuote = quote.Copy(null, 0, ref errorMessages);
//CoreCode.iq.set_sesh(lid, "quoteCursor", newQuote.RootItem.Children(0).ID);
//CoreCode.iq.set_sesh(lid, "QuoteID", newQuote.ID);
//}
//else if (Request["command"] == "MarkAsWon")
//{

//clsQuote quote = agentaccount.Quotes(Request["QID"]);
//if (quote.PassesValidation(lid))
//{
//errorMessages = quote.MarkAsWon(lid);
//}
//else
//{
//Panel1.Controls.Add(Utility.NewLit("[FV]"));
//}
//}
//else if (Request["command"] == "DiscardQuote")
//{

//clsQuote quote = agentaccount.Quotes(Request["QID"]);
//quote.State = CoreCode.iq.i_state_GroupCode["QT-#CX"]; //mark as closed
//quote.Update();
//}
//else if (Request["command"] == "ExpandQuoteVersions")
//{

//if (!CoreCode.iq.SeshContains(lid, "expandedQuotes"))
//{
//CoreCode.iq.set_sesh(lid, "expandedQuotes", new List<int>());
//}

//if (agentaccount.Quotes.ContainsKey(Request["RQID"])) //this should be redundant
//{

//System.Collections.Generic.List<int> expandedQuotes = CoreCode.iq.get_sesh(lid, "expandedQuotes"); // a list of the root quotes which are expanded
//if (!expandedQuotes.Contains(System.Convert.ToInt32(Request["RQID"])))
//{
//expandedQuotes.Add(System.Convert.ToInt32(Request["RQID"]));
//}
//}

//}
//else if (Request["command"] == "CollapseQuoteVersions")
//{
//CoreCode.iq.get_sesh(lid, "expandedQuotes").remove(Request["RQID"]); //The agent account has had all its quotes loaded - we add the quote form there
//} //focus can be set to a comma seperated value (which a *set* of 'focuses' - eg. Receta+Budget) (or whatever) - ProductVisible checks Focus (if it's set)
//else if (Request["command"] == "focus")
//{

//if (Request["value"] == "")
//{
//CoreCode.iq.set_sesh(lid, "focus", new List<string>()); //Spliting and empty string into a list products a single empty value (ie a list with one entry) - so we have a special case
//}
//else
//{
//CoreCode.iq.set_sesh(lid, "focus", Strings.Split(Request["value"], ",").ToList); //A LIST(of Strings) goes into the session variable
//}
//}
//else if (Request["command"] == "quoteNameChange")
//{

//clsQuote quote = agentaccount.Quotes(Request["QID"]);
//quote.Name = new nullableString(Regex.Replace(Request["quoteName"], "<[^>]+>", ""));
//quote.Update();
//}
//else
//{
//Debug.Print(Request["Command"]);
//Debugger.Break();
//}
//Panel1.Controls.Add(Utility.NewLit("<p>" + string.Join(Environment.NewLine, errorMessages) + "</p>"));
//}
//catch (Exception ex)
//{
//ErrorLog.Add(ex);
//exception = ex;
//}
//Audit Trail
//AuditLog.Instance.Add(lid, System.Convert.ToString(Request["command"].ToString()), System.Convert.ToString(sourcePath == null ? (sourcebranch == null ? string.Empty : sourcebranch.ID) : sourcePath), System.Convert.ToString(targetPath == null ? (targetBranch != null ? targetBranch.ID : null) : targetPath), errorMessages, exception, "", "", 0, Context.Request.HttpMethod, Context.Request.UrlReferrer.AbsoluteUri);

}



private string DomainPart(dynamic email)
{

int at = 0;
at = email.ToString().IndexOf("@") + 1;
if (at)
{
return Strings.Mid(System.Convert.ToString(email), at);
}
else
{
return "";
}

}


public void startQuote(int BuyerID, UInt64 lid)
{

if (lid == 0)
{
Debugger.Break();
}

if (CoreCode.iq.SeshContains(lid, "QuoteID"))
{
//save any changes to the quote in progress
clsQuote inprogress = default(clsQuote);
inprogress = ((clsAccount) (CoreCode.iq.get_sesh(lid, "AgentAccount"))).Quotes(CoreCode.iq.get_sesh(lid, "QuoteID"));
inprogress.Update();
}

CoreCode.iq.set_sesh(lid, "BuyerAccount", BuyerID);

//start a new quote
clsQuote aQuote = default(clsQuote);
clsAccount buyerAccount = (clsAccount) (CoreCode.iq.get_sesh(lid, "BuyerAccount"));
clsAccount agentAccount = (clsAccount) (CoreCode.iq.get_sesh(lid, "AgentAccount"));



//Dim l$ = UpdatePrices(buyeraccount)  'cache' (grab the latest prices from IQ2

NullablePrice nullprice = default(NullablePrice); //The quote will start life with an unknown price
nullprice = new NullablePrice(buyerAccount.Currency);
aQuote = new clsQuote(buyerAccount, agentAccount, null, DateTime.Now, DateTime.Now, 1, CoreCode.iq.i_state_GroupCode["QT-#NW"], nullprice, buyerAccount.Currency, false, false, false, System.Convert.ToString(false), new nullableString(), new nullableString(), 0);


CoreCode.iq.set_sesh(lid, "QuoteID", aQuote.ID);

//populate the customer name, display quote #

//txtBuyer.Text = buyeraccount.displayname(s_lang)
//txtBuyer.Enabled = False ' lock it !
//LblBuyer.Text = buyeraccount.displayname(s_lang)

//reveal the Product tree (if they have a quote on the go)
//Response.Write("<script display('treeHolder','inline')></script>;")
// PnlProductTree.CssClass = "visible"
}

}
}

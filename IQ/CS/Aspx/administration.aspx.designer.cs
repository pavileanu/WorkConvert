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


//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IQ
{


public partial class administration
{

///<summary>
///Pnl control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Panel Pnl;

///<summary>
///adminMenu control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Menu adminMenu;

///<summary>
///adminTabsLine control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.HtmlControls.HtmlGenericControl adminTabsLine;

///<summary>
///adminMultiView control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.MultiView adminMultiView;

///<summary>
///tabUserAdmin control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.View tabUserAdmin;

///<summary>
///txtFilter control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox txtFilter;

///<summary>
///btnSearch control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Button btnSearch;

///<summary>
///chkonlyDistiAdmin control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.CheckBox chkonlyDistiAdmin;

///<summary>
///grdUser control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.GridView grdUser;

///<summary>
///tabCreateUser control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.View tabCreateUser;

///<summary>
///txtAccountId control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox txtAccountId;

///<summary>
///lblChannelSelect control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Label lblChannelSelect;

///<summary>
///ddlChannels control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.DropDownList ddlChannels;

///<summary>
///Label1 control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Label Label1;

///<summary>
///Label4 control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Label Label4;

///<summary>
///txtFullName control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox txtFullName;

///<summary>
///txtEmailName control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox txtEmailName;

///<summary>
///drpDomain control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.DropDownList drpDomain;

///<summary>
///Label3 control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Label Label3;

///<summary>
///Label8 control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Label Label8;

///<summary>
///TxtTelephone control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox TxtTelephone;

///<summary>
///drpTeams control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.DropDownList drpTeams;

///<summary>
///divCurrency control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.HtmlControls.HtmlGenericControl divCurrency;

///<summary>
///lblCurrency control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Label lblCurrency;

///<summary>
///drpCurrency control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.DropDownList drpCurrency;

///<summary>
///lblRoles control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Label lblRoles;

///<summary>
///lbRoles control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.ListBox lbRoles;

///<summary>
///chkAdminUser control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.CheckBox chkAdminUser;

///<summary>
///chkEmailUser control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.RadioButton chkEmailUser;

///<summary>
///chkEmailAdmin control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.RadioButton chkEmailAdmin;

///<summary>
///BtnSave control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Button BtnSave;

///<summary>
///Pnl2 control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Panel Pnl2;

///<summary>
///PnlMultiSend control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Panel PnlMultiSend;

///<summary>
///Label10 control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Label Label10;

///<summary>
///txtMultiHost control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox txtMultiHost;

///<summary>
///btnGetStubs control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Button btnGetStubs;

///<summary>
///TxtMultisend control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox TxtMultisend;

///<summary>
///BtnMultisend control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Button BtnMultisend;

///<summary>
///chkMultiDoit control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.CheckBox chkMultiDoit;

///<summary>
///tabSystem control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.View tabSystem;

///<summary>
///panelSignInMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Panel panelSignInMessage;

///<summary>
///txtSignInSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox txtSignInSystemMessage;

///<summary>
///txtSystemMessageValidFrom control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox txtSystemMessageValidFrom;

///<summary>
///txtSystemMessageValidTo control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox txtSystemMessageValidTo;

///<summary>
///chkSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.CheckBox chkSystemMessage;

///<summary>
///btnAddSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Button btnAddSystemMessage;

///<summary>
///btnAmendSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Button btnAmendSystemMessage;

///<summary>
///btnDeleteSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Button btnDeleteSystemMessage;

///<summary>
///panelHpeSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Panel panelHpeSystemMessage;

///<summary>
///txtHpeSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox txtHpeSystemMessage;

///<summary>
///txtHpeSystemMessageValidFrom control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox txtHpeSystemMessageValidFrom;

///<summary>
///txtHpeSystemMessageValidTo control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox txtHpeSystemMessageValidTo;

///<summary>
///hpeMessageEnabled control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.CheckBox hpeMessageEnabled;

///<summary>
///btnAddHpeSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Button btnAddHpeSystemMessage;

///<summary>
///btnAmendHpeSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Button btnAmendHpeSystemMessage;

///<summary>
///btnDeleteHpeSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Button btnDeleteHpeSystemMessage;

///<summary>
///panelHpiSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Panel panelHpiSystemMessage;

///<summary>
///txtHpiSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox txtHpiSystemMessage;

///<summary>
///txtHpiSystemMessageValidFrom control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox txtHpiSystemMessageValidFrom;

///<summary>
///txtHpiSystemMessageValidTo control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.TextBox txtHpiSystemMessageValidTo;

///<summary>
///hpiMessageEnabled control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.CheckBox hpiMessageEnabled;

///<summary>
///btnAddHpiSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Button btnAddHpiSystemMessage;

///<summary>
///btnAmendHpiSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Button btnAmendHpiSystemMessage;

///<summary>
///btnDeleteHpiSystemMessage control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.Button btnDeleteHpiSystemMessage;

///<summary>
///tabReports control.
///</summary>
///<remarks>
///Auto-generated field.
///To modify move field declaration from designer file to code-behind file.
///</remarks>
protected global::System.Web.UI.WebControls.View tabReports;
}

}

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

using System.Data.SqlClient;


namespace IQ
{

/// <summary>
///
/// </summary>
/// <remarks></remarks>

public class clsClickThru : i_Editable
{

public int ID {get; set;}
public clsAccount Account {get; set;}
public clsAdvert Advert {get; set;}
public DateTime TimeStamp {get; set;}
private string conString; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.

public clsClickThru()
{
// VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
conString = ConfigurationManager.ConnectionStrings["DBConnectString"].ConnectionString;



}
public clsClickThru(clsAccount account, clsAdvert advert, DateTime timestamp)
{
// VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
conString = ConfigurationManager.ConnectionStrings["DBConnectString"].ConnectionString;


SqlConnection con = new SqlConnection(conString);
con.Open();
SqlCommand command = new SqlCommand();
command.CommandText = "AddClickThru";
command.CommandType = CommandType.StoredProcedure;
command.Connection = con;
SqlParameter paramUserID = new SqlParameter("@accountid", SqlDbType.Int);
paramUserID.Value = account.ID;
SqlParameter paramAdvertID = new SqlParameter("@advertid", SqlDbType.Int);
paramAdvertID.Value = advert.ID;
SqlParameter paramTimeStamp = new SqlParameter("@timestamp", SqlDbType.DateTime);
paramTimeStamp.Value = timestamp;

SqlParameter paramReturn = new SqlParameter("@ret", SqlDbType.Int);
paramReturn.Direction = ParameterDirection.ReturnValue;

command.Parameters.Add(paramUserID);
command.Parameters.Add(paramAdvertID);
command.Parameters.Add(paramTimeStamp);
command.Parameters.Add(paramReturn);

command.ExecuteNonQuery();

con.Close();

this.ID = Convert.ToInt32(paramReturn.Value);
this.Account = account;
this.Advert = advert;
this.TimeStamp = timestamp;

this.Advert.ClickThrus.Add(this.ID, this);
this.Account.ClickThrus.Add(this.ID, this);

}
public clsClickThru(int ID, clsUser user, clsAdvert advert, DateTime timestamp)
{
// VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
conString = ConfigurationManager.ConnectionStrings["DBConnectString"].ConnectionString;

this.ID = ID;
this.Account = Account;
this.Advert = advert;
this.TimeStamp = timestamp;

this.Advert.ClickThrus.Add(this.ID, this);
this.Account.ClickThrus.Add(this.ID, this);

}
public void delete(ref System.Collections.Generic.List<string> errorMessages)
{
this.Advert.ClickThrus.Remove(this.ID);
this.Account.ClickThrus.Remove(this.ID);
}

public string displayName(clsLanguage Language)
{

}

public dynamic Insert(ref System.Collections.Generic.List<string> errorMessages)
{
return new clsClickThru(this.Account, this.Advert, this.TimeStamp);
}

public void update(ref System.Collections.Generic.List<string> errorMessages)
{
if (this.ID > 0)
{

SqlConnection con = new SqlConnection(conString);
con.Open();
SqlCommand command = new SqlCommand();
command.CommandText = "UpdateClickThru";
command.CommandType = CommandType.StoredProcedure;
command.Connection = con;
SqlParameter paramID = new SqlParameter("@ID", SqlDbType.Int);
paramID.Value = this.ID;
SqlParameter paramUserID = new SqlParameter("@accountid", SqlDbType.Int);
paramUserID.Value = Account.ID;
SqlParameter paramAdvertID = new SqlParameter("@adverid", SqlDbType.Int);
paramAdvertID.Value = Advert.ID;
SqlParameter paramTimeStamp = new SqlParameter("@url", SqlDbType.DateTime);
paramTimeStamp.Value = TimeStamp;

SqlParameter paramReturn = new SqlParameter("@ret", SqlDbType.Int);
paramReturn.Direction = ParameterDirection.ReturnValue;

command.Parameters.Add(paramUserID);
command.Parameters.Add(paramAdvertID);
command.Parameters.Add(paramTimeStamp);
command.Parameters.Add(paramReturn);

command.ExecuteNonQuery();

con.Close();
}
}
}

}

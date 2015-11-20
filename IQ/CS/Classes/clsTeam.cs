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

using dataAccess;

namespace IQ
{
[Serializable]public class clsTeam : i_Editable
{

public int ID {get; set;}
public string Name {get; set;}
public List<clsUser> Members {get; set;}
public clsChannel Channel {get; set;}


public clsTeam()
{

//this is the 'delayed create' version - called by the generic editor
//an instance is created - but it is not added to its parent channel unti it is Update()d
this.ID = -1;
this.Channel = null;
this.Members = new List<clsUser>();
this.Channel = null;

}


public clsTeam(clsChannel channel, string Name)
{

object sql = null;
sql = "INSERT INTO Team(Name,FK_Channel_ID) VALUES (\'" + Name + "\'," + System.Convert.ToString(channel.ID) + ");";

object null_object = null;
this.ID = da.DBExecutesql(System.Convert.ToString(sql), true, ref null_object);
this.Name = Name;
this.Channel = channel;

Channel.Teams.Add(this.ID, this);
CoreCode.iq.Teams.Add(this.ID, this);

this.Members = new List<clsUser>();


}

public clsTeam(int id, clsChannel channel, string Name)
{

this.ID = id;
this.Name = Name;

Channel.Teams.Add(this.ID, this);
CoreCode.iq.Teams.Add(this.ID, this);

this.Members = new List<clsUser>();
this.Channel = channel;

}

public string displayName(clsLanguage Language)
{
string returnValue = "";
returnValue = this.Name; //& "(" & Me.Code & ")"
return returnValue;
}


public dynamic Insert(ref System.Collections.Generic.List<string> errormessages)
{

return new clsTeam(this.Channel, this.Name); //we *now* call the constructor which makes a team and adds it to the approprtiate dictionaries/parent object

}

public void update(ref System.Collections.Generic.List<string> errormessages)
{

object sql = null;
sql = "UPDATE [team] set name =" + da.SqlEncode(this.Name) + " WHERE ID=" + System.Convert.ToString(this.ID);
object null_object = null;
da.DBExecutesql(System.Convert.ToString(sql), false, ref null_object);

}

public void delete(ref System.Collections.Generic.List<string> errormessages)
{


object sql = null;
sql = "DELETE FROM [team] WHERE id=" + System.Convert.ToString(this.ID);

try
{
object null_object = null;
da.DBExecutesql(System.Convert.ToString(sql), false, ref null_object);
}
catch (Exception ex)
{
errormessages.Add(ex.Message);
}


}


}


}

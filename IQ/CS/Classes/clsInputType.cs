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
using System.Runtime.Serialization;

namespace IQ
{

[DataContract()]public class clsInputType
{

public int ID {get; set;}
public string code {get; set;}
public string name {get; set;}

public dynamic get_displayName(clsLanguage langauge)
{
return this.name + " (" + this.code + ")";
}

public clsInputType(int id, string code, string name)
{

this.ID = id;
this.code = code;
this.name = name;

CoreCode.iq.InputTypes.Add(this.ID, this);
CoreCode.iq.i_inputType_code.Add(this.code, this);

}



public clsInputType(string code, string name)
{

object sql = null;
sql = "INSERT INTO [InputType] (code,name) values (" + da.SqlEncode(code) + "," + da.SqlEncode(name) + ");";

object null_object = null;
this.ID = da.DBExecutesql(System.Convert.ToString(sql), true, ref null_object);
this.code = code;
this.name = name;

CoreCode.iq.InputTypes.Add(this.ID, this);
CoreCode.iq.i_inputType_code.Add(this.code, this);

}
}

}

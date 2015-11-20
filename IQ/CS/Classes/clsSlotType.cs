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

public class clsSlotType
{

//Branches have slots of one or more types (see clsbranch.slots)
//quantities attach to 'child' branches to 'consume' slots of a given type in a give 'parent' branch
//NB: 'parent' and 'child' in this discussion are not strictly parent or child.. a (typically) outer branch consumes the slots of some (typically) inner branch
//- as specified by the quantity.TakesSlotsIn

public int ID {get; set;}
public string MajorCode {get; set;}
public string MinorCode {get; set;}
public clsTranslation Translation {get; set;} //this is what's displayed
public clsTranslation TranslationShort {get; set;}
public SortedDictionary<int, clsSlotType> Fallback {get; set;} //Which type of slot should(can) we use if this type is unavialble - eg a 4x PCI card would fall back to an 8x slot (seems backwards - but we want to occupy the least 'expensive' slots first)
public bool EnforceMinorCode {get; set;}

public string get_displayName(clsLanguage language)
{
return this.MajorCode + "/" + this.MinorCode;
}


public string get_shortDisplayName(clsLanguage language)
{
if (this.TranslationShort != null)
{
return this.TranslationShort.get_text(language);
}
else
{
return get_displayName(language);
}
}

public clsSlotType(string MajorCode, string MinorCode)
{
//Must be a dummy
this.MajorCode = MajorCode;
this.MinorCode = MinorCode;
ID = CoreCode.iq.Utility.slottypes.Min(g => g.Key) - 1;
if (!CoreCode.iq.i_slotType_Code.ContainsKey(this.MajorCode))
{
CoreCode.iq.i_slotType_Code[this.MajorCode].Add(this.MajorCode.ToUpper(), this);
}
CoreCode.iq.i_slotType_Code[this.MajorCode].Add(this.MinorCode.ToUpper(), this);
CoreCode.iq.Utility.slottypes.Add(ID, this);
}


public clsSlotType(int id, string MajorCode, string MinorCode, clsTranslation translation, clsTranslation translationShort, bool EnforceMinorCode)
{

this.ID = id;
this.MajorCode = MajorCode.ToUpper();
this.MinorCode = MinorCode.ToUpper();
this.TranslationShort = translationShort;
this.Translation = translation;
this.Fallback = new SortedDictionary<int, clsSlotType>();
this.EnforceMinorCode = EnforceMinorCode;

CoreCode.iq.Utility.slottypes.Add(this.ID, this);

if (!CoreCode.iq.i_slotType_Code.ContainsKey(this.MajorCode))
{
CoreCode.iq.i_slotType_Code.Add(this.MajorCode, new Dictionary<string, clsSlotType>(StringComparer.InvariantCultureIgnoreCase));
}
if (!CoreCode.iq.i_slotType_Code[this.MajorCode].ContainsKey(MinorCode))
{
CoreCode.iq.i_slotType_Code[this.MajorCode].Add(this.MinorCode, this);
}
else
{
Interaction.Beep();
}



}

public clsSlotType(string majorCode, string minorCode, clsTranslation translation)
{

object sql = null;
sql = "INSERT INTO SlotType(fk_translation_key,majorCode,MinorCode) VALUES (" + System.Convert.ToString(translation.Key) + "," + da.SqlEncode(majorCode) + "," + da.SqlEncode(minorCode) + ");";

object null_object = null;
this.ID = da.DBExecutesql(System.Convert.ToString(sql), true, ref null_object);
this.MajorCode = majorCode.ToUpper();
this.MinorCode = minorCode.ToUpper();

this.Translation = translation;
this.Fallback = new SortedDictionary<int, clsSlotType>();

CoreCode.iq.Utility.slottypes.Add(this.ID, this);

if (!CoreCode.iq.i_slotType_Code.ContainsKey(this.MajorCode))
{
CoreCode.iq.i_slotType_Code.Add(this.MajorCode, new Dictionary<string, clsSlotType>(StringComparer.InvariantCultureIgnoreCase));
}
if (!CoreCode.iq.i_slotType_Code[this.MajorCode].ContainsKey(minorCode))
{
CoreCode.iq.i_slotType_Code[this.MajorCode].Add(this.MinorCode, this);
}
else
{
Interaction.Beep();
}

}

public clsSlotType Insert()
{

return new clsSlotType(this.MajorCode, this.MinorCode, this.Translation);

}

public void Update()
{

object sql = null;
sql = "UPDATE slottype set majorcode=" + da.SqlEncode(this.MajorCode) + ",minorcode=" + da.SqlEncode(this.MinorCode) + ",fk_translation_key=" + System.Convert.ToString(this.Translation.Key) + ",fk_translation_key_short=" + System.Convert.ToString(this.TranslationShort == null ? "null" : this.TranslationShort.Key);
sql += " WHERE ID = " + System.Convert.ToString(this.ID);
object null_object = null;
da.DBExecutesql(System.Convert.ToString(sql), false, ref null_object);

}

public bool Delete()
{

object sql = null;
sql = "Delete from slottype where id=" + System.Convert.ToString(this.ID);

try
{
//this may fail due to RI
object null_object = null;
da.DBExecutesql(System.Convert.ToString(sql), false, ref null_object);

CoreCode.iq.Utility.slottypes.Remove(this.ID);
return true;

}
catch (Exception)
{

return false;

}

}

public void AddFallback(int pos, clsSlotType st)
{
Fallback.Add(pos, st);
var sql = "INSERT INTO altSlotType VALUES (" + System.Convert.ToString(this.ID) + "," + System.Convert.ToString(st.ID) + "," + System.Convert.ToString(pos) + ")";
da.DBExecutesql(SQL: sql);
}

} //clsSlotType

}

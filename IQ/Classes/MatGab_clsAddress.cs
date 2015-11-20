
using dataAccess;
using System.IO;

// Represents a single-field address of some sort - e.g. an email address or a URL

public class clsAddress : i_Editable
{

	public int ID;
	public string Code;

	public clsTranslation Translation;

	public clsAddress()
	{
	}


	public clsAddress(int ID, string Code, clsTranslation Translation)
	{
		this.ID = ID;
		this.Code = Code;
		this.Translation = Translation;

	}

	public object i_Editable.Insert(ref List<string> errormessages)
	{

		return new clsAddress(this.ID, this.Code, this.Translation);

	}


	public void i_Editable.delete(ref List<string> errorMessages)
	{
		object sql = "delete from [Address] where id=" + this.ID;

		da.DBExecutesql(sql);

	}


	public void i_Editable.Update(ref List<string> errorMessages)
	{
		object sql = string.Format("update [Address] set Code='{0}' where ID={1}", this.Code, this.ID);

		da.DBExecutesql(sql, false);

	}

	public string i_Editable.DisplayName(clsLanguage language)
	{

		return this.Translation.text(language);

	}

}

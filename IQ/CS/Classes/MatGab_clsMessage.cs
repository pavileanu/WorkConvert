
using dataAccess;
using System.IO;

// Represents a message displayed to the user - e.g. the banner message on the Sign In screen


public class clsMessage : i_Editable
{

	public int ID;
	public string Code;
	public clsTranslation Translation;
	public DateTime ValidFrom;
	public DateTime ValidTo;
	public bool Enabled;

	public int ChannelID;

	private string DATEFORMAT = "dd-MMM-yyyy";

	public clsMessage()
	{
	}


	public clsMessage(int ID, string Code, clsTranslation Translation, DateTime ValidFrom, DateTime ValidTo, bool Enabled, int ChannelID)
	{
		this.ID = ID;
		this.Code = Code;
		this.Translation = Translation;
		this.ValidFrom = ValidFrom;
		this.ValidTo = ValidTo;
		this.Enabled = Enabled;
		this.ChannelID = ChannelID;

	}


	public clsMessage(string Code, clsTranslation Translation, DateTime ValidFrom, DateTime ValidTo, bool Enabled, int ChannelID)
	{
		this.Code = Code;
		this.Translation = Translation;
		this.ValidFrom = ValidFrom;
		this.ValidTo = ValidTo;
		this.Enabled = Enabled;
		this.ChannelID = ChannelID;

		object sql = string.Format("insert into [message](Code, FK_Translation_key_Name, ValidFrom, ValidTo, FK_Channel_ID, Enabled) values ('{0}', {1}, '{2}', '{3}', {4}, {5})", this.Code, this.Translation.Key, this.ValidFrom.ToString(DATEFORMAT), this.ValidTo.ToString(DATEFORMAT), 1, this.Enabled ? 1 : 0, this.ChannelID);

		this.ID = da.DBExecutesql(sql, true);

	}

	public object i_Editable.Insert(ref List<string> errormessages)
	{

		return new clsMessage(this.Code, this.Translation, this.ValidFrom, this.ValidTo, this.Enabled, this.ChannelID);

	}


	public void i_Editable.delete(ref List<string> errorMessages)
	{
		object sql = "delete from [message] where id=" + this.ID;

		da.DBExecutesql(sql);

	}


	public void i_Editable.Update(ref List<string> errorMessages)
	{
		object sql = string.Format("update [message] set ValidFrom='{0}', ValidTo='{1}', Enabled={2} where ID={3}", this.ValidFrom.ToString(DATEFORMAT), this.ValidTo.ToString(DATEFORMAT), this.Enabled ? 1 : 0, this.ID);

		da.DBExecutesql(sql, false);

	}

	public string i_Editable.DisplayName(clsLanguage language)
	{

		return this.Translation.text(language);

	}

}

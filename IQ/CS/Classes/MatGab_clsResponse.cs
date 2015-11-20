
using dataAccess;
using System.IO;

// Represents an HPE Care Pack Response level

public class clsResponse : i_Editable
{

	public int ID;
	public string mfrCode;
	public clsTranslation Title;
	public clsTranslation Description;

	public bool ResponseDefault;

	private const string TABLE = "Response";

	public clsResponse()
	{
	}


	public clsResponse(int id, string mfrCode, clsTranslation title, clsTranslation description, bool responseDefault)
	{
		this.ID = id;
		this.mfrCode = mfrCode;
		this.Title = title;
		this.Description = description;
		this.ResponseDefault = responseDefault;

	}

	public object i_Editable.Insert(ref List<string> errormessages)
	{

		return new clsResponse(this.ID, this.mfrCode, this.Title, this.Description, this.ResponseDefault);

	}


	public void i_Editable.Delete(ref List<string> errorMessages)
	{
		object sql = string.Format("delete from {0} where id={1}", TABLE, this.ID);

		da.DBExecutesql(sql);

	}


	public void i_Editable.Update(ref List<string> errorMessages)
	{
		object sql = string.Format("update {0} set mfrCode='{1}', IsDefault={2} where ID={3}", TABLE, this.mfrCode, this.ResponseDefault, this.ID);

		da.DBExecutesql(sql, false);

	}

	public string i_Editable.DisplayName(clsLanguage language)
	{

		return this.Title.text(language);

	}

}

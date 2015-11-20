using dataAccess;


public class clsRole
{

    public int ID { get; set; }
    public string Code { get; set; }
    public clsTranslation Translation { get; set; }
    public Dictionary<int, clsRight> Rights { get; set; }
    public Dictionary<string, clsRight> i_right_code { get; set; }


    public dynamic get_DisplayName(clsLanguage language)
    {
        dynamic returnValue = default(dynamic);
        returnValue = this.Translation.text(language);
        return returnValue;
    }

    public dynamic EnglishDisplayName
    {
        get
        {
            dynamic returnValue = default(dynamic);
            returnValue = this.Translation.text(English);
            return returnValue;
        }
    }

    public clsRole()
    {
        this.ID = -1;
        this.Rights = new Dictionary<int, clsRight>();
        this.i_right_code = new Dictionary<string, clsRight>();

    }
    public clsRole(string Code, clsTranslation translation)
    {

        object sql = null;
        sql = "INSERT INTO [Role] (code,fk_Translation_key) ";
        sql += " values (" + da.SqlEncode(Code) + "," + Translation.Key + ");";

        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        this.Code = Code;
        this.Translation = translation;
        this.Rights = new Dictionary<int, clsRight>();
        this.i_right_code = new Dictionary<string, clsRight>();

        iq.i_role_Code.Add(this.Code, this);

    }

    public clsRole(int Id, string Code, clsTranslation translation)
    {

        this.ID = Id;
        this.Code = Code;
        this.Translation = translation;
        this.Rights = new Dictionary<int, clsRight>();
        this.i_right_code = new Dictionary<string, clsRight>();

        iq.i_role_Code.Add(this.Code, this);

    }

    public void AddRight(clsRight right)
    {
        if (Rights.ContainsKey(right.ID))
        {
            return;
        }
        object sql = null;
        sql = "INSERT INTO [RoleRight] (fk_Role_Id,fk_right_id) ";
        sql += " values (" + System.Convert.ToString(this.ID) + "," + right.ID + ");";

        da.DBExecutesql(sql, true);
        this.Rights.Add(right.ID, right);
        this.i_right_code.Add(right.Code, right);
    }

} //clsRole
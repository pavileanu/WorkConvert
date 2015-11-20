using dataAccess;


public class clsValidation
{

    public string ID { get; set; }
    public string description { get; set; }
    public string regEx { get; set; }
    public string ViolationMessage { get; set; }

    public clsValidation()
    {

    }

    public clsValidation Insert()
    {
        return new clsValidation(this.description, this.regEx, this.ViolationMessage);
    }


    public dynamic get_DisplayName(clsLanguage Language)
    {
        return this.description;
    }


    public clsValidation(int id, string description, string regex, string violation)
    {

        this.ID = (id).ToString();
        this.description = description;
        this.regEx = regex;
        this.ViolationMessage = violation;

        iq.Validations.Add(int.Parse(this.ID), this);

    }

    public clsValidation(string description, string regex, string violation)
    {

        this.description = description;
        this.regEx = regex;
        this.ViolationMessage = violation;

        object sql = null;
        sql = "INSERT INTO [validation] (descripion,regex,violation) VALUES(" + da.SqlEncode(this.description) + "," + da.SqlEncode(this.regEx) + "," + da.SqlEncode(this.ViolationMessage) + ");";
        this.ID = (da.DBExecutesql(sql, true)).ToString();

        iq.Validations.Add(int.Parse(this.ID), this);

    }

    public void update()
    {

        object sql = null;
        sql = "UPDATE [validation] set ";
        sql += "description=" + da.SqlEncode(this.description) + ",";
        sql += "regex=" + da.SqlEncode(this.regEx) + ",";
        sql += "viloationmessage=" + da.SqlEncode(this.ViolationMessage);

        da.dbexecutesql(sql, false);

    }




}
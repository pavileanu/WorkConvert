using dataAccess;


public class clsCulture
{
    public int ID { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public clsCulture(int ID, string code, string name)
    {
        this.ID = ID;
        this.Code = code;
        this.Name = name;
        iq.Cultures.Add(ID, this);
        iq.i_culture_code.Add(code, this);
    }
}
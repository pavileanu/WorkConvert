public class NullableInt
{

    //This class gives a strongly typed Nullable integer - used for many of the foriegn key pointers
    //you must NEVER test is using IsDbNUll(.. - this would always return false
    //always check if its sqlvalue (suitable for INSERT etc) = "null"

    public object value;

    public string Displayvalue
    {
        get
        {
            if (this.value == DBNull.Value)
            {
                return "";
            }
            else
            {
                return (this.value).ToString();
            }
        }
    }

    public NullableInt()
    {
        this.value = DBNull.Value;

    }
    public NullableInt(object value)
    {

        if (value == null)
        {
            Debugger.Break(); // please pass dbnull.value to create a null - or use the paramaterless constructor e.g. New NullableInt()
        }

        if (Information.IsDBNull(value))
        {
            this.value = DBNull.Value;
        }
        else if ((value) is int)
        {
            this.value = value;
        }
        else
        {
            Debugger.Break();
        }

    }

    public string sqlvalue()
    {

        if (Information.IsDBNull(this.value))
        {
            return "null";
        }
        else
        {
            return this.value.ToString();
        }

    }

    public override bool Equals(object obj)
    {
        NullableInt v = (NullableInt)obj;
        return value == null && v.value == null || value == v.value;
    }


}
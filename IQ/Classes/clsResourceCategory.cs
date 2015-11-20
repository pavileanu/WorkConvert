using dataAccess;
using System.IO;



// Represents a Resource category for display on the Resources page
// TODO: needs to implement i_Editable

public class clsResourceCategory
{

    public int ID;
    public string Name;
    public clsTranslation Translation;
    public int Order;
    public List<clsResource> Resources;

    public clsResourceCategory()
    {

    }

    public clsResourceCategory(int ID, string Name, clsTranslation Translation, int Order)
    {

        this.ID = ID;
        this.Name = Name;
        this.Translation = Translation;
        this.Order = Order;

    }

}

using dataAccess;
using System.IO;

// Represents a Resource file item for display on the Resources page

public class clsResource : i_Editable
{

	public int ID;
	public string Description;
	public string Type;
	public string Code;
	public clsTranslation Title;
	public clsRegion Region;
	public clsLanguage Language;
	public clsChannel SellerChannel;
	public string MfrCode;
	public int Order;
	public int CategoryId;

	public bool Embed;

	public clsResource()
	{
	}

	public clsResource(int ID, string Description, string Type, string Code, clsTranslation Title, clsRegion Region, clsLanguage Language, clsChannel SellerChannel, string MfrCode, int Order,

	int CategoryId, bool Embed)
	{
		this.ID = ID;
		this.Description = Description;
		this.Type = Type;
		this.Code = Code;
		this.Title = Title;
		this.Region = Region;
		this.Language = Language;
		this.SellerChannel = SellerChannel;
		this.MfrCode = MfrCode;
		this.Order = Order;
		this.CategoryId = CategoryId;
		this.Embed = Embed;

	}

	public Manufacturer Manufacturer {


		get {
			Manufacturer = Manufacturer.Unknown;

			if (!string.IsNullOrEmpty(this.MfrCode)) {
				if (string.Equals(this.MfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase)) {
					Manufacturer = Manufacturer.HPI;
				} else if (string.Equals(this.MfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase)) {
					Manufacturer = Manufacturer.HPE;
				}
			}

		}
	}


	public object i_Editable.Insert(ref List<string> errormessages)
	{

		return new clsResource(this.ID, this.Description, this.Type, this.Code, this.Title, this.Region, this.Language, this.SellerChannel, this.MfrCode, this.Order,
		this.CategoryId, this.Embed);

	}


	public void i_Editable.delete(ref List<string> errorMessages)
	{
		object sql = "delete from [Resource] where id=" + this.ID;

		da.DBExecutesql(sql);

	}


	public void i_Editable.Update(ref List<string> errorMessages)
	{
		object sql = string.Format("update [Resource] set Code='{0}', [Description]='{1}', [FK_Resource_Category_ID]={2}, [Type]='{3}', [Code]='{4}', [FK_Region_ID]={5}, [FK_Language_ID]={6}, [FK_SellerChannel_ID]={7}, [mfrCode]='{8}', [Order]={9}, [Embed]={10} where ID={11}", this.Code, this.Description, this.CategoryId, this.Type, this.Code, this.Region.ID, this.Language.ID, this.SellerChannel.ID, this.MfrCode,
		this.Order, this.Embed, this.ID);

		da.DBExecutesql(sql, false);

	}

	public string i_Editable.DisplayName(clsLanguage language)
	{

		return this.Description;

	}

}

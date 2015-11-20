using dataAccess;
public class clsProductValidation
{
	public EnumValidationSeverity Severity {
		get { return m_Severity; }
		set { m_Severity = Value; }
	}
	private EnumValidationSeverity m_Severity;
	public clsTranslation Message {
		get { return m_Message; }
		set { m_Message = Value; }
	}
	private clsTranslation m_Message;
	public clsTranslation CorrectMessage {
		get { return m_CorrectMessage; }
		set { m_CorrectMessage = Value; }
	}
	private clsTranslation m_CorrectMessage;
	public string RequiredOptType {
		get { return m_RequiredOptType; }
		set { m_RequiredOptType = Value; }
	}
	private string m_RequiredOptType;
	public int RequiredQuantity {
		get { return m_RequiredQuantity; }
		set { m_RequiredQuantity = Value; }
	}
	private int m_RequiredQuantity;
	public enumValidationType ValidationType {
		get { return m_ValidationType; }
		set { m_ValidationType = Value; }
	}
	private enumValidationType m_ValidationType;
	public string DependantOptType {
		get { return m_DependantOptType; }
		set { m_DependantOptType = Value; }
	}
	private string m_DependantOptType;
	public string CheckAttribute {
		get { return m_CheckAttribute; }
		set { m_CheckAttribute = Value; }
	}
	private string m_CheckAttribute;
	public string CheckAttributeValue {
		get { return m_CheckAttributeValue; }
		set { m_CheckAttributeValue = Value; }
	}
	private string m_CheckAttributeValue;
	public string DependantCheckAttribute {
		get { return m_DependantCheckAttribute; }
		set { m_DependantCheckAttribute = Value; }
	}
	private string m_DependantCheckAttribute;
	public string DependantCheckAttributeValue {
		get { return m_DependantCheckAttributeValue; }
		set { m_DependantCheckAttributeValue = Value; }
	}
	private string m_DependantCheckAttributeValue;
	public string OptionFamily {
		get { return m_OptionFamily; }
		set { m_OptionFamily = Value; }
	}
	private string m_OptionFamily;
	public enumValidationMessageType ValidationMessageType {
		get { return m_ValidationMessageType; }
		set { m_ValidationMessageType = Value; }
	}
	private enumValidationMessageType m_ValidationMessageType;
	public int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	public string LinkOptType {
		get { return m_LinkOptType; }
		set { m_LinkOptType = Value; }
	}
	private string m_LinkOptType;
	public string LinkTechnology {
		get { return m_LinkTechnology; }
		set { m_LinkTechnology = Value; }
	}
	private string m_LinkTechnology;
	public string LinkOptionFamily {
		get { return m_LinkOptionFamily; }
		set { m_LinkOptionFamily = Value; }
	}
	private string m_LinkOptionFamily;


	public string MessageText {
		get { return Message.text(English); }
		set { Message.text(English) = value; }
	}

	public string CorrectMessageText {
		get { return CorrectMessage.text(English); }
		set { CorrectMessage.text(English) = value; }
	}

	public clsProductValidation()
	{
	}

	public clsProductValidation(string MessageType, string OptType, string ValidationType, string Seveirty, string CheckAttribute, string DependantOptType, string Message, string DependantCheckAttribute, int RequiredQuantity, string DependantCheckAttributeValue,
	string CheckAttributeValue, string OptionFamily, string SystemType, string CorrectMessage, string LinkOptType, string LinkTechnology, string LinkOptionFamily)
	{
		this.ValidationMessageType = Enum.Parse(typeof(enumValidationMessageType), MessageType);
		this.ValidationType = Enum.Parse(typeof(enumValidationType), ValidationType);
		this.Severity = Enum.Parse(typeof(EnumValidationSeverity), Seveirty);
		this.CheckAttribute = CheckAttribute;
		this.DependantOptType = DependantOptType;
		this.RequiredOptType = OptType;
		this.RequiredQuantity = RequiredQuantity;
		this.DependantCheckAttribute = DependantCheckAttribute;
		int nextKey;
		this.Message = iq.AddTranslation(Message, English, "", 0, null, nextKey, false);
		this.DependantCheckAttributeValue = DependantCheckAttributeValue;
		this.CheckAttributeValue = CheckAttributeValue;
		this.OptionFamily = OptionFamily;
		this.CorrectMessage = iq.AddTranslation(CorrectMessage, English, "", 0, null, nextKey, false);
		iq.ProductValidationsAssignment(SystemType).Add(this);
		this.LinkOptType = LinkOptType;
		this.LinkTechnology = LinkTechnology;
		this.LinkOptionFamily = LinkOptionFamily;

		object sql;

		sql = "INSERT INTO ProductValidations (ValidationMessageType,OptionFamily,OptType,ValidationType,Severity,CheckAttribute,DependantOptType,RequiredQuantity,DependantCheckAttribute,DependantCheckAttributeValue,CheckAttributeValue,FK_Translation_Key_Message,FK_Translation_Key_CorrectMessage) VALUES (" + da.SqlEncode(this.ValidationMessageType.ToString) + "," + da.SqlEncode(this.OptionFamily) + "," + dataAccess.da.SqlEncode(this.RequiredOptType) + "," + dataAccess.da.SqlEncode(this.ValidationType.ToString()) + "," + dataAccess.da.SqlEncode(this.Severity.ToString()) + "," + dataAccess.da.SqlEncode(this.CheckAttribute) + "," + dataAccess.da.SqlEncode(this.DependantOptType) + "," + this.RequiredQuantity.ToString() + "," + da.SqlEncode(this.DependantCheckAttribute) + "," + da.SqlEncode(this.DependantCheckAttributeValue) + "," + da.SqlEncode(this.CheckAttributeValue) + "," + this.Message.Key.ToString() + "," + this.CorrectMessage.Key.ToString() + ")";
		int d = dataAccess.da.DBExecutesql(sql, true);
		this.ID = d;

		sql = "INSERT INTO ProductValidationMappings VALUES (" + dataAccess.da.SqlEncode(SystemType) + "," + d.ToString() + ")";
		dataAccess.da.DBExecutesql(sql);


	}


	public void Update()
	{
		object sql;
		sql = "UPDATE ProductValidations SET ValidationMessageType=" + da.SqlEncode(this.ValidationMessageType.ToString) + ", OptionFamily=" + da.SqlEncode(this.OptionFamily) + ",DependantCheckAttributeValue=" + da.SqlEncode(this.DependantCheckAttributeValue) + ",CheckAttributeValue=" + da.SqlEncode(this.CheckAttributeValue) + ",DependantCheckAttribute=" + da.SqlEncode(this.DependantCheckAttribute) + ",RequiredQuantity=" + this.RequiredQuantity.ToString() + ",OptType=" + dataAccess.da.SqlEncode(this.RequiredOptType) + ", ValidationType=" + dataAccess.da.SqlEncode(this.ValidationType.ToString()) + ", Severity=" + dataAccess.da.SqlEncode(this.Severity.ToString()) + ",CheckAttribute=" + dataAccess.da.SqlEncode(this.CheckAttribute) + ", DependantOptType=" + dataAccess.da.SqlEncode(this.DependantOptType) + ",LinkTechnology=" + dataAccess.da.SqlEncode(this.LinkTechnology) + ",LinkOptTYpe=" + dataAccess.da.SqlEncode(this.LinkOptType) + ",LinkOptionFamily=" + dataAccess.da.SqlEncode(this.LinkOptionFamily) + " WHERE Id=" + this.ID.ToString();
		dataAccess.da.DBExecutesql(sql);

		sql = "UPDATE Translation SET Text=" + da.SqlEncode(this.MessageText) + " WHERE [Key]=" + this.Message.Key.ToString();
		dataAccess.da.DBExecutesql(sql);

		if (this.CorrectMessage != null) {
			sql = "UPDATE Translation SET Text=" + da.SqlEncode(this.CorrectMessageText) + " WHERE [Key]=" + this.CorrectMessage.Key.ToString();
			dataAccess.da.DBExecutesql(sql);
		}
	}

	public void Delete(string sys)
	{
		object sql;
		sql = "DELETE FROM ProductValidationMappings WHERE FK_ProductValidation_Id=" + this.ID.ToString();
		dataAccess.da.DBExecutesql(sql);

		sql = "DELETE FROM ProductValidations WHERE Id=" + this.ID.ToString();
		dataAccess.da.DBExecutesql(sql);

		iq.ProductValidationsAssignment(sys).RemoveAll(v => v.ID == this.ID);

	}
}

public enum enumValidationType
{
	Slot,
	MustHave,
	NotToppedUp,
	Dependancy,
	Mismatch,
	CapacityOverload,
	MultipleRequred,
	UpperWarning,
	Exists,
	MustHaveProperty,
	AtLeastSameQuantity,
	Divisible,
	SpecRequirement
}

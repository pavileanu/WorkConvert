using dataAccess;
using System.Xml;

public class clsProductAttribute : i_Editable
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private clsProduct Product {
		get { return m_Product; }
		set { m_Product = Value; }
	}
	private clsProduct m_Product;
	private clsAttribute Attribute {
		get { return m_Attribute; }
		set { m_Attribute = Value; }
	}
	private clsAttribute m_Attribute;
	private float NumericValue {
		get { return m_NumericValue; }
		set { m_NumericValue = Value; }
	}
	private float m_NumericValue;
	private clsTranslation Translation {
		get { return m_Translation; }
		set { m_Translation = Value; }
	}
	private clsTranslation m_Translation;
	//    Property rawtext As String 'some things such as part numbers
	private clsUnit Unit {
		get { return m_Unit; }
		set { m_Unit = Value; }
	}
	private clsUnit m_Unit;
	//what units is the (numeric) value expressed in GB/TB/MB/Mhz/Ghz/feet/Pounds/Centigrade etc
	private bool deleted {
		get { return m_deleted; }
		set { m_deleted = Value; }
	}
	private bool m_deleted;

	string oCode;

	clsProduct oProduct;

	public void i_Editable.delete(ref List<string> errorMessages)
	{
		try {
			object sql;
			sql = "DELETE FROM [ProductAttribute] WHERE ID=" + this.ID;
			da.DBExecutesql(sql);

			this.Product.Attributes.Remove(this.ID);
			this.Product.i_Attributes_Code.Remove(this.Attribute.Code);


		} catch (System.Exception ex) {
			errorMessages.Add(ex.Message);
		}

	}


	public void writeXML(XmlTextWriter xw)
	{
		xw.WriteStartElement("productAttribute");
		xw.WriteStartAttribute("id");
		xw.WriteString(this.ID.ToString);
		xw.WriteEndAttribute();

		xw.WriteStartAttribute("code");
		xw.WriteString(this.Attribute.Code.ToString);
		xw.WriteEndAttribute();

		xw.WriteStartAttribute("numericValue");
		xw.WriteString(this.NumericValue.ToString);
		xw.WriteEndAttribute();

		if (this.Unit != null) {
			xw.WriteStartAttribute("unit");
			xw.WriteString(this.Unit.Code.ToString);
			xw.WriteEndAttribute();
		}

		if (this.Translation != null) {
			xw.WriteStartAttribute("text");
			xw.WriteString(this.Translation.text(English));
			xw.WriteEndAttribute();
		}

		xw.WriteEndElement();
		///productAttribute

		// Return String.Format("<productAttribute id='{0}' code='{1}'><numericValue>{2}</numericValue><units>{3}</units><text>{4}</text></productAttribute>", _
		// Me.ID, _
		// Me.Attribute.Code, _
		// Me.NumericValue, _
		// If(Me.Unit Is Nothing, "none", Me.Unit.Code), _
		// If(Me.Translation IsNot Nothing, xmlEncode(Me.Translation.text(English)), "")
		//         )

	}
	public string i_Editable.displayName(clsLanguage language)
	{

		if (!Translation == null) {
			displayName = this.Attribute.Code + " " + Translation.text(language);
		} else {
			displayName = this.Attribute.Code + " " + NumericValue + " ";
			if (!this.Unit == null) {
				if (!object.ReferenceEquals(Unit, iq.i_unit_code("txt")))
					displayName += this.Unit.DisplayName(language);
			}
		}

	}

	public string displayNameNoCode(clsLanguage language)
	{

		if (!Translation == null) {
			displayNameNoCode = Translation.text(language);
		} else {
			displayNameNoCode = NumericValue + " ";
			if (!this.Unit == null) {
				if (!object.ReferenceEquals(Unit, iq.i_unit_code("txt")))
					displayNameNoCode += this.Unit.DisplayName(language);
			}
		}

	}


	public void i_Editable.update(ref List<string> errormessages)
	{
		object sql;

		sql = "UPDATE [ProductAttribute] SET fk_product_id=" + this.Product.ID + ",";
		sql += "fk_attribute_id=" + this.Attribute.ID + ",";
		sql += "numericvalue=" + this.NumericValue + ",";
		sql += "fk_unit_id=" + this.Unit.ID + ",";
		sql += "fk_translation_key_text=" + TranslationKey(this.Translation);
		sql += ",deleted=" + IIf(this.deleted, 1, 0);

		sql += " WHERE ID=" + this.ID;

		da.DBExecutesql(sql);

		//update the index and dictionary in the parent product (of this productAttribute) (in case we changed the code, or the product .. ie, reparented this productAttribute)
		this.Product.i_Attributes_Code.Remove(this.oCode);
		if (!this.Product.i_Attributes_Code.ContainsKey(this.Attribute.Code)) {
			this.Product.i_Attributes_Code.Add(this.Attribute.Code, new List<clsProductAttribute>());
		}
		this.Product.i_Attributes_Code(this.Attribute.Code).Add(this);
		this.oCode = this.Attribute.Code;


	}

	public clsProductAttribute()
	{
	}

	public object i_Editable.Insert(ref List<string> errormessages)
	{
		return new clsProductAttribute(this.Product, this.Attribute, this.NumericValue, this.Unit, this.Translation);

	}



	public clsProductAttribute(clsProduct Product, string attCode, float numericValue, string unitCode, string EnglishText, DataTable pawc, DataTable tlwc, ref int tlkey)
	{
		//additional ovload contstructor for *ProductAttribute*  for cleaner code - looks up the attribute and created the translation internally
		//constructor chaining . .  (calls the (other) constructor with the matching signature)
		this.New(Product, iq.i_attribute_code(attCode), numericValue, iq.i_unit_code(unitCode), IIf(EnglishText == "", null, iq.AddTranslation(EnglishText, English, "PA-" + attCode, 0, tlwc, tlkey, false)), pawc);

	}


	public clsProductAttribute(clsProduct Product, clsAttribute Attribute, float NumericValue, clsUnit Unit, clsTranslation Translation, DataTable pawc = null, bool InMemoryOnly = false)
	{
		this.Product = Product;
		this.Attribute = Attribute;
		this.NumericValue = NumericValue;
		this.Translation = Translation;
		this.Unit = Unit;
		//   Me.ShowUnderBranches  = ShowUnder 'Comma seperated list of branchID 
		//   Me.HideUnderBranches = HideUnder

		//A Product *CAN* have more than one attribute of the same type
		if (!this.Product.i_Attributes_Code.ContainsKey(this.Attribute.Code)) {
			this.Product.i_Attributes_Code.Add(this.Attribute.Code, new List<clsProductAttribute>());
		}

		// Me.Product.i_Attributes_Code(Me.Attribute.Code).Add(Me)

		if (!InMemoryOnly) {
			if (pawc == null) {
				object sql;
				sql = "INSERT INTO PRODUCTATTRIBUTE (FK_Product_id,FK_Attribute_id,NumericValue,FK_Translation_key_Text,fk_Unit_id) ";
				sql += " values (" + this.Product.ID + "," + this.Attribute.ID + "," + this.NumericValue.ToString + "," + TranslationKey(this.Translation) + "," + this.Unit.ID + ");";

				this.ID = da.DBExecutesql(sql, true);


			} else {
				System.Data.DataRow row;
				row = pawc.NewRow();
				row("FK_product_id") = Product.ID;
				row("FK_attribute_id") = Attribute.ID;
				row("numericvalue") = NumericValue;
				if (this.Translation == null) {
					row("fk_translation_key_text") = DBNull.Value;
				} else {
					row("fk_translation_key_text") = this.Translation.Key;
				}

				row("fk_unit_ID") = Unit.ID;
				row("deleted") = false;
				pawc.Rows.Add(row);

			}
		}

		if (Attribute.Code.ToLower == "mfrsku")
			System.Diagnostics.Debugger.Break();

		//add this attribute to the product we're making it against
		if (!this.ID == 0) {
			Product.Attributes.Add(this.ID, this);
		}

		//Index products by attribute (for attribute finder speed) - ML
		if (!this.Attribute.Products.ContainsKey(this.Product.ID))
			this.Attribute.Products.Add(this.Product.ID, this.Product);

		//add this attribute to the product we're making it against
		if (!Product.i_Attributes_Code.ContainsKey(this.Attribute.Code)) {
			Product.i_Attributes_Code.Add(this.Attribute.Code, new List<clsProductAttribute>());
		}

		Product.i_Attributes_Code(Attribute.Code).Add(this);

		this.oProduct = this.Product;
		this.oCode = this.Attribute.Code;

	}

	public clsProductAttribute Clone(clsProduct ontoProduct)
	{

		//returns an independent copy of the clsProductAttribute attached instead to Product - with an independent copy of the translation
		clsTranslation tl = null;
		if (this.Translation != null)
			tl = this.Translation.clone;
		Clone = new clsProductAttribute(ontoProduct, this.Attribute, this.NumericValue, this.Unit, tl);

	}


	public clsProductAttribute(int id, clsProduct Product, clsAttribute Attribute, float NumericValue, clsUnit Unit, clsTranslation Translation)
	{
		this.ID = id;
		this.Product = Product;
		this.Attribute = Attribute;
		this.NumericValue = NumericValue;
		this.Translation = Translation;
		this.Unit = Unit;

		//Backward compatability for SKU attribute
		//If Me.Product.SKU = "" And Me.Attribute.Code.ToLower = "mfrsku" Then
		//    Product.SKU = Translation.text(English)
		//    If Not iq.i_SKU.ContainsKey(Product.SKU) Then 'this is UGLY - but San Switches, Tape dirves etc have two products at the mo
		//        iq.i_SKU.Add(Product.SKU, Product)
		//    End If
		//End If


		//If Attribute.Code.ToLower = "mfrsku" Then Stop

		//*OBSOLETED* - Sku is not a native property of the product - we should NOT be making mfrSku Product attributes any more
		//'Add this SKU to the SKU-product index
		//If Attribute.Code.ToLower = "MfrSKU".ToLower Then
		//    Dim sku$

		//    If Product.SKU = "" Then Stop
		//    sku$ = Me.Translation.text(English)
		//    If Not iq.i_SKU.ContainsKey(sku$) Then
		//        'product hasn't (previously) had its sku set
		//        iq.i_SKU.Add(sku$, Product)
		//    End If
		//End If


		//add this attribute to the product we're making it against
		if (!Product.i_Attributes_Code.ContainsKey(this.Attribute.Code)) {
			Product.i_Attributes_Code.Add(this.Attribute.Code, new List<clsProductAttribute>());
		}

		Product.i_Attributes_Code(Attribute.Code).Add(this);
		Product.Attributes.Add(this.ID, this);

		//Index products by attribute (for attribute finder speed) - ML
		if (!this.Attribute.Products.ContainsKey(this.Product.ID))
			this.Attribute.Products.Add(this.Product.ID, this.Product);


		this.oProduct = Product;
		this.oCode = this.Attribute.Code;


	}


}

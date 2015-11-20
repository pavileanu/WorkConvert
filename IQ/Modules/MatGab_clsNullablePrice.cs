public class NullablePrice
{

		//Object
	public decimal value;
	public readonly clsCurrency currency;
		//Confirmed Price = false this price is poa   ... this should probably become a datestamp/source combo
	public bool isValid;
		//Price is - or contains a HP list price element
	public bool isList;
		//used to alter tooltips to say this price *Includes* HP LIst price elements
	public bool isTotal;

	//Public SKUVariant As clsVariant

	public string Message;
	public static NullablePrice operator *(NullablePrice P, float margin)
	{

		NullablePrice newprice = new NullablePrice(P.value * margin, P.currency, P.isList);
		newprice.isValid = P.isValid;
		//! very important - otherwise Invalid prices become valid 0 prices when multiplied by a margin !
		newprice.isList = P.isList;
		return newprice;

	}

	public static NullablePrice operator +(NullablePrice p1, NullablePrice p2)
	{

		NullablePrice newprice = new NullablePrice(p1.value + p2.value, p1.currency, p1.isList | p2.isList);

		if ((!p1.isValid) | (!p2.isValid))
			newprice.isValid = false;
		newprice.isTotal = true;

		return newprice;

	}


	public static NullablePrice operator -(NullablePrice p1, NullablePrice p2)
	{

		NullablePrice newprice = new NullablePrice(p1.value - p2.value, p1.currency, p1.isList | p2.isList);

		if ((!p1.isValid) | (!p2.isValid))
			newprice.isValid = false;
		newprice.isTotal = true;

		return newprice;


	}



	public NullablePrice(clsCurrency currency)
	{
		this.value = 0;
		//-9999999.4
		this.isValid = false;
		this.isList = false;
		this.currency = currency;

	}


	//, SKUvariant As clsVariant)
	public NullablePrice(object value, clsCurrency currency, bool isList)
	{

		if (value == null) {
			Beep();
			// please use the paramaterless constructor e.g. New NullableInt() - to create a 'POA' (invalid) price
		} else if (object.ReferenceEquals(value, DBNull.Value)) {
			Beep();
			// please use the paramaterless constructor e.g. New NullableInt() - to create a 'POA' (invalid) price
		}

		this.currency = currency;

		//TypeOf (value) Is Single Or TypeOf (value) Is Double Or TypeOf (value) Is Decimal Then
		if ((value) is decimal | (value) is double | (value) is float) {

			this.value = (decimal)value;
			this.isValid = true;
			this.isList = isList;
		} else {
			Beep();
		}

	}


	//Pair of small helper functions to make the main code more readable elsewhere
	public object isDifferentFrom(nullablePrice p)
	{

		return !isSameAs(p);

	}

	public bool isSameAs(NullablePrice p)
	{
		//We *could* have overloaded the equals operator - but i think that's potentially confusing

		if (p.isValid != !this.isValid)
			return false;

		if (this.value == p.value)
			return true;
		else
			return false;

	}

	private Panel DisplayPrice(clsAccount buyerAccount, ref List<string> errorMessages)
	{
		//Label

		DisplayPrice = new Panel();
		//Label - labels WOULD NOT accept a cssclass !

		//Prices containing listprice elements can be (are often 'invalid')
		if (!this.isValid & !this.isList) {
			// If Me.isList Then Stop
			DisplayPrice.Controls.Add(NewLit(Xlt(" ...", buyerAccount.Language)));
			DisplayPrice.CssClass = "POA";
			DisplayPrice.ToolTip = this.Message;
		} else {
			DisplayPrice.Controls.Add(NewLit(this.text(buyerAccount, errorMessages)));

			if (this.isList) {
				if (this.isTotal) {
					DisplayPrice.ToolTip = Xlt("contains HP List Price element(s)", buyerAccount.Language);
				} else {
					DisplayPrice.ToolTip = Xlt("HP List Price", buyerAccount.Language);
				}

				DisplayPrice.CssClass += " listPrice";
			} else {
				DisplayPrice.ToolTip = Xlt("Confirmed Price", buyerAccount.Language);

			}

		}

	}


	public string text(clsAccount buyeraccount, ref List<string> errormessages)
	{

		text = this.NumericValue == 0 ? "..." : this.currency.format(this.NumericValue, buyeraccount.Culture.Code, errormessages);
		if (this.isList)
			text += "&nbsp;*";
		//Add a * if it's a list price (or contains a list price element)

	}

	public decimal NumericValue()
	{
		if (IsDBNull(this.value)) {
			return 0;
		} else {
			return this.value;
		}

	}

	public string sqlvalue()
	{

		if (IsDBNull(this.value)) {
			return "null";
		} else {
			return this.value;
		}

	}

	public override string ToString()
	{

		if (IsDBNull(this.value)) {
			return "Null";
		} else {
			return System.Convert.ToString(this.value);
		}

	}

}


using dataAccess;
using System.Runtime.Serialization;

public class NullableDate
{


	public object value;
	public NullableDate()
	{
		this.value = DBNull.Value;
	}

	public NullableDate(object v)
	{
		if (IsDBNull(v)) {
			this.value = DBNull.Value;
		} else {
			this.value = v.ToString;
		}
	}

	public string DisplayValue()
	{
		if (IsDBNull(this.value)) {
			return "-";
		} else {
			return this.value.ToString;
		}

	}
	public string sqlValue()
	{

		if (IsDBNull(this.value)) {
			return "null";
		} else {
			return da.UniversalDate(this.value);
		}

	}

}

[DataContract()]
[System.Runtime.Serialization.KnownType(typeof(nullableString))]
public class nullableString
{
	[DataMember()]
	public object v {
		get { return object.ReferenceEquals(value, DBNull.Value) ? null : value; }
		set { this.value = value == null ? DBNull.Value : value; }
	}

	public object value;
	public nullableString()
	{
		this.value = DBNull.Value;
	}

	public nullableString(object v)
	{
		if (IsDBNull(v)) {
			this.value = DBNull.Value;
		} else {
			this.value = v.ToString;
		}
	}

	public string DisplayValue()
	{
		if (IsDBNull(this.value)) {
			return "-";
		} else {
			return this.value.ToString;
		}

	}
	public string sqlValue()
	{

		if (IsDBNull(this.value)) {
			return "null";
		} else {
			return da.SqlEncode(this.value.ToString);
		}

	}


}


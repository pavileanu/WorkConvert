
public class clsBand
{
	public Int64 min;
	public Int64 max;
		//The number of values falling in this band
	public int Survivors;

	public clsBand(Int64 min, Int64 max, int Survivors)
	{
		this.min = min;
		this.max = max;
		this.Survivors = Survivors;
	}

	public bool contains(Int64 numericValue)
	{
		contains = false;
		if (numericValue >= this.min & numericValue <= this.max) {
			contains = true;
		}
	}


	public void Stretch()
	{
		//do some rounding and overlapping here on the Max/Min

		this.min = (Int64)Math.Ceiling(this.min / 1000) * 1000;
		this.max = (Int64)Math.Floor(this.max / 1000) * 1000;


	}

	/// <summary>
	/// Compares the Min and Max of this band to those values specified for the LE and GE filters (in the dictionary, for the field provided - to determin with this band is the currently selected one
	/// </summary>
	/// <param name="fld"></param>
	/// <param name="filters"></param>
	/// <returns></returns>
	/// <remarks></remarks>
	public bool isSelected(clsField fld, Dictionary<clsField, Dictionary<clsFilter, List<Int64>>> filters)
	{

		isSelected = false;

		if (filters.ContainsKey(fld)) {
			clsFilter ge = iq.i_Filters_Code("GE");
			clsFilter le = iq.i_Filters_Code("LE");


				//If .Item(ge).First() = Me.min And .Item(le).First() = Me.max Then Return True 'ML - have guessed here that there will only be one min and one max, will need to enforce that this is the case - TODO
			 // ERROR: Not supported in C#: WithStatement

		}
	}

	public override bool Equals(object obj)
	{
		return max == obj.max && min == obj.min;
	}

	public override int GetHashCode()
	{
		return max.GetHashCode() + min.GetHashCode();
	}

}


public class clsBand
{
    public long min;
    public long max;
    public int Survivors; //The number of values falling in this band

    public clsBand(long min, long max, int Survivors)
    {
        this.min = min;
        this.max = max;
        this.Survivors = Survivors;
    }

    public bool contains(long numericValue)
    {
        bool returnValue = false;
        returnValue = false;
        if (numericValue >= this.min & numericValue <= this.max)
        {
            returnValue = true;
        }
        return returnValue;
    }

    public void Stretch()
    {

        //do some rounding and overlapping here on the Max/Min

        this.min = System.Convert.ToInt64(System.Convert.ToInt32(Math.Ceiling((double)this.min / 1000)) * 1000);
        this.max = System.Convert.ToInt64(System.Convert.ToInt32(Math.Floor((double)this.max / 1000)) * 1000);


    }

    /// <summary>
    /// Compares the Min and Max of this band to those values specified for the LE and GE filters (in the dictionary, for the field provided - to determin with this band is the currently selected one
    /// </summary>
    /// <param name="fld"></param>
    /// <param name="filters"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    public bool isSelected(clsField fld, Dictionary<clsField, Dictionary<clsFilter, List<long>>> filters)
    {
        bool returnValue = false;

        returnValue = false;

        if (filters.ContainsKey(fld))
        {
            clsFilter ge = iq.i_Filters_Code("GE");
            clsFilter le = iq.i_Filters_Code("LE");

            Dictionary with_1 = filters(fld);

            if (with_1.ContainsKey(ge))
            {
                if (with_1.ContainsKey(le))
                {
                    object mi = with_1.Item(ge).IndexOf(this.min);
                    if ((int)mi == -1)
                    {
                        return false;
                    }
                    if (with_1.Item(le)[mi] == this.max)
                    {
                        return true;
                    }
                    //If .Item(ge).First() = Me.min And .Item(le).First() = Me.max Then Return True 'ML - have guessed here that there will only be one min and one max, will need to enforce that this is the case - TODO
                }
            }
        }
        return returnValue;
    }

    public bool Equals(clsBand obj)
    {
        return max == (ulong)obj.max && min == obj.min;
    }

    public override int GetHashCode()
    {
        return max.GetHashCode() + min.GetHashCode();
    }

}
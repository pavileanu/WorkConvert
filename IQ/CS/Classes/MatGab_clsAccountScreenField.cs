public class clsAccountScreenField
{
	public int AccountID;
	public int ScreenID;
	public string Path;
	public int FieldId;
	public int Order;
	public int Width;
	public bool Visibility;
	public string Sort;
	public string Filter;
	public clsTranslation Description;
	public bool PromoColumn;
	public string LabelText {
		get { return Description.text(English); }
	}


	private float? _grownWidth;
	public float GrownWidth {
		get {
			if (_grownWidth.HasValue)
				return _grownWidth.Value;
			else
				return Width;
		}
		set {
			if (value == 0)
				_grownWidth = null;
			else
				_grownWidth = value;
		}
	}

	public bool HasScreenOverride {
		get { return iq.ScreenOverrides.ToList().Where(a => a.AccountID == AccountID & a.FieldId == FieldId & a.ScreenID == ScreenID & a.Path == Path).Count() > 0; }
	}

	public clsUnit DisplayUnit;
	public string DisplayUnitSymbol {
		get { return DisplayUnit == null ? string.Empty : DisplayUnit.Symbol; }
	}



	public object ConvertValueToUnit(long value, int? origialUnit)
	{
		if (DisplayUnit == null)
			return value;
		if (origialUnit == null)
			return value;
		if (!iq.Conversions.ContainsKey(origialUnit))
			return value;
		if (!iq.Conversions(origialUnit).ContainsKey(DisplayUnit.ID))
			return value;

		return value * iq.Conversions(origialUnit)(DisplayUnit.ID);
	}

}

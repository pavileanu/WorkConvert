using System.Globalization;
using IQ.clsScreenHeader;
using System.IO;
using System.Threading;


public class clsScreenHeader
{
	public clsScreen screen {
		get { return m_screen; }
		set { m_screen = Value; }
	}
	private clsScreen m_screen;

	public bool United {
		get { return m_United; }
		set { m_United = Value; }
	}
	private bool m_United;
	public Dictionary<clsField, Dictionary<clsFilter, List<Int64>>> Filters = null;
		//The priority is the key (makes sense, honest)
	public Dictionary<int, clsPriorityDirection> sorts;
	public Dictionary<clsField, enumColState> ColState;
	public System.Data.DataView Vw;
	public bool QuickFiltersVisible;
	private UInt64 lid;

	public string Path;
		//Holds the surviving count (distinct) translations for this Quick filter - populated my addmissingcolumns
	private Dictionary<clsField, Dictionary<clsTranslation, int>> dicTrans;
		//QuickFilter fields of type BANDS a
	private Dictionary<clsField, List<clsBand>> dicBands;
		//All the DISTINCT numeric values (and the survivor counts thereof)
	private Dictionary<clsField, Dictionary<Int64, int>> dicNums;
		//for each (numeric) field detect and validate the UNITS
	private Dictionary<clsField, clsUnit> dicUnits;


	private Dictionary<clsBranch, clsVisibility> descendants;
	private Dictionary<clsField, clsAccountScreenField> _FieldResultSet;
	public Dictionary<clsField, clsAccountScreenField> FieldResultSet {
		get {
			if (_FieldResultSet == null) {
				clsPromo pml = null;
				List<string> otherPromo = new List<string>();
				if (iq.seshDic(lid).ContainsKey("promoinforce") && !{
					"K",
					"I"
				}.Contains(iq.Branches(Split(this.Path, ".").Last).rca)) {
					pml = iq.Promos(iq.sesh(lid, "promoinforce"));
				}
				clsAccount l = iq.sesh(lid, "BuyerAccount");

				if (iq.i_PromoRegions.ContainsKey(l.BuyerChannel.Region)) {
					string t = "";
					foreach ( promo in iq.i_PromoRegions(l.BuyerChannel.Region)) {
						otherPromo.Add(promo.FieldProperty_Filter);
					}


				}

				if (pml != null)
					otherPromo.Remove(pml.FieldProperty_Filter);


				List<string> errormessages = new List<string>();
				//TODO add default display unit here
				IEnumerable<clsScreenOverride> asa = iq.ScreenOverrides.Where(so => so.AccountID == l.ID & so.ScreenID == this.screen.ID & so.Path == this.Path).Select(dd => dd);
				object screenOverrideObjects = from s in iq.Screens(this.screen.ID).Fields.Values.Where(fld => (pml != null && fld.propertyName == pml.FieldProperty_Filter) || (otherPromo != null && otherPromo.Contains(fld.propertyName)) || fld.ValidRegions.Count == 0 || fld.ValidRegions.Where(vr => vr.Value.Encompasses(l.BuyerChannel.Region)).Count > 0);
				Group();
				switch (new {
					a = new clsAccountScreenField {
						AccountID = l.ID,
						ScreenID = this.screen.ID,
						Path = Path,
						FieldId = s.ID,
						Visibility = (pml != null && s.propertyName == pml.FieldProperty_Filter) || (otherPromo != null && otherPromo.Contains(s.propertyName)) ? true : m == null || m.ForceVisibilityTo == null ? s.visibleList : m.ForceVisibilityTo,
						Order = m == null || m.ForceOrderTo == null ? s.order : m.ForceOrderTo,
						Width = m == null || m.ForceWidthTo == null ? s.width == 0 ? null : s.width : m.ForceWidthTo,
						Description = s.labelText,
						DisplayUnit = m == null || m.DisplayUnit == null ? null : m.DisplayUnit,
						PromoColumn = pml != null && s.propertyName == pml.FieldProperty_Filter ? true : false
					},
					b = s
				}) {
				}


				_FieldResultSet = screenOverrideObjects.ToDictionary(a => a.b, a => a.a);
			} else {
				_FieldResultSet = screenOverrideObjects.Where(f => f.b.CanUserSelect).ToDictionary(a => a.b, a => a.a);
			}
		}
	}
}

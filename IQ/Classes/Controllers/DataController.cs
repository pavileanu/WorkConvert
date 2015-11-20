using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Linq;
using System.Data.SqlClient;
using System.Reflection;



public class DataController : ApiController
{

    #region CustomizableField
    
    [ActionName("GetAvailableFields")]
    [HttpPost]
    public List<clsAccountScreenField> GetAvailableFields(clsGenericAjaxRequest request)
    {
        //Get a list of all fields from the screen and populate with any overrides for this user

        Dictionary<string, clsScreenHeader> l = iq.sesh(request.lid, "screenHeaders");
        return l[request.BranchPath].AllAvailableFields;
    }

    [HttpPost]
    [ActionName("SetFieldOverride")]
    public string SetFieldOverride(SetFieldOverrideRequest request)
    {
        //Set the override settings for this users account and screen, update OM and DB

        clsAccount l = iq.sesh(request.lid, "BuyerAccount");
        clsScreenOverride scr = iq.ScreenOverrides.Where(so => so.AccountID == l.ID && so.ScreenID == request.ScreenId && so.FieldId == request.FieldId && so.Path == request.BranchPath).FirstOrDefault();
        if (scr == null)
        {
            scr = new clsScreenOverride(l.ID, request.ScreenId, request.BranchPath, request.FieldId, request.ForceVisibilityTo, request.ForceOrderTo, request.ForceWidthTo, request.ForceSortTo, request.ForceFilterTo, null);
            scr.Insert();
        }

        if (request.ForceVisibilityTo != null)
        {
            scr.ForceVisibilityTo = request.ForceVisibilityTo;
        }
        if (request.ForceOrderTo != null)
        {
            scr.ForceOrderTo = request.ForceOrderTo;
        }
        if (request.ForceWidthTo != null)
        {
            scr.ForceWidthTo = request.ForceWidthTo;
        }
        if (request.ForceSortTo != null)
        {
            scr.ForceSortTo = request.ForceSortTo;
        }
        if (request.ForceFilterTo != null)
        {
            scr.ForceFilterTo = request.ForceFilterTo; // this is Not yet implemented in the GUI as not spec'd, just putting it in for when it is...
        }
        if (!scr.Update())
        {
            return null;
        }

        clsBranchInfo bi = new clsBranchInfo(request.lid, request.BranchPath, null, 0, enumParadigm.errorNotSet, null);
        bi.InvalidateMatrixBelow(request.BranchPath, true);

        return request.BranchPath;

    }

    [HttpPost]
    [ActionName("CloneTargets")]
    public string CloneTargets(CloneTargetsRequest request)
    {
        //Get the source screen
        List<string> errorMessages = new List<string>();
        clsScreen scOrig = iq.Screens(request.ScreenId);
        object bi = null;

        if (request.Targets.Count > 0)
        {
            foreach (var targ in request.Targets)
            {
                if (Strings.Len(targ) > 2 && Strings.Right(System.Convert.ToString(targ), 3) == "All")
                {
                    continue;
                }
                bi = new clsBranchInfo(request.lid, targ, null, 0, enumParadigm.errorNotSet, errorMessages);
                bi.Branch.Matrix = scOrig;
                bi.Branch.Update(errorMessages);
            }
        }
        else
        {
            //Get all branches with this level and value
            foreach (var br in iq.Branches.Where(b => b.Value.Translation.Group == request.Level && b.Value.Translation.text(English) == request.LevelValue).ToList())
            {
                br.Value.Matrix = scOrig;
                br.Value.Update(errorMessages);
            }

        }
        return string.Join(",", errorMessages);
    }

    [HttpPost]
    [ActionName("ResetFieldOverride")]
    public string ResetFieldOverride(clsGenericAjaxRequest request)
    {
        //Resets (removes) any overrides at this level
        Dictionary<string, clsScreenHeader> matrixHeaders = iq.sesh(request.lid, "matrixHeaders");
        clsScreenHeader matrixHeader = matrixHeaders[request.BranchPath];
        clsAccount l = iq.sesh(request.lid, "BuyerAccount");
        iq.ScreenOverrides.RemoveAll(so => so.AccountID == l.ID && so.ScreenID == request.ScreenId && so.Path == request.BranchPath);

        matrixHeader.InvalidateFields();

        if (dataAccess.da.DBExecutesql("DELETE FROM AccountScreenOverride WHERE FK_Account_Id = " + l.ID + " AND FK_Screen_ID=" + request.ScreenId + " AND Path = " + dataAccess.da.SqlEncode(request.BranchPath)) <= 0)
        {
            return null;
        }

        return request.BranchPath;
    }

    [HttpPost]
    [ActionName("SwitchOverrideFieldOrder")]
    public string SwitchOverrideFieldOrder(clsGenericAjaxRequest request)
    {

        //Find current dest field order
        Dictionary<string, clsScreenHeader> matrixHeaders = iq.sesh(request.lid, "screenHeaders");

        if (matrixHeaders == null)
        {
            return null;
        }
        clsScreenHeader matrixHeader = matrixHeaders[request.BranchPath];
        clsField destField = matrixHeader.EffectiveFields.Where(f => f.ID == request.DestinationFieldId).FirstOrDefault();
        clsField sourceField = matrixHeader.EffectiveFields.Where(f => f.ID == request.SourceFieldId).FirstOrDefault();
        clsAccount l = iq.sesh(request.lid, "BuyerAccount");

        if (destField == null || sourceField == null)
        {
            return null;
        }

        //Move all fields below this one down, this does mean creating an override for every field, can't think of another way at the moment...
        int ord = System.Convert.ToInt32(matrixHeader.FieldResultSet(destField).Order + 1);
        foreach (var v in matrixHeader.FieldResultSet.Where(a => a.Value.Order >= matrixHeader.FieldResultSet(destField).Order).OrderBy(a => a.Value.Order).ToList())
        {
            if (v.Value.HasScreenOverride)
            {
                object s = iq.ScreenOverrides.Where(f => f.AccountID == l.ID && f.ScreenID == request.ScreenId && f.Path == request.BranchPath && f.FieldId == v.Value.FieldId).FirstOrDefault();
                s.ForceOrderTo = ord;
                s.Update();
            }
            else
            {
                clsScreenOverride dd = new clsScreenOverride(l.ID, request.ScreenId, request.BranchPath, v.Value.FieldId, null, ord, null, null, null, null);
                dd.Insert();
            }
            ord++;
        }
        if (matrixHeader.FieldResultSet(sourceField).HasScreenOverride)
        {
            object s = iq.ScreenOverrides.Where(f => f.AccountID == l.ID && f.ScreenID == request.ScreenId && f.Path == request.BranchPath && f.FieldId == sourceField.ID).FirstOrDefault();
            s.ForceOrderTo = matrixHeader.FieldResultSet(destField).Order;
            s.Update();
        }
        else
        {
            clsScreenOverride dd = new clsScreenOverride(l.ID, request.ScreenId, request.BranchPath, sourceField.ID, null, matrixHeader.FieldResultSet(destField).Order, null, null, null, null);
            dd.Insert();
        }

        matrixHeader.InvalidateFields();

        return request.BranchPath;

    }

    [HttpPost]
    [ActionName("GetClonableTargets")]
    public dynamic GetClonableTargets(clsGenericAjaxRequest request)
    {
        clsAccount l = iq.sesh(request.lid, "BuyerAccount");
        List<string> errorMessages = new List<string>();
        //Break this out to a utiltiy

        clsBranchInfo bi = new clsBranchInfo(request.lid, request.BranchPath, null, 0, enumParadigm.errorNotSet, errorMessages);
        int s = 0;
        int c = 0;

        return bi.visibleChildren(errorMessages, false, s, c, false, false).Select(h => h.Value).Select(dd => new { Path = request.BranchPath + "." + dd.branch.ID.ToString(), Name = dd.branch.DisplayName(l.Language) }).ToList();
    }

    [HttpPost]
    [ActionName("GetClonableGroups")]
    public dynamic GetClonableGroups(clsGenericAjaxRequest request)
    {
        clsAccount l = iq.sesh(request.lid, "BuyerAccount");
        //Break this out to a utiltiy

        return iq.Branches.Where(b => b.Value.Translation.Group.Contains("OL")).GroupBy(b => b.Value.Translation.Group).ToDictionary(b => b.Key, e => e.Select(d => d.Value.Translation.text(l.Language)).Distinct()); //.Select(Function(di) New clsCloneData With {.Level = di.Key, .Values = di.Value.ToList()})


    }

    [HttpPost]
    [ActionName("SetScreenDefaults")]
    public bool SetScreenDefaults(clsGenericAjaxRequest request)
    {
        //Get user logon info
        clsAccount l = iq.sesh(request.lid, "BuyerAccount");
        List<string> errorMessages = new List<string>();

        if (!AccountHasRight(request.lid, "GLOBALADM") && (iq.seshDic(request.lid).ContainsKey("ElevatedKey") && iq.sesh(request.lid, "ElevatedKey") != request.elid))
        {
            return false;
        }

        object scr = iq.ScreenOverrides.Where(so => so.AccountID == l.ID && so.ScreenID == request.ScreenId && so.Path == request.BranchPath);
        foreach (var screenfield in scr)
        {
            if (screenfield.ForceVisibilityTo != null)
            {
                iq.Screens(request.ScreenId).Fields(screenfield.FieldId).visibleList = screenfield.ForceVisibilityTo;
            }
            if (screenfield.ForceOrderTo != null)
            {
                iq.Screens(request.ScreenId).Fields(screenfield.FieldId).order = screenfield.ForceOrderTo;
            }
            if (screenfield.ForceWidthTo != null)
            {
                iq.Screens(request.ScreenId).Fields(screenfield.FieldId).width = screenfield.ForceWidthTo;
            }
            if (screenfield.ForceFilterTo != null)
            {
                iq.Screens(request.ScreenId).Fields(screenfield.FieldId).defaultFilter = screenfield.ForceFilterTo;
            }
            if (screenfield.ForceSortTo != null)
            {
                iq.Screens(request.ScreenId).Fields(screenfield.FieldId).defaultSort = screenfield.ForceSortTo;
            }
            iq.Screens(request.ScreenId).Fields(screenfield.FieldId).update(errorMessages);
        }
        return true;

    }

    [HttpPost]
    [ActionName("CreateUniqueVersion")]
    public string CreateUniqueVersion(clsGenericAjaxRequest request)
    {
        //Create a screen copy

        if (request.ScreenTitle != null && iq.i_screens_code.ContainsKey(request.ScreenTitle))
        {
            return "This Screen Name is in Use";
        }

        clsAccount l = iq.sesh(request.lid, "BuyerAccount");
        List<string> errorMessages = new List<string>();

        object scr = iq.ScreenOverrides.Where(so => so.AccountID == l.ID && so.ScreenID == request.ScreenId && so.Path == request.BranchPath);

        clsScreen scOrig = iq.Screens(request.ScreenId);
        object scTarget = scOrig.copy(errorMessages);
        if (request.ScreenTitle != null)
        {
            scTarget.title = request.ScreenTitle;
            scTarget.code = request.ScreenTitle;
        }

        foreach (clsField f in scTarget.Fields.Values)
        {
            object soo = scr.Where(s => s.FieldId == f.ID).FirstOrDefault();
            if (soo != null)
            {
                scTarget.Fields(f.ID).visibleList = soo.ForceVisibilityTo;
                scTarget.Fields(f.ID).width = soo.ForceWidthTo;
                scTarget.Fields(f.ID).order = soo.ForceOrderTo;
            }
        }

        scTarget.Update(errorMessages);

        clsBranchInfo bi = new clsBranchInfo(request.lid, request.BranchPath, null, 0, enumParadigm.errorNotSet, errorMessages);
        bi.branch.Matrix = scTarget;
        bi.branch.Update(errorMessages);

        AuditLog.Instance.Add(request.lid, "CreateUniqueVersion", errorMessages, "");
        if (errorMessages.Count != 0)
        {
            return string.Join(",", errorMessages);
        }
        return null;
    }

    [HttpPost]
    [ActionName("RemoveUniqueVersion")]
    public bool RemoveUniqueVersion(clsGenericAjaxRequest request)
    {
        clsAccount l = iq.sesh(request.lid, "BuyerAccount");
        List<string> errorMessages = new List<string>();

        clsBranchInfo bi = new clsBranchInfo(request.lid, request.BranchPath, null, 0, enumParadigm.errorNotSet, errorMessages);
        bi.branch.Matrix = null;
        bi.branch.Update(errorMessages);

        AuditLog.Instance.Add(request.lid, "CreateUniqueVersion", errorMessages, "");
        if (errorMessages.Count != 0)
        {
            return false;
        }
    }
    #endregion

    public dynamic GetSystemMaintenanceUpdate()
    {
        return clsIQ.messages.ToList();
    }

    [HttpPost]
    [ActionName("GetAvailableUndos")]
    public dynamic GetAvailableUndos(clsGenericAjaxRequest request)
    {
        List<string> undoableActions = new List<string>() { "graft", "prune" };

        clsAccount l = iq.sesh(request.lid, "BuyerAccount");
        SqlConnection con = dataAccess.da.OpenDatabase(true);
        DataTable dt = new DataTable();
        dt.Load(dataAccess.da.DBExecuteReader(con, "SELECT * FROM AuditLog WHERE action in (\'" + string.Join("\',\'", undoableActions) + "\') AND lid=" + request.lid.ToString()));

        DataRow[] arr = dt.Select();
        //dt.Rows.CopyTo(arr, 0)

        return arr.Select(a => new { DateTime = a("DateTime"), Id = a("Id"), Action = a("Action"), SourceBranch = a("SourcePath"), TargetBranch = a("TargetPath") });

    }

    [ActionName("UndoAction")]
    [HttpPost]
    public string UndoAction(clsGenericAjaxRequest request)
    {
        //Get Details from DB
        SqlConnection con = dataAccess.da.OpenDatabase(true);
        DataTable dt = new DataTable();
        dt.Load(dataAccess.da.DBExecuteReader(con, "SELECT * FROM AuditLog WHERE id=" + request.ActionId.ToString() + " AND lid=" + request.lid.ToString()));
        DataRow arr = dt.Select()[0];

        //Do stuff
        System.Reflection.Assembly ty = Assembly.GetExecutingAssembly().CreateInstance("IQ.clsManip" + arr["Action"].ToString().Substring(0, 1).ToUpper() + arr["Action"].ToString().Substring(1, arr["Action"].ToString().Length - 1));
        ty.TargetPath = arr["TargetPath"];
        int i;
        if (!int.TryParse(System.Convert.ToString(arr["SourcePath"].ToString()), out i))
        {
            ty.SourcePath = arr["SourcePath"];
        }
        ty.AuditId = request.ActionId;
        ty.LoginId = request.lid;

        ty.UndoAction();


        return string.Empty;
    }

    [ActionName("GetFilters")]
    [HttpPost]
    public dynamic GetFilters(clsGenericAjaxRequest request)
    {
        clsAccount l = iq.sesh(request.lid, "BuyerAccount");
        object f = iq.Screens(request.ScreenId).Fields.ToList().Select(fi => new { FieldName = fi.Value.labelText.text(l.Language), FieldId = fi.Value.ID, Order = fi.Value.order, Filter = (fi.Value.defaultFilter == null ? null : (fi.Value.defaultFilter.Split(","))), Translation = (fi.Value.QuickFilterGroup == null ? "" : (fi.Value.QuickFilterGroup.text(l.Language))), WidgetUI = fi.Value.QuickFilterUItype });
        object t = iq.Filters.Values.ToList().Select(fi => new { Value = fi.Code, Text = fi.DisplayText.text(l.Language) });
        return new { Filters = f, Types = t };
    }
    [ActionName("SetFilters")]
    [HttpPost]
    public string SetFilters(clsFilterSetRequest request)
    {
        object screen = iq.Screens(request.ScreenID);
        List<string> errorMessages = new List<string>();

        foreach (var f in request.Fields)
        {
            if (f.FieldId == 0)
            {
                continue;
            }
            if (f.Enabled)
            {
                screen.Fields(f.FieldId).defaultFilter = f.DefaultFilter;
                screen.Fields(f.FieldId).QuickFilterUItype = f.FilterType;

                if (!string.IsNullOrEmpty(System.Convert.ToString(f.TranslationGroup)))
                {
                    if (screen.Fields(f.FieldId).QuickFilterGroup != null)
                    {
                        screen.Fields(f.FieldId).QuickFilterGroup.Group = f.TranslationGroup;
                        screen.Fields(f.FieldId).QuickFilterGroup.Order = f.Order;
                    }
                    else
                    {
                        screen.Fields(f.FieldId).QuickFilterGroup = iq.AddTranslation(f.TranslationGroup, English, f.TranslationGroup, f.Order, null, 0, false);
                    }
                }
                else
                {
                    screen.Fields(f.FieldId).QuickFilterGroup = null;
                }
            }
            else
            {
                screen.Fields(f.FieldId).QuickFilterGroup = null;
            }
            screen.Fields(f.FieldId).update(errorMessages);
        }
        return string.Join(",", errorMessages);
    }

    [ActionName("AcknowledgeValidation")]
    [HttpPost]
    public string AcknowledgeValidation(clsGenericAjaxRequest request)
    {
        clsAccount l = iq.sesh(request.lid, "BuyerAccount");
        l.Quotes(request.QuoteId).AcknowledgedValidations.Add(request.BranchPath);

        return "";
    }

    [ActionName("GetLearnMoreText")]
    [HttpPost]
    public string GetLearnMoreText(clsGenericAjaxRequest request)
    {

        clsAccount buyerAccount = iq.sesh(request.lid, "BuyerAccount");

        string key = "learnMore";
        if (buyerAccount.Manufacturer == Manufacturer.HPE)
        {
            key += "HPE";
        }
        else if (buyerAccount.Manufacturer == Manufacturer.HPI)
        {
            key += "HPI";
        }

        return Xlt(key, buyerAccount.Language);

    }

    [ActionName("CarePackJIT")]
    [HttpPost]
    public List<string> GetCarePacks(clsGenericAjaxRequest request)
    {
        return CarePackModule.CarePackJIT(request);
    }

    [ActionName("HideSystemMessage")]
    [HttpPost]
    public void HideSystemMessage(clsGenericAjaxRequest request)
    {

        clsAccount agentAccount = iq.sesh(request.lid, "AgentAccount");
        System.Char suppressKey = string.Format("Suppress{0}SystemMessages", agentAccount.mfrCode);
        iq.sesh(request.lid, suppressKey) = "Y";

    }

}
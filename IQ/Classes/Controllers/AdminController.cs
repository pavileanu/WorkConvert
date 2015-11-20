using System.Net;
using System.Web.Http;
using Newtonsoft.Json;


public class AdminController : ApiController
{

    [HttpPost]
    public dynamic GetAuditTree(clsGenericAjaxRequest req)
    {
        if (iq.seshDic == null || iq.seshDic.Count == 0)
        {
            return null;
        }
        try
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            d.Add("lid", req.lid == 0 ? (string.Join(",", iq.seshDic.Keys)) : req.lid);
            d.Add("ParentId", req.ParentId == 0 ? DBNull.Value : req.ParentId);
            DataTable dt = new DataTable();
            dt.Load(dataAccess.da.ExecuteSP(dataAccess.da.OpenDatabase(true), "usp_GetLoggingTree", d, null));
            DataRow[] arr = dt.Select();

            return new[] {
					arr.Select(a => new {Id = a("Id"), DateTime = a("DateTime"), SourceURL = a("SourceURL"), TimeToLoad = a("TimeToLoad_MS"), ParentId = req.ParentId, PageName = a("PageName"), lid = a("lid"), UserName = ((iq.seshDic(a("lid")).ContainsKey("AgentAccount")) ? (((clsAccount) (iq.sesh(a("lid"), "AgentAccount"))).User.RealName) : "Not Logged In")}),
					arr.Where(a => a("Action") != "PageLoad").Select(a => new {Id = a("Id"), DateTime = a("DateTime"), lid = a("lid"), ParentId = req.ParentId, AdminAction = ((a("Action") != "PageLoad") ? (a("Action")) : ""), SourceBranchName = iq.Branches(System.Convert.ToInt32(a("SourcePath"))).Translation.text(English), TargetPathName = PathName(a("TargetPath").ToString())})
				};


        }
        catch (Exception ex)
        {
            ErrorLog.Add(ex);
            return "Error";
        }
    }

    [HttpPost]
    public dynamic Stats(clsGenericAjaxRequest req)
    {
        if (iq.seshDic == null || iq.seshDic.Count == 0)
        {
            return null;
        }
        try
        {
            object avgLoad = dataAccess.da.DBExecuteReader(dataAccess.da.OpenDatabase(true), "SELECT AVG(TimeToLoad_MS) as TimeToLoadAverage FROM AuditLog WHERE lid IN (" + System.Convert.ToString(req.lid == 0 ? (string.Join(",", iq.seshDic.Keys)) : req.lid) + ")");
            avgLoad.Read();
            return avgLoad.Item("TimeToLoadAverage");
        }
        catch (Exception)
        {
            return "Error";
        }
    }

    [HttpPost]
    public dynamic GetProductValidations(clsGenericAjaxRequest req)
    {
        return iq.ProductValidationsAssignment(req.SysType).ToList();
    }

    [HttpPost]
    public dynamic GetSKUEditDetails(clsSystemEditRequest req)
		{
		clsProduct product = iq.i_SKU(req.SystemSku);
		
		return new {Product = new {Id = product.ID, Description = product.DisplayName(English)}, 
                    Attributes = product.Attributes.Select(s => new {AttributeId = s.Value.Attribute.ID, Id = s.Value.ID, 
                                                                     Text = (s.Value.Translation != null ? (s.Value.Translation.text(English)) : null), 
                                                                     NumericValue = s.Value.NumericValue, UnitID = s.Value.Unit.ID}).ToList, 
                                                                     AllAttributes = iq.Attributes.Select(a => new {Id = a.Value.ID, Order = a.Value.Order,
                                                                                Code = a.Value.Code, Text = a.Value.Translation.text(English),
                                                                                InFamily = a.Value.Products.Where(pro => pro.Value.i_Attributes_Code.ContainsKey("FamMajor") 
                                                                                    && pro.Value.i_Attributes_Code("FamMajor").First.Translation.text(English) = product.i_Attributes_Code("FamMajor").First.Translation.text(English)).Count() > 0,
                                                                                    Required = (new clsProduct.clsSpecTableEntry() {Type = "atr", Code = a.Value.Code, ProdType = product.ProductType.Code})}).ToDictionary(a => a.Id, a => a), Units = iq.Units.Select(u => new {ID = u.Value.ID, Text = u.Value.Code}).ToList};
	}

    [HttpPost]
    public void UpdateAttributes(EditDetails data)
    {
        System.Object a = data;
        clsProduct product = iq.Products(data.Product.Id);
        List<string> errormessages = new List<string>();
        foreach (var f in data.Attributes)
        {
            object attr = product.Attributes(f.Id);
            if (attr == null)
            {
                attr = new clsProductAttribute(product, iq.Attributes(f.AttributeId), f.NumericValue, iq.Units(f.UnitID), iq.AddTranslation(f.Text, English, "", 0, null, 0, false));
            }
            else
            {
                attr.NumericValue = f.NumericValue;
                attr.Unit = iq.Units(f.UnitID);
                if (f.Text == null)
                {
                    attr.Translation = null;
                }
                else
                {
                    if (attr.Translation == null)
                    {
                        attr.Translation = iq.AddTranslation(f.Text, English, "", 0, null, 0, false);
                    }
                    else
                    {
                        attr.Translation.text(English) = f.Text;
                        attr.Translation.Update(English);
                    }
                }
            }
            attr.update(errormessages);
        }
        object ids = data.Attributes.Select(f => f.Id).ToList;
        foreach (var pa in product.Attributes)
        {
            if (!ids.Contains(pa.Key))
            {
                pa.Value.delete(errormessages);
            }
        }


        if (errormessages.Count > 0)
        {
            AuditLog.Instance.Add(AuditType.Warning, string.Join(",", errormessages), "Admin");
        }

    }
    public class EditDetails
    {
        public ProductDetails Product;
        public List<ProdAttribute> Attributes;
        public List<IdTextPair> AllAttributes;
        public List<IdTextPair> Units;
        public class ProductDetails
        {
            public string Description;
            public int Id;
        }
        public class ProdAttribute
        {
            public long AttributeId;
            public long Id;
            public long NumericValue;
            public string Text;
            public long UnitID;
        }
        public class IdTextPair
        {
            public long Id;
            public string Text;
        }

    }

    #region IncrementalImport

    [HttpPost]
    public dynamic IncrementalImport(clsAjaxIncrementalImportRequest data)
    {
        //1 time 'first call' to here
        Import.ImportLog.clear();

        if (Import.ActionListLid.ContainsKey(data.lid))
        {
            Import.ActionListLid.Remove(data.lid);
        }

        return Import.Incremental(data.lid, new List<string>(Strings.Split(System.Convert.ToString(data.SKUList), ","))).ToClientList();

    }

    [HttpPost]
    public dynamic IncrementalImportSubmit(clsAjaxIncrementalImportRequest data)
    {

        return Import.Incremental(data.lid, data.SubmitList).ToClientList();

    }

    [HttpPost]
    public List<clsImportRow> GetIncrementalImportStatus(clsAjaxIncrementalImportRequest data)
    {

        //output the status messages after some point (so it doesnt repeat itself)
        try
        {



            List<clsImportRow> extra = new List<clsImportRow>();

            if (ImportLog.data.Count > 1) //needed becuase the initial query (and first messgae - can take more than 10 secs)
            {
                for (i = data.atPoint + 1; i <= ImportLog.data.Count - 1; i++) //clsImportLog.nextId - 1
                {
                    if (ImportLog.data.ContainsKey(i))
                    {
                        extra.Add(ImportLog.data(i));
                    }
                    //extra = Import.ImportLog.data.Values.Where(Function(al) al.Id > data.atPoint).OrderBy(Function(al) al.Id).ToList()
                }
            }


            return extra;

        }
        catch
        {
            return default(List<clsImportRow>);
        }


    }

    #endregion

    #region Editor
    [HttpPost]
    public void CreateNewTranslation(clsAjaxEditorRequest data)
    {
        object trn = iq.AddTranslation("", English, "", 0, null, 0, true);

        object Obj = iq;
        object ParentObj = null;
        List<string> errorMessages = new List<string>();
        ParsePath(data.ReloadPath, Obj, ParentObj, errorMessages);

        Reflection.setProperty(Obj, data.PropertyName, trn, null, errorMessages, true);
    }
    #endregion


    #region AttributeEditor
    //<HttpPost>
    //Public Function FindAttribute(data As clsAttributeFinderRequest)
    //    If Not iq.i_attribute_code.ContainsKey(data.AttributeCode) Then Return New List(Of clsProductAttribute)()

    //    Dim attrs = iq.i_attribute_code(data.AttributeCode)

    //    Return attrs.Products.Values.Select(Function(prod) New With {.SKU = prod.sku, .Attributes = prod.i_Attributes_Code(data.AttributeCode).Select(Function(pa) New With {.NumericValue = pa.NumericValue, .Text = If(pa.Translation IsNot Nothing, pa.Translation.text(English), ""), .Unit = If(pa.Unit IsNot Nothing, pa.Unit.Symbol, "")}).tolist(), .Description = If(prod.i_Attributes_Code.ContainsKey("desc"), prod.i_Attributes_Code("desc").First.Translation.text(English), ""), .ProductId = prod.ID}).ToList()

    //End Function
    #endregion

}
//Tip #1 - You can drag .vb files here
//from Windows Explorer.

//Tip #2 - Use the project converter
//for improved accuracy.
Imports System.Net
Imports System.Web.Http
Imports Newtonsoft.Json

Public Class AdminController
    Inherits ApiController

    <HttpPost>
    Public Function GetAuditTree(req As clsGenericAjaxRequest)
        If iq.seshDic Is Nothing OrElse iq.seshDic.Count = 0 Then Return Nothing
        Try
            Dim d As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
            d.Add("lid", If(req.lid = 0, String.Join(",", iq.seshDic.Keys), req.lid))
            d.Add("ParentId", If(req.ParentId = 0, DBNull.Value, req.ParentId))
            Dim dt As DataTable = New DataTable()
            dt.Load(dataAccess.da.ExecuteSP(dataAccess.da.OpenDatabase(True), "usp_GetLoggingTree", d, Nothing))
            Dim arr() As DataRow = dt.Select()

            Return {
                arr.Select(Function(a) New With {.Id = a("Id"), .DateTime = a("DateTime"), .SourceURL = a("SourceURL"), .TimeToLoad = a("TimeToLoad_MS"), .ParentId = req.ParentId, .PageName = a("PageName"), .lid = a("lid"), .UserName = If(iq.seshDic(a("lid")).ContainsKey("AgentAccount"), CType(iq.sesh(a("lid"), "AgentAccount"), clsAccount).User.RealName, "Not Logged In")}),
                arr.Where(Function(a) a("Action") <> "PageLoad").Select(Function(a) New With {.Id = a("Id"), .DateTime = a("DateTime"), .lid = a("lid"), .ParentId = req.ParentId, .AdminAction = If(a("Action") <> "PageLoad", a("Action"), ""), .SourceBranchName = iq.Branches(CInt(a("SourcePath"))).Translation.text(English), .TargetPathName = PathName(a("TargetPath").ToString)})
            }


        Catch ex As Exception
            ErrorLog.Add(ex)
            Return "Error"
        End Try
    End Function

    <HttpPost>
    Public Function Stats(req As clsGenericAjaxRequest)
        If iq.seshDic Is Nothing OrElse iq.seshDic.Count = 0 Then Return Nothing
        Try
            Dim avgLoad = dataAccess.da.DBExecuteReader(dataAccess.da.OpenDatabase(True), "SELECT AVG(TimeToLoad_MS) as TimeToLoadAverage FROM AuditLog WHERE lid IN (" & If(req.lid = 0, String.Join(",", iq.seshDic.Keys), req.lid) & ")")
            avgLoad.Read()
            Return avgLoad.Item("TimeToLoadAverage")
        Catch ex As Exception
            Return "Error"
        End Try
    End Function

    <HttpPost>
    Public Function GetProductValidations(req As clsGenericAjaxRequest)
        Return iq.ProductValidationsAssignment(req.SysType).ToList()
    End Function

    <HttpPost>
    Public Function GetSKUEditDetails(req As clsSystemEditRequest)
        Dim product As clsProduct = iq.i_SKU(req.SystemSku)

        Return New With {.Product = New With {.Id = product.ID, .Description = product.DisplayName(English)}, .Attributes = product.Attributes.Select(Function(s) New With {.AttributeId = s.Value.Attribute.ID, .Id = s.Value.ID, .Text = If(s.Value.Translation IsNot Nothing, s.Value.Translation.text(English), Nothing), .NumericValue = s.Value.NumericValue, .UnitID = s.Value.Unit.ID}).ToList, .AllAttributes = iq.Attributes.Select(Function(a) New With {.Id = a.Value.ID, .Order = a.Value.Order, .Code = a.Value.Code, .Text = a.Value.Translation.text(English), .InFamily = a.Value.Products.Where(Function(pro) pro.Value.i_Attributes_Code.ContainsKey("FamMajor") AndAlso pro.Value.i_Attributes_Code("FamMajor").First.Translation.text(English) = product.i_Attributes_Code("FamMajor").First.Translation.text(English)).Count > 0, .Required = (New clsProduct.clsSpecTableEntry() With {.Type = "atr", .Code = a.Value.Code, .ProdType = product.ProductType.Code}.Order > 0)}).ToDictionary(Function(a) a.Id, Function(a) a), .Units = iq.Units.Select(Function(u) New With {.ID = u.Value.ID, .Text = u.Value.Code}).ToList}
    End Function

    <HttpPost>
    Public Sub UpdateAttributes(data As EditDetails)
        Dim a = data
        Dim product As clsProduct = iq.Products(data.Product.Id)
        Dim errormessages As List(Of String) = New List(Of String)()
        For Each f In data.Attributes
            Dim attr = product.Attributes(f.Id)
            If attr Is Nothing Then
                attr = New clsProductAttribute(product, iq.Attributes(f.AttributeId), f.NumericValue, iq.Units(f.UnitID), iq.AddTranslation(f.Text, English, "", 0, Nothing, 0, False))
            Else
                attr.NumericValue = f.NumericValue
                attr.Unit = iq.Units(f.UnitID)
                If f.Text Is Nothing Then
                    attr.Translation = Nothing
                Else
                    If attr.Translation Is Nothing Then
                        attr.Translation = iq.AddTranslation(f.Text, English, "", 0, Nothing, 0, False)
                    Else
                        attr.Translation.text(English) = f.Text
                        attr.Translation.Update(English)
                    End If
                End If
            End If
            attr.update(errormessages)
        Next
        Dim ids = data.Attributes.Select(Function(f) f.Id).ToList
        For Each pa In product.Attributes
            If Not ids.Contains(pa.Key) Then pa.Value.delete(errormessages)
        Next


        If errormessages.Count > 0 Then
            AuditLog.Instance.Add(AuditType.Warning, String.Join(",",errormessages), "Admin")
        End If

    End Sub
    Public Class EditDetails
        Public Product As ProductDetails
        Public Attributes As List(Of ProdAttribute)
        Public AllAttributes As List(Of IdTextPair)
        Public Units As List(Of IdTextPair)
        Public Class ProductDetails
            Public Description As String
            Public Id As Int32
        End Class
        Public Class ProdAttribute
            Public AttributeId As Int64
            Public Id As Int64
            Public NumericValue As Int64
            Public Text As String
            Public UnitID As Int64
        End Class
        Public Class IdTextPair
            Public Id As Int64
            Public Text As String
        End Class
        
    End Class

#Region "IncrementalImport"

    <HttpPost>
    Public Function IncrementalImport(data As clsAjaxIncrementalImportRequest)
        '1 time 'first call' to here
        Import.ImportLog.clear()

        If Import.ActionListLid.ContainsKey(data.lid) Then Import.ActionListLid.Remove(data.lid)

        Return Import.Incremental(data.lid, New List(Of String)(Split(data.SKUList, ","))).ToClientList()

    End Function

    <HttpPost>
    Public Function IncrementalImportSubmit(data As clsAjaxIncrementalImportRequest)

        Return Import.Incremental(data.lid, data.SubmitList).ToClientList()

    End Function

    <HttpPost>
    Public Function GetIncrementalImportStatus(data As clsAjaxIncrementalImportRequest) As List(Of clsImportRow)

        'output the status messages after some point (so it doesnt repeat itself)
        Try



            Dim extra As New List(Of clsImportRow)

            If ImportLog.data.Count > 1 Then   'needed becuase the initial query (and first messgae - can take more than 10 secs)
                For i = data.atPoint + 1 To ImportLog.data.Count - 1 'clsImportLog.nextId - 1
                    If ImportLog.data.ContainsKey(i) Then
                        extra.Add(ImportLog.data(i))
                    End If
                    'extra = Import.ImportLog.data.Values.Where(Function(al) al.Id > data.atPoint).OrderBy(Function(al) al.Id).ToList()
                Next i
            End If


            Return extra

        Catch
            Return Nothing
        End Try


    End Function

#End Region

#Region "Editor"
    <HttpPost>
    Public Sub CreateNewTranslation(data As clsAjaxEditorRequest)
        Dim trn = iq.AddTranslation("", English, "", 0, Nothing, 0, True)

        Dim Obj As Object = iq
        Dim ParentObj As Object
        Dim errorMessages As List(Of String) = New List(Of String)()
        ParsePath(data.ReloadPath, Obj, ParentObj, errorMessages)

        Reflection.setProperty(Obj, data.PropertyName, trn, Nothing, errorMessages, True)
    End Sub
#End Region


#Region "AttributeEditor"
    '<HttpPost>
    'Public Function FindAttribute(data As clsAttributeFinderRequest)
    '    If Not iq.i_attribute_code.ContainsKey(data.AttributeCode) Then Return New List(Of clsProductAttribute)()

    '    Dim attrs = iq.i_attribute_code(data.AttributeCode)

    '    Return attrs.Products.Values.Select(Function(prod) New With {.SKU = prod.sku, .Attributes = prod.i_Attributes_Code(data.AttributeCode).Select(Function(pa) New With {.NumericValue = pa.NumericValue, .Text = If(pa.Translation IsNot Nothing, pa.Translation.text(English), ""), .Unit = If(pa.Unit IsNot Nothing, pa.Unit.Symbol, "")}).tolist(), .Description = If(prod.i_Attributes_Code.ContainsKey("desc"), prod.i_Attributes_Code("desc").First.Translation.text(English), ""), .ProductId = prod.ID}).ToList()

    'End Function
#End Region

End Class

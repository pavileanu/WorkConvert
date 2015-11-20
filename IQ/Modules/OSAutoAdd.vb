Imports dataAccess
Imports System.Data.SqlClient

Module OSAutoAdd

    Public Sub fixAutoAdds(lid As UInt64)


        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        ' Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        Dim errormessages As List(Of String) = New List(Of String)
        Dim dicSystems As Dictionary(Of String, clsBranch) = CType(loadDic(con, iq.Branches, "system"), Global.System.Collections.Generic.Dictionary(Of String, Global.IQ.clsBranch))
        Dim sysProducts As List(Of clsProduct) = (From s In iq.Products.Values Where s.isSystem).ToList()
        Dim sysbranch As clsBranch = New clsBranch()
        ' Dim region = buyerAccount.SellerChannel.Region

        Dim skuPrefix As String = ""


        For Each prod In sysProducts
            skuPrefix = ""
            If dicSystems.ContainsKey(prod.sku) Then
                sysbranch = dicSystems(prod.sku)

                Dim syspath = "tree." & Trim$(CStr(iq.RootBranch.ID)) 'root
                syspath &= "." & Trim$(CStr(sysbranch.Parent.Parent.ID)) 'System type
                syspath &= "." & Trim$(CStr(sysbranch.Parent.ID)) 'Family
                syspath &= "." & Trim$(CStr(sysbranch.ID))


                Dim parentName As String = sysbranch.Parent.EnglishName
                Select Case True
                    Case parentName.Contains("ML110"), parentName.Contains("MS001"), parentName.Contains("ML10")
                        skuPrefix = "748920"
                    Case parentName.Contains("ML310")
                        skuPrefix = "748919"
                    Case parentName.Contains("ML5"), parentName.Contains("DL5"), parentName.Contains("BL")
                        skuPrefix = "748922"
                    Case Else
                        Dim m As Match = Regex.Match(parentName, "[MD]L3[^1].*", RegexOptions.IgnoreCase)
                        If m.Success Then
                            skuPrefix = "748921"
                        Else
                            Dim n As Match = Regex.Match(parentName, "DL[68].*", RegexOptions.IgnoreCase)
                            If n.Success Then
                                skuPrefix = "748921"
                            ElseIf parentName.Contains("DL1") Or parentName.Contains("ML15") Then
                                skuPrefix = "748921"
                            End If

                        End If

                End Select

                If skuPrefix <> "" Then
                    For Each region In iq.Regions.Values
                        Dim skuSuffix As String = "021"
                        Dim preinstalled As List(Of clsQuantity)
                        preinstalled = sysbranch.GetPreInstalledRecursive(region, syspath, errormessages)
                        Select Case region.Code.ToLower
                            Case "ru", "pl", "cz"
                                skuSuffix = "421"
                            Case "fr", "gb", "us", "ca"
                                skuSuffix = "B21"
                            Case "jp"
                                skuSuffix = "291"
                            Case "au", "in"
                                skuSuffix = "371"

                        End Select
                        Dim ossku As String = skuPrefix & "-" & skuSuffix
                        For Each i In preinstalled
                            If i.Branch.Product IsNot Nothing AndAlso i.Branch.Product.ProductType.Code.ToLower = "sof1" Then
                                If i.Branch.Product.sku = ossku And i.NumPreInstalled = 0 Then
                                    i.NumPreInstalled = 1
                                    i.update(errormessages)
                                End If


                            End If
                        Next
                    Next
                End If

            End If

        Next

    End Sub

    Public Sub FixMissingCarepackAttributes(lid As UInt64)


        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        ' Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        Dim errormessages As List(Of String) = New List(Of String)
        Dim dicSystems As Dictionary(Of String, clsBranch) = CType(loadDic(con, iq.Branches, "system"), Global.System.Collections.Generic.Dictionary(Of String, Global.IQ.clsBranch))
        Dim cpqProducts As List(Of clsProduct) = (From s In iq.Products.Values Where s.ProductType.Code.ToUpper() = "WTY" And (s.i_Attributes_Code.ContainsKey("response") = False Or s.i_Attributes_Code.ContainsKey("servicelevel") = False Or
                                                                              s.i_Attributes_Code.ContainsKey("DMR_ISS") = False Or s.i_Attributes_Code.ContainsKey("desc") = False Or s.i_Attributes_Code.ContainsKey("capacity") = False)).ToList()
        Dim sysbranch As clsBranch = New clsBranch()

        Dim con2 As SqlClient.SqlConnection = New SqlClient.SqlConnection("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35")
        con2.Open()
        Dim sql2 = "select description,sl1.sLabel as response,sl2.sLabel as ServiceLevel, sl3.sLabel as Options,duration ,opttype,optfamily " & _
                       ",options.optsku,case when sl1.sLabel like '%24x7%' then 1 else 0 end as tfs ,travel,tracing,ADP,DMR,CTR,OnSite, OptTypeParent as L1,OptTypeName as L2," & _
                       "ISNULL(a.translation, CASE " & _
                       "WHEN ot2.OptTypeParent = 'Software' AND ot2.OptTypeName NOT LIKE 'Microsoft OS' THEN ISNULL(Options.Technology, Options.OptFamily)  " & _
                       "WHEN ot2.OptTypeParent = 'Services' AND ot2.OptTypeName = 'SW Support' THEN CASE WHEN Options.Technology <> 'SUP' THEN Options.Technology ELSE Options.OptFamily END  " & _
                       "END) as L3 " & _
                       "                   from h3.iq.products.options " & _
                       "left outer join h3.iq.products.[CarePack_Properties]  on options.optsku=[CarePack_Properties].optsku " & _
                       "left outer join h3.iq.products.carepack_servicelevels sl1 on sl1.sCode = ResponseCode_ISS " & _
                       "left outer join h3.iq.products.carepack_servicelevels sl2 on sl2.sCode = servicelevel_iss " & _
                       "left outer join h3.iq.products.carepack_servicelevels sl3 on sl3.sCode = options_iss " & _
                       "left outer join h3.iq.products.opttypes ot2 on ot2.OptTypeCode = options.opttype " & _
                       "left outer join h3.iq.dbo.Abbreviations a ON a.code =  CASE " & _
                       "WHEN ot2.OptTypeParent = 'Software' AND ot2.OptTypeName NOT LIKE 'Microsoft OS' THEN ISNULL(Options.Technology, Options.OptFamily) " & _
                       "WHEN ot2.OptTypeParent = 'Services' AND ot2.OptTypeName = 'SW Support' THEN CASE WHEN Options.Technology <> 'SUP' THEN Options.Technology ELSE Options.OptFamily END " & _
                      "End " & _
                       "WHERE OptTypeName='HW Support' and options.optsku in ("
        Dim skus As String = ""

        For Each prod2 In cpqProducts
            skus = skus & "'" & prod2.sku & "',"
        Next
        skus = skus.Substring(0, skus.Length - 1)
        skus = skus & ")"
        'skus = "'U7AT3E')"

        '  If Not prod.i_Attributes_Code.ContainsKey("response") Then
        Dim sql = sql2 & skus
        Dim rdr2 = dataAccess.da.DBExecuteReader(con2, sql)
        While rdr2.Read
            Dim prod As clsProduct
            If iq.i_SKU.ContainsKey(CStr(rdr2("optsku"))) Then
                prod = iq.i_SKU(CStr(rdr2("optsku")))
                If Not prod.i_Attributes_Code.ContainsKey("response") Then
                    If Not IsDBNull(rdr2("response")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("response"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("response").ToString, English, "CPQ", 0, Nothing, 0, False), Nothing)
                End If
                If Not prod.i_Attributes_Code.ContainsKey("servicelevel") Then
                    If Not IsDBNull(rdr2("servicelevel")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("servicelevel"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("servicelevel").ToString, English, "CPQ", 0, Nothing, 0, False), Nothing)
                End If
                If Not prod.i_Attributes_Code.ContainsKey("DMR_ISS") Then
                    If Not IsDBNull(rdr2("options")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("DMR_ISS"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("options").ToString, English, "CPQ", 0, Nothing, 0, False), Nothing)
                End If
                If Not prod.i_Attributes_Code.ContainsKey("capacity") Then
                    If Not IsDBNull(rdr2("duration")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("capacity"), CInt(rdr2("duration")), iq.i_unit_code("year"), Nothing, Nothing)
                End If
                If Not prod.i_Attributes_Code.ContainsKey("desc") Then
                    If Not IsDBNull(rdr2("description")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("desc"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("description").ToString, English, "CPQ", 0, Nothing, 0, False), Nothing)
                End If
            End If
        End While
        'End If


        con2.Close()

        ' Fix all missing slot issues
        Dim swc = Nothing 'dataAccess.da.MakeWriteCacheFor(iq2con, "Slot")

        cpqProducts = (From s In iq.Products.Values Where s.ProductType.Code.ToUpper() = "WTY" And s.Branches.Count > 0).ToList()
        Dim cpqBranch As clsBranch = New clsBranch()
        For Each carepack In cpqProducts
            cpqBranch = carepack.Branches.First
            If cpqBranch.slots.Count = 0 Then
                Dim s = New clsSlot(iq.i_slotType_Code("WTY")("CAREPACK"), cpqBranch, "", -1, Nothing, New NullableInt(), 0, 0, swc)

            End If
            Dim qtys = From q In cpqBranch.Quantities.Values Where q.Path = ""

            For Each qty In qtys

                qty.Path = ""

            Next

        Next
        'remove all post warranty products 
        cpqProducts = (From s In iq.Products.Values Where s.ProductType.Code.ToUpper() = "WTY" And s.i_Attributes_Code.ContainsKey("desc")).ToList()
        For Each carepack In cpqProducts
            ' Dim errormessages As List(Of String) = New List(Of String)
            If carepack.i_Attributes_Code("desc").First.Translation.text(English).ToUpper().Contains("POST") Then
                Dim a = carepack.i_Attributes_Code("desc").First.Translation.text(English)
                AuditLog.Instance.Add(AuditType.Information, "CarePack SKU " & carepack.sku & " Activeto date changed", errormessages, "")
                carepack.activeTo = Now.AddDays(-30)
                carepack.update(errormessages)
            End If
        Next


        con.Close()


    End Sub
    'Public Sub FixDuplicateTRO(lid As UInt64)
    '    Dim con As SqlClient.SqlConnection = da.OpenDatabase()
    '    Dim dicSystems As Dictionary(Of String, clsBranch) = loadDic(con, iq.Branches, "system")
    '    Dim sysProducts As List(Of clsProduct) = (From s In iq.Products.Values Where s.isSystem).ToList()
    '    Dim skuPrefix As String = ""
    '    Dim sysbranch As clsBranch = New clsBranch()
    '    Dim agentAccount = iq.seshTyped(Of clsAccount)(lid, "AgentAccount")
    '    For Each prod In sysProducts
    '        skuPrefix = ""
    '        If dicSystems.ContainsKey(prod.sku) Then
    '            sysbranch = dicSystems(prod.sku)

    '            Dim syspath = "tree." & Trim$(iq.RootBranch.ID) 'root
    '            syspath &= "." & Trim$(sysbranch.Parent.Parent.ID) 'System type
    '            syspath &= "." & Trim$(sysbranch.Parent.ID) 'Family
    '            syspath &= "." & Trim$(sysbranch.ID)

    '            Dim troPath As String = ""
    '            Dim troBranch As clsBranch = sysbranch.FindBranchByNameBelow("HP Top Recommended", syspath, False, 12, troPath)
    '            Dim troCPQBranch = troBranch.FindBranchByNameBelow("Care Pack", syspath, False, 12, troPath)
    '            Dim dupChildBranch As List(Of clsBranch) = New List(Of clsBranch)
    '            For Each child As clsBranch In troCPQBranch.childBranches.values
    '                If Not child.isPrunedAt(troPath & "." & child.ID, agentAccount.SellerChannel) Then
    '                    If trocpqs.ContainsKey(child.Product.sku) Then
    '                         If Not hasList.Contains(child.Product.sku) Then
    '                            hasList.Add(child.Product.sku)
    '                        Else ' duplicate tro item
    '                            '  Dim p = New clsPrune(troPath & "." & child.ID, New NullableInt(agentAccount.SellerChannel.ID), "CPQJIT")
    '                            dupChildBranch.Add(child)

    '                        End If


    '                    End If

    '                End If
    '            Next
    '            For Each br In dupChildBranch
    '                Dim priceCount = troCPQBranch.childBranches(br.ID).Quantities.Count
    '                'delete quantities
    '                For i = 0 To priceCount - 1
    '                    Dim qty = troCPQBranch.childBranches(br.ID).Quantities.Values.First
    '                    troCPQBranch.childBranches(br.ID).Quantities(qty.ID).Delete(errorMEssages)
    '                Next
    '                'Delte slots
    '                Dim slotsCount = troCPQBranch.childBranches(br.ID).slots.Count
    '                For i = 0 To slotsCount - 1
    '                    Dim slot = troCPQBranch.childBranches(br.ID).slots.Values.First
    '                    troCPQBranch.childBranches(br.ID).slots(slot.ID).delete(errorMEssages)
    '                Next
    '                troCPQBranch.childBranches(br.ID).delete(errorMEssages)

    '            Next
    '        End If
    '    Next


    'End Sub
    Public Sub FixCarePacks(lid As UInt64)

        FixMissingCarepackAttributes(lid)

        'Adds quantity record and creates new carepack Products
        Dim agentAccount As clsAccount = iq.seshTyped(Of clsAccount)(lid, "AgentAccount")
        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        ' Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        Dim errormessages As List(Of String) = New List(Of String)
        Dim dicSystems As Dictionary(Of String, clsBranch) = CType(loadDic(con, iq.Branches, "system"), Global.System.Collections.Generic.Dictionary(Of String, Global.IQ.clsBranch))
        Dim sysProducts As List(Of clsProduct) = (From s In iq.Products.Values Where s.isSystem).ToList()
        Dim sysbranch As clsBranch = New clsBranch()
        ' Dim region = buyerAccount.SellerChannel.Region
        Dim defaultregion As clsRegion = agentAccount.SellerChannel.Region
        Dim skuPrefix As String = ""
        Dim startTime As DateTime = Now


        For Each prod In sysProducts
            If Now.AddMinutes(-5) > startTime Then
                startTime = Now
                agentAccount = iq.seshTyped(Of clsAccount)(lid, "AgentAccount")
            End If

            skuPrefix = ""
            If dicSystems.ContainsKey(prod.sku) Then
                sysbranch = dicSystems(prod.sku)

                Dim syspath = "tree." & Trim$(CStr(iq.RootBranch.ID)) 'root
                syspath &= "." & Trim$(CStr(sysbranch.Parent.Parent.ID)) 'System type
                syspath &= "." & Trim$(CStr(sysbranch.Parent.ID)) 'Family
                syspath &= "." & Trim$(CStr(sysbranch.ID))

                Dim req As clsGenericAjaxRequest = New clsGenericAjaxRequest()
                req.lid = lid
                req.BranchPath = syspath
                 Dim a = CarePackModule.CarePackJIT(req, defaultregion)            
            End If
        Next


    End Sub
    Public Function CarePackReports(lid As UInt64) As List(Of clsSysCarePack)
        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Dim errormessages As List(Of String) = New List(Of String)
        Dim dicSystems As Dictionary(Of String, clsBranch) = CType(loadDic(con, iq.Branches, "system"), Global.System.Collections.Generic.Dictionary(Of String, Global.IQ.clsBranch))
        Dim sysProducts As List(Of clsProduct) = (From s In iq.Products.Values Where s.isSystem And s.activeTo >= Today And s.EOL = False And s.Active And s.Publish).ToList()
        Dim sysbranch As clsBranch = New clsBranch()
        ' Dim region = buyerAccount.SellerChannel.Region

        Dim skuPrefix As String = ""

        Dim sysList As List(Of clsSysCarePack) = New List(Of clsSysCarePack)
        For Each prod In sysProducts
            skuPrefix = ""
            If dicSystems.ContainsKey(prod.sku) Then
                sysbranch = dicSystems(prod.sku)
                Dim syspath = "tree." & Trim$(CStr(iq.RootBranch.ID)) 'root
                syspath &= "." & Trim$(CStr(sysbranch.Parent.Parent.ID)) 'System type
                syspath &= "." & Trim$(CStr(sysbranch.Parent.ID)) 'Family
                syspath &= "." & Trim$(CStr(sysbranch.ID))
                Dim region As clsRegion = iq.i_region_code("US")
                Dim systemBI = New clsBranchInfo(lid, syspath, Nothing, 70, enumParadigm.errorNotSet, errormessages)
                Dim hideReasons = systemBI.branch.ReasonsForHide(systemBI.buyerAccount, systemBI.foci, syspath, systemBI.buyerAccount.SellerChannel.priceConfig, False, errormessages)

                If hideReasons.Count = 0 Then
                    Dim preinstalled As List(Of clsQuantity)
                    preinstalled = sysbranch.GetPreInstalledRecursive(region, syspath, errormessages)
                    Dim carepackexists As Boolean = False
                    For Each i In preinstalled
                        If i.Branch.Product IsNot Nothing AndAlso i.Branch.Product.ProductType.Code.ToLower = "wty" Then
                            If i.NumPreInstalled = 1 Then
                                Dim sysCarePack As clsSysCarePack = New clsSysCarePack()
                                sysCarePack.sysSkus = prod.SKU
                                sysCarePack.carepackSku = i.Branch.Product.SKU
                                sysCarePack.sysDesc = prod.i_Attributes_Code("desc")(0).Translation.text(English)
                                If i.Branch.Product.i_Attributes_Code.ContainsKey("desc") Then
                                    sysCarePack.carePackDesc = i.Branch.Product.i_Attributes_Code("desc")(0).Translation.text(English)
                                Else
                                    sysCarePack.carePackDesc = "No Carepack Description available"
                                End If
                                sysList.Add(sysCarePack)
                                carepackexists = True
                                Exit For
                            End If
                        End If
                    Next
                    If Not carepackexists Then
                        Dim notexist As clsSysCarePack = New clsSysCarePack()
                        notexist.sysSkus = prod.SKU
                        notexist.sysDesc = prod.i_Attributes_Code("desc")(0).Translation.text(English)
                        sysList.Add(notexist)
                    End If
                End If

            End If

        Next

        Return sysList
    End Function

    Public Function CarePackTROReports(lid As UInt64) As List(Of clsSysCarePack)
        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        ' Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        Dim errormessages As List(Of String) = New List(Of String)
        Dim dicSystems As Dictionary(Of String, clsBranch) = CType(loadDic(con, iq.Branches, "system"), Global.System.Collections.Generic.Dictionary(Of String, Global.IQ.clsBranch))
        Dim sysProducts As List(Of clsProduct) = (From s In iq.Products.Values Where s.isSystem And s.activeTo >= Today).ToList()
        Dim sysbranch As clsBranch = New clsBranch()
        ' Dim region = buyerAccount.SellerChannel.Region
        Dim agentAccount = iq.seshTyped(Of clsAccount)(lid, "AgentAccount")

        Dim skuPrefix As String = ""
        Dim trocpqs As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)()
        Dim sysList As List(Of clsSysCarePack) = New List(Of clsSysCarePack)
        For Each prod In sysProducts
            skuPrefix = ""
            If dicSystems.ContainsKey(prod.sku) Then
                sysbranch = dicSystems(prod.sku)

                Dim syspath = "tree." & Trim$(CStr(iq.RootBranch.ID)) 'root
                syspath &= "." & Trim$(CStr(sysbranch.Parent.Parent.ID)) 'System type
                syspath &= "." & Trim$(CStr(sysbranch.Parent.ID)) 'Family
                syspath &= "." & Trim$(CStr(sysbranch.ID))

                Dim region As clsRegion = iq.i_region_code("US")
                Dim troPath As String = ""
                Dim troBranch As clsBranch = CType(sysbranch.FindBranchByNameBelow("Top Recommended", CStr(syspath), False, 12, troPath), clsBranch)
                Dim troCPQBranch As clsBranch
                Dim hasList = New List(Of String)
                Dim carepackexists As Boolean = False
                If troBranch IsNot Nothing Then
                    troCPQBranch = CType(troBranch.FindBranchByNameBelow("Care Pack", CStr(syspath), False, 12, troPath), clsBranch)

                    If troCPQBranch IsNot Nothing Then
                        For Each child As clsBranch In troCPQBranch.childBranches.Values
                            If CBool(Not child.PruneInForce(troPath & "." & child.ID, agentAccount.SellerChannel)) Then
                                If Not hasList.Contains(child.Product.SKU) Then
                                    hasList.Add(child.Product.SKU)
                                    Dim sysCarePack As clsSysCarePack = New clsSysCarePack()
                                    sysCarePack.sysSkus = prod.SKU
                                    sysCarePack.carepackSku = child.Product.SKU
                                    sysCarePack.sysDesc = prod.i_Attributes_Code("desc")(0).Translation.text(English)
                                    sysCarePack.carePackDesc = child.Product.i_Attributes_Code("desc")(0).Translation.text(English)
                                    sysList.Add(sysCarePack)
                                    carepackexists = True
                                End If
                            End If
                        Next
                    End If
                End If
                If Not carepackexists Then
                    Dim notexist As clsSysCarePack = New clsSysCarePack()
                    notexist.sysSkus = prod.sku
                    notexist.sysDesc = prod.i_Attributes_Code("desc")(0).Translation.text(English)
                    sysList.Add(notexist)
                End If




            End If

        Next

        Return sysList
    End Function
End Module
Public Class clsSysCarePack
    Property sysSkus As String
    Property sysDesc As String
    Property carepackSku As String
    Property carePackDesc As String

End Class
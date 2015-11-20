Imports System.Data.SqlClient
Imports System.Net.Mail
'Imports log4net
'Imports log4net.Config
Imports dataAccess

Module CarePackModule
    ' Private log As ILog = LogManager.GetLogger("IQ")

    Public Function CarePackJIT(request As clsGenericAjaxRequest, Optional importregion As clsRegion = Nothing) As List(Of String)
        'Return Nothing

        Dim lid As ULong = request.lid
        Dim errorMessages = New List(Of String)()
        'LogMessage("CarePackJIT")
        ' Make sure we got a branch path
        If request.BranchPath Is Nothing Then Return Nothing
        If request.BranchPath = "tree.1" Then request.BranchPath = CStr(iq.sesh(lid, "treecursor"))
        If request.BranchPath Is Nothing Then Return Nothing

        Dim agentAccount = iq.seshTyped(Of clsAccount)(lid, "AgentAccount")
        Dim buyerAccount = iq.seshTyped(Of clsAccount)(lid, "BuyerAccount")

        Dim systemBranch As clsBranch = Nothing
        Dim systemPath = String.Empty
        Dim troAmended As Boolean = False
        Dim sku As String = Nothing

        ' Look for the branch and product
        If iq.Branches.ContainsKey(CInt(Split(request.BranchPath, ".").Last)) Then
            systemBranch = iq.Branches(CInt(Split(request.BranchPath, ".").Last))
            Dim path = String.Empty
            systemBranch = systemBranch.FindSystemAbove(request.BranchPath, path)
        End If
        If systemBranch Is Nothing OrElse systemBranch.Product Is Nothing OrElse String.IsNullOrEmpty(systemBranch.Product.SKU) Then Return Nothing

        sku = systemBranch.Product.SKU

        ' Don't refresh care packs for this sku if they've been refreshed recently
        Dim refresh As Boolean = True

        'LogMessage("CarePackJIT : CarePackLastRefresh System SKU " & sku)
        If iq.CarePackLastRefresh.ContainsKey(sku) Then

            Dim lastRefresh As DateTime = iq.CarePackLastRefresh(sku)
            If DateTime.Now < lastRefresh.AddDays(1) Then   ' Refresh system if not refreshed in the previous 24 hours
                refresh = False
            End If
        End If
        If Not refresh Then Return Nothing
        'LogMessage("CarePackJIT : IsPQWSActive :" & IsPQWSActive())
        ' Make sure the PQWS service is installed/responding
        If Not IsPQWSActive() Then Return Nothing

        ' Get the system path
        systemPath = Left(request.BranchPath, request.BranchPath.IndexOf(systemBranch.ID) + Len(systemBranch.ID.ToString))

        Dim carePackScreen As clsScreen = New clsScreen()
        If systemBranch.Product.mfrCode.ToUpper() = "HPI" Then
            carePackScreen = iq.i_screens_code("optCPKDTO")
        Else
            carePackScreen = iq.i_screens_code("optCPK")
        End If


        ' Find the Hardware Support branch - care pack branches get grafted on there
        Dim hwSupportPath As String = Left(systemPath, Len(systemPath) - Len(Split(systemPath, ".").Last) - 1)
        Dim hwSupportBranch As clsBranch = CType(systemBranch.FindBranchByNameBelow("HW Support", Left(systemPath, Len(systemPath) - Len(Split(systemPath, ".").Last) - 1), True, 12, hwSupportPath), clsBranch)
        If hwSupportBranch.Matrix Is Nothing Then
            hwSupportBranch.Matrix = carePackScreen
            hwSupportBranch.Update(errorMessages)
        End If
        If hwSupportBranch Is Nothing Then

            ' Couldn't find the Hardware Support branch - create it under the Services branch
            Dim servicesBranchPath = String.Empty
            Dim servicesBranch = systemBranch.FindBranchByNameBelow("Services", "", True, 12, servicesBranchPath)
            If servicesBranch Is Nothing Then

                ' Couldn't find the Services branch; locate via the All Options branch
                Dim allOptionsBranchPath = String.Empty
                Dim allOptionsBranch = systemBranch.FindBranchByNameBelow("All Options", "", True, 12, allOptionsBranchPath)
                servicesBranch = New clsBranch(Nothing, CType(allOptionsBranch, clsBranch), iq.AddTranslation("Services", English, "", 0, Nothing, 0, False), "", iq.AddTranslation("Options", English, "", 0, Nothing, 0, False), iq.AddTranslation("Option", English, "", 0, Nothing, 0, False), Nothing, 40, False, "Y")

            End If

            If Not servicesBranch Is Nothing Then
                hwSupportBranch = New clsBranch(Nothing, CType(servicesBranch, clsBranch), iq.AddTranslation("HW Support", English, "", 0, Nothing, 0, False), "", iq.AddTranslation("Options", English, "", 0, Nothing, 0, False), iq.AddTranslation("Option", English, "", 0, Nothing, 0, False), carePackScreen, 0, False, "B")
            End If

        End If
        'LogMessage("CarePackJIT : HwSupportBranch : " & hwSupportBranch.ID)


        If hwSupportBranch Is Nothing Then Return Nothing

        ' Find the Top Recommended Options branch
        Dim troPath = String.Empty
        Dim troBranch As clsBranch = CType(systemBranch.FindBranchByNameBelow("Top Recommended", systemPath, False, 6, troPath), clsBranch)
        If troBranch Is Nothing Then

            ' Couldn't find the Top Recommended Options branch - create it
            troBranch = New clsBranch(Nothing, systemBranch, iq.AddTranslation("Top Recommended", English, "", 0, Nothing, 0, False), "", iq.AddTranslation("Top Recommended", English, "", 0, Nothing, 0, False), iq.AddTranslation("Top Recommended", English, "", 0, Nothing, 0, False), Nothing, 0, False, "H")
            troPath = systemPath & "." & troBranch.ID

        End If

        ' Find the Top Recommended Options/Care Pack branch - care pack branches get grafted on there
        Dim troCpqBranch As clsBranch = CType(troBranch.FindBranchByNameBelow("Care Pack", troPath, False, 7, troPath), clsBranch)
        If troCpqBranch Is Nothing Then

            ' Couldn't find the Top Recommended Options/Care Pack branch - create it
            troCpqBranch = New clsBranch(Nothing, troBranch, iq.AddTranslation("Care Pack", English, "", 0, Nothing, 0, False), "/images/product/category/cat2.png", iq.AddTranslation("Top Recommended", English, "", 0, Nothing, 0, False), iq.AddTranslation("Top Recommended", English, "", 0, Nothing, 0, False), Nothing, 0, False, "I")
            troPath = troPath & "." & troCpqBranch.ID
        End If

        'LogMessage("CarePackJIT : TroPath : " & troPath)

        Dim skuVariant As clsVariant
        ' Attempt to refresh the care packs via a call to PQWS
        Dim autoAddCreatedPath As String = Nothing
        Try

            'LogMessage("CarePackJIT : RefreshPQWSCarePacks")
            RefreshPQWSCarePacks(systemBranch.Product, systemPath, hwSupportBranch, troCpqBranch, agentAccount, autoAddCreatedPath, troAmended, lid, hwSupportPath, systemBranch, troPath, errorMessages)

            '  CreateCarePackVariants(carePackList, request, agentAccount, buyerAccount)

            ' Store the last refresh time for this sku so we don't refresh it constantly
            If iq.CarePackLastRefresh.ContainsKey(sku) Then
                iq.CarePackLastRefresh(sku) = DateTime.Now
            Else
                iq.CarePackLastRefresh.Add(sku, DateTime.Now)
            End If

        Catch ex As Exception
            ErrorLog.Add(ex)
        End Try

        ' Set up the return
        ' Dim results = New List(Of String)()

        ''LogMessage("CarePackJIT : AutoAddPath")
        'If autoAddCreatedPath IsNot Nothing Then
        '    'LogMessage("CarePackJIT : AutoAddPath :" & autoAddCreatedPath)
        '    results.Add("addpart:" & autoAddCreatedPath)
        'End If

        'If troAmended Then
        '    'LogMessage("CarePackJIT : TroChanged :")
        '    results.Add("refreshall")
        'End If

        'If results.Count > 0 Then Return results Else Return Nothing
        Return Nothing

    End Function

    Private Function IsPQWSActive() As Boolean

        IsPQWSActive = False

        Dim pqws As PQWS.PQWSClient = New PQWS.PQWSClient()

        If Not pqws.Endpoint.Address Is Nothing Then    ' False if the endpoint isn't configured

            Try
                Dim response = pqws.Hello()
                IsPQWSActive = (String.Equals(response, "Hello", StringComparison.InvariantCultureIgnoreCase))
            Catch ex As Exception
                ErrorLog.Add(ex)
            End Try

        End If

    End Function

    Private Function RefreshPQWSCarePacks(systemProduct As clsProduct, systemPath As String, hwSupportBranch As clsBranch, troBranch As clsBranch, agentAccount As clsAccount,
                                          ByRef autoAddCreatedPath As String, ByRef troAmended As Boolean, lid As ULong, hwSupportPath As String, systemBranch As clsBranch, troPath As String, errorMessages As List(Of String)) As Boolean

        RefreshPQWSCarePacks = True

        ' Call the PQWS webapi service to retrieve HP care packs
        Dim pqws As PQWS.PQWSClient = New PQWS.PQWSClient()
        Dim hpCarePackResults As PQWS.CPCHierarchyCarePackResults = Nothing

        ' Retrieve care pack data from HP
        Try
            hpCarePackResults = pqws.HPCarePacks(agentAccount.mfrCode, systemProduct.SKU, agentAccount.SellerChannel.Region.Code, agentAccount.Language.Code)
            'LogMessage("CarePackJIT : CarePack results")
        Catch ex As Exception
            'LogMessage("CarePackJIT : CarePack results Exception " & ex.Message)
            RefreshPQWSCarePacks = False
            ErrorLog.Add(ex)
        End Try
        If hpCarePackResults.AllHPCarePacks IsNot Nothing Then
            'LogMessage("CarePackJIT : CarePack results AllHPCarePacks :" & hpCarePackResults.AllHPCarePacks.Count)
        Else
            'LogMessage("CarePackJIT : CarePack results AllHPCarePacks 0")
        End If

        If hpCarePackResults.RecommendedHPCarePacks IsNot Nothing Then
            'LogMessage("CarePackJIT : CarePack results RecommendedHPCarePacks :" & hpCarePackResults.RecommendedHPCarePacks.Count)
        Else
            'LogMessage("CarePackJIT : CarePack results RecommendedHPCarePacks 0")
        End If
        '      'LogMessage("CarePackJIT : CarePack results RecommendedHPCarePacks" & IIf(hpCarePackResults.RecommendedHPCarePacks IsNot Nothing, hpCarePackResults.RecommendedHPCarePacks.Count, 0))
        ' Refresh the care packs
        If Not hpCarePackResults Is Nothing Then
            ' Dim carePacks = From u In hpCarePackResults.AllHPCarePacks Select u.CarePackProductNumber

            RefreshPQWSCarePacks = RefreshCarePacks(systemProduct, systemPath, hwSupportBranch, troBranch, hpCarePackResults, agentAccount, autoAddCreatedPath, troAmended, lid, hwSupportPath, systemBranch, troPath, errorMessages)
        End If

    End Function

    ' Creates and assigns IQ2 Care Packs to the owning sku from the passed list of HP care packs
    Private Function RefreshCarePacks(systemProduct As clsProduct, systemPath As String, hwSupportBranch As clsBranch, troBranch As clsBranch, hpCarePackResults As PQWS.CPCHierarchyCarePackResults, agentAccount As clsAccount, ByRef autoAddCreatedPath As String,
                                      ByRef troAmended As Boolean, lid As ULong, hwSupportPath As String, systemBranch As clsBranch, troPath As String, errorMessages As List(Of String)) As Boolean

        RefreshCarePacks = True

        If ValidateHPCarePacks(hpCarePackResults) Then

            Dim allCarePacks = New List(Of IQ.PQWS.CPCCarePack)
            Dim amendedCarePacks = New List(Of IQ.PQWS.CPCCarePack)
            Dim newCarePacks = New List(Of IQ.PQWS.CPCCarePack)
            Dim deletedCarePacks = New List(Of String)
            Dim newServiceLevels = New List(Of String)
            Dim notAddedCarePacks = New List(Of IQ.PQWS.CPCCarePack)
            Dim carePackRootBranch As clsBranch = iq.i_SpecialBranches("cpqroot")

            ' Apply any fixes to the data
            '  CleanData(hwSupportBranch, troBranch, errorMessages)

            ' Form a list of all the care packs by combining the "recommended" and "all" lists
            BuildCarePackList(hpCarePackResults.RecommendedHPCarePacks, hpCarePackResults.AllHPCarePacks, allCarePacks)
            'LogMessage("CarePackJIT : BuildCarePackList")
            ' Build a list of unknown service levels
            FindUnknownServiceLevels(allCarePacks, newServiceLevels)
            'LogMessage("CarePackJIT : FindUnknownServiceLevels")
            ' Build a list of new/amended care packs (as well as unrecognized service levels to send to support)
            FindNewAndAmendedCarePacks(allCarePacks, amendedCarePacks, newCarePacks, hpCarePackResults.CountryDetails, agentAccount, notAddedCarePacks)
            'LogMessage("CarePackJIT : FindNewAndAmendedCarePacks")
            ' Build a list of care packs deleted from the product
            FindDeletedCarePacks(hwSupportBranch, notAddedCarePacks, deletedCarePacks)
            'LogMessage("CarePackJIT : FindDeletedCarePacks")
            ' New care packs - these could be completely new to IQ2, or existing care packs newly
            ' added to the product
            For Each hpCarePack As PQWS.CPCCarePack In newCarePacks

                Dim carePackSKUCode = hpCarePack.CarePackProductNumber
                Dim serviceLevel As clsServiceLevel = iq.ServiceLevels.Values.FirstOrDefault(Function(sl) (sl.ServiceLevel = hpCarePack.ServiceLevel))
                Dim carePackBranch As clsBranch = Nothing

                ' This is a completely new care pack - create the clsProduct and, if not disabled/post-warranty, clsBranch
                carePackBranch = CreateOrAmendCarePack(Nothing, Nothing, hpCarePack, serviceLevel, carePackRootBranch, agentAccount, hpCarePackResults.CountryDetails, lid, errorMessages)

                If carePackBranch IsNot Nothing AndAlso carePackRootBranch.childBranches.ContainsKey(carePackBranch.ID) = False Then

                    ' Add the care pack to the care pack root branch
                    carePackRootBranch.childBranches.Add(carePackBranch.ID, carePackBranch)
                    carePackRootBranch.Update(errorMessages)
                End If
                ' Make sure the care pack branch is grafted onto the Hardware Support branch
                GraftCarePack(carePackSKUCode, carePackBranch, hwSupportBranch, errorMessages, hwSupportPath)



            Next
            'LogMessage("CarePackJIT : NewCarePacks")
            ' Amended care packs
            For Each hpCarePack As PQWS.CPCCarePack In amendedCarePacks

                Dim carePackSKUCode = hpCarePack.CarePackProductNumber
                Dim serviceLevel As clsServiceLevel = iq.ServiceLevels.Values.FirstOrDefault(Function(sl) (sl.ServiceLevel = hpCarePack.ServiceLevel))
                Dim carePack As clsProduct = iq.i_SKU(carePackSKUCode)

                ' Make sure the care pack branch is under the care pack root branch
                Dim carePackBranch As clsBranch = carePackRootBranch.childBranches.Values.FirstOrDefault(Function(cb) Not cb.Product Is Nothing AndAlso cb.Product.ID = carePack.ID)
                If carePackBranch Is Nothing Then
                    ' No care pack branch - create and add to the root branch (unless disabled or post-warranty)
                    Dim translation As clsTranslation = iq.AddTranslation(carePackSKUCode, English, "CPQ", 0, Nothing, 0, False)
                    carePackBranch = New clsBranch(carePack, carePackRootBranch, translation, String.Empty, translation, translation, Nothing, 0, False, "B")
                End If

                ' Update the care pack
                CreateOrAmendCarePack(carePack, carePackBranch, hpCarePack, serviceLevel, carePackRootBranch, agentAccount, hpCarePackResults.CountryDetails, lid, errorMessages)

                If carePackBranch IsNot Nothing Then
                    ' Make sure the care pack branch is grafted onto the Hardware Support branch
                    GraftCarePack(carePackSKUCode, carePackBranch, hwSupportBranch, errorMessages, hwSupportPath)
                End If

            Next
            'LogMessage("CarePackJIT : amendedCarePacks")
            ' Deleted care packs
            For Each sku As String In deletedCarePacks

                ' Delete the care pack from the system - care packs remain on the care pack root branch if they're there
                DeleteCarePackBranch(troBranch, sku, errorMessages)
                DeleteCarePackBranch(hwSupportBranch, sku, errorMessages)

            Next
            'LogMessage("CarePackJIT : deletedCarePacks")
            Dim buyerAccount = iq.seshTyped(Of clsAccount)(lid, "BuyerAccount")
            If newCarePacks.Count > 0 Then CreateCarePackVariants(newCarePacks, lid, agentAccount, buyerAccount)
            If amendedCarePacks.Count > 0 Then CreateCarePackVariants(amendedCarePacks, lid, agentAccount, buyerAccount)
            ' Assign any Top Recommended Option care packs
            troAmended = SetupTRO(systemProduct, troBranch, allCarePacks, carePackRootBranch, troPath, agentAccount, errorMessages)

            ' Set up any Auto Adds
            autoAddCreatedPath = SetUpAutoAdd(systemProduct, allCarePacks, carePackRootBranch, systemPath, agentAccount, errorMessages, systemBranch, hwSupportBranch, buyerAccount)

            ' Email support with any unknown service levels encountered
            If newServiceLevels.Count > 0 Then
                AddUnknownServiceLevel(newServiceLevels, systemProduct.mfrCode)
                ' SendServiceLevelsEmail(newServiceLevels, errorMessages)
            End If

            RefreshCarePacks = True

        Else
            RefreshCarePacks = False
            AuditLog.Instance.Add(AuditType.Error, "Invalid response retrieved from PQWS web service", String.Empty, lid)
        End If

    End Function

    ' Ensure the care pack branch is grafted onto the parent branch
    Private Sub GraftCarePack(carePackSKUCode As String, carePackBranch As clsBranch, hwSupportBranch As clsBranch, errorMessages As List(Of String), hwSupportPath As String)

        '  If hwSupportBranch.childBranches.Values.FirstOrDefault(Function(cb) cb.SKU = carePackSKUCode) Is Nothing Then
        If carePackBranch IsNot Nothing AndAlso Not carePackBranch.GraftedOnAt.Contains(hwSupportPath) Then     ' Not hwSupportBranch.childBranches.Values.Contains(carePackBranch) Then
            If hwSupportBranch.Graft(carePackBranch, "RefreshPQWSCarePacks", hwSupportPath, errorMessages, Nothing) Then
                hwSupportBranch.Update(errorMessages)
            End If
        End If

    End Sub

    Private Sub CleanData(hwSupportBranch As clsBranch, troBranch As clsBranch, errorMessages As List(Of String))

        ' Some care packs are "native children" of the system rather than branches grafted on from the care pack root branch - 
        ' to tidy this up, the "native" branch needs to be deleted and a new care pack root branch created and grafted on in its place

        RemoveNonGraftedBranches(hwSupportBranch, errorMessages)
        RemoveNonGraftedBranches(troBranch, errorMessages)

    End Sub

    Private Sub RemoveNonGraftedBranches(parentBranch As clsBranch, errorMessages As List(Of String))

        Dim nonGraftedBranchIDs = New List(Of Integer)

        ' Build a list of non-grafted branches to remove - these will have the parent
        ' branch as their Parent (whereas grafted branches retain their original Parent).
        For Each branch As clsBranch In parentBranch.childBranches.Values
            If branch.Parent Is Nothing OrElse branch.Parent.ID = parentBranch.ID Then  ' Parent will be different for grafted branches
                nonGraftedBranchIDs.Add(branch.ID)
            End If
        Next

        ' Delete any non-grafted branches
        For Each nonGraftedBranchID In nonGraftedBranchIDs

            parentBranch.childBranches(nonGraftedBranchID).Parent = Nothing
            parentBranch.childBranches(nonGraftedBranchID).Update(errorMessages)

            parentBranch.childBranches.Remove(nonGraftedBranchID)

        Next

    End Sub

    Private Sub DeleteCarePackBranch(parentBranch As clsBranch, sku As String, errorMessages As List(Of String))

        Dim carePackBranch As clsBranch = parentBranch.childBranches.Values.FirstOrDefault(Function(cb) cb.SKU = sku)

        While Not carePackBranch Is Nothing

            If carePackBranch.Parent.ID <> parentBranch.ID Then
                ' The care pack is grafted on - remove the graft
                parentBranch.DeleteGraftedOnBranch(carePackBranch.ID)
            Else
                ' The care pack is an actual child branch - remove its parent
                carePackBranch.Parent = Nothing
                carePackBranch.Update(errorMessages)
            End If

            ' Remove the care pack from the object model
            parentBranch.childBranches.Remove(carePackBranch.ID)

            ' Any more? Seems there can be duplicates...
            carePackBranch = parentBranch.childBranches.Values.FirstOrDefault(Function(cb) cb.SKU = sku)

        End While

    End Sub

    Private Function SetupTRO(systemProduct As clsProduct, troBranch As clsBranch, allCarePacks As List(Of PQWS.CPCCarePack), carePackRootBranch As clsBranch, troPath As String, agentAccount As clsAccount, errorMessages As List(Of String)) As Boolean

        Dim sysFamily As String = systemProduct.i_Attributes_Code("FamMajor")(0).Translation.text(English)
        Dim slotTypeCode As Integer = 2     ' 2 is the magic number for TROs on the TROAA table
        Dim MAXTROS = 2                     ' 2 is also the maximum number of TROs we display
        Dim amended As Boolean = False

        'LogMessage("CarePackJIT : SetupTRO : ")

        If systemProduct.mfrCode = "HPE" Then

            Dim tros As List(Of clsTROAA) = iq.ServiceLevelTROAA.Values.Where(Function(troaa) String.Equals(sysFamily, troaa.SysFamily, StringComparison.InvariantCultureIgnoreCase) AndAlso troaa.SlotTypeCode = slotTypeCode).OrderBy(Function(troaa) troaa.DisplayOrder).ToList()

            If tros Is Nothing Then Return amended

            Dim addedCount As Integer = 0
            For Each tro As clsTROAA In tros
                'LogMessage("CarePackJIT : Tro" & tros.Count)
                'LogMessage("CarePackJIT : Tro : allCarePacks" & allCarePacks.Count)
                Dim serviceLevel = tro.ServiceLevel

                ' There should be either none or one service packs matching this service level. We basically assume
                ' HP never sends us > 1 with the same level...
                Dim matchingHPCarePack As PQWS.CPCCarePack = allCarePacks.FirstOrDefault(Function(cp) cp.ServiceLevel = serviceLevel.ServiceLevel)


                If matchingHPCarePack IsNot Nothing AndAlso matchingHPCarePack.CarePackProductNumber IsNot Nothing Then
                    'LogMessage("CarePackJIT : matchingHPCarePack : " & matchingHPCarePack.CarePackProductNumber)
                    ' Graft this care pack onto the TRO branch
                    Dim carePackBranch As clsBranch = carePackRootBranch.childBranches.Values.FirstOrDefault(Function(cpb) Not cpb.Product Is Nothing AndAlso String.Equals(cpb.SKU, matchingHPCarePack.CarePackProductNumber, StringComparison.InvariantCultureIgnoreCase))
                    'LogMessage("CarePackJIT : carePackBranch : " & carePackRootBranch.childBranches.Values.Count)
                    If carePackBranch IsNot Nothing AndAlso Not carePackBranch.GraftedOnAt.Contains(troPath) Then   'AndAlso Not troBranch.childBranches.Values.Contains(carePackBranch)
                        If troBranch.Graft(carePackBranch, "RefreshPQWSTRO", troPath, errorMessages, Nothing) Then
                            'LogMessage("CarePackJIT : TRO Added  ")

                            troBranch.Update(errorMessages)
                            'LogMessage("CarePackJIT : TRO Updated ")
                            addedCount += 1
                            amended = True
                        End If

                    End If


                End If

                ' Don't add more than MAXTROS care packs
                If addedCount = MAXTROS Then Exit For

            Next
        Else
            For Each cpk In From c In allCarePacks Where c.OrderOfPreference > 1 Take 2 Order By c.OrderOfPreference
                Dim carePackBranch As clsBranch = carePackRootBranch.childBranches.Values.FirstOrDefault(Function(cpb) Not cpb.Product Is Nothing AndAlso String.Equals(cpb.SKU, cpk.CarePackProductNumber, StringComparison.InvariantCultureIgnoreCase))
                'LogMessage("CarePackJIT : TRO  SKU : " & cpk.CarePackProductNumber)
                If carePackBranch IsNot Nothing AndAlso Not carePackBranch.GraftedOnAt.Contains(troPath) Then
                    If troBranch.Graft(carePackBranch, "RefreshPQWSTRO", troPath, errorMessages, Nothing) Then
                        troBranch.Update(errorMessages)
                        amended = True
                    End If

                End If

            Next


        End If
        'LogMessage("CarePackJIT : TRO Ammended : " & amended)
        Return amended

    End Function

    Private Function SetUpAutoAdd(systemProduct As clsProduct, allCarePacks As List(Of PQWS.CPCCarePack), carePackRootBranch As clsBranch, systemPath As String, agentAccount As clsAccount, errorMessages As List(Of String), systemBranch As clsBranch, hwSupportBranch As clsBranch, buyerAccount As clsAccount) As String
        Dim autoAddCreatedPath As String = Nothing
        If systemProduct.mfrCode = "HPE" Then
            'LogMessage("CarePackJIT : SetUpAutoAdd : ")
            Dim sysFamily As String = systemProduct.i_Attributes_Code("FamMajor")(0).Translation.text(English)
            Dim slotTypeCode As Integer = 1     ' 1 is the magic number for AutoAdds on the TROAA table
            Dim MAXAAS = 1                      ' 1 is also the maximum number of AutoAdds

            If systemBranch.slots.Values.Where(Function(sl) sl.Type.MajorCode.ToUpper = "WTY").Count = 0 Then
                'LogMessage("CarePackJIT : SetUpAutoAdd : SlotsCreated ")
                Dim slt = New clsSlot(iq.i_slotType_Code("WTY")("CAREPACK"), systemBranch, "", 3, Nothing, New NullableInt(), 0, 0)
            End If


            Dim aas As List(Of clsTROAA) = iq.ServiceLevelTROAA.Values.Where(Function(troaa) String.Equals(sysFamily, troaa.SysFamily, StringComparison.InvariantCultureIgnoreCase) AndAlso troaa.SlotTypeCode = slotTypeCode).OrderBy(Function(troaa) troaa.DisplayOrder).ToList()

            If aas Is Nothing Then Return Nothing

            Dim addedCount As Integer = 0
            For Each aa As clsTROAA In aas

                Dim serviceLevel = aa.ServiceLevel

                ' There should be either none or one service packs matching this service level. We basically assume
                ' HP never sends us > 1 with the same level...
                Dim matchingHPCarePack As PQWS.CPCCarePack = allCarePacks.FirstOrDefault(Function(cp) cp.ServiceLevel = serviceLevel.ServiceLevel)

                If matchingHPCarePack IsNot Nothing Then
                    'LogMessage("CarePackJIT : SetUpAutoAdd : matchingHPCarePack ")
                    Dim carePackBranch As clsBranch = hwSupportBranch.childBranches.Values.FirstOrDefault(Function(cpb) Not cpb.Product Is Nothing AndAlso String.Equals(cpb.SKU, matchingHPCarePack.CarePackProductNumber, StringComparison.InvariantCultureIgnoreCase))
                    'LogMessage("CarePackJIT : SetUpAutoAdd : matchingHPCarePack " & hwSupportBranch.childBranches.Values.Count)
                    If carePackBranch IsNot Nothing Then
                        Dim qtyCount As List(Of clsQuantity) = (From q In carePackBranch.Quantities.Values Where (String.IsNullOrEmpty(q.Path) OrElse q.Path.Contains(systemPath)) AndAlso q.Region.Encompasses(agentAccount.BuyerChannel.Region) AndAlso q.NumPreInstalled = 1 AndAlso q.FOC = False).ToList()
                        'LogMessage("CarePackJIT : SetUpAutoAdd : Quantities " & qtyCount.Count)
                        If qtyCount.Count = 0 Then
                            'LogMessage("CarePackJIT : SetUpAutoAdd : Creating AutoAdd ")
                            Dim resultPath = String.Empty
                            Dim cpkProduct As clsProduct = iq.i_SKU(matchingHPCarePack.CarePackProductNumber)
                            systemBranch.findChildBySKU2(systemPath, matchingHPCarePack.CarePackProductNumber, resultPath)
                            autoAddCreatedPath = resultPath
                            'LogMessage("CarePackJIT : SetUpAutoAdd : Creating AutoAdd path " & resultPath)
                            Dim quantity = New clsQuantity(agentAccount.SellerChannel.Region, resultPath, carePackBranch, 1, 0, 0, False, Nothing)
                        End If
                        'For Each q In carePackBranch.Quantities.Values
                        '    Debug.WriteLine(q.Path & "| " & q.Region.Code & " | " & q.NumPreInstalled & " | " & q.FOC & " | " & q.displayName(English))
                        'Next
                        addedCount += 1
                    End If
                    ' carePackBranch.Quantities.Clear()

                End If

                ' Don't add more than MAXTROS care packs
                If addedCount = MAXAAS Then Exit For

            Next
        Else
            For Each cpk In From c In allCarePacks Where c.OrderOfPreference = 1
                Dim carePackBranch As clsBranch = carePackRootBranch.childBranches.Values.FirstOrDefault(Function(cpb) Not cpb.Product Is Nothing AndAlso String.Equals(cpb.SKU, cpk.CarePackProductNumber, StringComparison.InvariantCultureIgnoreCase))
                If Not carePackBranch Is Nothing Then
                    Dim qtyCount = From q In carePackBranch.Quantities.Values Where (String.IsNullOrEmpty(q.Path) OrElse q.Path.Contains(systemPath)) AndAlso q.Region.Encompasses(agentAccount.BuyerChannel.Region) AndAlso q.NumPreInstalled = 1 AndAlso q.FOC = False
                    'LogMessage("CarePackJIT : SetUpAutoAdd : Quantities " & qtyCount.Count)
                    If qtyCount.Count = 0 Then
                        'LogMessage("CarePackJIT : SetUpAutoAdd : Creating AutoAdd ")
                        Dim resultPath = String.Empty
                        systemBranch.findChildBySKU2(systemPath, cpk.CarePackProductNumber, resultPath)
                        autoAddCreatedPath = resultPath
                        'LogMessage("CarePackJIT : SetUpAutoAdd : Creating AutoAdd path " & resultPath)
                        Dim quantity = New clsQuantity(agentAccount.SellerChannel.Region, resultPath, carePackBranch, 1, 0, 0, False, Nothing)
                    End If

                End If

            Next

        End If
        'LogMessage("CarePackJIT : SetUpAutoAdd : autoAddCreatedPath " & autoAddCreatedPath)
        Return autoAddCreatedPath

    End Function

    Private Sub BuildCarePackList(recommendedHPCarePacks As PQWS.CPCCarePack(), allHPCarePacks As PQWS.CPCCarePack(), allCarePacks As List(Of PQWS.CPCCarePack))

        ' The "Recommended" and "All" care pack lists are mutually exclusive so we need to process both
        If recommendedHPCarePacks IsNot Nothing Then
            allCarePacks.AddRange(recommendedHPCarePacks)
        End If

        If allHPCarePacks IsNot Nothing Then
            allCarePacks.AddRange(allHPCarePacks)
        End If

    End Sub

    Private Sub FindUnknownServiceLevels(ByRef allCarePacks As List(Of PQWS.CPCCarePack), ByRef newServiceLevels As List(Of String))

        For Each hpCarePack In allCarePacks

            Dim serviceLevel As clsServiceLevel = iq.ServiceLevels.Values.FirstOrDefault(Function(sl) (sl.ServiceLevel = hpCarePack.ServiceLevel))

            If serviceLevel Is Nothing Then

                ' We got a care pack with a service level we don't recognize
                newServiceLevels.Add(CStr(hpCarePack.ServiceLevel))

            End If

        Next

    End Sub

    Private Sub FindNewAndAmendedCarePacks(ByRef allCarePacks As List(Of PQWS.CPCCarePack), ByRef amendedCarePacks As List(Of PQWS.CPCCarePack), ByRef newCarePacks As List(Of PQWS.CPCCarePack), countryDetails As PQWS.CPCCountry, agentAccount As clsAccount, ByRef carepacksNotAdded As List(Of PQWS.CPCCarePack))
        For Each hpCarePack In allCarePacks

            Dim serviceLevel As clsServiceLevel = iq.ServiceLevels.Values.FirstOrDefault(Function(sl) (sl.ServiceLevel = hpCarePack.ServiceLevel))

            If Not serviceLevel Is Nothing AndAlso Not (serviceLevel.Disabled Or serviceLevel.PostWarranty) Then

                ' Look for new/amended care packs
                Dim carePackSKUCode = hpCarePack.CarePackProductNumber

                If iq.i_SKU.ContainsKey(carePackSKUCode) Then
                    '  If HasCarePackChanged(hpCarePack, iq.i_SKU(carePackSKUCode), countryDetails, agentAccount, serviceLevel) Then
                    amendedCarePacks.Add(hpCarePack)
                    'End If
                Else
                    newCarePacks.Add(hpCarePack)

                End If
            Else
                ''LogMessage("CarePackJIT : FindNewAndAmendedCarePacks : carepacksNotAdded " & IIf(hpCarePack.CarePackProductNumber IsNot Nothing, hpCarePack.CarePackProductNumber, ""))
                carepacksNotAdded.Add(hpCarePack)
            End If
        Next
        'LogMessage("CarePackJIT : FindNewAndAmendedCarePacks : allCarePcks: " & allCarePacks.Count & " , amendedCarePacks:  " & amendedCarePacks.Count & " , newCarePacks :" & newCarePacks.Count & " , notAdded :" & carepacksNotAdded.Count)
        For Each cpk In carepacksNotAdded
            allCarePacks.Remove(cpk)
        Next

    End Sub

    Private Sub FindDeletedCarePacks(ByRef hwSupportBranch As clsBranch, ByRef allCarePacks As List(Of PQWS.CPCCarePack), ByRef deletedCarePacks As List(Of String))

        Dim cpkSKUs As List(Of String) = (From f In hwSupportBranch.childBranches.Values Where f.Product IsNot Nothing Select f.Product.SKU).ToList()

        Dim newSKUs As List(Of String) = (From n In allCarePacks Select n.CarePackProductNumber).ToList()

        For Each sku In cpkSKUs
            If newSKUs.Contains(sku) Then
                deletedCarePacks.Add(sku)

            End If
        Next

        'For Each carePackBranch As clsBranch In hwSupportBranch.childBranches.Values

        '    If Not carePackBranch.Product Is Nothing Then

        '        Dim sku As String = carePackBranch.Product.SKU
        '        Dim toDelete As Boolean = True

        '        For Each hpCarePack In allCarePacks
        '            If String.Equals(hpCarePack.CarePackProductNumber, sku, StringComparison.InvariantCultureIgnoreCase) Then
        '                Dim serviceLevel As clsServiceLevel = iq.ServiceLevels.Values.FirstOrDefault(Function(sl) (sl.ServiceLevel = hpCarePack.ServiceLevel))

        '                If Not (serviceLevel.Disabled Or serviceLevel.PostWarranty) Then
        '                    toDelete = False
        '                End If
        '                Exit For
        '            End If
        '        Next

        '        If toDelete Then
        '            If Not deletedCarePacks.Contains(sku) Then
        '                deletedCarePacks.Add(sku)
        '            End If
        '        End If
        '    End If
        'Next

    End Sub

    Private Function HasCarePackChanged(hpCarePack As PQWS.CPCCarePack, carePack As clsProduct, countryDetails As PQWS.CPCCountry, agentAccount As clsAccount, serviceLevel As clsServiceLevel) As Boolean

        Dim changed As Boolean = False

        If carePack.Manufacturer <> serviceLevel.Manufacturer Then changed = True

        Return changed

    End Function

    ' Creates an IQ2 care pack product from HP care pack details
    Private Function CreateOrAmendCarePack(carePack As clsProduct, carePackBranch As clsBranch, hpCarePack As PQWS.CPCCarePack, serviceLevel As clsServiceLevel, carePackRootBranch As clsBranch, agentAccount As clsAccount, countryDetails As PQWS.CPCCountry, lid As ULong, errorMessages As List(Of String)) As clsBranch

        Dim carePackSKUCode = hpCarePack.CarePackProductNumber

        If carePack Is Nothing Then
            carePack = New clsProduct(carePackSKUCode, False, True, iq.i_sector_code("HPBCS"), iq.i_ProductType_Code("wty"), DateTime.Now, DateTime.Now.AddYears(5), True, False, True, serviceLevel.MfrCode, Nothing, Nothing)
            AuditLog.Instance.Add(AuditType.Information, String.Format("Care Pack SKU {0} created", carePackSKUCode), errorMessages, lid)
        End If

        If carePackBranch Is Nothing Then
            If Not (serviceLevel.PostWarranty Or serviceLevel.Disabled) Then

                Dim translation As clsTranslation = iq.AddTranslation(carePackSKUCode, English, "CPQ", 0, Nothing, 0, False)
                carePackBranch = New clsBranch(carePack, carePackRootBranch, translation, String.Empty, translation, translation, Nothing, 0, False, "YTGB")

            End If
        End If

        If carePack.Manufacturer <> serviceLevel.Manufacturer Then
            carePack.mfrCode = serviceLevel.MfrCode
        End If

        ' Create and assign new product attributes

        If Not (CreateCarePackAttributes(carePack, hpCarePack, serviceLevel, agentAccount, errorMessages)) Then
            Return Nothing
        End If

        ' Make sure the care pack branch has a slot
        If Not carePackBranch Is Nothing Then
            If carePackBranch.slots.Count = 0 Then
                Dim newSlot = New clsSlot(iq.i_slotType_Code("wty")("carepack"), carePackBranch, String.Empty, -1, Nothing, New NullableInt(), 0, 0)
            ElseIf carePackBranch.slots.Count > 1 Then
                Return Nothing      ' Shouldn't be more than one slot here
            End If
        End If

        carePack.update(errorMessages)

        Return carePackBranch

    End Function

    Private Sub SendServiceLevelsEmail(newServiceLevels As List(Of String), errorMessages As List(Of String))

        Dim smtpclient As New SmtpClient
        Dim address As String = iq.Addresses("iQuoteSupportEmail").Translation.text(English)

        Dim sb As New StringBuilder()
        sb.AppendLine("<h2>New HP Service Levels returned by PQWS</h2>")
        For Each serviceLevel In newServiceLevels
            sb.AppendLine(serviceLevel)
        Next

        Dim msg As New MailMessage(address, address, "New HP Service Level returned by PQWS", sb.ToString())
        msg.IsBodyHtml = True
        msg.Priority = MailPriority.High
        smtpclient.ServicePoint.MaxIdleTime = 1

        Try
            smtpclient.Send(msg)
        Catch ex As Exception
            errorMessages.Add("Unable to send email at this time")
        End Try

    End Sub

    ' Creates IQ2 care pack product attributes from the HP care pack and the service level details held in IQ
    Private Function CreateCarePackAttributes(carePack As clsProduct, hpCarePack As PQWS.CPCCarePack, serviceLevel As clsServiceLevel, agentAccount As clsAccount, ByRef errorMessages As List(Of String)) As Boolean

        CreateCarePackAttributes = False
        Dim numericValue As Integer = 0
        If carePack.Attributes Is Nothing Then carePack.Attributes = New Dictionary(Of Integer, clsProductAttribute)()
        If carePack.i_Attributes_Code Is Nothing Then carePack.i_Attributes_Code = New Dictionary(Of String, List(Of clsProductAttribute))()

        ' Clear out any old attributes
        'Dim attributeCodes As New List(Of Integer)
        'For Each pa As clsProductAttribute In carePack.Attributes.Values
        '    attributeCodes.Add(pa.ID)
        'Next
        'For i As Integer = 0 To attributeCodes.Count - 1
        '    carePack.Attributes(attributeCodes(i)).delete(errorMessages)
        'Next

        Dim attribute As clsAttribute
        Dim translation As clsTranslation
        Dim productAttribute As clsProductAttribute

        ' Always create a description attribute
        attribute = iq.i_attribute_code("desc")
        Dim att As clsProductAttribute = (From at In carePack.Attributes.Values Where at.Attribute Is attribute).FirstOrDefault

        translation = iq.AddTranslation(hpCarePack.ServiceDescription, agentAccount.Language, "CPQ", 0, Nothing, 0, False)
        AmmendAttribute(att, carePack, translation, 0, iq.i_unit_code("txt"), agentAccount, attribute, numericValue, errorMessages)

        ' Always create a duration attribute
        attribute = iq.i_attribute_code("capacity")
        att = (From at In carePack.Attributes.Values Where at.Attribute Is attribute).FirstOrDefault
        Dim durUnit As Integer
        Dim unitCode As clsUnit

        If serviceLevel.Duration Mod 12 > 0 Then
            translation = iq.AddTranslation(String.Format("{0} mth", serviceLevel.Duration), English, "CPQ", 0, Nothing, 0, False)
            durUnit = serviceLevel.Duration
            unitCode = iq.i_unit_code("num")
        Else
            Dim years As Integer = CInt(serviceLevel.Duration / 12)
            translation = iq.AddTranslation(String.Format("{0} yr", years), English, "CPQ", 0, Nothing, 0, False)
            durUnit = years
            unitCode = iq.i_unit_code("year")
        End If
        AmmendAttribute(att, carePack, translation, durUnit, unitCode, agentAccount, attribute, numericValue, errorMessages)

        ' Loop through all the ServiceLevel/Attribute mappings looking for further ProductAttributes to add
        For Each code As String In iq.ServiceLevelAttributeMap.Keys
            numericValue = 0
            attribute = Nothing
            productAttribute = Nothing

            Dim description As clsTranslation = Nothing
            attribute = iq.ServiceLevelAttributeMap(code)
            Select Case code.ToLower()

                Case "fk_servicetype_id"
                    If Not serviceLevel.ServiceType Is Nothing Then
                        If serviceLevel.MfrCode = "HPI" Then
                            attribute = iq.i_attribute_code("servicedelivery")
                            'Else
                            '    attribute = iq.ServiceLevelAttributeMap(code)
                        End If


                        description = serviceLevel.ServiceType.Title
                    End If

                Case "fk_response_id"
                    If Not serviceLevel.Response Is Nothing Then
                        'attribute = iq.ServiceLevelAttributeMap(code)
                        description = serviceLevel.Response.Title
                    End If

                Case "hpedmr"
                    If serviceLevel.MfrCode = "HPE" Then
                        attribute = iq.i_attribute_code("DMR_ISS")
                        If serviceLevel.HpeDmr Then
                            description = iq.AddTranslation("DMR", English, "CPQ", 0, Nothing, 0, False)
                        Else
                            If serviceLevel.HpeCdmr Then
                                description = iq.AddTranslation("CDMR", English, "CPQ", 0, Nothing, 0, False)
                            Else
                                description = iq.AddTranslation("No DMR", English, "CPQ", 0, Nothing, 0, False)
                            End If

                        End If
                    End If
                Case "hpiadp"
                    '    attribute = iq.i_attribute_code("options")
                    If serviceLevel.HpiAdp Then
                        numericValue = 1
                    End If
                    description = iq.AddTranslation("ADP", English, "CPQ", 0, Nothing, 0, False)
                Case "hpidmr"
                    '   attribute = iq.i_attribute_code("options")
                    If serviceLevel.HpiDmr Then
                        numericValue = 1
                    End If
                    description = iq.AddTranslation("DMR", English, "CPQ", 0, Nothing, 0, False)
                Case "hpitravel"
                    '  attribute = iq.i_attribute_code("options")
                    If serviceLevel.HpiTravel Then
                        numericValue = 1
                    End If
                    description = iq.AddTranslation("Travel", English, "CPQ", 0, Nothing, 0, False)
                Case "hpitracing"
                    ' attribute = iq.i_attribute_code("options")
                    If serviceLevel.HpiTracing Then
                        numericValue = 1
                    End If
                    description = iq.AddTranslation("Tracing", English, "CPQ", 0, Nothing, 0, False)
                Case "hpitheft"
                    'attribute = iq.i_attribute_code("options")
                    If serviceLevel.HpiTheft Then
                        numericValue = 1
                    End If
                    description = iq.AddTranslation("Theft", English, "CPQ", 0, Nothing, 0, False)
            End Select


            If attribute IsNot Nothing AndAlso description IsNot Nothing Then
                att = (From at In carePack.Attributes.Values Where at.Attribute Is attribute).FirstOrDefault

                AmmendAttribute(att, carePack, description, 0, iq.i_unit_code("txt"), agentAccount, attribute, numericValue, errorMessages)
                If attribute.Code = "DMR_ISS" Then
                    attribute = iq.i_attribute_code("options")
                    att = (From at In carePack.Attributes.Values Where at.Attribute Is attribute).FirstOrDefault
                    AmmendAttribute(att, carePack, description, 0, iq.i_unit_code("txt"), agentAccount, attribute, numericValue, errorMessages)
                    '   productAttribute = New clsProductAttribute(carePack, attribute, 0, iq.i_unit_code("txt"), description)
                End If

            End If
        Next

        CreateCarePackAttributes = True

    End Function

    ' Vets whether the HP Care Pack passes the quality check
    Private Function ValidateHPCarePacks(hpCarePackResults As PQWS.CPCHierarchyCarePackResults) As Boolean

        ValidateHPCarePacks = False

        ' Data is valid if more than a certain number of care packs is returned
        If Not hpCarePackResults Is Nothing Then

            Dim count As Integer = 0

            If Not hpCarePackResults.RecommendedHPCarePacks Is Nothing Then count = hpCarePackResults.RecommendedHPCarePacks.Length
            If Not hpCarePackResults.AllHPCarePacks Is Nothing Then count += hpCarePackResults.AllHPCarePacks.Length

            ' Ensure we received more than the configured minimum no. of care packs
            Dim min As Integer = 50
            If Not ConfigurationManager.AppSettings("MinHPCarePacks") Is Nothing Then
                min = Convert.ToInt32(ConfigurationManager.AppSettings("MinHPCarePacks"))
            End If
            If count >= min Then ValidateHPCarePacks = True

        End If

    End Function

    Public Function CarePackJIT_Old(request As clsGenericAjaxRequest, Optional importregion As clsRegion = Nothing) As List(Of String)
        Try
            If request.BranchPath Is Nothing Then Exit Function
            Dim agentAccount = iq.seshTyped(Of clsAccount)(request.lid, "AgentAccount")
            Dim buyerAccount = iq.seshTyped(Of clsAccount)(request.lid, "BuyerAccount")
            Dim errorMEssages As List(Of String) = New List(Of String)()
            If request.BranchPath = "tree.1" Then request.BranchPath = CStr(iq.sesh(request.lid, "treecursor"))
            If request.BranchPath Is Nothing Then Exit Function

            Dim createdTROBranch As Boolean = False
            Dim autoAddCreated As String = Nothing


            If iq.Branches.ContainsKey(CInt(Split(request.BranchPath, ".").Last)) Then
                Dim branch = iq.Branches(CInt(Split(request.BranchPath, ".").Last))
                Dim sysPath As String = ""
                branch = branch.FindSystemAbove(request.BranchPath, sysPath)
                If branch Is Nothing Then Exit Function
                sysPath = Left(request.BranchPath, request.BranchPath.IndexOf(branch.ID) + Len(branch.ID.ToString))
                If branch.Product IsNot Nothing Then
                    Dim tgtList As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)() 'Contains the sku and a reference to the branch int he AllCPQ tree
                    Dim srcList As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)() 'contains the sku and a reference to the branch under the hw support section

                    Dim CPQRootBranch As clsBranch = iq.i_SpecialBranches("cpqroot")

                    'Find HW Support
                    Dim hwsupportpath As String = Left(sysPath, Len(sysPath) - Len(Split(sysPath, ".").Last) - 1)
                    Dim hwsupportBranch As clsBranch = CType(branch.FindBranchByNameBelow("HW Support", Left(sysPath, Len(sysPath) - Len(Split(sysPath, ".").Last) - 1), True, 12, hwsupportpath), clsBranch)

                    If hwsupportBranch Is Nothing Then
                        'Create it
                        Dim svcBranchPath = ""
                        Dim svcbranch = branch.FindBranchByNameBelow("Services", "", True, 12, svcBranchPath)
                        If svcbranch Is Nothing Then
                            Dim aoBranchPath = ""
                            Dim aoBranch = branch.FindBranchByNameBelow("All Options", "", True, 12, aoBranchPath)
                            svcbranch = New clsBranch(Nothing, CType(aoBranch, clsBranch), iq.AddTranslation("Services", English, "", 0, Nothing, 0, False), "", iq.AddTranslation("Options", English, "", 0, Nothing, 0, False), iq.AddTranslation("Option", English, "", 0, Nothing, 0, False), Nothing, 40, False, "Y")
                        End If
                        hwsupportBranch = New clsBranch(Nothing, CType(svcbranch, clsBranch), iq.AddTranslation("HW Support", English, "", 0, Nothing, 0, False), "", iq.AddTranslation("Options", English, "", 0, Nothing, 0, False), iq.AddTranslation("Option", English, "", 0, Nothing, 0, False), Nothing, 0, False, "B")
                    End If

                    For Each bb In hwsupportBranch.childBranches
                        If bb.Value.Product IsNot Nothing Then
                            If srcList.ContainsKey(bb.Value.Product.SKU) Then
                                'bb.Value.delete(errorMEssages) 'Duplicate, not sure why??
                            Else
                                If agentAccount Is Nothing Then
                                    agentAccount = iq.seshTyped(Of clsAccount)(request.lid, "AgentAccount")
                                End If

                                If CBool(Not bb.Value.PruneInForce(hwsupportpath & "." & bb.Value.ID, agentAccount.SellerChannel)) Then
                                    If Not String.IsNullOrEmpty(bb.Value.Product.SKU) Then
                                        srcList.Add(bb.Value.Product.SKU, bb.Value)
                                    End If

                                End If

                            End If
                        End If

                    Next

                    'Deal with the TRO's and autoadds here 
                    'Server rule time in iq1 parlance
                    'Get family major code
                    Dim fm = ""
                    If branch.Product.i_Attributes_Code.ContainsKey("FamMajor") Then
                        fm = branch.Product.i_Attributes_Code("FamMajor").First.Translation.text(English)
                    End If

                    Dim troresp
                    Dim autoresp = "24x7, 4hr"
                    Dim autoserv = "Foundation Care"
                    Dim troserv
                    Dim duration = 3
                    Dim options = "No DMR"

                    ' Tro checks
                    If fm.StartsWith("ML1") Or fm.StartsWith("ML31") Or fm.StartsWith("MS0") Or fm.StartsWith("DL320e") Or fm.StartsWith("DL160") Then
                        troresp = "Next Business Day"
                        troserv = "Proactive Care"
                    Else
                        troresp = "24x7, 4hr"
                        troserv = "Proactive Care"
                    End If

                    'AutoAdd checks 
                    For Each r In {"[D|M]L3[0-9]+eG8", "DL16[0|5]G[7|8|9]", "DL1[6|8]0G9", "DL120G[6|7]", "ML10", "ML110G7", "MS001"}
                        Dim rex As Regex = New Regex(r)
                        If rex.IsMatch(fm) Then
                            autoresp = "Next Business Day"
                            Exit For
                        End If
                    Next

                    Dim trocpqs As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)()

                    'End TRO and autoadd Setup


                    Dim ToCreate As List(Of String) = New List(Of String)()

                    'Go get info
                    Dim con = New SqlConnection("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35")
                    con.Open()
                    Dim params As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
                    Dim retVals As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
                    params.Add("HWsku", branch.Product.SKU)
                    params.Add("countryCode", agentAccount.SellerChannel.Region.Code)
                    Dim rdr = dataAccess.da.ExecuteSP(con, "products.[CarePackFinder]", params, retVals)
                    While rdr.Read
                        If Not IsDBNull(rdr("ccDescription")) Then
                            'Do we have this care pack in iq2?
                            Dim cpqBranch As clsBranch = New clsBranch()
                            If iq.i_SKU.ContainsKey(CStr(rdr("CPKpartnum"))) AndAlso iq.i_SKU(CStr(rdr("CPKpartnum"))).Branches.Count > 0 Then
                                cpqBranch = iq.i_SKU(CStr(rdr("CPKpartnum"))).Branches.FirstOrDefault  'CPQ should only be on 1 branch and grafted everywhere else...
                                If Not tgtList.ContainsKey(rdr("CPKpartnum").ToString.Trim()) Then tgtList.Add(rdr("CPKpartnum").ToString.Trim(), cpqBranch)

                            Else
                                'No create it
                                ToCreate.Add(dataAccess.da.SqlEncode(CStr(rdr("CPKpartnum"))))
                            End If

                            If {"DTO", "NBK"}.Contains(branch.Product.ProductType.Code) Then 'PPS items DTO and NBK go in here 
                                If CInt(rdr("cpkranking")) = 1 And cpqBranch.Quantities.Values.Count = 0 Then
                                    '  End If
                                    'If Not cpqBranch.Quantities.Values.Where(Function(q) (String.IsNullOrEmpty(q.Path) OrElse q.Path.Contains(sysPath)) AndAlso q.Region.Encompasses(agentAccount.BuyerChannel.Region) AndAlso q.NumPreInstalled = 1 AndAlso q.FOC = False).Count > 0 Then
                                    Dim resultPath = ""
                                    branch.findChildBySKU2(sysPath, CStr(rdr("CPKpartnum")), resultPath)
                                    Dim q = New clsQuantity(agentAccount.BuyerChannel.Region, resultPath, cpqBranch, 1, 0, 0, False, Nothing)
                                    AuditLog.Instance.Add(AuditType.Information, "CarePack SKU Qty record" & rdr("CPKpartnum") & " Syspath " & sysPath, errorMEssages, "")
                                    autoAddCreated = resultPath

                                End If

                            Else
                                'Dim test = From q In cpqBranch.Quantities.Values Where String.IsNullOrEmpty(q.Path) AndAlso q.Region.Encompasses(agentAccount.BuyerChannel.Region) AndAlso q.NumPreInstalled = 1 AndAlso q.FOC = False
                                'If cpqBranch.Product.ProductType.Code = "SWD" Then
                                '    Dim abc = autoresp
                                '    Dim opt = options

                                'End If
                                'Auto adds
                                If cpqBranch IsNot Nothing AndAlso cpqBranch.Product IsNot Nothing AndAlso cpqBranch.Product.i_Attributes_Code.ContainsKey("servicelevel") AndAlso
                                    cpqBranch.Product.i_Attributes_Code("servicelevel").First.Translation.text(English) = autoserv AndAlso
                                    cpqBranch.Product.i_Attributes_Code.ContainsKey("response") AndAlso cpqBranch.Product.i_Attributes_Code("response").First.Translation.text(English) = autoresp AndAlso
                                    cpqBranch.Product.i_Attributes_Code.ContainsKey("DMR_ISS") AndAlso cpqBranch.Product.i_Attributes_Code("DMR_ISS").First.Translation.text(English) = options AndAlso
                                    cpqBranch.Product.i_Attributes_Code.ContainsKey("capacity") AndAlso cpqBranch.Product.i_Attributes_Code("capacity").First.NumericValue = duration Then
                                    'This is an auto add so check its quantity record
                                    Dim x = From z In cpqBranch.Quantities.Values Where z.Region.Encompasses(agentAccount.BuyerChannel.Region) AndAlso z.NumPreInstalled = 1 AndAlso z.FOC = False
                                    If x.Count > 1 Then

                                        AuditLog.Instance.Add(AuditType.Information, "CarePack SKUmultiple qty record for preisntalled" & rdr("CPKpartnum") & " Syspath " & sysPath, errorMEssages, "")
                                    End If

                                    If Not cpqBranch.Quantities.Values.Where(Function(q) (String.IsNullOrEmpty(q.Path) OrElse q.Path.Contains(sysPath)) AndAlso q.Region.Encompasses(agentAccount.BuyerChannel.Region) AndAlso q.NumPreInstalled = 1 AndAlso q.FOC = False).Count > 0 Then
                                        Dim resultPath = ""
                                        branch.findChildBySKU2(sysPath, CStr(rdr("CPKpartnum")), resultPath)
                                        Dim q = New clsQuantity(agentAccount.BuyerChannel.Region, resultPath, cpqBranch, 1, 0, 0, False, Nothing)
                                        AuditLog.Instance.Add(AuditType.Information, "CarePack SKU Qty record" & rdr("CPKpartnum") & " Syspath " & sysPath, errorMEssages, "")
                                        autoAddCreated = resultPath
                                    End If
                                End If
                            End If

                            If branch.Product.ProductType.Code = "SVR" Then
                                If cpqBranch IsNot Nothing AndAlso cpqBranch.Product IsNot Nothing AndAlso cpqBranch.Product.i_Attributes_Code.ContainsKey("servicelevel") AndAlso cpqBranch.Product.i_Attributes_Code("servicelevel").First.Translation.text(English) = troserv AndAlso cpqBranch.Product.i_Attributes_Code.ContainsKey("response") AndAlso cpqBranch.Product.i_Attributes_Code("response").First.Translation.text(English) = troresp AndAlso cpqBranch.Product.i_Attributes_Code.ContainsKey("options") AndAlso cpqBranch.Product.i_Attributes_Code("options").First.Translation.text(English) = options AndAlso cpqBranch.Product.i_Attributes_Code.ContainsKey("capacity") AndAlso cpqBranch.Product.i_Attributes_Code("capacity").First.NumericValue = duration Then trocpqs.Add(cpqBranch.Product.SKU, cpqBranch)
                            ElseIf {"DTO", "NBK"}.Contains(branch.Product.ProductType.Code) Then
                                If cpqBranch IsNot Nothing AndAlso CInt(rdr("cpkranking")) = 2 Then trocpqs.Add(cpqBranch.Product.SKU, cpqBranch)
                            End If
                        End If
                    End While
                    rdr.Close()
                    con.Close()
                    'Find TRO branch
                    Dim troPath As String = ""
                    Dim troBranch As clsBranch = CType(branch.FindBranchByNameBelow("HP Top Recommended", sysPath, False, 12, troPath), clsBranch)
                    Dim troCPQBranch As clsBranch
                    If troBranch Is Nothing Then
                        troBranch = New clsBranch(Nothing, CType(branch, clsBranch), iq.AddTranslation("Top Recommended", English, "", 0, Nothing, 0, False), "", iq.AddTranslation("Top Recommended", English, "", 0, Nothing, 0, False), iq.AddTranslation("Top Recommended", English, "", 0, Nothing, 0, False), Nothing, 0, False, "H")
                        troPath = sysPath & "." & troBranch.ID
                        createdTROBranch = True
                    End If
                    troCPQBranch = CType(branch.FindBranchByNameBelow("Care Pack", sysPath, False, 12, troPath), clsBranch)

                    If troCPQBranch Is Nothing Then
                        'Create
                        troCPQBranch = New clsBranch(Nothing, troBranch, iq.AddTranslation("Care Pack", English, "", 0, Nothing, 0, False), "/images/product/category/cat2.png", iq.AddTranslation("HP Top Recommended", English, "", 0, Nothing, 0, False), iq.AddTranslation("HP Top Recommended", English, "", 0, Nothing, 0, False), Nothing, 0, False, "I")
                    End If

                    Dim hasList = New List(Of String)
                    Dim dupChildBranch As List(Of clsBranch) = New List(Of clsBranch)
                    For Each child As clsBranch In troCPQBranch.childBranches.Values
                        If CBool(Not child.PruneInForce(troPath & "." & child.ID, agentAccount.SellerChannel)) Then
                            If Not trocpqs.ContainsKey(child.Product.SKU) Then
                                'Remove (delete or prune, not sure)
                                Dim p = New clsPrune(troPath & "." & child.ID, New NullableInt(agentAccount.SellerChannel.ID), "CPQJIT")
                            Else
                                If Not hasList.Contains(child.Product.SKU) Then
                                    hasList.Add(child.Product.SKU)
                                Else ' duplicate tro item
                                    '  Dim p = New clsPrune(troPath & "." & child.ID, New NullableInt(agentAccount.SellerChannel.ID), "CPQJIT")
                                    dupChildBranch.Add(child)
                                End If
                            End If
                        Else
                            If trocpqs.ContainsKey(child.Product.SKU) Then
                                For Each prune As clsPrune In child.Prunes.Values
                                    If prune.Path = troPath & "." & child.ID AndAlso (prune.ChannelID.value Is Nothing OrElse IsDBNull(prune.ChannelID.value) OrElse prune.ChannelID.value = agentAccount.SellerChannel.ID) Then prune.delete()
                                Next
                            End If
                        End If
                    Next

                    'For Each br In dupChildBranch
                    '    Dim priceCount = troCPQBranch.childBranches(br.ID).Quantities.Count
                    '    'delete quantities
                    '    For i = 0 To priceCount - 1
                    '        Dim qty = troCPQBranch.childBranches(br.ID).Quantities.Values.First
                    '        troCPQBranch.childBranches(br.ID).Quantities(qty.ID).Delete(errorMEssages)
                    '    Next
                    '    'Delte slots
                    '    Dim slotsCount = troCPQBranch.childBranches(br.ID).slots.Count
                    '    For i = 0 To slotsCount - 1
                    '        Dim slot = troCPQBranch.childBranches(br.ID).slots.Values.First
                    '        troCPQBranch.childBranches(br.ID).slots(slot.ID).delete(errorMEssages)
                    '    Next
                    '    troCPQBranch.childBranches(br.ID).delete(errorMEssages)

                    'Next

                    For Each tro In trocpqs.Keys.Except(hasList)
                        'Dim troitem = New clsBranch(trocpqs(tro).Product, troCPQBranch, iq.AddTranslation(trocpqs(tro).Product.sku,English,"",0,Nothing,0,False),"",iq.AddTranslation("HP Top Recommended", English, "", 0, Nothing, 0, False),iq.AddTranslation("HP Top Recommended", English, "", 0, Nothing, 0, False),Nothing,0,False,"I"))
                        For Each prne In trocpqs(tro).Prunes.Where(Function(p) Not IsDBNull(p.Value.ChannelID.value) AndAlso p.Value.ChannelID.value = agentAccount.SellerChannel.ID AndAlso (String.IsNullOrEmpty(p.Value.Path) Or p.Value.Path = troPath & "." & trocpqs(tro).ID))
                            prne.Value.delete()
                        Next
                        troCPQBranch.Graft(trocpqs(tro), "TROCPQ", troPath, errorMEssages)
                    Next

                    If ToCreate.Count > 0 Then
                        Dim listSKUs = New List(Of String)

                        Dim iq2con = dataAccess.da.OpenDatabase()
                        Dim nextBId As Integer = 0
                        Dim pawc = Nothing ' dataAccess.da.MakeWriteCacheFor(iq2con, "ProductAttribute")
                        Dim swc = Nothing 'dataAccess.da.MakeWriteCacheFor(iq2con, "Slot")
                        Dim bwc = Nothing 'dataAccess.da.MakeWriteCacheFor(iq2con, "Branch", nextBId, True)
                        Dim nextKey = 0 'clsTranslation.NextKey
                        Dim twc = Nothing 'dataAccess.da.MakeWriteCacheFor(iq2con, "Translation")

                        ''' Could expand to education etc, only does HW support at the moment ML
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
                         "WHERE OptTypeName='HW Support' and options.optsku IN (" & Join(ToCreate.ToArray, ",") & ")"
                        If Not iq.i_attribute_code.ContainsKey("Tracing") Then Dim d = New clsAttribute("Tracing", iq.AddTranslation("Tracing", English, "", 0, CType(twc, DataTable), nextKey, True), 0)
                        If Not iq.i_attribute_code.ContainsKey("ADP") Then Dim d = New clsAttribute("ADP", iq.AddTranslation("ADP", English, "", 0, CType(twc, DataTable), nextKey, True), 0)
                        If Not iq.i_attribute_code.ContainsKey("CTR") Then Dim d = New clsAttribute("CTR", iq.AddTranslation("CTR", English, "", 0, CType(twc, DataTable), nextKey, True), 0)

                        Dim rdr2 = dataAccess.da.DBExecuteReader(con, sql2)
                        While rdr2.Read
                            Dim prod As clsProduct
                            AuditLog.Instance.Add(AuditType.Information, "CarePack SKU created" & rdr2("optsku"), errorMEssages, "")

                            If Not iq.i_SKU.ContainsKey(CStr(rdr2("optsku"))) Then
                                prod = New clsProduct(rdr2("optsku").ToString, False, True, iq.i_sector_code("HPBCS"), iq.i_ProductType_Code(CStr(rdr2("opttype"))), DateTime.Now, DateTime.Now.AddYears(5), True, False, True, buyerAccount.mfrCode, "", "")
                            Else
                                prod = iq.i_SKU(CStr(rdr2("optsku")))
                            End If

                            Dim cpqBranch = New clsBranch(prod, CPQRootBranch, iq.AddTranslation(rdr2("optsku").ToString, English, "CPQ", 0, CType(twc, DataTable), nextKey, False), "", iq.AddTranslation("Carepacks", English, "", 0, CType(twc, DataTable), nextKey, False), iq.AddTranslation("Carepack", English, "", 0, CType(twc, DataTable), nextKey, False), Nothing, 0, False, "B", bwc, nextBId)
                            Dim a = New clsProductAttribute(prod, iq.i_attribute_code("mfrSKU"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("optsku").ToString, English, "CPQ", 0, CType(twc, DataTable), nextKey, False), pawc)
                            If Not IsDBNull(rdr2("description")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("desc"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("description").ToString, English, "CPQ", 0, CType(twc, DataTable), nextKey, False), pawc)
                            If Not IsDBNull(rdr2("duration")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("capacity"), CInt(rdr2("duration")), iq.i_unit_code("year"), Nothing, pawc)

                            If Not IsDBNull(rdr2("servicelevel")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("servicelevel"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("servicelevel").ToString, English, "CPQ", 0, CType(twc, DataTable), nextKey, False), pawc)
                            If Not IsDBNull(rdr2("response")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("response"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("response").ToString, English, "CPQ", 0, CType(twc, DataTable), nextKey, False), pawc)
                            'this is ISS (Servers and Storage Device) DMR
                            If Not IsDBNull(rdr2("options")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("DMR_ISS"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr2("options").ToString, English, "CPQ", 0, CType(twc, DataTable), nextKey, False), pawc)
                            If Not IsDBNull(rdr2("tfs")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("twentyfourseven"), rdr2("tfs"), iq.i_unit_code("txt"), iq.AddTranslation("24x7", English, "CPQ", 0, CType(twc, DataTable), nextKey, False), pawc)
                            If Not IsDBNull(rdr2("travel")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("travel"), rdr2("travel"), iq.i_unit_code("txt"), Nothing, pawc)
                            If Not IsDBNull(rdr2("Tracing")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("tracing"), rdr2("Tracing"), iq.i_unit_code("txt"), Nothing, pawc)
                            If Not IsDBNull(rdr2("ADP")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("ADP"), rdr2("ADP"), iq.i_unit_code("txt"), Nothing, pawc)
                            'this PPS(Desktop and NoteBook) DMR
                            If Not IsDBNull(rdr2("DMR")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("DMR"), rdr2("DMR"), iq.i_unit_code("txt"), Nothing, pawc)
                            If Not IsDBNull(rdr2("OnSite")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("OnSite"), rdr2("OnSite"), iq.i_unit_code("txt"), Nothing, pawc)
                            If Not IsDBNull(rdr2("CTR")) Then Dim b = New clsProductAttribute(prod, iq.i_attribute_code("CTR"), rdr2("CTR"), iq.i_unit_code("txt"), Nothing, pawc)

                            Dim s = New clsSlot(iq.i_slotType_Code(CStr(rdr2("OPTTYPE")))(CStr(rdr2("OPTFAMILY"))), cpqBranch, "", -1, Nothing, New NullableInt(), 0, 0, swc)


                            If Not tgtList.ContainsKey(CStr(rdr2("optsku"))) Then tgtList.Add(CStr(rdr2("optsku")), CType(cpqBranch, clsBranch))
                            listSKUs.Add(CStr(rdr2("optsku")))
                            ToCreate.Remove(dataAccess.da.SqlEncode(CStr(rdr2("optsku"))))
                        End While

                        For Each sku In ToCreate
                            If Not iq.i_SKU.ContainsKey(sku) Then
                                Dim prod = New clsProduct(sku.Trim(CChar("'")).Trim(), False, True, iq.i_sector_code("HPBCS"), iq.i_ProductType_Code("WTY"), DateTime.Now, DateTime.Now.AddYears(5), True, False, True, buyerAccount.mfrCode, "", "")
                                Dim cpqBranch = New clsBranch(prod, CPQRootBranch, iq.AddTranslation(sku.Trim(CChar("'")).Trim(), English, "CPQ", 0, CType(twc, DataTable), nextKey, True), "", iq.AddTranslation("Carepacks", English, "", 0, CType(twc, DataTable), nextKey, True), iq.AddTranslation("Carepack", English, "", 0, CType(twc, DataTable), nextKey, True), Nothing, 0, False, "B", bwc, nextBId)

                                Dim a = New clsProductAttribute(prod, iq.i_attribute_code("mfrSKU"), 0, iq.i_unit_code("txt"), iq.AddTranslation(sku.Trim(CChar("'")).Trim(), English, "CPQ", 0, CType(twc, DataTable), nextKey, False), pawc)

                                If Not tgtList.ContainsKey(sku.Trim(CChar("'")).Trim()) Then tgtList.Add(sku.Trim(CChar("'")).Trim(), CType(cpqBranch, clsBranch))
                            End If
                        Next

                        'dataAccess.da.BulkWrite(iq2con, twc, "Translation")
                        'dataAccess.da.BulkWrite(iq2con, bwc, "Branch")
                        'dataAccess.da.BulkWrite(iq2con, pawc, "ProductAttribute")
                        'dataAccess.da.BulkWrite(iq2con, swc, "Slot")

                        Try

                            Dim cl As wsconsumer.I_UniTranClient = New wsconsumer.I_UniTranClient()
                            'cl.Endpoint.Binding.OpenTimeout = TimeSpan.FromSeconds(5)
                            'cl.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(10)

                            'Dim SKUlist() As String = cl.AllProducts(agentAccount.SellerChannel.Code)
                            Dim WSRQKey As String = agentAccount.Priceband.text
                            If iq.SeshContains(request.lid, "gk_SessionID") Then
                                WSRQKey &= ";" & iq.sesh(request.lid, "gk_sessionID")
                            End If

                            Dim lps = cl.ListPrices(buyerAccount.SellerChannel.Region.Code, buyerAccount.Currency.Code, listSKUs.ToArray)

                            Dim sp = Nothing
                            If CBool(Not buyerAccount.SellerChannel.priceConfig And 2) Then
                                'is there a webservice
                                If CBool((buyerAccount.SellerChannel.priceConfig And 8)) Then
                                    Dim unirequest As IQ.wsconsumer.clsStockPriceRequest = New wsconsumer.clsStockPriceRequest()
                                    unirequest = cl.BuildRequest(buyerAccount.SellerChannel.Code, buyerAccount.Priceband.text, CStr(buyerAccount.User.ID), CStr(request.lid), buyerAccount.Currency.Code, "", WSRQKey, listSKUs.ToArray, "", buyerAccount.User.Email, "iquote2")
                                    Dim handle = cl.RequestStockPrices(unirequest)
                                    If handle <> -1 Then sp = cl.CheckStockPrices(handle, True, 30)
                                End If
                            End If


                            For Each sku In listSKUs
                                If iq.i_SKU.ContainsKey(sku) Then
                                    Dim newListVariant As clsVariant = New clsVariant("list", iq.i_SKU(sku), HP, sku, "List Price", "", "", buyerAccount.SellerChannel.Region, False, Nothing, -1)
                                    Dim price As Single = 0
                                    For Each lp In lps
                                        If lp.SKU = sku Then
                                            price = lp.price
                                        End If
                                    Next
                                    Dim p = New clsPrice(newListVariant, iq.priceBands(""), New NullablePrice(price, buyerAccount.Currency, True), "")

                                    price = 0
                                    If sp IsNot Nothing Then
                                        For Each itm In sp.items
                                            If itm.sku = sku Then
                                                price = itm.ListPrice
                                            End If
                                        Next

                                    End If
                                    If price <> 0 Then
                                        Dim newChanVariant As clsVariant = New clsVariant("", iq.i_SKU(sku), buyerAccount.SellerChannel, sku, "", "", "", buyerAccount.SellerChannel.Region, False, Nothing, -1)
                                        p = New clsPrice(newChanVariant, buyerAccount.Priceband, New NullablePrice(price, buyerAccount.Currency, False), "")
                                    End If
                                End If

                            Next

                        Catch ex As Exception
                            ErrorLog.Add(ex)

                        End Try 'dont fail on unitran failure...
                    End If

                    Dim changed As Boolean = False
                    If ToCreate.Count > 0 Then

                        'Things that shouldn't be there
                        For Each sku In srcList.Keys.Except(tgtList.Keys)
                            'Prune at this point?
                            changed = True
                            Dim p = New clsPrune(hwsupportpath & "." & srcList(sku).ID, New NullableInt(agentAccount.SellerChannel.ID), "CPQJIT")
                        Next

                        For Each sku In tgtList.Keys.Except(srcList.Keys)

                            changed = True
                            hwsupportBranch.Graft(tgtList(sku), "CPQJIT", hwsupportpath, errorMEssages, Nothing)
                        Next

                    End If

                    Dim toreturn As List(Of String) = New List(Of String)()
                    If changed Then
                        Dim shs = iq.seshTyped(Of Dictionary(Of String, clsScreenHeader))(request.lid, "screenHeaders")
                        If shs IsNot Nothing AndAlso shs.ContainsKey(hwsupportpath) Then
                            shs.Remove(hwsupportpath)
                        End If
                        If shs IsNot Nothing AndAlso shs.ContainsKey(troPath) Then
                            shs.Remove(troPath)
                        End If

                        If iq.sesh(request.lid, "pathDataLoaded") IsNot Nothing AndAlso iq.seshTyped(Of List(Of String))(request.lid, "pathDataLoaded").Contains(hwsupportpath) Then iq.sesh(request.lid, "pathDataLoaded").Remove(hwsupportpath)
                        If iq.sesh(request.lid, "pathDataLoaded") IsNot Nothing AndAlso iq.seshTyped(Of List(Of String))(request.lid, "pathDataLoaded").Contains(troPath) Then iq.sesh(request.lid, "pathDataLoaded").Remove(troPath)
                        toreturn.Add("cmd=openTab&path=" & troPath)
                    End If

                    If autoAddCreated IsNot Nothing Then
                        toreturn.Add("addpart:" & autoAddCreated)
                    End If

                    If createdTROBranch Then
                        toreturn.Add("refreshall")
                    End If

                    If toreturn.Count > 0 Then Return toreturn Else Return Nothing



                End If
            End If

        Catch ex As Exception
            ErrorLog.Add(ex)
        End Try
    End Function

    Private Sub CreateCarePackVariants(carePackList As List(Of PQWS.CPCCarePack), lid As ULong, agentAccount As clsAccount, buyerAccount As clsAccount)
        Try
            Dim cl As wsconsumer.I_UniTranClient = New wsconsumer.I_UniTranClient()
            Dim WSRQKey As String = agentAccount.Priceband.text
            If iq.SeshContains(lid, "gk_SessionID") Then
                WSRQKey &= ";" & iq.sesh(lid, "gk_sessionID")
            End If

            Dim sp = Nothing
            ' If Not buyerAccount.SellerChannel.priceConfig And 2 Then
            Dim skuArray() As String = (From c In carePackList Select c.CarePackProductNumber).ToArray()
            'is there a webservice
            If CBool((buyerAccount.SellerChannel.priceConfig And 8)) Then
                Dim unirequest As IQ.wsconsumer.clsStockPriceRequest = New wsconsumer.clsStockPriceRequest()
                unirequest = cl.BuildRequest(buyerAccount.SellerChannel.Code, buyerAccount.Priceband.text, CStr(buyerAccount.User.ID), CStr(lid), buyerAccount.Currency.Code, "", WSRQKey, skuArray, "", buyerAccount.User.Email, "iquote2")
                Dim handle = cl.RequestStockPrices(unirequest)
                If handle <> -1 Then sp = cl.CheckStockPrices(handle, True, 30)
            End If
            'End If

            Dim carePack As clsProduct
            For Each cpk In carePackList
                If iq.i_SKU.ContainsKey(cpk.CarePackProductNumber) Then
                    'carePack = New clsProduct()
                    carePack = iq.i_SKU(cpk.CarePackProductNumber)
                    Dim price As Single = CSng(cpk.PriceLocalList)
                    Dim p As clsPrice
                    Dim SKUvariant = carePack.Variants.Values.Where(Function(v) v.HasListPrice(buyerAccount.SellerChannel.DefaultCurrency))
                    If SKUvariant.Count = 0 Then
                        Dim newListVariant As clsVariant = New clsVariant("list", carePack, HP, carePack.SKU, "List Price", "", "", buyerAccount.SellerChannel.Region, False, Nothing, -1)
                        p = New clsPrice(newListVariant, iq.priceBands(""), New NullablePrice(price, buyerAccount.Currency, True), "CarePackJIT")
                    End If
                    price = 0
                    If sp IsNot Nothing Then
                        For Each itm In sp.items
                            If itm.sku = carePack.SKU Then
                                price = itm.customerPrice
                                Exit For
                            End If
                        Next
                    End If
                    If price <> 0 Then
                        Dim SKUvariant2 = carePack.Variants.Values.Where(Function(v) v.Product.SKU = carePack.SKU And v.sellerChannel.Code = buyerAccount.SellerChannel.Code)
                        If SKUvariant2.Count = 0 Then
                            Dim newChanVariant As clsVariant = New clsVariant("", carePack, buyerAccount.SellerChannel, carePack.SKU, "", "", "", buyerAccount.SellerChannel.Region, False, Nothing, -1)
                            p = New clsPrice(newChanVariant, buyerAccount.Priceband, New NullablePrice(price, buyerAccount.Currency, False), "CarePackJIT")

                        End If

                    End If
                End If
            Next

        Catch ex As Exception
            ErrorLog.Add(ex)

        End Try 'dont fail on unitran failure...
    End Sub

    Private Sub AmmendAttribute(att As clsProductAttribute, carePack As clsProduct, description As clsTranslation, p4 As Integer, clsUnit As clsUnit, agentAccount As clsAccount, attribute As clsAttribute, numericValue As Integer, errorMessages As List(Of String))
        Try

            If att Is Nothing OrElse att.Translation Is Nothing OrElse att.Translation.text(agentAccount.Language) Is Nothing Then
                Dim productAttribute = New clsProductAttribute(carePack, attribute, numericValue, iq.i_unit_code("txt"), description)
            ElseIf att.Translation.text(agentAccount.Language) <> description.text(agentAccount.Language) Then
                carePack.Attributes(att.ID).delete(errorMessages)
                Dim productAttribute = New clsProductAttribute(carePack, attribute, numericValue, iq.i_unit_code("txt"), description)
            End If
        Catch ex As Exception
            ErrorLog.Add(ex)
        End Try

    End Sub

    'Private Sub 'LogMessage(message As String)

    '    If (Not log4net.LogManager.GetRepository().Configured) Then
    '        XmlConfigurator.Configure()
    '    End If
    '    log.Info(message)

    'End Sub

    Public Sub DeleteAllCarePacks()
        Dim carePackRootBranch As clsBranch = iq.i_SpecialBranches("cpqroot")
        Dim errors As List(Of String) = New List(Of String)
        Dim listOfProducts As List(Of String) = New List(Of String)
        Dim listOfBranches As List(Of String) = New List(Of String)
        For Each cpk In carePackRootBranch.childBranches.Values
            Try
                Dim counts As New Dictionary(Of String, Integer) 'total numbers of records by type affected
                Dim summary As String = ""
                If cpk.HasSKU Then
                    'Dim prod As clsProduct = iq.i_SKU(cpk.SKU)
                    'prod.isDeleted = True
                    'prod.update(errors)
                    listOfProducts.Add(cpk.SKU)
                End If
                '                cpk.deleted = True
                '               cpk.Update(errors)
                listOfBranches.Add(CStr(cpk.ID))
            Catch ex As Exception
                'LogMessage(ex.Message)

            End Try

        Next

        Dim otherProds() As String = (From p In iq.Products.Values Where p.ProductType.Code = "wty" Or p.ProductType.Code = "hwsw" Or p.ProductType.Code = "svc" Or p.ProductType.Code = "edu" Select p.SKU).ToArray()

        Dim prods As String = "'" & Join(listOfProducts.ToArray(), "','") & "'"

        Dim branches As String = Join(listOfBranches.ToArray(), ",")
        If branches IsNot Nothing Then
            da.DBExecutesql("update branch set deleted=1 where id in (" & branches & ")")

        End If
        If prods.Length > 3 Then
            da.DBExecutesql("update Product set deleted=1 where sku in (" & prods & ")")
        End If
        prods = "'" & Join(otherProds.ToArray(), "','") & "'"
        da.DBExecutesql("update Product set deleted=1 where sku in (" & prods & ")")
    End Sub
    Public Sub AddAllCarePacks(lid As ULong)

        'log = LogManager.GetLogger("IQOffline")
        'LogMessage("AddAllCarePacks : Start")
        Dim startTime As Date = Now
        Dim errorMessages As List(Of String) = New List(Of String)
        Dim allProds As List(Of clsProduct) = (From p In iq.Products.Values Where p.isSystem = True And p.isOption = False).ToList()
        Dim agentAccount = iq.seshTyped(Of clsAccount)(lid, "AgentAccount")
        Dim buyerAccount = iq.seshTyped(Of clsAccount)(lid, "BuyerAccount")
        If Not IsPQWSActive() Then Exit Sub
        'LogMessage("AddAllCarePacks : PQWSActive")
        Dim intLoop As Integer = 0

        For Each sysProd In allProds
            Try

                'LogMessage("AddAllCarePacks : SysSKU" & sysProd.SKU)


                For Each systemBranch In sysProd.Branches
                    For Each sysPath In systemBranch.AllPaths
                        Dim troAmended As Boolean = False
                        Dim carePackScreen As clsScreen = New clsScreen()
                        If systemBranch.Product.mfrCode.ToUpper() = "HPI" Then
                            carePackScreen = iq.i_screens_code("optCPKDTO")
                        Else
                            carePackScreen = iq.i_screens_code("optCPK")
                        End If
                        Dim hwSupportPath As String = Left(sysPath, Len(sysPath) - Len(Split(sysPath, ".").Last) - 1)
                        Dim hwSupportBranch As clsBranch = CType(systemBranch.FindBranchByNameBelow("HW Support", hwSupportPath, True, 12, hwSupportPath), clsBranch)
                        If hwSupportBranch Is Nothing Then

                            ' Couldn't find the Hardware Support branch - create it under the Services branch
                            Dim servicesBranchPath = String.Empty
                            Dim servicesBranch = systemBranch.FindBranchByNameBelow("Services", "", True, 12, servicesBranchPath)
                            If servicesBranch Is Nothing Then

                                ' Couldn't find the Services branch; locate via the All Options branch
                                Dim allOptionsBranchPath = String.Empty
                                Dim allOptionsBranch = systemBranch.FindBranchByNameBelow("All Options", "", True, 12, allOptionsBranchPath)
                                servicesBranch = New clsBranch(Nothing, CType(allOptionsBranch, clsBranch), iq.AddTranslation("Services", English, "", 0, Nothing, 0, False), "", iq.AddTranslation("Options", English, "", 0, Nothing, 0, False), iq.AddTranslation("Option", English, "", 0, Nothing, 0, False), Nothing, 40, False, "Y")

                            End If

                            If Not servicesBranch Is Nothing Then
                                hwSupportBranch = New clsBranch(Nothing, CType(servicesBranch, clsBranch), iq.AddTranslation("HW Support", English, "", 0, Nothing, 0, False), "", iq.AddTranslation("Options", English, "", 0, Nothing, 0, False), iq.AddTranslation("Option", English, "", 0, Nothing, 0, False), carePackScreen, 0, False, "B")
                            End If

                        End If
                        If hwSupportBranch.Matrix Is Nothing Then
                            hwSupportBranch.Matrix = carePackScreen
                            hwSupportBranch.Update(errorMessages)
                        End If

                        Dim troPath = String.Empty
                        Dim troBranch As clsBranch = CType(systemBranch.FindBranchByNameBelow("Top Recommended", sysPath, False, 12, troPath), clsBranch)
                        If troBranch Is Nothing Then

                            ' Couldn't find the Top Recommended Options branch - create it
                            troBranch = New clsBranch(Nothing, systemBranch, iq.AddTranslation("Top Recommended", English, "", 0, Nothing, 0, False), "", iq.AddTranslation("Top Recommended", English, "", 0, Nothing, 0, False), iq.AddTranslation("Top Recommended", English, "", 0, Nothing, 0, False), Nothing, 0, False, "H")
                            troPath = sysPath & "." & troBranch.ID

                        End If
                        Dim troCpqBranch As clsBranch = CType(troBranch.FindBranchByNameBelow("Care Pack", troPath, False, 12, troPath), clsBranch)
                        If troCpqBranch Is Nothing Then

                            ' Couldn't find the Top Recommended Options/Care Pack branch - create it
                            troCpqBranch = New clsBranch(Nothing, troBranch, iq.AddTranslation("Care Pack", English, "", 0, Nothing, 0, False), "/images/product/category/cat2.png", iq.AddTranslation("Top Recommended", English, "", 0, Nothing, 0, False), iq.AddTranslation("Top Recommended", English, "", 0, Nothing, 0, False), Nothing, 0, False, "I")
                            troPath = troPath & "." & troCpqBranch.ID
                        End If
                        Dim skuVariant As clsVariant
                        Dim autoAddCreatedPath As String = Nothing
                        RefreshPQWSCarePacks(systemBranch.Product, sysPath, hwSupportBranch, troCpqBranch, agentAccount, autoAddCreatedPath, troAmended, lid, hwSupportPath, systemBranch, troPath, errorMessages)
                        If iq.CarePackLastRefresh.ContainsKey(sysProd.SKU) Then
                            iq.CarePackLastRefresh(sysProd.SKU) = DateTime.Now
                        Else
                            iq.CarePackLastRefresh.Add(sysProd.SKU, DateTime.Now)
                        End If
                    Next  'sysPath
                Next 'systemBranch
                intLoop = intLoop + 1

            Catch ex As Exception

            End Try
        Next 'sysProd
        'LogMessage("AddAllCarePacks : Total Carepacks added " & intLoop)
        Dim ti As TimeSpan = Now - startTime
        'LogMessage("AddAllCarePacks : Total Carepacks added " & ti.ToString("d : hh :mm:ss"))
        'Dim partPAth = ""
        'Dim part = systemBranch.findChildBySKU2(Path, rdr("addsku"), partPAth)
        'If part IsNot Nothing Then
        '    If part.Quantities.Values.Where(Function(q) (String.IsNullOrEmpty(q.Path) OrElse q.Path = partPAth) AndAlso q.NumPreInstalled > 0 AndAlso q.Region.Encompasses(iq.i_region_code(rdr("CountryCode").replace("UK", "GB"))) AndAlso q.FOC = False).Count = 0 Then
        '        Dim q = New clsQuantity(iq.i_region_code(rdr("countrycode").replace("UK", "GB")), partPAth, part, 1, 0, 0, False, Nothing)
        '    End If
        'End If


    End Sub

    Public Sub AddUnknownServiceLevel(serviceLevelList As List(Of String), mfrCode As String)
        Dim conString As String = ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString
        For Each newLevel In serviceLevelList
            Dim serviceLevel As Integer = Convert.ToInt32(newLevel)
            Using con As SqlConnection = New SqlConnection(conString)
                con.Open()
                Using command As SqlCommand = New SqlCommand()
                    command.CommandText = "sp_AddNewPQWSServiceLevel"
                    command.CommandType = CommandType.StoredProcedure
                    command.Connection = con
                    command.Parameters.Add(New SqlParameter("@serviceLevel", serviceLevel))
                    command.Parameters.Add(New SqlParameter("@mfrCode", mfrCode))
                    command.ExecuteNonQuery()
                End Using
            End Using
        Next
    End Sub
End Module


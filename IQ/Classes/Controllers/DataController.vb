Imports System.Net
Imports System.Net.Http
Imports System.Web.Http
Imports System.Linq
Imports System.Data.SqlClient
Imports System.Reflection


Public Class DataController
    Inherits ApiController

#Region "CustomizableField"

    <ActionName("GetAvailableFields")>
    <HttpPost>
    Public Function GetAvailableFields(request As clsGenericAjaxRequest) As List(Of clsAccountScreenField)
        'Get a list of all fields from the screen and populate with any overrides for this user

        Dim l As Dictionary(Of String, clsScreenHeader) = iq.sesh(request.lid, "screenHeaders")
        Return l(request.BranchPath).AllAvailableFields
    End Function

    <HttpPost>
    <ActionName("SetFieldOverride")>
    Public Function SetFieldOverride(request As SetFieldOverrideRequest) As String
        'Set the override settings for this users account and screen, update OM and DB

        Dim l As clsAccount = iq.sesh(request.lid, "BuyerAccount")
        Dim scr As clsScreenOverride = iq.ScreenOverrides.Where(Function(so) so.AccountID = l.ID And so.ScreenID = request.ScreenId And so.FieldId = request.FieldId And so.Path = request.BranchPath).FirstOrDefault()
        If scr Is Nothing Then
            scr = New clsScreenOverride(l.ID, request.ScreenId, request.BranchPath, request.FieldId, request.ForceVisibilityTo, request.ForceOrderTo, request.ForceWidthTo, request.ForceSortTo, request.ForceFilterTo, Nothing)
            scr.Insert()
        End If

        If request.ForceVisibilityTo IsNot Nothing Then scr.ForceVisibilityTo = request.ForceVisibilityTo
        If request.ForceOrderTo IsNot Nothing Then scr.ForceOrderTo = request.ForceOrderTo
        If request.ForceWidthTo IsNot Nothing Then scr.ForceWidthTo = request.ForceWidthTo
        If request.ForceSortTo IsNot Nothing Then scr.ForceSortTo = request.ForceSortTo
        If request.ForceFilterTo IsNot Nothing Then scr.ForceFilterTo = request.ForceFilterTo ' this is Not yet implemented in the GUI as not spec'd, just putting it in for when it is...
        If Not scr.Update() Then Return Nothing

        Dim bi = New clsBranchInfo(request.lid, request.BranchPath, Nothing, 0, enumParadigm.errorNotSet, Nothing)
        bi.InvalidateMatrixBelow(request.BranchPath, True)

        Return request.BranchPath

    End Function

    <HttpPost>
   <ActionName("CloneTargets")>
    Public Function CloneTargets(request As CloneTargetsRequest) As String
        'Get the source screen
        Dim errorMessages As List(Of String) = New List(Of String)
        Dim scOrig As clsScreen = iq.Screens(request.ScreenId)
        Dim bi

        If request.Targets.Count > 0 Then
            For Each targ In request.Targets
                If Len(targ) > 2 AndAlso Right(targ, 3) = "All" Then
                    Continue For
                End If
                bi = New clsBranchInfo(request.lid, targ, Nothing, 0, enumParadigm.errorNotSet, errorMessages)
                bi.Branch.Matrix = scOrig
                bi.Branch.Update(errorMessages)
            Next
        Else
            'Get all branches with this level and value
            For Each br In iq.Branches.Where(Function(b) b.Value.Translation.Group = request.Level And b.Value.Translation.text(English) = request.LevelValue).ToList()
                br.Value.Matrix = scOrig
                br.Value.Update(errorMessages)
            Next

        End If
        Return String.Join(",", errorMessages)
    End Function

    <HttpPost>
    <ActionName("ResetFieldOverride")>
    Public Function ResetFieldOverride(request As clsGenericAjaxRequest) As String
        'Resets (removes) any overrides at this level
        Dim matrixHeaders As Dictionary(Of String, clsScreenHeader) = iq.sesh(request.lid, "matrixHeaders")
        Dim matrixHeader As clsScreenHeader = matrixHeaders(request.BranchPath)
        Dim l As clsAccount = iq.sesh(request.lid, "BuyerAccount")
        iq.ScreenOverrides.RemoveAll(Function(so) so.AccountID = l.ID And so.ScreenID = request.ScreenId And so.Path = request.BranchPath)

        matrixHeader.InvalidateFields()

        If dataAccess.da.DBExecutesql("DELETE FROM AccountScreenOverride WHERE FK_Account_Id = " & l.ID & " AND FK_Screen_ID=" & request.ScreenId & " AND Path = " & dataAccess.da.SqlEncode(request.BranchPath)) <= 0 Then Return Nothing

        Return request.BranchPath
    End Function

    <HttpPost>
    <ActionName("SwitchOverrideFieldOrder")>
    Public Function SwitchOverrideFieldOrder(request As clsGenericAjaxRequest) As String

        'Find current dest field order
        Dim matrixHeaders As Dictionary(Of String, clsScreenHeader) = iq.sesh(request.lid, "screenHeaders")

        If matrixHeaders Is Nothing Then Return Nothing
        Dim matrixHeader As clsScreenHeader = matrixHeaders(request.BranchPath)
        Dim destField As clsField = matrixHeader.EffectiveFields.Where(Function(f) f.ID = request.DestinationFieldId).FirstOrDefault()
        Dim sourceField As clsField = matrixHeader.EffectiveFields.Where(Function(f) f.ID = request.SourceFieldId).FirstOrDefault()
        Dim l As clsAccount = iq.sesh(request.lid, "BuyerAccount")

        If destField Is Nothing Or sourceField Is Nothing Then
            Return Nothing
        End If

        'Move all fields below this one down, this does mean creating an override for every field, can't think of another way at the moment...
        Dim ord As Integer = matrixHeader.FieldResultSet(destField).Order + 1
        For Each v In matrixHeader.FieldResultSet.Where(Function(a) a.Value.Order >= matrixHeader.FieldResultSet(destField).Order).OrderBy(Function(a) a.Value.Order).ToList()
            If v.Value.HasScreenOverride Then
                Dim s = iq.ScreenOverrides.Where(Function(f) f.AccountID = l.ID And f.ScreenID = request.ScreenId And f.Path = request.BranchPath And f.FieldId = v.Value.FieldId).FirstOrDefault()
                s.ForceOrderTo = ord
                s.Update()
            Else
                Dim dd = New clsScreenOverride(l.ID, request.ScreenId, request.BranchPath, v.Value.FieldId, Nothing, ord, Nothing, Nothing, Nothing, Nothing)
                dd.Insert()
            End If
            ord += 1
        Next
        If matrixHeader.FieldResultSet(sourceField).HasScreenOverride Then
            Dim s = iq.ScreenOverrides.Where(Function(f) f.AccountID = l.ID And f.ScreenID = request.ScreenId And f.Path = request.BranchPath And f.FieldId = sourceField.ID).FirstOrDefault()
            s.ForceOrderTo = matrixHeader.FieldResultSet(destField).Order
            s.Update()
        Else
            Dim dd = New clsScreenOverride(l.ID, request.ScreenId, request.BranchPath, sourceField.ID, Nothing, matrixHeader.FieldResultSet(destField).Order, Nothing, Nothing, Nothing, Nothing)
            dd.Insert()
        End If

        matrixHeader.InvalidateFields()

        Return request.BranchPath

    End Function

    <HttpPost>
    <ActionName("GetClonableTargets")>
    Function GetClonableTargets(request As clsGenericAjaxRequest)
        Dim l As clsAccount = iq.sesh(request.lid, "BuyerAccount")
        Dim errorMessages As List(Of String) = New List(Of String)
        'Break this out to a utiltiy?

        Dim bi = New clsBranchInfo(request.lid, request.BranchPath, Nothing, 0, enumParadigm.errorNotSet, errorMessages)
        Dim s As Integer
        Dim c As Integer

        Return bi.visibleChildren(errorMessages, False, s, c, False, False).Select(Function(h) h.Value).Select(Function(dd) New With {.Path = request.BranchPath & "." & dd.branch.ID.ToString(), .Name = dd.branch.DisplayName(l.Language)}).ToList()
    End Function

    <HttpPost>
    <ActionName("GetClonableGroups")>
    Function GetClonableGroups(request As clsGenericAjaxRequest)
        Dim l As clsAccount = iq.sesh(request.lid, "BuyerAccount")
        'Break this out to a utiltiy?

        Return iq.Branches.Where(Function(b) b.Value.Translation.Group.Contains("OL")).GroupBy(Function(b) b.Value.Translation.Group).ToDictionary(Function(b) b.Key, Function(e) e.Select(Function(d) d.Value.Translation.text(l.Language)).Distinct()) '.Select(Function(di) New clsCloneData With {.Level = di.Key, .Values = di.Value.ToList()})


    End Function

    <HttpPost>
    <ActionName("SetScreenDefaults")>
    Function SetScreenDefaults(request As clsGenericAjaxRequest) As Boolean
        'Get user logon info
        Dim l As clsAccount = iq.sesh(request.lid, "BuyerAccount")
        Dim errorMessages As List(Of String) = New List(Of String)

        If Not AccountHasRight(request.lid, "GLOBALADM") AndAlso (iq.seshDic(request.lid).ContainsKey("ElevatedKey") AndAlso iq.sesh(request.lid, "ElevatedKey") <> request.elid) Then Return False

        Dim scr = iq.ScreenOverrides.Where(Function(so) so.AccountID = l.ID And so.ScreenID = request.ScreenId And so.Path = request.BranchPath)
        For Each screenfield In scr
            If screenfield.ForceVisibilityTo IsNot Nothing Then iq.Screens(request.ScreenId).Fields(screenfield.FieldId).visibleList = screenfield.ForceVisibilityTo
            If screenfield.ForceOrderTo IsNot Nothing Then iq.Screens(request.ScreenId).Fields(screenfield.FieldId).order = screenfield.ForceOrderTo
            If screenfield.ForceWidthTo IsNot Nothing Then iq.Screens(request.ScreenId).Fields(screenfield.FieldId).width = screenfield.ForceWidthTo
            If screenfield.ForceFilterTo IsNot Nothing Then iq.Screens(request.ScreenId).Fields(screenfield.FieldId).defaultFilter = screenfield.ForceFilterTo
            If screenfield.ForceSortTo IsNot Nothing Then iq.Screens(request.ScreenId).Fields(screenfield.FieldId).defaultSort = screenfield.ForceSortTo
            iq.Screens(request.ScreenId).Fields(screenfield.FieldId).update(errorMessages)
        Next
        Return True

    End Function

    <HttpPost>
    <ActionName("CreateUniqueVersion")>
    Function CreateUniqueVersion(request As clsGenericAjaxRequest) As String
        'Create a screen copy 

        If request.ScreenTitle IsNot Nothing AndAlso iq.i_screens_code.ContainsKey(request.ScreenTitle) Then Return "This Screen Name is in Use"

        Dim l As clsAccount = iq.sesh(request.lid, "BuyerAccount")
        Dim errorMessages As List(Of String) = New List(Of String)

        Dim scr = iq.ScreenOverrides.Where(Function(so) so.AccountID = l.ID And so.ScreenID = request.ScreenId And so.Path = request.BranchPath)

        Dim scOrig As clsScreen = iq.Screens(request.ScreenId)
        Dim scTarget = scOrig.copy(errorMessages)
        If request.ScreenTitle IsNot Nothing Then
            scTarget.title = request.ScreenTitle
            scTarget.code = request.ScreenTitle
        End If

        For Each f As clsField In scTarget.Fields.Values
            Dim soo = scr.Where(Function(s) s.FieldId = f.ID).FirstOrDefault()
            If soo IsNot Nothing Then
                scTarget.Fields(f.ID).visibleList = soo.ForceVisibilityTo
                scTarget.Fields(f.ID).width = soo.ForceWidthTo
                scTarget.Fields(f.ID).order = soo.ForceOrderTo
            End If
        Next

        scTarget.Update(errorMessages)

        Dim bi = New clsBranchInfo(request.lid, request.BranchPath, Nothing, 0, enumParadigm.errorNotSet, errorMessages)
        bi.branch.Matrix = scTarget
        bi.branch.Update(errorMessages)

        AuditLog.Instance.Add(request.lid, "CreateUniqueVersion", errorMessages, "")
        If errorMessages.Count <> 0 Then Return String.Join(",", errorMessages)
        Return Nothing
    End Function

    <HttpPost>
    <ActionName("RemoveUniqueVersion")>
    Function RemoveUniqueVersion(request As clsGenericAjaxRequest) As Boolean
        Dim l As clsAccount = iq.sesh(request.lid, "BuyerAccount")
        Dim errorMessages As List(Of String) = New List(Of String)

        Dim bi = New clsBranchInfo(request.lid, request.BranchPath, Nothing, 0, enumParadigm.errorNotSet, errorMessages)
        bi.branch.Matrix = Nothing
        bi.branch.Update(errorMessages)

        AuditLog.Instance.Add(request.lid, "CreateUniqueVersion", errorMessages, "")
        If errorMessages.Count <> 0 Then Return False
    End Function
#End Region

    Public Function GetSystemMaintenanceUpdate()
        Return clsIQ.messages.ToList()
    End Function

    <HttpPost>
    <ActionName("GetAvailableUndos")>
    Function GetAvailableUndos(request As clsGenericAjaxRequest)
        Dim undoableActions = New List(Of String) From {"graft", "prune"}

        Dim l As clsAccount = iq.sesh(request.lid, "BuyerAccount")
        Dim con As SqlConnection = dataAccess.da.OpenDatabase(True)
        Dim dt As DataTable = New DataTable()
        dt.Load(dataAccess.da.DBExecuteReader(con, "SELECT * FROM AuditLog WHERE action in ('" + String.Join("','", undoableActions) + "') AND lid=" & request.lid.ToString()))

        Dim arr() As DataRow = dt.Select()
        'dt.Rows.CopyTo(arr, 0)

        Return arr.Select(Function(a) New With {.DateTime = a("DateTime"), .Id = a("Id"), .Action = a("Action"), .SourceBranch = a("SourcePath"), .TargetBranch = a("TargetPath")})

    End Function

    <ActionName("UndoAction")>
    <HttpPost>
    Function UndoAction(request As clsGenericAjaxRequest) As String
        'Get Details from DB
        Dim con As SqlConnection = dataAccess.da.OpenDatabase(True)
        Dim dt As DataTable = New DataTable()
        dt.Load(dataAccess.da.DBExecuteReader(con, "SELECT * FROM AuditLog WHERE id=" + request.ActionId.ToString() + " AND lid=" & request.lid.ToString()))
        Dim arr As DataRow = dt.Select()(0)

        'Do stuff
        Dim ty = Assembly.GetExecutingAssembly().CreateInstance("IQ.clsManip" + arr("Action").ToString().Substring(0, 1).ToUpper() + arr("Action").ToString().Substring(1, arr("Action").ToString().Length - 1))
        ty.TargetPath = arr("TargetPath")
        Dim i As Integer
        If Not Integer.TryParse(arr("SourcePath").ToString(), i) Then ty.SourcePath = arr("SourcePath")
        ty.AuditId = request.ActionId
        ty.LoginId = request.lid

        ty.UndoAction()


        Return String.Empty
    End Function

    <ActionName("GetFilters")>
    <HttpPost>
    Function GetFilters(request As clsGenericAjaxRequest)
        Dim l As clsAccount = iq.sesh(request.lid, "BuyerAccount")
        Dim f = iq.Screens(request.ScreenId).Fields.ToList().Select(Function(fi) New With {.FieldName = fi.Value.labelText.text(l.Language), .FieldId = fi.Value.ID, .Order = fi.Value.order, .Filter = If(fi.Value.defaultFilter Is Nothing, Nothing, fi.Value.defaultFilter.Split(",")), .Translation = If(fi.Value.QuickFilterGroup Is Nothing, "", fi.Value.QuickFilterGroup.text(l.Language)), .WidgetUI = fi.Value.QuickFilterUItype})
        Dim t = iq.Filters.Values.ToList().Select(Function(fi) New With {.Value = fi.Code, .Text = fi.DisplayText.text(l.Language)})
        Return New With {.Filters = f, .Types = t}
    End Function
    <ActionName("SetFilters")>
    <HttpPost>
    Function SetFilters(request As clsFilterSetRequest) As String
        Dim screen = iq.Screens(request.ScreenID)
        Dim errorMessages As List(Of String) = New List(Of String)()

        For Each f In request.Fields
            If f.FieldId = 0 Then Continue For
            If f.Enabled Then
                screen.Fields(f.FieldId).defaultFilter = f.DefaultFilter
                screen.Fields(f.FieldId).QuickFilterUItype = f.FilterType

                If Not String.IsNullOrEmpty(f.TranslationGroup) Then
                    If screen.Fields(f.FieldId).QuickFilterGroup IsNot Nothing Then
                        screen.Fields(f.FieldId).QuickFilterGroup.Group = f.TranslationGroup
                        screen.Fields(f.FieldId).QuickFilterGroup.Order = f.Order
                    Else
                        screen.Fields(f.FieldId).QuickFilterGroup = iq.AddTranslation(f.TranslationGroup, English, f.TranslationGroup, f.Order, Nothing, 0, False)
                    End If
                Else
                    screen.Fields(f.FieldId).QuickFilterGroup = Nothing
                End If
            Else
                screen.Fields(f.FieldId).QuickFilterGroup = Nothing
            End If
            screen.Fields(f.FieldId).update(errorMessages)
        Next
        Return String.Join(",", errorMessages)
    End Function

    <ActionName("AcknowledgeValidation")>
    <HttpPost>
    Function AcknowledgeValidation(request As clsGenericAjaxRequest) As String
        Dim l As clsAccount = iq.sesh(request.lid, "BuyerAccount")
        l.Quotes(request.QuoteId).AcknowledgedValidations.Add(request.BranchPath)

        Return ""
    End Function

    <ActionName("GetLearnMoreText")>
   <HttpPost>
    Function GetLearnMoreText(request As clsGenericAjaxRequest) As String

        Dim buyerAccount As clsAccount = iq.sesh(request.lid, "BuyerAccount")

        Dim key = "learnMore"
        If buyerAccount.Manufacturer = Manufacturer.HPE Then
            key &= "HPE"
        ElseIf buyerAccount.Manufacturer = Manufacturer.HPI Then
            key &= "HPI"
        End If

        Return Xlt(key, buyerAccount.Language)

    End Function

    <ActionName("CarePackJIT")>
    <HttpPost>
    Function GetCarePacks(request As clsGenericAjaxRequest) As List(Of String)
        Return CarePackModule.CarePackJIT(request)
    End Function

    <ActionName("HideSystemMessage")>
    <HttpPost>
    Sub HideSystemMessage(request As clsGenericAjaxRequest)

        Dim agentAccount As clsAccount = iq.sesh(request.lid, "AgentAccount")
        Dim suppressKey = String.Format("Suppress{0}SystemMessages", agentAccount.mfrCode)
        iq.sesh(request.lid, suppressKey) = "Y"

    End Sub

End Class




Option Strict On

Imports System.Xml
Imports dataAccess
Imports System.Globalization


Public Class clsQuoteItem

    Implements ISqlBasedObject
    Private Const PleaseCreateNewVersionToChangeQuote As String = "Please create new version to change quote."
    Property ID As Integer
    Public quote As clsQuote
    Property Indent As Integer

    Property Branch As clsBranch
    Property SKUVariant As clsVariant

    Property Path As String       'The numeric (dotted) path to this item in the product tree
    Property Quantity As Integer
    Property BasePrice As NullablePrice  'Pre margin 'quoted'  - price (from webserivce, or wherever)
    Property ListPrice As NullablePrice ' contains a snapshot of  the list price at the time the quote was prepared

    Property OPG As nullableString
    Property Bundle As nullableString  'an OPG can have many bundles
    Property rebate As Decimal 'amount of cash off this item for this OPG

    Property Margin As Single

    Property Children As List(Of clsQuoteItem)
    Property Parent As clsQuoteItem

    Property Msgs As List(Of ClsValidationMessage) 'String 'error (or other) message against this quote line item
    Property IsPreInstalled As Boolean 'Means you cant remove it from the quote - or flex its quantities (adding will add a new quote item)
    Property Created As DateTime
    Property validate As Boolean 'Whether to include in validation (or not)

    Property Note As nullableString

    Property collapsed As Boolean
    Property ExpandedPanels As HashSet(Of panelEnum) ' panelEnum  'System = 1, Options = 2, Spec = 4, Validation = 8, Promo= 16
    Property order As Integer 'this is not yet persited

    'Property price As clsPrice

    Dim FlexButtonState As EnumHideButton
    Private ImportId As Integer
    Private allRulesQualified As Boolean = False
    Public specPanelOpen As Panel   'the slot summaries ('specs' are built at a system level 'outside' the quote items via quote.validate
    Public specPanelClosed As Panel

    Property dicslots As Dictionary(Of clsSlotType, clsSlotSummary)

    'Public Sub Clone(onToQuote As clsquote)

    '    'recursively clones this quote item and all it's children - creating copies with new ID's

    '    Dim anitem As clsquoteItem = Nothing

    '    'If Me IsNot Me.quote.RootItem Then
    '    'we don't clone the root item as the new (empty) quote already has one
    '    'makes a new copy of me (the quote item) onto 'ontqoquote'

    '    'this constructor is recursive ! - it makes a whole set of itesm 0 attached to the quote
    '    anitem = New clsquoteItem(Me, onToQuote)
    '    'End If

    '    '     For Each item In Me.Children
    '    '     item.Clone(onToQuote)
    '    '     Next

    'End Sub
    'bulid a dictionary of quanties,by prodRef, by system


    ''' <summary>
    ''' This method is called on the quote.rootitem after price updates for a variant have arrived - it recurses the entire quote updating the price on any QuoteItems which refer to this skuvariant
    ''' </summary>
    ''' <param name="skuvariant"></param>
    ''' <param name="price"></param>
    ''' <remarks></remarks>
    Public Sub updateQuotedPrice(skuvariant As clsVariant, price As NullablePrice)

        If Me.SKUVariant Is skuvariant AndAlso price.value >= 0 Then

            Me.BasePrice = price

            'With Me.BasePrice
            '    .value = price
            '    .isValid = price.isValid
            '    .isList = price.isList
            'End With
            'save this quoteItem
            Me.Update()
            'and continute to recures (as the same item may appear more than once in the quote

        End If

        For Each c In Me.Children.ToArray  'iterate over an array (copy) of the list  (attempt to avoid a 'collection  modified enumeration may not execute' )
            c.updateQuotedPrice(skuvariant, price)
        Next

    End Sub

    Public Function findSystemItems() As List(Of clsQuoteItem)

        'returns a flat list of the 'system' items (which are indepentely validated) - things like a Chassis

        findSystemItems = New List(Of clsQuoteItem)

        If Me.Parent IsNot Nothing AndAlso Me.Parent IsNot Me.quote.RootItem Then Exit Function

        If Not Me.Branch Is Nothing Then  'the root item has no branch (is just a placeholder)
            If Me.Branch.Product.isSystem(Me.Path) Then findSystemItems.Add(Me)
        End If



        For Each child In Me.Children
            findSystemItems.AddRange(child.findSystemItems)
        Next

    End Function


    Public Sub ApplyMargin(factor As Single, propagate As Boolean)

        Me.Margin = factor
        If propagate Then
            For Each c In Me.Children
                c.ApplyMargin(factor, propagate)
            Next
        End If

        '     Me.Update()

    End Sub

    Public Sub doWarnings(dicslots As Dictionary(Of clsSlotType, clsSlotSummary))

        'called for each top level item

        If Me.Branch.Product.isSystem(Me.Path) Then
            'this isn't pretty - but becuase of the generic nature of 'products' we don't know what they 'are' (Desktop, notebook, server, storage) - so we read *where* they are (from the first level of the tree) to derive their fundamental type
            'should probably just add a sysType attribute - but validation would be its only purpose (at present)
            Dim systype As String
            Dim p() = Split(Me.Path, ".")
            systype = LCase(iq.Branches(CInt(p(2))).Translation.text(English))

            If iq.ProductValidationsAssignment.ContainsKey(systype) Then

                'build list of components
                Dim parts = Me.listComponents(False)

                For Each ProdVal In iq.ProductValidationsAssignment(systype)
                    Dim pth As String = ""
                    Select Case ProdVal.ValidationType
                        Case enumValidationType.MustHave
                            'Split multiple opt types with a /
                            Dim pres As Boolean = False
                            For Each ot In ProdVal.RequiredOptType.Split("/".ToArray)
                                If String.IsNullOrEmpty(pth) Then pth = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", ot, True) 'only populate the path if it isnt already (for instances like PCI and MOD network cards where one system may have one one may have the other)

                                If parts.Where(Function(par) par.OptType Like ot).Count = 0 Then
                                    'Option type not present
                                ElseIf Not String.IsNullOrEmpty(ProdVal.OptionFamily) AndAlso parts.Where(Function(pa) pa.hasAttribute("optFam", ProdVal.OptionFamily, True)).Count = 0 Then
                                    'OptionFamily specified but does not exist

                                ElseIf Not String.IsNullOrEmpty(ProdVal.CheckAttributeValue) AndAlso parts.Where(Function(pa) pa.hasAttribute(ProdVal.CheckAttribute, ProdVal.CheckAttributeValue, True)).Count = 0 Then
                                    'Check attribute present but value not found
                                Else
                                    pres = True
                                End If
                            Next

                            If Not pres Then Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, {}, Nothing, "", Nothing, ProdVal))
                        Case enumValidationType.MustHaveProperty
                            Dim found As Boolean = False
                            If Me.Branch.Product.i_Attributes_Code.ContainsKey(ProdVal.CheckAttribute) AndAlso ((Me.Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.Translation IsNot Nothing AndAlso Me.Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.Translation.text(English) = ProdVal.CheckAttributeValue) Or Me.Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.NumericValue.ToString = ProdVal.CheckAttributeValue) Then found = True
                            For Each pa In parts
                                Dim prod As clsBranch = iq.Branches(CInt(Split(pa.Path, ".").Last))

                                If prod.Product.i_Attributes_Code.ContainsKey(ProdVal.CheckAttribute) AndAlso ((prod.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.Translation IsNot Nothing AndAlso prod.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.Translation.text(English) = ProdVal.CheckAttributeValue) Or prod.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.NumericValue.ToString = ProdVal.CheckAttributeValue) Then found = True
                            Next
                            If Not found Then Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split(ProdVal.RequiredOptType.Split("/".ToArray).First, ",")))
                            'Not implemented yet
                        Case enumValidationType.Slot 'redundant
                            Dim dics As System.Collections.Generic.KeyValuePair(Of clsSlotType, clsSlotSummary) = dicslots.ToList().Where(Function(ds) ds.Key.MajorCode = ProdVal.RequiredOptType).Select(Function(ff) ff).FirstOrDefault()

                            If dics.Value Is Nothing Then Continue For
                            pth = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", ProdVal.RequiredOptType, True)
                            If dics.Value.Given < dics.Value.taken Then Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split(dics.Key.Translation.text(English) & "," & dics.Value.taken.ToString & "," & dics.Value.Given.ToString, ","))) Else Me.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.greenTick, Nothing, ProdVal.CorrectMessage, "", 0, 0, Split("")))
                        Case enumValidationType.CapacityOverload 'redundant
                            Dim dics As System.Collections.Generic.KeyValuePair(Of clsSlotType, clsSlotSummary) = dicslots.ToList().Where(Function(ds) ds.Key.MajorCode = ProdVal.RequiredOptType).Select(Function(ff) ff).FirstOrDefault()
                            If dics.Value Is Nothing Then Continue For
                            If parts.Where(Function(par) par.OptType = ProdVal.RequiredOptType).Sum(Function(par) FindRecursive(par.Path, True).Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute).First().NumericValue) > dics.Value.TotalCapacity Then
                                pth = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", ProdVal.RequiredOptType, True)
                                Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split(dics.Key.Translation.text(English), ",")))
                            Else
                                Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, ProdVal.Message, ProdVal.CorrectMessage, "", 0, 0, Split("")))
                            End If
                        Case enumValidationType.Dependancy
                            If Not String.IsNullOrEmpty(ProdVal.DependantCheckAttribute) OrElse Not String.IsNullOrEmpty(ProdVal.CheckAttribute) Then
                                Dim foundSource As Boolean = False
                                Dim foundTarget As Boolean = False
                                For Each ot In ProdVal.DependantOptType.ToUpper.Split("/".ToArray)
                                    For Each i In parts.ToList().Select(Function(pa) pa.Path)
                                        Dim part As clsProduct = FindRecursive(i, True).Branch.Product 'Get associated product
                                        If (String.IsNullOrEmpty(ProdVal.CheckAttribute.ToUpper) OrElse part.i_Attributes_Code.ContainsKey(ProdVal.CheckAttribute.ToUpper)) And part.i_Attributes_Code("optType").Select(Function(f) f.Translation.text(English).ToUpper).Where(Function(ppp) ProdVal.RequiredOptType.ToUpper.StartsWith(ppp.ToUpper)).Count > 0 AndAlso (String.IsNullOrEmpty(ProdVal.CheckAttribute) OrElse part.i_Attributes_Code(ProdVal.CheckAttribute.ToUpper).Select(Function(f) If(f.Translation IsNot Nothing, f.Translation.text(English).ToUpper, f.NumericValue.ToString())).Contains(ProdVal.CheckAttributeValue.ToUpper)) Then
                                            'We have a product of this type, find if we have the dependant one...
                                            foundSource = True
                                        End If
                                        If (String.IsNullOrEmpty(ot) OrElse part.i_Attributes_Code("optType").Select(Function(f) f.Translation.text(English).ToUpper).Where(Function(ppp) ot.ToUpper.StartsWith(ppp.ToUpper)).Count > 0) AndAlso part.i_Attributes_Code.ContainsKey(ProdVal.DependantCheckAttribute.ToUpper) AndAlso part.i_Attributes_Code(ProdVal.DependantCheckAttribute.ToUpper).Select(Function(f) If(f.Translation IsNot Nothing, f.Translation.text(English).ToUpper, f.NumericValue.ToString())).Contains(ProdVal.DependantCheckAttributeValue.ToUpper) Then
                                            foundTarget = True
                                        End If
                                    Next
                                Next
                                pth = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", ProdVal.DependantOptType, True)
                                If foundSource AndAlso Not foundTarget Then Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split(""))) 'Else Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, Nothing, ProdVal.CorrectMessage, "", 0, 0, Split("")))
                            Else
                                If parts.Where(Function(par) par.OptType = ProdVal.RequiredOptType).Count > 0 AndAlso parts.Where(Function(par) par.OptType = ProdVal.DependantOptType).Count = 0 Then
                                    pth = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", ProdVal.DependantOptType, True)
                                    Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split("")))
                                Else
                                    Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, Nothing, ProdVal.CorrectMessage, "", 0, 0, Split("")))
                                End If
                            End If
                        Case enumValidationType.Mismatch
                            If parts.Where(Function(par) par.OptType = ProdVal.RequiredOptType).Count > 0 Then
                                Dim o1 As List(Of clsProductAttribute) = Nothing
                                Dim o2 As List(Of clsProductAttribute) = Nothing
                                For Each i In parts.ToList().Where(Function(pa) pa.OptType = ProdVal.RequiredOptType).Select(Function(pa) pa.Path)
                                    o2 = If(FindRecursive(i, True).Branch.Product.i_Attributes_Code.ContainsKey(ProdVal.CheckAttribute), FindRecursive(i, True).Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute), Nothing)
                                    If o2 IsNot Nothing AndAlso o2(0).NumericValue > 0 Then
                                        If o1 IsNot Nothing AndAlso o1.Select(Function(o1o) If(o1o.Translation Is Nothing, o1o.NumericValue.ToString, o1o.Translation.text(English))).Intersect(o2.Select(Function(o2o) If(o2o.Translation Is Nothing, o2o.NumericValue.ToString, o2o.Translation.text(English)))).Count <> o1.Count Then
                                            pth = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", ProdVal.RequiredOptType, True)
                                            Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split(ProdVal.RequiredOptType, ",")))
                                            Exit For
                                        End If
                                        o1 = o2
                                        o2 = Nothing
                                    End If
                                Next
                            Else
                                'Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, Nothing, ProdVal.CorrectMessage, "", 0, 0, Split("")))
                            End If
                        Case enumValidationType.NotToppedUp
                            Dim dics As System.Collections.Generic.KeyValuePair(Of clsSlotType, clsSlotSummary) = dicslots.ToList().Where(Function(ds) ds.Key.MajorCode = ProdVal.RequiredOptType).Select(Function(ff) ff).FirstOrDefault()
                            If dics.Value Is Nothing Then Continue For
                            pth = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", ProdVal.RequiredOptType, True)
                            If dics.Value.taken < dics.Value.Given Then Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split(dics.Key.Translation.text(English) & "," & dics.Value.taken.ToString & "," & dics.Value.Given.ToString, ","))) Else Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, Nothing, ProdVal.CorrectMessage, "", 0, 0, Split("")))
                        Case enumValidationType.MultipleRequred
                            If dicslots.Where(Function(par) par.Key.MajorCode = ProdVal.RequiredOptType).Sum(Function(ds) ds.Value.taken) Mod ProdVal.RequiredQuantity <> 0 Then
                                pth = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", ProdVal.RequiredOptType, True)
                                Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split("", ",")))
                            Else
                                Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, Nothing, ProdVal.CorrectMessage, "", 0, 0, Split("")))
                            End If
                        Case enumValidationType.UpperWarning
                            Dim dics As System.Collections.Generic.KeyValuePair(Of clsSlotType, clsSlotSummary) = dicslots.ToList().Where(Function(ds) ds.Key.MajorCode = ProdVal.RequiredOptType).Select(Function(ff) ff).FirstOrDefault()
                            If dics.Value Is Nothing Then Continue For
                            If dics.Value.taken > ProdVal.RequiredQuantity Then
                                pth = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", ProdVal.RequiredOptType, True)
                                Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split(dics.Key.Translation.text(English) & "," & dics.Value.taken.ToString & "," & dics.Value.Given.ToString, ",")))
                            Else
                                Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, Nothing, ProdVal.CorrectMessage, "", 0, 0, Split("")))
                            End If
                        Case enumValidationType.Exists
                            If parts.Where(Function(par) par.OptType = ProdVal.RequiredOptType).Count > 0 Then
                                pth = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", ProdVal.RequiredOptType, True)
                                Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split("")))
                            Else
                                Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, Nothing, ProdVal.CorrectMessage, "", 0, 0, Split("")))
                            End If
                        Case enumValidationType.AtLeastSameQuantity
                            pth = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", ProdVal.RequiredOptType, True)
                            If parts.Where(Function(par) par.OptType = ProdVal.RequiredOptType).Count > 0 AndAlso parts.Where(Function(par) par.OptType = ProdVal.DependantOptType).Count > 0 Then
                                If dicslots.Where(Function(ds) ds.Key.MajorCode = ProdVal.RequiredOptType).Sum(Function(ds) ds.Value.taken) > dicslots.Where(Function(ds) ds.Key.MajorCode = ProdVal.DependantOptType).Sum(Function(ds) ds.Value.taken) Then
                                    Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split("")))
                                End If
                            End If
                        Case enumValidationType.Divisible
                            Dim dicsone As System.Collections.Generic.KeyValuePair(Of clsSlotType, clsSlotSummary) = dicslots.ToList().Where(Function(ds) ds.Key.MajorCode = ProdVal.RequiredOptType).Select(Function(ff) ff).FirstOrDefault()
                            Dim dicstwo As System.Collections.Generic.KeyValuePair(Of clsSlotType, clsSlotSummary) = dicslots.ToList().Where(Function(ds) ds.Key.MajorCode = ProdVal.DependantOptType).Select(Function(ff) ff).FirstOrDefault()

                            If dicsone.Value Is Nothing OrElse dicstwo.Value Is Nothing Then Continue For
                            If dicstwo.Value.taken <> 0 AndAlso dicsone.Value.taken Mod dicstwo.Value.taken > 0 Then Me.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Validation, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, "", 0, 0, Split("")))
                        Case enumValidationType.SpecRequirement
                            'Split multiple opt types with a /
                            For Each ot In ProdVal.RequiredOptType.Split("/".ToArray)
                                If String.IsNullOrEmpty(pth) Then pth = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", ot, True) 'only populate the path if it isnt already (for instances like PCI and MOD network cards where one system may have one one may have the other)

                                If parts.Where(Function(par) par.OptType Like ot).Count = 0 Then
                                    'Option type not present
                                ElseIf Not String.IsNullOrEmpty(ProdVal.OptionFamily) AndAlso parts.Where(Function(pa) pa.hasAttribute("optFam", ProdVal.OptionFamily, True)).Count = 0 Then
                                    'OptionFamily specified but does not exist
                                Else
                                    'We have one.
                                    For Each part In parts.Where(Function(pa) pa.OptType Like ot AndAlso (String.IsNullOrEmpty(ProdVal.OptionFamily) OrElse pa.hasAttribute("optFam", ProdVal.OptionFamily, True)))
                                        If part.Attributes.ContainsKey(ProdVal.CheckAttribute) Then
                                            'Assume then that the translation of this attribute is an opttype and the number is the required (minimum in this case) value for it
                                            For Each attr In part.Attributes(ProdVal.CheckAttribute)
                                                Dim targetOptType = attr.Translation.text(English)
                                                Dim targetValue = attr.NumericValue
                                                If dicslots.Where(Function(st) st.Key.MajorCode = targetOptType).Sum(Function(ds) ds.Value.TotalCapacity) < targetValue Then
                                                    Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, {targetOptType, targetValue.ToString}, Nothing, "", Nothing, ProdVal))
                                                End If
                                            Next
                                        End If
                                    Next
                                End If
                            Next
                    End Select

                Next
            End If

            'Ok, lets find any xText's - recurse to find any option xTexts

            'Change to this "Important Information" needs to be consolidated...
            ''Me.Msgs.AddRange(Me.getXtext(Me.Path))

            Dim impinfo = Me.getXtext(Me.Path)

            Dim ambinfo = impinfo.Where(Function(ai) ai.severity = EnumValidationSeverity.amberalert)
            Dim blueinfo = impinfo.Where(Function(ai) ai.severity = EnumValidationSeverity.BlueInfo)

            If blueinfo.Count > 0 Then
                blueinfo.First.variables = {String.Join("<br><br>", blueinfo.Select(Function(ii) ii.message.text(Me.quote.BuyerAccount.Language)))}
                blueinfo.First.message = iq.AddTranslation("%1", English, "", 0, Nothing, 0, False)
                Me.Msgs.Add(blueinfo.First)

            End If

            Me.Msgs.AddRange(ambinfo)


            '    For Each a In iq.ProductValidationsAssignment(systype).Where(Function(f) f.ValidationType = enumValidationType.Quantity AndAlso Not parts.Contains(f.RequiredOptType))
            ' Dim pth As String = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", a.RequiredOptType)
            ' Me.Msgs.Add(New ClsValidationMessage(a.Severity, a.Message, pth, 0, 0, Split("")))
            ' Next

            'For Each a In iq.ProductValidationsAssignment(systype).Where(Function(f) f.ValidationType = enumValidationType.Slot)
            ' Dim dics As System.Collections.Generic.KeyValuePair(Of clsSlotType, clsSlotSummary)? = dicslots.ToList().Where(Function(ds) ds.Key.MajorCode = a.RequiredOptType).Select(Function(ff) ff).FirstOrDefault()
            ' If dics Is Nothing Then Continue For
            ' Dim pth As String = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", a.RequiredOptType)
            ' If dics.Value.Value.Given < dics.Value.Value.taken Then Me.Msgs.Add(New ClsValidationMessage(a.Severity, a.Message, pth, 0, 0, Split(dics.Value.Key.Translation.text(English) & "," & dics.Value.Value.taken.ToString & "," & dics.Value.Value.Given.ToString, ",")))
            ' Next
        End If
        '    Dim pth$
        '    If systype.Contains("notebook") Then

        '    ElseIf systype.Contains("workstation") Then

        '    ElseIf systype.Contains("server") Then
        '        If Not Me.hasComponentOfType("KVM", False) Then
        '            pth$ = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", "KVM")
        '            Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.BlueInfo, iq.AddTranslation("We recommend you purchase a KVM adapter with this server", English, "VM", 0, Nothing, 0, False), pth, 0, 0, Split("")))
        '        End If
        '        If Not Me.hasComponentOfType("HDD", False) Then
        '            'Me.Msg &= " You should install at least one Hard Disk"
        '            pth$ = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", "HDD")
        '            Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.amberalert, iq.AddTranslation("You should install at least one Hard Disk", English, "VM", 0, Nothing, 0, False), pth, 0, 0, Split("")))
        '        End If
        '        If Not Me.hasComponentOfType("CPU", False) Then
        '            'Me.Msg &= " You should install at least one Hard Disk"
        '            pth$ = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", "CPU")
        '            Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.amberalert, iq.AddTranslation("You should install at least one CPU", English, "VM", 0, Nothing, 0, False), pth, 0, 0, Split("")))
        '        End If
        '        If Not Me.hasComponentOfType("ILO", False) Then
        '            'Me.Msg &= " You should install at least one Hard Disk"
        '            pth$ = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", "ILO")
        '            Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.BlueInfo, iq.AddTranslation("HP recomends ILO License", English, "VM", 0, Nothing, 0, False), pth, 0, 0, Split("")))
        '        End If
        '        For Each st In dicslots.Keys

        '            If st.MajorCode = "CPU" Then
        '                With dicslots(st)
        '                    If .taken < .Given Then
        '                        pth$ = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", "CPU")
        '                        Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.BlueInfo, iq.AddTranslation("CPU slots " & .taken & " used  " & (.Given - .taken) & " available", English, "VM", 0, Nothing, 0, False), pth, 0, 0, Split("")))
        '                    End If
        '                End With
        '            End If

        '            If st.MajorCode = "MEM" Then
        '                With dicslots(st)
        '                    If .taken < .Given Then
        '                        pth$ = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", "MEM")
        '                        Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.BlueInfo, iq.AddTranslation("Memory Optimisation " & .taken & " used  " & (.Given - .taken) & " available", English, "VM", 0, Nothing, 0, False), pth, 0, 0, Split("")))
        '                    End If
        '                End With
        '            End If

        '            Debug.WriteLine(st.MajorCode)
        '        Next

        '    ElseIf systype.Contains("storage") Then

        '    ElseIf systype.Contains("network") Then

        '    ElseIf systype.Contains("desktops") Then

        '    ElseIf systype.Contains("laptops") Then
        '        For Each st In dicslots.Keys
        '            If st.MajorCode = "MEM" Then
        '                With dicslots(st)
        '                    If .taken < .Given Then
        '                        pth$ = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", "MEM")
        '                        Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.BlueInfo, iq.AddTranslation("Memory Optimisation " & .taken & " used  " & (.Given - .taken) & " available", English, "VM", 0, Nothing, 0, False), pth, 0, 0, Split("")))
        '                    End If
        '                End With
        '            End If
        '        Next

        '    Else

        '        Beep()

        '    End If
        'End If

    End Sub

    Function getXtext(path As String) As List(Of ClsValidationMessage)
        getXtext = New List(Of ClsValidationMessage)()
        getXtext.AddRange(Branch.Product.getXtext(path, Me.quote.AcknowledgedValidations))
        getXtext.AddRange(Me.Children.Where(Function(ci) Not ci.IsPreInstalled).SelectMany(Function(qi) qi.getXtext(qi.Path)))
    End Function


    Public Function hasComponentOfType(type$, crossSystems As Boolean) As Boolean

        'crossystems is not yet implemented
        hasComponentOfType = Nothing

        If Me.Branch.Product.i_Attributes_Code.ContainsKey("optType") Then
            If Me.Branch.Product.i_Attributes_Code("optType")(0).Translation.text(English) = type$ Then
                hasComponentOfType = True
                Exit Function
            End If
        End If

        For Each c In Me.Children
            If c.hasComponentOfType(type$, crossSystems) Then hasComponentOfType = True : Exit Function
        Next

    End Function
    Public Class clsSubComponent
        Public OptType As String
        Public Path As String
        Public Attributes As Dictionary(Of String, List(Of clsProductAttribute))

        Public Function hasAttribute(code As String, value As Object, Wildcard As Boolean) As Boolean
            Dim dec As Decimal
            Return Attributes.ContainsKey(code) AndAlso Attributes(code).Where(Function(attr) Decimal.TryParse(value.ToString, dec) AndAlso attr.NumericValue = dec OrElse (attr.Translation IsNot Nothing AndAlso attr.Translation.text(English) Like value.ToString)).Count > 0
        End Function
    End Class
    Public Function listComponents(crossSystems As Boolean) As List(Of clsSubComponent)
        listComponents = New List(Of clsSubComponent)()
        'crossystems is not yet implemented
        If Me.Branch.Product.i_Attributes_Code.ContainsKey("optType") Then
            listComponents.Add(New clsSubComponent() With {.Path = Me.Path, .OptType = Me.Branch.Product.i_Attributes_Code("optType")(0).Translation.text(English), .Attributes = Me.Branch.Product.i_Attributes_Code})
        End If

        For Each c In Me.Children
            If c.validate Then listComponents.AddRange(c.listComponents(crossSystems))
        Next

    End Function

    ''' <summary>Counts products under each OPG, by product type - Recursively populates a dictionary of flexOPG>ProductType>clsMinMaxTotalUsed  - for this quoteItem(and it's descendants)</summary>
    '''<remarks>ClsMinMaxTotalUsed carries the 4 variables used later by SetFlexRebate()</remarks>
    Dim sysFlexID As Integer = 0
    Friend Sub QualifyFlex(ByRef qfdic As Dictionary(Of clsProduct, Dictionary(Of clsFlexOPG, Dictionary(Of clsProductType, clsMinMaxTotalUsed))), region As clsRegion, flexQualifiedSystem As Boolean, sysFlexID As Integer)

        If Not Me.Branch Is Nothing Then 'The RootItem has no branch

            If (Branch.Product.isSystem(Me.Path) And Branch.Product.OPGflexLines.Count > 0) Then 'Or qfdic.Count > 0 Then -- ML nobbled this as it says "qualifying system" but wasnt checking it was a system??
                Dim t = (From l In Me.Branch.Product.OPGflexLines.Values Where l.FlexOPG.AppliesToRegion(region) And l.FlexOPG.OPGSysType = Me.Branch.Product.ProductType.Code).ToList()
                If t.Count > 0 Then
                    Dim tlqualifies As clsTranslation = iq.AddTranslation("Qualifying System", English, "VM", 0, Nothing, 0, False)
                    Me.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesQualify, Nothing, tlqualifies, "", 0, 0, Split(",", ",")))
                    flexQualifiedSystem = True
                    sysFlexID = t.First.FlexOPG.ID
                End If
            End If

            Dim op = (From l In Me.Branch.Product.OPGflexLines.Values Where l.FlexOPG.AppliesToRegion(region)).ToList()
            If op.Count > 1 Then
                op = (From l In Me.Branch.Product.OPGflexLines.Values Where l.FlexOPG.AppliesToRegion(region) And l.FlexOPG.ID = sysFlexID).ToList()
            End If


            For Each flexLine In op

                'If Me.IsPreInstalled = False The
                Dim ProductType As clsProductType = Me.Branch.Product.ProductType
                If flexLine.isCurrent And flexLine.FlexOPG.isCurrent Then
                    If flexLine.FlexOPG.AppliesToRegion(region) Then
                        Dim qflDic As Dictionary(Of clsFlexOPG, Dictionary(Of clsProductType, clsMinMaxTotalUsed))
                        Dim sysProduct As clsProduct = New clsProduct()
                        If Me.Branch.Product.isSystem(Me.Path) Then
                            sysProduct = Me.Branch.Product
                        ElseIf Me.Parent IsNot Nothing _
                            AndAlso Me.Parent.Branch IsNot Nothing _
                            AndAlso Me.Parent.Branch.Product IsNot Nothing _
                            AndAlso Me.Parent.Branch.Product.isSystem(Me.Path) Then

                            sysProduct = Me.Parent.Branch.Product
                        End If
                        If flexQualifiedSystem Then
                            If Not qfdic.ContainsKey(sysProduct) Then
                                qflDic = New Dictionary(Of clsFlexOPG, Dictionary(Of clsProductType, clsMinMaxTotalUsed))
                                qfdic.Add(sysProduct, qflDic)
                            End If
                            qflDic = qfdic(sysProduct)
                            If Not qflDic.ContainsKey(flexLine.FlexOPG) Then
                                Dim ptdic As Dictionary(Of clsProductType, clsMinMaxTotalUsed) = New Dictionary(Of clsProductType, clsMinMaxTotalUsed)
                                qflDic.Add(flexLine.FlexOPG, ptdic)
                            End If

                            If Me.IsPreInstalled = False Then


                                If Not qflDic(flexLine.FlexOPG).ContainsKey(ProductType) And flexQualifiedSystem Then
                                    Dim rule As clsFlexRule = flexLine.FlexOPG.getRule(ProductType)
                                    If rule Is Nothing Then
                                        'No requried quantities on this Flexlines,Products,productType 
                                        qflDic(flexLine.FlexOPG).Add(ProductType, New clsMinMaxTotalUsed(1, 9999, 0, 0, True))

                                    Else
                                        qflDic(flexLine.FlexOPG).Add(ProductType, New clsMinMaxTotalUsed(rule.min, rule.max, 0, 0, rule.optionalRule))

                                    End If
                                    With qflDic(flexLine.FlexOPG)(ProductType)
                                        .Total += Me.DerivedQuantity  '<This is important !
                                    End With
                                End If

                            End If
                        End If
                    End If
                End If
                'End If
            Next
        End If

        For Each child In Me.Children
            child.QualifyFlex(qfdic, region, flexQualifiedSystem, sysFlexID)
        Next

    End Sub

    Friend Sub FlexCalculations(ByRef qfdic As Dictionary(Of clsProduct, Dictionary(Of clsFlexOPG, Dictionary(Of clsProductType, clsMinMaxTotalUsed))), region As clsRegion, sysFlexID As Integer, ByRef validationSuccess As Boolean, qualifyingProductTypes As Dictionary(Of clsProductType, ClsValidationMessage))
        Dim flexQualifiedSystem As Boolean
        Dim qflDic As Dictionary(Of clsFlexOPG, Dictionary(Of clsProductType, clsMinMaxTotalUsed))
        If Not Me.Branch Is Nothing Then 'Rootitem has no branch
            'if the branch is a system then check if it has OPG flexline for the region 
            If (Branch.Product.isSystem(Me.Path) And Branch.Product.OPGflexLines.Count > 0) Then
                Dim t = (From l In Me.Branch.Product.OPGflexLines.Values Where l.FlexOPG.AppliesToRegion(region) And l.FlexOPG.OPGSysType = Me.Branch.Product.ProductType.Code).ToList()
                If t.Count > 0 Then
                    ' The system qualifies for a flex 
                    Dim tlqualifies As clsTranslation = iq.AddTranslation("Qualifying System", English, "VM", 0, Nothing, 0, False)
                    Me.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesQualify, Nothing, tlqualifies, "", 0, 0, Split(",", ",")))
                    flexQualifiedSystem = True
                    sysFlexID = t.First.FlexOPG.ID
                    Dim sysFlexline As clsFlexLine = t.First
                    Dim sysProduct As clsProduct = Me.Branch.Product


                    If Not qfdic.ContainsKey(sysProduct) Then
                        qflDic = New Dictionary(Of clsFlexOPG, Dictionary(Of clsProductType, clsMinMaxTotalUsed))
                        qfdic.Add(sysProduct, qflDic)
                    End If

                    qflDic = qfdic(sysProduct)
                    If Not qflDic.ContainsKey(sysFlexline.FlexOPG) Then
                        Dim ptdic As Dictionary(Of clsProductType, clsMinMaxTotalUsed) = New Dictionary(Of clsProductType, clsMinMaxTotalUsed)
                        qflDic.Add(sysFlexline.FlexOPG, ptdic)
                    End If

                    ' Generate the system validation rules 
                    For Each flexRules In sysFlexline.FlexOPG.Rules.Values

                        If Not qflDic(sysFlexline.FlexOPG).ContainsKey(flexRules.ProductType) Then
                            qflDic(sysFlexline.FlexOPG).Add(flexRules.ProductType, New clsMinMaxTotalUsed(flexRules.min, flexRules.max, 0, 0, flexRules.optionalRule))
                        End If
                    Next
                    ' Now check which item is missing from the quote
                    Dim strflexLineProductTypes As String = ""
                    getAllproductTypes(strflexLineProductTypes, sysFlexline.FlexOPG.ID)
                    Dim includedproductTypes As List(Of clsProductType) = New List(Of clsProductType)
                    For Each productTypeRule In qflDic(sysFlexline.FlexOPG)
                        Dim productType As clsProductType = productTypeRule.Key
                        Dim systemRule As clsMinMaxTotalUsed = productTypeRule.Value
                        If Not systemRule.optionalRule Then
                            If Not strflexLineProductTypes.Contains(productType.Code) Then
                                If productTypeRule.Value.Min > 0 Then
                                    Dim v(1) As String
                                    v(0) = productType.Translation.text(English)
                                    v(1) = CStr(productTypeRule.Value.Min)
                                    Dim text As clsTranslation
                                    text = iq.AddTranslation("No Qualifying %1 (Min required %2)", English, "VM", 0, Nothing, 0, False)
                                    Me.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesntQualify, text, text, "", 0, 0, v))
                                    validationSuccess = False
                                End If
                            Else
                                includedproductTypes.Add(productType)

                                ' If we qualify on the warranty, explicitly display a summary message
                                If productType.Code = "wty" Then
                                    Dim v(1) As String
                                    v(0) = systemRule.Min.ToString()
                                    v(1) = productType.Translation.text(English)
                                    Dim text As clsTranslation
                                    text = iq.AddTranslation("%1 Qualifying %2", English, "VM", 0, Nothing, 0, False)
                                    Me.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesQualify, text, text, "", 0, 0, v))
                                End If

                            End If
                        End If
                    Next


                Else
                    ' No system flex line 

                End If
                '
            End If

            'get flex lines for a region 

            Dim op = (From l In Me.Branch.Product.OPGflexLines.Values Where l.FlexOPG.ID = sysFlexID).ToList()

            If op.Count = 1 Then
                'There should be only one flexline per country per system type 
                Dim flexLine As clsFlexLine = op.First

                If flexLine.isCurrent And flexLine.FlexOPG.isCurrent Then
                    If Me.IsPreInstalled = False Then
                        'Need to get the system product from dictionary
                        Dim sysProduct As clsProduct = New clsProduct()
                        If Me.Branch.Product.isSystem(Me.Path) Then
                            sysProduct = Me.Branch.Product
                        ElseIf Me.Parent IsNot Nothing AndAlso Me.Parent.Branch IsNot Nothing AndAlso Me.Parent.Branch.Product IsNot Nothing AndAlso Me.Parent.Branch.Product.isSystem(Me.Parent.Path) Then
                            sysProduct = Me.Parent.Branch.Product
                        End If

                        If (Not sysProduct Is Nothing) AndAlso qfdic.ContainsKey(sysProduct) Then
                            qflDic = qfdic(sysProduct)
                            Dim currentBranchProductType As clsProductType = Me.Branch.Product.ProductType
                            If qflDic.ContainsKey(flexLine.FlexOPG) Then
                                'This opg dictionary should already be created with system opg . 
                                ' if the opg is different that means we shouln't count it for our flex calculations
                                If Not qflDic(flexLine.FlexOPG).ContainsKey(currentBranchProductType) Then
                                    Dim rule As clsFlexRule = flexLine.FlexOPG.getRule(currentBranchProductType)
                                    If rule Is Nothing Then
                                        'No requried quantities on this Flexlines,Products,productType 
                                        qflDic(flexLine.FlexOPG).Add(currentBranchProductType, New clsMinMaxTotalUsed(1, 9999, 0, 0, True))
                                    Else
                                        qflDic(flexLine.FlexOPG).Add(currentBranchProductType, New clsMinMaxTotalUsed(rule.min, rule.max, 0, 0, rule.optionalRule))
                                    End If

                                End If
                                With qflDic(flexLine.FlexOPG)(currentBranchProductType)
                                    .Total += Me.DerivedQuantity  '<This is important !
                                    If .Total < .Min Then
                                        Dim text As clsTranslation
                                        text = iq.AddTranslation("%1 required for rebate", English, "VM", 0, Nothing, 0, False)
                                        Me.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesntQualify, text, text, "", 0, 0, Split(CStr(.Min) & " * " & currentBranchProductType.Translation.text(English), "*")))
                                        validationSuccess = False
                                    Else
                                        If Not .optionalRule And Me.Branch.Product.isSystem(Me.Path) = False And Me.IsPreInstalled = False Then
                                            If Not qualifyingProductTypes.ContainsKey(currentBranchProductType) Then
                                                Dim text As clsTranslation
                                                text = iq.AddTranslation("%1 Qualifying %2", English, "VM", 0, Nothing, 0, False)
                                                Dim validationMessage = New ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesQualify, text, text, "", 0, 0, Split(CStr(.Total) & " * " & currentBranchProductType.Translation.text(English), "*"))
                                                'Me.Msgs.Add(validationMessage)
                                                qualifyingProductTypes.Add(currentBranchProductType, validationMessage)
                                            End If
                                        End If
                                    End If
                                End With
                            End If
                        End If
                    End If
                End If
            End If
        End If
        For Each child In Me.Children
            child.FlexCalculations(qfdic, region, sysFlexID, validationSuccess, qualifyingProductTypes)
        Next

    End Sub

    ''' <summary>Counts qualifying options under each system in the basket, by OPG - Recursively populates a dictionary of System>OPGRef>Qty  </summary>
    Public Sub QualifyAvalanche(ByRef system As clsProduct, ByRef qdic As Dictionary(Of clsProduct, Dictionary(Of ClsAvalancheOPG, Integer)), region As clsRegion)

        If Not Me.Branch Is Nothing Then 'The RootItem has no branch
            If Me.Branch.Product.isSystem(Me.Path) Then
                system = Me.Branch.Product
                If Not qdic.ContainsKey(system) Then
                    qdic.Add(system, New Dictionary(Of ClsAvalancheOPG, Integer))
                End If
            Else
                If Me.IsPreInstalled = False Then
                    Dim opt As clsProduct = Me.Branch.Product
                    If system IsNot Nothing Then 'need to check if we're inside a system yet (as options can be orphans)
                        For Each avOPG In system.AvalancheOPGs.Values
                            Dim prodRef As String
                            Dim pra As clsProductAttribute
                            If opt.i_Attributes_Code.ContainsKey("ProdRef") Then 'only check options with a prodref
                                pra = opt.i_Attributes_Code("ProdRef")(0)
                                prodRef = pra.Translation.text(English)
                                If Not qdic(system).ContainsKey(avOPG) Then
                                    qdic(system).Add(avOPG, 0)
                                End If
                                If avOPG.getAvalancheOptions(prodRef, 0, Now, region).Count > 0 Then 'returns the avalancheOptions (containing % rebate information)  is for the sepcified prodref,qty..etc (which are all optional)
                                    qdic(system)(avOPG) += Me.DerivedQuantity
                                End If
                            End If
                        Next
                    End If
                End If

            End If
        End If

        For Each child In Me.Children
            child.QualifyAvalanche(system, qdic, region)
        Next

    End Sub

    ''' <summary>recursively sets the rebate (and OPG) on quoteitems holding qualifying options - according to the avalanche offers available on the system in which they reside</summary>
    '''<remarks>qDic contains a the number of qualifying options,  by opg, by systems (in the basket) - and was built by QualifyAvalanche()</remarks>
    Public Sub SetAvalancheRebate(system As clsProduct, qDic As Dictionary(Of clsProduct, Dictionary(Of ClsAvalancheOPG, Integer)))

        Dim prodRef As String
        If Not Me.Branch Is Nothing Then 'the quote's root item has no branch (and is the only quote item like it)
            If Me.Branch.Product.isSystem(Me.Path) Then
                Me.rebate = 0
                system = Me.Branch.Product
            Else
                'i'm an option
                If Not Me.IsPreInstalled Then
                    Me.rebate = 0
                    If Me.Branch.Product.i_Attributes_Code.ContainsKey("ProdRef") Then
                        prodRef = Me.Branch.Product.i_Attributes_Code("ProdRef")(0).Translation.text(English)

                        Me.OPG = New nullableString
                        If system IsNot Nothing Then  'we may not have 'hit' a system yet (options can be orhpans!)
                            For Each Av In system.AvalancheOPGs.Values  'these are the offers
                                Dim opt As List(Of clsAvalancheOption) = Av.getAvalancheOptions(prodRef, qDic(system)(Av), Now, Me.quote.BuyerAccount.SellerChannel.Region)
                                If opt.Count > 0 Then
                                    Dim listprice As clsPrice = Me.Branch.Product.ListPrice(Me.quote.BuyerAccount)

                                    If listprice Is Nothing Then
                                        Me.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.amberalert, iq.AddTranslation("No list price available - to calculate Avalanche Rebate", English, "VM", 0, Nothing, 0, False), iq.AddTranslation("%1 - No list price", English, "VM", 0, Nothing, 0, False), "", 0, 0, Split("")))
                                    ElseIf listprice.Price.value = 0 Then
                                        Me.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.amberalert, iq.AddTranslation("list price was 0 unable - to calculate Avalanche Rebate", English, "VM", 0, Nothing, 0, False), iq.AddTranslation("%1 - Zero list price", English, "VM", 0, Nothing, 0, False), "", 0, 0, Split("")))
                                    Else
                                        Me.rebate = CDec(opt.First.LPDiscountPercent) / 100 * listprice.Price.value
                                        If Me.rebate = 0 Then
                                            Me.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.amberalert, iq.AddTranslation("Avalanche Rebate was 0", English, "VM", 0, Nothing, 0, False), iq.AddTranslation("%1 - Zero Avalanche rebate", English, "VM", 0, Nothing, 0, False), "", 0, 0, Split("")))
                                        Else
                                            Me.OPG = New nullableString(Av.OPGref)
                                        End If
                                    End If
                                Else

                                End If
                            Next
                        End If
                    End If
                End If
            End If
        End If

        'and recurse... for all children
        For Each child In Me.Children
            child.SetAvalancheRebate(system, qDic)
        Next

    End Sub



    ''' <summary>recursively sets the rebate (and OPG) on quoteItems holding qualifying products - according to the flexOPG offers available</summary>
    Friend Sub SetFlexRebate(qfdic As Dictionary(Of clsFlexOPG, Dictionary(Of clsProductType, clsMinMaxTotalUsed)), rulesQualified As Boolean, systemopgid As Integer, systemTotalQualified As Boolean, ByRef totalrebate As Decimal)

        'qfDic contains the number of total number of products in the basket,  by opg, by ProductType and was built by QualifyFlex() 
        'each instance of clsMinMaxTotalUsed - tells us the Total in the baseket, min required ,max, rebatable of each product type under each OPG

        Dim product As clsProduct

        Dim region As clsRegion = Me.quote.BuyerAccount.BuyerChannel.Region
        If Not Me.Branch Is Nothing Then 'the quote's root item has no branch (and is the only quote item like it)
            If Not Me.IsPreInstalled Then

                Me.OPG = New nullableString
                product = Me.Branch.Product
                If product.isSystem(Me.Path) AndAlso product.OPGflexLines.Count > 0 Then
                    rulesQualified = True
                    'Now add all the rules associated with the system                   
                    If product.OPGflexLines.Values.Count > 0 Then
                        Dim t = (From l In product.OPGflexLines.Values Where l.FlexOPG.AppliesToRegion(region) And l.FlexOPG.OPGSysType = product.ProductType.Code).ToList()
                        Dim sysFlexline As clsFlexLine = product.OPGflexLines.Values.First
                        If t.Count > 0 Then
                            sysFlexline = t.First
                        End If
                        systemopgid = sysFlexline.FlexOPG.ID

                    End If

                End If

                If systemTotalQualified Then

                    Dim op = (From l In Me.Branch.Product.OPGflexLines.Values Where l.FlexOPG.AppliesToRegion(Me.quote.BuyerAccount.BuyerChannel.Region)).ToList()
                    If op.Count > 1 Then
                        op = (From l In Me.Branch.Product.OPGflexLines.Values Where l.FlexOPG.AppliesToRegion(Me.quote.BuyerAccount.BuyerChannel.Region) And l.FlexOPG.ID = systemopgid).ToList()
                    End If

                    For Each flexLine In op

                        ' If Not (flexLine.Product.isSystem) Then
                        'If qfdic.ContainsKey(flexLine.FlexOPG) Then 'we may have removed the OPG becuase we didnt have enough options

                        If flexLine.FlexOPG.AppliesToRegion(Me.quote.BuyerAccount.BuyerChannel.Region) Then

                            If flexLine.isCurrent And flexLine.FlexOPG.isCurrent Then
                                'does the basket contain a product of this flexlines,products type
                                Dim q As clsMinMaxTotalUsed
                                If systemopgid = flexLine.FlexOPG.ID Then
                                    If qfdic.ContainsKey(flexLine.FlexOPG) AndAlso qfdic(flexLine.FlexOPG).ContainsKey(flexLine.Product.ProductType) Then
                                        q = qfdic(flexLine.FlexOPG)(flexLine.Product.ProductType)
                                        Dim r As clsFlexRule = Nothing
                                        r = flexLine.FlexOPG.getRule(flexLine.Product.ProductType)
                                        If r IsNot Nothing Then
                                            ' Make sure that the values are from rules if it exists
                                            q.Min = r.min
                                            q.Max = r.max
                                        End If
                                    Else
                                        'we don't have a product of this flexlines product type  in the basket yet
                                        Dim r As clsFlexRule = Nothing
                                        r = flexLine.FlexOPG.getRule(flexLine.Product.ProductType)
                                        If r Is Nothing Then
                                            q = New clsMinMaxTotalUsed(1, 9999, 0, 0, True)
                                        Else
                                            q = New clsMinMaxTotalUsed(r.min, r.max, 0, 0, r.optionalRule)
                                        End If
                                    End If
                                    If q.Total >= q.Min Then

                                        ' If Me.OPG.value IsNot DBNull.Value Then Stop 'TODO - report an error that a products is qualifying for more than One opg
                                        Me.OPG = New nullableString(flexLine.FlexOPG.OPGRef)
                                        Dim remainingQuota As Integer = q.Max - q.Used 'how many more units of this product can attract the rebate

                                        Dim dq As Integer = Me.DerivedQuantity
                                        If dq < remainingQuota Then
                                            Me.rebate = dq * flexLine.rebate
                                        Else
                                            Me.rebate = remainingQuota * flexLine.rebate
                                        End If
                                        If Not rulesQualified Then
                                            Me.rebate = 0
                                        End If
                                        If Me.rebate > 0 And Not Me.Parent Is Nothing Then
                                            Update(False)
                                            totalrebate += Me.rebate
                                        End If
                                    Else

                                        Me.rebate = 0 'IMPORTANT (otherwise rebates stay on the item) when it 'unqualifies'
                                    End If
                                End If

                            End If
                        End If
                        '  End If
                    Next
                End If
            End If 'new
        End If

        'and recurse... for all children
        For Each child In Me.Children
            child.SetFlexRebate(qfdic, rulesQualified, systemopgid, systemTotalQualified, totalrebate)
        Next

    End Sub


    Public Sub Flex(ByVal qty As Integer, absolute As Boolean)

        'branch As clsBranch, ByVal path As String, ByVal quote As clsquote,
        'if absolute is true - it sets the quantity to (otherwise changes it by qty)

        'The quote contained an item of this product - not preinstalled (posisbly another instance of a product that was originally preinstalled)
        If absolute Then
            'Dim preInstalledOfThis As clsquoteItem = quote.RootItem.FindRecursive(path$, True) 'dangerous ?
            'Dim subtract As Integer
            'If preInstalledOfThis.IsPreInstalled Then subtract = preInstalledOfThis.Quantity Else subtract = 0
            If Me.IsPreInstalled Then
                Beep()
            End If
            Me.Quantity = qty '- subtract
        Else
            Dim quan = Me.Branch.Quantities.Where(Function(q) Me.quote.BuyerAccount.BuyerChannel.Region.Encompasses(q.Value.Region)).FirstOrDefault
            If quan.Value IsNot Nothing AndAlso quan.Value.MinIncrement <> 0 Then
                Me.Quantity += quan.Value.MinIncrement
            Else
                Me.Quantity += qty
            End If

            If Me.Quantity < 0 Then Me.Quantity = 0
        End If

        'Remove this item if we just flexed/set its quantity to zero
        If Me.Quantity < 0 Then
            Beep()
        End If

        If Me.Quantity = 0 Then
            If Me Is Me.quote.RootItem Then
                Beep()
            Else
                Me.Parent.Children.Remove(Me)
            End If
        End If

    End Sub

    Private Function FlexButtons(AllowFlexUp As Boolean) As PlaceHolder

        Dim ph As PlaceHolder = New PlaceHolder

        'Javascript function declaration function flex(branchID, path, qty){

        'Flex Up button
        If Me.FlexButtonState <> EnumHideButton.Up And Me.FlexButtonState <> EnumHideButton.Both Then
            If AllowFlexUp Then
                ph.Controls.Add(Me.FlexButton("quoteTreeFlexUp", +1, Xlt("Add one", quote.BuyerAccount.Language), "../images/navigation/plus.png"))
            End If
        End If

        'Flex down button
        If Me.FlexButtonState <> EnumHideButton.Down And Me.FlexButtonState <> EnumHideButton.Both Then
            ph.Controls.Add(Me.FlexButton("quoteTreeFlexDown", -1, Xlt("Remove one", quote.BuyerAccount.Language), "../images/navigation/minus.png"))
        End If

        'Remove from/add back to Validation
        If Me.IsPreInstalled Then
            If Me.validate Then
                ph.Controls.Add(Me.FlexButton("quoteTreeFlexDown", -1, Xlt("Remove from validation", quote.BuyerAccount.Language), "../images/navigation/cross.png"))
            Else
                ph.Controls.Add(Me.FlexButton("quoteTreeFlexUp", +1, Xlt("Include in validation", quote.BuyerAccount.Language), "../images/navigation/tick.png"))
            End If
        End If

        Return ph

    End Function

    Private Function FlexButton(cssClass As String, qty As Integer, toolTip As String, imageURL As String) As Image

        'returns an individual flex button (+ or -) - or a a 'remvove from/add to validation' button (on preinstalled options)
        FlexButton = New Image

        FlexButton.ImageUrl = imageURL
        FlexButton.ToolTip = toolTip
        FlexButton.CssClass = cssClass & " quoteTreeButton"

        If Not Me.quote.Locked Then
            FlexButton.Attributes("onclick") = "burstBubble(event);flex('" & Me.Path & "'," & CStr(qty) & "," & Me.ID & "," & Me.SKUVariant.ID & ");"
            FlexButton.ToolTip = toolTip
        Else
            Dim text As String = Xlt(PleaseCreateNewVersionToChangeQuote, quote.BuyerAccount.Language)
            FlexButton.Attributes.Add("onmousedown", String.Format("burstBubble(event); setupanddisplaymsg('{0}','{1}');return(false);", text, Me.Path))



        End If

    End Function
    Public Function Compatible(path$) As Boolean

        'checks wether the product with the path - path$ - is compatible with (an option for) this item

        If path$ = Me.Path$ Then Return False 'explicitly stop things being compatible with themselves ! (otherwise systems add as a nested cascase)

        If Left$(path$, Len(Me.Path$)) = Me.Path$ Then
            Return True
        Else
            Return False
        End If

    End Function

    'Private Function NOTSYSDerivedQuantity() As Integer

    '    NOTSYSDerivedQuantity = Me.Quantity
    '    Dim item As clsQuoteItem
    '    item = Me
    '    While Not item.Parent Is Nothing
    '        item = item.Parent
    '        If item.Branch IsNot Nothing Then
    '            If Not item.Branch.Product.isSystem Then
    '                NOTSYSDerivedQuantity *= item.Quantity
    '            End If
    '        End If
    '    End While
    'End Function

    Public Sub ValidateSlots2(dicSlots As Dictionary(Of clsSlotType, clsSlotSummary), ForGives As Boolean)  'pair contains the running used total, and the total available to this point

        'Validates this item and all of its children
        'by recursing the tree of the quoteitems 
        'Populates and then manipulates the dicSlots dicitonary as the tree (of quote items) is walked
        'The dictionary therefore contains the slots available (of each type) at each point in the tree - wherever it goes negative - there are isufficient slots and validation fails.

        If Me.validate Then 'am i excluded from validation ? (pre-installed items can be removed from validation in the UI)
            If Me.IsPreInstalled Then
                Me.FlexButtonState = EnumHideButton.Down
            Else
                Me.FlexButtonState = EnumHideButton.Neither
            End If
            Dim qs As Integer
            If Not Me.Branch Is Nothing Then 'skip the rootitem - it's just a placeholder for the top level items (typically systems)
                Dim sif As List(Of clsSlot)

                For Each slot In Me.Branch.slotsInForce(Path)
                    Dim pn$ = Me.Branch.Product.DisplayName(English) 'For watching/debugging
                    If slot.NonStrictType.MajorCode.ToLower() = "wty" Then
                        Dim a = 9
                    End If
                    If Not dicSlots.ContainsKey(slot.NonStrictType) Then dicSlots.Add(slot.NonStrictType, New clsSlotSummary(0, 0, 0, 0))

                    Dim dq As Integer = Me.DerivedQuantity

                    'GREG OVERRIDE - COUNT AND VALIDATE SLOTS PER SERVER - REMOVE TO GO BACK TO MULTIPLIED.
                    If Me.Branch.Product.isSystem(Me.Path) Then
                        dq = 1
                    Else
                        dq = Me.Quantity
                    End If
                    '</gregness>

                    qs = slot.numSlots * dq
                    If qs > 0 Then
                        If ForGives Then
                            dicSlots(slot.NonStrictType).Given += qs

                        End If
                    Else
                        If Not ForGives Then
                            'Find if there is a fallback WITH SPACE
                            Dim toAllocate As Integer = -qs
                            If slot.NonStrictType.Fallback IsNot Nothing AndAlso slot.NonStrictType.Fallback.Count > 0 Then
                                For Each fbs In slot.NonStrictType.Fallback.Values
                                    If dicSlots.ContainsKey(fbs) Then
                                        If dicSlots(fbs).Given > dicSlots(fbs).taken Then
                                            Dim theseslots = toAllocate
                                            If toAllocate > (dicSlots(fbs).Given - dicSlots(fbs).taken) Then theseslots = (dicSlots(fbs).Given - dicSlots(fbs).taken)
                                            dicSlots(fbs).taken += theseslots
                                            dicSlots(slot.NonStrictType).taken += theseslots 'These two lines are simply in to get the count correct in the error message...
                                            dicSlots(slot.NonStrictType).Given += theseslots
                                            If Me.IsPreInstalled Then dicSlots(fbs).PreInstalledTaken += theseslots
                                            toAllocate = toAllocate - theseslots
                                            If toAllocate = 0 Then Exit For
                                        End If
                                    End If
                                Next
                            End If
                            If toAllocate > 0 Then
                                dicSlots(slot.NonStrictType).taken += toAllocate  'takes slots are negative - so we subtract (to add)
                                If Me.IsPreInstalled Then dicSlots(slot.NonStrictType).PreInstalledTaken += toAllocate
                            End If
                        End If
                    End If

                    'ML - added this for the n-1 PSU functionality, after speaking with Paul aparently this doesnt need to happen... leaving it here incase it suddenly does again.
                    'If slot.Branch.Product.ProductType.Code.ToLower = "psu" AndAlso slot.NonStrictType.MajorCode.ToLower = "psu" AndAlso qs < 0 Then
                    '    'n-1 for power, do it on the slots so that it effects everywhere, validation ,specs, etc
                    '    Dim noPSUs = dicSlots.Where(Function(ds) ds.Key.MajorCode.ToLower = "psu" AndAlso Branch.ID = slot.Branch.ID).Sum(Function(ds) ds.Value.taken)
                    '    Dim noWatts = Me.Branch.slotsInForce(Path).Where(Function(ds) ds.Type.MajorCode.ToLower = "pwr").Max(Function(ds) ds.numSlots)
                    '    Dim wattSlot = dicSlots.Where(Function(ds) ds.Key.MajorCode.ToLower = "pwr").FirstOrDefault
                    '    If wattSlot.Value IsNot Nothing Then wattSlot.Value.Given = If(noPSUs > 1, noWatts * (noPSUs - 1), noPSUs)
                    'End If

                    If slot.numSlots < 0 AndAlso Not ForGives Then   'this option TAKES slots'
                        If Me.Branch.Product.i_Attributes_Code.ContainsKey("capacity") AndAlso {"MEM", "HDD", "CPU", "PSU"}.Contains(slot.NonStrictType.MajorCode.ToUpper()) Then

                            'options like HDD's or MEM have a Capacity ...
                            ' (say 100Mb... which has nothing really to do with slots - but we want to sum the total capacities (for storage, memory etc)
                            Dim capacity As Single = Me.Branch.Product.i_Attributes_Code("capacity")(0).NumericValue
                            dicSlots(slot.NonStrictType).TotalCapacity += Me.Quantity * capacity
                            'dicSlots(slot.NonStrictType).TotalCapacity += dq * capacity
                            If slot.NonStrictType.MajorCode.ToUpper() = "CPU" Then dicSlots(slot.NonStrictType).TotalCapacity = capacity
                            If slot.NonStrictType.MajorCode.ToUpper() = "PSU" Then dicSlots(slot.NonStrictType).TotalRedundantCapacity = capacity * (dicSlots(slot.NonStrictType).taken - 1) 'PSU's are n+1 and always have to be the same power...
                            If slot.NonStrictType.MajorCode.ToUpper() = "PSU" Then dicSlots(slot.NonStrictType).TotalCapacity = capacity

                            'OK this is really ugly - we need to know the capacity Unit Gb/MB/Watt etc
                            'options take slots - and have the (somehwat legacy) capacity
                            dicSlots(slot.NonStrictType).CapacityUnit = Me.Branch.Product.i_Attributes_Code("capacity")(0).Unit
                            If slot.NonStrictType.MinorCode = "W" Then dicSlots(slot.NonStrictType).CapacityUnit = iq.i_unit_code("W")
                        End If
                    End If
                Next

            End If

            For Each item In Me.Children 'These are the child quote items - NOT the children of the branch
                Dim nm$ = item.Branch.Product.DisplayName(English)
                item.ValidateSlots2(dicSlots, ForGives)  'Recurse (validate each child) - dicSlots is passed BYREF - so it accumulates changes made by the item.validateslots
            Next

        End If  'move this End If up (to above the for each) if you want to still validate sub options even though their parent is excluded from validation

    End Sub



    Public Sub OldValidateSlots(ByRef dicSlots As Dictionary(Of clsSlotType, clsSlotSummary))  'pair contains the running used total, and the total available to this point

        'Validates this item and all of its children
        'by recursing the tree of the quoteitems 
        'Populates and then manipulates the dicSlots dicitonary as the tree (of quote items) is walked
        'The dictionary therefore contains the slots available (of each type) at each point in the tree - wherever it goes negative - there are isufficient slots and validation fails.

        'adding a processor might enable (give) 9 UDIMM *OR* 6 RDIMM ports
        'We need these to be multiplied by the number of processors
        'because processors are encountered before memory - the slots are 'given' before the memory 'takes' them
        'and everything works

        If Me.validate Then
            If Me.IsPreInstalled Then
                Me.FlexButtonState = EnumHideButton.Down
            Else
                Me.FlexButtonState = EnumHideButton.Neither
            End If
            Dim qs As Integer
            If Not Me.Branch Is Nothing Then 'skip the rootitem - it's just a placeholder for the top level items (typically systems)

                'Combined system and chassis slots (union)
                Dim combinedslots As List(Of clsSlot) = New List(Of clsSlot)

                For Each slot In Me.Branch.slots.Values  'a branch may have slots of more than one type - It might take Watts and Give USB's
                    If slot.path = Me.Path Then  'a
                        combinedslots.Add(slot)
                    End If
                Next
                combinedslots.AddRange(Me.Branch.slots.Values.ToList) 'system

                'UGLY fix to include the (generally GIVES) slots defined on the chassis branch 
                'If Me.Branch.Product.isSystem Then
                Dim chassispath As String = ""
                For Each cb In Branch.childBranches.Values
                    If cb.Product IsNot Nothing AndAlso cb.Product.ProductType.Code = "CHAS" Then
                        combinedslots.AddRange(cb.slots.Values.ToList)  'chassis
                        chassispath = Path$ & "." & cb.ID
                        Exit For
                    End If
                Next

                For Each slot In combinedslots
                    If slot.path = Me.Path Or slot.path = chassispath Or slot.path = "" Then
                        ' If slot.path <> "" Then Stop
                        If Not dicSlots.ContainsKey(slot.Type) Then
                            ' qs = slot.numSlots * Me.DerivedQuantity
                            dicSlots.Add(slot.Type, New clsSlotSummary(0, 0, 0, 0))
                        End If

                        Dim dq As Integer = Me.DerivedQuantity
                        qs = slot.numSlots * dq
                        If qs > 0 Then
                            dicSlots(slot.Type).Given += qs
                        Else
                            dicSlots(slot.Type).taken -= qs 'takes slots are negative - so we subtract (to add)
                            If Me.IsPreInstalled Then dicSlots(slot.Type).PreInstalledTaken -= qs
                        End If

                        'If qs > 0 Then dicslots(slot.Type).Total += qs

                        If slot.numSlots < 0 Then   'this option TAKES slots'
                            If Me.Branch.Product.i_Attributes_Code.ContainsKey("capacity") AndAlso {"PWR", "MEM", "HDD"}.Contains(slot.Type.MajorCode.ToUpper()) Then

                                'options like HDD's or MEM have a Capacity ...
                                ' (say 100Mb... which has nothing really to do with slots - but we want to sum the total capacities (for storage, memory etc)
                                Dim capacity As Single = Me.Branch.Product.i_Attributes_Code("capacity")(0).NumericValue
                                If slot.Type.MajorCode = "CPU" Then dicSlots(slot.Type).TotalCapacity += Me.Quantity * capacity Else dicSlots(slot.Type).TotalCapacity = capacity

                                'OK this is really ugly - we need to know the capacity Unit Gb/MB/Watt etc
                                'options take slots - and have the (somehwat legacy) capacity
                                dicSlots(slot.Type).CapacityUnit = Me.Branch.Product.i_Attributes_Code("capacity")(0).Unit
                                If slot.Type.MinorCode = "W" Then dicSlots(slot.Type).CapacityUnit = iq.i_unit_code("W")

                            End If
                        End If

                        'If dicslots(slot.Type).Available < 0 Then
                        '    If Me.Parent IsNot Me.quote.RootItem Then  'Don't require sufficient slots on orphaned (root level) options

                        '        Dim tl As clsTranslation
                        '        tl = iq.AddTranslation("Not enough " & slot.Type.displayName(English) & " slots available", English)
                        '        Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.RedCross, tl, "", 0, 0))
                        '        Me.FlexButtonState = EnumHideButton.Up  'Stop them attempting to add more
                        '        'look through every option on the system to find those that would offer more slots of this type
                        '        resolveOverFlows(slot.Type, Math.Abs(dicslots(slot.Type).Available))
                        '    End If
                        'End If
                    End If
                Next
            End If

            For Each item In Me.Children
                '      If Not item.Branch.Product.isSystem Then 'don't cross system boudaries
                Dim nm$ = item.Branch.Product.DisplayName(English)
                item.OldValidateSlots(dicSlots)  'Recurse (validate each child) - dicSlots is passed BYREF - so it accumulates changes made by the item.validateslots
                '   End If
            Next
        End If  'move this End If up (to above the for each) if you want to still validate sub options even though their parent is excluded from validation

    End Sub

    Function SystemItem() As clsQuoteItem

        If Me Is Me.quote.RootItem Then
            Return Nothing
        Else
            If Me.Branch.Product.isSystem(Me.Path) Then
                Return Me
            Else
                Return Me.Parent.SystemItem()
            End If
        End If

        'recursively finds the items parent system (within the tree of quote items)


    End Function
    ''' <summary>
    ''' This is for item where the slots number of slots is less than 0 thte slottype amd Nostricttype  of dicSlots create innstances of slotsummary where the taken - the given is equal to the short fall.  
    ''' </summary>
    ''' <param name="slotType">An instance clsSlotType.  </param>
    ''' <param name="shortfall">An integer value representing the shortfall</param>
    ''' <param name="buyeraccount">An instance of ClsAccount.</param>
    ''' <param name="msg">An instance of ClsValidationMessage.</param>
    ''' <param name="errormessages">An intsance of list of type string that represent any errror messages recorded.</param>
    ''' <param name="lid">A Uint64 value that represent the logon ID</param>
    ''' <param name="dicSlots">An instance of Dictionary of Type of ClsSlotType , clsSlotSummary. </param>
    ''' <remarks></remarks>
    Public Sub resolveOverFlows(slotType As clsSlotType, shortfall As Integer, buyeraccount As clsAccount, ByRef msg As ClsValidationMessage, ByRef errormessages As List(Of String), lid As UInt64, dicSlots As Dictionary(Of clsSlotType, clsSlotSummary))

        'adds validationmessages - to resolve slot overflows

        Dim tl As clsTranslation = iq.AddTranslation("Resolve with ", English, "VM", 0, Nothing, 0, False)

        Dim branch As clsBranch = Nothing
        Dim rq As Integer 'number of units (of the partnumber) required to resove

        Dim systemItem As clsQuoteItem = Me.SystemItem
        If systemItem Is Nothing Then Exit Sub
        Dim gives As Dictionary(Of String, Integer) = systemItem.Branch.findSlotGivers(Path, slotType, False)  'the 'false' stops it incuding another system unit as a slot donor
        For Each pth In gives.OrderByDescending(Function(g) g.Value).Select(Function(j) j.Key)
            branch = iq.Branches(CInt(Split(pth, ".").Last))
            Dim foci As HashSet(Of String) = New HashSet(Of String)(iq.sesh(lid, "foci").ToString.Split(",".ToArray))
            If foci Is Nothing Then foci = New HashSet(Of String)
            If branch.ReasonsForHide(buyeraccount, foci, Path, buyeraccount.SellerChannel.priceConfig, False, errormessages).Count = 0 AndAlso Not branch.Product.SKU.StartsWith("###") Then
                Dim remainingtakes = -1

                For Each slot In branch.slots
                    'important must be slots.numSlots must be negative.
                    If slot.Value.numSlots >= 0 Then Continue For
                    'Important if clsSlotType of NonStrictType exists.
                    If Not dicSlots.ContainsKey(slot.Value.NonStrictType) Then Continue For
                    Dim slotTypeNoneStrict As clsSlotSummary = dicSlots(slot.Value.NonStrictType)
                    With slotTypeNoneStrict
                        'Important if these checks are not in place not the incorrect path and the incorrect message is displayed 
                        If (.taken - .Given) = shortfall Then
                            Dim need As Double = -((.Given - .taken) / slot.Value.numSlots)
                            If need < remainingtakes Or remainingtakes = -1 Then
                                remainingtakes = Convert.ToInt32(need)
                            End If
                        End If
                    End With

                Next
                'Could tell them exactly what they need here?? for now lets just suggest the first thing to get them started.
                rq = If(Convert.ToInt32(remainingtakes * gives(pth)) > shortfall, Convert.ToInt32(remainingtakes * gives(pth)), shortfall)
                With msg
                    .ResolvePath = pth
                    .ResolverGives = gives(pth) * remainingtakes
                    .ResolvingQty = remainingtakes
                    .resolutionMessage = tl
                End With
                If remainingtakes > 0 Then Exit For 'Remove this when counting more than the best item
            End If
        Next

    End Sub

    Public Sub ValidateFill(agentAccount As clsAccount)

        'checks that enough slots of each type are filled (accoring to the slot.requiredFill property

        If Me.Branch IsNot Nothing Then 'the root QuoteItem has no branch (it's just a placholder for the top level items in the Quote)
            For Each slot In Me.Branch.slots.Values
                If LCase(slot.path) = LCase(Me.Path) Or slot.path = "" Then
                    If slot.requiredFill > 0 Then
                        If Me.CountFilledDescendants(slot.Type) < slot.requiredFill Then
                            Dim tl As clsTranslation
                            tl = iq.AddTranslation("You must have at least %1", agentAccount.Language, "VM", 0, Nothing, 0, False)
                            Me.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.amberalert, tl, tl, "", 0, 0, Split(slot.requiredFill & " " & slot.Type.Translation.text(agentAccount.Language), ",")))

                        End If
                    End If
                End If
            Next
        End If

        For Each item In Me.Children
            item.ValidateFill(agentAccount) ' (agentAccount)  'Recurse (validate each child) - dicSlots is passed BYREF - so it accumulates changes made by the item.validateslots
        Next

    End Sub

    Public Function CountFilledDescendants(slottype As clsSlotType) As Integer

        Dim count As Integer

        For Each slot In Me.Branch.slots.Values
            If slot.Type Is slottype Then
                count += 1
            End If
        Next

        For Each item In Me.Children
            count += item.CountFilledDescendants(slottype)
        Next

        Return (count)

    End Function

    ''' <summary>Returns this Items quantity - multiplied by that of all its ancesetors </summary>
    ''' <remarks>Handles the 'nestedness' of quotes - the fact hat you might be buying 2 racks  each containing 3 servers each containing 4 Drives - For the drives, this fuction gives you the 2*3*4 </remarks>
    Private Function DerivedQuantity() As Integer

        DerivedQuantity = Me.Quantity

        Dim item As clsQuoteItem
        item = Me
        While Not item.Parent Is Nothing
            item = item.Parent
            DerivedQuantity *= item.Quantity
        End While


    End Function
    Public Sub validateExclusivity()

        Dim incompatible As List(Of clsQuoteItem)
        For Each ex In iq.Excludes.Values 'There is typically one exclude per family - so a few hundred at most
            For Each o In Me.Children
                ' If o.validate Then  'is it removed from validation ! ?
                If ex.havingAnyOf.Contains(o.Branch) Then
                    incompatible = o.siblingsBranchesIn(ex.excludesAllOf)
                    If incompatible.Count > 0 Then
                        o.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.RedCross, iq.AddTranslation("Incompatbile with %1", English, "VM", 0, Nothing, 0, False), iq.AddTranslation("%1 - Incompatbile", English, "VM", 0, Nothing, 0, False), "", 0, 0, Split(incompatible(0).Branch.DisplayName(English), ",")))
                    End If
                End If
                ' End If
            Next
        Next

    End Sub



    ''' <summary>Returns any sibling QuoteItem whos branches are contained in the specified list </summary>
    Public Function siblingsBranchesIn(L As List(Of clsBranch)) As List(Of clsQuoteItem)

        siblingsBranchesIn = New List(Of clsQuoteItem)

        For Each i In Me.Parent.Children
            If i IsNot Me Then
                If i.validate Then   'is it removed from validation (preinstalled items can be - by minusing them in the basket)
                    If L.Contains(i.Branch) Then
                        siblingsBranchesIn.Add(i)
                    End If
                End If
            End If
        Next

    End Function

    Public Sub ValidateIncrements(ByVal dicItemCounts As Dictionary(Of String, Integer), agentaccount As clsAccount, ByRef errorMessages As List(Of String))

        'Validates this items quantity (against is Min and Preferred increments) 
        'recurses for all children
        Dim translationLanguage As clsLanguage = agentaccount.Language
        If translationLanguage Is Nothing Then translationLanguage = English

        'translationLanguage = English

        If Me.validate Then
            Dim sellerRegion As clsRegion = Me.quote.BuyerAccount.SellerChannel.Region

            Dim quantity As clsQuantity = Nothing

            If Not Me.Branch Is Nothing Then 'skip the rootitem
                quantity = Me.Branch.LocalisedQuantity(sellerRegion, Me.Path, errorMessages)

                If quantity Is Nothing Then

                    Logit("No quantity limits for " & Branch.Translation.text(translationLanguage))
                    Exit Sub
                End If

                Dim qty As Integer
                If dicItemCounts.ContainsKey(Me.Path) Then
                    qty = dicItemCounts(Me.Path)  'this is the TOTAL quantity of this branch.product in the quote

                    If quantity.PreferredIncrement <> 0 Then
                        If qty Mod quantity.PreferredIncrement <> 0 Then

                            Dim tl As clsTranslation
                            tl = iq.AddTranslation("Optimum performance is achieved when %1 is installed in multiples of %2 modules %3 selected", English, "VM", 0, Nothing, 0, False)
                            Me.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Upsell, EnumValidationSeverity.Exclamation, tl, iq.AddTranslation(String.Format("{0} Optimisation", quantity.Branch.Product.ProductType.Translation.text(English)), English, "", 0, Nothing, 0, False), Me.Path, 1 - (qty Mod quantity.PreferredIncrement) * quantity.PreferredIncrement, 0, {quantity.Branch.Product.ProductType.Translation.text(translationLanguage), quantity.PreferredIncrement.ToString(), qty.ToString()}))
                        End If
                    End If

                    If Me.Quantity > 0 And Me.Quantity < quantity.MinIncrement Then
                        Me.Quantity = quantity.MinIncrement
                        Dim tl As clsTranslation
                        tl = iq.AddTranslation("Quantity adjusted to meet minimum of %1", English, "VM", 0, Nothing, 0, False)
                        Me.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.BlueInfo, tl, tl, "", 0, 0, Split(CStr(quantity.MinIncrement), ",")))
                    End If
                End If
            End If

            For Each item In Me.Children
                item.ValidateIncrements(dicItemCounts, agentaccount, errorMessages) 'RECURSE
            Next
        End If

    End Sub


    Public Sub clearMessage()

        'Nice easy one.. clears the (warning) message on this quoute item - and recurses for all children.
        Me.Msgs = New List(Of ClsValidationMessage)
        For Each item In Me.Children
            item.clearMessage()
        Next

    End Sub

    Public Function HasProduct(Product As clsProduct) As Boolean

        If Me.Branch IsNot Nothing Then
            If Me.Branch.Product Is Product Then
                Return True
            End If

        End If

        For Each item In Me.Children
            If item.HasProduct(Product) Then Return True
        Next

    End Function


    Public Function HasProductType(ProductType As clsProductType) As Boolean
        If Me.Branch IsNot Nothing Then

            If Me.Branch.Product.ProductType Is ProductType Then
                Return True

            End If

        End If
        For Each item In Me.Children
            If item.HasProductType(ProductType) Then Return True
        Next
    End Function


    Public Sub countSystems(ByRef systems As Integer, ByRef options As Integer)

        If Me.Branch IsNot Nothing Then
            If Me.Branch.Product.isSystem(Me.Path) Then
                systems += Me.Quantity
            Else
                If Not Me.IsPreInstalled Then
                    options += Me.Quantity
                End If
            End If
        End If

        For Each c In Me.Children
            c.countSystems(systems, options)
        Next

    End Sub
    Public Sub CountItems(ByRef dicItems As Dictionary(Of String, Integer))

        'recursively (used from the rootitem)
        'builds a dictionary keyed by branch path - of the counts of every item (so we can validate the total number of items (preinstalled and added) agains their minimum/preferred increments
        'because each path is unique (but the same wether an option is pre-installed or user selected) , the validation will happen per item (sku)

        'Note: dicItems is passed BYREF.. so it will accumulate a count of all items

        If Not Me.Branch Is Nothing Then 'skip the rootitem

            Dim qty As Integer = Me.Quantity
            If Not Me.validate Then qty = 0 'NOTE: items excluded from validation are not counted !

            If Not dicItems.ContainsKey(Me.Path) Then
                dicItems.Add(Me.Path, qty)
            Else
                dicItems(Me.Path) += qty
            End If

        End If

        For Each item In Me.Children
            item.CountItems(dicItems)
        Next

    End Sub

    Public Sub Totalise(ByRef runningTotal As NullablePrice, ByRef runningRebate As Decimal, includeMargin As Boolean) ' As nullablePrice

        'for each item - add myself to the running total - and recurse for all children
        'TotalPrice = New nullablePrice(Me.quote.Currency)

        If Not Me Is Me.quote.RootItem Then  'the ROOT item has no price
            If Not Me.BasePrice.isValid And Not Me.IsPreInstalled Then ' Also check if the item is pre installed 
                runningTotal.isValid = False
                '   Exit Sub
            End If
            If Me.BasePrice.isList And Not Me.IsPreInstalled Then runningTotal.isList = True
        End If

        Dim mgf As Single = 1 'Margin Factor
        If Me.Margin = 0 Then Me.Margin = 1
        If includeMargin Then mgf = Me.Margin


        'NB this uses the * operator overload on clsNullablePrice - which preserves the IsValid/IsList
        If Not Me.IsPreInstalled And Not Me Is Me.quote.RootItem Then ' Fix for recalculation bug ignore pre installed items
            runningTotal += Me.BasePrice * CSng(Me.DerivedQuantity * mgf)
        End If

        If Not Me Is Me.quote.RootItem Then
            If Me.Branch.Product.hasSKU Then  'Chasis are from HP but not List price !
                If Me.SKUVariant.sellerChannel Is HP AndAlso Not Me.IsPreInstalled Then runningTotal.isList = True
            End If
        End If

        Dim dq As Integer = Me.DerivedQuantity
        If Me.Branch IsNot Nothing Then
            If Me.Branch.Product.isSystem(Me.Path) Then runningRebate = 0 'NEW - IMPORTANT
        End If

        runningRebate += Me.rebate '* dq  - NB: Rebates are per line (are already multiplied by the permisssible quantity)
        '  If runningRebate > 0 Then Stop

        For Each item In Me.Children
            item.Totalise(runningTotal, runningRebate, includeMargin)
            'If runningTotal.valid = False Then Exit Sub  - keep going becuase we may still be able to give a valid total rebate

            'If item.Price.valid = False Then TotalPrice.valid = False : Exit Function
            ' TotalPrice = New nullablePrice(TotalPrice.NumericValue + item.TotalPrice.NumericValue, Me.quote.BuyerAccount.Currency)
        Next


    End Sub

    Public Function FindRecursive(itemID As Integer) As clsQuoteItem

        'Recursively locates and returns an item from the quote by ID
        FindRecursive = Nothing

        If Me.ID = itemID Then
            Return Me
        End If

        Dim result As clsQuoteItem
        For Each item As clsQuoteItem In Me.Children
            result = item.FindRecursive(itemID)
            If Not result Is Nothing Then
                Return result
            End If
        Next

    End Function

    Public Function FindRecursive(path$, includepreinstalled As Boolean) As clsQuoteItem ', IncludePreinstalled As Boolean) As clsquoteItem

        'Checks wether a quote item has the specified path - if so, returns the item
        'works *backwards* through the children for LIFO behaviour http://en.wikipedia.org/wiki/LIFO_(computing)
        FindRecursive = Nothing

        If LCase(Me.Path) = LCase(path) Then
            'If Me.IsPreInstalled = includepreinstalled Then Return Me - This ISNT the same as the two lines below
            If Me.IsPreInstalled And includepreinstalled Then Return Me
            If Me.IsPreInstalled = False Then Return Me
        End If

        Dim result As clsQuoteItem
        'For Each item As clsquoteItem In Me.Children 
        For i = Me.Children.Count - 1 To 0 Step -1  'We want to go backwards to find the last item first (when looking by path... primarliy for decrementing via the product tree - not the quote - so we end up with LIFO behaviour
            result = Me.Children(i).FindRecursive(path$, includepreinstalled)
            If Not result Is Nothing Then
                Return result
            End If
        Next i
        'Next

    End Function

    Public Function summarise(ByRef options As Integer) As String

        'Gets the name of every system, and a count of its options - recursively 

        summarise = ""
        If Not Me.Branch Is Nothing Then  ' the quote.rootitem has no branch defined (so we must check)
            With Me.Branch.Product
                If .isSystem(Me.Path) Then
                    summarise = .SKU
                    options = 0
                Else
                    options += 1
                End If
            End With
        End If

        Dim so$
        For Each item In Me.Children.OrderBy(Function(x) x.order)
            so$ = item.summarise(options) & "(+" & options & " options)" & "," 'we must always recurse
            If item.Branch.Product.isSystem(Me.Path) Then summarise &= so$ 'but only add the result for systems
        Next

    End Function

    Public Function Descendants() As List(Of clsQuoteItem)

        Descendants = New List(Of clsQuoteItem)
        Descendants.Add(Me)

        For Each c In Me.Children.OrderBy(Function(x) x.order)
            Descendants.AddRange(c.Descendants)
        Next

    End Function

    ''' <summary>
    '''returns a list of product/SKUvariant/quantity (so it doesn't matter where they pick an option from in the tree - they will be consolidated) 
    ''' </summary>
    ''' <param name="Consolidate">Wether to consolidate identical parts (or list them as seperate quantity/partnos)</param>
    ''' <param name="IncludePreinstalled"></param>
    ''' <param name="Indent"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function Flattened(Consolidate As Boolean, IncludePreinstalled As Boolean, Indent As Integer, Optional quote As Boolean = False) As clsFlatList '

        Indent += 1
        Flattened = New clsFlatList

        If Not Me.IsPreInstalled Or IncludePreinstalled Then  'We don't include preinstalled items on the 'flat' quote (Bill Of Materials) view
            If Not Me.Branch Is Nothing Then  'the root item (placeholder)
                Flattened.items.Add(New clsFlatListItem(Me, Indent, If(Consolidate, Me.DerivedQuantity, Me.Quantity)))  'NOT quantity
                Me.Indent = Indent
            End If
        End If

        For Each item In Me.Children.OrderBy(Function(x) x.order)
            'merge
            If quote Then
                Flattened = MergeItems(Flattened, item.Flattened(Consolidate, IncludePreinstalled, Indent, quote), Consolidate, IncludePreinstalled)
            Else
                Flattened = MergeItems(Flattened, item.Flattened(Consolidate, IncludePreinstalled, Indent))
            End If
        Next

    End Function

    Private Function MergeItems(ByRef a As clsFlatList, ByVal b As clsFlatList) As clsFlatList

        'used to merge the lists of child items (of different parent systems)  together into one big, flat shopping list
        'appends/merges Dictionary b to a - extending a

        Dim existing As clsFlatListItem
        For Each i In b.items
            existing = a.PSV(i.QuoteItem.Branch.Product, i.QuoteItem.SKUVariant)
            If existing Is Nothing Then  'the flat list didnt have one of these yet so add the product and its quantity Then
                a.items.Add(i)
            Else
                existing.Quantity += i.Quantity  'add the quantity in b to the exisiting quoute item in a (for this product)
            End If
        Next
        Return a

    End Function
    ''' <summary>
    ''' This is for reports only so we can have the sperate items with in the system break down of report.
    ''' </summary>
    ''' <param name="a">An instance of clsFlatList</param>
    ''' <param name="b"></param>
    ''' <param name="consolidate">A boolean value true/ false that represents whether to consolidate the quote.</param>
    ''' <param name="includePreinstalled">A boolean value true/ false and represents to inclued preinstalled items.</param>
    ''' <returns>An instance of clsFlatList.</returns>
    ''' <remarks></remarks>
    Private Function MergeItems(ByRef a As clsFlatList, ByVal b As clsFlatList, consolidate As Boolean, includePreinstalled As Boolean) As clsFlatList

        'used to merge the lists of child items (of different parent systems)  together into one big, flat shopping list
        'appends/merges Dictionary b to a - extending a

        Dim existing As clsFlatListItem
        For Each i In b.items
            existing = a.PSV(i.QuoteItem.Branch.Product, i.QuoteItem.SKUVariant)
            If (existing Is Nothing) Then
                a.items.Add(i)
            ElseIf includePreinstalled And consolidate = False And i.QuoteItem.IsPreInstalled = False Then 'the flat list didnt have one of these yet so add the product and its quantity Then
                If a.DoesNoneInstalledExist(i.QuoteItem) = False Then
                    a.items.Add(i)
                Else
                    existing.Quantity += i.Quantity  'add the quantity in b to the exisiting quoute item in a (for this product)
                End If
            Else
                existing.Quantity += i.Quantity
            End If
        Next
        Return a

    End Function
    Public Function FlatListTXT(buyeraccount As clsAccount) As String

        'Returns a SKU tab Qty CRLF  delimited 'Bill of materials' type list (for Ingram Copy to ClipBoard)
        Dim Product As clsProduct

        FlatListTXT = ""
        For Each lineitem In quote.RootItem.Flattened(True, False, 0).items  'see consolidated() - recurses to provide a 'flattened', consolidated (by Product/SKU varaint) view of the quote

            Product = lineitem.QuoteItem.Branch.Product
            FlatListTXT &= Product.SKU & vbTab & lineitem.Quantity & vbCrLf

        Next

    End Function

    Public Function EmailSummary(ByVal includePreinstalled As Boolean, BuyerAccount As clsAccount, agentAccount As clsAccount, ByRef errorMessages As List(Of String), ByRef runningtotal As NullablePrice) As String

        'Returns this QuoteItem (as simple HTML div with a left margin (indent) ..and recurses for all child items (nesting Divs)

        Dim product As clsProduct

        EmailSummary = ""
        Dim translationLanguage As clsLanguage = agentAccount.Language
        If translationLanguage Is Nothing Then translationLanguage = English

        'translationLanguage = English


        Dim mgf As Single = 1 'Margin Factor
        'NB this uses the * operator overload on clsNullablePrice - which preserves the IsValid/IsList
        If Not Me.IsPreInstalled Then ' Fix for recalculation bug ignore pre installed items
            Dim dq As Integer = Me.DerivedQuantity 'autoadded things are not preinstalled - presinstalle din synonymous with "FIO"
            runningtotal += Me.BasePrice * dq * mgf 'this total ALWAYS includes any margin applied
        End If


        Dim th$ = "<th style='background-color:#0096D6;text-align:left;'>"
        If Me.Branch Is Nothing Then  'This is the root item (it has no branch!)

            EmailSummary &= "<table cellpadding='3px' style='font-family:arial;font-size:10pt;border-collapse:collapse;border:solid gray 1px;'>"
            EmailSummary &= "<tr>"
            EmailSummary &= th$ & Xlt("Product type", translationLanguage) & "</th>"
            EmailSummary &= th$ & Xlt("Part Number", translationLanguage) & "</th>"
            EmailSummary &= th$ & Xlt("Description", translationLanguage) & "</th>"
            EmailSummary &= th$ & Xlt("Quantity", translationLanguage) & "</th>"
            EmailSummary &= th$ & Xlt("Unit Price", translationLanguage) & "</th>"
            EmailSummary &= th$ & Xlt("Note", translationLanguage) & "</th>"

        Else

            product = Me.Branch.Product

            'EmailSummary = "<div style='margin-left:3em;" & IIf(product.isSystem, "font-weight:bold;", "font-weight:normal;") & IIf(Me.IsPreInstalled, "font-style:italic;", "font-style:normal;") & "clear:both;'>" & vbCrLf


            'Dim border$ = "border:1px solid black;"
            '            Dim display$ = "float:left;" '"display:inline-block;"

            'If Not Me.validate Then EmailSummary &= "* "
            If Not Me.IsPreInstalled Or (includePreinstalled And Me.IsPreInstalled) Then

                EmailSummary &= "<tr>"

                Dim ct$, ect$  'cell type/end cell type
                ct$ = "<td style='padding-left:10px;'>" : ect = "</td>"
                If product.isSystem(Me.Path) Then ct$ = "<th style='text-align:left;'>" : ect = "</th>"

                '    If Me.IsPreInstalled Then
                ' EmailSummary &= "preInstalled"
                ' End If

                'Product type NoteBook/Server/HardDisk Drive Etc.
                'EmailSummary &= "<div style='width:15em;" & display$ & border$ & "'>" & product.ProductType.Translation.text(s_lang) & "</div>" & vbCrLf
                EmailSummary &= ct$ & product.ProductType.Translation.text(translationLanguage) & ect$

                'EmailSummary &= "<div style='width:10em;" & display$ & border$ & "'>" & product.sku & "</div>" & vbCrLf
                If product.SKU.StartsWith("###") Then
                    EmailSummary &= ct$ & "built in" & ect$
                Else
                    EmailSummary &= ct$ & product.SKU & ect$
                End If


                Dim desc$ = "No description available"

                If product.i_Attributes_Code.ContainsKey("desc") Then
                    desc$ = product.i_Attributes_Code("desc")(0).Translation.text(translationLanguage)
                End If

                'EmailSummary &= "<div style='width:30em;" & display$ & border$ & "'>" & desc$ & "</div>" & vbCrLf
                EmailSummary &= ct$ & desc$ & ect$

                'Quantity
                'EmailSummary &= "<div style='width:2em;" & display$ & border$ & "'>" & Me.Quantity & "</div>" & vbCrLf
                EmailSummary &= ct$ & Me.Quantity & ect$

                'Price
                'EmailSummary &= "<div style='width:8em;" & display$ & border$ & "'>" & Me.QuotedPrice.DisplayPrice(quote.BuyerAccount).Text & "</div>" & vbCrLf

                If Me.IsPreInstalled Then
                    EmailSummary &= ct$ & "-" & ect$
                Else
                    Dim PriceIncludingAnyMargin As NullablePrice = Me.BasePrice * Me.Margin
                    EmailSummary &= ct$ & PriceIncludingAnyMargin.text(quote.BuyerAccount, errorMessages) & ect$
                End If

                'TODO (avalanche/rebates)
                'Dim promoMarkers As PlaceHolder = Me.Branch.PromoIndicators(BuyerAccount, Me.Path, "quoteTreeAvalancheStar")
                'UI.Controls.Add(promoMarkers)
                'If Me.rebate <> 0 Then
                '    Dim avlabel As Label = New Label
                '    avlabel.Text = "✔"
                '    avlabel.CssClass &= "quoteTreeAvalancheTick"  'probably needs a slight change (add a seperate *)

                '    Dim saving As nullablePrice = New nullablePrice(Me.rebate, Me.quote.Currency) 'making it into a nullableprice allows us to get this displaprice (lable) - which does the currency/culture formatting
                '    avlabel.ToolTip = "Qualifies - Saving  " & saving.DisplayPrice(quote.BuyerAccount).Text & " per item (" & Me.OPG.value & ")"

                '    UI.Controls.Add(avlabel)
                'End If

                'EmailSummary &= "<div style=width:20em;" & display$ & border$ & "'>"
                EmailSummary &= ct$
                If Me.Note.value IsNot DBNull.Value Then
                    EmailSummary &= Me.Note.DisplayValue
                End If
                'EmailSummary &= "</div>" & vbCrLf
                EmailSummary &= ect$

                EmailSummary &= "</tr>"

                'For Each vm As ClsValidationMessage In Me.Msgs
                ' UI.Controls.Add(vm.UI(BuyerAccount, agentAccount.Language))
                ' Next

            End If
        End If

        'we *always* recurse (otherwise we'd lose options in preinstalled options) .. but only append the HTML for preinstalled items if includePreinstalled=true

        For Each item In Me.Children.OrderBy(Function(x) x.order)
            If Not Me.IsPreInstalled Or includePreinstalled Then
                EmailSummary &= item.EmailSummary(includePreinstalled, BuyerAccount, agentAccount, errorMessages, runningtotal)
            End If
        Next

        'EmailSummary &= "</div>" & vbCrLf
        If Me.Branch Is Nothing Then

            'Grand total 'may have a * for contains list price elements
            EmailSummary &= "<tr><td></td><td></td><td></td><td></td><td> " & Xlt("TOTAL", translationLanguage) & " " & runningtotal.text(BuyerAccount, errorMessages) & CStr(IIf(runningtotal.isList, " " & Xlt("Contains list price elements", translationLanguage), "")) & "</td><td></td><tr>"
            EmailSummary &= "</table>"
        End If



    End Function


    Public Function FlatList(buyerAccount As clsAccount, ByRef errorMessages As List(Of String)) As Panel

        'Returns a HTML Panel 'bill of materials' type consolidated view of the 
        'Flatlist is called on the quotes 'root' item - it 
        'Calls the consolidated() function - which recurses through all quoteitems to return a dictionary of the counts of parts, indexed by Product.

        Dim translationLanguage As clsLanguage = Me.quote.AgentAccount.Language
        If translationLanguage Is Nothing Then translationLanguage = English

        'translationLanguage = English


        FlatList = New Panel
        FlatList.CssClass = "flatQuotePanel"

        'Dim tbl As Table = New Table
        'FlatList.Controls.Add(tbl)

        'Dim thr As TableHeaderRow = New TableHeaderRow
        'tbl.Controls.Add(thr)

        'Dim thc As TableHeaderCell
        'thc = New TableHeaderCell
        'thr.Controls.Add(thc)
        'thc.Text = "Qty"

        'thc = New TableHeaderCell
        'thr.Controls.Add(thc)

        'thc = New TableHeaderCell
        'thr.Controls.Add(thc)
        'thc.Text = "Part#"

        'thc = New TableHeaderCell
        'thr.Controls.Add(thc)
        'thc.Text = "Price"

        'Dim tr As TableRow
        'Dim td As TableCell

        Dim Product As clsProduct

        Dim lbl As Label
        Dim line As Panel


        'For Each lineitem In quote.RootItem.Flattened(True, False, 0).items  'see consolidated() - recurses to provide a 'flattened', consolidated (by Product/SKU varaint) view of the quote
        For Each lineitem In Me.Flattened(True, False, 0).items  'see consolidated() - recurses to provide a 'flattened', consolidated (by Product/SKU varaint) view of the quote


            Product = lineitem.QuoteItem.Branch.Product
            line = New Panel
            FlatList.Controls.Add(line)
            line.CssClass = "flatLine"
            If Product.isSystem(Me.Path) Then line.CssClass &= " isSystem"

            'tr = New TableRow
            'tbl.Controls.Add(tr)

            'td = New TableCell
            'tr.Controls.Add(td)
            lbl = New Label
            lbl.CssClass = "quoteFlatQty"
            lbl.Text = lineitem.Quantity.ToString & " "  'note.. this can be a consolidated quantity (from more than one quote line) 
            line.Controls.Add(lbl)

            'td = New TableCell
            'tr.Controls.Add(td)
            lbl = New Label
            lbl.CssClass = "quoteFlatType"
            lbl.Text = Product.ProductType.Translation.text(translationLanguage)
            line.Controls.Add(lbl)


            'td = New TableCell
            'tr.Controls.Add(td)
            lbl = New Label
            lbl.CssClass = "quoteFlatSKU"
            line.Controls.Add(lbl)

            If Product.i_Attributes_Code.ContainsKey("MfrSKU") Then
                lbl.Text = Product.SKU
            End If

            If Product.i_Attributes_Code.ContainsKey("desc") Then
                lbl.ToolTip = Product.i_Attributes_Code("desc")(0).Translation.text(translationLanguage)
            End If

            'calls NEW internally (to create a new label)

            'td = New TableCell
            'tr.Controls.Add(td)
            Dim PriceIncludingMargin As NullablePrice = lineitem.QuoteItem.BasePrice * lineitem.QuoteItem.Margin

            Dim pp As Panel = PriceIncludingMargin.DisplayPrice(quote.BuyerAccount, errorMessages)

            pp.CssClass &= " quoteFlatPrice"
            line.Controls.Add(pp)
        Next

    End Function

    Public Function UI(ByVal includePreinstalled As Boolean, BuyerAccount As clsAccount, agentAccount As clsAccount, foci As HashSet(Of String), ByRef errorMessages As List(Of String), validationMode As Boolean, lid As UInt64) As Panel

        'Returns this QuoteItem (as a panel) ..and recurses for all child items (nesting panels)

        UI = New Panel

        If Me.Branch IsNot Nothing Then
            If Me.Branch.Hidden Then Exit Function 'return an entirely emptt panel from hidden (chassis) branches
        End If

        UI.CssClass = "quoteItemTree"
        If Not Me.validate Then UI.CssClass &= " exVal"
        If Me Is quote.MostRecent Then UI.CssClass &= " mostRecent" 'used to target the flying frame animation '~~~
        If Me Is quote.Cursor Then UI.CssClass &= " quoteCursor" 'used to target the flying frame animation

        UI.ID = "QI" & Me.ID
        Dim product As clsProduct
        Dim issystem As Boolean = False

        If Me Is Me.quote.RootItem Then 'AKA 'Parts bin
            'this is the outermost 'root' item (where we *can* add (orphaned) options)
            'When we click on - OR THE EVENT BUBBLING REACHES the root item - we 'unlock' the cursor
            '                                                                                   note this is OUTside the IF
            'was omd
            UI.CssClass &= " quoteRoot"


            ''THIS WAS THE 'PARTS BIN' - Which was disbaled at Gregs request /01/01/2015 - UI.Attributes("onclick") = "burstBubble(event);if(!ajaxing){setQuoteCursor(" & Me.ID & ",'');}"
            'The root item

            Dim qh As Panel = New Panel
            qh.CssClass = "quoteHeader"



            Dim namepanel As Panel = New Panel()

            namepanel.Controls.Add(NewLit(Xlt("Quote", agentAccount.Language) & " " & Me.quote.RootQuote.ID & "-" & quote.Version & If(quote.Saved, "<span class='saved'>(" & Xlt("saved", BuyerAccount.Language), "<span class='draft'>(" & Xlt("draft", BuyerAccount.Language)) & ")</span>"))
            qh.Controls.Add(namepanel)
            'action buttons
            Dim butts As Literal = New Literal
            Dim criticalMsgs As List(Of ClsValidationMessage) = Me.ValidationsGreaterThanEqualTo(EnumValidationSeverity.RedCross)
            'SAMS version FindValidation(selectedMsgs, Me)

            '       If Me.quote.Saved Then
            ' 'butts.Visible = False
            'butts.Text = "<div class='q_outputs'><div class='q_saved' onclick = ""burstBubble(event); quoteEvent('Save'); return false;"" title ='" & Xlt("Save", agentAccount.Language) & "'></div> "
            'Else
            '            butts.Text = "<div class='q_outputs'><div class='q_save' onclick = ""burstBubble(event); quoteEvent('Save'); return false;"" title ='" & Xlt("Save", agentAccount.Language) & "'></div> "
            ' End If

            If criticalMsgs.Count = 0 Then  ' need to sort out the images for 
                '"<div class='q_outputs'><div class='q_save' onclick = ""burstBubble(event); quoteEvent('Save'); return false;"" title ='Save'></div> " & _
                butts.Text = butts.Text & "<div title ='" & Xlt("Export", agentAccount.Language) & "' class='q_export ' onclick = ""burstBubble(event); showMenu(" & Me.quote.ID.ToString & "); return false;""><div id = ""exportMenu" & Me.quote.ID.ToString & """  class = ""submenu"" > " & _
                    "<a class=""account""> " & Xlt("Export Option", agentAccount.Language) & "s</a> <ul class=""root"" >" & _
                    "<li><a onclick = ""burstBubble(event); quoteEvent('Excel'); return false;"" href=""#"">" & Xlt("Excel", agentAccount.Language) & "</a></li>" & _
                    "<li><a onclick = ""burstBubble(event);  quoteEvent('PDF'); return false;"" href=""#"">" & Xlt("PDF", agentAccount.Language) & "</a></li>" & _
                    "<li><a onclick = ""burstBubble(event);  quoteEvent('XML'); return false;"" href=""#"">" & Xlt("XML", agentAccount.Language) & "</a></li>" & _
                    "<li><a href=""#"" onclick = ""burstBubble(event);  quoteEvent('XMLAdv'); return false;"">" & Xlt("XML Advanced", agentAccount.Language) & "</a></li>" & _
                    "<li><a href=""#"" onclick = ""burstBubble(event);  quoteEvent('XMLSmartQuote'); return false;"">" & Xlt("XML SmartQuote", agentAccount.Language) & "</a></li></ul>" & _
                    " </div></div> " & _
                    "<div title ='" & Xlt("Email", agentAccount.Language) & "' class='q_email' onclick = ""burstBubble(event); quoteEvent('Email'); return false;""></div> "
                '& "<div class='q_excel' onclick = ""burstBubble(event); saveNote(); quoteEvent('Excel'); return false;""></div> " _
                '& "<div class='q_xml' onclick = ""burstBubble(event); saveNote(); quoteEvent('XML'); return false;""></div></div>"
            Else
                butts.Text = butts.Text & "<div title ='" & Xlt("Export", agentAccount.Language) & "' class='q_export ' onclick = ""burstBubble(event); displayMsg('" & Xlt("Export not available due to validation errors", BuyerAccount.Language) & "'); return false;""></div>"
            End If
            If quote.Locked Then
                butts.Text = butts.Text & "<div title ='" & Xlt("Create Next Version", agentAccount.Language) & "' class='q_newVlocked' onclick = ""burstBubble(event); displayMsg('Next version created');  rExec('Manipulation.aspx?command=createNextVersion&quoteId=" & quote.ID & "', showQuote); return false;""></div> "
            Else
                If quote.Saved Then
                    butts.Text = butts.Text & "<div  title ='" & Xlt("Create Next Version", agentAccount.Language) & "' class='q_newVunlocked' onclick = ""burstBubble(event);displayMsg('Next version created');  rExec('Manipulation.aspx?command=createNextVersion&quoteId=" & quote.ID & "', showQuote); return false;""></div>"
                End If

                'Dim btnNextVersion As New Button
                'btnNextVersion.Text = Xlt("Create next version", agentAccount.Language)
                'btnNextVersion.ToolTip = Xlt("Creates a copy leaving the original quote intact", agentAccount.Language)
                'btnNextVersion.OnClientClick = "rExec('Manipulation.aspx?command=createNextVersion&quoteId=" & quote.ID & "', gotoTree);return false;"
                'UI.Controls.Add(btnNextVersion)

            End If

            If quote IsNot Nothing Then

                If criticalMsgs.Count = 0 And (iq.sesh(lid, "GK_BasketURL") IsNot Nothing Or agentAccount.SellerChannel.orderEmail <> "") Then
                    ' Dim litAddtobasket = New Literal
                    'butts.Text = butts.Text & "<div class='hpBlueButton smallfont ib'  onclick = ""burstBubble(event); saveNote(false); quoteEvent('Addbasket'); return false;"">" & Xlt("Place Order", agentAccount.Language) & "</div></div> "

                    butts.Text = butts.Text & "<div class='hpOrangeButton q_basket smallfont'  onclick = ""burstBubble(event); saveNote(false); quoteEvent('Addbasket" & quote.Saved & "'); return false;"">&nbsp;</div>"
                Else
                    'butts.Text = butts.Text & "</div> "
                End If

                'Dim addToBasket As Button = New Button()
                'addToBasket.Text = Xlt("Add to Basket", quote.BuyerAccount.Language)
                'addToBasket.ID = "btnAddToBasket"
                'AddHandler addToBasket.Click, AddressOf Me.addToBasket_Click

                '   qh.Controls.Add(litAddtobasket)
            Else
                butts.Text = butts.Text & "</div>"
            End If

            namepanel.Controls.Add(butts)

            Dim ih As Panel = New Panel

            ih.CssClass = "innerHeader"


            'quoteName/Customer - Sams pop open panel - part of the export tools
            Dim qnp As Panel = New Panel
            qnp.ID = "quotepanel"


            Dim qn$ = Me.quote.Name.DisplayValue
            If Trim$(qn$) = "" Or Trim$(qn$) = "-" Then qn$ = Me.quote.BuyerAccount.BuyerChannel.Name


            qnp.Controls.Add(NewLit("<div id = 'quoteText' ><input id='saveQuoteName'" & If(quote.Saved, " value='" & qn & "'", "") & " type='text' placeholder='" & Xlt("enter quote name", BuyerAccount.Language) & "' onclick= 'burstBubble(event); return false;'   onkeydown = 'var e=event||window.event; var keyCode=e.keyCode||e.which; if (keyCode==13){return false;}'/> " _
                                    & "<input id = 'hiddenType' type='hidden' value='Save' /><input id = 'hiddenName' type='hidden' value='" & qn$ & "' /><input id ='hdnEmail' type = 'hidden' value ='" & Me.quote.BuyerAccount.User.Email & "'/><input id = 'continueBtn' type='button' class=""hpBlueButton smallfont"" style=""margin-top:-0.75em; margin-left:0.6em;"" value = '" & Xlt("Save", agentAccount.Language) & "' onClick ='burstBubble(event); continueClick();'/><input id = 'cancelBtn' type='button' onclick=""burstBubble(event); $('#saveQuoteName').val($('#hiddenName').val());$('#continueBtn').val($('#hiddenSaveTrans').val());$('#cancelBtn').hide();$('#quoteText').show();$('#hiddenType').val('Save');return false;"" class=""hpBlueButton smallfont"" style='display:none;margin-top:-0.70em; margin-left:0.6em;' value ='" & Xlt("Cancel", agentAccount.Language) & "'  />" _
                                    & "<input id = 'hiddenEmailTrans' type='hidden' value ='" & Xlt("Send Email", agentAccount.Language) & "'  /><input id = 'hiddenSaveTrans' type='hidden' value ='" & Xlt("Save", agentAccount.Language) & "'  /></div>")) ' removed 19/01 <input id = 'cancelBtn' type='button' class=""hpGreyButton  smallfont"" value = '" & Xlt("Cancel", agentAccount.Language) & "' style=""margin-top:-0.75em; margin-left:0.6em;"" onClick ='burstBubble(event); quoteCancel();'/>


            'Systems Options summary + validation rollup

            Dim sysOptSum As Panel = New Panel
            sysOptSum.ID = "sysOptSumm"

            Dim systems As Integer, options As Integer
            Me.countSystems(systems, options)

            Dim syss As String : If systems > 1 Or systems = 0 Then syss = "systems" Else syss = "system"
            Dim opts As String : If options > 1 Or options = 0 Then opts = "options" Else opts = "option"
            syss = Xlt(syss, agentAccount.Language)
            opts = Xlt(opts, agentAccount.Language)

            sysOptSum.Controls.Add(NewLit("<span class='sysOptCount'>" & systems & " " & syss & ", " & options & " " & opts & "</span>"))

            Dim vdRollup As Panel = New Panel
            vdRollup.CssClass = "validationRollup"
            sysOptSum.Controls.Add(Me.MessageCounts({enumValidationMessageType.Validation}, {}, {EnumValidationSeverity.DoesQualify, EnumValidationSeverity.DoesntQualify}, True)) 'exclude flex qualification messages from the roll up

            Dim lblSpace As Label = New Label
            lblSpace.Text = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"
            sysOptSum.Controls.Add(lblSpace)

            Dim dlp = New Dictionary(Of clsScheme, Integer)
            Me.LoyaltyPoints(dlp)

            Dim BCTotal = 0
            Dim toolTip As String = ""

            If iq.i_scheme_code.ContainsKey("BC") Then
                For Each scheme In iq.i_scheme_code("BC")
                    If scheme.Region.Encompasses(agentAccount.SellerChannel.Region) Then
                        'We have an active region for this account
                        If dlp.ContainsKey(scheme) Then BCTotal += dlp(scheme)
                        toolTip = scheme.displayName(agentAccount.Language)
                    End If
                Next
            End If

            If BCTotal > 0 Then

                Dim points As String = Xlt("Points", agentAccount.Language)

                Dim lblpointsTitle As Label = New Label
                lblpointsTitle.CssClass = "BlueCarpetTitle"
                lblpointsTitle.Text = points & ": "
                lblpointsTitle.ToolTip = points
                sysOptSum.Controls.Add(lblpointsTitle)

                Dim lblpoints As Label
                lblpoints = New Label
                lblpoints.CssClass = "BlueCarpet"
                lblpoints.Text = BCTotal.ToString
                lblpoints.ToolTip = points
                sysOptSum.Controls.Add(lblpoints)

                Dim lblSpace2 As Label = New Label
                lblSpace2.Text = lblSpace.Text
                sysOptSum.Controls.Add(lblSpace2)

                Dim lnkBtn As LinkButton = New LinkButton
                lnkBtn.Attributes.Add("onClick", "return false;")

                lnkBtn.Text = Xlt("Learn More", BuyerAccount.Language)
                lnkBtn.OnClientClick = "LearnMoreClick();"

                sysOptSum.Controls.Add(lnkBtn)
            End If

            'grand total
            'If Me.quote.QuotedPrice.isValid Then
            Dim PnlGrandTotal As Panel = New Panel
            PnlGrandTotal.CssClass = "grandTotal"
            PnlGrandTotal.Controls.Add(NewLit(Xlt("Total", agentAccount.Language) & " "))

            'price panel
            Dim finalprice As NullablePrice = New NullablePrice(Me.quote.QuotedPrice.NumericValue - Me.quote.TotalRebate, Me.quote.Currency, Me.quote.QuotedPrice.isList)
            finalprice.isTotal = True
            Dim pp As Panel = finalprice.DisplayPrice(BuyerAccount, errorMessages)
            pp.CssClass &= " finalPrice"
            PnlGrandTotal.Controls.Add(pp)

            'show the 'quotewide',propogating margin only once there is more than one item at the root level
            If Me.quote.RootItem.Children.Count > 1 And Not (agentAccount.SellerChannel.marginMin = 0 And agentAccount.SellerChannel.marginMax = 0) Then
                PnlGrandTotal.Controls.Add(Me.MarginUI(True, Me.quote.Locked)) 'whole quote margin - goes inside the header at Dans )
            End If


            ih.Controls.Add(qnp)
            ih.Controls.Add(PnlGrandTotal)
            ih.Controls.Add(sysOptSum)
            qh.Controls.Add(ih)
            UI.Controls.Add(qh)
        Else
            product = Me.Branch.Product

            If product IsNot Nothing Then 'skip chassis (and other invisible) branches
                If Me.Branch.childBranches.Count > 0 Then
                    'SYSTEM
                    issystem = True
                    'this quote items' product branch has sub items and so can be targetted for options (it's a 'system')
                    UI.CssClass &= " quoteSystem"
                    UI.Controls.Add(Me.SystemHeader(agentAccount, BuyerAccount, errorMessages)) 'the virtual system/rollup/total header (also contains the expand/collapse button
                    UI.Attributes("onclick") = "burstBubble(event);if(!ajaxing){setQuoteCursor(" & Me.ID & ");getBranches('cmd=open&path=" & Me.Path & "&into=tree&Paradigm=C')};"

                Else
                    'It's an OPTION - show it in the tree (and highlight it if its clicked)
                    UI.CssClass &= " quoteOption"

                    Dim from As String = "tree." & iq.RootBranch.ID 'Default to 'from' the root
                    If Me.SystemItem IsNot Nothing Then from = Me.SystemItem.Path 'Override with the system if its there
                    UI.Attributes("onclick") = "burstBubble(event);if(!ajaxing){setQuoteCursor(" & Me.Parent.ID & ");getBranches('cmd=open&path=" & from & "&to=" & Me.Path & "&into=tree&Paradigm=C')};"

                End If
                '          UI.Attributes("onclick") = "burstBubble(event);if(!ajaxing){setQuoteCursor(" & Me.Parent.ID & ");getBranches('cmd=open&path=" & from & "&to=" & Me.Path & "&into=tree')};"
            End If

            If Not Me.collapsed Then

                'note - for Pauls benefit if you have the DIAGVIEW role .. you see ALL products int he basket

                If Me.ShouldShowInBasket(includePreinstalled, BuyerAccount, foci) Then
                    UI.Controls.Add(Me.basketLine(agentAccount, BuyerAccount, product, foci, errorMessages, lid))
                End If

                'Dim vms As Panel = New Panel
                'UI.Controls.Add(vms)
                'vms.CssClass = "validationMessages"
                'For Each vm As ClsValidationMessage In Me.Msgs
                '    If vm IsNot Nothing Then  'TODO remove - some 'nothings' are getting in
                '        vms.Controls.Add(vm.UI(BuyerAccount, agentAccount.Language, errorMessages))
                '    End If
                'Next


            End If
        End If


        'we *always* recurse (Through invisible options) otherwise we'd lose options in preinstalled options) .. but only append the HTML for preinstalled items if includePreinstalled=true

        'dont recurse into collapse items
        If Not Me.collapsed Then

            Dim options As Panel = New Panel
            Dim systems As Panel = New Panel

            Dim addto As Panel
            UI.Controls.Add(systems)
            UI.Controls.Add(options)

            Dim prevDisplayText As String = ""
            Dim itemsToRemove As New ArrayList
            Dim isPrevPreInstalled As Boolean
            Dim isPreInstalled As Boolean
            Dim prevItemQuantity As Integer
            Dim itemQuantity As Integer
            Dim path As String
            Dim isASystem As Boolean

            Dim prevItemQuantities As New ArrayList
            Dim prevDisplayTexts As New ArrayList
            Dim prevItem As IQ.clsQuoteItem = Nothing

            '' This avoids systems and only uses options. It also orders by the option name.
            For Each item In From c In Me.Children Where c.order > 2 Order By c.SKUVariant.DistiSku

                path = item.Path
                isASystem = item.Branch.Product.isSystem(path)
                If (isASystem = False) Then
                    Dim displayText As String = item.SKUVariant.DistiSku
                    isPreInstalled = item.IsPreInstalled

                    itemQuantity = item.Quantity
                    If ((displayText = prevDisplayText) And (isPreInstalled = False) And (isPrevPreInstalled = False)) Then
                        item.Quantity = prevItemQuantity + item.Quantity
                        If (Not prevItem Is Nothing) Then
                            Me.Children.Remove(prevItem)
                        End If
                    End If
                    prevDisplayText = displayText
                    isPrevPreInstalled = isPreInstalled
                    prevItemQuantity = item.Quantity
                    prevItem = item
                End If
            Next

            'order systems first then options - so that we can group the root level options into a parts bin (single div) we can draw a box around

            For Each item In Me.Children.Where(Function(ch) ch.ShouldShowInBasket(includePreinstalled, BuyerAccount, foci)).GroupBy(Function(ch) ch.Branch.Product.isSystem(ch.Path))
                If item.Key Then addto = systems Else addto = options

                'Ruined below temp with andalso False until this is ok'd
                For Each i In item.GroupBy(Function(ch) ch.Branch.Product.ProductType.Translation.text(English) & If(ch.IsPreInstalled AndAlso False, 0, ch.ID).ToString).OrderByDescending(Function(ch) ch.First.Branch.Product.ProductType.Order).OrderByDescending(Function(ch) ch.First.IsPreInstalled)
                    If i.Count > 1 And i.First.IsPreInstalled Then
                        'Add header
                        Dim panel As Panel = New Panel()
                        panel.CssClass = "quoteGroup"
                        panel.Attributes("OnClick") = "burstBubble(event);"
                        panel.Controls.Add(NewLit("<h3 style=""outline-color:white;background:white;"">" + i.First.Branch.Product.ProductType.Translation.text(BuyerAccount.Language) + "</h3>"))
                        Dim panel2 As Panel = New Panel()
                        addto.Controls.Add(panel)
                        panel.Controls.Add(panel2)

                        For Each qi In i
                            If qi.Branch IsNot Nothing Then panel2.Controls.Add(qi.UI(includePreinstalled, BuyerAccount, agentAccount, foci, errorMessages, validationMode, lid))
                        Next
                    Else
                        If Not i.First.IsPreInstalled Or includePreinstalled Then  'was me.preinstalled - which was a bug (i think) NA 12/06/2014
                            'and... recurse
                            If i.First.Branch IsNot Nothing Then
                                If Not i.First.Branch.Product.isSystem(i.First.Path) Then
                                    If Me Is quote.RootItem Then
                                        If Me Is quote.RootItem Then options.Attributes("class") = "partsBin"
                                    End If
                                End If
                            End If

                            'formerly UI.controls.add 

                            addto.Controls.Add(i.First.UI(includePreinstalled, BuyerAccount, agentAccount, foci, errorMessages, validationMode, lid))
                        End If
                    End If
                Next
            Next



            '    Case Is = viewTypeEnum.Summary
            'this was the old summary/BOM view - which has been obsoleted (before it ever made it out) 'is essentially the same - potentially consolidates some items
            ' UI.Controls.Add(Me.FlatList(BuyerAccount, errorMessages))

            '        If viewType and viewTypeEnum).validation then 
            'Case Is = viewTypeEnum.validation

            If issystem Then

                UI.Controls.Add(Me.SystemFooter(BuyerAccount, agentAccount, errorMessages)) 'Includes flex checklist

                If Me.ExpandedPanels.Contains(panelEnum.Spec) Then
                    UI.Controls.Add(Me.specPanelOpen)
                Else
                    If specPanelClosed IsNot Nothing Then
                        UI.Controls.Add(Me.specPanelClosed)
                    End If
                    'UI.Controls.Add(Me.validationpanel)
                    'UI.Controls.Add(Me.PromosPanel)
                End If

                UI.Controls.Add(Me.ValidationPanel(BuyerAccount, agentAccount, errorMessages))
                ' OBSOLETED      UI.Controls.Add(Me.PromosPanel(agentAccount)) ' Total rebate, Loyalty point by schecme, Bundle savings

            End If

            ''output each items validation messages *HERE* to get them in contexct
            'Dim vms As Panel = New Panel
            'UI.Controls.Add(vms)
            'vms.CssClass = "validationMessages"
            'For Each vm As ClsValidationMessage In Me.Msgs
            '    If vm IsNot Nothing Then  'TODO remove - some 'nothings' are getting in
            '        vms.Controls.Add(vm.UI(BuyerAccount, agentAccount.Language, errorMessages))
            '    End If
            'Next

        End If

    End Function

    'Private Shared Function Flatten(source As IEnumerable(Of clsQuoteItem)) As IEnumerable(Of clsQuoteItem)
    '    Return source.Concat(source.SelectMany(Function(p) Children.Flatten()))
    'End Function

    Public Iterator Function GetFamily(parent As clsQuoteItem) As IEnumerable(Of clsQuoteItem)
        Yield parent
        For Each child As clsQuoteItem In parent.Children
            ' check null if you must
            For Each relative As clsQuoteItem In GetFamily(child)
                Yield relative
            Next
        Next
    End Function


    Private Function QuantityBox(buyerAccount As clsAccount, language As clsLanguage, ByRef errorMessages As List(Of String)) As TextBox

        'Quantity
        Dim txtqty As TextBox
        txtqty = New TextBox
        txtqty.Text = Me.Quantity.ToString.Trim
        txtqty.ID = "Q" & Me.ID
        txtqty.CssClass = "quoteTreeQty"
        Dim stk As Int32 = 0
        Dim prices As List(Of clsPrice) = Me.Branch.Product.GetPrices(buyerAccount, buyerAccount.SellerChannel.priceConfig, iq.AllVariants, errorMessages, True)

        Dim stock As String = Me.Branch.Product.CurrentStock(buyerAccount, stk, Me.SKUVariant, errorMessages)
        If stk < Me.Quantity And Not Me.IsPreInstalled Then
            txtqty.CssClass &= " outOfStock"
            txtqty.ToolTip = String.Format(Xlt("Warning: {0} in stock", language), stock)
        End If

        If Me.IsPreInstalled Or Me.Branch.Product.hasSKU = False Then
            txtqty.Enabled = False
        Else
            If Not Me.quote.Locked Then
                txtqty.Attributes("onclick") = "burstBubble(event); return false;"
                txtqty.Attributes("onblur") = "burstBubble(event); changeQty('" & txtqty.ID & "','" & Me.ID & "','','',true);return false;"
                txtqty.Attributes("onkeypress") = "Javascript: if (event.keyCode==13) {burstBubble(event); changeQty('" & txtqty.ID & "','" & Me.ID & "','','',true);return false; }"
            Else
                txtqty.Enabled = False
            End If
        End If
        Return txtqty

    End Function

    Public Function basketLine(agentAccount As clsAccount, buyerAccount As clsAccount, product As clsProduct, foci As HashSet(Of String), ByRef errormessages As List(Of String), lid As UInt64) As Panel

        Dim bl As Panel = New Panel

        Dim priceChange As Boolean = False
        'Dim translationLanguage As clsLanguage = agentAccount.Language
        'If translationLanguage Is Nothing Then translationLanguage = English
        'translationLanguage = English

        bl.CssClass = "basketLine"
        If Me.IsPreInstalled Then
            bl.CssClass &= " preInstalled"
        ElseIf Not Me.Branch.Product.isSystem(Me.Path) Then
            bl.CssClass &= " addonItem"
        End If


        'System/option name label
        Dim lbl As New Label
        If Me.Branch.Product.isSystem(Me.Path) Then
            lbl.Text = Xlt("System unit", agentAccount.Language)
            bl.CssClass &= " systemLine"
        Else
            lbl.Text = Me.ShortName(agentAccount.Language, True)
            lbl.ToolTip = Me.ShortName(agentAccount.Language, True)
        End If


        'lbl.Text = product.ProductType.Translation.text(s_lang) & " "
        lbl.CssClass = "quoteTreeType"

        If product.isSystem(Me.Path) Then lbl.Font.Bold = True
        bl.Controls.Add(lbl)

        'SKU
        lbl = New Label

        If Left$(product.SKU, 3) = "###" Then
            lbl.Text = decodeTrebbleHash(product.SKU)
        Else
            lbl.Text = product.SKU
            If Me.IsPreInstalled And Me.Branch.Product.ProductType.Code.ToLower = "cpu" Then
                Dim altSKU As String = Me.Branch.Product.FirstAttributeEnglishText("altSKU")
                If altSKU <> "" Then  'IF an FIO CPU has an altSKU we display it as that
                    lbl.Text = altSKU
                End If
            End If

            If (Not String.IsNullOrEmpty(Me.SKUVariant.Code) AndAlso (Not Me.SKUVariant.Code.Equals("list", StringComparison.InvariantCultureIgnoreCase))) Then
                lbl.Text += "#" & Me.SKUVariant.Code
            End If
        End If

        lbl.CssClass = "quoteTreeSKU"

        If product.isSystem(Me.Path) Then lbl.CssClass &= " isSystem"

        'With tooltip description
        If product.i_Attributes_Code.ContainsKey("desc") Then
            lbl.ToolTip = product.i_Attributes_Code("desc")(0).Translation.text(agentAccount.Language)
        End If

        If Me.SKUVariant.DistiSku <> product.SKU Then
            lbl.ToolTip &= " (" & Me.SKUVariant.DistiSku & ")"
        End If

        If Me.Branch.Product.isFIO Then
            lbl.ToolTip &= " *FIO"
        End If

        'for debugging Watts Slots (aka powersizing)
        If AccountHasRight(lid, "DIAGVIEW") Then
            Dim wlbl As Label
            wlbl = New Label
            wlbl.Text = "W"
            Dim tt$ = ""
            For Each slot In Branch.slots.Values

                If slot.Type.MinorCode = "W" Then
                    If slot.path = "" Or LCase(slot.path) = LCase(Me.Path) Then
                        If LCase(slot.path) = LCase(Me.Path) And Me.Path <> "" Then
                            tt$ &= "*" & slot.numSlots.ToString & "W*" & vbCrLf & tt$
                        Else
                            tt$ &= slot.numSlots.ToString & "W " & vbCrLf & tt$
                        End If
                    End If
                End If
            Next
            wlbl.ToolTip = tt$  'No translation required - this is an admin tool
            bl.Controls.Add(wlbl) 'adds a 'W' in forn of the part number with a tooltip gicing wattage
        End If


        bl.Controls.Add(lbl)

        'new - nick23/03/2015 - more consistent with how the visibility of BuyUI is determined elesewhere
        'Dim pc As Integer = buyerAccount.SellerChannel.priceConfig
        'Dim prices As List(Of clsPrice) = Me.Branch.Product.GetPrices(buyerAccount, pc, Me.SKUVariant, errormessages, False)


        'system unit quantities have been moved 'up' to become the 'multiplier' in the systemheader
        'which is clearer - but won't work great for racks/options for options
        Dim allowflexup As Boolean
        If Not Me.Branch.Product.isSystem(Me.Path) Then

            bl.Controls.Add(Me.QuantityBox(buyerAccount, agentAccount.Language, errormessages))

            'In quote Quantity Flex Buttons - also calls validate for every item (which is expensive)
            If Me.Branch.Product.hasSKU Then
                '                  DONT insist it's in the feed anymore - (there's probably a list price)
                '                  it shouldn't be in the basket in the first place if there isnt !
                allowflexup = True 'Me.Branch.Product.inFeed(buyerAccount.SellerChannel)
                If Me.Branch.Product.isFIO Then allowflexup = False 'Factory Installed Options (mostly ###'s) cannot be flexed 
                'If slots are maxed then stop flex up
                If Not Me.SystemItem.hasRoomFor(Me) AndAlso Not Me.Branch.Product.isSystem(Me.Path) Then allowflexup = False
                bl.Controls.Add(Me.FlexButtons(allowflexup))
            End If

        End If

        'price
        'lbl = Me.QuotedPrice.DisplayPrice(quote.BuyerAccount, errormessages)
        'lbl.CssClass = "quoteTreePrice"
        'If product.isSystem Then lbl.CssClass &= " isSystem"

        ''dislpay prices that have been confirmed in the last hour as solid or green or something
        'If Me.upToDate Then lbl.CssClass &= " upToDate" Else lbl.CssClass &= " unconfirmed"

        'this is an AJAX updateable Price (which will be updated by placePrices - such that we don't need to refresh the basket anymore !

        'Dim price As List(Of clsPrice) = New List(Of clsPrice)
        'price.Add(

        If (allowflexup) Or (Me.Branch.Product.isSystem(Me.Path)) Or (Not Me.IsPreInstalled) Then
            Dim prices As List(Of clsPrice) = SKUVariant.Product.Prices(buyerAccount, SKUVariant)
            ' Me.Branch.Product.Prices(buyerAccount, Me.SKUVariant)
            Dim oldPrice As NullablePrice = Me.BasePrice
            Dim updateablePrice As Panel = New Panel
            If prices.Count = 0 And SKUVariant.Product.ListPrice(buyerAccount) IsNot Nothing Then
                'fetch HPs list price according to the buyers region and currency - NB list pricing is per country at the moment (ie. there is no EMEA list price)
                prices.Add(SKUVariant.Product.ListPrice(buyerAccount))
            End If
            If prices.Count = 0 OrElse prices(0) Is Nothing Then
                Dim m$ = String.Format(Xlt("* No price available for {0} ( {1}  variant)", agentAccount.Language), Me.Branch.Product.SKU, SKUVariant.Code)
                errormessages.Add(m$)

            Else
                Dim container As Panel = New Panel
                container.CssClass = "quoteTreePrice"
                ''what if a price gets ajax'd in AFTER we've applied margin  ??
                'Check for Items price with the latest price and flag of price change 
                Dim currentPrice As List(Of clsPrice) = (From p In prices Where p.Price.value > 0).ToList()
                If currentPrice.Count > 0 Then
                    For Each newPrice In currentPrice
                        If newPrice.Price.value <> oldPrice.value Then
                            priceChange = True
                        End If
                    Next
                End If

                If priceChange Then
                    Dim litPriceChange As Literal = New Literal()
                    litPriceChange.Text = "<input type=""hidden"" class =""pricechangeitem"" id =""prchange" & product.ID & """  value =""true"" />"
                    bl.Controls.Add(litPriceChange)
                End If

                If currentPrice.Count = 0 Then
                    updateablePrice = oldPrice.DisplayPrice(buyerAccount, errormessages)
                Else
                    updateablePrice = prices(0).Ui(buyerAccount, Me.Margin, lid)
                End If
                If Me.ID = 22508 Then
                    Dim litPriceChange As Literal = New Literal()
                    litPriceChange.Text = "<input type=""hidden"" class =""pricechangeitem"" id =""prchange" & product.ID & """  value =""true"" />"
                    bl.Controls.Add(litPriceChange)
                End If


                container.Controls.Add(updateablePrice)
                bl.Controls.Add(container)
            End If
        End If

        'Preinstalled Items should not showPromo markers (flex Attach or  Blue carpet)
        If Not Me.IsPreInstalled Then
            Dim promoMarkers As PlaceHolder = Me.Branch.PromoIndicators(buyerAccount, agentAccount, Me.Path, foci, True, Me.IsPreInstalled, errormessages, Nothing)
            bl.Controls.Add(promoMarkers)
        End If

        If Me.rebate <> 0 Then
            Dim avlabel As Label = New Label
            avlabel.Text = "✔"
            avlabel.CssClass &= "basketAvalancheTick"  'probably needs a slight change (add a seperate *)

            Dim saving As NullablePrice = New NullablePrice(Me.rebate / Me.DerivedQuantity, Me.quote.Currency, False) 'making it into a nullableprice allows us to get this displaprice (lable) - which does the currency/culture formatting
            avlabel.ToolTip = Xlt("Qualifies - Saving ", agentAccount.Language) & saving.text(quote.BuyerAccount, errormessages) & Xlt(" per item ", agentAccount.Language) '(" & CStr(Me.OPG.value) & ")"

            bl.Controls.Add(avlabel)
        End If


        'flex buttons were here
        If Not Me Is Me.quote.RootItem Then
            bl.Controls.Add(Me.AddNoteButton)
        End If


        If Me.Parent.Margin <> 1 Or Me.Margin <> 1 Then
            If Not Me.IsPreInstalled And Not (agentAccount.SellerChannel.marginMin = 0 And agentAccount.SellerChannel.marginMax = 0) Then

                bl.Controls.Add(Me.MarginUI(False, Me.quote.Locked))
            End If
        End If


        If Not IsDBNull(Me.Note.value) Then
            Dim notePanel As New Panel
            bl.Controls.Add(notePanel)
            Dim tb As New TextBox
            tb.ID = "note" & Me.ID
            tb.Attributes("style") = "position:relative;left:1.25em;top:-.25em;background-color:#FCF0AD;color:black;width:23em;border:none;"
            tb.Attributes("onclick") = "burstBubble(event);" 'stops the event propagation
            tb.Attributes("onfocus") = "currentNote='" & tb.ID & "';"
            tb.Attributes("onblur") = "saveNote(false);"  'uses the currentNote JS variable

            tb.Attributes("onkeydown") = "var e=event||window.event; var keyCode=e.keyCode||e.which; if (keyCode==13){return false;}"

            'var e=event||window.event; var keyCode=e.keyCode||e.which; if (keyCode!=13){//code goes here}

            tb.AutoPostBack = False
            notePanel.Controls.Add(tb)
            tb.Text = Me.Note.DisplayValue
            bl.Controls.Add(notePanel)

            Dim img As Image = New Image
            img.ImageUrl = "../images/navigation/trash.png"

            notePanel.Controls.Add(img)
            img.Attributes("onclick") = "burstBubble(event);delNote(" & Me.ID & ");"
            img.Attributes("style") = "width:1.25em;height:1.25em;position:relative;left:1.75em;top:-.15em;"
            notePanel.Controls.Add(img)
            img.ToolTip = Xlt("Delete this note", agentAccount.Language)
        End If

        Return bl


    End Function

    Public Function fetchPreinstalled(lid As UInt64, buyeraccount As clsAccount, ByRef errormessages As List(Of String)) As Integer 'WebControls.Image previoulsy a fetcheimage - with onloadscript


        'DON'T DO This if there's no webservice !
        If (quote.BuyerAccount.SellerChannel.priceConfig And 8) = 0 Then Return 0

        Dim toget As List(Of clsVariant) = additionalUpdates(Me.Branch, buyeraccount, Me.Path, errormessages)

        Dim handle As Integer
        handle = ModUniTran.DispatchUpdateRequest(lid, toget, "", errormessages)

        'pbi.path$ - tree.1 was pbi.path - but there's no real reason (apart from perhaps swift) not to placeprices across the whole tree
        If handle = 0 Then
            errormessages.Add("*" & Xlt("Could not dispatch web request (handle was 0)", Me.quote.AgentAccount.Language))
        Else
            'inserts an image with an onload script which calls the js FillPrices() after 5 seconds
            'refreshing the prices within the quotouter dost update totals
            '    Return fetcherImage("quote", handle, toget)
            Return handle
        End If

    End Function

    Private Function SystemFooter(buyeraccount As clsAccount, agentaccount As clsAccount, ByRef errorMessages As List(Of String)) As Panel

        'contains the loyalty points and flex discount summary per system - 

        Dim dicPoints As Dictionary(Of clsScheme, Integer) = New Dictionary(Of clsScheme, Integer)

        Me.LoyaltyPoints(dicPoints)

        SystemFooter = New Panel
        SystemFooter.Attributes("class") = "flexChecklist"

        'If dicPoints.Count > 0 Then
        '    For Each scheme In dicPoints.Keys
        '        Dim lps As String = "<div class='pointsScheme'>" & scheme.Name.text(buyeraccount.Language) & "</div>"
        '        lps &= "<div class='pointsValue'>" & dicPoints(scheme).ToString & "</div><br/>"
        '        SystemFooter.Controls.Add(NewLit(lps))
        '    Next
        'End If

        Dim region = agentaccount.BuyerChannel.Region
        Dim t = (From l In Me.Branch.Product.OPGflexLines.Values Where l.FlexOPG.AppliesToRegion(region)).ToList()

        Dim flexConditionCount As Integer = 0
        Dim flexDoesQualifyPlaceholder As PlaceHolder = outputValidations({enumValidationMessageType.Flex}, {EnumValidationSeverity.DoesQualify}, {}, buyeraccount, agentaccount, errorMessages, flexConditionCount)
        Dim flexDoesntQualifyPlaceholder As PlaceHolder = outputValidations({enumValidationMessageType.Flex}, {EnumValidationSeverity.DoesntQualify}, {}, buyeraccount, agentaccount, errorMessages, flexConditionCount)

        If flexConditionCount > 0 Then

            'Ouput the Flex Qualifiaction messages
            Dim litFlexH3 As Literal = New Literal
            If t.Count > 0 Then
                litFlexH3.Text = "<h3>" & Xlt("HP FlexAttach Requirements", agentaccount.Language) & "</h3>"
            End If
            SystemFooter.Controls.Add(litFlexH3)

            SystemFooter.Controls.Add(flexDoesQualifyPlaceholder)
            SystemFooter.Controls.Add(flexDoesntQualifyPlaceholder)

            'need to totalise the rebate acrros 'me' (the system) and 'my' options 
            Dim rr As Decimal 'running rebate
            Me.Totalise(New NullablePrice(agentaccount.Currency), rr, False)

            Dim flexRebate As Literal = New Literal
            If rr <> 0 Then

                flexRebate.Text = "<div id=""flexDis"" class= ""flexDiscountDiv""> " & Xlt("HP FlexAttach Saving", agentaccount.Language) & "&nbsp;&nbsp;" & Me.quote.Currency.format(rr, buyeraccount.Culture.Code, errorMessages, 2) & "</div>"
                '  flexRebate.BackColor = System.Drawing.ColorTranslator.FromHtml("#E1F0E1")

            Else
                If Me.Branch.Product.isSystem(Me.Path) And Me.Branch.Product.OPGflexLines.Count > 0 Then

                    If t.Count > 0 Then
                        flexRebate.Text = "<div id=""flexDis"" class= ""flexDiscountDiv"">" & Xlt("HP FlexAttach Saving None", agentaccount.Language) & "</div>"
                    End If

                End If

            End If
            SystemFooter.Controls.Add(flexRebate)

        End If


    End Function


    Private Function SystemHeader(Agentaccount As clsAccount, buyeraccount As clsAccount, ByRef errorMessages As List(Of String)) As Panel

        Dim header As Panel = New Panel

        Dim script$ = ""
        If Me.collapsed Then
            header.Controls.Add(Me.ExpandCollapseButton(False, script))
        Else
            header.Controls.Add(Me.ExpandCollapseButton(True, script))
        End If

        'add the 'virtual' header row (with the rolled up price)
        Dim lbl As New Label
        lbl.Text = Me.ShortName(buyeraccount.Language)



        'second m.path is the div to scroll to top
        header.Attributes("onclick") = "burstBubble(event);if(!ajaxing){setQuoteCursor(" & Me.ID & ",'');getBranches('cmd=open&path=" & Me.Path & "&to=" & Me.Path & "&into=tree&Paradigm=C');return(false);}"
        'was on mousedown -= bad (runs 2 threads)
        header.Attributes("class") &= " systemHeader"

        lbl.Font.Bold = True
        header.Controls.Add(lbl)
        If Not (Agentaccount.SellerChannel.marginMin = 0 And Agentaccount.SellerChannel.marginMax = 0) Then
            Dim br As New Literal
            br.Text = "<br/>"
            header.Controls.Add(br)
            header.Controls.Add(Me.MarginUI(True, Me.quote.Locked))
        End If

        Dim tot As NullablePrice = New NullablePrice(CDec(0), buyeraccount.Currency, False)
        Dim rr As Decimal
        Me.Totalise(tot, rr, True)  'the act of totalising may make this include a list price element

        tot.value -= rr 'Subtract the rebate in the sysytem header too

        Dim sysTot As Panel = New Panel

        sysTot.CssClass = "h_sysTotal"
        Dim totPnl As Panel = tot.DisplayPrice(buyeraccount, errorMessages)
        sysTot.Controls.Add(totPnl)
        header.Controls.Add(sysTot)

        'MULTIPLIER
        Dim MultiPanel As Panel = New Panel
        MultiPanel.CssClass = "multiDiv"
        header.Controls.Add(MultiPanel)

        Dim Mlabel As Literal = NewLit("<span class='multiLabel'>" & Xlt("Multiplier", Agentaccount.Language) & "</span>")
        MultiPanel.Controls.Add(Mlabel)

        Dim multiFlex As Panel = New Panel
        multiFlex.CssClass = "multiFlex"

        'In quote Quantity Flex Buttons - also calls validate for every item (which is expensive)
        If Me.Branch.Product.hasSKU Then
            Dim allowflexup As Boolean = True ' Me.Branch.Product.inFeed(buyeraccount.SellerChannel)
            If Me.Branch.Product.isFIO Then allowflexup = False 'Factory Installed Options cannot be flexed 
            multiFlex.Controls.Add(Me.FlexButtons(allowflexup))
        End If

        multiFlex.Controls.Add(Me.QuantityBox(buyeraccount, Agentaccount.Language, errorMessages))

        MultiPanel.Controls.Add(multiFlex)

        '//END of Multiplier

        If Me.collapsed Then
            header.Controls.Add(Me.MessageCounts({enumValidationMessageType.Validation}, {}, {EnumValidationSeverity.DoesQualify, EnumValidationSeverity.DoesntQualify}, False))
        End If

        If Me.ID = 0 Then
            errorMessages.Add("QuoteItemID was 0")
        End If
        Dim marginsAvailable As List(Of String) = New List(Of String)
        CheckMargin(Me, marginsAvailable)
        If marginsAvailable.Count > 0 Then
            'your (price before margin)
            Dim lit As Literal = New Literal
            Dim baseprice As NullablePrice = New NullablePrice(0, Me.quote.Currency, False)
            baseprice.isValid = True
            Me.Totalise(baseprice, 0, False)
            lit.Text = String.Format("<div class='basePrice' title='{0}'>{1}</div>", Xlt("Price before margin", Agentaccount.Language), Xlt("Base price:" & baseprice.text(buyeraccount, errorMessages), buyeraccount.Language))
            header.Controls.Add(lit)
        End If

        Return header

    End Function

    Private Function QuoteItemTabs() As Panel

        'OBSOLETED (before it ever saw the light of day) - surplanted by panels - 

        Dim tabstrip As Panel
        tabstrip = New Panel

        tabstrip.ID = "quoteBasketTabStrip"

        'tabstrip.Controls.Add(MakeTab("Breakdown", viewTypeEnum.Breakdown))
        'tabstrip.Controls.Add(MakeTab("Summary", viewTypeEnum.Summary))
        'tabstrip.Controls.Add(MakeTab("Validation", viewTypeEnum.Validation))

        Return tabstrip

    End Function

    Public Function ShortName(language As clsLanguage, Optional forQuotes As Boolean = False) As String
        'Gives the familyname for a system unit

        If Me.Branch.Product.i_Attributes_Code.ContainsKey("FamMajor") Then
            If {"NBK", "SVR", "DTO", "SWD", "HPN"}.Contains(Me.Branch.Product.ProductType.Code) Then 'need to change this for a flag
                'new for greg
                'changes added for overlapping text in quotes
                If forQuotes Then
                    ShortName = Me.Branch.Parent.Translation.text(language) '
                Else
                    ShortName = Me.Branch.Parent.Translation.text(language) ' Product.i_Attributes_Code("FamMajor")(0).displayName(English)
                    ShortName &= " " & Me.Branch.Product.ProductType.Translation.text(language)
                End If
            Else
                ShortName = Me.Branch.Product.i_Attributes_Code("FamMajor")(0).displayName(language)
            End If
        Else
            'Lets mix this up a little, this was far to simple and neat... so we now go and look for any slots on the product and use the minor translation if its available, specifically for Hardware Kit as this just repeats in the basket
            Dim st = Me.Branch.slots.Where(Function(s) s.Value.Type.MajorCode.ToLower = Me.Branch.Product.ProductType.Code.ToLower).FirstOrDefault
            If st.Value IsNot Nothing AndAlso st.Value.NonStrictType.TranslationShort IsNot Nothing Then
                ShortName = st.Value.NonStrictType.TranslationShort.text(language)
            Else
                ShortName = Me.Branch.Product.ProductType.Translation.text(language)
            End If
        End If

    End Function


    Private Function ExpandCollapseButton(open As Boolean, ByRef script As String) As Panel
        Dim Btn As Panel = New Panel ' - new panel 'expandCollapsebutton As WebControls.Image = New WebControls.Image
        Btn.CssClass = "expandContract"

        If open Then
            'do not 'ShowInTree' when collapsing (Basecamp 054 bascamp 54)
            script = "burstBubble(event);if(!ajaxing){setQuoteCursor(" & Me.ID & ",'collapse');};return false;"
        Else
            Btn.CssClass &= " collapsed"
            script = "burstBubble(event);if(!ajaxing){setQuoteCursor(" & Me.ID & ",'expand');getBranches('cmd=open&path=" & Me.Path & "&into=tree&Paradigm=C')}"

        End If

        Btn.Attributes("onclick") = script$

        Return Btn
    End Function


    Private Function AddNoteButton() As Image

        AddNoteButton = New Image
        With AddNoteButton
            .ImageUrl = "../images/navigation/pencil.png"
            .CssClass = "quoteAddNote quoteTreeButton"
            .ToolTip = Xlt("Add a note", Me.quote.BuyerAccount.Language)
            If Me.Note.value Is DBNull.Value Then
                .Attributes("onclick") = "burstBubble(event);addNote(" & Me.ID & ");"
            End If
        End With

    End Function


    Public Sub New(quote As clsQuote)

        'This constructor is used to create the root quoteitem only - which just holds a set of children - the top level items (typically systems) in a quote - but also 'loose ' parts
        Me.quote = quote

        Me.ID = -99  'NB: the 'virtual' root item never exits on disk - first level nodes have a 
        Me.Parent = Nothing
        Me.Children = New List(Of clsQuoteItem)
        Me.Quantity = 1 'This is important (as all derived quanities are multiplied by it !)
        Me.BasePrice = New NullablePrice(quote.Currency)
        Me.validate = True
        Me.Msgs = New List(Of ClsValidationMessage) 'String 'error (or other) message against this quote line item
        Me.Note = New nullableString()
        Me.Created = Now
        Me.ExpandedPanels = New HashSet(Of panelEnum)   'IMPORTANT (that the root item with options 'open' 'NB Viewtype uses the viewType enum BITWISE  (to compbile multiple states)
        Me.Margin = 1 'default margin for the root item to 1
        Me.order = 0

    End Sub


    Public Sub New(ID As Integer, ByVal quote As clsQuote, ByVal Branch As clsBranch, SKUVariant As clsVariant, ByVal path$, _
                   ByVal Quantity As Integer, ByVal basePrice As NullablePrice, listprice As NullablePrice, ByVal isPreInstalled As Boolean, ByRef parent As clsQuoteItem, Opg As nullableString, bundle As nullableString, rebate As Decimal, created As DateTime, margin As Single, note As nullableString, order As Integer, validate As Boolean)

        'this constructor is used when recreating a record from the database 

        Me.ID = ID
        Me.quote = quote
        Me.Branch = Branch

        Me.SKUVariant = SKUVariant
        Me.Path$ = path$
        Me.Quantity = Quantity
        Me.BasePrice = basePrice  'this isn't a good name - becuase it's BEFORE margin (so wasn't actually the price quoted)
        Me.ListPrice = listprice
        Me.Bundle = bundle
        Me.Margin = margin  'TODO
        Me.Note = note

        Me.OPG = Opg
        Me.rebate = rebate
        Me.Created = created

        'Me.quote.Items.Add(Me.ID, Me) 'add to the flat list
        Me.IsPreInstalled = isPreInstalled

        Me.Parent = parent
        If Not Me.Parent Is Nothing Then
            'If Me.Parent.Children Is Nothing Then Me.Parent.Children = New List(Of clsquoteItem)
            Me.Parent.Children.Add(Me)
        End If
        Me.Children = New List(Of clsQuoteItem)

        Me.quote.UpdateDescAndPrice()

        Me.validate = validate

        Me.Msgs = New List(Of ClsValidationMessage) 'String 'error (or other) message against this quote line item
        Me.Note = note

        Me.ExpandedPanels = New HashSet(Of panelEnum)
        Me.ExpandedPanels.Add(panelEnum.Options)
        Me.order = order

    End Sub

    Public Sub New(ByVal quote As clsQuote, ByVal Branch As clsBranch, SKUvariant As clsVariant, ByVal path$, ByVal Quantity As Integer, ByVal basePrice As NullablePrice, ByVal listprice As NullablePrice, _
                   ByVal isPreInstalled As Boolean, ByRef parent As clsQuoteItem, Opg As nullableString, bundle As nullableString, rebate As Decimal, margin As Single, note As nullableString, order As Integer, Optional writecache As DataTable = Nothing, Optional importID As Integer = 0)

        'Note - Bundle, OPG and Margin are nullable and/or have a default value

        'Top level items sit under the virtual root item (which doesnt exist in the database) - their parent pointers are null
        'Having a real root item might be neater - but we'd have to hide it - and it would have no branch or product

        Me.ImportId = importID

        ' If quote.ID > 3000 And quote.ID < 3005 Then Stop 'TODO remove

        Me.Created = Now

        Me.quote = quote
        Me.Branch = Branch
        Me.SKUVariant = SKUvariant

        Me.Path$ = path$
        Me.Quantity = Quantity
        Me.BasePrice = basePrice
        Me.ListPrice = listprice
        Me.Bundle = Nothing
        Me.Margin = margin
        Me.Note = New nullableString

        Me.OPG = Opg
        Me.Bundle = bundle
        Me.rebate = rebate
        '  Me.Created = Now

        Me.order = order

        'Me.quote.Items.Add(Me.ID, Me) 'add to the flat list
        Me.IsPreInstalled = isPreInstalled

        Me.Parent = parent
        If Not Me.Parent Is Nothing Then
            'If Me.Parent.Children Is Nothing Then Me.Parent.Children = New List(Of clsquoteItem)
            If Me.Parent Is Me.quote.RootItem Then
                Me.Parent.Children.Insert(0, Me)
            Else
                Me.Parent.Children.Add(Me)
            End If
        End If

        Me.Children = New List(Of clsQuoteItem)

        Me.validate = True
        Me.Msgs = New List(Of ClsValidationMessage) 'String 'error (or other) message against this quote line item

        Me.ExpandedPanels = New HashSet(Of panelEnum)
        '   ExpandedPanels.Add(panelEnum.Spec) - greg/dan didnt want this open by default 20/07/2014



        If writecache Is Nothing Then
            Pmark("New QuoteItem (INSERT)")

            Me.ID = SQLInsert()


            Pacc("New QuoteItem (INSERT)")

        Else

            Pmark("New QuoteItem (Writecache)")

            Me.ID = -1  'they will get their true ID's next time they're loaded
            Dim row As System.Data.DataRow
            row = writecache.NewRow()
            row("FK_Quote_id") = quote.ID
            row("FK_Branch_id") = Branch.ID
            row("path") = path$
            row("Quantity") = Quantity
            row("price") = basePrice.value
            row("listprice") = listprice.value
            row("fk_variant_id") = SKUvariant.ID
            row("created") = Created
            row("IsPreinstalled") = isPreInstalled
            row("Margin") = margin
            row("rebate") = rebate
            row("fk_import_id") = importID 'todo !
            row("opg") = Opg.value
            row("bundle") = bundle.value
            row("validate") = True

            row("note") = note.value
            row("order") = Me.order

            If parent Is Nothing Then
                row("fk_quoteItem_id_parent") = DBNull.Value
            Else
                row("fk_quoteItem_id_parent") = parent.ID
            End If

            writecache.Rows.Add(row)

            Pacc("New QuoteItem (Writecache)")

        End If



    End Sub

    ''' <summary>Writes the quote Item(s) to the database</summary>
    ''' <remarks></remarks>
    Public Sub updateRecursive()

        If Not Me Is Me.quote.RootItem Then 'the root item is 'virtual' (not on disk) and need not be updated (top level items in the quote have no parent)
            Me.Update()
        End If

        For Each child In Me.Children
            child.Update()
        Next

    End Sub

    ''' <summary>Populates the dictionary of LoyaltyScheme>Points - and recurses for all child Items</summary>
    ''' <remarks>Fills this Dictionary - giving total points per loyalty scheme for the entire 'basket' (accouting for Item quantities)</remarks>
    Public Sub LoyaltyPoints(ByRef Dic As Dictionary(Of clsScheme, Integer))

        If Me.Branch IsNot Nothing Then  '(the root quoteItem is a placeholder and has no branch/product)

            If Not Me.IsPreInstalled Then   ' Don't include Pre-installed items in Loyalty Scheme calculations

                For Each scheme In Me.Branch.Product.Points.Keys
                    If scheme.Region.Encompasses(Me.quote.AgentAccount.SellerChannel.Region) Then  'Is the agent (who we presume is the one earning points) in this schemes' region ? 
                        If Now > scheme.StartDate And Now < scheme.EndDate Then 'is the scheme active
                            Dim dq As Integer = Me.DerivedQuantity
                            If Dic.ContainsKey(scheme) Then
                                Dic(scheme) += dq * Me.Branch.Product.Points(scheme)
                            Else
                                Dic.Add(scheme, dq * Me.Branch.Product.Points(scheme))
                            End If
                        End If
                    End If
                Next

            End If
        End If


        For Each child In Me.Children
            child.LoyaltyPoints(Dic)
        Next

    End Sub

    Public Sub Update(Optional updatePrice As Boolean = True)
        If updatePrice Then
            Me.quote.UpdateDescAndPrice()
        End If
        Me.SQLUpdate()


        ' Return Me

    End Sub

    Private Function MarginUI(propagate As Boolean, quoteLocked As Boolean) As Panel

        'hello

        'returns a margin button/indicator of the current margin per quote item
        'propagate determines wether all child items will inherrit this margin is it's applied
        'System headers propagate margins to all chlidren (sytems and options)
        'but systems themslves are 'divorced' from their otions

        MarginUI = New Panel
        MarginUI.CssClass = "marginHolder"

        'A unique ID is needed for the two versions of the system unit (The header which propagates and the actual SU which does not)
        Dim uid As String = Me.ID.ToString & IIf(propagate, "p", "").ToString

        Dim isRetainedMargin As Boolean = True
        Dim showbutton As Panel = New Panel
        Dim lit As Literal = New Literal
        lit.Text = "%"
        Dim mp As Double 'margin as a percentage
        mp = (Me.Margin * 100) - 100




        'see bug 1094
        'If mp <> 0 Then

        '    Dim e As Double = Math.Round(mp * 1000) - (mp * 1000)
        '    If Math.Abs(e) < 0.01 Then
        '        'its an 'exact' margin (so it's cost plus)
        '        mp = Math.Round(mp * 1000) / 1000

        '        lit.Text = mp.ToString & "%"
        '        isRetainedMargin = False
        '    Else
        'it's some strange precentage (1.02038503) - so it's 'retained'


        'need to add a retained/costplus property to the channel
        'The above was not robust (at all!) .. 20% RM is *exactly* 25% costplus see bug 1094

        mp = 1 - 1 / Me.Margin 'work out what the RM was (although there will be a v.small rounding error)
        mp = Math.Round(mp * 1000) / 10 'round to the nearest 1000'th of a % (and multiply by 100)
        lit.Text = mp.ToString & "%"  ' ("0.0") & "%"
        '     End If
        '  End If

        '        If Me.Margin <> 1 Then
        showbutton.CssClass = "textButton addMarginButton"
        showbutton.ID = "amb" & uid
        If Not quoteLocked Then
            showbutton.Attributes("onclick") = "burstBubble(event);display('" & showbutton.ID & "','none');display('mg" & uid & "','block');"
        End If
        showbutton.Controls.Add(lit)

        MarginUI.Controls.Add(showbutton)

        Dim marginGuts As Panel = New Panel
        MarginUI.Controls.Add(marginGuts)
        marginGuts.ID = "mg" & uid
        marginGuts.Attributes("style") &= " display:none"

        ''Insert the (initially hidden) UI for the margin  

        Dim tb As TextBox = New TextBox
        tb.Text = mp.ToString
        tb.MaxLength = 5
        tb.ID = "mv" & uid
        tb.CssClass = "marginTextBox"
        tb.Attributes("onclick") = "burstBubble(event);"
        tb.Attributes("onkeydown") = "var e=event||window.event; var keyCode=e.keyCode||e.which; if (keyCode==13){return false;}"


        marginGuts.Controls.Add(tb)

        Dim ms As Literal = New Literal
        ms.Text = "<select class='cprt' id='mt" & uid & "' onclick='burstBubble(event);'>"
        ms.Text &= "<option value='R'" & IIf(isRetainedMargin, "selected", "").ToString & ">Retained margin</option>" 'eg * 1/.98
        ms.Text &= "<option value='C'" & IIf(isRetainedMargin, "", "selected").ToString & ">Cost Plus</option>" 'eg *1.02
        ms.Text &= "</select>"
        marginGuts.Controls.Add(ms)

        Dim dib As Literal = New Literal 'Do It Button
        marginGuts.Controls.Add(dib)
        dib.Text = "<div id='applyMarginButton' class='amb textButton' onclick='burstBubble(event);"
        dib.Text &= "var ddl=document.getElementById(|mt" & uid & "|);"  'Marging Type (retained/cost plus)
        dib.Text &= "var txt=document.getElementById(|mv" & uid & "|); if (marginValue(txt.value)) {  "
        dib.Text &= "saveNote(false);var url;url=|quote.aspx?cmd=margin=|+txt.value +|=|+ ddl.value+|&quoteCursor=" & Me.ID & "&propagate=" & IIf(propagate, "1", "0").ToString & "|;"
        dib.Text &= "rExec(url, displayQuote);} else { $(""#errmsg"").show(); } '>Apply</div><div id 'errmsg' style='display: none;' >Please enter a numeric value between -20 to 40</div>"
        dib.Text = dib.Text.Replace("|", Chr(34)) 'to use quotes in script . . .
        'End If

    End Function


    Public Function ValidationPanel(buyeraccount As clsAccount, agentaccount As clsAccount, ByRef ErrorMessages As List(Of String)) As Panel

        'dicslots is  by 'minor' type - slot types is the list of 'categories' we're validating by W,MEM,HDD etc
        ValidationPanel = New Panel
        ValidationPanel.CssClass = "panelOuter"

        Dim vHeader As Panel = New Panel
        vHeader.CssClass = "panelHeader"
        ValidationPanel.Controls.Add(vHeader)

        Dim script As String = "" 'passed byref and POPULATES the script (which goes onto the WHOLE panel (not just the button)
        vHeader.Controls.Add(Me.PanelButton(panelEnum.Validation, Me.ExpandedPanels.Contains(panelEnum.Validation), script$))
        vHeader.Attributes("onclick") = script$

        Dim title As Literal = New Literal
        title.Text = "<div class='panelTitle'>" & Xlt("Validation", agentaccount.Language) & "</div>"
        vHeader.Controls.Add(title)


        Dim critOutstanding As Boolean = True
        Dim vdRollup As Panel = New Panel
        vdRollup.CssClass = "validationRollup"
        vHeader.Controls.Add(Me.MessageCounts({enumValidationMessageType.Validation}, {}, {EnumValidationSeverity.DoesQualify, EnumValidationSeverity.DoesntQualify}, False, critOutstanding)) 'exclude felx qualification messages from the roll up

        If Not critOutstanding Then
            vHeader.Controls.AddAt(2, NewLit("<img class='headerValidationIcon'  src='/images/navigation/ICON_CIRCLE_tick.png'><span class='iconX'></span>"))
        End If

        Dim lblSpace As Label = New Label
        lblSpace.Text = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"
        vHeader.Controls.Add(lblSpace)

        If Me.AllChildMsgs.Where(Function(df) df.type = enumValidationMessageType.Upsell).Count > 0 Then
            vHeader.Controls.Add(NewLit("<img class='headerUpsellIcon'  src='/images/navigation/ICON_IQ2_UPSELL.png'><span class='iconX'></span>"))
        End If

        If Me.ExpandedPanels.Contains(panelEnum.Validation) Then
            'Output ALL validation messsages - EXCEPT flex qualification - which greg wants in the systemfooter
            Dim count As Integer
            ValidationPanel.Controls.Add(Me.outputValidations({enumValidationMessageType.Validation}, {}, {EnumValidationSeverity.DoesntQualify, EnumValidationSeverity.DoesQualify}, buyeraccount, agentaccount, ErrorMessages, count))

        End If

    End Function

    Public Function outputValidations(onlytype As enumValidationMessageType(), only As EnumValidationSeverity(), Except As EnumValidationSeverity(), buyeraccount As clsAccount, agentaccount As clsAccount, ByRef errorMessages As List(Of String), ByRef count As Integer) As PlaceHolder

        'Returns a placeholder filled with (some of) the validation messages associated with the quote item 
        Dim ph As PlaceHolder = New PlaceHolder

        If Me.AllChildMsgs.Where(Function(df) df.type = enumValidationMessageType.Upsell).Count > 0 And Not onlytype.Contains(enumValidationMessageType.Flex) Then
            ph.Controls.Add(New ClsValidationMessage(enumValidationMessageType.UpsellHolder, EnumValidationSeverity.Upsell, Nothing, iq.AddTranslation("Upsell Opportunities Available", English, "", 0, Nothing, 0, False), "", 0, 0, {}).UI(buyeraccount, agentaccount.Language, errorMessages, Me.quote.ID))
            count += 1
        End If

        For Each vm As ClsValidationMessage In Me.AllChildMsgs.Distinct()
            If vm IsNot Nothing AndAlso Not Except.Contains(vm.severity) AndAlso (only.Length = 0 Or only.Contains(vm.severity)) AndAlso (onlytype.Contains(vm.type) Or onlytype.Length = 0) Then  'TODO remove - some 'nothings' are getting in
                ph.Controls.Add(vm.UI(buyeraccount, agentaccount.Language, errorMessages, Me.quote.ID))
                count += 1
            End If
        Next

        Return ph

    End Function
    Public ReadOnly Property AllChildMsgs As List(Of ClsValidationMessage)
        Get
            Return Me.Msgs.Union(Me.Children.SelectMany(Function(ch) ch.AllChildMsgs)).ToList()
        End Get
    End Property
    Private Function Initials(phrase As String) As String

        Initials = ""
        For Each w In phrase.Split(" ".ToArray)
            Initials &= w.First
        Next

    End Function

    Public Function PromosRollup(dicPoints As Dictionary(Of clsScheme, Integer), language As clsLanguage) As Panel

        'will also want total rebate

        PromosRollup = New Panel
        PromosRollup.CssClass = "promosRollup"

        For Each scheme In dicPoints.Keys
            Dim lit As New Literal
            lit.Text = "<div class = promoRollUpItem>" & Initials(scheme.displayName(language)) & ":" & dicPoints(scheme) & "</div>"
            PromosRollup.Controls.Add(lit)
        Next


        ''group and count points by scheme to make a compact header
        'Dim counts As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)
        'For Each m In Me.Msgs
        '    If Not counts.ContainsKey(m.imagename) Then
        '        counts.Add(m.imagename, 1)
        '    Else
        '        counts(m.imagename) += 1
        '    End If
        'Next

        'For Each j In counts.Keys
        '    Dim i As WebControls.Image = New WebControls.Image
        '    i.ImageUrl = "/images/navigation/" & j
        '    i.CssClass = "headerValidationIcon"
        '    MessageCounts.Controls.Add(i)
        '    Dim lit As Literal = New Literal
        '    lit.Text = "<span class='iconX'>x" & counts(j).ToString.Trim & "</span>"
        '    MessageCounts.Controls.Add(lit)
        'Next

    End Function


    Public Function MessageCounts(includetype As enumValidationMessageType(), include As EnumValidationSeverity(), exclude As EnumValidationSeverity(), allSystems As Boolean, Optional ByRef critOutstanding As Boolean = True) As Panel

        MessageCounts = New Panel
        MessageCounts.CssClass = "validationRollup"

        Dim combinedValidations = If(allSystems, Me.quote.RootItem.Children.SelectMany(Function(sys) sys.Msgs).ToList(), Me.Msgs.ToList).Where(Function(ch) (include.Length = 0 Or include.Contains(ch.severity)) AndAlso (exclude.Length = 0 Or Not exclude.Contains(ch.severity)) AndAlso (includetype.Length = 0 Or includetype.Contains(ch.type)))

        For Each p In combinedValidations.OrderByDescending(Function(val) val.severity).GroupBy(Function(sm) sm.imagename).Select(Function(msg) NewLit("<img class='headerValidationIcon' src='/images/navigation/" & msg.Key & "' style='border-width:0px;'><span class='iconX'>x" & msg.Count.ToString & "</span>"))
            MessageCounts.Controls.Add(p)
        Next

        If combinedValidations.Count = 0 OrElse combinedValidations.Max(Function(f) f.severity) <= EnumValidationSeverity.BlueInfo Then
            critOutstanding = False
        End If


        Return MessageCounts

        ''ground and count each type of warning (by image name)
        'Dim counts As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)

        'Dim worsterror As Integer = 0


        'If allSystems Then  'Ugly - but this whole thing got compromised by the 'two level' approach (messages are not on their items anymore :o( )
        '    For Each System As clsQuoteItem In Me.quote.RootItem.Children
        '        For Each m In From r In System.Msgs Where r IsNot Nothing
        '            If m.severity <> exclude And m.severity <> exclude2 Then
        '                If m.severity > worsterror Then worsterror = m.severity
        '                If Not counts.ContainsKey(m.imagename) Then
        '                    counts.Add(m.imagename, 1)
        '                Else
        '                    counts(m.imagename) += 1
        '                End If
        '            End If
        '        Next
        '    Next
        'Else
        '    For Each m In From r In Me.Msgs Where r IsNot Nothing
        '        If m.severity <> exclude And m.severity <> exclude2 Then
        '            If m.severity > worsterror Then worsterror = m.severity
        '            If Not counts.ContainsKey(m.imagename) Then
        '                counts.Add(m.imagename, 1)
        '            Else
        '                counts(m.imagename) += 1
        '            End If
        '        End If
        '    Next
        'End If

        'For Each j In counts.Keys
        '    Dim i As WebControls.Image = New WebControls.Image
        '    i.ImageUrl = "/images/navigation/" & j
        '    i.CssClass = "headerValidationIcon"
        '    MessageCounts.Controls.Add(i)
        '    Dim lit As Literal = New Literal
        '    lit.Text = "<span class='iconX'>x" & counts(j).ToString.Trim & "</span>"
        '    MessageCounts.Controls.Add(lit)
        'Next

        ''Last part of the code executed if there are no errors and shows green tick 
        'If worsterror <= EnumValidationSeverity.BlueInfo Then
        '    Dim i As WebControls.Image = New WebControls.Image
        '    i.ImageUrl = "/images/navigation/ICON_CIRCLE_tick.png" 'bodge
        '    i.CssClass = "headerValidationIcon"
        '    MessageCounts.Controls.Add(i)
        '    Dim lit As Literal = New Literal
        '    lit.Text = "<span class='iconX'></span>"
        '    MessageCounts.Controls.Add(lit)
        'End If

    End Function

    Public Function PanelButton(paneltype As panelEnum, open As Boolean, ByRef script As String) As Panel

        Dim Btn As Panel = New Panel ' - new panel 'expandCollapsebutton As WebControls.Image = New WebControls.Image
        Btn.CssClass = "expandContract"

        'With (expandCollapsebutton)
        ' .CssClass = "panelButton quoteTreeButton"
        If open Then
            '.ImageUrl = "../images/navigation/minus.png"
            script = "burstBubble(event);if(!ajaxing){setQuoteCursor(" & Me.ID & ",'closePanel=" & paneltype & "');}"
            '.ToolTip = "Click to collapse this panel"
        Else
            Btn.CssClass &= " collapsed"
            '.ImageUrl = "../images/navigation/plus.png"
            script = "burstBubble(event);if(!ajaxing){setQuoteCursor(" & Me.ID & ",'openPanel=" & paneltype & "');}"
            '.ToolTip = "click to expand this panel"
            'End If
        End If

        Return Btn

    End Function

    Public Function SQLInsert(Optional BootStrap As Boolean = False) As Integer Implements ISqlBasedObject.SQLInsert
        Dim parentid As String
        If Parent Is Nothing Then
            parentid = "null"
        Else
            If Parent Is quote.RootItem Or Parent.Branch Is Nothing Then 'the virtual root has no branch - wh need this check when cloning it (to make a new quote)
                parentid = "null"
            Else
                parentid = Parent.ID.ToString
            End If
        End If

        Dim Sql$
        Sql$ = "INSERT INTO QuoteItem (fk_quote_id,fk_branch_id,path,quantity,price,listprice,fk_variant_id,created,ispreinstalled,fk_quoteitem_id_parent,margin,fk_import_id,opg,bundle,note,[order]) "
        Sql$ &= " values (" & quote.ID & "," & Branch.ID & ",'" & Path$ & "'," & Quantity & "," & BasePrice.sqlvalue & "," & ListPrice.sqlvalue & "," & SKUVariant.ID & ","
        Sql$ &= da.UniversalDate(Created) & "," & CStr(IIf(IsPreInstalled, "1", "0")) & "," & parentid & "," & Margin & "," & Me.ImportId & "," & OPG.sqlValue & "," & Bundle.sqlValue & "," & Note.sqlValue & "," & order.ToString & ");"

        SQLInsert = da.DBExecutesql(Sql$, True, Me)
    End Function
    Public Sub SQLUpdate() Implements ISqlBasedObject.SQLUpdate
        Dim sql As New StringBuilder(String.Empty)

        If Me.Quantity > 0 Then
            sql.Append(String.Format("{0}{1}", "UPDATE QuoteItem SET Quantity = ", Me.Quantity))
            sql.Append(String.Format("{0}{1}", ",price = ", Me.BasePrice.sqlvalue))
            sql.Append(String.Format("{0}{1}", ",listPrice = ", Me.ListPrice.sqlvalue))
            sql.Append(String.Format("{0}{1}", ",OPG = ", OPG.sqlValue))
            sql.Append(String.Format("{0}{1}", ",margin = ", Me.Margin))
            sql.Append(String.Format("{0}{1}", ",bundle = ", Me.Bundle.sqlValue))
            sql.Append(String.Format("{0}{1}", ",validate = ", CStr(IIf(Me.validate, "1", "0"))))
            sql.Append(String.Format("{0}{1}", ",note = ", Note.sqlValue))
            sql.Append(String.Format("{0}{1}", ",Rebate = ", Me.rebate))
            sql.Append(String.Format("{0}{1}", " WHERE ID = ", Me.ID))

            Try
                da.DBExecutesql(sql.ToString, False, Me)
            Catch ex As System.Exception
                Throw New Exception("***" & ex.Message & "SQL was:" & sql.ToString)
            End Try

        Else

            ' SK - updates wrapped in a single DB transaction to protect against multiple UI thread deadlocks
            sql.Append("BEGIN TRAN;")
            sql.Append(String.Format("DELETE FROM QuoteItem WHERE fk_quoteitem_id_parent = {0};", Me.ID)) 'delete any children first (including Chassis!)
            sql.Append(String.Format("DELETE FROM QuoteItem WHERE ID = {0};", Me.ID))
            sql.Append("COMMIT TRAN;")

            da.DBExecutesql(sql.ToString, False, Me)

            If Me.quote.Cursor Is Me Then
                Me.quote.Cursor = quote.RootItem 'fix for bug 721 (adding options to a basket you have removed the system from causes a crash)
            End If

            Me.Parent.Children.Remove(Me) 'THIS IS REALLY IMPORTANT

        End If

    End Sub
    Public Sub UpdateSelfAfterIdChange(NewId As Integer) Implements ISqlBasedObject.UpdateSelfAfterIdChange
        'Update OM
        'no need for quote item...
        Me.ID = NewId

    End Sub

    'Private Sub addToBasket_Click(sender As Object, e As EventArgs)
    '    If quote IsNot Nothing Then
    '        Dim url As String = iq.sesh(Me., "GK_BasketURL")
    '        If url.Length > 0 Then

    '            Dim req As HttpWebRequest = WebRequest.Create(New Uri(url))
    '            req.ContentType = "text/xml; charset=utf-8"
    '            req.Method = "POST"
    '            req.Accept = "text/xml"

    '            'Generate the xml using the proxy class
    '            Dim dt As Data = New Data()
    '            dt.Quote.ID = quote.ID
    '            dt.Quote.Name = quote.Name.ToString
    '            dt.Quote.CreatedBy = quote.AgentAccount.User.RealName
    '            dt.Quote.Supplier = quote.AgentAccount.SellerChannel.Name
    '            dt.Quote.URLProductImage = quote.RootItem.Note.value 'need to ask nick abt this
    '            Dim products As List(Of DataQuoteProduct) = New List(Of DataQuoteProduct)
    '            Dim product As DataQuoteProduct
    '            For Each flatListItem In quote.RootItem.Flattened(True, False, 0).items
    '                product = New DataQuoteProduct()
    '                product.Class = flatListItem.QuoteItem.Branch.Product.ProductType.Code
    '                product.PartNum = "" 'flatListItem.QuoteItem.Branch.Product.i_Attributes_Code("MfrSKU").
    '                product.SupplierPartNum = flatListItem.QuoteItem.SKUVariant.DistiSku

    '                product.ListPrice = flatListItem.QuoteItem.ListPrice.value
    '                product.Description = flatListItem.QuoteItem.Branch.DisplayName(quote.BuyerAccount.Language)
    '                product.Qty = flatListItem.QuoteItem.Quantity
    '                product.URLProductImage = flatListItem.QuoteItem.Branch.Picture
    '                ' product.URLProductSpecs =
    '                products.Add(product)

    '            Next
    '            dt.Quote.Product = products.ToArray()


    '            Dim xmlString As String = SerializeToString(dt)
    '            iq.sesh(lid, "basketContent") = xmlString

    '            Dim trueUri As Uri = New Uri(Request.Url.AbsoluteUri)
    '            Dim uri As String = trueUri.Scheme + "://"
    '            uri = uri & Request.Url.Host.ToString()
    '            uri = uri & Page.ResolveUrl("~/BasketPost.aspx?lid=" & lid)

    '            Response.Redirect(uri)

    '        End If
    '    End If
    'End Sub


    'Private Sub FindValidation(ByRef selectedMsgs As List(Of ClsValidationMessage), root As clsQuoteItem)
    '    If root IsNot Nothing Then
    '        For Each msg In root.Msgs
    '            If msg.severity <= EnumValidationSeverity.RedCross Then
    '                selectedMsgs.Add(msg)
    '            End If
    '        Next
    '        For Each child In root.Children
    '            FindValidation(selectedMsgs, child)
    '        Next
    '    End If


    Public Function ValidationsGreaterThanEqualTo(vmin As EnumValidationSeverity) As List(Of ClsValidationMessage)

        ValidationsGreaterThanEqualTo = New List(Of ClsValidationMessage)

10:
        For Each msg In Me.Msgs
15:
            If msg.severity >= vmin Then
                ValidationsGreaterThanEqualTo.Add(msg)
17:
            End If
        Next
20:
        For Each child In Me.Children
30:
            ValidationsGreaterThanEqualTo.AddRange(child.ValidationsGreaterThanEqualTo(vmin))
        Next


    End Function


    Private Sub CheckMargin(quoteItem As clsQuoteItem, ByRef margin As List(Of String))
        If quoteItem.Margin <> 1 And quoteItem.IsPreInstalled = False Then
            margin.Add(quoteItem.ID.ToString())
        Else
            For Each child In quoteItem.Children
                CheckMargin(child, margin)

            Next
        End If

    End Sub

    Private Sub getAllproductTypes(ByRef alltypes As String, flexOPGID As Integer)

        Dim sysFlexLine = (From l In Me.Branch.Product.OPGflexLines.Values Where l.FlexOPG.ID = flexOPGID).ToList()
        If sysFlexLine.Count > 0 Then
            alltypes &= Me.Branch.Product.ProductType.Code & " , "
        Else
            ' If there's a warranty, always include it (even if not set up for flex)
            If Me.Branch.Product.ProductType.Code = "wty" Then
                alltypes &= Me.Branch.Product.ProductType.Code & " , "
            End If
        End If

        For Each child In Me.Children
            child.getAllproductTypes(alltypes, flexOPGID)
        Next

    End Sub
    Public Sub getQuoteVariant(ByRef variants As List(Of clsVariant), Optional includePreinstalled As Boolean = False)
        If Me.SKUVariant IsNot Nothing Then
            variants.Add(Me.SKUVariant)
        End If
        For Each child In Me.Children
            If Not child.IsPreInstalled Or includePreinstalled Then child.getQuoteVariant(variants, includePreinstalled)
        Next
    End Sub

    Function hasRoomFor(qi As clsQuoteItem) As Boolean
        For Each slot In qi.Branch.slots.Values

            If Me.dicslots.ContainsKey(slot.NonStrictType) Then

                'does this slot apply here
                If (String.IsNullOrEmpty(slot.path) OrElse slot.path.Contains(Me.Path)) Then

                    If slot.numSlots < 0 Then

                        'it's a 'takes' slot (occupies slots) - typically an option
                        Dim slotsLeft = Me.dicslots(slot.NonStrictType).Given - Me.dicslots(slot.NonStrictType).taken

                        If slotsLeft <= 0 Then Return False
                    End If
                End If

            End If
        Next
        Return True
    End Function

    Function ShouldShowInBasket(includePreInstalled As Boolean, BuyerAccount As clsAccount, foci As HashSet(Of String)) As Boolean
        'Can you purchase one of me?
        'Ok, so new logic as of 16/03/2015 based on Dan's input on what should or should not show in the basket
        'Rules are
        '1. Honour the is preinstalled flag (not even sure if this is used, maybe flat list?)
        '2. Does this item have any slots, therefore is there any point in showing this so that it can be disabled from validation?
        '3. Always show systems (of course!)
        '4. Its not a fake part (controversial) 
        '5. Can you buy it?  If you can't select a replacement, why show it
        '***ML- I HAVE NOT IMPLEMENTED THIS*** 6. Are there any slots left for it? - debatable logic here I think, the point is surely that you can remove them from validation and MAKE slots available for a replacement 

        If BuyerAccount.HasRight("DIAGVIEW") Then Return True

        'Return Not Me.Branch.Product.ProductType.Code.ToUpper = "EMB" AndAlso Not Me.Branch.Product.isFakePart AndAlso
        '           (((Me.IsPreInstalled OrElse (includePreInstalled And Not Me.IsPreInstalled)) AndAlso Me.Branch.slots.Count > 0) Or (Me.Branch.Product.isSystem(Me.Path) = False And Me.Branch.slots.Count = 0)) AndAlso
        '           (Me.SystemItem.Branch.findChildByProductType(Me.Path, Me.Branch.Product.ProductType, BuyerAccount, foci) IsNot Nothing Or Not Me.IsPreInstalled)

        ' SNK - Conditional logic split up so things are hopefully a little clearer...
        If Me.Branch.Product.ProductType.Code.ToUpper = "EMB" Then Return False
        If Me.Branch.Product.isFakePart Then Return False

        ' Always show systems
        If Me.Branch.Product.isSystem(Me.Path) Then Return True

        Dim slotsOK As Boolean = False


        If Me.Branch.slots.Count = 0 Then
            slotsOK = False
        Else
            slotsOK = Me.IsPreInstalled OrElse (includePreInstalled And Not Me.IsPreInstalled)
        End If


        Dim branch As clsBranch = Me.SystemItem.Branch.findChildByProductType(Me.Path, Me.Branch.Product.ProductType, BuyerAccount, foci)
        Dim branchOK As Boolean = (branch IsNot Nothing Or Not Me.IsPreInstalled)

        Return slotsOK AndAlso branchOK

    End Function
End Class


'End of clsQuoteItem

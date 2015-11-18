Public Class ClsValidationMessage

    Public severity As EnumValidationSeverity
    Public type As enumValidationMessageType
    Public imagename As String
    Public message As clsTranslation
    Public title As clsTranslation
    Public resolutionMessage As clsTranslation
    Public ResolvePath As String 'path to resolving part
    Public ResolvingQty As Integer
    Public ResolverGives As Integer
    Public variables() As String '%1 type variables embedded in the validation messages
    Public slotTypeMajor As String
    Public Acknowledged As Boolean = False
    Public ID As String 'Arbitary unique ID used for acknowledgement syn with the client script
    Public DefaultFilters As Dictionary(Of clsFilter, List(Of String)) 'Used mainly for optimizations
    Private ProductValidation As clsProductValidation

    Public Sub New(type As enumValidationMessageType, severity As EnumValidationSeverity, message As clsTranslation, title As clsTranslation, resolvePath As String, resolvingQty As Integer, Resolvergives As Integer, Variables() As String, Optional slotTypeMajor As String = Nothing, Optional ID As String = "", Optional defFilters As Dictionary(Of clsFilter, List(Of String)) = Nothing, Optional ProdValidation As clsProductValidation = Nothing)

        Me.type = type
        Me.severity = severity
        Me.message = message
        Me.title = title
        Me.resolutionMessage = resolutionMessage
        Select Case severity
            Case Is = EnumValidationSeverity.greenTick
                Me.imagename = "ICON_CIRCLE_tick.png"
            Case Is = EnumValidationSeverity.BlueInfo
                Me.imagename = "ICON_CIRCLE_info.png"
            Case Is = EnumValidationSeverity.Question
                Me.imagename = "ICON_CIRCLE_question.png"
            Case Is = EnumValidationSeverity.Exclamation
                Me.imagename = "ICON_CIRCLE_exclamation.png"
            Case Is = EnumValidationSeverity.amberalert
                Me.imagename = "ICON_CIRCLE_amberAlert.png"
            Case Is = EnumValidationSeverity.RedCross
                Me.imagename = "ICON_CIRCLE_Cross.png"
            Case Is = EnumValidationSeverity.DoesQualify
                ' Me.imagename = "ICON_CIRCLE_doesQualify.png"
                Me.imagename = "ICON_CIRCLE_tick.png"
            Case Is = EnumValidationSeverity.DoesntQualify
                Me.imagename = "ICON_IQ2_FlexFail.png"
                'Me.imagename = "ICON_IQ2_RedAlert.png"
            Case Is = EnumValidationSeverity.Upsell
                Me.imagename = "ICON_IQ2_UPSELL.png"
            Case Else
                Beep()
        End Select

        Me.DefaultFilters = defFilters
        Me.ResolvePath = resolvePath
        Me.ResolvingQty = resolvingQty
        Me.ResolverGives = Resolvergives
        Me.variables = Variables
        Me.slotTypeMajor = slotTypeMajor
        Me.ProductValidation = ProdValidation
        Me.ID = ID

    End Sub

    Public Function CompactUI(language As clsLanguage) As Panel

        CompactUI = New Panel

        CompactUI.Attributes("style") &= "display:inline-block;"

        Dim branch As clsBranch = Nothing
        iq.infoID += 1

        Dim img As Image
        img = New Image
        img.ImageUrl = "/images/navigation/" & Me.imagename
        img.Attributes("onmousedown") = "TagToTip('I_" & Trim$(iq.infoID) & "', TITLE, 'Message', CLICKSTICKY, true, CLICKCLOSE, false, CLOSEBTN, true, COPYCONTENT, false, DELAY, 400, BORDERWIDTH, 1, BORDERCOLOR, '#2F7BD1', PADDING, 2,WIDTH,400);return false;"

        Dim q$ = Chr(34)

        Dim lit As New Literal
        'clicking anwhere on the div will dismiss it - so will hovering and 'leaving'
        lit.Text = "<div id='I_" & Trim$(iq.infoID) & "' style='display:none'>" ' class='infoBox' "
        'lit.Text &= "onmousedown=" & q$ & "display('I_" & iq.infoID & "','none');return false;" & q$
        'lit.Text &= " onMouseLeave=" & q$ & "display('I_" & iq.infoID & "','none');return false;" & q$
        'lit.Text &= ">"

        ' lit.Text &= "<img src=/images/navigation/close.png title='Click anywhere in the box to hide this message' style='float:right'; >"
        lit.Text &= replaceVariables(Me.message.text(language), variables)
        lit.Text &= "</div>"

        CompactUI.Controls.Add(img)
        CompactUI.Controls.Add(lit)


    End Function



    Public Function replaceVariables(l$, v() As String) As String

        For i = 0 To UBound(v)
            l$ = Replace(l$, "%" & Trim(CStr(i + 1)), v(i))
        Next

        Return l$

    End Function

    Public Function UI(buyeraccount As clsAccount, language As clsLanguage, ByRef errorMessages As List(Of String), QuoteId As Int32) As Panel
        'ML - passing QuoteId in for a unique id for validation acknowledgement
        'Dan wanted validation messages rendered as <LI>'s - so this returns a listitem not not a Panel (DIV) yuck

        '   Dim txt$
        UI = New Panel 'laceHolder
        UI.Attributes("class") = "panelMessage"

        iq.infoID = iq.infoID + 1

        Dim li As HtmlGenericControl
        If False Then  'Dan's way
            li = New HtmlGenericControl("LI")
            li.Attributes("class") &= " severity" & Trim$(Me.severity)
        Else
            li = New HtmlGenericControl("DIV")
            Dim i As New Image
            li.Controls.Add(i)
            i.ImageUrl = "/images/navigation/" & Me.imagename
            If Acknowledged Then i.ImageUrl = "/images/navigation/ICON_CIRCLE_tick.png"
        End If

        UI.Controls.Add(li)

        Dim branch As clsBranch = Nothing
        ' Dim desc As clsTranslation
        Dim lbl As Label = New Label
        lbl.Style.Add("padding-left", "2px")
        Dim displaytext As String = ""
        Dim title As String = Nothing
        If Not Me.title Is Nothing Then title = Me.title.text(language).Replace("[mfr]", buyeraccount.mfrCode)
        Dim message As String = Nothing
        If Not Me.message Is Nothing Then message = Me.message.text(language).Replace("[mfr]", buyeraccount.mfrCode)

        If Me.title IsNot Nothing Then
            lbl.Text = replaceVariables(title, Me.variables)
        End If

        If (Me.severity = EnumValidationSeverity.Question Or Me.severity = EnumValidationSeverity.Exclamation Or Me.severity = EnumValidationSeverity.BlueInfo Or Me.severity = EnumValidationSeverity.RedCross Or severity = EnumValidationSeverity.amberalert Or Acknowledged) AndAlso Me.message IsNot Nothing Then
            'Does this require an acceptance, if so add to the popup
            displaytext &= replaceVariables(message, Me.variables)
            If severity = EnumValidationSeverity.amberalert Then
                displaytext &= "<br><br><span class='acknowledge' onclick=""burstBubble(event);acknowledgedvalidation('" & Me.ID & "'," & QuoteId & ");return false;"">" & Xlt("Click to Acknowledge", language) & "</span>"
            End If

        End If
        UI.Attributes("onclick") = "burstBubble(event);return false;"


        If type = enumValidationMessageType.UpsellHolder Then
            UI.Attributes.Add("onclick", " burstBubble(event);$('.upsell').click();return false; ")
            UI.Style.Add("cursor", "pointer")
        End If



        If ResolvePath <> "" And Me.ResolvingQty > 0 Then
            branch = iq.Branches(Split(Me.ResolvePath, ".").Last)

            'lbl.Text &= "&nbsp;" & Me.ResolvingQty & " x " & branch.SKU & " alllows +" & ResolverGives
            'lbl.Text &= "&nbsp;" & Me.ResolvingQty & " x " & branch.Translation.text(English) & " allows +" & ResolverGives
            displaytext &= "<br> " & Xlt("Consider adding", language) & " " & Me.ResolvingQty & " x <span style='text-decoration:underline;cursor:pointer;' onmousedown=""burstBubble(event);getBranches('cmd=open&to=" & ResolvePath & "');"">" & branch.Translation.text(language) & "</span>" & " allowing " & ResolverGives & " more."

            ' desc = branch.Product.i_Attributes_Code("Name")(0).Translation
            lbl.ToolTip = branch.Product.DisplayName(language)
            Dim prices As List(Of clsPrice) = branch.Product.GetPrices(buyeraccount, buyeraccount.SellerChannel.priceConfig, iq.AllVariants, errorMessages, False)
            If prices.Count > 0 AndAlso (From p In prices Where p IsNot Nothing).Any Then
                lbl.ToolTip &= "&nbsp; from " & LowestPrice(prices).Price.text(buyeraccount, errorMessages)
            End If
        End If

        li.Controls.Add(lbl)

        ' Dim img As Image
        ' img = New Image
        ' img.ImageUrl = "/images/navigation/" & Me.imagename


        If Not String.IsNullOrEmpty(displaytext) Then
            Dim script As String = ""
            If ResolvePath <> "" Then
                If Me.ResolvingQty > 0 Then
                    Dim skuvariant As clsVariant = Nothing
                    'problems if the disti does not sell the resolving part so . . 
                    If branch.Product.i_Variants.ContainsKey(buyeraccount.SellerChannel) Then
                        skuvariant = branch.Product.i_Variants(buyeraccount.SellerChannel)(0)  'picks the first variant (would be nice if it found the 'best' one - see matchwith
                    Else
                        'this disti doesn't sell this resolving part.. so use the list prices variant (which will automatically pick the 'best' hp/everyone variant (basedon the region and currency of the buyer account)
                        Dim lp As clsPrice = branch.Product.ListPrice(buyeraccount)  'picks the first variant (would be nice if it found the 'best' one - see matchwith
                        If lp IsNot Nothing Then
                            skuvariant = lp.SKUVariant
                        End If
                    End If

                    If skuvariant IsNot Nothing Then
                        script = "flex('" & Me.ResolvePath & "'," & branch.ID & "," & CStr(ResolvingQty) & ",''," & skuvariant.ID & ");"
                    End If



                    UI.Controls.Add(NewLit("<div onclick=""" & script & ";return false;"" style='display:none;" & If(String.IsNullOrEmpty(script) Or ResolvePath IsNot Nothing, "", "text-decoration:underline;cursor:pointer;") & "' id='I_" & Trim$(iq.infoID) & "'>" & displaytext & "</div>"))
                Else
                    'This needs to be more intelligent
                    'Priorities are...  
                    '1. Find the option type branch (what if its spread?  links to all?)
                    '2. Find if it has a help me choose definition
                    '3. Add default filters on if they exist

                    'Do we have any more links needed?
                    Dim fromSystem = Utility.systemPath(Me.ResolvePath)
                    Dim prod = iq.Branches(Split(Me.ResolvePath, ".").Last).Product
                    Dim s = displaytext

                    If ProductValidation IsNot Nothing Then
                        For Each v In Split(If(String.IsNullOrEmpty(ProductValidation.LinkOptType), ProductValidation.RequiredOptType, ProductValidation.LinkOptType), "/")
                            For Each path In iq.Branches(Split(fromSystem, ".").Last).findAllProductPathsByAttributeValueRecursive(fromSystem, "optType", v, True, buyeraccount)
                                If String.IsNullOrEmpty(ProductValidation.LinkOptionFamily) OrElse iq.Branches(Split(path, ".").Last).findAllProductPathsByAttributeValueRecursive(fromSystem, "optfamily", ProductValidation.LinkOptionFamily, True, buyeraccount).Count > 0 Then
                                    If String.IsNullOrEmpty(ProductValidation.LinkTechnology) OrElse iq.Branches(Split(path, ".").Last).findAllProductPathsByAttributeValueRecursive(fromSystem, "technology", ProductValidation.LinkTechnology, True, buyeraccount).Count > 0 Then
                                        If iq.Branches(Split(Me.ResolvePath, ".").Last).Translation.text(English) <> "FIOs" Then 'Never show FIO's (might need TRO's too in here?)
                                            s &= "<div onclick=""getBranches('cmd=defFilterOn&path=" & fromSystem & "&to=" & path & "&into=tree');"">Click To view " & iq.Branches(Split(path, ".").Last).Translation.text(English) & "</div>"
                                        End If
                                    End If
                                End If
                            Next
                        Next
                    Else
                        If iq.Branches(Split(Me.ResolvePath, ".").Last).Translation.text(English) <> "FIOs" Then s &= "<div onclick=""getBranches('cmd=defFilterOn&path=" & fromSystem & "&to=" & Me.ResolvePath & "&into=tree');"">Click To view " & iq.Branches(Split(Me.ResolvePath, ".").Last).Translation.text(English) & "</div>"
                    End If
                    UI.Controls.Add(NewLit("<div style='display:none;" & If(String.IsNullOrEmpty(script) Or ResolvePath IsNot Nothing, "", "text-decoration:underline;cursor:pointer;") & "' id='I_" & Trim$(iq.infoID) & "'>" & s & "</div>"))
                End If
            Else
                UI.Controls.Add(NewLit("<div onclick=""" & script & ";return false;"" style='display:none;" & If(String.IsNullOrEmpty(script) Or ResolvePath IsNot Nothing, "", "text-decoration:underline;cursor:pointer;") & "' id='I_" & Trim$(iq.infoID) & "'>" & displaytext & "</div>"))
            End If
            lbl.CssClass = "validationMessageTitle"
            UI.Attributes("onmousedown") = "burstBubble(event);TagToTip('I_" & Trim$(iq.infoID) & "', TITLE, '" & replaceVariables(title, Me.variables) & "',FOLLOWMOUSE ,false, CLICKSTICKY, false, CLICKCLOSE, false, CLOSEBTN, true, COPYCONTENT, false, DURATION, 8950,DELAY, 700, BORDERWIDTH, 1, BORDERCOLOR, '#2F7BD1', PADDING, 2,WIDTH,200,FADEIN,150,EXCLUSIVE,true);return false;"
            UI.Style("cursor") = "pointer"
        End If


    End Function


    Public Function UIExpanded(buyeraccount As clsAccount, language As clsLanguage, ByRef errorMessages As List(Of String), QuoteId As Int32) As Panel
        'ML - passing QuoteId in for a unique id for validation acknowledgement
        'Dan wanted validation messages rendered as <LI>'s - so this returns a listitem not not a Panel (DIV) yuck

        '   Dim txt$
        UIExpanded = New Panel 'laceHolder
        UIExpanded.Attributes("class") = "panelMessage"

        iq.infoID = iq.infoID + 1

        Dim li As HtmlGenericControl

        li = New HtmlGenericControl("DIV")

        UIExpanded.Controls.Add(li)

        Dim branch As clsBranch = Nothing
        ' Dim desc As clsTranslation
        Dim lbl As Label = New Label

        Dim displaytext As String = ""
        Dim title = Me.title.text(language).Replace("[mfr]", buyeraccount.mfrCode)
        Dim message = Me.message.text(language).Replace("[mfr]", buyeraccount.mfrCode)

        li.Controls.Add(NewLit("<h2 class='upsellLineHeader'>" & replaceVariables(title, Me.variables) & "</h2>"))
        li.Controls.Add(NewLit("<p class='upsellLineBody'>" & replaceVariables(message, Me.variables) & "</p>"))

        'Search for All Options Link
        If (Not String.IsNullOrEmpty(ResolvePath)) Then
            Dim fromSystem = Utility.systemPath(Me.ResolvePath)
            If ProductValidation IsNot Nothing Then
                Dim prod = iq.Branches(Split(Me.ResolvePath, ".").Last).Product
                For Each v In Split(If(String.IsNullOrEmpty(ProductValidation.LinkOptType), ProductValidation.RequiredOptType, ProductValidation.LinkOptType), "/")
                    For Each path In iq.Branches(Split(fromSystem, ".").Last).findAllProductPathsByAttributeValueRecursive(fromSystem, "optType", v, True, buyeraccount)
                        If String.IsNullOrEmpty(ProductValidation.LinkOptionFamily) OrElse iq.Branches(Split(path, ".").Last).findAllProductPathsByAttributeValueRecursive(fromSystem, "optfamily", ProductValidation.LinkOptionFamily, True, buyeraccount).Count > 0 Then
                            If String.IsNullOrEmpty(ProductValidation.LinkTechnology) OrElse iq.Branches(Split(path, ".").Last).findAllProductPathsByAttributeValueRecursive(fromSystem, "technology", ProductValidation.LinkTechnology, True, buyeraccount).Count > 0 Then
                                If iq.Branches(Split(path, ".").Last).Translation.text(English) <> "FIOs" Then 'Never show FIO's (might need TRO's too in here?)
                                    li.Controls.Add(NewLit("<p class='upsellLineBody' onclick=""getBranches('cmd=defFilterOn&path=" & fromSystem & "&to=" & path & "&into=tree');"" style='cursor:pointer;text-decoration:underline;'>Click To view " & iq.Branches(Split(path, ".").Last).Translation.text(English) & " options</p>"))
                                End If
                            End If
                        End If
                    Next
                Next
            Else
                If iq.Branches(Split(Me.ResolvePath, ".").Last).Translation.text(English) <> "FIOs" Then li.Controls.Add(NewLit("<p class='upsellLineBody' onclick=""getBranches('cmd=defFilterOn&path=" & fromSystem & "&to=" & ResolvePath & "&into=tree');"" style='cursor:pointer;text-decoration:underline;'>Click To view " & iq.Branches(Split(ResolvePath, ".").Last).Parent.Translation.text(English) & " options</p>"))
            End If

        End If


    End Function
    Public Overrides Function Equals(obj As Object) As Boolean
        Return severity = obj.severity AndAlso If(message IsNot Nothing AndAlso obj.message IsNot Nothing, message Is obj.message, title Is obj.title) AndAlso variables Is obj.variables
    End Function
    Public Overrides Function GetHashCode() As Integer
        Return severity.GetHashCode() + If(message IsNot Nothing, message.GetHashCode(), title.GetHashCode()) + variables.GetHashCode()
    End Function

End Class

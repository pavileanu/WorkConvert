public class ClsValidationMessage
{

    public EnumValidationSeverity severity;
    public enumValidationMessageType type;
    public string imagename;
    public clsTranslation message;
    public clsTranslation title;
    public clsTranslation resolutionMessage;
    public string ResolvePath; //path to resolving part
    public int ResolvingQty;
    public int ResolverGives;
    public string[] variables; //%1 type variables embedded in the validation messages
    public string slotTypeMajor;
    public bool Acknowledged = false;
    public string ID; //Arbitary unique ID used for acknowledgement syn with the client script
    public Dictionary<clsFilter, List<string>> DefaultFilters; //Used mainly for optimizations
    private clsProductValidation ProductValidation;

    public ClsValidationMessage(enumValidationMessageType type, EnumValidationSeverity severity, clsTranslation message, clsTranslation title, string resolvePath, int resolvingQty, int Resolvergives, string[] Variables, string slotTypeMajor = null, string ID = "", Dictionary<clsFilter, List<string>> defFilters = null, clsProductValidation ProdValidation = null)
    {

        this.type = type;
        this.severity = severity;
        this.message = message;
        this.title = title;
        this.resolutionMessage = resolutionMessage;
        if (severity == EnumValidationSeverity.greenTick)
        {
            this.imagename = "ICON_CIRCLE_tick.png";
        }
        else if (severity == EnumValidationSeverity.BlueInfo)
        {
            this.imagename = "ICON_CIRCLE_info.png";
        }
        else if (severity == EnumValidationSeverity.Question)
        {
            this.imagename = "ICON_CIRCLE_question.png";
        }
        else if (severity == EnumValidationSeverity.Exclamation)
        {
            this.imagename = "ICON_CIRCLE_exclamation.png";
        }
        else if (severity == EnumValidationSeverity.amberalert)
        {
            this.imagename = "ICON_CIRCLE_amberAlert.png";
        }
        else if (severity == EnumValidationSeverity.RedCross)
        {
            this.imagename = "ICON_CIRCLE_Cross.png";
        }
        else if (severity == EnumValidationSeverity.DoesQualify)
        {
            // Me.imagename = "ICON_CIRCLE_doesQualify.png"
            this.imagename = "ICON_CIRCLE_tick.png";
        }
        else if (severity == EnumValidationSeverity.DoesntQualify)
        {
            this.imagename = "ICON_IQ2_FlexFail.png";
            //Me.imagename = "ICON_IQ2_RedAlert.png"
        }
        else if (severity == EnumValidationSeverity.Upsell)
        {
            this.imagename = "ICON_IQ2_UPSELL.png";
        }
        else
        {
            Interaction.Beep();
        }

        this.DefaultFilters = defFilters;
        this.ResolvePath = resolvePath;
        this.ResolvingQty = resolvingQty;
        this.ResolverGives = Resolvergives;
        this.variables = Variables;
        this.slotTypeMajor = slotTypeMajor;
        this.ProductValidation = ProdValidation;
        this.ID = ID;

    }

    public Panel CompactUI(clsLanguage language)
    {
        Panel returnValue = default(Panel);

        returnValue = new Panel();

        returnValue.Attributes("style") += "display:inline-block;";

        clsBranch branch = null;
        iq.infoID += 1;

        Image img = default(Image);
        img = new Image();
        img.ImageUrl = "/images/navigation/" + this.imagename;
        img.Attributes("onmousedown") = "TagToTip(\'I_" + Strings.Trim(System.Convert.ToString(iq.infoID)) + "\', TITLE, \'Message\', CLICKSTICKY, true, CLICKCLOSE, false, CLOSEBTN, true, COPYCONTENT, false, DELAY, 400, BORDERWIDTH, 1, BORDERCOLOR, \'#2F7BD1\', PADDING, 2,WIDTH,400);return false;";

        System.Char q = '\u0022';

        Literal lit = new Literal();
        //clicking anwhere on the div will dismiss it - so will hovering and 'leaving'
        lit.Text = "<div id=\'I_" + Strings.Trim(System.Convert.ToString(iq.infoID)) + "\' style=\'display:none\'>"; // class='infoBox' "
        //lit.Text &= "onmousedown=" & q$ & "display('I_" & iq.infoID & "','none');return false;" & q$
        //lit.Text &= " onMouseLeave=" & q$ & "display('I_" & iq.infoID & "','none');return false;" & q$
        //lit.Text &= ">"

        // lit.Text &= "<img src=/images/navigation/close.png title='Click anywhere in the box to hide this message' style='float:right'; >"
        lit.Text += replaceVariables(this.message.text(language), variables);
        lit.Text += "</div>";

        returnValue.Controls.Add(img);
        returnValue.Controls.Add(lit);


        return returnValue;
    }



    public string replaceVariables(object l, string[] v)
    {

        for (i = 0; i <= (v.Length - 1); i++)
        {
            l = Strings.Replace(System.Convert.ToString(l), "%" + (i + 1).ToString().Trim(), v[i]);
        }

        return System.Convert.ToString(l);

    }

    public Panel UI(clsAccount buyeraccount, clsLanguage language, List<string> errorMessages, int QuoteId)
    {
        Panel returnValue = default(Panel);
        //ML - passing QuoteId in for a unique id for validation acknowledgement
        //Dan wanted validation messages rendered as <LI>'s - so this returns a listitem not not a Panel (DIV) yuck

        //   Dim txt$
        returnValue = new Panel(); //laceHolder
        returnValue.Attributes("class") = "panelMessage";

        iq.infoID = iq.infoID + 1;

        HtmlGenericControl li = default(HtmlGenericControl);
        if (false) //Dan's way
        {
            li = new HtmlGenericControl("LI");
            li.Attributes("class") += " severity" + Strings.Trim(this.severity);
        }
        else
        {
            li = new HtmlGenericControl("DIV");
            Image i = new Image();
            li.Controls.Add(i);
            i.ImageUrl = "/images/navigation/" + this.imagename;
            if (Acknowledged)
            {
                i.ImageUrl = "/images/navigation/ICON_CIRCLE_tick.png";
            }
        }

        returnValue.Controls.Add(li);

        clsBranch branch = null;
        // Dim desc As clsTranslation
        Label lbl = new Label();
        lbl.Style.Add("padding-left", "2px");
        string displaytext = "";
        string title = null;
        if (!(this.title == null))
        {
            title = System.Convert.ToString(this.title.text(language).Replace("[mfr]", buyeraccount.mfrCode));
        }
        string message = null;
        if (!(this.message == null))
        {
            message = System.Convert.ToString(this.message.text(language).Replace("[mfr]", buyeraccount.mfrCode));
        }

        if (this.title != null)
        {
            lbl.Text = replaceVariables(title, this.variables);
        }

        if ((this.severity == EnumValidationSeverity.Question || this.severity == EnumValidationSeverity.Exclamation || this.severity == EnumValidationSeverity.BlueInfo || this.severity == EnumValidationSeverity.RedCross || severity == EnumValidationSeverity.amberalert || Acknowledged) && this.message != null)
        {
            //Does this require an acceptance, if so add to the popup
            displaytext += replaceVariables(message, this.variables);
            if (severity == EnumValidationSeverity.amberalert)
            {
                displaytext += "<br><br><span class=\'acknowledge\' onclick=\"burstBubble(event);acknowledgedvalidation(\'" + this.ID + "\'," + System.Convert.ToString(QuoteId) + ");return false;\">" + Xlt("Click to Acknowledge", language) + "</span>";
            }

        }
        returnValue.Attributes("onclick") = "burstBubble(event);return false;";


        if (type == enumValidationMessageType.UpsellHolder)
        {
            returnValue.Attributes.Add("onclick", " burstBubble(event);$(\'.upsell\').click();return false; ");
            returnValue.Style.Add("cursor", "pointer");
        }



        if (!string.IsNullOrEmpty(ResolvePath) && this.ResolvingQty > 0)
        {
            branch = iq.Branches(this.ResolvePath.Split('.').Last);

            //lbl.Text &= "&nbsp;" & Me.ResolvingQty & " x " & branch.SKU & " alllows +" & ResolverGives
            //lbl.Text &= "&nbsp;" & Me.ResolvingQty & " x " & branch.Translation.text(English) & " allows +" & ResolverGives
            displaytext += "<br> " + Xlt("Consider adding", language) + " " + System.Convert.ToString(this.ResolvingQty) + " x <span style=\'text-decoration:underline;cursor:pointer;\' onmousedown=\"burstBubble(event);getBranches(\'cmd=open&to=" + ResolvePath + "\');\">" + branch.Translation.text(language) + "</span>" + " allowing " + System.Convert.ToString(ResolverGives) + " more.";

            // desc = branch.Product.i_Attributes_Code("Name")(0).Translation
            lbl.ToolTip = branch.Product.DisplayName(language);
            List<clsPrice> prices = branch.Product.GetPrices(buyeraccount, buyeraccount.SellerChannel.priceConfig, iq.AllVariants, errorMessages, false);
            if (prices.Count > 0 && (from p in prices where p != null select p).Any)
            {
                lbl.ToolTip += "&nbsp; from " + LowestPrice(prices).Price.text(buyeraccount, errorMessages);
            }
        }

        li.Controls.Add(lbl);

        // Dim img As Image
        // img = New Image
        // img.ImageUrl = "/images/navigation/" & Me.imagename


        if (!string.IsNullOrEmpty(displaytext))
        {
            string script = "";
            if (!string.IsNullOrEmpty(ResolvePath))
            {
                if (this.ResolvingQty > 0)
                {
                    clsVariant skuvariant = null;
                    //problems if the disti does not sell the resolving part so . .
                    if (branch.Product.i_Variants.ContainsKey(buyeraccount.SellerChannel))
                    {
                        skuvariant = branch.Product.i_Variants(buyeraccount.SellerChannel)[0]; //picks the first variant (would be nice if it found the 'best' one - see matchwith
                    }
                    else
                    {
                        //this disti doesn't sell this resolving part.. so use the list prices variant (which will automatically pick the 'best' hp/everyone variant (basedon the region and currency of the buyer account)
                        clsPrice lp = branch.Product.ListPrice(buyeraccount); //picks the first variant (would be nice if it found the 'best' one - see matchwith
                        if (lp != null)
                        {
                            skuvariant = lp.SKUVariant;
                        }
                    }

                    if (skuvariant != null)
                    {
                        script = "flex(\'" + this.ResolvePath + "\'," + branch.ID + "," + (ResolvingQty).ToString() + ",\'\'," + skuvariant.ID + ");";
                    }



                    returnValue.Controls.Add(NewLit("<div onclick=\"" + script + ";return false;\" style=\'display:none;" + ((string.IsNullOrEmpty(script) || ResolvePath != null) ? "" : "text-decoration:underline;cursor:pointer;") + "\' id=\'I_" + Strings.Trim(System.Convert.ToString(iq.infoID)) + "\'>" + displaytext + "</div>"));
                }
                else
                {
                    //This needs to be more intelligent
                    //Priorities are...
                    //1. Find the option type branch (what if its spread?  links to all?)
                    //2. Find if it has a help me choose definition
                    //3. Add default filters on if they exist

                    //Do we have any more links needed
                    object fromSystem = Utility.systemPath(this.ResolvePath);
                    object prod = iq.Branches(this.ResolvePath.Split('.').Last).Product;
                    System.String s = displaytext;

                    if (ProductValidation != null)
                    {
                        foreach (var v in Strings.Split(System.Convert.ToString((string.IsNullOrEmpty(System.Convert.ToString(ProductValidation.LinkOptType))) ? ProductValidation.RequiredOptType : ProductValidation.LinkOptType), "/"))
                        {
                            foreach (var path in iq.Branches(Strings.Split(System.Convert.ToString(fromSystem), ".").Last).findAllProductPathsByAttributeValueRecursive(fromSystem, "optType", v, true, buyeraccount))
                            {
                                if (string.IsNullOrEmpty(System.Convert.ToString(ProductValidation.LinkOptionFamily)) || iq.Branches(Strings.Split(System.Convert.ToString(path), ".").Last).findAllProductPathsByAttributeValueRecursive(fromSystem, "optfamily", ProductValidation.LinkOptionFamily, true, buyeraccount).Count() > 0)
                                {
                                    if (string.IsNullOrEmpty(System.Convert.ToString(ProductValidation.LinkTechnology)) || iq.Branches(Strings.Split(System.Convert.ToString(path), ".").Last).findAllProductPathsByAttributeValueRecursive(fromSystem, "technology", ProductValidation.LinkTechnology, true, buyeraccount).Count() > 0)
                                    {
                                        if (iq.Branches(this.ResolvePath.Split('.').Last).Translation.text(English) != "FIOs") //Never show FIO's (might need TRO's too in here?)
                                        {
                                            s += "<div onclick=\"getBranches(\'cmd=defFilterOn&path=" + System.Convert.ToString(fromSystem) + "&to=" + path + "&into=tree\');\">Click To view " + iq.Branches(Strings.Split(System.Convert.ToString(path), ".").Last).Translation.text(English) + "</div>";
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (iq.Branches(this.ResolvePath.Split('.').Last).Translation.text(English) != "FIOs")
                        {
                            s += "<div onclick=\"getBranches(\'cmd=defFilterOn&path=" + System.Convert.ToString(fromSystem) + "&to=" + this.ResolvePath + "&into=tree\');\">Click To view " + iq.Branches(this.ResolvePath.Split('.').Last).Translation.text(English) + "</div>";
                        }
                    }
                    returnValue.Controls.Add(NewLit("<div style=\'display:none;" + ((string.IsNullOrEmpty(script) || ResolvePath != null) ? "" : "text-decoration:underline;cursor:pointer;") + "\' id=\'I_" + Strings.Trim(System.Convert.ToString(iq.infoID)) + "\'>" + s + "</div>"));
                }
            }
            else
            {
                returnValue.Controls.Add(NewLit("<div onclick=\"" + script + ";return false;\" style=\'display:none;" + ((string.IsNullOrEmpty(script) || ResolvePath != null) ? "" : "text-decoration:underline;cursor:pointer;") + "\' id=\'I_" + Strings.Trim(System.Convert.ToString(iq.infoID)) + "\'>" + displaytext + "</div>"));
            }
            lbl.CssClass = "validationMessageTitle";
            returnValue.Attributes("onmousedown") = "burstBubble(event);TagToTip(\'I_" + Strings.Trim(System.Convert.ToString(iq.infoID)) + "\', TITLE, \'" + replaceVariables(title, this.variables) + "\',FOLLOWMOUSE ,false, CLICKSTICKY, false, CLICKCLOSE, false, CLOSEBTN, true, COPYCONTENT, false, DURATION, 8950,DELAY, 700, BORDERWIDTH, 1, BORDERCOLOR, \'#2F7BD1\', PADDING, 2,WIDTH,200,FADEIN,150,EXCLUSIVE,true);return false;";
            returnValue.Style("cursor") = "pointer";
        }


        return returnValue;
    }


    public Panel UIExpanded(clsAccount buyeraccount, clsLanguage language, List<string> errorMessages, int QuoteId)
    {
        Panel returnValue = default(Panel);
        //ML - passing QuoteId in for a unique id for validation acknowledgement
        //Dan wanted validation messages rendered as <LI>'s - so this returns a listitem not not a Panel (DIV) yuck

        //   Dim txt$
        returnValue = new Panel(); //laceHolder
        returnValue.Attributes("class") = "panelMessage";

        iq.infoID = iq.infoID + 1;

        HtmlGenericControl li = default(HtmlGenericControl);

        li = new HtmlGenericControl("DIV");

        returnValue.Controls.Add(li);

        clsBranch branch = null;
        // Dim desc As clsTranslation
        Label lbl = new Label();

        string displaytext = "";
        object title = this.title.text(language).Replace("[mfr]", buyeraccount.mfrCode);
        object message = this.message.text(language).Replace("[mfr]", buyeraccount.mfrCode);

        li.Controls.Add(NewLit("<h2 class=\'upsellLineHeader\'>" + replaceVariables(title, this.variables) + "</h2>"));
        li.Controls.Add(NewLit("<p class=\'upsellLineBody\'>" + replaceVariables(message, this.variables) + "</p>"));

        //Search for All Options Link
        if (!string.IsNullOrEmpty(ResolvePath))
        {
            object fromSystem = Utility.systemPath(this.ResolvePath);
            if (ProductValidation != null)
            {
                object prod = iq.Branches(this.ResolvePath.Split('.').Last).Product;
                foreach (var v in Strings.Split(System.Convert.ToString((string.IsNullOrEmpty(System.Convert.ToString(ProductValidation.LinkOptType))) ? ProductValidation.RequiredOptType : ProductValidation.LinkOptType), "/"))
                {
                    foreach (var path in iq.Branches(Strings.Split(System.Convert.ToString(fromSystem), ".").Last).findAllProductPathsByAttributeValueRecursive(fromSystem, "optType", v, true, buyeraccount))
                    {
                        if (string.IsNullOrEmpty(System.Convert.ToString(ProductValidation.LinkOptionFamily)) || iq.Branches(Strings.Split(System.Convert.ToString(path), ".").Last).findAllProductPathsByAttributeValueRecursive(fromSystem, "optfamily", ProductValidation.LinkOptionFamily, true, buyeraccount).Count() > 0)
                        {
                            if (string.IsNullOrEmpty(System.Convert.ToString(ProductValidation.LinkTechnology)) || iq.Branches(Strings.Split(System.Convert.ToString(path), ".").Last).findAllProductPathsByAttributeValueRecursive(fromSystem, "technology", ProductValidation.LinkTechnology, true, buyeraccount).Count() > 0)
                            {
                                if (iq.Branches(Strings.Split(System.Convert.ToString(path), ".").Last).Translation.text(English) != "FIOs") //Never show FIO's (might need TRO's too in here?)
                                {
                                    li.Controls.Add(NewLit("<p class=\'upsellLineBody\' onclick=\"getBranches(\'cmd=defFilterOn&path=" + System.Convert.ToString(fromSystem) + "&to=" + path + "&into=tree\');\" style=\'cursor:pointer;text-decoration:underline;\'>Click To view " + iq.Branches(Strings.Split(System.Convert.ToString(path), ".").Last).Translation.text(English) + " options</p>"));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (iq.Branches(this.ResolvePath.Split('.').Last).Translation.text(English) != "FIOs")
                {
                    li.Controls.Add(NewLit("<p class=\'upsellLineBody\' onclick=\"getBranches(\'cmd=defFilterOn&path=" + System.Convert.ToString(fromSystem) + "&to=" + ResolvePath + "&into=tree\');\" style=\'cursor:pointer;text-decoration:underline;\'>Click To view " + iq.Branches(ResolvePath.Split('.').Last).Parent.Translation.text(English) + " options</p>"));
                }
            }

        }


        return returnValue;
    }
    public override bool Equals(object obj)
    {
        return severity == obj.severity && (message != null && obj.message != null ? message == obj.message : title == obj.title) && variables == obj.variables;
    }
    public override int GetHashCode()
    {
        return severity.GetHashCode() + (message != null ? (message.GetHashCode()) : (title.GetHashCode())) + variables.GetHashCode();
    }

}
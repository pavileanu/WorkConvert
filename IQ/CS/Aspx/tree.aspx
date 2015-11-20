<%@ Page Title="" Language="C#" AutoEventWireup="true" MasterPageFile="~/Site.Master" CodeBehind="tree.aspx.cs" Inherits="IQ.tree1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">

    <style>
        .ui-menu {
            width: 150px;
            z-index: 50;
        }
    </style>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    
    <div id='popupMsg' class='triangle-isosceles bottom popupPanel'>&nbsp;</div>

    <div id="customerPanel">

        <asp:Label ID="Label12" runat="server" Text="Quoting for:" CssClass="formLabel"></asp:Label>

        <!-- there are two textbox associated with each autosuggest - one, client side INPUT holds the display value   -->
        <!-- hide the start quote button when we click in the find customer box-->
        <asp:TextBox ID="txtBuyer" CssClass="buyerSearchBox" runat="server" onkeyup="suggest('ctl00_MainContent_txtBuyer','ddlBuyer','accounts');"
            onclick="blank('ctl00_MainContent_txtBuyer');display('ddlBuyer','block');display('ctl00_MainContent_BtnStartQuote','none');"></asp:TextBox>

        <!-- and a server side textbox which holds the selected *value* (not necessarily what is displayed) -->
        <asp:TextBox ID="txtBuyerID" runat="server" Width="1px" Height="1px" Style="display: none"></asp:TextBox>

        <asp:Button ID="BtnStartQuote" UseSubmitBehavior="false" runat="server" Text="Start Quote" CssClass="textButton"
            OnClientClick="rExec('manipulation.aspx?command=startquote&buyerID=' + document.getElementById('ctl00_MainContent_txtBuyerID').value,showQuote);
             " />
        <!-- nb we do not return false - to let the page post back, so that the Quoting For: textbox can be updated -->

        <!--The select box is client side and initially hidden - display = none - the js display() show/hides it -->
        <!-- the PAIR or textboxes are set to the ID and value From the DDL -->
        <select id="ddlBuyer" class="findBuyer"
            onclick="setTxtsFromDDL('ctl00_MainContent_txtBuyer','ctl00_MainContent_txtBuyerID',this);
                display('ddlBuyer','none');
                display('ctl00_MainContent_BtnStartQuote','inline-block');
                processBuyerSelection(ctl00_MainContent_txtBuyerID.value);"
            onchange="setTxtsFromDDL('ctl00_MainContent_txtBuyer','ctl00_MainContent_txtBuyerID',this);" size="20" style="display: none;">
        </select>

        <asp:Panel ID="PnlAddContact" runat="server"></asp:Panel>
        <!--has an iframe injected into it if we add a company or contact -->

    </div>
    <!--/customerPanel-->
    <div>
        <!--
        <div>
            <input type="button" value="Search" onclick="searchClick();" />
        </div>
        -->

    </div>
    <div id="search" style='display: none'>
        <div id="patch" class="kwPatch"></div>
        <!--obliterate some underflow-->
        <div id="systemsSearch">
            <h2>Systems Search</h2>
        </div>
        <div id="optionsSearch">
            <h2>Options Search</h2>
        </div>

        <%-
-System.Xml.Linq.XElement.Parse("<span style=\"vertical-align: top;\">Keyword Search</span>")--;
%>

        <!--Keyword search -->
        <div id='searchBoxHolder'>
            <!-- style="display: inline-block;"> -->
            <input type="search" id="searchBox" class="kwSearchBox" text="" value="" autocomplete="off"
                onkeyup="Keystroke();"
                onclick="ShowKWResults();" />
            <!--blank('find') -->
        </div>

        <!--Radio buttons/tickboxes (in a div so the will wrap as one) -->
        <!--<div id="KwButtons" style="display: inline-block;"> -->
        <div id="KwButtons">

            <!--        <input id="Radio1" type="radio" name="ST" onclick="if(this.value){searchType='stocked'};suggest('find','ddlMatches','keywords');"/>Search in stock only -->

            <!--Search type ie. 'stocked' (products in the feed) only-->
            <input id="Radio2" checked="checked" type="checkbox" name="ST"
                onclick="keywordSearch('searchBox', 'KwResultsHolder');" />
            <asp:Label ID="Label1" runat="server">Search priced products only</asp:Label>

            <!--All-->
            <!--   <input id="Radio3" type="radio" name="ST" onclick="if (this.value) { searchType = 'all' }; keywordSearch('searchBox', 'KwResultsHolder');" />
            <asp:Label ID="Label2" runat="server">Search all products</asp:Label> -->

            <!--Scope-- Path - now Always 'false' (Until somebody realised that actually being able to search just Desktops, or just one family was a useful feature ! -->
            <span style="visibility: hidden;">
                <input id="searchScope" type="checkbox" onclick="changeScope(this);" />
                <asp:Label ID="Label3" runat="server">Search under current selection only</asp:Label></span>
        </div>

        <div id="KwResultsHolder">
        </div>

        <!-- Replaced by Kwresultsdiv
                
                    <select id="ddlMatches" class="keywordResults"  size="10"  style="display:none;" 
                    onclick="selectedResult()" 
                    >                 
                    <option value="1"></option>
                    </select>
                -->

    </div>

    <!--/Keywordsearch-->


    <!-- The product tree is initially hidden- and subsequently revealed with a js display() (setting it's style to 'inline') -->

    <!-- Nobbled for vegas
        <asp:Panel ID="focusPanel"  runat="server" ScrollBars="none" > 
                <asp:label ID="FocusLabel" runat="server">Currently focussing on ..</asp:label>
                <asp:Button ID="FocusButton" runat="server" Text="All HP" />                     
           </asp:panel>
    -->


    <div id='displayMsg' class='triangle-isosceles right displayPanel'>&nbsp;</div>
    <div id="quoteOuter">
        <!-- this gets refilled by the ajax call (to quote.aspx)-->
        <div id="quote">quote</div>
        <asp:Panel ID="pnlQuoteTools" runat="server" Visible="false" CssClass="exportTools">
            <asp:TextBox ID="txtQuoteName" runat="server"></asp:TextBox>
            <asp:Button ID="Btnsave" runat="server" Text="Save" CssClass="textButton" />
            <asp:Button ID="btnXMLQuote" runat="server" Text="XML" CssClass="textButton"></asp:Button>
            <asp:Label ID="LblSave" runat="server" Text=""></asp:Label>
            <!-- used to display 'saved' message -->
            <asp:Button ID="BtnExcel" runat="server" Text="Excel" CssClass="textButton" />
            <!--<asp:Button ID="BtnPDF" runat="server" Text="PDF" cssclass="textButton"/> -->
            <asp:Button ID="BtnCopy" runat="server" Text="Copy" CssClass="textButton" ToolTip="Copy the quote parts list to the ClipBoard" onmousedown="copyToClipBoard(txtPartsList);return false;" />
            <asp:Button ID="BtnEmail" runat="server" Text="Email" CssClass="textButton" ToolTip="Email this quote to the customer" />
        </asp:Panel>
        <!--/exportTools-->
        <asp:Panel ID="PnlErrors" runat="server"></asp:Panel>
        <!--has an iframe injected into it if we add a company or contact -->

    </div>
    <asp:Literal ID="litAdvert" runat="server"></asp:Literal>
    <div id="ssHolder" style="border:1px solid #efefef;display:none;margin:0px;padding:0px;overflow:no-content;"><iframe style="overflow:hidden;border:0px;width:100%;" id="ssFrame"></iframe></div>
    <div id="messageDiv"></div>
    <div id="adDiv"></div>
    <div id="BreadCrumbs"></div>
    <!--<img src="../images/Navigation/unite.png" id="BtnUndo" style="visibility:visible;" onclick="ShowUndo();return false;" alt="Undo" height="17px"/>-->

    <div id="UndoContainer" class="UndoPaneContainer">
        Actions<br />
        <table id="UndoTable">
            <tr>
                <th>Action</th>
                <th>Date</th>
                <th>Source Branch Name</th>
                <th>Destination Branch Name</th>
                <th>&nbsp;</th>
            </tr>

        </table>
    </div>
    <div id="FieldFilter" class="FieldFilterContainer">
        <div class="FieldFilterButtons">
            <img alt="Reset" width="20px" height="20px" onclick="resetColumnOverrides();return false;" src="../images/Navigation/refresh.png" />
            <img alt="Close" width="20px" height="20px" onclick="collapseFieldFilter();return false;" src="../images/Navigation/close.png" />
        </div>
        <div class="FieldFilterHeader">
            <h2 id="custHeader">Customise Screen</h2>
            <div>
                <img src="../images/Navigation/tick.png" style="cursor: default; width: 24px; height: 24px;" onclick="makeCustomSettingsDefault();return false;" alt="Make Default" title="Make Default" />
                <img src="../images/Navigation/plus.png" style="cursor: default; width: 24px; height: 24px;" onclick="createUniqueScreen();return false;" alt="Create Unique Screen Here" title="Create Unique Screen Here" />
                <img src="../images/Navigation/minus.png" style="cursor: default; width: 24px; height: 24px;" onclick="removeUniqueScreen();return false;" alt="Remove This Screen" title="Remove This Screen" />
                <img src="../images/Navigation/expand.png" style="cursor: default; width: 24px; height: 24px;" onclick="changeFilters();return false;" alt="Change Filters" title="Change Filters" />
                <img alt="Clone screen settings to other branches" style="cursor: default; width: 24px; height: 24px;" title="Clone screen settings to other branches" onclick="expandCloneScreenSettings('tree.1','customizeCloneObjects');return false;" src="../images/Navigation/Adopt.png" />
            </div>
            <div id="custText">Drag columns to reorder and resize using the bar on the right
                <button type="button" onclick="$('#addfieldContainer').dialog();return false;">Add Fields</button></div>
        </div>
        <div id="FieldFilterFieldList" class="FieldFilterFieldList"></div>

    </div>
    <div id="changeFilterContainer"></div>
    <div id="addfieldContainer"></div>
    <div id="customizeClone" class="customizeCloneList">
        <div style="overflow: scroll; height: 300px;">
            <ul style="list-style: none; content: '';">
                <li><a href="#customizeCloneObjects">Branches</a></li>
                <li><a href="#customizeCloneLevels">Levels</a></li>
            </ul>
            <div id="customizeCloneObjects"></div>
            <div id="customizeCloneLevels"></div>
        </div>
        <div id="customizeCloneFooter">
            <button onclick="submitClone();return false;">Save</button></div>
    </div>
    <!--/quoteOuter-->

    <!--BRAZIL-- set to visible to see it ! -->
    <asp:Panel ID="warehousePanel" runat="server" ScrollBars="none" visible="false">
      </asp:Panel>


    <asp:Panel ID="treeHolder" runat="server" ScrollBars="none" CssClass="treeHolder">

        <!-- this is dynamically replaced as the tree is opened -- Dont't remove it !!! -->
        <!--<div id="swift"></div> -->

        <!-- Used by the javascript to (getbranches) to measure the width of 1em in pixels -->

        <div id="tree"></div>

    </asp:Panel>


    <!-- this is the 'prices have changed in the quote - do you want to update - thing -->
    <asp:Panel ID="PnlPriceCheck" runat="server">
    </asp:Panel>

    <div id="foot" style="display: none">
        &nbsp;
    </div>

    <div id="contrastDiv" style="clear: both">
    </div>

    <!-- this initial JS call replaces the div 'tree' with a div called Tree.2 
     the root branch, it's inserted open - containing the top level elements (server, desktops, notebooks, storage)     
    -->
    <script type="text/javascript">

        //note there is no path specified here so the one in the (path) session variable is used (see tree.aspx.vb) Onload
        window.onload = function () { loading = true; showQuote(); }  //show the current quote after postbacks (rare.. but happen on the export, save etc)

        $(window).on('resize', function () {
            clearTimeout(myResize); myResize = setTimeout(function () { resizeOpenGrids(); }, 500);



            var ha = $(window).height() - $('#spacer').height();//footer
            if ($('#quoteOuter').height() > ha) {
                $('#quoteOuter').css({ 'height': (ha) + 'px' })
            }
        }
        );



        function compareClick(divToFill) {
            var divPop = '#Pnl';
            var popupWidth = $(document).width() * 80 / 100;
            $(divPop).dialog({
                resizable: false,
                height: 640,
                width: popupWidth,
                modal: true,
                dialogClass: "compareTable",
                title: "Compare",
                draggable: false
            });


        }


        $('#searchBox').bind('paste', function () {
            Keystroke();
        });

        function stClick(checkValue) {
            if (checkValue) {
                searchType = 'priced';
            }
            else {
                searchType = 'all';
            }

            keywordSearch('searchBox', 'KwResultsHolder');
        }

    </script>

</asp:Content>


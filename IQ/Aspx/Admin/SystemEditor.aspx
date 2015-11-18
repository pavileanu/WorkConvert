<%@ Page Title="" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.master" CodeBehind="SystemEditor.aspx.vb" Inherits="IQ.SystemEditor" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">


  
     <style type="text/css" >
  .ui-menu { width: 150px;
              z-index: 50;
  }  
  .menuBar {
            margin-top:0.9em;
        }

        .menuBar span {
            padding: 0.5em;
            border:1px solid #EFEFEF;
            border-radius:5px;
            background-color:#FEFEFE;
            cursor:pointer;
        }

        .menuBar span:hover {
            background-color:#DEDEDE;
        }

        .Main {
            margin-top:1em;
        }
  </style>
    
          
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="menuBar"><span onclick="document.location='SystemEditor.aspx?lid=<%= Request("lid") %>';">System Editor</span><span onclick="document.location='AttributeFinder.aspx?lid=<%= Request("lid") %>';">Attribute Finder</span><span onclick="document.location='AttrFinder.aspx';">Attribute Finder</span><span onclick="document.location='AttrFinder.aspx';">Quote Finder</span><span onclick="document.location='AttrFinder.aspx';">List Price Lookup</span></div>
    <div>
        <div class="Main">
            <div class="Search">System SKU: <input id="SKU" name="SKU" /><input type="button" value="GO" onclick="loadSKU($('#SKU').val()); return false;"/></div>
            <div id="prodDetails"><span id="prodDesc"></span></div>
            <div id="menus" class="menus">
                <ul>
                    <li><a href="#menus-Attributes">Attributes</a></li>
                    <li><a href="#menus-Slots">Slots</a></li>
                    <li><a href="#menus-Children">Children</a></li>
                </ul>
                <div class="tabPanel" id="menus-Attributes">
                    <div ><input style="display:inline-block;" type="button" onclick="SaveAttributes(); return false;" value="Save" /><div style="display:inline-block;background-image:url('../../images/wastebin.png');" id="bin"></div>
                    <div style="display:inline-block;" id="attrTblHolder">
                        <table cellpadding="2" border="0"  id="attrTbl">
                            <tr style="border-bottom:1px solid black;"><th>Attribute</th><th>Value</th><th>Unit</th><th>Text</th></tr>

                        </table>
                    </div>
                            
                    <div style="display:inline-block;background-color:#ededed;float:right;">
                        <div>
                        Search <input id="availSearch" onkeypress="Javascript: if (event.keyCode==13) filterAvailableAttributes($('#availSearch').val());" /><input type="button" value="Go" onclick="filterAvailableAttributes($('#availSearch').val()); return false;"/></div>
                        <div style="display:inline-block;">
                            <h3>SpecTable</h3>
                            <div  id="attr-req"></div>
                        </div>
                         <div style="display:inline-block;">
                            <h3>Family</h3>
                            <div id="attr-fam">

                            </div>
                        </div>
                        <div style="display:inline-block;">
                            <h3>All</h3>
                            <div id="attr-avail">

                            </div>                           
                        </div>
                       </div>
                    </div>
                </div>
                <div id="menus-Slots">
                    Slots
                </div>
                <div id="menus-Children">
                    Children
                </div>
            </div>
        </div>
    </div>

    <script type="text/javascript" >
        $("#menus").tabs();
        $("#attrTblHolder").bind('dragover', function () { $("#attrTblHolder").toggleClass('highlight'); });
        $("#attrTblHolder").droppable({
            drop: function (event, ui) {
                var attrid = ui.draggable[0].id;
                data.Attributes.push({ AttributeId: attrid, Id: -1, Text: '', NumericValue: 0, UnitID: 1 });
                refreshAttributes();
            }
        });
        $("#bin").droppable({
            drop: function (event, ui) {
                var attrid = ui.draggable[0].id;
                $.each(data.Attributes,function (a,b)
                {
                    if (b !== undefined && b.Id == attrid)
                    {
                        data.Attributes.splice(a, 1);
                    }
                });
                
                refreshAttributes();
            }
        });
        
        function loadSKU(sku)
        {
            $.ajax({
                url: "../../AdminCtl/GetSKUEditDetails",
                data: JSON.stringify({ SystemSKU: sku }),
                type: "POST",
                contentType: "application/JSON",
                success: loadSKUSuccess,
            });
        }

        var data;
        function loadSKUSuccess(d)
        {
            data = d;
            //Create the table data
            refreshProductDetails();
            refreshAttributes();
            filterAvailableAttributes("");
        }
        function refreshProductDetails()
        {
            $("#prodDesc").html(data.Product.Description);
        }

        function refreshAttributes()
        {
            $("#attrTbl").empty();
            $.each(data.Attributes, function (a, b) {
                var aa = "<tr style='border:0px;' class='ATTR' id='" + b.Id + "'><td><span id='" + b.AttributeId + "' class='floatingoption' title='" + data.AllAttributes[b.AttributeId].Text + "'>" + data.AllAttributes[b.AttributeId].Text + "</span></td><td><input class='smooth NumericValue' value='" + b.NumericValue + "'></input></td><td>";
                aa += "<select class='smooth Unit'  id='unit'>";
                $.each(data.Units, function (c, d) {
                    aa += "<option value='" + d.ID + "'";
                    if (b.UnitID !== undefined && b.UnitID == d.ID) { aa += " selected " };
                    aa += ">" + d.Text + "</option>";
                });
                aa += "</select>";

                $("#attrTbl").append(aa + "</td><td><input class='smooth Text' style='width:250px;' value='" + b.Text + "'></input></td></tr>");
            });
            $(".floatingoption").draggable({ revert: true });
        }

        function filterAvailableAttributes(txt)
        {
            $("#attr-avail").empty();
            $("#attr-req").empty();
            $.each(data.AllAttributes, function (a, b) {
                if (b.Text.indexOf(txt) > -1 || txt=="")
                {
                    if (b.Required && $("#ATTR" & b.Id).val() == undefined) {
                        $("#attr-req").append("<span id='" + b.Id + "' class='floatingoption'>" + b.Text + "</span>");
                    }
                    if (b.InFamily && $("#ATTR" & b.id).val() == undefined) {
                        $("#attr-fam").append("<span id='" + b.Id + "' class='floatingoption'>" + b.Text + "</span>");
                    } 
                    $("#attr-avail").append("<span id='" + b.Id + "' class='floatingoption'>" + b.Text + "</span>");
                }
            });
            $(".floatingoption").draggable({ revert: true });
        }

        function SaveAttributes()
        {
            //Update the JSON
            $(".ATTR").each(function (a, b) {
                var g = $("#" + b.id);
                $.each(data.Attributes, function (d, c) {
                    if (c.Id == b.id) {
                        c.UnitID = g.find(".Unit").find(":selected").val();
                        c.Text = g.find(".Text").val();
                        c.NumericValue = g.find(".NumericValue").val();
                    }
                });
                
            });

            $.ajax({
                url: "../../AdminCtl/UpdateAttributes",
                data: JSON.stringify(data),
                type: "POST",
                contentType: "application/JSON",
                success: updateSuccess,
            });
       
        }

        function updateSuccess()
        {
            alert("Updated");
        }
    </script>
    <style>
        .Search {
            text-align:center;
        }
        .floatingoption
        {
            display:block;
            border:1px solid #DFDFDF;
            border-radius:5px;
            background-color:#AFAFAF;
            padding:2px;
            cursor:pointer;
            
            max-width:100px;
            overflow:no-display;
        }
        .highlight {
            background-color:red;
        }
        input.smooth {
            border:1px solid #EFEFEF;
            border-radius:10px;
            padding:0px 2px 0px 2px;
        }
    </style>
</asp:Content>

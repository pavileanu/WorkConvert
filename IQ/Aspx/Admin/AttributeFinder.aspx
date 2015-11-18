<%@ Page Language="vb" AutoEventWireup="false"  MasterPageFile="~/Site.master" CodeBehind="AttributeFinder.aspx.vb" Inherits="IQ.AttributeFinder" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
 
    <style type="text/css" >
         .ui-menu { width: 150px;z-index: 50; }
          
         .tResults {
             border:1px solid #808080;
         }

         .tResults th {
             background-color:#EFEFEF;
             text-align:center;
             font-weight:bold;
             padding:0.5em;
         }

        .tResults td {
            padding: 0.5em;
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
            <div class="Search">Attribute<input id="SKU" name="SKU" /><input type="button" value="GO" onclick="loadResults($('#SKU').val()); return false;"/></div>
        </div>
        <div>
            <table id="tResults" class="tResults">
                <tr><th>SKU</th><th>Description</th><th>NumericValue</th><th>Text</th></tr>
                <tr class='search'><th><input id='SKUFilter' onchange='refreshData();' value='' /></th><th><input id='DescFilter' onchange='refreshData();' value='' /></th><th><input id='NumericValueFilter' onchange='refreshData();' value='' /></th><th><input id='TextFilter' onchange='refreshData();' value='' /></th></tr>
            </table>
        </div>
    </div>

    <script>
        function loadResults(sku)
        {
            $.ajax({
                url: "../../Adminctl/FindAttribute",
                data: JSON.stringify({ AttributeCode: sku }),
                type: "POST",
                contentType: "application/JSON",
                success: loadResultsSuccess,
                failed: loadResultsFail,
            });
        }

        var data;

        function loadResultsSuccess(d)
        {
            data = d;
            refreshData();
        }

        function loadResultsFail(d)
        { }

        function refreshData()
        {
           

            var output = "<tr><th>SKU</th><th>Description</th><th>NumericValue</th><th>Text</th></tr>";
            output += "<tr class='search'><th><input id='SKUFilter' onchange='refreshData();' value='" + $("#SKUFilter").val() + "' /></th><th><input id='DescFilter' onchange='refreshData();' value='" + $("#DescFilter").val() + "' /></th><th><input id='NumericValueFilter' onchange='refreshData();' value='" + $("#NumericValueFilter").val() + "' /></th><th><input id='TextFilter' onchange='refreshData();' value='" + $("#TextFilter").val() + "' /></th></tr>";
            $.each(data, function (a, b) {
                $.each(b.Attributes, function (e, f) {
                    if ($("#SKUFilter").val() === undefined || b.SKU.indexOf($("#SKUFilter").val()) > -1)
                        if ($("#DescFilter").val() === undefined || b.Description.indexOf($("#DescFilter").val()) > -1)
                            if ($("#NumericValueFilter").val() === undefined || f.NumericValue.toString().indexOf($("#NumericValueFilter").val()) > -1)
                                if ($("#TextFilter").val() === undefined || f.Text.indexOf($("#TextFilter").val()) > -1)
                                    output += "<tr><td>" + b.SKU + "</td><td>" + b.Description + "</td><td style='text-align:center;'>" + f.NumericValue + "</td><td style='text-align:center;'>" + f.Text + "</td></tr>";
                });

            });
            $("#tResults").empty();
            $("#tResults").append(output);
        }

        
    </script>
</asp:Content>

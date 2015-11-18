<%@ Page Language="vb" AutoEventWireup="false"  MasterPageFile="~/site.master" CodeBehind="Sync.aspx.vb" Inherits="IQ.Sync" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
    <script src="../../scripts/jquery-1.11.2.min.js" type="text/javascript"></script>
        <script src="../../scripts/jquery-ui-1.11.2.min.js" type="text/javascript"></script> 
    <style>
        .skuElement {
            
        }
        .tblHR th{
            background-color:#ADADAD;
            text-align:center;
        }
        .tblRW td{
            background-color:#DADADA;
            padding:5px;
        }
    </style>
    <script type ="text/javascript" >
        var lastRecNo = 0;

        function importScan()
        {
            $.ajax({
                url: "../../AdminCtl/IncrementalImport",
                data: JSON.stringify({ lid: loginID, elid: elevatedID, SkuList: $("#txtSKUList").val()}),
                type: "POST",
                contentType: "application/JSON",
                success: importScanSuccess,
                headers: { "lid": "=" + loginID + ";" }
            });
            setInterval('refreshStatus()', 10000);
            $("btnDoImport").css('color', 'grey');
            $("btnDoImport").click('return false;');
        }
        function importScanSuccess(d)
        {
            var $toAdd = ''
            var $sku = '';
            var $obj = '';
            $.each(d, function (a, b) {
                if ($sku != b.SKU && $sku != '') $toAdd += "</table></div></div>";
                if ($obj != b.ObjectType) $toAdd += "</table>" + b.ObjectType + "<table>" + addObjHeader(b.ObjectType);
                if ($sku != b.SKU) { $toAdd += "<div class='skuElement'><span onclick='$(event.target).next().toggle();'>+</span><div>" + b.SKU + "<br/><table><tr class='tblHR'><th>Action</th><th>Object</th><th>Col1</th><th>Col2</th><th>Col3</th></tr>"; $sku = b.SKU; }
                $toAdd += "<tr class='tblRW' id='" + b.ID + "'><td>Accept<input type='checkbox'/></td><td>" + b.Type + "</td><td>" + b.ObjectType + "</td><td>" + b.Col1 + "</td><td>" + b.Col2 + "</td><td>";
                if (b.Col3 !== undefined) { $toAdd += b.Col3 }
                $toAdd += "</td><td>";
                if (b.Col4 !== undefined) { $toAdd += b.Col4 }
                $toAdd += "</td></tr>";
            });
            $("#ScanResult").append($toAdd);
        }

        function addObjHeader(oh)
        {
            switch (oh)
            {
                case "Attribute":
                    return "<tr><th></th><th>Action</th><th>Attribute</th><th>CurrentValue</th><th>New Value</th></tr>";
                    break;
                case "Quantity":
                    return "<tr><th></th><th>Action</th><th>System SKU</th><th>Path(if known)</th><th>Quantity Details</th></tr>";
                    break;
                default:
                    return "<tr><th></th><th>Action</th><th>Col1</th><th>Col2</th><th>Col3</th></tr>";
                    break;
            }
        }
        function SubmitResults()
        {
            var $tt = new Array();
            $(".skuElement").children(0).children(0).next().children(0).children("tr").each(function (a, b) { $tt.push({ Key: $(b).attr('ID'), Value: $(b).find("input").is(':checked') }) });
            $.ajax({
                url: "../../AdminCtl/IncrementalImportSubmit",
                data: JSON.stringify({ lid: loginID, elid: elevatedID, SubmitList: $tt }),
                type: "POST",
                contentType: "application/JSON",
                success: importScanSuccess,
                headers: { "lid": "=" + loginID + ";" }
            });
            setInterval('refreshStatus()', 2000);
        }

        function refreshStatus()
        {
            $.ajax({
                url: "../../AdminCtl/GetIncrementalImportStatus",
                data: JSON.stringify({ lid: loginID, elid: elevatedID, atPoint: lastRecNo }),
                type: "POST",
                contentType: "application/JSON",
                success: refreshStatusSuccess,
                headers: { "lid": "=" + loginID + ";" }
            });
        }

        function refreshStatusSuccess(d)
        {
            if(d!=null){
            $.each(d, function (a, b) {
                $("#logLines").append("<div>" + b.DateTime + " - " + b.Message + "</div>");
                if (b.Id > lastRecNo) lastRecNo = b.Id;
            });
            }
            else { $("#logLines").append("<div>error</div>") }
        }
    </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <asp:TextBox ID="txtSKUList" runat="server" ClientIDMode ="Static" Width ="600" Wrap ="true" />
    <asp:Button ID="btnDoImport" ClientIDMode="Static"   Text="Import"  OnClientClick="importScan();return false;" runat="server" UseSubmitBehavior="false"   />

    <asp:Button ID="Button1" ClientIDMode="Static"   Text="submit results"  OnClientClick="SubmitResults();return false;" runat="server" UseSubmitBehavior="false"   />
    <div id="logLines"></div>
    <div id="ScanResult"></div>
</asp:Content>
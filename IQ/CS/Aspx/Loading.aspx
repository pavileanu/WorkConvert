<%@ Page Language="C#" AutoEventWireup="true"  CodeBehind="Loading.aspx.cs" Inherits="IQ.Loading" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Loading iQuote...</title>
    <link href="../Styles/Site.css" rel="stylesheet" type="text/css" />
    <script src="//code.jquery.com/jquery-1.9.1.js" type="text/javascript">
</script>
    <script src="//code.jquery.com/ui/1.10.4/jquery-ui.js" type="text/javascript">
</script>
    
</head>
<body >
    <form id="form1" runat="server">
    <div id="headerBack">
         <div id="headerBar">
        </div>
        <input type="hidden" runat="server" id="hidref" />
        <div style="top:120px;text-align:center;width:100%">
            <font size="18pt">System is loading...</font><br /><br />Please wait, you will be automatically redirected
            <br />
            <br />
            <center>
                <asp:Panel runat="server" ID="progressPanel"  Width="200px" Height="15px" style="border-radius:20px;background-color:#efefef;"><asp:Panel style="border-radius:20px;background-color:#dfdfdf;" Width="0px" Height="15px" runat="server" ID="progressBar" /></asp:Panel>
                <div id="lblStatus" runat="server" style="display:none;height:300px;width:100%;overflow-y:scroll;text-align:center;">
                    <table id="tblStatus" border="0" style=" margin-left:auto;margin-right:auto;" ><tr><th style="width:75px;border:0px;"></th><th style="width:75px;border:0px;"></th><th style="width:700px;border:0px;"></th></tr></table>
                </div>
                <div id="exception" style="background-color:red;width:100%;height:2px;display:none;" >&nbsp;</div>
            </center>
        </div>
        
    </div>

    </form>
    <div style="position:absolute;left:0px;bottom:0px;width:50px;height:50px;" onclick="$('#lblStatus').show();return false;"></div>
<script>
    window.setInterval("update()", 3000);
    function update()
    {
        $.ajax({
            url: "../GetSystemMaintenanceUpdate",
            success: refreshScreen
        });
    }
    var rs = 0;
    function refreshScreen(o)
    {
        if (o.length >= 52) {
            if ($("#hidref").val() != "") {
                window.location.assign($("#hidref").val());
            }
            else {
                window.location.href = "signin.aspx";
            }
            return false;
        }
        $("#progressBar").width((o.length / 50) * $("#progressPanel").width());
        var i = 0;
        o.forEach(function (o) {
            if (i > rs) {
                if (o.Message.indexOf("Exception") != -1) { $("#exception").show(); }
                $("#tblStatus").prepend("<tr><td>" + o.Time + "s</td><td>" + o.HeapSize + "MB</td><td>" + o.Message + "</td></tr>"); rs++;
            } i++;

        });
        
    }
</script>    
</body>
</html>


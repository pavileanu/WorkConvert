<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="SolutionStoreLanding.aspx.vb" Inherits="IQ.SolutionStoreLanding" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Solution Store</title>
    <link href="/Styles/Site.css" rel="stylesheet" type="text/css" />
	<script src="../scripts/jquery-1.11.2.min.js" type="text/javascript"></script>
        <script src="../scripts/jquery-ui-1.11.2.min.js" type="text/javascript"></script> 
</head>
<body style="overflow:hidden;"> 
<div style="width:750px;padding-top:300px;">
    <h2 style="text-align:center;font-family:HP;">Adding to your basket...</h2>
    <form runat="server">
        <asp:ScriptManager ID="scriptManager" runat="server"></asp:ScriptManager>
        <asp:UpdatePanel ID="updatePanel" runat="server" Visible="false"></asp:UpdatePanel>
    </form>
</div>
</body>
</html>

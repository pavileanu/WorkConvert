<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="edit.aspx.vb" Inherits="IQ.edit1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
   <link href="../Styles/Site.css" rel="stylesheet" type="text/css" />
   <link href="../Styles/dan.css" rel="stylesheet" type="text/css" />

    <link href="~/css/start/jquery-ui-1.8.21.custom.css" rel="stylesheet" type="text/css" />
        <script src="../scripts/jquery-1.11.2.min.js" type="text/javascript"></script>
        <script src="../scripts/jquery-ui-1.11.2.min.js" type="text/javascript"></script> 
        <script  src="../scripts/iQuote2-1.0.0.js" type="text/javascript"></script> 
        <script src="../scripts/embed.js" type="text/javascript"></script>
        <title>
    </title>


    <!--Similar to master - and ESSENTIAL-->
    <script type="text/javascript" >
        loginID = '<%=Request.QueryString("lid") %>';
    </script>
    
</head>
<body>
    <form id="form1" runat="server">
    <div style="width:200em;overflow-x:scroll;" >
        <div>
        <asp:DropDownList ID="drpLanguage" runat="server"  AutoPostBack="true"></asp:DropDownList></div>
        <div id="EditPanel"></div>
        <!--<asp:Panel ID="EditPanel" runat="server">
        </asp:Panel> -->
    </div>
    </form>
</body>

    <script type="text/javascript">
        window.onunload = refreshParent;
        function refreshParent() {
            window.opener.location.reload();
        }
</script>

</html>

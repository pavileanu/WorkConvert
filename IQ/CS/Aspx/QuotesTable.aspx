<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="QuotesTable.aspx.cs" Inherits="IQ.QuotesTable" %>

     <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script src="//code.jquery.com/jquery-1.9.1.js" type="text/javascript">
</script>
    <script src="//code.jquery.com/ui/1.10.4/jquery-ui.js" type="text/javascript">
</script>
       <link rel="stylesheet" href="//code.jquery.com/ui/1.10.4/themes/smoothness/jquery-ui.css" />
    <link href="/Styles/Site.css" rel="stylesheet" type="text/css" />
    
</head>
<body>
    <form id="form1" runat="server">
    !begin  
    
       <div id="tabs">
        <ul>
            <li><a href="#DraftPanel">
                <asp:Literal ID="litDraft" runat="server"></asp:Literal></a></li>
            <li><a href="#SavedPanel"><asp:Literal ID="litSaved" runat="server"></asp:Literal></a></li>
        </ul>
           
        

    </form>
</body>
</html>


<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="BasketPost.aspx.vb" Inherits="IQ.BasketPost" EnableViewState="false" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
     <script type ="text/javascript">
        window.onload = function () {
            document.getElementById("form1").submit();
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <input type="hidden" value="<% =xmlString %>" name="configuration" />  
        <input type="hidden" value="<% = sessionID%>" name="LONGSID" />  
        <input type="hidden" value="<% =accountNum%>" name="cAccountNum " />  
        <input type="hidden" value="configuration" name="CONTENT" />  
        <asp:Literal ID="Literal1" runat="server"></asp:Literal>
    </div>
        <asp:Label ID="Label1" runat="server" Text="" Visible="false"></asp:Label>
    </form>
</body>
</html>

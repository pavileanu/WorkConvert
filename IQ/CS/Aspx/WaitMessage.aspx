<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WaitMessage.aspx.cs" Inherits="IQ.WaitMessage" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
     <script type ="text/javascript">
         window.onload = function () {
             window.location.href = "Tree.aspx?lid=<%=lid%>";
         }
    </script>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <p> Please Wait ..........</p>
    </div>
    </form>
</body>
</html>


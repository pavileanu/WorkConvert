<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="editlog.aspx.vb" Inherits="IQ.editlog" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>

    <link href="../Styles/Site.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
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
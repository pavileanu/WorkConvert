<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="AddAccount.aspx.vb" Inherits="IQ.NewAccount" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
   <script type="text/javascript" src="../scripts/iQuote2.js"></script> 
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <asp:Button ID="BtnCancel" runat="server" Text="Cancel" />
    <asp:Label ID="Label5" runat="server" Text="Adding a new customer contact "></asp:Label><br />
    <asp:Label ID="Label6" runat="server" Text="Company"></asp:Label><asp:TextBox ID="TxtCompany" runat="server" Enabled = "false"></asp:TextBox><br />
    <asp:Label ID="Label1" runat="server" Text="Contact Name"></asp:Label><asp:TextBox ID="TxtName" runat="server"></asp:TextBox><br />
    <asp:Label ID="Label2" runat="server" Text="Email"></asp:Label><asp:TextBox ID="TxtEmail" runat="server"></asp:TextBox><br />
    <asp:Label ID="Label3" runat="server" Text="Account Number"></asp:Label><asp:TextBox ID="TxtpriceBand" runat="server"></asp:TextBox><br />

    <asp:Label ID="Label4" runat="server" Text="Send welcome email/login"></asp:Label><br />
    <asp:CheckBox ID="chkWelcome" runat="server"  Checked="True" />
    <br />
        <asp:Button ID="BtnGo" runat="server" Text="Create" />
    
    </div>
    </form>
</body>
</html>

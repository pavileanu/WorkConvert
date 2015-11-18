<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="Regions.aspx.vb" Inherits="IQ.Regions" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:Panel ID="Panel1" runat="server" style="width:400px;height:800px;overflow-y:scroll;">
            <asp:TreeView ID="TreeView1" runat="server">
            </asp:TreeView>
            <br />
            <br />
        </asp:Panel>
        <asp:Panel ID="Panel3" runat="server" style="position:absolute;top:10px;left:420px;width:400px;">
            ID<asp:TextBox ID="TxtID" runat="server" Enabled="False" ToolTip="You can't change this"></asp:TextBox>
            <br />
            Code
            <asp:TextBox ID="TxtCode" runat="server" ToolTip="Short mnemonic for this region (Or ISO country code)"></asp:TextBox>
            <br />
            Name<asp:TextBox ID="TxtName" runat="server" Width="351px"></asp:TextBox>
            Parent
            <asp:TextBox ID="TxtParent" runat="server" ToolTip="Change the parent to move this region into a different one"></asp:TextBox>
            <br />
            <asp:CheckBox ID="ChkIscountry" runat="server" Text="Is a country" />
            <br />
            <asp:CheckBox ID="chkIsPlaceHolder" runat="server" Text="Is a placeholder ('unofficial' region)" />
            <br />
             Geographical Region <asp:DropDownList ID="drpGeoRegions" runat="server"></asp:DropDownList>
            <br />
            Notes<br />
            <asp:TextBox ID="txtNotes" runat="server" Height="99px" Width="358px"></asp:TextBox>
            <asp:Button ID="BtnAddChild" runat="server" Text="Add a Child" ToolTip="Make a new region Inside this one" />
            <asp:Button ID="BtnSave" runat="server" Text="Save" Width="42px" />
        </asp:Panel>
    </div>
        
        <asp:Panel ID="Panel4" runat="server">
        </asp:Panel>
        
    </form>
</body>
</html>

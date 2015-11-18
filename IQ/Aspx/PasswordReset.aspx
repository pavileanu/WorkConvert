<%@ Page Title="" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="PasswordReset.aspx.vb" Inherits="IQ.PasswordReset" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <p>
        <asp:Label ID="LblFailed" runat="server" BackColor="Red" ForeColor="White" 
            Text="Failed" Visible="False"></asp:Label>

    </p>
    
    <asp:Panel ID="Pnl" runat="server">
    </asp:Panel>
</asp:Content>

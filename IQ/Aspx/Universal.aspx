<%@ Page Title="iQuote" ValidateRequest="True" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Universal.aspx.vb" Inherits="IQ.Universal" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
   
    <div>
        <h1>Welcome to iQuote Universal</h1>
    </div>

    <asp:Panel ID="univSubheading" ClientIDMode="Static" runat="server">
        <asp:Label runat="server" ID="univSubtitle" ClientIDMode="Static"></asp:Label>
        <asp:Label runat="server" ID="univInstructions" ClientIDMode="Static"></asp:Label>
    </asp:Panel>

    <asp:Panel ID="panelManufacturer" ClientIDMode="Static" runat="server">
        <asp:Panel ID="panelHPI" ClientIDMode="Static" runat="server">
        </asp:Panel>
        <asp:Panel ID="panelHPE" ClientIDMode="Static" runat="server">
        </asp:Panel>
    </asp:Panel>
    <asp:Panel ID="panelBanner" ClientIDMode="Static" runat="server" Visible="false"  style="margin-top: 30px; width: 670px; line-height:22px; margin-left: 22px;">
        <b>PLEASE NOTE:</b>  The <b>hpiquote.net</b> URL will be retired soon.  Please use the new URLs above to access iQuote Universal for <b>HP Inc.</b> and <b>Hewlett Packard Enterprise</b>.
    </asp:Panel>


</asp:Content>

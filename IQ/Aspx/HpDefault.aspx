<%@ Page Title="" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="HpDefault.aspx.vb" Inherits="IQ.HpDefault" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h1>Register for HP iQuote Universal</h1>
    <div class="divStyle" style ="padding-top: 10px; padding-bottom: 10px;">
        <div>Please select your region and click Register.</div>
    </div>
    <div class="divStyle">
    <asp:ListBox ID="lstcountries" runat="server" Height="400"></asp:ListBox>
    </div>
    <div>
        <asp:Button ID="btnRegister" runat="server" Text="Register" CssClass="hpOrangeButton smallfont" />
    </div>
</asp:Content>

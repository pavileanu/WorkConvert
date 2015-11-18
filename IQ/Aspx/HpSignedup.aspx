<%@ Page Title="" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="HpSignedup.aspx.vb" Inherits="IQ.HpSignedup" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <div class="mainDiv">

        <fieldset style="border: 0px currentColor; border-image: none;">
            <h1>Register for iQuote Universal (<asp:Label ID="labelCountry" runat="server" Text=""></asp:Label>)</h1>

                <div class="HPSignupInfo" 
                    style ="padding-top: 10px; padding-bottom: 10px;">
                   <asp:Literal ID="litRegistered" runat="server"></asp:Literal>
                </div>
            <div class="divStyle">
               <asp:Literal ID="litMsg" runat="server"></asp:Literal>
            </div>

        </fieldset>
    </div>
</asp:Content>

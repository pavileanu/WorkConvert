<%@ Page Title="" Language="C#" AutoEventWireup="true" MasterPageFile="~/Site.Master" CodeBehind="HpSignup.aspx.cs" Inherits="IQ.HpSignup" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <div class="mainDiv">

        <fieldset style="border: 0px currentColor; border-image: none;">
            <h1 id="header" runat="server"></h1>

            <div class="containerStyle" id="accountSettings">
                <div class="divStyle">
                    <div>
                        <asp:Label ID="LabelFullName" runat="server"></asp:Label><span class="HPRequired">*</span>
                    </div>
                    <div>
                        <asp:TextBox ID="txtFullName" runat="server" CssClass="inputStyle" MaxLength="100"></asp:TextBox>
                    </div>
                </div>
                <div class="divStyle">
                    <div>
                        <asp:Label ID="LabelEmailName" runat="server"></asp:Label><span class="HPRequired">*</span>
                    </div>
                    <div>
                        <asp:TextBox ID="txtEmailName" runat="server" CssClass="inputStyle" MaxLength="100"></asp:TextBox>
                    </div>
                </div>
                <div class="divStyle">
                    <div>
                        <asp:Label ID="LabelConfirmEmail" runat="server"></asp:Label><span class="HPRequired">*</span>
                    </div>
                    <div>
                        <asp:TextBox ID="txtConfirmEmail" runat="server" CssClass="inputStyle" MaxLength="100"></asp:TextBox>
                    </div>
                </div>
                <div class="divStyle">
                    <div>
                        <asp:Label ID="LabelCompanyName" runat="server"></asp:Label><span class="HPRequired">*</span>
                    </div>
                    <div>
                        <asp:TextBox ID="txtCompanyName" runat="server" CssClass="inputStyle" MaxLength="50"></asp:TextBox>
                    </div>
                </div>
                <div class="divStyle">
                    <div>
                        <asp:Label ID="LabelUserType" runat="server"></asp:Label><span class="HPRequired">*</span>
                    </div>
                    <div>
                        <asp:DropDownList ID="ddlUserType" runat="server" CssClass="inputStyle" Width="150"></asp:DropDownList>
                    </div>
                </div>
                <div class="divStyle">
                    <div>
                        <asp:Label ID="LabelPostCode" runat="server"></asp:Label><span class="HPRequired">*</span>
                    </div>
                    <div>
                        <asp:TextBox ID="txtPostCode" runat="server" CssClass="inputStyle" MaxLength="16"></asp:TextBox>
                    </div>
                </div>
                <div class="divStyle">
                    <div>
                        <asp:Label ID="LabelTelephone" runat="server"></asp:Label>
                    </div>
                    <div>
                        <asp:TextBox ID="txtTelephone" runat="server" CssClass="inputStyle" MaxLength="50"></asp:TextBox>
                    </div>
                </div>
                <div class="divStyle">
                    <div>
                        <span class="HPRequired">*</span><asp:Label ID="LabelREquiredField" runat="server"></asp:Label>
                    </div>
                </div>
                <div class="subhead">
                    <div>
                        <asp:Literal ID="HeaderTandC" runat="server"></asp:Literal>
                    </div>
                </div>
                <br />
                <div class="divStyle">
                    <div>
                        <asp:Literal ID="litLegal" runat="server"></asp:Literal>
                    </div>
                </div>
                <div class="divStyle">
                    <div>
                        <asp:CheckBox ID="chkAgree" runat="server"/>
                    </div>
                </div>
                <div class="divStyle">
                    <asp:Button ID="BtnSave" runat="server" CssClass="hpOrangeButton smallfont"  OnClick="BtnSave_Click"/>
                    <asp:Button ID="BtnCancel" runat="server" CssClass="hpOrangeButton smallfont"  OnClick="BtnCancel_Click"/>
                </div>
                <div class="HPSignupError">
                   <asp:Literal ID="litError" runat="server"></asp:Literal>
                </div>
                <div class="HPSignupInfo">
                   <asp:Literal ID="litRegistered" runat="server"></asp:Literal>
                </div>
            </div>
            <div class="divStyle">
               <asp:Literal ID="litMsg" runat="server"></asp:Literal>
            </div>

        </fieldset>
    </div>
</asp:Content>


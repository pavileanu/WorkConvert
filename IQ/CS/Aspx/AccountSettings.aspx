<%@ Page Title="Account Settings" Language="C#" AutoEventWireup="true" MasterPageFile="~/Site.Master" CodeBehind="AccountSettings.aspx.cs" Inherits="IQ.AccountSettings" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <div id="tabs">
        <div id="accountSettings" class="mainDiv">
            <h1 runat="server" id="h1HeaderContainer">Account Settings</h1>
            <div id="container" class="containerStyle">
                <fieldset style="border: 0px currentColor; border-image: none;">
                    <div class="divStyle">
                        <asp:Label ID="LblInfo" runat="server" Text="Label"></asp:Label>
                    </div>
                    <div class="divStyle">
                        <div>
                            <asp:Label ID="Label4" runat="server" Text="Full Name"></asp:Label>
                        </div>
                        <div>
                            <asp:TextBox ID="TxtFullName" runat="server" CssClass="inputStyle" MaxLength="100"></asp:TextBox>
                        </div>
                    </div>
                    <div class="divStyle">
                        <div>
                            <asp:Label ID="Label8" runat="server" Text="Email"></asp:Label>
                        </div>
                        <div>
                            <asp:TextBox ID="txtEmail" runat="server" CssClass="inputStyle" MaxLength="100" Enabled="false"></asp:TextBox>
                        </div>
                    </div>
                    <div class="divStyle">
                        <div>
                            <asp:Label ID="Label1" runat="server" Text="New Password"></asp:Label>
                        </div>
                        <div>
                            <asp:TextBox ID="TxtChangePassword" runat="server" TextMode="Password" autocomplete="off" CssClass="inputStyle" MaxLength="100"></asp:TextBox>
                        </div>

                    </div>
                    <div class="divStyle">
                        <div>
                            <asp:Label ID="Label2" runat="server" Text="Confirm New Password"></asp:Label>
                        </div>
                        <div>
                            <asp:TextBox ID="TxtConfirmChangePassword" runat="server" TextMode="Password" autocomplete="off" CssClass="inputStyle" MaxLength="100"></asp:TextBox>
                        </div>
                        <div>
                            <asp:CompareValidator ID="CompareValidator1" Display="Dynamic" runat="server" ControlToCompare="TxtChangePassword" ControlToValidate="TxtConfirmChangePassword" ></asp:CompareValidator>
                            <asp:RequiredFieldValidator ID="requiredPasswordConfirm" runat="server" ControlToValidate="TxtConfirmChangePassword" Enabled="false"></asp:RequiredFieldValidator>
                            <asp:Label ID="lblRegex" runat="server" ForeColor="Red" Text="[Password strength]" Visible="False"></asp:Label>
                        </div>
                    </div>
                    <div class="divStyle">
                        <div>
                            <asp:Label ID="Label3" runat="server" Text="Telephone"></asp:Label>
                        </div>
                        <div>
                            <asp:TextBox ID="TxtTelephone" runat="server" CssClass="inputStyle" MaxLength="50"></asp:TextBox>
                        </div>
                    </div>
                    <div class="divStyle">
                        <div>
                            <asp:Label ID="Label6" runat="server" Text="Language:"></asp:Label>
                        </div>
                        <div>
                            <asp:DropDownList ID="DDLLanguage" runat="server" Height="1.5em" >
                            </asp:DropDownList>
                        </div>
                    </div>
                    <div class="divStyle">
                        <div>
                            <asp:Label ID="Label7" runat="server" Text="Currency and Date Format:"></asp:Label>
                        </div>
                        <div>
                            <asp:DropDownList ID="ddlCulture" runat="server" height ="1.5em">
                            </asp:DropDownList>

                        </div>

                    </div>
                    <div class="divStyle">
                        <div>
                            <asp:Label ID="lblRoles" runat="server" Text="Roles:"></asp:Label>
                        </div>
                        <div>
                            <asp:ListBox DataTextField="Code" Enabled="false" ID="lbRoles" runat="server" CssClass="inputStyle" Height="50px"></asp:ListBox>
                        </div>
                    </div>

                    <div class="divStyle">
                        <asp:CheckBox ID="chkUpdadateAccounts" runat="server" Checked="true" />
                    </div>
                    <div class="divStyle">
                        <div>
                            <asp:Label ID="Label5" runat="server" Text="Host Account Number"></asp:Label>
                        </div>
                        <div>
                            <asp:TextBox ID="TxtpriceBand" runat="server" Enabled="false" CssClass="inputStyle"></asp:TextBox>
                        </div>
                    </div>
                    <div class="divStyle">
                        <asp:Button ID="BtnSave" runat="server" Text="Save" CssClass="hpOrangeButton smallfont"  OnClick="BtnSave_Click"/>
                    </div>


                </fieldset>

            </div>
        </div>
        <!--   <div id="accountPreferences" class="mainDiv">
            <h1>Account Preferences</h1>
             <div runat="server" id="prefcontainer" class="containerStyle">
                <fieldset style="border: 0px currentColor; border-image: none;">
                   
                </fieldset>
             </div>
        </div>-->
    </div>
    <script src="/Scripts/AccountSettings.js" type="text/javascript">
</script>
</asp:Content>


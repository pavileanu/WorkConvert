<%@ Page Title="iQuote" ValidateRequest="True" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="SignIn.aspx.vb" Inherits="IQ.SignIn" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
       
    <asp:Panel ID="panelBanner" ClientIDMode="Static" runat="server" Visible="false">
    </asp:Panel>

    <asp:Panel ID="panel1" runat="server">
    </asp:Panel>
    
    <div>
        <h1>Welcome to iQuote <asp:Label runat="server" ID="labelUniversal"></asp:Label> </h1>
    </div>

    <br />

    <asp:Panel ID="panelSignIn" ClientIDMode="Static" runat="server" style="display: inline-block;" NOBBLEDDefaultButton="btnSignIn">
        <div>
            <h2>Log In</h2>
        </div>
        <asp:Label runat="server" Visible ="false"  ID="lblElevate" Text="Sign in to elevate this session"></asp:Label>

        <div id="logonPanel" class="pwPanel ib" style="height:14em;width:30em;">
            <div style="position:absolute;right:24em;top:2em;">Email</div>
            <asp:TextBox ID="txtEmail" AutoPostBack ="false"  
                MaxLength="100" runat="server"
                prompt="Email" 
                CssClass="textbox" style="position:absolute;left:8.2em;top:2em;width:24em;"></asp:TextBox>
        
            <div style="position:absolute;right:24em;top:6em;">Password</div>        
            <asp:TextBox ID="txtPassword" runat="server" CssClass="textbox"  TextMode="Password" style="position:absolute;left:8.2em; top:7em;width:24em;"></asp:TextBox>
                
            <asp:Label ID="LblFailed" runat="server" cssclass="failed" Text="failedMessage" ></asp:Label>
        
            <asp:Button ID="btnSignIn" CausesValidation ="true" runat="server" Text="Log in" CssClass="hpOrangeButton smallfont" OnClick="btnSignIn_Click" TabIndex="1" style="position:absolute;right:2em;top:14em;"/>
            <asp:Button ID="BtnForgot" runat="server" Text="Forgotten Password" CssClass="hpGreyButton smallfont" style="position:absolute;left:1em;top:14em;" />
        </div>
    </asp:Panel>

    <asp:Panel ID="panelOr" ClientIDMode="Static" runat="server" style="display: inline-block; padding-left:5em; padding-top:10em;" Visible="false">
            <h2>Or</h2>
    </asp:Panel>

    <asp:Panel ID="panelUniversal" ClientIDMode="Static" runat="server" style="display: inline-block; padding-left:5em; padding-bottom:3em;" Visible="false">
        <div>
            <h2 id="subHeading" runat="server">Sign Up</h2>
        </div>

        <div id="registerPanel" class="pwPanel ib" style="height:14em;width:30em; border:1px solid #5A5A5A; margin:2em 0 2em 0;">
            
            <div id="selectHost" runat="server" style="float:left; width:40%; padding:1em;">
                <h3>Please select country</h3>
                <br />
                <asp:ListBox ID="listCountries" runat="server" Height="125" Width="180"></asp:ListBox>
                <asp:Button ID="btnSignInUniversal" runat="server" Text="Log in" CssClass="hpOrangeButton smallfont" style="float:left;"/>
                <asp:Button ID="btnRegister" runat="server" Text="Register" CssClass="hpBlueButton smallfont" style="float:right;"/>
            </div>

            <div style="float:left; width:45%; padding:1em;">
                <h3>Country not listed?</h3>
                <br />
                <div style="background-color:lightgray; height:115px; width:200px; padding: 5px;">
                    Request iQuote be made available in your country!
                <br />
                <asp:DropDownList ID="dropDownOtherCountries" runat="server" Width="199" style="margin-top:6px;"></asp:DropDownList>
                <asp:TextBox ID="txtEmailAddress" runat="server" CssClass="textbox" Width="195" style="margin-top:3px;" placeholder="Your email address"></asp:TextBox>
                <asp:Button ID="btnRequest" runat="server" Text="Request" CssClass="hpGreyButton smallfont" style="float:right;" />

                </div>
                <asp:Label id="requestFeedback" runat="server" style="margin-top:0.5em; display:inline-block;" Visible="false">Your request has been received</asp:Label>
                <asp:Label id="requestNoEmail" runat="server" cssclass="failed" style="margin-top:0.5em; display:inline-block;" Visible="false">Please provide an email address</asp:Label>
            </div>
         
        </div>
        <asp:HiddenField runat="server" ID="hiddenMfrCode" />
    </asp:Panel>

    <div class="teesAndCees">You agree that any information you enter may be collected, stored, accessed, disclosed, transmitted, processed, and otherwise used in accordance with the <asp:HyperLink ID="teesAndCeesLink" Target="_blank" runat="server"> privacy policy</asp:HyperLink>.</div>
    
</asp:Content>

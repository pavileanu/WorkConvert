
<%@ Page Title="" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Signup.aspx.vb" Inherits="IQ.Signup" Debug="true" EnableEventValidation="false" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>


<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <h2>User</h2>
    
    <asp:Label ID="Label12" runat="server" Text="Buyer Company" cssclass="formLabel"></asp:Label>    
    <asp:TextBox ID="txtBuyer" runat="server" CssClass="formBox" 
    onkeyup="suggest('ctl00_MainContent_txtBuyer','ddlBuyer','channels');">
    </asp:TextBox>        
    <asp:TextBox ID="txtBuyerID" runat="server" CssClass="formBox" >    
    </asp:TextBox>        
    
    <br />
    <select id="ddlBuyer" onchange="setTxtsFromDDL('ctl00_MainContent_txtBuyer','ctl00_MainContent_txtBuyerID',this);" onclick="setTxtsFromDDL('ctl00_MainContent_txtBuyer','ctl00_MainContent_txtBuyerID',this);">
            
    </select>
    
    <br/>
    
    <asp:Label ID="Label3" runat="server" Text="Email" cssclass="formLabel"></asp:Label>
    <asp:TextBox ID="TxtEmail" runat="server" cssclass="formBox"></asp:TextBox>
    <asp:Label ID="LblInvalidEmail" runat="server" CssClass="errorLabel" 
        Text="Invalid email address" Visible="False"></asp:Label>
    <asp:Button ID="BtnFindUser" runat="server" Text="Find/Add" />
    <br/>
    
    <asp:Label ID="Label1" runat="server" cssclass="formLabel" Text="Real Name"></asp:Label>
    <asp:TextBox ID="TxtRealName" runat="server" CssClass="formBox"></asp:TextBox>
    
    <br/>
    <asp:Label ID="Label5" runat="server" Text="Telephone 1" cssclass="formLabel"></asp:Label>
    <asp:TextBox ID="TxtTel1" runat="server" CssClass="formBox"></asp:TextBox>

    <br/>
        <asp:Label ID="Label6" runat="server" Text="Telephone 2" cssclass="formLabel"></asp:Label>
    <asp:TextBox ID="txtTel2" runat="server" CssClass="formBox"></asp:TextBox>
    <br/>
    
    <h2>Account</h2>
    <asp:Label ID="Label11" runat="server" Text="Seller" cssclass="formLabel"></asp:Label>
    
    
    <!-- the last parameter to suggest() contains a literal string of additional script to be added the the SELECT lists's tag -->
    <asp:TextBox ID="txtSeller" runat="server"  CssClass="formBox" 
    onkeyup="suggest('ctl00_MainContent_txtSeller','ddlSeller','channels')"    
    />

    <asp:TextBox ID="txtSellerID" runat="server"  CssClass="formBox" />

    <br />    
    <!-- We use a client side select for the pick list as it is difficult and costly to re-populate a server control on postback
    we would need to re-instate its contents to get its value correctly
    it is more efficent to populate a hidden (textbox) control with the value we want to pass back -->

    <select id="ddlSeller"
    onblur="fillTeams('ddlSeller','ddlTeam');" 
    onchange="setTxtsFromDDL('ctl00_MainContent_txtSeller','ctl00_MainContent_txtSellerID',this);"    
    onclick="setTxtsFromDDL('ctl00_MainContent_txtSeller','ctl00_MainContent_txtSellerID',this);">
    </select>
    
    <asp:Label ID="Label13" runat="server" Text="Host Account Number"></asp:Label>
    <asp:TextBox ID="TxtpriceBand" runat="server"></asp:TextBox>
    <asp:TextBox ID="TxtAccountID" runat="server" Enabled="False"></asp:TextBox>
    <br />
    
    <br/>
    
    <asp:Label ID="Label2" runat="server" Text="Password" cssclass="formLabel"></asp:Label>
    <asp:TextBox ID="TxtPassword" runat="server" CssClass="formBox"></asp:TextBox>
    <asp:Label ID="Label4" runat="server" Text="Leave blank to auto-generate" cssclass="FormLabel"></asp:Label>    
    <br />
    
    <asp:Label ID="Label7" runat="server" Text="Role" cssclass="formLabel" ></asp:Label>
    <asp:DropDownList ID="ddlRole" runat="server" CssClass="formBox">
    </asp:DropDownList>    
    <br />
    
    <asp:Label ID="Label8" runat="server" Text="team" cssclass="formLabel"></asp:Label>    
    <div class="formBox ">
    <select id="ddlTeam" 
    onchange="setTxtsFromDDL(null,'ctl00_MainContent_txtTeamID',this);"    
    onclick="setTxtsFromDDL(null,'ctl00_MainContent_txtTeamID',this);"></select>   
    </div>

    <asp:TextBox ID="txtTeamID" runat="server"></asp:TextBox>
    <br />
    
    <asp:Label ID="Label9" runat="server" Text="Language" cssclass="formLabel"></asp:Label>
    <asp:DropDownList ID="ddlLanguage" runat="server" CssClass="formBox">
    </asp:DropDownList> 
    <br />
    
    <asp:Label ID="Label10" runat="server" Text="Currency" cssclass="formLabel"></asp:Label>
    <asp:DropDownList ID="ddlCurrency" runat="server" CssClass="formBox">
    </asp:DropDownList>
    <br />
        
    <asp:Button ID="BtnSignUp" runat="server" Text="Sign Up" style="height: 26px" />

    <script type="text/javascript" src="/scripts/iQuote2-1.0.0.js"/>

    <asp:Label ID="lblError" runat="server" CssClass="errorLabel" Text="Error"></asp:Label>

</asp:Content>


<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="administration.aspx.cs" Inherits="IQ.administration" MasterPageFile="~/Site.Master" EnableEventValidation ="false"%>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="Pnl" runat="server">
    </asp:Panel>

    <asp:Menu ID="adminMenu" runat="server" Orientation="Horizontal" OnMenuItemClick="AdminMenu_MenuItemClick" OnMenuItemClick="AdminMenu_MenuItemClick">
        <LevelMenuItemStyles>
            <asp:MenuItemStyle CssClass="admin_tabs"/>
        </LevelMenuItemStyles>
        <StaticSelectedStyle CssClass="admin_tab_selected" />
        <Items>
            <asp:MenuItem Value="UserAdmin" Text="User Administration" Selected="true"></asp:MenuItem>
            <asp:MenuItem Value="CreateUser" Text ="Create User"></asp:MenuItem>
            <asp:MenuItem Value="System" Text="System"></asp:MenuItem>
            <asp:MenuItem Value="Reports" Text="Reports"></asp:MenuItem>
        </Items>
    </asp:Menu>
    <div id="adminTabsLine" runat="server" class="admin_tabs_line"></div> 
    <asp:MultiView ID="adminMultiView" runat="server" ActiveViewIndex="0">
        <asp:View ID="tabUserAdmin" runat="server">
            <br />
            <h3 style="padding-bottom:10px; font-size:larger;">User Administration</h3>
                <div id="adminListUsers" style="font-size: 0.8em">
                <div style="float: left; padding-right: 30px;">
                    <div id="userFilterPanel">
                        <span style="padding-right: 10px;">Search</span>
                        <asp:TextBox runat="server" id="txtFilter"></asp:TextBox><asp:Button Text="Search" runat="server" ID="btnSearch" CssClass="hpOrangeButton smallfont" style="margin-left: 10px;" OnClick="btnSearch_Click"  />
                        <asp:CheckBox  runat="server" id="chkonlyDistiAdmin" Text="Admin Only" ></asp:CheckBox>
                    </div> 
                    <asp:GridView ID="grdUser"  runat="server" AutoGenerateColumns="False" AllowPaging="True" OnPageIndexChanging="grdUser_PageIndexChanging" PageSize="50" DataKeyNames="ID" gridLines="None" HeaderStyle-CssClass="usrGridHeader" RowStyle-CssClass="usrGridRow">
                        <AlternatingRowStyle BackColor="#FFFFFF" ForeColor="#284775" />
                        <Columns>
                            <asp:BoundField DataField="Email" HeaderText="Email" />
                            <asp:BoundField DataField="RealName" HeaderText="Name" />
                            <asp:BoundField DataField="ChannelName" HeaderText="Company" />
                            <asp:BoundField DataField="DistiAdmin" HeaderText="Admin" Visible="false" />
                            <asp:BoundField DataField="LastUsed" HeaderText="Last Used" />
                            <asp:TemplateField HeaderText="Disabled" ItemStyle-HorizontalAlign="Center">
                                <ItemTemplate>

                                    <asp:CheckBox ID="chkDisabled" Enabled="<%#AccountCanDisableUsers%>" runat="server" Checked='<%# Bind("Disabled") %>' OnCheckedChanged="chkDisabled_CheckedChanged" AutoPostBack="true" />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Password">
                                <ItemTemplate>
                                    <asp:button ID="btnPasswordReset" CssClass="hpGreyButton smallfont" Enabled='<%# AccountCanResetPasswords %>' runat="server" Text="Reset" onClick="btnPasswordReset_Click"  AutoPostBack="true" />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Welcome">
                                <ItemTemplate>
                                    <asp:button ID="btnWelcomeResend" CssClass="hpGreyButton smallfont" Enabled='<%# AccountCanResetPasswords %>' runat="server" Text="Send" onClick="btnWelcomeResend_Click"  AutoPostBack="true" />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Role" >
                                <ItemTemplate >
                                    <a visible="<%#AccountIsDistiAdmin%>" onclick='<%# Eval("RoleFunction")%>' href="#"><%#Eval("HighestRole")%></a>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField Visible ="true">
                                <ItemTemplate>
                                    <div class="rolefloater" id="rolefloater<%#eval("id").tostring %>" style="padding:3px;width:250px;text-align:center;background-color:white;border:3px solid #C0C0C0;">
                                        <button class="hpBlueButton" style="float:right;top:-0px;" onclick="$('#<%# "rolefloater" & Eval("Id").ToString%>').hide();return false;">X</button>
                                        <h2 style="color:#C0C0C0;text-align:center;"><%#Eval("RealName")%></h2><br />
                                        <span style="float:left; padding-left:25px;">Current Roles</span>
                                        <asp:ListBox CssClass="inputStyle lbinput" Width="200px" Rows="5" ID="lbRoles" DataTextField="EnglishDisplayName" DataValueField="Code" DataSource='<%# Bind("Roles")%>' runat="server"></asp:ListBox><br />
                                        <div style="text-align:center; padding-bottom:10px;">
                                            <asp:Button Text="&#8593;" runat="server" onclick="btnAddRole_Click"  CssClass="hpBlueButton arrowButton" />
                                            <asp:Button UseSubmitBehavior="true"  runat="server" CssClass="hpBlueButton arrowButton" Text="&#8595;" onclick="btnRemoveRole_Click" Enabled="true" />
                                        </div>
                                        <asp:ListBox CssClass="inputStyle lbinput" Width="200px" Rows="5" ID="lbAvailableRoles" DataTextField="EnglishDisplayName" DataValueField="Code" DataSource='<%# Bind("AvailableRoles")%>' runat="server"></asp:ListBox><br /><br />
                                    </div>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <EditRowStyle BackColor="#999999" />
                        <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                        <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                        <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                        <RowStyle BackColor="#E6E5E2" ForeColor="#333333" />
                        <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                        <SortedAscendingCellStyle BackColor="#E9E7E2" />
                        <SortedAscendingHeaderStyle BackColor="#506C8C" />
                        <SortedDescendingCellStyle BackColor="#FFFDF8" />
                        <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                    </asp:GridView>
                </div>
            </div>
            <div style="clear:both">
            </div>
        </asp:View>
        <asp:View ID="tabCreateUser" runat="server">
            <br />
            <h3 style="padding-bottom:10px; font-size:larger;">Create User</h3>
            <div>
                <div id="adminCreateUser" style="clear:both">
                    <fieldset>
                        <asp:TextBox Visible ="false" Text="" ID="txtAccountId" runat="server" />
                        <div class="divStyle">
                            <div>
                                <div>
                                    <asp:Label ID="lblChannelSelect" runat="server" Text="Channel"></asp:Label>
                                </div>
                                <div>
                                    <asp:DropDownList  CssClass="inputStyle" DataTextField ="CompoundDisplayName" DataValueField="ID" AutoPostBack ="true"  OnSelectedIndexChanged="ddlChannels_SelectedIndexChanged" enabled='<%#AccountCanSetupUsers%>' ID="ddlChannels" runat="server"></asp:DropDownList>
                                </div>
                            </div>
                        </div>
                        <br />
                        <div class="divStyle" style="padding-bottom:40px;">
                            <span style="float:left;">
                                <asp:Label ID="Label1" runat="server" Text="Full Name" Width="190px"></asp:Label>
                                <span style="margin-left:35px;"><asp:Label ID="Label4" runat="server" Text="Email"></asp:Label></span>
                                <br />
                                <asp:TextBox ID="txtFullName" runat="server" enabled='<%#AccountCanSetupUsers%>'  CssClass="inputStyle" Width="190px"></asp:TextBox>
                                <span style="margin-left:30px;"><asp:TextBox ID="txtEmailName" enabled='<%#AccountCanSetupUsers%>'  runat="server" CssClass="inputStyle" ></asp:TextBox></span>
                                &nbsp;&nbsp;@&nbsp;
                                <asp:DropDownList ID="drpDomain" runat="server"></asp:DropDownList>
                            </span>
                        </div>
                        <br />
                        <div class="divStyle" style="padding-bottom:20px;">
                                <asp:Label ID="Label3" runat="server" enabled='<%#AccountCanSetupUsers%>' Text="Telephone" Width="190px"></asp:Label>
                                <span style="margin-left:35px;"><asp:Label ID="Label8" runat="server" Text="Team"></asp:Label></span>
                                <br />
                                  <asp:TextBox ID="TxtTelephone" runat="server" CssClass="inputStyle" Width="190px"></asp:TextBox>
                                <span style="margin-left:30px;"><asp:DropDownList  CssClass="inputStyle" enabled='<%#AccountCanSetupUsers%>' ID="drpTeams" runat="server" Width="170px"></asp:DropDownList></span>
                        </div>
                        <div class="divStyle" id="divCurrency" runat="server" style="padding-bottom:20px;">
                            <div>
                                <asp:Label ID="lblCurrency" runat="server" enabled='<%#AccountCanSetupUsers%>'  Text="Currency"></asp:Label>
                            </div>
                            <div>
                                <asp:DropDownList ID="drpCurrency" runat="server"></asp:DropDownList>
                            </div>   
                        </div>
                        <div class="divStyle" style="padding-bottom:20px;">
                                <asp:label id="lblRoles" runat="server">Roles</asp:label><br />
                                <asp:ListBox ID="lbRoles" enabled='<%#AccountCanSetupUsers%>' SelectionMode ="Multiple" runat="server" Width="190px" Rows="5"  />
                        </div>
                        <div class="divStyle" style="padding-bottom:20px;">
                            <div>
                                <asp:CheckBox ID="chkAdminUser" enabled='<%#IsGlobalAdmin%>'  runat="server" Text=" Admin User" visible="false" />
                            </div>
                            <div>
                                <asp:RadioButton ID="chkEmailUser" enabled='<%#AccountCanSetupUsers%>' runat="server" Text="  Email to user" Checked="True" GroupName="email"/>
                            </div>
                            <div>
                                <asp:RadioButton ID="chkEmailAdmin" enabled='<%#AccountCanSetupUsers%>' runat="server" Text="  Email to administrator" GroupName="email" />
                                <br />
                            </div>
                            <%-
-System.Xml.Linq.XElement.Parse("<asp:Panel ID=\"PnlHostOverride\" runat=\"server\" Visible =\"false\">"++"Host Override <asp:TextBox ID=\"txtHostOverride\" runat=\"server\"></asp:TextBox> (create user/account at this host)"+"</asp:Panel>")--;
%>               
                        </div>
                        <div class="divStyle">
                            <asp:Button ID="BtnSave" runat="server" enabled='<%#AccountCanSetupUsers% OnClick="BtnSave_Click">'  Text="Save" CssClass="hpOrangeButton smallfont" />
                        </div>
                    </fieldset>
                </div>
            </div>
            <br />
            <asp:Panel ID="Pnl2" runat="server">
            </asp:Panel>
            <asp:Panel ID="PnlMultiSend" CssClass="multiBox" runat="server" Visible="false">
                Multiple Welcome (resets passwords) <br />
                <asp:Label ID="Label10" runat="server" Text="For host:"></asp:Label>
                <asp:TextBox ID="txtMultiHost" CssClass="inputStyle" runat="server"></asp:TextBox>
                <asp:Button ID="btnGetStubs" CssClass="hpBlueButton smallfont" runat="server" Text="Get Internals"  OnClick="btnGetStubs_Click"/><br /><br />
                List of emails to send:<asp:TextBox ID="TxtMultisend" cssclass="inputStyle " runat="server" Width="676px"></asp:TextBox>
                <asp:Button CssClass="hpBlueButton smallfont" ID="BtnMultisend" runat="server" Text="Send Mails"  OnClick="BtnMultisend_Click"/>
                <asp:CheckBox ID="chkMultiDoit" runat="server" Text="Actually Send mails" />
            </asp:Panel>
        </asp:View>
        <asp:View ID="tabSystem" runat="server">
            <br />
            <h3 style="padding-bottom:10px; font-size:larger;">System</h3>
            <asp:Panel ID="panelSignInMessage" CssClass="multiBox" runat="server" Visible="False">
                Sign In System Message
                <br />
                <asp:TextBox ID="txtSignInSystemMessage" CssClass="inputStyle" TextMode="MultiLine" style="height:50px; width:676px;" ValidateRequestMode="Disabled" runat="server"></asp:TextBox>
                <br />
                <asp:Label runat="server" CssClass="inputStyle" Text="Show From: "/><asp:TextBox ID="txtSystemMessageValidFrom" CssClass="inputStyle" ClientIDMode="Static" style="width:80px; margin-right:10px;" onkeydown="return false;" runat="server"></asp:TextBox>
                <asp:Label runat="server" CssClass="inputStyle" Text="Until: "/><asp:TextBox ID="txtSystemMessageValidTo" CssClass="inputStyle" ClientIDMode="Static" style="width:80px;" onkeydown="return false;" runat="server"></asp:TextBox>
                <asp:CheckBox ID="chkSystemMessage" Text=" Enabled" runat="server" AutoPostBack="true" OnCheckedChanged="chkSystemMessage_CheckedChanged" style="padding-left:10px; padding-right:20px;"/>
                <asp:Button CssClass="hpBlueButton smallfont" ID="btnAddSystemMessage" runat="server" Text="Save" Visible="False" OnClick="btnAddSystemMessage_Click"/>
                <asp:Button CssClass="hpBlueButton smallfont" ID="btnAmendSystemMessage" runat="server" Text="Save" Visible="False" OnClick="btnAmendSystemMessage_Click"/>
                <asp:Button CssClass="hpBlueButton smallfont" ID="btnDeleteSystemMessage" runat="server" Text="Delete" Visible="False" OnClick="btnDeleteSystemMessage_Click"/>
            </asp:Panel>

            <br /><br /><br />
            <asp:Panel ID="panelHpeSystemMessage" CssClass="multiBox" runat="server" Visible="False">
                HPE System Message
                <br />
                <asp:TextBox ID="txtHpeSystemMessage" CssClass="inputStyle" TextMode="MultiLine" style="height:50px; width:676px;" ValidateRequestMode="Disabled" runat="server"></asp:TextBox>
                <br />
                <asp:Label runat="server" CssClass="inputStyle" Text="Show From: "/><asp:TextBox ID="txtHpeSystemMessageValidFrom" CssClass="inputStyle" ClientIDMode="Static" style="width:80px; margin-right:10px;" onkeydown="return false;" runat="server"></asp:TextBox>
                <asp:Label runat="server" CssClass="inputStyle" Text="Until: "/><asp:TextBox ID="txtHpeSystemMessageValidTo" CssClass="inputStyle" ClientIDMode="Static" style="width:80px;" onkeydown="return false;" runat="server"></asp:TextBox>
                <asp:CheckBox ID="hpeMessageEnabled" Text=" Enabled" runat="server" AutoPostBack="true" OnCheckedChanged="hpeMessageEnabled_CheckedChanged" style="padding-left:10px; padding-right:20px;"/>
                <asp:Button CssClass="hpBlueButton smallfont" ID="btnAddHpeSystemMessage" runat="server" Text="Save" Visible="False" OnClick="btnAddHpeSystemMessage_Click"/>
                <asp:Button CssClass="hpBlueButton smallfont" ID="btnAmendHpeSystemMessage" runat="server" Text="Save" Visible="False" OnClick="btnAmendHpeSystemMessage_Click"/>
                <asp:Button CssClass="hpBlueButton smallfont" ID="btnDeleteHpeSystemMessage" runat="server" Text="Delete" Visible="False" OnClick="btnDeleteHpeSystemMessage_Click"/>
            </asp:Panel>

            <br /><br /><br />
            <asp:Panel ID="panelHpiSystemMessage" CssClass="multiBox" runat="server" Visible="False">
                HPI System Message
                <br />
                <asp:TextBox ID="txtHpiSystemMessage" CssClass="inputStyle" TextMode="MultiLine" style="height:50px; width:676px;" ValidateRequestMode="Disabled" runat="server"></asp:TextBox>
                <br />
                <asp:Label runat="server" CssClass="inputStyle" Text="Show From: "/><asp:TextBox ID="txtHpiSystemMessageValidFrom" CssClass="inputStyle" ClientIDMode="Static" style="width:80px; margin-right:10px;" onkeydown="return false;" runat="server"></asp:TextBox>
                <asp:Label runat="server" CssClass="inputStyle" Text="Until: "/><asp:TextBox ID="txtHpiSystemMessageValidTo" CssClass="inputStyle" ClientIDMode="Static" style="width:80px;" onkeydown="return false;" runat="server"></asp:TextBox>
                <asp:CheckBox ID="hpiMessageEnabled" Text=" Enabled" runat="server" AutoPostBack="true" OnCheckedChanged="HpiMessageEnabled_CheckedChanged" style="padding-left:10px; padding-right:20px;"/>
                <asp:Button CssClass="hpBlueButton smallfont" ID="btnAddHpiSystemMessage" runat="server" Text="Save" Visible="False" OnClick="btnAddHpiSystemMessage_Click"/>
                <asp:Button CssClass="hpBlueButton smallfont" ID="btnAmendHpiSystemMessage" runat="server" Text="Save" Visible="False" OnClick="btnAmendHpiSystemMessage_Click"/>
                <asp:Button CssClass="hpBlueButton smallfont" ID="btnDeleteHpiSystemMessage" runat="server" Text="Delete" Visible="False" OnClick="btnDeleteHpiSystemMessage_Click"/>
            </asp:Panel>
            <br /><br />
        </asp:View>
        <asp:View ID="tabReports" runat="server">
            <br />
            <h3 style="padding-bottom:10px; font-size:larger;">Reports</h3>
            <!-- TODO -->
        </asp:View>
    </asp:MultiView>

    <script>
        $(".rolefloater").hide();
        function showRoles(divid)
        {
            $(".rolefloater").hide();
            $("#" + divid).css("position", "absolute");
            $("#" + divid).css("z-index", "1");
            $("#" + divid).show();
        }
    </script>

    <script>
        $(function () {

            // Set up date pickers on the system message date range controls

            $("#<%=txtSystemMessageValidFrom.ClientID%>").attr('readonly', true);
            $("#<%=txtSystemMessageValidTo.ClientID%>").attr('readonly', true);
            $("#<%=txtHpeSystemMessageValidFrom.ClientID%>").attr('readonly', true);
            $("#<%=txtHpeSystemMessageValidTo.ClientID%>").attr('readonly', true);
            $("#<%=txtHpiSystemMessageValidFrom.ClientID%>").attr('readonly', true);
            $("#<%=txtHpiSystemMessageValidTo.ClientID%>").attr('readonly', true);

            $("#<%=txtSystemMessageValidFrom.ClientID%>").datepicker({ dateFormat: "dd-M-yy" }).val();
            $("#<%=txtSystemMessageValidTo.ClientID%>").datepicker({ dateFormat: "dd-M-yy" }).val();
            $("#<%=txtHpeSystemMessageValidFrom.ClientID%>").datepicker({ dateFormat: "dd-M-yy" }).val();
            $("#<%=txtHpeSystemMessageValidTo.ClientID%>").datepicker({ dateFormat: "dd-M-yy" }).val();
            $("#<%=txtHpiSystemMessageValidFrom.ClientID%>").datepicker({ dateFormat: "dd-M-yy" }).val();
            $("#<%=txtHpiSystemMessageValidTo.ClientID%>").datepicker({ dateFormat: "dd-M-yy" }).val();
        });
  </script>


    </asp:Content>


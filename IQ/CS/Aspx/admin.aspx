<%@ Page Title="" Language="C#" AutoEventWireup="true" MasterPageFile="~/Site.Master" CodeBehind="admin.aspx.cs" Inherits="IQ.WebForm1"  EnableEventValidation ="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .formfields {
            float: left;
            width: 100px;
            text-align: right;
            padding-right: 10px;
        }

        .divStyle {
            margin: 10px;
            padding-bottom: 20px;
            width: 400px;
        }

        .inputStyle {
            border-radius: 5px;
            padding: 4px 8px;
            width: 236px;
            height: 15px;
        }

        .inputStyleSmall {
            border-radius: 5px;
            padding: 4px 8px;
            width: 100px;
            height: 15px;
        }

        .containerStyle {
            margin: 10px 0px;
            padding: 1.4em;
            border: 1px solid silver;
            border-image: none;
            width: 25em;
            background-color: rgb(239, 239, 239);
        }

        .mainDiv {
            padding-left: 40px;
        }
    </style>
    <asp:Panel ID="Pnl" runat="server">
    </asp:Panel>
    <asp:Panel ID="PnlMultiSend" runat="server" Visible="False">
        Multiple Welcome (resets passwords) 
        
        <asp:Label ID="Label10" runat="server" Text="For host:-"></asp:Label>
        <asp:TextBox ID="txtMultiHost" runat="server"></asp:TextBox>
        <asp:Button ID="btnGetStubs" runat="server" Text="Get Internals"  OnClick="btnGetStubs_Click"/>
                <asp:TextBox ID="TxtMultisend" runat="server" Width="676px"></asp:TextBox>
        <br />
        <asp:Button ID="BtnMultisend" runat="server" Text="Send Mails"  OnClick="BtnMultisend_Click"/>
                <asp:CheckBox ID="chkMultiDoit" runat="server" Text="Actually Send mails" />
        
    </asp:Panel>
    <div style="padding-left: 50px; display: inline-block; font-size: 0.8em">
        <div style="float: left; padding-right: 30px;">
            <asp:GridView ID="grdUser"  runat="server" AutoGenerateColumns="False" AllowPaging="True" OnPageIndexChanging="grdUser_PageIndexChanging" PageSize="50" DataKeyNames="ID" CellPadding="4" ForeColor="#333333" GridLines="None">
                <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                <Columns>
                    <asp:BoundField DataField="RealName" HeaderText="Name" />
                    <asp:BoundField DataField="ChannelName" HeaderText="Channel" />
                    <asp:BoundField DataField="Quotes" HeaderText="Quotes" />
                    <asp:BoundField DataField="Options" HeaderText="Options" />
                    <asp:BoundField DataField="Systems" HeaderText="Systems" />
                    <asp:BoundField DataField="Pitch" HeaderText="Pitch" />
                    <asp:BoundField DataField="LastUsed" HeaderText="Last Used" />
                    <asp:TemplateField HeaderText="Disabled">
                        <ItemTemplate>
                            <asp:CheckBox ID="chkDisabled" Enabled='<%=AccountCanDisableUsers.tostring%>' runat="server" Checked='<%=Bind("Disabled")%>' OnCheckedChanged="chkDisabled_CheckedChanged1" AutoPostBack="true" />
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Password">
                        <ItemTemplate>
                            <asp:button ID="btnPasswordReset" Enabled='<%=AccountCanResetPasswords.tostring%>' runat="server" Text="Reset" onClick="btnPasswordReset_Click"  AutoPostBack="true" />
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Roles" >
                        <ItemTemplate >
                            <button visible='<%=AccountIsDistiAdmin.ToString()%>' onclick="showRoles('<%="rolefloater" + Eval("Id").ToString()%>');return false;">Change</button>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField Visible ="true">
                        <ItemTemplate>
                            <div class="rolefloater" id="rolefloater<%=Eval("id").ToString()%>" style="padding:3px;width:500px;">
                                <h2>Roles</h2>
                                <button style="float:right;top:0px;" onclick="$('#<%="rolefloater" + Eval("Id").ToString()%>').hide();return false;">X</button>
                                <asp:ListBox Width="200" ID="lbRoles" DataTextField="Code" DataSource='<%=Bind("Roles")%>' runat="server"></asp:ListBox>
                                <asp:Button Text="<" runat="server" onclick="btnAddRole_Click"  />
                                <asp:Button UseSubmitBehavior="true"  runat="server" Text=">" onclick="btnRemoveRole_Click" Enabled="true" />
                                <asp:ListBox Width="200" DataTextField="Code" DataSource="<%=AvailableRoles%>" runat="server"></asp:ListBox>
                                
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
                <EditRowStyle BackColor="#999999" />
                <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                <RowStyle BackColor="#F7F6F3" ForeColor="#333333" />
                <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                <SortedAscendingCellStyle BackColor="#E9E7E2" />
                <SortedAscendingHeaderStyle BackColor="#506C8C" />
                <SortedDescendingCellStyle BackColor="#FFFDF8" />
                <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
            </asp:GridView>
        </div>
        
        <div style ="float:left;">
            <div>
                <asp:GridView ID="grdActivity" runat="server" AutoGenerateColumns="False">
                    <Columns>
                        <asp:BoundField DataField="Type" />
                        <asp:BoundField DataField="Today" HeaderText="Today" />
                        <asp:BoundField DataField="7Days" HeaderText="7 Days" />
                        <asp:BoundField DataField="MTD" HeaderText="MTD" />
                        <asp:BoundField DataField="LastMonth" HeaderText="Last Month" />
                    </Columns>
                </asp:GridView>
            </div>
            <div style ="padding-top : 20px;">

                <fieldset style="border: 0px currentColor; border-image: none;">
                    <legend>New User</legend>
                    <div class="divStyle">
                        <div>
                            <asp:Label ID="Label8" runat="server" Text="Team"></asp:Label>
                            <asp:DropDownList  enabled='<%=AccountCanSetupUsers.tostring%>' ID="drpTeams" runat="server"></asp:DropDownList>
                        </div>
                    </div>
                    <div class="divStyle">
                        <div>
                            <asp:Label ID="Label4" runat="server" Text="Email"></asp:Label>
                        </div>
                        <div>
                            <asp:TextBox ID="txtEmailName" enabled='<%=AccountCanSetupUsers.tostring%>'  runat="server" CssClass="inputStyle"></asp:TextBox>                         
                        <!--<asp:DropDownList ID="drpDomain" runat="server"></asp:DropDownList> -->
                        </div>
                    </div>
                    <div class="divStyle">
                        <div>
                            <asp:Label ID="Label1" runat="server" Text="Full Name"></asp:Label>
                        </div>
                        <div>
                            <asp:TextBox ID="txtFullName" runat="server" enabled='<%=AccountCanSetupUsers.tostring%>'  CssClass="inputStyle"></asp:TextBox>
                        </div>

                    </div>
                    <div class="divStyle">
                        <div>
                            <asp:Label ID="Label3" runat="server" enabled='<%=AccountCanSetupUsers.tostring%>'  Text="Telephone"></asp:Label>
                        </div>
                        <div>
                            <asp:TextBox ID="TxtTelephone" runat="server" CssClass="inputStyle"></asp:TextBox>
                        </div>
                    </div>
                     <div class="divStyle">
                      <div>
                            <asp:Label ID="Label2" runat="server" enabled='<%=AccountCanSetupUsers.tostring%>'  Text="Currency"></asp:Label>
                        </div>
                           <div>
                              <asp:DropDownList ID="drpCurrency" runat="server"></asp:DropDownList>
                        </div>   
                     </div>
                    <div class="divStyle">
                        <div>
                            <asp:CheckBox ID="chkAdminUser" enabled='<%=AccountCanSetupUsers.tostring%>'  runat="server" Text=" Admin User" />
                        </div>
                        <div>
                            <asp:CheckBox ID="chkEmailUser" enabled='<%=AccountCanSetupUsers.tostring%>'  runat="server" Text="  Email to User" />
                        </div>
                        <div>
                            <asp:CheckBox ID="chkEmailAdmin" enabled='<%=AccountCanSetupUsers.tostring%>'  runat="server" Text="  Email to admin" />
                            <br />
                        </div>
    
                        <asp:Panel ID="PnlHostOverride" runat="server" Visible ="false">
                    
                            Host Override <asp:TextBox ID="txtHostOverride" runat="server"></asp:TextBox> (create user/account at this host)
                            </asp:Panel>                

                        </div>
                    

                    <div class="divStyle">
                        <asp:Button ID="BtnSave" runat="server" enabled='<%=AccountCanSetupUsers.tostring% OnClick="BtnSave_Click">'  Text="Save" CssClass="hpOrangeButton smallfont" />
                      
                    </div>
                    <div class="divStyle">
                    </div>

                </fieldset>
            </div>
        </div>
    </div>
    <script>
        $(".rolefloater").hide();
        function showRoles(divid)
        {
            $(".rolefloater").hide();
            $("#" + divid).css("position", "absolute")
            $("#" + divid).css("border", "3px solid grey")
            $("#" + divid).css("background-color", "white")
            $("#" + divid).css("z-index", "1")
            $("#" + divid).css("width", 300)
            $("#" + divid).show();
        }
    </script>
    
</asp:Content>


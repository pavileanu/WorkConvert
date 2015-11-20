<%@ Page Language="C#" AutoEventWireup="true"  MasterPageFile="~/Site.Master" CodeBehind="ValidationManager.aspx.cs" Inherits="IQ.ValidationManager" %>


<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
   <link rel="stylesheet" href="Resources/styles/jqx.base.css" type="text/css" />
    <link rel="stylesheet" href="Resources/styles/jqx.classic.css" type="text/css" />
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
     
    <script type="text/javascript" src="Resources/jqxcore.js">
</script>
    <script type="text/javascript" src="Resources/jqxbuttons.js">
</script>
    <script type="text/javascript" src="Resources/jqxscrollbar.js">
</script>
    <script type="text/javascript" src="Resources/jqxmenu.js">
</script>
    <script type="text/javascript" src="Resources/jqxgrid.js">
</script>
    <script type="text/javascript" src="Resources/jqxdata.js">
</script>
    <div>
        <asp:DropDownList runat="server" id="ddSysTypes" ClientIDMode="Static"  AutoPostBack ="true"></asp:DropDownList>    
        <asp:DataGrid OnEditCommand="dgProdVals_EditCommand"  ClientIDMode="Static"  OnItemDataBound ="dgProdVals_DataBinding"  OnItemCommand="dgProdVals_ItemCommand" runat="server" ID="dgProdVals" AutoGenerateColumns="False" >
            <Columns>
                <asp:BoundColumn DataField="Id" Visible="true"  />
                <asp:BoundColumn DataField="RequiredOptType" />
                <asp:TemplateColumn HeaderText ="Validation Type">
                    <ItemTemplate>
                        <asp:DropDownList id="ddsProdValType" runat="server" />
                    </ItemTemplate>
                </asp:TemplateColumn>
                <asp:BoundColumn DataField="RequiredQuantity" />
                <asp:BoundColumn DataField="DependantOptType" />
                <asp:BoundColumn DataField="CheckAttribute" />
                <asp:BoundColumn DataField="MessageText" ItemStyle-Width ="500"  />
                <asp:BoundColumn DataField="CorrectMessageText" ItemStyle-Width ="500"  />
                <asp:TemplateColumn HeaderText ="Severity">
                    <ItemTemplate>
                        <asp:DropDownList id="ddsSeverity" runat="server" />
                    </ItemTemplate>
                </asp:TemplateColumn>
               <asp:ButtonColumn CommandName="Follow" Text ="E"></asp:ButtonColumn>
               <asp:ButtonColumn CommandName="Delete" Text ="D"></asp:ButtonColumn>
            </Columns>
        </asp:DataGrid>
        <asp:ObjectDataSource ID="ObjectDataSource1" runat="server"></asp:ObjectDataSource>
         <asp:Button OnClick ="btnAdd_Click"  ID="btnAdd"  UseSubmitBehavior ="true" runat="server" Text="Save"  />
    </div>
    <script>

        /*
        var vals;
        $("#ddSysTypes").change(function (f) {
            $.ajax({
                url: "../../Admin/GetProductValidations",
                data: JSON.stringify({ lid: loginID, SysType: 'servers' }),
                type: "POST",
                contentType: "application/JSON",
                success: GetProductValidationsSuccess,
                headers: { "lid": loginID }
            });
        });
    */

        function GetProductValidationsSuccess(e) {
            $.each(e,function (e,d) {
                $("#jqxgrid").append("<tr><td><input value='" + d.RequiredOptType + "'/></td><td><input value='" + validationTypes[d.ValidationType] + "'/></td><td><select </tr>");
            });
        };
        var validationTypes = new Array();
        validationTypes = { 4: "Quantity" };
    </script>
</asp:Content>

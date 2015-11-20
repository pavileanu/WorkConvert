<%@ Page Title="" Language="C#" AutoEventWireup="true" MasterPageFile="~/Site.Master" CodeBehind="ListQuotes.aspx.cs" Inherits="IQ.Listquotes" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <!-- <asp:CheckBox ID="ChkOnlyLatest" runat="server" />-->
    <script type="text/javascript">
        function popDialog(divID,quoteNo) {
         
            $("#" + divID).dialog({
                resizable: false,
                height: 350,
                width: 1000,
                modal: true,
                title: quoteNo,
                position: {  my:"center" }
            });

            $("#" + divID).css({ 'font-size': "small;" });
        }

        $("#targetDiv").dialog({  //create dialog, but keep it closed
            height: 300,
            width: 350,
            modal: true
        });

        function showDialog(url) {
            $("#targetDiv").dialog({
                autoOpen: false,//create dialog, but keep it closed
                height: 300,
                width: 350,
                modal: true
            });
            //load content and open dialog
            $("#targetDiv").load(url);
            $("#targetDiv").dialog("open");
        }

    </script>
    <asp:Panel ID="ListQuotes" runat="server">
    </asp:Panel>

        <div id="ListOfQuotes" style="font-size : small; min-width: 1000px;">
        </div>

    <div id ="targetDiv"></div>

    <script type="text/javascript">
        showQuotes('') // Ajaxes in  the list of quotes - using the 'suck() function to call QuotesTable.aspx    
    </script>


</asp:Content>


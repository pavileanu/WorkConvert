<%@ Page Title="" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Resources.aspx.vb" Inherits="IQ.Resources" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <script type="text/javascript">
        function displayData(url)
        {
           rExec(url, output);
            
        }
        function output( data)
        {
           
            $("#WhiteBoard").html(data);
        }

    </script>
    <style type ="text/css">
        h1 { font-size :200%;}
        h2{ font-size :125%;}
    </style>
    <div style ="display:inline-block">
        <h1 style="margin: 1em 0 0 0;"><asp:Label runat="server" ID="titleLabel">HP iQuote Resources</asp:Label></h1>
        <div id="ResourceMenu">
            <asp:Literal ID="litMenu" runat="server"></asp:Literal>
        </div>
        <div id="DisplayContent" >
            <div id="WhiteBoard">
                <asp:Literal ID="litContent" runat="server"></asp:Literal>
            </div>
        </div>
    </div>
</asp:Content>

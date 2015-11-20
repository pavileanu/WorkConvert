<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ExportedQuotes.aspx.cs" Inherits="IQ.ExportedQuotes" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>

<body>
    <form id="form1" runat="server">
        <div>
            <asp:Label ID="lblQuoteID" runat="server" Text="Quote ID : "></asp:Label>
               <asp:Label ID="quoteNumber" runat="server" Text=""></asp:Label>
        </div>
        <div>
            <asp:Repeater runat="server" ID="exportTable">
                <HeaderTemplate>
                    <table border="1" width="100%">
                        <tr>
                            <th> <% 
 = version;

%> </th>
                            <th><% 
 = quoteType;

%></th>
                            <th><% 
 = quoteDate;

%></th>
                          
                        </tr>
                </HeaderTemplate>

                <ItemTemplate>
                    <tr>
                        <td><%#Container.DataItem("Version")%></td>
                        <td><%# TranslateType(Container.DataItem("Type"))%></td>
                        <td><%# ConvertDate(Container.DataItem("Timestamp"))%></td>
                       
                    </tr>
                </ItemTemplate>

                <FooterTemplate>
                    </table>
                </FooterTemplate>



            </asp:Repeater>
        </div>
    </form>
</body>
</html>


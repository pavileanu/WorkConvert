<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="ValidationEditor.aspx.vb" Inherits="IQ.ValidationEditor" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <p>Edit Validation </p>
        <span><h3>Logic</h3></span>
        <p>Validation Message Type<asp:DropDownList runat="server" ID="ddlMessageType"></asp:DropDownList></p>
        <p>Apply to Options of Type <asp:TextBox runat="server" ID="txtOptType"/> (e.g. SOF1, can use * wildcard and multiple types separated with /) </p>
        <p>Apply to options of OptionFamily <asp:TextBox runat="server" ID="txtOptFamily"/> (IQ1 terms e.g. OPERATING_SYSTEM)</p>
        <p>Type<asp:DropDownList runat="server" ID="ddlType"></asp:DropDownList></p>
        <span><h3>Display</h3></span>
        <p>Title<asp:TextBox runat="server" id="txtCorrectMessage" /> (displays in the validation box, %1 vars can be used)</p>
        <p>Message<asp:TextBox runat="server" id="txtMessage" /> (displays in the popup)</p>
        <p>Severity<asp:DropDownList runat="server" ID="ddlSeverity"></asp:DropDownList> (The icon shown next to it, certain types can cause validation failures)</p>
        <span><h3>Attribute Values (The function of the following depends on the validation type selected)</h3></span>
        <p>CheckAttribute<asp:TextBox runat="server" ID="txtCheckAttr"/> </p>
        <p>CheckAttributeValue<asp:TextBox runat="server" ID="txtCheckAttrValue"/></p>
        <p>DependancyOptType<asp:TextBox runat="server" ID="txtDepOpt"/></p>
        <p>DependantCheckAttribute<asp:TextBox runat="server" ID="txtDepCheckAttr"/></p>
        <p>DependantCheckAttributeValue<asp:TextBox runat="server" ID="txtDepCheckAttrValue"/></p>
        <p>Quantity<asp:TextBox runat="server" ID="txtQuantity"/></p>
        <span><h3>Links</h3></span>
        <p>LinkTechnology<asp:TextBox runat="server" ID="txtLinkTechnology"/> (Limit clickable links shown to this technology)</p>
        <p>LinkOptType<asp:TextBox runat="server" ID="txtLinkOptType"/> (Limit clickable links shown to this opttype)</p>
        <p>LinkOptionFamily<asp:TextBox runat="server" ID="txtLinkOptionFamily"/> (Limit clickable links shown to this optfamily)</p>
        <hr />
        <p><asp:Button runat="server" OnClick="Unnamed_Click"  UseSubmitBehavior ="true" Text="Save" /></p>
    </div>
    </form>
</body>
</html>

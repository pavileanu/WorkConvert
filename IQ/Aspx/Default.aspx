<%@ Page Title="Home Page" Language="vb" MasterPageFile="~/Site.Master" AutoEventWireup="false"
    CodeBehind="Default.aspx.vb" Inherits="IQ._Default" EnableViewState="false" %>
    
<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <p>
    </p>
<asp:Panel ID="Panel1" runat="server">
</asp:Panel>
 
<p>
    <asp:Button ID="BtnImportUsers" runat="server" Text="Import Users" />

        <asp:Button ID="BtnAdmin" runat="server" Text="Admin" />
        <asp:TextBox ID="TxtSKU" runat="server"></asp:TextBox>
        <!--<asp:Button ID="BtnFindSKU" runat="server" Text="Find SKU" />-->
    <asp:Button ID="Button31" runat="server" Text="Chassis Mem Gives slots" />
    <asp:Button ID="Button35" runat="server" Text="Make AA Variants" />
    <asp:Button ID="Button40" runat="server" Text="Clones &amp; margins" />
    <asp:Button ID="Button47" runat="server" Text="FIOS" />
    <asp:Button ID="btnCullBadOptions" runat="server" Text="Cull bad options" />
    </p>
    <p>
        <asp:Button ID="BtnREset" runat="server" Text="RESET" />
        <asp:CheckBox ID="ChkImportSystems" runat="server" Text="Import Systems" />
        <asp:Button ID="Button48" runat="server" Text="Test FastFind" />
    </p>
<p>
        <asp:CheckBox ID="ChkImportOptions" runat="server" Text="Import Options" />
    </p>
<p>
        <asp:CheckBox ID="ChkImportAccounts" runat="server" Text="Import Accounts" />
        <asp:Button ID="Button30" runat="server" Text="O/S Slots" />
        <asp:Button ID="Button42" runat="server" Text="de-dupe translations" />
        <asp:Button ID="Button43" runat="server" Text="FixFilters" />
        <asp:Button ID="Button44" runat="server" Text="CheckSlots" />
        <asp:Button ID="CheckFamilies" runat="server" Text="CheckFamilies" />
        <asp:Button ID="BtnOptionsPerSystem" runat="server" Text="ExportOptionsperSystems" />
    </p>
<p>
        <asp:Button ID="BtnImport" runat="server" Text="Import" /><asp:Button ID="BtnSerialize"
            runat="server" Text="Serialize" />
        <asp:Button ID="Button1" runat="server" Text="Update options" />
        <asp:Button ID="BtnIndexProducts" runat="server" Text="Index Products" />
        <asp:Button ID="BtnImportListPrices" runat="server" Text="Import List Prices" />
        <asp:TextBox ID="LPCountry" runat="server"></asp:TextBox>
        <asp:Button ID="Button3" runat="server" Text="Regions" />
        <asp:Button ID="SetScreen"    runat="server" Text="Set Matrices" />
        <asp:Button ID="PtnPower" runat="server" Text="Power sizing" />
        <asp:Button ID="BtnExtText" runat="server" Text="External Text" />
        <asp:Button ID="btnSlotAdds" runat="server" Text="SLot Adds" />
        <asp:Button ID="Button11" runat="server" Text="Loyalty Points" />
        <asp:Button ID="Button2"            runat="server" Text="Fix pictures" />
        <asp:Button ID="Button18" runat="server" Text="Scrape Translations" />
    <asp:Button ID="Button19" runat="server" Text="Fix family Translations" />
        <asp:Button ID="Button20" runat="server" Text="DefaultWTY" />
        <asp:Button ID="Button23" runat="server" Text="QuoteStates" />
        <asp:Button ID="Button24" runat="server" Text="RCA" />
        <asp:Button ID="Button27" runat="server" Text="Order Families by FF" />
        <asp:Button ID="Button28" runat="server" Text="DoPrunes" />
        <asp:Button ID="Button38" runat="server" Text="UK unhosted Vars" />
        <asp:Button OnClick="Button29_Click"  ID="Button29" runat="server" Text="FreezeState" />
        <asp:Button ID="ButtonFixMicrosoft" runat="server" Text="Fix Microsoft" />
    
        <asp:TextBox runat="server" ID="txtSpecificImport" Text="Fallbacks"></asp:TextBox>
    <asp:Button ID="btnSpecificImport" runat="server" OnClick="btnSpecificImport_Click"   Text="Run S Import" />

    <asp:TextBox runat="server" ID="txtOutput"></asp:TextBox>
        <asp:Button ID="Button39" runat="server" Text="Tokens" />
    </p>
<p>
        Manufacturers SKU<asp:TextBox ID="TxtMfrSKU" runat="server"></asp:TextBox>
    </p>
    <p>
        Product name<asp:TextBox ID="TxtProductName" runat="server" Width="264px"></asp:TextBox>
    </p>
<p>
        Language <asp:TextBox ID="TxtLanguage" runat="server"></asp:TextBox>
    </p>
<p>
        txtMass<asp:TextBox ID="TxtMass" runat="server"></asp:TextBox>
    </p>
    <asp:Button ID="BtnAddProduct" runat="server" Text="Add Product" />
&nbsp;This will add a product, and two attributes a SKU and A product name.<br />
    <br />
    Username<asp:TextBox ID="txtUserName" runat="server">Admin</asp:TextBox>
    <br />
    <br />
&nbsp;Password<asp:TextBox ID="TxtPassword" runat="server" 
        TextMode="Password"></asp:TextBox>
    <br />
    <asp:Button ID="BtnLogin" runat="server" Text="Log in" />
    

    There are currently 
    <asp:Label ID="LblProducts" runat="server" Text="#Products"></asp:Label>
&nbsp;products
    <br />
    '<asp:TextBox ID="TextBox1" runat="server"></asp:TextBox>
    <asp:Button ID="BtnMakeScreens" runat="server" Text="Make Screens" />
    <asp:Button ID="BtnImportFormFactors" runat="server" Text="Form Factors" />
    <asp:Button ID="btnAvalanche" runat="server" Text="Import Avalanche" />
    <asp:Button ID="BtnImportBundles" runat="server" Text="Import Bundles" />
    <asp:Button ID="Button5" runat="server" Text="Channels" />
    <asp:Button ID="Button6" runat="server" Text="Chassis Variants" />
<asp:Button ID="Button7" runat="server" Text="Fix Variants (distiSkus)" />
    <asp:Button ID="Button8" runat="server" Text="AutoAdds" />
    <asp:Button ID="Button9" runat="server" Text="Order Branches" />
    <asp:Button ID="Button10" runat="server" Text="CarePack Properties" />
    <asp:Button ID="Button12" runat="server" Text="Receta" />
    <asp:Button ID="Button13" runat="server" Text="Flex" />
    <asp:Button ID="Button14" runat="server" Text="CPUs" Width="56px" />
    <asp:Button ID="Button21" runat="server" Text="UnhostedPricing" />
    <asp:Button ID="btnManufacturer" runat="server" Text="HP Split (manufacturer)" />
        <asp:Button ID="Button45" runat="server" Text="PQWS Data Import" />
    <asp:Button ID="Button49" runat="server" Text="Fix CarepackSlots" />
    <asp:Button ID="btnDeleteCarepacks" runat="server" Text="Delete All Carepacks" />
    <asp:Button ID="cmdAddAll" runat="server" Text="Add All CarePacks" />
    <br />
    <br />
    <asp:Panel ID="EditPanel" runat="server" BackColor="#66FF99">
    </asp:Panel>
    <asp:TextBox ID="TxtHostID" runat="server">DINUS92705</asp:TextBox>
    <asp:Button ID="Button4" runat="server" Text="Import host prices" />
    <asp:Button ID="Button15" runat="server" Text="TRO" />
    <asp:Button ID="Button16" runat="server" Text="High Performance" />
    <asp:Button ID="Button17" runat="server" Text="Energy Star (one off)" />

    <asp:TextBox ID="txtHostCode" runat="server">UNHOSTED</asp:TextBox>

    <asp:Button ID="Button22" runat="server" Text="Getvariants" />
    <asp:Button ID="btnSBSO" runat="server" Text="SBSO Hierarchy" />
    <asp:Button ID="Button25" runat="server" Text="Quotes" />
    <asp:Button ID="btnPreInstalled" runat="server" Text="Import PreInstalled" />
    <asp:Button ID="btnImpcrePacks" runat="server" Text="Import CarePacks" />
    <asp:Button ID="cmdQuickSpecs" runat="server" Text="Import QuickSpecs" />
    <asp:Button ID="cmdExtras" runat="server" Text="Import Extras" />
    <asp:Button ID="btnGraphics" runat="server" Text="Import Graphics" />
    <asp:Button ID="cmdNetworking" runat="server" Text="Import Networking" />
    <asp:Button ID="cmdPSU" runat="server" Text="Import PSU" />
    <asp:Button ID="cmdOS" runat="server" Text="Import OS" />
    <asp:Button ID="Button36" runat="server" Text="FIO Focus" />
    <asp:Button ID="ButtonOP" runat="server" Text="Import Other PreInstalled" OnClick ="ButtonOP_Click" />
    <br />


    <div>
    <p><b>Export Translations</b></p>
    <label>txtspecifLanguage 2Letter Code </label>
    <span style ="padding-right:20px;"><asp:TextBox ID="txtLang" runat="server">EN</asp:TextBox></span>
        <span></span>
           <span><asp:Button ID="btnGenerateLang" runat="server" Text="Generate Translation" /></span>  

    </div>
    <div>
     <p><b>Import Translations</b></p>
       <label>File Path </label>
      <span><asp:FileUpload ID="uploadTranslations" runat="server" /></span>
        <span>
             <asp:Button ID="btnImportTranslation" runat="server" Text="Import Translation" />
        <asp:Button ID="Button26" runat="server" Text="NewOptions" />
        <asp:Button ID="Button32" runat="server" Text="DoPrunes" />
        <asp:Button ID="Button33" runat="server" Text="FixFamMinor" />
        <asp:Button ID="Button34" runat="server" Text="FixPCIs" />
        <asp:Button ID="Button37" runat="server" Text="Legal (one off)" />
        <asp:Button ID="Button41" runat="server" Text="PSU " />
        <asp:Button ID="btnFixOS" runat="server" Text="FixOS" />
        </span>  
        </div>
    <div>
        <p><b>Import Quotes </b></p>
        <p>   <span>     <asp:TextBox ID="txtHostID2" runat="server"></asp:TextBox></span><asp:Button ID="btnImportQuotes" runat="server" Text="Import Quotes" /> 
            <asp:Label ID="Label9" runat="server" Text=""></asp:Label>
            <asp:Button ID="Button46" runat="server" Height="26px" Text="Sweep FIOs" />
        </p>

        <p>
            <asp:Button ID="btnExpScreen" runat="server" Text="Create CSV export Screen" />
        </p>


        <asp:Panel ID="Panel2" runat="server">
            Set currency for host (and accounts)
            <asp:TextBox ID="txtHost4Curr" runat="server"></asp:TextBox>
            &nbsp; Currency<asp:TextBox ID="TxtCurr" runat="server" Width="30px"></asp:TextBox>
            <asp:Button ID="BtnSetCur" runat="server" Text="Set Currencies" />
            <asp:Literal ID="Literal1" runat="server"></asp:Literal>
            <asp:Button ID="cmdFixCarePack" runat="server" Text="Fix Carepacks" />
            <asp:Button ID="btnCpkReport" runat="server" Text="Carepack Export" />
            <asp:TextBox ID="txtSysSku" runat="server"></asp:TextBox>
            <asp:Button ID="btnFixQuantities" runat="server" Text="Fix Quantities" />
            <asp:Button ID="btnFixMissingMemory" runat="server" Text="FixMissingMem" />
            <asp:Button ID="Button50" runat="server" Text="Button" />
        </asp:Panel>

    </div>
</asp:Content>

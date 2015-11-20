<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="IQ._Default" EnableViewState="false" %>
    
<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <p>
    </p>
<asp:Panel ID="Panel1" runat="server">
</asp:Panel>
 
<p>
    <asp:Button ID="BtnImportUsers" runat="server" Text="Import Users"  OnClick="BtnImportUsers_Click"/>

        <asp:Button ID="BtnAdmin" runat="server" Text="Admin" />
        <asp:TextBox ID="TxtSKU" runat="server"></asp:TextBox>
        <!--<asp:Button ID="BtnFindSKU" runat="server" Text="Find SKU" />-->
    <asp:Button ID="Button31" runat="server" Text="Chassis Mem Gives slots"  OnClick="Button31_Click"/>
    <asp:Button ID="Button35" runat="server" Text="Make AA Variants"  OnClick="Button35_Click"/>
    <asp:Button ID="Button40" runat="server" Text="Clones &amp; margins"  OnClick="Button40_Click"/>
    <asp:Button ID="Button47" runat="server" Text="FIOS"  OnClick="Button47_Click"/>
    <asp:Button ID="btnCullBadOptions" runat="server" Text="Cull bad options"  OnClick="btnCullBadOptions_Click"/>
    </p>
    <p>
        <asp:Button ID="BtnREset" runat="server" Text="RESET" />
        <asp:CheckBox ID="ChkImportSystems" runat="server" Text="Import Systems" />
        <asp:Button ID="Button48" runat="server" Text="Test FastFind"  OnClick="Button48_Click"/>
    </p>
<p>
        <asp:CheckBox ID="ChkImportOptions" runat="server" Text="Import Options" />
    </p>
<p>
        <asp:CheckBox ID="ChkImportAccounts" runat="server" Text="Import Accounts" />
        <asp:Button ID="Button30" runat="server" Text="O/S Slots"  OnClick="Button30_Click"/>
        <asp:Button ID="Button42" runat="server" Text="de-dupe translations"  OnClick="Button42_Click"/>
        <asp:Button ID="Button43" runat="server" Text="FixFilters"  OnClick="Button43_Click"/>
        <asp:Button ID="Button44" runat="server" Text="CheckSlots"  OnClick="Button44_Click"/>
        <asp:Button ID="CheckFamilies" runat="server" Text="CheckFamilies"  OnClick="CheckFamilies_Click"/>
        <asp:Button ID="BtnOptionsPerSystem" runat="server" Text="ExportOptionsperSystems"  OnClick="BtnOptionsPerSystem_Click"/>
    </p>
<p>
        <asp:Button ID="BtnImport" runat="server" Text="Import"  OnClick="BtnImport_Click"/><asp:Button ID="BtnSerialize"
            runat="server" Text="Serialize" />
        <asp:Button ID="Button1" runat="server" Text="Update options"  OnClick="Button1_Click"/>
        <asp:Button ID="BtnIndexProducts" runat="server" Text="Index Products"  OnClick="Button2_Click"/>
        <asp:Button ID="BtnImportListPrices" runat="server" Text="Import List Prices"  OnClick="BtnImportListPrices_Click"/>
        <asp:TextBox ID="LPCountry" runat="server"></asp:TextBox>
        <asp:Button ID="Button3" runat="server" Text="Regions"  OnClick="Button3_Click"/>
        <asp:Button ID="SetScreen"    runat="server" Text="Set Matrices"  OnClick="SetScreen_Click"/>
        <asp:Button ID="PtnPower" runat="server" Text="Power sizing"  OnClick="PtnPower_Click"/>
        <asp:Button ID="BtnExtText" runat="server" Text="External Text"  OnClick="BtnExtText_Click"/>
        <asp:Button ID="btnSlotAdds" runat="server" Text="SLot Adds"  OnClick="btnSlotAdds_Click"/>
        <asp:Button ID="Button11" runat="server" Text="Loyalty Points"  OnClick="Button11_Click"/>
        <asp:Button ID="Button2"            runat="server" Text="Fix pictures"  OnClick="Button2_Click1"/>
        <asp:Button ID="Button18" runat="server" Text="Scrape Translations"  OnClick="Button18_Click"/>
    <asp:Button ID="Button19" runat="server" Text="Fix family Translations"  OnClick="Button19_Click"/>
        <asp:Button ID="Button20" runat="server" Text="DefaultWTY"  OnClick="Button20_Click"/>
        <asp:Button ID="Button23" runat="server" Text="QuoteStates"  OnClick="Button23_Click"/>
        <asp:Button ID="Button24" runat="server" Text="RCA"  OnClick="Button24_Click"/>
        <asp:Button ID="Button27" runat="server" Text="Order Families by FF"  OnClick="Button27_Click"/>
        <asp:Button ID="Button28" runat="server" Text="DoPrunes"  OnClick="Button28_Click"/>
        <asp:Button ID="Button38" runat="server" Text="UK unhosted Vars"  OnClick="Button38_Click"/>
        <asp:Button OnClick="Button29_Click"  ID="Button29" runat="server" Text="FreezeState" />
        <asp:Button ID="ButtonFixMicrosoft" runat="server" Text="Fix Microsoft"  OnClick="FixMSBranches"/>
    
        <asp:TextBox runat="server" ID="txtSpecificImport" Text="Fallbacks"></asp:TextBox>
    <asp:Button ID="btnSpecificImport" runat="server" OnClick="btnSpecificImport_Click"   Text="Run S Import"  OnClick="btnSpecificImport_Click"/>

    <asp:TextBox runat="server" ID="txtOutput"></asp:TextBox>
        <asp:Button ID="Button39" runat="server" Text="Tokens"  OnClick="Button39_Click"/>
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
    <asp:Button ID="BtnLogin" runat="server" Text="Log in"  OnClick="BtnLogin_Click"/>
    

    There are currently 
    <asp:Label ID="LblProducts" runat="server" Text="#Products"></asp:Label>
&nbsp;products
    <br />
    '<asp:TextBox ID="TextBox1" runat="server"></asp:TextBox>
    <asp:Button ID="BtnMakeScreens" runat="server" Text="Make Screens"  OnClick="BtnMakeScreens_Click"/>
    <asp:Button ID="BtnImportFormFactors" runat="server" Text="Form Factors"  OnClick="BtnImportFormFactors_Click"/>
    <asp:Button ID="btnAvalanche" runat="server" Text="Import Avalanche"  OnClick="btnAvalanche_Click"/>
    <asp:Button ID="BtnImportBundles" runat="server" Text="Import Bundles"  OnClick="BtnImportBundles_Click"/>
    <asp:Button ID="Button5" runat="server" Text="Channels" />
    <asp:Button ID="Button6" runat="server" Text="Chassis Variants"  OnClick="Button6_Click"/>
<asp:Button ID="Button7" runat="server" Text="Fix Variants (distiSkus)"  OnClick="Button7_Click"/>
    <asp:Button ID="Button8" runat="server" Text="AutoAdds"  OnClick="Button8_Click"/>
    <asp:Button ID="Button9" runat="server" Text="Order Branches"  OnClick="Button9_Click"/>
    <asp:Button ID="Button10" runat="server" Text="CarePack Properties"  OnClick="Button10_Click"/>
    <asp:Button ID="Button12" runat="server" Text="Receta"  OnClick="Button12_Click"/>
    <asp:Button ID="Button13" runat="server" Text="Flex"  OnClick="Button13_Click"/>
    <asp:Button ID="Button14" runat="server" Text="CPUs" Width="56px"  OnClick="Button14_Click"/>
    <asp:Button ID="Button21" runat="server" Text="UnhostedPricing" />
    <asp:Button ID="btnManufacturer" runat="server" Text="HP Split (manufacturer)"  OnClick="btnManufacturer_Click"/>
        <asp:Button ID="Button45" runat="server" Text="PQWS Data Import"  OnClick="Button45_Click"/>
    <asp:Button ID="Button49" runat="server" Text="Fix CarepackSlots"  OnClick="Button49_Click"/>
    <asp:Button ID="btnDeleteCarepacks" runat="server" Text="Delete All Carepacks"  OnClick="btnDeleteCarepacks_Click"/>
    <asp:Button ID="cmdAddAll" runat="server" Text="Add All CarePacks"  OnClick="cmdAddAll_Click"/>
    <br />
    <br />
    <asp:Panel ID="EditPanel" runat="server" BackColor="#66FF99">
    </asp:Panel>
    <asp:TextBox ID="TxtHostID" runat="server">DINUS92705</asp:TextBox>
    <asp:Button ID="Button4" runat="server" Text="Import host prices" />
    <asp:Button ID="Button15" runat="server" Text="TRO"  OnClick="Button15_Click"/>
    <asp:Button ID="Button16" runat="server" Text="High Performance"  OnClick="Button16_Click"/>
    <asp:Button ID="Button17" runat="server" Text="Energy Star (one off)"  OnClick="Button17_Click"/>

    <asp:TextBox ID="txtHostCode" runat="server">UNHOSTED</asp:TextBox>

    <asp:Button ID="Button22" runat="server" Text="Getvariants"  OnClick="Button22_Click"/>
    <asp:Button ID="btnSBSO" runat="server" Text="SBSO Hierarchy"  OnClick="btnSBSO_Click"/>
    <asp:Button ID="Button25" runat="server" Text="Quotes"  OnClick="Button25_Click"/>
    <asp:Button ID="btnPreInstalled" runat="server" Text="Import PreInstalled"  OnClick="btnPreInstalled_Click"/>
    <asp:Button ID="btnImpcrePacks" runat="server" Text="Import CarePacks"  OnClick="btnImpcrePacks_Click"/>
    <asp:Button ID="cmdQuickSpecs" runat="server" Text="Import QuickSpecs"  OnClick="cmdQuickSpecs_Click"/>
    <asp:Button ID="cmdExtras" runat="server" Text="Import Extras"  OnClick="cmdExtras_Click"/>
    <asp:Button ID="btnGraphics" runat="server" Text="Import Graphics"  OnClick="btnGraphics_Click"/>
    <asp:Button ID="cmdNetworking" runat="server" Text="Import Networking"  OnClick="cmdNetworking_Click"/>
    <asp:Button ID="cmdPSU" runat="server" Text="Import PSU"  OnClick="cmdPSU_Click"/>
    <asp:Button ID="cmdOS" runat="server" Text="Import OS"  OnClick="cmdOS_Click"/>
    <asp:Button ID="Button36" runat="server" Text="FIO Focus"  OnClick="Button36_Click"/>
    <asp:Button ID="ButtonOP" runat="server" Text="Import Other PreInstalled" OnClick ="ButtonOP_Click" />
    <br />


    <div>
    <p><b>Export Translations</b></p>
    <label>txtspecifLanguage 2Letter Code </label>
    <span style ="padding-right:20px;"><asp:TextBox ID="txtLang" runat="server">EN</asp:TextBox></span>
        <span></span>
           <span OnClick="btnGenerateLang_Click"><asp:Button ID="btnGenerateLang" runat="server" Text="Generate Translation" /></span>  

    </div>
    <div>
     <p><b>Import Translations</b></p>
       <label>File Path </label>
      <span><asp:FileUpload ID="uploadTranslations" runat="server" /></span>
        <span>
             <asp:Button ID="btnImportTranslation" runat="server" Text="Import Translation"  OnClick="btnImportTranslation_Click"/>
        <asp:Button ID="Button26" runat="server" Text="NewOptions"  OnClick="Button26_Click"/>
        <asp:Button ID="Button32" runat="server" Text="DoPrunes"  OnClick="Button32_Click"/>
        <asp:Button ID="Button33" runat="server" Text="FixFamMinor"  OnClick="Button33_Click"/>
        <asp:Button ID="Button34" runat="server" Text="FixPCIs"  OnClick="Button34_Click"/>
        <asp:Button ID="Button37" runat="server" Text="Legal (one off)"  OnClick="Button37_Click"/>
        <asp:Button ID="Button41" runat="server" Text="PSU "  OnClick="Button41_Click"/>
        <asp:Button ID="btnFixOS" runat="server" Text="FixOS"  OnClick="btnFixOS_Click"/>
        </span>  
        </div>
    <div>
        <p><b>Import Quotes </b></p>
        <p>   <span>     <asp:TextBox ID="txtHostID2" runat="server"></asp:TextBox></span><asp:Button ID="btnImportQuotes" runat="server" Text="Import Quotes" /> 
            <asp:Label ID="Label9" runat="server" Text=""></asp:Label>
            <asp:Button ID="Button46" runat="server" Height="26px" Text="Sweep FIOs"  OnClick="Button46_Click"/>
        </p>

        <p>
            <asp:Button ID="btnExpScreen" runat="server" Text="Create CSV export Screen"  OnClick="btnExpScreen_Click"/>
        </p>


        <asp:Panel ID="Panel2" runat="server">
            Set currency for host (and accounts)
            <asp:TextBox ID="txtHost4Curr" runat="server"></asp:TextBox>
            &nbsp; Currency<asp:TextBox ID="TxtCurr" runat="server" Width="30px"></asp:TextBox>
            <asp:Button ID="BtnSetCur" runat="server" Text="Set Currencies"  OnClick="BtnSetCur_Click"/>
            <asp:Literal ID="Literal1" runat="server"></asp:Literal>
            <asp:Button ID="cmdFixCarePack" runat="server" Text="Fix Carepacks"  OnClick="cmdFixCarePack_Click"/>
            <asp:Button ID="btnCpkReport" runat="server" Text="Carepack Export"  OnClick="btnCpkReport_Click"/>
            <asp:TextBox ID="txtSysSku" runat="server"></asp:TextBox>
            <asp:Button ID="btnFixQuantities" runat="server" Text="Fix Quantities"  OnClick="btnFixQuantities_Click"/>
            <asp:Button ID="btnFixMissingMemory" runat="server" Text="FixMissingMem"  OnClick="btnFixMissingMemory_Click"/>
            <asp:Button ID="Button50" runat="server" Text="Button"  OnClick="Button50_Click"/>
        </asp:Panel>

    </div>
</asp:Content>


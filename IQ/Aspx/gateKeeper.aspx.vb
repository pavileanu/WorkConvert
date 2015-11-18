Imports dataAccess
Imports System.Globalization
Imports System.Net
Imports System.Xml

Public Class gateKeeper
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'The gateKeeper allows a user to be authenticated by the disti and to then bypass our login page
        'We *must not* allow the gatekeeper to be accessed by any agent not known to be authenticated - becuase our system contains confidential, customer specific pricing.
        'Any disti who has implemented iQuote integration *knows* the technical implementation - so obscurity/secrecy gives us no protection (and should never be relied upon)

        'we cannot rely upon the referer - as Internet Security Suites, Proxies and firewalls can potentially strip this information - also it is easily manipulated

        'We need a simple mechanism for the disti to tell us that a user is authenticated so
        'They call our https://webservice GetOneTimeToken(hostID,password) as string - and for a given email address - recieve a one time token
        'they then response.redirecet their client here (gateKeeper.aspx) passing only the token
        'The token they receive will have a short lifespan (the length of the session on their system) and can only be used one
        'The token becomes the *only* thing the client needs to submit as it provides the key to the full set of pre-stored NVP's

        'for legacy clients who may not want to (or have the technology to) call a webservice
        'a backward compatible form does what the IQ1 Gatekeeper did - accepting the NVP's and calling our webservcie 
        '(also prompting for missing, mandatory values)

        ' Dim lit As Literal

        ' If Not Request.IsSecureConnection Then
        ' form1.Controls.Add(ErrorDymo("The connection is not reporting as secure"))
        ' Else
        'If Request.UrlReferrer Is Nothing Then
        '    lit = ErrorDymo("The referrer is blank - you may have been directed to this page via a script, bookmark or response.redirect, Please ensure you arrive via a direct POST to this URL from a known referring domain.")
        '    Form.Controls.Add(lit)
        '    lit = New Literal
        '    lit.Text = "Please contact development@channelcentral.net if you are having trouble integrating iQuote 2"
        '    Form.Controls.Add(lit)
        'Else

        Dim errorMessages As List(Of String) = New List(Of String)

        If iq Is Nothing Then

            'iq = New clsIQ  'This IS the 'object model'

            Application("IQ") = iq  'holding a reference to the (entire) object mode means it will never time out - and we don't need asp.net's sessions
            iq.load(errorMessages)

        End If

        If Request("token") Is Nothing Then
            form1.Controls.Add(ErrorDymo("No TOKEN was supplied - you need to call our webservice submitting the data for a session, to receive a 20 character 'one time' token - which should be passed to this page to gain access to iQuote"))
        Else

            Dim error$ = ""
            Dim nvps As Dictionary(Of String, String)

            If Request("token") = "none" Then
                nvps = NVPsFromRequest(Request, errorMessages)
            ElseIf Request("token") = "tdeu" Then
                nvps = NVPsFromTechdataEU(Request, errorMessages)
            Else
                Dim token As String = Request("Token")
                nvps = NVPsFromDB(token, errorMessages) 'The webserives has put them in the DB !

                'Ingram Micro specific (EU) - may need to change for US
                If errorMessages.Count = 0 Then
                    If nvps("host").ToUpper.StartsWith("DIN") Then
                        If Not nvps.ContainsKey("cPriceBand") Then
                            nvps.Add("cPriceBand", "")
                        End If

                        'this is confusing - but the IngramPriceBands Method was added to the existing feedreader logging webservice.
                        Dim pbclient As IngramPB.I_LoggingClient = New IngramPB.I_LoggingClient
                        Dim pb As Integer = 0
                        Try
                            pb = pbclient.IngramPriceBand(nvps("host"), nvps("cAccountNum"))
                        Catch ex As System.Exception
                            errorMessages.Add(ex.Message)
                        End Try

                        If pb > 0 Then
                            '*** accounts are located by the users email
                            'and the priceband is loaded into the caccount num
                            'becuase it will be assigend back into priceband shortly
                            nvps("cPriceBand") = pb.ToString
                            'we may have to add cAccountNum back to account - such that it can be passed back with a basket
                        Else
                            errorMessages.Add("Unable to retreive price band - result was " & pb)
                        End If
                    End If
                End If

            End If

            ' Pick up any MFR - default to HPE
            Dim mfrCode As String = "HPE"
            Dim mfr As Manufacturer = Manufacturer.Unknown
            Dim mfrSpecified As Boolean = False

            ' MFR should be the key in use, although the docs also mention MFG
            If (nvps.ContainsKey("mfr")) AndAlso (Not String.IsNullOrEmpty(nvps("mfr"))) Then
                mfrCode = nvps("mfr")
                mfrSpecified = True
            ElseIf (nvps.ContainsKey("mfg")) AndAlso (Not String.IsNullOrEmpty(nvps("mfg"))) Then
                mfrCode = nvps("mfg")
                mfrSpecified = True
            End If

            If Not String.IsNullOrEmpty(mfrCode) Then
                If String.Equals(mfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase) Then
                    mfr = Manufacturer.HPE
                ElseIf String.Equals(mfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase) Then
                    mfr = Manufacturer.HPI
                Else
                    mfrCode = String.Empty
                    mfrSpecified = False
                End If
            End If

            ''synnex don't submit a host ID (allegedy)
            'If InStr(ref$, "synnex.ca") > 0 Then
            '    nvps.Add("host", "DSYCAN1H1B4")
            'ElseIf InStr(ref, "synnex.com") > 0 Then
            '    nvps.Add("host", "DSYUS94538")
            'End If

            Dim user As clsUser = Nothing
            If errorMessages.Count Then
                'The token might have already been used, or not be found/valid
                For Each msg In errorMessages
                    form1.Controls.Add(ErrorDymo(msg))
                Next
            Else
                Dim hostChannel As clsChannel
                hostChannel = iq.i_channel_code(nvps("host"))  'we *know* it will be there (the oneTimeToken webservice created this and already validated)

                Dim Account As clsAccount = Nothing
                If nvps("token") <> hostChannel.WebToken And nvps("token") <> "tdeu" And nvps("token") <> "none" Then
                    'the only way this could covcievably happen is if we changed a host ID or token (whlist the system was running)
                    form1.Controls.Add(ErrorDymo("Token mismatch (should never happen - the HostID or Token must have changed ?)"))
                Else
                    Dim uEmail As String = nvps("uEmail")
                    Dim reseller As clsChannel
                    'all the user account and the reseller
                    Dim CompanyAccounts = From ca In hostChannel.CustomerAccounts.Values Where ca.Priceband.text = nvps("cAccountNum") 'LINQ  
                    If CompanyAccounts.Any Then
                        reseller = CompanyAccounts.First.BuyerChannel
                    Else
                        'There is no (existing) account (for anyone at that company)
                        If Not nvps.ContainsKey("cName") Or Not nvps.ContainsKey("cPCode") Then
                            form1.Controls.Add(ErrorDymo("We have no account '" & nvps("cAccountNum") & "' - and you haven't provided a 'cName' AND 'cPcode'  (so we can't create a [channel] to place the [account] on)"))
                        Else
                            Dim buyerID$ = UCase("R" & Left(nvps("cName"), 2) & nvps("cPCode").Replace(" ", ""))

                            If iq.i_channel_code.ContainsKey(buyerID$) Then
                                reseller = iq.i_channel_code(buyerID$)
                            Else
                                reseller = New clsChannel(hostChannel, nvps("cName"), nvps("cName"), "", buyerID, hostChannel.Region, New nullableString(), New nullableString(), New nullableString(Left(uEmail, InStr(uEmail, "@") - 1)), 15, "tree.1", "", 0, 0, "R", "", "", hostChannel.DefaultCurrency, False, "", "", "")
                            End If
                        End If
                    End If

                    'CompanyAccounts is the set of (user) accounts belonging to people at the company with the priceBand 
                    'NB: There may be more than one ! (Fred, Bill and Jane May all work at FredsComputers.com)

                    'the USER may already exist ('under' another disti) - (even if they don't have an account with this one)
                    If Not iq.i_user_email.ContainsKey(uEmail.ToLower) Then
                        'Nope - no user (at all) with this email

                        'find the users buyer company (who do they work for)
                        ' Dim buyerCompany As clsChannel = CompanyAccounts.First.BuyerChannel
                        Dim uName As String = "" : If nvps.ContainsKey("uName") Then uname = nvps("uName")
                        Dim uTel As String = "" : If nvps.ContainsKey("uTel") Then uTel = nvps("uTel")

                        user = New clsUser(reseller, nvps("uEmail"), uName, New nullableString(uTel), New nullableString())
                    Else
                        user = iq.i_user_email(uEmail)
                    End If

                    Dim buyerAccounts As List(Of clsAccount) = (From ba In hostChannel.CustomerAccounts.Values Where ba.User.Email.ToLower = nvps("uEmail").ToLower()).ToList() 'And ba.mfrCode = (If(mfrSpecified, mfrCode, ba.mfrCode))

                    If Not buyerAccounts.Any Then

                        'There is no buyeraccount/cusutomeraccount for this user(email)
                        Dim cl$ = "EN-gb"
                        If hostChannel.Region.Culture.Code <> "" Then cl$ = hostChannel.Region.Culture.Code
                        Dim ci As CultureInfo = New CultureInfo(cl$)
                        Dim language As clsLanguage = iq.i_language_Code(UCase(ci.TwoLetterISOLanguageName)) 'ISO 639-1  'GB,DE,FR etc.
                        Dim ri As RegionInfo = New RegionInfo(cl$)
                        Dim currency As clsCurrency = hostChannel.DefaultCurrency ' iq.i_currency_code(ri.ISOCurrencySymbol) 'Three Char ISO 4217 code (GBP, EUR, USD e.t.c.)

                        'an explict priceband (in the NVPs) will override a cAccountNum
                        Dim priceband As String = ""
                        If nvps.ContainsKey("cPriceBand") Then priceband = nvps("cPriceBand")
                        If priceband = "" AndAlso nvps.ContainsKey("cAccountNum") Then priceband = nvps("cAccountNum")

                        Account = New clsAccount(user, "PeeWee3", reseller, {iq.i_role_Code("user")}, Nothing, language, currency, hostChannel, iq.getPriceBand(priceband), hostChannel.Region.Culture, mfrCode)
                        buyerAccounts.Add(Account)

                        'ElseIf buyerAccounts.Count = 1 Then
                        '    Account = buyerAccounts.First 'We have an exact match (by email and priceBand - with the Hosts customerAccounts

                        'Else
                        '    form1.Controls.Add(ErrorDymo("There is more than one user account with that email"))
                    End If


                    If buyerAccounts.Any Then

                        Dim tid = iq.recordLogin(user, False, user.Email, String.Empty)
                        Dim lid = simpleHash(CStr(tid))
                        iq.updateLogin(tid, lid)

                        For Each nvp In nvps
                            iq.sesh(lid, "gk_" & nvp.Key) = nvp.Value
                        Next
                        iq.sesh(lid, "viaGatekeeper") = True

                        ' Mark the token as used
                        If Request("token") IsNot Nothing Then
                            If Request("tom") Is Nothing Then
                                If Request("token") <> "tdeu" And Request("token") <> "none" Then
                                    da.DBExecutesql("UPDATE gk.token SET [usedAt]=getdate() WHERE Token='" & nvps("token") & "';")
                                End If
                            End If
                        End If

                        ' Add User ID to the session
                        iq.sesh(lid, "UserID") = user.ID

                        ' Add the account list to the session; Accounts.aspx will handle which one is used
                        iq.sesh(lid, "AccountList") = buyerAccounts

                        ' Add MFR to the session if specified
                        If mfrSpecified AndAlso mfr <> Manufacturer.Unknown Then
                            iq.sesh(lid, "MFR") = mfr
                        End If

                        ' Add any requested deep link SKU to the session
                        If (nvps.ContainsKey("base")) AndAlso (Not String.IsNullOrEmpty(nvps("base"))) Then
                            iq.sesh(lid, "Base") = nvps("base")
                        End If

                        ' Add any requested Host (seller channel) to the session
                        If nvps.ContainsKey("host") Then
                            iq.sesh(lid, "Host") = Request("host")
                        End If

                        ' Accounts.aspx will handle selecting the account to use
                        Response.Redirect("accounts.aspx?lid=" & lid)

                    Else
                        form1.Controls.Add(ErrorDymo("Could not locate/create account"))

                    End If
                End If
            End If
        End If

    End Sub


    Private Function NVPsFromTechdataEU(request As HttpRequest, ByRef errorMessages As List(Of String)) As Dictionary(Of String, String)

        NVPsFromTechdataEU = New Dictionary(Of String, String)

        With NVPsFromTechdataEU

            Dim hostid As String = ""
            Dim con As SqlClient.SqlConnection = da.OpenDatabase
            Dim sql$ = "SELECT HostID FROM iquote2.techdata.accounts WHERE countryNumber=" & request("corpregionid")
            Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql$)
            If rdr.HasRows Then
                rdr.Read()
                hostid = rdr.Item("HostID")
            End If

            .Add("host", rdr.Item("HostID"))   'dicCRs(request("corpregionid")))
            rdr.Close()
            con.Close()

            .Add("token", request("Token"))

            Dim doc As XmlDocument = New XmlDocument
            Dim sid As String = request("sessionID")
            doc.Load("https://intouch.techdata.com/guiservices/createXMLObj.aspx?session=" & sid)
            ''<?xml version="1.0"?>
            ' <usrObjRpl>
            '           <usrObjRplHdr>
            '               <sessionID>13780271351937408A88E92B46C41319991D6FEC84BAD21</sessionID>
            '               <return><text>OK</text><code>0</code></return>
            '           </usrObjRplHdr>
            '          <usrObjRplBdy>
            '               <user><internal>0</internal>
            '<ID>1378027</ID><LoginName>javi</LoginName><status>1</status><name1>Nombre</name1><name2>Apellidos</name2><stdLang>ES</stdLang><country>40 </country>
            '<rights><order>1</order><dropShip>1</dropShip><administrate>1</administrate><inTouch>1</inTouch><LOL>3</LOL>
            '<Menu>1,2,3,5,10,11,12,20,23,24,25,26,27,28,29,65,66,71</Menu></rights>
            '</user>
            '<customer><ID>429475</ID><status>1</status><currencyCode>EUR</currencyCode><name1>INTERSOFT OFIMATICA Y</name1>
            '<address><name1>INTERSOFT OFIMATICA Y</name1><name2>DESARROLLO, SL</name2><street>Sierra de Loja,13 P.La Juaida</street><PCPrefix>04240</PCPrefix><city>VIATOR</city>
            '<postCountry>ES </postCountry></address></customer>
            '<properties>
            '   <value0></value0><value1></value1><value2></value2><value3></value3><value4></value4><value5></value5><value6>1</value6>
            '   <value7>1</value7><value8>0</value8>
            '   <value9>
            '               ev=2|CurrencyCode=EUR|DefPriceCode=|IP=80.35.80.102|ListPriceCode=LP|PriceGroup=C6|RightsList=AddressMaintenance,OrderSubmit,OrderTracking,DropShipment,exactquantity,AIO,LOL,ResellerAdmin,eRMA,TopConfig,pc2000,MarketingCampaigns,UserProfile,TechSelect,TechPartner,OrderReservation,OrderModification,EndUserQuotation|CorpRegionCode=ES|CurrencyCulture=de-DE|DateCulture=en-GB|LangCulture=es-ES|NumberCulture=de-DE|CompleteDelivery=|IsPostCodeCountry=1|lastAccess=2013-07-19 12:32:17
            '   </value9>
            '</properties>
            '</usrObjRplBdy>
            '</usrObjRpl> 


            '       WITH userInfo (status, uName, uEmail, ordering, curr,cAccount,cName,cPCode,cCountry,uInternal) AS (
            'SELECT 'OK' AS Status, FirstName+' '+LastName as uName,uEmail,ordering,curr,cAccount,cName,cPCode,cCountry,uInternal
            'FROM OPENXML(@hdoc, 'usrObjRpl/usrObjRplBdy',3)   
            'WITH (
            '	FirstName varchar(40)'user/name1', 
            '	LastName varchar(40)'user/name2',
            '	uEmail varchar(100)'user/email',
            '	ordering bit 'user/rights/order',
            '	curr char(3) 'customer/currencyCode',
            '	cAccount varchar(10) 'customer/ID',
            '	cName varchar(100) 'customer/name1',
            '	cPCode varchar(10) 'customer/address/PCPrefix',
            '	cCountry char(2) 'customer/address/postCountry',
            '	uInternal bit 'user/internal'
            '	)

            Dim user As XmlNode = doc.DocumentElement.SelectSingleNode("usrObjRplBdy/user")

            If user Is Nothing Then

                errorMessages.Add("Unexpected response (XML Follows)")

                Dim xml As String = doc.InnerXml
                errorMessages.Add(xml)
                Exit Function

            End If


            If user.SelectSingleNode("name1") IsNot Nothing Then

                .Add("uName", user.SelectSingleNode("name1").InnerText & " " & user.SelectSingleNode("name2").InnerText)
                .Add("uEmail", user.SelectSingleNode("email").InnerText)
                .Add("orderEntry", user.SelectSingleNode("rights/order").InnerText)
                .Add("internal", user.SelectSingleNode("internal").InnerText)

                Dim customer As XmlNode = doc.DocumentElement.SelectSingleNode("usrObjRplBdy/customer")
                .Add("currency", customer.SelectSingleNode("currencyCode").InnerText)
                .Add("cAccountNum", customer.SelectSingleNode("ID").InnerText)
                .Add("cName", customer.SelectSingleNode("name1").InnerText)
                .Add("cPCode", customer.SelectSingleNode("address/PCPrefix").InnerText)
                .Add("PostCountry", customer.SelectSingleNode("address/postCountry").InnerText)

                .Add("ref", "http://intouch.techdata.com/intouch/Home.aspx?sessionid=" & hostid)
                .Add("uTel", "")  'techdata don't 

            Else

                Dim tn As XmlNode = user.SelectSingleNode("Text")
                If tn IsNot Nothing Then
                    If tn.InnerText = "Session expired" Then
                        errorMessages.Add("Session expired")
                        Exit Function
                    End If
                End If

            End If
        End With


    End Function

    Private Function NVPsFromRequest(request As HttpRequest, erromessages As List(Of String)) As Dictionary(Of String, String)

        'for a tokenless (insecure) implementation

        NVPsFromRequest = New Dictionary(Of String, String)(StringComparer.InvariantCultureIgnoreCase)
        For Each k In request.Form.Keys
            NVPsFromRequest.Add(k, request.Form.Item(k))
        Next k

    End Function

    ''' <summary>Fetches the set of name-value pairs associated with the supplied 'one-time' token</summary>
    ''' <returns>A (populated) dictionary of Name>Value - or an ErrorMessage</returns>

    Private Function NVPsFromDB(token As String, ByRef errorMessages As List(Of String)) As Dictionary(Of String, String)
        Dim sql$ = "SELECT n.name, t.timestamp,t.usedAt,[fk_Name_ID],value FROM gk.token T "
        sql$ &= "JOIN gk.value V on fk_token_id=t.id "
        sql$ &= "JOIN gk.[name] N ON N.ID=V.FK_name_id WHERE t.token=" & da.SqlEncode(Request("token"))

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql)

        NVPsFromDB = New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase)

        If Not rdr.HasRows Then
            errorMessages.Add("The token does not exist (or is very old and was deleted)")
        Else
            Do While rdr.Read
                If Not IsDBNull(rdr.Item("usedat")) Then
                    errorMessages.Add("This token has already been used - you may need to 'go out' and 'come in' again ") : Exit Do
                End If
                If DateDiff(DateInterval.Minute, Now, rdr.Item("Timestamp")) > 1 Then

                End If
                NVPsFromDB.Add(rdr.Item("Name"), rdr.Item("Value"))
            Loop
        End If

        rdr.Close()
        con.Close()

    End Function

End Class

Option Strict On
Option Explicit On

'each user has many accounts - each having a password (which *may* be the same as other accounts)
'The account *can* have a team (meaning the user can be a member of many teams - 1 per account)
'The account is 'with' a seller channel - this is who the user is buying stuff (and seeing prices and stock) from.
'The account has a role - giving a set of rights - see clsRole

Imports dataAccess
<Serializable>
Public Class clsAccount
    Implements i_Editable

    Property ID As Integer
    Property Password As String
    Property MustChangePassword As Boolean
    Property SellerChannel As clsChannel 'The channel that's selling - ie.that this account buys from or is 'with'  - this is NOT editable (add a customerAccount under a Channel to make one of these)
    Property Team As clsTeam
    Property User As clsUser  'again NOT editable - add an account under a user
    Property Quotes As Dictionary(Of Integer, clsQuote)
    Property Language As clsLanguage
    Property Currency As clsCurrency
    Property Culture As clsCulture
    ReadOnly Property Roles As clsRole()
        Get
            Return i_roles_code.Values.ToArray
        End Get
    End Property
    Property i_roles_code As Dictionary(Of String, clsRole)
    Property NumQuotes As Integer  'Update via the USER.countQuotesPerAccount method (which will count the quotes for all a users' accounts) - this is NOT persisted to the database
    Property Priceband As clsPriceBand 'String
    Property BuyerChannel As clsChannel ' carries information about the company this buyer works for (who, coincidently, may also be a seller in their own right)
    Property Impressions As Dictionary(Of Integer, clsImpression)
    Property ClickThrus As Dictionary(Of Integer, clsClickThru)
    Property mfrCode As String

    'BRAZIL
    Public wareHouseFilter As String = "" 'The default (empty string) will display all warehouses (variants), Set to 'NONE' to see list prices,  any valid warehouse code will display only variants for that warehouse,

    Public Function displayName(lang As clsLanguage) As String Implements i_Editable.displayName
        displayName = Me.BuyerChannel.DisplayName(lang) & "-" & Me.User.RealName
    End Function

    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        iq.Accounts.Remove(Me.ID)
        iq.Users(Me.User.ID).Accounts.Remove(Me.ID)

        Try
            da.DBExecutesql("DELETE FROM account where id=" & Me.ID)
        Catch ex As Exception
            errorMessages.Add(ex.Message.ToString)
        End Try

    End Sub

    Public Sub New()

        Me.ID = -1
        Me.Quotes = New Dictionary(Of Integer, clsQuote)
        Me.Impressions = New Dictionary(Of Integer, clsImpression)
        Me.ClickThrus = New Dictionary(Of Integer, clsClickThru)

    End Sub

    Public Function MaxQuoteVersion(rootQuote As clsQuote) As Integer

        MaxQuoteVersion = 0

        'returns the highest version number of the quote with the specified root quoteid

        For Each quote In Me.Quotes.Values.ToList
            If quote.RootQuote Is rootQuote Then
                If quote.Version > MaxQuoteVersion Then MaxQuoteVersion = quote.Version
            End If
        Next

    End Function

    Public Sub New(user As clsUser, Password As String, BuyerChannel As clsChannel, roles As clsRole(), Team As clsTeam, Language As clsLanguage, currency As clsCurrency, sellerChannel As clsChannel, priceBand As clsPriceBand, culture As clsCulture, mfrCode As String, Optional wc As DataTable = Nothing, Optional ByRef nextid As Integer = Nothing)

        Dim teamOrNull As String

        If Team Is Nothing Then teamOrNull = "null" Else teamOrNull = Team.ID.ToString

        'If user Is Nothing Then user = iq.Users.Values.First 'This is a bit of a dirty fix to allow users to be created in the editor

        Me.i_roles_code = New Dictionary(Of String, clsRole)()
        Me.Password = Password
        Me.BuyerChannel = BuyerChannel
        Me.Team = Team
        Me.Language = Language
        Me.Currency = currency
        Me.User = user
        Me.SellerChannel = sellerChannel
        Me.Priceband = priceBand
        Me.Culture = culture
        Me.mfrCode = mfrCode

        For Each r In roles
            Me.i_roles_code.Add(r.Code, r)
        Next


        If wc Is Nothing Then
            Dim sql$
            sql$ = "INSERT INTO [Account] (FK_user_id,Password,fk_team_id,fk_language_id,fk_currency_id,fk_channel_id_buyer,fk_channel_id_seller,priceBand, fk_culture_id,mfrCode) "
            sql$ &= "VALUES (" & user.ID & "," & da.SqlEncode(Password) & "," & teamOrNull & "," & Language.ID & "," & currency.ID & "," & BuyerChannel.ID & "," & sellerChannel.ID & "," & da.SqlEncode(priceBand.text) & "," & Me.Culture.ID & "," & da.SqlEncode(Me.mfrCode) & ");"

            Me.ID = da.DBExecutesql(sql$, True)

            For Each r In Me.i_roles_code.Values
                sql$ = "INSERT INTO AccountRoles VALUES (" & Me.ID & "," & r.ID & ")"
                da.DBExecutesql(sql$)
            Next


        Else
            Me.ID = nextid
            nextid += 1

            Dim row As System.Data.DataRow
            row = wc.NewRow()
            row("ID") = Me.ID '- we EXPLICITLY set ids on branches
            row("FK_User_id") = Me.User.ID
            row("password") = Me.Password
            If Me.Team Is Nothing Then
                row("fk_team_id") = DBNull.Value
            Else
                row("fk_team_id") = Me.Team.ID
            End If

            row("fk_language_id") = Me.Language.ID
            row("fk_currency_id") = Me.Currency.ID  'Multiple currency from one disti are not yet supported (or required) - but probably will be
            row("fk_channel_id_buyer") = BuyerChannel.ID
            row("fk_channel_id_seller") = sellerChannel.ID
            row("priceBand") = Me.Priceband
            row("mfrCode") = Me.mfrCode

            row("fk_culture_id") = Me.Culture.ID

            wc.Rows.Add(row)

        End If
        Me.User.Accounts.Add(Me.ID, Me)

        'iq.Users(user.ID).Accounts.Add(Me.ID, Me)
        iq.Accounts.Add(Me.ID, Me) 'add to the MASTER list
        Dim ck$
        ck$ = Me.SellerChannel.Code & "^" & Me.Priceband.text
        If Not iq.i_Account_HostIDpriceBand.ContainsKey(ck$) Then
            iq.i_Account_HostIDpriceBand.Add(ck$, Me) 'Add a compund key of (seller) hostID^accountpriceBand - used during hostPrices import
        Else
            '  Throw New Exception("Duplicate host accountnum compound key:" & ck$)
        End If

        sellerChannel.CustomerAccounts.Add(Me.ID, Me)

        Quotes = New Dictionary(Of Integer, clsQuote)
        Me.Impressions = New Dictionary(Of Integer, clsImpression)
        Me.ClickThrus = New Dictionary(Of Integer, clsClickThru)
        NumQuotes = -1

    End Sub

    Public Sub New(Id As Integer, user As clsUser, Password As String, BuyerChannel As clsChannel, roles As clsRole(), Team As clsTeam, Language As clsLanguage, Currency As clsCurrency, sellerchannel As clsChannel, priceBand As clsPriceBand, culture As clsCulture, mfrCode As String)

        Me.i_roles_code = New Dictionary(Of String, clsRole)()
        Me.ID = Id
        Me.Password = Password
        Me.BuyerChannel = BuyerChannel
        Me.Team = Team
        Me.Language = Language
        Me.Currency = Currency
        Me.User = user
        Me.SellerChannel = sellerchannel
        Me.Priceband = priceBand
        Me.Culture = culture
        Me.mfrCode = mfrCode

        For Each r In roles
            Me.i_roles_code.Add(r.Code, r)
        Next
        Me.User.Accounts.Add(Me.ID, Me)

        If sellerchannel Is Nothing Then Stop

        'iq.Users(user.ID).Accounts.Add(Me.ID, Me)
        iq.Accounts.Add(Me.ID, Me) 'add to the MASTER list

        Dim ck$
        ck$ = Me.SellerChannel.Code & "^" & Me.Priceband.text

        If iq.i_Account_HostIDpriceBand.ContainsKey(ck$) Then
            '  Logit(ck$ & " is duplicated ")
        Else
            iq.i_Account_HostIDpriceBand.Add(ck$, Me) 'Add a compund key of (seller) hostID^accountpriceBand - used during hostPrices import
        End If

        sellerchannel.CustomerAccounts.Add(Me.ID, Me)

        Me.Quotes = New Dictionary(Of Integer, clsQuote)
        Me.Impressions = New Dictionary(Of Integer, clsImpression)
        Me.ClickThrus = New Dictionary(Of Integer, clsClickThru)
        NumQuotes = -1

    End Sub

    Public Sub LoadQuotes(BuyerFilter As Integer)

        'loads all quotes pertaining to this agentAccount (into the account.quotes) - AND the root level quotes dictionary
        'NB: Does NOT laod a quotes quoteItems

        Me.Quotes.Clear() 'Clears the quotes collection of this ACCOUNT

        Dim sql As New StringBuilder(String.Empty)
        sql.AppendFormat("{0}", "SELECT [ID],[FK_Account_ID_Agent],[FK_State_ID],[Created],[FK_Account_ID_Buyer],[Locked],[Hidden],[Notes],[Description],[Price],[Version],[FK_Quote_ID_Root],[Name],[FK_Currency_ID],[Reference],[Updated],[FK_import_id],[saved],[totalrebate] ")
        sql.AppendFormat("{0}{1}", " FROM quote WHERE fk_account_id_agent = ", Me.ID)
        If BuyerFilter <> 0 Then
            sql.AppendFormat("{0}{1}", "AND fk_account_id_buyer= " & BuyerFilter.ToString)
        End If


        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase()

        Dim rdr As SqlClient.SqlDataReader
        rdr = da.DBExecuteReader(con, sql.ToString)

        Dim buyerAccount As clsAccount = Nothing
        Dim aQuote As clsQuote
        Dim errorMessages As List(Of String) = New List(Of String)
        Dim rows As Integer = 0
        While rdr.Read
            rows += 1
            If Me.SellerChannel.CustomerAccounts.ContainsKey(CInt(rdr.Item("fk_account_id_agent"))) Then
                'If iq.Accounts.ContainsKey(rdr.Item("fk_account_id_agent")) Then
                buyerAccount = Me.SellerChannel.CustomerAccounts(CInt(rdr.Item("fk_account_id_buyer")))
                Dim notes As nullableString = New nullableString(rdr.Item("Notes"))
                '    If rdr.Item("name") IsNot DBNull.Value Then Stop
                Dim name As nullableString = New nullableString(rdr.Item("name"))
                Dim desc As nullableString = New nullableString(rdr.Item("Description"))
                Dim currency As clsCurrency = iq.Currencies(CInt(rdr.Item("fk_currency_id")))
                'It doesn't need a sellerAccount becuase that's the buyerAccount's FK_Channel_ID_seller
                'Dim culture As clsCulture = iq.Cultures(CInt(rdr.Item("fk_culture_id")))
                With rdr
                    Dim rootQuote As clsQuote = Nothing
                    If buyerAccount.Quotes.ContainsKey(CInt(.Item("fk_quote_id_root"))) Then
                        rootQuote = buyerAccount.Quotes(CInt(.Item("fk_quote_id_root")))
                    End If

                    Dim state As clsState = iq.States(CInt(.Item("fk_state_id")))
                    Dim created As DateTime = CType(rdr.Item("created"), DateTime)
                    Dim updated As DateTime = CType(rdr.Item("updated"), DateTime)
                    Dim saved As Boolean = CType(rdr.Item("saved"), Boolean)
                    Dim locked As Boolean = CType(rdr.Item("locked"), Boolean)
                    Dim hidden As Boolean = CType(rdr.Item("hidden"), Boolean)
                    Dim version As Integer = CInt(.Item("Version"))
                    Dim totalRebate As Decimal = 0
                    If Not IsDBNull(rdr("totalRebate")) Then
                        totalRebate = CDec(rdr("totalrebate"))
                    End If

                    'aQuote = New clsQuote(CInt(.Item("id")), buyerAccount, Me, created, updated, notes, rootQuote, CInt(.Item("Version")), state, New nullablePrice(currency), currency, desc, name, rdr.Item("reference").ToString)
                    Dim quotedprice As NullablePrice = New NullablePrice(rdr.Item("price"), currency, False)
                    aQuote = New clsQuote(CInt(.Item("id")), buyerAccount, Me, rootQuote, created, updated, version, state, quotedprice, currency, locked, hidden, saved, rdr.Item("reference").ToString, name, desc, totalRebate)
                    If Not aQuote.QuotedPrice.isValid Or aQuote.QuotedPrice.NumericValue = 0 Then
                        aQuote.UpdateDescAndPrice() 'fix unsaved/legacy quotes
                    End If


                End With
            Else
                Logit("Missing buyer account for quote " & rdr.Item("Id").ToString)
            End If

        End While

        rdr.Close()

        con.Close()

    End Sub
   
    Public Function insert(ByRef errorMessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsAccount(Me.User, Me.Password, Me.BuyerChannel, Me.i_roles_code.Values.ToArray, Me.Team, Me.Language, Me.Currency, Me.SellerChannel, Me.Priceband, Me.Culture, Me.mfrCode)

    End Function

    Public Sub update(ByRef errorMessages As List(Of String)) Implements i_Editable.update

        'Martin has left - and I don't like the idea of queing all the data - we only need send the id of the object to re-instance from the database to the 'other' machine
        ' simple implemntaion need only deal with the root level dictionaries  

        'Dim toUpdate As List(Of Tuple(Of String, String, Object, Int32?)) = New List(Of Tuple(Of String, String, Object, Int32?))()

        'toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("FK_User_Id", "User", Me.User.ID, Nothing))
        'toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("FK_Channel_Id_Seller", "SellerChannel", Me.SellerChannel.ID, Nothing))
        'toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("Password", "Password", Me.Password, Nothing))
        'toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("fk_team_id", "Team", If(Me.Team Is Nothing, Nothing, CType(Me.Team.ID, Int32?)), Nothing))
        'toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("fk_language_id", "Language", Me.Language.ID, Nothing))
        'toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("fk_currency_id", "Currency", Me.Currency.ID, Nothing))
        'toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("priceBand", "Priceband", Me.Priceband.text, Nothing))
        'toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("Fk_culture_ID", "Culture", Me.Culture.ID, Nothing))

        Dim sql$
        'Try
        '    clsBrokerManager.Update("Accounts(" & Me.ID & ")", toUpdate)
        'Catch ex As Exception
        '    ErrorLog.Add(ex)
        '    'Broker is down, what do we want to do?
        '    'Ultimately this needs to go in a threaded queue and be retried, for now fall back to the original code
        Dim teamID As String
        If Me.Team Is Nothing Then teamID = "null" Else teamID = Me.Team.ID.ToString

        Sql$ = "update [Account] "
        Sql$ &= "SET FK_user_id=" & Me.User.ID & ",fk_Channel_id_seller=" & Me.SellerChannel.ID
        Sql$ &= ",password=" & da.SqlEncode(Me.Password)
        Sql$ &= ",fk_team_id=" & teamID & ",fk_language_id=" & Me.Language.ID & ",fk_currency_id=" & Me.Currency.ID
        Sql$ &= ",priceBand=" & da.SqlEncode(Me.Priceband.text) & ", Fk_culture_ID =" & Me.Culture.ID
        Sql$ &= " WHERE id=" & Me.ID

        da.DBExecutesql(Sql$, False)
        ' End Try
        'If Me.ID < 0 Then Stop

        'Dim teamID As String
        'If Me.Team Is Nothing Then teamID = "null" Else teamID = Me.Team.ID.ToString
        'sql$ = "update [Account] "
        'sql$ &= "SET FK_user_id=" & Me.User.ID & ",fk_Channel_id_seller=" & Me.SellerChannel.ID
        'sql$ &= ",password=" & da.SqlEncode(Me.Password)
        'sql$ &= ",fk_team_id=" & teamID & ",fk_language_id=" & Me.Language.ID & ",fk_currency_id=" & Me.Currency.ID
        'sql$ &= ",priceBand=" & da.SqlEncode(Me.Priceband.Text) & ", Fk_culture_ID =" & Me.Culture.ID
        'sql$ &= " WHERE id=" & Me.ID

        'da.DBExecutesql(sql$, False)

        'Set roles
        Sql$ = "DELETE FROM AccountRoles WHERE FK_Account_Id=" & Me.ID
        da.DBExecutesql(Sql$, False)

        If i_roles_code.Count > 0 Then
            Sql$ = String.Join(";", i_roles_code.Select(Function(irc) "INSERT INTO AccountRoles (FK_Account_Id,FK_Role_Id) VALUES (" & Me.ID & "," & irc.Value.ID & ")"))
            da.DBExecutesql(Sql$, False)
        End If

    End Sub

    ''' <summary>
    ''' Checks id this account has the selected right code in any roles
    ''' </summary>
    ''' <param name="right">Right code as string</param>
    ''' <returns>User has this right</returns>
    ''' <remarks></remarks>
    Public Function HasRight(right As String) As Boolean
        If Not iq.i_right_Code.ContainsKey(right) Then Return False
        Return Me.i_roles_code.SelectMany(Function(rid) rid.Value.Rights.Select(Function(ri) ri.Value)).Contains(iq.i_right_Code(right))
    End Function

    Public Sub AddRole(role As clsRole)
        If Not Me.i_roles_code.ContainsKey(role.Code) Then
            Me.i_roles_code.Add(role.Code, role)

            Dim sql$
            Sql$ = "INSERT INTO AccountRoles (FK_Account_Id,FK_Role_Id) VALUES (" & Me.ID & "," & role.ID & ")"
            da.DBExecutesql(Sql$, False)
        End If

    End Sub
    Public Sub RemoveRole(role As clsRole)
        If Me.i_roles_code.ContainsKey(role.Code) Then
            Me.i_roles_code.Remove(role.Code)

            Dim sql$
            sql$ = "DELETE FROM AccountRoles WHERE fk_Account_id=" & Me.ID & " AND fk_Role_Id=" & role.ID
            da.DBExecutesql(sql$, False)
        End If

    End Sub

    Public Function ResetPassword() As List(Of String)

        Dim pw As String
        pw = GeneratePassword()

        'we do NOT set the password on the account (yet) - the act of clicking the URL (will actually do the reset)
        Dim em As List(Of String) = New List(Of String)
        Dim tags As Dictionary(Of String, String) = New Dictionary(Of String, String)

        Dim baseurl As String = ConfigurationManager.AppSettings("BaseURL")

        'The link specifies the accountID (to reset), the new (salted) hash of the password, and a hash of the pair (as an anti-tamper device)
        'Although this is one place an attacker could see both the plaintext, and the hashed version -the salt makes it impossible to determine anything useful

        tags.Add("url", baseurl & "/aspx/signin.aspx?reset=" & Trim(Me.ID.ToString) & "&pw=" & simpleHash(pw) & "&antiTamper=" & simpleHash(Trim(Me.ID.ToString) & simpleHash(pw)).ToString)

        tags.Add("password", pw)
        tags.Add("firstname", Split(Me.User.RealName, " ")(0))
        tags.Add("hostname", Me.SellerChannel.DisplayName(English))
        tags.Add("email", Me.User.Email)
        tags.Add("mfr", Me.mfrCode)
        tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English))

        SendEmail(Me.User.Email, "forgotten.htm", tags, Me.Language, em, False)

        Return em

    End Function

    Public Function ResendWelcomeEmail() As List(Of String)
        ResendWelcomeEmail = New List(Of String)()
        Dim baseurl As String = ConfigurationManager.AppSettings("BaseURL")
        Dim pw As String
        pw = GeneratePassword()
        Dim tags As Dictionary(Of String, String) = New Dictionary(Of String, String)

        Dim url As String
        url = baseurl & "signin.aspx"
        tags.Add("hostname", SellerChannel.DisplayName(Language))
        tags.Add("email", User.Email)
        tags.Add("password", pw$)
        tags.Add("firstname", User.RealName)
        tags.Add("url", url)
        tags.Add("mfr", Me.mfrCode)
        tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English))

        SendEmail(User.Email, "WelcomeEmail.htm", tags, Language, ResendWelcomeEmail, True)
    End Function

    Public ReadOnly Property Manufacturer As Manufacturer

        Get

            Manufacturer = Manufacturer.Unknown

            If String.Equals(Me.mfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase) Then
                Manufacturer = Manufacturer.HPI
            ElseIf String.Equals(Me.mfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase) Then
                Manufacturer = Manufacturer.HPE
            End If

        End Get

    End Property

    Public ReadOnly Property ManufacturerDescription As String

        Get

            ManufacturerDescription = String.Empty

            If Me.Manufacturer = Global.IQ.Manufacturer.HPE Then
                ManufacturerDescription = "Hewlett Packard Enterprise"
            ElseIf Me.Manufacturer = Global.IQ.Manufacturer.HPI Then
                ManufacturerDescription = "HP Inc."
            End If

        End Get

    End Property

End Class 'End of clsAccount



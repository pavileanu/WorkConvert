Imports System.Text.RegularExpressions
Imports dataAccess
Imports System.Data
Imports System.Security.Cryptography
Imports System.Runtime.Serialization
Imports System.ServiceModel
Imports System.ServiceModel.Activation


<DataContractAttribute()> _
Public Class clsNameValuePair
    <DataMemberAttribute()> _
    Public Name As String
    <DataMemberAttribute()> _
    Public value As String
End Class

<DataContractAttribute()> _
Public Class clsToken
    <DataMemberAttribute()> _
    Public Value As String
    <DataMemberAttribute()> _
    Public Errors As List(Of String)
End Class


<DataContractAttribute()> _
Public Class clsGKAccount

    <DataMemberAttribute()> Public sc_hostCode As String
    <DataMemberAttribute()> Public u_Name As String
    <DataMemberAttribute()> Public u_email As String
    <DataMemberAttribute()> Public a_PriceBand As String
    <DataMemberAttribute()> Public u_Telephone As String
    <DataMemberAttribute()> Public bc_CompanyName As String
    <DataMemberAttribute()> Public bc_PostCode As String

    Public Sub New(sc_hc As String, u_name As String, u_email As String, a_priceband As String, u_telephone As String, bc_CompanyName As String, bc_postcode As String)

        Me.sc_hostCode = sc_hc
        Me.u_Name = u_name
        Me.u_email = u_email
        Me.a_PriceBand = a_priceband
        Me.u_Telephone = u_telephone
        Me.bc_PostCode = bc_postcode
        Me.bc_CompanyName = bc_CompanyName

    End Sub

End Class


<DataContractAttribute()> _
Public Class clsName
    Public ID As Integer
    <DataMemberAttribute()> Public name As String
    <DataMemberAttribute()> Public Example As String
    <DataMemberAttribute()> Public Required As Boolean
    <DataMemberAttribute()> Public RegEx As String
    <DataMemberAttribute()> Public MinLength As Integer
    <DataMemberAttribute()> Public MaxLength As Integer
    <DataMemberAttribute()> Public Notes As String

    Public Sub New(ID As Integer, Name As String, Example As String, Required As Boolean, RegEx As String, MinLength As Integer, MaxLength As Integer, Notes As String)

        Me.ID = ID
        Me.name = Name
        Me.Example = Example
        Me.Required = Required
        Me.RegEx = RegEx
        Me.MinLength = MinLength
        Me.MaxLength = MaxLength
        Me.Notes = Notes

    End Sub

End Class




<AspNetCompatibilityRequirements(RequirementsMode:=AspNetCompatibilityRequirementsMode.Allowed)> _
Public Class TokenFactory
    Implements IOneTimeToken


    ''' <summary>Fetches a diictionary of all permissible Name-Value-Pair Names, including their validation rules</summary>
    ''' <returns>Dictionary Name>clsName</returns>

    Private Function LoadNames() As Dictionary(Of String, clsName)

        LoadNames = New Dictionary(Of String, clsName)(StringComparer.CurrentCultureIgnoreCase)
        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase()
        Dim sql$ = "SELECT ID,Name,Example,Required,RegEx,MinLength,MaxLength,Notes FROM gk.name"

        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql$)
        While rdr.Read
            If LoadNames.ContainsKey(rdr.Item("Name")) Then Throw New Exception("The NAME " & rdr.Item("Name") & " is defined more than once !")
            LoadNames.Add(rdr.Item("Name"), New clsName(rdr.Item("ID"), rdr.Item("Name"), rdr.Item("Example"), rdr.Item("Required"), rdr.Item("Regex"), rdr.Item("MinLength"), rdr.Item("Maxlength"), rdr.Item("Notes")))
        End While
        rdr.Close()
        con.Close()

    End Function

    Public Function GetToken(HostId As String, HostToken As String, NameValuePairs As List(Of clsNameValuePair)) As clsToken Implements IOneTimeToken.GetToken

        'Gives us the names and validation rules for each possible Name-Value Pair
        Dim names As Dictionary(Of String, clsName) = LoadNames()

        Dim retval As clsToken = New clsToken()
        retval.Value = ""
        retval.Errors = New List(Of String)

        Try

            'Convert the supplied 'Pairs' into a Dictionary of name>value (it's a shame we cant expose dictionaries via SOAP webservices !)
            Dim NVPs As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase)
            For Each p In NameValuePairs
                If NVPs.ContainsKey(LCase(p.Name)) Then
                    retval.Errors.Add("You have provided more than one value for " & p.Name & " (max one).")
                End If
                NVPs.Add(LCase(p.Name), p.value)
            Next

            If Not iq.i_channel_code.ContainsKey(HostId) Then
                retval.Errors.Add("HostID " & HostId & " is not recognised.(object model not loaded?) - there are " & iq.Channels.Count & " channels loaded")
            Else
                Dim channel As clsChannel = iq.i_channel_code(HostId)
                If HostToken <> channel.WebToken Then
                    retval.Errors.Add("Invalid password ('fixed' token) - logon is forbidden for 5 seconds") 'that's a lie
                Else
                    For Each name In names.Values
                        If name.Required And Not NVPs.ContainsKey(name.name) Then
                            retval.Errors.Add("A Name/value pair for '" & name.name & "' is required. for example " & name.Example & " Help:" & name.Notes)
                        ElseIf NVPs.ContainsKey(name.name) Then
                            If name.Required And Trim$(NVPs(name.name)) = "" Then
                                retval.Errors.Add("You have supplied an empty value for for '" & name.name & "' which is a required field. for example " & name.Example & " Help:" & name.Notes)
                            Else
                                Dim value As String = NVPs(name.name)
                                If (Not name.Required) And (value = "") Then
                                    'Skip emmpty non required fields
                                Else
                                    'but validate it if it's required OR populated
                                    If Len(value) < name.MinLength Then
                                        retval.Errors.Add("The value '" & value & "' for '" & name.name & "' must be at least " & name.MinLength & " characters." & name.Notes)
                                    ElseIf Len(value) > name.MaxLength Then
                                        retval.Errors.Add("The value '" & value & "' for '" & name.name & "' must a maximum of " & name.MinLength & " characters." & name.Notes)
                                    End If
                                    If name.RegEx <> "" Then
                                        If Not Regex.IsMatch(value, name.RegEx) Then
                                            retval.Errors.Add("The value '" & value & "' for '" & name.name & "' fails the Regular Expression " & name.RegEx & " " & name.Notes)
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    Next

                    If NVPs.ContainsKey("base") Then
                        If NVPs("base") <> "" Then
                            If Not iq.i_SKU.ContainsKey(NVPs("Base")) Then
                                retval.Errors.Add(NVPs("Base") & " is not a valid HP part Number")
                            End If
                        End If
                    End If

                    If retval.Errors.Count = 0 Then
                        Dim token As String = makeToken()
                        Dim tokenID As Integer = WriteToken(token, channel)

                        Try
                            WriteValues(tokenID, NVPs, names)
                            retval.Value = token
                        Catch ex As System.Exception
                            retval.Errors.Add("Error writing values (field overflow ?) " & ex.Message.ToString)
                            If Not ex.InnerException Is Nothing Then
                                retval.Errors.Add(ex.InnerException.ToString)
                            End If
                        End Try
                    End If
                End If
            End If

        Catch ex As Exception

            retval.Errors.Add(ex.Message & " " & ex.StackTrace)

        End Try

        Return retval

    End Function

    Public Function Help() As List(Of clsName) Implements IOneTimeToken.Help

        Help = New List(Of clsName)
        Dim dicNames As Dictionary(Of String, clsName) = LoadNames()

        For Each name In dicNames.Values
            Help.Add(name)
        Next

    End Function

    '''<summary>Writes the suplied random token to the databse</summary>
    ''' <returns>The Integer ID of the token</returns>

    Private Function WriteToken(token As String, Channel As clsChannel) As Integer

        WriteToken = da.DBExecutesql("INSERT INTO GK.Token (Token,Timestamp,FK_Channel_ID_Host) VALUES (" & da.SqlEncode(token) & ",getdate()," & Channel.ID & " );", True)

    End Function

    Private Sub WriteValues(TokenID As Integer, NVPs As Dictionary(Of String, String), names As Dictionary(Of String, clsName))

        'we don't really need to persist the vaues - or the tokens

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()

        Dim dt As DataTable = da.MakeWriteCacheFor(con, "gk.value")

        Dim dr As DataRow
        For Each k In NVPs.Keys

            If names.ContainsKey(k) Then  'They may have submitted some unknown values (UserID)
                dr = dt.NewRow
                dt.Rows.Add(dr)
                dr.Item("FK_Name_id") = names(k).ID
                dr.Item("FK_Token_id") = TokenID
                dr.Item("Value") = NVPs(k)
            End If
        Next

        da.BulkWrite(con, dt, "gk.value")
        con.Close()

    End Sub

    Private Function makeToken() As String

        'The RND() function is not cryptographically strong - It is possible to work out the seed (system timestamp) for a given set of pseudorandom numbers.. then by 'synchronsing watches' with the server - one could generate many tokens - and have a small chance of predicting (and using) the next token generated.
        'whilst we could obscure things with XORs or Hashes or different seeds ... it's better to use a 'real' random key generated from the Cryptographic Service Provider.

        'hence 
        Dim rngCsp As New RNGCryptoServiceProvider()
        Dim bytes(19) As Byte
        rngCsp.GetBytes(bytes)

        For i = 0 To 19
            bytes(i) = 65 + bytes(i) / 255 * 25

        Next
        makeToken = Encoding.ASCII.GetString(bytes)

    End Function

    Public Function GetUserDetails(HostCode As String, Email As String, Priceband As String) As clsGKAccount Implements IOneTimeToken.GetUserDetails

        'for synnex their USERID is stored in the account.priceband (formerly HostAccountNum)
        GetUserDetails = Nothing
        Dim j = From a In iq.Accounts.Values Where a.SellerChannel.Code = HostCode And (a.User.Email = Email Or (a.Priceband.text = Priceband And Priceband <> ""))

        If j.Any Then

            Dim ac As clsAccount = j.First
            With ac
                GetUserDetails = New clsGKAccount(.SellerChannel.Code, .User.RealName, .User.Email, .Priceband.text, .User.tel1.DisplayValue, .BuyerChannel.Name, .BuyerChannel.Address)
            End With
        End If

    End Function

End Class



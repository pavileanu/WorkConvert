Imports dataAccess

Public Class clsUser

    Implements IQ.i_Editable  'this defines a set of function this class *must* implement/expose

    Property ID As Integer
    'Public UserName As String - use email !
    Property RealName As String

    Property Email As String
    Property tel1 As nullableString
    Property tel2 As nullableString
    Property Disabled As Boolean
    Property Channel As clsChannel
    Property Accounts As Dictionary(Of Integer, clsAccount)

    'Public Buyer As clsChannel 'Works for


    Public Sub CountQuotesPerAccount()

        'Populates numquotes for each user.account

        Dim sql$
        sql$ = "SELECT fk_account_id_agent as AcID,count(*) AS c FROM quote q JOIN account a ON a.id=q.fk_account_id_agent "
        sql$ &= "WHERE a.fk_user_id = " & Me.ID 'user id
        sql$ &= " GROUP BY fk_account_id_agent "

        For Each ac In Me.Accounts.Values
            ac.NumQuotes = 0  'Zero them all (becuase they're not all in the recordset)
        Next

        Dim con As SqlClient.SqlConnection
        con = da.opendatabase()
        Dim rdr As SqlClient.SqlDataReader
        rdr = da.DBExecuteReader(con, sql$)
        If rdr.HasRows Then
            While rdr.Read
                'It's possible that this OM has no knoweledge of an account created in the database by external means (ie. another instance point at the same database)
                If Me.Accounts.ContainsKey(rdr.Item("acid")) Then  '
                    Me.Accounts(rdr.Item("ACiD")).NumQuotes = rdr.Item("c")
                End If
            End While
        End If

        rdr.Close()
        con.Close()

    End Sub

    Public Sub New() 'Parameterless constructor is called by the editor - which subsequently sets defualt values for most properties

        Me.ID = -1
        Me.Accounts = New Dictionary(Of Integer, clsAccount)

    End Sub

    Public Function displayName(Language As clsLanguage) As String Implements i_Editable.displayName

        displayName = Me.RealName

    End Function

    Public Function Insert(ByRef errormessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsUser(Me.Channel, Me.Email, Me.RealName, Me.tel1, Me.tel2)

    End Function


    Public Sub New(channel As clsChannel, ByVal email As String, RealName As String, tel1 As nullableString, tel2 As nullableString, Optional uwc As DataTable = Nothing, Optional ByRef nextID As Integer = -1)

        Me.Channel = channel
        ' Me.UserName = Username
        Me.RealName = RealName
        Me.Email = email
        Me.tel1 = tel1
        Me.tel2 = tel2
        Me.Disabled = False

        'Me.Buyer = BuyerChannel

        If uwc Is Nothing Then

            Dim sql$
            sql$ = "INSERT INTO [User] (realname,email,tel1,tel2,fk_channel_id) "
            sql$ &= " VALUES (" & da.SqlEncode(RealName) & "," & da.SqlEncode(email, True) & ","
            sql$ &= tel1.sqlValue & "," & tel2.sqlValue & "," & channel.ID & ");" 'NB: SqlValue also SQLencodes

            Me.ID = da.DBExecutesql(sql$, True)

        Else
            Me.ID = nextID
            nextID += 1

            Dim row As System.Data.DataRow
            row = uwc.NewRow()

            row("realname") = RealName
            row("email") = email
            row("tel1") = tel1.value.ToString()
            row("tel2") = tel2.value.ToString()
            row("disabled") = Disabled
            row("fk_channel_id") = channel.ID
            uwc.Rows.Add(row)
        End If

        If Not iq.i_user_email.ContainsKey(LCase(Trim$(Me.Email))) Then
            iq.i_user_email.Add(LCase(Trim$(Me.Email)), Me)
            iq.Users.Add(Me.ID, Me) 'add to the MASTER list
        Else
            Beep()
        End If

        Accounts = New Dictionary(Of Integer, clsAccount)
        Me.Channel.Users.Add(Me.ID, Me)

    End Sub
    Public Sub delete(ByRef errormessages As List(Of String)) Implements i_Editable.delete

        'Deleteing users would require deleing their quotes, accounts and other descendant objects
        'IQ.Users.REMOVE

        errormessages.Add("Users cannot be deleted - because of dependencies (quotes etc)")

    End Sub

    Public Sub New(ByVal id As Integer, channel As clsChannel, ByVal email As String, RealName As String, tel1 As nullableString, tel2 As nullableString, disabled As Boolean)

        Me.ID = id
        'Me.UserName = Username
        Me.RealName = RealName
        Me.Email = email
        Me.tel1 = tel1
        Me.tel2 = tel2
        Me.Disabled = disabled
        Me.Channel = channel

        'Me.Buyer = Buyer

        iq.Users.Add(Me.ID, Me) 'add to the MASTER list

        If Not iq.i_user_email.ContainsKey(Trim$(LCase(Me.Email))) Then
            iq.i_user_email.Add(LCase(Trim$(Me.Email)), Me)
        Else
            ' Stop - this email is not unique
        End If

        Accounts = New Dictionary(Of Integer, clsAccount)
        Me.Channel.Users.Add(Me.ID, Me)
        Me.Accounts = New Dictionary(Of Integer, clsAccount)

    End Sub

    Public Sub update(ByRef errormessages As List(Of String)) Implements i_Editable.update


        'Dim toUpdate As List(Of Tuple(Of String, String, Object, Int32?)) = New List(Of Tuple(Of String, String, Object, Int32?))()

        'toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("Disabled", "Disabled", IIf(Me.Disabled, 1, 0), Nothing))
        'toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("Email", "Email", Me.Email, Nothing))
        'toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("RealName", "RealName", Me.RealName, Nothing))
        'toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("tel1", "tel1", Me.tel1.v, Nothing))
        'toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("tel2", "tel2", Me.tel2.v, Nothing))

        'Try
        '    clsBrokerManager.Update("Users(" & Me.ID & ")", toUpdate)
        'Catch ex As Exception
        'ErrorLog.Add(ex)
        '            If Me.ID = -1 Then Stop

        Dim sql$
        If Me.Email.Length > 100 Or Me.RealName.Length > 100 Then errormessages.Add("Email and Real Name length must be less than 100")
        If Me.tel1.value.ToString().Length > 50 Or Me.tel2.value.ToString().Length > 50 Then errormessages.Add("Telephone numbers must be less than 50")

        sql$ = "UPDATE [User] SET disabled=" & IIf(Me.Disabled, 1, 0) & ",email=" & da.SqlEncode(Me.Email) & ",realname=" & da.SqlEncode(Me.RealName, True) & ",tel1=" & Me.tel1.sqlValue & ",tel2=" & Me.tel2.sqlValue & " WHERE ID=" & Me.ID
        da.DBExecutesql(sql$)
        '  End Try
    End Sub

End Class  'User

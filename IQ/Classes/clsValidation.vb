Imports dataAccess

Public Class clsValidation

    Property ID As String
    Property description As String
    Property regEx As String
    Property ViolationMessage As String

    Public Sub New()

    End Sub

    Public Function Insert() As clsValidation
        Return New clsValidation(Me.description, Me.regEx, Me.ViolationMessage)
    End Function

    Public ReadOnly Property DisplayName(Language As clsLanguage)

        Get
            Return Me.description
        End Get

    End Property

    Public Sub New(id As Integer, description As String, regex As String, violation As String)

        Me.ID = id
        Me.description = description
        Me.regEx = regex
        Me.ViolationMessage = violation

        iq.Validations.Add(Me.ID, Me)

    End Sub

    Public Sub New(description As String, regex As String, violation As String)

        Me.description = description
        Me.regEx = regex
        Me.ViolationMessage = violation

        Dim sql$
        sql$ = "INSERT INTO [validation] (descripion,regex,violation) VALUES(" & da.SqlEncode(Me.description) & "," & da.SqlEncode(Me.regEx) & "," & da.SqlEncode(Me.ViolationMessage) & ");"
        Me.ID = da.DBExecutesql(sql$, True)

        iq.Validations.Add(Me.ID, Me)

    End Sub

    Public Sub update()

        Dim sql$
        sql$ = "UPDATE [validation] set "
        sql$ &= "description=" & da.SqlEncode(Me.description) & ","
        sql$ &= "regex=" & da.SqlEncode(Me.regEx) & ","
        sql$ &= "viloationmessage=" & da.SqlEncode(Me.ViolationMessage)

        da.dbexecutesql(sql$, False)

    End Sub




End Class

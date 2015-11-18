Imports dataAccess
Imports System.Runtime.Serialization

<DataContract()> Public Class clsInputType

    Property ID As Integer
    Property code As String
    Property name As String

    Public ReadOnly Property displayName(langauge As clsLanguage)
        Get
            Return Me.name & " (" & Me.code & ")"
        End Get
    End Property

    Public Sub New(id As Integer, code As String, name As String)

        Me.ID = id
        Me.code = code
        Me.name = name

        iq.InputTypes.Add(Me.ID, Me)
        iq.i_inputType_code.Add(Me.code, Me)

    End Sub



    Public Sub New(code As String, name As String)

        Dim sql$
        sql$ = "INSERT INTO [InputType] (code,name) values (" & da.SqlEncode(code) & "," & da.SqlEncode(name) & ");"

        Me.ID = da.DBExecutesql(sql$, True)
        Me.code = code
        Me.name = name

        iq.InputTypes.Add(Me.ID, Me)
        iq.i_inputType_code.Add(Me.code, Me)

    End Sub
End Class

Imports dataAccess
Imports System.Runtime.Serialization

<DataContract()>
Public Class clsLanguage
    Property ID As Integer
    Property Code As String
    Property LocalName As String
    Property RTL As Boolean
    Property Live As Boolean
    Property Active As Boolean


    Public ReadOnly Property displayName(language As clsLanguage)
        Get
            displayName = Me.LocalName & " (" & Me.Code & ")"
        End Get

    End Property

    Public Sub New()
        'required for reflection
    End Sub

    Public Sub New(ByVal id As Integer, ByVal code As String, ByVal LocalName As String, ByVal RTL As Boolean, ByVal live As Boolean, ByVal active As Boolean)

        'This is an overriden constructor - becuase the ID *is* specified - it *knows* we' dont want to do a database insert
        Me.ID = id
        Me.Code = code
        Me.LocalName = LocalName
        Me.RTL = RTL
        Me.Live = live
        Me.Active = active

        iq.Languages.Add(Me.ID, Me)  'add this language to the master list
        iq.i_language_Code.Add(Me.Code, Me)

    End Sub
    Public Sub New(ByVal code As String, ByVal LocalName As String, ByVal RTL As Boolean, ByVal live As Boolean, ByVal active As Boolean)

        'Creates a new (instance of the class cls)Language - populates its ID

        Dim sql$
        sql$ = "INSERT INTO [language] ([code],[LocalName],[RTL],[live],[active]) VALUES (" & da.SqlEncode(code) & "," & da.SqlEncode(LocalName) & "," & CInt(RTL) & "," & CInt(live) & "," & CInt(active) & ");"

        Me.ID = da.DBExecutesql(sql$, True)
        Me.Code = code
        Me.LocalName = LocalName
        Me.RTL = RTL
        Me.Live = live
        Me.Active = active
        iq.Languages.Add(Me.ID, Me)  'add this language to the master list
        iq.i_language_Code.Add(Me.Code, Me)

    End Sub



End Class

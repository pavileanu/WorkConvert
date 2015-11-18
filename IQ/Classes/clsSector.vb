Imports dataAccess

Public Class clsSector

    Property ID As Integer
    Property code As String
    Property Translation As clsTranslation

    Dim currentCode As String

    Public ReadOnly Property DisplayName(language As clsLanguage)
        Get
            Return Me.Translation.text(language) & " (" & Me.code & ")"
        End Get
    End Property


    Public Sub New(Code As String, translation As clsTranslation)

        Dim sql$
        sql$ = "INSERT INTO [Sector] (code,fk_Translation_key_name) "
        sql$ &= " values (" & da.SqlEncode(Code) & "," & translation.Key & ");"

        Me.ID = da.DBExecutesql(sql$, True)
        Me.code = Code
        Me.Translation = translation

        iq.Sectors.Add(Me.ID, Me)
        iq.i_sector_code.Add(Me.code, Me)
        currentCode = Me.code


    End Sub

    Public Function Insert()

        Return New clsSector(Me.code, Me.Translation)

    End Function

    Public Sub New(Id As Integer, Code As String, translation As clsTranslation)

        Me.ID = Id
        Me.code = Code
        Me.Translation = translation

        iq.Sectors.Add(Me.ID, Me)
        iq.i_sector_code.Add(Me.code, Me)
        currentCode = Me.code

    End Sub

    Public Function shortCode() As String ' a really dirty fix to back match with IQ1 for gregs snapshots/comparisons
        'DON NOT USE THIS FOR ANYTHING ELSE

        If Me.code.Contains("ISS") Then Return "SVR"
        If Me.code.Contains("SWD") Then Return "SWD"
        If Me.code.Contains("BCS") Then Return "BCS"
        If Me.code.Contains("COM") Then Return "COM"
        If Me.code.Contains("NET") Then Return "NET"

        Return Me.code


    End Function



    Public Sub update()

        Dim sql$
        sql$ = "UPDATE [Sector] set code='" & Me.code & "',fk_translation_key_name=" & Me.Translation.Key & " WHERE id=" & Me.ID

        iq.i_sector_code.Remove(currentCode)

        currentCode = Me.code
        iq.i_sector_code.Add(currentCode, Me)

        da.DBExecutesql(sql$)

    End Sub

End Class

Imports dataAccess

Public Class clsSlotType

    'Branches have slots of one or more types (see clsbranch.slots)
    'quantities attach to 'child' branches to 'consume' slots of a given type in a give 'parent' branch
    'NB: 'parent' and 'child' in this discussion are not strictly parent or child.. a (typically) outer branch consumes the slots of some (typically) inner branch 
    '- as specified by the quantity.TakesSlotsIn

    Property ID As Integer
    Property MajorCode As String
    Property MinorCode As String
    Property Translation As clsTranslation 'this is what's displayed
    Property TranslationShort As clsTranslation
    Property Fallback As SortedDictionary(Of Integer, clsSlotType)  'Which type of slot should(can) we use if this type is unavialble - eg a 4x PCI card would fall back to an 8x slot (seems backwards - but we want to occupy the least 'expensive' slots first)
    Property EnforceMinorCode As Boolean

    Public ReadOnly Property displayName(language As clsLanguage) As String
        Get
            Return Me.MajorCode & "/" & Me.MinorCode
        End Get

    End Property

    Public ReadOnly Property shortDisplayName(language As clsLanguage) As String
        Get
            If Me.TranslationShort IsNot Nothing Then Return Me.TranslationShort.text(language) Else Return displayName(language)
        End Get
    End Property

    Public Sub New(MajorCode As String, MinorCode As String)
        'Must be a dummy
        Me.MajorCode = MajorCode
        Me.MinorCode = MinorCode
        ID = iq.SlotTypes.Min(Function(g) g.Key) - 1
        If Not iq.i_slotType_Code.ContainsKey(Me.MajorCode) Then iq.i_slotType_Code(Me.MajorCode).Add(Me.MajorCode.ToUpper, Me)
        iq.i_slotType_Code(Me.MajorCode).Add(Me.MinorCode.ToUpper, Me)
        iq.SlotTypes.Add(ID, Me)
    End Sub


    Public Sub New(id As Integer, MajorCode As String, MinorCode As String, translation As clsTranslation, translationShort As clsTranslation, EnforceMinorCode As Boolean)

        Me.ID = id
        Me.MajorCode = MajorCode.ToUpper
        Me.MinorCode = MinorCode.ToUpper
        Me.TranslationShort = translationShort
        Me.Translation = translation
        Me.Fallback = New SortedDictionary(Of Integer, clsSlotType)
        Me.EnforceMinorCode = EnforceMinorCode

        iq.SlotTypes.Add(Me.ID, Me)

        If Not iq.i_slotType_Code.ContainsKey(Me.MajorCode) Then
            iq.i_slotType_Code.Add(Me.MajorCode, New Dictionary(Of String, clsSlotType)(StringComparer.InvariantCultureIgnoreCase))
        End If
        If Not iq.i_slotType_Code(Me.MajorCode).ContainsKey(MinorCode) Then
            iq.i_slotType_Code(Me.MajorCode).Add(Me.MinorCode, Me)
        Else
            Beep()
        End If



    End Sub

    Public Sub New(majorCode As String, minorCode As String, translation As clsTranslation)

        Dim sql$
        sql$ = "INSERT INTO SlotType(fk_translation_key,majorCode,MinorCode) VALUES (" & translation.Key & "," & da.SqlEncode(majorCode) & "," & da.SqlEncode(minorCode) & ");"

        Me.ID = da.DBExecutesql(sql$, True)
        Me.MajorCode = majorCode.ToUpper
        Me.MinorCode = minorCode.ToUpper

        Me.Translation = translation
        Me.Fallback = New SortedDictionary(Of Integer, clsSlotType)

        iq.SlotTypes.Add(Me.ID, Me)

        If Not iq.i_slotType_Code.ContainsKey(Me.MajorCode) Then
            iq.i_slotType_Code.Add(Me.MajorCode, New Dictionary(Of String, clsSlotType)(StringComparer.InvariantCultureIgnoreCase))
        End If
        If Not iq.i_slotType_Code(Me.MajorCode).ContainsKey(minorCode) Then
            iq.i_slotType_Code(Me.MajorCode).Add(Me.MinorCode, Me)
        Else
            Beep()
        End If

    End Sub

    Public Function Insert() As clsSlotType

        Return New clsSlotType(Me.MajorCode, Me.MinorCode, Me.Translation)

    End Function

    Public Sub Update()

        Dim sql$
        sql$ = "UPDATE slottype set majorcode=" & da.SqlEncode(Me.MajorCode) & ",minorcode=" & da.SqlEncode(Me.MinorCode) & _
            ",fk_translation_key=" & Me.Translation.Key & ",fk_translation_key_short=" & If(Me.TranslationShort Is Nothing, "null", Me.TranslationShort.Key)
        sql$ &= " WHERE ID = " & Me.ID
        da.dbexecutesql(sql$, False)

    End Sub

    Public Function Delete() As Boolean

        Dim sql$
        sql$ = "Delete from slottype where id=" & Me.ID

        Try
            'this may fail due to RI
            da.dbexecutesql(sql$, False)

            iq.SlotTypes.Remove(Me.ID)
            Return True

        Catch ex As Exception

            Return False

        End Try

    End Function

    Sub AddFallback(pos As Int32, st As clsSlotType)
        Fallback.Add(pos, st)
        Dim sql = "INSERT INTO altSlotType VALUES (" & Me.ID & "," & st.ID & "," & pos & ")"
        da.DBExecutesql(sql)
    End Sub

End Class  'clsSlotType

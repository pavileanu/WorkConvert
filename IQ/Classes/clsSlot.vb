Imports dataAccess
Imports System.Xml

Public Class clsSlot
    Implements i_Editable

    'Physical slots in a machine

    Property ID As Integer
    Property path As String          'OPTIONAL where (the context in which) this 'gives' works  - leave blank for it to work wherever it is grafted
    Property Branch As clsBranch  'the branch (which may appear in many locations in the tree) to which these slots apply - single branch can have many of the same slot type with different paths (one for each of the positiosn it's grafted at)
    Property Type As clsSlotType

    Property deleted As Boolean

    Public ReadOnly Property NonStrictType As clsSlotType
        Get
            If iq.StrictSlotValidation Or Type.EnforceMinorCode Then
                Return Type
            Else
                Dim st As clsSlotType
                If iq.i_slotType_Code(Type.MajorCode).ContainsKey("") Then
                    st = iq.i_slotType_Code(Type.MajorCode)("")
                Else
                    st = New clsSlotType(Type.MajorCode, "") With {.Translation = If(Type.TranslationShort IsNot Nothing, Type.TranslationShort, Type.Translation), .TranslationShort = Type.TranslationShort}
                End If
                'Dim st = iq.SlotTypes.Where(Function(slt) slt.Value.MajorCode = Type.MajorCode AndAlso slt.Value.MinorCode = If(Type.TranslationShort IsNot Nothing, Type.TranslationShort.Key.ToString, "")).FirstOrDefault

                'Return If(st.Value Is Nothing, , st.Value)
                Return st
            End If
        End Get
    End Property

    Property numSlots As Integer     'slots given + / - taken (per item) 
    Property notes As clsTranslation
    Property slotNum As IQ.NullableInt     ' for 'gives' slots you *can* specify the slot number 
    '                               (and must do so, if specifying more than one slot of the same type in the same product) 
    '                               numslots MUST be 1 if slotNum is specified
    '                               slotnum MUST be null for 'takes' slots (there is no functionality to specify that a particular card must go in a particular slot)

    Property requiredFill As Integer ' the number of "given" slots (of this type) that *must* be filled - eg.. you MUST have a PSU in certain servers
    Property advisedFill As Integer

    Public CurrentCompoundKey As String

    Function clone(newpath$) As clsSlot

        Return New clsSlot(Me.Type, Me.Branch, newpath, Me.numSlots, Me.notes, Me.slotNum, Me.requiredFill, Me.advisedFill)

    End Function

    Public Function writeXml(W As xmltextwriter)

        With Me

            W.WriteStartElement("slot")

            W.WriteStartAttribute("id")
            W.WriteString(.ID.ToString)
            W.WriteEndAttribute()

            W.WriteStartAttribute("majorCode")
            W.WriteString(.Type.MajorCode.ToString)
            W.WriteEndAttribute()

            W.WriteStartAttribute("minorCode")
            W.WriteString(.Type.MinorCode.ToString)
            W.WriteEndAttribute()

            W.WriteStartAttribute("numSlots")
            W.WriteString(.numSlots.ToString)
            W.WriteEndAttribute()

            If .notes IsNot Nothing Then
                W.WriteStartAttribute("notes")
                W.WriteString(.notes.text(English))
                W.WriteEndAttribute()
            End If
            If .slotNum.sqlvalue <> "null" Then
                W.WriteStartAttribute("slotNum")
                W.WriteString(.slotNum.sqlvalue)
                W.WriteEndAttribute()
            End If

            W.WriteEndElement() '/slot


            'Return String.Format("<slot id='{0}' majorType='{1}' minorType='{2}' numSlots='{3}' notes='{4}' slotNum'{5}'/>" _
            '                              , .ID, _
            '                              xmlEncode(.Type.MajorCode), _
            'xmlEncode(.Type.MinorCode), _
            '                          .numSlots, _
            '                        If(.notes Is Nothing, "", xmlEncode(.notes.text(English))), _
            '                      .slotNum.sqlvalue)
        End With

    End Function

    Sub New(type As clsSlotType, ByVal Branch As clsBranch, ByVal Path As String, numslots As Integer, notes As clsTranslation, slotnum As IQ.NullableInt, requiredfill As Integer, advisedFill As Integer, Optional writecache As DataTable = Nothing)

        Me.path = Path
        Me.Branch = Branch
         Me.Type = type
        Me.numSlots = numslots
        Me.notes = notes
        Me.slotNum = slotnum
        Me.requiredFill = requiredfill
        Me.advisedFill = advisedFill


        If type.ID <= 0 Then Stop

        '        If Val(slotnum.sqlvalue) > 100 Then Stop

        If Branch IsNot Nothing AndAlso Branch.ID > 0 Then  'A temproary (branchless) slot is created during import - just so we can constuct/access a compound key

            Dim nk$
            If notes Is Nothing Then nk = "null" Else nk = notes.Key

            If Branch.i_Slots Is Nothing Then Branch.i_Slots = New Dictionary(Of String, clsSlot)

            CurrentCompoundKey = Me.compoundKey()
            If Branch.slots Is Nothing Then Branch.slots = New Dictionary(Of Integer, clsSlot)

            If writecache Is Nothing Then
                Dim sql$
                sql$ = "INSERT INTO [slot] (path,fk_branch_id,fk_slottype_id,numslots,slotnum,fk_translation_key_notes,requiredFill,advisedFill) "
                sql$ &= "VALUES (" & da.SqlEncode(Path) & "," & Branch.ID & "," & type.ID & "," & numslots & "," & slotnum.sqlvalue & "," & nk & "," & requiredfill & "," & advisedFill & ");"

                Try

                    Me.ID = da.DBExecutesql(sql, True)
                Catch
                    Beep()
                End Try


                Branch.slots.Add(Me.ID, Me)
                '    If Not Branch.i_Slots.ContainsKey(CurrentCompoundKey) Then
                Branch.i_Slots.Add(CurrentCompoundKey, Me)
                'End If

            Else

                If Branch.i_Slots.ContainsKey(CurrentCompoundKey) Then
                    ' Beep()
                    'no biggie (the same brachs is master into many FAMILIES
                    'Logit("Duplicate branch slot key " & CurrentCompoundKey)
                Else

                    ' Me.ID = -1
                    Dim row As System.Data.DataRow
                    row = writecache.NewRow()
                    row("path") = Me.path
                    row("fk_branch_id") = Me.Branch.ID
                    row("fk_slottype_id") = Me.Type.ID
                    row("numslots") = Me.numSlots

                    If Me.slotNum.sqlvalue = "null" Then
                        row("slotnum") = DBNull.Value
                    Else
                        row("slotnum") = Me.slotNum.sqlvalue
                    End If

                    If nk = "null" Then
                        row("fk_translation_key_notes") = DBNull.Value
                    Else
                        row("fk_translation_key_notes") = nk
                    End If

                    row("requiredFill") = requiredfill
                    row("advisedFill") = advisedFill
                    row("deleted") = False
                    writecache.Rows.Add(row)
                    Branch.i_Slots.Add(CurrentCompoundKey, Me)

                    '  Branch.slots.Add(Me.ID, Me) 'new
                End If

            End If

            'Note - when created wth a writecache - the slots are not added to the branches (becuase they have no ID's yet)

        End If



    End Sub

    Public Function HasSlotNum() As Boolean

        HasSlotNum = False

        If Me.slotNum IsNot Nothing Then
            If Me.slotNum.value IsNot Nothing Then
                If Not (Me.slotNum.value Is DBNull.Value) Then
                    HasSlotNum = True
                End If
            End If
        End If

    End Function

    Public Function compoundKey() As String

        'used to make a lookup in the branches slot sorteddictionary - having them in a sorted dictionary means they can be presented in a sensible order
        Return Me.Type.MajorCode & "_" & Me.Type.MinorCode & "_" & Me.path & "_" & Math.Sign(Me.numSlots) & "_" & Me.slotNum.sqlvalue

    End Function

    Public Function NonStrictCompoundKey() As String

        'used to make a lookup in the branches slot sorteddictionary - having them in a sorted dictionary means they can be presented in a sensible order
        Return Me.NonStrictType.MajorCode & "_" & Me.NonStrictType.MinorCode & "_" & Me.path & "_" & Math.Sign(Me.numSlots) & "_" & Me.slotNum.sqlvalue

    End Function


    Public Function update(newbranch As clsBranch, newpath As String)


        'this is a littel delicat e- we have to maintain the index of slits on the branch carefully.
        Me.Branch.i_Slots.Remove(Me.compoundKey)

        Me.Branch = newbranch
        Me.path = newpath

        Dim sql$
        sql$ = "UPDATE [slot] set "
        sql$ &= "path=" & da.SqlEncode(Me.path) & ",fk_branch_id=" & Me.Branch.ID
        sql$ &= " WHERE ID=" & Me.ID

        da.DBExecutesql(sql, False)

        Me.CurrentCompoundKey = Me.compoundKey
        Me.Branch.i_Slots.Add(Me.CurrentCompoundKey, Me)


    End Function

    Public Sub Update(ByRef Errormessages As List(Of String)) Implements i_Editable.update

        Try

            If Me.Type.ID <= 0 Then Stop

            Dim sql$
            sql$ = "UPDATE [slot] set "
            sql$ &= "path=" & da.SqlEncode(Me.path) & ",fk_branch_id=" & Me.Branch.ID
            sql$ &= ",fk_slottype_id=" & Me.Type.ID & ",numslots=" & Me.numSlots
            sql$ &= ",requiredFill=" & Me.requiredFill
            sql$ &= ",advisedfill=" & Me.advisedFill
            If Me.notes IsNot Nothing Then
                sql$ &= ",fk_translation_key_notes=" & Me.notes.Key
            Else
                sql$ &= ",fk_translation_key_notes=null"
            End If
            sql$ &= ",deleted=" & IIf(Me.deleted, 1, 0)


            sql$ &= " WHERE ID=" & Me.ID

            da.DBExecutesql(sql, False)


            If Branch.i_Slots.ContainsKey(CurrentCompoundKey) Then
                Me.Branch.i_Slots.Remove(CurrentCompoundKey)
            End If

            If Not Me.deleted Then
                CurrentCompoundKey = compoundKey()
                Me.Branch.i_Slots.Add(CurrentCompoundKey, Me)
            End If


        Catch ex As System.Exception

            Errormessages.Add(ex.Message)
        End Try

    End Sub

    Public Sub New()

        'needs to add it to the parent products distionary of slots

    End Sub

    Public Function Insert(ByRef Errormessages As List(Of String)) As Object Implements i_Editable.Insert
        If Me.Branch Is Nothing Then Me.Branch = iq.RootBranch 'temporary for editor, always gets updated...
        Me.Type = iq.SlotTypes.First.Value

        Return New clsSlot(Me.Type, Me.Branch, Me.path, Me.numSlots, Me.notes, Me.slotNum, Me.requiredFill, Me.advisedFill)

    End Function

    Public Sub New(ID As Integer, type As clsSlotType, ByVal Branch As clsBranch, ByVal Path As String, numSlots As Integer, notes As clsTranslation, slotnum As IQ.NullableInt, requiredFill As Integer, advisedFill As Integer)

        Me.ID = ID
        Me.path = Path
        Me.Branch = Branch
        Me.Type = type
        Me.numSlots = numSlots
        Me.notes = notes
        Me.slotNum = slotnum
        Me.requiredFill = requiredFill
        Me.advisedFill = advisedFill

        CurrentCompoundKey = Me.compoundKey()

        If Len(Path) < 5 And Path <> "" Then Stop

        If type.ID <= 0 Then Stop

        'Auto -DeDupe (see loadslots)
        If Branch.i_Slots.ContainsKey(Me.compoundKey) Then
            Me.ID = -1
        Else
            'Note duplicated slots are NOT added to the branch/indexed
            Branch.i_Slots.Add(Me.compoundKey, Me)
            Branch.slots.Add(Me.ID, Me)
        End If

    End Sub


    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        Try
            Dim sql$
            sql$ = "DELETE FROM slot where id=" & Me.ID
            da.DBExecutesql(sql$)

            Me.Branch.slots.Remove(Me.ID)
            Me.Branch.i_Slots.Remove(Me.compoundKey)


        Catch
            'delete = False  'failed (almost certainly due to RI) (although with slots specifically - it's hard to see how that would happen
            Throw
        End Try

    End Sub

    Public Function displayName(Language As clsLanguage) As String Implements i_Editable.displayName

        Return String.Format("maj:{0} mnr:{1} ns:{2} sn:{3} nts:{4} rqf:{5} adf:{6}", _
        Me.Type.MajorCode, _
        Me.Type.MinorCode, _
        Me.numSlots, _
        IIf(Me.slotNum Is Nothing, "", Me.slotNum), _
        IIf(Me.notes Is Nothing, "", Me.notes), _
        Me.requiredFill, _
        Me.advisedFill)

    End Function


End Class

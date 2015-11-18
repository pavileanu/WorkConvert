Imports dataAccess

Public Class clsThread

    Property ID As Integer
    Property CreatedBy As clsUser
    Property AssignedTo As clsUser
    Property Parent As clsThread
    Property Priority As clsState
    Property Status As clsState
    Property hours As Single
    Property title As String
    Property Text As nullableString
    Property Children As Dictionary(Of Integer, clsThread) 'replies
    'Property EventLog As clsEvent
    Property Created As DateTime
    Property Updated As DateTime
    Property internal As Boolean

    Public oParent As clsThread

    'There are three 'constructors' (sub New's) - which one is used depends on the type and number of paramaters supplied (this is called 'overloading')
    'The parameterless constructor - sub new() is used by the 'add' button of the generic editor - 
    'The one *with* the ID, is used to load up an exiting instance  from the database
    'The one *without* the ID fills the object AND inserts it to the DB, this is typically the one called by INSERT()

    Public Sub New()

        'Replies = New Dictionary(Of Integer, clsThread) 'this should really be done via reflection in the generic addnew() which would allow the parameterless constructors to be empty
        Me.Children = New Dictionary(Of Integer, clsThread)

        'NB: SetDefaults() sets my parent, and adds me to my parents children

    End Sub

    Public Sub New(id As Integer, CreatedBy As clsUser, AssignedTo As clsUser, Parent As clsThread, priority As clsState, status As clsState, hours As Single, title As String, text As nullableString, Created As DateTime, Updated As DateTime, Internal As Boolean)

        'this overload is the 'reconstructor' used when loading threads up into the OM from the DB - see loadThreads()

        Me.ID = id
        Me.CreatedBy = CreatedBy
        Me.AssignedTo = AssignedTo
        Me.Parent = Parent
        Me.Priority = priority
        Me.Status = status
        Me.hours = hours
        Me.title = title
        Me.Text = text
        Me.Children = New Dictionary(Of Integer, clsThread)
        '    Me.EventLog = EventLog
        Me.Created = Created
        Me.Updated = Updated
        Me.internal = Internal

        iq.Threads.Add(Me.ID, Me)
        If iq.Threads.Count = 1 Then
            If Not Me.Parent Is Nothing Then Stop ' the root thread should not have a parent
            iq.RootThread = Me
        End If

        If Not Me.Parent Is Nothing Then
            Me.Parent.Children.Add(Me.ID, Me)  'add me to my parents children (to create the heirarchy)
        End If

        oParent = Parent

    End Sub

    Public Sub New(CreatedBy As clsUser, AssignedTo As clsUser, Parent As clsThread, priority As clsState, status As clsState, hours As Single, title As String, text As nullableString, created As DateTime, updated As DateTime, Internal As Boolean)

        Me.CreatedBy = CreatedBy
        Me.AssignedTo = AssignedTo
        Me.Parent = Parent
        Me.Priority = priority
        Me.Status = status
        Me.hours = hours
        Me.Text = text
        Me.Children = New Dictionary(Of Integer, clsThread)

        Me.Created = created
        Me.Updated = updated
        Me.internal = Internal

        Dim sql$

        Dim pid$
        If Me.Parent Is Nothing Then
            pid$ = "null"
        Else
            pid$ = Me.Parent.ID
        End If

        Dim elid$
        'If EventLog Is Nothing Then
        elid = "null"
        ' Else
        ' elid = EventLog.id
        ' End If

        sql$ = "INSERT INTO Thread (FK_User_ID_CreatedBy,FK_User_ID_AssignedTo,FK_Thread_ID_Parent,FK_State_ID_Priority,FK_State_ID_Status,[hours],title,[Text],FK_Event_ID,[created],[Updated],[Internal]) VALUES ("
        sql$ &= CreatedBy.ID & "," & AssignedTo.ID & "," & pid$ & "," & priority.ID & "," & status.ID & "," & hours & "," & da.SqlEncode(title) & "," & text.sqlValue & "," & elid & ",getdate(),getdate()," & IIf(Internal, 1, 0) & ");"

        Me.ID = da.DBExecutesql(sql$, True)  'this is important !

        iq.Threads.Add(Me.ID, Me)
        If iq.Threads.Count = 1 Then
            If Not Me.Parent Is Nothing Then Stop ' the root thread should not have a parent
            iq.RootThread = Me
        End If

        If Not Me.Parent Is Nothing Then
            Me.Parent.Children.Add(Me.ID, Me)  'add me to my parents children (to create the heirarchy)
        End If

        oParent = Parent


    End Sub

    Public Function Delete()

        If Not Me.oParent Is Nothing Then
            Me.oParent.Children.Remove(Me.ID)
        End If

        Dim sql$
        Sql$ = "DELETE FROM Thread where id=" & Me.ID

        da.dbexecutesql(sql$)

        Return True

    End Function

    Public Function Insert() As clsThread
        'called after the default values (and parent) have been set - see setDefaults()
        'returns the new, 'real' thread - complete with ID (@@IDENTITY)
        'AND adds it to it's parents children

        Return New clsThread(Me.CreatedBy, Me.AssignedTo, Me.Parent, Me.Priority, Me.Status, Me.hours, Me.title, Me.Text, Me.Created, Me.Updated, Me.internal)

    End Function

    Public Sub update()

        Dim sql$

        If Not Me.Parent Is Nothing Then
            If Not Me.Parent.Children.ContainsKey(Me.ID) Then
                Stop 'You have reparented a thread - it needs removing from it's original parents childern, and adding to its new parents children
            End If

        End If

        sql$ = "UPDATE thread set "
        sql$ &= "fk_user_id_createdby=" & Me.CreatedBy.ID & ","
        sql$ &= "fk_user_id_assignedto=" & Me.AssignedTo.ID & ","
        If Me.Parent Is Nothing Then
            sql$ &= "fk_thread_id_parent=null" & ","
        Else
            sql$ &= "fk_thread_id_parent=" & Me.Parent.ID & ","
        End If

        sql$ &= "fk_state_id_priority=" & Me.Priority.ID & ","
        sql$ &= "fk_state_id_status=" & Me.Status.ID & ","
        sql$ &= "hours=" & hours & ","
        sql$ &= "title=" & da.SqlEncode(Me.title) & ","

        sql$ &= "text=" & Text.sqlValue & ","
        'If Me.EventLog Is Nothing Then
        sql$ &= "fk_event_id=null,"
        ' Else
        ' sql$ &= "fk_event_id=" & Me.EventLog.ID & ","
        ' End If
        sql$ &= "[updated]=getdate(),"
        sql$ &= "[internal]=" & IIf(internal, 1, 0)

        sql$ &= " WHERE id=" & Me.ID

        da.DBExecutesql(sql$, False)


        'Supports reparenting
        Me.oParent.Children.Remove(Me.ID)
        Me.Parent.Children.Add(Me.ID, Me)


    End Sub

    ReadOnly Property displayName(language As clsLanguage)
        Get
            Return Me.title & " (" & Me.ID & ")"
        End Get
    End Property


End Class


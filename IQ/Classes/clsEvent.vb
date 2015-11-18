Imports dataAccess

Public Class OLDclsEvent

    Property ID As Integer
    Property children As Dictionary(Of Integer, clsEvent)
    Property message As String
    Property parent As clsEvent
    Property EventType As clsState
    Property timeStamp As DateTime
    Property duration As Integer  'duration in milliseconds
    Property startTick As Double 'used in the calculation of duration (via calls to .Update)
    Property severity As Integer

    ReadOnly Property displayName(Language As clsLanguage) As String
        Get
            Return Me.EventType.Translation.text(Language) & " - " & message
        End Get

    End Property


    Public Sub New(id As Integer, parent As clsEvent, message As String, EventType As clsState, timestamp As DateTime, duration As Integer)
        're-constructor (typically from the database)
        'But, we also construct events with a -1 ID 

        Me.ID = id
        Me.parent = parent

        Me.message = message
        Me.EventType = EventType
        Me.timeStamp = timestamp
        Me.duration = duration
        Me.children = New Dictionary(Of Integer, clsEvent)

        iq.Events.Add(Me.ID, Me)

        If iq.Events.Count = 1 Then
            If Not Me.parent Is Nothing Then Stop ' the root event should not have a parent
            iq.RootEvent = Me
        End If

        If Not Me.parent Is Nothing Then
            Me.parent.children.Add(Me.ID, Me)  'add me to my parents children (to create the heirarchy)
        End If

        Me.startTick = Stopwatch.GetTimestamp

    End Sub

    Public Sub close()  'does not persist to the database (they should be bulk written en-masse periocially for performance)

        Me.duration = (Stopwatch.GetTimestamp - Me.startTick) / Stopwatch.Frequency * 1000

    End Sub
    Public Sub New(parent As clsEvent, message As String, EventType As clsState)

        Me.message = message
        Me.EventType = EventType
        Me.timeStamp = Now
        Me.duration = -1
        Me.children = New Dictionary(Of Integer, clsEvent)

        Dim sql$
        Dim pid As String
        If parent Is Nothing Then pid = "null" Else pid = parent.ID

        sql$ = "INSERT INTO EVENT (message,fk_event_id_parent,fk_state_id_eventtype,timestamp,duration,severity) VALUES ("
        sql$ &= da.SqlEncode(message) & "," & pid & "," & EventType.ID & ",getdate(),0," & Me.severity.ToString & ");"
        Me.ID = da.DBExecutesql(sql$, True)
        iq.Events.Add(Me.ID, Me)

        If iq.Events.Count = 1 Then
            If Not Me.parent Is Nothing Then Stop ' the root event should not have a parent
            iq.RootEvent = Me
        End If

        If Not Me.parent Is Nothing Then
            Me.parent.children.Add(Me.ID, Me)  'add me to my parents children (to create the heirarchy)
        End If

        Me.startTick = Stopwatch.GetTimestamp

    End Sub

    Public Sub update(Optional Message$ = "")

        'records the duration 

        If Message$ <> "" Then Me.message = Message$

        Dim sql$
        Me.duration = (Stopwatch.GetTimestamp - Me.startTick) / Stopwatch.Frequency * 1000
        sql$ = "UPDATE [Event] SET message=" & da.SqlEncode(Me.message$) & ",FK_State_ID_EventType=" & EventType.ID & ",duration=" & Me.duration & " WHERE ID=" & Me.ID
        da.DBExecutesql(sql$)

    End Sub


End Class

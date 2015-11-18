'Option Strict On
<Serializable>
Public Class clsBranchState

    '    Public renderAs As bt
    'Public state As oc  'Open or closed
    Public maxChildren As Integer 'for grids - how many rows to display (show all rows button)
    Public rownum As Integer ' used if/when rendering alternatind rows (matric row odd, matric row even)
    Public United As Boolean 'Flatten this branch - (render all the SKUD descendedents)
    Public rca As enumBt   'Descendants render as 'the current' switcher mode of this branch - strictly redundant - but saves alot of looking up of the state ofr first descendants etc
    'Public openWhich As OpenWhich
    Public timestamp As DateTime


    Public Shared aLock = New Object

    Public Sub New()

    End Sub


    ''' <summary>
    ''' returns the branch state (from the session) - United,OpenWhich, RCA etc..
    ''' </summary>
    ''' <param name="lid"></param>
    ''' <param name="path"></param>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property getbranchstate(lid As UInt64, path As String) As clsBranchState
        Get
            Dim branchStates As Dictionary(Of String, clsBranchState) = DirectCast(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))
            If Not branchStates.ContainsKey(path) Then
                Return Nothing
            Else
                Return branchStates(path) ' branchStates(bi.path)
            End If
        End Get

    End Property

    Public Shared Sub setBranchState(lid As UInt64, path As String, bs As clsBranchState)

        Dim branchStates As Dictionary(Of String, clsBranchState) = DirectCast(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))
        branchStates(path) = bs ' branchStates(bi.path)

    End Sub


    Public Shared ReadOnly Property getBranchStateAbove(lid As UInt64, ByVal path As String, errormessages As List(Of String)) As clsBranchState
        Get

            If getbranchstate(lid, "tree") Is Nothing Then
                Dim aboveroot As clsBranchState = New clsBranchState(lid, "tree", enumBt.OpenSquare, False, 0, 100) 'the 'All products' renders as a breadcrumb
            End If


            If path$ = "" Then
                errormessages.Add("Path was blank in getBranchStateAbove")
                Return Nothing
            Else
                If InStr(path$, ".") = 0 Then
                    errormessages.Add("Path contained no . in getBranchStateAbove")
                    Return Nothing
                Else
                    Do
                        path = Left(path, InStrRev(path, ".") - 1)
                        Dim bs As clsBranchState = getbranchstate(lid, path)
                        If bs IsNot Nothing Then Return bs

                        If InStr(path, ".") = 0 Then
                            errormessages.Add("No dots left in getBranchStateAbove") : Return Nothing
                        End If

                    Loop

                End If

            End If

        End Get

    End Property


    'Public Shared ReadOnly Property branchState(bi As clsBranchInfo) As clsBranchState 'Sub setState(state As oc, renderAs As bt)

    '    Get
    '        'Dim branchStates As Dictionary(Of String, clsBranchState) = iq.sesh(bi.lid, "branchStates")
    '        'If branchStates Is Nothing Then Stop
    '        Return iq.sesh(bi.lid, "branchStates")(bi.path) ' branchStates(bi.path)
    '    End Get
    'End Property

    Public Sub New(lid As ULong, path$, rca As enumBt, unite As Boolean, rownum As Integer, maxchildren As Integer)

        Me.rca = rca
        Me.rownum = rownum
        Me.maxChildren = maxchildren
        Me.United = unite

        Dim branchStates As Dictionary(Of String, clsBranchState) = DirectCast(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))

        If Not branchStates.ContainsKey(path) Then
            branchStates.Add(path, Me)
        Else
            branchStates(path) = Me  'happens when we autoopen a branch already autoopened
        End If
        Me.timestamp = Now


    End Sub

    Public Shared Function removeBranchState(lid As UInt64, path$)

        Dim branchStates As Dictionary(Of String, clsBranchState) = DirectCast(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))
        branchStates.Remove(path$)

    End Function

    Public Shared Sub HideSiblings(lid As UInt64, path$)

        Dim branchStates As Dictionary(Of String, clsBranchState) = DirectCast(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))

        For Each p In branchStates.Keys.ToArray
            If plen(p) = plen(path) And p <> path Then
                branchStates(p).rca = enumBt.Hidden
            End If
        Next
    End Sub

    Public Shared Function plen(path$) As Integer
        plen = Split(path$, ".").Count

    End Function
    Public Shared Sub closeBranchesNotOnPath(lid As UInt64, path$)

        'Sets the sesh variables affecting the tree rendering
        '                                                                                                           Path
        Dim branchStates As Dictionary(Of String, clsBranchState) = DirectCast(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))

        For Each p In branchStates.Keys.ToArray
            If plen(p) > plen(path) Then
                'this is below the path
                branchStates.Remove(p)
            Else
                If p <> Left$(path$, Len(p)) Then
                    branchStates.Remove(p)
                End If
            End If
        Next

    End Sub


    Public Shared Sub CloseBelow(lid As UInt64, path$)

        'during a resize - this gets called by multiple threads .. and it can remove a branchstate twice
        'the synclock may or may not 'solve' the problem - but it would nice to fully understand the root cause

        SyncLock alock
            'Sets the sesh variables affecting the tree rendering
            '                                                                                                           Path
            'Dim branchStates As Dictionary(Of String, clsBranchState) = iq.sesh(lid, "branchStates") ';, Dictionary(Of String, clsBranchState)
            Dim branchStates As Dictionary(Of String, clsBranchState) = iq.sesh(lid, "branchStates") ';, Dictionary(Of String, clsBranchState)

            'branchStates.Clear() TESTING

            For Each p In branchStates.Keys.ToArray
                If plen(p) > plen(path$) Then

                    'branchStates(p).rca = enumBt.Hidden
                    branchStates.Remove(p)

                End If

                If plen(p) = plen(path$) AndAlso branchStates(p).rca = enumBt.OpenSquare Then branchStates.Remove(p)
            Next
        End SyncLock

    End Sub
    Public Shared Sub CloseSiblings(lid As UInt64, path As String)
        Dim branchStates As Dictionary(Of String, clsBranchState) = iq.sesh(lid, "branchStates") ';, Dictionary(Of String, clsBranchState)

        'branchStates.Clear() TESTING
        Dim pth As String = ""
        If path.Split(".").Length > 1 Then pth = path.Substring(0, path.Length - path.Split(".").Last.Length)

        For Each p In branchStates.Keys.ToList().Where(Function(f) f.StartsWith(pth))
            'If branchStates(p).rca = enumBt.OpenBranch Then
            If p <> path Then branchStates(p).rca = enumBt.Hidden
            ' End If
        Next
    End Sub
    Public Shared Sub CloseAbove(lid As UInt64, path$)

        'Sets the sesh variables affecting the tree rendering
        '                                                                                                           Path
        'Dim branchStates As Dictionary(Of String, clsBranchState) = iq.sesh(lid, "branchStates") ';, Dictionary(Of String, clsBranchState)
        Dim branchStates As Dictionary(Of String, clsBranchState) = iq.sesh(lid, "branchStates") ';, Dictionary(Of String, clsBranchState)

        'branchStates.Clear() TESTING

        For Each p In branchStates.Keys.ToArray
            If plen(p) <= plen(path$) AndAlso (branchStates(p).rca = enumBt.DetailSquare Or branchStates(p).rca = enumBt.Square) Then

                branchStates(p).rca = enumBt.OpenSquare
                ' branchStates.Remove(p)

            End If
        Next

    End Sub

    ''' <summary>Returns the subsection of the path down to (and including) the system unit (if present)</summary>
    Public Shared Function pathToSystem(path$) As String

        Dim pth$ = ""
        For Each seg In Split(path$, ".")
            pth$ &= seg
            If seg <> "tree" Then
                If iq.Branches(CInt(seg)).Product IsNot Nothing Then
                    If iq.Branches(CInt(seg)).Product.isSystem Then
                        Exit For
                    End If
                End If
            End If
            pth$ &= "."
        Next

        If pth$.EndsWith(".") Then pth$ = Left(pth, Len(pth) - 1)
        Return pth$

    End Function

    Public Shared Function HasSystem(path$) As Boolean

        HasSystem = False
        For Each seg In Split(path$, ".")
            If seg <> "tree" Then
                If iq.Branches(CInt(seg)).Product IsNot Nothing Then
                    If iq.Branches(CInt(seg)).Product.isSystem Then
                        Return True
                    End If
                End If
            End If
        Next
    End Function


    Public Shared Sub PloughPath(lid As UInt64, path$, ByRef errorMessages As List(Of String), treewidth As Single, paradigm As enumParadigm) ', treewidth As Single)

        'Sets the sesh variables to Open all the branches on the path appropriately for the 'show in tree' function

        Dim pp$
        Dim pth$ = "tree"

        Dim segs() As String = Split(path$, ".")
        'Dim hitSystem As Boolean = False

        'Dim lastButOne = segs(UBound(segs) - 1)
        For Each seg In segs
            If seg <> "tree" Then

                pp$ = pth$
                pth$ &= "." & seg

                'we want to keep state if it's there - and if not use .open to make it
                Dim bs As clsBranchState = getbranchstate(lid, pth)
                Dim bi As clsBranchInfo = New clsBranchInfo(lid, pth$, Nothing, treewidth, paradigm, errorMessages)

                ' If bs Is Nothing Then
                bs = bi.open(errorMessages, False) 'this branch has not yet been opened (rendered)               
                '*KW If bs.rca = enumBt.Square Or bs.rca = enumBt.DetailSquare Then CloseAbove(lid, pth$)
                If CType(iq.sesh(lid, "Paradigm"), enumParadigm) = enumParadigm.configuringSystem Then
                    CloseSiblings(lid, pth$)
                End If
                'End If
                'If bs.rca = enumBt.Square Or bs.rca = enumBt.DetailSquare Then
                ' bs.rca = enumBt.OpenSquare
                '    CloseAbove(bi.lid, bi.path)
                'End If
                ' If bs.rca = enumBt.Branch Then bs.rca = enumBt.OpenBranch
            End If
        Next

    End Sub





End Class


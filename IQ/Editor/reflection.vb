'Option Strict On
Imports System.IO
Imports System.Reflection

Module Reflection

    Public Class clsCriterea
        Public Prop As String
        Public Op As String
        Public value As String

        Public Sub New(Prop As String, op As String, value As String)
            Me.Prop = Prop
            Me.Op = op
            Me.value = value
        End Sub

    End Class

    'Public Function Match(obj As Object, screen As clsScreen, critirea As List(Of clsCriterea)) As Boolean

    '    Dim v As Object = Reflection.getPropertyValue(obj, prop)
    '    If LCase(v) = LCase(value) Then Return True Else Return False

    'End Function

    'Public Function Parsefilter(filter$) As List(Of clsCriterea)

    '    'filter contains a comma seperated list of expressions
    '    'of the form Property opertator Value
    '    'e.g. Displayname=Tech*,Country=UK
    '    'or Price>15<20
    '    'generally these are assembled from dropdown lists which have been generated automatically
    '    'they may include calculated properties such as price
    '    'or indexed properies such as productAttributes(mass)<10

    '    Parsefilter = New List(Of clsCriterea)

    '    Dim c() As String
    '    c = Split(filter$, ",")

    '    For Each f In c
    '        If InStr(f, "=") Then
    '            Dim b$() = Split(f, "=")
    '            critera.add new clsCriterea(b(0),"=",b(edit1)
    '        End If
    '    Next

    '    Dim pv() As String = Split(filter, "=")
    '    prop = pv(0)
    '    value = pv(1)

    'End Function

    Public Function Findmatch(dic As Object, filterPropValue As String, ByRef errorMessages As List(Of String)) As Object

        '    'If no filter is specified, returns the supplied dictionary DIC
        '    'otherwise
        '    'filters can originate from a fields  .LookUpOf property. e.g. States(group=TH)
        '    'NB:- filters (and reflection generally) are case sensitive against the OM i.e.   e.g. states(group=TH)  - would NOT work

        Findmatch = Nothing

        If dic Is Nothing Then Return Nothing
        If dic.Values.Count = 0 Then Return Nothing
        If filterPropValue = "" Then
            'Return dic.Values(0)  <-- This *does not* work - you get 'no accessible 'values' accepts tis number of arguments - the only way to get to the values seesm to be through emumeration.. (which works just fine, so don't fight it)
            For Each v In dic.Values
                Return v
                Exit Function 'probably redundant - but here ffor clarity
            Next 'will never execute
        End If

        Dim p() As String = Split(filterPropValue, "=")
        'todo - check there's 2 bits

        For Each obj In dic.Values
            '         Dim prop As Object = Reflection.getPropertyValue(obj, p(0)) ' probably need to get the text of translations here
            Dim prop As Object = Reflection.WalkPropertyValue(obj, p(0), errorMessages) ' probably need to get the text of translations here

            If prop = p(1) Then Return obj
        Next

    End Function

    'Public Sub oldWalkDown(ByRef obj As Object, ByRef path$)

    '    'modifies OBJ and path to be the last object and property on the path 
    '    'could almost certainly be consolidated with parseProperty/parsePath

    '    'Starts at obj, and walks path$ to return a property

    '    'eg. on a branch we may want to find
    '    ' product.attributes(17).Numericvalue

    '    Dim hops As List(Of String)
    '    hops = Split(path$, ".").ToList


    '    Dim hopName As String
    '    Dim hopIndex As String
    '    Dim ob, cb As Integer

    '    Dim hc As Integer = 0

    '    For Each hop In hops
    '        hc = hc + 1
    '        ob = InStr(hop, "(")
    '        cb = InStr(hop, ")")
    '        If ob Then
    '            hopName = Left(hop, ob - 1)
    '            hopIndex = Mid$(hop, ob + 1, cb - ob - 1)

    '            '                ParentObj = OBJ
    '            obj = Reflection.getPropertyValue(obj, hopName) 'this is a dictionary
    '            If IsNumeric(hopIndex) Then
    '                If obj.containskey(Val(hopIndex)) Then
    '                    obj = obj(Val(hopIndex)) 'fetch the specified row (by ID) from it
    '                Else
    '                    Stop
    '                End If
    '            Else
    '                'this hop index (in parentheses) is not a number - so it's a filter eg Country=En*
    '                obj = Findmatch(obj, hopIndex)
    '                'ParsePath$ = hopIndex 'a filter
    '            End If
    '        Else
    '            '               ParentObj = OBJ
    '            obj = Reflection.getPropertyValue(obj, hop) 'this is a DICTIONARY
    '        End If

    '        If hc = hops.Count - 1 Then Exit For

    '    Next

    '    path$ = hops.Last

    'End Sub

    'Public Function OldParseProperty(path$, ByVal OBJ As Object) As Object

    '    'Starts at obj, and walks path$ to return a property

    '    'eg. on a branch we may want to find
    '    ' product.attributes(17).Numericvalue
    '    'we may also 'vector' to anothe product (CPU being the prime example)

    '    'OBJ would be a branch
    '    'CPU.i_attributes_code(Speed)

    '    path$ = Replace(path$, ")(", ").(")  'This replaces any default indexer - adding the .

    '    Dim hops As List(Of String)
    '    hops = Split(path$, ".").ToList

    '    Dim hopName As String
    '    Dim hopIndex As String
    '    Dim ob, cb As Integer

    '    For Each hop In hops
    '        ob = InStr(hop, "(")
    '        cb = InStr(hop, ")")
    '        If ob Then
    '            hopName = Left(hop, ob - 1)
    '            hopIndex = Mid$(hop, ob + 1, cb - ob - 1)

    '            If hopName <> "" Then ' skip over the dfault indexers - eg i_attributes(sku)(0)
    '                OBJ = Reflection.getPropertyValue(OBJ, hopName) 'this is a dictionary
    '            End If


    '            If InStr(hopIndex, "=") Then
    '                OBJ = Findmatch(OBJ, hopIndex)
    '            Else
    '                If TypeOf (OBJ) Is IList Then
    '                    OBJ = OBJ(hopIndex)
    '                Else
    '                    If OBJ.containskey(hopIndex) Then
    '                        OBJ = OBJ(hopIndex) 'look isn't that pretty                    
    '                    Else
    '                        OBJ = Nothing
    '                        Exit For
    '                    End If

    '                End If

    '            End If

    '        Else
    '            'Something like "product."
    '            If UCase(hop) = "CPU" Then 'Allows us to vector to the attributes of the CPU, by SKU, as help in one of the product attributes
    '                Dim product As clsProduct
    '                product = OBJ
    '                If product.i_Attributes_Code.ContainsKey("cpuSKU") Then
    '                    OBJ = iq.i_SKU(product.i_Attributes_Code("cpuSKU")(0).Translation.text(English))
    '                Else
    '                    Return Nothing
    '                End If

    '            Else
    '                OBJ = Reflection.getPropertyValue(OBJ, hop)
    '            End If
    '        End If
    '    Next hop

    '    Return OBJ


    'End Function

    'Public Function oldParsePath(path$, ByRef Obj As Object, ByRef ParentObj As Object) As String

    '    'sets Obj and ParentObj 
    '    'returns the filter portion - if present

    '    'uses reflection from the IQ root object to find the specified object OR dictionary (and it's parent)

    '    ' a path can be to a single object 
    '    ' e.g.Threads(17)
    '    ' a dictionary
    '    ' e.g. Variants
    '    ' or a filtered dictionary
    '    ' e.g. ProductAttributes(product.id=1,attribute.code=weight)

    '    Dim hops As List(Of String)
    '    hops = Split(path$, ".").ToList

    '    Obj = iq 'let's start at the very begining (a very good place to start)

    '    Dim hopName As String
    '    Dim hopIndex As String
    '    Dim ob, cb As Integer

    '    For Each hop In hops
    '        ob = InStr(hop, "(")
    '        cb = InStr(hop, ")")
    '        If ob Then
    '            hopName = Left(hop, ob - 1)
    '            hopIndex = Mid$(hop, ob + 1, cb - ob - 1)

    '            ParentObj = Obj
    '            Obj = Reflection.getPropertyValue(Obj, hopName) 'this is a dictionary
    '            If IsNumeric(hopIndex) Then  'an index may be another objet - or path thereto  - so we needs some sort of recursive parses
    '                Obj = Obj(Val(hopIndex)) 'fetch the specified row (by ID) from it 


    '            Else
    '                'this hop index (in parentheses) is not a number - so it's a filter eg Country=En*
    '                oldParsePath$ = hopIndex 'a filter
    '            End If
    '        Else
    '            ParentObj = Obj
    '            Obj = Reflection.getPropertyValue(Obj, hop) 'this is an object (not a  DICTIONARY - surely)
    '        End If
    '    Next

    'End Function

    ''' <summary>uses reflection from the Specified object to find the specified object OR dictionary (and it's parent)</summary>
    Public Function ParsePath(path$, ByRef Obj As Object, ByRef ParentObj As Object, ByRef errorMessages As List(Of String)) As String

        ParsePath = ""
        'sets Obj and ParentObj 
        'returns the filter portion - if present

        ' a path can be to a single object 
        ' e.g.Threads(17)
        ' a dictionary
        ' e.g. Variants
        ' or a filtered dictionary
        ' e.g. ProductAttributes(product.id=1,attribute.code=weight)
        'Dictionary keys can also be other objects - with nested evaluations such as
        'branch.Product.i_variants(Channels(17))
        'you can also index dictionaries with literal string keys - such as product.i_attributes(mfrsku)

        'Where a default indxer has been used - add the .item( explicitly
        'for example products.i_attributes(mfsku)(0)
        'Becomes products.i_attributes(mfrsku).Items(0)


        path$ = Replace(path$, ")(", ").Items(")

        Dim hops As List(Of String)
        hops = Split(path$, ".").ToList


        Dim objName As String
        Dim hopIndex As String
        Dim ob As Integer

        Dim indexedObj As Object ' a dictionary or list

        For Each hop In hops
            ParentObj = Obj

            ob = InStr(hop, "(")
            If ob Then
                objName = Left(hop, ob - 1)

                hopIndex = betweenParentheses(hop, ob)   'Mid$(hop, ob + 1, cb - ob - 1)

                If InStr(hopIndex, "(") Then 'does the index need evaluating (to an object) eg Product.i_variants(Channels(23))
                    Dim io As Object = iq
                    ParsePath(hopIndex, io, Nothing, errorMessages)  'recurse to evalute the inner (indexing) object
                    Obj = Reflection.getPropertyValue(Obj, objName)
                    If Obj Is Nothing Then
                        errorMessages.Add("Obj was nothing at path " & path$)
                    Else
                        Obj = Obj(io)
                    End If


                Else
                    If objName = "Items" Then
                        Obj = Obj(hopIndex)
                        If Obj Is Nothing Then errorMessages.Add("Items hopindex was nothing " & path)
                    Else
                        indexedObj = Reflection.getPropertyValue(Obj, objName) 'a dictionary or list
                        If TypeOf (indexedObj) Is IDictionary Or TypeOf (indexedObj) Is IList Then
                            If hopIndex Is Nothing Then
                                errorMessages.Add("Indexer was nothing in paresepath:" & path)
                                Exit Function
                            Else
                                If indexedObj.containskey(hopIndex) Then
                                    Obj = indexedObj(hopIndex)
                                    If Obj Is Nothing Then errorMessages.Add("Obj was nothing in parsepath")
                                Else
                                    Obj = Nothing : Exit For
                                End If
                            End If
                        Else
                            If indexedObj Is Nothing Then
                                errorMessages.Add("Could not evaluate path beyond " & objName & " (Paths are CaSe SeNsitiVe)")
                            Else
                                errorMessages.Add("An unrecognised object type " & indexedObj.GetType.ToString & " was indexed")
                            End If
                            Obj = Nothing


                            Exit Function
                        End If
                    End If
                End If
            Else
                If UCase(hop) = "CPU" Then 'Allows us to vector to the attributes of the CPU, by SKU, as help in one of the product attributes
                    Dim product As clsProduct
                    product = Obj
                    If product.i_Attributes_Code.ContainsKey("cpuSKU") Then
                        Dim CPUsku As String = product.i_Attributes_Code("cpuSKU")(0).Translation.text(English)

                        If iq.i_SKU.ContainsKey(CPUsku) Then
                            Obj = iq.i_SKU(CPUsku)
                        Else
                            Obj = Nothing
                            Return ""
                        End If
                    Else
                        Obj = Nothing
                        Return ""
                    End If

                Else

                    Obj = Reflection.getPropertyValue(Obj, hop)
                    If Obj Is Nothing Then
                        Return ""
                    End If

                End If

            End If
        Next

    End Function


    'If TypeOf (Obj) Is IList Then
    '    Obj = Obj = Obj(hopIndex)
    'ElseIf TypeOf (Obj) Is IDictionary Then
    '    If InStr(hopIndex, "(") = 0 Then
    '        If Obj.containskey(hopIndex) Then Obj = Obj(hopIndex)
    '    Else
    '        Dim io As Object = iq
    '        ParsePath(hopIndex, io, Nothing)  'recurse to evalute the inner (indexing) object

    '        Obj = Obj(io)
    '    End If
    '    Obj = Reflection.getPropertyValue(Obj, hopName) 'this is a dictionary
    '    ' End If

    '    If IsNumeric(hopIndex) Then  'an index may be another object - or path thereto  - so we needs some sort of recursive parses

    '        '   Obj = Reflection.getPropertyValue(Obj, hopName) 'this is a dictionary
    '        Obj = Obj(Val(hopIndex)) 'fetch the specified row (by ID) from it 

    '    Else

    '        If TypeOf (Obj) Is IList Then
    '            Obj = Obj(hopIndex)
    '        Else
    '            If TypeOf (Obj) Is IDictionary Then
    '                If InStr(hopIndex, "(") = 0 Then
    '                    If Obj.containskey(hopIndex) Then Obj = Obj(hopIndex)
    '                Else
    '                    Dim io As Object = iq
    '                    ParsePath(hopIndex, io, Nothing)  'recurse to evalute the inner (indexing) object

    '                    Obj = Obj(io)

    '                    'now take the part beyond the parentheses - this is the property we're looking for - or (0) in the case of attributes lists

    '                    'Dim dic As Object = Reflection.getPropertyValue(Obj, hopName) 'this is a dictionary
    '                    ' Obj = getDicEntry(dic, io)
    '                End If
    '            End If


    '        End If

    'Obj = Reflection.getPropertyValue(dic.values(0), "values", io) 'this is a dictionary

    'this hop index (in parentheses) is not a number - so it's a filter eg Country=En*
    'ParsePath$ = hopIndex 'a filter


    Private Function betweenParentheses(l$, ob As Integer) As String

        betweenParentheses = Nothing

        'Returns the portion of l$ between the "(" at OB and it's *corresponding* ")"
        'e.g. 
        'Products.i_variants(Channels(32).users(12))  

        Dim c$
        Dim o As Integer = 0
        Dim i As Integer
        For i = ob To Len(l$)
            c$ = Mid(l$, i, 1)
            If c = "(" Then o = o + 1
            If c = ")" Then
                o = o - 1
                If o = 0 Then betweenParentheses = Mid$(l$, ob + 1, i - 1 - ob) : Exit For
            End If
        Next

        ' If i > Len(l$) Then Stop 'failed (mismatching parentheses)

    End Function

    Public classList As Dictionary(Of String, String)

    Public Sub setupClassList()

        classList = New Dictionary(Of String, String)
        classList.Add("clsCountry", "DisplayName") 'multilingual
        classList.Add("clsAccount", "Name")
        classList.Add("clsTeam", "Name")          'localised (one language)
        classList.Add("clsUser", "RealName")
        classList.Add("clsLanguage", "LocalName")  'localsed one language
        classList.Add("clsCurrency", "DisplayName") 'multilingual
        classList.Add("clsRole", "DisplayName")      'multilingual
        classList.Add("clsChannel", "DisplayName")
        classList.Add("clsUnit", "Code")
        classList.Add("clsProduct", "DisplayName")
        classList.Add("clsAttribute", "Code")
        classList.Add("clsProductType", "DisplayName")
        classList.Add("clsSector", "code")
        classList.Add("clsThread", "title")
        classList.Add("clsState", "code")
        classList.Add("clsEvent", "ID")
        classList.Add("clsScreen", "title")
        classList.Add("clsValidation", "description")
        classList.Add("clsInputType", "name")
        classList.Add("clsBranch", "name")
        classList.Add("clsQuantity", "name")
        classList.Add("clsSlotType", "name")
        classList.Add("clsSlot", "name")
        classList.Add("clsVariant", "name")
        classList.Add("clsStock", "name")
        classList.Add("clsPrice", "name")
        classList.Add("clsRegion", "DisplayName")
        classList.Add("clsCampaign", "DisplayName")
        classList.Add("clsAdvert", "DisplayName")
        classList.Add("clsClickThru", "DisplayName")
        classList.Add("clsImpression", "DispayName")


    End Sub

    Public Function RootDictionary(ty As Type) As String
        'returns the name of the root level dictionary which contains objects of the specified type - eg, countries, currencies, accounts, products etc.

        RootDictionary = ""
        Dim tod As System.Type

        Dim properties_info As PropertyInfo() = GetType(IQ.clsIQ).GetProperties()

        For Each p In properties_info
            If LCase(p.PropertyType.Name) = "dictionary`2" Then
                Debug.Print(p.Name)
                '      If LCase(p.Name) = "channels" Then Stop
                tod = typeOfDictionary(p.PropertyType.FullName)
                If ty Is tod Then
                    Return p.Name
                End If
            End If
        Next

        ' Stop
        'couldn't locate a root level dictionary of type ty

    End Function

    Public Function IsDictionary(obj As Object) As Boolean

        Dim properties_info As PropertyInfo() = obj.GetType.GetProperties()
        Dim ty As Type
        ty = obj.GetType
        If ty.Name = "Dictionary`2" Then Return True Else Return False

        'For Each objProperty In properties_info
        '    order += 1 'each field has an order property - allowing them to be shuffled around
        '    ptn = objProperty.PropertyType.Name  'the type of this property of the obejct (that this screen manages)
        '    Select LCase(ptn)
        '        Case "dictionary`2"  'we've found a 'dictionary of' within the OM.. so we'll make a new subscreen to manage objects of the type contained in the dictionary

    End Function

    Public Sub DeleteScreen(ID As Integer, ByRef errormessages As List(Of String))

        iq.Screens(ID).Delete(errormessages)

    End Sub
    Public Sub Serialize(obj As Object, sw As StreamWriter, level As Integer)

        Dim ty As Type = obj.GetType
        Dim properties_info As PropertyInfo() = ty.GetProperties() 'Get all the public properties of the object we're making a screen to handle

        Dim ptn As String
        For Each objProperty In properties_info

            ptn = objProperty.PropertyType.Name  'the type of this property of the obejct (that this screen manages)

            Select Case (LCase(ptn))

                Case "string", "integer", "single", "int32", "boolean"

                    If objProperty.GetIndexParameters().Length > 0 Then
                        'it's a dictionary or list (and therefore of some complex type/class) - properties with paramaters (eg. display name) also come through here it seems
                        Dim w As String = objProperty.PropertyType.ToString
                    Else

                        Dim v As Object = Reflection.getPropertyValue(obj, objProperty.Name)
                        Dim s As String = obj.ToString
                        sw.Write(s.ToCharArray)
                        sw.Write("¬".ToCharArray)

                    End If

                Case "dictionary`2"  'we've found a 'dictionary OF' within the OM.. so we'll recurse make a new subscreen to manage objects of the type contained in the dictionary

                    Dim dic As Object
                    dic = Reflection.getPropertyValue(obj, objProperty.Name)
                    For Each v In dic.values
                        If level > 1 Then
                            sw.Write(Trim(CStr(v.id)).ToCharArray)
                        Else
                            Serialize(v, sw, level + 1)
                        End If
                    Next
            End Select
        Next

    End Sub


    Public Function MakeScreen(Title As String, code As String, objType As Type, parentType As Type, errormessages As List(Of String)) As clsScreen

        'Builds an input screen with a specified name, for the type of object specified... and recurses (for screens that manage 'many')

        'Dim dicname As String
        'dicname = RootDictionary(objType)

        Dim afield As clsField
        Dim ptn As String

        'Each Object eg. "Customer" has many Properties, eg. Name, Address, Telehone number - Some of which may be objects (or collections of objects) in their own right
        Dim order As Integer = 0

        Dim ThisScreen As clsScreen
        ThisScreen = New clsScreen(objType.Name, code, Title, errormessages)

        Dim properties_info As PropertyInfo() = objType.GetProperties() 'Get all the properties of the object we're making a screen to handle

        For Each objProperty In properties_info
            order += 1 'each field has an order property - allowing them to be shuffled around

            ptn = objProperty.PropertyType.Name  'the type of this property of the obejct (that this screen manages)

            Dim emshigh As Single = 1.5

            Select Case LCase(ptn)
                Case "dictionary`2"  'we've found a 'dictionary OF' within the OM.. so we'll recurse make a new subscreen to manage objects of the type contained in the dictionary
                    'by choice, we embed dictionaries not lists - because elements of a dictionary are rapidly and individually addressable (by key) - by the editor (amongst other things)

                    ' All very clever - but unnecessary ! - it's better to make the subscreen 'just in time' too - less code, less complex

                    ''what type of object are the values of his dictionary
                    'dty = objProperty.PropertyType.GetGenericArguments(1)  'You MUST use objProperty.PropertyType NOT .gettype ! (took a long time to work out!)

                    'Dim subScreen As clsScreen
                    'If iq.i_screens_ObjType.ContainsKey(dty.Name) Then
                    '    subScreen = iq.i_screens_ObjType(dty.Name)  'we've already made this screen (possibly through recursion)
                    'Else
                    '    'recurse
                    '    subScreen = MakeScreen(objProperty.Name, dty, objType)   'make a screen for this type of object
                    'End If

                    'make a field - which embeds the screen we just created - to manage a list of objects of this type
                    Dim it As clsInputType
                    it = iq.i_inputType_code("many")

                    'this used named arguments for clarity - it just shows you (the developer) which parameters are called what - without having to use intellisense
                    'http://msdn.microsoft.com/en-us/library/51wfzyw0(v=vs.80).aspx
                    afield = New clsField(screen:=ThisScreen, propertyName:=objProperty.Name, lookupof:="", _
                                                        labeltext:=iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, Nothing, 0, False), helptext:="help for " & objProperty.Name, validation:=Nothing, _
                                                        inputType:=it, length:=50, order:=order, width:=5, height:=emshigh, defaultvalue:="", visibleList:=True, visiblePage:=True, defaultFilter:="", defaultsort:="", priority:=5, quickFilterGroup:=Nothing, QuickFilterUIType:="", CanUserSelect:=False, LinkedFieldID:=Nothing, FilterVisible:=True)


                Case "string", "integer", "single", "int32", "boolean"

                    If objProperty.GetIndexParameters().Length > 0 Then
                        'it's a dictionary or list (and therefore of some complex type/class) - properties with paramaters (eg. display name) also come through here it seems
                        Dim w As String = objProperty.PropertyType.ToString
                    Else
                        'it's a simple type 
                        Dim width As Integer
                        If LCase(ptn) = "int32" Then width = 5 Else width = 10
                        If LCase(ptn) = "boolean" Then width = 2
                        Dim default$ = ""
                        If LCase(objProperty.Name) = "current" Then default$ = "1" 'Default the 'current' field on auditable objects to 1

                        If objType.Name = "clsQuantity" And ptn$ = "Path" Then default$ = "[treepath]"
                        If objType.Name = "clsSlot" And ptn$ = "Path" Then default$ = "[treepath]"

                        afield = New clsField(ThisScreen, CStr(objProperty.Name), "", iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, Nothing, 0, False), "help for " & objProperty.Name, Nothing, iq.i_inputType_code(LCase(ptn)), 50, order, width, emshigh, default$, True, True, "", "", 5, Nothing, "", False, Nothing, True)
                    End If

                Case "datetime"
                    afield = New clsField(ThisScreen, CStr(objProperty.Name), "", iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, Nothing, 0, False), "help for " & objProperty.Name, Nothing, iq.i_inputType_code("date"), 10, order, 5, emshigh, "[now]", True, True, "", "", 5, Nothing, "", False, Nothing, True)
                Case "clstranslation"
                    afield = New clsField(ThisScreen, CStr(objProperty.Name), "", iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, Nothing, 0, False), "help for " & objProperty.Name, Nothing, iq.i_inputType_code("translate"), 100, order, 15, emshigh, "", True, True, "", "", 5, Nothing, "", False, Nothing, True)
                Case "nullablestring"
                    afield = New clsField(ThisScreen, CStr(objProperty.Name), "", iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, Nothing, 0, False), "help for " & objProperty.Name, Nothing, iq.i_inputType_code("nullstring"), 100, order, 15, emshigh, "", True, True, "", "", 5, Nothing, "", False, Nothing, True)
                Case "nullableint"
                    afield = New clsField(ThisScreen, CStr(objProperty.Name), "", iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, Nothing, 0, False), "help for " & objProperty.Name, Nothing, iq.i_inputType_code("nullint"), 7, order, 7, emshigh, "", True, True, "", "", 5, Nothing, "", False, Nothing, True)
                Case "nullableprice"
                    afield = New clsField(ThisScreen, CStr(objProperty.Name), "", iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, Nothing, 0, False), "help for " & objProperty.Name, Nothing, iq.i_inputType_code("nullprice"), 10, order, 12, emshigh, "", True, True, "", "", 5, Nothing, "", False, Nothing, True)
                Case Else

                    'its another type or object (but not a list) - present as a dropdown list
                    If classList.ContainsKey(ptn$) Then

                        Dim a As Assembly = Assembly.GetExecutingAssembly
                        Dim ty As Type = Nothing
                        For Each t In a.GetTypes
                            If LCase(ptn$) = LCase(t.Name) Then ty = t : Exit For
                        Next

                        'ty is the type of object - eg clsUser, clsChannel, clsThread

                        If ty Is Nothing Then errormessages.Add("could not locate type")

                        If LCase(objProperty.Name) = "auditroot" Then
                            ThisScreen.Auditable = True 'This Class has a root property - grouping instances (and a Current Property)
                        End If

                        Dim default$
                        default$ = ""
                        If ty Is parentType Then
                            'where any child object contains a reference back to its parent - such as a field.screen 
                            ' (being a member of the screen.fields dictionary)
                            'having links though the OM in 'both directions' ('down' to descendants and 'up' to ancestors)
                            ' make it easier to navigate and faster
                            '
                            default$ = "[parent]" 'this is foreign key, rendered as a drop down list - under
                        End If

                        If ty Is objType Then
                            default$ = "[tree]" 'this field (on this screen) points to instances of itself (a recursive tree)... not sure this is materially different from the above
                        End If


                        Dim lkup$
                        lkup = objProperty.Name
                        If InStr(lkup$, "_") Then
                            lkup$ = lkup$.Split("_")(0) 'used when creating tree structures eg. channel_children - obsoleted now i think
                        End If


                        If LCase(objProperty.Name) = "auditroot" Then  'any object that contains an AuditRoot property has a pointer to its top ancestor
                            '              lkup$ = dicname & ".ID"
                        Else
                            'exceptions
                            Select Case LCase(lkup$)
                                Case Is = "iscloneof", "sellerchannel", "buyer"
                                    lkup$ = "Channels"
                                Case Is = "parent"
                                    'You don't generally want to be shoing these (i don't think)
                                    If LCase(ptn$) = "clschannel" Then
                                        lkup$ = "Channels"
                                    ElseIf ptn$ = "clsThread" Then 'want to expose this as an integer (to allow reparenting)
                                        lkup$ = "Threads"
                                    ElseIf ptn$ = "clsBranch" Then
                                        lkup$ = "Branches"
                                    ElseIf ptn$ = "clsRegion" Then
                                        lkup$ = "Regions"
                                    Else
                                        ' Stop
                                    End If
                                Case Is = "buyerchannel"
                                    lkup$ = "Channels"

                                Case Is = "createdby", "assignedto"
                                    lkup$ = "Users"
                                Case Is = "status" 'Thread status
                                    lkup$ = "States(group=TH)" 'need some ability to filter
                                Case Is = "priority" 'Thread Priority
                                    lkup$ = "States(group=PR)" ' 'to become a translation
                                Case Is = "eventlog"
                                    lkup$ = "Events"
                                Case Is = "branch"
                                    lkup$ = "Branches"
                                Case Is = "type"
                                    lkup$ = "SlotTypes"
                                Case Is = "seller"
                                    lkup$ = "Channels"
                                Case Is = "skuvariant"
                                    lkup$ = "Variants"
                                Case Is = "matrix", "grid"
                                    lkup$ = "Screens"
                                Case Is = "present", "absent"  'adverts
                                    lkup$ = "ProductTypes"

                                Case Else
                                    'the class is no longer required as it is now present in every field (which is clearer)
                                    lkup$ = Plural(lkup$)  '& "." & classlist(ptn$) 'adds the Display property as defined in the ClassList (often. displayname)
                            End Select
                        End If

                        'this is a reference to a single object - and should not be a plural
                        ' If LCase(Right$(.Name, 1)) = "s" Then Stop
                        afield = New clsField(ThisScreen, CStr(objProperty.Name), lkup$, iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, Nothing, 0, False), "help for " & objProperty.Name, Nothing, iq.i_inputType_code("one"), 0, order, 10, emshigh, default$, True, True, "", "", 5, Nothing, "", False, Nothing, True)
                    Else
                        Debug.Print(ptn$)
                        ' Stop
                    End If
            End Select
        Next

        Return ThisScreen

    End Function

    Public Function TypeOfDicOrObj(o As Object) As Type

        If Reflection.IsDictionary(o) Then
            Return typeOfDictionary(o)
        Else
            Return o.GetType()
        End If

    End Function


    Public Function typeOfDictionary(obj As Object) As Type

        'returns the type of the values collection of a regulay key-value dictionary

        Dim args() As Type = obj.GetType.GetGenericArguments
        Return args(1) 'the second dimension (the values) in the dictionary

    End Function

    Public Function typeOfDictionary(pfn As String) As System.Type

        'find the type of the second dimension of the dictionary (the first dimension is the integer index) - the second is some kind of object
        'an Account, Invoice. Product, Country etc.

        Dim opfn As String
        opfn = pfn

        pfn = Mid$(pfn, InStr(pfn, "],[") + 3)
        pfn = Left(pfn, InStr(pfn, ",") - 1)

        Dim cn() As String = pfn.Split(".")
        Dim typename As String = cn.Last

        'this is dirty - but classes declared in a module seem to get prefixed moudulename+
        'in fact his whole bit is ugly .. i'm sure there's a better way (to get the type of a class by name)
        If InStr(typename, "+") Then typename = Split(typename, "+")(1)

        Dim a As Assembly = Assembly.GetExecutingAssembly
        Dim ty As Type = Nothing
        For Each t In a.GetTypes
            If LCase(typename) = LCase(t.Name) Then
                ty = t : Exit For
            End If
        Next
        'If ty Is Nothing Then Stop

        Return ty
        '/End of ugly bit


    End Function

    Public Function TypeFits(prop As Object, obj As Object, propname$) As Boolean

        TypeFits = False

        'is the object Prop, the correct type to fit in the propname$ propery of Obj ?
        ' Dim test As Object = Reflection.getPropertyValue(obj, propname$)

        Dim type As Type = obj.GetType
        Dim pinfo1 As PropertyInfo = type.GetProperty(propname$) 'get the information anout Obj's Propname$

        Dim ty1 As Type
        Dim ty2 As Type
        ty1 = prop.GetType
        ty2 = pinfo1.GetType
        If ty1 Is ty2 Then
            Return True
        End If

    End Function

    Public Function getPropertyString(obj As Object, prop As String, language As clsLanguage) As String

        Dim value = getPropertyValue(obj, prop)

        Dim retval As String

        If TypeOf obj Is clsTranslation Then
            retval = obj.text(language)
        ElseIf TypeOf obj Is NullableInt Then
            If CType(obj, NullableInt).sqlvalue = "null" Then retval = "" Else retval = obj.value.ToString
        ElseIf TypeOf obj Is nullableString Then
            If CType(obj, nullableString).sqlValue = "null" Then retval = "" Else retval = obj.value.ToString
        Else
            retval = value.ToString
        End If

        Return retval

    End Function

    Public Function setProperty(obj As Object, prop As String, value As Object, index As Object, ByRef errorMessages As List(Of String), audit As Boolean) As Boolean

        setProperty = False

        If InStr(prop, "(") Or InStr(prop, ".") Then

            'This screen template contains a derived field.. and we're adding with it
            Exit Function

        End If

        'need to walk the Prop

        setProperty = True

        Dim type As Type = obj.GetType
        Dim pinfo As PropertyInfo = type.GetProperty(prop)
        Dim ind(0) As Object

        'If pinfo IsNot Nothing Then
        If pinfo.PropertyType.Name = "DateTime" Then
            value = CDate(value)
        ElseIf pinfo.PropertyType.Name = "Boolean" Then
            value = CBool(value)
        ElseIf pinfo.PropertyType.Name = "Single" Then
            value = CSng(value)
        ElseIf pinfo.PropertyType.Name = "Int16" Then
            value = CType(value, Int16)
        ElseIf pinfo.PropertyType.Name = "Int32" Then
            value = CType(value, Int32)

        End If
        ' End If

        If Not pinfo.CanWrite Then
            ' Don't attempt to set the value of a read-only field
            Exit Function
        End If

        Try
            Dim was As Object
            If index Is Nothing Then
                was = pinfo.GetValue(obj)
                pinfo.SetValue(obj, value, Nothing)
            Else
                ind(0) = index
                was = pinfo.GetValue(obj, ind)
                pinfo.SetValue(obj, value, ind)
            End If

            If audit Then
                AuditLog.Instance.Add(AuditType.Editor, String.Format("{0}, Id:{1} was {2} now {3}", prop, obj.id, If(Not was Is Nothing, was.ToString, String.Empty), obj.displayname(English)), "Editor", 0)
            End If

        Catch ex As System.Exception
            errorMessages.Add(ex.ToString)
            Return False 'The assignemnet failed
        End Try

    End Function

    Public Function toString(prop As Object) As String

        'return a string representation

    End Function


    Public Function setPropertyFromString(col As clsField, target As Object, value As String, language As clsLanguage, ByRef errorMessages As List(Of String)) As Object

        'used to save the onscreen (textbox) values into the object
        setPropertyFromString = Nothing

        ' for 'one' datatypes - these values are indices into pick lists
        ' the cols propertyname may be a derived property - such as attributes(12).translation - so we need to walk to the correct target object/property

        If col.lookupOf <> "" Then
            'for foriegn key fields - this must resolve to a dictionary to look in - we need to 
            Dim rootdic = getPropertyValue(iq, Split(col.lookupOf, ".")(0)) 'We only take the first segment (no root paths are legacy) or possibly for autosuggest only
            Dim OBJ As Object = rootdic(CInt(value)) ' lookups are (currently) always into a root level dictionary 

            '  Dim was As String = OBJ.displayname(English)

            'SW.WriteLine(col.propertyName)
            'SW.WriteLine("WAS:" & OBJ.DISPLAYNAme(English) & " OBJid:" & OBJ.ID)

            'we may want to do something more exotic - to lookup from the members of another object (etc) 
            'Dim luo As String = Split(col.lookupOf, "(")(0) 'the bit before any open parenthesis - theoretically we may want to use lookups on some 
            'OBJ = WalkPropertyValue(iq, luo, (CInt(v$)), errorMessages) 'object we selected in the DDL, in the appropraite dictionary

            'TARGET MIGHT BE (FOR EXAMPLE) BRANCH.PRODUCT - WE'RE SETTING THE REFERENCE TO POINT TO OBJ
            setProperty(target, col.propertyName, OBJ, Nothing, errorMessages, True)
            'SW.WriteLine("NOW:" & OBJ.DISPLAYNAME(English) & " OBJidl" & OBJ.ID)

            'AuditLog.Instance.Add(AuditType.Editor, String.Format("{0}, Id:{1} was {2} now {3}", col.propertyName, OBJ.id, was, OBJ.displayname(English)), "Editor", 0)

        Else

            Dim prop As String = col.propertyName

            'If InStr(prop, ".") Or InStr(prop, "(") Then Stop

            Dim ob As Integer = InStr(prop, "(")
            If ob Then
                Dim cb As Integer = InStr(prop, ")")
                'e.g. i_attributes_code(Name).Translation
                Dim dic As Object = getPropertyValue(target, Left$(prop, ob - 1))
                Dim ib$ = Mid$(prop, ob + 1, cb - ob - 1)
                target = dic(ib$)
                prop = Mid$(prop, InStrRev(prop, ".") + 1)
            End If


            If InStr(prop, ".") Or InStr(prop, "(") Then errorMessages.Add("Prop contained . or (") : Exit Function

            If col.InputType.code = "int32" Then
                If col.propertyName <> "ID" Then
                    setProperty(target, prop, CInt(value), Nothing, errorMessages, True)
                End If
            ElseIf col.InputType.code = "boolean" Then
                setProperty(target, prop, CBool(value), Nothing, errorMessages, True)

            ElseIf col.InputType.code = "translate" Then

                Dim tobj As Object ' the property is a translation object (on which we set the text of a language)
                tobj = Reflection.WalkPropertyValue(target, prop, errorMessages)
                If tobj Is Nothing Then
                    tobj = iq.AddTranslation(value, language, "ED", 0, Nothing, 0, False)  'editing an (underlying) translation will change it everywhere
                Else
                    If value = "" Then
                        tobj = Nothing  'Allow them to de-reference a translation by blanking it
                    Else
                        tobj.text(language) = value
                    End If

                End If
                setProperty(target, prop, tobj, Nothing, errorMessages, True)

                If tobj IsNot Nothing Then
                    tobj.Update(language) 'we must update the translation opbject itself (aswell as the row containing it)
                End If

            ElseIf col.InputType.code = "nullstring" Then
                Dim ntobj As nullableString
                ntobj = Reflection.WalkPropertyValue(target, prop, errorMessages)
                ntobj.value = value  'setProperty(target, prop, v$, Nothing)

            ElseIf col.InputType.code = "nullint" Then
                Dim ntobj As NullableInt
                ntobj = Reflection.WalkPropertyValue(target, prop, errorMessages)
                'ntobj.value = CInt(value)  'setProperty(target, prop, v$, Nothing)
                Dim i As Integer
                If Integer.TryParse(value, i) Then
                    ntobj.value = i
                End If
            Else
                If LCase(prop) = "password" Then
                    value = simpleHash(Trim$(value)) 'Shuffle(md5(Trim$(value)))
                End If
                setProperty(target, prop, value, Nothing, errorMessages, True)
            End If
        End If

    End Function

    Public Function WalkPropertyValue(obj As Object, prop As String, ByRef errorMessages As List(Of String)) As Object

        'prop may be some 'straight' property - like Text
        'or a path to a property on the object
        'like attributes(16).Numericvalue

        ParsePath(prop, obj, Nothing, errorMessages)  'return ParseProperty(prop, obj)  <-previously

        Return obj

    End Function

    Public Function TypeOfProperty(obj As Object, prop As String) As String

        'Returns the typename of the specified property of the specified object (whose *value* might well be 'nothing' - but we need to know it's *type*)

        Dim type As Type = obj.GetType
        Dim pinfo As PropertyInfo = type.GetProperty(prop)

        Return pinfo.PropertyType.Name
        'If Index Is Nothing Then
        '    getPropertyValue = pinfo.GetValue(obj, Nothing)
        'Else
        '    Dim ind(0) As System.Object
        '    ind(0) = Index
        '    getPropertyValue = pinfo.GetValue(obj, ind)
        'End If

    End Function

    'Public Function getDicEntry(dic As Object, index As Object) As Object


    '    Dim type As Type = dic.GetType
    '    Dim pinfo As PropertyInfo = type.GetProperty("Item")

    '    Dim ind(0) As System.Object
    '    ind(0) = index
    '    'Dim ind As System.Object = CType(Index, System.Object)
    '    getDicEntry = pinfo.GetValue(dic, ind)

    'End Function

    Public Function getPropertyValue(obj As Object, prop As String, Optional Index As Object = Nothing) As Object

        Dim type As Type = obj.GetType

        Dim pinfo As PropertyInfo = type.GetProperty(prop)

        If pinfo Is Nothing Then Return Nothing
        If Index Is Nothing Then
            getPropertyValue = pinfo.GetValue(obj, Nothing)
        Else
            Dim ind(0) As System.Object
            ind(0) = Index
            'Dim ind As System.Object = CType(Index, System.Object)
            getPropertyValue = pinfo.GetValue(obj, ind)
        End If

    End Function

    Public Function CreateInstanceLike(TemplateObj As Object) As Object

        Dim r As Object
        r = Activator.CreateInstance(TemplateObj.GetType)
        Return r

    End Function

    'Public Function createInstance(assemblyname$, typename$) As Type


    '    Dim ty As Type
    '    ty = Activator.CreateInstance(assemblyname$, typename$).GetType()

    '    Return ty


    'End Function


    'Public Class TestPropertyInfo
    '    Public Shared Sub Main()
    '        Dim t As New TestClass()

    '        ' Get the type and PropertyInfo.
    '        Dim myType As Type = t.GetType()
    '        Dim pinfo As PropertyInfo = myType.GetProperty("Caption")

    '        ' Display the property value, using the GetValue method.
    '        Console.WriteLine(vbCrLf & "GetValue: " & pinfo.GetValue(t, Nothing))

    '        ' Use the SetValue method to change the caption.
    '        pinfo.SetValue(t, "This caption has been changed.", Nothing)

    '        ' Display the caption again.
    '        Console.WriteLine("GetValue: " & pinfo.GetValue(t, Nothing))

    '        Console.WriteLine(vbCrLf & "Press the Enter key to continue.")
    '        Console.ReadLine()
    '    End Sub
    'End Class



End Module




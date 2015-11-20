//Option Strict On
using System.IO;
using System.Reflection;

class Reflection
{

	public class clsCriterea
	{
		public string Prop;
		public string Op;

		public string value;
		public clsCriterea(string Prop, string op, string value)
		{
			this.Prop = Prop;
			this.Op = op;
			this.value = value;
		}

	}

	//Public Function Match(obj As Object, screen As clsScreen, critirea As List(Of clsCriterea)) As Boolean

	//    Dim v As Object = Reflection.getPropertyValue(obj, prop)
	//    If LCase(v) = LCase(value) Then Return True Else Return False

	//End Function

	//Public Function Parsefilter(filter$) As List(Of clsCriterea)

	//    'filter contains a comma seperated list of expressions
	//    'of the form Property opertator Value
	//    'e.g. Displayname=Tech*,Country=UK
	//    'or Price>15<20
	//    'generally these are assembled from dropdown lists which have been generated automatically
	//    'they may include calculated properties such as price
	//    'or indexed properies such as productAttributes(mass)<10

	//    Parsefilter = New List(Of clsCriterea)

	//    Dim c() As String
	//    c = Split(filter$, ",")

	//    For Each f In c
	//        If InStr(f, "=") Then
	//            Dim b$() = Split(f, "=")
	//            critera.add new clsCriterea(b(0),"=",b(edit1)
	//        End If
	//    Next

	//    Dim pv() As String = Split(filter, "=")
	//    prop = pv(0)
	//    value = pv(1)

	//End Function

	public object Findmatch(object dic, string filterPropValue, ref List<string> errorMessages)
	{

		//    'If no filter is specified, returns the supplied dictionary DIC
		//    'otherwise
		//    'filters can originate from a fields  .LookUpOf property. e.g. States(group=TH)
		//    'NB:- filters (and reflection generally) are case sensitive against the OM i.e.   e.g. states(group=TH)  - would NOT work

		Findmatch = null;

		if (dic == null)
			return null;
		if (dic.Values.Count == 0)
			return null;
		if (filterPropValue == "") {
			//Return dic.Values(0)  <-- This *does not* work - you get 'no accessible 'values' accepts tis number of arguments - the only way to get to the values seesm to be through emumeration.. (which works just fine, so don't fight it)
			foreach ( v in dic.Values) {
				return v;
				return;
				//probably redundant - but here ffor clarity
			}
			//will never execute
		}

		string[] p = Split(filterPropValue, "=");
		//todo - check there's 2 bits

		foreach ( obj in dic.Values) {
			//         Dim prop As Object = Reflection.getPropertyValue(obj, p(0)) ' probably need to get the text of translations here
			object prop = Reflection.WalkPropertyValue(obj, p(0), errorMessages);
			// probably need to get the text of translations here

			if (prop == p(1))
				return obj;
		}

	}

	//Public Sub oldWalkDown(ByRef obj As Object, ByRef path$)

	//    'modifies OBJ and path to be the last object and property on the path 
	//    'could almost certainly be consolidated with parseProperty/parsePath

	//    'Starts at obj, and walks path$ to return a property

	//    'eg. on a branch we may want to find
	//    ' product.attributes(17).Numericvalue

	//    Dim hops As List(Of String)
	//    hops = Split(path$, ".").ToList


	//    Dim hopName As String
	//    Dim hopIndex As String
	//    Dim ob, cb As Integer

	//    Dim hc As Integer = 0

	//    For Each hop In hops
	//        hc = hc + 1
	//        ob = InStr(hop, "(")
	//        cb = InStr(hop, ")")
	//        If ob Then
	//            hopName = Left(hop, ob - 1)
	//            hopIndex = Mid$(hop, ob + 1, cb - ob - 1)

	//            '                ParentObj = OBJ
	//            obj = Reflection.getPropertyValue(obj, hopName) 'this is a dictionary
	//            If IsNumeric(hopIndex) Then
	//                If obj.containskey(Val(hopIndex)) Then
	//                    obj = obj(Val(hopIndex)) 'fetch the specified row (by ID) from it
	//                Else
	//                    Stop
	//                End If
	//            Else
	//                'this hop index (in parentheses) is not a number - so it's a filter eg Country=En*
	//                obj = Findmatch(obj, hopIndex)
	//                'ParsePath$ = hopIndex 'a filter
	//            End If
	//        Else
	//            '               ParentObj = OBJ
	//            obj = Reflection.getPropertyValue(obj, hop) 'this is a DICTIONARY
	//        End If

	//        If hc = hops.Count - 1 Then Exit For

	//    Next

	//    path$ = hops.Last

	//End Sub

	//Public Function OldParseProperty(path$, ByVal OBJ As Object) As Object

	//    'Starts at obj, and walks path$ to return a property

	//    'eg. on a branch we may want to find
	//    ' product.attributes(17).Numericvalue
	//    'we may also 'vector' to anothe product (CPU being the prime example)

	//    'OBJ would be a branch
	//    'CPU.i_attributes_code(Speed)

	//    path$ = Replace(path$, ")(", ").(")  'This replaces any default indexer - adding the .

	//    Dim hops As List(Of String)
	//    hops = Split(path$, ".").ToList

	//    Dim hopName As String
	//    Dim hopIndex As String
	//    Dim ob, cb As Integer

	//    For Each hop In hops
	//        ob = InStr(hop, "(")
	//        cb = InStr(hop, ")")
	//        If ob Then
	//            hopName = Left(hop, ob - 1)
	//            hopIndex = Mid$(hop, ob + 1, cb - ob - 1)

	//            If hopName <> "" Then ' skip over the dfault indexers - eg i_attributes(sku)(0)
	//                OBJ = Reflection.getPropertyValue(OBJ, hopName) 'this is a dictionary
	//            End If


	//            If InStr(hopIndex, "=") Then
	//                OBJ = Findmatch(OBJ, hopIndex)
	//            Else
	//                If TypeOf (OBJ) Is IList Then
	//                    OBJ = OBJ(hopIndex)
	//                Else
	//                    If OBJ.containskey(hopIndex) Then
	//                        OBJ = OBJ(hopIndex) 'look isn't that pretty                    
	//                    Else
	//                        OBJ = Nothing
	//                        Exit For
	//                    End If

	//                End If

	//            End If

	//        Else
	//            'Something like "product."
	//            If UCase(hop) = "CPU" Then 'Allows us to vector to the attributes of the CPU, by SKU, as help in one of the product attributes
	//                Dim product As clsProduct
	//                product = OBJ
	//                If product.i_Attributes_Code.ContainsKey("cpuSKU") Then
	//                    OBJ = iq.i_SKU(product.i_Attributes_Code("cpuSKU")(0).Translation.text(English))
	//                Else
	//                    Return Nothing
	//                End If

	//            Else
	//                OBJ = Reflection.getPropertyValue(OBJ, hop)
	//            End If
	//        End If
	//    Next hop

	//    Return OBJ


	//End Function

	//Public Function oldParsePath(path$, ByRef Obj As Object, ByRef ParentObj As Object) As String

	//    'sets Obj and ParentObj 
	//    'returns the filter portion - if present

	//    'uses reflection from the IQ root object to find the specified object OR dictionary (and it's parent)

	//    ' a path can be to a single object 
	//    ' e.g.Threads(17)
	//    ' a dictionary
	//    ' e.g. Variants
	//    ' or a filtered dictionary
	//    ' e.g. ProductAttributes(product.id=1,attribute.code=weight)

	//    Dim hops As List(Of String)
	//    hops = Split(path$, ".").ToList

	//    Obj = iq 'let's start at the very begining (a very good place to start)

	//    Dim hopName As String
	//    Dim hopIndex As String
	//    Dim ob, cb As Integer

	//    For Each hop In hops
	//        ob = InStr(hop, "(")
	//        cb = InStr(hop, ")")
	//        If ob Then
	//            hopName = Left(hop, ob - 1)
	//            hopIndex = Mid$(hop, ob + 1, cb - ob - 1)

	//            ParentObj = Obj
	//            Obj = Reflection.getPropertyValue(Obj, hopName) 'this is a dictionary
	//            If IsNumeric(hopIndex) Then  'an index may be another objet - or path thereto  - so we needs some sort of recursive parses
	//                Obj = Obj(Val(hopIndex)) 'fetch the specified row (by ID) from it 


	//            Else
	//                'this hop index (in parentheses) is not a number - so it's a filter eg Country=En*
	//                oldParsePath$ = hopIndex 'a filter
	//            End If
	//        Else
	//            ParentObj = Obj
	//            Obj = Reflection.getPropertyValue(Obj, hop) 'this is an object (not a  DICTIONARY - surely)
	//        End If
	//    Next

	//End Function

	/// <summary>uses reflection from the Specified object to find the specified object OR dictionary (and it's parent)</summary>
	public string ParsePath(path, ref object Obj, ref object ParentObj, ref List<string> errorMessages)
	{

		ParsePath = "";
		//sets Obj and ParentObj 
		//returns the filter portion - if present

		// a path can be to a single object 
		// e.g.Threads(17)
		// a dictionary
		// e.g. Variants
		// or a filtered dictionary
		// e.g. ProductAttributes(product.id=1,attribute.code=weight)
		//Dictionary keys can also be other objects - with nested evaluations such as
		//branch.Product.i_variants(Channels(17))
		//you can also index dictionaries with literal string keys - such as product.i_attributes(mfrsku)

		//Where a default indxer has been used - add the .item( explicitly
		//for example products.i_attributes(mfsku)(0)
		//Becomes products.i_attributes(mfrsku).Items(0)


		path = Replace(path, ")(", ").Items(");

		List<string> hops;
		hops = Split(path, ".").ToList;


		string objName;
		string hopIndex;
		int ob;

		object indexedObj;
		// a dictionary or list

		foreach ( hop in hops) {
			ParentObj = Obj;

			ob = InStr(hop, "(");
			if (ob) {
				objName = Left(hop, ob - 1);

				hopIndex = betweenParentheses(hop, ob);
				//Mid$(hop, ob + 1, cb - ob - 1)

				//does the index need evaluating (to an object) eg Product.i_variants(Channels(23))
				if (InStr(hopIndex, "(")) {
					object io = iq;
					ParsePath(hopIndex, io, null, errorMessages);
					//recurse to evalute the inner (indexing) object
					Obj = Reflection.getPropertyValue(Obj, objName);
					if (Obj == null) {
						errorMessages.Add("Obj was nothing at path " + path);
					} else {
						Obj = Obj(io);
					}


				} else {
					if (objName == "Items") {
						Obj = Obj(hopIndex);
						if (Obj == null)
							errorMessages.Add("Items hopindex was nothing " + path);
					} else {
						indexedObj = Reflection.getPropertyValue(Obj, objName);
						//a dictionary or list
						if ((indexedObj) is IDictionary | (indexedObj) is IList) {
							if (hopIndex == null) {
								errorMessages.Add("Indexer was nothing in paresepath:" + path);
								return;
							} else {
								if (indexedObj.containskey(hopIndex)) {
									Obj = indexedObj(hopIndex);
									if (Obj == null)
										errorMessages.Add("Obj was nothing in parsepath");
								} else {
									Obj = null;
									break; // TODO: might not be correct. Was : Exit For
								}
							}
						} else {
							if (indexedObj == null) {
								errorMessages.Add("Could not evaluate path beyond " + objName + " (Paths are CaSe SeNsitiVe)");
							} else {
								errorMessages.Add("An unrecognised object type " + indexedObj.GetType.ToString + " was indexed");
							}
							Obj = null;


							return;
						}
					}
				}
			} else {
				//Allows us to vector to the attributes of the CPU, by SKU, as help in one of the product attributes
				if (UCase(hop) == "CPU") {
					clsProduct product;
					product = Obj;
					if (product.i_Attributes_Code.ContainsKey("cpuSKU")) {
						string CPUsku = product.i_Attributes_Code("cpuSKU")(0).Translation.text(English);

						if (iq.i_SKU.ContainsKey(CPUsku)) {
							Obj = iq.i_SKU(CPUsku);
						} else {
							Obj = null;
							return "";
						}
					} else {
						Obj = null;
						return "";
					}


				} else {
					Obj = Reflection.getPropertyValue(Obj, hop);
					if (Obj == null) {
						return "";
					}

				}

			}
		}

	}


	//If TypeOf (Obj) Is IList Then
	//    Obj = Obj = Obj(hopIndex)
	//ElseIf TypeOf (Obj) Is IDictionary Then
	//    If InStr(hopIndex, "(") = 0 Then
	//        If Obj.containskey(hopIndex) Then Obj = Obj(hopIndex)
	//    Else
	//        Dim io As Object = iq
	//        ParsePath(hopIndex, io, Nothing)  'recurse to evalute the inner (indexing) object

	//        Obj = Obj(io)
	//    End If
	//    Obj = Reflection.getPropertyValue(Obj, hopName) 'this is a dictionary
	//    ' End If

	//    If IsNumeric(hopIndex) Then  'an index may be another object - or path thereto  - so we needs some sort of recursive parses

	//        '   Obj = Reflection.getPropertyValue(Obj, hopName) 'this is a dictionary
	//        Obj = Obj(Val(hopIndex)) 'fetch the specified row (by ID) from it 

	//    Else

	//        If TypeOf (Obj) Is IList Then
	//            Obj = Obj(hopIndex)
	//        Else
	//            If TypeOf (Obj) Is IDictionary Then
	//                If InStr(hopIndex, "(") = 0 Then
	//                    If Obj.containskey(hopIndex) Then Obj = Obj(hopIndex)
	//                Else
	//                    Dim io As Object = iq
	//                    ParsePath(hopIndex, io, Nothing)  'recurse to evalute the inner (indexing) object

	//                    Obj = Obj(io)

	//                    'now take the part beyond the parentheses - this is the property we're looking for - or (0) in the case of attributes lists

	//                    'Dim dic As Object = Reflection.getPropertyValue(Obj, hopName) 'this is a dictionary
	//                    ' Obj = getDicEntry(dic, io)
	//                End If
	//            End If


	//        End If

	//Obj = Reflection.getPropertyValue(dic.values(0), "values", io) 'this is a dictionary

	//this hop index (in parentheses) is not a number - so it's a filter eg Country=En*
	//ParsePath$ = hopIndex 'a filter


	private string betweenParentheses(l, int ob)
	{

		betweenParentheses = null;

		//Returns the portion of l$ between the "(" at OB and it's *corresponding* ")"
		//e.g. 
		//Products.i_variants(Channels(32).users(12))  

		object c;
		int o = 0;
		int i;
		for (i = ob; i <= Len(l); i++) {
			c = Mid(l, i, 1);
			if (c == "(")
				o = o + 1;
			if (c == ")") {
				o = o - 1;
				if (o == 0){betweenParentheses = Mid(l, ob + 1, i - 1 - ob);break; // TODO: might not be correct. Was : Exit For
}
			}
		}

		// If i > Len(l$) Then Stop 'failed (mismatching parentheses)

	}


	public Dictionary<string, string> classList;

	public void setupClassList()
	{
		classList = new Dictionary<string, string>();
		classList.Add("clsCountry", "DisplayName");
		//multilingual
		classList.Add("clsAccount", "Name");
		classList.Add("clsTeam", "Name");
		//localised (one language)
		classList.Add("clsUser", "RealName");
		classList.Add("clsLanguage", "LocalName");
		//localsed one language
		classList.Add("clsCurrency", "DisplayName");
		//multilingual
		classList.Add("clsRole", "DisplayName");
		//multilingual
		classList.Add("clsChannel", "DisplayName");
		classList.Add("clsUnit", "Code");
		classList.Add("clsProduct", "DisplayName");
		classList.Add("clsAttribute", "Code");
		classList.Add("clsProductType", "DisplayName");
		classList.Add("clsSector", "code");
		classList.Add("clsThread", "title");
		classList.Add("clsState", "code");
		classList.Add("clsEvent", "ID");
		classList.Add("clsScreen", "title");
		classList.Add("clsValidation", "description");
		classList.Add("clsInputType", "name");
		classList.Add("clsBranch", "name");
		classList.Add("clsQuantity", "name");
		classList.Add("clsSlotType", "name");
		classList.Add("clsSlot", "name");
		classList.Add("clsVariant", "name");
		classList.Add("clsStock", "name");
		classList.Add("clsPrice", "name");
		classList.Add("clsRegion", "DisplayName");
		classList.Add("clsCampaign", "DisplayName");
		classList.Add("clsAdvert", "DisplayName");
		classList.Add("clsClickThru", "DisplayName");
		classList.Add("clsImpression", "DispayName");


	}

	public string RootDictionary(Type ty)
	{
		//returns the name of the root level dictionary which contains objects of the specified type - eg, countries, currencies, accounts, products etc.

		RootDictionary = "";
		System.Type tod;

		PropertyInfo[] properties_info = typeof(IQ.clsIQ).GetProperties();

		foreach ( p in properties_info) {
			if (LCase(p.PropertyType.Name) == "dictionary`2") {
				Debug.Print(p.Name);
				//      If LCase(p.Name) = "channels" Then Stop
				tod = typeOfDictionary(p.PropertyType.FullName);
				if (object.ReferenceEquals(ty, tod)) {
					return p.Name;
				}
			}
		}

		// Stop
		//couldn't locate a root level dictionary of type ty

	}

	public bool IsDictionary(object obj)
	{

		PropertyInfo[] properties_info = obj.GetType.GetProperties();
		Type ty;
		ty = obj.GetType;
		if (ty.Name == "Dictionary`2")
			return true;
		else
			return false;

		//For Each objProperty In properties_info
		//    order += 1 'each field has an order property - allowing them to be shuffled around
		//    ptn = objProperty.PropertyType.Name  'the type of this property of the obejct (that this screen manages)
		//    Select LCase(ptn)
		//        Case "dictionary`2"  'we've found a 'dictionary of' within the OM.. so we'll make a new subscreen to manage objects of the type contained in the dictionary

	}


	public void DeleteScreen(int ID, ref List<string> errormessages)
	{
		iq.Screens(ID).Delete(errormessages);

	}

	public void Serialize(object obj, StreamWriter sw, int level)
	{
		Type ty = obj.GetType;
		PropertyInfo[] properties_info = ty.GetProperties();
		//Get all the public properties of the object we're making a screen to handle

		string ptn;

		foreach ( objProperty in properties_info) {
			ptn = objProperty.PropertyType.Name;
			//the type of this property of the obejct (that this screen manages)

			switch ((LCase(ptn))) {

				case "string":
				case "integer":
				case "single":
				case "int32":
				case "boolean":

					if (objProperty.GetIndexParameters().Length > 0) {
						//it's a dictionary or list (and therefore of some complex type/class) - properties with paramaters (eg. display name) also come through here it seems
						string w = objProperty.PropertyType.ToString;

					} else {
						object v = Reflection.getPropertyValue(obj, objProperty.Name);
						string s = obj.ToString;
						sw.Write(s.ToCharArray);
						sw.Write("¬".ToCharArray);


					}
				case "dictionary`2":
					//we've found a 'dictionary OF' within the OM.. so we'll recurse make a new subscreen to manage objects of the type contained in the dictionary

					object dic;
					dic = Reflection.getPropertyValue(obj, objProperty.Name);
					foreach ( v in dic.values) {
						if (level > 1) {
							sw.Write(Trim((string)v.id).ToCharArray);
						} else {
							Serialize(v, sw, level + 1);
						}
					}

			}
		}

	}


	public clsScreen MakeScreen(string Title, string code, Type objType, Type parentType, List<string> errormessages)
	{

		//Builds an input screen with a specified name, for the type of object specified... and recurses (for screens that manage 'many')

		//Dim dicname As String
		//dicname = RootDictionary(objType)

		clsField afield;
		string ptn;

		//Each Object eg. "Customer" has many Properties, eg. Name, Address, Telehone number - Some of which may be objects (or collections of objects) in their own right
		int order = 0;

		clsScreen ThisScreen;
		ThisScreen = new clsScreen(objType.Name, code, Title, errormessages);

		PropertyInfo[] properties_info = objType.GetProperties();
		//Get all the properties of the object we're making a screen to handle

		foreach ( objProperty in properties_info) {
			order += 1;
			//each field has an order property - allowing them to be shuffled around

			ptn = objProperty.PropertyType.Name;
			//the type of this property of the obejct (that this screen manages)

			float emshigh = 1.5;

			switch (LCase(ptn)) {
				case "dictionary`2":
					//we've found a 'dictionary OF' within the OM.. so we'll recurse make a new subscreen to manage objects of the type contained in the dictionary
					//by choice, we embed dictionaries not lists - because elements of a dictionary are rapidly and individually addressable (by key) - by the editor (amongst other things)

					// All very clever - but unnecessary ! - it's better to make the subscreen 'just in time' too - less code, less complex

					//'what type of object are the values of his dictionary
					//dty = objProperty.PropertyType.GetGenericArguments(1)  'You MUST use objProperty.PropertyType NOT .gettype ! (took a long time to work out!)

					//Dim subScreen As clsScreen
					//If iq.i_screens_ObjType.ContainsKey(dty.Name) Then
					//    subScreen = iq.i_screens_ObjType(dty.Name)  'we've already made this screen (possibly through recursion)
					//Else
					//    'recurse
					//    subScreen = MakeScreen(objProperty.Name, dty, objType)   'make a screen for this type of object
					//End If

					//make a field - which embeds the screen we just created - to manage a list of objects of this type
					clsInputType it;
					it = iq.i_inputType_code("many");

					//this used named arguments for clarity - it just shows you (the developer) which parameters are called what - without having to use intellisense
					//http://msdn.microsoft.com/en-us/library/51wfzyw0(v=vs.80).aspx
					afield = new clsField(screen: ThisScreen, propertyName: objProperty.Name, lookupof: "", labeltext: iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, null, 0, false), helptext: "help for " + objProperty.Name, validation: null, inputType: it, length: 50, order: order, width: 5,
					height: emshigh, defaultvalue: "", visibleList: true, visiblePage: true, defaultFilter: "", defaultsort: "", priority: 5, quickFilterGroup: null, QuickFilterUIType: "", CanUserSelect: false,

					LinkedFieldID: null, FilterVisible: true);

				case "string":
				case "integer":
				case "single":
				case "int32":
				case "boolean":

					if (objProperty.GetIndexParameters().Length > 0) {
						//it's a dictionary or list (and therefore of some complex type/class) - properties with paramaters (eg. display name) also come through here it seems
						string w = objProperty.PropertyType.ToString;
					} else {
						//it's a simple type 
						int width;
						if (LCase(ptn) == "int32")
							width = 5;
						else
							width = 10;
						if (LCase(ptn) == "boolean")
							width = 2;
						object @default = "";
						if (LCase(objProperty.Name) == "current")
							@default = "1";
						//Default the 'current' field on auditable objects to 1

						if (objType.Name == "clsQuantity" & ptn == "Path")
							@default = "[treepath]";
						if (objType.Name == "clsSlot" & ptn == "Path")
							@default = "[treepath]";

						afield = new clsField(ThisScreen, (string)objProperty.Name, "", iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, null, 0, false), "help for " + objProperty.Name, null, iq.i_inputType_code(LCase(ptn)), 50, order, width,
						emshigh, @default, true, true, "", "", 5, null, "", false,
						null, true);

					}
				case "datetime":
					afield = new clsField(ThisScreen, (string)objProperty.Name, "", iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, null, 0, false), "help for " + objProperty.Name, null, iq.i_inputType_code("date"), 10, order, 5,
					emshigh, "[now]", true, true, "", "", 5, null, "", false,
					null, true);
				case "clstranslation":
					afield = new clsField(ThisScreen, (string)objProperty.Name, "", iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, null, 0, false), "help for " + objProperty.Name, null, iq.i_inputType_code("translate"), 100, order, 15,
					emshigh, "", true, true, "", "", 5, null, "", false,
					null, true);
				case "nullablestring":
					afield = new clsField(ThisScreen, (string)objProperty.Name, "", iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, null, 0, false), "help for " + objProperty.Name, null, iq.i_inputType_code("nullstring"), 100, order, 15,
					emshigh, "", true, true, "", "", 5, null, "", false,
					null, true);
				case "nullableint":
					afield = new clsField(ThisScreen, (string)objProperty.Name, "", iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, null, 0, false), "help for " + objProperty.Name, null, iq.i_inputType_code("nullint"), 7, order, 7,
					emshigh, "", true, true, "", "", 5, null, "", false,
					null, true);
				case "nullableprice":
					afield = new clsField(ThisScreen, (string)objProperty.Name, "", iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, null, 0, false), "help for " + objProperty.Name, null, iq.i_inputType_code("nullprice"), 10, order, 12,
					emshigh, "", true, true, "", "", 5, null, "", false,
					null, true);
				default:

					//its another type or object (but not a list) - present as a dropdown list

					if (classList.ContainsKey(ptn)) {
						Assembly a = Assembly.GetExecutingAssembly;
						Type ty = null;
						foreach ( t in a.GetTypes) {
							if (LCase(ptn) == LCase(t.Name)){ty = t;break; // TODO: might not be correct. Was : Exit For
}
						}

						//ty is the type of object - eg clsUser, clsChannel, clsThread

						if (ty == null)
							errormessages.Add("could not locate type");

						if (LCase(objProperty.Name) == "auditroot") {
							ThisScreen.Auditable = true;
							//This Class has a root property - grouping instances (and a Current Property)
						}

						object @default;
						@default = "";
						if (object.ReferenceEquals(ty, parentType)) {
							//where any child object contains a reference back to its parent - such as a field.screen 
							// (being a member of the screen.fields dictionary)
							//having links though the OM in 'both directions' ('down' to descendants and 'up' to ancestors)
							// make it easier to navigate and faster
							//
							@default = "[parent]";
							//this is foreign key, rendered as a drop down list - under
						}

						if (object.ReferenceEquals(ty, objType)) {
							@default = "[tree]";
							//this field (on this screen) points to instances of itself (a recursive tree)... not sure this is materially different from the above
						}


						object lkup;
						lkup = objProperty.Name;
						if (InStr(lkup, "_")) {
							lkup = lkup.Split("_")(0);
							//used when creating tree structures eg. channel_children - obsoleted now i think
						}


						//any object that contains an AuditRoot property has a pointer to its top ancestor
						if (LCase(objProperty.Name) == "auditroot") {
						//              lkup$ = dicname & ".ID"
						} else {
							//exceptions
							switch (LCase(lkup)) {
								case  // ERROR: Case labels with binary operators are unsupported : Equality
"iscloneof":
								case "sellerchannel":
								case "buyer":
									lkup = "Channels";
								case  // ERROR: Case labels with binary operators are unsupported : Equality
"parent":
									//You don't generally want to be shoing these (i don't think)
									if (LCase(ptn) == "clschannel") {
										lkup = "Channels";
									//want to expose this as an integer (to allow reparenting)
									} else if (ptn == "clsThread") {
										lkup = "Threads";
									} else if (ptn == "clsBranch") {
										lkup = "Branches";
									} else if (ptn == "clsRegion") {
										lkup = "Regions";
									} else {
										// Stop
									}
								case  // ERROR: Case labels with binary operators are unsupported : Equality
"buyerchannel":

									lkup = "Channels";
								case  // ERROR: Case labels with binary operators are unsupported : Equality
"createdby":
								case "assignedto":
									lkup = "Users";
								case  // ERROR: Case labels with binary operators are unsupported : Equality
"status":
									//Thread status
									//need some ability to filter
									lkup = "States(group=TH)";
								case  // ERROR: Case labels with binary operators are unsupported : Equality
"priority":
									//Thread Priority
									// 'to become a translation
									lkup = "States(group=PR)";
								case  // ERROR: Case labels with binary operators are unsupported : Equality
"eventlog":
									lkup = "Events";
								case  // ERROR: Case labels with binary operators are unsupported : Equality
"branch":
									lkup = "Branches";
								case  // ERROR: Case labels with binary operators are unsupported : Equality
"type":
									lkup = "SlotTypes";
								case  // ERROR: Case labels with binary operators are unsupported : Equality
"seller":
									lkup = "Channels";
								case  // ERROR: Case labels with binary operators are unsupported : Equality
"skuvariant":
									lkup = "Variants";
								case  // ERROR: Case labels with binary operators are unsupported : Equality
"matrix":
								case "grid":
									lkup = "Screens";
								case  // ERROR: Case labels with binary operators are unsupported : Equality
"present":
								case "absent":
									//adverts

									lkup = "ProductTypes";
								default:
									//the class is no longer required as it is now present in every field (which is clearer)
									//& "." & classlist(ptn$) 'adds the Display property as defined in the ClassList (often. displayname)
									lkup = Plural(lkup);
							}
						}

						//this is a reference to a single object - and should not be a plural
						// If LCase(Right$(.Name, 1)) = "s" Then Stop
						afield = new clsField(ThisScreen, (string)objProperty.Name, lkup, iq.AddTranslation(objProperty.Name, English, "FLDLBL", 1, null, 0, false), "help for " + objProperty.Name, null, iq.i_inputType_code("one"), 0, order, 10,
						emshigh, @default, true, true, "", "", 5, null, "", false,
						null, true);
					} else {
						Debug.Print(ptn);
						// Stop
					}
			}
		}

		return ThisScreen;

	}

	public Type TypeOfDicOrObj(object o)
	{

		if (Reflection.IsDictionary(o)) {
			return typeOfDictionary(o);
		} else {
			return o.GetType();
		}

	}


	public Type typeOfDictionary(object obj)
	{

		//returns the type of the values collection of a regulay key-value dictionary

		Type[] args = obj.GetType.GetGenericArguments;
		return args(1);
		//the second dimension (the values) in the dictionary

	}

	public System.Type typeOfDictionary(string pfn)
	{

		//find the type of the second dimension of the dictionary (the first dimension is the integer index) - the second is some kind of object
		//an Account, Invoice. Product, Country etc.

		string opfn;
		opfn = pfn;

		pfn = Mid(pfn, InStr(pfn, "],[") + 3);
		pfn = Left(pfn, InStr(pfn, ",") - 1);

		string[] cn = pfn.Split(".");
		string typename = cn.Last;

		//this is dirty - but classes declared in a module seem to get prefixed moudulename+
		//in fact his whole bit is ugly .. i'm sure there's a better way (to get the type of a class by name)
		if (InStr(typename, "+"))
			typename = Split(typename, "+")(1);

		Assembly a = Assembly.GetExecutingAssembly;
		Type ty = null;
		foreach ( t in a.GetTypes) {
			if (LCase(typename) == LCase(t.Name)) {
				ty = t;
				break; // TODO: might not be correct. Was : Exit For
			}
		}
		//If ty Is Nothing Then Stop

		return ty;
		///End of ugly bit


	}

	public bool TypeFits(object prop, object obj, propname)
	{

		TypeFits = false;

		//is the object Prop, the correct type to fit in the propname$ propery of Obj ?
		// Dim test As Object = Reflection.getPropertyValue(obj, propname$)

		Type type = obj.GetType;
		PropertyInfo pinfo1 = type.GetProperty(propname);
		//get the information anout Obj's Propname$

		Type ty1;
		Type ty2;
		ty1 = prop.GetType;
		ty2 = pinfo1.GetType;
		if (object.ReferenceEquals(ty1, ty2)) {
			return true;
		}

	}

	public string getPropertyString(object obj, string prop, clsLanguage language)
	{

		object value = getPropertyValue(obj, prop);

		string retval;

		if (obj is clsTranslation) {
			retval = obj.text(language);
		} else if (obj is NullableInt) {
			if (((NullableInt)obj).sqlvalue == "null")
				retval = "";
			else
				retval = obj.value.ToString;
		} else if (obj is nullableString) {
			if (((nullableString)obj).sqlValue == "null")
				retval = "";
			else
				retval = obj.value.ToString;
		} else {
			retval = value.ToString;
		}

		return retval;

	}

	public bool setProperty(object obj, string prop, object value, object index, ref List<string> errorMessages, bool audit)
	{

		setProperty = false;


		if (InStr(prop, "(") | InStr(prop, ".")) {
			//This screen template contains a derived field.. and we're adding with it
			return;

		}

		//need to walk the Prop

		setProperty = true;

		Type type = obj.GetType;
		PropertyInfo pinfo = type.GetProperty(prop);
		object[] ind = new object[-1];

		//If pinfo IsNot Nothing Then
		if (pinfo.PropertyType.Name == "DateTime") {
			value = (System.DateTime)value;
		} else if (pinfo.PropertyType.Name == "Boolean") {
			value = (bool)value;
		} else if (pinfo.PropertyType.Name == "Single") {
			value = (float)value;
		} else if (pinfo.PropertyType.Name == "Int16") {
			value = (Int16)value;
		} else if (pinfo.PropertyType.Name == "Int32") {
			value = (Int32)value;

		}
		// End If

		if (!pinfo.CanWrite) {
			// Don't attempt to set the value of a read-only field
			return;
		}

		try {
			object was;
			if (index == null) {
				was = pinfo.GetValue(obj);
				pinfo.SetValue(obj, value, null);
			} else {
				ind(0) = index;
				was = pinfo.GetValue(obj, ind);
				pinfo.SetValue(obj, value, ind);
			}

			if (audit) {
				AuditLog.Instance.Add(AuditType.Editor, string.Format("{0}, Id:{1} was {2} now {3}", prop, obj.id, !was == null ? was.ToString : string.Empty, obj.displayname(English)), "Editor", 0);
			}

		} catch (System.Exception ex) {
			errorMessages.Add(ex.ToString);
			return false;
			//The assignemnet failed
		}

	}

	public string toString(object prop)
	{

		//return a string representation

	}


	public object setPropertyFromString(clsField col, object target, string value, clsLanguage language, ref List<string> errorMessages)
	{

		//used to save the onscreen (textbox) values into the object
		setPropertyFromString = null;

		// for 'one' datatypes - these values are indices into pick lists
		// the cols propertyname may be a derived property - such as attributes(12).translation - so we need to walk to the correct target object/property

		if (col.lookupOf != "") {
			//for foriegn key fields - this must resolve to a dictionary to look in - we need to 
			object rootdic = getPropertyValue(iq, Split(col.lookupOf, ".")(0));
			//We only take the first segment (no root paths are legacy) or possibly for autosuggest only
			object OBJ = rootdic((int)value);
			// lookups are (currently) always into a root level dictionary 

			//  Dim was As String = OBJ.displayname(English)

			//SW.WriteLine(col.propertyName)
			//SW.WriteLine("WAS:" & OBJ.DISPLAYNAme(English) & " OBJid:" & OBJ.ID)

			//we may want to do something more exotic - to lookup from the members of another object (etc) 
			//Dim luo As String = Split(col.lookupOf, "(")(0) 'the bit before any open parenthesis - theoretically we may want to use lookups on some 
			//OBJ = WalkPropertyValue(iq, luo, (CInt(v$)), errorMessages) 'object we selected in the DDL, in the appropraite dictionary

			//TARGET MIGHT BE (FOR EXAMPLE) BRANCH.PRODUCT - WE'RE SETTING THE REFERENCE TO POINT TO OBJ
			setProperty(target, col.propertyName, OBJ, null, errorMessages, true);
		//SW.WriteLine("NOW:" & OBJ.DISPLAYNAME(English) & " OBJidl" & OBJ.ID)

		//AuditLog.Instance.Add(AuditType.Editor, String.Format("{0}, Id:{1} was {2} now {3}", col.propertyName, OBJ.id, was, OBJ.displayname(English)), "Editor", 0)


		} else {
			string prop = col.propertyName;

			//If InStr(prop, ".") Or InStr(prop, "(") Then Stop

			int ob = InStr(prop, "(");
			if (ob) {
				int cb = InStr(prop, ")");
				//e.g. i_attributes_code(Name).Translation
				object dic = getPropertyValue(target, Left(prop, ob - 1));
				object ib = Mid(prop, ob + 1, cb - ob - 1);
				target = dic(ib);
				prop = Mid(prop, InStrRev(prop, ".") + 1);
			}


			if (InStr(prop, ".") | InStr(prop, "(")){errorMessages.Add("Prop contained . or (");return;
}

			if (col.InputType.code == "int32") {
				if (col.propertyName != "ID") {
					setProperty(target, prop, (int)value, null, errorMessages, true);
				}
			} else if (col.InputType.code == "boolean") {
				setProperty(target, prop, (bool)value, null, errorMessages, true);


			} else if (col.InputType.code == "translate") {
				object tobj;
				// the property is a translation object (on which we set the text of a language)
				tobj = Reflection.WalkPropertyValue(target, prop, errorMessages);
				if (tobj == null) {
					tobj = iq.AddTranslation(value, language, "ED", 0, null, 0, false);
					//editing an (underlying) translation will change it everywhere
				} else {
					if (value == "") {
						tobj = null;
						//Allow them to de-reference a translation by blanking it
					} else {
						tobj.text(language) = value;
					}

				}
				setProperty(target, prop, tobj, null, errorMessages, true);

				if (tobj != null) {
					tobj.Update(language);
					//we must update the translation opbject itself (aswell as the row containing it)
				}

			} else if (col.InputType.code == "nullstring") {
				nullableString ntobj;
				ntobj = Reflection.WalkPropertyValue(target, prop, errorMessages);
				ntobj.value = value;
				//setProperty(target, prop, v$, Nothing)

			} else if (col.InputType.code == "nullint") {
				NullableInt ntobj;
				ntobj = Reflection.WalkPropertyValue(target, prop, errorMessages);
				//ntobj.value = CInt(value)  'setProperty(target, prop, v$, Nothing)
				int i;
				if (int.TryParse(value, i)) {
					ntobj.value = i;
				}
			} else {
				if (LCase(prop) == "password") {
					value = simpleHash(Trim(value));
					//Shuffle(md5(Trim$(value)))
				}
				setProperty(target, prop, value, null, errorMessages, true);
			}
		}

	}

	public object WalkPropertyValue(object obj, string prop, ref List<string> errorMessages)
	{

		//prop may be some 'straight' property - like Text
		//or a path to a property on the object
		//like attributes(16).Numericvalue

		ParsePath(prop, obj, null, errorMessages);
		//return ParseProperty(prop, obj)  <-previously

		return obj;

	}

	public string TypeOfProperty(object obj, string prop)
	{

		//Returns the typename of the specified property of the specified object (whose *value* might well be 'nothing' - but we need to know it's *type*)

		Type type = obj.GetType;
		PropertyInfo pinfo = type.GetProperty(prop);

		return pinfo.PropertyType.Name;
		//If Index Is Nothing Then
		//    getPropertyValue = pinfo.GetValue(obj, Nothing)
		//Else
		//    Dim ind(0) As System.Object
		//    ind(0) = Index
		//    getPropertyValue = pinfo.GetValue(obj, ind)
		//End If

	}

	//Public Function getDicEntry(dic As Object, index As Object) As Object


	//    Dim type As Type = dic.GetType
	//    Dim pinfo As PropertyInfo = type.GetProperty("Item")

	//    Dim ind(0) As System.Object
	//    ind(0) = index
	//    'Dim ind As System.Object = CType(Index, System.Object)
	//    getDicEntry = pinfo.GetValue(dic, ind)

	//End Function

	public object getPropertyValue(object obj, string prop, object Index = null)
	{

		Type type = obj.GetType;

		PropertyInfo pinfo = type.GetProperty(prop);

		if (pinfo == null)
			return null;
		if (Index == null) {
			getPropertyValue = pinfo.GetValue(obj, null);
		} else {
			System.Object[] ind = new System.Object[-1];
			ind(0) = Index;
			//Dim ind As System.Object = CType(Index, System.Object)
			getPropertyValue = pinfo.GetValue(obj, ind);
		}

	}

	public object CreateInstanceLike(object TemplateObj)
	{

		object r;
		r = Activator.CreateInstance(TemplateObj.GetType);
		return r;

	}

	//Public Function createInstance(assemblyname$, typename$) As Type


	//    Dim ty As Type
	//    ty = Activator.CreateInstance(assemblyname$, typename$).GetType()

	//    Return ty


	//End Function


	//Public Class TestPropertyInfo
	//    Public Shared Sub Main()
	//        Dim t As New TestClass()

	//        ' Get the type and PropertyInfo.
	//        Dim myType As Type = t.GetType()
	//        Dim pinfo As PropertyInfo = myType.GetProperty("Caption")

	//        ' Display the property value, using the GetValue method.
	//        Console.WriteLine(vbCrLf & "GetValue: " & pinfo.GetValue(t, Nothing))

	//        ' Use the SetValue method to change the caption.
	//        pinfo.SetValue(t, "This caption has been changed.", Nothing)

	//        ' Display the caption again.
	//        Console.WriteLine("GetValue: " & pinfo.GetValue(t, Nothing))

	//        Console.WriteLine(vbCrLf & "Press the Enter key to continue.")
	//        Console.ReadLine()
	//    End Sub
	//End Class



}




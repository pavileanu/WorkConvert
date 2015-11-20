using System.Linq;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Data.SqlClient;
using dataAccess;
using System.IO;

class Import
{
		// change this to '' for all
	string restrictImportToFamily = "";

	public Dictionary<UInt64, clsActionList> ActionListLid = new Dictionary<ulong, clsActionList>();
	Dictionary<string, string> dicAbbreviations;
	//we don't immediately turn all of these into translations (chances are many of them are un-needed)
		//"h3." '"[www3.channelcentral.net,8484]."
	public const string server = "h3.";
		//"[www.channelcentral.net,8484]."  'datastore
	public const string DSserver = "h3.";

		//Early enforcement of the unique contrain on clsQuantitys (by branch,region, path)
	public List<string> i_Quantities = new List<string>();
	public clsImportLog ImportLog = new clsImportLog();
	public clsActionList Incremental(UInt64 lid, List<System.Collections.Generic.KeyValuePair<int, bool>> submitlist)
	{
		//Pass in a list of SKU's to add or update...
		foreach ( ac in ActionListLid(lid).ToList()) {
			if (submitlist(ac.ID).Value)
				ac.Authorized = true;
		}
		return Incremental(lid, ActionListLid(lid).ToList().Where(al => al.Authorized).Select(al => al.SKU).Distinct().ToList());

	}


	public object checkfamilies()
	{

		int haves;
		int havenots;
		foreach ( b in iq.Branches.Values) {
			if (b.Product != null) {
				if (b.Product.isSystem & !b.Product.isOption) {
					if (b.Parent.Product != null) {
						if (b.Parent.Product.i_Attributes_Code.ContainsKey("FamMajor")) {
							haves += 1;
						} else {
							havenots += 1;
						}
					}
				}
			}
		}
		//If Me.Product IsNot Nothing AndAlso Me.Product.i_Attributes_Code.ContainsKey("FamMajor") Then
		// If Me.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English).ToLower = famname.ToLower Then

		Beep();

	}




	public void sweepFios()
	{
		//options that are FIOs in the (sub) family should appear on the FIOs tab
		//thier altSKU's should appear under Options
		//The FIO should have the autoadd
		//check for missing options (some L21 parts have never been imported)


		object sql;
		StreamWriter sw = new StreamWriter("c:\\temp\\fioinfo.txt");

		int made = 0;
		SqlClient.SqlConnection con;
		SqlClient.SqlDataReader rdr;

		con = da.OpenDatabase;

		Dictionary<string, int> dicCPUqmax = new Dictionary<string, int>();
		sql = "select sysfamily,qtymax from h3.iq.products.optionlimits where opttype='cpu'";
		rdr = da.DBExecuteReader(con, sql);
		while (rdr.Read) {
			dicCPUqmax.Add(rdr("sysfamily"), rdr("qtymax"));
		}
		rdr.Close();

		sql = "select optsku,altsku,opttype,DescriptionGen,fio from h3.iq.products.options where altsku is not null and opttype='cpu'";

		rdr = da.DBExecuteReader(con, sql);


		while (rdr.Read) {
			//the optskus are the 'normal' B21 parts and the altskus are the FIO versions

			string optsku = rdr.Item("optsku");
			string altsku = rdr.Item("altsku");

			clsProduct optProduct;
			if (iq.i_SKU.ContainsKey(optsku)) {
				optProduct = iq.i_SKU(optsku);
				if (iq.i_SKU.ContainsKey(altsku)) {
					clsProduct altproduct = iq.i_SKU(altsku);

					if (altproduct.FirstAttributeEnglishText("altsku") == "") {
						sw.WriteLine("The alternate for " + optsku + "(" + altsku + ") does not have a reciprocal altsku");
					//         Dim raltsku As New clsProductAttribute(altproduct, iq.i_attribute_code("altSKU"), 0, iq.i_unit_code("txt"), iq.AddTranslation(optsku, English, "alstskus", 0, Nothing, 0, False))
					// Beep()
					//this product B21 doesnt have its (valid) altsku populated

					} else if (altproduct.FirstAttributeEnglishText("altsku") != optsku) {
						if (optProduct.ProductType.Code == "cpu") {
							sw.WriteLine("The alternate for " + optsku + "(" + altsku + ") has the WRONG reciprocal altsku " + altproduct.FirstAttributeEnglishText("altsku"));
						}

					} else {
						//ok This L21 already has the B21 as an alt

					}
				} else {
					//the altsku (L21) was never imported (does not exist in iquote2)
					sw.WriteLine("The alternate for " + optsku + "(" + altsku + ") is not in iQuote2 - this is normal ");

					//Dim altproduct As clsProduct = optProduct.clone(altsku)

					//If rdr.Item("fio") = 1 Then
					//    Dim isfio = New clsProductAttribute(altproduct, iq.i_attribute_code("focus"), 1, iq.i_unit_code("txt"), iq.AddTranslation("FIO", English, "Foci", 0, Nothing, 0, False))

					//    Dim raltsku As New clsProductAttribute(altproduct, iq.i_attribute_code("altSKU"), 0, iq.i_unit_code("txt"), iq.AddTranslation(optsku, English, "alstskus", 0, Nothing, 0, False))
					//    Dim raltDesc As New clsProductAttribute(altproduct, iq.i_attribute_code("desc"), 0, iq.i_unit_code("txt"), optProduct.i_Attributes_Code("desc")(0).Translation)
					//    made += 1

					//End If
				}
			} else {
				sw.WriteLine("option " + optsku + " is NOT IN IQUOTE2 (ancient history ?)");
			}

		}
		rdr.Close();
		con.Close();
		sw.Close();

		List<string> errormessages = new List<string>();

		Dictionary<string, clsBranch> systems = new Dictionary<string, clsBranch>();
		systems = iq.RootBranch.findSystemBranches("tree.1");

		Dictionary<clsProduct, string> currentOptions = new Dictionary<clsProduct, string>();
		clsBranch currentalloptionsbranch = null;

		int qm;
		int qd;
		int sm;
		int sd;

		clsSlotType genericCPU = iq.i_slotType_Code("cpu")("gen_cpu");

		HashSet<clsSlot> slotstodel = new HashSet<clsSlot>();

		int done = 0;

		foreach (string systemPath in systems.Keys) {
			done += 1;

			clsBranch systemBranch = systems(systemPath);
			clsProduct system = systems(systemPath).Product;



			string sysfam = system.FirstAttributeEnglishText("famminor");


			foreach ( cb in systemBranch.childBranches.Values) {
				if (sysfam != "") {
					if (cb.Translation.text(English).ToLower.EndsWith("chassis")) {
						foreach ( s in cb.slots.Values.ToList) {
							//make sure we have 1 cpu slot oin
							if (s.Type.MajorCode.ToLower == "cpu") {
								if (s.Type.MinorCode.ToLower != "gen_cpu") {
									if (cb.hasSlot(genericCPU)) {
										s.deleted = true;
										s.update(errormessages);
									} else {
										cb.i_Slots.Remove(s.compoundKey);
										s.Type = genericCPU;
										//some non generic cpu
										s.CurrentCompoundKey = s.compoundKey;
										//remake the compound key (so 'update' can remove the 'right old' one)
										cb.i_Slots.Add(s.compoundKey, s);
										s.update(errormessages);
									}
								} else {
									//More than one CPU slot
									if (s.numSlots > 1) {
										//look  over the slots on the chassis branch
										foreach ( cs in cb.slots.Values.ToList) {
											if (cs.Type.MajorCode.ToLower == "mem") {
												//Memory slot on the chassis (shouldn't be here!)

												clsTranslation cputl = systemBranch.Product.i_Attributes_Code("cpuSKU")(0).Translation;
												object cpusku = cputl.text(English);
												object CPUpth = "";
												clsBranch cpubranch = systemBranch.findChildBySKU2(systemPath, cpusku, CPUpth);
												if (cpubranch == null) {
													System.Diagnostics.Debugger.Break();
												} else {
													bool cpuHasMemory = false;
													//cpu memory slots
													foreach ( cms in cpubranch.slots.Values.ToList) {

														if (cms.Type.MajorCode == "MEM" & (cms.path == "" | cms.path == CPUpth)) {
															if (!slotstodel.Contains(cs))
																slotstodel.Add(cs);
															//cs.delete(errormessages) 'we already have memory in force - just delete what was on the chassis branch
															cpuHasMemory = true;
														}
													}

													if (!cpuHasMemory) {
														//*copy* the memory off the chassis branch onto the cpu (With a specific path)
														clsSlot cpuMem = new clsSlot(cs.Type, cpubranch, CPUpth, cs.numSlots, null, new NullableInt(), 0, 0);
														if (!slotstodel.Contains(cs))
															slotstodel.Add(cs);
													}
												}
											}
										}
									}
								}

								if (!s.deleted) {
									if (dicCPUqmax.ContainsKey(sysfam)) {
										if (s.numSlots != dicCPUqmax(sysfam)) {
											s.numSlots = dicCPUqmax(sysfam);
											s.update(errormessages);
										}
									}
								}
								//Else
								//                                s.Type = iq.i_slotType_Code("cpu")("gen_cpu")
								//break
								//non generic cpu 
								// Beep()
								//End If
							}
						}
					}
				} else {
					object l = PathName(systemPath);
					// Beep()
				}

				if (cb.Translation.text(English).ToLower == "all options") {
					if (!object.ReferenceEquals(cb, currentalloptionsbranch)) {
						currentOptions.Clear();
						cb.optionsBelow(cb.ID.ToString, currentOptions);
						currentalloptionsbranch = cb;
					}

					foreach ( gb in cb.childBranches.Values) {
						if (gb.Translation.text(English).ToLower == "fios") {
							//option branch
							foreach ( fiob in gb.childBranches.Values) {

								string altsku = fiob.Product.FirstAttributeEnglishText("altsku");

								if (fiob.Product.ProductType.Code.ToLower == "cpu") {
									//If Not ob.Product.isFIO Then

									if (!fiob.Product.SKU.StartsWith("###")) {

										if (currentOptions.ContainsKey(fiob.Product)) {
											object optionpath = systemPath + "." + currentOptions(fiob.Product);
											clsBranch optionBranch = iq.Branches(Split(optionpath, ".").Last);

											//                                    Beep()
											//this FIO is an option (and that option should have the autoadd,memory slots and altSKU)
											//If ob.Quantities.Count > 0 Then

											string fiopath = systemPath + "." + cb.ID + "." + gb.ID + "." + fiob.ID;
											object l = PathName(fiopath);
											object eaoa = PathName(systemPath + "." + currentOptions(fiob.Product));

											//move the quantities (autoadds) - onto the option (off the FIO)
											foreach ( q in fiob.Quantities.Values.ToList) {
												if (q.Path == fiopath | q.Path == "") {
													if (!optionBranch.hasQuantity(optionpath, q.Region)) {
														q.Path = optionpath;
														q.Branch = optionBranch;
														fiob.Quantities.Remove(q.ID);
														//optionBranch.Quantities.Add(q.ID, q) -not needed as the q.update does it
														qm += 1;
														q.update(errormessages);
													} else {
														q.deleted = true;
														qd += 1;
														q.update(errormessages);
													}
												}
											}

											foreach ( s in fiob.slots.Values.ToList) {
												//If s.path = "" Then Stop
												if (s.path == fiopath | s.path == "") {
													//.ContainsKey(s.compoundKey) Then
													if (!optionBranch.HasMajorSlot(s.Type.MajorCode)) {
														s.path = optionpath;
														s.Branch = optionBranch;

														fiob.slots.Remove(s.ID);
														optionBranch.slots.Add(s.ID, s);
														// optionBranch.i_Slots.Add(s.compoundKey, s) done by ste s.update
														sm += 1;
														s.update(errormessages);
													} else {
														s.deleted = true;
														s.update(errormessages);
													}
													sd += 1;

												}
											}

										//see if an option has this FIO as its altSKU (Ie. a b21 has this l21)


										//    'we have the wrong 'real' (B21) cpu listed as an FIO
										//    ob.Product = iq.Products(altsku)
										//    ob.Update(errormessages)

										//End If
										} else {
											//it's NOT an option for the system (Probably an L21) or ### - and the autoadd and slots need to stay here
											//this system should have no processor !
										}
									}


									if (altsku != "") {
										if (iq.i_SKU.ContainsKey(altsku)) {
											if (!currentOptions.ContainsKey(iq.i_SKU(altsku))) {
											//Beep()
											} else {
												// Beep()
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		foreach ( s in slotstodel) {
			s.delete(errormessages);
		}

		Debug.Print(qm, qd, sm, sd);

	}

	private object MultiCPUs()
	{






	}




	public object fixFilterDefaults()
	{

		//Key>Text^Group
		Dictionary<int, string> dt = new Dictionary<int, string>();
		//Deleted translations
		object sql = "SELECT [key],text,[group] FROM Translation WHERE deleted=1 AND fk_language_id=1";
		SqlClient.SqlConnection con = da.OpenDatabase;
		SqlClient.SqlDataReader r;
		r = da.DBExecuteReader(con, sql);

		int duped = 0;
		while (r.Read) {
			int k = r.Item("key");
			if (!dt.ContainsKey(k)) {
				dt.Add(k, r.Item("text") + "^" + r.Item("group"));
			} else {
				duped += 1;
			}

		}
		con.Close();

		List<string> em = new List<string>();

		List<string> cantfix = new List<string>();
		foreach ( f in iq.Fields.Values.ToList) {
			if (f.DefaultFilterValues != "") {
				string[] bits = Split(f.DefaultFilterValues, "|");
				if (bits.Count == 2) {
					int okey = bits(1);
					//old key
					if (dt.ContainsKey(okey)) {
						string[] kb = Split(dt(okey), "^");
						//Text^group (of old translations)
						object nkey = iq.EnglishIndex(kb(0), kb(1)).Key;
						bits(1) = nkey;
						f.DefaultFilterValues = bits(0) + "^" + bits(1);
						f.update(em);
					} else {
						cantfix.Add(f.ID + "^" + okey);
					}

				}
			}
		}

	}

	public object fixTranslations()
	{

		//get the occurance of this phrase with the most translations first
		object sql = "select count(*) as c,[key] as k,[order],[group] from translation group by [key],[order],[group] order by c desc";

		SqlClient.SqlConnection con = da.OpenDatabase;
		SqlClient.SqlDataReader r;

		//the iq.translations is already indexed by KEY - NOT ID
		r = da.DBExecuteReader(con, sql);

		Dictionary<string, int> cks = new Dictionary<string, int>();
		//Compound key > key  'stores the best 'unique' versiona
		Dictionary<int, int> mappings = new Dictionary<int, int>();
		//other keys >master key

		Dictionary<string, int> counts = new Dictionary<string, int>();
		List<string> errormessages = new List<string>();

		while (r.Read) {
			int k = r.Item("k");
			clsTranslation t = iq.Translations(k);

			if (cks.ContainsKey(t.compoundkey(English))) {
				//this is a dupe = we need to add a mapping..
				//of other key > best key
				if (!mappings.ContainsKey(r.Item("k"))) {
					//DON'T map rows to themselves !
					if (cks(t.compoundkey(English)) != r.Item("k")) {
						mappings.Add(r.Item("k"), cks(t.compoundkey(English)));
					}
				}
			} else {
				//compound key will be text^group^language?
				cks.Add(t.compoundkey(English), r.Item("K"));
				// this will be our master
			}

		}
		r.Close();

		counts.Add("SlotTypes", 0);

		foreach ( st in iq.SlotTypes.Values) {
			 // ERROR: Not supported in C#: WithStatement

			if (st.TranslationShort != null) {
				 // ERROR: Not supported in C#: WithStatement

			}
		}

		counts.Add("Promos", 0);
		foreach ( p in iq.Promos.Values) {
			 // ERROR: Not supported in C#: WithStatement

		}

		//Some transaltions are getting mapped to themselves !- yuck
		counts.Add("Attributes", 0);

		foreach ( a in iq.Attributes.Values) {
				//Doubly mapped
			 // ERROR: Not supported in C#: WithStatement

		}


		counts.Add("ROKAttributes", 0);

		foreach ( la in iq.ROKAttributes.Values) {
			foreach ( Ra in la) {
					//Doubly mapped
				 // ERROR: Not supported in C#: WithStatement

			}
		}


		//'Attributes
		//counts.Add("Attributes", 0)
		//For Each a In iq.Attributes.Values
		//    With a.Translation
		//        If mappings.ContainsKey(.Key) Then
		//            a.Translation = iq.Translations(mappings(.Key))
		//            counts("Attributes") += 1
		//            a.update(errormessages)
		//        End If
		//    End With
		//Next

		//fields
		counts.Add("Fields", 0);
		foreach ( f in iq.Fields.Values) {
			 // ERROR: Not supported in C#: WithStatement

		}

		//Product Attributes (includes descrption & sku)
		counts.Add("ProductAttributes", 0);
		foreach ( p in iq.Products.Values) {
			foreach ( pa in p.Attributes.Values) {
				 // ERROR: Not supported in C#: WithStatement

			}
		}

		//ProductTypes
		counts.Add("ProductTypes", 0);
		foreach ( pt in iq.ProductTypes.Values) {
			 // ERROR: Not supported in C#: WithStatement

		}


		//states
		counts.Add("States", 0);
		foreach ( s in iq.States.Values) {
			 // ERROR: Not supported in C#: WithStatement

		}


		//units
		counts.Add("units", 0);
		foreach ( u in iq.Units.Values) {
			 // ERROR: Not supported in C#: WithStatement

		}

		//sector
		counts.Add("sectors", 0);
		foreach ( sctr in iq.Sectors.Values) {
			 // ERROR: Not supported in C#: WithStatement

		}

		//regions
		counts.Add("regions", 0);
		foreach ( rgn in iq.Regions.Values) {
			 // ERROR: Not supported in C#: WithStatement

		}



		//validation messages
		counts.Add("vm", 0);
		foreach ( vl in iq.ProductValidationsAssignment.Values) {

			foreach ( v in vl) {

				 // ERROR: Not supported in C#: WithStatement

			}
		}


		//branches
		counts.Add("branches", 0);
		counts.Add("slots", 0);


		foreach ( branch in iq.Branches.Values) {

			bool ub = false;
			 // ERROR: Not supported in C#: WithStatement


			 // ERROR: Not supported in C#: WithStatement


			 // ERROR: Not supported in C#: WithStatement


			if (ub)
				branch.Update(errormessages);


			foreach ( s in branch.slots.Values) {
				if (s.notes != null) {
					 // ERROR: Not supported in C#: WithStatement

				}
			}
			//quantities have no text

		}

		con.Close();

		//map every FK through the mappings table

		//we can now delete all the translations in the keys of the mappings

		List<string> todel = new List<string>();
		foreach ( k in mappings.Keys) {
			todel.Add((string)k);
		}

		if (todel.Count) {
			int toskip = 0;
			int chunk = 1000;
			do {
				object ll = from j in todel.Skip(toskip).Take(chunk);
				if (!ll.Any)
					break; // TODO: might not be correct. Was : Exit Do

				sql = "update translation set deleted=1 WHERE [key] IN(" + Join(ll.ToArray, ",") + ")";

				LongSQL(sql);
				toskip += 1000;
			} while (true);

		}


	}


	//Manufactuer 'group' ?
	public void Manufacturer()
	{

		//get a definitive list of skus>manufactirer from iq1
		//Dim sql$ = "SELECT h.UPCNum, pl.PL, bu.IQBU, "
		//sql$ &= "CASE WHEN bu.IQBU IN ('IPG','PSG') THEN 'HPI' ELSE 'HPE' END as NewMfrCode,h.ccdescription "
		//sql$ &= "FROM h3.Channelcentral.products.Hierarchy h "
		//sql$ &= "INNER JOIN h3.ChannelCentral.products.Codes_PL pl ON h.PL = pl.PL "
		//sql$ &= "INNER JOIN h3.Channelcentral.products.TranslateBU bu ON bu.BUID2 = pl.BUCode "
		//sql$ &= "WHERE bu.IQBU IS NOT NULL "
		//sql$ &= "AND bu.IQBU <> 'SER' -- these are not iQuote products"


		string sql = "SELECT      h.UPCNum, pl.PL, bu.IQBU, bu.Mfr_Code AS NewMfrCode,h.ccdescription ,syssn,";
		sql += "            ISNULL(s.ActiveFromDate, o.ActiveFromDate) ActiveFromDate, ISNULL(s.ActiveToDate, o.ActiveToDate) ActiveToDate, ";
		sql += "            ISNULL(s.EOL, o.EOL) EOL, ISNULL(s.Active, o.Active) Active,s.aaonly ";
		sql += "FROM h3.Channelcentral.products.Hierarchy h ";
		sql += "      LEFT JOIN h3.iQ.products.Systems s ON s.ModelSKU = h.UPCNum ";
		sql += "      LEFT JOIN h3.iQ.products.Options o ON o.OptSKU = h.UPCNum ";
		sql += "INNER JOIN h3.ChannelCentral.products.Codes_PL pl ON h.PL = pl.PL ";
		sql += "INNER JOIN h3.Channelcentral.products.TranslateBU bu ON bu.BUID2 = pl.BUCode AND ISNULL(bu.IQBU,'SER')<>'SER' ";
		//     sql &= "AND ISNULL(s.Active,o.Active)=1"


		SqlClient.SqlConnection con = da.OpenDatabase;
		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql);

		//Dim dicMfr As Dictionary(Of String, String) = New Dictionary(Of String, String)

		//Dim tls As Dictionary(Of String, clsTranslation) = New Dictionary(Of String, clsTranslation)
		//tls.Add("HPI", iq.AddTranslation("HPI", English, "Division", 1, Nothing, 0, False))
		//tls.Add("HPE", iq.AddTranslation("HPE", English, "Division", 2, Nothing, 0, False))

		HashSet<string> niiq2 = new HashSet<string>();
		//Not in iq2 list

		List<string> em = new List<string>();

		int updated = 0;
		int skipped = 0;

		updated += 1;

		HashSet<string> iiq1 = new HashSet<string>();

		int ad;

		int ca;
		while (rdr.Read) {
			string sku = rdr.Item("upcnum");
			if (sku == "589256-B21")
				System.Diagnostics.Debugger.Break();
			iiq1.Add(rdr.Item("upcnum"));
			if (iq.i_SKU.ContainsKey(sku)) {
				clsProduct product = iq.i_SKU(sku);
				if (product.SKU != sku & product.SKU != "")
					System.Diagnostics.Debugger.Break();

				bool update = false;
				if (product.mfrCode != rdr.Item("newmfrcode")){product.mfrCode = rdr.Item("newmfrcode");update = true;}
				if (product.buCode != rdr.Item("iqbu")){product.buCode = rdr.Item("iqbu");update = true;}
				if (product.plCode != rdr.Item("pl")){product.plCode = rdr.Item("pl");update = true;}

				//Publish = NOT aaONLY
				//AndAlso rdr.Item("aaonly") <> 0 Then
				if (!object.ReferenceEquals(rdr.Item("aaonly"), DBNull.Value)) {

					if (product.Publish & rdr.Item("aaonly") == 1) {
						product.Publish = 0;
						update = true;
					} else if (product.Publish == false & rdr.Item("aaonly") == 0) {
						product.Publish = true;
						update = true;
					}
				}

				if (!object.ReferenceEquals(rdr.Item("active"), DBNull.Value)) {
					bool active = rdr.Item("active");
					if (product.Active != active) {
						ad += 1;
						product.Active = rdr.Item("active");
						update = true;
					}
				}


				if (!object.ReferenceEquals(rdr.Item("eol"), DBNull.Value)) {
					bool eol = rdr.Item("eol");
					if (product.EOL != eol) {
						product.EOL = eol;
						update = true;
					}

				} else {
					if (product.Active != 0){product.Active = 0;update = true;}
				}

				if (!object.ReferenceEquals(rdr.Item("activeFromDate"), DBNull.Value)) {
					if (product.activeFrom != rdr.Item("activeFromDate")){product.activeFrom = rdr.Item("activeFromDate");update = true;}
				}
				if (!object.ReferenceEquals(rdr.Item("activeToDate"), DBNull.Value)) {
					if (product.activeTo != rdr.Item("activeToDate")) {
						product.activeTo = rdr.Item("activeToDate");
						update = true;
					}
				}

				if (update) {
					product.update(em);
					updated += 1;
				} else {
					skipped += 1;
				}
			} else {
				//only output 'missing' systems
				if (!object.ReferenceEquals(rdr.Item("syssn"), DBNull.Value)) {
					niiq2.Add(rdr.Item("upcnum"));
					//& rdr.Item("ccdescription"))
				}
			}

			//        dicMfr.Add(sku, rdr.Item("newMfrCode") & "|" 
			//& rdr.Item("iqbu") & "|" & rdr.Item("pl"))
		}
		rdr.Close();

		HashSet<string> niiq1 = new HashSet<string>();
		foreach ( p in iq.Products.Values) {
			//And p.mfrCode = "" Then
			if (p.hasSKU) {
				if (!iiq1.Contains(p.SKU)) {
					niiq1.Add(p.SKU);
					//If p.Active = True Then
					//    p.Active = False
					//    p.update(em)
					//Else
					//    Beep()
					//End If
				}
			}
		}


		sql = "update product set active = 0 where sku in ('" + Join(niiq1.ToArray, "','") + "')";
		LongSQL(sql);


		//Dim updates As Integer = 0
		//Dim SKUless As Integer = 0
		//Dim already As Integer = 0
		//Dim missing As New List(Of String)

		//For Each Product In iq.Products 'iq.i_SKU
		//    'sku>product
		//    If Product.i_attributes_code Then
		//        Dim sku As String = kvp.Key
		//        Dim product As clsProduct = kvp.Value

		//        'there are few products (Chassis and 'Family' products) that don't have skus
		//        If dicMfr.ContainsKey(sku) Then
		//            Dim b As String() = Split(dicMfr(sku), "|")
		//            Dim update As Boolean = False
		//            If product.SKU <> sku Then product.SKU = sku : update = True
		//            If product.mfrCode <> b(0) Then product.mfrCode = b(0) : update = True
		//            If product.buCode <> b(1) Then product.buCode = b(1) : update = True
		//            If product.plCode <> b(2) Then product.plCode = b(2) : update = True
		//            If update Then
		//                product.update(em) : updates += 1
		//            Else
		//                already += 1
		//            End If
		//        Else
		//            'skuless product . . set them all to HPE for now
		//            missing.Add(sku)
		//            SKUless += 1
		//        End If
		//    End If
		//Next

		StreamWriter sw = new StreamWriter("c:\\temp\\niiq2.txt");
		foreach ( s in niiq2) {
			sw.Write(s + ",");
		}
		sw.Close();

		sw = new StreamWriter("c:\\temp\\niiq1.txt");
		foreach ( s in niiq1) {
			sw.WriteLine(s);
		}
		sw.Close();

		con.Close();

	}


	public int fixCurrencies(string HOSTID, clsCurrency currency, ref List<string> errs)
	{

		fixCurrencies = 0;
		List<string> errors = new List<string>();

		int okalready = 0;
		if (iq.i_channel_code.ContainsKey(HOSTID)) {
			clsChannel sc = iq.i_channel_code(HOSTID);
			sc.DefaultCurrency = currency;
			sc.Update(errors);

			foreach ( ac in iq.Accounts.Values) {
				if (object.ReferenceEquals(ac.SellerChannel, sc)) {
					if (!object.ReferenceEquals(ac.Currency, currency)) {
						ac.Currency = sc.DefaultCurrency;
						ac.update(errors);
						fixCurrencies += 1;
					} else {
						okalready += 1;
					}
				}
			}
			errs.Add("Updated " + fixCurrencies + "," + okalready + " were already OK -  success");
		} else {
			errs.Add("No such host");
		}

	}


	public int Tokens(List<string> errormessages)
	{

		SqlClient.SqlConnection con = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;");
		SqlClient.SqlDataReader rdr;

		string query = "SELECT PropName,valuechar from h3.iq.admin.properties";

		rdr = da.DBExecuteReader(con, query);


		int done;

		while (rdr.Read) {
			string[] bits = Split(rdr.Item("Propname"), "_");
			if (bits(0) == "TOKEN") {
				if (iq.i_channel_code.ContainsKey(bits(1))) {
					clsChannel c = iq.i_channel_code(bits(1));
					if (!c.Code.StartsWith("DSYUS") & !c.Code.StartsWith("DYSCA")) {
						c.WebToken = rdr.Item("valuechar");
						c.Update(errormessages);
						done += 1;
					}
				}
			}
		}

		rdr.Close();
		con.Close();

		return done;

	}

	public clsActionList Incremental(UInt64 lid, List<string> skus)
	{
		//Pass in a list of SKU's to add or update...


		if (skus.Contains("ALL")) {
			skus.Clear();
			string sql2 = "SELECT  h.UPCNum, pl.PL, bu.IQBU, bu.Mfr_Code AS NewMfrCode," + "            s.ActiveFromDate, s.ActiveToDate," + "s.EOL, s.Active, s.aaonly " + "FROM h3.Channelcentral.products.Hierarchy h " + "LEFT JOIN h3.iQ.products.Systems s ON s.ModelSKU = h.UPCNum " + "INNER JOIN h3.ChannelCentral.products.Codes_PL pl ON h.PL = pl.PL " + "INNER JOIN h3.Channelcentral.products.TranslateBU bu ON bu.BUID2 = pl.BUCode " + "AND s.Active=1";

			SqlClient.SqlConnection con = da.OpenDatabase;
			SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql2);
			while (rdr.Read) {
				skus.Add(rdr("upcnum"));
			}
			rdr.Close();
			con.Close();
		}

		clsActionList ActionList;
		if (!ActionListLid.ContainsKey(lid)) {
			ActionList = new clsActionList();
			ActionListLid(lid) = ActionList;
		} else {
			ActionList = ActionListLid(lid);
		}

		//Try
		ImportLog.Add(DateTime.Now, "Beginning Import");

		List<string> errorMessages = new List<string>();

		//Collect system information
		//fetch all the skus we need to update (thae EXIST in IQ2
		object updateSkus = iq.i_SKU.Where(s => skus.Contains(s.Key)).Select(s => s.Key).ToList();
		//What do we have?
		//The rest we're going to need to ADD
		object addSkus = skus.Where(s => !iq.i_SKU.ContainsKey(s)).ToList();
		//What do we need?

		ImportLog.Add(DateTime.Now, string.Format("Found: {0} SKU's to update and {1} SKU's to add", updateSkus.Count, addSkus.Count));

		//Create lDic...
		//Find the alloptions branch? - This WAS for SBSO - (but doesnt exist yet)
		string ck = "";
		Dictionary<string, clsBranch> lDic = new Dictionary<string, clsBranch>();
		foreach ( branch in iq.RootBranch.childBranches) {
			if (branch.Value.Translation.text(English).ToLower == "accessories and services") {
				branch.Value.BuildPathDic("", lDic, false);
				break; // TODO: might not be correct. Was : Exit For
			}
		}

		LoadAbbreviations(da.OpenDatabase());

		Dictionary<string, clsSlotType> dicSlotTypes;
		dicSlotTypes = Import.slotTypes(da.OpenDatabase(), null, true);
		//20 secs
		Dictionary<clsProduct, List<clsRegion>> dicOptLocalization = new Dictionary<clsProduct, List<clsRegion>>();


		bool AllowDelete = true;

		//Add any missing families...
		ImportLog.Add(DateTime.Now, string.Format("Checking if new families are needed"));
		Dictionary<string, string> famlocs = FamiliesInc(da.OpenDatabase(), string.Join(",", skus.Select(ass => da.SqlEncode(ass))));


		//Add any systems, branches and products with attributes
		ImportLog.Add(DateTime.Now, string.Format("checking for systems"));
		SystemsInc(da.OpenDatabase(), string.Join(",", skus.Select(ass => da.SqlEncode(ass))), errorMessages, ActionList, AllowDelete, famlocs);

		//Options for systems

		List<string> AllSystemOptions = new List<string>();
		List<string> systemOptionsToAdd = new List<string>();

		object Sql = "SELECT DISTINCT familycode,sysfamilyname " + "FROM h3.iq.products.systems " + "INNER JOIN h3.iq.products.sysfamilydefinitions sd ON sd.sysfamily=familycode " + "WHERE modelsku IN (" + string.Join(",", skus.Select(ass => da.SqlEncode(ass))) + ")";

		object rdr3 = da.DBExecuteReader(da.OpenDatabase(), Sql);
		object famList = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
		while (rdr3.Read) {
			if (!famList.Contains(rdr3("familycode")))
				famList.Add(UCase(rdr3("familycode")));
			if (!famList.Contains(rdr3("sysfamilyname")))
				famList.Add(UCase(rdr3("sysfamilyname")));
		}
		rdr3.Close();

		Sql = "SELECT optsku,sysfamily FROM h3.iq.products.options WHERE sysfamily is not null and opttype<>'wty'";
		rdr3 = da.DBExecuteReader(da.OpenDatabase(), Sql);

		while (rdr3.Read) {
			object sp = Split(rdr3("sysfamily"), ",");

			foreach ( s in sp) {
				if (!string.IsNullOrEmpty(s) && famList.Contains(s)) {
					string optsku = rdr3("optsku");
					if (!iq.i_SKU.ContainsKey(optsku)) {
						if (!systemOptionsToAdd.Contains(optsku)) {
							systemOptionsToAdd.Add(optsku);
						}
					}

					if (!AllSystemOptions.Contains(optsku)) {
						AllSystemOptions.Add(optsku);
					}
				}
			}
		}
		rdr3.Close();

		//Now add any FIO's
		Sql = "SELECT psu,cpu,ram,WLAN,WWAN,FAN,PriStor,SecStor,RAID,NIC,ICEincluded,options,software,ILOLicense from h3.iq.products.systems where modelsku IN (" + string.Join(",", skus.Select(ass => da.SqlEncode(ass))) + ")";
		rdr3 = da.DBExecuteReader(da.OpenDatabase(), Sql);
		while (rdr3.Read) {
			for (i = 0; i <= rdr3.FieldCount - 1; i++) {
				if (!IsDBNull(rdr3(i))) {
					object sp = Split(rdr3(i), ",");
					foreach ( s in sp) {
						if (!AllSystemOptions.Contains(s) & s.Contains("###") == false) {
							if (!AllSystemOptions.Contains(s))
								AllSystemOptions.Add(s);
						}

						if (!iq.i_SKU.ContainsKey(s) & s.Contains("###") == false) {
							if (!systemOptionsToAdd.Contains(s))
								systemOptionsToAdd.Add(s);
						}
					}
				}
			}
		}
		rdr3.Close();

		List<string> optSkus = new List<string>();

		ImportLog.Add(DateTime.Now, string.Format("Adding System Options"));

		if (systemOptionsToAdd.Any) {
			//this is really only maintaining the 'accessories and services' catalogue - see buildTreeInc for system options
			optSkus.AddRange(optionsIncremental(da.OpenDatabase(), systemOptionsToAdd, iq.i_unit_code, lDic, dicOptLocalization, ActionList, AllowDelete));
		}


		//Add any options, branches and products with attributes
		//ImportLog.Add(DateTime.Now, String.Format("Adding Options"))
		//optSkus.AddRange(optionsIncremental(da.OpenDatabase(), skus, iq.i_unit_code, lDic, dicOptLocalization, ActionList, AllowDelete))

		//'NOTE - AT THIS POINT WE HAVE SYSTEMS AND OPTIONS BUT NO RELATIONSHIPS
		ImportLog.Add(DateTime.Now, string.Format("Building Tree"));
		// If optSkus.Count > 0 Then 


		if (AllSystemOptions.Contains("AN975A")) {
			// Beep()

		}

		StreamWriter sw = new StreamWriter("c:\\temp\\allSystemOptions.txt");
		foreach ( l in AllSystemOptions) {
			sw.WriteLine(l);
		}
		sw.Close();

		Buildtreeinc(da.OpenDatabase(), AllSystemOptions, dicOptLocalization, dicSlotTypes, ActionList, AllowDelete, skus);

		//option compatibility is defined gasint the broad falimi

		//Add options to relevant systems, scan the tree to find where the options go

		//AddAndCheckOptionsForSystem()

		//AddOptionToSystemsAndCheck()
		ImportLog.Add(DateTime.Now, string.Format("Complete"));

		return ActionList;
		//Create preinstalled quantity information FIO and autoadd's by region

		//Add slot information

		//Populate attributes
		//        Catch ex As Exception
		// ImportLog.Add(DateTime.Now, "Exception: " & ex.Message & "-" & If(ex.InnerException IsNot Nothing, ex.InnerException.Message, ""))
		// Return ActionList
		// End Try

	}

	public void SystemsInc(SqlClient.SqlConnection con, string skus, ref List<string> errormessages, clsActionList ActionList, bool AllowDelete, Dictionary<string, string> famlocs)
	{
		clsProductAttribute U;
		clsAttribute aa;
		if (!iq.i_attribute_code.ContainsKey("U")) {
			aa = new clsAttribute("U", iq.AddTranslation("U", English, "U", 0, null, 0, false), 0);
		}

		object pristorAtt;
		if (!iq.i_attribute_code.ContainsKey("PriStor")) {
			pristorAtt = new clsAttribute("PriStor", iq.AddTranslation("Primary Storage (import only)", English, "UI", 0, null, 0, false), 0);
		} else {
			pristorAtt = iq.i_attribute_code("PriStor");
		}

		object sctl = iq.AddTranslation("Supply Chain", English, "cats", 0, null, 0, false);

		//returns a dictionary of system branches by ModelSKU

		object nextPaID = -1, nextQuantityId = -1;
		clsProduct Product;
		clsBranch sysBranch;
		//used to create systems (which go into the dictionaries)

		DataTable AttribWriteCache = new DataTable();
		AttribWriteCache = da.MakeWriteCacheFor(con, "ProductAttribute", nextPaID, true);

		DataTable QtyWritecache = da.MakeWriteCacheFor(con, "Quantity", nextQuantityId, true);
		//Disabling bulkwrite 
		//QtyWritecache = Nothing
		//AttribWriteCache = Nothing
		//Dim dicFormFactors As Dictionary(Of String, clsTranslation) = FormFactors(con)

		SqlClient.SqlDataReader rdr;
		object sql;

		//small dictionary of supply chains to their translations keys - used to look up 
		//the supply chain branches (under the family branches) 

		//supply chains are obsoleted (before they ever really saw the light of day!)
		Dictionary<string, clsTranslation> dicChains = new Dictionary<string, clsTranslation>(StringComparer.CurrentCultureIgnoreCase);
		dicChains = new Dictionary<string, clsTranslation>();

		//hard coded - until someone can tell me where to find the full supply chain names/list
		dicChains.Add("A", iq.AddTranslation("Regular models", English, "SC", 10, null, 0, false));
		dicChains.Add("TV", iq.AddTranslation("Top value", English, "SC", 20, null, 0, false));
		dicChains.Add("SB", iq.AddTranslation("Smart buy", English, "SC", 30, null, 0, false));
		dicChains.Add("R", iq.AddTranslation("HP Renew", English, "SC", 30, null, 0, false));
		dicChains.Add("PR", iq.AddTranslation("Promotional", English, "SC", 30, null, 0, false));
		dicChains.Add("GO", iq.AddTranslation("Golden Offers", English, "SC", 30, null, 0, false));


		//the focus attributes are matched against the code (but theyr'e attributes - so they need trasnlations (until and unless we invent a text type for attributes!)
		Dictionary<string, clsTranslation> dicSC = new Dictionary<string, clsTranslation>(StringComparer.CurrentCultureIgnoreCase);
		dicSC = new Dictionary<string, clsTranslation>();

		dicSC.Add("A", iq.AddTranslation("A", English, "SCC", 10, null, 0, false));
		dicSC.Add("TV", iq.AddTranslation("TV", English, "SCC", 20, null, 0, false));
		dicSC.Add("SB", iq.AddTranslation("SB", English, "SCC", 30, null, 0, false));
		dicSC.Add("R", iq.AddTranslation("R", English, "SCC", 30, null, 0, false));
		dicSC.Add("PR", iq.AddTranslation("PR", English, "SCC", 30, null, 0, false));
		dicSC.Add("GO", iq.AddTranslation("GO", English, "SCC", 30, null, 0, false));


		Dictionary<string, clsTranslation> sysTypeToPortfolio = new Dictionary<string, clsTranslation>();

		//FYI
		//HP's Corporate hierarchy goes
		//Division (ESSN..
		//  BU (business unit) ISS/PSG/HPN/SWD
		//     Exhibit  'Desktops/Notebooks

		sysTypeToPortfolio.Add("DTO", iq.AddTranslation("PSG", English, "BU", 1, null, 0, false));
		sysTypeToPortfolio.Add("HPN", iq.AddTranslation("HPN", English, "BU", 1, null, 0, false));
		sysTypeToPortfolio.Add("IPG", iq.AddTranslation("IPG", English, "BU", 1, null, 0, false));
		sysTypeToPortfolio.Add("NBK", iq.AddTranslation("PSG", English, "BU", 1, null, 0, false));
		sysTypeToPortfolio.Add("SVR", iq.AddTranslation("ISS", English, "BU", 1, null, 0, false));
		sysTypeToPortfolio.Add("SWD", iq.AddTranslation("SWD", English, "BU", 1, null, 0, false));
		sysTypeToPortfolio.Add("PSG", iq.AddTranslation("PPS", English, "BU", 1, null, 0, false));
		sysTypeToPortfolio.Add("RAK", iq.AddTranslation("ISS", English, "BU", 1, null, 0, false));

		//Create a dictionary of all the abbreviations referenced in any of these columns (of products.union_systems)
		//these are NOT the columns which contain only part numbers (RAM,discretegraphics, etc - handled in import.fios()
		//theyre the ones that may have abbreviations in

		string columns = "extras,options,software,warrantyCode,formFactor,mfrBuildCode,display,intVideo,ILOhardware,terStorTech,raidTech";

		//extras and options contain moslty abbreviations - but some part no's
		//software contains a CD list of abbreviations

		Dictionary<string, clsTranslation> optabbreviations;
		optabbreviations = Import.OptAbbreviations(con, columns);

		if (!optabbreviations.ContainsKey("TOWER")) {
			optabbreviations.Add("TOWER", iq.AddTranslation("Tower", English, "FF", 100, null, 0, false));
		}

		if (!optabbreviations.ContainsKey("BLADE")) {
			optabbreviations.Add("BLADE", iq.AddTranslation("Blade", English, "FF", 90, null, 0, false));
		}

		columns = Replace(columns, "ILOhardware", "Sys.ILOhardware");
		//the column nane is ambiguous otherise (this isn't pretty - but it's only an import)


		//Build a dictionary of all xtext for systems we're concerened with
		//(Running a query per system in import.xtext was VERY slow)
		Dictionary<string, List<string>> xtdic = new Dictionary<string, List<string>>();
		SqlClient.SqlDataReader xrdr;

		sql = "SELECT  [SKU],[SKUtext],[SysFamilyShowText],[SysFamilyHideText],[MsgType] from " + server + "[iq].[Products].[TextExt]  WHERE SKU in(" + skus + ");";

		xrdr = da.DBExecuteReader(con, sql);

		while (xrdr.Read) {
			string sku = xrdr.Item("sku");
			if (!xtdic.ContainsKey(sku)) {
				xtdic.Add(sku, new List<string>());
			}

			object mt = xrdr.Item("msgtype");
			if (object.ReferenceEquals(mt, DBNull.Value))
				mt = "NULL";
			xtdic(sku).Add(xrdr.Item("skuText") + "^" + xrdr.Item("sysFamilyShowText") + "^" + xrdr.Item("sysFamilyHideText") + "^" + mt);

		}
		xrdr.Close();





		int nextkey = clsTranslation.NextKey();
		DataTable Tlwc = new DataTable();
		Tlwc = da.MakeWriteCacheFor(con, "Translation");

		//Tlwc = Nothing
		// nextkey = 0
		sql = "SELECT h.ccdescription,familyPriStor,familySecStor,busunit,modelSKU,sysfamilyname,familycode,cpu,sfd.systype,h.ccDescription as [desc],Isnull([SupplyChainCode],'A') as [supplyChainCode],sfd.u, Activesites,sfd.instformfactor, ";
		sql += columns;
		//THIS FORMS THE BULK OF THE SPEC TABLE
		sql += ",alsoHost,extras,options,[DiscreteGraphics],[IntVideo],[InstVGA],[WLAN],[WWAN],[InstNIC]";
		sql += ",productNote,vga,energystar,weightUnboxed,activeFromDate,activeToDate,active,eol,sfd.sysfamilyimg,aaOnly,isnull(pl,'none') as pl ";
		sql += "FROM " + server + "[iq].products.union_systems sys ";
		sql += "INNER join " + server + "[iq].products.union_sysfamilydefinitions sfd ON sfd.SysFamily=sys.FamilyCode ";
		sql += "INNER join " + server + "[iq].products.hierarchyiq h ON modelSKU=h.UPCNum ";
		sql += "WHERE modelsku in (" + skus + ")";


		columns = Replace(columns, "Sys.ILOhardware", "ILOhardware");
		//put it back so we can pull out this column later
		rdr = da.DBExecuteReader(con, sql);

		clsProductAttribute FamMajor;
		clsProductAttribute FamMinor;
		clsProductAttribute FamDisp;

		clsProductAttribute cpuSKU;
		clsProductAttribute mfrSKU;
		clsProductAttribute PLcode;

		clsSector sector;
		clsTranslation sysTrans = iq.AddTranslation("systems", English, "collect", 0, Tlwc, nextkey, false);
		clsTranslation sysTransSingular = iq.AddTranslation("system", English, "collect", 0, Tlwc, nextkey, false);

		clsTranslation optTrans = iq.AddTranslation("options", English, "collect", 0, Tlwc, nextkey, false);
		clsTranslation optTransSingular = iq.AddTranslation("option", English, "collect", 0, Tlwc, nextkey, false);
		clsUnit textUnit = iq.i_unit_code("txt");

		clsAttribute att = null;

		clsTranslation tlyes = iq.AddTranslation("Yes", English, "hasFeature", 0, Tlwc, nextkey, false);

		object jj = InStr(skus, "QK765A");


		string sqlMfr = "SELECT      h.UPCNum, pl.PL, bu.IQBU, bu.Mfr_Code AS NewMfrCode,h.ccdescription ,";
		sqlMfr += "            ISNULL(s.ActiveFromDate, o.ActiveFromDate) ActiveFromDate, ISNULL(s.ActiveToDate, o.ActiveToDate) ActiveToDate, ";
		sqlMfr += "            ISNULL(s.EOL, o.EOL) EOL, ISNULL(s.Active, o.Active) Active ";
		sqlMfr += "FROM h3.Channelcentral.products.Hierarchy h ";
		sqlMfr += "      LEFT JOIN h3.iQ.products.Systems s ON s.ModelSKU = h.UPCNum ";
		sqlMfr += "      LEFT JOIN h3.iQ.products.Options o ON o.OptSKU = h.UPCNum ";
		sqlMfr += "INNER JOIN h3.ChannelCentral.products.Codes_PL pl ON h.PL = pl.PL ";
		sqlMfr += "INNER JOIN h3.Channelcentral.products.TranslateBU bu ON bu.BUID2 = pl.BUCode AND ISNULL(bu.IQBU,'SER')<>'SER' ";
		sqlMfr += "AND ISNULL(s.Active,o.Active)=1 and  h.UPCNum in (" + skus + ")";

		//'" & rdr.Item("ModelSKU") & "'"

		SqlClient.SqlDataReader rdrMfr;
		rdrMfr = da.DBExecuteReader(con, sqlMfr);

		Dictionary<string, string> dicmfr = new Dictionary<string, string>();
		while (rdrMfr.Read) {
			dicmfr.Add(rdrMfr.Item("upcnum"), rdrMfr("NewMfrCode") + "^" + rdrMfr("PL") + "^" + rdrMfr("IQBU"));
			//mfrCode = rdrMfr("NewMfrCode")
			//mfrplcode = rdrMfr("PL")
			//mfrbuCode = rdrMfr("IQBU")

		}
		rdrMfr.Close();

		if (!rdr.HasRows) {
			ImportLog.Add(DateTime.Now, string.Format("no systems affected"));
		} else {
			while (rdr.Read) {
				ImportLog.Add(DateTime.Now, string.Format("Checking system:" + rdr.Item("modelSku")));
				// do not import systems begging with X they are 'fake'
				if (LCase(Left(rdr.Item("ModelSKU"), 1)) != "x") {

					//Gather some common information about the system to create / check 
					sector = iq.i_sector_code("HP" + rdr.Item("busunit"));

					System.DateTime activeTo = (System.DateTime)"31/12/2100";
					if (!IsDBNull(rdr.Item("activeToDate")))
						activeTo = rdr.Item("activetodate");

					bool publish = true;
					if (rdr.Item("AAonly") != 0) {
						publish = false;
					}

					bool Inserting = true;
					//Get the product if we already have it otherwise create a new one

					string modelsku = rdr.Item("ModelSKU");
					//      If modelsku = "QK765A" Then Stop



					if (!dicmfr.ContainsKey(modelsku)) {
						Beep();
					} else {
						string[] b = Split(dicmfr(modelsku), "^");
						string mfrCode = b(0);
						string mfrbuCode = b(1);
						string mfrplcode = b(2);


						clsProduct ep;
						if (iq.i_SKU.ContainsKey(modelsku)) {
							ep = iq.i_SKU(modelsku);
							if (!ep.isSystem()) {
								ep.isSystem = true;
								ep.update(errormessages);
								ImportLog.Add(DateTime.Now, string.Format("SETTING SYSTEM FLAG:" + rdr.Item("modelSku")));
								Inserting = false;
								//lets' be explicit about this
							} else {
								// Product = iq.i_SKU(rdr.Item("ModelSKU")) ---Do not  get the current iq2 data
								Inserting = false;
								//Reconstruct a product only in memory from iq1 data
								Product = new clsProduct(modelsku, true, false, sector, iq.i_ProductType_Code(rdr.Item("systype")), rdr.Item("activefromdate"), activeTo, rdr.Item("active"), rdr.Item("eol"), publish,
								mfrCode, mfrbuCode, mfrplcode, null, -1, true);
							}

						} else {
							Product = new clsProduct(modelsku, true, false, sector, iq.i_ProductType_Code(rdr.Item("systype")), rdr.Item("activefromdate"), activeTo, rdr.Item("active"), rdr.Item("eol"), publish,
							mfrCode, mfrbuCode, mfrplcode);
							//this IS a system 
							ImportLog.Add(DateTime.Now, string.Format("ADDING:" + rdr.Item("modelSku")));
						}


						//Make a focus attibute based on the system type (lightly translated to the portfolio)
						//ISS PSG SWD
						clsProductAttribute FA = new clsProductAttribute(Product, iq.i_attribute_code("FOCUS"), 0, textUnit, sysTypeToPortfolio(rdr.Item("systype")), AttribWriteCache, !Inserting);
						string scc = rdr.Item("supplyChainCode");
						if (dicChains.ContainsKey(scc)) {
							//If Product.Attributes.Count > 0 Then
							//    Dim scFound = From f In Product.Attributes.Values Where f.Attribute.Code = "SC"

							//End If

							clsProductAttribute sc = new clsProductAttribute(Product, iq.i_attribute_code("SC"), 0, iq.i_unit_code("txt"), dicChains(scc), AttribWriteCache, !Inserting);
							clsProductAttribute SCFA = new clsProductAttribute(Product, iq.i_attribute_code("FOCUS"), 0, textUnit, dicSC(scc), AttribWriteCache, !Inserting);
						}


						if (!IsDBNull(rdr.Item("U"))) {
							//U = New clsProductAttribute(Product, iq.i_attribute_code("U"), rdr.Item("U"), iq.i_unit_code("U"),  iq.AddTranslation(rdr.Item("U") & " U", English, "U"), AttribWriteCache)
							U = new clsProductAttribute(Product, iq.i_attribute_code("U"), rdr.Item("U"), iq.i_unit_code("U"), null, AttribWriteCache, !Inserting);
						}

						if (!IsDBNull(rdr.Item("productNote"))) {
							if (Trim(rdr.Item("productnote")) != "") {
								clsProductAttribute note = new clsProductAttribute(Product, iq.i_attribute_code("note"), 0, textUnit, iq.AddTranslation(rdr.Item("productNote"), English, "ProdNote", 0, Tlwc, nextkey, true), AttribWriteCache, !Inserting);
							}
						}

						if (!IsDBNull(rdr.Item("EnergyStar"))) {
							if ((int)rdr.Item("energystar") > 0) {
								clsProductAttribute es = new clsProductAttribute(Product, iq.i_attribute_code("eStar"), 1, textUnit, tlyes, AttribWriteCache, !Inserting);
							}
						}

						if (!IsDBNull(rdr.Item("WLAN"))) {
							clsProductAttribute wl = new clsProductAttribute(Product, iq.i_attribute_code("WLAN"), 1, textUnit, tlyes, AttribWriteCache, !Inserting);
						}

						if (!IsDBNull(rdr.Item("WWAN"))) {
							clsProductAttribute ww = new clsProductAttribute(Product, iq.i_attribute_code("WWAN"), 1, textUnit, tlyes, AttribWriteCache, !Inserting);
						}


						if (!IsDBNull(rdr.Item("vga")) && (int)rdr("vga") == 1) {
							clsProductAttribute note = new clsProductAttribute(Product, iq.i_attribute_code("vga"), 1, textUnit, tlyes, AttribWriteCache, !Inserting);
						}


						//will need to do the same for the secondary storage/optical drives
						if (!IsDBNull(rdr.Item("FamilyPriStor"))) {
							//optfamily translation  -- This is a code like NHP355SFF
							clsTranslation oftl = iq.AddTranslation(rdr.Item("familypristor"), English, "", 0, Tlwc, nextkey, false);
							clsProductAttribute pristor = new clsProductAttribute(Product, pristorAtt, 0, textUnit, oftl, AttribWriteCache, !Inserting);
						}


						//same as formfactor

						//If Not IsDBNull(rdr.Item("instFormFactor")) Then
						// Dim FormF As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("formFactor"), 1, textUnit, iq.AddTranslation(rdr.Item("instFormFactor"), English, "FF"), AttribWriteCache)
						// End If


						if (!IsDBNull(rdr.Item("weightUnboxed"))) {
							//21kg&nbsp;&nbsp;(46.30lb)
							//take the --- text out and use the conversions

							object wu = rdr.Item("weightUnboxed");
							[] p = Split(wu, "kg");
							if (UBound(p) != 1)
								System.Diagnostics.Debugger.Break();
							float kg = Val(p(0));
							//Dim tl As clsTranslation = iq.AddTranslation(wu$, English, "WU", 0, Nothing, True)
							//     Dim mass As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("mass"), kg, textUnit, tl, AttribWriteCache)
							clsProductAttribute mass = new clsProductAttribute(Product, iq.i_attribute_code("mass"), kg, iq.i_unit_code("kg"), null, AttribWriteCache, !Inserting);

						}

						//MAKE THE MAJOR SPEC TABLE ATTRIBUTES - preinstalled options are in import.FIOs()
						//Make an attribute for every abbreviation referenced in the various COLUMNS of products.union_systems
						clsProductAttribute pa;
						clsTranslation abtl;
						//abbreviation translation

						foreach ( k in Split(columns, ",")) {
							// If k = "formFactor" Then Stop
							if (!IsDBNull(rdr.Item(k))) {
								//some of the columns (notably options and extras) contain CD lists

								float nv = -1;
								if (k == "display") {
									if (InStr(rdr.Item(k), "_")) {
										string[] p = Split(rdr.Item(k), "_");
										string res = p(3);
										if (res == "LED")
											res = p(4);
										string[] dm = Split(res, "x");
										if (dm.Count > 1) {
											nv = Val(dm(0)) * Val(dm(1));
											//find the number of pixels
											if (nv == 0)
												System.Diagnostics.Debugger.Break();
											//we create the productattribute a little later

											if (!iq.i_attribute_code.ContainsKey("displaySize")) {
												clsAttribute ds = new clsAttribute("displaySize", iq.AddTranslation("Display Size (diagonal)", English, "DispSZ", 0, Tlwc, nextkey, false), 0);
											}

											//DIS_15.6_WXGA_1366x768_AGBV
											pa = new clsProductAttribute(Product, iq.i_attribute_code("displaySize"), p(1), iq.i_unit_code("Inch"), null, AttribWriteCache, !Inserting);
										}
										//If InStr(p(1)(3)),"x" then do something here for megapixels

									}
								}

								if (k == "extras" | k == "options" | k == "software" | k == "raidTech") {
									foreach ( ik in Split(rdr.Item(k), ",")) {
										//for each of the CD values ad an attribute of the type of the value
										//abtl = optabbreviations(UCase(k)) 'abbreviations translation of MCR,CAM,SDR,BT etc
										if (!iq.i_attribute_code.ContainsKey(Left(ik, 20))) {
											//we don't have an MCR, CAM, SDR *attribute* yet - so make one
											if (!optabbreviations.ContainsKey(UCase(ik))) {
												//well it wasn't in abbreviations - so it's *probably* a part number.. or maybe something like "keyboard kit"
												// Stop
												//append it to the "additional" attribute
												att = null;
											} else {
												if (LCase(ik) == "name")
													System.Diagnostics.Debugger.Break();
												att = new clsAttribute(Left(ik, 20), optabbreviations(UCase(ik)), 0);
												//an MCR,CAM,SDR (or some other recogised abbreviation)
											}
										}

										if (!att == null) {
											att = iq.i_attribute_code(Left(ik, 20));
											//                                                                                      yes
											if (!Product.i_Attributes_Code.ContainsKey(att.Code)) {
												pa = new clsProductAttribute(Product, att, 1, textUnit, null, AttribWriteCache, !Inserting);
											} else {
												Product.i_Attributes_Code(att.Code)(0).NumericValue += 1;
												Product.i_Attributes_Code(att.Code)(0).update(errormessages);
												Logit("duplicate " + k + ":" + ik);
											}
										}
									}
								} else {
									//add an attribute of the type of the column header (e.g. warrantyCode,formFactor,mfrBuildCode,display,intVideo,ILOhardware,terStorTech,raidTech"
									//'  If InStr(rdr.Item(k), ",") Then Stop
									if (LCase(rdr.Item(k)) == "name")
										System.Diagnostics.Debugger.Break();
									if (LCase(k) == "name")
										System.Diagnostics.Debugger.Break();
									if (optabbreviations.ContainsKey(UCase(rdr.Item(k)))) {
										abtl = optabbreviations(UCase(rdr.Item(k)));
										//the translation of theis abbreviation will (should) alrery exist .. eg."WTY111NBD" = [[IQ.clsLanguage, 1 Year Parts / 1 Year Labour / 1 Year Onsite Warranty Next Business Day]
										pa = new clsProductAttribute(Product, iq.i_attribute_code(k), nv, textUnit, abtl, AttribWriteCache, !Inserting);
									} else {
										//Something for which there was no IQ1 abbreviation like 'french keyboard' or EMA7029 
										//    Beep()
									}
								}
							}
						}

						//This is done in import descriptions
						// Dim desc = New clsProductAttribute(Product, iq.Attributes("desc"), 0, iq.Units("txt"), iq.AddTranslation(Trim$(rdr.Item("desc"))).Key, AttribWriteCache)

						object sku;
						sku = Trim(rdr.Item("modelsku"));
						if (InStr(LCase(sku), "paul"))
							System.Diagnostics.Debugger.Break();
						if (sku == "")
							System.Diagnostics.Debugger.Break();

						// mfrSKU = New clsProductAttribute(Product, iq.i_attribute_code("MfrSKU"), 0, textUnit, iq.AddTranslation(sku$, English, "SKU", 0, Tlwc, nextkey, False), AttribWriteCache, Not Inserting)
						PLcode = new clsProductAttribute(Product, iq.i_attribute_code("PLcode"), 0, textUnit, iq.AddTranslation(rdr.Item("pl"), English, "PL", 0, Tlwc, nextkey, false), AttribWriteCache, !Inserting);


						//for systems - their 'name' *is* their part number
						// SystemName = New clsProductAttribute(Product, iq.i_attribute_code("~ame"), 0, textUnit, mfrSKU.Translation, AttribWriteCache)

						// If InStr(LCase(SystemName.displayName(English)), "paul") Then Stop
						//SystemName = New clsProductAttribute(Product, iq.Attributes("~ame"), 0, iq.Units("txt"), iq.AddText(rdr.Item("familycode"), s_lang, TranslationWriteCache).Key, AttribWriteCache)

						//product attributes are a list of each type.. so we can have multiple alsohosts and don't need a horrid comma separated list)
						clsProductAttribute alsoHost;
						if (!IsDBNull(rdr.Item("alsohost"))) {
							foreach ( h in Split(rdr.Item("alsoHost"), ",")) {
								alsoHost = new clsProductAttribute(Product, iq.i_attribute_code("alsoHost"), 0, textUnit, iq.AddTranslation(rdr.Item("alsoHost"), English, "", 0, Tlwc, nextkey, false), AttribWriteCache, !Inserting);
							}
						}

						object fn = rdr.Item("sysFamilyname");

						//DO NOT unabreviate it here !!
						//If dicAbbreviations.ContainsKey(fn$) Then fn$ = dicAbbreviations(fn$)
						//NOTE - the translations of the family name won't be duplicated - so, although every system will have a family attribute - all those attributes to a s set of a hundred or so tranlsations
						if (!Product.i_Attributes_Code.ContainsKey("FamMajor")) {
							FamMajor = new clsProductAttribute(Product, iq.i_attribute_code("FamMajor"), 0, textUnit, iq.AddTranslation(fn, English, "FamMajor", 0, Tlwc, nextkey, false), AttribWriteCache, !Inserting);
						}

						if (dicAbbreviations.ContainsKey(fn))
							fn = dicAbbreviations(fn);
						FamDisp = new clsProductAttribute(Product, iq.i_attribute_code("FamDisp"), 0, textUnit, iq.AddTranslation(fn, English, "FamDisp", 0, Tlwc, nextkey, false), AttribWriteCache, !Inserting);


						//Family Minor -- (Familycode - granular)
						clsTranslation tl;
						//   nextkey = 0
						//   Tlwc = Nothing
						tl = iq.AddTranslation(rdr.Item("ccdescription"), s_lang, "sysDesc", 0, Tlwc, nextkey, false);
						object desc = new clsProductAttribute(Product, iq.i_attribute_code("desc"), 0, textUnit, tl, AttribWriteCache, !Inserting);

						clsProductAttribute prda;
						//Also Included and Options
						if (!IsDBNull(rdr.Item("Extras"))) {
							prda = new clsProductAttribute(Product, iq.i_attribute_code("Also included"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("Extras"), English, "", 0, Tlwc, nextkey, false), AttribWriteCache, !Inserting);
						}

						if (!IsDBNull(rdr.Item("Options"))) {
							prda = new clsProductAttribute(Product, iq.i_attribute_code("Options"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("Options"), English, "", 0, Tlwc, nextkey, false), AttribWriteCache, !Inserting);
						}


						//Graphics
						string linkAttributes = "";
						if (rdr("DiscreteGraphics") != null && !object.ReferenceEquals(rdr("DiscreteGraphics"), DBNull.Value)) {
							linkAttributes += rdr("DiscreteGraphics");
						} else if (rdr("IntVideo") != null && !object.ReferenceEquals(rdr("IntVideo"), DBNull.Value)) {
							linkAttributes += rdr("IntVideo");
						} else if (rdr("InstVGA") != null && !object.ReferenceEquals(rdr("InstVGA"), DBNull.Value)) {
							linkAttributes += rdr("InstVGA");
						}
						prda = new clsProductAttribute(Product, iq.i_attribute_code("Graphics"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "VID", 1, Tlwc, nextkey, false), AttribWriteCache, !Inserting);

						//Networking
						linkAttributes = "";
						if (rdr("WLAN") != null && !object.ReferenceEquals(rdr("WLAN"), DBNull.Value)) {
							if ((iq.i_SKU.ContainsKey(rdr("WLAN")))) {
								clsProduct prodAlsoIncuded = iq.i_SKU(rdr("WLAN"));
								if (prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc")) {
									prda = new clsProductAttribute(Product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation, AttribWriteCache, !Inserting);
								}
							} else {
								linkAttributes += rdr("WLAN") + ", ";
							}

						}
						if (rdr("WWAN") != null && !object.ReferenceEquals(rdr("WWAN"), DBNull.Value)) {
							if ((iq.i_SKU.ContainsKey(rdr("WWAN")))) {
								clsProduct prodAlsoIncuded = iq.i_SKU(rdr("WWAN"));
								if (prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc")) {
									prda = new clsProductAttribute(Product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation, AttribWriteCache, !Inserting);
								}
							} else {
								linkAttributes += rdr("WWAN") + ", ";
							}

						}
						if (rdr("InstNIC") != null && !object.ReferenceEquals(rdr("InstNIC"), DBNull.Value)) {
							if ((iq.i_SKU.ContainsKey(rdr("InstNIC")))) {
								clsProduct prodAlsoIncuded = iq.i_SKU(rdr("InstNIC"));
								if (prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc")) {
									prda = new clsProductAttribute(Product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation, AttribWriteCache, !Inserting);
								}
							} else {
								linkAttributes += rdr("InstNIC");
							}
						}
						if (linkAttributes.Length > 0) {
							linkAttributes = Left(linkAttributes, Len(linkAttributes) - 2);
							prda = new clsProductAttribute(Product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "NKW", 0, Tlwc, nextkey, false), AttribWriteCache, !Inserting);
						}

						//QuickSpecs
						//   ImportQuickSpecsInc(Product, Trim$(rdr.Item("sysfamilyname")), Inserting, AttribWriteCache)

						//xText
						Import.ExtText(Product, Inserting, AttribWriteCache, nextkey, Tlwc, xtdic, famlocs);
						//  nextkey = 0
						//End of attrs
						if (Trim(rdr.Item("familycode")) == "")
							System.Diagnostics.Debugger.Break();
						tl = iq.AddTranslation(Trim(rdr.Item("familycode")), English, "FamMinor", 0, Tlwc, nextkey, false);
						FamMinor = new clsProductAttribute(Product, iq.i_attribute_code("FamMinor"), 0, textUnit, tl, AttribWriteCache, !Inserting);


						//this was missing - added by nick 1.10.2015
						tl = iq.AddTranslation(Trim(rdr.Item("sysfamilyname")), English, "FamMajor", 0, Tlwc, nextkey, false);
						FamMajor = new clsProductAttribute(Product, iq.i_attribute_code("FamMajor"), 0, textUnit, tl, AttribWriteCache, !Inserting);



						if (!object.ReferenceEquals(rdr.Item("cpu"), DBNull.Value)) {
							cpuSKU = new clsProductAttribute(Product, iq.i_attribute_code("cpuSKU"), 0, textUnit, iq.AddTranslation(Trim(rdr.Item("cpu")), English, "CPUSKU", 0, Tlwc, nextkey, false), AttribWriteCache, !Inserting);
						}

						string fcode;
						clsBranch famBranch;

						fcode = Trim(rdr.Item("sysfamilyname"));

						//Find the family branch
						//famBranch = FindBranchByName("tree.1", fcode)
						string path = "";

						if (!famlocs.ContainsKey(fcode)) {
							object dud = modelsku + " " + fcode;
							object kkk = 0;
						} else {
							string fampath = famlocs(fcode);
							famBranch = iq.Branches(Split(fampath, ".").Last);

							string famname = famBranch.DisplayName(English);

							if (famBranch.Product.SKU != "") {
								//    famBranch = famBranch.Parent
								famBranch.Product.SKU = "";
								famBranch.Product.update(errormessages);
								if (famBranch.Product.i_Attributes_Code.ContainsKey("mfrSKU")) {
									clsProductAttribute pat = famBranch.Product.i_Attributes_Code("mfrsku")(0);
									pat.delete(errormessages);
								}
							}


							object picture;

							picture = famBranch.Picture;
							if (!IsDBNull(rdr.Item("sysfamilyimg"))) {
								picture = rdr.Item("sysfamilyimg");
								//picture = Split(picture, "_")(1)
							}

							// Need to do this for editing...
							if (Inserting) {
								sysBranch = new clsBranch(Product, famBranch, iq.AddTranslation(Product.SKU, English, "SKU", 10, Tlwc, nextkey, false), picture, optTrans, optTransSingular, null, famBranch.childBranches.Count * 10, false, "K");
								//these ARE the systems (so we use the opt key - becuase they *contain* options)
								//make the quantity records the make the system visible by region/country - these are the gobal/pathless ones

								string rgns = "";
								if (!IsDBNull(rdr.Item("activesites")))
									rgns = rdr.Item("activesites");
								if (rdr.Item("aaonly") != 0) {
									rgns += ",AA";
								}

								//there are a few 'junk' systems with no activesites
								if (rgns == "") {
									clsQuantity qty;
									//EXCLUDE this system eveywhere (with a min increment of 0) - 
									qty = new clsQuantity(r_worldwide, "", sysBranch, 0, 0, 0, 0, QtyWritecache);
								//Public Sub New(region As clsRegion, ByVal Path As String, ByVal branch As clsBranch, ByVal numPreInstalled As Integer, ByVal MinIncrement As Integer, ByVal PreferredIncrement As Integer, freeOfCharge As Boolean, Optional Writecache As DataTable = Nothing)
								} else {
									MakeSystemQuantities(sysBranch, rgns, clsRegion.containment(), QtyWritecache);
								}
							} else {
								string outPath = "";
								List<int> branchIDs = new List<int>();
								sysBranch = famBranch.findChildBySKU2(path, Product.SKU, outPath, false);
								if (sysBranch == null) {
									foreach ( branch in famBranch.childBranches.Values) {
										if (branch.Product != null && branch.Product.SKU == "") {
											branchIDs.Add(branch.ID);
										}
									}
									List<string> errorMsg = new List<string>();
									foreach ( brID in branchIDs) {
										famBranch.childBranches.Remove(brID);
										iq.Branches(brID).deleted = true;
										iq.Branches(brID).Update(errormessages);
										//famBranch.childBranches(brID).delete(errormessages)
									}
								}
								//    Dim Product = New clsProduct(rdr.Item("ModelSKU"), True, sector, iq.i_ProductType_Code(rdr.Item("systype")), rdr.Item("activefromdate"), activeTo, rdr.Item("active"), rdr.Item("eol"), publish, "", "", "") 'this IS a system 
								//    famBranch.Product = Product
								//    Dim errorMsg As List(Of String) = New List(Of String)
								//    famBranch.Update(errorMsg)
								//End If
								//Compare the products
								CompareProduct(iq.i_SKU(Product.SKU), Product, true, ActionList, true, AttribWriteCache);
							}
						}
					}
				}
			}
		}
		rdr.Close();


		da.BulkWrite(con, QtyWritecache, "Quantity");
		QtyWritecache = null;

		//write the accumulated product attributes (bulk copy)
		da.BulkWrite(con, AttribWriteCache, "ProductAttribute");
		AttribWriteCache = null;

		da.BulkWrite(con, Tlwc, "Translation");
		Tlwc = null;

	}


	public void CullBadOptions()
	{
		Dictionary<string, string> famlocs = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
		RootBranch.FindFamilyBranchesBelow("tree.1", 4, famlocs);

		object sql__1;

		Sql = "SELECT v.OptSN,po.optsku,v.sortorder,";
		Sql += "speedUnitQty as speed,optTypeSpeedUnit as speedUnit,sysfamily,slotaddtype,slotaddqty,";
		Sql += "unitQty as capacity,ot.optTypeUnit as capacityUnit,";
		Sql += "technology,altsku,incompatible,v.L1,v.L2,v.L3, po.optfamily,po.opttype,activeFromDate,activeToDate,active,eol,aaonly,descriptionHP,slots,ccdescription ";
		Sql += "FROM [iq].products.V2_OptionCats v ";
		Sql += "JOIN [iq].products.options po ON v.optsn=po.optsn ";
		Sql += "JOIN [iq].products.optTypes as OT on OT.optTypeCode=optType ";
		Sql += "JOIN [channelcentral].products.Hierarchy h ON h.upcNUM = po.optsku ";
		//Sql$ &= "WHERE po.optsku IN (" & optString & ")"
		Sql += " order by sysfamily";

		SqlClient.SqlConnection con = new SqlConnection("data source=www3.channelcentral.net,8484;initial catalog=IQ;uid=editor;pwd=wainwright");
		con.Open();

		SqlClient.SqlDataReader rdr;

		rdr = da.DBExecuteReader(con, sql__1);
		Dictionary<string, HashSet<string>> dicValid = new Dictionary<string, HashSet<string>>(StringComparer.CurrentCultureIgnoreCase);

		while (rdr.read) {

			if (!object.ReferenceEquals(rdr("sysfamily"), DBNull.Value)) {
				string sf = rdr.Item("sysfamily");
				if (sf != "") {

					foreach ( f in sf.Split(",")) {
						if (!famlocs.ContainsKey(f)) {
							object a = 0;
						} else {
							if (!dicValid.ContainsKey(f))
								dicValid.Add(f, new HashSet<string>(StringComparer.CurrentCultureIgnoreCase));
							string optsku = rdr.Item("optsku");
							if (dicValid(f).Add(optsku) == false) {
								Beep();
								//option sku is listed twice in the same family
							}
						}
					}
				}
			}
		}
		rdr.Close();
		con.Close();

		//now walk each all options branch and delete anything not the list per family

		HashSet<clsBranch> done = new HashSet<clsBranch>();
		HashSet<string> todel = new HashSet<string>();
		int naobs = 0;
		int kept = 0;
		HashSet<string> toclean = new HashSet<string>();
		//these (category) branches need their products removed

		//we will walk down each 'all options' branch - making sure that every option we come accross appears in the 'validpaths'
		foreach ( fam in famlocs.Keys) {

			clsBranch fambranch = iq.Branches(Split(famlocs(fam), ".").Last);
			clsBranch aoBranch = fambranch.FindBranchByNameBelow("All Options", "tree.1", false, 6);

			if (aoBranch != null) {
				//check each 'all options' branch



				if (!done.Contains(aoBranch)) {
					//no child of alloptions (L1 BRANCH) should have a SKU !
					foreach ( l1 in aoBranch.childBranches.Values) {
						if (l1.HasSKU)
							todel.Add(l1.ID);
						//these branches need deleting
						l1.deleted = true;

						//If l1.Product IsNot Nothing Then toclean.Add(l1.ID) 'these need their products removed
						//For Each l2 In 
					}

					if (dicValid.ContainsKey(fam)) {
						aoBranch.compareAgainst(dicValid(fam), kept, todel);
						done.Add(aoBranch);
					}
				}
			} else {
				naobs += 1;
			}
		}

		if (todel.Count) {
			int toskip = 0;
			int chunk = 1000;
			do {
				object ll = from j in todel.Skip(toskip).Take(chunk);
				if (!ll.Any)
					break; // TODO: might not be correct. Was : Exit Do

				sql__1 = "update branch set deleted =1 WHERE [id] IN(" + Join(ll.ToArray, ",") + ")";

				LongSQL(sql__1);
				toskip += 1000;
			} while (true);

		}

	}


	public void FixMissingMemory()
	{
		SqlClient.SqlConnection con = da.OpenDatabase();
		Dictionary<string, Dictionary<string, string>> ofDic = Import.FamilyOptTypeToOptFamily();
		//Gives a lookup of narrow optfamily from BroadOptType per sysfamily
		Dictionary<string, IQ.clsLimit> dicOptLimits;

		//the actual installed cpu quantity comes form products.systems (overriding optionlimits !)
		Dictionary<string, int> dicCpuQty = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
		Dictionary<string, string> dicCpuSku = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
		object sql = "select modelsku,cpuqty,cpu from h3.iq.products.systems";
		SqlClient.SqlDataReader r = da.DBExecuteReader(con, sql);
		while (r.Read) {
			if (!object.ReferenceEquals(r("CpuQty"), DBNull.Value)) {
				dicCpuQty.Add(r("modelsku"), r("cpuqty"));
			}
			if (!object.ReferenceEquals(r("Cpu"), DBNull.Value)) {
				if (r("CPU") != "") {
					dicCpuSku.Add(r("MODELSKU"), r("Cpu"));
				}
			}
		}
		r.Close();


		List<string> delSlots = new List<string>();

		//returns a dictinoary of the narrow,minor sysFamilyCode^optfamily>clslimit EG.. DL580PLFF^NHPLFF>blah
		dicOptLimits = Import.BuildOptLimits(con, ofDic);

		object pth = "";
		Dictionary<string, clsBranch> sysLocs = RootBranch.findSystemBranches("tree.1");


		int added = 0;
		DataTable swc = da.MakeWriteCacheFor(con, "slot");

		foreach ( sysPath in sysLocs.Keys) {
			clsBranch sysBranch = sysLocs(sysPath);

			if (sysBranch.Product.i_Attributes_Code.ContainsKey("famminor")) {
				object sysMinorFam = sysBranch.Product.i_Attributes_Code("famminor")(0).Translation.text(English);

				string systemSKU = sysBranch.Product.SKU;

				//   If sysBranch.Product.SKU = "765822-031" Then Stop 'this is a 2 processor ML350g9 - fully pop'd

				clsBranch chassisBranch;
				string chassispath;
				foreach ( cb in sysBranch.childBranches) {
					if (cb.Value.Translation.text(English).Contains(" chassis")) {
						chassisBranch = cb.Value;
						chassispath = sysPath + "." + chassisBranch.ID;
						break; // TODO: might not be correct. Was : Exit For
					}
				}

				if (chassisBranch == null)
					System.Diagnostics.Debugger.Break();

				// If sysMinorFam.ToUpper.Contains("BL460") Then Stop

				string memMinor = "";

				clsLimit cpuLimits = null;
				clsLimit memLimits = null;
				foreach ( k in dicOptLimits.Keys) {
					if (k.StartsWith(sysMinorFam + "^CPU")) {
						cpuLimits = dicOptLimits(k);
					} else if (k.StartsWith(sysMinorFam + "^MEM")) {
						memLimits = dicOptLimits(k);
						memMinor = Split(k, "^")(2);
					}
				}


				if (cpuLimits != null & memLimits != null) {
					if (dicCpuQty.ContainsKey(systemSKU)) {
						cpuLimits.Qinstalled = dicCpuQty(systemSKU);
					}

					//make sure CPU slots reside on the chassis branch and are 'generic'
					List<string> errormessages = new List<string>();
					bool hadone = false;
					foreach ( s in chassisBranch.slots.Values) {
						if (s.Type.MajorCode == "CPU") {
							if (s.Type.MinorCode == "GEN_CPU" & !hadone) {
								if (s.numSlots != cpuLimits.Qmax){s.numSlots = cpuLimits.Qmax;s.update(errormessages);}
								hadone = true;
							} else {
								s.deleted = true;
								delSlots.Add(s.ID);
							}
						}
					}
					if (!hadone) {
						//there was no generic cpu slot on this chassisbranch .. so make one
						clsSlot cpuslots = new clsSlot(iq.i_slotType_Code("CPU")("GEN_CPU"), chassisBranch, "", cpuLimits.Qmax, null, new NullableInt(), 0, 1, swc);
					}



					//Some multiprocesor machines (typically workstations) have the same SKU for the upgrade and presintalled CPU - so we *must* put the memory sockets on the CPU branch (not the chassis)
					//(if they are on the cassis, and the 'upgrade' CPU we end up with double the right initial number)

					//the upshot is that for multicpu machines the memory slots must NOT be on the branch, but must be on both the preinstalled (sometimes L21) and (if it's different) the additional CPU sku




					clsSlotType st;
					st = iq.SlotTypes.Values.Where(sst => sst.MajorCode.ToUpper == "MEM" & sst.MinorCode.ToUpper == memMinor.ToUpper).FirstOrDefault;
					if (st == null) {
						st = new clsSlotType("MEM", memMinor);
					}



					//we will delete all chassis branch memory slots - and recreate them either on the chassis or the cpu(s)
					clsSlot chassisMemslot;
					bool ok = false;
					foreach ( s in chassisBranch.slots.Values) {
						if (s.Type.MajorCode.ToUpper == "MEM") {
							s.deleted = true;
							delSlots.Add(s.ID);
						}
					}


					if (cpuLimits.Qinstalled == cpuLimits.Qmax) {
						//single cpu (Or fully popupulated)  machine - memory on the chassis branch (the cpu isnt an option)
						clsSlot gslot = new clsSlot(st, chassisBranch, "", memLimits.Qmax, null, new NullableInt(), memLimits.Qmin, 0, swc);
					} else {
						clsSlot gslot = new clsSlot(st, chassisBranch, "", memLimits.Qmax, null, new NullableInt(), memLimits.Qmin, 0, swc);


						//Find the B21 under all options
						//make a memory (enablement) slots *with a correct path*
						if (dicCpuSku.ContainsKey(systemSKU)) {
							string cpuSku = dicCpuSku(systemSKU);
							Dictionary<clsProduct, string> optlocs = new Dictionary<clsProduct, string>();
							bool foundCpuAsOption = false;
							sysBranch.optionsBelow(sysPath, optlocs);
							Dictionary<string, string> skuToPath = new Dictionary<string, string>();
							foreach ( p in optlocs) {
								skuToPath.Add(p.Key.SKU, p.Value);
							}

							if (skuToPath.ContainsKey(cpuSku)) {
								string cpupath = skuToPath(cpuSku);
								string fullpathname = PathName(cpupath);
								//see if the cpu is already giving memory slots at this path 
								clsBranch cpubranch = iq.Branches(Split(cpupath, ".").Last);

								bool addit = true;
								foreach ( s in cpubranch.slots.Values) {
									if (s.path == "")
										delSlots.Add(s.ID);
									//no (upgradeable) cpu should give memory slots everywhere 
									//ok this is good - but we should check
									if (s.path == cpupath) {
										addit = false;
										//we don't need to add
										if (s.numSlots != memLimits.Qmax)
											System.Diagnostics.Debugger.Break();
									}
								}

								if (addit) {
									//make a new slot
									//    Dim st As clsSlotType
									//    st = iq.SlotTypes.Values.Where(Function(sst) sst.MajorCode.ToUpper = "MEM" And sst.MinorCode.ToUpper = memMinor.ToUpper).FirstOrDefault
									//    If st Is Nothing Then
									// st = New clsSlotType("MEM", memMinor)
									//  End If

									object ssss = 1;
									clsSlot cpumemslot = new clsSlot(st, cpubranch, cpupath, memLimits.Qmax, null, new NullableInt(), memLimits.Qmin, 0, swc);
								}

								if (cpubranch.Product.i_Attributes_Code.ContainsKey("altsku")) {
									string altsku = cpubranch.Product.i_Attributes_Code("altsku")(0).Translation.text(English);
									object fff = 0;


								}



							// Beep()
							} else {
								Beep();
								//cpu is not an option..
							}
						} else {
							//unknown CPU


						}
					}
				}
			}
		}

		con.Close();

		con = da.OpenDatabase();
		Debug.Print(added);
		da.BulkWrite(con, swc, "slot");

		da.DBExecutesql("update slot set deleted = 1 where id in ('" + Join(delSlots.ToArray, "','") + "');");


		con.Close();



	}

	public object checkoptions()
	{



		HashSet<string> sysfamilies = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

		SqlClient.SqlConnection con = da.OpenDatabase;

		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, "select sysfamilyname from h3.iq.products.sysfamilydefinitions");
		while (rdr.Read) {
			sysfamilies.@add(rdr(0));
		}
		rdr.Close();

		rdr = da.DBExecuteReader(con, "select optsku,sysfamily from h3.iq.products.options");

		HashSet<string> badfamilies = new HashSet<string>();

		int duds = 0;
		StreamWriter sw = new StreamWriter("c:\\temp\\dudoptions.txt");
		while (rdr.Read) {
			if (!object.ReferenceEquals(rdr("sysfamily"), DBNull.Value)) {
				foreach ( sf in Split(rdr("sysfamily"), ",")) {
					if (sf.Trim != "") {
						if (!sysfamilies.Contains(sf)) {
							sw.WriteLine(rdr("optsku") + "-" + sf);
							duds += 1;
							if (sysfamilies.Contains(Trim(sf)))
								System.Diagnostics.Debugger.Break();
							badfamilies.Add(sf);
						}
					}

				}
			}
		}
		sw.Close();
		rdr.Close();
		con.Close();


		sw = new StreamWriter("c:\\temp\\badfamilies.txt");
		foreach ( f in badfamilies) {
			sw.WriteLine(f);
		}
		sw.Close();





	}


	//Options only
	public object Buildtreeinc(SqlClient.SqlConnection con, List<string> optskus, Dictionary<clsProduct, List<clsRegion>> dicOptLocalisation, Dictionary<string, clsSlotType> dicSlotTypes, clsActionList ActionList, bool AllowDelete, List<string> systemSKUs)
	{

		//adds any missing optSKUs to the broad options tree - note, they may need to be pruned in manyy locations if they
		//are explicity incompatible OR impliciltly incompatible (wrong opttype for the sysfamily)


		bool JustDoit = true;
		//added in to skip the user prompts as there are sooooo many changes at the moment, the ActionList idea can be reinstigated when itsd 1 or 2 changes a run
		object atCount = 0;
		object totalCount = optskus.Count;

		object optString = string.Join(",", optskus.Select(l => "'" + l + "'"));

		object sysString = string.Join(",", systemSKUs.Select(l => "'" + l + "'"));

		List<string> ERRORMESSAGES = new List<string>();

		int kept = 0;
		int pruned = 0;

		//Dim dicSlotTypes As Dictionary(Of String, clsSlotType)
		//dicSlotTypes = Import.slotTypes(con, dicsystems) 'dicFamily) '20 secs

		Dictionary<string, Dictionary<string, string>> ofDic = Import.FamilyOptTypeToOptFamily();
		//Gives a lookup of narrow optfamily from BroadOptType per sysfamily
		Dictionary<string, IQ.clsLimit> dicOptLimits;
		//returns a dictinoary of the narrow,minor sysFamilyCode^optfamily>clslimit EG.. DL580PLFF^NHPLFF>blah
		dicOptLimits = Import.BuildOptLimits(con, ofDic, systemSKUs);

		//        fixmissingGiveSLOTS()



		clsBranch chassisBranch;
		//        Dim chassisVariant As clsVariant
		//        Dim chassisProduct As clsProduct
		clsTranslation chassisTL = iq.AddTranslation("Chassis", English, "", 0, null, 0, false);

		//    'FACTORY INSTALLED OPTIONS/components - call them what you will
		//    'get a list (by system mfrSKU) of the part numbers and quantities of all factory installed components (PriStor, sec stor CPU, MEM etc,Raid)
		ImportLog.Add(DateTime.Now, string.Format("Checking FIOs"));
		Dictionary<string, Dictionary<string, int>> dicFIOs;
		dicFIOs = Import.FIOs(con, optString, sysString);

		int NEXTbId = 0;
		//Dim NEXTgId As Integer = 0
		// Dim nextpruneid As Integer = 0

		clsTranslation tlOptions = iq.AddTranslation("Options", English, "collect", 0, null, 0, false);
		clsTranslation tlOption = iq.AddTranslation("Option", English, "collect", 0, null, 0, false);


		object sql2 = "SELECT * FROM h3.iQ.products.SysFamilyDefinitions";
		DataTable FamilyOptionDefs;
		FamilyOptionDefs = SlowFilledDataTable(con, sql2);


		DataTable bwc = da.MakeWriteCacheFor(con, "branch", NEXTbId, true);
		//nextID is SET by this call !
		DataTable Gwc = da.MakeWriteCacheFor(con, "GRAFT");
		DataTable qwc = da.MakeWriteCacheFor(con, "quantity");
		DataTable swc = da.MakeWriteCacheFor(con, "slot");
		DataTable pwc = da.MakeWriteCacheFor(con, "prune");

		int nextkey = clsTranslation.NextKey;
		DataTable tlwc = da.MakeWriteCacheFor(con, "Translation");

		object sql;


		if (optString == "") {
			ImportLog.Add(DateTime.Now, string.Format("No options are missing ..."));

		} else {
			ImportLog.Add(DateTime.Now, string.Format("Querying IQ1 for tree details..."));

			sql = "SELECT v.OptSN,po.optsku,v.sortorder,";
			sql += "speedUnitQty as speed,optTypeSpeedUnit as speedUnit,sysfamily,slotaddtype,slotaddqty,";
			sql += "unitQty as capacity,ot.optTypeUnit as capacityUnit,";
			sql += "technology,altsku,incompatible,v.L1,v.L2,v.L3, po.optfamily,po.opttype,activeFromDate,activeToDate,active,eol,aaonly,descriptionHP,slots,ccdescription ";
			sql += "FROM [iq].products.V2_OptionCats v ";
			sql += "JOIN [iq].products.options po ON v.optsn=po.optsn ";
			sql += "JOIN [iq].products.optTypes as OT on OT.optTypeCode=optType ";
			sql += "JOIN [channelcentral].products.Hierarchy h ON h.upcNUM = po.optsku ";
			sql += "WHERE po.optsku IN (" + optString + ")";
			sql += " order by sysfamily";


			//CK branches contians an 'all options' branch for every family (and holds references to every sub-branch to 
			Dictionary<string, clsBranch> ckBranches;
			//Compound key of Sysfamily^l1^l2^l3>Branch
			ckBranches = new Dictionary<string, clsBranch>(StringComparer.CurrentCultureIgnoreCase);
			//   Dim allOptions As clsBranch
			List<string> famlist = new List<string>();
			Dictionary<string, Dictionary<string, string>> stDic = Import.FamilyOptTypeToOptFamily;

			object sql3 = "SELECT [SysFamilyName],optsku as [mfrPartNum],ISNULL([HPPS_Options].[powerMin],options.powermin) as PowerMin,isnull([HPPS_Options].[powerMax] ,options.powermax) as PowerMax ";
			sql3 += "FROM h3.iq.products.options left outer join h3.[iq].[Products].[HPPS_Options] on optsku=mfrPartNum ";
			sql3 += "WHERE (ISNULL([HPPS_Options].[powerMin],options.powermin) is not null or ISNULL([HPPS_Options].[powerMax],options.powermax) is not null) ";
			sql3 += "and mfrPartNum in (" + optString + ") order by sysfamilyname,OPTTYPE DESC";

			object iq1con = new SqlConnection("data source=www3.channelcentral.net,8484;initial catalog=IQ;uid=editor;pwd=wainwright");
			iq1con.Open();
			object powerDT = SlowFilledDataTable(iq1con, sql3);
			//seems to close the connection?

			iq1con = new SqlConnection("data source=www3.channelcentral.net,8484;initial catalog=IQ;uid=editor;pwd=wainwright");
			iq1con.Open();

			DataTable optionTable;
			optionTable = SlowFilledDataTable(iq1con, sql);
			// Dim rdr = da.DBExecuteReader(iq1con, sql$)
			object optionRdr = new DataTableReader(optionTable);
			object ock;
			List<clsSlot> todel = new List<clsSlot>();

			clsBranch mastercpusbranch;
			foreach ( branch in iq.Branches.Values) {
				if (branch.Translation.text(English).ToUpper == "CPU") {
					if (branch.childBranches.Count > 30) {
						mastercpusbranch = branch;
						break; // TODO: might not be correct. Was : Exit For
					}

				}
			}

			if (mastercpusbranch == null)
				System.Diagnostics.Debugger.Break();

			clsTranslation proctrans = iq.AddTranslation("Processor", English, "OL2", 0, tlwc, nextkey, false);
			clsTranslation procTransPlural = iq.AddTranslation("Processors", English, "OL2", 0, null, 0, false);



			int originalOptionCount = totalCount;
			totalCount = optionTable.Rows.Count;
			ImportLog.Add(DateTime.Now, string.Format("Totals Skus looked up {0} total returned ({1}", originalOptionCount, totalCount));

			//Dim bcache As New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase) 'cache paths (by various keys) for speed (branch is alaways the least segment)
			// Dim validPaths As New HashSet(Of String) 'we're compiling a list of 'subpaths' under a number of 'all options' branches (one for each familty affected)
			HashSet<string> dudFamilies;
			//log those families that options are listed as compatible with that do not exist

			Dictionary<string, string> famlocs = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
			RootBranch.FindFamilyBranchesBelow("tree.1", 4, famlocs);

			int jjj = 0;
			int kkk = 0;
			int lll = 0;

			List<string> dudoptions = new List<string>();

			clsTranslation tlFIOs = iq.AddTranslation("FIOs", English, "FIO", 0, tlwc, nextkey, false);


			while (optionRdr.Read) {
				atCount = atCount + 1;
				string optfamily = optionRdr.Item("Optfamily");
				//CAREPACK

				ImportLog.Add(DateTime.Now, string.Format("Checking placement for option SKU: {0} ({1}/{2})", optionRdr("optsku"), atCount, totalCount));

				string ot = optionRdr.Item("opttype");
				//  If ot = "CPU" Or ot = "MEM" Then

				//This is a CD list of the broad families an OPTION is compatible with eg. DL380
				if (!object.ReferenceEquals(optionRdr.Item("sysfamily"), DBNull.Value)) {

					[] sf = Split(optionRdr.Item("sysfamily"), ",");

					//                    If optionRdr.Item("optsku") = "N3R88AA" Then Stop



					foreach ( f in sf) {
						//If optionRdr("optsku") = "AN975A" And f.ToUpper = "DL380PG8" Then Stop
						//If optionRdr("optSku") = "AE465A" And f.ToUpper = "ML310EG8V2" Then Stop

						if (string.IsNullOrEmpty(f))
							continue;

						f = f.Trim;

						// If f.ToUpper.StartsWith("PROX261") Then Stop

						string ck;
						string famPath = "";
						clsBranch famBranch = null;

						if (famlocs.ContainsKey(f)) {
							famPath = famlocs(f);
							famBranch = iq.Branches(Split(famPath, ".").Last);
						}

						if (famBranch == null) {
							dudoptions.Add(optionRdr("optsku") + " *" + f + "*");
							object j = 0;
							jjj += 1;
							if (dudFamilies == null)
								dudFamilies = new HashSet<string>();
							dudFamilies.Add(f);

						//options are listed in many obsolete families - so we get a stupid number of these warnings
						//     ActionList.Add(rdr.Item("optsku"), ObjectType.WARNING, "Family " & f & " cannot be found, this import cannot create families at present")

						} else {
							lll += 1;
							string outPath = "";
							int order = optionRdr.Item("sortorder");
							clsBranch AllOptions = famBranch.FindBranchByNameBelow("All Options", "tree.1", false, 6);

							if (AllOptions == null) {
								//holder = New clsBranch(Nothing, Nothing, iq.AddTranslation("All Options", English, "", 0, tlwc, nextkey, False), "", iq.AddTranslation("All Options", English, "", 0, tlwc, nextkey, False), iq.AddTranslation("All Options", English, "", 0, tlwc, nextkey, False), Nothing, 0, False, "T", bwc, NEXTbId)
								//famBranch.Graft(holder, "IncImport", "", ERRORMESSAGES, Gwc)
								//Eek no systems in this family then...
								if (famBranch.childBranches.Count == 0) {
									AuditLog.Instance.Add(AuditType.Warning, "No systems found for family, " + f, "IncImport", 0);
									continue;

								} else {
									clsTranslation aot;
									aot = iq.AddTranslation("All Options", English, "AO", 0, tlwc, nextkey, false);

									//Create the all options branch
									object b = new clsBranch(null, null, aot, "", aot, aot, null, 0, false, "TGB",
									bwc, NEXTbId);
									//Graft it to the first system...
									foreach ( cb in famBranch.childBranches.Values) {
										if (cb.Product != null && cb.Product.isSystem) {
											cb.Graft(b, "IncImport", "", ERRORMESSAGES, Gwc);
											AllOptions = b;
											break; // TODO: might not be correct. Was : Exit For
										}
									}

									object FIOBranch__1 = new clsBranch(null, b, tlFIOs, "", tlOptions, tlOption, null, order, true, "B",
									bwc, NEXTbId);
								}
							} else {
								if (AllOptions.Parent != null)
									System.Diagnostics.Debugger.Break();
								//The families all options branch is grafted on and should NEVER have a parent

								if (AllOptions.locked)
									continue;
							}

							bool slotchanged;
							clsBranch l1branch = null;
							clsBranch l2branch = null;
							clsBranch l3branch = null;

							if (!IsDBNull(optionRdr("l1")))
								l1branch = AllOptions.FindBranchByNameBelow(optionRdr("l1"), "", false, 6);
							if (l1branch == null) {
								clsTranslation tl1 = iq.AddTranslation(optionRdr.Item("l1"), English, "OL1", 0, tlwc, nextkey, false);
								l1branch = new clsBranch(null, AllOptions, tl1, "", tlOptions, tlOption, null, order, false, "YTGB",
								bwc, NEXTbId);
							}

							if (!IsDBNull(optionRdr("l2")))
								l2branch = l1branch.FindBranchByNameBelow(optionRdr("l2"), "", false, 6);
							if (l2branch == null) {
								clsTranslation tl2 = iq.AddTranslation(optionRdr.Item("l2"), English, "OL2", 0, tlwc, nextkey, false);
								l2branch = new clsBranch(null, l1branch, tl2, "", tlOptions, tlOption, null, order, false, IsDBNull(optionRdr("l3")) ? "GTB" : "B",
								bwc, NEXTbId);
							}

							clsBranch holderbranch = l2branch;
							//there isn't always an l3 branch ! - sometimes options are only 2 levels deep

							if (!IsDBNull(optionRdr("l3"))) {
								l3branch = l2branch.FindBranchByNameBelow(optionRdr("l3"), "", false, 6);
								if (l3branch == null) {
									object txt = optionRdr.Item("l3");
									clsTranslation tl3 = iq.AddTranslation(txt, English, "OL3", 0, tlwc, nextkey, false);
									l3branch = new clsBranch(null, l2branch, tl3, "", tlOptions, tlOption, null, order, false, "G",
									bwc, NEXTbId);
								}
								holderbranch = l3branch;
							}

							string resultPath = "";
							clsBranch optionbranch = new clsBranch();
							clsBranch existingOption = holderbranch.findChildBySKU2("", optionRdr("optSKU"), resultPath);

							//Compile a list of the valid options - anything under the all otpions and not in this list needs deleting
							//Dim pth$ = AllOptions.ID & "." & l1branch.ID & "." & l2branch.ID & "."
							//If l3branch IsNot Nothing Then pth$ &= l3branch.ID & "."
							//If existingOption IsNot Nothing Then
							//    pth$ &= existingOption.ID
							//ElseIf optionbranch IsNot Nothing Then
							//    pth$ &= optionbranch.ID
							//Else
							//    Stop
							//End If

							//If validPaths.Contains(pth$) Then
							//    kkk += 1
							//Else
							//    validPaths.Add(pth$)
							//End If


							if (existingOption != null & optionRdr("l2") != "Processor") {
								//ADD NOTHING TO DO
								//ImportLog.Add(DateTime.Now, String.Format("Option exist: {0} )", rdr("optsku")))
								object a = 0;

								optionbranch = existingOption;

							//need to check here that the opttype is compatible with the sub families FamilyMem, FamilyPriStore etc

							// If existingOption.Product.i_Attributes_Code.ContainsKey("opttype") Then
							//optionbranch = New clsBranch(anOption, holderbranch, SKUTL, "", tlOption, tlOptions, Nothing, 0, False, "B", bwc, NEXTbId)
							//     For Each slot In 

							//Dim s = New clsSlot(iq.i_slotType_Code(rdr("opttype"))(rdr("optfamily")), optionbranch, "", -CInt(rdr("slots")), Nothing, New NullableInt(), 0, 0, swc)

							} else {
								//ADD TO ADD
								//ImportLog.Add(DateTime.Now, String.Format("Option Not found : {0} )", rdr("optsku")))
								string opttype = optionRdr.Item("Opttype");


								if (opttype.ToUpper.Trim == "CPU") {
									string cpusku = optionRdr("optsku");
									ImportLog.Add(DateTime.Now, string.Format("CPU Found SKU: {0}", cpusku));

									//CPU's are handled very differently - see import.cpus
									//(only the CPU preinstalled in the system is an option for it - and CPUs enable banks of memory etc)

									//Find CPU's branch (in the master list)
									clsBranch cpusbranch = mastercpusbranch;
									clsBranch cpubranch = cpusbranch.findChildBySKU2("tree." + cpusbranch.ID.ToString(), cpusku, outPath);


									if (cpubranch == null) {
										clsProduct cpuProd = iq.i_SKU(cpusku);
										clsTranslation skuTrans = iq.AddTranslation(cpusku, English, "SKU", 10, tlwc, nextkey, false);
										clsTranslation processor = iq.AddTranslation("Processor", English, "OL2", 0, tlwc, nextkey, false);
										clsTranslation processors = iq.AddTranslation("Processors", English, "OL2", 0, tlwc, nextkey, false);
										cpubranch = new clsBranch(cpuProd, cpusbranch, skuTrans, "", processors, processor, iq.Screens(719), 0, false, "B",
										bwc, NEXTbId);
									}

									object rws = powerDT.Select("mfrpartnum = '" + optionRdr("optsku") + "'");
									// If rws.Count > 1 Then Stop

									bool cpusPowerSlotFound = false;
									int count = 0;
									foreach ( cpuslot__2 in cpubranch.slots.Values) {
										if (cpuslot__2.Type.MajorCode == iq.i_slotType_Code("PWR")("W").MajorCode) {
											cpusPowerSlotFound = true;
											count += 1;
										}
									}

									if (cpusPowerSlotFound) {
										//duplicate slots
										if (count > 1) {
											ImportLog.Add(DateTime.Now, string.Format("Duplicate power slots found", optionRdr("optsku")));
										}
									} else {
										foreach ( rw in rws) {
											//Handles power consumptiuon (of the CPU)
											object s = new clsSlot(iq.i_slotType_Code("PWR")("W"), cpubranch, "", -(int)rw("powerMax"), null, new NullableInt(), 0, 0, swc);
										}
									}

									//Graft to relevant systems (find ALL the systems having this cpu) - we will cehck each system in in the importing systems list  in the loop
									object rdr2 = da.DBExecuteReader(con, "SELECT modelsku,familycode,cpu,cpuqty,[QtyInstalled],[QtyMax],qtymin,[Incr_Min],[Incr_Pref]  FROM h3.iq.products.systems inner join h3.iq.products.optionlimits sfd on sfd.sysfamily = systems.familycode where cpu='" + optionRdr.Item("optsku") + "' AND opttype='CPU'");
									while (rdr2.Read) {
										//Find system
										object aSystemSku = rdr2("modelsku");
										//Is this a system we're importing !
										if (systemSKUs.Contains(aSystemSku)) {

											if (iq.i_SKU.ContainsKey(aSystemSku)) {
												object sysProduct = iq.i_SKU(aSystemSku);
												if (sysProduct.Branches.Count == 1) {
													// this system will only appear once 
													foreach ( systemBranch__3 in sysProduct.Branches) {

														//It will only appear in one place
														foreach ( Path in systemBranch__3.AllPaths) {
															//Find our Processors
															object cpuPath = "";
															string systemPath = "";
															systemPath = Path;
															clsBranch g = systemBranch__3.FindBranchByNameBelow("Processor", systemPath, false, 10, cpuPath);

															object pn = PathName(cpuPath);

															if (g == null) {
																//Doesn't exist
																object AllOptionsSystem = systemBranch__3.FindBranchByNameBelow("System", Path, false, 10, cpuPath);
																object AlloptionsSystemProcessor = new clsBranch(null, AllOptionsSystem, proctrans, "", proctrans, procTransPlural, null, 0, false, "G",
																bwc, NEXTbId);
																cpuPath = cpuPath + "." + AlloptionsSystemProcessor.ID;
																g = AlloptionsSystemProcessor;
															}
															string chassisPath = "";
															clsBranch chassisB = null;

															foreach ( cb in systemBranch__3.childBranches) {
																if (cb.Value.Translation.text(English).Contains(" chassis")) {
																	chassisB = cb.Value;
																	chassisPath = Path + "." + chassisB.ID;
																	break; // TODO: might not be correct. Was : Exit For
																}
															}

															//Graft it on to chassis - Total red heerring ??
															string chassispathFound = cpuPath;
															//REMOVE 
															chassispathFound = chassisPath;
															//If chassisB IsNot Nothing AndAlso chassisB.findChildBySKU2(chassisPath, rdr("optsku"), chassispathFound) Is Nothing Then
															//chassisB.Graft(cpubranch, "CPUIncImport", cpuPath, ERRORMESSAGES, Gwc)
															if (!g.childBranches.ContainsKey(cpubranch.ID)) {
																g.Graft(cpubranch, "buildtreeinc", cpuPath, ERRORMESSAGES, Gwc);
															}
															//End If

															if (cpuPath.Contains(cpubranch.ID))
																System.Diagnostics.Debugger.Break();

															cpuPath = cpuPath + "." + cpubranch.ID;

															if (cpubranch.slots.Values.Where(sl => sl.Type.MajorCode == "CPU" && (sl.path == "" | sl.path == chassispathFound)).Count == 0) {
																object sl = new clsSlot(iq.i_slotType_Code("CPU")("GEN_CPU"), cpubranch, "", -1, null, new NullableInt(), rdr2("qtymin"), IsDBNull(rdr2("incr_pref")) ? null : rdr2("incr_pref"), swc);
															// Dim sl = New clsSlot(iq.i_slotType_Code(rdr("opttype"))(rdr("optfamily")), cpubranch, "", -1, Nothing, New NullableInt(), rdr2("qtymin"), If(IsDBNull(rdr2("incr_pref")), Nothing, rdr2("incr_pref")), swc)
															} else {
																bool genCpufound = false;
																foreach ( slot in cpubranch.slots.Values) {
																	string optfam = optionRdr.Item("optfamily");
																	string oty = optionRdr.Item("opttype");
																	if (slot.Type.MajorCode == oty & slot.Type.MinorCode == optfam) {
																		slot.deleted = true;
																		slot.update(ERRORMESSAGES);

																	} else if (slot.Type.MajorCode == "CPU" & slot.Type.MinorCode == "GEN_CPU") {
																		genCpufound = true;
																	}
																	if (slot.Type.MajorCode == "CPU" & genCpufound == false) {
																		object sl = new clsSlot(iq.i_slotType_Code("CPU")("GEN_CPU"), cpubranch, "", -1, null, new NullableInt(), rdr2("qtymin"), IsDBNull(rdr2("incr_pref")) ? null : rdr2("incr_pref"), swc);
																	}
																}

																clsSlot cpuSlot__4 = cpubranch.slots.Values.Where((sl => sl.Type.MajorCode == "CPU" && (sl.path == "" | sl.path == chassispathFound))).FirstOrDefault;
																if (cpuSlot__4.numSlots > 0) {
																	cpuSlot__4.numSlots = -1;
																	cpuSlot__4.update(ERRORMESSAGES);
																}
															}
															//Slot to the chassis?

															//Create limits, FIOs etc
															if (!IsDBNull(rdr2("cpuqty"))) {
																if (cpubranch.Quantities.Values.Where(q => q.NumPreInstalled == rdr2("cpuqty") && q.FOC == true & q.Path == cpuPath).Count == 0) {
																	object qty = new clsQuantity(iq.i_region_code("XW"), cpuPath, cpubranch, rdr2("cpuqty"), rdr2("qtymin"), IsDBNull(rdr2("incr_pref")) ? 0 : rdr2("incr_pref"), true, qwc);
																}
															}

															//If Not IsDBNull(rdr2("qtymax")) AndAlso rdr2("qtymax") > 1 Then

															//    'Dim chassisB = branch.Value.FindBranchByNameBelow("chassis", chassisPath, False, 10)
															//    If chassisB IsNot Nothing Then

															//        'The CPU (which exists in one 'global' place) - will give lots of different (minor) types of memory slot 
															//        ' at lots of different paths
															//        Dim memorymax As Integer = 0
															//        Dim foundmem As Boolean = False
															//        For Each slot In chassisB.slots.Values.ToArray
															//            If slot.Type.MajorCode = "MEM" Then 'locate the memory slots in the chassis.. 
															//                Dim newpath As String = cpuPath
															//                'slot.Update(cpuBranch, newpath)

															//                Dim sysMinorFamily As String = "" 'comes from the iq.systems.familycode
															//                If sysProduct.i_Attributes_Code.ContainsKey("FamMinor") Then sysMinorFamily = sysProduct.i_Attributes_Code("FamMinor")(0).Translation.text(English) 'IMPORTANT for compatibility
															//                If sysMinorFamily = "" Then Continue For
															//                For Each k In dicOptLimits.Keys
															//                    Dim bits() As String = Split(k, "^")
															//                    If LCase(bits(0)) = LCase(sysMinorFamily) Then
															//                        'for every narrow OptFamily in the sysfamily
															//                        Dim Limit As clsLimit = dicOptLimits(k)
															//                        If UCase(bits(1)) = "FAMILYMEM" Then Stop
															//                        optfamily = bits(2)
															//                        Dim opttypeMem As String = bits(1)

															//                        If opttypeMem = "MEM" Then
															//                            'code to update memory 
															//                            memorymax = Limit.Qmax
															//                        End If
															//                    End If
															//                Next

															//                ImportLog.Add(DateTime.Now, String.Format("Moving memory from chassis {0} to option SKU: {1}", chassisB.Translation.text(English), optionRdr("optsku")))

															//                Dim cpath As String = PathName(cpuPath)
															//                Dim alreadyThere = False
															//                For Each s In cpubranch.slots.Values
															//                    If s.Type.MajorCode = "MEM" Then
															//                        If (String.IsNullOrEmpty(s.path) OrElse s.path = cpuPath) Then
															//                            s.numSlots = memorymax
															//                            s.update(ERRORMESSAGES)
															//                            alreadyThere = True
															//                        End If
															//                    End If
															//                Next

															//                If Not alreadyThere Then
															//                    Dim cpuMemEnable As clsSlot = New clsSlot(slot.Type, cpubranch, cpuPath, memorymax, slot.notes, slot.slotNum, slot.requiredFill, slot.advisedFill, swc)
															//                End If

															//                slot.numSlots = memorymax
															//                slot.deleted = True
															//                slot.update(ERRORMESSAGES)
															//                foundmem = True

															//            ElseIf slot.Type.MajorCode = "CPU" Then
															//                If slot.Type.MinorCode <> "GEN_CPU" Then

															//                    Dim st As clsSlotType = iq.i_slotType_Code("CPU")("GEN_CPU")
															//                    Dim x = From z In chassisB.slots.Values Where z.Type.MajorCode = st.MajorCode And z.Type.MinorCode = st.MinorCode
															//                    If x.Count = 0 Then
															//                        slot.Type = st
															//                    Else
															//                        slot.deleted = True
															//                    End If
															//                    slot.update(ERRORMESSAGES)
															//                End If
															//            End If

															//        Next
															//        If Not foundmem Then
															//            'memory in cpu branch
															//            For Each s In cpubranch.slots.Values
															//                slotchanged = False
															//                If s.Type.MajorCode = "MEM" Then
															//                    If s.path = cpuPath Then
															//                        Dim sysMinorFamily As String = "" 'comes from the iq.systems.familycode
															//                        If sysProduct.i_Attributes_Code.ContainsKey("FamMinor") Then sysMinorFamily = sysProduct.i_Attributes_Code("FamMinor")(0).Translation.text(English) 'IMPORTANT for compatibility
															//                        If sysMinorFamily = "" Then Continue For

															//                        For Each k In dicOptLimits.Keys
															//                            Dim bits() As String = Split(k, "^")
															//                            If LCase(bits(0)) = LCase(sysMinorFamily) Then
															//                                'for every narrow OptFamily in the sysfamily
															//                                Dim Limit As clsLimit = dicOptLimits(k)
															//                                If UCase(bits(1)) = "FAMILYMEM" Then Stop
															//                                optfamily = bits(2)
															//                                Dim opttypeMem As String = bits(1)

															//                                If opttypeMem = "MEM" Then
															//                                    'code to update memory 
															//                                    memorymax = Limit.Qmax
															//                                End If
															//                            End If
															//                        Next
															//                        s.numSlots = memorymax
															//                        slotchanged = True
															//                    ElseIf String.IsNullOrEmpty(s.path) Then
															//                        s.deleted = True
															//                        slotchanged = True
															//                    Else
															//                        Dim slotPaths() = s.path.Split(".")
															//                        If slotPaths(slotPaths.Length - 1) <> cpubranch.ID Then
															//                            s.deleted = True
															//                            slotchanged = True
															//                        End If
															//                    End If
															//                    If slotchanged Then
															//                        s.update(ERRORMESSAGES)
															//                    End If
															//                End If
															//            Next
															//        End If
															//    End If
															//End If
														}
													}
												} else {
													ImportLog.Add(DateTime.Now, string.Format("Sys Product: {0}  has multiple branches", sysProduct.SKU));
												}
											}
										}
									}
									rdr2.Close();


									optionbranch = cpubranch;

								} else if (opttype.ToUpper.Trim != "WTY") {
									//Dont add warrenties
									//If ActionList.IsGo(rdr("optSKU"), ActionType.INSERT, ObjectType.Branch, holder, rdr("optSKU")) Then

									string optsku = optionRdr.Item("optSKU");
									clsProduct anOption = iq.i_SKU(optsku);

									clsTranslation SKUTL = iq.AddTranslation(anOption.SKU, English, "SKU", 10, tlwc, nextkey, false);


									optionbranch = new clsBranch(anOption, holderbranch, SKUTL, "", tlOption, tlOptions, null, 0, false, "B",
									bwc, NEXTbId);
									//The branch.translation is the part number (Points to the same TL)

									//SLOT!
									if (!IsDBNull(optionRdr("slots")) && (int)optionRdr("slots") > 0) {
										if (!iq.i_slotType_Code.ContainsKey(optionRdr("opttype")) || !iq.i_slotType_Code(optionRdr("opttype")).ContainsKey(optionRdr("optfamily"))) {
											object c = new clsSlotType(optionRdr("opttype"), optionRdr("optfamily"), iq.AddTranslation("", English, "slottype", 0, tlwc, nextkey, false));
										}
										object s = new clsSlot(iq.i_slotType_Code(optionRdr("opttype"))(optionRdr("optfamily")), optionbranch, "", -(int)optionRdr("slots"), null, new NullableInt(), 0, 0, swc);
									}

									//  Else
									//   ActionList.Add(rdr("optSKU"), ActionType.INSERT, ObjectType.Branch, holder, rdr("optSKU"))
									//  End If
								}
							}

							if (optionbranch != null && optionbranch.ID > 0) {
								//PowerSizing...
								object rws = powerDT.Select("mfrpartnum = '" + optionRdr("optsku") + "'");
								bool optsPowerSlotFound = false;
								foreach ( optslot in optionbranch.slots.Values) {
									if (optslot.Type.MajorCode == iq.i_slotType_Code("PWR")("W").MajorCode) {
										optsPowerSlotFound = true;
									}
								}

								if (!optsPowerSlotFound) {
									foreach ( rw in rws) {
										object s = new clsSlot(iq.i_slotType_Code("PWR")("W"), optionbranch, "", {
											"psu",
											"psum"
										}.Contains(optionbranch.Product.ProductType.Code) ? (int)rw("powerMax") : -(int)rw("powerMax"), null, new NullableInt(), 0, 0, swc);
									}
								}

								//SlotAdds
								string[] types = optionRdr.Item("slotaddType").ToString().Split(";".ToArray(), StringSplitOptions.RemoveEmptyEntries);
								string[] qtys = optionRdr.Item("slotaddqty").ToString().Split(";".ToArray(), StringSplitOptions.RemoveEmptyEntries);
								clsSlot aslot;
								clsSlotType st;

								// stdic contains a dictionary of option type > optionfamily - per systemfamilyCODE (minorFamilty)
								//eg              dl580pg825NHPlff > HDD > NHP35LFFSC


								foreach ( Path in optionbranch.AllPaths) {
									for (i = 0; i <= UBound(types); i++) {
										if (!iq.i_slotType_Code.ContainsKey(types(i))) {
											//THIS causes massive duplication (new clsslottype!)) - investigate !
											// If Not iq.i_slotType_Code(types(i)).ContainsKey(types(i)) Then
											st = new clsSlotType(types(i), types(i), iq.AddTranslation(types(i), English, "st", 0, tlwc, nextkey, false));
											// End If
										}

										clsBranch systemBranchAbove = FindSystemBranch(Path);
										clsProduct productAbove = null;
										string famAbove = "";
										if (systemBranchAbove == null) {
											Beep();

										} else {
											if (!systemBranchAbove.Product.i_Attributes_Code.ContainsKey("FamMajor")) {
												if (systemBranchAbove.Parent.Product == null) {
													famAbove = "";

												} else {

													clsProductAttribute fa = systemBranchAbove.Parent.Product.i_Attributes_Code("FamMajor").First;
													famAbove = fa.Translation.text(English);
													//create the missing family major attribute
													clsProductAttribute missingFa = new clsProductAttribute(systemBranchAbove.Product, fa.Attribute, fa.NumericValue, fa.Unit, fa.Translation);
												}

											} else {
												famAbove = systemBranchAbove.Product.i_Attributes_Code("FamMajor").First.Translation.text(English);
											}

											//                                        End If

											if (dicSlotTypes.ContainsKey(famAbove)) {
												if (stDic(famAbove).ContainsKey(types(i)) && iq.i_slotType_Code(types(i)).ContainsKey(stDic(famAbove)(types(i)))) {
													st = iq.i_slotType_Code(famAbove)(types(i));
													//Dim alreadythere = branch.slots.Where(Function(sl) sl.Value.Type Is st AndAlso Math.Sign(sl.Value.numSlots) <> Math.Sign(CInt(qtys(i))))
													//For Each s In alreadythere.ToList()
													//    s.Value.delete()
													//Next
													if (optionbranch.slots != null && optionbranch.slots.Where(sl => object.ReferenceEquals(sl.Value.Type, st) && Math.Sign(sl.Value.numSlots) == Math.Sign((int)qtys(i))).Count == 0) {
														aslot = new clsSlot(st, optionbranch, "", qtys(i), null, new NullableInt(), 0, 0, swc);
													}
												} else {
													if (iq.i_slotType_Code.ContainsKey(types(i))) {
														st = iq.i_slotType_Code(types(i)).First.Value;
														if (optionbranch.slots != null && optionbranch.slots.Where(sl => object.ReferenceEquals(sl.Value.Type, st)).Count == 0)
															aslot = new clsSlot(st, optionbranch, "", qtys(i), null, new NullableInt(), 0, 0, swc);
													}
												}
											}
										}
									}
								}
							}


							//only 'do' each family ONCE

							if (!famlist.Contains(famBranch.Translation.text(English))) {
								//Graft the all options branch onto every system, and make chassis slots

								famlist.Add(famBranch.Translation.text(English));
								//  Debug.WriteLine(famBranch.Translation.text(English))
								foreach ( systembranch__5 in famBranch.childBranches.Values.ToList) {
									if (systembranch__5.Product != null && systembranch__5.Product.SKU == "")
										System.Diagnostics.Debugger.Break();

									if (systembranch__5.Product == null)
										System.Diagnostics.Debugger.Break();

									//Do we need to add the All Options branch to this system or is it already there?
									object AOfound = false;
									clsBranch CBfound = null;

									List<clsBranch> dupSlotBranch = (from dup in systembranch__5.childBranches.Valueswhere dup.Product != null && dup.Product.ProductType.Code == "CHAS").ToList();
									if (dupSlotBranch.Count > 1) {
										foreach ( br in dupSlotBranch) {
											if (!br.Translation.text(English).Contains(" chassis")) {
												br.deleted = true;
												br.Update(ERRORMESSAGES);
											}
										}
									}

									foreach ( child in systembranch__5.childBranches.Values) {
										// Debug.WriteLine(child.Translation.text(English))
										if (child.Translation.text(English) == "All Options") {
											//we are not importing the family so this is ok...
											AOfound = true;
										}
										if (child.Translation.text(English).Contains(" chassis")) {
											CBfound = child;
										}
									}

									object systemsku = systembranch__5.Product.SKU;

									if (!AOfound) {
										//Need to find family all options branch..., do we need to check if this is a system we are already adding???
										object faob = famBranch.FindBranchByNameBelow("All Options", famPath, false, 8);
										// If ActionList.IsGo(rdr("optSKU"), ActionType.INSERT, ObjectType.Graft, systembranch, faob) Then
										systembranch__5.Graft(faob, "buildtreeInc", "", ERRORMESSAGES, Gwc);
										//Else
										//      ActionList.Add(rdr("optSKU"), ActionType.INSERT, ObjectType.Graft, systembranch, faob)
										//    End If
										//What if others are missing?  TRO's etc?
									}

									string sysMinorFamily = "";
									//comes from the iq.systems.familycode
									if (iq.i_SKU(systemsku).i_Attributes_Code.ContainsKey("FamMinor"))
										sysMinorFamily = iq.i_SKU(systemsku).i_Attributes_Code("FamMinor")(0).Translation.text(English);
									//IMPORTANT for compatibility
									if (sysMinorFamily == "")
										continue;

									//Do we have a chassis branch already?
									if (CBfound != null) {
										chassisBranch = CBfound;
									} else {
										if (JustDoit || ActionList.IsGo(optionRdr("optSKU"), ActionType.INSERT, ObjectType.Branch, systembranch__5, "Chassis Branch")) {
											// chassisProduct = New clsProduct("", False, True, iq.i_sector_code("HPPSG"), iq.i_ProductType_Code("CHAS"), DateAdd(DateInterval.Day, -500, Now), DateAdd(DateInterval.Day, 10000, Now), True, False, True, "", "", "")
											// chassisVariant = New clsVariant("", chassisProduct, HP, chassisProduct.ID.ToString, "", "", "", r_worldwide, 0) 'Every product needs a variant - so it can be stored in a QuoteItem
											//chassisBranch = New clsBranch(chassisProduct, Nothing, iq.AddTranslation(f & " chassis", English, "UI", 0, tlwc, nextkey, False), "", chassisTL, chassisTL, Nothing, 100, True, "B", bwc, NEXTbId)
											chassisBranch = new clsBranch(null, null, iq.AddTranslation(f + " chassis", English, "UI", 0, tlwc, nextkey, false), "", chassisTL, chassisTL, null, 100, true, "B",
											bwc, NEXTbId);
										}
									}

									//chassis branch needs to be per MinorFamily !!
									//Gives Slots

									clsSlot gslot;
									foreach ( k in dicOptLimits.Keys) {
										string[] bits = Split(k, "^");
										if (LCase(bits(0)) == LCase(sysMinorFamily)) {
											//for every narrow OptFamily in the sysfamily
											clsLimit Limit = dicOptLimits(k);
											if (UCase(bits(1)) == "FAMILYMEM") {
												//Beep()
												object zzz = 0;
											}


											string opttype = bits(1);
											//slot major
											optfamily = bits(2);
											//slot minor

											clsSlotType st = null;
											//make the slot TYPE on the fly if necessary
											if (iq.SlotTypes.Values.Where(sst => sst.MajorCode.ToUpper == opttype.ToUpper & sst.MinorCode.ToUpper == optfamily.ToUpper).Count > 0) {
												st = iq.SlotTypes.Values.Where(sst => sst.MajorCode.ToUpper == opttype.ToUpper & sst.MinorCode.ToUpper == optfamily.ToUpper).First;
											} else {
												clsTranslation slotname = iq.AddTranslation(opttype + " " + optfamily + " slot(s)", English, "slots", 0, tlwc, nextkey, false);
												st = new clsSlotType(opttype.ToUpper, optfamily.ToUpper, slotname);
											}
											//the gives stos do NOT need a path (systempath & "." & chassisBranch.ID) - becuase they are active weherever this subchassis appears

											if (!chassisBranch.hasSlot(st)) {
												gslot = new clsSlot(st, chassisBranch, "", Limit.Qmax, null, new NullableInt(), Limit.Qmin, 0, swc);
												//
												//MakeGivesSlots(chassisBranch, dicOptLimits(sysSubFamily), dicSlotTypes, dicSubFamOptTypeSlotType, QuantityWriteCache, SlotWriteCache)
											}
										}
									}

									//Add PCIs here!!!!
									//' removed for speed ath the moment      AddPCIChassisSlots(sysMinorFamily, f, systembranch, tlwc, nextkey, swc)

									systembranch__5.Graft(chassisBranch, "", "", ERRORMESSAGES, Gwc);
									ActionList.Add(optionRdr("optSKU"), ActionType.INSERT, ObjectType.Branch, systembranch__5, "Chassis Branch");


								}
							}
						}
					}
				} else {
					//What do we do if the option has no sysfamily?  This seems to be a valid setting for some FIO's
				}
				//  End If


			}

			optionRdr.Close();

			foreach ( slot in todel.ToList()) {
				slot.delete(ERRORMESSAGES);
			}

			ImportLog.Add(DateTime.Now, string.Format("Writing DB Changes"));

			da.BulkWrite(con, qwc, "quantity");
			da.BulkWrite(con, bwc, "Branch", , true);
			con.Close();
			con = da.OpenDatabase();
			da.BulkWrite(con, Gwc, "Graft");
			da.BulkWrite(con, tlwc, "translation");
			da.BulkWrite(con, swc, "slot");
			da.BulkWrite(con, pwc, "prune");



			bwc = da.MakeWriteCacheFor(con, "branch", NEXTbId, true);
			//nextID is SET by this call !
			Gwc = da.MakeWriteCacheFor(con, "GRAFT");
			qwc = da.MakeWriteCacheFor(con, "quantity");
			swc = da.MakeWriteCacheFor(con, "slot");
			pwc = da.MakeWriteCacheFor(con, "prune");

			nextkey = clsTranslation.NextKey;


			Debug.Print(jjj);
			Debug.Print(kkk);
			Debug.Print(lll);
			tlwc = da.MakeWriteCacheFor(con, "Translation");

			StreamWriter sw;
			//= New StreamWriter("c:\temp\validpaths.txt")
			sw = new StreamWriter("c:\\temp\\dudfamilies.txt");
			if (dudFamilies != null) {
				foreach ( l in dudFamilies) {
					sw.WriteLine(l);
				}
			}
			sw.Close();


			sw = new StreamWriter("c:\\temp\\dudoptions.txt");
			foreach ( l in dudoptions) {
				sw.WriteLine(l);
			}
			sw.Close();



		}


		ImportLog.Add(DateTime.Now, string.Format("Checking FIOS"));

		int cc;
		foreach ( sku in dicFIOs.Keys) {
			cc += 1;
			if (iq.i_SKU.ContainsKey(sku)) {
				foreach ( optionsku in dicFIOs(sku)) {

					if (optskus.Contains(optionsku.Key)) {
						ImportLog.Add(DateTime.Now, string.Format("Checking FIOS for system {0}, option {1} {2}/{3}", sku, optionsku.Key, cc, dicFIOs.Count));

						foreach ( br in iq.i_SKU(sku).Branches) {

							foreach ( p in br.AllPaths) {
								object resPath = "";

								string[] segs = Split(p, ".");
								int j = segs.Count;
								Array.Resize(ref segs, 21);
								int l = br.FastfindChildBySKU2(optionsku.Key, segs, j);
								//locate this FIO (under each (although there is only 1) occurance of the systemsku)
								clsBranch optp = null;


								if (l) {
									StringBuilder sb = new StringBuilder(100);
									for (i = 0; i <= l - 1; i++) {
										sb.Append(segs(i).ToString);
										if (i != l - 1)
											sb.Append(".");
									}
									optp = iq.Branches(segs(l - 1));
									resPath = sb.ToString;
								}

								// ''     Dim check As String = optp.Product.SKU

								// ''Scan here to make sure we dont have any dups, particalully under the FIO's
								//'If optp IsNot Nothing Then
								//'    Dim holderBranch = iq.Branches(Split(resPath, ".")(Split(resPath, ".").Count - 2))
								//'    Dim btodel = New List(Of clsBranch)
								//'    For Each child In holderBranch.childBranches.Values
								//'        If child.Product IsNot Nothing AndAlso child.Product.SKU = optionsku.Key AndAlso child.ID <> optp.ID Then
								//'            'prune it out of caution!
								//'            'Remove qty records
								//'            Dim todel2 = New List(Of clsQuantity)
								//'            For Each q In child.Quantities.Values
								//'                todel2.Add(q)
								//'            Next
								//'            For Each too In todel2
								//'                too.Delete(ERRORMESSAGES)
								//'            Next
								//'            Dim ptodel As List(Of clsPrune) = New List(Of clsPrune)()
								//'            For Each pr In child.Prunes.Values
								//'                ptodel.Add(pr)
								//'            Next
								//'            For Each ptd In ptodel
								//'                ptd.delete()
								//'            Next
								//'            Dim stodel As List(Of clsSlot) = New List(Of clsSlot)()
								//'            For Each pr In child.slots.Values
								//'                stodel.Add(pr)
								//'            Next
								//'            For Each std In stodel
								//'                std.delete(ERRORMESSAGES)
								//'            Next
								//'            Dim sqlc = "UPDATE QuoteItem SET FK_Branch_ID = " & optp.ID & " WHERE FK_Branch_ID = " & child.ID
								//'            da.DBExecutesql(con, sqlc)

								//'            btodel.Add(child)
								//'            'Dim pr = New clsPrune(resPath, New NullableInt(), "FIOPrune", Nothing)
								//'        End If
								//'    Next
								//'    For Each btd In btodel
								//'        '      btd.delete(ERRORMESSAGES)
								//'    Next
								//'End If

								foreach ( cb in br.childBranches.Values) {
									if (cb.Translation.text(English).Contains(" chassis")) {
										chassisBranch = cb;
										break; // TODO: might not be correct. Was : Exit For
									}
								}

								//If this option isnt there then create it under FIO's
								string fioPath = "";
								if (optp == null && iq.i_SKU.ContainsKey(optionsku.Key)) {
									object fioBranch__6 = br.FindBranchByNameBelow("FIOs", fioPath, false, 0);
									if (fioBranch__6 == null)
										fioBranch__6 = new clsBranch(null, br.FindBranchByNameBelow("All Options", fioPath, false, 0), iq.AddTranslation("FIOs", English, "", 0, tlwc, nextkey, false), "", iq.AddTranslation("FIOs", English, "", 0, null, 0, false), iq.AddTranslation("FIOs", English, "", 0, null, 0, false), null, 0, true, "B",
										bwc, NEXTbId);
									// What if AO branch doesnt exit???
									// If ActionList.IsGo(optsku, ActionType.INSERT, ObjectType.Branch, fioBranch, optsku) Or Inserting Then
									clsBranch branch = new clsBranch(iq.i_SKU(optionsku.Key), fioBranch__6, iq.AddTranslation(optionsku.Key, English, "", 0, tlwc, nextkey, false), "", tlOption, tlOptions, null, 0, false, "B",
									bwc, NEXTbId);
									optp = br.findChildBySKU2(p, optionsku.Key, resPath);
									//Else
									//    ActionList.Add(optsku, ActionType.INSERT, ObjectType.Branch, fioBranch, optsku)
									//   End If
								}
								if (!string.IsNullOrEmpty(resPath)) {
									object aa = PathName(resPath);
									//NB: Makelimits prunes off incompatible options !
									MakeLimits(p, optionsku.Key, Right(resPath, Len(resPath) - Len(p)), Gwc, swc, null, 0, qwc, false, dicOptLimits,
									dicSlotTypes, dicOptLocalisation, dicFIOs, sku, kept, pruned, chassisBranch, br, FamilyOptionDefs, null);
								} else {
									//This will only happen on a dummy run...
									isFIO(optionsku.Key, sku, p, dicFIOs, dicOptLocalisation, new clsLimit(0, 1, 100, 1, 1), qwc, null);
								}
							}

							// Next
						}
					}
				}
			}
		}


		ImportLog.Add(DateTime.Now, string.Format("Writing DB Changes"));

		da.BulkWrite(con, qwc, "quantity");
		da.BulkWrite(con, bwc, "Branch", , true);
		da.BulkWrite(con, Gwc, "Graft");
		da.BulkWrite(con, tlwc, "translation");
		da.BulkWrite(con, swc, "slot");
		da.BulkWrite(con, pwc, "prune");

		con.Close();




		//For Each l In validPaths
		//    sw.WriteLine(PathName(l))
		//Next
		//sw.Close()


		int todelete = 0;
		int tokeep = 0;
		HashSet<clsBranch> done = new HashSet<clsBranch>();

		//        LongSQL("update branch set deleted=1 where id in (" & Join(dellist.ToArray, ",") & ")")

	}

	public object TestFastFind()
	{



		object sku = "652749-z21";

		string p = "tree.1.5";
		object resPath = "";

		object t = Stopwatch.GetTimestamp;

		string[] segs = Split(p);
		int j = segs.Count;
		Array.Resize(ref segs, 21);
		int l = iq.Branches(5).FastfindChildBySKU2(sku, segs, j);
		if (l > 0) {
			clsBranch optp = iq.Branches(segs(l));
			resPath = Join(segs.Take(l).ToArray, ".");
		}

		double t1 = (Stopwatch.GetTimestamp - t) / Stopwatch.Frequency * 1000;

		p = "tree.1.5";
		t = Stopwatch.GetTimestamp;
		clsBranch b = iq.Branches(5).findChildBySKU2("tree.1.5", sku, p);
		double t2 = (Stopwatch.GetTimestamp - t) / Stopwatch.Frequency * 1000;

	}


	public List<string> optionsIncremental(SqlClient.SqlConnection con, List<string> addSkus, Dictionary<string, clsUnit> dicUnits, Dictionary<string, clsBranch> lDic, Dictionary<clsProduct, List<clsRegion>> dicOptLocalisation, clsActionList ActionList, bool AllowDelete)
	{

		//lDic is used to construct the L1/L2/L3 global options (accessories and services) catalogue

		//NB: in Iq1 a 'family' is a 'narrow/specific' designation - and type is broad (major type)
		//This *always* confuses me

		optionsIncremental = new List<string>();

		Dictionary<string, clsTranslation> dicSC = new Dictionary<string, clsTranslation>(StringComparer.CurrentCultureIgnoreCase);
		dicSC = new Dictionary<string, clsTranslation>();

		dicSC.Add("A", iq.AddTranslation("A", English, "SCC", 10, null, 0, false));
		dicSC.Add("TV", iq.AddTranslation("TV", English, "SCC", 20, null, 0, false));
		dicSC.Add("SB", iq.AddTranslation("SB", English, "SCC", 30, null, 0, false));
		dicSC.Add("R", iq.AddTranslation("R", English, "SCC", 30, null, 0, false));
		dicSC.Add("PR", iq.AddTranslation("PR", English, "SCC", 30, null, 0, false));
		dicSC.Add("GO", iq.AddTranslation("GO", English, "SCC", 30, null, 0, false));

		if (!iq.i_attribute_code.ContainsKey("Slots")) {
			clsAttribute sa = new clsAttribute("Slots", iq.AddTranslation("Slots used (legacy/import)", English, "", 0, null, 0, false), 0);
		}

		// LoadAbbreviations(con)
		object sql;

		sql = "SELECT v.OptSN,optsc,po.optsku,v.sortorder,fio,";
		sql += "case when po.sysfamily = ''  then isnull((select  sysfamilyname+', ' as 'data()' from h3.iq.products.systems inner join  h3.[iQ].[products].[SysFamilyDefinitions] on [SysFamilyDefinitions].sysfamily=systems.familycode  where PSU = po.optsku and opttype='PSU'  group by sysfamilyname FOR XML PATH('')),'') else po.sysfamily end as sysfamily,";
		sql += "speedUnitQty as speed,optTypeSpeedUnit as speedUnit,";
		sql += "unitQty as capacity,ot.optTypeUnit as capacityUnit,localisation,h.manuf7,";
		sql += "technology,altsku,incompatible,v.L1,v.L2,v.L3, po.optfamily,po.opttype,po.opttype2,activeFromDate,activeToDate,active,eol,aaonly,descriptionHP,slots,ccdescription,isnull(h.pl,'none') as pl ";
		sql += "FROM h3.iq.products.V2_OptionCats v ";
		sql += "JOIN h3.iq.products.options po ON v.optsn=po.optsn ";
		sql += "JOIN h3.[iq].products.optTypes as OT on OT.optTypeCode=optType ";
		sql += "JOIN h3.[channelcentral].products.Hierarchy h ON h.upcNUM = po.optsku ";
		sql += "WHERE po.optsku IN ('" + Join(addSkus.ToArray, "','") + "')";

		int nextBid = 0;
		int nextProdID = 0;
		int nextsId = 0;
		clsTranslation tlOptions = iq.AddTranslation("Options", English, "cats", 0, null, 0, false);
		clsTranslation tlOption = iq.AddTranslation("Option", English, "cats", 0, null, 0, false);

		//Write caches (for MUCH faster bulk writes)
		DataTable pawc = da.MakeWriteCacheFor(con, "ProductAttribute");
		DataTable bwc = da.MakeWriteCacheFor(con, "branch", nextBid, true);
		//nextID is SET by this call !

		DataTable twc = da.MakeWriteCacheFor(con, "Translation");
		DataTable pwc = da.MakeWriteCacheFor(con, "Product", nextProdID, true);
		DataTable swc = da.MakeWriteCacheFor(con, "Slot", nextsId, true);

		int nextKey = clsTranslation.NextKey();

		clsTranslation tlacs = iq.AddTranslation("Accessories and Services", English, "cat", 0, twc, nextKey, false);

		clsBranch allOptions = iq.RootBranch.FindBranchByNameBelow("Accessories and Services", "tree.1", false, 3);
		//New clsBranch(Nothing, iq.RootBranch, tlacs, "/images/iq/accSvcs.gif", tlOptions, tlOption, Nothing, 0, False, "B", bwc, nextBid)

		ImportLog.Add(DateTime.Now, string.Format("Querying IQ1 for option details"));

		SqlClient.SqlDataReader rdr;
		try {
			rdr = da.DBExecuteReader(con, sql);

			clsBranch l1Branch;
			clsBranch l2Branch;
			clsBranch l3Branch;
			clsBranch l4Branch;

			clsBranch addTo;

			int options = 0;

			while (rdr.Read) {
				ImportLog.Add(DateTime.Now, string.Format("Checking Option SKU: {0}", rdr("optsku")));

				optionsIncremental.Add(rdr("OptSku"));

				bool Inserting = true;
				if (iq.i_SKU.ContainsKey(rdr("OptSku"))) {
					Inserting = false;
				}


				if (Inserting) {
					string ck = rdr.Item("l1").trim;

					if (!lDic.ContainsKey(ck)) {
						l1Branch = new clsBranch(null, allOptions, iq.AddTranslation(rdr.Item("l1"), English, "OL1", 0, twc, nextKey, false), "", tlOptions, tlOption, null, rdr.Item("sortorder"), false, "B",
						bwc, nextBid);
						lDic.Add(ck, l1Branch);
					} else {
						l1Branch = lDic(ck);
					}

					addTo = null;

					ck = rdr.Item("l1").trim + "^" + rdr.Item("l2").trim;
					if (!lDic.ContainsKey(ck)) {
						l2Branch = new clsBranch(null, l1Branch, iq.AddTranslation(rdr.Item("l2"), English, "OL2", 0, twc, nextKey, false), "", tlOptions, tlOption, null, rdr.Item("sortorder"), false, "BGT",
						bwc, nextBid);
						lDic.Add(ck, l2Branch);
					} else {
						l2Branch = lDic(ck);
					}
					addTo = l2Branch;

					if (!object.ReferenceEquals(rdr.Item("l3"), DBNull.Value)) {
						ck = rdr.Item("l1").trim + "^" + rdr.Item("l2").trim + "^" + rdr.Item("l3").trim;
						if (!lDic.ContainsKey(ck)) {
							l3Branch = new clsBranch(null, l2Branch, iq.AddTranslation(rdr.Item("l3"), English, "OL3", 0, twc, nextKey, false), "", tlOptions, tlOption, null, rdr.Item("sortorder"), false, "BGT",
							bwc, nextBid);
							lDic.Add(ck, addTo);
						} else {
							l3Branch = lDic(ck);
						}
						addTo = l3Branch;
					}

					//optfamily is not globally unique... 5.25lff drives appear in optical and HDD
					object optfam = rdr.Item("optFamily");
					// is 'L4'

					string l3t = "";
					if (!object.ReferenceEquals(rdr.Item("l3"), DBNull.Value))
						l3t = rdr.Item("l3").trim.tolower;
					ck = rdr.Item("l1").trim + "^" + rdr.Item("l2").trim + "^" + l3t + "^" + optfam.trim;
					if (!lDic.ContainsKey(ck)) {
						object txt = "";
						if (dicAbbreviations.ContainsKey(optfam))
							txt = dicAbbreviations(optfam);
						else
							txt = Replace(txt, "_", " ");
						if (txt == null)
							txt = "";
						l4Branch = new clsBranch(null, addTo, iq.AddTranslation(txt, English, "OL4", 0, twc, nextKey, false), "", tlOptions, tlOption, null, rdr.Item("sortorder"), false, "G",
						bwc, nextBid);
						lDic.Add(ck, l4Branch);
					} else {
						l4Branch = lDic(ck);
					}
				}

				string otc = rdr.Item("opttype");
				//these are broad
				string otc2 = IsDBNull(rdr.Item("opttype2")) ? "" : rdr.Item("opttype2");
				//ML horrid but don't understand opttype2 and its causing data categorization issues for cables, even giving them a W value
				if (otc2 == "CAB")
					otc = "CAB";

				clsProduct OptionProduct = null;
				clsBranch optionbranch;

				if (iq.i_ProductType_Code.ContainsKey(otc)) {
					clsProductType pt = iq.i_ProductType_Code(otc);
					System.DateTime af = (System.DateTime)"01/01/1980";
					System.DateTime at = (System.DateTime)"01/01/2400";

					if (!IsDBNull(rdr.Item("activeFromDate")))
						af = rdr.Item("activeFromDate");
					if (!IsDBNull(rdr.Item("activeToDate")))
						at = rdr.Item("activeToDate");

					if (!iq.i_SKU.ContainsKey(rdr.Item("optsku"))) {
						OptionProduct = new clsProduct(rdr.Item("optsku"), false, true, iq.Sectors.Values(0), pt, af, at, rdr.Item("active"), rdr.Item("eol"), !rdr.Item("AAonly"),
						"", "", "", pwc, nextProdID);
					} else {
						OptionProduct = iq.i_SKU(rdr.Item("optsku"));
					}


					clsTranslation TLdesc = null;

					if (!IsDBNull(rdr.Item("ccDescription"))) {
						object dsc = rdr.Item("ccdescription");
						if (rdr.Item("ccDescription").tolower.contains("amd cpu"))
							System.Diagnostics.Debugger.Break();

						TLdesc = iq.AddTranslation(rdr.Item("ccdescription"), English, "OPTDSC", 0, twc, nextKey, false);

						if (rdr("opttype") == "CPU") {
							clsBranch cpuroot = null;
							foreach ( b in iq.Branches.Values) {
								if (b.Translation.text(English) == "CPU" && b.childBranches.Count > 30) {
									cpuroot = b;
									break; // TODO: might not be correct. Was : Exit For
								}
							}
							optionbranch = new clsBranch(OptionProduct, cpuroot, iq.AddTranslation(rdr.Item("optsku"), English, "OPTDSC", 0, twc, nextKey, false), "", tlOptions, tlOption, null, rdr.Item("sortorder"), false, "B",
							bwc, nextBid);
						} else {
							optionbranch = new clsBranch(OptionProduct, l4Branch, iq.AddTranslation(rdr.Item("optsku"), English, "OPTDSC", 0, twc, nextKey, false), "", tlOptions, tlOption, null, rdr.Item("sortorder"), false, "B",
							bwc, nextBid);
						}

						addOptionAttributesInc(OptionProduct, pawc, twc, nextKey, rdr, new Dictionary<string, string> { {
							rdr("optsku"),
							rdr("pl")
						} }, dicUnits, TLdesc, Inserting);
					} else {
						Logit("Missing description");
					}
				} else {
					Logit("Missing opttype:" + otc);
				}
				//Supply Chain Focus Attribute
				if (!IsDBNull(rdr.Item("optSC"))) {
					string optsc = Trim(rdr.Item("optsc"));
					if (optsc != "" & optsc != "Z") {
						clsProductAttribute SCfa = new clsProductAttribute(OptionProduct, iq.i_attribute_code("focus"), 0, iq.i_unit_code("txt"), dicSC(optsc), pawc);
					}
				}

				if (!Inserting) {
					CompareProduct(iq.i_SKU(OptionProduct.SKU), OptionProduct, true, ActionList, true, pawc);
				}

				//systypefocus attribute

				options += 1;

				//Populate the Dictionary of option localisations (Countries in which it's active) - which is used later in BuildTree
				//we DO NOT add options which are not localised to the dicoptlocalisation dictionary (they are unrestricted)

				//need to work out what to do while editing...
				if (Inserting) {
					string rgns = "";
					if (!IsDBNull(rdr.Item("localisation")))
						rgns = rdr.Item("localisation");

					if (rdr.Item("aaonly") != 0) {
						rgns += ",AA";
					}

					if (rgns != "") {

						if (OptionProduct != null) {
							List<clsRegion> regions = new List<clsRegion>();
							List<string> cs = Split(rgns, ",").ToList;

							//Anything paul has localised 'worldwide' needs no restriction
							if (!cs.Contains("XW")) {

								cleanRegions(cs, new Dictionary<string, List<string>>());

								foreach ( c in cs) {
									if (c == "UCSA")
										c = "USCA";
									//fix a typo
									if (iq.i_region_code.ContainsKey(c)) {
										regions.Add(iq.i_region_code(c));
									} else {
										Logit("invalid region " + c + " (in products.options.localisation)");
										//    Stop
									}
								}
								dicOptLocalisation.Add(OptionProduct, regions);
							}
						}
					}
				}
			}


		} catch (Exception ex) {
			optionsIncremental = null;

		} finally {
			rdr.Close();

			ImportLog.Add(DateTime.Now, string.Format("Writing Option Changes"));

			da.BulkWrite(con, twc, "translation", , true);
			da.BulkWrite(con, pwc, "product", , true);
			da.BulkWrite(con, bwc, "branch", , true);
			da.BulkWrite(con, pawc, "productattribute");
			da.BulkWrite(con, swc, "slot");

			con.Close();
		}

	}


	public object addOptionAttributesInc(clsProduct optionProduct, DataTable pawc, DataTable twc, ref int nextKey, SqlClient.SqlDataReader rdr, Dictionary<string, string> dicplcode, Dictionary<string, clsUnit> dicunits, clsTranslation tldesc, bool Inserting)
	{

		clsProductAttribute incompatible;
		clsProductAttribute altsku;
		clsProductAttribute anAttribute;
		clsProductAttribute mfrsku;
		clsProductAttribute plcode;


		// Dim ssde As clsTranslation = iq.AddTranslation("Solid State Drive", English, "DriveType")
		clsUnit textUnit = iq.i_unit_code("txt");
		if (textUnit == null)
			System.Diagnostics.Debugger.Break();


		clsProductAttribute desc = new clsProductAttribute(optionProduct, iq.i_attribute_code("desc"), 0, textUnit, tldesc, pawc, !Inserting);

		//record the options OptFamily - this is the MinorOption type - but isn't globally unique..
		//eg. HPL35inchLFF may appear under oth OPT and HDD opt types
		anAttribute = new clsProductAttribute(optionProduct, iq.i_attribute_code("optFamily"), 0, textUnit, iq.AddTranslation(rdr.Item("optfamily"), English, "", 0, twc, nextKey, false), pawc, !Inserting);

		//This IS used in the quote summary (amongst other places)

		if (Len(rdr.Item("opttype")) > 5)
			System.Diagnostics.Debugger.Break();
		anAttribute = new clsProductAttribute(optionProduct, iq.i_attribute_code("optType"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("optType"), English, "", 0, twc, nextKey, false), pawc, !Inserting);

		//If Not iq.Attributes.ContainsKey("MfrSKU") Then j = New clsAttribute("MfrSKU", New clsText(iq.addTranslation("MfrSKU")))

		clsProductAttribute speed;
		clsProductAttribute capacity;
		if (!IsDBNull(rdr.Item("speed"))) {
			//Some things (tape drives/Graphics cards/batteries have 'speeds' without units - we're not imprtiong - mentioned to dan 02/08/2012
			if (!IsDBNull(rdr.Item("speedunit"))) {
				speed = new clsProductAttribute(optionProduct, iq.i_attribute_code("speed"), rdr.Item("speed"), dicunits(rdr.Item("speedUnit")), null, pawc, !Inserting);
			}
		} else {
			if (rdr.Item("Opttype") == "HDD") {
				//HDD's without a speed are SSD's - give them a numerically high RPM (so they sort to the 'top' speed wise - but display the text SSD (instead of 100,000 rpm)
				clsTranslation ssd = iq.AddTranslation("SSD", English, "DriveType", 0, twc, nextKey, false);
				speed = new clsProductAttribute(optionProduct, iq.i_attribute_code("speed"), 100000, iq.i_unit_code("txt"), ssd, pawc, !Inserting);
			}
		}


		if (!IsDBNull(rdr.Item("capacity"))) {
			object uk;
			//'Some things (cables,newtork cards have  capacities without units - we're not importing - mentioned to dan 02/08/2012 - am now... with a TXT unit
			if (!IsDBNull(rdr.Item("capacityUnit"))) {
				uk = rdr.Item("capacityUnit");
			} else {
				uk = "txt";
			}

			capacity = new clsProductAttribute(optionProduct, iq.i_attribute_code("capacity"), rdr.Item("capacity"), iq.i_unit_code(uk), null, pawc, !Inserting);

		}


		if (!IsDBNull(rdr.Item("opttype2"))) {
			object ot2 = new clsProductAttribute(optionProduct, iq.i_attribute_code("opttype2"), 0, textUnit, iq.AddTranslation(rdr.Item("opttype2"), English, "", 0, twc, nextKey, false), pawc, !Inserting);
		}

		string optsku = rdr.Item("optsku");

		if (!IsDBNull(rdr.Item("technology"))) {
			object t = rdr.Item("technology");
			int cp;
			cp = InStr(t, "CORE");
			int numcores;
			if (cp) {
				numcores = Val(Left(t, cp - 1));
				//  If numcores = 3 Or numcores = 5 Or numcores = 7 Or numcores > 16 Then Stop 'odd number of cores
				clsProductAttribute cores = new clsProductAttribute(optionProduct, iq.i_attribute_code("cores"), numcores, iq.i_unit_code("num"), null, pawc, !Inserting);

				int numthreads;
				cp = InStr(t, "TH");
				if (cp) {
					numthreads = Val(Mid(t, cp - 2, 2));
					clsProductAttribute threads = new clsProductAttribute(optionProduct, iq.i_attribute_code("threads"), numthreads, iq.i_unit_code("num"), null, pawc, !Inserting);
				}
			}
		}

		//mfrsku = New clsProductAttribute(optionProduct, iq.i_attribute_code("MfrSKU"), 0, textUnit, iq.AddTranslation(Trim$(rdr.Item("OptSKU")), English, "", 0, twc, nextKey, False), pawc, Not Inserting)
		object pl;
		//iq.i_SKU.Add(Trim$(rdr.Item("OptSKU")), optionProduct)

		if (!dicplcode.ContainsKey(rdr.Item("optSKU"))) {
			Logit("No PL code for option '" + rdr.Item("Optsku") + "' (not in HeirarchyIQ).");
		} else {
			pl = dicplcode(rdr.Item("optSKU"));
			plcode = new clsProductAttribute(optionProduct, iq.i_attribute_code("PLcode"), 0, textUnit, iq.AddTranslation(pl, English, "", 0, twc, nextKey, false), pawc, !Inserting);
		}

		//Dim opttype As clsProductAttribute
		//Dim opt$
		//opt$ = rdr.Item("OptType")
		//opttype = New clsProductAttribute(optionproduct, iq.Attributes("OptType"), 0, iq.Units("txt"), iq.addTranslation(opt, twc).Key, awc)
		//End If

		if (!IsDBNull(rdr.Item("incompatible"))) {
			if (Trim(rdr.Item("incompatible")) != "") {
				object ic = Replace(rdr.Item("incompatible"), " ", "");
				incompatible = new clsProductAttribute(optionProduct, iq.i_attribute_code("incompat"), 0, textUnit, iq.AddTranslation(ic, English, "incompat", 0, twc, nextKey, false), pawc, !Inserting);
			}
		}

		if (!IsDBNull(rdr.Item("altsku"))) {
			if (Trim(rdr.Item("altsku")) != "") {
				altsku = new clsProductAttribute(optionProduct, iq.i_attribute_code("altSKU"), 0, textUnit, iq.AddTranslation(Replace(rdr.Item("altSKU"), " ", ""), English, "atSKU", 0, twc, nextKey, false), pawc, !Inserting);
			}
		}

		//required later when making 'takes' slots - to respect iq.products.options.slots
		clsProductAttribute slots = new clsProductAttribute(optionProduct, iq.i_attribute_code("Slots"), rdr.Item("slots"), iq.i_unit_code("num"), null, pawc, !Inserting);
		//Dont do this for PSU enablement kits, they dont take a PSU slot....
		if (!IsDBNull(rdr.Item("technology")) && rdr.Item("technology") == "UPGRADE") {
			slots.NumericValue = 0;
		}

		if (!IsDBNull(rdr.Item("technology"))) {
			object tech = new clsProductAttribute(optionProduct, iq.i_attribute_code("technology"), 0, textUnit, iq.AddTranslation(Replace(rdr.Item("technology"), " ", ""), English, "", 0, twc, nextKey, false), pawc, !Inserting);
		}

		if (!IsDBNull(rdr.Item("fio")) && rdr.Item("fio") != 0) {
			object tech = new clsProductAttribute(optionProduct, iq.i_attribute_code("focus"), 1, textUnit, iq.AddTranslation("FIO", English, "Foci", 0, twc, nextKey, false), pawc, !Inserting);
		}


		clsAttribute ofa;
		if (!iq.i_attribute_code.ContainsKey("optFam")) {
			ofa = new clsAttribute("optFam", iq.AddTranslation("Options family", English, "", 0, twc, nextKey, false), 0);
		} else {
			ofa = iq.i_attribute_code("optFam");
		}

		object ofm = rdr.Item("OptFamily");
		clsProductAttribute optfam = new clsProductAttribute(optionProduct, ofa, 0, textUnit, iq.AddTranslation(ofm, English, "", 0, twc, nextKey, false), pawc, !Inserting);

		if (!IsDBNull(rdr("manuf7"))) {
			object refAttr = new clsProductAttribute(optionProduct, iq.i_attribute_code("ProdRef"), 0, textUnit, iq.AddTranslation(rdr("manuf7"), English, "", 0, twc, nextKey, false), pawc, !Inserting);
		}

	}


	/// <summary>
	/// Imports families incrementally
	/// </summary>
	/// <returns>Returns a dictionary of Dans SysFamilyName to The family Branch I create for it</returns>
	/// <remarks></remarks>
	public Dictionary<string, string> FamiliesInc(SqlClient.SqlConnection con, string SKUs)
	{


		SqlClient.SqlDataReader rdr;

		if (!iq.i_attribute_code.ContainsKey("bays"))
			clsAttribute ba = new clsAttribute("bays", iq.AddTranslation("Drive bays", English, "attribs", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("HPL"))
			clsAttribute hpa = new clsAttribute("HPL", iq.AddTranslation("Hot Pluggable", English, "attribs", 0, null, 0, false), 0);


		//The family branches can only carry the Major' fore
		clsAttribute fMaj = iq.i_attribute_code("FamMajor");
		clsAttribute fMin = iq.i_attribute_code("FamMinor");
		clsAttribute fDisp = iq.i_attribute_code("FamDisp");

		//the Unabbreviated family name is the BRANCH.Translation


		clsTranslation lff = iq.AddTranslation("LFF", English, "bays", 0, null, 0, 0);
		clsTranslation lffL = iq.AddTranslation("Large form factor (3.5 inch) drive bays ", English, "bays", 0, null, 0, false);
		// Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

		clsTranslation sff = iq.AddTranslation("SFF", English, "bays", 0, null, 0, false);
		clsTranslation sffL = iq.AddTranslation("Small Form Factor (2.5 inch) drive bays ", English, "bays", 0, null, 0, false);
		// Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

		clsTranslation bff = iq.AddTranslation("Both", English, "bays", 0, null, 0, false);
		clsTranslation bffL = iq.AddTranslation("Has both Small Form Factor (2.5 inch) and Large Form Factor (3.5 inch) drive bays ", English, "bays", 0, null, 0, false);
		// Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

		clsTranslation HPL = iq.AddTranslation("HP", English, "bays", 0, null, 0, false);
		clsTranslation HPLL = iq.AddTranslation("Hot Pluggable", English, "bays", 0, null, 0, false);
		// Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

		object sql;
		sql = "SELECT DISTINCT sysfamilyname,systype,lifeCycleMonths,managementTxt,SecurityTxt,RangeText,subTitle,FamilyPriStor,FamilySecStor ";
		sql += "from " + server + "[iq].products.union_sysfamilydefinitions right join " + server + "[iq].products.sysrangetext ON sysfamilyname=rangename";
		sql += " INNER JOIN h3.iq.products.systems ON systems.familycode=sysfamily WHERE modelsku IN (" + SKUs + ")";
		rdr = da.DBExecuteReader(con, sql);

		clsProduct product;
		// family branches need a product to attach additional attributes to (primarly descriptions)

		clsProductAttribute pa;

		clsTranslation sysTrans = iq.AddTranslation("systems", English, "collect", 0, null, 0, false);
		clsTranslation sysTransSingular = iq.AddTranslation("system", English, "collect", 0, null, 0, false);

		clsProductAttribute fnpa;


		//find the existing families - check and fix them here



		//get a dictionary of the locations of all family branches by their fammajor attributes
		Dictionary<string, string> famlocs = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
		RootBranch.FindFamilyBranchesBelow("tree.1", 4, famlocs);

		List<string> errormessages = new List<string>();

		clsBranch FamBranch;
		if (rdr.HasRows) {

			while (rdr.Read) {
				if (!IsDBNull(rdr.Item("sysfamilyname"))) {
					string famMajor = rdr.Item("sysfamilyname");
					if (famlocs.ContainsKey(famMajor)) {
						FamBranch = iq.Branches(Split(famlocs(famMajor), ".").Last);
						if (FamBranch.Product == null)
							System.Diagnostics.Debugger.Break();
						if (FamBranch.Product.SKU != "")
							System.Diagnostics.Debugger.Break();


					} else {
						//If Not iq.EnglishIndex(rdr.Item("sysfamilyname"), "FamMajor") Is Nothing Then

						ImportLog.Add(DateTime.Now, string.Format("Creating Family" + rdr.Item("sysfamilyname")));

						//this is a 'virtual' product on the family branch = to hold a number of pan-family attributes - and the family image
						string st = rdr.Item("systype");
						if (!iq.i_ProductType_Code.ContainsKey(st))
							clsProductType nst = new clsProductType(st, iq.AddTranslation(st, English, "", 0, null, 0, false), 0);
						product = new clsProduct("", false, false, iq.i_sector_code("NoSector"), iq.i_ProductType_Code(st), (System.DateTime)"01/01/2000", (System.DateTime)"31/12/2100", true, false, true,
						"", "", "");

						//record the family name under the 'majorFamily'  attribute on the branch - required for suppressing/displaying notes by family - see import.ExtText
						fnpa = new clsProductAttribute(product, fMaj, 0, iq.i_unit_code("txt"), iq.AddTranslation(Trim(rdr.Item("sysfamilyname")), English, "FamMajor", 0, null, 0, false));

						if (!object.ReferenceEquals(rdr.Item("lifecyclemonths"), DBNull.Value)) {
							pa = new clsProductAttribute(product, iq.i_attribute_code("lifeCycle"), rdr.Item("lifecyclemonths"), iq.i_unit_code("num"), iq.AddTranslation(rdr.Item("lifecyclemonths"), English, "", 0, null, 0, false));
						}

						if (!object.ReferenceEquals(rdr.Item("managementTxt"), DBNull.Value)) {
							pa = new clsProductAttribute(product, iq.i_attribute_code("management"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("managementTxt"), English, "", 0, null, 0, false));
						}

						if (!object.ReferenceEquals(rdr.Item("securityTxt"), DBNull.Value)) {
							pa = new clsProductAttribute(product, iq.i_attribute_code("security"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("securityTxt"), English, "", 0, null, 0, false));
						}

						if (!object.ReferenceEquals(rdr.Item("rangeText"), DBNull.Value)) {
							pa = new clsProductAttribute(product, iq.i_attribute_code("desc"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("rangeText"), English, "", 0, null, 0, false));
						}

						if (!object.ReferenceEquals(rdr.Item("subTitle"), DBNull.Value)) {
							pa = new clsProductAttribute(product, iq.i_attribute_code("subTitle"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("subTitle"), English, "", 0, null, 0, false));
						}

						//Large/small form factor dirve bays
						int bays = 0;
						//1=sff 2 = lff 3 = both

						if (!IsDBNull(rdr.Item("FamilyPriStor"))) {
							if (InStr(UCase(rdr.Item("FamilyPriStor")), "LFF")) {
								bays = bays | 2;
							}
							if (InStr(UCase(rdr.Item("FamilyPriStor")), "SFF")) {
								bays = bays | 1;
							}

							clsTranslation baytran = null;
							if (bays == 1)
								baytran = sff;
							if (bays == 2)
								baytran = lff;
							if (bays == 3)
								baytran = bff;
							// both form factors

							pa = new clsProductAttribute(product, iq.i_attribute_code("bays"), bays, iq.i_unit_code("txt"), baytran);

							if (InStr(UCase(rdr.Item("FamilyPriStor")), "HP") & InStr(UCase(rdr.Item("FamilyPriStor")), "NHP") == 0) {
								pa = new clsProductAttribute(product, iq.i_attribute_code("HPL"), 1, iq.i_unit_code("txt"), HPL);
							}
						}


						string code = rdr.Item("sysFamilyName");
						string FnEn;
						if (dicAbbreviations.ContainsKey(code.ToLower)) {
							FnEn = dicAbbreviations(code.ToLower);
							//'xlate()("en")
						} else {
							FnEn = code;
							Logit("no abbreviation for " + code);
						}

						clsTranslation fntl;
						//If iq.EnglishIndex.ContainsKey(FnEn) Then 'this is the abbreviation/key   - we do not append the word "family" (dans choice)
						// fntl = iq.EnglishIndex(FnEn)
						//Else
						fntl = iq.AddTranslation(FnEn, English, "", 0, null, 0, false);
						//               End If
						//
						clsBranch stBranch = null;
						foreach ( cb in iq.Branches.Values) {
							if (cb.Picture != null && cb.Picture.Contains(rdr.Item("systype"))){stBranch = cb;break; // TODO: might not be correct. Was : Exit For
}
							//    If cb.ID > 100 Then Stop
						}

						if (stBranch == null) {
							//Panic?!!
							stBranch = new clsBranch(null, iq.RootBranch, iq.AddTranslation(rdr.Item("systype"), English, "ST", 50, null, 0, 0), "/images/iq/prod_range_" + rdr.Item("systype") + ".jpg", sysTrans, sysTransSingular, iq.Screens(719), 100, false, "S",
							null, 0);
							//Stop
						}

						if (stBranch.Product != null && stBranch.Product.SKU != "")
							System.Diagnostics.Debugger.Break();
						if (product.i_Attributes_Code.ContainsKey("mfrsku"))
							System.Diagnostics.Debugger.Break();
						//  If SKUs <> "" Then Stop
						FamBranch = new clsBranch(product, stBranch, fntl, "/images/iq/prod_" + rdr.Item("sysfamilyname") + ".gif", sysTrans, sysTransSingular, null, 100, false, "B");

						//add the family under its systype branch (Servers, Notebooks, desktops, storage etc)
						// - NO need - it's done internall now dicSysTypes(rdr.Item("systype")).childBranches.Add(FamBranch.ID, FamBranch)

					}
				}
			}
		} else {
			ImportLog.Add(DateTime.Now, string.Format("No families affected"));
		}

		rdr.Close();


		famlocs = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
		RootBranch.FindFamilyBranchesBelow("tree.1", 4, famlocs);

		return famlocs;


	}



	// Can enable this for memory if needed, enforces a minor slot type against system slot types and prunes 'incompatable' ones
	public void PruneOffNonCompatableFamilyMinorSLotTypes()
	{
		object con = da.OpenDatabase(false);
		object sql;
		sql = "SELECT SysFamilyDefinitions.systype,SysFamilyDefinitions.sysfamily,sysfamilyname,familymem,familypristor,familysecstor,familyterstor FROM  h3.iQ.products.SysFamilyDefinitions";
		object dt = dataAccess.da.FilledDataTable(con, sql);

		sql = "SELECT  optsku from h3.iQ.products.options  where  opttype='HDD'";

		SqlClient.SqlDataReader rdr;
		rdr = da.DBExecuteReader(con, sql);
		while (rdr.Read) {
			if (iq.i_SKU.ContainsKey(rdr("optsku").ToString)) {
				object prod = iq.i_SKU(rdr("optsku").ToString);
				if (prod.SKU == "652605-B21") {
					object g = 8;
				}
				if (prod.hasSKU) {
					foreach ( branch in prod.Branches) {
						if (branch.Product != null && branch.Product.ProductType.Code.ToUpper == "HDD") {
							object hddslots = branch.slots.Where(sl => sl.Value.Type.MajorCode == "HDD" && sl.Value.numSlots < 0);
							if (hddslots.Count > 0) {
								foreach ( path in branch.AllPaths) {
									List<string> sto = new List<string>();
									object newpath = "";
									object fam = branch.FindSystemAbove(path, newpath);
									if (fam != null) {
										object rw = dt.Select("SysFamily='" + fam.Product.i_Attributes_Code("FamMinor").First.Translation.text(English) + "'");

										if (rw.Length > 0) {

											if (!IsDBNull(rw(0)("familypristor")))
												sto.Add(rw(0)("familypristor").ToString.ToUpper);
											if (!IsDBNull(rw(0)("familysecstor")))
												sto.Add(rw(0)("familysecstor").ToString.ToUpper);
											if (!IsDBNull(rw(0)("familyterstor")))
												sto.Add(rw(0)("familyterstor").ToString.ToUpper);


											if (hddslots.Where(hd => sto.Contains(hd.Value.Type.MinorCode.ToUpper)).Count == 0) {
												object p = new clsPrune(path, new NullableInt(), "MinorCodeComp");
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}

	public void EnableSASLicense()
	{
		foreach ( prod in iq.Products.Values) {
			if ({
				"BC393A",
				"BC393AAE",
				"BC393B"
			}.Contains(prod.SKU)) {
				object f = new clsProductAttribute(prod, iq.i_attribute_code("supportSAS"), 1, iq.i_unit_code("num"), null);
			}
			if (prod.i_Attributes_Code.ContainsKey("desc") && prod.i_Attributes_Code("desc").First.Translation.text(English).StartsWith("HP Dynamic Smart Array B320i")) {
				foreach ( branch in prod.Branches) {
					object s = new clsSlot(iq.i_slotType_Code("MAN3").First.Value, branch, "", 1, null, new NullableInt(), 0, 0);
				}
			}
		}
	}



	public void preInstalledParts()
	{
		SqlClient.SqlConnection con = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;");
		SqlClient.SqlDataReader rdr;


		SqlClient.SqlConnection oCon = da.OpenDatabase();
		DataTable qwc = da.MakeWriteCacheFor(oCon, "Quantity");

		int parts = 0;
		int dupes = 0;

		string query = "SELECT  pp.SysSN,pp.optsn,[OptQty],s.HierDescription as sysDesc,s.ModelSKU,o.DescriptionGen as optDesc,o.Optsku,o.OptType,o.OptType2 ";
		query += " FROM  [iQ].[products].[Systems_PreInstalledParts] pp  join products.systems as s on s.syssn=pp.syssn  join products.options as o on o.OptSN=pp.OptSN";

		string systemSku = string.Empty;
		string optionSku = string.Empty;
		rdr = da.DBExecuteReader(con, query);


		Dictionary<string, List<string>> systemLocations = new Dictionary<string, List<string>>(StringComparer.CurrentCultureIgnoreCase);
		//SKU>Path
		iq.RootBranch.SkuPaths(systemLocations, "tree.1", false);
		while (rdr.Read) {
			if (!object.ReferenceEquals(rdr.Item("ModelSKU"), DBNull.Value)) {
				systemSku = rdr.Item("ModelSKU").ToString();


				if (systemLocations.ContainsKey(systemSku)) {
					if (systemLocations(systemSku).Count > 1)
						System.Diagnostics.Debugger.Break();
					//System appears in more than one place !
					string systemTreePath = systemLocations(systemSku)(0);
					clsBranch systemBranch = iq.Branches(Split(systemTreePath, ".").Last);
					Dictionary<string, List<string>> optLocations = new Dictionary<string, List<string>>(StringComparer.CurrentCultureIgnoreCase);

					//index the options
					systemBranch.SkuPaths(optLocations, "", true);
					string optSku = rdr.Item("Optsku").ToString();

					//   If systemSku = "728546-421" And optSku = "732411-B21" Then Stop

					if (optLocations.ContainsKey(optSku)) {
						if (optLocations(optSku).Count > 1)
							System.Diagnostics.Debugger.Break();
						clsBranch branch = iq.Branches(Split(optLocations(optSku)(0), ".").Last);
						string fullpath = systemLocations(systemSku)(0) + optLocations(optSku)(0);
						string whereItsAt = PathName(fullpath);

						string ck = branch.ID + "^" + r_worldwide.ID + "^" + fullpath;
						if (i_Quantities.Contains(ck)) {
							NoOp();
							dupes += 1;
						} else {
							int qty = (int)rdr("OptQty");
							clsQuantity aQty = new clsQuantity(r_worldwide, fullpath, branch, qty, 1, 1, true, qwc);
							parts += 1;
						}
					}
				}
			} else {
				System.Diagnostics.Debugger.Break();
				//WTF ?
			}
		}
		rdr.Close();

		da.BulkWrite(oCon, qwc, "Quantity");

		con.Close();

	}

	//Public Sub OtherPreInstalled()

	//    Dim con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;")
	//    Dim rdr As SqlClient.SqlDataReader


	//    Dim oCon As SqlClient.SqlConnection = da.OpenDatabase()
	//    Dim qwc As DataTable = da.MakeWriteCacheFor(oCon, "Quantity")

	//    Dim parts As Integer = 0
	//    Dim dupes As Integer = 0

	//    Dim query As String = "select nic,display,raid,modelsku from h3.iq.products.systems"
	//    Dim systemSku As String = String.Empty
	//    Dim optionSku As String = String.Empty
	//    rdr = da.DBExecuteReader(con, query)
	//    Dim SkuLocations As Dictionary(Of String, String) = New Dictionary(Of String, String)  'SKU>Path
	//    iq.RootBranch.SkuPaths(SkuLocations, "Tree.1", False)
	//    While rdr.Read
	//        If rdr.Item("ModelSKU") IsNot DBNull.Value Then
	//            systemSku = rdr.Item("ModelSKU").ToString()
	//            If SkuLocations.ContainsKey(systemSku) Then

	//                Dim systemTreePath As String = SkuLocations(systemSku)
	//                Dim systemBranch As clsBranch = iq.Branches(Split(systemTreePath, ".").Last)
	//                Dim optLocations As Dictionary(Of String, String) = New Dictionary(Of String, String)
	//                systemBranch.SkuPaths(optLocations, Split(systemTreePath, ".").Last, True)
	//                Dim optSku As String = rdr.Item("NIC").ToString()
	//                If optLocations.ContainsKey(optSku) Then
	//                    Dim optTreePath As String = optLocations(optSku)
	//                    Dim branch As clsBranch = iq.Branches(Split(optTreePath, ".").Last)
	//                    Dim fullpath As String = Left(systemTreePath, InStrRev(systemTreePath, ".")) & optTreePath

	//                    Dim ck As String = branch.ID & "^" & r_worldwide.ID & "^" & fullpath
	//                    If i_Quantities.Contains(ck) Then
	//                        NoOp()
	//                        dupes += 1
	//                    Else
	//                        Dim aQty As clsQuantity = New clsQuantity(r_worldwide, fullpath, branch, 1, 1, 1, True, qwc)
	//                        parts += 1
	//                    End If
	//                Else
	//                    'No part so lets just add the info (for spec table etc)
	//                    Dim a = New clsProductAttribute(systemBranch.Product, iq.i_attribute_code("NetworkCard"), 0, iq.i_unit_code("txt"), New clsTranslation(English, optSku))
	//                End If
	//                optSku = rdr.Item("raid").ToString()
	//                If optLocations.ContainsKey(optSku) Then
	//                    Dim optTreePath As String = optLocations(optSku)
	//                    Dim branch As clsBranch = iq.Branches(Split(optTreePath, ".").Last)
	//                    Dim fullpath As String = Left(systemTreePath, InStrRev(systemTreePath, ".")) & optTreePath

	//                    Dim ck As String = branch.ID & "^" & r_worldwide.ID & "^" & fullpath
	//                    If i_Quantities.Contains(ck) Then
	//                        NoOp()
	//                        dupes += 1
	//                    Else
	//                        Dim aQty As clsQuantity = New clsQuantity(r_worldwide, fullpath, branch, 1, 1, 1, True, qwc)
	//                        parts += 1
	//                    End If
	//                Else
	//                    'No part so lets just add the info (for spec table etc)
	//                    Dim a = New clsProductAttribute(systemBranch.Product, iq.i_attribute_code("RaidCard"), 0, iq.i_unit_code("txt"), New clsTranslation(English, optSku))
	//                End If
	//                optSku = rdr.Item("display").ToString()
	//                If optLocations.ContainsKey(optSku) Then
	//                    Dim optTreePath As String = optLocations(optSku)
	//                    Dim branch As clsBranch = iq.Branches(Split(optTreePath, ".").Last)
	//                    Dim fullpath As String = Left(systemTreePath, InStrRev(systemTreePath, ".")) & optTreePath

	//                    Dim ck As String = branch.ID & "^" & r_worldwide.ID & "^" & fullpath
	//                    If i_Quantities.Contains(ck) Then
	//                        NoOp()
	//                        dupes += 1
	//                    Else
	//                        Dim aQty As clsQuantity = New clsQuantity(r_worldwide, fullpath, branch, 1, 1, 1, True, qwc)
	//                        parts += 1
	//                    End If
	//                Else
	//                    'No part so lets just add the info (for spec table etc)
	//                    Dim a = New clsProductAttribute(systemBranch.Product, iq.i_attribute_code("Display"), 0, iq.i_unit_code("txt"), New clsTranslation(English, optSku))
	//                End If
	//            End If
	//        End If
	//    End While
	//    rdr.Close()

	//    da.BulkWrite(oCon, qwc, "Quantity")

	//    con.Close()

	//End Sub


	public void fixProductFamilies()
	{
		//DONT use this any more
		System.Diagnostics.Debugger.Break();

		//Pushes the Family attributes of products through dans Abbreviations - to make G8 families into Gen8

		SqlClient.SqlConnection con = da.OpenDatabase();

		List<string> errormessages = new List<string>();

		LoadAbbreviations(con);

		int done = 0;
		clsProductAttribute pa;

		foreach ( product in iq.Products.Values) {
			if (product.i_Attributes_Code.ContainsKey("FamMajor")) {
				pa = product.i_Attributes_Code("FamMajor")(0);
				object txt = pa.Translation.text(English);
				if (dicAbbreviations.ContainsKey(txt)) {
					if (dicAbbreviations(txt) != txt) {
						pa.Translation = iq.AddTranslation(dicAbbreviations(txt), English, "fams", 0, null, 0, false);
						pa.update(errormessages);
						done += 1;
					}
				} else {
					Debug.Print(txt);
				}
			}
		}

		// OutputErrors()

	}




	public void WriteDicOptions(Dictionary<string, clsProduct> options, filename)
	{
		IO.StreamWriter sw = new IO.StreamWriter(filename, false);

		foreach ( ck in options.Keys) {
			sw.WriteLine(ck + " ---" + options(ck).DisplayName(English));
		}

		sw.Close();

	}

	public object fixPci()
	{

		SqlClient.SqlConnection con;
		SqlClient.SqlDataReader rdr;

		con = da.OpenDatabase();

		Dictionary<string, clsBranch> dicSystems = loadDic(con, iq.Branches, "system");

		Import.slotTypes(con, dicSystems);


	}


	public void everything()
	{
		iq.PNAdown = true;
		//Suspend the webservice whilst running the import

		Logit("Import started " + Now.ToString);

		//        Dim ImportEvent As clsEvent
		//        ImportEvent = New clsEvent(iq.RootEvent, "Iquote 1 Import", ev_Info)

		//Dim EventDicLoad As clsEvent = New clsEvent(ImportEvent, "Loading IQ1 dictionaries", ev_Info)
		//Dim eventDicSave As clsEvent = New clsEvent(ImportEvent, "Saving updated dictionaries", ev_Info)

		xlate = new Dictionary<string, Dictionary<string, string>>(StringComparer.CurrentCultureIgnoreCase);
		//see BtnImport_click 

		SqlClient.SqlConnection con;
		SqlClient.SqlDataReader rdr;

		con = da.OpenDatabase();

		Dictionary<string, clsBranch> dicSystems = loadDic(con, iq.Branches, "system");
		Dictionary<string, clsProduct> OptionsBySku = loadDic(con, iq.Products, "option");
		//        Dim DicVariants As Dictionary(Of String, clsVariant) = loadDic(con, iq.Variants, "variant")

		// If Not DicVariants.ContainsKey("") Then DicVariants.Add("", iq.StandardVariant)
		// If Not DicVariants.ContainsKey("ABU") Then DicVariants.Add("ABU", New clsVariant("ABU", "", "UK", "", "#ABU", ""))
		// If Not DicVariants.ContainsKey("ABF") Then DicVariants.Add("ABF", New clsVariant("ABF", "", "France", "", "#ABF", ""))
		//saveDic(con, DicVariants, "variant")

		Dictionary<string, clsChannel> dicChannels = loadDic(con, iq.Channels, "channel");
		Dictionary<string, clsRegion> dicRegions = loadDic(con, iq.Regions, "region");

		// build a lookup of currencies by country code
		Dictionary<string, clsCurrency> DicRegionCurrency = loadDic(con, iq.Currencies, "coCurr");

		con.Close();
		con = da.OpenDatabase;
		Import.LoadAbbreviations(con);

		//LANGUAGES
		clsLanguage aLanguage = null;
		Dictionary<string, clsLanguage> dicLanguage = loadDic(con, iq.Languages, "lang");
		//If Not dicLanguage.ContainsKey("en") Then dicLanguage.Add("en", iq.i_language_Code("EN"))

		con.Close();
		con = da.OpenDatabase;

		Import.Languages(con, dicLanguage);
		saveDic(con, dicLanguage, "lang");

		//Iquote 1 Code to Expanded name
		//populate the dictionary of all option types (MEM/HDD/CPU) + NOTEBOOK,DESKTOP,SERVER

		Dictionary<string, clsProductType> dicOptTypes = new Dictionary<string, clsProductType>(StringComparer.CurrentCultureIgnoreCase);
		dicOptTypes = loadDic(con, iq.ProductTypes, "optType");
		Import.ProductTypes(con, dicOptTypes);

		if (!iq.i_ProductType_Code.ContainsKey("CHAS")) {
			clsProductType chassisPT = new clsProductType("CHAS", iq.AddTranslation("Chassis", English, "UI", 0, null, 0, false), 0);
			//Add a Chassis Option type
		}

		if (!dicOptTypes.ContainsKey("CHAS")) {
			dicOptTypes.Add("CHAS", iq.i_ProductType_Code("CHAS"));
			//
		}
		//dicOptTypes.Add("MOBO", New clsProductType("MOBO", iq.AddTranslation("MotherBoard", English, "UI", 0, Nothing, 0, False))) 'Add a Mobo

		saveDic(con, dicOptTypes, "optType");

		Import.LoadTranslations(con);
		//populates the xlate dictionary (from dbo.language_key)

		//makes branches for the Desktop/notebook/server level branches
		Dictionary<string, clsBranch> dicSysTypes = loadDic(con, iq.Branches, "sysType");
		Import.SysTypes(con, dicSysTypes);
		saveDic(con, dicSysTypes, "sysType");

		Dictionary<string, clsSector> DicSectors = loadDic(con, iq.Sectors, "sector");
		Import.Sectors(con, DicSectors);
		//poulates iq.sectors - HP Business UNITS ISS/PSG/SWD etc
		saveDic(con, DicSectors, "sector");

		//contains the family branches (each one containing a number of systems)
		// loadDic(con, iq.Branches, "family")
		Dictionary<string, clsBranch> dicFamily = Import.Families(con, dicSysTypes);
		saveDic(con, dicFamily, "family");

		Logit("building PLcode lookup dictionary");
		Dictionary<string, string> dicplcode;
		dicplcode = LoadPLCodes(con);
		//generates a dictionary of SKU>Plcode

		Dictionary<string, clsCurrency> dicCurrencies = loadDic(con, iq.Currencies, "currency");
		// Import.Currencies(con, dicCurrencies)
		saveDic(con, dicCurrencies, "currency");

		dicRegions = Import.Regions(con);
		//, dicRegions) ', DicRegionCurrency) '15 seconds or so ! (yuck)
		saveDic(con, dicRegions, "region");

		//used for high speed fixing of localisations

		Dictionary<string, List<string>> containment = clsRegion.containment();
		List<string> errormessages = new List<string>();

		//bloody ages *circa 1 minute)
		//saveDic(con, dicSystems, "systemB4")  'keep a copy of the systems we had *before* this update (so we know which to add in buildtree() later)
		Import.Systems(con, dicSystems, dicFamily, dicplcode, containment, errormessages);
		saveDic(con, dicSystems, "system");

		Logit("Imported " + dicSystems.Count + " systems", false, true);

		//Import.givesslots(con, dicSystems, dicOptTypes) 'makes the non PCI 'gives' slots (drive bays, memory etc)

		//ND NOT ((options.OptType='CPU' AND options.OptSKU<>sys.CPU))

		//4 secs
		int numDescs;
		Dictionary<string, clsTranslation> dicDescs = loadDic(con, iq.Translations, "sysDesc");
		numDescs = Import.SystemDescriptions(con, dicDescs, dicSystems);
		saveDic(con, dicDescs, "sysDesc");

		//OS
		//Dim numOS As Integer
		// Dim dicOS As Dictionary(Of String, clsTranslation) = loadDic(con, iq.Translations, "sysOS")
		// numOS = Import.SystemDescriptions(con, dicOS, dicSystems)
		// saveDic(con, dicOS, "sysOS")


		//units
		Dictionary<string, clsUnit> dicUnits = loadDic(con, iq.Units, "unit");
		Import.units(con, dicUnits);
		saveDic(con, dicUnits, "unit");

		//Options is a Biggie - returns a flat list (of option products by SKU)  and populates the 6D dictionary Dicsysfam
		//of all potential options under each SysFamily



		//                           sysfam^l1^l2^l3^optSn 
		//Dim optionsByCK As Dictionary(Of String, clsProduct) = _
		//Import.options(con, OptionsBySku, dicplcode, dicOptLocalisation, dicUnits, containment)

		//system family names are the short(broad) codes - like DL580eG8
		//sysFamily (or sysfamilyCode) are the narrower long codes like DL580eG8C25SFFLRD        
		//WriteDicOptions(optionsByCK, "c:\temp\options.txt") 'just for debugging purposes

		//saveDic(con, optionsByCK, "option")

		//buildtree takes the 5D dicitonary and grafts the 'master' (per family) copy options onto every system - then prunes the incompatible options off
		//for incremental imports..it only adds systems that are not already in the import dictionary
		//loadDic(con, dicSystems, iq.Branches, "systemB4")

		//option quanitities (products.options.localisation)
		//around 3 minutes

		// Import.BuildTree(con, dicSystems, OptionsBySku, dicFamily, optionsByCK, dicOptTypes, dicOptLocalisation, ImportEvent, errormessages)

		Dictionary<clsProduct, List<clsRegion>> dicOptLocalisation = new Dictionary<clsProduct, List<clsRegion>>();
		Dictionary<string, clsBranch> opts = Import.options2(con, dicplcode, dicUnits, dicOptLocalisation, containment);
		//- POPULATES dicOptLocatlisations

		con.Close();

		//new - 'Flags' some options as FIOs
		Import.FIOfocus();

		con = da.OpenDatabase();
		Buildtree2(con, opts, dicFamily, dicSystems, dicOptLocalisation);
		//1 minute  

		con.Close();

		con = da.OpenDatabase();

		Logit("BuiltTree", false, true);

		iq.LoadGrafts(con, errormessages);
		//we'll be needing these ! (to recurse the prodcut tree correctly)
		// Import.TopRecommendations()
		Import.HighPerformance();

		//Import.CPUs(con) 'fails beacuse we cant get descendants if we're not logged in

		opts = null;
		// free some memory !

		//saveDic(con, DicRegionCurrency, "rgCurr")

		//Import channels and clones
		//old' Channeld ID's to new IQ2 channel objects

		con.Close();
		con.Dispose();
		con = da.OpenDatabase();

		//around 40 secs
		Import.channels(con, dicChannels, dicRegions, errormessages);

		con.Close();
		con = da.OpenDatabase();
		saveDic(con, dicChannels, "channel");

		//USERS, ACCOUNTS and TEAMS about 21 seconds
		Dictionary<string, clsAccount> dicAccounts = loadDic(con, iq.Accounts, "account");
		Dictionary<string, clsTeam> dicTeams = loadDic(con, iq.Teams, "team");
		Dictionary<string, clsUser> dicUsers = loadDic(con, iq.Users, "user");

		//15 secs
		Import.users(con, dicChannels, dicAccounts, dicTeams, dicUsers);

		saveDic(con, dicAccounts, "account");
		saveDic(con, dicTeams, "team");
		saveDic(con, dicUsers, "user");

		dicAccounts = null;
		dicTeams = null;
		dicUsers = null;

		//     Import.DoPrunes()


		//TODO write the dictionary out to create priceBands table - actually, no we'll put them in the clsAccounts
		//For Each seller In dicpriceBands.Keys
		// For Each buyer In dicpriceBands(seller).Keys
		//Next
		//Next

		//PRICES
		//gets the 'base' pricing for each seller
		//about 40 secs

		System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect();

		// anevent = New clsEvent(ImportEvent, "prices", ev_Info)
		// Import.Prices(con, dicSystems, dicOptions, dicChannels) ', DicVariants)
		// anevent.update()

		con.Close();
		con = da.OpenDatabase();

		Dictionary<string, clsstock> dicStock = loadDic(con, iq.Stock, "Stock");
		//now loadUp the dictionary with previously imported stock (from PNA_Stock)

		//around 40 secs
		//NOBBLED   Import.Stock(con, dicStock, dicSystems, dicOptions, dicChannels, anevent)

		//        con.Close()
		//        con = da.OpenDatabase()
		//        saveDic(con, dicStock, "Stock")
		//        anevent.update()

		//14 secs
		//Calculates the margins per product type per (buying) customer
		//Import.ExtText()

		Import.Margins(con, dicSystems, OptionsBySku, dicChannels);


		con.Close();
		con = da.OpenDatabase();

		//around 18 seconds
		Dictionary<string, clsProduct> dicAutoAdds;
		// = loadDic(con, iq.Products, "autoAdd")
		dicAutoAdds = new Dictionary<string, clsProduct>();

		Import.autoadds(con, dicAutoAdds, dicSystems, dicRegions);
		saveDic(con, dicAutoAdds, "autoadd");

		con.Close();
		con.Dispose();
		con = da.OpenDatabase();

		System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect();

		Dictionary<string, clsState> dicStates;
		dicStates = Import.quoteStates(con);

		// - temporaily removed        Import.slotAdds(con) ' options that add slots (like CPUs that enable memory)

		//Get the disting listprice countries - so we can load one country at once
		//need the 'everyone channel' to exist first

		if (false) {
			SqlClient.SqlConnection con2 = da.OpenDatabase("Data Source=iquote2.channelcentral.net\\charliel,8484\\;Initial Catalog=Pricing; password=wainwright; user id=editor; Connection Timeout=10;");
			SqlDataReader lpr = da.DBExecuteReader(con2, "SELECT DISTINCT country from pricing.products.hpPriceList");
			List<string> lpcountries = new List<string>();
			while (lpr.Read) {
				lpcountries.Add(lpr.Item("country"));
			}
			lpr.Close();
			con2.Close();

			foreach ( lpc in lpcountries) {
				Import.listprices(con, lpc);
				//needed for calculation of avalanche rebates during import of quote options
			}
		}



		GetVariants("UNHOSTED", "", errormessages);

		con = da.OpenDatabase();
		Import.RefCodes(con);
		//adds the refcodes to every option
		//  Import.Avalanche(con)  'imports the avalanche offers from Datastore.Products_Avalalance_rules
		//  Import.Bundles(con)


		Import.preInstalledParts();
		//DL570 Memory boards (amongst other thigns)

		//        Import.defaultWarranty()

		if (false) {
			quoteImport.all(con);
		}

		con.Close();
		con.Dispose();
		con.Dispose();

		//Setup username sand passwords
		//Dim ac As clsAccount
		//Dim u As clsUser = iq.i_user_email("tim.moyle@channelcentral.net")
		//ac = u.Accounts(iq.i_channel_code("DAZRG248NE").ID)
		//ac.priceBand = "325009"
		//ac.update(errormessages)

		//  ac = iq.i_user_email("tim.moyle@channelcentral.net").Accounts(iq.i_channel_code("DWERG74AH").ID)
		//  ac.priceBand = "CHA097"
		//  ac.update(errormessages)

		//reload the OM
		clsIQ.reset();
		object d = iq.Users.Count;
		while ((!clsIQ.IsLoaded)) {
			System.Threading.Thread.Sleep(100);
		}

		Import.SoftwareSlots();

		con = da.OpenDatabase();
		Import.CPUs(con, errormessages);

		Import.Extras();
		Import.Networking();
		Import.Graphics();
		Import.ImportQuickSpecs();
		Import.InterfaceSlots();
		_Default.SetRCAs(errormessages);
		Import.TopRecommendations();
		// Import.ExtText()
		Import.FlexOPGs();
		Import.PowerSizing(con);
		Import.RunSQLScripts();
		Import.NetworkSlots(con);
		con.Close();


		Logit("Import complete " + Now.ToString, false, true);
		iq.PNAdown = false;

	}


	private void RunSQLScripts()
	{
		foreach (FileInfo f in new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.GetData("DataDirectory"), "..", "Modules", "ImportSQL")).GetFiles("*.sql")) {
			SqlConnection con = da.OpenDatabase(true);
			FileStream fs = f.OpenRead();
			TextReader tr = new StreamReader(fs);

			SqlCommand sql = new SqlCommand(tr.ReadToEnd(), con);
			sql.CommandTimeout = 500000;
			sql.ExecuteNonQuery();

		}

		clsIQ.reset();
	}
	//Public Sub addTROtoSC(tro As clsBranch, SCk As String, dicFinished As Dictionary(Of String, clsBranch), hptro As clsTranslation, sing As clsTranslation, plur As clsTranslation)


	//    'SCK is the supplcani branches Compound key
	//    Dim hb As clsBranch 'header branch
	//    If Not dicFinished.ContainsKey(SCk) Then

	//        hb = New clsBranch(Nothing, Nothing, hptro, "", plur, sing, Nothing, 0, False, )
	//        dicFinished.Add(SCk, hb)

	//    Else
	//        hb = dicFinished(SCk)
	//    End If

	//    'check wether the header has the tros catergory branch - and/or make it
	//    'add the tro to the cat

	//End Sub


	public void NetworkSlots(con)
	{
		DataTable dt = new DataTable();
		dt = da.MakeWriteCacheFor(con, "Slot");

		foreach ( b in iq.Branches.Values) {
			if (b.Product != null) {
				if (b.Product.i_Attributes_Code.ContainsKey("PriPorts")) {
					object slt = splitNetworkSlotType(b.Product.i_Attributes_Code("PriConnectivity").First.Translation.text(English));
					if (slt.Length > 0) {
						if (!iq.i_slotType_Code.ContainsKey(slt(0)) || !iq.i_slotType_Code(slt(0)).ContainsKey(slt(1))) {
							object c = new clsSlotType(slt(0), slt(1), iq.AddTranslation("", English, "slottype", 0, null, 0, false));
						}
						object s = new clsSlot(iq.i_slotType_Code(slt(0))(slt(1)), b, null, b.Product.i_Attributes_Code("PriPorts").First.NumericValue, iq.AddTranslation("", English, "", 0, null, 0, false), new NullableInt(), 0, 0, dt);
						if (slt(2) != "") {
							if (!iq.i_slotType_Code.ContainsKey(slt(2)) || !iq.i_slotType_Code(slt(2)).ContainsKey(slt(3))) {
								object c = new clsSlotType(slt(2), slt(3), iq.AddTranslation("", English, "slottype", 0, null, 0, false));
							}
							s = new clsSlot(iq.i_slotType_Code(slt(2))(slt(3)), b, null, b.Product.i_Attributes_Code("PriPorts").First.NumericValue, iq.AddTranslation("", English, "", 0, null, 0, false), new NullableInt(), 0, 0, dt);
						}
					}
				}
				if (b.Product.i_Attributes_Code.ContainsKey("SecPorts")) {
					object slt = splitNetworkSlotType(b.Product.i_Attributes_Code("SecConnectivity").First.Translation.text(English));
					if (slt.Length > 0) {
						if (!iq.i_slotType_Code.ContainsKey(slt(0)) || !iq.i_slotType_Code(slt(0)).ContainsKey(slt(1))) {
							object c = new clsSlotType(slt(0), slt(1), iq.AddTranslation("", English, "slottype", 0, null, 0, false));
						}
						object s = new clsSlot(iq.i_slotType_Code(slt(0))(slt(1)), b, null, b.Product.i_Attributes_Code("SecPorts").First.NumericValue, iq.AddTranslation("", English, "", 0, null, 0, false), new NullableInt(), 0, 0, dt);
						if (slt(2) != "") {
							if (!iq.i_slotType_Code.ContainsKey(slt(2)) || !iq.i_slotType_Code(slt(2)).ContainsKey(slt(3))) {
								object c = new clsSlotType(slt(2), slt(3), iq.AddTranslation("", English, "slottype", 0, null, 0, false));
							}
							s = new clsSlot(iq.i_slotType_Code(slt(2))(slt(3)), b, null, b.Product.i_Attributes_Code("SecPorts").First.NumericValue, iq.AddTranslation("", English, "", 0, null, 0, false), new NullableInt(), 0, 0, dt);
						}
					}
				}
				if (b.Product.i_Attributes_Code.ContainsKey("UpPorts")) {
					object slt = splitNetworkSlotType(b.Product.i_Attributes_Code("UpConnectivity").First.Translation.text(English));
					if (slt.Length > 0) {
						if (!iq.i_slotType_Code.ContainsKey(slt(0)) || !iq.i_slotType_Code(slt(0)).ContainsKey(slt(1))) {
							object c = new clsSlotType(slt(0), slt(1), iq.AddTranslation("", English, "slottype", 0, null, 0, false));
						}
						object s = new clsSlot(iq.i_slotType_Code(slt(0))(slt(1)), b, null, b.Product.i_Attributes_Code("UpPorts").First.NumericValue, iq.AddTranslation("", English, "", 0, null, 0, false), new NullableInt(), 0, 0, dt);
						if (slt(2) != "") {
							if (!iq.i_slotType_Code.ContainsKey(slt(2)) || !iq.i_slotType_Code(slt(2)).ContainsKey(slt(3))) {
								object c = new clsSlotType(slt(2), slt(3), iq.AddTranslation("", English, "slottype", 0, null, 0, false));
							}
							s = new clsSlot(iq.i_slotType_Code(slt(2))(slt(3)), b, null, b.Product.i_Attributes_Code("UpPorts").First.NumericValue, iq.AddTranslation("", English, "", 0, null, 0, false), new NullableInt(), 0, 0, dt);
						}
					}
				}
				if (b.Product.i_Attributes_Code.ContainsKey("POEP") && b.Product.i_Attributes_Code.ContainsKey("POETech")) {
					if (!iq.i_slotType_Code.ContainsKey("POE") || !iq.i_slotType_Code("POE").ContainsKey(b.Product.i_Attributes_Code("POETech").First.Translation.text(English))) {
						object c = new clsSlotType("POE", b.Product.i_Attributes_Code("POETech").First.Translation.text(English), iq.AddTranslation("", English, "slottype", 0, null, 0, false));
					}
					object s = new clsSlot(iq.i_slotType_Code("POE")(b.Product.i_Attributes_Code("POETech").First.Translation.text(English)), b, null, b.Product.i_Attributes_Code("POEP").First.NumericValue, iq.AddTranslation("", English, "", 0, null, 0, false), new NullableInt(), 0, 0, dt);
				}
			}
		}
		da.BulkWrite(con, dt, "slot");
	}
	private string[] splitNetworkSlotType(string desc)
	{
		object majorCode1 = "";
		object majorCode2 = "";
		object minorCode1 = "";
		object minorCode2 = "";
		object j = 0;
		object bump = 0;

		object i = desc.IndexOf("Dual Personality");
		majorCode1 = "RJ45";
		if (i > -1) {
			bump = 17;
			majorCode2 = "SFP";
			minorCode2 = "Open Mini-GBIC";
			j = desc.IndexOf(" or ") - i;
		}
		if (i < 0) {
			i = desc.IndexOf("SFP+");
			majorCode1 = "SFP+";
		}
		if (i < 0) {
			i = desc.IndexOf("SFP");
			majorCode1 = "SFP";
		}
		if (i < 0) {
			i = desc.IndexOf("RJ45");
			majorCode1 = "RJ45";
		}
		if (i < 0) {
			i = desc.IndexOf("XENPACK");
			majorCode1 = "XENPACK";
		}
		if (i < 0) {
			return {
				
			};
		}
		if (j == 0)
			j = desc.Length - i - majorCode1.Length - 1;
		minorCode1 = desc.Substring(i + majorCode1.Length + bump + 1, j).Replace("Ethernet", "").Trim();

		return {
			majorCode1,
			minorCode1,
			majorCode2,
			minorCode2
		};
	}


	public void energyStar()
	{
		//this is a one off to patch the ballsed up import

		clsAttribute estar = iq.i_attribute_code("eStar");

		SqlClient.SqlConnection con = da.OpenDatabase();
		DataTable wc = da.MakeWriteCacheFor(con, "ProductAttribute");

		//Energy star options
		object Sql = "SELECT modelsku from h3.iQ.products.systems where energystar=1";

		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, Sql);

		int esc = 0;
		while (rdr.Read) {
			if (iq.i_SKU.ContainsKey(rdr.Item("modelsku"))) {
				clsProduct sys = iq.i_SKU(rdr.Item("modelsku"));
				clsProductAttribute pa = new clsProductAttribute(sys, estar, 1, iq.i_unit_code("txt"), null, wc);
				esc += 1;
			}
		}
		rdr.Close();

		da.BulkWrite(con, wc, "ProductAttribute");

	}


	public void CPUs(SqlClient.SqlConnection con, ref List<string> errormessages)
	{

		//Delete any (preinstalled) quantities pertaining to CPU's

		int cpuProdID = iq.i_ProductType_Code("CPU").ID;
		object sql = "DELETE FROM Quantity WHERE FK_Branch_ID IN (SELECT ID from Branch where FK_Product_ID in (select id from product where fk_productType_id='" + cpuProdID + "'));";
		da.DBExecutesql(sql);

		//delete any slots on branches carrying CPU products
		sql = "DELETE FROM Slot WHERE FK_Branch_ID IN (SELECT ID from Branch where FK_Product_ID in (select id from product where fk_productType_id='" + cpuProdID + "'));";
		da.DBExecutesql(sql);

		//delete the grafts of cpu branches
		sql = "DELETE FROM Graft WHERE FK_Branch_ID_Source in (select ID from Branch where FK_Product_ID in (select id from product where fk_producttype_id='" + cpuProdID + "'))";
		da.DBExecutesql(sql);


		//delete quoteitems which reference CPU branches
		sql = "DELETE FROM Quoteitem WHERE FK_Branch_ID IN (SELECT ID from Branch where FK_Product_ID in (select id from product where fk_productType_id='" + cpuProdID + "'));";
		da.DBExecutesql(sql);


		//delete the actual exsiting CPU branches
		sql = "DELETE FROM Branch where FK_Product_ID in (select id from product where fk_productType_id='" + cpuProdID + "');";
		da.DBExecutesql(sql);

		//Note - we don't delete the cpu PRODUCTS at any point (they're not part of the CPU import)

		//Fetch IQ1 limits for CPUs (specifically) into a dictionary - accesible by family code

		sql = "SELECT [SysFamily],isnull(optTypeParent,'Miscellaneous') as optCat,[OptType],[QtyInstalled],[QtyMax],qtymin,[Incr_Min],[Incr_Pref] FROM " + server + "[iq].[products].[OptionLimits] ";
		sql += "INNER JOIN " + server + "[iq].[products].[opttypes] o ON o.OptTypeCode = opttype WHERE opttype='CPU'";




		Dictionary<string, clsLimit> dicCpuLimitsBySysFamCode;
		dicCpuLimitsBySysFamCode = new Dictionary<string, clsLimit>(StringComparer.CurrentCultureIgnoreCase);

		Dictionary<string, clsBranch> dicCPUs = new Dictionary<string, clsBranch>(StringComparer.CurrentCultureIgnoreCase);
		//SysFamily>CPU Branch

		clsBranch cpuRoot = new clsBranch(null, null, iq.AddTranslation("CPU", English, "UI", 0, null, 0, false), "", iq.AddTranslation("Processors", English, "collect", 0, null, 0, false), iq.AddTranslation("Processor", English, "collect", 0, null, 0, false), iq.Screens(719), 1, false, "BG");

		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql);

			//If rdr.Item("qtymax") > 1 Then Stop

		 // ERROR: Not supported in C#: WithStatement

		rdr.Close();

		//Whilst the increments and max come from the subfamily - the actual number of installed CPUS comes from products.systems
		Dictionary<string, int> SysCpuQTY = new Dictionary<string, int>();
		sql = "SELECT cpuqty,modelsku from h3.iq.products.systems";
		rdr = da.DBExecuteReader(con, sql);
		while (rdr.Read) {
			if (!IsDBNull(rdr.Item("cpuqty"))) {
				SysCpuQTY.Add(rdr.Item("modelsku"), rdr.Item("cpuqty"));
			}
		}
		rdr.Close();

		List<clsSlot> todel = new List<clsSlot>();

		//anything without a set of optionlimits - will use this
		clsLimit StandardLimits = new clsLimit(1, 1, 1, 0, 0);

		Dictionary<string, List<string>> sysPaths = new Dictionary<string, List<string>>();
		iq.RootBranch.SkuPaths(sysPaths, "tree.1", false);
		//find the location of every system

		clsTranslation processor = iq.AddTranslation("Processor", English, "OL2", 0, null, 0, false);
		clsTranslation processors = iq.AddTranslation("Processors", English, "OL2", 0, null, 0, false);

		clsTranslation performance = iq.AddTranslation("Performance", English, "OL1", 0, null, 0, false);
		Dictionary<string, clsBranch> pbranches = new Dictionary<string, clsBranch>(StringComparer.CurrentCultureIgnoreCase);

		DataTable qtywritecache = da.MakeWriteCacheFor(con, "quantity");
		DataTable slotwritecache = da.MakeWriteCacheFor(con, "slot");

		int done = 0;
		List<string> nonexistent = new List<string>();


		foreach ( systemSKU in sysPaths.Keys.ToArray) {
			//            If sysPaths(systemSKU).Count > 1 Then Stop 'this system appears in more than one place in the tree
			clsBranch systembranch = iq.Branches(Split(sysPaths(systemSKU)(0), ".").Last);
			if (systembranch.Product.i_Attributes_Code.ContainsKey("cpuSKU")) {
				clsTranslation cputl = systembranch.Product.i_Attributes_Code("cpuSKU")(0).Translation;
				object cpusku = cputl.text(English);

				if (!systembranch.Product.i_Attributes_Code.ContainsKey("FamMinor")) {
					Logit(systembranch.DisplayName(English) + " Has no minor family");
				} else {
					string subfam = systembranch.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English);

					//If Not cpusku.StartsWith("###") Then
					if (cpusku == "664011-B21") {
						object a = 9;
					}
					if (!iq.i_SKU.ContainsKey(cpusku)) {
						Logit("CPU " + cpusku + " does not exist\"");
						if (!nonexistent.Contains(cpusku)) {
							nonexistent.Add(cpusku);
						}

					} else {
						clsProduct cpuProd = iq.i_SKU(cpusku);

						//constructs the 'master' set of CPU's unde a cpuRoot branch (hanging in space)
						clsBranch cpuBranch;
						if (dicCPUs.ContainsKey(cpusku)) {
							cpuBranch = dicCPUs(cpusku);
						} else {
							cpuBranch = new clsBranch(cpuProd, cpuRoot, cputl, "", processors, processor, iq.Screens(719), 0, false, "B");
							dicCPUs.Add(cpusku, cpuBranch);
						}

						object path = sysPaths(systemSKU)(0);
						if (systemSKU == "646905-421") {
							object a = "";
						}
						// If sysPaths(systemSKU).Count > 1 Then Stop

						if (!systembranch.NameSurf(path, "All Options/System/Processor")) {
							Logit("could not locate All Options/System/processor under " + systembranch.DisplayName(English));
						} else {
							clsBranch processorCatbranch = iq.Branches((int)Split(path, ".").Last);

							object exp = PathName(path);

							//NB: this graft has a path !! - generally grafts apply to every occurance of the branch 
							//but CPU's is one scenario where you only want the graft to 'work' in one place (it's the *same* procesor branch on every model in the family !)
							//not: the SAM CPU may be (is often) grafted to more than one syyem in the family
							processorCatbranch.Graft(cpuBranch, "CPU Import", path, errormessages);

							clsLimit limits;
							if (dicCpuLimitsBySysFamCode.ContainsKey(subfam)) {
								limits = dicCpuLimitsBySysFamCode(subfam);
							} else {
								limits = StandardLimits;
							}

							//NEW (and important) get the number of CPUS from products.systems
							//ML added containskey check as it was tripping the import up
							if (SysCpuQTY.ContainsKey(systemSKU))
								limits.Qinstalled = SysCpuQTY(systemSKU);

							clsQuantity qty = new clsQuantity(r_worldwide, path + "." + cpuBranch.ID.ToString.Trim, cpuBranch, limits.Qinstalled, limits.MinIncr, limits.PrefIncr, true, qtywritecache);

							//put a 'gives' CPU slot on the system
							object sysPath = sysPaths(systemSKU)(0);
							clsSlot CPUgiveSlot = new clsSlot(iq.i_slotType_Code("CPU")("GEN_CPU"), systembranch, sysPath, limits.Qmax, null, new NullableInt(), limits.Qmin, limits.Qinstalled, slotwritecache);
							clsSlotType st = iq.i_slotType_Code("CPU")("GEN_CPU");
							//And a TAKES slot on the CPU itself
							clsSlot cpuTakeslot = new clsSlot(st, cpuBranch, "", -1, null, new NullableInt(), 1, 0, slotwritecache);
							//                'and give memory slots... (see import.slotadds)

							if (limits.Qmax > 1) {
								Logit("MultiCpu machine " + systemSKU);
								//for multiCPU machines take the memory gives slots off the chassis and put them on the CPU
								//although a CPU potentially enables more (or less) slots if using UDIMM vs RDIMM 

								object pth = sysPaths(systemSKU)(0);

								if (systembranch.NameSurf(pth, "chassis")) {
									//The CPU (which exists in one 'global' place) - will give lots of different (minor) types of memory slot 
									// at lots of different paths
									clsBranch chassisbranch = iq.Branches(Split(pth, ".").Last);

									bool foundmem = false;
									foreach ( slot in chassisbranch.slots.Values.ToArray) {
										//locate the memory slots in the chassis.. 
										if (slot.Type.MajorCode == "MEM") {
											string newpath = path + "." + cpuBranch.ID.ToString.Trim;
											//path has been surfed down to the processor (option) branch
											//slot.Update(cpuBranch, newpath)

											clsSlot cpuMemEnable = new clsSlot(slot.Type, cpuBranch, newpath, slot.numSlots, slot.notes, slot.slotNum, slot.requiredFill, slot.advisedFill, slotwritecache);
											Logit("Duped memory slots from chassis to CPU " + cpusku);
											Logit("Path is " + newpath);

											if (!todel.Contains(slot))
												todel.Add(slot);
											object fpn = PathName(newpath);
											Logit(fpn);
											foundmem = true;
										}
									}
								} else {
									System.Diagnostics.Debugger.Break();
								}

								done += 1;
							}
						}
						//End If
					}
				}
			}
		}

		int nex = nonexistent.Count;
		//non-existent cpus (not in I_Sku)

		da.BulkWrite(con, qtywritecache, "quantity");
		da.BulkWrite(con, slotwritecache, "slot");
		foreach ( slot in todel) {
			slot.delete(errormessages);
		}

		Logit("Finished CPUs", false, true);

	}




	public void HighPerformance()
	{
		SqlClient.SqlConnection con = da.OpenDatabase();
		object Sql = "SELECT modelsku from h3.iQ.products.Systems s join h3.iQ.products.options o on s.CPU = o.optsku where o.highperformance = 1";

		if (!iq.i_attribute_code.ContainsKey("Perf")) {
			clsAttribute perfa = new clsAttribute("Perf", iq.AddTranslation("High Performance", English, "UI", 0, null, 0, false), 0);
		}

		clsAttribute perf = iq.i_attribute_code("Perf");
		DataTable wc = da.MakeWriteCacheFor(con, "ProductAttribute");


		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, Sql);

		//high performance systems

		int perfsys = 0;

		//   Dim yes As clsTranslation = iq.AddTranslation("Yes", English)
		while (rdr.Read) {
			if (iq.i_SKU.ContainsKey(rdr.Item("ModelSKU"))) {
				clsProduct system = iq.i_SKU(rdr.Item("ModelSKU"));
				clsProductAttribute pa = new clsProductAttribute(system, perf, 1, iq.i_unit_code("txt"), null, wc);
				perfsys += 1;
			}
		}
		rdr.Close();

		//High perforamce options
		Sql = "SELECT optsku from h3.iQ.products.options where highperformance = 1";

		rdr = da.DBExecuteReader(con, Sql);
		int perfopt = 0;
		while (rdr.Read) {
			if (iq.i_SKU.ContainsKey(rdr.Item("optSKU"))) {
				clsProduct opt = iq.i_SKU(rdr.Item("optsku"));
				clsProductAttribute pa = new clsProductAttribute(opt, perf, 1, iq.i_unit_code("txt"), null, wc);
				perfopt += 1;
			}
		}
		rdr.Close();

		da.BulkWrite(con, wc, "ProductAttribute");

	}

	public string TopRecommendations()
	{

		SqlClient.SqlConnection con = da.OpenDatabase();

		List<string> errorMessages = new List<string>();

		//Every family/supplychain has a distinct TRO branch 

		//there are 11 (or so) categories
		object sql = "SELECT [Category_ID],[Category_textID],[Category_Rank],[Category_Image],l.en as cat_text FROM(h3.[iq].[Products].[Option_Recommendations_Categories]";
		sql += "  join h3.[iQ].[dbo].[Language_Key] l on l.textID = category_textid)";

		//CatID>rank|image|text
		Dictionary<int, string> cats = new Dictionary<int, string>();
		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql);
		clsTranslation tl;
		while (rdr.Read) {
			tl = iq.AddTranslation((string)rdr.Item("CAT_TEXT"), English, "TROct", (int)rdr.Item("category_rank"), null, 0, false);
			cats.Add((int)rdr.Item("category_id"), (string)rdr.Item("category_rank") + "|" + (string)rdr.Item("category_image") + "|" + (string)tl.Key);
		}
		rdr.Close();

		//index the first 3 levels of the tree.. (root/sector/family)  
		Dictionary<string, clsBranch> famBranches = new Dictionary<string, clsBranch>(StringComparer.CurrentCultureIgnoreCase);
		iq.RootBranch.index2(famBranches, 1, "tree.1");

		//Make the 'Upsell opportunity' branches (yuck) - one per family, grafted on to every system in that family
		int grafts;
		foreach (clsBranch family in famBranches.Values) {
			if (family.childBranches.Count > 0) {
				clsBranch UpsellBranch = new clsBranch(null, null, iq.AddTranslation("Upsell Opportunities", English, "UI", 0, null, 0, false), "upsell", iq.AddTranslation("Products", English, "collect", 0, null, 0, false), iq.AddTranslation("Product", English, "collect", 0, null, 0, false), iq.i_screens_code("Base"), 20, false, "U",
				null);
				foreach ( sys__1 in family.childBranches.Values) {
					grafts += 1;
					sys__1.Graft(UpsellBranch, "TRO/Upsell", "", errorMessages);
				}
			}
		}


		int nbid = 0;
		DataTable bwc = da.MakeWriteCacheFor(con, "Branch", nbid, true);

		sql = "SELECT [SysFamilyName],[Category_ID],[MfrPartNum],[Region],[SupplyChain] FROM h3.[iq].[Products].[Option_Recommendations]";

		DataTable qwc;
		qwc = da.MakeWriteCacheFor(con, "Quantity");


		rdr = da.DBExecuteReader(con, sql);

		//                        CK           header branch (contains cat branches)        
		Dictionary<string, clsBranch> troHeads;
		troHeads = new Dictionary<string, clsBranch>(StringComparer.CurrentCultureIgnoreCase);

		//Dim upsells As Dictionary(Of String, clsBranch)
		//upsells = New Dictionary(Of String, clsBranch)

		//make a branch for every TRO HEADING and put them in a dictionary, compound keyed by sysfamily|supplychain
		clsTranslation tlpart = iq.AddTranslation("Option", English, "collect", 0, null, 0, false);
		clsTranslation tlparts = iq.AddTranslation("Options", English, "collect", 0, null, 0, false);

		clsTranslation hptr = iq.AddTranslation("Top Recommended", English, "UI", 0, null, 0, false);

		string[] bits;
		int trobranches = 0;

		Dictionary<string, string> dicsc;
		dicsc = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
		dicsc.Add("TV", "top value");
		dicsc.Add("A", "regular models");
		dicsc.Add("SB", "smart buy");
		dicsc.Add("R", "manufacturer refurbished");


		int scs = 0;
		int duds = 0;

		object k;

		while (rdr.Read) {
			object sku = rdr.Item("mfrpartnum");
			if (!iq.i_SKU.ContainsKey(sku)) {
				Logit(sku + " is not a recognised SKU for TRO");
				duds += 1;

			} else {
				k = LCase(rdr.Item("sysfamilyname"));

				clsBranch headerbranch;
				if (troHeads.ContainsKey(k)) {
					headerbranch = troHeads(k);
				} else {
					//make a new TRO Overall header branch 'HP Top Recommended'
					headerbranch = new clsBranch(null, null, hptr, "hptop", tlpart, tlparts, null, 10, false, "H",
					bwc, nbid);
					troHeads.Add(k, headerbranch);
					scs += 1;
					//each sysfamily/supplychain will get its own upsells branch (but they share a translation)
					//nb: The upsell opportunities branch would not usually display (becuase it has no descendant products).. so there is a (cough) feature - to ensure it is always returned as a descendant 

				}

				clsBranch catbranch;
				bits = Split(cats(rdr.Item("category_id")), "|");

				object catbranches = from CB in troHeads(k).childBranches.Valueswhere CB.Translation.Key == bits(2);

				// If Not tros(ck).ContainsKey(rdr.Item("category_id")) Then
				if (catbranches.Any) {
					catbranch = catbranches.First;
				} else {
					//  MAKE THE CATEGORY BRANCH                                                          tKey      pic                               order
					catbranch = new clsBranch(null, headerbranch, iq.Translations(bits(2)), bits(1), tlparts, tlpart, null, bits(0), false, "I",
					bwc, nbid);
				}



				clsProduct product = iq.i_SKU(sku);
				clsTranslation tln;
				tln = iq.AddTranslation(product.SKU, English, "SKU", 10, null, 0, false);

				//the branches name isn't theat important as TROItems display the product.displamyname
				clsBranch trobranch = new clsBranch(product, catbranch, tln, "", tlparts, tlpart, null, 0, false, "",
				bwc, nbid);
				trobranches += 1;

				//TODO - make qty records to limit them by region
				//Most branches don't have a quantity record 
				//a quantities restrict availablity to the region(s) specified
				clsQuantity qty = new clsQuantity(iq.i_region_code(rdr.Item("region")), "", trobranch, 0, 1, 1, false, qwc);

			}

		}

		rdr.Close();
		Debug.Print(trobranches);

		da.BulkWrite(con, bwc, "branch", , true);
		da.BulkWrite(con, qwc, "quantity");

		DataTable gwc;

		gwc = da.MakeWriteCacheFor(con, "graft");

		//Graft the finished headerBranches onto each system in the family

		Dictionary<string, clsBranch> fb = new Dictionary<string, clsBranch>(StringComparer.CurrentCultureIgnoreCase);
		foreach ( k in famBranches.Keys) {
			fb.Add(Split(k, "|")(0), famBranches(k));
		}


		//header branches Compound key, of the form Famname|sc   (sc may be blank !)
		foreach ( famkey in troHeads.Keys) {

			//Dim sccks = From scCk In scBranches.Keys Where scCk = hbCK Or (Split(hbCK, "|").Last = "" And Left(hbCK, Len(scCk)) = scCk)

			//sysfamilyname > branch
			//    For Each k In famBranches.Keys 'Each scck In sccks 'for each Supply Chain Compound Key in Supply Chain Compound Keys
			//                If LCase(k) = hbCK Or (Split(hbCK, "|")(1) = "" And Left(hbCK, Len(k)) = LCase(k)) Then
			if (fb.ContainsKey(famkey)) {
				foreach ( Sys__2 in fb(famkey).childBranches.Values) {
					grafts += 1;
					if (!Sys__2.Product.isSystem)
						System.Diagnostics.Debugger.Break();
					Sys__2.Graft(troHeads(famkey), "TRO import", "", errorMessages, gwc);
				}
			}
			//End If
		}

		da.BulkWrite(con, gwc, "graft");

		con.Close();

		object r = duds + " Unrecognised options, grafted " + grafts + " option sets " + trobranches + " TRO branches";
		return r;

	}

	public void Receta(ref List<string> errormessages)
	{
		//Tag every product as receta (or not)

		//walk the entire tree - not crossing systems --graft all receta systems to a top level receta branch

		//We create a 'focus' attribute and add a 'receta' productAttribute (of attribute type focus) to every system and option flagged Receta (in IQ1).
		//We could later add addtional product grouping/focus attributes (smart buy..whatevever) - the front end has a 'matching' session variable which is a list of focuses.. when enabled - products are filtered by their focus
		//Countries !?? (ask dan)  have a 'focus' - and root branch

		clsAttribute FocusAttrib;

		FocusAttrib = iq.i_attribute_code("focus");


		//Grab ALL the recta Skus (systems and options) into a list
		List<string> rs = new List<string>();
		//RecetaSkus
		SqlClient.SqlConnection con = da.OpenDatabase("Data Source=www3.channelcentral.net\\charliel,8484\\;Initial Catalog=iQuote2; password=wainwright; user id=editor; Connection Timeout=10;");
		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, "Select ModelSKU FROM iq.products.systems WHERE recetaSystem=1 UNION SELECT OptSKU FROM iq.products.options WHERE receta=1");
		while (rdr.Read) {
			rs.Add(rdr.Item(0));
		}
		rdr.Close();

		con.Close();
		con = da.OpenDatabase();

		//Add a Recta attribute to every product in the list
		clsTranslation rt = iq.AddTranslation("receta", English, "UI", 0, null, 0, false);
		clsProduct product;
		clsProductAttribute pa;
		clsUnit textUnit = iq.i_unit_code("txt");
		DataTable pawc = da.MakeWriteCacheFor(con, "ProductAttribute");
		foreach ( sku in rs) {
			if (iq.i_SKU.ContainsKey(sku)) {
				product = iq.i_SKU(sku);
				if (!product.i_Attributes_Code.ContainsKey("focus")) {
					pa = new clsProductAttribute(product, FocusAttrib, 1, textUnit, rt, pawc);
				} else {
					//     Beep()
				}
			}
		}
		da.BulkWrite(con, pawc, "ProductAttribute");

		con.Close();

		//Dim systemBranches As Dictionary(Of String, IQ.clsBranch) = New Dictionary(Of String, IQ.clsBranch)
		//Now get ALL the 'first' SKUD branches (which will be systems)
		//systemBranches = iq.RootBranch.SKUdDescendants(Nothing, "tree.1", True, False, False, False)

		//We don't want supply chains in the receta tree -so we construct a slightly simiplified 'deep copy' of the top two levels of the tree (sysType,Family - then graft every system in
		//The actual displayed systems are (additonally) filtered in real-time against the 'receta' attribute (see HideReasons)

		clsBranch recetaRoot = new clsBranch(null, null, iq.AddTranslation("Receta", English, "UI", 0, null, 0, false), "", iq.AddTranslation("Systems", English, "collect", 0, null, 0, false), iq.AddTranslation("System", English, "collect", 0, null, false, false), iq.i_screens_code("Servers"), 1, false, "S");


		con = da.OpenDatabase();
		DataTable gwc = da.MakeWriteCacheFor(con, "graft");
		DataTable pwc = da.MakeWriteCacheFor(con, "Prune");

		clsBranch rcat;
		clsBranch rfam;

		foreach ( cat in iq.RootBranch.childBranches.Values) {
			object kk = cat.DisplayName(English);
			rcat = new clsBranch(cat.Product, recetaRoot, cat.Translation, cat.Picture, cat.CollectiveNoun, cat.collectiveNounSingular, cat.Matrix, cat.order, false, "S");
			foreach ( fam in cat.childBranches.Values) {
				rfam = new clsBranch(fam.Product, rcat, fam.Translation, fam.Picture, fam.CollectiveNoun, fam.collectiveNounSingular, fam.Matrix, fam.order, false, "B");
				//these are the supply chains - we want to skip 
				foreach ( sc in fam.childBranches.Values) {
					foreach ( sys in sc.childBranches.Values) {
						if (sys.Product.i_Attributes_Code.ContainsKey("focus")) {
							rfam.Graft(sys, "receta", "", errormessages, gwc);
							//graft each system into the (new, receta) family

							// below is an attempt to construct a pre-compiled Receta tree - which is not without its merits.. (Would be faster, giving a smaller tree and removing the realtime checks - which is signinicant when counting options)
							//However - it makes the Receta attribute confusing/redundant - 'flagging' things as receta is probably more intuitive for the product team (than grafting) - although new families will need to be grafted.

							// ''find ALL the skud products (options) and their paths under this receta system
							//Dim syspath$ = "tree." & Trim(recetaRoot.ID) & "." & Trim(rcat.ID) & "." & Trim(rfam.ID)
							//Dim all As Dictionary(Of String, clsBranch) = sys.SKUdDescendants(Nothing, syspath, True, True, False, False)

							//Dim Keep As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)
							//For Each kvp In all
							//    If kvp.Value.Product IsNot Nothing Then
							//        If kvp.Value.Product.i_Attributes_Code.ContainsKey("receta") Then
							//            Keep.Add(kvp.Key, kvp.Value)
							//        End If
							//    End If
							//Next

							//Dim prune As clsPrune = Nothing
							//Dim prunelist As List(Of String) = sys.SeverePrune(syspath, Keep)
							//For Each path In prunelist
							//    prune = New clsPrune(path, New NullableInt, "Receta", pwc)
							//Next
						}
					}
				}
			}
		}

		da.BulkWrite(con, gwc, "Graft");
		da.BulkWrite(con, pwc, "Prune");

		con.Close();

		iq.RootBranch.Graft(recetaRoot, "Receta", "", errormessages);

	}


	public void LoyaltyPoints()
	{

		//source (Iq1)
		SqlClient.SqlConnection scon = da.OpenDatabase();

		SqlClient.SqlConnection tcon = da.OpenDatabase;
		//target IQ2
		DataTable wc = da.MakeWriteCacheFor(tcon, "Points");

		Dictionary<string, clsScheme> ix_lp = new Dictionary<string, clsScheme>(StringComparer.CurrentCultureIgnoreCase);
		//Build an index of the (existsing) 
		clsScheme scheme;
		foreach ( scheme in iq.Schemes.Values) {
			ix_lp.Add(scheme.compoundKey, scheme);
			//Buid
		}

		SqlClient.SqlDataReader rdr = da.DBExecuteReader(scon, "Select MfrPartnum,points,country,startdate,enddate from h3.iq.products.loyaltypoints where enddate>getdate()");

		clsTranslation bc = iq.AddTranslation("Blue Carpet", English, "schemes", 0, null, 0, false);

		clsProduct product = null;

		object cc;
		//Country Code
		object ck;
		//Compound Key (for determining distinct schemes)

		List<string> dudCountries = new List<string>();

		while (rdr.Read) {
			if (!iq.i_SKU.ContainsKey(rdr.Item("MfrPartNum"))) {
				//If rdr.Item("mfrpartnum") = "C9299A" Then Stop
				Logit("Part " + rdr.Item("mfrpartnum") + " does not exist");
			} else {
				cc = rdr.Item("Country");
				if (!iq.i_region_code.ContainsKey(cc)) {
					if (!dudCountries.Contains(cc)) {
						dudCountries.Add(cc);
						Logit("Country " + cc + " does not exists");
					}
				} else {
					clsRegion region = iq.i_region_code(cc);
					ck = region.ID + "^" + rdr.Item("startdate") + "^" + rdr.Item("enddate");

					// We create a scheme for each distinct Country,StartDate,Enddate combo
					if (ix_lp.ContainsKey(ck)) {
						scheme = ix_lp(ck);
					} else {

						if (!iq.i_scheme_code.ContainsKey("BC")) {
							scheme = new clsScheme("BC", bc, region, rdr.Item("startdate"), rdr.Item("enddate"));
							ix_lp.Add(ck, scheme);
							// add it to the index (we only use locally for the import)
						}
					}

					product = iq.i_SKU(rdr.Item("MfrPartNum"));

					DataRow row = wc.NewRow;
					wc.Rows.Add(row);
					row.Item("Fk_Product_id") = product.ID;
					row.Item("Fk_scheme_id") = scheme.ID;
					row.Item("Points") = rdr.Item("Points");
				}
			}
		}

		rdr.Close();
		scon.Close();

		Logit("imported " + wc.Rows.Count + " points sets into " + iq.Schemes.Count + " schemes");
		Logit("All done", 0, true);


		da.BulkWrite(tcon, wc, "Points");


		tcon.Close();



	}


	public string PowerSizing(SqlClient.SqlConnection con)
	{


		clsSlotType watts;
		if (iq.i_slotType_Code.ContainsKey("PWR") && iq.i_slotType_Code("PWR").ContainsKey("W")) {
			watts = iq.i_slotType_Code("PWR")("W");
		} else {
			watts = new clsSlotType("PWR", "W", iq.AddTranslation("Watts", English, "units", 0, null, 0, false));
		}

		DataTable dt = new DataTable();
		dt = da.MakeWriteCacheFor(con, "Slot");

		SqlClient.SqlDataReader rdr;

		rdr = da.DBExecuteReader(con, "SELECT [rowID],[SysFamilyName],[powerMin],[powerMax]  from " + server + "[iq].[Products].[HPPS_SystemFamilies] order by sysfamilyname");

		//                                         sysfamilyname      path>branch
		Dictionary<string, Pair> FamilyBranches = iq.Branches(1).findFamilyBranches("tree." + Trim(iq.Branches(1).ID));

		//make the 'takes' watts slots for every system - which are for the Motherboard

		Dictionary<string, clsBranch> locations;

		clsBranch systemBranch;
		object systempath;

		int takes;

		object err = "";

		object familyName;

		clsSlot aslot;
		while (rdr.Read) {
			familyName = rdr.Item("sysfamilyname");
			if (FamilyBranches.ContainsKey(familyName)) {
				object path = FamilyBranches(familyName).First;
				clsBranch familyBranch = FamilyBranches(familyName).Second;
				locations = familyBranch.findSystemBranches(path);
				//find the locations of all the system branches (under the family branch)

				foreach ( sysloc in locations) {
					systempath = sysloc.Key;
					systemBranch = sysloc.Value;
					aslot = new clsSlot(watts, systemBranch, systempath, -rdr.Item("powermax"), null, new NullableInt(), 0, 0, dt);
					takes += 1;
				}
			} else {
				err = err + "Skipped family " + familyName + "<br/>";
			}

		}
		rdr.Close();

		rdr = da.DBExecuteReader(con, "SELECT [rowID],[SysFamilyName],optsku as [mfrPartNum],ISNULL(HPPS_Options.[powerMin],options.powermin),ISNULL(HPPS_Options.[powerMax],options.powermax)  from " + server + "[iq].[Products].[HPPS_Options] right outer join " + server + "[iq].[Products].[Options] on options.optsku = mfrPArtNum WHERE (ISNULL(HPPS_Options.[powerMax],options.powermax) is not null or ISNULL(HPPS_Options.[powerMax],options.powermax) is not null) and opttype<>'PSUm' order by sysfamilyname");

		//make the 'takes' (watts)  slot for every option
		//NOTE these 'TAKES' slots have paths - they take different amounts of power depending on which stsyems they are installed in

		List<clsProduct> consumingParts = new List<clsProduct>();
		string partno;
		while (rdr.Read) {
			partno = rdr.Item("mfrPartNum");
			if (iq.i_SKU.ContainsKey(partno)) {
				consumingParts.Add(iq.i_SKU(partno));
			}
		}
		rdr.Close();

		//Dim MonsterIndex As Dictionary(Of clsProduct, List(Of String)) 'A list of all the paths at which a product appears
		//MonsterIndex = New Dictionary(Of clsProduct, List(Of String))
		//iq.RootBranch.IndexProductPaths("tree." & Trim(iq.RootBranch.ID), MonsterIndex, False, True, consumingParts)  ' 5 SECS !

		//locate all the branches carrying this product
		Dictionary<clsProduct, List<clsBranch>> locs = new Dictionary<clsProduct, List<clsBranch>>();
		//to array is a (bad) fix for a collection modified error(which is almost certainly a double ajax call)
		foreach ( b in iq.Branches.Values.ToArray) {
			if (b.Product != null) {
				if (consumingParts.Contains(b.Product)) {
					if (!locs.ContainsKey(b.Product))
						locs.Add(b.Product, new List<clsBranch>());
					locs(b.Product).Add(b);
				}
			}
		}

		clsProduct optionProduct;
		Dictionary<string, List<string>> optPaths = new Dictionary<string, List<string>>(StringComparer.CurrentCultureIgnoreCase);

		clsBranch branch;

		string ofn = "";
		string fn;

		List<string> invalids = new List<string>();

		rdr = da.DBExecuteReader(con, "SELECT [SysFamilyName],optsku as [mfrPartNum],ISNULL([HPPS_Options].[powerMin],options.powermin) as PowerMin,isnull([HPPS_Options].[powerMax] ,options.powermax) as PowerMax from " + server + "iq.products.options left outer join " + server + "[iq].[Products].[HPPS_Options] on optsku=mfrPartNum where ISNULL([HPPS_Options].[powerMin],options.powermin) is not null or ISNULL([HPPS_Options].[powerMax],options.powermax) is not null order by sysfamilyname");


		Dictionary<string, int> sums = new Dictionary<string, int>();
		//We need to work out the average consumption per opttype
		Dictionary<string, int> counts = new Dictionary<string, int>();

		while (rdr.Read) {
			//for partial imports
			if (iq.i_SKU.ContainsKey(rdr.Item("mfrpartnum"))) {
				string optionSKU = rdr.Item("mfrPartnum");

				optionProduct = iq.i_SKU(optionSKU);

				//There are soms systems  in here !
				if (!optionProduct.isSystem) {

					bool skip = false;
					if (optionProduct.i_Attributes_Code.ContainsKey("optType")) {
						if ({
							"PSU",
							"PSUm"
						}.Contains(optionProduct.i_Attributes_Code("optType")(0).Translation.text(English)))
							skip = true;
					}

					if (!skip) {
						if (IsDBNull(rdr.Item("sysfamilyname"))) {
							//row applies *wherever*  this part appears .. trouble is it can appear on many branches
							if (locs.ContainsKey(optionProduct)) {
								foreach ( b in locs(optionProduct)) {
									aslot = new clsSlot(watts, b, "", -rdr.Item("powermax"), null, new NullableInt(), 0, 0, dt);
									//at each distinct branch (carrying this product) make a 'global' slot
									string opttype = optionProduct.i_Attributes_Code("opttype")(0).Translation.text(English);
									if (!sums.ContainsKey("NONE^" + opttype))
										sums.Add("NONE^" + opttype, 0);
									if (!counts.ContainsKey("NONE^" + opttype))
										counts.Add("NONE^" + opttype, 0);
									sums("NONE^" + opttype) += rdr.Item("powermax");
									counts("NONE^" + opttype) += 1;

								}
							}
						} else {
							fn = rdr.Item("sysfamilyname");
							if (FamilyBranches.ContainsKey(fn)) {
								object fampath = FamilyBranches(fn).First;
								clsBranch familyBranch = FamilyBranches(fn).Second;

								//find the path of every option under this family
								if (fn != ofn) {
									optPaths.Clear();
									//Important !! (or they'd just build up in here!)
									familyBranch.SkuPaths(optPaths, "", true);
									ofn = fn;
								}


								if (optPaths.ContainsKey(optionProduct.SKU)) {
									//contains every path of this option under the family
									foreach ( optionPath in optPaths(optionProduct.SKU)) {
										clsBranch optbranch = iq.Branches(Split(optionPath, ".").Last);

										aslot = new clsSlot(watts, optbranch, fampath + optionPath, -rdr.Item("powermax"), null, new NullableInt(), 0, 0, dt);
										takes += 1;

										string opttype = optionProduct.i_Attributes_Code("opttype")(0).Translation.text(English);
										if (!sums.ContainsKey(familyBranch.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English) + "^" + opttype))
											sums.Add(familyBranch.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English) + "^" + opttype, 0);
										if (!counts.ContainsKey(familyBranch.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English) + "^" + opttype))
											counts.Add(familyBranch.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English) + "^" + opttype, 0);
										sums(familyBranch.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English) + "^" + opttype) += rdr.Item("powermax");
										counts(familyBranch.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English) + "^" + opttype) += 1;

									}

								}
							} else {
								//   Beep() 'invalid family name
								NoOp();
								if (!invalids.Contains(fn)) {
									invalids.Add(fn);
									Logit("HPPSOptions references a sysFamilyname '" + fn + "' which does not exist");
								}
							}
						}
					}
				}
			}
		}
		rdr.Close();


		int made = 0;
		int cannyDo;

		clsTranslation tlapprox = iq.AddTranslation("* Estimated/typical power consumption", English, "U", 1, null, 0, false);
		//Fill the power sizing gaps
		//For EVERY option branch - make a takes watts slot based on the average consumption of the opttype in the family

		Dictionary<Int32, string> neededList = new Dictionary<int, string>();


		foreach ( branch in iq.Branches.Values) {

			if (branch.Product != null && branch.Product.hasSKU && !branch.Product.isSystem) {
				//it's an option...
				bool needed = true;
				if (branch.Product.i_Attributes_Code.ContainsKey("opttype")) {
					string opttype = branch.Product.i_Attributes_Code("opttype")(0).Translation.text(English);
					if (opttype != "PSU") {
						foreach ( slot in branch.slots.Values) {
							if (slot.Type.MinorCode == "W") {
								needed = false;
								break; // TODO: might not be correct. Was : Exit For
							}
						}

						if (needed) {
							//Make a slot takes slot - based on the average max consumption for the opt type

							neededList.Add(branch.ID, "");


						}
					} else {
						//    Stop
					}
				}
			}
		}
		iq.Branches(1).indexProductBranchesByPath("tree", true, neededList);
		//Very slow, this needs improving but is a quick fix to try and match up a family if known (on the power record)

		foreach ( n in neededList) {
			object fambranchl = FamilyBranches.Where(fb => fb.Value.First == Left(n.Value, Len(fb.Value.First)));
			fn = "NONE";
			if (fambranchl.Count > 0) {
				object fambranch = fambranchl(0).Value.Second;
				fn = fambranch.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English);
			}
			branch = iq.Branches(n.Key);
			object opttype = branch.Product.i_Attributes_Code("opttype")(0).Translation.text(English);


			if (sums.ContainsKey(fn + "^" + opttype)) {
				clsSlot AvgTakes = new clsSlot(watts, branch, "", -sums(fn + "^" + opttype) / counts(fn + "^" + opttype), tlapprox, new NullableInt(), 0, 0, dt);
				made += 1;
			} else {
				if (sums.ContainsKey("NONE^" + opttype)) {
					clsSlot AvgTakes = new clsSlot(watts, branch, "", -sums("NONE^" + opttype) / counts("NONE^" + opttype), tlapprox, new NullableInt(), 0, 0, dt);
					made += 1;
				} else {
					cannyDo += 1;
				}
			}
		}



		//make the 'Gives' WATTS slots for the power supplies - ML added PSUm watts in this and excluded from the takes
		//'rdr = da.DBExecuteReader(con, "SELECT optSKU,unitqty from " & server$ & "[iq].products.options where opttype='PSU' and active=1")
		rdr = da.DBExecuteReader(con, "SELECT optSKU,unitqty from " + server + "[iq].products.options where opttype='PSU' and opttype2 is null and active=1\tunion SELECT mfrpartnum,[HPPS_Options].powerMax from " + server + "[iq].[Products].[HPPS_Options] inner join h3.[iq].[Products].options on options.optsku= mfrpartnum where opttype='PSUm'");

		List<clsProduct> PSUs = new List<clsProduct>();
		while (rdr.Read) {
			string psuSku__1 = (string)rdr.Item("optsku");
			if (iq.i_SKU.ContainsKey(psuSku__1)) {
				PSUs.Add(iq.i_SKU(psuSku__1));
			}
		}
		rdr.Close();

		locs.Clear();
		foreach ( b in iq.Branches.Values) {
			if (b.Product != null) {
				if (PSUs.Contains(b.Product)) {
					if (!locs.ContainsKey(b.Product))
						locs.Add(b.Product, new List<clsBranch>());
					locs(b.Product).Add(b);
				}
			}
		}

		int gives;

		//For Each psu In PSUs
		rdr = da.DBExecuteReader(con, "SELECT optSKU,unitqty from " + server + "[iq].products.options where opttype='PSU' and active=1\tunion SELECT mfrpartnum,[HPPS_Options].powerMax from " + server + "[iq].[Products].[HPPS_Options] inner join h3.[iq].[Products].options on options.optsku= mfrpartnum where opttype='PSUm'");

		List<clsBranch> done = new List<clsBranch>();

		while (rdr.Read) {

			string psusku__2 = rdr.Item("optsku");

			if (iq.i_SKU.ContainsKey(psusku__2)) {
				//If Left$(psusku, 3) = "###" Then Stop ' this is good
				clsProduct psu = iq.i_SKU(psusku__2);

				if (locs.ContainsKey(psu)) {
					if (!IsDBNull(rdr.Item("unitqty"))) {
						int qty = rdr.Item("unitqty");

						//the same power supply is attached to many branches
						foreach ( branch in locs(psu)) {
							aslot = new clsSlot(watts, branch, "", qty, null, new NullableInt(), 0, 0, dt);
							gives += 1;
						}

					}
				}
			}
		}
		rdr.Close();

		int rc = dt.Rows.Count;

		da.BulkWrite(con, dt, "slot");

		PowerSizing = err + "<p>made " + gives + " gives and " + takes + " takes WATT slots. ";

		Logit("Completed powersizing import", false, true);

	}



	//Public Function ExtText()
	//    Return ExtText(Nothing, True, Nothing, 0, Nothing)
	//End Function


	public string ExtText(clsProduct prod, bool Inserting, DataTable AttribWriteCache, ref Int32 nextkey, DataTable TlWC__1, Dictionary<string, List<string>> xtdic, Dictionary<string, string> famlocs)
	{

		//If AttribWriteCache Is Nothing Then AttribWriteCache = da.MakeWriteCacheFor(da.OpenDatabase(), "ProductAttribute")
		//If TranslationWriteCache Is Nothing Then
		//    TranslationWriteCache = da.MakeWriteCacheFor(da.OpenDatabase(), "Translation")
		//    nextkey = clsTranslation.NextKey()
		//End If

		//Static FamilyBranches As Dictionary(Of String, Pair)

		//If FamilyBranches Is Nothing Then
		//    FamilyBranches = iq.RootBranch.findFamilyBranches("tree." & Trim(iq.RootBranch.ID))  'One time conversion of all family branch names to 'FamMajor' attributes
		//End If

		object err = "";
		//    Dim con As SqlClient.SqlConnection = da.OpenDatabase()
		// Dim PAWC As DataTable = da.MakeWriteCacheFor(con, "ProductAttribute") 'Allows us to bulk write (many times faster than lots of INSERTS


		string sku;
		clsProduct product = null;
		clsProductAttribute xText = null;

		clsAttribute noteAtt;
		if (!iq.i_attribute_code.ContainsKey("xText")) {
			noteAtt = new clsAttribute("xText", iq.AddTranslation("Note", English, "UI", 0, tlwc, nextkey, false), 0);
		} else {
			noteAtt = iq.i_attribute_code("xText");
		}

		clsAttribute HideAtt;
		if (!iq.i_attribute_code.ContainsKey("HideF")) {
			HideAtt = new clsAttribute("HideF", iq.AddTranslation("Hide in families", English, "UI", 0, TlWC__1, nextkey, false), 0);
		} else {
			HideAtt = iq.i_attribute_code("HideF");
		}

		clsAttribute showAtt;
		if (!iq.i_attribute_code.ContainsKey("ShowF")) {
			showAtt = new clsAttribute("ShowF", iq.AddTranslation("Show (only) in families", English, "UI", 0, TlWC__1, nextkey, false), 0);
		} else {
			showAtt = iq.i_attribute_code("ShowF");
		}

		Dictionary<string, int> dicSeverity;
		dicSeverity = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
		dicSeverity.Add("NULL", 4);
		dicSeverity.Add("NOTE", 4);
		dicSeverity.Add("MANDATORY", 7);
		dicSeverity.Add("WARNING", 7);
		clsProductAttribute hide = null;
		clsProductAttribute show = null;

		int notes;

		// Dim mt As Object


		object su, hu;
		//While rdr.Read

		if (xtdic.ContainsKey(prod.SKU)) {
			//all lines peratining to a SKu are in a list
			foreach ( line in xtdic(prod.SKU)) {

				string[] bits = Split(line, "^");
				string skutext = bits(0);
				string sysFamilyShowText = bits(1);
				string sysFamilyHideText = bits(2);
				string msgType = bits(3);

				//sku$ = rdr.Item("SKU")
				//                If iq.i_SKU.ContainsKey(sku) Then
				product = iq.i_SKU(prod.SKU);
				clsUnit textUnit = iq.i_unit_code("txt");


				clsTranslation tl = iq.AddTranslation(skutext, English, "xText", 0, TlWC__1, nextkey, true);
				xText = new clsProductAttribute(product, noteAtt, dicSeverity(msgType), textUnit, tl, AttribWriteCache, !Inserting);
				notes += 1;

				//we have to make a showUnder and hideUnder attribute for *every* xText - so that they stay 'in synch' with the xTexts themselves - this is all becuase a single product can have multiple external texts
				su = IIf(IsDBNull(sysFamilyShowText), "", "SysFamilyShowText");
				show = new clsProductAttribute(product, showAtt, 0, textUnit, iq.AddTranslation(su, English, "shows", 0, TlWC__1, nextkey, false), AttribWriteCache, !Inserting);

				hu = IIf(IsDBNull(sysFamilyHideText), "", "sysFamilyHideText");
				hide = new clsProductAttribute(product, HideAtt, 0, textUnit, iq.AddTranslation(hu, English, "hides", 0, TlWC__1, nextkey, false), AttribWriteCache, !Inserting);

				//Else
				//err$ &= "Invalid SKU " & sku
				//End If
			}
		}

		//  End While

		//da.BulkWrite(con, TranslationWriteCache, "Translation")
		//da.BulkWrite(con, AttribWriteCache, "ProductAttribute")

		//        rdr.Close()
		// con.Close()



		return err + "<p/>Added " + notes + " Notes";

	}

	public string listprices(SqlClient.SqlConnection con, string countryCode)
	{


		Logit("Importing list prices", false, false);

		iq.PNAdown = true;
		//STOP the webservices

		//NB: - this is not 'thread safe' - we MUST make sure than nothing else creates prices at the same time
		//ie. make the webservices unavailable

		int nextVid = 0;
		int nextPid = 0;

		con.Close();
		con = da.OpenDatabase();

		DataTable PriceWriteCache = new DataTable();
		PriceWriteCache = da.MakeWriteCacheFor(con, "Price", nextPid, true);
		DataTable vwc = da.MakeWriteCacheFor(con, "variant", nextVid, true);

		SqlClient.SqlDataReader rdr;
		//iq.products.hplp
		//rdr = da.dbexecuteReader(con, "SELECT oursku as pn,lp,curr FROM " & DSserver & "datastore.products.hplp_lite")

		SqlClient.SqlConnection con2 = da.OpenDatabase("Data Source=iquote2.channelcentral.net,8484; user id=editor;Initial Catalog=pricing; password=wainwright; connection timeout=35;");
		object sql = "SELECT Mfrpartnum,Currency,country,listprice FROM pricing.products.hpPriceList";
		if (countryCode != "")
			sql += " WHERE country='" + countryCode + "' ";
		sql += " ORDER BY country,currency";
		rdr = da.DBExecuteReader(con2, sql);
		//datastore.products.hplp_lite")

		Logit("Importing HP list prices for " + countryCode + " " + Now.ToString, false);

		object bpn;
		//Base part number #variant stripped

		clsVariant SKUVariant = null;
		clsProduct Product = null;
		clsCurrency Currency = null;
		NullablePrice Price = null;

		int rows = 0;
		int Updated = 0;
		int Added = 0;
		int Unchanged = 0;
		int unknown = 0;
		int dupes = 0;

		clsCurrency newcurrency;
		if (!iq.i_currency_code.ContainsKey("CHF"))
			newcurrency = new clsCurrency("CHF", null, iq.AddTranslation("Swiss Franc", English, "currencies", 0, null, 0, false), "Fr", 1, null);



		clsPrice aPrice;


		while (rdr.Read) {
			bpn = rdr.Item("MfrPartNum");
			bpn = Split(bpn, "#")(0);
			//take the part preceeding any #

			object cc;
			cc = rdr.Item("currency");

			if (!iq.i_currency_code.ContainsKey(cc)) {
				if (cc != "CHF" & cc != "MXN") {
					Logit("unknown currency:" + cc);
				}

			} else {
				Currency = iq.i_currency_code(cc);

				// If cc$ = "GBP" Then Stop

				if (!iq.i_SKU.ContainsKey(bpn)) {
					//  Logit("Unknown part number '" & bpn$ & "'")
					unknown += 1;

				} else {
					Product = iq.i_SKU(bpn);

					///' <summary>Provides access to a List of sellerChannel specific variants of the product</summary>
					//    Property Variants As Dictionary(Of clsChannel, List(Of clsVariant))

					SKUVariant = null;

					clsRegion country;
					country = iq.i_region_code(rdr.Item("country"));

					//we use UK (ask Dan!) - so map accross
					//HP use GB - the correct ISO code IS GB
					// If rdr.Item("country") = "GB" Then country = iq.i_region_code("UK") '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

					if (Product.i_Variants != null) {
						//Get the HP (sellers) variant for this country
						if (Product.i_Variants.ContainsKey(HP)) {
							foreach ( v in Product.i_Variants(HP)) {
								if (object.ReferenceEquals(v.Region, country)) {
									SKUVariant = v;
									//Variants *can* have a region (precisely to allow list prices per region)
								}
							}
						}
					}

					//there was no HP variant for this country - make one
					if (SKUVariant == null) {
						SKUVariant = new clsVariant("list", Product, HP, rdr.Item("MfrPartnum"), "List Price", "", "", country, false, vwc,
						nextVid);
					}

					aPrice = null;
					//SKUvariant is now the HP variant for the correct region (country for now)
					if (SKUVariant.i_prices.ContainsKey(Everyone)) {
						if (SKUVariant.i_prices(Everyone).ContainsKey(Currency)) {
							aPrice = SKUVariant.i_prices(Everyone)(Currency);
						}
					}

					if (aPrice == null) {
						//the HP variant for 'eveyone' in this region - DIDN'T have a price record - make one
						//create a new price - the price carries the currency - but the variant carries the region
						aPrice = new clsPrice(SKUVariant, Everyone, new NullablePrice(rdr.Item("listprice"), Currency, true), "I", PriceWriteCache, nextPid);

						Added += 1;
					} else {
						if (aPrice.Price.value == (decimal)rdr.Item("listprice")) {
							Unchanged += 1;
							//no need to do anything
						} else {
							aPrice.Price.value = rdr.Item("listprice");
							aPrice.lastUpdated = Now;
							aPrice.lastRequested = Now;

							aPrice.Update();
							Updated += 1;

						}
					}
				}
			}

			rows += 1;
		}
		rdr.Close();
		con2.Close();

		da.BulkWrite(con, PriceWriteCache, "Price");
		da.BulkWrite(con, vwc, "Variant");
		PriceWriteCache = null;


		foreach ( c in iq.Channels.Values) {
			c.pricesLoadedFor.Clear();
			//force a relaod of pricing 
		}


		Logit("dupes " + dupes);
		object l;
		l = "Import listprices - Processed:" + rows + " Updated:" + Updated + " Checked (unchanged):" + Unchanged + " Unknown (not in iQuoute):" + unknown + " Added:" + Added;

		iq.PNAdown = false;

		return l;


	}


	public void LoadAbbreviations(SqlClient.SqlConnection con)
	{
		dicAbbreviations = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

		object sql;
		sql = "SELECT CODE,TRANSLATION from " + server + "[iq].dbo.abbreviations";

		SqlClient.SqlDataReader rdr;

		rdr = da.DBExecuteReader(con, sql);
		while (rdr.Read) {
			if (!dicAbbreviations.ContainsKey(rdr.Item("code"))) {
				dicAbbreviations.Add(LCase(rdr.Item("code")), rdr.Item("translation"));
			}

		}
		rdr.Close();

	}

	public struct pciStruct
	{
		string tech;
		int connector;
		int speed;
		int w;
		int h;
			//can be blank - obsolete (we think)
		string generation;
		bool dedicated;

		string fullText;
	}

	public string DefaultCulture(CountryCode)
	{

		//returns a .net culture code for the specified country code (to be stored in the countries table... only used at import)

		if (CountryCode == "BE") {
			return "NL";
		} else {
			return CountryCode;
		}

	}


	public void updateQuoteDescriptionsAndTotals(ref SqlClient.SqlConnection con, Dictionary<string, clsQuote> dicquotes)
	{
		object sql;

		sql = Space(8192);
		// define a large buffer to avoid string concatenation (which is slow)
		object c;
		NullablePrice PriceBefore;

		//Update the descriptions and price, execute the SQL in large blocks for improved efficiency (could be faster with some fancy/dancy merge/stored procedure
		int ip = 0;
		int cp = 0;
		int p = 1;
		foreach ( q in dicquotes.Values) {
			PriceBefore = q.QuotedPrice;
			q.Saved = true;
			q.Locked = true;
			//updates the quote and saves it in db
			q.Update(null, false);



		}

	}

	public void RefCodes(SqlClient.SqlConnection con)
	{
		//HP Product COdes (used for avalanche)

		object sql;
		DataTable tlwc;

		clsAttribute prodref;
		if (!iq.i_attribute_code.ContainsKey("ProdRef")) {
			prodref = new clsAttribute("ProdRef", iq.AddTranslation("Product Reference code", English, "UI", 0, null, 0, false), 0);
		} else {
			prodref = iq.i_attribute_code("ProdRef");
		}


		sql = "DELETE FROM PRODUCTATTRIBUTE WHERE FK_ATTRIBUTE_ID=" + prodref.ID;
		da.DBExecutesql(con, sql);

		//remove all existing prodref codes
		sql = "DELETE FROM Translation WHERE [group]='rc'";
		da.DBExecutesql(con, sql);
		iq.LoadTranslations(con);


		//remove all existing prodref attributes from all products in the OM
		foreach ( p in iq.Products.Values) {
			if (p.i_Attributes_Code.ContainsKey("Prodref")) {
				foreach ( pa in p.i_Attributes_Code("ProdRef")) {
					p.Attributes.Remove(pa.ID);
				}
				p.i_Attributes_Code.Remove("ProdRef");
			}
		}

		//First make a translation for every distinct RefCode (do this a a first pass so we can bulk-write them
		tlwc = da.MakeWriteCacheFor(con, "translation");
		int nextkey = clsTranslation.NextKey();

		//sql$ = "Select distinct [Manuf7] from " & server$ & "[channelcentral].[products].[Hierarchy] where ISNULL (Manuf7,'')<>''"
		sql = "Select distinct cast([Manuf7] as nvarchar) as manuf7  from h3.[channelcentral].[products].[Hierarchy]";

		SqlClient.SqlDataReader rdr;
		rdr = da.DBExecuteReader(con, sql);

		while (rdr.Read) {
			if (!IsDBNull(rdr.Item("manuf7"))) {
				iq.AddTranslation(rdr.Item("manuf7"), English, "refcode", 0, tlwc, nextkey, false);
			}
		}
		rdr.Close();

		da.BulkWrite(con, tlwc, "translation");

		iq.LoadTranslations(con);
		//now we *must* load the translations up again from disk - so that they have their ID's

		//now we can import the options's refcodes
		sql = "SELECT [UPCNum],[Manuf7] from " + server + "[channelcentral].[products].[Hierarchy] where ISNULL (Manuf7,'')<>''";

		DataTable pawc = da.MakeWriteCacheFor(con, "productAttribute");

		rdr = da.DBExecuteReader(con, sql);

		clsProduct Product;
		clsUnit textUnit = iq.i_unit_code("txt");
		int duds = 0;
		int skipped = 0;

		clsTranslation tl;


		clsProductAttribute anAttribute = new clsProductAttribute();
		while (rdr.Read) {
			if (iq.i_SKU.ContainsKey(rdr.Item("UPCNum"))) {
				Product = iq.i_SKU(rdr.Item("UPCnum"));
				if (!Product.i_Attributes_Code.ContainsKey("ProdRef")) {
					tl = iq.EnglishIndex(rdr.Item("manuf7"), "refcode");
					if (tl == null)
						System.Diagnostics.Debugger.Break();
					anAttribute = new clsProductAttribute(Product, prodref, 0, textUnit, tl, pawc);
				} else {
					skipped += 1;
				}
			} else {
				//     Stop
				duds += 1;
			}

		}
		rdr.Close();

		da.BulkWrite(con, pawc, "productAttribute");
		pawc = null;

	}

	//Public Sub Bundles(con As SqlClient.SqlConnection)

	//    Dim writecache As DataTable = da.MakeWriteCacheFor(con, "bundle")
	//    Dim TranslationWritecache As DataTable = da.MakeWriteCacheFor(con, "Translation")

	//    Dim rdr As SqlClient.SqlDataReader
	//    Dim bundleNames As Dictionary(Of String, clsTranslation) = New Dictionary(Of String, clsTranslation)

	//    iq.Bundles.Clear()
	//    iq.i_Bundle_code.Clear()

	//    da.DBExecutesql(con, "Delete from bundleitem")
	//    da.DBExecutesql(con, "Delete from bundle")
	//    da.DBExecutesql(con, "Delete from bundleSystem")

	//    da.DBExecutesql(con, "Delete from translation where  [group]='bc'")
	//    iq.LoadTranslations(con)

	//    For Each p In iq.Products.Values
	//        If Not p.Bundles Is Nothing Then
	//            p.Bundles.Clear()
	//        End If
	//    Next

	//    'This is done in several passes to allow us to use bulkwrites generally - which makes it orders of magnitude faster (despite the multiple passes)

	//    'Pass 1 - Make the translations for every bundle name
	//    'note, several bundle codes can have the same name

	//    Dim sql$ = "SELECT bundleCode,bundleName from " & server$ & "[iq].products.bundleIndex_ISSRebates"

	//    rdr = da.DBExecuteReader(con, sql$)

	//    Dim bName As clsTranslation
	//    Dim bnametext As String
	//    While rdr.Read

	//        If IsDBNull(rdr.Item("bundlename")) Then
	//            bnametext = ""
	//        Else
	//            bnametext = rdr.Item("bundlename")
	//        End If
	//        bName = iq.AddTranslation(bnametext, English, "bc", 0, )
	//        bundleNames.Add(rdr.Item("Bundlecode"), bName)
	//    End While
	//    rdr.Close()

	//    da.BulkWrite(con, TranslationWritecache, "Translation")

	//    iq.LoadTranslations(con)  'load the translation up (so the all have their ID's) after the bulk insert

	//    'Pass 2 - Create the bundles
	//    sql$ = "SELECT opgref,bundleCode,rebate,systems,startdate,enddate,sites from " & server$ & "[iq].[products].bundleIndex_ISSRebates"

	//    iq.Bundles.Clear()

	//    rdr = da.DBExecuteReader(con, sql$)

	//    Dim aBundle As clsBundle
	//    Dim bn As clsTranslation
	//    Dim region As clsRegion

	//    Dim bs As clsBundleSystem

	//    While rdr.Read

	//        If Not IsDBNull(rdr.Item("opgref")) Then
	//            If Not IsDBNull(rdr.Item("sites")) Then
	//                If InStr(rdr.Item("sites"), ",") = 0 Then
	//                    If iq.i_region_code.ContainsKey(rdr.Item("sites")) Then
	//                        region = iq.i_region_code(rdr.Item("sites"))
	//                        bn = bundleNames(rdr.Item("bundlecode"))

	//                        aBundle = New clsBundle(bn, rdr.Item("opgref"), rdr.Item("bundlecode"), region, rdr.Item("startdate"), rdr.Item("enddate"), writecache)
	//                    Else
	//                        Logit("Invalid region code " & rdr.Item("sites") & " in bundle " & rdr.Item("bundlecode"))
	//                    End If
	//                Else
	//                    Logit("Invalid region code " & rdr.Item("sites") & " in bundle " & rdr.Item("bundlecode"))
	//                End If
	//            End If
	//        End If
	//    End While

	//    rdr.Close()
	//    da.BulkWrite(con, writecache, "BUNDLE")
	//    iq.LoadBundles(con, rdr)



	//    'Pass3 - load the bundle items (options)

	//    Dim bundle As clsBundle

	//    Dim bi As clsBundleItem
	//    sql$ = "SELECT bs.BundleCode,bundlePn,bundlePnPrice,qty from " & server$ & "[iq].products.Bundle_prices bp join " & server$ & "[iq].products.BundleStore_ISSrebates bs on bp.bundleCode=bs.BundleCode and bp.bundlePn=bs.OptSKU"

	//    Dim currencies As Dictionary(Of clsRegion, clsCurrency) = New Dictionary(Of clsRegion, clsCurrency)

	//    currencies.Add(iq.i_region_code("GB"), iq.i_currency_code("GBP"))
	//    currencies.Add(iq.i_region_code("AA"), iq.i_currency_code("GBP"))
	//    currencies.Add(iq.i_region_code("US"), iq.i_currency_code("USD"))
	//    currencies.Add(iq.i_region_code("NL"), iq.i_currency_code("EUR"))
	//    currencies.Add(iq.i_region_code("IE"), iq.i_currency_code("EUR"))

	//    Dim itemwritecache As DataTable = da.MakeWriteCacheFor(con, "BundleItem")

	//    Dim price As NullablePrice
	//    Dim product As clsProduct

	//    rdr = da.DBExecuteReader(con, sql$)

	//    While rdr.Read
	//        If iq.i_Bundle_code.ContainsKey(rdr.Item("bundlecode")) Then
	//            bundle = iq.i_Bundle_code(rdr.Item("bundlecode"))
	//            If iq.i_SKU.ContainsKey(rdr.Item("bundlepn")) Then
	//                product = iq.i_SKU(rdr.Item("bundlepn"))
	//                price = New NullablePrice(rdr.Item("Bundlepnprice"), currencies(bundle.Region), False)
	//                bi = New clsBundleItem(bundle, product, price, 0, rdr.Item("qty"), itemwritecache) 'makes the enty in the [BundleItem] table
	//            Else
	//                'invalid bundle item sku 
	//            End If
	//        Else
	//            'invalid bundle code
	//        End If


	//    End While
	//    rdr.Close()

	//    da.BulkWrite(con, itemwritecache, "BundleItem")

	//    iq.LoadBundles(con, rdr) 'we must load the bundles - becuase they were created with bulkwrite, the bundleitems had no ID's so could not yet be added to the bundles

	//    'Pass 4 - Add the bundles (now they have their ID's to the systems)

	//    Dim bswc As DataTable = da.MakeWriteCacheFor(con, "BundleSystem") 'bundleSystem,WriteCache
	//    Dim system As clsProduct
	//    Dim rebate As Single
	//    sql$ = "SELECT bundleCode,systems,rebate from " & server$ & "[iq].products.bundleIndex_ISSRebates"
	//    rdr = da.DBExecuteReader(con, sql$)

	//    Dim systems As String
	//    While rdr.Read
	//        'add the bundles to the systems
	//        If iq.i_Bundle_code.ContainsKey(rdr.Item("bundlecode")) Then
	//            bundle = iq.i_Bundle_code(rdr.Item("bundlecode"))
	//            If bundle.Items.Count Then


	//                If IsDBNull(rdr.Item("systems")) Then
	//                    'this wont be fast (but is rare) - chris's 'leave the systems blank to mean it works on every system
	//                    Dim systemList As List(Of clsProduct) = iq.RootBranch.SystemsThatTake(Nothing, Nothing, bundle)
	//                    For Each system In systemList
	//                        If IsDBNull(rdr.Item("rebate")) Then rebate = 0 Else rebate = rdr.Item("rebate")
	//                        bs = New clsBundleSystem(bundle, system, rebate, bswc)  'makes the entry in the [BundleSystem] table, and adds the bundle to the system
	//                    Next
	//                Else
	//                    systems = rdr.Item("systems")
	//                    For Each sys In Split(rdr.Item("systems"), ",")
	//                        If iq.i_SKU.ContainsKey(sys) Then
	//                            system = iq.i_SKU(sys)
	//                            If IsDBNull(rdr.Item("rebate")) Then rebate = 0 Else rebate = rdr.Item("rebate")
	//                            bs = New clsBundleSystem(bundle, system, rebate, bswc)  'makes the entry in the [BundleSystem] table, and adds the bundle to the system
	//                        End If
	//                    Next
	//                End If
	//            End If
	//        End If

	//    End While
	//    rdr.Close()

	//    da.BulkWrite(con, bswc, "BundleSystem")



	//End Sub

	public string Hierarchy()
	{

		//heirarcy import for SBSO and Printers plus standalone options
		//heirarcy import for SBSO and Printers plus standalone options

		object sql = "SELECT top 1000 [UPCNum],[MfrName],[ccDescription],[H1],[H2],[H3],[H4],[BUcode],[BU],[PL],[PLDesc],[Manuf4],[Manuf5],[Manuf6],[Manuf7],[AltPartNum],[Long Desc],[ProdCreated],[LastUpdated],[Source]";
		sql += "UPCID,[Electronic],[UPCURL],[OEM] FROM [ChannelCentral].[products].[Hierarchy]   where H1 is not null and h2 is not null and h3 is not null and BUcode is not null and UPCNum not like '###%' order by h1,h2,h3";

		SqlClient.SqlConnection con;
		con = da.OpenDatabase("Data Source=www3.channelcentral.net\\charliel,8484\\;Initial Catalog=iQuote2; password=wainwright; user id=editor; Connection Timeout=10;");

		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql);

		//hang the H1 branches of a 'catalogue view' branch

		//columns to become attributes
		//products should not (ever) appear twice - use iq.i_SKU

		//we will augment he existing product with any missing columns (as attribites)

		clsBranch catroot = null;
		clsBranch l1Branch;
		clsBranch l2branch;
		clsBranch l3branch;
		string ch1;
		string ch2;
		string ch3;
		clsProduct product;
		clsSector sector;

		ch1 = string.Empty;
		ch2 = string.Empty;
		ch3 = string.Empty;

		//For SBSO generalise everything to "Products"
		clsTranslation TLProducts = iq.AddTranslation("Products", English, "collect", 0, null, 0, false);
		//The Collective noun is used for counds and labels of categories - such as 57 Printers (or cacti)
		clsTranslation TLProduct = iq.AddTranslation("Product", English, "collect", 0, null, 0, false);
		//The Collective noun singular is the 'singleton' version 1 printer (or 1 cactus ! )

		//Dim nbid, npid, ntid, npaid As Integer
		//Dim bwc As DataTable = da.MakeWriteCacheFor(con, "Branch", nbid)
		//Dim pwc As DataTable = da.MakeWriteCacheFor(con, "Product", npid)
		//Dim tlwc As DataTable = da.MakeWriteCacheFor(con, "Translation", ntid)
		//Dim pawc As DataTable = da.MakeWriteCacheFor(con, "ProductAttribute", npaid)

		int h1count;
		int h2count;
		int h3count;
		int productCount;


		while (rdr.Read) {

			if (!object.ReferenceEquals(rdr.Item("H1"), DBNull.Value)) {
				if (iq.i_sector_code.ContainsKey(rdr.Item("BUcode"))) {
					sector = iq.i_sector_code(rdr.Item("BUcode"));

					System.DateTime activeTo = (System.DateTime)"31/12/2100";
					System.DateTime activefrom = (System.DateTime)"1/1/2000";

					product = new clsProduct(rdr.Item("UpcNum"), true, false, sector, iq.i_ProductType_Code("SYS"), activefrom, activeTo, true, false, true,
					"", "", "");
					productCount += 1;

					if (string.IsNullOrEmpty(ch1)) {
						catroot = new clsBranch(null, null, iq.AddTranslation("SBSO Catalogue View", English, "UI", 0, null, 0, false), "", TLProducts, TLProduct, iq.Screens(719), 1, false, "B");
					}


					if (rdr.Item("h1") != ch1) {
						h1count += 1;
						clsTranslation TLH1 = iq.AddTranslation(rdr.Item("H1"), English, "H1", 0, null, 0, false);

						l1Branch = new clsBranch(null, catroot, TLH1, "", TLProducts, TLProduct, null, 1, true, "SB");
						ch1 = rdr.Item("h1");

						if (rdr.Item("h2") != ch2) {
							h2count += 1;
							clsTranslation tlh2 = iq.AddTranslation(rdr.Item("H2"), English, "H2", 0, null, 0, false);
							l2branch = new clsBranch(null, l1Branch, tlh2, "", TLProducts, TLProduct, null, 1, true, "BG");
							ch2 = rdr.Item("h2");

							if (rdr.Item("h3") != ch3) {
								h3count += 1;
								clsTranslation tlh3 = iq.AddTranslation(rdr.Item("H3"), English, "H3", 0, null, 0, false);
								l3branch = new clsBranch(product, l2branch, tlh3, "", TLProducts, TLProduct, null, 1, false, "BG");
								ch3 = rdr.Item("h3");
							}
						}
					}

					AddAttribute("ccDescription", rdr, product);
					AddAttribute("Manuf4", rdr, product);
					AddAttribute("Manuf5", rdr, product);
					AddAttribute("Manuf6", rdr, product);
					AddAttribute("Manuf7", rdr, product);

				}

			}
		}

		rdr.Close();
		con.Close();

		return h1count + " H1's " + h2count + " H2's " + h3count + " H3's " + productCount + " Products - CatrootID is " + catroot.ID;

	}



	public void FlexOPGs()
	{
		da.DBExecutesql("DELETE FROM FlexRegion");
		da.DBExecutesql("DELETE FROM FlexLine");
		da.DBExecutesql("DELETE FROM FlexRule");
		da.DBExecutesql("DELETE FROM Flex");

		iq.FlexOPGs.Clear();

		SqlClient.SqlConnection scon;
		scon = da.OpenDatabase("Data Source=www3.channelcentral.net\\charlie,8484\\;Initial Catalog=iQuote2; password=wainwright; user id=editor; Connection Timeout=10;");
		SqlClient.SqlConnection tcon = da.OpenDatabase();

		//read the headers
		string sql = "SELECT OPG_ID,opg_description,OPG_StartDate,Opg_EndDate,opg_currencycode,opg_OptionCount_Min,OPG_OptionCount_Max,OPG_SysType FROM iq.products.opg_FlexPromo_Header WHERE opg_startDate<getdate() AND opg_enddate>getdate()";

		SqlClient.SqlDataReader rdr = da.DBExecuteReader(scon, sql);

		clsFlexOPG flexOPG;
		clsCurrency currency;
		Dictionary<int, clsFlexOPG> i_opgref = new Dictionary<int, clsFlexOPG>();

		DataTable dt = da.MakeWriteCacheFor(tcon, "Flex");

		while (rdr.Read) {
			if (iq.i_currency_code.ContainsKey(rdr.Item("opg_currencycode"))) {
				currency = iq.i_currency_code(rdr.Item("opg_currencycode"));
				flexOPG = new clsFlexOPG(rdr.Item("opg_id"), rdr.Item("opg_description"), rdr.Item("opg_startDate"), rdr.Item("opg_endDate"), currency, rdr("opg_OptionCount_Min"), IsDBNull(rdr("OPG_OptionCount_Max")) ? 999 : rdr("OPG_OptionCount_Max"), rdr.Item("OPG_SysType"), dt);

			} else {
				Logit(rdr.Item("opg_currency") + " is not recognised");
			}
		}
		rdr.Close();

		da.BulkWrite(tcon, dt, "flex");

		//We have to load them back (having bulk written them) so the get their ID's
		iq.LoadFlex(tcon, rdr);
		foreach ( v in iq.FlexOPGs.Values) {
			i_opgref.Add(v.OPGRef, v);
		}

		//read the lines
		int orphaned = 0;
		clsProduct product;
		clsFlexLine flexLine;

		dt = da.MakeWriteCacheFor(tcon, "FlexLine");

		//sql$ = "SELECT OPG_ID as ref,OPG_LINE_UPC_NUM as sku,opg_line_listprice as listprice,opg_line_netprice as netprice,opg_line_discount_additional as DiscPerc,opg_line_startDate as Vf, opg_line_Enddate as Vt "
		//sql$ &= "FROM iq.products.OPG_FlexPromo_Lines WHERE opg_line_startDate<getdate() and opg_line_endDate>getdate()"
		// Using wrong discount field use OPG_Line_Discount_Std instead of opg_line_discount_additional.
		sql = "SELECT OPG_ID as ref,OPG_LINE_UPC_NUM as sku,opg_line_listprice as listprice,opg_line_netprice as netprice,OPG_Line_Discount_Std as discPerc,opg_line_startDate as Vf, opg_line_Enddate as Vt ";
		sql += "FROM iq.products.OPG_FlexPromo_Lines WHERE opg_line_startDate<getdate() and opg_line_endDate>getdate()";
		rdr = da.DBExecuteReader(scon, sql);
		while (rdr.Read) {
			string ref__1 = rdr.Item("ref");

			if (!i_opgref.ContainsKey(ref__1)) {
				orphaned += 1;
			} else {
				flexOPG = i_opgref(ref__1);
				string sku = rdr.Item("SKU");

				if (!iq.i_SKU.ContainsKey(sku)) {
					orphaned += 1;
				} else {
					product = iq.i_SKU(sku);
					//Making the flexline adds it to the OPG (and a few other places (the product, the root dictionary of flexLines))
					//Dim rebate As Single = rdr.Item("listprice") * rdr.Item("discPerc") / 100
					float rebate = 0;

					if (!IsDBNull(rdr.Item("netprice"))) {
						string listPrice = rdr.Item("listprice");
						float lps = (float)listPrice;
						float discountPerc = (float)rdr("discPerc");
						// using OPG_Line_Discount_Std as discount instead of opg_line_discount_additional
						rebate = (listPrice - (listPrice * discountPerc / 100)) - (decimal)rdr.Item("netprice");
						//rebate = listPrice * discountPerc / 100   'not included - rdr.Item("netprice") >>>old formula..
					}

					flexLine = new clsFlexLine(flexOPG, product, rebate, rdr.Item("vf"), rdr.Item("vt"), dt);
				}
			}
		}

		rdr.Close();

		da.BulkWrite(tcon, dt, "Flexline");

		Logit("Orphaned lines" + orphaned);

		//read the rules

		dt = da.MakeWriteCacheFor(tcon, "FlexRule");
		sql = "SELECT OPG_ID as opgref,UPC_Type as ProdType,UPC_qty_min as [min],UPC_qty_max as [max],[optional] FROM iq.Products.opg_flexPromo_ProductRules";

		rdr = da.DBExecuteReader(scon, sql);


		int badrules = 0;
		clsFlexRule Rule = null;
		clsProductType productType = null;


		while (rdr.Read) {
			string ref__1 = rdr.Item("opgref");

			if (!i_opgref.ContainsKey(ref__1)) {
				badrules += 1;
			} else {
				flexOPG = i_opgref(ref__1);
				string pt = rdr.Item("ProdType");
				//Flex opgs should only be valid for storage in the UK
				if (pt == "SYS")
					pt = flexOPG.OPGSysType;
				// ugly mapping to IQ2 ProductType
				if (!iq.i_ProductType_Code.ContainsKey(pt)) {
					Logit("Unknown Product type:" + pt);
				} else {
					Rule = new clsFlexRule(flexOPG, iq.i_ProductType_Code(pt), rdr.Item("min"), rdr.Item("max"), (bool)rdr.Item("optional"), dt);
				}
			}
		}
		rdr.Close();

		da.BulkWrite(tcon, dt, "FlexRule");

		int badCountries = 0;
		int missingOPGs = 0;

		//read the countries
		clsFlexRegion flexRegion;
		sql = "SELECT OPG_ID,OPG_COUNTRYcode FROM iq.products.OPG_FlexPromo_Countries";
		rdr = da.DBExecuteReader(scon, sql);

		dt = da.MakeWriteCacheFor(tcon, "flexRegion");

		clsRegion region;

		while (rdr.Read) {
			string Ref__2 = rdr.Item("OPG_ID");
			string cc = rdr.Item("opg_countrycode");


			if (cc == "UK") {
				cc = "GB";
			}


			if (!iq.i_region_code.ContainsKey(cc)) {
				badCountries += 1;
			} else {
				region = iq.i_region_code(cc);
				if (!i_opgref.ContainsKey(Ref__2)) {
					missingOPGs += 1;
				} else {
					flexOPG = i_opgref(Ref__2);
					flexRegion = new clsFlexRegion(flexOPG, region, dt);
				}
			}
		}

		da.BulkWrite(tcon, dt, "FlexRegion");

		scon.Close();
		tcon.Close();

		Logit("Done flex import", false, true);

	}


	public void Avalanche(SqlClient.SqlConnection con)
	{
		//around 600 rows

		//first pass = gets the distinct OPGs

		da.DBExecutesql("DELETE FROM avalancheSystem");
		da.DBExecutesql("DELETE FROM avalancheOption");
		da.DBExecutesql("DELETE FROM avalancheOPG");

		da.DBExecutesql("DELETE FROM promoScan");

		iq.i_OpgRef.Clear();

		foreach ( product in iq.Products.Values) {
			if (product.AvalancheOPGs != null) {
				if (product.AvalancheOPGs.Count) {
					product.AvalancheOPGs.Clear();
				}
			}
		}


		DataTable writecache = da.MakeWriteCacheFor(con, "avalancheOPG");
		DataTable WriteAvSys = da.MakeWriteCacheFor(con, "avalancheSystem");
		// a datatable to bulk insert the foriegn key pairs which relates systems (products) to avalanche offers

		SqlClient.SqlDataReader rdr;

		string sql = "SELECT optionCountMin,systems,optionCountMax,startDate,endDate,opgREF,countries FROM " + DSserver + "[DataStore].[products].[Avalanche_Rules] ";
		sql += " WHERE enddate > getdate() ";
		sql += "GROUP BY optioncountmin,optioncountmax,startdate,enddate,opgref,countries,systems  ORDER BY enddate";
		rdr = da.DBExecuteReader(con, sql);


		ClsAvalancheOPG AvOPG;
		string[] systems;


		System.Data.DataRow row;

		string opg = string.Empty;
		clsAvalancheOption avopt;
		while (rdr.Read) {

			if (!iq.i_OpgRef.ContainsKey(rdr.Item("opgref"))) {
				clsRegion region = null;
				if (iq.i_region_code.ContainsKey(rdr.Item("countries"))) {
					region = iq.i_region_code(rdr.Item("countries"));
					AvOPG = new ClsAvalancheOPG(rdr.Item("opgref"), region, rdr.Item("startDate"), rdr.Item("endDate"), rdr.Item("OptionCountMin"), rdr.Item("OptionCountMax"), writecache);
				} else {
					Logit("Unrecognised region/country " + rdr.Item("Countries") + " In opg " + rdr.Item("opgRef"));
				}
			} else {
				System.Diagnostics.Debugger.Break();
			}
		}
		rdr.Close();

		da.BulkWrite(con, writecache, "AvalancheOPG");
		Logit("Wrote " + writecache.Rows.Count + " avalanche systems");


		iq.LoadAvalancheOPGs(con, rdr);
		//need to load them up to give them their ID's (after the bulk-write)


		//Now need to (in a second pass becuase they didn't have their ID's until the were re-loaded after the bulk write) add the OPG's to the systems
		sql = "SELECT DISTINCT  systems,opgref FROM " + DSserver + "[DataStore].[products].[Avalanche_Rules] ";
		rdr = da.DBExecuteReader(con, sql);
		while (rdr.Read) {
			systems = Split(rdr.Item("systems"), ",");
			string @ref = rdr.Item("opgREF");
			if (!iq.i_OpgRef.ContainsKey(@ref)) {
				Logit("Avalanche Rules");

			} else {
				AvOPG = iq.i_OpgRef(@ref);

				foreach ( Sys in systems) {
					if (iq.i_SKU.ContainsKey(Sys)) {
						if (!iq.i_SKU(Sys).AvalancheOPGs.ContainsKey(AvOPG.ID)) {
							iq.i_SKU(Sys).AvalancheOPGs.Add(AvOPG.ID, AvOPG);
							//add the avalanche to every qualifying system 
							row = WriteAvSys.NewRow();
							row("fk_product_id_system") = iq.i_SKU(Sys).ID;
							row("fk_avalancheOPG_id") = AvOPG.ID;
							WriteAvSys.Rows.Add(row);

						} else {
							Logit("Part " + Sys + " is listed more than once in opg " + AvOPG.OPGref);
						}
					} else {
						Logit("Avalanche " + rdr.Item("OpgRef") + " contains unrecognised system SKU " + Sys);
						//    Stop
					}
				}
			}
		}
		rdr.Close();


		DataTable WriteAvOpts = da.MakeWriteCacheFor(con, "avalancheOption");

		da.BulkWrite(con, WriteAvSys, "AvalancheSystem");

		Logit("Wrote " + WriteAvOpts.Rows.Count + " avalanche systems");

		sql = "Select prodrefcode,lpdiscountpercent,opgref,systems  FROM " + DSserver + "[DataStore].[products].[Avalanche_Rules] ";
		rdr = da.DBExecuteReader(con, sql);

		while (rdr.Read) {
			if (iq.i_OpgRef.ContainsKey(rdr.Item("opgref"))) {
				//and for every line make an OPGoption - hooking them to the OPG's we made earlier 
				avopt = new clsAvalancheOption(iq.i_OpgRef(rdr.Item("opgref")), rdr.Item("ProdRefCode"), rdr.Item("LPDiscountPercent"), WriteAvOpts);
			}

		}
		rdr.Close();

		da.BulkWrite(con, WriteAvOpts, "AvalancheOption");

		Logit("Wrote " + WriteAvOpts.Rows.Count + " avalanche options");

		Logit("done avalanche import", false, true);

		iq.LoadAvalancheOPGs(con, rdr);

	}


	public Dictionary<string, clsRegion> Regions(SqlClient.SqlConnection con)
	{

		//returns a dictionary of region code to clsRegion

		Dictionary<string, clsRegion> dicRegions;
		dicRegions = new Dictionary<string, clsRegion>(StringComparer.CurrentCultureIgnoreCase);

		//improve with... (get rid of the try catch(
		//CultureInfo[] cultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures);

		//load  the regions we already have (from the bootstrap)
		foreach ( r in iq.Regions.Values) {
			dicRegions.Add(r.Code, r);
		}


		object sql;

		sql = "SELECT DISTINCT region from " + server + "[iq].dbo.countries";

		SqlClient.SqlDataReader rdr;
		rdr = da.DBExecuteReader(con, sql);

		object rgn;

		while (rdr.Read) {
			rgn = rdr.Item("region");
			if (!object.ReferenceEquals(rgn, DBNull.Value)) {
				if (!dicRegions.ContainsKey(rgn)) {
					dicRegions.Add(rgn, new clsRegion(r_worldwide, rgn, iq.AddTranslation(rgn, English, "RR", 0, null, 0, false), false, iq.i_culture_code("en-gb"), false, ""));
					//Make the root level regions
				}
			}
		}
		rdr.Close();

		sql = "SELECT Region,countryName,countrycode FROM " + server + "iq.dbo.[Countries] ORDER BY countryName";
		rdr = da.DBExecuteReader(con, sql);

		object cntry;
		object cname;
		int o = 0;

		clsRegion region;

		//make a root level placeholder for those countries that have not yet been assigned to a region



		while (rdr.Read) {
			//lots of the piddly countries haven't been assigned to a region yet (fun job for someone !)
			if (IsDBNull(rdr.Item("Region"))) {
				region = r_RestOfWorld;
			} else {
				region = dicRegions(rdr.Item("Region"));
			}

			cntry = rdr.Item("Countrycode");

			clsCulture culture = iq.i_culture_code("en-gb");


			// If cntry = "UK" Then culture = iq.i_culture_code("en-gb") : cntry = "GB"
			if (cntry == "US")
				culture = iq.i_culture_code("en-us");
			if (cntry == "DK")
				culture = iq.i_culture_code("da-dk");
			if (cntry == "FR")
				culture = iq.i_culture_code("fr-fr");
			if (cntry == "AF")
				culture = iq.i_culture_code("af-ZA");
			if (cntry == "AL")
				culture = iq.i_culture_code("sq-AL");
			//albania
			if (cntry == "DZ")
				culture = iq.i_culture_code("ar-DZ");
			//
			if (cntry == "AS")
				culture = iq.i_culture_code("as-EN");
			//America samoa
			if (cntry == "CH")
				culture = iq.i_culture_code("ch-DE");
			//switzerlan
			if (cntry == "AT")
				culture = iq.i_culture_code("at-DE");
			//austria





			cname = rdr.Item("countryName");


			if (!iq.i_region_code.ContainsKey(cntry)) {
				dicRegions.Add(cntry, new clsRegion(region, cntry, iq.AddTranslation(cname, English, "CN", o, null, 0, false), true, culture, false, ""));
				//make a region for each Country
				o += 1;
			}
		}


		rdr.Close();

		//sql$ = "SELECT distinct activeSites FROM   [IQ].[products].[Systems]"

		//Dim sd As SortedList = New SortedList
		//Dim c() As String

		//Dim uniqueCombos As Dictionary(Of String, clsRegion) = New Dictionary(Of String, clsRegion)
		//rdr = da.dbexecuteReader(con, sql$)

		//While rdr.Read
		//    c = Split(rdr.Item("activeSites"), ",")
		//    sd = New SortedList
		//    For Each cc In c
		//        If cc <> "ZZ" Then
		//            sd.Add(cc, cc)
		//        End If
		//    Next

		//    If sd.Count Then
		//        Dim sorted As List(Of String) = New List(Of String)
		//        For Each cc In sd.Keys
		//            sorted.Add(cc)
		//        Next
		//        Dim sl As String = Join(sorted.ToArray, ",")

		//        If Not uniqueCombos.ContainsKey(sl) Then
		//            uniqueCombos.Add(sl, Nothing)
		//        End If
		//    End If

		//End While

		//rdr.Close()

		return dicRegions;

	}


	/// <summary>Builds and returns a dictionary of SysFamilycode^optfamily>clsLimit</summary>
	/// <remarks>ofdic Is an INPUT used to work our narrow option families from broad option types (per family) eg. (HDD>NHP35LFF)  - these are later made into clsQuantitys (autoAdds) and pointed to the option branches - </remarks>
	public Dictionary<string, clsLimit> BuildOptLimits(SqlClient.SqlConnection con, Dictionary<string, Dictionary<string, string>> ofdic, List<string> sysSkus = null, string filter = "")
	{

		Dictionary<string, clsLimit> dicLimits = new Dictionary<string, clsLimit>(StringComparer.CurrentCultureIgnoreCase);
		//we RETURN this

		SqlClient.SqlDataReader rdr;
		object sql;
		if (sysSkus != null) {
			sql = "SELECT [SysFamily],isnull(optTypeParent,'Miscellaneous') as optCat,[OptType],[QtyInstalled],[QtyMax],[Incr_Min],[Incr_Pref] ";
			sql += "from  " + server + "[iq].[products].[OptionLimits] INNER join  " + server + "[iq].[products].[opttypes] o on o.OptTypeCode = opttype ";
			sql += "inner join   " + server + "[iq].[products].[systems] on [SysFamily] = [familycode]";
			sql += "WHERE modelsku IN ('" + Join(sysSkus.ToArray, "','") + "')";


		} else {
			sql = "SELECT [SysFamily],isnull(optTypeParent,'Miscellaneous') as optCat,[OptType],[QtyInstalled],[QtyMax],[Incr_Min],[Incr_Pref] from " + server + "[iq].[products].[OptionLimits] ";
			sql += "INNER join " + server + "[iq].[products].[opttypes] o on o.OptTypeCode = opttype";
			if (filter != "")
				sql += " where opttype='" + filter + "'";


		}
		//Return WLNA,FAN,MEM,PSU etc (opt types)

		//OptCat - Chassis
		//opttype fan
		//qtyinstalled 4


		rdr = da.DBExecuteReader(con, sql);

		string sysfam;
		string OptionType;

		while (rdr.Read) {
			sysfam = rdr.Item("sysfamily");
			//this is actual the narrow sysfamily code
			//        optCat = Trim$(rdr.Item("optCat"))
			OptionType = Trim(rdr.Item("optType"));
			//- We have to work out the narrow option family (NHP35LFF) from the broad option type (HDD)
			if (ofdic.ContainsKey(sysfam)) {

				if (ofdic(sysfam).ContainsKey(OptionType) | OptionType == "CPU") {

					string optionfamily;
					if (OptionType == "CPU") {
						optionfamily = "GEN_CPU";
						// ;ERIC"
					} else {
						optionfamily = ofdic(sysfam)(OptionType);
					}


					//  If OptionType = "CPU" Then Stop

					string ck = sysfam + "^" + OptionType + "^" + optionfamily;


					//If Not dicLimits.ContainsKey(ck) Then
					//dicLimits.Add(ck,ring, clsLimit))
					//End If

					//The qinstalled are all set to zero 
					//otherwise EVERY option of the type (eg MEM) has an installed qty
					int qtyInstalled = 0;
					if (!IsDBNull(rdr.Item("qtyInstalled")))
						qtyInstalled = rdr.Item("qtyInstalled");
					clsLimit alimit = new clsLimit(qtyInstalled, 0, LockNull(rdr.Item("QtyMax"), 999, 1), LockNull(rdr.Item("incr_min"), 1, 1), LockNull(rdr.Item("incr_pref"), 1, 1));
					if (alimit.MinIncr == 1 & alimit.PrefIncr == 1 & alimit.Qinstalled == 0 & alimit.Qmin == 0 & alimit.Qmax == 999) {
						//no limit required 
						NoOp();
						//somewhere to hang a breakpoint
					} else {
						if (dicLimits.ContainsKey(ck)) {
							dicLimits(ck).MinIncr = alimit.MinIncr;
							dicLimits(ck).PrefIncr = alimit.PrefIncr;
							dicLimits(ck).Qinstalled = alimit.Qinstalled;
							dicLimits(ck).Qmax = alimit.Qmax;
							dicLimits(ck).Qmin = alimit.Qmin;

						} else {
							dicLimits.Add(ck, alimit);
						}
					}
				} else {
					//    Stop

				}
			}

		}
		rdr.Close();

		return dicLimits;

	}

	private object NoOp()
	{

	}

	private int LockNull(object f, int nullsubst, int min)
	{
		if (IsDBNull(f))
			return nullsubst;

		if (f < min)
			f = min;

		return f;

	}

	public object defaultWarranty()
	{

		SqlClient.SqlConnection con = da.OpenDatabase();
		SqlClient.SqlDataReader rdr;


		if (dicAbbreviations == null)
			LoadAbbreviations(con);

		object sql;


		DataTable wc = da.MakeWriteCacheFor(con, "productAttribute");


		//these are the most granular 'sub' families - family code
		//                  narrow     broad
		sql = "SELECT sysfamily,defaultWTY,familyRaid,instRaid,sysfamily_cat ";
		sql += "FROM " + server + "[iq].products.sysfamilydefinitions";
		rdr = da.DBExecuteReader(con, sql);

		//Group all products by their sysfamily
		Dictionary<string, List<clsProduct>> dsf = new Dictionary<string, List<clsProduct>>(StringComparer.CurrentCultureIgnoreCase);


		clsProductAttribute pa;
		string sysfam;
		foreach ( product in iq.Products.Values) {
			if (product.i_Attributes_Code.ContainsKey("FamMinor")) {
				pa = product.i_Attributes_Code("FamMinor")(0);
				sysfam = pa.Translation.text(English);
				if (!dsf.ContainsKey(sysfam))
					dsf.Add(sysfam, new List<clsProduct>());
				dsf(sysfam).Add(product);
			}
		}



		clsAttribute attrib_wty = null;
		iq.i_attribute_code.TryGetValue("DWTY", attrib_wty);
		if (attrib_wty == null)
			attrib_wty = new clsAttribute("dDWTY", iq.AddTranslation("CC-Warranty", English, "aNames", 0, null, 0, false), 0);

		clsAttribute attrib_fRAID = null;
		iq.i_attribute_code.TryGetValue("fRAID", attrib_fRAID);
		if (attrib_fRAID == null)
			attrib_fRAID = new clsAttribute("fRAID", iq.AddTranslation("CC-RAID", English, "aNames", 0, null, 0, false), 0);

		clsAttribute attrib_cat = null;
		iq.i_attribute_code.TryGetValue("sCat", attrib_cat);
		if (attrib_cat == null)
			attrib_cat = new clsAttribute("sCat", iq.AddTranslation("CC-Category", English, "aNames", 0, null, 0, false), 0);

		clsTranslation tl;

		while (rdr.Read) {
			sysfam = rdr.Item("sysfamily");
			if (dsf.ContainsKey(sysfam)) {

				foreach ( p in dsf(sysfam)) {
					//Default warranty
					if (!IsDBNull(rdr.Item("defaultwty"))) {
						string wtycode = rdr.Item("defaultwty");
						if (dicAbbreviations.ContainsKey(wtycode)) {
							tl = iq.AddTranslation(dicAbbreviations(wtycode), English, "DWTY", 0, null, 0, false);
							//Each translation will only be created once
							float value = Val(Left(tl.text(English), 1));
							//Take the value of the first char (which is usually the length in years) as the numeric value
							pa = new clsProductAttribute(p, attrib_wty, value, iq.i_unit_code("Text"), tl, wc);
						} else {
							Logit(sysfam + " defaultWTY " + wtycode + " does not exist in abbreviations");
						}
					}

					if (!IsDBNull(rdr.Item("familyRaid"))) {
						string raidcode = rdr.Item("familyRaid");
						if (dicAbbreviations.ContainsKey(raidcode)) {
							tl = iq.AddTranslation(dicAbbreviations(raidcode), English, "fRAID", 0, null, 0, false);
							//Each translation will only be created once
							pa = new clsProductAttribute(p, attrib_fRAID, 0, iq.i_unit_code("Text"), tl, wc);
						} else {
							Logit(sysfam + " familyRaid " + raidcode + " does not exist in abbreviations");
						}
					}

					if (!IsDBNull(rdr.Item("sysFamily_cat"))) {
						string catcode = rdr.Item("sysFamily_Cat");
						if (dicAbbreviations.ContainsKey(catcode)) {
							tl = iq.AddTranslation(dicAbbreviations(rdr.Item("sysFamily_cat")), English, "sCat", 0, null, 0, false);
							//Each translation will only be created once
							pa = new clsProductAttribute(p, attrib_cat, 0, iq.i_unit_code("Text"), tl, wc);
						} else {
							Logit(catcode + " sysFamily_cat " + catcode + " does not exist in abbreviations");
						}
					}
				}
			}
		}

		rdr.Close();

		da.BulkWrite(con, wc, "productAttribute");

		con.Close();

		Logit("Warranty import complete ", false, true);


	}


	public Dictionary<string, Dictionary<string, string>> FamilyOptTypeToOptFamily()
	{

		//Returns a dictionary of option type > optionfamily - per systemfamilyCODE (minorFamilty)
		//eg              dl580pg825NHPlff > HDD > NHP35LFFSC

		Dictionary<string, Dictionary<string, string>> dic;
		dic = new Dictionary<string, Dictionary<string, string>>(StringComparer.CurrentCultureIgnoreCase);

		SqlClient.SqlConnection con = da.OpenDatabase();
		SqlClient.SqlDataReader rdr;
		object sql;

		//these are the most granular 'sub' families - family code
		//                  narrow     broad


		sql = "SELECT * ";
		sql += "from " + server + "[iq].products.sysfamilydefinitions";
		rdr = da.DBExecuteReader(con, sql);

		Dictionary<string, string> cols;

		//Gives' slots are created for each sysystem - according to the OptionLimits

		while (rdr.Read) {
			cols = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
			if (dic.ContainsKey(rdr.Item("sysFamily")))
				continue;
			dic.Add(rdr.Item("sysFamily"), cols);

			if (!dic.ContainsKey(rdr.Item("sysfamilyname"))) {
				dic.Add(rdr.Item("sysfamilyname"), cols);
				//add a duplicate for the broader Sysfamilyname.. as come option limits are specified against this (grrr)
			}

			AddOTcol(cols, rdr, "MEM", "familyMem");
			AddOTcol(cols, rdr, "HDD", "familyPriStor");
			AddOTcol(cols, rdr, "OPT", "familySecStor");
			AddOTcol(cols, rdr, "FAN", "familyFan");
			AddOTcol(cols, rdr, "PSU", "familyPSU");
			AddOTcol(cols, rdr, "WTY", "familyWAR");
			AddOTcol(cols, rdr, "MAN", "familyMAN");
			AddOTcol(cols, rdr, "VGA", "familyVGA");
			AddOTcol(cols, rdr, "RAID", "familyRAID");
			//  AddOTcol(cols, rdr, "CPU", "familyMAN")

		}
		rdr.Close();
		con.Close();

		return dic;

	}


	private void AddOTcol(ref Dictionary<string, string> cols, SqlClient.SqlDataReader rdr, string opttype, string FamCol)
	{
		//add an option type code > Slot type to the inner dictionary of one sysFamily
		//eg. HDD > NHP35SFF (for a DL580pG7)

		string optFam;
		if (!IsDBNull(rdr.Item(FamCol))) {
			optFam = rdr.Item(FamCol);
			//If cols.ContainsKey(opttype) Then Stop
			cols.Add(opttype, optFam);
		}

	}



	public void Sectors(SqlClient.SqlConnection con, ref Dictionary<string, clsSector> dicSectors)
	{
		//returns a list of sectors by HP "BU" code

		object sql;
		sql = "select BUID2,BUlabelShort from " + server + "[channelcentral].products.translateBU";

		SqlClient.SqlDataReader rdr;
		rdr = da.DBExecuteReader(con, sql);

		clsSector sector;


		while (rdr.Read) {
			if (!dicSectors.ContainsKey(Trim(rdr.Item("BUID2")))) {
				sector = new clsSector(rdr.Item("BUID2"), iq.AddTranslation(rdr.Item("BULabelShort"), English, "BUs", 0, null, 0, false));
				dicSectors.Add(Trim(rdr.Item("BUID2")), sector);
			}

		}

		if (!dicSectors.ContainsKey("NoSector")) {
			sector = new clsSector("NoSector", iq.AddTranslation("No Sector", English, "BUs", 0, null, 0, false));
			dicSectors.Add("NoSector", sector);
		}

		rdr.Close();

	}
	/// <summary>
	/// Imports families 
	/// </summary>
	/// <returns>Returns a dictionary of Dans SysFamilyName to The family Branch I create for it</returns>
	/// <remarks></remarks>
	public Dictionary<string, clsBranch> Families(SqlClient.SqlConnection con, Dictionary<string, clsBranch> dicSysTypes)
	{

		Families = new Dictionary<string, clsBranch>(StringComparer.CurrentCultureIgnoreCase);

		SqlClient.SqlDataReader rdr;

		clsAttribute ba = new clsAttribute("bays", iq.AddTranslation("Drive bays", English, "attribs", 0, null, 0, 0), 0);
		clsAttribute hpa = new clsAttribute("HPL", iq.AddTranslation("Hot Pluggable", English, "attribs", 0, null, 0, 0), 0);


		//The family branches can only carry the Major' fore
		clsAttribute fMaj = iq.i_attribute_code("FamMajor");
		clsAttribute fMin = iq.i_attribute_code("FamMinor");
		clsAttribute fDisp = iq.i_attribute_code("FamDisp");

		//the Unabbreviated family name is the BRANCH.Translation


		clsTranslation lff = iq.AddTranslation("LFF", English, "bays", 0, null, 0, 0);
		clsTranslation lffL = iq.AddTranslation("Large form factor (3.5 inch) drive bays ", English, "bays", 0, null, 0, false);
		// Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

		clsTranslation sff = iq.AddTranslation("SFF", English, "bays", 0, null, 0, false);
		clsTranslation sffL = iq.AddTranslation("Small Form Factor (2.5 inch) drive bays ", English, "bays", 0, null, 0, false);
		// Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

		clsTranslation bff = iq.AddTranslation("Both", English, "bays", 0, null, 0, false);
		clsTranslation bffL = iq.AddTranslation("Has both Small Form Factor (2.5 inch) and Large Form Factor (3.5 inch) drive bays ", English, "bays", 0, null, 0, false);
		// Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

		clsTranslation HPL = iq.AddTranslation("HP", English, "bays", 0, null, 0, false);
		clsTranslation HPLL = iq.AddTranslation("Hot Pluggable", English, "bays", 0, null, 0, false);
		// Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

		object sql;
		sql = "SELECT DISTINCT sysfamilyname,systype,lifeCycleMonths,managementTxt,SecurityTxt,RangeText,subTitle,FamilyPriStor,FamilySecStor ";
		sql += "from " + server + "[iq].products.union_sysfamilydefinitions right join " + server + "[iq].products.sysrangetext ON sysfamilyname=rangename";
		rdr = da.DBExecuteReader(con, sql);


		//    '                             hello                 fr      bonjour
		//Dim xlate As New Dictionary(Of String, Dictionary(Of String, String))

		clsProduct product;
		// family branches need a product to attach additional attributes to (primarly descriptions)

		clsProductAttribute pa;

		clsTranslation sysTrans = iq.AddTranslation("systems", English, "collect", 0, null, 0, false);
		clsTranslation sysTransSingular = iq.AddTranslation("system", English, "collect", 0, null, 0, false);

		clsProductAttribute fnpa;

		clsBranch FamBranch;
		while (rdr.Read) {
			if (!IsDBNull(rdr.Item("sysfamilyname"))) {
				// this is the short (e.g. 'G8' version)
				if (!Families.ContainsKey(Trim(rdr.Item("sysfamilyname")))) {

					product = new clsProduct("", false, false, iq.i_sector_code("NoSector"), iq.i_ProductType_Code(rdr.Item("systype")), (System.DateTime)"01/01/2000", (System.DateTime)"31/12/2100", true, false, true,
					"", "", "");

					//record the family name under the 'majorFamily'  attribute on the branch - required for suppressing/displaying notes by family - see import.ExtText
					fnpa = new clsProductAttribute(product, fMaj, 0, iq.i_unit_code("txt"), iq.AddTranslation(Trim(rdr.Item("sysfamilyname")), English, "FamMajor", 0, null, 0, false));

					if (!object.ReferenceEquals(rdr.Item("lifecyclemonths"), DBNull.Value)) {
						pa = new clsProductAttribute(product, iq.i_attribute_code("lifeCycle"), rdr.Item("lifecyclemonths"), iq.i_unit_code("num"), iq.AddTranslation(rdr.Item("lifecyclemonths"), English, "", 0, null, 0, false));
					}

					if (!object.ReferenceEquals(rdr.Item("managementTxt"), DBNull.Value)) {
						pa = new clsProductAttribute(product, iq.i_attribute_code("management"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("managementTxt"), English, "", 0, null, 0, false));
					}

					if (!object.ReferenceEquals(rdr.Item("securityTxt"), DBNull.Value)) {
						pa = new clsProductAttribute(product, iq.i_attribute_code("security"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("securityTxt"), English, "", 0, null, 0, false));
					}

					if (!object.ReferenceEquals(rdr.Item("rangeText"), DBNull.Value)) {
						pa = new clsProductAttribute(product, iq.i_attribute_code("desc"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("rangeText"), English, "", 0, null, 0, false));
					}

					if (!object.ReferenceEquals(rdr.Item("subTitle"), DBNull.Value)) {
						pa = new clsProductAttribute(product, iq.i_attribute_code("subTitle"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("subTitle"), English, "", 0, null, 0, false));
					}

					//Large/small form factor dirve bays
					int bays = 0;
					//1=sff 2 = lff 3 = both

					if (!IsDBNull(rdr.Item("FamilyPriStor"))) {
						if (InStr(UCase(rdr.Item("FamilyPriStor")), "LFF")) {
							bays = bays | 2;
						}
						if (InStr(UCase(rdr.Item("FamilyPriStor")), "SFF")) {
							bays = bays | 1;
						}

						clsTranslation baytran = null;
						if (bays == 1)
							baytran = sff;
						if (bays == 2)
							baytran = lff;
						if (bays == 3)
							baytran = bff;
						// both form factors

						pa = new clsProductAttribute(product, iq.i_attribute_code("bays"), bays, iq.i_unit_code("txt"), baytran);

						if (InStr(UCase(rdr.Item("FamilyPriStor")), "HP") & InStr(UCase(rdr.Item("FamilyPriStor")), "NHP") == 0) {
							pa = new clsProductAttribute(product, iq.i_attribute_code("HPL"), 1, iq.i_unit_code("txt"), HPL);
						}
					}


					string code = rdr.Item("sysFamilyName");
					string FnEn;
					if (dicAbbreviations.ContainsKey(code.ToLower)) {
						FnEn = dicAbbreviations(code.ToLower);
						//'xlate()("en")
					} else {
						FnEn = code;
						Logit("no abbreviation for " + code);
					}

					clsTranslation fntl;
					//If iq.EnglishIndex.ContainsKey(FnEn) Then 'this is the abbreviation/key   - we do not append the word "family" (dans choice)
					// fntl = iq.EnglishIndex(FnEn)
					//Else
					fntl = iq.AddTranslation(FnEn, English, "", 0, null, 0, false);
					//               End If
					//
					FamBranch = new clsBranch(product, dicSysTypes(rdr.Item("systype")), fntl, "/images/iq/prod_" + rdr.Item("sysfamilyname") + ".gif", sysTrans, sysTransSingular, null, 100, false, "B");

					Families.Add(Trim(rdr.Item("sysfamilyname")), FamBranch);
					//add the family under its systype branch (Servers, Notebooks, desktops, storage etc)
					// - NO need - it's done internall now dicSysTypes(rdr.Item("systype")).childBranches.Add(FamBranch.ID, FamBranch)

				}
			}
		}

		rdr.Close();

	}



	public Dictionary<string, clsBranch> options2(SqlClient.SqlConnection con, Dictionary<string, string> dicplcode, Dictionary<string, clsUnit> dicUnits, ref Dictionary<clsProduct, List<clsRegion>> dicOptLocalisation, Dictionary<string, List<string>> containment)
	{
		//

		//NB: in Iq1 a 'family' is a 'narrow/specific' designation - and type is broad (major type)
		//This *always* confuses me

		options2 = new Dictionary<string, clsBranch>(StringComparer.CurrentCultureIgnoreCase);

		Dictionary<string, clsTranslation> dicSC = new Dictionary<string, clsTranslation>(StringComparer.CurrentCultureIgnoreCase);
		dicSC = new Dictionary<string, clsTranslation>();

		dicSC.Add("A", iq.AddTranslation("A", English, "SCC", 10, null, 0, false));
		dicSC.Add("TV", iq.AddTranslation("TV", English, "SCC", 20, null, 0, false));
		dicSC.Add("SB", iq.AddTranslation("SB", English, "SCC", 30, null, 0, false));
		dicSC.Add("R", iq.AddTranslation("R", English, "SCC", 30, null, 0, false));
		dicSC.Add("PR", iq.AddTranslation("PR", English, "SCC", 30, null, 0, false));
		dicSC.Add("GO", iq.AddTranslation("GO", English, "SCC", 30, null, 0, false));

		if (!iq.i_attribute_code.ContainsKey("Slots")) {
			clsAttribute sa = new clsAttribute("Slots", iq.AddTranslation("Slots used (legacy/import)", English, "", 0, null, 0, false), 0);
		}

		//If Not iq.i_attribute_code.ContainsKey("OptFam") Then  'Narrow
		//    Dim optfamAtt As clsAttribute = New clsAttribute("OptFam", iq.AddTranslation("Option Family (legacy/import)", English, "", 0, Nothing, 0, False), 0)
		//End If
		//Builds the global otpions tree- and returns Returns a dictionary of l1^l2^(l3)^OptFamily (slot.minor) > optfamily branch. 
		//e_Code(rdr.Item("opttype")), rdr.Item("activefrom"), rdr.Item("activeto"), rdr.Item("active"), rdr.Item("eol"), Not rdr.Item("AAonly"))

		LoadAbbreviations(con);
		object sql;

		sql = "SELECT v.OptSN,optsc,po.optsku,v.sortorder,fio,";
		sql += "case when po.sysfamily = ''  then isnull((select  sysfamilyname+', ' as 'data()' from h3.iq.products.systems inner join  h3.[iQ].[products].[SysFamilyDefinitions] on [SysFamilyDefinitions].sysfamily=systems.familycode  where PSU = po.optsku and opttype='PSU'  group by sysfamilyname FOR XML PATH('')),'') else po.sysfamily end as sysfamily,";
		sql += "speedUnitQty as speed,optTypeSpeedUnit as speedUnit,";
		sql += "unitQty as capacity,ot.optTypeUnit as capacityUnit,localisation,";
		sql += "technology,altsku,incompatible,v.L1,v.L2,v.L3, po.optfamily,po.opttype,po.opttype2,activeFromDate,activeToDate,active,eol,aaonly,descriptionHP,slots,ccdescription ";
		sql += "FROM h3.iq.products.V2_OptionCats v ";
		sql += "JOIN h3.iq.products.options po ON v.optsn=po.optsn ";
		sql += "JOIN h3.[iq].products.optTypes as OT on OT.optTypeCode=optType ";
		sql += "JOIN h3.[channelcentral].products.Hierarchy h ON h.upcNUM = po.optsku ";
		sql += "WHERE (sYSFAMILY LIKE '%" + restrictImportToFamily + "%' or sysfamily = '' or sysfamily is null or sysfamily='E2610') and active=1";
		//sql$ &= "WHERE active=1"

		int nextBid = 0;
		int nextProdID = 0;

		clsTranslation tlOptions = iq.AddTranslation("Options", English, "cats", 0, null, 0, false);
		clsTranslation tlOption = iq.AddTranslation("Option", English, "cats", 0, null, 0, false);

		//Write caches (for MUCH faster bulk writes)
		DataTable pawc = da.MakeWriteCacheFor(con, "ProductAttribute");
		DataTable bwc = da.MakeWriteCacheFor(con, "branch", nextBid, true);
		//nextID is SET by this call !

		DataTable twc = da.MakeWriteCacheFor(con, "Translation");
		DataTable pwc = da.MakeWriteCacheFor(con, "Product", nextProdID, true);
		int nextKey = clsTranslation.NextKey();

		clsTranslation tlacs = iq.AddTranslation("Accessories and Services", English, "cat", 0, twc, nextKey, false);
		clsBranch allOptions = new clsBranch(null, iq.RootBranch, tlacs, "/images/iq/accSvcs.gif", tlOptions, tlOption, null, 0, false, "B",
		bwc, nextBid);
		options2.Add("ALL", allOptions);

		SqlClient.SqlDataReader rdr;
		rdr = da.DBExecuteReader(con, sql);

		Dictionary<string, clsBranch> ldic = new Dictionary<string, clsBranch>(StringComparer.CurrentCultureIgnoreCase);

		clsBranch l1Branch;
		clsBranch l2Branch;
		clsBranch l3Branch;
		clsBranch l4Branch;

		clsBranch addTo;

		int options = 0;

		while (rdr.Read) {
			string ck = rdr.Item("l1").trim;
			//Can remove
			if (rdr.Item("optsku") == "###16MB_FB_128MB_SD_2MB") {
				object a = 1;
			}

			if (!ldic.ContainsKey(ck)) {
				l1Branch = new clsBranch(null, allOptions, iq.AddTranslation(rdr.Item("l1"), English, "OL1", 0, twc, nextKey, false), "", tlOptions, tlOption, null, rdr.Item("sortorder"), false, "B",
				bwc, nextBid);
				ldic.Add(ck, l1Branch);
			} else {
				l1Branch = ldic(ck);
			}

			addTo = null;

			ck = rdr.Item("l1").trim + "^" + rdr.Item("l2").trim;
			if (!ldic.ContainsKey(ck)) {
				l2Branch = new clsBranch(null, l1Branch, iq.AddTranslation(rdr.Item("l2"), English, "OL2", 0, twc, nextKey, false), "", tlOptions, tlOption, null, rdr.Item("sortorder"), false, "BGT",
				bwc, nextBid);
				ldic.Add(ck, l2Branch);
			} else {
				l2Branch = ldic(ck);
			}
			addTo = l2Branch;

			if (!object.ReferenceEquals(rdr.Item("l3"), DBNull.Value)) {
				ck = rdr.Item("l1").trim + "^" + rdr.Item("l2").trim + "^" + rdr.Item("l3").trim;
				if (!ldic.ContainsKey(ck)) {
					l3Branch = new clsBranch(null, l2Branch, iq.AddTranslation(rdr.Item("l3"), English, "OL3", 0, twc, nextKey, false), "", tlOptions, tlOption, null, rdr.Item("sortorder"), false, "BGT",
					bwc, nextBid);
					ldic.Add(ck, addTo);
				} else {
					l3Branch = ldic(ck);
				}
				addTo = l3Branch;
			}

			//optfamily is not globally unique... 5.25lff drives appear in optical and HDD
			object optfam = rdr.Item("optFamily");
			// is 'L4'

			string l3t = "";
			if (!object.ReferenceEquals(rdr.Item("l3"), DBNull.Value))
				l3t = rdr.Item("l3").trim.tolower;
			ck = rdr.Item("l1").trim + "^" + rdr.Item("l2").trim + "^" + l3t + "^" + optfam.trim;
			if (!ldic.ContainsKey(ck)) {
				object txt = "";
				if (dicAbbreviations.ContainsKey(optfam))
					txt = dicAbbreviations(optfam);
				else
					txt = Replace(txt, "_", " ");
				if (txt == null)
					txt = "";
				l4Branch = new clsBranch(null, addTo, iq.AddTranslation(txt, English, "OL4", 0, twc, nextKey, false), "", tlOptions, tlOption, null, rdr.Item("sortorder"), false, "G",
				bwc, nextBid);
				ldic.Add(ck, l4Branch);
			} else {
				l4Branch = ldic(ck);
			}

			if (!options2.ContainsKey(ck)) {
				if (ck.ToLower.Contains("amd socket"))
					System.Diagnostics.Debugger.Break();
				options2.Add(ck, l4Branch);
			}

			string otc = rdr.Item("opttype");
			//these are broad
			string otc2 = IsDBNull(rdr.Item("opttype2")) ? "" : rdr.Item("opttype2");
			//ML horrid but don't understand opttype2 and its causing data categorization issues for cables, even giving them a W value
			if (otc2 == "CAB")
				otc = "CAB";

			clsProduct OptionProduct = null;

			if (iq.i_ProductType_Code.ContainsKey(otc)) {
				clsProductType pt = iq.i_ProductType_Code(otc);
				System.DateTime af = (System.DateTime)"01/01/1980";
				System.DateTime at = (System.DateTime)"01/01/2400";

				if (!IsDBNull(rdr.Item("activeFromDate")))
					af = rdr.Item("activeFromDate");
				if (!IsDBNull(rdr.Item("activeToDate")))
					at = rdr.Item("activeToDate");

				OptionProduct = new clsProduct(rdr.Item("optsku"), false, true, iq.Sectors.Values(0), pt, af, at, rdr.Item("active"), rdr.Item("eol"), !rdr.Item("AAonly"),
				"", "", "", pwc, nextProdID);

				clsTranslation TLdesc = null;

				if (!IsDBNull(rdr.Item("ccDescription"))) {
					object dsc = rdr.Item("ccdescription");
					if (rdr.Item("ccDescription").tolower.contains("amd cpu"))
						System.Diagnostics.Debugger.Break();

					TLdesc = iq.AddTranslation(rdr.Item("ccDescription"), English, "OPTDSC", 0, twc, nextKey, false);
					clsBranch optionbranch = new clsBranch(OptionProduct, l4Branch, TLdesc, "", tlOptions, tlOption, null, rdr.Item("sortorder"), false, "B",
					bwc, nextBid);
					addOptionAttributes(OptionProduct, pawc, twc, nextKey, rdr, dicplcode, dicUnits, TLdesc);
				} else {
					Logit("Missing description");
				}
			} else {
				Logit("Missing opttype:" + otc);
			}

			//Supply Chain Focus Attribute
			if (!IsDBNull(rdr.Item("optSC"))) {
				string optsc = Trim(rdr.Item("optsc"));
				if (optsc != "" & optsc != "Z") {
					clsProductAttribute SCfa = new clsProductAttribute(OptionProduct, iq.i_attribute_code("focus"), 0, iq.i_unit_code("txt"), dicSC(optsc), pawc);
				}
			}

			//systypefocus attribute

			options += 1;

			//Populate the Dictionary of option localisations (Countries in which it's active) - which is used later in BuildTree
			//we DO NOT add options which are not localised to the dicoptlocalisation dictionary (they are unrestricted)


			string rgns = "";
			if (!IsDBNull(rdr.Item("localisation")))
				rgns = rdr.Item("localisation");

			if (rdr.Item("aaonly") != 0) {
				rgns += ",AA";
			}


			if (rgns != "") {

				if (OptionProduct != null) {
					List<clsRegion> regions = new List<clsRegion>();
					List<string> cs = Split(rgns, ",").ToList;

					//Anything paul has localised 'worldwide' needs no restriction
					if (!cs.Contains("XW")) {

						cleanRegions(cs, containment);

						foreach ( c in cs) {
							if (c == "UCSA")
								c = "USCA";
							//fix a typo
							if (iq.i_region_code.ContainsKey(c)) {
								regions.Add(iq.i_region_code(c));
							} else {
								Logit("invalid region " + c + " (in products.options.localisation)");
								//    Stop
							}
						}
						dicOptLocalisation.Add(OptionProduct, regions);
					}
				}
			}
		}

		rdr.Close();

		da.BulkWrite(con, twc, "translation", , true);
		da.BulkWrite(con, pwc, "product", , true);
		da.BulkWrite(con, bwc, "branch", , true);
		da.BulkWrite(con, pawc, "productattribute");

		con.Close();

		StreamWriter sw = new StreamWriter("c:\\temp\\allOptions.txt");
		allOptions.toDisk(sw, 0, "");
		sw.Close();

	}


	public void DoPrunes()
	{

		//Options are generally set a compatible with broad families - (sysfamilyName)
		//however, certain option types (HDD,MEM etc) - have further contstraints based on the more granular (and badly named) optFamily (eg.25SFFNHPHDD)
		//and the equally confusing  sysfamilyCode  (which i call Minor Family)


		int nextpruneid = 0;

		SqlClient.SqlConnection con = da.OpenDatabase();
		DataTable pwc = da.MakeWriteCacheFor(con, "prune", nextpruneid);

		Dictionary<string, Dictionary<string, string>> dic = Import.FamilyOptTypeToOptFamily;

		int kept = 0;
		int pruned = 0;

		iq.RootBranch.DoPrunes(pwc, nextpruneid, "tree.1", "", dic, kept, pruned);

		int rows = pwc.Rows.Count;

		da.BulkWrite(con, pwc, "Prune", 10000, true);

		con.Close();

	}

	public object SoftwareSlots()
	{

		//make gives slot on systems for a qty record on all software options

		//Build a dictioanry of FamilyMinor code >List(of systems)
		Dictionary<string, List<clsBranch>> families = new Dictionary<string, List<clsBranch>>(StringComparer.CurrentCultureIgnoreCase);
		foreach ( b in iq.Branches.Values) {
			if (b.Product != null) {
				if (b.Product.isSystem) {
					string famname = b.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English);
					if (!families.ContainsKey(famname)) {
						families.Add(famname, new List<clsBranch>());
					}
					families(famname).Add(b);

					famname = b.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English);
					if (!families.ContainsKey(famname)) {
						families.Add(famname, new List<clsBranch>());
					}
					families(famname).Add(b);

				}
			}
		}

		object sql = "SELECT [SysFamily],isnull(optTypeParent,'Miscellaneous') as optCat,[OptType],[QtyInstalled],[QtyMax],[Incr_Min],[Incr_Pref] from h3.[iq].[products].[OptionLimits] ";
		sql += "INNER join h3.[iq].[products].[opttypes] o on o.OptTypeCode = opttype ";
		sql += "where opttype like 'sof1%'";

		SqlClient.SqlConnection con = da.OpenDatabase;


		DataTable swc = da.MakeWriteCacheFor(con, "Slot");
		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql);

		List<clsProduct> systems = new List<clsProduct>();

		List<string> duds = new List<string>();

		while (rdr.Read) {
			string sf = rdr.Item("sysfamily");
			if (families.ContainsKey(sf)) {
				foreach ( SysBranch in families(sf)) {
					if (!SysBranch.hasSlot(iq.i_slotType_Code("SOF")("OPERATING_SYSTEMS"))) {
						clsSlot aslot = new clsSlot(iq.i_slotType_Code("SOF1")("OPERATING_SYSTEMS"), SysBranch, "", rdr.Item("qtymax"), null, new NullableInt(), 0, 0, swc);
						clsSlot aslot2 = new clsSlot(iq.i_slotType_Code("SOF2")("OPERATING_SYSTEMS"), SysBranch, "", rdr.Item("qtymax"), null, new NullableInt(), 0, 0, swc);
						// Dim aslot3 As clsSlot = New clsSlot(iq.i_slotType_Code("SOF3")("OPERATING_SYSTEMS"), SysBranch, "", rdr.Item("qtymax"), Nothing, New NullableInt, 0, 0, swc)
					}
				}
			} else {
				if (!duds.Contains(sf)) {
					duds.Add(sf);
				}
			}

		}

		rdr.Close();


		int rc = swc.Rows.Count;

		da.BulkWrite(con, swc, "Slot");

		con.Close();



	}

	//Do we need this?? ML


	public void chassisMemSlots()
	{
		object sql;
		sql = "SELECT ol.[SysFamily],isnull(optTypeParent,'Miscellaneous') as optCat,[OptType],[QtyInstalled],[QtyMax],[Incr_Min],[Incr_Pref],familymem  from h3.[iq].[products].[OptionLimits] ol ";
		sql += "INNER join h3.[iq].[products].[opttypes] o on o.OptTypeCode = ol.opttype ";
		sql += "join h3.iq.products.sysfamilydefinitions sfd on ol.SysFamily = sfd.sysfamily ";
		sql += "where opttype like 'mem'";


		//build a dictionary of all the systems in every major and minor family
		Dictionary<string, List<clsBranch>> families = new Dictionary<string, List<clsBranch>>(StringComparer.CurrentCultureIgnoreCase);
		foreach ( b in iq.Branches.Values) {
			if (b.Product != null) {
				if (b.Product.isSystem & !b.Product.isOption) {
					string famname = b.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English);
					if (!families.ContainsKey(famname)) {
						families.Add(famname, new List<clsBranch>());
					}
					families(famname).Add(b);

					//famname = b.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English)
					//If Not families.ContainsKey(famname) Then
					//    families.Add(famname, New List(Of clsBranch))
					//End If
					//families(famname).Add(b)

				}
			}
		}

		SqlClient.SqlConnection con = da.OpenDatabase;


		DataTable swc = da.MakeWriteCacheFor(con, "Slot");
		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql);

		List<clsProduct> systems = new List<clsProduct>();

		List<string> duds = new List<string>();

		int added = 0;

		while (rdr.Read) {
			string sf = rdr.Item("sysfamily");
			//this is the granular (minor) one
			if (families.ContainsKey(sf)) {

				foreach ( SysBranch in families(sf)) {
					bool found = false;
					foreach ( branch in SysBranch.childBranches.Values) {
						if (branch.Translation.text(English).ToLower.Contains("chassis")) {
							if (!branch.HasMajorSlot("MEM")) {
								//If Not SysBranch.hasSlot(iq.i_slotType_MinorCode("OPERATING_SYSTEMS")) Then
								if (!object.ReferenceEquals(rdr.Item("familymem"), DBNull.Value)) {
									string fm = rdr.Item("familyMem");
									branch.i_Slots.Clear();
									//MAKE SURE WE MAKE IT !
									clsSlot aslot = new clsSlot(iq.i_slotType_Code("MEM")(fm), branch, "", rdr.Item("qtymax"), null, new NullableInt(), 0, 0, swc);
									added += 1;
									found = true;

									//End If
								}
							}
						}
					}
					//  If Not found Then Stop

					break; // TODO: might not be correct. Was : Exit For
					//we only need do this for the first branch in the family as they all share a chassis
				}
			} else {
				if (!duds.Contains(sf)) {
					duds.Add(sf);
				}
			}

		}

		rdr.Close();


		int rc = swc.Rows.Count;

		da.BulkWrite(con, swc, "Slot");

		Debug.Print(added);

		con.Close();



	}


	public object Buildtree2(SqlClient.SqlConnection con, Dictionary<string, clsBranch> ProductCat, Dictionary<string, clsBranch> dicfamilies, Dictionary<string, clsBranch> dicsystems, Dictionary<clsProduct, List<clsRegion>> dicOptLocalisation)
	{

		//NB: in Iq1 a 'family' is a 'narrow/specific' designation - and type is broad (major type)
		//This *always* confuses me

		//Products.options tell us which (broad) families an option is available under 
		//however ! .. Option limits are specified per narrow sysfamily
		//so we graft the same set of options to every system in a broad family - but apply limits at the narrower level


		List<string> ERRORMESSAGES = new List<string>();

		int kept = 0;
		int pruned = 0;

		Dictionary<string, clsSlotType> dicSlotTypes;

		//Return a dictionary of minorSlot Type codes to slot types (needs systems dictionary because of some PCI stuff)
		dicSlotTypes = Import.slotTypes(con, dicsystems);
		//dicFamily) '20 secs

		//Build a dictionary to look up the slot type per minorFamily/option type
		//                                           minorfamily            option type
		//Dim dicSubFamOptTypeSlotType As Dictionary(Of String, Dictionary(Of String, clsSlotType))
		//dicSubFamOptTypeSlotType = Import.SubFamiliyOptionTypes(con, dicSlotTypes)


		//Returns a dictionary of option type > optionfamily - per systemfamilyCODE (minorFamilty)
		//eg              dl580pg825NHPlff > HDD > NHP35LFFSC
		Dictionary<string, Dictionary<string, string>> ofDic = Import.FamilyOptTypeToOptFamily();
		//Gives a lookup of narrow optfamily from BroadOptType per sysfamily

		//OPTION LIMITS 
		//build a dictionary by sysfamily/option family of the Limits - used later to attach instances of clsQuantity (autoAdds/Preinstalled) to the option branches

		//    '                                                  sysSubFam      optionfaimily       limit
		//    '                                                                       NHP35lff
		Dictionary<string, IQ.clsLimit> dicOptLimits;
		//returns a dictinoary of the narrow,minor sysFamilyCode^optfamily>clslimit EG.. DL580PLFF^NHPLFF>blah
		dicOptLimits = Import.BuildOptLimits(con, ofDic);

		writeDicOptLimits(dicOptLimits, "c:\\temp\\optlimits.txt");

		// FACTORY INSTALLED OPTIONS/components - call them what you will
		// get a list (by system mfrSKU) of the part numbers and quantities of all factory installed components (PriStor, sec stor CPU, MEM etc,Raid)

		Dictionary<string, Dictionary<string, int>> dicFIOs;
		//returns a list (by system mfrSKU) of the quantities and part numbers of all factory installed components (PriStor, sec stor CPU, MEM etc)
		dicFIOs = Import.FIOs(con);

		WriteDicFIOs(dicFIOs, "c:\\temp\\fios.txt");


		int NEXTbId = 0;
		int nextpruneid = 0;

		DataTable bwc = da.MakeWriteCacheFor(con, "branch", NEXTbId, true);
		//nextID is SET by this call !
		DataTable Gwc = da.MakeWriteCacheFor(con, "GRAFT");
		DataTable qwc = da.MakeWriteCacheFor(con, "quantity");
		DataTable swc = da.MakeWriteCacheFor(con, "slot");
		DataTable pwc = da.MakeWriteCacheFor(con, "prune", nextpruneid);

		int nextkey = clsTranslation.NextKey;
		DataTable tlwc = da.MakeWriteCacheFor(con, "Translation");

		object sql;

		sql = "SELECT v.OptSN,po.optsku,v.sortorder,";
		sql += "speedUnitQty as speed,optTypeSpeedUnit as speedUnit,sysfamily,";
		sql += "unitQty as capacity,ot.optTypeUnit as capacityUnit,";
		sql += "technology,altsku,incompatible,v.L1,v.L2,v.L3, po.optfamily,po.opttype,activeFromDate,activeToDate,active,eol,aaonly,descriptionHP,slots,ccdescription ";
		sql += "FROM h3.iq.products.V2_OptionCats v ";
		sql += "JOIN h3.iq.products.options po ON v.optsn=po.optsn ";
		sql += "JOIN h3.[iq].products.optTypes as OT on OT.optTypeCode=optType ";
		sql += "JOIN h3.[channelcentral].products.Hierarchy h ON h.upcNUM = po.optsku ";
		sql += "WHERE (sYSFAMILY LIKE '%" + restrictImportToFamily + "%' or sysfamily = '' or sysfamily is null) and active=1 order by sysfamily";
		//sql$ &= "where active=1 order by sysfamily"

		object rdr = da.DBExecuteReader(con, sql);

		sql = "SELECT * FROM h3.iQ.products.SysFamilyDefinitions";
		DataTable FamilyOptionDefs;
		FamilyOptionDefs = da.FilledDataTable(con, sql);

		//CK branches contians an 'all options' branch for every family (and holds references to every sub-branch to 
		Dictionary<string, clsBranch> ckBranches;
		//Compound key of Sysfamily^l1^l2^l3>Branch
		ckBranches = new Dictionary<string, clsBranch>(StringComparer.CurrentCultureIgnoreCase);
		clsTranslation tlOptions = iq.AddTranslation("Options", English, "collect", 0, tlwc, nextkey, false);
		clsTranslation tlOption = iq.AddTranslation("Option", English, "collect", 0, tlwc, nextkey, false);
		clsBranch allOptions;

		// this is the All options branch for each family (we will prune incompatibles)
		Dictionary<string, clsBranch> carepacks = new Dictionary<string, clsBranch>(StringComparer.CurrentCultureIgnoreCase);
		//record the carepack branch under each Broad Family name

		clsTranslation tlcarepack = iq.AddTranslation("Care Pack", English, "cats", 0, tlwc, nextkey, false);
		clsTranslation tlcarepacks = iq.AddTranslation("Care Packs", English, "cats", 0, tlwc, nextkey, false);


		while (rdr.Read) {
			// WRONG - >>>>>>>>for every option - add its parent branch (ie, it, and all its sibilings) to the family branch.. if it's not there already
			// the optfamily must match the sysfamily defintions FamilyPriStro,Secstop for optical and HDDS
			// TODO - If rdr.Item("optfamily") = dicfamilies(rdr.option("sysFamilies").product) Then
			// what we'ere actually doing, is constructing an all options branch per (broad) sysfamily

			object l3 = "";

			if (!object.ReferenceEquals(rdr.Item("l3"), DBNull.Value)) {
				l3 = rdr.Item("l3");
			}

			string optfamily = rdr.Item("Optfamily");
			// CAREPACK

			// This is a CD list of broad families eg DL580PG8
			if (!object.ReferenceEquals(rdr.Item("sysfamily"), DBNull.Value)) {

				[] sf = Split(rdr.Item("sysfamily"), ",");


				foreach ( f in sf) {
					string ck;
					ck = f + "^" + rdr.Item("l1").trim + "^" + rdr.Item("l2").trim + "^" + l3.Trim;
					// & "^" & optfamily.Trim

					//'switch between OR True and OR False to restirct what's imported
					if ((f.ToUpper.Contains(restrictImportToFamily.ToUpper)) | string.IsNullOrEmpty(restrictImportToFamily)) {
						if (dicfamilies.ContainsKey(f)) {
							clsBranch fambranch = dicfamilies(f);
							// these are broad families - possible options are defined at the broad level (option limits are defined at the narrower familycode level)
							clsBranch holder = makeholder(rdr, ckBranches, f, tlwc, nextkey, bwc, NEXTbId, tlOptions, tlOption);

							string opttype = rdr.Item("Opttype");


							if (opttype.ToUpper.Trim == "CPU") {
							// CPU's are handled very differently - see import.cpus
							// (only the CPU preinstalled in the system is an option for it - and CPUs enable banks of memory etc)

							} else {
								string optsku = rdr.Item("optSKU");
								clsProduct anOption = iq.i_SKU(optsku);

								clsTranslation SKUTL = iq.AddTranslation(anOption.SKU, English, "SKU", 10, null, 0, false);

								// The branch.translation is the part number (Points to the same TL)
								clsBranch branch = new clsBranch(anOption, holder, SKUTL, "", tlOption, tlOptions, null, 0, false, "B",
								bwc, NEXTbId);

							}
						}

					} else {
					}

				}
			}

		}

		rdr.Close();
		int done = 0;

		List<string> invalidSlotTypes = new List<string>();

		clsBranch chassisBranch;
		//        Dim chassisVariant As clsVariant
		//        Dim chassisProduct As clsProduct
		clsBranch chassisRoot;
		clsTranslation chassisTL = iq.AddTranslation("Chassis", English, "", 0, tlwc, nextkey, false);
		//  Dim chassisProductType As clsProductType = (From j In iq.ProductTypes.Values Where j.Code = "CHAS").First

		//BROAD FAMILIES - but we're grafting onto systems

		Dictionary<string, clsBranch> chassis = new Dictionary<string, clsBranch>(StringComparer.CurrentCultureIgnoreCase);

		int grafted = 0;


		foreach ( f in ckBranches.Keys) {

			if (!f.Contains("^")) {
				//create a flat dictionary of the options, by SKU
				//SKU>OptionPath  (it's path under the system)

				//switch between OR True and OR False to restirct what's imported
				if ((f.ToUpper.Contains(restrictImportToFamily.ToUpper)) | string.IsNullOrEmpty(restrictImportToFamily)) {

					//For debug - 
					StreamWriter sw = new StreamWriter("c:\\temp\\FAMS\\" + f + ".txt");
					ckBranches(f).toDisk(sw, 0, "");
					sw.Close();

					clsBranch aoBranch = ckBranches(f);
					Dictionary<string, string> optionPaths = aoBranch.OptionPaths("." + (string)aoBranch.ID);
					//NOTE - theses are the paths below the system

					//find the paths (relative to the system) of the (previously 'tagged') optfamily/holder Branches
					//                            tag>Path
					//  Dim ofpths As Dictionary(Of String, String) = aoBranch.TaggedPaths("." & CStr(aoBranch.ID))

					if (!dicfamilies.ContainsKey(f)) {
						Beep();
					} else {
						clsBranch fb = dicfamilies(f);
						bool firstinfamily = true;

						//chassisProduct = New clsProduct(False, )
						//chassisBranches.Add(sysSubFamily, chassisBranch)

						//                    'Systems Name (is SKU)
						//                    'Dim n As clsProductAttribute = New clsProductAttribute(chassisProduct, iq.i_attribute_code("~ame"), 0, iq.i_unit_code("txt"), dicFamily(sysfamkey).Translation, ProductAttributeWriteCache)
						//                    Dim cq As clsQuantity = New clsQuantity(r_worldwide, "", chassisBranch, 1, 0, 0, True, QuantityWriteCache) 'make a global auto add - to add the chassis to the system
						//                    'all the gives slots are in the chassis (for now)
						//                    ' MakeGivesSlots(chassisBranch, dicOptLimits(sysSubFamily), dicSlotTypes, dicSubFamOptTypeSlotType, QuantityWriteCache, SlotWriteCache)

						//DicOptLimits sysFamilyCode^optfamily>clslimit EG.. DL580PLFF^NHPLFF>blah


						foreach ( systembranch in fb.childBranches.Values) {
							//were grafting on at root.sector.family.system

							object systempath;
							 // ERROR: Not supported in C#: WithStatement



							foreach ( child in systembranch.childBranches.Values) {
								if (child.Translation.text(English) == "All Options) Then") {
									System.Diagnostics.Debugger.Break();
									//ut oh - wer'e trying to graft it on twice !
								}
							}

							object systemsku = systembranch.Product.SKU;

							systembranch.Graft(ckBranches(f), "buildtree2", "", ERRORMESSAGES, Gwc);
							grafted += 1;

							string sysMinorFamily;
							//comes from the iq.systems.familycode
							sysMinorFamily = iq.i_SKU(systemsku).i_Attributes_Code("FamMinor")(0).Translation.text(English);
							//IMPORTANT for compatibility
							if (sysMinorFamily == "")
								System.Diagnostics.Debugger.Break();

							if (chassis.ContainsKey(sysMinorFamily)) {
								chassisBranch = chassis(sysMinorFamily);
							} else {
								//chassisProduct = New clsProduct("", False, False, iq.i_sector_code("HPPSG"), iq.i_ProductType_Code("CHAS"), DateAdd(DateInterval.Day, -500, Now), DateAdd(DateInterval.Day, 10000, Now), True, False, True, "", "", "")
								//chassisVariant = New clsVariant("", chassisProduct, HP, chassisProduct.ID.ToString, "", "", "", r_worldwide, 0) 'Every product needs a variant - so it can be stored in a QuoteItem
								chassisBranch = new clsBranch(null, null, iq.AddTranslation(f + " chassis", English, "UI", 0, tlwc, nextkey, false), "", chassisTL, chassisTL, null, 100, true, "B",
								bwc, NEXTbId);


								//chassis branch needs to be per MinorFamily !!
								//Gives Slots
								clsSlot gslot;
								foreach ( k in dicOptLimits.Keys) {
									string[] bits = Split(k, "^");
									if (LCase(bits(0)) == LCase(sysMinorFamily)) {
										//for every narrow OptFamily in the sysfamily
										clsLimit Limit = dicOptLimits(k);



										if (UCase(bits(1)) == "FAMILYMEM")
											System.Diagnostics.Debugger.Break();
										string optfamily = bits(2);
										string opttype = bits(1);

										//If Left(optfamily, 3).ToLower = "mem" Then Stop
										if (iq.SlotTypes.Values.Where(sst => sst.MajorCode == opttype & sst.MinorCode == optfamily).Count > 0) {
											// iq.i_slotType_MinorCode.ContainsKey(optfamily) Then

											clsSlotType st = iq.SlotTypes.Values.Where(sst => sst.MajorCode == opttype & sst.MinorCode == optfamily).First;
											//                                            If st.MajorCode = "MEM" Then Stop

											//the gives stos do NOT need a path (systempath & "." & chassisBranch.ID) - becuase they are active weherever this subchassis appears
											gslot = new clsSlot(st, chassisBranch, "", Limit.Qmax, null, new NullableInt(), Limit.Qmin, 0, swc);
										} else {
											//Weird ones like PSUm which is in option limits as PSU??? why                                            
											// If invalidSlotTypes Is Nothing Then
											if (!invalidSlotTypes.Contains(sysMinorFamily + "^" + opttype + "^" + optfamily)) {
												invalidSlotTypes.Add(sysMinorFamily + "^" + opttype + "^" + optfamily);
											}
											//End If
										}
										//
										//     MakeGivesSlots(chassisBranch, dicOptLimits(sysSubFamily), dicSlotTypes, dicSubFamOptTypeSlotType, QuantityWriteCache, SlotWriteCache)
									}
								}
								chassis.Add(sysMinorFamily, chassisBranch);
							}
							systembranch.Graft(chassisBranch, "", "", ERRORMESSAGES, Gwc);
							//makes autoAdds (quantities) for FIOs, Takes slots 
							// and prunes incompatible option branches (by thier narrow sysfamilies)
							// Double check to see if we have a part in dicFIO's which is NOT in all options, should never happen
							if (dicFIOs.ContainsKey(systembranch.Product.SKU)) {
								foreach ( s in dicFIOs(systembranch.Product.SKU).Keys) {
									//Do we have a slot for this FIO???
									if (!optionPaths.ContainsKey(s)) {
										//Add it somewhere here??? Todo ML
										if (s == "###16MB_FB_128MB_SD_2MB") {
											object a = 9;
										}
										string fioPath = systempath;
										if (iq.i_SKU.ContainsKey(s)) {
											clsBranch branch = new clsBranch(iq.i_SKU(s), systembranch.FindBranchByNameBelow("FIOs", fioPath, false, 0), iq.AddTranslation(s, English, "", 0, null, 0, false), "", tlOption, tlOptions, null, 0, false, "B",
											bwc, NEXTbId);
											isFIO(s, systembranch.Product.SKU, fioPath + "." + branch.ID, dicFIOs, dicOptLocalisation, new clsLimit(dicFIOs(systembranch.Product.SKU)(s), 0, dicFIOs(systembranch.Product.SKU)(s), null, null), qwc);

										}

									}
								}
							}

							foreach ( optionsku in optionPaths.Keys) {
								if (optionsku == "###16MB_FB_128MB_SD_2MB") {
									object a = 1;
								}
								MakeLimits(systempath, optionsku, optionPaths(optionsku), Gwc, swc, pwc, nextpruneid, qwc, firstinfamily, dicOptLimits,
								dicSlotTypes, dicOptLocalisation, dicFIOs, systemsku, kept, pruned, chassisBranch, systembranch, FamilyOptionDefs);
							}
							//If dicFIOs.ContainsKey(systembranch.Product.sku) Then ' Double check to see if we have a part in dicFIO's which is NOT in all options, should never happen
							//    For Each s In dicFIOs(systembranch.Product.sku).Keys
							//        'Do we have a slot for this FIO???


							//        If Not optionPaths.ContainsKey(s) Then
							//            'ML - maybe need to add these to an FIO branch under the system / family? These are things like RAM for HPN
							//            isFIO(s, systembranch.Product.sku, systempath, dicFIOs, dicOptLocalisation, New clsLimit(dicFIOs(systembranch.Product.sku)(s), 0, dicFIOs(systembranch.Product.sku)(s), Nothing, Nothing), qwc)
							//            Logit("Preinstalled part found but no match in all options: " & systemsku & ":" & s)
							//            '            Dim a = 1 
							//            '            'Add optionfi

							//            '            '  MakeLimits(systempath, s, Nothing _
							//            '            '        , Gwc, swc, pwc, nextpruneid, qwc, firstinfamily, _
							//            '            '        dicOptLimits, dicSlotTypes, dicOptLocalisation, _
							//            '        dicFIOs, systemsku, kept, pruned)
							//        End If
							//    Next
							//End If
							//prune Optical and HDD drives which are not compatible with their minorFamilies
							//locate the optfamily branches

							//Dim syspruned As Integer = 0

							//If systembranch.Product.i_Attributes_Code.ContainsKey("PriStor") Then
							//    Dim pristor As String = systembranch.Product.i_Attributes_Code("PriStor")(0).Translation.text(English)

							//    For Each k In ofpths.Keys 'there are the paths of the nodes to which we graft optyfamily branches - (like NHP35LFF)
							//        Dim bits() = Split(k, "^")
							//        If bits(0) = "HDD" And bits(1) <> pristor And bits(1) <> "" Then
							//            Dim pruneat As String = systempath & ofpths(k)
							//            Dim jj As String = iq.Branches(2084).DisplayName(English)
							//            Dim aprune = New clsPrune(pruneat, New NullableInt, "import", pwc, nextpruneid)
							//            syspruned += 1
							//        End If
							//    Next
							//End If

							//   If syspruned > 0 Then
							object syssku = systembranch.SKU;
							StreamWriter ssw = new StreamWriter("c:\\temp\\SYSTEMS\\" + syssku + ".txt");
							systembranch.toDisk(ssw, 0, systempath);
							ssw.Close();
							//End If

							//        firstinfamily = False

						}
						done += 1;

					}
				}
			}
			//Only do DL's - REMOVE
		}
		//family

		Debug.Print(grafted);

		da.BulkWrite(con, qwc, "quantity");
		da.BulkWrite(con, bwc, "Branch", , true);
		da.BulkWrite(con, Gwc, "Graft");
		da.BulkWrite(con, tlwc, "translation");
		da.BulkWrite(con, swc, "slot");
		da.BulkWrite(con, pwc, "prune", , true);

		con.Close();

		Logit("These slot types were invalid");
		foreach ( s in invalidSlotTypes) {
			Logit(s);
		}
		Logit("End of list", false, true);


	}

	private clsBranch makeholder(SqlClient.SqlDataReader rdr, ref Dictionary<string, clsBranch> ckBranches, string famname, DataTable tlwc, ref int nextkey, DataTable bwc, ref int nextbid, clsTranslation tloptions, clsTranslation tloption, string outPath = "")
	{

		//makes (or returns) a bottom level category branch to which we attach options

		clsBranch FamAlloptBranch;
		clsBranch l1branch;
		clsBranch l2branch;
		clsBranch l3branch;
		clsBranch FIOBranch;
		int order = rdr.Item("sortorder");
		//   Dim alloptions As clsBranch

		object ck;

		if (ckBranches == null)
			ckBranches = new Dictionary<string, clsBranch>(StringComparer.CurrentCultureIgnoreCase);

			//An all options branch is created for each family (hanging in space) 
			//it is subsequently grafted on to every system in the family
			//(and pruned off those subFamilies with which it is incompatible)



			//Level branches (1 to 3/4) for option in one family









		 // ERROR: Not supported in C#: WithStatement


	}


	public object addOptionAttributes(clsProduct optionProduct, DataTable pawc, DataTable twc, ref int nextKey, SqlClient.SqlDataReader rdr, Dictionary<string, string> dicplcode, Dictionary<string, clsUnit> dicunits, clsTranslation tldesc)
	{

		clsProductAttribute incompatible;
		clsProductAttribute altsku;
		clsProductAttribute anAttribute;
		clsProductAttribute mfrsku;
		clsProductAttribute plcode;

		// Dim ssde As clsTranslation = iq.AddTranslation("Solid State Drive", English, "DriveType")
		clsUnit textUnit = iq.i_unit_code("txt");
		if (textUnit == null)
			System.Diagnostics.Debugger.Break();


		clsProductAttribute desc = new clsProductAttribute(optionProduct, iq.i_attribute_code("desc"), 0, textUnit, tldesc, pawc);

		//record the options OptFamily - this is the MinorOption type - but isn't globally unique..
		//eg. HPL35inchLFF may appear under oth OPT and HDD opt types
		anAttribute = new clsProductAttribute(optionProduct, iq.i_attribute_code("optFamily"), 0, textUnit, iq.AddTranslation(rdr.Item("optfamily"), English, "", 0, twc, nextKey, false), pawc);

		//This IS used in the quote summary (amongst other places)
		if (Len(rdr.Item("opttype")) > 5)
			System.Diagnostics.Debugger.Break();
		anAttribute = new clsProductAttribute(optionProduct, iq.i_attribute_code("optType"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("optType"), English, "", 0, twc, nextKey, false), pawc);

		//If Not iq.Attributes.ContainsKey("MfrSKU") Then j = New clsAttribute("MfrSKU", New clsText(iq.addTranslation("MfrSKU")))

		clsProductAttribute speed;
		clsProductAttribute capacity;
		if (!IsDBNull(rdr.Item("speed"))) {
			//Some things (tape drives/Graphics cards/batteries have 'speeds' without units - we're not imprtiong - mentioned to dan 02/08/2012
			if (!IsDBNull(rdr.Item("speedunit"))) {
				speed = new clsProductAttribute(optionProduct, iq.i_attribute_code("speed"), rdr.Item("speed"), dicunits(rdr.Item("speedUnit")), null, pawc);
			}
		} else {
			if (rdr.Item("Opttype") == "HDD") {
				//HDD's without a speed are SSD's - give them a numerically high RPM (so they sort to the 'top' speed wise - but display the text SSD (instead of 100,000 rpm)
				clsTranslation ssd = iq.AddTranslation("SSD", English, "DriveType", 0, twc, nextKey, false);
				speed = new clsProductAttribute(optionProduct, iq.i_attribute_code("speed"), 100000, iq.i_unit_code("txt"), ssd, pawc);
			}
		}


		if (!IsDBNull(rdr.Item("capacity"))) {
			object uk;
			//'Some things (cables,newtork cards have  capacities without units - we're not importing - mentioned to dan 02/08/2012 - am now... with a TXT unit
			if (!IsDBNull(rdr.Item("capacityUnit"))) {
				uk = rdr.Item("capacityUnit");
			} else {
				uk = "txt";
			}

			capacity = new clsProductAttribute(optionProduct, iq.i_attribute_code("capacity"), rdr.Item("capacity"), iq.i_unit_code(uk), null, pawc);

		}


		if (!IsDBNull(rdr.Item("opttype2"))) {
			object ot2 = new clsProductAttribute(optionProduct, iq.i_attribute_code("opttype2"), 0, textUnit, iq.AddTranslation(rdr.Item("opttype2"), English, "", 0, twc, nextKey, false), pawc);
		}

		string optsku = rdr.Item("optsku");

		if (!IsDBNull(rdr.Item("technology"))) {
			object t = rdr.Item("technology");
			int cp;
			cp = InStr(t, "CORE");
			int numcores;
			if (cp) {
				numcores = Val(Left(t, cp - 1));
				//  If numcores = 3 Or numcores = 5 Or numcores = 7 Or numcores > 16 Then Stop 'odd number of cores
				clsProductAttribute cores = new clsProductAttribute(optionProduct, iq.i_attribute_code("cores"), numcores, iq.i_unit_code("num"), null, pawc);

				int numthreads;
				cp = InStr(t, "TH");
				if (cp) {
					numthreads = Val(Mid(t, cp - 2, 2));
					clsProductAttribute threads = new clsProductAttribute(optionProduct, iq.i_attribute_code("threads"), numthreads, iq.i_unit_code("num"), null, pawc);
				}
			}
		}

		// mfrsku = New clsProductAttribute(optionProduct, iq.i_attribute_code("MfrSKU"), 0, textUnit, iq.AddTranslation(Trim$(rdr.Item("OptSKU")), English, "", 0, twc, nextKey, False), pawc)
		object pl;

		if (!dicplcode.ContainsKey(rdr.Item("optSKU"))) {
			Logit("No PL code for option '" + rdr.Item("Optsku") + "' (not in HeirarchyIQ).");
		} else {
			pl = dicplcode(rdr.Item("optSKU"));
			plcode = new clsProductAttribute(optionProduct, iq.i_attribute_code("PLcode"), 0, textUnit, iq.AddTranslation(pl, English, "", 0, twc, nextKey, false), pawc);
		}

		//Dim opttype As clsProductAttribute
		//Dim opt$
		//opt$ = rdr.Item("OptType")
		//opttype = New clsProductAttribute(optionproduct, iq.Attributes("OptType"), 0, iq.Units("txt"), iq.addTranslation(opt, twc).Key, awc)
		//End If

		if (!IsDBNull(rdr.Item("incompatible"))) {
			if (Trim(rdr.Item("incompatible")) != "") {
				object ic = Replace(rdr.Item("incompatible"), " ", "");
				incompatible = new clsProductAttribute(optionProduct, iq.i_attribute_code("incompat"), 0, textUnit, iq.AddTranslation(ic, English, "incompat", 0, twc, nextKey, false), pawc);
			}
		}

		if (!IsDBNull(rdr.Item("altsku"))) {
			if (Trim(rdr.Item("altsku")) != "") {
				altsku = new clsProductAttribute(optionProduct, iq.i_attribute_code("altSKU"), 0, textUnit, iq.AddTranslation(Replace(rdr.Item("altSKU"), " ", ""), English, "atSKU", 0, twc, nextKey, false), pawc);
			}
		}

		//required later when making 'takes' slots - to respect iq.products.options.slots
		clsProductAttribute slots = new clsProductAttribute(optionProduct, iq.i_attribute_code("Slots"), rdr.Item("slots"), iq.i_unit_code("num"), null, pawc);
		//Dont do this for PSU enablement kits, they dont take a PSU slot....
		if (!IsDBNull(rdr.Item("technology")) && rdr.Item("technology") == "UPGRADE") {
			slots.NumericValue = 0;
		}

		if (!IsDBNull(rdr.Item("technology"))) {
			object tech = new clsProductAttribute(optionProduct, iq.i_attribute_code("technology"), 0, textUnit, iq.AddTranslation(Replace(rdr.Item("technology"), " ", ""), English, "", 0, twc, nextKey, false), pawc);
		}


		clsAttribute ofa;
		if (!iq.i_attribute_code.ContainsKey("optFam")) {
			ofa = new clsAttribute("optFam", iq.AddTranslation("Options family", English, "", 0, twc, nextKey, false), 0);
		} else {
			ofa = iq.i_attribute_code("optFam");
		}

		object ofm = rdr.Item("OptFamily");
		clsProductAttribute optfam = new clsProductAttribute(optionProduct, ofa, 0, textUnit, iq.AddTranslation(ofm, English, "", 0, twc, nextKey, false), pawc);

	}


	//Public Function options(con As SqlClient.SqlConnection, ByRef OptionsBySKU As Dictionary(Of String, clsProduct), _
	//                   dicPlcode As Dictionary(Of String, String), _
	//                   ByRef dicOptLocalisation As Dictionary(Of clsProduct, List(Of clsRegion)), _
	//                   dicUnits As Dictionary(Of String, clsUnit), _
	//                   containment As Dictionary(Of String, List(Of String))) _
	//                   As Dictionary(Of String, clsProduct)

	//    'Options returns a dictionary of compound key 
	//    'Dim ck$ = sysfamily & "^" & rdr.Item("l1") & "^" & rdr.Item("l2") & "^" & l3 & "^" & rdr.Item("optsn")


	//    options = New Dictionary(Of String, clsProduct)

	//    dicOptLocalisation = New Dictionary(Of clsProduct, List(Of clsRegion))

	//    Dim sql$
	//    Dim rdr As SqlClient.SqlDataReader

	//    'Makes option Procucts and multi-dimensional dictionary of them

	//    'Options

	//    'OptSN is and IQ1 PK (unique ID)
	//    sql$ = "SELECT po.optSN,po.optsku,sysfamily,optType,cu.optTypeParent as optCat,optfamily,technology,active,activetodate,altsku,eol,fiosysfamily,descriptionHP"
	//    sql$ &= ",ccDescription,incompatible,h.bucode,localisation,unitQty as capacity,SpeedUnitQty as speed, "
	//    sql$ &= "su.OptTypeSpeedUnit as speedUnit,Cu.OptTypeUnit as capacityUnit,Technology ,slots,aaonly,l1,l2,l3 "
	//    sql$ &= "from " & server$ & "[iq].Products.options "
	//    sql$ &= "join " & server$ & "[channelcentral].products.Hierarchy h ON h.upcNUM = optsku "
	//    sql$ &= "join " & server$ & "[iq].products.optTypes as su on su.optTypeCode=optType "
	//    sql$ &= "join " & server$ & "[iq].products.optTypes as cu on cu.optTypeCode=optType "
	//    sql$ &= "join " & server$ & "[iq].products.optTypeParents as pu on pu.optTypeParent=cu.optTypeParent "
	//    sql$ &= "join products.V2_OptionCats v on v.optsn=po.optsn "
	//    sql$ &= "WHERE active=1 "
	//    'sql$ &= "where sysfamily like '%DL380%'"
	//    sql$ &= "ORDER BY pu.ParentRank,su.OptTypeRank"

	//    '                          performance,storage etc, HDD,TAP etc

	//    'the ordering is new


	//    'becuase (for example_ CPU's are not an option Type for laptops.. then laptops get no CPU

	//    'Create a set of options under each optType, under each Broad SysFamily name (options.sysfamily,sysfamilydefinitons.sysfamilyname - NOT the (narrow) sysfamilycode) 
	//    'We will subsequently graft the optType branches (containg the products from the inner most dictionary)
	//    'under each (pre-existing) system branch in the sysfamily 

	//    'We create something like this (in DicSysFam)
	//    'DL385G5p (family)
	//    '     +Performance (opttype parent) -(optCat)
	//    '        + MEM (option type)
	//    '              + MEM_PC3-10600SODIMM (option family)
	//    '                    +DDR 3 (technology)
	//    '                        HP SB 8GB Dual Rank x4 PC3-10600 (DDR3-1333MHz) (option)
	//    '                        HP 16GB Quad Rank x4 PC3-8500 RDIMM (option
	//    '      +Storage (optTypeParent)
	//    '        + HDD (option type)
	//    '              +5.25LFF (option family)
	//    '                    + SATA (technology)
	//    '                        750GB SATA 1.5G 7K Mid-Line HDD (option)
	//    '                        .... (etc)

	//    'the outer dictionary is keyed by sysfamily - and exposes a set of optTypes (MEM,HDD etc)
	//    'the optTypes expose a dictionary of option families .. etc 

	//    Dim attribwritecache As DataTable
	//    attribwritecache = da.MakeWriteCacheFor(con, "ProductAttribute")

	//    Dim TranslationWriteCache As DataTable
	//    TranslationWriteCache = da.MakeWriteCacheFor(con, "Translation")

	//    rdr = da.DBExecuteReader(con, sql$)

	//    'Dim optType As Dictionary(Of String, Dictionary(Of Integer, clsProduct))  '
	//    'optType = New Dictionary(Of String, Dictionary(Of Integer, clsProduct))

	//    'we will make one of these for each option catergory (optTypeParent)  (storage, performance etc) - it contains the opt Types... (eg. HDD/MEM etc)
	//    '                                               cat                            type                              family                     tech                           SN
	//    'Dim dicOptCat As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, clsProduct)))))  '
	//    '  dicOptCat = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, clsProduct)))))

	//    Dim anOption As clsProduct

	//    Dim count As Integer = 0
	//    '        Dim optTypeName As String
	//    '        Dim optFamilyName As String
	//    '        Dim technologyName As String

	//    Dim anAttribute As clsProductAttribute
	//    Dim MfrSKU As clsProductAttribute
	//    Dim PLCode As clsProductAttribute
	//    Dim Incompatible As clsProductAttribute

	//    Dim ssd As clsTranslation = iq.AddTranslation("SSD", English, "DriveType")
	//    Dim ssde As clsTranslation = iq.AddTranslation("Solid State Drive", English, "DriveType")

	//    'circa 110 secs

	//    Dim ProductType As clsProductType
	//    Dim l2$

	//    Dim textUnit As clsUnit = iq.i_unit_code("txt")
	//    Dim sector As clsSector

	//    While rdr.Read

	//        l2$ = rdr.Item("l2")
	//        If iq.i_ProductType_Code.ContainsKey(l2) Then
	//            ProductType = iq.i_ProductType_Code(l2$)
	//        Else
	//            If iq.i_ProductType_Code.ContainsKey("FIO") Then
	//                ProductType = iq.i_ProductType_Code("FIO") 'Nothing  'these are FIO's (fingerprint readers, Multi card readers etc.. that are never standalone products - so there are no productTypes for them)
	//            Else
	//                ProductType = New clsProductType("FIO", iq.AddTranslation("Factory Installed/Ancillary Option", English))
	//            End If
	//        End If

	//        If iq.i_sector_code.ContainsKey("BUcode") Then
	//            sector = iq.i_sector_code(rdr.Item("BUcode"))
	//        Else
	//            sector = iq.i_sector_code("NoSector")
	//        End If

	//        'If rdr.Item("ccDescription") Is DBNull.Value Then Stop

	//        If OptionsBySKU.ContainsKey(rdr.Item("optsku")) Then 'Have we already made this option ((on a previous import)
	//            'Beep()
	//            anOption = OptionsBySKU(rdr.Item("optsku"))
	//        Else

	//            Dim activeto As Date = CDate("31/12/2100")
	//            If Not IsDBNull(rdr.Item("activetodate")) Then activeto = rdr.Item("activeToDate")

	//            Dim publish As Boolean = True
	//            If rdr.Item("AAonly") <> 0 Then publish = False
	//            anOption = New clsProduct(CStr(rdr.Item("ccDescription")), False, sector, ProductType, CDate("01/01/2000"), activeto, rdr.Item("active"), rdr.Item("eol"), publish)

	//            'Populate the Dictionary of option localisations (Countries in which it's active) - which is used later in BuildTree
	//            'we DO NOT add options which are not localised to the dicoptlocalisation dictionary (they are unrestricted)
	//            If Not IsDBNull(rdr.Item("localisation")) Then
	//                Dim regions As List(Of clsRegion) = New List(Of clsRegion)

	//                Dim cs As List(Of String) = Split(rdr.Item("localisation"), ",").ToList

	//                If Not cs.Contains("XW") Then   'Anything paul has localised 'worldwide' needs no restriction
	//                    cleanRegions(cs, containment)
	//                    For Each c In cs
	//                        If c = "UCSA" Then c = "USCA" 'fix a typo
	//                        If iq.i_region_code.ContainsKey(c) Then
	//                            regions.Add(iq.i_region_code(c))
	//                        Else
	//                            Logit("invalid region " & c & " (in products.options.localisation)")
	//                            '    Stop
	//                        End If
	//                    Next
	//                    dicOptLocalisation.Add(anOption, regions)
	//                End If
	//            End If

	//            'record the options OptFamily
	//            anAttribute = New clsProductAttribute(anOption, iq.i_attribute_code("optFamily"), 0, textUnit, iq.AddTranslation(rdr.Item("optfamily"), English, , , TranslationWriteCache), attribwritecache)

	//            'This IS used in the quote summary (amongst other places)
	//            anAttribute = New clsProductAttribute(anOption, iq.i_attribute_code("optType"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("optType"), English, , , TranslationWriteCache), attribwritecache)

	//            'If Not iq.Attributes.ContainsKey("MfrSKU") Then j = New clsAttribute("MfrSKU", New clsText(iq.addTranslation("MfrSKU")))

	//            Dim speed As clsProductAttribute
	//            Dim capacity As clsProductAttribute
	//            If Not IsDBNull(rdr.Item("speed")) Then
	//                If Not IsDBNull(rdr.Item("speedunit")) Then  'Some things (tape drives/Graphics cards/batteries have 'speeds' without units - we're not imprtiong - mentioned to dan 02/08/2012
	//                    speed = New clsProductAttribute(anOption, iq.i_attribute_code("speed"), rdr.Item("speed"), dicUnits(rdr.Item("speedUnit")), Nothing, attribwritecache)
	//                End If
	//            Else
	//                If rdr.Item("Opttype") = "HDD" Then
	//                    'HDD's without a speed are SSD's - give them a numerically high RPM (so they sort to the 'top' speed wise - but display the text SSD (instead of 100,000 rpm)
	//                    speed = New clsProductAttribute(anOption, iq.i_attribute_code("speed"), 100000, iq.i_unit_code("txt"), ssd, attribwritecache)
	//                End If
	//            End If

	//            If Not IsDBNull(rdr.Item("capacity")) Then

	//                Dim uk$
	//                If Not IsDBNull(rdr.Item("capacityunit")) Then ''Some things (cables,newtork cards have  capacities without units - we're not importing - mentioned to dan 02/08/2012 - am now... with a TXT unit
	//                    uk$ = rdr.Item("capacityunit")
	//                Else
	//                    uk$ = "txt"
	//                End If

	//                capacity = New clsProductAttribute(anOption, iq.i_attribute_code("capacity"), rdr.Item("capacity"), iq.i_unit_code(uk$), Nothing, attribwritecache)

	//            End If

	//            If Not IsDBNull(rdr.Item("technology")) Then
	//                Dim t$ = rdr.Item("technology")
	//                Dim cp As Integer
	//                cp = InStr(t$, "CORE")
	//                Dim numcores As Integer
	//                If cp Then
	//                    numcores = Val(Left$(t$, cp - 1))
	//                    Dim cores As clsProductAttribute = New clsProductAttribute(anOption, iq.i_attribute_code("cores"), numcores, iq.i_unit_code("num"), Nothing)

	//                    Dim numthreads As Integer
	//                    cp = InStr(t$, "TH")
	//                    If cp Then
	//                        numthreads = Val(Mid$(t$, cp - 2, 2))
	//                        Dim threads As clsProductAttribute = New clsProductAttribute(anOption, iq.i_attribute_code("threads"), numthreads, iq.i_unit_code("num"), Nothing)
	//                    End If
	//                End If
	//            End If

	//            MfrSKU = New clsProductAttribute(anOption, iq.i_attribute_code("MfrSKU"), 0, textUnit, iq.AddTranslation(Trim$(rdr.Item("OptSKU")), English, "SKU", , TranslationWriteCache), attribwritecache)
	//            Dim pl$

	//            If Not dicPlcode.ContainsKey(rdr.Item("optSKU")) Then
	//                Logit("No PL code for option '" & rdr.Item("Optsku") & "' (not in HeirarchyIQ).")
	//            Else
	//                pl = dicPlcode(rdr.Item("optSKU"))
	//                PLCode = New clsProductAttribute(anOption, iq.i_attribute_code("PLcode"), 0, textUnit, iq.AddTranslation(pl, English, , , TranslationWriteCache), attribwritecache)
	//            End If

	//            'Dim opttype As clsProductAttribute
	//            'Dim opt$
	//            'opt$ = rdr.Item("OptType")
	//            'opttype = New clsProductAttribute(anOption, iq.Attributes("OptType"), 0, iq.Units("txt"), iq.addTranslation(opt, TranslationWriteCache).Key, attribwritecache)
	//            'End If

	//            If Not IsDBNull(rdr.Item("incompatible")) Then
	//                If Trim$(rdr.Item("incompatible")) <> "" Then
	//                    Incompatible = New clsProductAttribute(anOption, iq.i_attribute_code("incompat"), 0, textUnit, _
	//                    iq.AddTranslation(Replace(rdr.Item("incompatible"), " ", ""), English, , , TranslationWriteCache), attribwritecache)
	//                End If
	//            End If

	//            If Not IsDBNull(rdr.Item("altsku")) Then
	//                If Trim$(rdr.Item("altsku")) <> "" Then
	//                    Incompatible = New clsProductAttribute(anOption, iq.i_attribute_code("altSKU"), 0, textUnit, _
	//                    iq.AddTranslation(Replace(rdr.Item("altSKU"), " ", ""), English, , , TranslationWriteCache), attribwritecache)
	//                End If
	//            End If

	//            If Not IsDBNull(rdr.Item("altsku")) Then
	//                If Trim$(rdr.Item("altsku")) <> "" Then
	//                    Incompatible = New clsProductAttribute(anOption, iq.i_attribute_code("altSKU"), 0, textUnit, _
	//                    iq.AddTranslation(Replace(rdr.Item("altSKU"), " ", ""), English, , , TranslationWriteCache), attribwritecache)
	//                End If
	//            End If

	//            'required later when making 'takes' slots - to respect iq.products.options.slots
	//            If Not iq.i_attribute_code.ContainsKey("Slots") Then
	//                Dim sa As clsAttribute = New clsAttribute("Slots", iq.AddTranslation("Slots used (legacy/import)", English, , , TranslationWriteCache), 0)
	//            End If
	//            Dim slots As clsProductAttribute = New clsProductAttribute(anOption, iq.i_attribute_code("Slots"), rdr.Item("slots"), iq.i_unit_code("num"), Nothing, attribwritecache)

	//            If Not iq.i_attribute_code.ContainsKey("OptFam") Then
	//                Dim optfamAtt As clsAttribute = New clsAttribute("OptFam", iq.AddTranslation("Option Family (legacy/import)", English, , , TranslationWriteCache), 0)
	//            End If
	//            Dim optfam As clsProductAttribute = New clsProductAttribute(anOption, iq.i_attribute_code("OptFam"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("OptFamily"), English, , , TranslationWriteCache))

	//            '  If Not IsDBNull(rdr.Item("speedUnitQuantity")) Then
	//            '      speed = New clsProductAttribute(anOption,IQ.Attributes(IQ.i_attribute("Speed")
	//            ' End If

	//        End If
	//        If Not rdr.Item("sysfamily") Is DBNull.Value Then
	//            For Each sysfamily In Split(rdr.Item("sysfamily"), ",")
	//                sysfamily = Trim$(sysfamily)
	//                If sysfamily <> "" Then

	//                    Dim l3 As String
	//                    If rdr.Item("l3") = DBNull.Value Then
	//                        l3 = ""
	//                    Else
	//                        l3 = rdr.Item("l3")
	//                        If l3 = UCase(l3) And Len(l3) > 3 Then l3 = capitalise(l3)
	//                    End If
	//                    Dim ck$ = sysfamily & "^" & rdr.Item("l1") & "^" & rdr.Item("l2") & "^" & l3 & "^" & rdr.Item("optsn")

	//                    'some options are listed as being in the same family more than once 
	//                    ' so ignore - if it's already listed
	//                    If Not options.ContainsKey(ck) Then
	//                        'add this as a general otpion for the family - it may get pruned later in some contexts
	//                        options.Add(ck, anOption)
	//                        count += 1

	//                        'create a handy lookup in a 'flat' dictionary
	//                        If Not OptionsBySKU.ContainsKey(Trim$(rdr.Item("optsku"))) Then
	//                            OptionsBySKU.Add(Trim$(rdr.Item("optsku")), anOption)
	//                        End If

	//                        'Make 'Takes' slots on this branch from Products.options.[Slots] and .[OptType]
	//                        'hard to do when the branch doesn't exist yet !

	//                        'the exact slot type (optfamily) in question must be looked up from the optType and sysfamily
	//                        'we can use the dictionary to look it up (may need a second pass) <<< (go through options table again.. use Dic(fam)(opttype) to get optfams
	//                        'make Gives and Takes slots - for this branch the slotaddqty and slotaddtype (which refers to a set of opt types)
	//                        'eg -3;4   RAC,OPT
	//                        'or
	//                        '5 MEM

	//                    End If
	//                End If
	//            Next
	//        End If
	//        'End If
	//    End While

	//    rdr.Close()

	//    'write all the accumulated ProductAttributes
	//    Dim Pas As Integer = attribwritecache.Rows.Count
	//    da.BulkWrite(con, attribwritecache, "ProductAttribute")
	//    attribwritecache = Nothing


	//    da.BulkWrite(con, TranslationWriteCache, "Translation")

	//    Logit("Done options", False, True)


	//End Function

	public List<string> cleanRegions(List<string> i, Dictionary<string, List<string>> containment)
	{

		//For each non-country region in the list 'dirty' list 'I', remove all contained, regions/countries
		//containment - is a pre-prepared dicitionary of the descendants of each region


		//First - see if all the countries of any region are in the list - and if so add that region (in lieu of the many countries)

		//for each region code..
		foreach ( rc in i.ToList) {
			if (rc != "" & iq.i_region_code.ContainsKey(rc)) {
				clsRegion r = iq.i_region_code(rc);
				//get a reference to the actual region

				if (!r.isCountry) {
					if (!i.Contains(r.Code)) {
						if (containment(r.Code).Intersect(i).Count == containment(r.Code).Count) {
							//all the countries in this region are in the list
							i.Add(r.Code);
							// add  the region (we will subsequently remove all of the consitiuent countries)
						}
					}
				}
			} else {
				if (rc != "") {
					//Note, UK is NOT a valid country code
					//        Beep()
				}

			}
		}


		if (i.Contains("XW"))
			System.Diagnostics.Debugger.Break();
		//Sustitute GWE for EMEA if both are present
		if (i.Contains("GWE")) {
			if (i.Contains("EMEA"))
				i.Remove("EMEA");
		}

		List<string> o = new List<string>();

		List<string> toremove = new List<string>();
		//for each region code..
		foreach ( rc in i) {
			if (rc == "UCSA")
				rc = "USCA";
			//fix a typo
			if (rc != "" & iq.i_region_code.ContainsKey(rc)) {
				clsRegion r = iq.i_region_code(rc);
				//get a reference to the actual region

				if (!r.isCountry) {
					toremove = toremove.Union(clsRegion.containment(rc)).ToList;
					//remove all the countries (and regions) this region contains
					toremove.Remove(r.Code);
					//@@@
				}
			}
		}

		//   cleanRegions = From j In i Where Not j in toremove 'didnt work
		if (toremove.Count) {
			cleanRegions = i.Except(toremove).ToList;
			//very Neat LINQ
			if (cleanRegions.Count < i.Count) {
				Logit("collapsed " + Join(i.ToArray, ",") + " to " + Join(cleanRegions.ToArray, ","));
				//   Beep()
			}
		} else {
			cleanRegions = i;
		}



	}

	//Public Sub countries(con As SqlClient.SqlConnection, ByRef dicCountries As Dictionary(Of String, clsCountry), dicCountryCurrencies As Dictionary(Of String, clsCurrency))

	//    Dim sql$
	//    Dim rdr As SqlClient.SqlDataReader

	//    sql$ = "SELECT countrycode,[CountryName],[Currency],[Region],[active],[MainsV],[Notes],[possible] from " & server$ & "[iq].dbo.countries"

	//    rdr = da.dbexecuteReader(con, sql$)

	//    Dim acountry As clsCountry
	//    While rdr.Read
	//        If Not dicCountries.ContainsKey(Trim$(rdr.Item("countrycode"))) Then
	//            acountry = New clsCountry(rdr.Item("countrycode"), iq.AddTranslation(rdr.Item("CountryName"), English), DefaultCulture(rdr.Item("countrycode")))
	//            dicCountries.Add(Trim$(acountry.Code), acountry)
	//            If Not IsDBNull(rdr.Item("currency")) Then
	//                dicCountryCurrencies.Add(Trim$(rdr.Item("countrycode")), iq.i_currency_code(rdr.Item("currency")))  'used when importing quotes
	//            End If
	//        End If
	//    End While
	//    rdr.Close()



	//End Sub


	public void ProductTypes(SqlClient.SqlConnection con, ref Dictionary<string, clsProductType> dicOptType)
	{
		SqlClient.SqlDataReader rdr;
		clsProductType aproducttype;
		//Dim dicOptType As New Dictionary(Of String, clsProductType)

		//populate the dictionary of all option types (MEM/HDD/CPU)
		rdr = da.DBExecuteReader(con, "SELECT optTypecode as code, optTypename as name from " + server + "[iq].products.optTypes");

		//'Dim np As clsSector = iq.i_sector_code("nonProdut")

		int existed;
		int added;


		while (rdr.Read) {
			object lc = Trim(LCase(rdr.Item("code")));
			if (!dicOptType.ContainsKey(lc)) {
				aproducttype = new clsProductType(lc, iq.AddTranslation(Abbreviation(rdr.Item("name")), English, "PT", 0, null, 0, false), 0);
				dicOptType.Add(lc, aproducttype);
				added += 1;
			} else {
				existed += 1;
			}

		}
		rdr.Close();

		Logit("Loaded " + dicOptType.Count + " option type codes, " + existed + " existed, " + added + " added.");


		// I *think* this is  replaced by systypes
		if (!dicOptType.ContainsKey("DTO")) {
			aproducttype = new clsProductType("DTO", iq.AddTranslation("Desktop", English, "TOP", 0, null, 0, false), 0);
			dicOptType.Add("DTO", aproducttype);
			aproducttype = new clsProductType("NBK", iq.AddTranslation("Notebook", English, "TOP", 0, null, 0, false), 0);
			dicOptType.Add("NBK", aproducttype);
			aproducttype = new clsProductType("SVR", iq.AddTranslation("Server", English, "TOP", 0, null, 0, false), 0);
			dicOptType.Add("SVR", aproducttype);
			aproducttype = new clsProductType("SWD", iq.AddTranslation("Storage device", English, "TOP", 0, null, 0, false), 0);
			dicOptType.Add("SWD", aproducttype);
			//storage
			aproducttype = new clsProductType("HPN", iq.AddTranslation("Network device", English, "TOP", 0, null, 0, false), 0);
			dicOptType.Add("HPN", aproducttype);
			//networking
		}

		//    Return dicOptType

	}

	public void users(SqlClient.SqlConnection con, Dictionary<string, clsChannel> dicChannels, ref Dictionary<string, clsAccount> dicAccounts, ref Dictionary<string, clsTeam> DicTeams, Dictionary<string, clsUser> dicUsers)
	{
		object sql;
		SqlClient.SqlDataReader rdr;
		clsAccount anaccount;
		clsUser auser = null;
		clsChannel buyer;
		clsChannel seller;
		string chanid;

		//USERS (& Accounts)
		//each user can have accounts with many sellers - they will have one username, but a password for each seller
		//Dictionary used to construct priceBand table - first index is the host (seller)  - pointing to a dictionary of BuyerAccounts(buyers)>priceBands
		//users are also loaded into the buyer channel teams

		//TEAMS

		clsTeam aTeam;
		rdr = da.DBExecuteReader(con, "select TeamID,ChanID,TeamName from " + server + "[channelcentral].customers.host_teams");
		object lc;
		while (rdr.Read) {
			lc = Trim(LCase(rdr.Item("teamID")));
			if (!DicTeams.ContainsKey(lc)) {
				aTeam = new clsTeam(dicChannels(Trim(rdr.Item("chanid"))), Trim(rdr.Item("TeamName")));
				DicTeams.Add(lc, aTeam);
			}
		}
		rdr.Close();

		int nextUID = 0;
		int nextACid = 0;
		DataTable uwc = da.MakeWriteCacheFor(con, "user", nextUID, true);
		DataTable awc = da.MakeWriteCacheFor(con, "account", nextACid, true);

		int countUsers;

		sql = "SELECT username,[password],realname,chanID,priceBand,realname,admin,email,team,tel1,tel2,[admin],[lang],[disabled],";
		sql += "(SELECT TOP 1 currency from " + server + "[channelcentral].[customers].[HostAccounts] where priceBand = u.priceBand) as currency";
		sql += " from " + server + "[channelcentral].customers.users u " + !string.IsNullOrEmpty(restrictImportToFamily) ? " WHERE username like '%@channelcentral.net'" : "" + " order by ltrim(rtrim(email))";
		//order by email so users multiple accounts appear together

		dicAccounts = new Dictionary<string, clsAccount>(StringComparer.CurrentCultureIgnoreCase);

		Dictionary<string, clsAccount> DicHostAccounts;
		DicHostAccounts = new Dictionary<string, clsAccount>(StringComparer.CurrentCultureIgnoreCase);

		rdr = da.DBExecuteReader(con, sql);
		string email = "xxx";
		bool dud;
		clsRole role;

		while (rdr.Read) {
			dud = false;

			if (Left(rdr.Item("Username"), 2) == "IQ") {
				chanid = Split(rdr.Item("username"), "_")(1);
				//seller dist 'EG Computer Gross (DCOIT00143)


				if (!dicChannels.ContainsKey(chanid)) {
					Logit("channel " + chanid + " does not exist");


				} else {
					seller = dicChannels(chanid);
					//seller dist 'EG Computer Gross (DCOIT00143)

					if (dicChannels.ContainsKey(Trim(rdr.Item("chanID")))) {
						buyer = dicChannels(Trim(rdr.Item("chanID")));
						//BUYER 'EG tcsystems.is - will have a priceBand
					} else {
						Logit("The buyer channelID " + Trim(rdr.Item("ChanID")) + " referenced in channelcentral.customers.users.chanID does not exist in channelcentral.customers.channel ");
						dud = true;
						buyer = null;
						//    Stop
					}

					if (Trim(rdr.Item("email")) == "")
						dud = true;
					//there are some duds

					if (!dud) {
						//we make (multiple) accounts (for the same user) until the user changes (the list we're iterating is ordered by email)
						//each user has many accounts (potentially)
						if (LCase(Trim(rdr.Item("email"))) != LCase(Trim(email))) {
							if (!dicUsers.ContainsKey(rdr.Item("username"))) {
								auser = new clsUser(buyer, (string)rdr.Item("email"), rdr.Item("realname"), new nullableString(rdr.Item("tel1")), new nullableString(rdr.Item("tel2")), uwc, nextUID);
							}
						}
						email = Trim(rdr.Item("email"));

						clsTeam team = null;
						if (!IsDBNull(rdr.Item("team"))) {
							if (DicTeams.ContainsKey(Trim(rdr.Item("team")))) {
								team = DicTeams(Trim(rdr.Item("Team")));
								team.Members.Add(auser);
							} else {
								Logit("Team " + Trim(rdr.Item("team")) + " referenced in channelcentral.customers.users.team is not present in channelcentral.customers.host_teams");
								//    Stop
							}
						}

						object cur;
						if (IsDBNull(rdr.Item("currency"))) {
							cur = "GBP";
						} else {
							cur = rdr.Item("currency");
						}
						if (cur == "nul")
							cur = "GBP";
						//fix for bad data (techdata)

						clsRole arole;
						if (!iq.i_role_Code.ContainsKey("user"))
							arole = new clsRole("user", iq.AddTranslation("User", English, "UI", 0, null, 0, false));
						if (!iq.i_role_Code.ContainsKey("admin"))
							arole = new clsRole("admin", iq.AddTranslation("Administrator", English, "UI", 0, null, 0, false));


						if (rdr.Item("admin") == "Y")
							role = iq.i_role_Code("admin");
						else
							role = iq.i_role_Code("user");

						clsLanguage language;



						if (!iq.i_language_Code.ContainsKey(Trim(UCase(rdr.Item("Lang"))))) {
							Logit(rdr.Item("username") + " has an invalid language code of '" + rdr.Item("lang") + "'");

						} else {
							language = iq.i_language_Code(Trim(UCase(rdr.Item("Lang"))));

							//If UCase(language.Code) = "EL" Then Stop


							//If Not dicpriceBands.ContainsKey(seller) Then
							// dicpriceBands.Add(seller, New Dictionary(Of clsBuyerGroup, String)) ' for each seller create a lookup of buyergroup>priceBand
							//End If

							//                             what we're importing is already MD5'd (so we only need to shuffle it)
							//anAccount = New clsAccount(aUser, Shuffle(Trim$(rdr.Item("password"))), buyer, Role, team, language, dicCurrencies(cur$))

							//Dim buyerGroup As clsBuyerGroup
							//If IsDBNull(rdr.Item("priceBand")) Then
							// buyerGroup = dicBuyerGroups(Trim$(rdr.Item("chanid")) & "_self")
							//Els() 'e
							// buyerGroup = dicBuyerGroups(Trim$(rdr.Item("chanid")) & "_" & rdr.Item("priceBand"))
							//End If

							//we cannot know the passwords - becuase they are hashed 
							anaccount = new clsAccount(auser, Shuffle(Trim(rdr.Item("password"))), buyer, { role }, team, language, iq.i_currency_code(cur), seller, IIf(IsDBNull(rdr.Item("priceBand")), "", rdr.Item("priceBand")), buyer.Region.Culture,
							"HPE", awc, nextACid);

							dicAccounts.Add(Trim(rdr.Item("Username")), anaccount);

							if (!IsDBNull(rdr.Item("priceBand"))) {
								if (!DicHostAccounts.ContainsKey(Trim(rdr.Item("priceBand")))) {
									DicHostAccounts.Add(Trim(rdr.Item("priceBand")), anaccount);
								}
							}

							//If Not IsDBNull(rdr.Item("priceBand")) Then
							//    If Not dicpriceBands(seller).ContainsKey(buyer) Then

							//        dicpriceBands(seller).Add(buyer, Trim$(rdr.Item("priceBand")))
							//    Else
							//        'conflicting host account nums - ie two users working for the same buyer (with the same ChanID) - have different host account numbers
							//        'OK to take a arbitrary
							//        'If dicpriceBands(seller)(buyer) <> Trim$(rdr.Item("priceBand")) Then Stop
							//    End If
							//End If

							countUsers += 1;
						}
					}
					//not dud
				}
			}
		}
		rdr.Close();

		da.BulkWrite(con, uwc, "[user]");
		da.BulkWrite(con, awc, "account");

	}


	public int Languages(SqlClient.SqlConnection con, ref Dictionary<string, clsLanguage> diclanguages)
	{

		SqlClient.SqlDataReader rdr;
		clsLanguage alanguage;
		Languages = 0;

		rdr = da.DBExecuteReader(con, "SELECT LangCode,LangName from " + server + "[iq].dbo.languages");
		object lc;
		while (rdr.Read) {
			lc = Trim(LCase(rdr.Item("langcode")));
			if (!iq.i_language_Code.ContainsKey(UCase(lc))) {
				alanguage = new clsLanguage(UCase(Trim(lc)), rdr.Item("LangName"), false, true, true);
				diclanguages.Add(lc, alanguage);
				Languages += 1;
			}
		}

		rdr.Close();

	}

	//stand alone legal (& margins) import (one off)

	public void Legal()
	{
		SqlClient.SqlConnection con;
		con = da.OpenDatabase;

		object sql;
		SqlClient.SqlDataReader rdr;

		Dictionary<string, string> dicClones;
		dicClones = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
		//A temporary dictionary (by channel id, child:parent)

		sql = "SELECT parenthost as parent,subhost as child,margin,marginPSG from " + server + "[channelcentral].customers.host_parents";
		rdr = da.DBExecuteReader(con, sql);
		while (rdr.Read) {
			dicClones.Add(rdr.Item("child"), rdr.Item("parent"));
		}
		rdr.Close();

		List<string> errorMessages = new List<string>();

		sql = "SELECT hostid,supplyChains,portfolios,ChannelID,ChannelName,h.CountryCode,c.Currency,hp.pic,hp.pic2,hp.url,hp.dp,hp.listpriceonlyskus,hp.feedonlyskus,hpreceta,";
		sql += "terms,marginMin,MarginMax,marginType ";
		sql += "FROM " + server + "[channelcentral].customers.channel h ";
		sql += "JOIN " + server + "[iq].dbo.countries c on h.countrycode=c.countrycode ";
		sql += "LEFT JOIN " + server + "[channelcentral].customers.host_properties hp on hp.hostid= h.channelid ";
		sql += "where hostid is not null ";

		rdr = da.DBExecuteReader(con, sql);


		while (rdr.Read) {
			clsChannel channel;
			string hostid = rdr.Item("hostid");

			if (iq.i_channel_code.ContainsKey(hostid)) {
				channel = iq.i_channel_code(hostid);

				if (IsDBNull(rdr.Item("terms"))) {
					channel.Legal = "<b>Usage of iQuote means that you agree to the following Terms & Conditions:<b>";
					channel.Legal += "Every care is taken to ensure that the information contained within this site is accurate, however Errors and Omissions Excepted.";
				} else {
					channel.Legal = rdr.Item("Terms");
				}

				channel.marginMin = -20;
				channel.marginMax = 40;
				if (!IsDBNull(rdr.Item("marginMin"))) {
					channel.marginMin = rdr.Item("MarginMin");
				}

				if (!IsDBNull(rdr.Item("marginMax"))) {
					channel.marginMax = rdr.Item("MarginMax");
				}

				channel.Update(errorMessages);

			}

		}

		rdr.Close();
		con.Close();



	}

	private class clsTmpClone
	{


			//this is the clone (child) channel
		internal clsChannel parentChannel;

		internal float marginPSG;
		internal float marginISS;

		internal string priceband;
		public clsTmpClone(clsChannel ParentChannel, float marginpsg, float marginIss, string priceband)
		{
			this.parentChannel = ParentChannel;
			this.marginPSG = marginpsg;
			this.marginISS = marginIss;
			this.priceband = priceband;

		}

	}


	public void clones(List<string> errormessages)
	{
		//one time import of clones - as it hasn't worked :(

		SqlClient.SqlConnection con = da.OpenDatabase;

		object sql;
		SqlClient.SqlDataReader rdr;

		Dictionary<string, clsTmpClone> dicClones;
		dicClones = new Dictionary<string, clsTmpClone>(StringComparer.CurrentCultureIgnoreCase);
		//A temporary dictionary (by channel id, child:parent)

		sql = "SELECT parenthost as parent,subhost as child,margin,isnull(marginpsg,0) as marginpsg,externalPrice FROM " + server + "[channelCentral].customers.host_parents";
		rdr = da.DBExecuteReader(con, sql);
		while (rdr.Read) {
			dicClones.Add(rdr.Item("child"), new clsTmpClone(iq.i_channel_code(rdr.Item("parent")), rdr.Item("marginpsg"), rdr.Item("margin"), IIf((bool)rdr.Item("externalprice"), "EXT", "")));
		}
		rdr.Close();

		Dictionary<string, clsSector> dicBUs = new Dictionary<string, clsSector>();
		sql = "select upcnum,bucode from h3.ChannelCentral.products.Hierarchy where bucode is not null";
		rdr = da.DBExecuteReader(con, sql);

		List<string> dudbucs = new List<string>();
		int nullBUs = 0;
		while (rdr.Read) {
			if (!rdr.Item("upcnum").startswith("###")) {
				if (!object.ReferenceEquals(rdr.Item("Bucode"), DBNull.Value)) {
					object buc = rdr.Item("bucode");
					if (iq.i_sector_code.ContainsKey(buc)) {
						dicBUs.Add(rdr.Item("upcnum"), iq.i_sector_code(rdr.Item("bucode")));
					} else {
						if (!dudbucs.Contains(buc)) {
							errormessages.Add(buc + " is invalid");
						}
					}
				} else {
					nullBUs += 1;
				}
			}
		}
		rdr.Close();
		errormessages.Add(nullBUs + " null BUs");

		int done;

		//SKU>Product
		foreach ( sku in iq.i_SKU.Keys) {
			clsProduct product = iq.i_SKU(sku);
			if (!product.isSystem) {
				if (!product.SKU.StartsWith("###")) {
					if (dicBUs.ContainsKey(sku)) {
						if (!object.ReferenceEquals(product.Sector, dicBUs(sku))) {
							product.Sector = dicBUs(sku);
							product.update(errormessages);
							done += 1;
						}
					}
				}
			}
		}

		errormessages.Add("Updated BU on " + done + " options.");

		foreach ( channel in iq.Channels.Values) {
			channel.Margin.Clear();
		}
		da.DBExecutesql(con, "DELETE FROM MARGIN");

		int @fixed = 0;
		int made;

		clsChannel parent;
		clsChannel child;

		//for every child (clone) set its parent
		foreach ( childChannelid in iq.i_channel_code.Keys) {
			//this channel is a clone (a child)
			if (dicClones.ContainsKey(childChannelid)) {
				clsTmpClone clone = dicClones(childChannelid);
				parent = clone.parentChannel;
				child = iq.i_channel_code(childChannelid);
				child.IsCloneOf = parent;
				child.Update(errormessages);
				//writes the new FK_Channel_Id_IsCloneOf  to the database
				string pb = "";

				//If rdr.Item("ExternalPrice") <> 0 Then pb$ = "EXT" Else pb$ = ""

				float mfISS;
				float mfPSG;

				if (child.marginType == "R") {
					mfISS = 100 / (100 - clone.marginISS);
					mfPSG = 100 / (100 - clone.marginPSG);
				} else {
					mfISS = (100 + clone.marginISS) / 100;
					mfPSG = (100 + clone.marginPSG) / 100;
				}
				clsMargin margISS = new clsMargin(parent, child, mfISS, pb, iq.i_sector_code("HPISS"), "");
				clsMargin margPSG = new clsMargin(parent, child, mfPSG, pb, iq.i_sector_code("HPPSG"), "");
				made += 2;

			} else {
				//the parent isn't loaded yet
				Beep();
			}
		}

		con.Close();


	}


	//As Dictionary(Of String, clsChannel)
	public void channels(SqlClient.SqlConnection Con, ref Dictionary<string, clsChannel> dicChannels, Dictionary<string, clsRegion> dicRegions, ref List<string> errormessages)
	{

		int nextcID;
		DataTable channelWriteCache = da.MakeWriteCacheFor(Con, "Channel", nextcID);

		object sql;
		SqlClient.SqlDataReader rdr;

		Dictionary<string, string> dicClones;
		dicClones = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
		//A temporary dictionary (by channel id, child:parent)

		sql = "SELECT parenthost as parent,subhost as child,margin,marginpsg from " + server + "[channelcentral].customers.host_parents";
		rdr = da.DBExecuteReader(Con, sql);
		while (rdr.Read) {
			dicClones.Add(rdr.Item("child"), rdr.Item("parent"));
		}
		rdr.Close();

		sql = "SELECT supplyChains,portfolios,ChannelID,ChannelName,h.CountryCode,c.Currency,hp.pic,hp.pic2,hp.url,hp.dp,hp.listpriceonlyskus,hp.feedonlyskus,hpreceta,terms,marginMin,MarginMax,marginType,hp.universal ";
		sql += "FROM " + server + "[channelcentral].customers.channel h ";
		sql += "JOIN " + server + "[iq].dbo.countries c on h.countrycode=c.countrycode ";
		sql += "LEFT JOIN " + server + "[channelcentral].customers.host_properties hp on hp.hostid= h.channelid ";

		rdr = da.DBExecuteReader(Con, sql);

		clsChannel achannel;
		clsChannel isCloneOf = null;

		string cnc;
		string crn;
		string chn;
		string channelID;
		int priceConfig = 0;


		while (rdr.Read) {
			channelID = Trim(rdr.Item("channelid"));

			if (!dicChannels.ContainsKey(channelID)) {
				crn = "";
				cnc = "";
				chn = "";
				cnc = UCase(Trim(rdr.Item("countrycode")));
				if (IsDBNull(rdr.Item("currency"))) {
					Beep();
				} else {
					crn = Trim(UCase(rdr.Item("currency")));
				}

				priceConfig = 0;
				if (!IsDBNull(rdr.Item("feedonlyskus"))) {
					if (rdr.Item("feedonlyskus") == 0)
						priceConfig = 1;
					//Show POA=NOT feedOnlySkus
				}
				if (!IsDBNull(rdr.Item("ListPriceOnlySkus"))) {
					if (rdr.Item("ListpriceOnlyskus"))
						priceConfig = priceConfig | 2;
					//Locically OR on the '2 bit'
				}

				if (!IsDBNull(rdr.Item("DP"))) {
					if (rdr.Item("DP") != 0)
						priceConfig = priceConfig | 4;
					//Show Base Price = DataProvider 
				}


				priceConfig = priceConfig | 8;
				// we pretty much always want to display a specific price if we have it (with the posible exception of ebuyer)

				chn = Trim(rdr.Item("channelname"));
				string focus = rdr.Item("SupplyChains") + "," + rdr.Item("portfolios");
				if (rdr.Item("hpreceta"))
					focus += ",receta";
				bool universal = false;
				if (rdr.Item("univeral") != null) {
					universal = (bool)rdr.Item("univeral");
				}

				if (cnc == "UK")
					cnc = "GB";
				achannel = new clsChannel(null, chn, "", "", channelID, dicRegions(cnc), new nullableString(rdr.Item("pic")), new nullableString(rdr.Item("pic2")), new nullableString(rdr.Item("URL")), priceConfig,
				"tree.1", focus, IsDBNull(rdr.Item("marginmin")) ? 0 : rdr.Item("marginmin"), IsDBNull(rdr.Item("marginmax")) ? 20 : rdr.Item("marginmax"), IsDBNull(rdr.Item("margintype")) ? "" : Left(rdr.Item("margintype"), 1), "", IsDBNull(rdr.Item("terms")) ? "" : rdr.Item("terms"), null, universal, "",
				"", "", channelWriteCache, nextcID);

				//this is NOT the iq.channels dictionary (which is autmoatically added to)
				dicChannels.Add(channelID, achannel);

			}

		}

		rdr.Close();

		//DBExecutesql(Con, "set identity_insert Channel ON")
		da.BulkWrite(Con, channelWriteCache, "Channel", , true);
		// DBExecutesql(Con, "set identity_insert Channel OFF")

		channelWriteCache = null;

		//This bit isn't very clear - for each channel - it checks to see if it's a clone of another (in dicclones)

		clsChannel parent;
		clsChannel child;
		foreach ( channelID in dicChannels.Keys) {
			//this channel is a clone (a child)
			if (dicClones.ContainsKey(channelID)) {
				//does the dictionary contain the parent (it should do now)
				if (dicChannels.ContainsKey(dicClones(channelID))) {
					parent = dicChannels(dicClones(channelID));
					child = dicChannels(dicClones(channelID));
					child.IsCloneOf = parent;
					child.Update(errormessages);
					//writes the new FK_Channel_Id_IsCloneOf  to the database
				} else {
					//the parent isn't loaded yet
					Beep();
				}
			} else {
				isCloneOf = null;
				//this will be turned to a max(ID)+1 - to clone itself
				// End If
			}
		}

		Con.Close();
		Con = da.OpenDatabase();

		DataTable dt = da.MakeWriteCacheFor(Con, "Domain");

		string query = string.Empty;
		query = "SELECT HostID, Host_Domain FROM " + server + "ChannelCentral.customers.Host_Domains  hd inner join  ";
		query = query + server + "ChannelCentral.customers.Host_Properties hp on hd.HID= hp.HID order by hp.hid";
		rdr = da.DBExecuteReader(Con, query);
		string hostID;
		clsChannel channel;

		while (rdr.Read) {
			hostID = Trim(rdr.Item("HostID"));
			if (!iq.i_channel_code.ContainsKey(hostID)) {
				Logit("couldnt locate channel " + hostID);

			} else {
				channel = iq.i_channel_code(hostID);
				System.Data.DataRow row;
				row = dt.NewRow();
				row("Domain") = rdr.Item("Host_Domain");
				row("FK_Channel_ID") = channel.ID;

				dt.Rows.Add(row);
			}
		}
		rdr.Close();

		da.BulkWrite(Con, dt, "Domain");


	}


	public void Currencies(SqlClient.SqlConnection con, Dictionary<string, clsCurrency> dicCurrencies)
	{
		SqlClient.SqlDataReader rdr;
		object sql;

		int added;
		int loaded;

		//CURRENCIES
		sql = "SELECT [CurrencyCode],[CurrencyName],[CurrencySymbol],[Notes] from " + server + "[iq].[dbo].[Currencies]";
		rdr = da.DBExecuteReader(con, sql);

		clsCurrency aCurrency;
		string cs;
		clsTranslation notes;


		while (rdr.Read) {
			if (dicCurrencies.ContainsKey(rdr.Item("currencycode"))) {
				loaded += 1;
			} else {
				added += 1;

				cs = rdr.Item("currencySymbol");

				if (InStr(cs, "&")) {
					cs = HttpUtility.HtmlDecode(cs);
				}

				notes = null;
				if (!IsDBNull(rdr.Item("notes"))) {
					if (Trim(rdr.Item("notes")) != "") {
						notes = iq.AddTranslation(rdr.Item("Notes"), English, "currnote", 0, null, 0, false);
					}
				}

				//If rdr.Item("currencycode") = "GBP" Then culture = "EN-gb"
				aCurrency = new clsCurrency(Trim(rdr.Item("currencycode")), null, iq.AddTranslation(Trim(rdr.Item("currencyname")), English, "curr", 0, null, 0, false), cs, 1, notes);
				dicCurrencies.Add(rdr.Item("currencycode"), aCurrency);

			}
		}
		rdr.Close();


	}

	//Public Sub BuildTree(con As SqlClient.SqlConnection, _
	//                         dicSystems As Dictionary(Of String, clsBranch), _
	//                          optionsbysku As Dictionary(Of String, clsProduct), _
	//                          dicFamily As Dictionary(Of String, clsBranch), _
	//                          ByRef optionsByCK As Dictionary(Of String, clsProduct), _
	//                          dicOptType As Dictionary(Of String, clsProductType), _
	//                          dicOptLocalisation As Dictionary(Of clsProduct, List(Of clsRegion)), _
	//                         ByRef errormessages As List(Of String))

	//    'OptionsByCK is keyed sysfamily^l1^l2^l3^optsn>Product
	//    'DicSystems - mfrSkU>Branch   (these breanches are already attached to their families)
	//    'we  need to graft on the correct optCats (L2's)

	//    Dim rdr As SqlClient.SqlDataReader
	//    Dim chassisTL As clsTranslation = iq.AddTranslation("Chassis", English, "")

	//    Dim chassisRoot As clsBranch = New clsBranch(Nothing, Nothing, chassisTL, "", chassisTL, chassisTL, Nothing, 100, False, "B")

	//    Dim alltl As clsTranslation = iq.AddTranslation("All Options", English, "UI")
	//    Dim tlOption As clsTranslation = iq.AddTranslation("Option", English, "UI")
	//    Dim tlOptions As clsTranslation = iq.AddTranslation("Options", English, "UI")


	//    '
	//    Dim dicSlotTypes As Dictionary(Of String, clsSlotType)

	//    'Return a dictionary of minorSlot|Type codes to slot types 
	//    dicSlotTypes = Import.slotTypes(con, dicSystems) 'dicFamily) '20 secs

	//    'Build a dictionary to look up the slot type per subfamily/option type
	//    '                                                                subfamily            option type
	//    'Dim dicSubFamOptTypeSlotType As Dictionary(Of String, Dictionary(Of String, clsSlotType))
	//    ' dicSubFamOptTypeSlotType = Import.SubFamiliyOptionTypes(con, dicSlotTypes)

	//    Dim ofDic As Dictionary(Of String, Dictionary(Of String, String)) = Import.FamiliyOptTypeToOptFamily()

	//    'OPTION LIMITS 
	//    'build a dictionary by sysfamily/option family of the Limits - used later to attach instances of clsQuantity (autoAdds/Preinstalled) to the option branches

	//    '                                                  sysSubFam      optionfaimily       limit
	//    '                                                                       NHP35lff
	//    Dim dicOptLimits As Dictionary(Of String, IQ.clsLimit)
	//    dicOptLimits = Import.OptLimits(con, ofDic) 'returns sysfamilycode^optfamily>clslimit EG.. DL380PLFF^NHPLFF>blah

	//    'FACTORY INSTALLED OPTIONS/components - call them what you will
	//    'get a list (by system mfrSKU) of the part numbers and quantities of all factory installed components (PriStor, sec stor CPU, MEM etc,Raid)


	//    Dim dicFIOs As Dictionary(Of String, Dictionary(Of String, Integer))
	//    'returns a list (by system mfrSKU) of the quantities and part numbers of all factory installed components (PriStor, sec stor CPU, MEM etc)
	//    dicFIOs = Import.FIOs(con)


	//    WriteDicFIOs(dicFIOs, "c:\temp\fios.txt")

	//    'anevent = New clsEvent(buildTreeEvent, "", ev_Info)
	//    'we need just a translation key for each opt type (although many branches will reference each opt type)
	//    Dim dicOptFam As New Dictionary(Of String, clsTranslation)

	//    'Make a dictionary of option parent (Performance, storage etc) > branches

	//    'Populate the dictionary of all option families (MEM_PC3-1060/5.25LFF/Socket/INTEL-SocketT-52)
	//    rdr = da.DBExecuteReader(con, "SELECT distinct optfamily from " & server$ & "[iq].products.options")
	//    While rdr.Read
	//        dicOptFam.Add(Trim$(rdr.Item("optfamily")), iq.AddTranslation(Abbreviation(rdr.Item("optfamily")), English))
	//    End While
	//    rdr.Close()
	//    'anevent.update("Imported " & dicOptFam.Count & " options family codes")

	//    'Populate the dictionary of all option technologies (UDIMM/RDIMM/DDR3)/(SAS/SATA) (4 Core/8 Core)
	//    ' anevent = New clsEvent(buildTreeEvent, "", ev_Info)
	//    Dim dicOptTech As New Dictionary(Of String, clsTranslation)(StringComparer.CurrentCultureIgnoreCase)  'makes the dictionary keys case insensitive
	//    rdr = da.DBExecuteReader(con, "SELECT distinct technology from " & server$ & "[iq].products.options")
	//    While rdr.Read
	//        If IsDBNull(rdr.Item("technology")) Then
	//            'many technologies are NULL
	//            dicOptTech.Add("unspecified technology", iq.AddTranslation("Unspecified technology", English, "", 0, Nothing, 0, False))  'New clsProduct("Unspecified technology", False, np, Nothing))
	//        Else
	//            dicOptTech.Add(Trim$(rdr.Item("technology")), iq.AddTranslation(Abbreviation(rdr.Item("Technology")), English)) 'New clsProduct(rdr.Item("technology").ToString, False, np, Nothing))
	//        End If
	//    End While
	//    rdr.Close()

	//    'anevent.update("Imported " & dicOptTech.Count & " options technology codes")

	//    Dim L1branch As clsBranch
	//    Dim L2branch As clsBranch
	//    'Dim optFamBranch As clsBranch
	//    ' Dim optTechBranch As clsBranch
	//    ' Dim optBranch As clsBranch

	//    'Dim OptProduct As clsProduct
	//    Dim grafts As Integer = 0
	//    Dim kept As Integer = 0
	//    Dim pruned As Integer = 0

	//    Dim Incompatibles As Integer = 0

	//    Dim xx As Integer = 0

	//    Dim invalidFamilies As New List(Of String)
	//    Dim invalidOptTypes As New List(Of String)

	//    Dim optTrans As clsTranslation
	//    optTrans = iq.AddTranslation("options", English)
	//    Dim optTransSingular As clsTranslation
	//    optTransSingular = iq.AddTranslation("option", English)

	//    'anevent = New clsEvent(buildTreeEvent, "", ev_Info)

	//    Dim nextBranchID As Integer = 0 'Will force the use of Next IDs (allowing un to know the branch ID before it's be BulkWritten
	//    Dim QuantityWriteCache As DataTable = da.MakeWriteCacheFor(con, "Quantity")
	//    Dim SlotWriteCache As DataTable = da.MakeWriteCacheFor(con, "Slot")
	//    Dim ProductAttributeWriteCache As DataTable = da.MakeWriteCacheFor(con, "ProductAttribute")

	//    Dim branchWriteCache As DataTable = da.MakeWriteCacheFor(con, "Branch", nextBranchID, True) 'will populate nextbrnachID with MAX(ID)+1

	//    Dim GraftWriteCache As DataTable = da.MakeWriteCacheFor(con, "Graft")
	//    Dim pruneWriteCache As DataTable = da.MakeWriteCacheFor(con, "Prune")

	//    Dim fams As Integer = 0
	//    '        Dim cats As Dictionary(Of String, Dictionary(Of String, clsBranch)) 'Holds sets of option categories (per sysfamily)


	//    Dim chassisProductType As clsProductType = (From j In iq.ProductTypes.Values Where j.Code = "CHAS").First
	//    Dim chassis As Dictionary(Of String, clsBranch)  'store up all the chassis products - so we can make the 'gives' slots en-mass
	//    chassis = New Dictionary(Of String, clsBranch)

	//    'SubFamily>Chassis Branch
	//    Dim chassisBranches As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)

	//    'The options dictionary has a compund key (sysfam^l1^l2^L3^optSN>product)
	//    '(the multidimensional dictionary is a sm

	//    'go through

	//    For Each ck In optionsByCK.Keys

	//        fams += 1

	//        Dim p() = Split(ck, "^")  '(sysfam^l1^l2^L3^optSN>product)

	//        Dim sysfamkey As String = p(0)
	//        If Not dicFamily.ContainsKey(sysfamkey) Then  'check the family is valid
	//            If Not invalidFamilies.Contains(sysfamkey) Then
	//                Logit("invalid family:[" & sysfamkey & "] not in (distinct sysFamilyName from sysfamilydefinitions)")
	//                invalidFamilies.Add(sysfamkey)
	//            End If
	//        Else

	//            Dim l1key As String = p(1)
	//            Dim l2key As String = p(2)
	//            Dim l3key As String = p(3)
	//            Dim optsn As String = p(4)

	//            Dim l2keys As Dictionary(Of clsBranch, String)  'needed in GraftOn, for a 'reverse' lookup of the OptTypeKey from the optTypeBranch
	//            l2keys = New Dictionary(Of clsBranch, String)

	//            'Dim optFamkeys As Dictionary(Of clsBranch, String)
	//            'optFamkeys = New Dictionary(Of clsBranch, String)

	//            Dim chassisBranch As clsBranch
	//            Dim chassisVariant As clsVariant
	//            Dim chassisProduct As clsProduct
	//            '  Dim MoboBranch As clsBranch
	//            '   Dim moboProduct As clsBranch


	//            'make the 'all options' branch (we'll graft this onto every system in the family later)
	//            'If Not iq.i_screens_code.ContainsKey("base") Then
	//            'Dim ascreen As clsScreen = New clsScreen("Branch", "Options", "Options", errormessages)
	//            'End If

	//            Dim AllBranch As clsBranch = New clsBranch(Nothing, Nothing, alltl, "", tlOptions, tlOption, iq.i_screens_code("Base"), 30, False, "B", branchWriteCache, nextBranchID)

	//            Dim familybranch As clsBranch = dicFamily(sysfamkey)  'We already made family branches earlies

	//            Dim firstsystem As clsProduct = dicFamily(sysfamkey).childBranches.Values(0).Product

	//            'Create a chassis for every subFamily, and graft it onto every system in that subfamily
	//            ' For Each SupplyChain In dicFamily(sysfamkey).childBranches.Values 'systems reside under supply chains
	//            For Each Systembranch In familybranch.childBranches.Values
	//                Dim sysSubFamily As String  'comes from the iq.systems.familycode
	//                sysSubFamily = Systembranch.Product.i_Attributes_Code("subFamily")(0).Translation.text(s_lang)  'IMPORTANT for compatibility

	//                If chassisBranches.ContainsKey(sysSubFamily) Then 'only make 1 chassis per subfamily
	//                    chassisBranch = chassisBranches(sysSubFamily)
	//                Else
	//                    chassisProduct = New clsProduct(False, firstsystem.Sector, chassisProductType, DateAdd(DateInterval.Day, -500, Now), DateAdd(DateInterval.Day, 10000, Now), True, False, True)
	//                    chassisVariant = New clsVariant("", chassisProduct, HP, chassisProduct.ID.ToString, "", "", "", r_worldwide, 0) 'Every product needs a variant - so it can be stored in a QuoteItem
	//                    chassisBranch = New clsBranch(chassisProduct, chassisRoot, iq.AddTranslation(sysSubFamily & " chassis", English), "", chassisTL, chassisTL, Nothing, 100, True, "B", branchWriteCache, nextBranchID)
	//                    chassisBranches.Add(sysSubFamily, chassisBranch)

	//                    'Systems Name (is SKU)
	//                    'Dim n As clsProductAttribute = New clsProductAttribute(chassisProduct, iq.i_attribute_code("~ame"), 0, iq.i_unit_code("txt"), dicFamily(sysfamkey).Translation, ProductAttributeWriteCache)

	//                    Dim cq As clsQuantity = New clsQuantity(r_worldwide, "", chassisBranch, 1, 0, 0, True, QuantityWriteCache) 'make a global auto add - to add the chassis to the system

	//                    'all the gives slots are in the chassis (for now)
	//                    ' MakeGivesSlots(chassisBranch, dicOptLimits(sysSubFamily), dicSlotTypes, dicSubFamOptTypeSlotType, QuantityWriteCache, SlotWriteCache)
	//                End If
	//                'graft the chassis on to every system - It already has a (global) quanity to auto-add it
	//                Systembranch.Graft(chassisBranch, "BuildTree", "", errormessages, GraftWriteCache)
	//                Systembranch.Graft(AllBranch, "Buildtree", "", errormessages, GraftWriteCache) 'pre-graft the 'all options' branch onto every system in the family

	//            Next 'system (In the family)


	//            ' These branches need to hang in space (before being grafted into multiple locations) (under each system in a family)
	//            'make the l1-3 branches, plus options ..

	//            L1branch = New clsBranch(Nothing, AllBranch, iq.AddTranslation(l1key, English), "", optTrans, optTransSingular, iq.Screens(719), 100, False, "T", branchWriteCache, nextBranchID)

	//            'dicsysfam = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, clsProduct))))))
	//            '                                            sysfam(dl380g6Lffa)      optcat(perf)            opttype(mem)           optFamily (SODIM)    OptTech(ddr3)           IQ1SN                       


	//            'All Options - TRO and Upsell Opportunities
	//            L2branch = New clsBranch(Nothing, L1branch, dicOptType(LCase(l2key)).Translation, "", optTrans, optTransSingular, Nothing, 100, False, "T", branchWriteCache, nextBranchID)
	//            l2keys.Add(L2branch, l2key)
	//            If l2key IsNot Nothing Then
	//                'adds the the opt family,opttechnology and option branches - to this optTypeBranch
	//                If l2key <> "CPU" Then ' Stop 'we don't need to add CPU option.. we make a bespoke, singleton branch on the fly.                  
	//                    '                    AddOptions(L2branch, l2key, sysfamkey, l1key, optionsByCK, dicOptType, optTransSingular, optTrans, branchWriteCache, dicOptFam, dicOptTech, nextBranchID)
	//                Else
	//                    'just make an empty branch which will hold the lone CPU
	//                    '            cpuHolder = optTypeBranch 'this CPU opttype branch exists once for each family (every system in the family uses it)
	//                End If
	//            End If
	//            '  Next l2key
	//            '   Dim cpusku As String
	//            '   Dim cpuBranch As clsBranch = Nothing

	//            'some things (takes slots) - will have global scope (apply wherever this BRANCH appears) and only need be made once (per family) 
	//            'Others (preinstalled quanitites, localisations) - vary by system - and must be made for each option in the category branch

	//            Dim systemSKU As String
	//            Dim firstinfamily As Boolean = True

	//            '            'we now have a completed the All Branch (containing all the optCatbranches) we can graft this onto every system in the family  (Pruning off incomatibles)
	//            'For Each SupplyChain In dicFamily(l1key).childBranches.Values 'systems reside under supply chains
	//            '    For Each Systembranch In SupplyChain.childBranches.Values
	//            '        'IMPORTANT
	//            '        If Systembranch.Product.isSystem Then   'for each system
	//            '            systemSKU = Systembranch.Product.sku

	//            '            'If systemSKU = "662257-421" Then Stop ' should have a 662266-b21 cpu

	//            '            ' Systembranch.Graft(optCatBranch, "import", GraftWriteCache) 'Graft the WHOLE option category Branch on to each system (in the supply chain, in the family)
	//            '            ' grafts += 1

	//            '            Systembranch.Graft(AllBranch, "import", "", errormessages, GraftWriteCache) 'Graft the WHOLE option category Branch on to each system (in the supply chain, in the family)
	//            '            grafts += 1

	//            '            'make autoAdds (quantities), takes slots - and prune incompatible option branches
	//            '            Dim syspath$
	//            '            syspath$ = "tree." & Trim$(iq.RootBranch.ID)
	//            '            syspath$ &= "." & Trim$(SupplyChain.Parent.Parent.ID) 'System type
	//            '            syspath$ &= "." & Trim$(SupplyChain.Parent.ID) 'Family
	//            '            syspath$ &= "." & Trim$(SupplyChain.ID) 'supply chain  '<<<new
	//            '            syspath$ &= "." & Trim$(Systembranch.ID) 'system

	//            '            'If systemSKU = "668812-421" Then Stop
	//            '            '                                If systemSKU = "646902-421" Then Stop 'DL380P
	//            '            'Option:'647893-B21' 647893-B21 QTY:4
	//            '            'Option:'656362-B21' 656362-B21 QTY:1


	//            '            'makes autoAdds (quantities) for FIOs, Takes slots - and prunes incompatible option branches - on a set of option category branches (eg. Performance,Managment....)
	//            '            Limits(Systembranch, syspath$, AllBranch, options, l2keys, GraftWriteCache, SlotWriteCache, pruneWriteCache, _
	//            '                                 QuantityWriteCache, firstinfamily, SupplyChain, dicOptLimits, dicSlotTypes, dicOptLocalisation, dicFIOs, systemSKU, kept, pruned, ofDic)

	//            '            firstinfamily = False

	//            '            Dim sysSubFamily As String  'comes from the iq.systems.familycode
	//            '            sysSubFamily = Systembranch.Product.i_Attributes_Code("subFamily")(0).Translation.text(s_lang)  'IMPORTANT for compatibility

	//            '        End If 'isSystem
	//            '        ' End If
	//            '    Next Systembranch
	//            'Next SupplyChain

	//            da.BulkWrite(con, SlotWriteCache, "Slot")
	//            SlotWriteCache = da.MakeWriteCacheFor(con, "Slot")


	//            da.BulkWrite(con, pruneWriteCache, "Prune")


	//            pruneWriteCache = da.MakeWriteCacheFor(con, "Prune")

	//            da.BulkWrite(con, GraftWriteCache, "Graft")

	//            GraftWriteCache = da.MakeWriteCacheFor(con, "Graft")

	//            da.BulkWrite(con, QuantityWriteCache, "Quantity")
	//            QuantityWriteCache = da.MakeWriteCacheFor(con, "Quantity")

	//            da.DBExecutesql(con, "set identity_insert Branch ON")
	//            da.BulkWrite(con, branchWriteCache, "Branch", , True)
	//            da.DBExecutesql(con, "SET IDENTITY_INSERT branch OFF")

	//            Dim nbid As Integer = nextBranchID
	//            nbid = nextBranchID
	//            nextBranchID = 0
	//            branchWriteCache = da.MakeWriteCacheFor(con, "Branch", nextBranchID, True)
	//            'If nextBranchID <> nbid Then Stop 'elaborate  error tracking - remove

	//            da.BulkWrite(con, ProductAttributeWriteCache, "ProductAttribute")

	//        End If
	//    Next ck



	//    'BulkWrite(con, SlotWriteCache, "Slot")
	//    'SlotWriteCache = Nothing

	//    'anevent = New clsEvent(buildTreeEvent, "", ev_Info)
	//    'BulkWrite(con, pruneWriteCache, "Prune")

	//    'anevent.update("Bulk Wrote " & pruneWriteCache.Rows.Count & " prunes")
	//    'pruneWriteCache = Nothing

	//    'anevent = New clsEvent(buildTreeEvent, "", ev_Info)

	//    'BulkWrite(con, GraftWriteCache, "Graft")

	//    'anevent.update("Bulk Wrote " & GraftWriteCache.Rows.Count & " grafts ")
	//    'GraftWriteCache = Nothing

	//    'BulkWrite(con, QuantityWriteCache, "Quantity")
	//    'QuantityWriteCache = Nothing

	//    ''DBExecutesql(con, "set identity_insert Branch ON")
	//    'BulkWrite(con, branchWriteCache, "Branch", , True)
	//    ''DBExecutesql(con, "SET IDENTITY_INSERT branch OFF")
	//    'branchWriteCache = Nothing

	//    optionsByCK = Nothing 'free the (very large amount of) memory
	//    ' dicSKUOptionProduct = Nothing

	//    'makes the gives slots (on the chassis branches)
	//    Import.OptionLimits(chassisBranches, dicSlotTypes)

	//    ' Logit("Recorded " & cpuBranches.Count & " CPU branches")

	//    Logit("built tree", False, True)


	//End Sub

	//'Private Function RecordCPUsku(systembranch As clsBranch, cpubranches As Dictionary(Of String, clsBranch), cpuroot As clsBranch, _
	//'                              ByRef branchWriteCache As DataTable, ByRef nextbranchid As Integer, systemsku As String) As String

	//'    Dim cpusku As String = ""
	//'    Dim cpubranch As clsBranch

	//'    If systembranch.Product.i_Attributes_Code.ContainsKey("cpuSKU") Then
	//'        cpusku = systembranch.Product.i_Attributes_Code("cpuSKU")(0).Translation.text(English)

	//'        'buld a master tree of CPU's as we go
	//'        If cpubranches.ContainsKey(cpusku) Then
	//'            cpubranch = cpubranches(cpusku)
	//'        Else
	//'            If iq.i_SKU.ContainsKey(cpusku) Then
	//'                Dim cpuProd As clsProduct = iq.i_SKU(cpusku)
	//'                'cpubranch = New clsBranch(iq.i_SKU(cpusku),  cpuroot, cpuProd.i_Attributes_Code("~ame")(0).Translation, "", cpuroot.CollectiveNoun, cpuroot.collectiveNounSingular, Nothing, 100, branchwritecache, nextbranchid)
	//'                cpubranch = New clsBranch(iq.i_SKU(cpusku), cpuroot, cpuProd.i_Attributes_Code("MfrSKU")(0).Translation, "", cpuroot.CollectiveNoun, cpuroot.collectiveNounSingular, Nothing, 100, False, branchWriteCache, nextbranchid)
	//'                cpubranches.Add(cpusku, cpubranch)
	//'            Else
	//'                If Not cpusku.StartsWith("###") Then
	//'                    Logit("No such CPU " & cpusku & " for " & systembranch.DisplayName(English))
	//'                End If

	//'            End If
	//'        End If

	//'        'If dicFIOs(systemSKU).ContainsKey(cpusku) Then
	//'        '    cpuqty = dicFIOs(systemSKU)(cpusku)
	//'        'Else
	//'        '    Stop
	//'        'End If
	//'    Else
	//'        Logit(systemsku & " has no CPU ")
	//'    End If

	//'    Return cpusku

	//'End Function

	public void InterfaceSlots()
	{
		//Ok, lets get the data on any drives which take SAS

		//  Dim MonsterIndex As Dictionary(Of clsProduct, List(Of String)) 'A list of all the paths at which a product appears
		//  MonsterIndex = New Dictionary(Of clsProduct, List(Of String))
		//  iq.RootBranch.IndexProductPaths("tree." & Trim(iq.RootBranch.ID), MonsterIndex, False, True, Nothing)  ' 180 SECS !

		object requiresSASAttribute = iq.i_attribute_code.ContainsKey("requireSAS") ? iq.i_attribute_code("requireSAS") : new clsAttribute("requireSAS", iq.AddTranslation("Requires SAS", English, "", 0, null, 0, false), 0);
		object supportsSASAttribute = iq.i_attribute_code.ContainsKey("supportSAS") ? iq.i_attribute_code("supportSAS") : new clsAttribute("supportSAS", iq.AddTranslation("Supports SAS", English, "", 0, null, 0, false), 0);
		clsProductAttribute aProp;

		SqlClient.SqlConnection iq1con = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;");

		//Dim SkuIndex As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))()
		//iq.Branches(1).SkuPaths(SkuIndex, "tree.1", True)

		SqlClient.SqlConnection iq2con = da.OpenDatabase();

		//Dim wc As DataTable = da.MakeWriteCacheFor(iq2con, "Slot")
		DataTable wc = da.MakeWriteCacheFor(iq2con, "ProductAttribute");

		SqlClient.SqlDataReader rdr;
		object sql = "select 0 as gives, 1 as takes,optsku from products.options where Technology='SAS' and optsku not like '###%' and opttype  in ('TAP','HDD') and (activetodate > getdate() or activetodate is null) union all select intport as gives,0 as takes,optsku from products.options left outer join  products.[HierarchyFull] h on optsku=h.upcnum  left outer join products.OptRAIDprops ro on h.ccDescription LIKE '%'+ro.RAIDfamily+'%'  where Technology='SAS' and optsku not like '###%' and opttype  not in ('TAP','HDD','CHK','IOC')";

		rdr = da.DBExecuteReader(iq1con, sql);

		Dictionary<string, List<clsBranch>> locs = new Dictionary<string, List<clsBranch>>();

		List<clsProduct> givers = new List<clsProduct>();
		//all the options that give slots
		while (rdr.Read) {
			if (iq.i_SKU.ContainsKey(rdr.Item("optsku"))) {
				givers.Add(iq.i_SKU(rdr.Item("optsku")));
			}
		}
		rdr.Close();

		clsBranch branch;
		Dictionary<Int32, string> index = new Dictionary<Int32, string>();
		foreach ( branch in iq.Branches.Values) {
			if (givers.Contains(branch.Product)) {
				if (!locs.ContainsKey(branch.Product.SKU))
					locs.Add(branch.Product.SKU, new List<clsBranch>());
				locs(branch.Product.SKU).Add(branch);
				index.Add(branch.ID, null);
			}
		}

		iq.Branches(1).indexProductBranchesByPath("tree", true, index);

		clsSlot aslot;
		clsSlotType st;

		if (!iq.i_slotType_Code.ContainsKey("SAS") || iq.i_slotType_Code("SAS").ContainsKey("SAS"))
			object s = new clsSlotType("SAS", "SAS", iq.AddTranslation("SAS", English, "", 0, null, 0, false));
		st = iq.i_slotType_Code("SAS")("SAS");
		int added = 0;

		int notfound = 0;

		clsProduct product;
		rdr = da.DBExecuteReader(iq1con, sql);
		while (rdr.Read) {
			if (iq.i_SKU.ContainsKey(rdr.Item("optsku"))) {
				if (rdr.Item("optsku") == "726821-B21") {
					object a = 1;
				}
				product = iq.i_SKU(rdr.Item("optsku"));
				if (locs.ContainsKey(product.SKU)) {
					List<clsBranch> done = new List<clsBranch>();
					foreach ( path in locs(product.SKU)) {
						//branch = iq.Branches(Split(path, ".").Last)

						if (!done.Contains(path)) {
							if (!object.ReferenceEquals(rdr.Item("gives"), DBNull.Value) && (int)rdr.Item("gives") > 0)
								aProp = new clsProductAttribute(product, supportsSASAttribute, 1, iq.i_unit_code("txt"), null, wc);
							if (!object.ReferenceEquals(rdr.Item("gives"), DBNull.Value) && (int)rdr.Item("gives") > 0)
								aProp = new clsProductAttribute(product, requiresSASAttribute, 1, iq.i_unit_code("txt"), null, wc);

							//If rdr.Item("gives") IsNot DBNull.Value AndAlso CInt(rdr.Item("gives")) > 0 Then aslot = New clsSlot(st, path, "", CInt(rdr.Item("gives")), Nothing, New NullableInt(), 0, 0, wc)
							//If rdr.Item("takes") IsNot DBNull.Value AndAlso CInt(rdr.Item("takes")) > 0 Then aslot = New clsSlot(st, path, "", -CInt(rdr.Item("takes")), Nothing, New NullableInt(), 0, 0, wc)

							done.Add(path);
						}
					}
				} else {
					notfound += 1;
				}
			} else {
				//        Stop
			}

		}

		rdr.Close();

		//da.BulkWrite(iq2con, wc, "Slot")
		da.BulkWrite(iq2con, wc, "ProductAttribute");

		if (notfound > 0)
			System.Diagnostics.Debugger.Break();

	}


	//TODO add to incremental import
	// ''' <summary>
	// ''' Imports OPTIONS that add slots (such as drive cages)
	// ''' </summary>
	public string slotAdds(SqlClient.SqlConnection con)
	{

		int i;

		//        Dim MonsterIndex As Dictionary(Of clsProduct, List(Of String)) 'A list of all the paths at which a product appears
		//        MonsterIndex = New Dictionary(Of clsProduct, List(Of String))
		//        iq.RootBranch.SkuPaths("tree." & Trim(iq.RootBranch.ID), MonsterIndex, False, True, Nothing)  ' 180 SECS !

		DataTable wc = da.MakeWriteCacheFor(con, "Slot");
		Dictionary<string, Dictionary<string, string>> stDic = Import.FamilyOptTypeToOptFamily;


		object sql;
		SqlClient.SqlDataReader rdr;

		sql = "SELECT optsku,optType,slotAddType,SlotAddQty,optFamily,sysFamily ";
		sql += "FROM " + server + "[iq].Products.options ";
		sql += "WHERE slotaddqty is not null AND slotaddtype is not null ";
		sql += "AND (sYSFAMILY LIKE '%" + restrictImportToFamily + "%' or sysfamily = '' or sysfamily is null )";

		//487936-B21	CHK	HDD;OPT	2;-2	HP ML350/ML370/DL370 G6 Two-bay LFF Drive Cage Option Kit

		//We need a list of every branch holding a product which add slots (we don't actually need the paths!)
		rdr = da.DBExecuteReader(con, sql);

		//OptionSKU>List of branches having that product
		Dictionary<string, List<clsBranch>> locs = new Dictionary<string, List<clsBranch>>();

		List<clsProduct> givers = new List<clsProduct>();
		//all the options that give slots
		while (rdr.Read) {
			if (iq.i_SKU.ContainsKey(rdr.Item("optsku"))) {
				givers.Add(iq.i_SKU(rdr.Item("optsku")));
			}
		}
		rdr.Close();

		clsBranch branch;
		Dictionary<Int32, string> index = new Dictionary<Int32, string>();
		foreach ( branch in iq.Branches.Values) {
			if (givers.Contains(branch.Product)) {
				if (!locs.ContainsKey(branch.Product.SKU))
					locs.Add(branch.Product.SKU, new List<clsBranch>());
				locs(branch.Product.SKU).Add(branch);
				index.Add(branch.ID, null);
			}
		}

		iq.Branches(1).indexProductBranchesByPath("tree", true, index);

		clsSlot aslot;
		clsSlotType st;
		int added = 0;
		int notfound = 0;

		//same SQL pass 2
		rdr = da.DBExecuteReader(con, sql);

		clsProduct optionProduct;

		while (rdr.Read) {
			string optSKU = rdr.Item("optsku");
			if (optSKU == "662883-B21") {
				object a = 1;
			}
			if (iq.i_SKU.ContainsKey(optSKU)) {
				if (optSKU == "675843-B21") {
					object a = 90;
				}
				optionProduct = iq.i_SKU(optSKU);

				string[] types = rdr.Item("slotaddType").ToString().Split(";".ToArray(), StringSplitOptions.RemoveEmptyEntries);
				string[] qtys = rdr.Item("slotaddqty").ToString().Split(";".ToArray(), StringSplitOptions.RemoveEmptyEntries);

				if (locs.ContainsKey(optSKU)) {
					List<clsBranch> done = new List<clsBranch>();

					foreach ( branch in locs(optSKU)) {
						for (i = 0; i <= UBound(types); i++) {
							// If iq.i_slotType_Code.ContainsKey(types(i)) Then
							// st = iq.i_slotType_MinorCode(types(i))
							// aslot = New clsSlot(st, branch, "", qtys(i), Nothing, New NullableInt(), 0, 0, wc)
							// added += 1
							// Else
							if (index.ContainsKey(branch.ID)) {
								object idx = index(branch.ID);
								if (!iq.i_slotType_Code.ContainsKey(types(i)))
									object f = new clsSlotType(types(i), types(i), iq.AddTranslation(types(i), English, "st", 0, null, 0, false));
								if (FindSystemBranch(idx) != null && FindSystemBranch(idx).Product.i_Attributes_Code.ContainsKey("FamMajor") && stDic.ContainsKey(FindSystemBranch(idx).Product.i_Attributes_Code("FamMajor").First.Translation.text(English))) {
									if (stDic(FindSystemBranch(idx).Product.i_Attributes_Code("FamMajor").First.Translation.text(English)).ContainsKey(types(i)) && iq.i_slotType_Code(types(i)).ContainsKey(stDic(FindSystemBranch(idx).Product.i_Attributes_Code("FamMajor").First.Translation.text(English))(types(i)))) {
										st = iq.i_slotType_Code(types(i))(stDic(FindSystemBranch(idx).Product.i_Attributes_Code("FamMajor").First.Translation.text(English))(types(i)));
										//Dim alreadythere = branch.slots.Where(Function(sl) sl.Value.Type Is st AndAlso Math.Sign(sl.Value.numSlots) <> Math.Sign(CInt(qtys(i))))
										//For Each s In alreadythere.ToList()
										//    s.Value.delete()
										//Next
										if (branch.slots != null && branch.slots.Where(sl => object.ReferenceEquals(sl.Value.Type, st) && Math.Sign(sl.Value.numSlots) == Math.Sign((int)qtys(i))).Count == 0) {
											aslot = new clsSlot(st, branch, "", qtys(i), null, new NullableInt(), 0, 0, wc);
											added += 1;
											Logit("DICT MATCH," + optSKU + "," + types(i) + "," + IsDBNull(rdr.Item("optFamily")) ? "" : rdr.Item("optFamily") + "," + stDic(FindSystemBranch(idx).Product.i_Attributes_Code("FamMajor").First.Translation.text(English))(types(i)));
										}
									} else {
										if (iq.i_slotType_Code.ContainsKey(types(i))) {
											st = iq.i_slotType_Code(types(i)).First.Value;
											if (branch.slots != null && branch.slots.Where(sl => object.ReferenceEquals(sl.Value.Type, st)).Count == 0)
												aslot = new clsSlot(st, branch, "", qtys(i), null, new NullableInt(), 0, 0, wc);
											added += 1;
										}
										Logit("NO MATCH FOR MINOR" + optSKU + "," + types(i) + "," + IsDBNull(rdr.Item("optFamily")) ? "" : rdr.Item("optFamily"));

									}
								} else {
									Logit("NO MATCH," + optSKU + "," + types(i) + "," + IsDBNull(rdr.Item("optFamily")) ? "" : rdr.Item("optFamily"));
								}
							} else {
								Logit("NO MATCH," + optSKU + "," + types(i) + "," + IsDBNull(rdr.Item("optFamily")) ? "" : rdr.Item("optFamily"));
							}


							//Dim sf As String() = rdr.Item("sysFamily").split(",")
							//Dim sf2 = stDic.Where(Function(std) stDic.Keys.Intersect(sf).Contains(std.Key)).Where(Function(fg) fg.Value.ContainsKey(types(i))).Select(Function(ff) ff.Value(types(i))).Distinct()
							//If sf2.Count > 1 Then
							//    Stop
							//ElseIf sf2.Count > 0 Then
							//    'Is our slot type actually in the family??? PSU Kits for example
							//    If iq.i_slotType_MinorCode.ContainsKey(sf2.First) Then
							//        st = iq.i_slotType_MinorCode(sf2.First)
							//        aslot = New clsSlot(st, branch, "", qtys(i), Nothing, New NullableInt(), 0, 0, wc)
							//        added += 1
							//        Logit("Added option type based on family dictionary lookup: " & optSKU & "," & types(i) & "," & If(IsDBNull(rdr.Item("optFamily")), "", rdr.Item("optFamily")))
							//    Else
							//        Debug.Print(types(i) & " is an unknown slot type")
							//    End If


							//  End If
						}

					}
				} else {
					notfound += 1;
				}
			} else {
				Logit("NO SKU," + optSKU);
			}

		}

		rdr.Close();

		da.BulkWrite(con, wc, "Slot");

		// If notfound > 0 Then Stop

		return "Added:" + added + " slot adds";

	}

	//Not used?

	private void OptionLimits(Dictionary<string, clsBranch> chassisbranches, Dictionary<string, clsSlotType> dicslottypes)
	{
		//option limits become the 'gives' slots on the chassis branches
		//Option limits only tell us the optType - but we need the more granular OptFamily - this dictionary provided the necessary (and tricky lookup)
		//                                sysfamily                  opttype optfamily
		Dictionary<string, Dictionary<string, string>> stDic = Import.FamilyOptTypeToOptFamily;

		SqlClient.SqlDataReader rdr;
		object sql;

		SqlClient.SqlConnection con;
		con = da.OpenDatabase();

		//chassisBranches are at a MinorFamily level
		//however some of the option limits are specified at a family level - wherein we need to make a slot in every chassis in the family
		//so - we need a dictionary of family>Minorfamilies

		Dictionary<string, List<string>> dicfamilies = new Dictionary<string, List<string>>(StringComparer.CurrentCultureIgnoreCase);

		//Sysfamilyname is broad(major)family, sysfamily is the narrow(minor) subfamily
		//DL560G8	DL560G8C5SFFLRD
		sql = "Select sysfamilyname as FamMajor,sysfamily as FamMinor from " + server + "iq.products.sysfamilydefinitions";

		//we build a dictionary of the 'broads' to all the 'narrows'
		rdr = da.DBExecuteReader(con, sql);
		while (rdr.Read) {
			if (!dicfamilies.ContainsKey(rdr.Item("FamMajor"))) {
				dicfamilies.Add(rdr.Item("FamMajor"), new List<string>());
			}

			dicfamilies(rdr.Item("famMajor")).Add(rdr.Item("famMinor"));
		}
		rdr.Close();




		DataTable slotWriteCache = da.MakeWriteCacheFor(con, "slot");

		sql = "SELECT [SysFamily],[OptFamily],[OptType],[QtyInstalled],[QtyMax],[Incr_Min],[Incr_Pref] from " + server + "[iq].[products].[OptionLimits] ";

		//Return WLAN,FAN,MEM,PSU etc (opt types)

		rdr = da.DBExecuteReader(con, sql);

		clsSlot aslot;
		clsBranch branch;

		clsSlotType st;
		string sysFam;
		string opttype;
		string optfamily;
		int Gives = 0;
		int lines = 0;

		List<string> dudFamilies = new List<string>();
		List<string> dudOptFamilies = new List<string>();

		while (rdr.Read) {
			lines += 1;

			sysFam = Trim(rdr.Item("sysfamily"));
			//this is sometimes a 'narrow' subFamily - sometimes the broad 'sysfamilyname'
			opttype = rdr.Item("opttype");
			optfamily = rdr.Item("optfamily");

			if (!stDic.ContainsKey(sysFam)) {
				if (!dudFamilies.Contains(sysFam)) {
					dudFamilies.Add(sysFam);
					Logit(sysFam + " is not a valid system family");
					//This is probably a MinorCode
					//and woudl expainl why we get no OS slots on some systems !
				}
			} else {
				if (!stDic(sysFam).ContainsKey(opttype)) {
				//some opt type for which we dont' need to limit by slots (software or card readers or something)
				//   Beep()
				} else {
					//Dim optfamily As String = stDic(sysFam)(opttype)
					if (!dicslottypes.ContainsKey(opttype + "^" + optfamily)) {
						if (!dudOptFamilies.Contains(opttype + "^" + optfamily)) {
							Logit(opttype + "^" + optfamily + " is not valid");
							dudOptFamilies.Add(opttype + "^" + optfamily);
						}
					} else {
						st = dicslottypes(opttype + "^" + optfamily);
						if (chassisbranches.ContainsKey(sysFam)) {
							branch = chassisbranches(sysFam);
							if (st.MajorCode == "CPU")
								System.Diagnostics.Debugger.Break();
							aslot = new clsSlot(st, branch, "", rdr.Item("qtymax"), null, new NullableInt(), 0, 0, slotWriteCache);
							Gives += 1;
						} else {
							//The sysFam wasn't a subfamily (it must be a broader 'family') - so we need to make this slot on every chassis in the family

							if (false) {
								//this is obsoleted 'Codes'

								if (dicfamilies.ContainsKey(sysFam)) {
									//make a slot on every chassis in the family


									foreach ( subfam in dicfamilies(sysFam)) {
										if (chassisbranches.ContainsKey(subfam)) {
											branch = chassisbranches(subfam);
											//     If rdr.Item("qtymax") = 0 Then Stop - means 'no maximum'
											aslot = new clsSlot(st, branch, "", rdr.Item("qtymax"), null, new NullableInt(), 0, 0, slotWriteCache);
											Gives += 1;
										}
									}
								}
							}
						}
					}
				}
			}
		}
		rdr.Close();

		da.BulkWrite(con, slotWriteCache, "slot");

		Logit("Done optionlimiits", false, true);

		con.Close();


	}


	//Not used -  ML?
	//    ,  cpuBranch As clsBranch, CPUsInstalled As Integer)
	private void MakeGivesSlots(clsBranch chassisBranch, string sysSubFamily, Dictionary<string, object> dicoptlimits, object dicslottypes, Dictionary<string, Dictionary<string, clsSlotType>> dicSubFamOptTypeSlotType, DataTable quantityWriteCache, DataTable slotWriteCache)
	{

		//---- OBSOLETED ---
		//now done 'in-line' in buildtree



		//Gives slots do NOT come from products.systems, or sysfamilydefinitions.. these only tell us the preinstalled quantities (although they do tell us the optionFamily)
		//they come from the optType limits - per system family

		//dicsysfam = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, clsProduct))))))
		//                                                 sysfam                       optcat                         optfam                          opttech                  opttype                       

		//Now make the 'gives' slots

		//Dim sysSubFamily As String  'comes from the iq.systems.familycode
		//sysSubFamily = Systembranch.Product.i_Attributes_Code("subFamily")(0).Translation.text(s_lang)  'IMPORTANT for compatibility

		//Dim syspath$
		//syspath$ = "tree." & Trim$(iq.RootBranch.ID)
		//syspath$ &= "." & Trim$(supplychain.Parent.Parent.ID) 'System type
		//syspath$ &= "." & Trim$(supplychain.Parent.ID) 'Family
		//syspath$ &= "." & Trim$(supplychain.ID) 'supply chain  '<<<new
		//syspath$ &= "." & Trim$(systemBranch.ID) 'system

		clsLimit limit;

		//make the gives slots on each system for each option type (MEM/HDD/CPU etc.) - according to the option type limits  (note - this does not do the PCI slots)
		clsSlot gslot;

		clsSlotType st;

		//Dim optTypekey As String
		//For Each opttypebranch In optCatBranch.childBranches.Values  'key In dicsysfam(optCatkey).keys
		//    '                                                                                                                subfam       cat           OptType>limit 
		//    optTypekey = optTypekeys(opttypebranch)

		//    'graft on the CPU (from a 'master' tree of CPU's)
		//    If optTypekey = "CPU" Then
		//        ' If Not opttypebranch.childBranches.Values.Contains(cpuBranch) Then
		//        If Not opttypebranch.childBranches.Values.Contains(cpuBranch) Then
		//            opttypebranch.Graft(cpuBranch, "", graftwriteCache)  'very neat
		//            ' End If
		//            Dim cpupath$ = syspath$ & "." & Trim$(opttypebranch.ID) & "." & Trim$(cpuBranch.ID)

		//            Dim minIncr As Integer = 1
		//            Dim preferredIncr As Integer = 1
		//            Dim limits As clsLimit
		//            If dicoptlimits(sysSubFamily).containskey("Performance") Then
		//                If dicoptlimits(sysSubFamily)("Performance").containskey("CPU") Then
		//                    limits = dicoptlimits(sysSubFamily)("Performance")("CPU")
		//                    preferredIncr = limits.PrefIncr
		//                    minIncr = limits.MinIncr
		//                End If
		//            End If

		//            'AutoAdd (of the right CPU)
		//            Dim qty As New clsQuantity(iq.i_region_code("IX"), cpupath$, cpuBranch, iq.StandardVariant, cpuqty, minIncr, preferredIncr, True, quantityWriteCache)

		//            'make each CPU take a CPU slot
		//            Dim cpuTakeslot As clsSlot = New clsSlot(iq.i_slotType_code("CPU"), cpuBranch, cpupath, -1, Nothing, New NullableInt(), 1, 0, slotWriteCache)
		//        End If
		//    End If

		//are there special limits.increments on this option Type (slot type) 

		if (dicoptlimits.ContainsKey(sysSubFamily)) {
			foreach ( optCatkey in dicoptlimits(sysSubFamily).keys) {
				//The opttype keys are MEM,CPU, HDD etc ... not granular enough for slots - so we look the right slot type up for the subFamily/OptType
				foreach ( optTypekey in dicoptlimits(sysSubFamily)(optCatkey).keys) {

					limit = dicoptlimits(sysSubFamily)(optCatkey)(optTypekey);

					st = dicSubFamOptTypeSlotType(sysSubFamily)(optTypekey);
					//If dicslottypes.ContainsKey(optTypekey) Then

					//st = dicslottypes(optTypekey)
					//                                                                                                                     requiredFill
					//If limit.Qmin Then Stop ' Required fill (check they're goning in on)
					gslot = new clsSlot(st, chassisBranch, "", limit.Qmax, null, new NullableInt(), limit.Qmin, 0, slotWriteCache);
					//Else
					//Logit("invalid slot/option type:" & optTypekey)
					// End If
				}
			}
		}

	}


	/// <summary>makes autoAdds (quantities) for FIOs, takes slots - and prunes incompatible option branches - on single option category branch (eg. Performance,Managment....)</summary>
	/// 
	//                         sysfam                  l1                  l2                     optSn  
	//dicsysfam As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String,  Dictionary(Of Integer, clsProduct)))))), _
	//dicsysfam As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, clsProduct)))


	//Private Function AddOptions(ByRef l2branch As clsBranch, l2key As String, sysfamkey As String, l1key As String, _
	//                            optionsByCk As Dictionary(Of String, clsProduct), _
	//                            dicopttype As Object, opttranssingular As clsTranslation, opttrans As clsTranslation, _
	//                            ByRef branchWritecache As DataTable, dicoptfam As Object, dicOptTech As Object, ByRef NextBranchID As Integer) As clsBranch

	//    'makes a complete option type branch  


	//    AddOptions = Nothing

	//    '    Dim optTypeBranch As clsBranch
	//    Dim optFamBranch As clsBranch
	//    Dim optTechBranch As clsBranch
	//    Dim optBranch As clsBranch

	//    '    optTypeBranch = New clsBranch(Nothing, optCatBranch, dicopttype(LCase(optTypeKey)).Translation, "", optTrans, optTransSingular, Nothing, branchWriteCache, nextBranchID)

	//    For Each l3keyoptFamKey In optionsByCk(sysfamkey)(l1key)(l2).Keys
	//        'create an option family branch under the option type branch
	//        optFamBranch = New clsBranch(Nothing, optTypeBranch, dicoptfam(optFamKey), "", opttrans, opttranssingular, Nothing, 100, False, "B", branchWritecache, NextBranchID)
	//        For Each optTechKey In dicsysfam(sysfamkey)(optCatKey)(opttypekey)(optFamKey).Keys
	//            'create an option technology branch under the option family branch

	//            optTechBranch = New clsBranch(Nothing, optFamBranch, dicOptTech(optTechKey), "", opttrans, opttranssingular, Nothing, 100, False, "B", branchWritecache, NextBranchID)
	//            'create the Option branches under the technology branch
	//            For Each opt As clsProduct In dicsysfam(sysfamkey)(optCatKey)(opttypekey)(optFamKey)(optTechKey).Values

	//                Dim optName As clsTranslation
	//                If opt.i_Attributes_Code.ContainsKey("~ame") Then
	//                    optName = opt.i_Attributes_Code("~ame")(0).Translation
	//                Else
	//                    optName = opt.i_Attributes_Code("MfrSKU")(0).Translation
	//                End If

	//                '                                               If opt.i_attributes_code("~ame").Translation.ID(English) <= 0 Then Stop
	//                ' Debug.Print(opt.i_attributes_code("MfrSKU").Translation.text(English))

	//                optBranch = New clsBranch(opt, optTechBranch, optName, "", opttrans, opttranssingular, Nothing, 100, False, "B", branchWritecache, NextBranchID)

	//                'Else
	//                ' Beep()
	//                'End If
	//            Next opt
	//        Next optTechKey
	//    Next optFamKey

	//End Function


	private bool HaveLimits(object dicOptLimits, string SysSubFamily, string optcatkey, string opTtypeKey)
	{

		//                                                  sysSubFam             optcat                       optiontype       limit
		//        Dim dicOptLimits As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, IQ.clsLimit)))


		HaveLimits = false;
		if (dicOptLimits.ContainsKey(SysSubFamily)) {
			if (dicOptLimits(SysSubFamily).ContainsKey(optcatkey)) {
				if (dicOptLimits(SysSubFamily)(optcatkey).ContainsKey(opTtypeKey)) {
					return true;
				}
			}
		}


	}
	public string Abbreviation(@in)
	{

		if (dicAbbreviations.ContainsKey(@in))
			return dicAbbreviations(@in);

		return @in;

	}

	public bool isFIO(string optionSKU, string systemSKU, path, object dicFIOs, object dicOptLocalisation, clsLimit Increments, ref DataTable quantityWriteCache, clsActionList ActionList = null)
	{

		//FIOs are per system - whereas option limits are per family
		//hence FIOS qtyIns override options limits --

		isFIO = false;

		//make pre-installed quantity in the context of the specified system branch
		//add a more specific (locally scoped) quantity limits carrying the preinstalled qty's
		//for mem,cpu, pristore etc

		clsQuantity aQuantity;

		clsBranch optionbranch;
		optionbranch = iq.Branches(Split(path, ".").Last);

		//If optionbranch.Product.i_Attributes_Code.ContainsKey("cpuSKU") Then Stop

		// If systemSKU = "704558-421" And optionSKU = "715218-B21" Then Stop

		//Is this option - factory fitted in the currrent system
		if (dicFIOs.ContainsKey(systemSKU)) {

			if (dicFIOs(systemSKU).ContainsKey(optionSKU)) {
				int fittedQTY;
				fittedQTY = dicFIOs(systemSKU)(optionSKU);
				if (fittedQTY == 0)
					System.Diagnostics.Debugger.Break();

				if (fittedQTY == -1) {
					//there was a system whos PUSqty (for example) was Null - see import.fios
					fittedQTY = Increments.Qinstalled;
					//Increments come from the (vague)  vw291eaoptionLimits - which are per familiy PSUQty, RamQty etc.
					//    If fittedQTY = 0 Then Stop -  investigate - this does happen ! vw291ea  - ar941aa (for example)

				}

				isFIO = true;

				if (dicOptLocalisation.ContainsKey(optionbranch.Product)) {

					foreach ( region in dicOptLocalisation(optionbranch.Product)) {
						object ck__1 = optionbranch.ID + "^" + region.id + "^" + path;
						//aQuantity = New clsQuantity(region, path, optionbranch, Nothing, fittedQTY, limit.MinIncr, limit.PrefIncr, True, quantityWriteCache) 'note - preinstalled options are 'Free of Charge' (last parameter)
						if (i_Quantities.Contains(ck__1)) {
							NoOp();
						} else {
							if (path == "")
								System.Diagnostics.Debugger.Break();
							//these should alwyas have a path (i think)
							if (ActionList == null || ActionList.IsGo(optionSKU, systemSKU, ActionType.INSERT, ObjectType.Quantity, fittedQTY + "," + Increments.MinIncr + "," + Increments.PrefIncr)) {
								aQuantity = new clsQuantity(region, path, optionbranch, fittedQTY, Increments.MinIncr, Increments.PrefIncr, true, quantityWriteCache);
								//note - preinstalled options are 'Free of Charge' (last parameter)
								i_Quantities.Add(ck__1);
							} else {
								ActionList.Add(optionSKU, systemSKU, ActionType.INSERT, ObjectType.Quantity, fittedQTY + "," + Increments.MinIncr + "," + Increments.PrefIncr);
							}
						}
					}
				} else {
					if (fittedQTY > 0 | Increments.MinIncr > 1 | Increments.PrefIncr > 1) {
						object CK__2 = optionbranch.ID + "^" + r_worldwide.ID + "^" + path;
						//aQuantity = New clsQuantity(r_worldwide, path, optionbranch, Nothing, fittedQTY, limit.MinIncr, limit.PrefIncr, True, quantityWriteCache)
						if (i_Quantities.Contains(CK__2)) {
							NoOp();
						} else {
							if (path == "")
								System.Diagnostics.Debugger.Break();
							//these should alwyas have a path (i think)
							//   If fittedQTY < 1 Then Stop
							//If ActionList Is Nothing OrElse ActionList.IsGo(optionSKU, systemSKU, ActionType.INSERT, ObjectType.Quantity, fittedQTY & "," & Increments.MinIncr & "," & Increments.PrefIncr) Then
							aQuantity = new clsQuantity(r_worldwide, path, optionbranch, fittedQTY, Increments.MinIncr, Increments.PrefIncr, true, quantityWriteCache);
							i_Quantities.Add(CK__2);
							//Else
							//ActionList.Add(optionSKU, systemSKU, ActionType.INSERT, ObjectType.Quantity, fittedQTY & "," & Increments.MinIncr & "," & Increments.PrefIncr)
							//End If
						}
					}
				}
			}
		} else {
			NoOp();
			//no fios in the system 
		}

	}

	public object Compatible(clsBranch optionbranch, string sysSubFamily)
	{

		Compatible = true;

		if (!optionbranch.Product == null) {
			if (optionbranch.Product.i_Attributes_Code.ContainsKey("incompat")) {
				string IncompatibleSubFamilies = optionbranch.Product.i_Attributes_Code("incompat")(0).Translation.text(s_lang);
				List<string> li;
				li = Split(UCase(IncompatibleSubFamilies), ",").ToList;
				if (li.Contains(UCase(sysSubFamily))) {
					Compatible = false;
				}
			}
		}

	}

	///<summary>RESTRICTS *where* a product can be (or is auto)  added - We don't make quantity records for unrestricted parts</summary>
	///<remarks>If there is no quantity record attached to a branch it is assumed to be available everywhere with a qinstalled of zero, and a minIncr of 1</remarks>

	public void MakeLocalisedQuantity(clsBranch optionbranch, clsVariant skuvariant, clsLimit limit, Dictionary<clsProduct, List<clsRegion>> dicOptLocalisation, DataTable quantityWriteCache, path, clsActionList ActionList = null)
	{
		clsQuantity aquantity;

			//aquantity = New clsQuantity(region, "", optionbranch, skuvariant, .Qinstalled, .MinIncr, .PrefIncr, False, quantityWriteCache)

			//aquantity = New clsQuantity(region, path$, optionbranch, .Qinstalled, .MinIncr, .PrefIncr, False, quantityWriteCache)
			//aquantity = New clsQuantity(r_worldwide, path$, optionbranch, .Qinstalled, .MinIncr, .PrefIncr, False, quantityWriteCache)

		 // ERROR: Not supported in C#: WithStatement


	}



	public void units(SqlClient.SqlConnection con, Dictionary<string, clsUnit> dicunits)
	{
		//returns a dictionary of unit codes

		SqlClient.SqlDataReader rdr;
		object sql;
		clsUnit aunit;


		// NULL
		// GB
		//GHz
		//in
		//k rpm
		// MB
		//MHz
		//TB
		//VA

		sql = "SELECT DISTINCT optTypeSpeedUnit u from " + server + "[iq].products.opttypes UNION SELECT optTypeUnit u from " + server + "[iq].products.opttypes";
		rdr = da.DBExecuteReader(con, sql);
		while (rdr.Read) {
			if (!IsDBNull(rdr.Item("u"))) {

				if (!dicunits.ContainsKey(rdr.Item("u"))) {
					aunit = new clsUnit(Trim(rdr.Item("u")), iq.AddTranslation(rdr.Item("u"), English, "units", 0, null, 0, false), "", 0);
					dicunits.Add(rdr.Item("u"), aunit);

				}
			}
		}
		rdr.Close();

	}


	//Not used - ML
	//Operating systems
	public void OSs()
	{

		SqlClient.SqlConnection con = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;");
		SqlClient.SqlDataReader rdr;

		int count;
		count = 0;

		clsUnit textUnit = iq.i_unit_code("txt");
		object prods = iq.Products.Where(p => p.Value.hasSKU()).Select(p => new {
			Sku = p.Value.SKU,
			Id = p.Key
		}).ToList();
		//fetch the (long) descriptions for every System
		rdr = da.DBExecuteReader(con, "select software,ModelSKU from " + server + "[iq].[products].[systems] where software is not null");
		clsProductAttribute desc;
		while (rdr.Read) {
			object g = prods.Where(a => a.Sku == Trim(rdr.Item("ModelSKU"))).FirstOrDefault();
			if (g != null) {
				clsProduct cp = iq.Products(g.Id);

				clsTranslation tl;
				string s = rdr.Item("software").ToString().Contains(",") ? Split(rdr.Item("software"), ",")(0) : rdr.Item("software");
				tl = iq.Translations.Where(d => d.Value.text(English) == s).Select(f => f.Value).FirstOrDefault();
				if (tl == null) {
					tl = iq.AddTranslation(s, English, "", 0, null, 0, false);
				}
				if (!cp.i_Attributes_Code.ContainsKey("os"))
					desc = new clsProductAttribute(cp, iq.i_attribute_code("os"), 0, textUnit, tl);
				count += 1;

			}
		}
		rdr.Close();


	}

	//This is done in the incremental import
	public int SystemDescriptions(SqlClient.SqlConnection con, ref Dictionary<string, clsTranslation> dicDescs, Dictionary<string, clsBranch> dicsystems)
	{

		SqlClient.SqlDataReader rdr;

		int count;
		count = 0;


		int nextkey = clsTranslation.NextKey();
		DataTable TranslationWriteCache;


		TranslationWriteCache = da.MakeWriteCacheFor(con, "Translation");

		clsUnit textUnit = iq.i_unit_code("txt");

		//fetch the (long) descriptions for every System
		rdr = da.DBExecuteReader(con, "select ccdescription,upcnum from " + server + "[iq].[products].[HierarchyIQ]");
		clsProduct system;
		clsProductAttribute desc;
		while (rdr.Read) {
			if (dicsystems.ContainsKey(Trim(rdr.Item("upcnum")))) {
				if (!dicDescs.ContainsKey(rdr.Item("upcnum"))) {
					system = dicsystems(Trim(rdr.Item("upcnum"))).Product;

					//note we don't need to add this to the systems attributes collection - the object model does that for us
					//just making the ProductAttribute (against the product) is enough
					clsTranslation tl;
					tl = iq.AddTranslation(rdr.Item("ccdescription"), s_lang, "sysDesc", 0, TranslationWriteCache, nextkey, false);
					dicDescs.Add(rdr.Item("upcnum"), tl);
					desc = new clsProductAttribute(system, iq.i_attribute_code("desc"), 0, textUnit, tl);

				}
				count += 1;
			}
		}
		rdr.Close();

		da.BulkWrite(con, TranslationWriteCache, "Translation");
		TranslationWriteCache = null;


		return count;

	}

	public Dictionary<string, string> LoadPLCodes(SqlClient.SqlConnection con)
	{

		//returns a dictionary of SKU to PLcode

		Dictionary<string, string> plc = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

		SqlClient.SqlDataReader rdr;
		rdr = da.DBExecuteReader(con, "SELECT UPCNum,Isnull(PL,'none') as pl from " + server + "[iq].products.hierarchyIQ");

		while (rdr.Read) {
			plc.Add(Trim(rdr.Item("upcnum")), rdr.Item("PL"));
		}

		rdr.Close();

		return plc;


	}



	public void LoadTranslations(SqlClient.SqlConnection con)
	{
		//Loads up Dans IQ Translations into the public Dictionary 'Xlate' - only used for the purposes of importing

		//which looks like this:-
		//                             hello                 fr      bonjour
		//Dim xlate As New Dictionary(Of String, Dictionary(Of String, String))

		//and is accessed like this:-

		//greeting=xlate("hello")("fr")

		SqlClient.SqlDataReader rdr;
		rdr = da.DBExecuteReader(con, "SELECT textid,en,de,es,fr,it,tr from " + server + "[iq].dbo.language_key");

		object languages = Split("de,es,fr,it,tr", ",");

		xlate.Clear();

		string Key;
		while (rdr.Read) {
			Key = rdr.Item("EN");

			if (!xlate.ContainsKey(Key)) {
				xlate.Add(Key, new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase));
				 // ERROR: Not supported in C#: WithStatement

			}
		}

		rdr.Close();

	}


	public void SysTypes(SqlClient.SqlConnection con, ref Dictionary<string, clsBranch> dicSysTypes)
	{
		//SysTypes (Desktop,Server,Storage,notebook) are now the first level in the tree.
		//and are added as branches under the root node IQ.Root

		clsBranch sysType;
		SqlClient.SqlDataReader rdr;
		string sysTypeEN;

		rdr = da.DBExecuteReader(con, "SELECT code,translation from " + server + "[iq].dbo.Abbreviations WHERE code IN ('DTO','NBK','SVR','SWD','HPN')");

		//Dim Name As clsProductAttribute
		//Dim TextUnit As clsUnit
		//TextUnit = iq.Units("txt")
		//Dim NameAttribute As clsAttribute
		//NameAttribute = iq.Attributes("~ame")

		//tranlation keys for the collective noun 
		clsTranslation collective = iq.AddTranslation("families", English, "collect", 0, null, 0, false);
		clsTranslation collectiveSingular = iq.AddTranslation("family", English, "collect", 0, null, 0, false);


		while (rdr.Read) {
			sysTypeEN = rdr.Item("translation");
			if (!dicSysTypes.ContainsKey(rdr.Item("code"))) {
				//                                                                       \/ iQ1

				object bn = rdr.Item("translation");
				clsTranslation btl = iq.AddTranslation(bn, English, "SysTypes", 0, null, 0, false);

				sysType = new clsBranch(null, iq.RootBranch, btl, "/images/iq/prod_range_" + rdr.Item("code") + ".jpg", collective, collectiveSingular, null, 100, false, "S");
				dicSysTypes.Add(rdr.Item("code"), sysType);
			}

		}

		rdr.Close();

	}

	public string ConvertPCIMinorToMajor(string minor)
	{
		switch (Left(minor.ToUpper, 4)) {
			case "PCIE":
			case "PCIG":
				object cw = Mid(minor, 7, minor.IndexOf("B", 7) - 6);
				//connector width
				object bw = Mid(minor, minor.IndexOf("B", 7) + 2, (minor.IndexOf("_", minor.IndexOf("B", 7) + 1)) - (minor.IndexOf("B", 7) + 1));
				//bus width
				switch (bw) {
					case "133":
						return "PCIX";
					case "16":
						return "PCIG";
					case "8":
						return "PCIF";
					case "4":
						return "PCIE";
					case "1":
						return "PCIC";
					case "0":
						return "RISER";

				}
			case "PCI_":
				object speed = Mid(minor, 6, minor.IndexOf("B", 6) - 5);
				object bw = Mid(minor, minor.IndexOf("B", 6) + 2, (minor.IndexOf("_", minor.IndexOf("B", 6) + 1)) - (minor.IndexOf("B", 6) + 1));
				switch (speed) {
					case "0":
						return "RISER";
					default:
						return "PCI";
				}
			case "KOD":
				return "KOD";
			default:
				if (Mid(minor, 5, 1) == "_")
					return Left(minor, 4);
				return minor;
		}
		return minor;
	}



	public void AddPCIChassisSlots(string minorfamily, string majorfamily, clsBranch systemBranch, DataTable tlwc, ref int nextkey, DataTable swc)
	{
		object con = da.OpenDatabase();
		//for every system in the family of each slot type - set the slots (1127 rows)
		object Sql = "SELECT familyname,slotnum,pciCode,dedicated,dedisku,notes FROM h3.[iq].products.sysfamilyPCIslots ";
		Sql += " WHERE FamilyName='" + minorfamily + "' ";
		Sql += "ORDER BY familyname,pcicode,slotnum ";

		object rdr = da.DBExecuteReader(con, Sql);

		if (rdr.RecordsAffected == 0) {
			Sql = "SELECT familyname,slotnum,pciCode,dedicated,dedisku,notes FROM h3.[iq].products.sysfamilyPCIslots ";
			Sql += " WHERE FamilyName='" + majorfamily + "' ";
			Sql += "ORDER BY familyname,pcicode,slotnum ";

			rdr = da.DBExecuteReader(con, Sql);
		}

		clsSlot aslot;
		int slots = 0;
		bool found = false;
		int notFound = 0;
		IQ.clsTranslation notes;
		DBNull aNull = DBNull.Value;
		IQ.NullableInt slotNum;
		List<string> dudFams = new List<string>();
		string majorcode;
		string minorcode;


		StreamWriter sw = new StreamWriter("c:\\temp\\badPCISlots.txt", true);

		//For every PCI slot definition
		while (rdr.Read) {
			found = false;

			//This column is actually familyCODE - the longer version - although there is some left over FamilyName (EG.DL360) data
			object rfam = rdr.Item("familyname");
			//go through every system - and if the familyCODE matches - make slots on that system

			if (rdr.Item("pcicode") == "") {
				minorcode = "KOD";
				//Knocked out slots (some pci slots are 'knocked out' by risres cards, chassis kits etc.
			} else {
				minorcode = fixPci(rdr.Item("pcicode"));
				//make sure it has 4 parts (inlcluding a GEN which may well be blank
				minorcode += "_" + Math.Abs((int)rdr.Item("dedicated"));
			}

			if (IsDBNull(rdr.Item("notes"))) {
				notes = null;
			} else {
				notes = iq.AddTranslation(rdr.Item("notes"), s_lang, "SlotNotex", 0, tlwc, nextkey, false);
			}

			if (IsDBNull(rdr.Item("slotnum"))) {
				slotNum = new IQ.NullableInt(DBNull.Value);
			} else {
				//This is a little messy as the IQ1 slotnum is a byte (usually you could pass the value straight from the reader
				if (IsDBNull(rdr.Item("slotNum"))) {
					slotNum = new IQ.NullableInt();
				} else {
					slotNum = new NullableInt((int)rdr.Item("slotnum"));
				}
			}

			majorcode = ConvertPCIMinorToMajor(minorcode);

			if (majorcode == minorcode) {
				sw.WriteLine("couldn't convert" + minorcode + " for " + rfam);


			} else {
				if (!(iq.i_slotType_Code.ContainsKey(majorcode) && iq.i_slotType_Code(majorcode).ContainsKey(minorcode))) {
					//need to create the missing slot type 
					//Dim slotMajor As clsSlotType = New clsSlotType(majorcode, minorcode)
					sw.WriteLine("iq.i_slottype(" + majorcode + ") does not contain " + minorcode);

				} else {
					clsSlotType st = iq.i_slotType_Code(majorcode)(minorcode);
					clsSlot tmpslot = new clsSlot(st, null, "", 1, notes, slotNum, 0, 0);

					if (!systemBranch.i_Slots.ContainsKey(tmpslot.compoundKey)) {
						//Make the missing slot (which addes it to the branch index etc,etc)
						aslot = new clsSlot(st, systemBranch, "", 1, notes, slotNum, 0, 0, swc);

						//ck = aslot.compoundKey  
						//b.Slots.Add(ck, aslot) 'NOOOO you don't need to do this - it's automatically added to the branch
						slots += 1;

					//  ImportLog.Add(DateTime.Now, String.Format("Creating new Slot " & tmpslot.compoundKey))

					} else {
						//  ImportLog.Add(DateTime.Now, String.Format("Found " & tmpslot.compoundKey & ". Not importing."))
						//aslot = New clsSlot(dicSlotTypes("UNAVAIL"), systemBranch, "", 1, notes, slotNum, 0, 0)

						//Logit(systemBranch.SKU & " in " & rfam & " has an invalid PCI slot type '" & rdr.Item("pcicode") & "'")
						//    Stop
					}
				}
			}

		}

		sw.Close();

		rdr.Close();
		con.Close();

	}


	public Dictionary<string, clsSlotType> slotTypes(SqlClient.SqlConnection con, Dictionary<string, clsBranch> dicSystems, bool rOnly = false)
	{

		//SlotType minor code (E.G.NHPSFF3.5DD > clsslottype (containing majorcode, minorcode, ID, fallback info)

		object sql;

		Dictionary<string, clsSlotType> dicSlotTypes;
		//this is the return value of the function
		dicSlotTypes = new Dictionary<string, clsSlotType>(StringComparer.CurrentCultureIgnoreCase);
		//SlotType minor code (E.G.NHPSFF3.5DD > clsslottype (containing majorcode, minorcode, ID, fallback info)

		sql = "Select distinct PCICode,dedicated from " + server + "[iq].products.SysFamilyPCIslots";

		SqlClient.SqlDataReader rdr;
		clsSlotType aSlotType;
		rdr = da.DBExecuteReader(con, sql);

		string minorCode;
		string majorCode;
		pciStruct slotdesc;

		while (rdr.Read) {
			//was = ???
			if (rdr.Item("pciCode") != "") {
				minorCode = fixPci(rdr.Item("pcicode"));
				//make sure it has 4 parts (inlcluding a GEN which may well be blank
				minorCode += "_" + Math.Abs((int)rdr.Item("dedicated"));
				majorCode = ConvertPCIMinorToMajor(minorCode);
				slotdesc = ExpandPCI(minorCode);
				if (iq.i_slotType_Code.ContainsKey(majorCode) && iq.i_slotType_Code(majorCode).ContainsKey(minorCode)) {
					aSlotType = iq.i_slotType_Code(majorCode)(minorCode);
				} else {
					if (rOnly) {
						if (iq.i_slotType_Code.ContainsKey(majorCode) && iq.i_slotType_Code(majorCode).ContainsKey(minorCode)) {
							aSlotType = iq.i_slotType_Code(majorCode)(minorCode);
						}

					} else {
						aSlotType = new clsSlotType(majorCode, minorCode, iq.AddTranslation(slotdesc.fullText, English, "PCIST", 0, null, 0, true));
					}

				}
				dicSlotTypes.Add(majorCode + "^" + minorCode, aSlotType);
				//store a lookup by original code

			}
		}

		//Knocked out (KO'd) slot's (obscured PCI slots)
		if (!dicSlotTypes.ContainsKey("PCI^KOD")) {
			clsSlotType kod;
			if (rOnly) {
				if (iq.i_slotType_Code.ContainsKey("PCI") && iq.i_slotType_Code("PCI").ContainsKey("KOD")) {
					kod = iq.i_slotType_Code("PCI")("KOD");
				} else {
					kod = new clsSlotType("PCI", "KOD", iq.AddTranslation("Physically Obstructed", English, "PCIST", 0, null, 0, true));
				}


			}
			dicSlotTypes.Add("PCI^KOD", kod);
		}

		rdr.Close();

		pciStruct ospec;
		pciStruct ispec;

		if (!rOnly) {
			//Find All the fallback slot types

			//probably need to consider dedicated slots more carefully
			foreach ( ko in iq.SlotTypes.Keys) {
				//nneceesary (had to hack it in during an import)
				if (UBound(Split(iq.SlotTypes(ko).MinorCode, "_")) == 4) {
					ospec = ExpandPCI(iq.SlotTypes(ko).MinorCode);
					foreach ( ki in iq.SlotTypes.Keys) {
						//nneceesary (had to hack it in during an import)
						if (UBound(Split(iq.SlotTypes(ki).MinorCode, "_")) == 4) {
							if (ki > ko) {
								ispec = ExpandPCI(iq.SlotTypes(ki).MinorCode);
								//same 'technology' (PCIe/x/BLcm
								if (ispec.tech == ospec.tech) {
									//slotform 1,8,16 - only add higher connector width slots
									if (ispec.connector > ospec.connector) {
										//Speed 4x 8x 16x - only use higher speed slots
										if (ispec.speed > ospec.speed) {
											//only an alternative if same or wider AND same or higher
											if (ispec.w >= ospec.w & ispec.h >= ospec.h) {
												 // ERROR: Not supported in C#: WithStatement

											}
										}
									}
								}
							}
						}
					}
				}
			}



			//for every system in the family of each slot type - set the slots (1127 rows)
			sql = "SELECT familyname,slotnum,pciCode,dedicated,dedisku,notes FROM " + server;
			sql += "[iq].products.sysfamilyPCIslots ";
			sql += "WHERE (FAMILYname LIKE '%" + restrictImportToFamily + "%' or familyname = '' or familyname is null)";
			sql += "ORDEr BY familyname,pcicode,slotnum ";

			rdr = da.DBExecuteReader(con, sql);

			clsSlot aslot;
			int slots = 0;
			bool found = false;
			string Fam;
			string FamMaj;
			int notFound = 0;
			IQ.clsTranslation notes;
			DBNull aNull = DBNull.Value;
			IQ.NullableInt slotNum;
			List<string> dudFams = new List<string>();

			//For every PCI slot definition
			while (rdr.Read) {
				found = false;

				//This column is actually familyCODE - the longer version - although there is some left over FamilyName (EG.DL360) data
				object rfam = rdr.Item("familyname");
				//go through every system - and if the familyCODE matches - make slots on that system
				foreach ( systemBranch in dicSystems.Values) {
					Fam = systemBranch.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English);
					FamMaj = systemBranch.Product.i_Attributes_Code("famMajor")(0).Translation.text(English);

					if (LCase(Trim(Fam)) == LCase(Trim(rfam)) | LCase(Trim(FamMaj)) == LCase(Trim(rfam))) {
						found = true;

						if (rdr.Item("pcicode") == "") {
							minorCode = "KOD";
							//Knocked out slots (some pci slots are 'knocked out' by risres cards, chassis kits etc.
						} else {
							minorCode = fixPci(rdr.Item("pcicode"));
							//make sure it has 4 parts (inlcluding a GEN which may well be blank
							minorCode += "_" + Math.Abs((int)rdr.Item("dedicated"));
						}

						if (IsDBNull(rdr.Item("notes"))) {
							notes = null;
						} else {
							notes = iq.AddTranslation(rdr.Item("notes"), s_lang, "SlotNotex", 0, null, 0, false);
						}

						if (IsDBNull(rdr.Item("slotnum"))) {
							slotNum = new IQ.NullableInt(DBNull.Value);
						} else {
							//This is a little messy as the IQ1 slotnum is a byte (usually you could pass the value straight from the reader
							if (IsDBNull(rdr.Item("slotNum"))) {
								slotNum = new IQ.NullableInt();
							} else {
								slotNum = new NullableInt((int)rdr.Item("slotnum"));
							}
						}

						majorCode = ConvertPCIMinorToMajor(minorCode);

						if (dicSlotTypes.ContainsKey(majorCode + "^" + minorCode)) {
							//these are the GIVES slots (on every system) - for PCIslots only

							aslot = new clsSlot(dicSlotTypes(majorCode + "^" + minorCode), systemBranch, "", 1, notes, slotNum, 0, 0);

							//ck = aslot.compoundKey  
							//b.Slots.Add(ck, aslot) 'NOOOO you don't need to do this - it's automatically added to the branch
							slots += 1;

						} else {
							//aslot = New clsSlot(dicSlotTypes("UNAVAIL"), systemBranch, "", 1, notes, slotNum, 0, 0)

							Logit(systemBranch.SKU + " in " + rfam + " has an invalid PCI slot type '" + rdr.Item("pcicode") + "'");
							//    Stop
						}
					}
				}

				if (!found) {
					if (!dudFams.Contains(rfam)) {
						Logit("Could not locate a system within the subFamily #" + rfam + "#");
						dudFams.Add(rfam);
					}
					notFound += 1;
				}
			}

			rdr.Close();
		}
		Logit("making slot types", false, true);
		//Read the distinct product.options.optfamily codes to make the slot types for memory, drive bays etc.

		//OptType is not granular enough - it doesnt distinguish between the drive types - so inappropriate drive type are not pruned off the 
		//optfamily is too grainy - meaning you can't search by the number of drive bays or processor slots - because they're not the same slot type

		sql = "SELECT DISTINCT optfamily,opttype  from " + server + "[iq].products.options order by opttype";
		//was optfamily (is more granular)
		rdr = da.DBExecuteReader(con, sql);

		string major = "";
		string minor = "";

		//build a dictionary of minor > slottype - where 
		string mapTo = "";

		while (rdr.Read) {
			major = rdr.Item("OptType");
			//HDD,PSU,WAR,CPU
			minor = rdr.Item("optFamily");
			//NHP35LFFSC   'was optfamily - opttype is the broader types HDD,PSU,WTY etc  - OptType is no good to us
			object txt;

			//@@@@@ IMPORTANT
			if (major == "CHK" & InStr(UCase(minor), "_SC")) {
				major = "HDD";
				// map all the chassis kit smart carriers to hard drive (major) slot types
			}

			// If major = "HDD" Or major = "OPT" Then  'consolidate all non-drive type slots (CPUs, Carepacks etc)
			// mapTo = minor
			// Else
			// mapTo = major
			// End If

			if (dicAbbreviations.ContainsKey(minor))
				txt = dicAbbreviations(minor);
			else
				txt = minor;

			//need to map - 
			if (dicSlotTypes.ContainsKey(major + "^" + minor)) {
				// If Not dicSlotTypes.ContainsKey(minor) Then
				dicSlotTypes.Add(major + "^" + minor, dicSlotTypes(mapTo));
			//End If
			} else {
				//If Not dicSlotTypes.ContainsKey(minor) Then
				object st;
				if (rOnly) {
					if (iq.i_slotType_Code.ContainsKey(major) && iq.i_slotType_Code(major).ContainsKey(minor))
						st = iq.i_slotType_Code(major)(minor);
				} else {
					st = new clsSlotType(major, minor, iq.AddTranslation(txt, English, "ST", 0, null, 0, false));
				}

				dicSlotTypes.Add(major + "^" + minor, st);
				//End If
			}
		}
		rdr.Close();

		//familyMan


		Logit("finished slot types", false, true);

		return dicSlotTypes;

	}

	public string FixPCI(string code)
	{

		//some PCI codes are missing their generation - add the required extra underscore
		if (code == "") {
			code = "___";
		} else {
			if (Split(code, "_").Length < 4)
				code += "_";
		}

		FixPCI = code;


	}
	public pciStruct ExpandPCI(string code)
	{

		//Takes an IQuote1 style code for a PCI slot
		//Returns a popuplauteds pci Structure - will all the info including a .fullText description

		string[] bits = Split(code, "_");


			//Don't mention the generation (I did once but I think I got away with it)

		 // ERROR: Not supported in C#: WithStatement


		//returns the populated structure

	}

	public Dictionary<string, clsTranslation> FormFactors(SqlClient.SqlConnection con)
	{

		FormFactors = new Dictionary<string, clsTranslation>(StringComparer.CurrentCultureIgnoreCase);

		object sql;
		sql = "SELECT DISTINCT instformfactor, a.Translation,a.code from " + server + "[iq].products.UNION_SysFamilyDefinitions sfd left join " + server + "[iq].dbo.Abbreviations a ON sfd.InstFormFactor=a.code order by instformfactor";

		//   sql = "select distinct sf.InstFormFactor as ff from IQ.products.Systems s join IQ.products.SysFamilyDefinitions sf on s.FamilyCode = sf.SysFamily"

		SqlClient.SqlDataReader rdr;
		rdr = da.DBExecuteReader(con, sql);
		int o = 0;

		clsTranslation tl;

		while (rdr.Read) {
			if (!IsDBNull(rdr.Item("instformfactor"))) {
				o += 10;
				if (IsDBNull(rdr.Item("translation"))) {
					tl = iq.AddTranslation(rdr.Item("instformfactor"), English, "FF", o, null, 0, true);
					//iq1 translations don't exsit for some of the codes (such as 'blade')
				} else {
					tl = iq.AddTranslation(rdr.Item("translation"), English, "FF", o, null, 0, true);
					//this creates a translation for each possible form factor and groups them under the group code FF
				}
				FormFactors.Add(rdr.Item("instformfactor"), tl);
			}
		}

		rdr.Close();

	}

	public Dictionary<string, clsTranslation> OptAbbreviations(SqlClient.SqlConnection Con, string columns)
	{

		//Imports dans abbreviations - creating groups (of translations) for some of the abbreviations which weren't previously grouped


		int nextKey = clsTranslation.NextKey;
		DataTable Tlwc = new DataTable();
		//transaltion Wcrite cache
		Tlwc = da.MakeWriteCacheFor(Con, "Translation");
		nextKey = 0;
		Tlwc = null;

		OptAbbreviations = new Dictionary<string, clsTranslation>(StringComparer.CurrentCultureIgnoreCase);

		//these are all the columns that contain abbreviations (they may contain a CD list, they may alos contain part numbers)
		object sql;
		SqlClient.SqlDataReader rdr;

		clsAttribute aa;

		foreach ( k in Split(columns, ",")) {
			//make an attribute for each column - ProductAttributes will be made later to carry the actual data
			if (!iq.i_attribute_code.ContainsKey(k)) {
				aa = new clsAttribute(k, iq.AddTranslation(k, English, "attrib", 0, Tlwc, nextKey, false), 0);
			}

			sql = "SELECT distinct " + k + " from " + server + "[iq].products.union_systems";
			//each row may be a cd list - so the DISTNCT reduces things - but we still need to check we havent already processed each one
			rdr = da.DBExecuteReader(Con, sql);

			List<string> uniqueCodes = new List<string>();
			while (rdr.Read) {
				if (!IsDBNull(rdr.Item(k))) {
					//split any/all comma seperated value in each column
					foreach ( c in Split(rdr.Item(k), ",")) {
						if (!uniqueCodes.Contains(UCase(c))) {
							uniqueCodes.Add(UCase(c));
							//uniqueCodes.Add(rdr.Item(k))
						}
					}
				}
			}
			//we now have a list of all the unique abbreviation codes  in this column 'k'  (with a few part numbers jumbled in perhaps)
			rdr.Close();

			sql = "SELECT CODE,TRANSLATION from " + server + "[iq].dbo.abbreviations where code IN ('" + Join(uniqueCodes.ToArray, "','") + "');";
			rdr = da.DBExecuteReader(Con, sql);
			while (rdr.Read) {
				if (!OptAbbreviations.ContainsKey(UCase(rdr.Item("code")))) {
					OptAbbreviations.Add(UCase(rdr.Item("code")), iq.AddTranslation(rdr.Item("translation"), English, "AT_" + k, 0, Tlwc, nextKey, false));
					uniqueCodes.Remove(UCase(rdr.Item("code")));
				}
			}

			//            For Each H In uniqueCodes
			// Debug.Print(H)
			// Next

			rdr.Close();


			//anything now left in unqiueCodes wasn't in the abbreviations table - so is either a part number of some of Pauls random junk - EG., French keyboard kit

			foreach ( leftover in uniqueCodes) {
				if (!OptAbbreviations.ContainsKey(leftover)) {
					OptAbbreviations.Add(UCase(leftover), iq.AddTranslation(leftover, English, "LO_" + k, 0, Tlwc, nextKey, false));
					// don't add part numbers here - (in the options and extra columns they shoudl become FOC preinstalled parts)
				}
			}

		}

		//   da.BulkWrite(Con, Tlwc, "Translation")
		Tlwc = null;
	}


	public void fixFamMinor()
	{
		object sql;
		object sever = "h3";

		sql = "SELECT familyPriStor,familySecStor,busunit,modelSKU,sysfamilyname,familycode,cpu,sfd.systype,h.ccDescription as [desc],Isnull([SupplyChainCode],'A') as [supplyChainCode],sfd.u, Activesites,sfd.instformfactor, ";
		//;Sql$ &= columns  'THIS FORMS THE BULK OF THE SPEC TABLE
		sql += "WLAN,WWAN,alsoHost";
		sql += ",productNote,vga,energystar,weightUnboxed,activeFromDate,activeToDate,active,eol,sfd.sysfamilyimg,aaOnly from " + server + "[iq].products.union_systems sys ";
		sql += "INNER join " + server + "[iq].products.union_sysfamilydefinitions sfd ON sfd.SysFamily=sys.FamilyCode ";
		sql += "INNER join " + server + "[iq].products.hierarchyiq h ON modelSKU=h.UPCNum ";
		sql += "WHERE (sYSFAMILY LIKE '%" + restrictImportToFamily + "%' or sysfamily = '' or sysfamily is null)";

		SqlClient.SqlConnection con = da.OpenDatabase;
		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql);

		while (rdr.Read) {
			object sku = rdr.Item("modelsku");
			if (iq.i_SKU.ContainsKey(sku)) {
				clsProduct system;
				system = iq.i_SKU(sku);
				if (system.i_Attributes_Code.ContainsKey("famMinor")) {
					clsProductAttribute fm = system.i_Attributes_Code("famMinor")(0);

					string fmc = fm.Translation.text(English);
					if (fmc == "")
						System.Diagnostics.Debugger.Break();


				} else {
					System.Diagnostics.Debugger.Break();
				}
			}

		}

		rdr.Close();
		con.Close();


	}


	//As Dictionary(Of String, clsBranch)
	public void Systems(SqlClient.SqlConnection con, Dictionary<string, clsBranch> dicsystems, Dictionary<string, clsBranch> dicfamily, Dictionary<string, string> dicPlcode, Dictionary<string, List<string>> containment, ref List<string> errormessages)
	{

		clsProductAttribute U;
		clsAttribute aa;
		if (!iq.i_attribute_code.ContainsKey("U")) {
			aa = new clsAttribute("U", iq.AddTranslation("U", English, "U", 0, null, 0, false), 0);
		}

		object pristorAtt;

		if (!iq.i_attribute_code.ContainsKey("PriStor")) {

			pristorAtt = new clsAttribute("PriStor", iq.AddTranslation("Primary Storage (import only)", English, "UI", 0, null, 0, false), 0);
		} else {
			pristorAtt = iq.i_attribute_code("PriStor");
		}

		object sctl = iq.AddTranslation("Supply Chain", English, "cats", 0, null, 0, false);

		//returns a dictionary of system branches by ModelSKU

		clsProduct Product;
		clsBranch sysBranch;
		//used to create systems (which go into the dictionaries)

		DataTable AttribWriteCache = new DataTable();
		AttribWriteCache = da.MakeWriteCacheFor(con, "ProductAttribute");

		DataTable QtyWritecache = da.MakeWriteCacheFor(con, "Quantity");


		//Dim dicFormFactors As Dictionary(Of String, clsTranslation) = FormFactors(con)

		SqlClient.SqlDataReader rdr;
		object sql;

		//small dictionary of supply chains to their translations keys - used to look up 
		//the supply chain branches (under the family branches) 

		//supply chains are obsoleted (before they ever really saw the light of day!)
		Dictionary<string, clsTranslation> dicChains = new Dictionary<string, clsTranslation>(StringComparer.CurrentCultureIgnoreCase);
		dicChains = new Dictionary<string, clsTranslation>();

		//hard coded - until someone can tell me where to find the full supply chain names/list
		dicChains.Add("A", iq.AddTranslation("Regular models", English, "SC", 10, null, 0, false));
		dicChains.Add("TV", iq.AddTranslation("Top value", English, "SC", 20, null, 0, false));
		dicChains.Add("SB", iq.AddTranslation("Smart buy", English, "SC", 30, null, 0, false));
		dicChains.Add("R", iq.AddTranslation("HP Renew", English, "SC", 30, null, 0, false));
		dicChains.Add("PR", iq.AddTranslation("Promotional", English, "SC", 30, null, 0, false));
		dicChains.Add("GO", iq.AddTranslation("Golden Offers", English, "SC", 30, null, 0, false));


		//the focus attributes are matched against the code (but theyr'e attributes - so they need trasnlations (until and unless we invent a text type for attributes!)
		Dictionary<string, clsTranslation> dicSC = new Dictionary<string, clsTranslation>(StringComparer.CurrentCultureIgnoreCase);
		dicSC = new Dictionary<string, clsTranslation>();

		dicSC.Add("A", iq.AddTranslation("A", English, "SCC", 10, null, 0, false));
		dicSC.Add("TV", iq.AddTranslation("TV", English, "SCC", 20, null, 0, false));
		dicSC.Add("SB", iq.AddTranslation("SB", English, "SCC", 30, null, 0, false));
		dicSC.Add("R", iq.AddTranslation("R", English, "SCC", 30, null, 0, false));
		dicSC.Add("PR", iq.AddTranslation("PR", English, "SCC", 30, null, 0, false));
		dicSC.Add("GO", iq.AddTranslation("GO", English, "SCC", 30, null, 0, false));


		Dictionary<string, clsTranslation> sysTypeToPortfolio = new Dictionary<string, clsTranslation>();

		//FYI
		//HP's Corporate hierarchy goes
		//Division (ESSN..
		//  BU (business unit) ISS/PSG/HPN/SWD
		//     Exhibit  'Desktops/Notebooks

		sysTypeToPortfolio.Add("DTO", iq.AddTranslation("PSG", English, "BU", 1, null, 0, false));
		sysTypeToPortfolio.Add("HPN", iq.AddTranslation("HPN", English, "BU", 1, null, 0, false));
		sysTypeToPortfolio.Add("IPG", iq.AddTranslation("IPG", English, "BU", 1, null, 0, false));
		sysTypeToPortfolio.Add("NBK", iq.AddTranslation("PSG", English, "BU", 1, null, 0, false));
		sysTypeToPortfolio.Add("SVR", iq.AddTranslation("ISS", English, "BU", 1, null, 0, false));
		sysTypeToPortfolio.Add("SWD", iq.AddTranslation("SWD", English, "BU", 1, null, 0, false));
		sysTypeToPortfolio.Add("PSG", iq.AddTranslation("PPS", English, "BU", 1, null, 0, false));

		//Create a dictionary of all the abbreviations referenced in any of these columns (of products.union_systems)
		//these are NOT the columns which contain only part numbers (RAM,discretegraphics, etc - handled in import.fios()
		//theyre the ones that may have abbreviations in

		string columns = "extras,options,software,warrantyCode,formFactor,mfrBuildCode,display,intVideo,ILOhardware,terStorTech,raidTech";

		//extras and options contain moslty abbreviations - but some part no's
		//software contains a CD list of abbreviations

		Dictionary<string, clsTranslation> optabbreviations;
		optabbreviations = Import.OptAbbreviations(con, columns);

		if (!optabbreviations.ContainsKey("TOWER")) {
			optabbreviations.Add("TOWER", iq.AddTranslation("Tower", English, "FF", 100, null, 0, false));
		}

		if (!optabbreviations.ContainsKey("BLADE")) {
			optabbreviations.Add("BLADE", iq.AddTranslation("Blade", English, "FF", 90, null, 0, false));
		}

		columns = Replace(columns, "ILOhardware", "Sys.ILOhardware");
		//the column nane is ambiguous otherise (this isn't pretty - but it's only an import)

		makeSpecAttributes("vga^has onboard VGA,eStar^Energy Star Compliant,mass^Weight unboxed,note^Product Note,WLAN^Wireless LAN,WWAN^3G/CellularConnectivity,displaySize^Display Size (diagonal)");

		int nextkey = clsTranslation.NextKey();
		DataTable Tlwc = new DataTable();
		Tlwc = da.MakeWriteCacheFor(con, "Translation");

		sql = "SELECT familyPriStor,familySecStor,busunit,modelSKU,sysfamilyname,familycode,cpu,sfd.systype,h.ccDescription as [desc],Isnull([SupplyChainCode],'A') as [supplyChainCode],sfd.u, Activesites,sfd.instformfactor, ";
		sql += columns;
		//THIS FORMS THE BULK OF THE SPEC TABLE
		sql += ",WLAN,WWAN,alsoHost";
		sql += ",productNote,vga,energystar,weightUnboxed,activeFromDate,activeToDate,active,eol,sfd.sysfamilyimg,aaOnly from " + server + "[iq].products.union_systems sys ";
		sql += "INNER join " + server + "[iq].products.union_sysfamilydefinitions sfd ON sfd.SysFamily=sys.FamilyCode ";
		sql += "INNER join " + server + "[iq].products.hierarchyiq h ON modelSKU=h.UPCNum ";
		sql += "WHERE (sYSFAMILY LIKE '%" + restrictImportToFamily + "%' or sysfamily = '' or sysfamily is null)";

		columns = Replace(columns, "Sys.ILOhardware", "ILOhardware");
		//put it back so we can pull out this column later
		rdr = da.DBExecuteReader(con, sql);

		clsProductAttribute SystemName;
		clsProductAttribute FamMajor;
		clsProductAttribute FamMinor;
		clsProductAttribute FamDisp;

		clsProductAttribute cpuSKU;
		clsProductAttribute mfrSKU;
		clsProductAttribute PLcode;

		clsSector sector;
		clsTranslation sysTrans = iq.AddTranslation("systems", English, "collect", 0, Tlwc, nextkey, false);
		clsTranslation sysTransSingular = iq.AddTranslation("system", English, "collect", 0, Tlwc, nextkey, false);

		clsTranslation optTrans = iq.AddTranslation("options", English, "collect", 0, Tlwc, nextkey, false);
		clsTranslation optTransSingular = iq.AddTranslation("option", English, "collect", 0, Tlwc, nextkey, false);
		clsUnit textUnit = iq.i_unit_code("txt");

		clsAttribute att = null;

		clsTranslation tlyes = iq.AddTranslation("Yes", English, "hasFeature", 0, Tlwc, nextkey, false);

		while (rdr.Read) {
			// do not import systems begging with X they are 'fake'
			if (LCase(Left(rdr.Item("ModelSKU"), 1)) != "x") {

				if (!dicsystems.ContainsKey(Trim(rdr.Item("ModelSku")))) {
					sector = iq.i_sector_code("HP" + rdr.Item("busunit"));

					System.DateTime activeTo = (System.DateTime)"31/12/2100";
					if (!IsDBNull(rdr.Item("activeToDate")))
						activeTo = rdr.Item("activetodate");

					bool publish = true;
					if (rdr.Item("AAonly") != 0) {
						publish = false;
					}

					Product = new clsProduct(rdr.Item("modelsku"), true, false, sector, iq.i_ProductType_Code(rdr.Item("systype")), rdr.Item("activefromdate"), activeTo, rdr.Item("active"), rdr.Item("eol"), publish,
					"", "", "");
					//this IS a system 

					//Make a focus attibute based on the system type (lightly translated to the portfolio)
					//ISS PSG SWD
					clsProductAttribute FA = new clsProductAttribute(Product, iq.i_attribute_code("FOCUS"), 0, textUnit, sysTypeToPortfolio(rdr.Item("systype")));

					string scc = rdr.Item("supplyChainCode");
					if (dicChains.ContainsKey(scc)) {
						clsProductAttribute sc = new clsProductAttribute(Product, iq.i_attribute_code("SC"), 0, iq.i_unit_code("txt"), dicChains(scc), AttribWriteCache);
						clsProductAttribute SCFA = new clsProductAttribute(Product, iq.i_attribute_code("FOCUS"), 0, textUnit, dicSC(scc));
					} else {
						Beep();
					}


					if (!IsDBNull(rdr.Item("U"))) {
						//U = New clsProductAttribute(Product, iq.i_attribute_code("U"), rdr.Item("U"), iq.i_unit_code("U"),  iq.AddTranslation(rdr.Item("U") & " U", English, "U"), AttribWriteCache)
						U = new clsProductAttribute(Product, iq.i_attribute_code("U"), rdr.Item("U"), iq.i_unit_code("U"), null, AttribWriteCache);
					}

					if (!IsDBNull(rdr.Item("productNote"))) {
						if (Trim(rdr.Item("productnote")) != "") {
							clsProductAttribute note = new clsProductAttribute(Product, iq.i_attribute_code("note"), 0, textUnit, iq.AddTranslation(rdr.Item("productNote"), English, "ProdNote", 0, Tlwc, nextkey, true), AttribWriteCache);
						}
					}

					if (!IsDBNull(rdr.Item("EnergyStar"))) {
						if ((int)rdr.Item("energystar") > 0) {
							clsProductAttribute es = new clsProductAttribute(Product, iq.i_attribute_code("eStar"), 1, textUnit, tlyes, AttribWriteCache);
						}
					}

					if (!IsDBNull(rdr.Item("WLAN"))) {
						clsProductAttribute wl = new clsProductAttribute(Product, iq.i_attribute_code("WLAN"), 1, textUnit, tlyes, AttribWriteCache);
					}

					if (!IsDBNull(rdr.Item("WWAN"))) {
						clsProductAttribute ww = new clsProductAttribute(Product, iq.i_attribute_code("WWAN"), 1, textUnit, tlyes, AttribWriteCache);
					}


					if (!IsDBNull(rdr.Item("vga"))) {
						clsProductAttribute note = new clsProductAttribute(Product, iq.i_attribute_code("vga"), 1, textUnit, tlyes, AttribWriteCache);
					}


					//will need to do the same for the secondary storage/optical drives
					if (!IsDBNull(rdr.Item("FamilyPriStor"))) {
						//optfamily translation  -- This is a code like NHP355SFF
						clsTranslation oftl = iq.AddTranslation(rdr.Item("familypristor"), English, "", 0, Tlwc, nextkey, false);
						clsProductAttribute pristor = new clsProductAttribute(Product, pristorAtt, 0, textUnit, oftl, AttribWriteCache);
					}


					//same as formfactor

					//If Not IsDBNull(rdr.Item("instFormFactor")) Then
					// Dim FormF As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("formFactor"), 1, textUnit, iq.AddTranslation(rdr.Item("instFormFactor"), English, "FF"), AttribWriteCache)
					// End If


					if (!IsDBNull(rdr.Item("weightUnboxed"))) {
						//21kg&nbsp;&nbsp;(46.30lb)
						//take the --- text out and use the conversions

						object wu = rdr.Item("weightUnboxed");
						[] p = Split(wu, "kg");
						if (UBound(p) != 1)
							System.Diagnostics.Debugger.Break();
						float kg = Val(p(0));
						//Dim tl As clsTranslation = iq.AddTranslation(wu$, English, "WU", 0, Nothing, True)
						//     Dim mass As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("mass"), kg, textUnit, tl, AttribWriteCache)
						clsProductAttribute mass = new clsProductAttribute(Product, iq.i_attribute_code("mass"), kg, iq.i_unit_code("kg"), null, AttribWriteCache);

					}

					//MAKE THE MAJOR SPEC TABLE ATTRIBUTES - preinstalled options are in import.FIOs()
					//Make an attribute for every abbreviation referenced in the various COLUMNS of products.union_systems
					clsProductAttribute pa;
					clsTranslation abtl;
					//abbreviation translation

					foreach ( k in Split(columns, ",")) {
						// If k = "formFactor" Then Stop
						if (!IsDBNull(rdr.Item(k))) {
							//some of the columns (notably options and extras) contain CD lists

							float nv = -1;
							if (k == "display") {
								if (InStr(rdr.Item(k), "_")) {
									string[] p = Split(rdr.Item(k), "_");
									string res = p(3);
									if (res == "LED")
										res = p(4);
									string[] dm = Split(res, "x");
									nv = Val(dm(0)) * Val(dm(1));
									//find the number of pixels
									if (nv == 0)
										System.Diagnostics.Debugger.Break();
									//we create the productattribute a little later

									if (!iq.i_attribute_code.ContainsKey("displaySize")) {
										clsAttribute ds = new clsAttribute("displaySize", iq.AddTranslation("Display Size (diagonal)", English, "DispSZ", 0, Tlwc, nextkey, false), 0);
									}

									//DIS_15.6_WXGA_1366x768_AGBV
									pa = new clsProductAttribute(Product, iq.i_attribute_code("displaySize"), p(1), iq.i_unit_code("Inch"), null, AttribWriteCache);

									//If InStr(p(1)(3)),"x" then do something here for megapixels

								}
							}

							if (k == "extras" | k == "options" | k == "software" | k == "raidTech") {
								foreach ( ik in Split(rdr.Item(k), ",")) {
									//for each of the CD values ad an attribute of the type of the value
									//abtl = optabbreviations(UCase(k)) 'abbreviations translation of MCR,CAM,SDR,BT etc
									if (!iq.i_attribute_code.ContainsKey(Left(ik, 20))) {
										//we don't have an MCR, CAM, SDR *attribute* yet - so make one
										if (!optabbreviations.ContainsKey(UCase(ik))) {
											//well it wasn't in abbreviations - so it's *probably* a part number.. or maybe something like "keyboard kit"
											// Stop
											//append it to the "additional" attribute
											att = null;
										} else {
											if (LCase(ik) == "name")
												System.Diagnostics.Debugger.Break();
											att = new clsAttribute(Left(ik, 20), optabbreviations(UCase(ik)), 0);
											//an MCR,CAM,SDR (or some other recogised abbreviation)
										}
									}

									if (!att == null) {
										att = iq.i_attribute_code(Left(ik, 20));
										//                                                                                      yes
										if (!Product.i_Attributes_Code.ContainsKey(att.Code)) {
											pa = new clsProductAttribute(Product, att, 1, textUnit, null, AttribWriteCache);
										} else {
											Product.i_Attributes_Code(att.Code)(0).NumericValue += 1;
											Product.i_Attributes_Code(att.Code)(0).update(errormessages);
											Logit("duplicate " + k + ":" + ik);
										}

									}

								}
							} else {
								//add an attribute of the type of the column header (e.g. warrantyCode,formFactor,mfrBuildCode,display,intVideo,ILOhardware,terStorTech,raidTech"
								//'  If InStr(rdr.Item(k), ",") Then Stop
								if (LCase(rdr.Item(k)) == "name")
									System.Diagnostics.Debugger.Break();
								if (LCase(k) == "name")
									System.Diagnostics.Debugger.Break();
								if (optabbreviations.ContainsKey(UCase(rdr.Item(k)))) {
									abtl = optabbreviations(UCase(rdr.Item(k)));
									//the translation of theis abbreviation will (should) alrery exist .. eg."WTY111NBD" = [[IQ.clsLanguage, 1 Year Parts / 1 Year Labour / 1 Year Onsite Warranty Next Business Day]
									pa = new clsProductAttribute(Product, iq.i_attribute_code(k), nv, textUnit, abtl, AttribWriteCache);
								} else {
									//Something for which there was no IQ1 abbreviation like 'french keyboard' or EMA7029 
									//    Beep()
								}
							}
						}
					}

					//This is done in import descriptions
					//desc = New clsProductAttribute(Product, iq.Attributes("desc"), 0, iq.Units("txt"), iq.addTranslation(Trim$(rdr.Item("desc"))).Key, AttribWriteCache)

					object sku;
					sku = Trim(rdr.Item("modelsku"));
					if (InStr(LCase(sku), "paul"))
						System.Diagnostics.Debugger.Break();
					if (sku == "")
						System.Diagnostics.Debugger.Break();

					//    mfrSKU = New clsProductAttribute(Product, iq.i_attribute_code("MfrSKU"), 0, textUnit, iq.AddTranslation(sku$, English, "SKU", 0, Tlwc, nextkey, False), AttribWriteCache)

					if (!dicPlcode.ContainsKey(sku)) {
						Logit("Can't locate PLCode for system '" + sku + "'");
					} else {
						object pl;
						pl = dicPlcode(sku);
						PLcode = new clsProductAttribute(Product, iq.i_attribute_code("PLcode"), 0, textUnit, iq.AddTranslation(pl, English, "PL", 0, Tlwc, nextkey, false), AttribWriteCache);
					}

					//for systems - their 'name' *is* their part number
					// SystemName = New clsProductAttribute(Product, iq.i_attribute_code("~ame"), 0, textUnit, mfrSKU.Translation, AttribWriteCache)

					// If InStr(LCase(SystemName.displayName(English)), "paul") Then Stop
					//SystemName = New clsProductAttribute(Product, iq.Attributes("~ame"), 0, iq.Units("txt"), iq.AddText(rdr.Item("familycode"), s_lang, TranslationWriteCache).Key, AttribWriteCache)

					//product attributes are a list of each type.. so we can have multiple alsohosts and don't need a horrid comma separated list)
					clsProductAttribute alsoHost;
					if (!IsDBNull(rdr.Item("alsohost"))) {
						foreach ( h in Split(rdr.Item("alsoHost"), ",")) {
							alsoHost = new clsProductAttribute(Product, iq.i_attribute_code("alsoHost"), 0, textUnit, iq.AddTranslation(rdr.Item("alsoHost"), English, "", 0, Tlwc, nextkey, false), AttribWriteCache);
						}
					}

					object fn = rdr.Item("sysFamilyname");

					//DO NOT unabreviate it here !!
					//If dicAbbreviations.ContainsKey(fn$) Then fn$ = dicAbbreviations(fn$)
					//NOTE - the translations of the family name won't be duplicated - so, although every system will have a family attribute - all those attributes to a s set of a hundred or so tranlsations
					FamMajor = new clsProductAttribute(Product, iq.i_attribute_code("FamMajor"), 0, textUnit, iq.AddTranslation(fn, English, "FamMajor", 0, Tlwc, nextkey, false), AttribWriteCache);

					if (dicAbbreviations.ContainsKey(fn))
						fn = dicAbbreviations(fn);
					FamDisp = new clsProductAttribute(Product, iq.i_attribute_code("FamDisp"), 0, textUnit, iq.AddTranslation(fn, English, "FamDisp", 0, Tlwc, nextkey, false), AttribWriteCache);


					//Family Minor -- (Familycode - granular)
					clsTranslation tl;

					if (Trim(rdr.Item("familycode")) == "")
						System.Diagnostics.Debugger.Break();
					tl = iq.AddTranslation(Trim(rdr.Item("familycode")), English, "FamMinor", 0, Tlwc, nextkey, false);
					FamMinor = new clsProductAttribute(Product, iq.i_attribute_code("FamMinor"), 0, textUnit, tl, AttribWriteCache);

					if (!object.ReferenceEquals(rdr.Item("cpu"), DBNull.Value)) {
						cpuSKU = new clsProductAttribute(Product, iq.i_attribute_code("cpuSKU"), 0, textUnit, iq.AddTranslation(Trim(rdr.Item("cpu")), English, "CPUSKU", 0, Tlwc, nextkey, false), AttribWriteCache);
					}

					string fcode;
					clsBranch famBranch;

					fcode = Trim(rdr.Item("sysfamilyname"));
					//family dictionary contains familycode>branch 
					if (dicfamily.ContainsKey(fcode)) {

						famBranch = dicfamily(fcode);


						//If dicChains.ContainsKey(sc$) Then 'There are some PR supply chains

						//        Dim scbranch As clsBranch = famBranch.ChildNamed(dicChains(sc$)) 'supply chain (top value/regular) - contains systems

						// If scbranch Is Nothing Then
						//the 'regular' suuply chain is less impotant (comes after) any promo supply chain TV/SB
						// scbranch = New clsBranch(Nothing, famBranch, dicChains(sc$), "", sysTrans, sysTransSingular, Nothing, IIf(sc$ = "A", 100, 10), False, "B")
						// End If

						//creates a new branch and adds it as a child of the SUPPLY CHAIN under the family 

						//aBranch = New clsBranch(Product, dicfamily(fcode), product.i_attributes_code("~ame").TextKey, "")
						//    aBranch = New clsBranch(Product, scbranch, Product.i_Attributes_Code("~ame").Translation, "", optTrans, optTransSingular, Nothing) 'these ARE the systems (so we use the opt key - becuase they *contain* options)

						//   If InStr(LCase(SystemName.Translation.text(English)), "paul") Then Stop

						object picture;
						picture = famBranch.Picture;
						if (!IsDBNull(rdr.Item("sysfamilyimg"))) {
							picture = rdr.Item("sysfamilyimg");
							//picture = Split(picture, "_")(1)
						}

						//aBranch = New clsBranch(Product, scbranch, SystemName.Translation, picture, optTrans, optTransSingular, Nothing, scbranch.childBranches.Count * 10, False, "T") 'these ARE the systems (so we use the opt key - becuase they *contain* options)
						//  Dim SKU As clsTranslation = iq.AddTranslation(rdr.Item("MoDELSKU"),English,"sysSKUs",0,tlwc
						sysBranch = new clsBranch(Product, famBranch, mfrSKU.Translation, picture, optTrans, optTransSingular, null, famBranch.childBranches.Count * 10, false, "T");
						//these ARE the systems (so we use the opt key - becuase they *contain* options)
						dicsystems.Add(Trim(rdr.Item("ModelSKU")), sysBranch);

						//make the quantity records the make the system visible by region/country - these are the gobal/pathless ones

						string rgns = "";
						if (!IsDBNull(rdr.Item("activesites")))
							rgns = rdr.Item("activesites");
						if (rdr.Item("aaonly") != 0) {
							rgns += ",AA";
						}


						//there are a few 'junk' systems with no activesites
						if (rgns == "") {
							clsQuantity qty;
							//EXCLUDE this system eveywhere (with a min increment of 0) - 
							qty = new clsQuantity(r_worldwide, "", sysBranch, 0, 0, 0, 0, QtyWritecache);
						//Public Sub New(region As clsRegion, ByVal Path As String, ByVal branch As clsBranch, ByVal numPreInstalled As Integer, ByVal MinIncrement As Integer, ByVal PreferredIncrement As Integer, freeOfCharge As Boolean, Optional Writecache As DataTable = Nothing)
						} else {
							MakeSystemQuantities(sysBranch, rgns, containment, QtyWritecache);
						}
					} else {
						System.Diagnostics.Debugger.Break();
					}

				}
			}


		}
		rdr.Close();
		da.BulkWrite(con, QtyWritecache, "Quantity");
		QtyWritecache = null;

		//write the accumulated product attributes (bulk copy)
		da.BulkWrite(con, AttribWriteCache, "ProductAttribute");
		AttribWriteCache = null;

		da.BulkWrite(con, Tlwc, "Translation");
		Tlwc = null;


	}

	public void makeSpecAttributes(att)
	{
		//Accepts a comma seperated list of ^ delimited code^name pairs - for which to make attributes (if they don't exist)
		//prefixes each attribute with 

		string[] p;
		clsAttribute anAttribute;

		foreach ( a in Split(att, ",")) {
			p = Split(a, "^");
			if (!iq.i_attribute_code.ContainsKey(p(0))) {
				anAttribute = new clsAttribute(p(0), iq.AddTranslation(p(1), English, "SpecAtts", 0, null, 0, false), 0);
			}
		}

	}

	public void MakeSystemQuantities(clsBranch branch, string regionList, Dictionary<string, List<string>> containment, DataTable qtyWriteCache)
	{
		//RegionList contains a comma seperated list of region and country codes (now all regions) - from the ActiveSties column of dbo.systems

		regionList = Replace(regionList, ".", ",");
		//Fix some minor issues in the source data - some .'s that should be commas
		regionList = Replace(regionList, " ", "");
		// and some spaces - that shouldn't be there

		List<string> rl = Split(regionList, ",").ToList;

		clsRegion RGN;

		if (!rl.Contains("XW")) {
			List<string> codelist = cleanRegions(rl, containment);

			clsQuantity aQuantity;

			foreach ( code in codelist) {
				if (code != "") {
					if (code == "UK")
						code = "GB";
					//If code = "AA" Then Stop
					if (!iq.i_region_code.ContainsKey(Trim(code))) {
						object sku = branch.SKU;
						Logit(code + " is not a valid region for system " + sku);
					} else {
						RGN = iq.i_region_code(Trim(code));

						string ck = branch.ID + "^" + RGN.ID + "^";
						if (!Import.i_Quantities.Contains(ck)) {
							aQuantity = new clsQuantity(RGN, "", branch, 0, 1, 1, 0, qtyWriteCache);
							Import.i_Quantities.Add(ck);
						} else {
							NoOp();
						}
					}
				}
			}
		}


	}

	public string autoadds(SqlClient.SqlConnection con, Dictionary<string, clsProduct> dicAutoadds, Dictionary<string, clsBranch> dicSystems, Dictionary<string, clsRegion> dicRegions)
	{

		DataTable QuantityWriteCache;
		QuantityWriteCache = da.MakeWriteCacheFor(con, "Quantity");

		SqlClient.SqlDataReader rdr;
		object sql;

		int added;

		Logit("Importing autoadds", true, true);

		// sql$ = "SELECT [CountryCode],[ModelSKU],[AddSKU],[OptType] from " & server$ & "[iq].[Products].[AutoAdds] order by modelsku,addsku,countrycode"  'If we were clever we could collpase autoadds by region


		sql = "SELECT [CountryCode],a.[ModelSKU],[AddSKU],[OptType],s.FamilyCode,a.ranking from " + server + "[iq].[Products].[AutoAdds] a ";
		sql += "JOIN " + server + "iq.products.Systems s on a.modelsku=s.ModelSKU ";
		sql += "WHERE (familycode LIKE '%" + restrictImportToFamily + "%' or familycode = '' or familycode is null )";
		sql += "ORDER by s.familycode,modelsku,addsku,countrycode ";

		object path;
		clsBranch sysBranch;
		clsBranch optionbranch;
		object optionpath = "";
		object optsku = "";
		object syssku;
		object ck;

		int missed = 0;

		string sysfam = "";
		string osysfam = "";

		int ranking = 0;

		Dictionary<string, List<string>> optPaths = new Dictionary<string, List<string>>(StringComparer.CurrentCultureIgnoreCase);

		StreamWriter sw = new StreamWriter("c:\\temp\\autoAdds.txt");

		int dudopts = 0;
		int there = 0;
		int inactive = 0;
		List<string> adds = new List<string>();



		rdr = da.DBExecuteReader(con, sql);

		while ((rdr.Read)) {
			//compound key (used to check if we've already imported)
			ck = rdr.Item("countrycode") + "^" + rdr.Item("ModelSku") + "^" + rdr.Item("addSku");

			sw.WriteLine(ck);

			syssku = rdr.Item("modelsku");
			sysfam = rdr.Item("familycode");
			ranking = rdr.Item("ranking");

			//'switch between OR True and OR False to restirct what's imported
			if ((Left(sysfam, restrictImportToFamily.Length).ToUpper == restrictImportToFamily.ToUpper) | string.IsNullOrEmpty(restrictImportToFamily)) {
				//                If ck$ = "UK^704560-421^U2GC1E" Then Stop
				if (dicAutoadds.ContainsKey(ck)) {
					Logit("Already there:" + ck);
					there += 1;

				} else {
					if (!dicSystems.ContainsKey(syssku)) {
						//TODO Reinstate anerror = New clsEvent(parentEvent, "AutoAdd " & ck$ & "system " & syssku & " is not recognised", ev_Warning)
						Logit("Auto add for sku " + ck + " is not vaild (system may be inactive");
						inactive += 1;
					//Beep()

					} else {
						// Only autoadd if ranking is 1 otherwise it is a top recomendation 
						if (ranking == 1) {
							optsku = rdr.Item("addsku");


							//  If optsku = "UK066E" Then Stop '

							if (!iq.i_SKU.ContainsKey(optsku)) {
								//TODO  reinstate        anerror = New clsEvent(parentEvent, "Autoadd option sku " & optsku & " not recognised", ev_Warning)
								Logit("Autoadd option sku " + optsku + " not recognised");
								dudopts += 1;
							} else {
								//Comment this line if you want warrent in autoadds.
								if (iq.i_SKU(optsku).ProductType.Code.ToLower != "wty") {
									sysBranch = dicSystems(syssku);
									object syspath = "tree." + Trim(iq.RootBranch.ID);
									//root
									syspath += "." + Trim(sysBranch.Parent.Parent.ID);
									//System type
									syspath += "." + Trim(sysBranch.Parent.ID);
									//Family
									syspath += "." + Trim(sysBranch.ID);

									//when the system subfamily changes - re-populate sysbanch.skupaths
									if (sysfam != osysfam) {
										osysfam = sysfam;
										optPaths.Clear();
										//Important !! (or they'd just build up in here!)
										sysBranch.SkuPaths(optPaths, "", true);
									}

									if (optPaths.ContainsKey(optsku)) {
										//this options appears more than once under the system
										if (optPaths(optsku).Count > 1) {
											//Need to find the non TRO one...
											foreach ( ob in optPaths(optsku)) {
												if (iq.Branches(Split(ob, ".").Last) != null && iq.Branches(Split(ob, ".").Last).Parent != null && iq.Branches(Split(ob, ".").Last).Parent.Parent != null && !iq.Branches(Split(ob, ".").Last).Parent.Parent.Translation.text(English) == "Top Recommended") {
													optionbranch = iq.Branches(Split(ob, ".").Last);
													optionpath = syspath + ob;
													break; // TODO: might not be correct. Was : Exit For
												}
											}
										} else {
											optionbranch = iq.Branches(Split(optPaths(optsku)(0), ".").Last);
											optionpath = syspath + optPaths(optsku)(0);
										}
									} else {
										optionbranch = null;
									}

									//optionbranch = sysBranch.findChildBySKU2(path$, optsku, optionpath$) 'staring at this branch/path - recurse down until you find the sku - returns branch and its address 
									if (optionbranch == null) {
										Logit("Could not locate autoadd option " + optsku + " under system " + syssku);
										missed += 1;
									} else {
										//If optionpath$ = "" Then Stop
										object ffff = PathName(optionpath);
										//If ck$ = "UK^704560-421^U2GC1E" Then Stop
										object found = false;
										foreach ( q in optionbranch.Quantities.Values) {
											if (q.NumPreInstalled > 0 & q.Path == optionpath)
												found = true;
										}
										if (!found) {
											makeAutoAdd(optionbranch, rdr.Item("countrycode"), optionpath, QuantityWriteCache);
											dicAutoadds.Add(ck, optionbranch.Product);
											adds.Add(ck + " " + optionbranch.Product.DisplayName(English));

											Logit("Added " + ck + optionbranch.Product.DisplayName(English) + " " + optionbranch.Product.ProductType.Code);
											added += 1;
										}
									}
								}
							}
						} else {
							//Top Recomended option code goes here 
						}
					}
				}
			}
		}



		rdr.Close();
		sw.Close();

		da.BulkWrite(con, QuantityWriteCache, "Quantity");
		QuantityWriteCache = null;

		Logit("added:" + added + " missed:" + missed + " dudopts:" + dudopts + " There:" + there + " inactive: " + inactive);
		Logit("completed autoadds", false, true);




	}

	//Private Function makeAutoAdd(branch As clsBranch, skuvariant As clsVariant, countryCodes As String, path As String, writecache As DataTable)

	private void makeAutoAdd(clsBranch branch, string countryCodes, string path, DataTable writecache)
	{
		clsQuantity aQuantity = null;

		List<string> cclist = Split(countryCodes, ",").ToList;

		foreach ( ccode in cclist) {
			if (ccode == "UK") {
				ccode = "GB";

			}
			if (iq.i_region_code.ContainsKey(ccode)) {
				clsRegion rgn = iq.i_region_code(ccode);
				//aQuantity = New clsQuantity(iq.i_region_code(ccode), path$, branch, skuvariant, 1, 1, 1, 0, writecache)

				object ck = branch.ID + "^" + rgn.ID + "^" + path;
				if (i_Quantities.Contains(ck)) {
					NoOp();
				} else {
					aQuantity = new clsQuantity(iq.i_region_code(ccode), path, branch, 1, 1, 1, 0, writecache);
					i_Quantities.Add(ck);
				}


			} else {
				System.Diagnostics.Debugger.Break();
				Logit("country " + ccode + " is not in the imported dictionary");
			}
		}

	}



	public void Margins(SqlClient.SqlConnection con, Dictionary<string, clsBranch> dicSystems, Dictionary<string, clsProduct> optionsbysku, Dictionary<string, clsChannel> dicChannels)
	{
		SqlClient.SqlDataReader rdr;
		decimal price;
		int pass = 1;

		List<clsMargin> badmargins;
		badmargins = new List<clsMargin>();


		for (pass = 1; pass <= 2; pass++) {
			//fix rougue account numbers created by wescoast logins to the 'wrong' portal
			da.DBExecutesql("update h3.channelcentral.customers.users set priceBand=null where ChanID='DWERG74AH'");

			object sql;
			sql = "SELECT h.hostid as seller,ha.currency,u.chanid as buyer,mfrpartnum AS partno,hostmfrpartnum,internalprice AS iprice,ha.priceBand,hostPartNum,externalprice as price ";
			sql += "FROM " + server + "[iq].products.pricelistmaster h ";
			sql += "JOIN " + server + "[channelcentral].customers.users u ON u.priceBand=h.priceBand  ";
			sql += "JOIN " + server + "[channelcentral].customers.hostaccounts ha ON h.priceBand=ha.priceBand ";
			sql += "WHERE ha.priceBand is not null AND currency<>'nul' and internalprice is not null ";
			sql += "GROUP BY h.hostid,chanid,mfrpartnum,hostmfrpartnum,internalprice,curr,currency,ha.priceBand,hostpartnum,externalprice ";
			sql += "ORDER BY partno,seller,buyer";

			rdr = da.DBExecuteReader(con, sql);

			clsProduct part = null;
			//For each product - each seller offers prices to each buyer in each currency
			//Dim prices As Dictionary(Of clsChannel, Dictionary(Of clsChannel, Dictionary(Of String, Single)))

			string partno;
			string oPartno = "";

			clsChannel BuyerChannel;
			clsChannel SellerChannel;
			clsCurrency Currency;
			int partnos;

			int pricesrows = 0;

			int bad = 0;
			int good = 0;
			int nobase = 0;
			int zerobase = 0;

			Dictionary<clsChannel, int> dicbad = new Dictionary<clsChannel, int>();
			dicbad = new Dictionary<clsChannel, int>();

			while (rdr.Read) {
				partno = Trim(rdr.Item("partno"));
				if (partno != oPartno) {
					partnos += 1;
					if (dicSystems.ContainsKey(partno)) {
						part = dicSystems(partno).Product;
					} else {
						if (optionsbysku.ContainsKey(partno)) {
							part = optionsbysku(partno);
						} else {
							part = null;
						}
					}
				}

				clsVariant SKUvariant;

				int dupes = 0;

				if (part == null) {
					Logit("Invalid SKU (mfrPartno) '" + partno + "' whilst importing pricelistmaster");
				} else {
					if (!dicChannels.ContainsKey(Trim(rdr.Item("seller")))) {
						Logit("Couldn't locate the seller channel for '" + rdr.Item("seller") + "' (pricelistmaster.hostID) - check CaSe and trailing     spaces.");
					} else {
						SellerChannel = dicChannels(Trim(rdr.Item("seller")));
						if (!dicChannels.ContainsKey(Trim(rdr.Item("buyer")))) {
							Logit("Couldn't locate the buyer channel for '" + rdr.Item("buyer") + "' (channelcentral.customers.users.[chanid] - check CaSe and trailing     spaces.");
						} else {
							if (Trim(rdr.Item("Buyer")) == "RTERG74AH") {
								Logit("Skipping test account " + rdr.Item("buyer"));

							} else {
								BuyerChannel = dicChannels(Trim(rdr.Item("buyer")));
								Currency = iq.i_currency_code(Trim(rdr.Item("currency")));

								//See if the host partnumber has a # in, determine the variant
								string hostpartnum;
								hostpartnum = rdr.Item("Hostmfrpartnum");
								int ih = InStr(hostpartnum, "#");

								//      Dim newvariant As clsVariant
								//      Dim distiSku As String
								//      If IsDBNull(rdr.Item("hostpartnum")) Then distiSku = rdr.Item("mfrpartnum") Else distiSku = rdr.Item("hostpartnum")

								// If distiSku = "" Then Stop
								// newvariant = New clsVariant("", distiSku, "", "", "", "")

								//see if we already have a price for this part - for this seller/currency/variant combo
								//we may well do as PricelistMaster contains rows for many buyers

								if (Trim(SellerChannel.Code) == "DWERG74AH" & Currency.Code == "EUR") {
									Logit(partno + " was quoted in euros by Westcoast (not WestCoast Ireland!) - skipping");

								} else {
									if (!SellerChannel.Margin.ContainsKey(BuyerChannel)) {
										SellerChannel.Margin.Add(BuyerChannel, new Dictionary<clsSector, clsMargin>());
									}

									clsProductType producttype = part.ProductType;
									string factor;
									NullablePrice basePrice;

									if (part.i_Variants != null) {

										if (!part.i_Variants.ContainsKey(SellerChannel)) {
											//this part is not sold by this challel
											Logit(part.SKU + " is not sold by " + SellerChannel.Code);


										} else {
											SKUvariant = part.i_Variants(SellerChannel)(0);
											basePrice = SKUvariant.BasePrice(Currency);

											if (basePrice.NumericValue == 0) {
												//Logit("basePrice price for " & partno & " (" & SellerChannel.Name & ") was 0")
												zerobase += 1;

											} else {
												if (IsDBNull(basePrice.value)) {
													Logit("No base price defined for " + partno);
													nobase += 1;

												} else {
													if (object.ReferenceEquals(rdr.Item("price"), DBNull.Value)) {
													//Null external price

													} else {
														price = rdr.Item("price");
														factor = price / basePrice.NumericValue;

														//                              buyer                   sector                   product type    margin
														//Public Margin As Dictionary(Of clsChannel, Dictionary(Of clsSector, Dictionary(Of clsProductType, clsMargin)))

														clsMargin amargin;

															//work with this sellers margins for this buyer 
															//If Not .ContainsKey(part.Sector) Then
															//    SellerChannel.Margin(BuyerChannel).Add(clsSector,ector, clsMargin))
															//End If
															//create a new margin (which adds it to the sellers(buyers) dictionary - and inserts it in the database)


															//we know there are some prices inconsistent with the margin
															// make a specific price (for every product in sector any sector with bad margins)

															//'@@NObbled - TODO                                           aprice = New clsPrice(part, SKUvariant, SellerChannel, BuyerChannel, New nullablePrice(rdr.Item("iprice"), Currency), "Specific price")



															//oh dear - we have conflicting margins on products of the same ProductType - Within one sector




															//yep - that's fine (margins match)
															//Beep()


														 // ERROR: Not supported in C#: WithStatement

													}
												}
											}
										}
									}

								}
							}
						}
					}
				}

				oPartno = partno;
				pricesrows += 1;
			}

			Logit(bad + " SKUs had inconsistent margins, " + nobase + "SKUs had no base price defined," + zerobase + " base prices were zero");
			//Logit("Imported " & good & " margins for " & partnos & " distinct SKUs in " & TimeSince(lastmilestone), False, True)
			rdr.Close();

		}

	}



	public void Stock(SqlClient.SqlConnection con, Dictionary<string, clsstock> dicStock, Dictionary<string, clsBranch> dicSystems, Dictionary<string, clsProduct> dicOptions, Dictionary<string, clsChannel> dicChannels)
	{
		//Pna stock only contains 1 row per HostID/Sku ... with a maximum of one future shipment
		object sql;
		//   sql$ = "SELECT rowID,hostID,hostmfrpartnum,mfrpartnum,stock,ts,duedate,dueqty from " & server$ & "[iq].products.PNA_Stock"

		con.Close();
		con = da.OpenDatabase();

		sql = "SELECT hostid,hostmfrpartnum,mfrpartnum,hostsku,Stock,DueDate,dueqty,rowid from " + server + "[iq].products.PNA_stock group by hostid,hostmfrpartnum,mfrpartnum,hostsku,Stock,DueDate,dueqty,rowid order by rowid desc";

		SqlClient.SqlDataReader rdr;

		int added = 0;
		int Updated = 0;
		rdr = da.DBExecuteReader(con, sql);
		clsstock stock__1;

		int problems = 0;
		int dupes;

		if (rdr.HasRows) {
			while (rdr.Read) {
				//current stock/future
				for (int pass = 1; pass <= 2; pass++) {
					//UPDATE existing
					if (dicStock.ContainsKey(rdr.Item("rowid") + "-" + pass)) {
						stock__1 = dicStock(rdr.Item("rowid") + "-" + pass);
						object duedate;
						duedate = IIf(object.ReferenceEquals(rdr.Item("duedate"), DBNull.Value), (System.DateTime)"1980-01-01", rdr.Item("duedate"));

						if (stock__1.quantity != rdr.Item("stock") | stock__1.Arrival != duedate) {
							stock__1.quantity = rdr.Item("stock");
							stock__1.Arrival = duedate;
							stock__1.LastUpdated = Now;
							stock__1.Source = "IQ1 import";
							stock__1.update();
							// quite expensive - could in theory bulk delete/write (with INSERT_IDENTITY on), for better performance
							Updated += 1;
						}

					} else {
						//and ADD new
						clsProduct product = null;
						object sku;
						sku = rdr.Item("mfrpartnum");
						if (dicSystems.ContainsKey(sku)) {
							product = dicSystems(sku).Product;
						} else if (dicOptions.ContainsKey(sku)) {
							product = dicOptions(sku);
						} else {
							//    Dim Err = New clsEvent(parentEvent, sku & " is not a system or an option", "error")
							problems += 1;
							//    Stop
						}


						if (!product == null) {
							System.DateTime arrival;

							if (pass == 1) {
								arrival = DateAdd(DateInterval.Day, -1, Now);
								//First pass is current stock - ie.. it arrived in the past
							} else if (pass == 2) {
								if (object.ReferenceEquals(rdr.Item("duedate"), DBNull.Value)) {
									arrival = Now;
								} else {
									arrival = rdr.Item("duedate");
								}
							}

							//if there are no variants (ie no 'internal' price - we can't add stock)
							if (product.Variants != null) {
								addstock(rdr, product, arrival, dicChannels, dicStock, pass, dupes, added);
							}



						} else {
							//anevent = New clsEvent(parentEvent, "Could not locate host/channel " & rdr.Item("hostid") & " from PNAStock", "error")
							problems += 1;

						}
					}
				}
			}
		}
		rdr.Close();


	}

	private void addstock(SqlDataReader rdr, clsProduct Product, System.DateTime arrival, Dictionary<string, clsChannel> dicChannels, Dictionary<string, clsstock> dicStock, int pass, ref int dupes, ref int added)
	{
		clsVariant skuvariant = null;
		bool Existing = false;
		clsstock stock = null;
		clsChannel seller;

		if (Product.i_Variants == null)
			return;

		if (dicChannels.ContainsKey(rdr.Item("hostID"))) {
			seller = dicChannels(rdr.Item("hostid"));
			//  ).IsCloneOf)  'get the stock from the source of any  clone ??? RM - ask dan

			// do we have a variant for this seller ?
			if (Product.i_Variants.ContainsKey(seller)) {
				skuvariant = Product.i_Variants(seller)(0);
				foreach ( shipment in skuvariant.shipments.Values) {
					if (pass == 1) {
						if (shipment.IsCurrent){Existing = true;break; // TODO: might not be correct. Was : Exit For
}
					} else {
						if (!shipment.IsCurrent){Existing = true;break; // TODO: might not be correct. Was : Exit For
}
					}
				}
				if (Existing) {
					// anevent = New clsEvent(parentEvent, "Duplicate stock in PNA_Stock for host " & rdr.Item("hostid") & " part " & sku$, "error")
					dupes += 1;
				}

				//check it's not a dupe - (IQ1.PNA_Stock had dupes)
				bool isdupe = false;
				if (!Existing) {
					bool isCurrent = (pass == 1);
					if (Product.i_Variants == null) {
						//stock for a product nobody has a price for
						Beep();
					} else {
						if (Product.i_Variants.ContainsKey(seller)) {
							if (Product.i_Variants(seller) != null) {
								skuvariant = Product.i_Variants(seller)(0);


								if (skuvariant.shipments.ContainsKey(arrival.Date)) {
									isdupe = true;
								}
							}
						}

						if (!isdupe) {
							if (!IsDBNull(rdr.Item("stock"))) {
								stock = new clsstock(skuvariant, rdr.Item("stock"), arrival.Date, "IQ1ii " + Now.ToString, isCurrent);
								dicStock.Add(rdr.Item("rowID") + "-" + pass, stock);
								added += 1;
							}

						} else {
							//duplicated stock
						}
					}
				}
			}
		}
	}

	//, dicvariants As Dictionary(Of String, clsVariant))
	public void Prices(SqlClient.SqlConnection con, Dictionary<string, clsBranch> dicSystems, Dictionary<string, clsProduct> dicskuoptionproduct, Dictionary<string, clsChannel> dicChannels)
	{

		double lastmilestone;

		//get the 'base' prices from pricelistmaster (for each Seller) - ie. the pricelistmaster.internalprice where priceBand is null
		//we do not import customer specific prices - they will come from the pricing database/feeds or webservice

		//This DOES NOT get prices for clones... who's prices now derive from the parent - and use the margin of the enquiring customer (of the clone)

		SqlClient.SqlDataReader rdr;
		object sql;

		sql = "SELECT h.hostid as seller,mfrpartnum AS partno,hostmfrpartnum,hostpartnum,internalprice AS iprice,c.currencyCode ";
		sql += "from " + server + "[iq].products.pricelistmaster h ";
		sql += "join " + server + "[iq].dbo.currencies c on h.curr=c.CurrencySymbol ";
		sql += "WHERE(h.priceBand Is null or h.priceBand='sp') ";
		//servers plus
		sql += "AND h.HostID NOT IN (SELECT subhost from " + server + "[channelcentral].customers.Host_Parents)";
		//DON't get prices for clones !
		sql += "GROUP BY h.hostid,mfrpartnum,internalprice,c.CurrencyCode,hostmfrpartnum,hostpartnum  ";
		sql += "ORDER BY partno,seller ";

		DataTable PriceWriteCache = new DataTable();
		PriceWriteCache = da.MakeWriteCacheFor(con, "Price");

		DataTable vWritecache = new DataTable();
		int nextid = 0;
		vWritecache = da.MakeWriteCacheFor(con, "Variant", nextid, true);
		//fetches the next available ID


		rdr = da.DBExecuteReader(con, sql);

		clsProduct part = null;
		//For each product - each seller offers prices to each buyer in each currency
		//Dim prices As Dictionary(Of clsChannel, Dictionary(Of clsChannel, Dictionary(Of String, Single)))

		string partno;
		string oPartno = "";

		//Dim BuyerChannel As clsChannel
		clsChannel SellerChannel;
		clsPrice Price;
		clsCurrency Currency;

		int numprices;
		int partnos;

		int pricesrows = 0;

		int dupes = 0;
		bool Dupe = false;

		SqlClient.SqlConnection wcon;

		while (rdr.Read) {
			partno = Trim(rdr.Item("partno"));
			if (partno != oPartno) {
				partnos += 1;
				if (dicSystems.ContainsKey(partno)) {
					part = dicSystems(partno).Product;

				} else {
					if (dicskuoptionproduct.ContainsKey(partno)) {
						part = dicskuoptionproduct(partno);
					} else {
						part = null;
						//can happen for things like printer cartridges
						// Logit("Invalid SKU (mfrPartno) '" & partno & "' whilst importing pricelistmaster")
					}
				}
			}

			clsVariant SKUvariant;

			if (part == null) {
			//logging moved to above so it only happens when the part changes
			} else {
				if (!dicChannels.ContainsKey(Trim(rdr.Item("seller")))) {
					Logit("Couldn't locate the seller channel for '" + rdr.Item("seller") + "' (pricelistmaster.hostID) - check CaSe and trailing     spaces.");
				} else {
					Currency = iq.i_currency_code(Trim(rdr.Item("currencycode")));
					SellerChannel = dicChannels(Trim(rdr.Item("seller")));

					//See if the host partnumber has a # in, determine the variant
					//Dim hostpartnum As String
					//hostpartnum = rdr.Item("Hostmfrpartnum")
					//Dim ih As Integer = InStr(hostpartnum, "#")

					//If ih Then
					//    Dim vc As String
					//    vc = Trim$(Mid$(hostpartnum, ih))
					//    If Not dicvariants.ContainsKey(vc) Then
					//        Dim newvariant As clsVariant
					//        newvariant = New clsVariant(vc, vc) 'for now - we use the code 'eg #ABA as the name  (see the variants table in the DB to translate)
					//        dicvariants.Add(vc, newvariant)
					//    End If
					//    SKUvariant = dicvariants(vc)
					//Else
					//    SKUvariant = iq.StandardVariant
					//End If

					//set the distiSKU in the variant to HostPartnum,HostMfrPartnum or Partnum - in that order of precedence
					object distisku;
					if (IsDBNull(rdr.Item("hostPartnum"))) {
						if (IsDBNull(rdr.Item("hostmfrpartnum"))) {
							distisku = rdr.Item("partno");
						} else {
							distisku = rdr.Item("hostMfrPartNum");
						}
					} else {
						distisku = rdr.Item("HostPartnum");
					}

					SKUvariant = new clsVariant("", part, SellerChannel, distisku, "", "", "", null, false, vWritecache,
					nextid);


					//see if we already have a price for this part - for this seller/currency/variant combo
					//we may well do as PricelistMaster contains rows for many buyers

					if (IsDBNull(rdr.Item("iPrice"))) {
					//      Logit("Price was null for " & rdr.Item("seller") & " " & hostpartnum)
					} else {
						Pmark("DupeCheck");
						Dupe = false;
						if (SKUvariant.PriceExists(Everyone, Currency)) {
							Dupe = true;
							dupes += 1;
						}
						Pacc("DupeCheck");

						if (Dupe) {
							Logit("Duplicate base price for " + SellerChannel.DisplayName(s_lang) + "('" + rdr.Item("seller") + "') SKU='" + partno + "' currency='" + Currency.Code + "' SKUVariant='" + SKUvariant.Code + "'");

						} else {
							//If rdr.Item("iPrice") = 0 Then Stop
							Price = new clsPrice(SKUvariant, Everyone, new NullablePrice((decimal)rdr.Item("iprice"), Currency, false), "Import", PriceWriteCache);
							numprices += 1;

						}
					}
				}
			}

			oPartno = partno;
			pricesrows += 1;


			if (PriceWriteCache.Rows.Count > 5000) {
				wcon = da.OpenDatabase();
				//seperate conection for the bulk writers

				da.BulkWrite(wcon, PriceWriteCache, "Price");
				//clone the STRUCTURE (emptying the table)
				DataTable temp = PriceWriteCache.Clone;
				PriceWriteCache.Dispose();

				PriceWriteCache = temp;
				//PriceWriteCache.Clone '.Clear() 'da.MakeWriteCacheFor(con, "Price")
				wcon.Close();
			}

			if (vWritecache.Rows.Count > 5000) {
				wcon = da.OpenDatabase();

				da.BulkWrite(wcon, vWritecache, "Variant");

				DataTable temp = vWritecache.Clone;
				vWritecache.Dispose();
				vWritecache = temp;
				wcon.Close();

				System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
				GC.Collect();

			}

		}

		rdr.Close();

		wcon = da.OpenDatabase();
		//seperate conection for the bulk writers

		da.BulkWrite(wcon, PriceWriteCache, "Price");
		PriceWriteCache = null;

		da.BulkWrite(wcon, vWritecache, "Variant");
		vWritecache = null;
		wcon.Close();

		Logit("Imported " + numprices + " base prices for " + partnos + " distinct SKUs with " + dupes + " duplicates in " + TimeSince(lastmilestone));

		Logit("Wrote them to database (BulkCopy) in " + TimeSince(lastmilestone));


		//        con = da.opendatabase()


		//       WE MUST RELOAD THEM NOW WE'VE (BULK) CREATED  THE PRICES AND VARIANTS - TO GET THIER id'S
		foreach ( P in iq.Products.Values) {
			P.i_Variants = null;
		}

		iq.Variants.Clear();
		con.Close();
		con = da.OpenDatabase();
		// iq.LoadVariants(con, rdr, 0)

		List<string> errorMessages = new List<string>();
		//   iq.LoadPrices(con, rdr, 0, errorMessages)

	}



	public void CarePackProperties()
	{

		clsAttribute a;

		if (English == null)
			English = iq.i_language_Code("EN");

		if (!iq.i_attribute_code.ContainsKey("CP_DRN"))
			a = new clsAttribute("CP_DRN", iq.AddTranslation("Duration", English, "CPP", 0, null, 0, false), 0);
		//9x5,13x5,24x7,Next Business Day,6hr Call-to-Response,Pickup and Return,12x7,13x7,9x7
		if (!iq.i_attribute_code.ContainsKey("CP_SVC"))
			a = new clsAttribute("CP_SVC", iq.AddTranslation("Service Level", English, "CPP", 0, null, 0, false), 0);
		//This isn't displayed at present and has values 24x7, 13x5, next business day, 6hr Call To Response, pickup an return
		if (!iq.i_attribute_code.ContainsKey("CP_DMR"))
			a = new clsAttribute("CP_DMR", iq.AddTranslation("Defective Media Retention", English, "CPP", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("CP_TRV"))
			a = new clsAttribute("CP_TRV", iq.AddTranslation("International Travel Cover", English, "CPP", 0, null, 0, false), 0);
		//       If Not iq.i_attribute_code.ContainsKey("CP_CVR") Then a = New clsAttribute("CP_CVR", iq.AddTranslation("Coverage", English, "CPP", , , False))
		if (!iq.i_attribute_code.ContainsKey("CP_ADP"))
			a = new clsAttribute("CP_ADP", iq.AddTranslation("Accidental Damage Protection", English, "CPP", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("CP_RSP"))
			a = new clsAttribute("CP_RSP", iq.AddTranslation("Response Time", English, "CPP", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("CP_CTR"))
			a = new clsAttribute("CP_CTR", iq.AddTranslation("Call-to-Repair", English, "CPP", 0, null, 0, false), 0);
		//repair/response
		if (!iq.i_attribute_code.ContainsKey("CP_ONS"))
			a = new clsAttribute("CP_ONS", iq.AddTranslation("On-Site", English, "CPP", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("CP_TRC"))
			a = new clsAttribute("CP_TRC", iq.AddTranslation("Tracing", English, "CPP", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("CP_PST"))
			a = new clsAttribute("CP_PST", iq.AddTranslation("Post Warranty", English, "CPP", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("CP_EXC"))
			a = new clsAttribute("CP_EXC", iq.AddTranslation("Exchange", English, "CPP", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("CP_PAR"))
			a = new clsAttribute("CP_PAR", iq.AddTranslation("Pickup and Return", English, "CPP", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("CP_RTD"))
			a = new clsAttribute("CP_RTD", iq.AddTranslation("Return to Depot", English, "CPP", 0, null, 0, false), 0);

		//CDMR 

		//for the ISS carepacks - The are 3 foreign key columns  - each becomes a SINGLE attribute  - with  many possible values (translations)  - (linkes to the products via productSttributes)

		SqlClient.SqlConnection con;
		con = da.OpenDatabase();
		SqlClient.SqlDataReader rdr;

		//Dim cs As List(Of String) 'service level codes
		//note placeholder comma (for a 1 based list)
		//cs = Split(",9x5,13x5,24x7,NBD,PnR,12x7,13x7,9x7,6CTR,24CTR,24x7x4,13x5x4,PAC,Colab,HWO,CDMR,DMR,NoDMR,I&S", ",").ToList

		Dictionary<int, clsTranslation> dicSLs = new Dictionary<int, clsTranslation>();
		rdr = da.DBExecuteReader(con, "SELECT scode,slabel FROM h3.iq.products.carepack_serviceLevels");
		//This is a terrible name as it contains not just service Levels but Response Times & DMR  info too

		clsTranslation tl;
		IEnumerable<clsTranslation> tokill;
		tokill = from j in iq.Translations.Valueswhere j.Group == "CPSL";
		foreach ( tl in tokill) {
			tl.delete(English);
		}

		while (rdr.Read) {
			tl = iq.AddTranslation(rdr.Item("slabel"), English, "CPSL", 0, null, 0, false);
			//these MUST NOT have an order on them as that takes precedence
			dicSLs.Add((int)rdr.Item("scode"), tl);
		}
		rdr.Close();

		//These three attributes represent the ISS columns (which are pointers to iq.products.carePack_ServiceLevels

		if (!iq.i_attribute_code.ContainsKey("ISS_SL"))
			a = new clsAttribute("ISS_SL", iq.AddTranslation("Service Level", English, "CPSL", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("ISS_OP"))
			a = new clsAttribute("ISS_OP", iq.AddTranslation("Options", English, "CPSL", 0, null, 0, false), 0);
		if (!iq.i_attribute_code.ContainsKey("ISS_RC"))
			a = new clsAttribute("ISS_RC", iq.AddTranslation("Response Time", English, "CPSL", 0, null, 0, false), 0);

		//delete any existing carepack productattributes
		da.DBExecutesql("DELETE FROM productAttribute WHERE fk_attribute_id IN (SELECT attribute.id FROM attribute JOIN translation t ON fk_translation_key_name = t.[key] WHERE [group]='CPP')");
		da.DBExecutesql("DELETE FROM productAttribute WHERE fk_attribute_id IN (SELECT attribute.id FROM attribute JOIN translation t ON fk_translation_key_name = t.[key] WHERE [group]='CPSL')");

		con = da.OpenDatabase();

		DataTable pawc = da.MakeWriteCacheFor(con, "ProductAttribute");
		//Allows a bulk insert -many many times faster than INSERTING the individual rows

		//OptTechnology and OptProvision rules are pauls random stuff
		object sql = "SELECT [OptSN] ,[OptSKU],[Description],[Duration],[DMR],servicelevel,[Travel],[OptTechnology],[OptProvisionRules],[ADP],[ResponseTime],[CTR],[OnSite],[Tracing],[PostWarranty]";
		sql += ",[Exchange],[PickUpReturn],[ReturnToDepot],servicelevel_iss,Responsecode_iss,options_iss ";
		sql += "FROM h3.[iq].[Products].[CarePack_Properties]";
		// join h3.iq.products.carepack_servicelevels sl on sl.scode = servicelevel"
		//sql$ &= "join h3.iq.products.carepack_servicelevels iss_sl on iss_sl.scode = iss_servicelevel"
		//sql$ &= "join h3.iq.products.carepack_servicelevels iss_sl on iss_sl.scode = iss_servicelevel"

		rdr = da.DBExecuteReader(con, sql);
		object sku;
		clsProduct product;

		clsProductAttribute pa;

		tokill = from j in iq.Translations.Valueswhere j.Group == "CPSVCLVLS";
		foreach ( tl in tokill) {
			tl.delete(English);
		}

		while (rdr.Read) {
			sku = rdr.Item("optSku");
			if (iq.i_SKU.ContainsKey(sku)) {
				product = iq.i_SKU(sku);

				//we will display these as CSS lozenges - using the ProductAttributes (text) Value as the lozenge text and the using the attributes Name as a tool tip
				//For now we're not importing post warranty care packs
				if (rdr.Item("PostWarranty") == false) {
					//We also ignore rows where onsite is null
					if (!IsDBNull(rdr.Item("Onsite"))) {
						//These are the 'boolean' ones
						//Pipe is the major seperator - Attribute code,Sql Column Name, and displaly text are comma seperated within that
						object l = "CP_DMR,DMR,DMR|CP_TRV,Travel,Travel|CP_ADP,ADP,ADP|CP_CTR,CTR,CTR|CP_ONS,Onsite,On-Site|CP_TRC,Tracing,Trace|CP_PST,PostWarranty,Post|CP_EXC,Exchange,Exch|CP_PAR,PickUpReturn,PickUp|CP_RTD,ReturnToDepot,RTD";

						string[] bits;
						foreach ( one in Split(l, "|")) {
							bits = one.Split(",");
							if (!object.ReferenceEquals(rdr.Item(bits(1)), DBNull.Value)) {
								pa = new clsProductAttribute(product, bits(0), rdr.Item(bits(1)), "txt", bits(2), pawc, null, 0);
								//Their numeric value will be 1 or 0 and their text will be the short code
							}
						}

						//duration
						if (!IsDBNull(rdr.Item("duration"))) {
							pa = new clsProductAttribute(product, "CP_DRN", rdr.Item("duration"), "year", rdr.Item("Duration") + " yr", pawc, null, 0);
						}

						//response time
						if (!IsDBNull(rdr.Item("ResponseTime"))) {
							pa = new clsProductAttribute(product, "CP_RSP", (float)rdr.Item("ResponseTime"), "hour", rdr.Item("ResponseTime") + " hrs", pawc, null, 0);
						}

						//Servicelevel is NON Iss (i beleive)
						//servicelevel_ISS (Hardware only, colaborative support, ProActive Care, installation and startup)
						//responsecode_ISS (24x7, 6hr CTR, NBD, 13x5 4hr, 24hr CTR
						//options_ISS (NO DMR, DMR, Comprehnsive DMR)
						//"SL_ISS") Then a = New clsAttribute("SL_ISS", iq.AddTranslation("Service Level", English, "CPSL"))


						//CDMR ???
						foreach ( k in Split("servicelevel^CP_SVC,servicelevel_iss^ISS_SL,options_iss^ISS_OP,responsecode_iss^ISS_RC", ",")) {
							string[] p = Split(k, "^");
							//get the IQ1 database column name (in iq.products.carePackProperties) and corresponding attribute code (for the attribute we've made)
							if (!IsDBNull(rdr.Item(p(0)))) {
								int fk = (int)rdr.Item(p(0));
								//This is the foriegn key value from the column
								//Make the ProductAttribute (attaching an instance and value of this attribute to this product)
								pa = new clsProductAttribute(product, iq.i_attribute_code(p(1)), fk, iq.i_unit_code("txt"), dicSLs(fk), pawc);
								//dicSLS carries the pre-prepared translations one for each FK target (from iq.products.carepack_servicelevels)
							}
						}
					}
				}
			}
		}
		rdr.Close();

		Debug.Print(pawc.Rows.Count);
		da.BulkWrite(con, pawc, "[ProductAttribute]");
		con.Close();


	}

	//Public Function HostPrices(forHostID As String, priceBand As String) As String

	//    'Obsoleted


	//    Dim con As SqlClient.SqlConnection
	//    con = da.OpenDatabase()

	//    Dim lastmilestone As Double

	//    'This DOES NOT get prices for clones... who's prices now derive from the parent - and use the margin of the enquiring customer (of the clone)

	//    Dim hostID As String
	//    Dim rdr As SqlClient.SqlDataReader
	//    Dim sql$


	//    'sql$ = "DELETE FROM [Price] WHERE Fk_variant_id IN "
	//    'sql$ &= "(SELECT ID FROM [variant] WHERE fk_channel_id_seller="
	//    'sql$ &= iq.i_channel_code(forHostID).ID & ")"

	//    '        For Each product In iq.Products.Values
	//    ' product.i_Variants()
	//    ' Next


	//    'da.DBExecutesql(sql$)

	//    'read all prices for some buyeraccount in the pricing database..
	//    'create/update variants
	//    'and make the stock and price records


	//    'sql$ = "SELECT h.hostid as seller,hostpartnum,hostmfrpartnum,internalprice AS iprice,externalPrice as ePrice, priceBand,c.currencyCode "
	//    'sql$ &= "FROM " & server$ & "[iq].products.pricelistmaster h "
	//    'sql$ &= "JOIN " & server$ & "[iq].dbo.currencies c ON h.curr=c.CurrencySymbol "
	//    ''sql$ &= "WHERE(h.priceBand Is null) "
	//    'sql$ &= "WHERE h.hostid='" & forHostID & "'"
	//    'sql$ &= " AND h.HostID NOT IN (SELECT subhost from " & server$ & "[channelcentral].customers.Host_Parents)" 'DON't get prices for clones !
	//    'If priceBand = "" Then
	//    '    sql$ &= " AND priceBand is null "
	//    'Else
	//    '    sql$ &= " AND priceBand='" & priceBand & "'"
	//    'End If
	//    'sql$ &= " GROUP BY h.HostID,hostpartnum,hostmfrpartnum,InternalPrice,ExternalPrice,priceBand,currencycode "
	//    'sql$ &= "ORDER BY HOSTPARTNUM,seller,priceBand,currencycode "


	//    sql$ = "SELECT ba.id as baid, c.ID as catid, p.price,c.HostPartNum,c.hostmfrpartnum FROM h1.pricing.pna.buyeraccount AS ba"
	//    sql$ &= "JOIN h1.pricing.pna.price AS p ON p.buyeraccount_id=ba.id "
	//    sql$ &= "JOIN h1.pricing.pna.cat AS c ON p.Cat_ID = c.id "
	//    sql$ &= "JOIN h3.channelcentral.customers.host_properties hp on hp.HID=ba.host_id "
	//    sql$ &= "WHERE hp.HostID='DAZRG248NE'"


	//    Dim PriceWriteCache As New DataTable
	//    PriceWriteCache = da.MakeWriteCacheFor(con, "Price")

	//    Dim svWriteCache As DataTable = da.MakeWriteCacheFor(con, "variant")


	//    rdr = da.DBExecuteReader(con, sql$)

	//    Dim part As clsProduct = Nothing
	//    'For each product - each seller offers prices to each buyer in each currency
	//    'Dim prices As Dictionary(Of clsChannel, Dictionary(Of clsChannel, Dictionary(Of String, Single)))

	//    Dim HostPartNo As String
	//    Dim oHostPartNo As String = ""

	//    'Dim BuyerChannel As clsChannel
	//    Dim SellerChannel As clsChannel
	//    'Dim Price As clsPrice
	//    Dim Currency As clsCurrency

	//    Dim partnos As Integer

	//    Dim pricesrows As Integer = 0

	//    Dim dupes As Integer = 0
	//    Dim Dupe As Boolean = False
	//    Dim newPrice As clsPrice
	//    Dim ExistingPrice As clsPrice

	//    Dim increased As Integer
	//    Dim decreased As Integer
	//    Dim same As Integer
	//    Dim newPrices As Integer

	//    Dim skipped As Integer

	//    Dim ok$ = "", k$ = ""

	//    While rdr.Read

	//        hostID = rdr.Item("seller")
	//        If rdr.Item("HOSTPARTNUM") Is DBNull.Value Then
	//            HostPartNo = Trim$(rdr.Item("hostmfrpartnum"))  'YUCK - westcoast
	//        Else
	//            HostPartNo = Trim$(rdr.Item("hostpartnum"))
	//        End If

	//        HostPartNo = Split(HostPartNo, "#")(0)  'trim any #suffix

	//        If HostPartNo <> oHostPartNo Then
	//            partnos += 1
	//            Dim mfrpartno = rdr.Item("HostMfrPartNum")
	//            mfrpartno = Split(mfrpartno, "#")(0)  'trim any #suffix
	//            If iq.i_SKU.ContainsKey(mfrpartno) Then
	//                part = iq.i_SKU(mfrpartno)
	//            Else
	//                Logit("Invalid SKU:" & mfrpartno)
	//                part = Nothing
	//            End If
	//        End If

	//        Dim hac As String
	//        If IsDBNull(rdr.Item("priceBand")) Then hac$ = "" Else hac = rdr.Item("priceBand")
	//        k$ = HostPartNo & "^" & hostID & "^" & hac$ & "^" & rdr.Item("currencycode")
	//        ok$ = k$

	//        Dim SKUvariant As clsVariant

	//        If part IsNot Nothing Then
	//            'logging moved to above so it only happens when the part changes
	//            If Not iq.i_channel_code.ContainsKey(Trim$(rdr.Item("seller"))) Then
	//                Logit("Couldn't locate the seller channel for '" & rdr.Item("seller") & "' (pricelistmaster.hostID) - check CaSe and trailing     spaces.")
	//            Else
	//                Currency = iq.i_currency_code(Trim$(rdr.Item("currencycode")))
	//                SellerChannel = iq.i_channel_code(Trim$(rdr.Item("seller")))

	//                'See if the host partnumber has a # in, determine the variant
	//                'Dim hostpartnum As String
	//                'hostpartnum = rdr.Item("Hostmfrpartnum")
	//                'Dim ih As Integer = InStr(hostpartnum, "#")

	//                'If ih Then
	//                '    Dim vc As String
	//                '    vc = Trim$(Mid$(hostpartnum, ih))
	//                '    If Not iq.i_variant_code.ContainsKey(vc) Then
	//                '        Dim newvariant As clsVariant
	//                '        newvariant = New clsVariant(vc, vc) 'for now - we use the code 'eg #ABA as the name  (see the variants table in the DB to translate)
	//                '    End If
	//                '    SKUvariant = iq.i_variant_code(vc)
	//                'Else
	//                '    SKUvariant = iq.StandardVariant
	//                'End If

	//                Dim distiSKU As String = NothingFromNull(rdr.Item("HOSTpartnUM")) 'this is this hostpartnumber
	//                'SellerChannel.Variants(distiSKU)iq.i_variant_hostSKU(SellerChannel.ID & "^" & distiSKU))
	//                SKUvariant = New clsVariant("", part, SellerChannel, distiSKU, "", "", "", Nothing, False, svWriteCache)

	//                'see if we already have a price for this part - for this seller/currency/variant combo
	//                'we may well do as PricelistMaster contains rows for many buyers

	//                Dim buyerChannel As clsChannel = Nothing
	//                Dim price As Decimal
	//                If IsDBNull(rdr.Item("priceBand")) Then
	//                    'Internal Price
	//                    buyerChannel = Everyone
	//                    If IsDBNull(rdr.Item("iprice")) Then
	//                        price = -1
	//                    Else
	//                        price = rdr.Item("iprice")
	//                    End If
	//                Else
	//                    'the accounts hold an index to the (buyer)channel/priceBand (of that buyer)
	//                    If iq.i_Account_HostIDpriceBand.ContainsKey(hostID & "^" & rdr.Item("priceBand")) Then
	//                        buyerChannel = iq.i_Account_HostIDpriceBand(hostID & "^" & rdr.Item("priceBand")).BuyerChannel
	//                        If IsDBNull(rdr.Item("eprice")) Then
	//                            price = -1
	//                        Else
	//                            price = rdr.Item("eprice")
	//                        End If
	//                    Else
	//                        price = -1  'missing account info
	//                    End If
	//                End If

	//                Dupe = False

	//                If price = -1 Then
	//                    'null price (synnex and a few unhosted)
	//                    skipped += 1
	//                Else
	//                    If SKUvariant.PriceExists(buyerChannel, Currency) Then
	//                        'update
	//                        Dim existingPrices As List(Of clsPrice)
	//                        existingPrices = part.Prices(SellerChannel, buyerChannel, Currency, SKUvariant)

	//                        ExistingPrice = existingPrices(0)

	//                        If ExistingPrice.ID = -1 Then
	//                            dupes += 1
	//                        Else
	//                            If price = ExistingPrice.Price.value Then
	//                                same = same + 1
	//                                If Math.Abs(DateDiff(DateInterval.Day, Now, ExistingPrice.DateStamp)) > 6 Then ExistingPrice.Update() 'touch any file more than 7 days old
	//                            ElseIf price > ExistingPrice.Price.value Then
	//                                increased += 1
	//                                ExistingPrice.Update() 'slow
	//                            Else
	//                                decreased += 1
	//                                ExistingPrice.Update() 'slow
	//                            End If
	//                        End If
	//                    Else
	//                        'add
	//                        newPrice = New clsPrice(SKUvariant, buyerChannel, New NullablePrice(CDec(rdr.Item("iprice")), Currency), "zImport", PriceWriteCache)
	//                        newPrices += 1
	//                    End If
	//                End If
	//            End If
	//        End If

	//        oHostPartNo = HostPartNo
	//        pricesrows += 1
	//    End While

	//    rdr.Close()

	//    da.BulkWrite(con, PriceWriteCache, "Price")
	//    PriceWriteCache = Nothing

	//    con.Close()

	//    HostPrices = "Processed " & pricesrows & " pricelistmaster rows. " & _
	//        skipped & " prices were skipped (null), " & _
	//        increased & " prices increased," & _
	//        decreased & " prices decreased," & _
	//        same & " prices were the same. " & _
	//        newPrices & " prices were added. " & _
	//        "Total time:" & TimeSince(lastmilestone)

	//    Logit(HostPrices)

	//    Logit("Wrote them to database (BulkCopy) in " & TimeSince(lastmilestone))


	//End Function


	public int FIOfocus()
	{

		SqlClient.SqlConnection con = da.OpenDatabase;
		SqlClient.SqlDataReader rdr;


		DataTable pawc = da.MakeWriteCacheFor(con, "ProductAttribute");

		rdr = da.DBExecuteReader(con, "SELECT Optsku,fio from h3.iq.products.options");

		clsAttribute focusAtt = iq.i_attribute_code("focus");
		clsTranslation fioTl = iq.AddTranslation("FIO", English, "Foci", 0, null, 0, false);


		while (rdr.Read) {
			if (rdr.Item("fio") != 0) {
				string sku = rdr.Item("optsku");


				if (iq.i_SKU.ContainsKey(sku)) {
					clsProduct optionProd = iq.i_SKU(sku);
					//make an FIO focus attribute
					clsProductAttribute fa = new clsProductAttribute(optionProd, focusAtt, 1, iq.i_unit_code("txt"), fioTl, pawc);
					FIOfocus += 1;
				}
			}
		}

		rdr.Close();

		da.BulkWrite(con, pawc, "ProductAttribute");

		con.Close();

	}





	public Dictionary<string, Dictionary<string, int>> FIOs(SqlClient.SqlConnection con, string opt = null, string sys = null)
	{

		//Returns a dictionary (by system mfrSKU) of the quantities and part numbers of all factory installed components (PriStor, sec stor CPU, MEM etc)
		//for each system
		//Does NOT return the Qmax, Or Increments - as these come from optionLimits
		//which despite the name is much LESS specific (a better name would be optionTypeLimitsPerFamily !)

		FIOs = new Dictionary<string, Dictionary<string, int>>(StringComparer.CurrentCultureIgnoreCase);

		// [CPU],[CPUqty],[RAM],[RAMqty],[Comms/Controllers/Other],[PriStor],[PriStorQty],[SecStor],[SecStorQty]
		//,[TerStor],[terstorqty],[RAID],[RAIDtech],[RAIDcache],[VGA],[PSU],[PSUqty],[Extras],[Software],[IntVideo],[Display],[WarrantyCode],[TextInt]
		//,[SupplyChainCode],[EOL],[Active],[TerStorQty],[TerStorTech],[SysType2],[Options],[WLAN],[WWAN],[EnergyStar],[DiscreteGraphics]

		// 	FormFactor	MfrBuildCode	ActiveSites	ActiveFromDate	AlsoHost	IntVideo	PriStor	PriStorQty	SecStor	SecStorQty	TerStor	TerStorQty	TerStorTech	RAID	RAIDcache	RAIDtech	VGA	PSU	PSUqty	Extras	Software	Options	WarrantyCode	WeightUnboxed	TextInt	Active	EOL	ActiveToDate	EnergyStar	DiscreteGraphics	ILOhardware	ILOlicense	ICEincluded	AvalancheSystem	ProductNote

		//outstanding
		//[RAIDtech],[VGA],[Extras],[Software],[IntVideo],[Display],[WarrantyCode],[TextInt]
		//,[SupplyChainCode],[EOL],[Active],[TerStorTech],[SysType2],[Options],[WLAN],[WWAN],[EnergyStar],[DiscreteGraphics]

		//the 'technology' (of terstore,Raid) - should be an attribute of the product they point to
		//Display DIS_size_abbrev_wxh_AG

		//intVideo and discreteGrapics are abbreviations -  become attributes in import(systems)

		//Extras and software are both a 'mix' of abbreviations and part numbers

		SqlClient.SqlDataReader rdr;
		object sql;

		//IloHardware is an abbreviation (handled in import.systems)

		sql = "SELECT sysType,modelSKU,cpu,cpuqty,ram,ramqty,pristor,pristorqty,secstor,secstorqty,terstor,terstorqty,raid,raidcache,psu,psuqty,iloLicense,iloHardware,iceIncluded,";
		sql += "WLAN,WWAN,[controllers],extras,software,discreteGraphics";
		sql += " FROM " + server + "[iq].products.union_systems ";
		//If opt IsNot Nothing Then
		//    sql &= "WHERE '%' + cpu + '%' like '%' + " & opt & " + '%' or '%' + ram + '%' like '%' + " & opt & " + '%' or '%' + pristor + '%' like '%' + " & opt & " + '%' or '%' + secstor + '%' like '%' + " & opt & " + '%' or '%' + terstor + '%' like '%' + " & opt & " + '%' or '%' + raid + '%' like '%' + " & opt & " + '%' or '%' + raidcache + '%' like '%' + " & opt & " + '%' or '%' + psu + '%' like '%' + " & opt & " + '%' or '%' + ilolicense + '%' like '%' + " & opt & " + '%' or '%' + ilohardware + '%' like '%' + " & opt & " + '%' or '%' + software + '%' like '%' + " & opt & " + '%'"
		//End If
		if (sys != null) {
			if (opt == null)
				sql += " AND ";
			sql += " where modelsku IN (" + sys + ")";
		}
		sql += " ORDER BY modelsku";
		rdr = da.DBExecuteReader(con, sql);



		int errs;

		int nex;

		string sysSKU;
		sysSKU = "";
		Dictionary<string, int> dic = null;
		//this is the inner dictionary for one system (of all it's FIO's and their qtys)
		while (rdr.Read) {
			//can be removed
			if (rdr.Item("ModelSKU") == "JE074A") {
				object a = 1;
			}
			// do not import systems begining with X they are 'fake'
			if (LCase(Left(rdr.Item("ModelSKU"), 1)) != "x") {
				if (rdr.Item("modelsku") != sysSKU) {
					//we're onto the next system
					dic = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
					FIOs.Add(rdr.Item("modelsku"), dic);
					sysSKU = rdr.Item("modelsku");
					//     If sysSKU = "668812-421" Then Stop 'this should have 1 * 647893-B21  - for ram 

				}


				//each of these columns (may) contain a part number - and has a corresponsing 'qty' column

				foreach ( Thing in Split("cpu,ram,pristor,secstor,terstor,psu", ",")) {
					if (!IsDBNull(rdr.Item(Thing))) {
						string pn = rdr.Item(Thing);
						//part number (for tis families CPU, HDD, DVDROM etc
						//there were a few parts with null qty columns
						if (IsDBNull(rdr.Item(Thing + "qty"))) {
							if (!dic.ContainsKey(pn)) {
								dic.Add(pn, -1);
								//
							} else {
								dic(pn) = -1;
								//more than one option for this system both with Null qtys
							}
						} else {
							// If sysSKU = "675421-421" And pn = "675450-B21" Then Stop

							if (dic.ContainsKey(pn)) {
								if (errs < 100) {
									Logit(sysSKU + " has " + pn + " as its " + Thing + " Which appears in another column (pirstor,secstor,terstor,psu or ram - see iq.products,union_systems (first 100 occurances logged)");
								}
								errs += 1;

								dic(pn) += rdr.Item(Thing + "qty");
							} else {
								int qty = rdr.Item(Thing + "qty");
								if (!iq.i_SKU.ContainsKey(pn))
									nex += 1;
								dic.Add(pn, qty);
								//add the part number and a quantity   - we don't record the option type... just that it is an (installed) option
							}
						}
					}
				}

				//each of these columns (may) contain a part number - these are the 'quantityliess' ones
				foreach ( one in Split("raid,raidcache,wlan,wwan,ilolicense,ilohardware,iceIncluded,discreteGraphics,software", ",")) {
					if (!IsDBNull(rdr.Item(one))) {
						//Some of the storage devices have a chache controller and CPU as the same sku - which was tripping this up
						if (!dic.ContainsKey(rdr.Item(one))) {
							foreach ( s in Split(rdr.Item(one), ",")) {
								if (!iq.i_SKU.ContainsKey(s))
									nex += 1;
								dic.Add(s, 1);
								//add the part number and a quantity   - we don't record the option type... just that it is an (installed) option
							}

						}
					}
				}
			}
		}
		rdr.Close();

		Debug.Print("there are " + nex + " nonexistent FIOs");

	}


	public object loadDic(SqlClient.SqlConnection con, object dicIQ2, string dicCode)
	{


		//Dictionary(Of String, Object)


		//Reads all the rows of type dicCode from the IQ2 IMPORTDics table to 
		//RETURN a dictionary which resolves some string Key (in IQ1)  (a row ID, partnumber, or sometimes a compound key made by concatenating fields)
		//to some IQ2 Object (pulled from dicIQ2) - for exampole and IQ1 HostID to IQ2 Channel object


		//This bit was VERY hard.. creates (using reflection) a dictionary of string>correct IQ2 object

		Type d1 = dicIQ2.GetType.GetGenericTypeDefinition;
		Type[] typeargs = d1.GetGenericArguments;

		typeargs(0) = typeof(string);
		//Our new dictionary will *always* have a string key
		Type[] IQdicttypes = dicIQ2.GetType.GetGenericArguments;
		//gets the types of the Key and Value in the IQ2 dictionary

		typeargs(1) = IQdicttypes(1);
		//make the new dictionary have VALUES of the same type (class) as the IQ2 dictionary's values

		//This (convoluted) bit , createas a dictionary with a case insensitive key - so we can copy that argument.. (2).. and create similarly case insensitive dics
		//Dim adic As New Dictionary(Of String, Object)(StringComparer.CurrentCultureIgnoreCase)
		//ReDim Preserve typeargs(2)
		//Dim adicargs() As Type = dicIQ2.GetType.GetGenericArguments
		//typeargs(2) = adicargs(2)

		Type d2 = typeof(Dictionary<, >);
		//we'll be constucting a '2D' dictionary...

		Type constructed = d2.MakeGenericType(typeargs);

		loadDic = Activator.CreateInstance(constructed);
		//Create it

		//Dim AnEvent As clsEvent = Nothing
		//If parentEvent IsNot Nothing Then
		//    AnEvent = New clsEvent(parentEvent, "", ev_Info)
		//End If
		//con.Close()
		//con = da.OpenDatabase()
		object sql;
		SqlClient.SqlDataReader rdr;
		sql = "Select [key],id,dicCode from importDics where dicCode ='" + dicCode + "';";

		rdr = da.DBExecuteReader(con, sql);

		int count = 0;
		while (rdr.Read) {
			if (dicIQ2 == null) {
				loadDic.Add(rdr.Item("key"), rdr.Item("id"));
				//If no target dictionary is specified - it's assumed to be a dictionary of integer keys
			} else {
				string id = rdr.Item("id");
				if (id != "-1") {
					loadDic.Add(rdr.Item("key"), dicIQ2(rdr.Item("id")));
					//<<Here's where it all happens
					count += 1;
				}

			}

		}
		rdr.Close();


	}


	public void saveDic(SqlClient.SqlConnection con, object Dic, string dicCode)
	{
		da.DBExecutesql("DELETE FROM importDics WHERE diccode='" + dicCode + "';");

		DataTable WriteCache = new DataTable();
		WriteCache = da.MakeWriteCacheFor(con, "ImportDics");

		System.Data.DataRow row;

		foreach ( k in Dic.keys) {
			row = WriteCache.NewRow();

			row("key") = k;
			//this is the original key or code in IQ1
			if (dicCode == "sysDesc" | dicCode == "sysOS") {
				row("id") = Dic(k).key;
				//record translation onjects have keys not ID's
			} else {
				row("id") = Dic(k).id;
				//record the ID of every object
			}

			row("dicCode") = dicCode;
			WriteCache.Rows.Add(row);
		}

		da.BulkWrite(con, WriteCache, "ImportDics");
		WriteCache = null;

	}


	public void writeDicOptLimits(Dictionary<string, clsLimit> dic, filename)
	{
		IO.StreamWriter sw = new IO.StreamWriter(filename, false);

		foreach ( kvp in dic) {
			sw.WriteLine(kvp.Key + "|In:" + kvp.Value.Qinstalled + " Mn" + kvp.Value.Qmin + " Mx" + kvp.Value.Qmax);
		}

		sw.Close();


	}

	public void WriteDicFIOs(Dictionary<string, Dictionary<string, int>> dicfios, filename)
	{
		IO.StreamWriter sw = new IO.StreamWriter(filename, false);

		int errs = 0;

		int msys = 0;
		foreach ( SystemSKU in dicfios.Keys) {

			if (Left(SystemSKU, 3) != "###") {
				object sysname = "";
				if (iq.i_SKU.ContainsKey(SystemSKU)) {
					sysname = iq.i_SKU(SystemSKU).DisplayName(English);
				} else {
					sysname = "Missing System SKU ???";
					msys += 1;
				}

				sw.WriteLine("System:'" + SystemSKU + "' " + sysname);
				foreach ( OptionSKU in dicfios(SystemSKU)) {
					if (iq.i_SKU.ContainsKey(OptionSKU.Key)) {
						sw.WriteLine("  Option:'" + OptionSKU.Key + "' " + iq.i_SKU(OptionSKU.Key).DisplayName(English) + " QTY:" + OptionSKU.Value);
					} else {
						sw.WriteLine("  Missing Option SKU:'" + OptionSKU.Key + "'");
						errs += 1;
						//these arent really errors - they were just abbreviations (not part numbers) in some og the option columns
					}
				}
			}

		}

		sw.Close();

		//    If errs Then Stop

	}

	//If UCase(optTypeKey) = "CPU" Then
	//    'refactor the CPU branch - only one CPU is an option per system - we just create and graft one branch for it (rather than pruning many non-options)
	//    For Each SupplyChain In dicFamily(sysfamkey).childBranches.Values
	//        For Each systemBranch In SupplyChain.childBranches.Values

	//            '                                    If systemBranch.Product.Attributes("MfrSKU").Translation.Text(English) = "666157-B21" Then Stop

	//            If systemBranch.Product.isSystem Then  'IMPORTANT
	//                'Dim cpuBranch As New clsBranch(dicOptTypeProd("cpu"), Systembranch,)
	//                'Make the placholder branch "CPU"
	//                Dim cpuBranch As New clsBranch(Nothing, systemBranch, dicOptType("cpu").Translation, "", optTrans, optTransSingular, Nothing, branchWriteCache, nextBranchID)
	//                If systemBranch.Product.i_attributes_code.containskey("cpuSKU") Then
	//                    Dim cpusku As String = systemBranch.Product.i_attributes_code("cpuSKU").Translation.text(s_lang)
	//                    If Not optionsbysku.ContainsKey(cpusku) Then
	//                        Logit("cpu " & cpusku & " is not an option")  'Many systems don't have their processor as an option (becuase they are already fully populated, or are single CPU)
	//                    Else
	//                        product = optionsbysku(cpusku)
	//                        Dim cpu As New clsBranch(product, cpuBranch, product.i_attributes_code("~ame").Translation, "", optTrans, optTransSingular, Nothing, branchWriteCache, nextBranchID)

	//                        'need a quantity of cpu's  - 

	//                    End If
	//                Else
	//                    Logit("System:" & systemBranch.Name & " has no processor")
	//                End If
	//            End If
	//        Next
	//    Next
	//Else
	//(as they contain a different set of options in each family (although the same in each system))


	public Dictionary<string, clsState> quoteStates(SqlClient.SqlConnection con)
	{

		//STATES
		quoteStates = new Dictionary<string, clsState>(StringComparer.CurrentCultureIgnoreCase);

		clsState aState;

		SqlClient.SqlDataReader rdr;
		rdr = da.DBExecuteReader(con, "SELECT statuscode,statustext FROM " + server + "iq.quote.statuscodes");

		object code;

		int order = 10;

		while (rdr.Read) {
			code = Trim(rdr.Item("statusCode"));
			aState = new clsState("QT", code, iq.AddTranslation(Trim(rdr.Item("statustext")), English, "QS", 0, null, 0, false), order, "#a0a0a0");
			order += 10;
			quoteStates.Add(code, aState);
		}
		rdr.Close();

	}



	private void AddAttribute(string ColName, SqlDataReader rdr, clsProduct product)
	{
		clsAttribute Attribute;
		clsProductAttribute productAttribute;

		if (!iq.i_attribute_code.ContainsKey(ColName)) {
			Attribute = new clsAttribute(ColName, iq.AddTranslation(ColName, English, "", 0, null, 0, false), 0);
			//i'll let you off this one
		}

		Attribute = iq.i_attribute_code(ColName);

		if (!IsDBNull(rdr.Item(ColName))) {
			clsTranslation tlcn = iq.AddTranslation(rdr.Item(ColName), English, "attrib", 0, null, 0, true);
			productAttribute = new clsProductAttribute(product, Attribute, 0, iq.i_unit_code("txt"), tlcn, null);
		}

	}
	public void MLCarePacks()
	{
		// Delete care packs 



	}


	public void CarePacks()
	{
		//

		SqlClient.SqlConnection con = da.OpenDatabase("Data Source=www.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;");
		SqlClient.SqlDataReader rdr;
		string query = "select distinct CPKpartnum from  datastore.products.carepacks";

		string systemSku = string.Empty;
		string optionSku = string.Empty;
		rdr = da.DBExecuteReader(con, query);
		clsTranslation cptl;
		clsTranslation cpstl;
		cptl = iq.AddTranslation("Carepack", English, "collect", 0, null, 0, false);
		cpstl = iq.AddTranslation("Carepacks", English, "collect", 0, null, 0, false);
		clsBranch carePackBranch;
		clsBranch carePackRoot = new clsBranch(null, null, cpstl, "", cpstl, cptl, iq.i_screens_code("Care"), 1, false, "B");
		int noProducts;
		while (rdr.Read) {
			string carepackPart = rdr.Item("CPKpartnum").ToString();
			if (iq.i_SKU.ContainsKey(carepackPart)) {
				clsProduct carepackProduct = iq.i_SKU(carepackPart);
				object cpqBranch = iq.i_SKU(rdr("CPKpartnum")).Branches.First;
				clsTranslation carepackTrans = iq.AddTranslation(rdr.Item("CPKpartnum"), English, "CPSKUS", 0, null, 0, false);
				carePackBranch = new clsBranch(carepackProduct, carePackRoot, carepackTrans, "", cpstl, cptl, null, 1, false, "B");

			} else {
				// if we cant find products 
				noProducts += 1;

			}
		}
	}

	//

	public void ImportQuickSpecsInc(clsProduct product, string famName, bool Inserting, DataTable AttributeCache)
	{
		SqlClient.SqlConnection con = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;");
		SqlClient.SqlDataReader rdr;
		string query = "select [SysFamilyName],[docType],[docURL],[URLexists] FROM [iQ].[products].[SupportDocs] WHERE SysFamilyName='" + famName + "'";

		rdr = da.DBExecuteReader(con, query);

		if (!iq.i_attribute_code.ContainsKey("Document Links")) {
			clsAttribute quickspecAttribute;
			quickspecAttribute = new clsAttribute("Document Links", iq.AddTranslation("Document Links", English, "", 0, null, 0, false), 0);
		}

		DataTable dt = new DataTable();
		dt.Load(rdr);

		string linkAttributes = string.Empty;
		foreach (DataRow row in dt.Rows) {
			string attributeName = row("docType");
			linkAttributes += "<a href =\"" + row("docUrl") + "\"  target=\"_blank\">" + attributeName + "</a>  ";
		}

		clsProductAttribute fn;
		fn = new clsProductAttribute(product, iq.i_attribute_code("Document Links"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "QS", 0, null, 0, false), AttributeCache, !Inserting);

		con.Close();


	}



	public void ImportQuickSpecs()
	{
		SqlClient.SqlConnection con = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;");
		SqlClient.SqlDataReader rdr;
		string query = "select [SysFamilyName],[docType],[docURL],[URLexists] FROM [iQ].[products].[SupportDocs]";

		rdr = da.DBExecuteReader(con, query);

		if (!iq.i_attribute_code.ContainsKey("Document Links")) {
			clsAttribute quickspecAttribute;
			quickspecAttribute = new clsAttribute("Document Links", iq.AddTranslation("Document Links", English, "", 0, null, 0, false), 0);
		}

		DataTable dt = new DataTable();
		dt.Load(rdr);

		string systemSku = string.Empty;
		string optionSku = string.Empty;
		foreach ( product in iq.Products.Values) {
			List<clsProductAttribute> perf = null;
			product.i_Attributes_Code.TryGetValue("FamMajor", perf);
			if (perf != null) {
				clsProductAttribute prdAttribute = perf(0);
				string valueAttribute = prdAttribute.Translation.text(English);
				DataRow[] drarray;
				string filterExp = "SysFamilyName = '" + Trim(valueAttribute) + "'";
				drarray = dt.Select(filterExp);

				if (drarray.Length > 0) {
					string linkAttributes = string.Empty;
					foreach (DataRow row in drarray) {
						string attributeName = row("docType");
						linkAttributes += "<a href =\"" + row("docUrl") + "\"  target=\"_blank\">" + attributeName + "</a>  ";
					}

					clsProductAttribute fn;
					fn = new clsProductAttribute(product, iq.i_attribute_code("Document Links"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "QS", 0, null, 0, false));

				}

			}

		}

		//While rdr.Read
		//    Dim carepackPart As String = rdr.Item("CPKpartnum").ToString()
		//    If iq.i_SKU.ContainsKey(carepackPart) Then
		//        Dim carepackProduct As clsProduct = iq.i_SKU(carepackPart)
		//        Dim carepackTrans As clsTranslation = iq.AddTranslation(rdr.Item("CPKpartnum"), English)
		//        carePackBranch = New clsBranch(carepackProduct, carePackRoot, carepackTrans, "", cpstl, cptl, Nothing, 1, False, "B")

		//    Else
		//        ' if we cant find products 
		//        noProducts += 1

		//    End If
		//End While
	}
	//ML - integrated into systeinc
	public void Extras()
	{
		SqlClient.SqlConnection con = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;");
		SqlClient.SqlDataReader rdr;
		string query = "SELECT  [SystemType],[ModelSKU],[FamilyCode],[Extras],[Options]  FROM [iQ].[products].[Systems] where (Extras is not null or Options is not null)";

		rdr = da.DBExecuteReader(con, query);

		if (!iq.i_attribute_code.ContainsKey("Also included")) {
			clsAttribute quickspecAttribute;
			quickspecAttribute = new clsAttribute("Also included", iq.AddTranslation("Also included", English, "", 0, null, 0, false), 0);
		}
		if (!iq.i_attribute_code.ContainsKey("Options")) {
			clsAttribute quickspecAttribute;
			quickspecAttribute = new clsAttribute("Options", iq.AddTranslation("Options", English, "", 0, null, 0, false), 0);
		}

		DataTable dt = new DataTable();
		dt.Load(rdr);

		string systemSku = string.Empty;
		string optionSku = string.Empty;
		foreach ( product in iq.Products.Values) {
			List<clsProductAttribute> perf = null;
			product.i_Attributes_Code.TryGetValue("Also included", perf);
			if (perf == null) {
				DataRow[] drarray;
				string filterExp = "ModelSKU = '" + product.SKU + "'";
				drarray = dt.Select(filterExp);

				if (drarray.Length > 0) {
					clsProductAttribute fn;
					string linkAttributes = string.Empty;
					foreach (DataRow row in drarray) {
						if (!object.ReferenceEquals(row("Extras"), DBNull.Value)) {
							if ((iq.i_SKU.ContainsKey(row("Extras")))) {
								clsProduct prodAlsoIncuded = iq.i_SKU(row("Extras"));
								if (prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc")) {
									fn = new clsProductAttribute(product, iq.i_attribute_code("Also included"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation);
									//  ElseIf prodAlsoIncuded.i_Attributes_Code.ContainsKey("~ame") Then
									//     fn = New clsProductAttribute(product, iq.i_attribute_code("Also included"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("~ame")(0).Translation)
								}
							} else {
								linkAttributes += row("Extras");
								fn = new clsProductAttribute(product, iq.i_attribute_code("Also included"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "extras", 0, null, 0, false));
							}
						}
					}
				}

			}
			perf = null;
			product.i_Attributes_Code.TryGetValue("Options", perf);
			if (perf == null) {
				DataRow[] drarray;
				string filterExp = "ModelSKU = '" + product.SKU + "'";
				drarray = dt.Select(filterExp);

				if (drarray.Length > 0) {
					clsProductAttribute fn;
					string linkAttributes = string.Empty;
					foreach (DataRow row in drarray) {
						if (!object.ReferenceEquals(row("Options"), DBNull.Value)) {
							if ((iq.i_SKU.ContainsKey(row("Options")))) {
								clsProduct prodAlsoIncuded = iq.i_SKU(row("Options"));
								if (prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc")) {
									fn = new clsProductAttribute(product, iq.i_attribute_code("Options"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation);
									//  ElseIf prodAlsoIncuded.i_Attributes_Code.ContainsKey("~ame") Then
									//     fn = New clsProductAttribute(product, iq.i_attribute_code("Also included"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("~ame")(0).Translation)
								}
							} else {
								linkAttributes += row("Options");
								fn = new clsProductAttribute(product, iq.i_attribute_code("Options"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "Options", 0, null, 0, false));
							}
						}
					}
				}

			}
		}


	}
	//ML - integrated into systeinc
	public void Graphics()
	{
		SqlClient.SqlConnection con = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;");
		SqlClient.SqlDataReader rdr;
		string query = "SELECT  distinct [SystemType],[ModelSKU],[DiscreteGraphics],[IntVideo],[InstVGA] FROM [iQ].[products].[Systems] s inner join [iQ].[products].[SysFamilyDefinitions] sf on s.FamilyCode = sf.sysfamily ";

		rdr = da.DBExecuteReader(con, query);

		if (!iq.i_attribute_code.ContainsKey("Graphics")) {
			clsAttribute quickspecAttribute;
			quickspecAttribute = new clsAttribute("Graphics", iq.AddTranslation("Graphics", English, "", 0, null, 0, false), 0);
		}

		DataTable dt = new DataTable();
		dt.Load(rdr);

		string systemSku = string.Empty;
		string optionSku = string.Empty;

		foreach ( product in iq.Products.Values) {
			List<clsProductAttribute> perf = null;
			product.i_Attributes_Code.TryGetValue("Graphics", perf);
			if (perf == null) {
				DataRow[] drarray;
				string filterExp = "ModelSKU = '" + product.SKU + "'";
				drarray = dt.Select(filterExp);

				if (drarray.Length > 0) {
					string linkAttributes = string.Empty;
					foreach (DataRow row in drarray) {
						if (row("DiscreteGraphics") != null && !object.ReferenceEquals(row("DiscreteGraphics"), DBNull.Value)) {
							linkAttributes += row("DiscreteGraphics");
						} else if (row("IntVideo") != null && !object.ReferenceEquals(row("IntVideo"), DBNull.Value)) {
							linkAttributes += row("IntVideo");
						} else if (row("InstVGA") != null && !object.ReferenceEquals(row("InstVGA"), DBNull.Value)) {
							linkAttributes += row("InstVGA");
						}
					}
					clsProductAttribute fn;
					fn = new clsProductAttribute(product, iq.i_attribute_code("Graphics"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "VID", 1, null, 0, false));
				}

			}

		}

	}
	//ML 23/01/2015 added to incremental import

	public void Networking()
	{
		SqlClient.SqlConnection con = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;");
		SqlClient.SqlDataReader rdr;
		string query = " SELECT  distinct [SystemType],[ModelSKU],[WLAN],[WWAN],[InstNIC] FROM [iQ].[products].[Systems] s inner join [iQ].[products].[SysFamilyDefinitions] sf on s.FamilyCode = sf.sysfamily  ";

		rdr = da.DBExecuteReader(con, query);

		if (!iq.i_attribute_code.ContainsKey("Networking")) {
			clsAttribute nwa;
			nwa = new clsAttribute("Networking", iq.AddTranslation("Networking", English, "", 0, null, 0, false), 1);
		}

		DataTable dt = new DataTable();
		dt.Load(rdr);
		List<string> errorMessages = new List<string>();
		string systemSku = string.Empty;
		string optionSku = string.Empty;

		foreach ( product in iq.Products.Values) {
			if (product.SKU == "C5A73ET") {
				object a = 9;
			}
			List<clsProductAttribute> perf = null;
			product.i_Attributes_Code.TryGetValue("Networking", perf);
			//  If perf Is Nothing Then
			DataRow[] drarray;
			string filterExp = "ModelSKU = '" + product.SKU + "'";
			drarray = dt.Select(filterExp);

			if (drarray.Length > 0) {
				string linkAttributes = string.Empty;
				foreach (DataRow row in drarray) {
					if (row("WLAN") != null && !object.ReferenceEquals(row("WLAN"), DBNull.Value)) {
						if ((iq.i_SKU.ContainsKey(row("WLAN")))) {
							clsProductAttribute fn;
							clsProduct prodAlsoIncuded = iq.i_SKU(row("WLAN"));
							if (prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc")) {
								object found = false;
								if (product.i_Attributes_Code.ContainsKey("Networking")) {
									foreach ( a in product.i_Attributes_Code("Networking")) {
										if (a.Translation.text(English) == Left(prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English), Len(prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English)) - 2)) {
											found = true;
											a.Translation.text(English) = prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English);
											a.Translation.Update(English);
										}
										if (a.Translation.text(English) == prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English))
											found = true;
									}
								}
								if (!found)
									fn = new clsProductAttribute(product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation);
							}
						} else {
							linkAttributes += row("WLAN") + ", ";
						}

					}
					if (row("WWAN") != null && !object.ReferenceEquals(row("WWAN"), DBNull.Value)) {
						if ((iq.i_SKU.ContainsKey(row("WWAN")))) {
							clsProductAttribute fn;
							clsProduct prodAlsoIncuded = iq.i_SKU(row("WWAN"));
							if (prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc")) {
								object found = false;
								if (product.i_Attributes_Code.ContainsKey("Networking")) {
									foreach ( a in product.i_Attributes_Code("Networking")) {
										if (a.Translation.text(English) == Left(prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English), Len(prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English)) - 2)) {
											found = true;
											a.Translation.text(English) = prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English);
											a.Translation.Update(English);
										}
										if (a.Translation.text(English) == prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English))
											found = true;
									}
								}
								if (!found)
									fn = new clsProductAttribute(product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation);
							}
						} else {
							linkAttributes += row("WWAN") + ", ";
						}

					}
					if (row("InstNIC") != null && !object.ReferenceEquals(row("InstNIC"), DBNull.Value)) {
						if ((iq.i_SKU.ContainsKey(row("InstNIC")))) {
							clsProductAttribute fn;
							clsProduct prodAlsoIncuded = iq.i_SKU(row("InstNIC"));
							if (prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc")) {
								object found = false;
								if (product.i_Attributes_Code.ContainsKey("Networking")) {
									foreach ( a in product.i_Attributes_Code("Networking")) {
										if (a.Translation.text(English) == Left(prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English), Len(prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English)) - 2)) {
											found = true;
											a.Translation.text(English) = prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English);
											a.Translation.Update(English);
										}
										if (a.Translation.text(English) == prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English))
											found = true;
									}
								}
								if (!found)
									fn = new clsProductAttribute(product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation);
							}
						} else {
							linkAttributes += row("InstNIC");
						}
					}
				}

				if (linkAttributes.Length > 0) {
					//linkAttributes = Left(linkAttributes, Len(linkAttributes))
					clsProductAttribute fn;
					object found = false;
					if (product.i_Attributes_Code.ContainsKey("Networking")) {
						foreach ( a in product.i_Attributes_Code("Networking")) {
							if (a.Translation.text(English) == Left(linkAttributes, Len(linkAttributes) - 2)) {
								found = true;
								a.Translation.text(English) = linkAttributes;
								a.Translation.Update(English);
							}
							if (a.Translation.text(English) == linkAttributes)
								found = true;
						}
					}
					if (!found)
						fn = new clsProductAttribute(product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "NKW", 0, null, 0, false));
				}
			}
			//     End If
		}


	}

	public void ImportPSU()
	{
		SqlClient.SqlConnection con = da.OpenDatabase("Data Source=www.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;");
		SqlClient.SqlDataReader rdr;
		string query = "SELECT distinct [SystemType],[ModelSKU],[FamilyCode],[PSU],[PSUqty], ccDescription FROM [iQ].[products].[Systems]" + "inner join iq.products.HierarchyFull on upcNum = [PSU]  where psu is not null order by FamilyCode";

		rdr = da.DBExecuteReader(con, query);

		if (!iq.i_attribute_code.ContainsKey("PSU")) {
			clsAttribute quickspecAttribute;
			quickspecAttribute = new clsAttribute("PSU", iq.AddTranslation("PSU", English, "", 0, null, 0, false), 0);
		}

		DataTable dt = new DataTable();
		dt.Load(rdr);

		string systemSku = string.Empty;
		string optionSku = string.Empty;
		foreach ( product in iq.Products.Values) {
			List<clsProductAttribute> perf = null;
			product.i_Attributes_Code.TryGetValue("PSU", perf);
			if (perf == null) {
				DataRow[] drarray;
				string filterExp = "ModelSKU = '" + product.SKU + "'";
				drarray = dt.Select(filterExp);
				if (drarray.Length > 0) {
					string linkAttributes = string.Empty;
					clsProductAttribute fn;
					foreach (DataRow row in drarray) {
						if (row("PSU") != null && !object.ReferenceEquals(row("PSU"), DBNull.Value)) {
							if ((iq.i_SKU.ContainsKey(row("PSU")))) {
								clsProduct prodAlsoIncuded = iq.i_SKU(row("PSU"));

								if (prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc")) {
									fn = new clsProductAttribute(product, iq.i_attribute_code("PSU"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation);

									// i might have messed this up .. Nick 
									//ElseIf prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc") Then
									//    fn = New clsProductAttribute(product, iq.i_attribute_code("PSU"), 0, iq.i_unit_code("txt"), iq.AddTranslation(row("ccDescription"), English, "PSU", 0, Nothing, 0, False))

								}
							} else {
								linkAttributes += row("PSU");
								fn = new clsProductAttribute(product, iq.i_attribute_code("PSU"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "PSU", 0, null, 0, false));
							}
						}
					}
				}
			}
		}
	}
	private string capitalise(string l)
	{

		[] w = Split(l);
		//splits at spaces
		foreach ( word in w) {
			Mid(word, 1, 1) = UCase(Mid(word, 1, 1));
		}
		capitalise = Join(w, " ");

	}

	public void MakeLimits(systemPath, optionsku, optionpath, DataTable graftwriteCache, DataTable slotWriteCache, DataTable prunewritecache, ref int nextPruneID, DataTable quantityWriteCache, bool FirstInFamily, Dictionary<string, clsLimit> dicOptlimits,

	Dictionary<string, clsSlotType> dicSlottypes, object dicOptLocalisation, object dicFIOs, string systemSKU, ref int kept, ref int pruned, ref clsBranch chassisBranch, ref clsBranch systemBranch, DataTable FamilyOptionDefs, clsActionList ActionList = null)
	{
		clsPrune aprune;

		string fullpath = systemPath + optionpath;

		if (InStr(fullpath, ".."))
			System.Diagnostics.Debugger.Break();


		clsBranch optionBranch = iq.Branches((int)Split(optionpath, ".").Last);
		clsProduct product = optionBranch.Product;

		string sysSubFamily;
		//comes from the iq.systems.familycode
		sysSubFamily = iq.i_SKU(systemSKU).i_Attributes_Code("FamMinor")(0).Translation.text(English);
		//IMPORTANT for compatibility

		// Dim obn$ = Me.DisplayName(English)
		// If InStr(LCase(obn$), "drive") > 0 Then Stop
		//Dim sku$ = Me.SKU
		//can be removed
		if (product.ID == 610) {
			object a = 1;
		}


		//option limits are specified per broad option type eg HDD (not narrow option family.. NHPLFF2.5)

		if (product.i_Attributes_Code.ContainsKey("optfamily")) {
			string optfamily = product.i_Attributes_Code("optFamily")(0).Translation.text(English);
			string optType = product.i_Attributes_Code("optType")(0).Translation.text(English);

			//option type limits become 'quanitities'  defining minimum and preferred increments for the option
			bool incompat = false;
			if (optType == "HDD") {
				//We need to prune anything not compatibile with the family
				object r = FamilyOptionDefs.Select("SysFamily = '" + sysSubFamily + "'");
				if (r.Length > 0)
					incompat = (!IsDBNull(r(0)("FamilyPriStor")) && r(0)("FamilyPriStor") != optfamily) && (!IsDBNull(r(0)("FamilySecStor")) && r(0)("FamilySecStor") != optfamily) && (!IsDBNull(r(0)("FamilyTerStor")) && r(0)("FamilyTerStor") != optfamily);

			}

			if (!Compatible(optionBranch, sysSubFamily) | incompat) {
				if (optionBranch.Product.SKU == "AJ838A")
					System.Diagnostics.Debugger.Break();
				pruned += 1;
				if (ActionList == null || ActionList.IsGo(optionsku, ActionType.INSERT, ObjectType.Prune, null, fullpath)) {
					aprune = new clsPrune(fullpath, new NullableInt(), "Import - Pruned Incompatible with subfamily", prunewritecache, nextPruneID);
				} else {
					ActionList.Add(optionsku, ActionType.INSERT, ObjectType.Prune, null, fullpath);
				}
			} else {
				object ck = sysSubFamily + "^" + optType + "^" + optfamily;
				//  If InStr(optfamily, "NHP") Then Stop


				//NOTE OPtionsLimits are the 'vague' - per sysfamily limits
				//eg 'dl320's have 2 PSU's (with an icrement of 1)

				//FIOs have the more definite quanitites - but no inccrements 


				clsLimit famLimits = null;
				if (!dicOptlimits.ContainsKey(ck)) {
					// Its' legitimate for there to be no option limits for some combos
					object j = false;
					//          Logit("no limits for " & ck$)
					famLimits = new clsLimit(0, 1, 100, 1, 1);

				} else {
					famLimits = dicOptlimits(ck);
					//get the Qinstalled, Qmax, Qmin, MinIncr, and PrefIncr for this opttion type in this subfamily - For example MEM,1,4,1,2

					if (famLimits.Qinstalled < 0)
						System.Diagnostics.Debugger.Break();
					//this same (option type)  branch is grafted on to every system in the family - so we only need to make the quanities once (even though they're geographically localised - they have global scope ie. no paths)

					//Category option limits does NOT do preinstalled options - it does MINs, Maxes and Increments
					//THAT's a lie - it turns out that OptionLimits are overridden by FIOs
					//qmax.Qinstalled = 0 '@@@

					//makes a quantity record for the region(s) in which this option is available
					//this one handles Increments and Maximums  - NB the path is empty ! (not fullpath)
					//this is making a bunch of pathless quantities - but they're on the branch

					famLimits.Qinstalled = 0;
					MakeLocalisedQuantity(optionBranch, null, famLimits, dicOptLocalisation, quantityWriteCache, "", ActionList);
					//Make quantity limits/increments per country/region
					//isFIO was here - in error
					kept += 1;
					//End If

				}
				//make an (overriding because it has a path) quantity record for each option branch at its specific path if it's a SKUd 
				//factory installed option 
				//NOTE Fios (which come from Products.Systems) Override the quanitites that may be specified in OptionLimits
				//PSU's are a case in point where many of the optionLimits.qtyinstalled are zero but they have the correct inforamtion in 
				//products.systems.PSUqty

				bool fio = isFIO(optionsku, systemSKU, fullpath, dicFIOs, dicOptLocalisation, famLimits, quantityWriteCache, ActionList);
				//was limit

				//we only make takes slots for those options with limits (in the dictionary)
				//Dim optfam As String = optionBranch.Product.i_Attributes_Code("OptFam")(0).Translation.text(English)
				if (product.i_Attributes_Code.ContainsKey("opttype") && product.i_Attributes_Code.ContainsKey("Slots")) {
					object existingSlots = from y in chassisBranch.slots.Valueswhere y.Type.MajorCode == optType;
					if (existingSlots.Count == 0) {
						if (dicSlottypes.ContainsKey(optType + "^" + optfamily)) {
							//Does the chassis (or system) have this slot as a give, if not we need to add one!!! - revised, from Paul, all factory installed should add a slot of its type, soldered parts should become non-removable...
							//This is an odd one, if its a PCI card, it doesnt seem to follow the norm.....   Careful with memory though as it may be on the CPU...
							if (fio && product.ProductType.Code.ToUpper() != "MEM" && product.i_Attributes_Code("Slots").First.NumericValue > 0 && optionBranch.Product.i_Attributes_Code.ContainsKey("opttype") && optionBranch.Product.i_Attributes_Code.ContainsKey("optfamily") && optionBranch.Product.i_Attributes_Code("opttype").First.Translation.text(English).ToUpper() != "PCI") {
								object cb = chassisBranch != null ? chassisBranch : systemBranch.slots.Where(sl => sl.Value.Type.MajorCode == optionBranch.Product.i_Attributes_Code("opttype").First.Translation.text(English) && sl.Value.Type.MinorCode == optionBranch.Product.i_Attributes_Code("optfamily").First.Translation.text(English) && sl.Value.numSlots > 0);

								if (cb.Count == 0) {
									if (ActionList == null || ActionList.IsGo(optionsku, ActionType.INSERT, ObjectType.Slot, chassisBranch != null ? chassisBranch : systemBranch, dicSlottypes(optionBranch.Product.i_Attributes_Code("opttype").First.Translation.text(English) + "^" + optionBranch.Product.i_Attributes_Code("optfamily").First.Translation.text(English)), "", dicFIOs(systemSKU)(optionsku) * product.i_Attributes_Code("Slots").First.NumericValue)) {
										clsSlot AddsSlot = new clsSlot(dicSlottypes(optionBranch.Product.i_Attributes_Code("opttype").First.Translation.text(English) + "^" + optionBranch.Product.i_Attributes_Code("optfamily").First.Translation.text(English)), chassisBranch != null ? chassisBranch : systemBranch, "", dicFIOs(systemSKU)(optionsku) * product.i_Attributes_Code("Slots").First.NumericValue, null, new NullableInt(), 0, 0, slotWriteCache);
									} else {
										ActionList.Add(optionsku, ActionType.INSERT, ObjectType.Slot, chassisBranch != null ? chassisBranch : systemBranch, dicSlottypes(optionBranch.Product.i_Attributes_Code("opttype").First.Translation.text(English) + "^" + optionBranch.Product.i_Attributes_Code("optfamily").First.Translation.text(English)), "", dicFIOs(systemSKU)(optionsku) * product.i_Attributes_Code("Slots").First.NumericValue);
									}
								} else {
									cb.First.Value.numSlots += (dicFIOs(systemSKU)(optionsku) * product.i_Attributes_Code("Slots").First.NumericValue);
								}
							}

							if (optionBranch.slotsGiven("", dicSlottypes(optType + "^" + optfamily)) == 0) {
								int consumes = -product.i_Attributes_Code("Slots")(0).NumericValue;
								//Need to convert PCI slot types back?
								if (ActionList == null || ActionList.IsGo(optionsku, ActionType.INSERT, ObjectType.Slot, chassisBranch != null ? chassisBranch : systemBranch, dicSlottypes(optType + "^" + optfamily), "", consumes)) {
									clsSlot TakesSlot = new clsSlot(dicSlottypes(optType + "^" + optfamily), optionBranch, "", consumes, null, new NullableInt(), 0, 0, slotWriteCache);
								} else {
									ActionList.Add(optionsku, ActionType.INSERT, ObjectType.Slot, chassisBranch != null ? chassisBranch : systemBranch, dicSlottypes(optType + "^" + optfamily), "", consumes);
								}
							} else {
								object j = false;
							}

						}
					}
				}
			}
		}


	}

	private object CompareProduct(clsProduct iq2Prod, clsProduct iq1prod, bool AllowDelete, clsActionList ActionList, bool JustDoIt, ref DataTable awc)
	{
		//Attributes
		List<string> errormessages = new List<string>();
		foreach ( a in iq1prod.i_Attributes_Code.ToList()) {
			if (!iq2Prod.i_Attributes_Code.ContainsKey(a.Key)) {
				//EASY ADD THIS
				foreach ( atr in a.Value) {
					if (JustDoIt)
						object at = new clsProductAttribute(iq2Prod, iq.i_attribute_code(a.Key), atr.NumericValue, atr.Unit, atr.Translation, awc);
					ActionList.Add(iq2Prod.SKU, ActionType.INSERT, ObjectType.Attribute, null, atr);
				}
			}

			if (iq2Prod.i_Attributes_Code(a.Key).Count != a.Value.Count) {
				//Panic and reset all
				//DELETE ALL on iq2 and ADD all of iq1 again
				foreach ( atr in iq2Prod.i_Attributes_Code(a.Key).ToList()) {
					//If JustDoIt Then Dim at = New clsProductAttribute(prod1, iq.i_attribute_code(a.Key), atr.NumericValue, atr.Unit, atr.Translation) - not brave enough for now, needs more testing
					ActionList.Add(iq2Prod.SKU, ActionType.DELETE, ObjectType.Attribute, atr, null);
				}
				foreach ( atr in a.Value.ToList()) {
					if (JustDoIt)
						object at = new clsProductAttribute(iq2Prod, iq.i_attribute_code(a.Key), atr.NumericValue, atr.Unit, atr.Translation, awc);
					ActionList.Add(iq2Prod.SKU, ActionType.INSERT, ObjectType.Attribute, null, atr);
				}
			}

			for (i = 0; i <= a.Value.Count - 1; i++) {
				if (!CompareAttribute(iq2Prod.i_Attributes_Code(a.Key)(i), a.Value(i))) {
					//UPDATE THIS
					if (JustDoIt) {
						a.Value(i).Translation = iq2Prod.i_Attributes_Code(a.Key)(i).Translation;
						a.Value(i).NumericValue = iq2Prod.i_Attributes_Code(a.Key)(i).NumericValue;
						a.Value(i).Unit = iq2Prod.i_Attributes_Code(a.Key)(i).Unit;
						if (a.Value(i).ID != 0) {
							a.Value(i).update(errormessages);
						}

					}
					ActionList.Add(iq2Prod.SKU, ActionType.UPDATE, ObjectType.Attribute, iq2Prod.i_Attributes_Code(a.Key)(i), a.Value(i));
				}
			}
		}
		if (AllowDelete) {
			foreach ( a in iq2Prod.i_Attributes_Code) {
				if (!iq1prod.i_Attributes_Code.ContainsKey(a.Key)) {
					//DELETE 
					foreach ( atr in a.Value.ToList()) {
						ActionList.Add(iq2Prod.SKU, ActionType.DELETE, ObjectType.Attribute, atr, null);
					}
				}
			}
		}

		//Deal with direct product properties... (and dont prompt, just update as these are deemed correct in iq1 for now)
		iq2Prod.EOL = iq1prod.EOL;
		iq2Prod.Active = iq1prod.Active;
		iq2Prod.activeFrom = iq1prod.activeFrom;
		iq2Prod.activeTo = iq1prod.activeTo;

		if (iq2Prod.ID == 0)
			System.Diagnostics.Debugger.Break();
		iq2Prod.update(errormessages);

	}
	public bool CompareAttribute(clsProductAttribute iq2Attr, clsProductAttribute iq1Attr)
	{

		if (iq1Attr.Translation == null & iq2Attr.Translation != null)
			return false;
		if (iq1Attr.Translation != null & iq2Attr.Translation == null)
			return false;

		//for clarity
		if (iq2Attr.Translation != null & iq1Attr.Translation != null) {
			object iq1txt = iq1Attr.Translation.text(English);
			object iq2txt = iq2Attr.Translation.text(English);
			if (LCase(iq2txt) != LCase(iq1txt))
				return false;
		}

		if (iq2Attr.NumericValue != iq1Attr.NumericValue)
			return false;

		return true;
	}
	//Private Sub AddOrUpdateProductAttribute(ActionList As clsActionList, DummyRun As Boolean, DontDelete As Boolean, Inserting As Boolean, Product As clsProduct, clsAttribute As clsAttribute, p3 As Integer, textUnit As clsUnit, clsTranslation As clsTranslation, AttribWriteCache As DataTable)
	//    If Inserting Then
	//        ActionList.Add(Product.sku, ActionType.INSERT, ObjectType.Attribute, clsAttribute.Code, clsTranslation.text(English))
	//        If Not DummyRun Then
	//            Dim FA As clsProductAttribute = New clsProductAttribute(Product, clsAttribute, p3, textUnit, clsTranslation, AttribWriteCache)
	//        End If
	//    Else
	//        Dim cr = CompareAttributeAndLog(Product, clsAttribute, clsTranslation)
	//        Select Case cr
	//            Case ActionType.UPDATE
	//                For Each pa In Product.i_Attributes_Code(clsAttribute.Code)

	//                Next
	//                ActionList.Add(Product.sku, ActionType.INSERT, ObjectType.Attribute, clsAttribute.Code, clsTranslation.text(English))
	//            Case ActionType.INSERT
	//                Dim FA As clsProductAttribute = New clsProductAttribute(Product, clsAttribute, p3, textUnit, clsTranslation, AttribWriteCache)
	//                ActionList.Add(Product.sku, ActionType.INSERT, ObjectType.Attribute, clsAttribute.Code, clsTranslation.text(English))
	//            Case ActionType.DELETE
	//                If Not DontDelete Then

	//                End If
	//        End Select


	//    End If
	//    End If

	//End Sub
	//Function CompareAttributeAndLog(ActionList As clsActionList, Product As clsProduct, clsAttribute As clsAttribute, newValue As clsTranslation) As ActionType
	//    'Ok how do we deal with multiple of the same attribute without the old value?
	//    Dim found As Boolean = False
	//    If Not Product.i_Attributes_Code.ContainsKey(clsAttribute.Code) Then Return ActionType.INSERT
	//    For Each pa In Product.i_Attributes_Code(clsAttribute.Code)
	//        If pa.Translation.text(English) = newValue.text(English) Then found = True
	//    Next
	//    If Not found Then
	//        If Product.i_Attributes_Code(clsAttribute.Code).Count > 1 Then
	//            For Each pa In Product.i_Attributes_Code(clsAttribute.Code)
	//                ActionList.Add(Product.sku, ActionType.DELETE, ObjectType.Attribute, clsAttribute.Code, pa.Translation.text(English))
	//            Next
	//            ActionList.Add(Product.sku, ActionType.INSERT, ObjectType.Attribute, clsAttribute.Code, pa.Translation.text(English))
	//            Return ActionType.UPDATE
	//        Else
	//            ActionList.Add(Product.sku, ActionType.UPDATE, ObjectType.Attribute, clsAttribute.Code, value)
	//            Return ActionType.UPDATE
	//        End If
	//    Else
	//        Return ActionType.NONE
	//    End If

	//End Function

	public DataTable SlowFilledDataTable(SqlConnection con, sql)
	{

		SqlDataAdapter adpt = new SqlDataAdapter(sql, con);
		adpt.SelectCommand.CommandTimeout = 120;
		SlowFilledDataTable = new DataTable();
		adpt.Fill(SlowFilledDataTable);

	}



}

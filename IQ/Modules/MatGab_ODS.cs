using System.IO;
using System.Linq;
using System.Xml;
using Ionic.Zip;
class ODS
{

	public string Filename(string url)
	{

		//Dim s, bs As Integer
		//s = InStrRev(url$, "/")
		//bs = InStrRev(url$, "\")
		//If s = 0 And bs = 0 Then  'in case there's No slashe at all (just a plain filename)
		//    s = 0
		//Else
		//    If bs > s Then
		//        s = bs
		//    End If

		//End If

		//Filename = Mid$(url, s + 1)
		return Path.GetFileName(url);

	}

	public string OutputQuote(clsQuote quote, string writepath, ref List<string> errorMessages)
	{
		//returns the filename of the generated .ODS file


		try {
			//find the virtual, and from that the physical path to the app folder
			object vPath = HttpContext.Current.Request.ApplicationPath;
			object pPath = HttpContext.Current.Request.MapPath(vPath) + "\\";

			string tf;
			string fn = writepath + "\\" + quote.RootQuote.ID + "-" + quote.Version + ".ods";
			tf = pPath + fn;
			try {
				if (My.Computer.FileSystem.FileExists(tf))
					My.Computer.FileSystem.DeleteFile(tf);
			} catch (Exception ex) {
				ErrorLog.Add(ex);
				return tf;
			}


			if (!My.Computer.FileSystem.DirectoryExists(pPath + writepath + "\\media")) {
				errorMessages.Add("TO SUPPORT: we need to make " + pPath + writepath + "\\media folder");
				return string.Empty;
				return;
			}

			string rf;
			System.Net.WebClient webClient = new System.Net.WebClient();
			string sourceFilename;
			string sl = quote.SellerLogo;
			sl = Filename(sl);
			sourceFilename = pPath + writepath + "\\media\\" + sl;
			rf = imagebase + "images/" + quote.SellerLogo;

			Image imgLogo = new Image();
			imgLogo.ImageUrl = rf;


			try {
				webClient.DownloadFile(rf, sourceFilename);
			} catch (System.Exception ex) {
				errorMessages.Add(" Couldn't fetch logo:" + rf + " err :" + ex.Message);
			}

			System.Drawing.Bitmap test;
			test = new System.Drawing.Bitmap(sourceFilename);

			object ht = test.Height;
			object wid = test.Width;

			object ht2 = ht / test.VerticalResolution;
			object wid2 = wid / test.HorizontalResolution;

			My.Computer.FileSystem.CopyFile(pPath + "quoteTemplates\\quote.ods", tf);
			//make a Unique copy of the template ODS file
			string ZipToUnpack = tf;

			//Dim UnpackDirectory As String = "Extracted Files"
			using (ZipFile zip1 = ZipFile.Read(ZipToUnpack)) {

				ZipEntry content = null;
				string tmp = string.Empty;

				foreach (ZipEntry ze in zip1) {
					//ze.Extract(UnpackDirectory, ExtractExistingFileAction.OverwriteSilently)
					if (ze.FileName == "content.xml") {
						content = ze;
						//unpack it - we don't need to do this - we now keep an unpacked copy of the XML content (so it can be part an editable of the VS project)
						//tmp = pPath & "\quotes\" & quote.ID & ".tmp"
						//ze.Extract(tmp, ExtractExistingFileAction.OverwriteSilently)
						break; // TODO: might not be correct. Was : Exit For
					}
				}

				if (!content == null) {
					zip1.RemoveEntry(content);
				}

				string modifiedContent = string.Empty;

				StreamReader sw = new StreamReader(pPath + "quotetemplates\\content.xml");
				//read the template
				string l = sw.ReadToEnd;
				sw.Close();

				// My.Computer.FileSystem.DeleteFile(tmp)


				//Dim ll As Integer = modifiedContent.Length

				modifiedContent = modifiedContent.Replace(vbCrLf, string.Empty);

				XmlDocument quotexmldoc = null;
				modifiedContent = quote.ReplaceTags(l, quotexmldoc, errorMessages, quote.BuyerAccount.Language);
				//replaces the !tags" (with the contents of the the corresponding elements) and returns a reference to the quoteXmlDoc



				modifiedContent = Replace(modifiedContent, "!imgwidth!", wid2.ToString() + "in");
				modifiedContent = Replace(modifiedContent, "!imgheight!", ht2.ToString() + "in");

				//ll = modifiedContent.Length

				zip1.AddEntry("content.xml", modifiedContent, System.Text.Encoding.UTF8);
				//merge the modified template back into the ZIP file - note .. content.xml file is left untouched


				string logofile = Filename(quote.SellerLogo);
				if (!string.IsNullOrWhiteSpace(logofile) & LCase(logofile != "iq.nullablestring")) {
					logofile = pPath + writepath + "/media/" + logofile;
					//prepend the path (in the zipf file)
					zip1.AddItem(logofile, "/media");
				}


				//Find all the advice icons (images) we'll be needing - they have to be embedded in the zip/ods file


				//'select all the advice nodes anywhere under the TreeQuote


				string iconFile = string.Empty;
				List<string> added = new List<string>();
				//we only want to add each icon once - so we track them in a list
				XmlNodeList adviceNodes = quotexmldoc.SelectSingleNode("//TreeQuote").SelectNodes("//Advice");
				foreach (XmlNode i in adviceNodes) {
					if (!string.IsNullOrWhiteSpace(i.InnerXml)) {
						iconFile = pPath + "images\\navigation\\" + i.SelectSingleNode("AdviceIcon").InnerText;
						if (!added.Contains(iconFile)) {
							if (File.Exists(iconFile)) {
								zip1.AddItem(iconFile, "/media");
								added.Add(iconFile);
							}
						}
					}
				}


				zip1.Save();

				return tf;
			}
		} catch (System.Exception ex) {
			ErrorLog.Add(ex);

			errorMessages.Add(ex.Message);

			return "Error ";
		}


	}



}

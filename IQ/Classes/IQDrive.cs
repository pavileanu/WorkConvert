using System;
using System.Diagnostics;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Util;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using System.Threading;
using Google.Apis.Util.Store;
using System.Security.Cryptography.X509Certificates;


public class IQDrive
{
    private const string SERVICE_ACCOUNT_EMAIL = "8998119919-pj9atbiqukbogao708ulgq4oibsvc0a5@developer.gserviceaccount.com";
    private const string SERVICE_ACCOUNT_PKCS12_FILE_PATH = "C:\\Sites\\IQ\\IQ\\drive.p12";



    public static string uploadFile(string filepath, string certpath)
	{
		
		
		try
		{
			
			
			List<string> scope = new List<string>();
			scope.Add(DriveService.Scope.Drive);
			X509Certificate2 certificate = new X509Certificate2(certpath, "notasecret", X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
			ServiceAccountCredential credential = new ServiceAccountCredential(
				new ServiceAccountCredential.Initializer(SERVICE_ACCOUNT_EMAIL)
				{Scopes = scope}FromCertificate(certificate));
				
				
				//Dim credential As UserCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(New ClientSecrets() With {
					//    .ClientId = "8998119919-89rcef4ofs1ph19cv0g589qc1j5kan7b.apps.googleusercontent.com",
					//    .ClientSecret = "IKwPKskHoPJvSMo4SSS_nLon" _
					//}, scope, "user", CancellationToken.None).Result
					
					
					
					// Create the service.
					BaseClientService.Initializer serviceInitialiser = new BaseClientService.Initializer() {
							HttpClientInitializer = credential,
							ApplicationName = "IQDrive"
						};
					
					
					//Dim filearray() As String = Split(filepath, "\")
					//Dim filename As String = filearray(UBound(filearray))
					string filename = System.Convert.ToString(IO.Path.GetFileName(filepath));
					DriveService service = new DriveService(serviceInitialiser);
					
					
					
					string mimetype = "application/x-vnd.oasis.opendocument.spreadsheet";
					
					string googlemimetype = "application/vnd.google-apps.spreadsheet";
					Google.Apis.Drive.v2.Data.File body = new Google.Apis.Drive.v2.Data.File() {
							Title = filename,
							Description = filename + " ODS file",
							MimeType = googlemimetype
						};
					
					byte[] byteArray = System.IO.File.ReadAllBytes(filepath);
					System.IO.MemoryStream stream2 = new System.IO.MemoryStream(byteArray);
					
					FilesResource.InsertMediaUpload request = service.Files.Insert(body, stream2, mimetype);
					
					request.Upload();
					string downloadUrl = "";
					Google.Apis.Drive.v2.Data.File file = request.ResponseBody;
					string pdffilepath = string.Empty;
					if (file != null)
					{
						downloadUrl = file.ExportLinks("application/pdf") + "&portrait=true&size=A4";
						object stream = service.HttpClient.GetStreamAsync(downloadUrl);
						object result = stream.Result;
						// ReDim Preserve filearray(UBound(filearray) - 1)
						//pdffilepath = Join(filearray, "\") & Replace(filename, "ods", "pdf")
						pdffilepath = System.Convert.ToString(IO.Path.GetFullPath(filepath) + IO.Path.GetFileNameWithoutExtension(filepath) + System.Convert.ToString((IO.Path.GetExtension(filepath) ==".ods") ? ".pdf" : (IO.Path.GetExtension(filepath))));
						using (var  fileStream = System.IO.File.Create(pdffilepath))
						{
							
							result.CopyTo(fileStream);
						}
						
						
					}
					return pdffilepath;
					
				}
				catch (Exception ex)
				{
					ErrorLog.Add(ex);
					return "";
					
				}
				
			}
    public static List<File> retrieveAllFiles(DriveService service, string filename)
    {
        List<File> result = new List<File>();
        FilesResource.ListRequest request = service.Files.List();
        request.Q = "title contains \'" + filename + "\'";

        do
        {
            try
            {
                FileList files = request.Execute();

                result.AddRange(files.Items);
                request.PageToken = files.NextPageToken;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
                request.PageToken = null;
            }
        } while (!string.IsNullOrEmpty(System.Convert.ToString(request.PageToken)));
        return result;
    }

    public static bool deleteAllFiles(DriveService service)
    {
        List<File> result = new List<File>();
        FilesResource.ListRequest request = service.Files.List();
        FileList files = request.Execute();
        bool success = false;
        if (files.Items.Count > 100)
        {


            foreach (File seletectedFile in files.Items)
            {
                try
                {
                    service.Files.Delete(seletectedFile.Id).Execute();
                    success = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    request.PageToken = null;
                    success = false;
                }
            }
        }
        return success;
    }

}
Imports System
Imports System.Diagnostics
Imports Google.Apis.Drive.v2
Imports Google.Apis.Drive.v2.Data
Imports Google.Apis.Util
Imports Google.Apis.Services
Imports Google.Apis.Auth.OAuth2
Imports System.Threading
Imports Google.Apis.Util.Store
Imports System.Security.Cryptography.X509Certificates

Public Class IQDrive
    Private Const SERVICE_ACCOUNT_EMAIL As String = "8998119919-pj9atbiqukbogao708ulgq4oibsvc0a5@developer.gserviceaccount.com"
    Private Const SERVICE_ACCOUNT_PKCS12_FILE_PATH As String = "C:\Sites\IQ\IQ\drive.p12"



    Public Shared Function uploadFile(filepath As String, certpath As String) As String

      
        Try

       
        Dim scope = New List(Of String)
        scope.Add(DriveService.Scope.Drive)
            Dim certificate = New X509Certificate2(certpath, "notasecret", X509KeyStorageFlags.MachineKeySet Or X509KeyStorageFlags.PersistKeySet Or X509KeyStorageFlags.Exportable)
        Dim credential As ServiceAccountCredential = New ServiceAccountCredential(
                              New ServiceAccountCredential.Initializer(SERVICE_ACCOUNT_EMAIL) With
                              {.Scopes = scope}.FromCertificate(certificate))


        'Dim credential As UserCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(New ClientSecrets() With {
        '    .ClientId = "8998119919-89rcef4ofs1ph19cv0g589qc1j5kan7b.apps.googleusercontent.com",
        '    .ClientSecret = "IKwPKskHoPJvSMo4SSS_nLon" _
        '}, scope, "user", CancellationToken.None).Result



        ' Create the service.
            Dim serviceInitialiser As BaseClientService.Initializer = New BaseClientService.Initializer() With {
                .HttpClientInitializer = credential,
                .ApplicationName = "IQDrive"
            }


            'Dim filearray() As String = Split(filepath, "\")
            'Dim filename As String = filearray(UBound(filearray))
            Dim filename As String = IO.Path.GetFileName(filepath)
        Dim service = New DriveService(serviceInitialiser)

        

        Dim mimetype As String = "application/x-vnd.oasis.opendocument.spreadsheet"

        Dim googlemimetype As String = "application/vnd.google-apps.spreadsheet"
        Dim body As New Google.Apis.Drive.v2.Data.File() With {
            .Title = filename,
            .Description = filename & " ODS file",
            .MimeType = googlemimetype
        }

        Dim byteArray As Byte() = System.IO.File.ReadAllBytes(filepath)
        Dim stream2 As New System.IO.MemoryStream(byteArray)

        Dim request As FilesResource.InsertMediaUpload = service.Files.Insert(body, stream2, mimetype)

        request.Upload()
        Dim downloadUrl As String
        Dim file As Google.Apis.Drive.v2.Data.File = request.ResponseBody
        Dim pdffilepath As String = String.Empty
        If file IsNot Nothing Then
                downloadUrl = file.ExportLinks("application/pdf") & "&portrait=true&size=A4"
            Dim stream = service.HttpClient.GetStreamAsync(downloadUrl)
            Dim result = stream.Result
                ' ReDim Preserve filearray(UBound(filearray) - 1)
                'pdffilepath = Join(filearray, "\") & Replace(filename, "ods", "pdf")
                pdffilepath = IO.Path.GetFullPath(filepath) & IO.Path.GetFileNameWithoutExtension(filepath) & If(IO.Path.GetExtension(filepath) = ".ods", ".pdf", IO.Path.GetExtension(filepath))
            Using fileStream = System.IO.File.Create(pdffilepath)

                result.CopyTo(fileStream)
            End Using

        End If
            Return pdffilepath

        Catch ex As Exception
            ErrorLog.Add(ex)
            Return ""

        End Try

    End Function
    Public Shared Function retrieveAllFiles(service As DriveService, filename As String) As List(Of File)
        Dim result As New List(Of File)()
        Dim request As FilesResource.ListRequest = service.Files.List()
        request.Q = "title contains '" & filename & "'"

        Do
            Try
                Dim files As FileList = request.Execute()

                result.AddRange(files.Items)
                request.PageToken = files.NextPageToken
            Catch e As Exception
                Console.WriteLine("An error occurred: " + e.Message)
                request.PageToken = Nothing
            End Try
        Loop While Not [String].IsNullOrEmpty(request.PageToken)
        Return result
    End Function

    Public Shared Function deleteAllFiles(service As DriveService) As Boolean
        Dim result As New List(Of File)()
        Dim request As FilesResource.ListRequest = service.Files.List()
        Dim files As FileList = request.Execute()
        Dim success As Boolean = False
        If files.Items.Count > 100 Then


            For Each seletectedFile As File In files.Items
                Try
                    service.Files.Delete(seletectedFile.Id).Execute()
                    success = True
                Catch e As Exception
                    Console.WriteLine("An error occurred: " + e.Message)
                    request.PageToken = Nothing
                    success = False
                End Try
            Next
        End If
        Return success
    End Function

End Class

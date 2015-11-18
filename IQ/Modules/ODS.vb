Imports System.IO
Imports System.Linq
Imports System.Xml
Imports Ionic.Zip
Module ODS

    Public Function Filename(url As String) As String

        'Dim s, bs As Integer
        's = InStrRev(url$, "/")
        'bs = InStrRev(url$, "\")
        'If s = 0 And bs = 0 Then  'in case there's No slashe at all (just a plain filename)
        '    s = 0
        'Else
        '    If bs > s Then
        '        s = bs
        '    End If

        'End If

        'Filename = Mid$(url, s + 1)
        Return Path.GetFileName(url)

    End Function

    Public Function OutputQuote(quote As clsQuote, writepath As String, ByRef errorMessages As List(Of String)) As String 'returns the filename of the generated .ODS file

        Try

            'find the virtual, and from that the physical path to the app folder
            Dim vPath = HttpContext.Current.Request.ApplicationPath
            Dim pPath = HttpContext.Current.Request.MapPath(vPath) & "\"

            Dim tf As String
            Dim fn As String = writepath & "\" & quote.RootQuote.ID & "-" & quote.Version & ".ods"
            tf = pPath & fn
            Try
                If My.Computer.FileSystem.FileExists(tf) Then My.Computer.FileSystem.DeleteFile(tf)
            Catch ex As Exception
                ErrorLog.Add(ex)
                Return tf
            End Try


            If Not My.Computer.FileSystem.DirectoryExists(pPath$ & writepath & "\media") Then
                errorMessages.Add("TO SUPPORT: we need to make " & pPath$ & writepath$ & "\media folder")
                Return String.Empty
                Exit Function
            End If

            Dim rf As String
            Dim webClient As New System.Net.WebClient
            Dim sourceFilename As String
            Dim sl As String = quote.SellerLogo
            sl = Filename(sl)
            sourceFilename = pPath & writepath & "\media\" & sl
            rf = imagebase & "images/" & quote.SellerLogo

            Dim imgLogo As Image = New Image()
            imgLogo.ImageUrl = rf


            Try
                webClient.DownloadFile(rf, sourceFilename)
            Catch ex As System.Exception
                errorMessages.Add(" Couldn't fetch logo:" & rf & " err :" & ex.Message)
            End Try

            Dim test As System.Drawing.Bitmap
            test = New System.Drawing.Bitmap(sourceFilename)

            Dim ht = test.Height
            Dim wid = test.Width

            Dim ht2 = ht / test.VerticalResolution
            Dim wid2 = wid / test.HorizontalResolution

            My.Computer.FileSystem.CopyFile(pPath & "quoteTemplates\quote.ods", tf)  'make a Unique copy of the template ODS file
            Dim ZipToUnpack As String = tf

            'Dim UnpackDirectory As String = "Extracted Files"
            Using zip1 As ZipFile = ZipFile.Read(ZipToUnpack)

                Dim content As ZipEntry = Nothing
                Dim tmp As String = String.Empty

                For Each ze As ZipEntry In zip1
                    'ze.Extract(UnpackDirectory, ExtractExistingFileAction.OverwriteSilently)
                    If ze.FileName = "content.xml" Then
                        content = ze
                        'unpack it - we don't need to do this - we now keep an unpacked copy of the XML content (so it can be part an editable of the VS project)
                        'tmp = pPath & "\quotes\" & quote.ID & ".tmp"
                        'ze.Extract(tmp, ExtractExistingFileAction.OverwriteSilently)
                        Exit For
                    End If
                Next

                If Not content Is Nothing Then
                    zip1.RemoveEntry(content)
                End If

                Dim modifiedContent As String = String.Empty

                Dim sw As New StreamReader(pPath & "quotetemplates\content.xml") 'read the template
                Dim l As String = sw.ReadToEnd
                sw.Close()

                ' My.Computer.FileSystem.DeleteFile(tmp)


                'Dim ll As Integer = modifiedContent.Length

                modifiedContent = modifiedContent.Replace(vbCrLf, String.Empty)

                Dim quotexmldoc As XmlDocument = Nothing
                modifiedContent = quote.ReplaceTags(l, quotexmldoc, errorMessages, quote.BuyerAccount.Language)  'replaces the !tags" (with the contents of the the corresponding elements) and returns a reference to the quoteXmlDoc
                


                modifiedContent = Replace(modifiedContent, "!imgwidth!", wid2.ToString() & "in")
                modifiedContent = Replace(modifiedContent, "!imgheight!", ht2.ToString() & "in")

                'll = modifiedContent.Length

                zip1.AddEntry("content.xml", modifiedContent, System.Text.Encoding.UTF8)  'merge the modified template back into the ZIP file - note .. content.xml file is left untouched


                Dim logofile As String = Filename(quote.SellerLogo)
                If Not String.IsNullOrWhiteSpace(logofile) And LCase(logofile <> "iq.nullablestring") Then
                    logofile = pPath & writepath & "/media/" & logofile 'prepend the path (in the zipf file)
                    zip1.AddItem(logofile, "/media")
                End If


                'Find all the advice icons (images) we'll be needing - they have to be embedded in the zip/ods file


                ''select all the advice nodes anywhere under the TreeQuote


                Dim iconFile As String = String.Empty
                Dim added As List(Of String) = New List(Of String) 'we only want to add each icon once - so we track them in a list
                Dim adviceNodes As XmlNodeList = quotexmldoc.SelectSingleNode("//TreeQuote").SelectNodes("//Advice")
                For Each i As XmlNode In adviceNodes
                    If Not String.IsNullOrWhiteSpace(i.InnerXml) Then
                        iconFile = pPath & "images\navigation\" & i.SelectSingleNode("AdviceIcon").InnerText
                        If Not added.Contains(iconFile) Then
                            If File.Exists(iconFile) Then
                                zip1.AddItem(iconFile, "/media")
                                added.Add(iconFile)
                            End If
                        End If
                    End If
                Next


                zip1.Save()

                Return tf$
            End Using
        Catch ex As System.Exception
            ErrorLog.Add(ex)

            errorMessages.Add(ex.Message)

            Return "Error "
        End Try


    End Function



End Module

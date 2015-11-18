Imports System.IO
Imports System.Net

Public Class Support
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim ba As clsAccount = iq.sesh(Request.QueryString("lid"), "BuyerAccount")

        Dim rq = HttpWebRequest.Create("http://localhost:8080/rest/auth/1/session")
        rq.ContentType = "application/json"
        rq.Method = "POST"


        Try
            Dim streamWriter As StreamWriter = New StreamWriter(rq.GetRequestStream())

            Dim json As String = "{" + String.Format("""username"":""{0}"",""password"":""{1}""", ba.User.Email, ba.User.Accounts.First().Value.Password) + "}"
            streamWriter.Write(json)
            streamWriter.Flush()

            Dim httpResponse As HttpWebResponse = GetWebResponse(rq)
            If httpResponse.StatusCode <> HttpStatusCode.OK Then
                'We dont have a valid user???
                Dim userCheckRequest = HttpWebRequest.Create("http://localhost:8080/rest/api/2/user?username=" + ba.User.Email)
                userCheckRequest.Headers("Authorization") = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("mling:ch4nnelP455"))

                'check if they exist
                Dim userCheckResponse As HttpWebResponse = GetWebResponse(userCheckRequest)
                If userCheckResponse.StatusCode <> HttpStatusCode.OK Then
                    'Add 

                    userCheckRequest = HttpWebRequest.Create("http://localhost:8080/rest/api/2/user")
                    userCheckRequest.ContentType = "application/json"
                    userCheckRequest.Headers("Authorization") = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("mling:ch4nnelP455"))
                    userCheckRequest.Method = "POST"
                    Dim userAddStreamWriter As StreamWriter = New StreamWriter(userCheckRequest.GetRequestStream())
                    json = "{" + String.Format("""name"":""{0}"",""password"":""{1}"",""emailAddress"":""{2}"",""displayName"":""{3}"",""notification"":""{4}""", ba.User.Email, ba.User.Accounts.First().Value.Password, ba.User.Email, ba.User.RealName, "false") + "}"
                    userAddStreamWriter.Write(json)
                    userAddStreamWriter.Flush()

                    userCheckRequest.GetResponse()
                Else
                    'Amend password
                    userCheckRequest = HttpWebRequest.Create("http://localhost:8080/rest/api/2/user/password?username=" & ba.User.Email)
                    userCheckRequest.ContentType = "application/json"
                    userCheckRequest.Headers("Authorization") = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("mling:ch4nnelP455"))
                    userCheckRequest.Method = "PUT"
                    Dim userAddStreamWriter As StreamWriter = New StreamWriter(userCheckRequest.GetRequestStream())
                    json = "{" + String.Format("""password"":""{0}""", ba.User.Accounts.First().Value.Password) + "}"
                    userAddStreamWriter.Write(json)
                    userAddStreamWriter.Flush()

                    userCheckRequest.GetResponse()

                End If
                rq = HttpWebRequest.Create("http://localhost:8080/rest/auth/1/session")
                rq.ContentType = "application/json"
                rq.Method = "POST"

                streamWriter = New StreamWriter(rq.GetRequestStream())

                json = "{" + String.Format("""username"":""{0}"",""password"":""{1}""", ba.User.Email, ba.User.Accounts.First().Value.Password) + "}"
                streamWriter.Write(json)
                streamWriter.Flush()
                httpResponse = GetWebResponse(rq)
            End If


            Dim StreamReader As StreamReader = New StreamReader(httpResponse.GetResponseStream())
            Dim s As String = StreamReader.ReadToEnd()
            'Response.Write(s)

            Response.SetCookie(New HttpCookie("JSESSIONID", s.Substring(s.IndexOf("JSESSIONID") + 21, s.IndexOf("""}", s.IndexOf("JSESSIONID")) - s.IndexOf("JSESSIONID") - 21)))
            Response.Redirect("http://localhost:8080/servicedesk/customer/portal/2/user/login?destination=portal%2F2", False)
        Catch ex As System.Net.WebException
            Response.Write("Cannot contact helpdesk!")
        End Try


    End Sub
    Public Function GetWebResponse(req As HttpWebRequest) As HttpWebResponse
        Try
            Return req.GetResponse()
        Catch ex As WebException
            Dim resp As HttpWebResponse = ex.Response
            If resp Is Nothing Then Throw ex
            Return resp
        End Try

    End Function
End Class
using System.IO;
using System.Net;

public class Support : System.Web.UI.Page
{

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		clsAccount ba = iq.sesh(Request.QueryString("lid"), "BuyerAccount");

		object rq = HttpWebRequest.Create("http://localhost:8080/rest/auth/1/session");
		rq.ContentType = "application/json";
		rq.Method = "POST";


		try {
			StreamWriter streamWriter = new StreamWriter(rq.GetRequestStream());

			string json = "{" + string.Format("\"username\":\"{0}\",\"password\":\"{1}\"", ba.User.Email, ba.User.Accounts.First().Value.Password) + "}";
			streamWriter.Write(json);
			streamWriter.Flush();

			HttpWebResponse httpResponse = GetWebResponse(rq);
			if (httpResponse.StatusCode != HttpStatusCode.OK) {
				//We dont have a valid user???
				object userCheckRequest = HttpWebRequest.Create("http://localhost:8080/rest/api/2/user?username=" + ba.User.Email);
				userCheckRequest.Headers("Authorization") = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("mling:ch4nnelP455"));

				//check if they exist
				HttpWebResponse userCheckResponse = GetWebResponse(userCheckRequest);
				if (userCheckResponse.StatusCode != HttpStatusCode.OK) {
					//Add 

					userCheckRequest = HttpWebRequest.Create("http://localhost:8080/rest/api/2/user");
					userCheckRequest.ContentType = "application/json";
					userCheckRequest.Headers("Authorization") = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("mling:ch4nnelP455"));
					userCheckRequest.Method = "POST";
					StreamWriter userAddStreamWriter = new StreamWriter(userCheckRequest.GetRequestStream());
					json = "{" + string.Format("\"name\":\"{0}\",\"password\":\"{1}\",\"emailAddress\":\"{2}\",\"displayName\":\"{3}\",\"notification\":\"{4}\"", ba.User.Email, ba.User.Accounts.First().Value.Password, ba.User.Email, ba.User.RealName, "false") + "}";
					userAddStreamWriter.Write(json);
					userAddStreamWriter.Flush();

					userCheckRequest.GetResponse();
				} else {
					//Amend password
					userCheckRequest = HttpWebRequest.Create("http://localhost:8080/rest/api/2/user/password?username=" + ba.User.Email);
					userCheckRequest.ContentType = "application/json";
					userCheckRequest.Headers("Authorization") = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("mling:ch4nnelP455"));
					userCheckRequest.Method = "PUT";
					StreamWriter userAddStreamWriter = new StreamWriter(userCheckRequest.GetRequestStream());
					json = "{" + string.Format("\"password\":\"{0}\"", ba.User.Accounts.First().Value.Password) + "}";
					userAddStreamWriter.Write(json);
					userAddStreamWriter.Flush();

					userCheckRequest.GetResponse();

				}
				rq = HttpWebRequest.Create("http://localhost:8080/rest/auth/1/session");
				rq.ContentType = "application/json";
				rq.Method = "POST";

				streamWriter = new StreamWriter(rq.GetRequestStream());

				json = "{" + string.Format("\"username\":\"{0}\",\"password\":\"{1}\"", ba.User.Email, ba.User.Accounts.First().Value.Password) + "}";
				streamWriter.Write(json);
				streamWriter.Flush();
				httpResponse = GetWebResponse(rq);
			}


			StreamReader StreamReader = new StreamReader(httpResponse.GetResponseStream());
			string s = StreamReader.ReadToEnd();
			//Response.Write(s)

			Response.SetCookie(new HttpCookie("JSESSIONID", s.Substring(s.IndexOf("JSESSIONID") + 21, s.IndexOf("\"}", s.IndexOf("JSESSIONID")) - s.IndexOf("JSESSIONID") - 21)));
			Response.Redirect("http://localhost:8080/servicedesk/customer/portal/2/user/login?destination=portal%2F2", false);
		} catch (System.Net.WebException ex) {
			Response.Write("Cannot contact helpdesk!");
		}


	}
	public HttpWebResponse GetWebResponse(HttpWebRequest req)
	{
		try {
			return req.GetResponse();
		} catch (WebException ex) {
			HttpWebResponse resp = ex.Response;
			if (resp == null)
				throw ex;
			return resp;
		}

	}
}

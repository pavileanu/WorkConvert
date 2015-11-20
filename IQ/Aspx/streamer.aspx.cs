
using System.Net.Mail;


public class streamer : System.Web.UI.Page
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		//iq.sesh(lid,"tostream") = fullpath$
		//Response.Redirect("streamer.aspx")

		UInt64 lid = Request.QueryString("lid");
		string systemSKU = iq.sesh(lid, "systemSKU");
		bool boolToolsCSVExport = iq.sesh(lid, "toolsCSVExport");

		object fn;
		fn = iq.sesh(lid, "tostream");

		if (!My.Computer.FileSystem.FileExists(fn)) {
			Response.Write("We're sorry - there seemed to be a problem generating that file" + fn + " , The support team have been notified and will fix it shortly.");

			System.Net.Mail.SmtpClient smtpclient = new System.Net.Mail.SmtpClient();

			MailMessage msg;
			msg = new MailMessage(iq.Addresses("iQuoteSupportEmail").Translation.text(English), "nick.axworthy@channelcentral.net", "It's all gone horribly wrong (well a small bit has anyway)", "The PDF Generator (PDFgen) failed to convert " + fn + " in a timely manner.. Please check it is running on the server desktop.");
			msg.ReplyToList.Add(new MailAddress("dan.mason@channelcentral.net"));
			msg.IsBodyHtml = true;
			msg.Priority = MailPriority.High;
			smtpclient.ServicePoint.MaxIdleTime = 1;
			smtpclient.Send(msg);

		} else {
			Response.Clear();
			string FileName = "";
			if ((boolToolsCSVExport)) {
				int RandomNumber;
				RandomNumber = new Random().Next(999999);
				object RandomNumberStr = RandomNumber.ToString("000000");
				FileName = systemSKU + "_" + RandomNumberStr + Right(fn, 4);
				iq.sesh(lid, "toolsCSVExport") = false;
			} else {
				FileName = "Export" + Now.ToString("yyyyMMddHHmmss") + Right(fn, 4);
			}

			Response.AppendHeader("Content-Disposition", "attachment;filename=" + FileName);
			Response.ContentType = iq.sesh(lid, "streamcontent-type");
			// Request("streamcontent-type") "text/xml"
			//"application/vnd.ms-excel;charset=UTF-8"

			System.IO.FileInfo fi;
			fi = My.Computer.FileSystem.GetFileInfo(fn);

			Response.AddHeader("Content-Length", fi.Length.ToString());

			Response.Flush();
			Response.WriteFile(fn);


			Response.End();

			if (iq.sesh(lid, "DeleteStreamed") == true) {
				try {
					My.Computer.FileSystem.DeleteFile(fn);

				} catch {
				}
			}
		}

	}

}

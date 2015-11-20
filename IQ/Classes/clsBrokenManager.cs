using System.ServiceModel;


public class clsBrokerManager
{
    public clsBrokerManager()
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        Id = Guid.Empty;

    }
    public static Guid Id; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.

    public static void Update(string Path, List<Tuple<string, string, object, int?>> Properties)
    {
        //Try
        using (iQuoteBrokerService.IiQuoteBrokerServiceClient client = new iQuoteBrokerService.IiQuoteBrokerServiceClient())
        {
            if (!client.UpdateObject(Id, Path, Properties, DateTime.Now))
            {
                //Check we are registered
                Register();
                if (!client.UpdateObject(Id, Path, Properties, DateTime.Now))
                {
                    //Error state
                    throw (new Exception("Broker Down"));
                }
            }
        }

        //Catch ex As System.Exception
        //End Try
    }

    public static void Register()
    {
        if (System.Configuration.ConfigurationManager.AppSettings("Guid") != null)
        {
            Id = Guid.Parse(System.Configuration.ConfigurationManager.AppSettings("Guid"));
        }
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
            object config = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~");
            KeyValueConfigurationCollection appSettings = config.AppSettings.Settings;
            appSettings.Add("Guid", Id.ToString());
            // appSettings("Guid").Value = Id.ToString()
            config.Save(ConfigurationSaveMode.Modified);

        }
        using (iQuoteBrokerService.IiQuoteBrokerServiceClient client = new iQuoteBrokerService.IiQuoteBrokerServiceClient())
        {
            client.RegisterParticipant(Id, Environment.MachineName, "http://localhost:8021/services/iQuoteBrokerClient.svc", false, false);
        }

    }


}
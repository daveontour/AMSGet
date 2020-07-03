using System;
using System.Configuration;

namespace AMSUtilLib
{
    public class Parameters
    {

        public static string TOKEN;
        public static string AMS_REST_SERVICE_URI;
        public static string AMS_WEB_SERVICE_URI;
        public static string APT_CODE;
        static Parameters()
        {

            Configuration myDllConfig = ConfigurationManager.OpenExeConfiguration(typeof(Parameters).Assembly.Location);
            AppSettingsSection myDllConfigAppSettings = (AppSettingsSection)myDllConfig.GetSection("appSettings");
            try
            {

                APT_CODE = myDllConfigAppSettings.Settings["IATAAirportCode"].Value;
                TOKEN = myDllConfigAppSettings.Settings["Token"].Value;
                AMS_REST_SERVICE_URI = myDllConfigAppSettings.Settings["AMSRestServiceURI"].Value;
                AMS_WEB_SERVICE_URI = myDllConfigAppSettings.Settings["AMSWebServiceURI"].Value;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

}

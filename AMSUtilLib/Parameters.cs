using System;
using System.Configuration;

namespace AMSUtilLib {
    public class Parameters {

        public static string TOKEN;
        public static string AMS_REST_SERVICE_URI;
        public static string AMS_WEB_SERVICE_URI;
        public static string APT_CODE;
        public static string FLIGHT_QUERY_API_URI;
        public static string FLIGHT_QUERY_API_USER;
        public static string FLIGHT_QUERY_API_PASSWORD;
        public static bool USE_FLIGHT_QUERY_API;
        public static string SAVE_JOB_CRON;
        public static string SAVE_JOB_OUTPUT_FOLDER;
        public static string SAVE_JOB_OUTPUT_BASEFILENAME;
        public static string SAVE_JOB_OUTPUT_FILETIMEFORMAT;
        public static string SAVE_JOB_SETS;

        static Parameters() {

            Configuration myDllConfig = ConfigurationManager.OpenExeConfiguration(typeof(Parameters).Assembly.Location);
            AppSettingsSection myDllConfigAppSettings = (AppSettingsSection)myDllConfig.GetSection("appSettings");
            try {

                APT_CODE = myDllConfigAppSettings.Settings["IATAAirportCode"].Value;
                TOKEN = myDllConfigAppSettings.Settings["Token"].Value;
                AMS_REST_SERVICE_URI = myDllConfigAppSettings.Settings["AMSRestServiceURI"].Value;
                AMS_WEB_SERVICE_URI = myDllConfigAppSettings.Settings["AMSWebServiceURI"].Value;
                FLIGHT_QUERY_API_URI = myDllConfigAppSettings.Settings["FlightQueryAPIURI"].Value;
                FLIGHT_QUERY_API_USER = myDllConfigAppSettings.Settings["FlightQueryAPIUser"].Value;
                FLIGHT_QUERY_API_PASSWORD = myDllConfigAppSettings.Settings["FlightQueryAPIPassword"].Value;

            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
                SAVE_JOB_CRON = myDllConfigAppSettings.Settings["SaveJobCron"].Value;
                SAVE_JOB_OUTPUT_FOLDER = myDllConfigAppSettings.Settings["SaveJobOutputFolder"].Value;
                SAVE_JOB_SETS = myDllConfigAppSettings.Settings["SaveJobSets"].Value;
                SAVE_JOB_OUTPUT_BASEFILENAME = myDllConfigAppSettings.Settings["SaveJobOutputBaseFile"].Value;
                SAVE_JOB_OUTPUT_FILETIMEFORMAT = myDllConfigAppSettings.Settings["SaveJobOutputBaseFileTimeFormat"].Value;

            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            try {
                USE_FLIGHT_QUERY_API = bool.Parse(myDllConfigAppSettings.Settings["UseFlightQueryAPI"].Value);
            } catch (Exception) {
                USE_FLIGHT_QUERY_API = false;
            }
        }
    }

}

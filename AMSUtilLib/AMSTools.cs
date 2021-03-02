using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WorkBridge.Modules.AMS.AMSIntegrationAPI.Mod.Intf.DataTypes;
using WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv;

namespace AMSUtilLib {
    public class AMSTools {

        public static BasicHttpBinding GetWSBinding() {
            BasicHttpBinding binding = new BasicHttpBinding();

            binding.MaxReceivedMessageSize = 200000000;
            binding.MaxBufferSize = 200000000;
            binding.MaxBufferSize = 200000000;

            return binding;
        }

        public static EndpointAddress GetWSEndPoint() {
            EndpointAddress address = new EndpointAddress(Parameters.AMS_WEB_SERVICE_URI);
            return address;
        }

        public static FlightId GetFlightID(bool arr, string airline, string fltnum, double offset = 0.0) {


            LookupCode apCode = new LookupCode();
            apCode.codeContextField = CodeContext.IATA;
            apCode.valueField = Parameters.APT_CODE;
            LookupCode[] ap = { apCode };

            LookupCode alCode = new LookupCode();
            alCode.codeContextField = CodeContext.IATA;
            alCode.valueField = airline; ;
            LookupCode[] al = { alCode };


            FlightId flightID = new FlightId();
            flightID.flightKindField = arr ? FlightKind.Arrival : FlightKind.Departure;
            flightID.airportCodeField = ap;
            flightID.airlineDesignatorField = al;
            flightID.scheduledDateField = DateTime.Now.AddDays(offset);
            flightID.flightNumberField = fltnum;

            return flightID;

        }
        public static string PrintXML(string xml) {
            string result = "";

            MemoryStream mStream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.Unicode);
            XmlDocument document = new XmlDocument();

            try {
                // Load the XmlDocument with the XML.
                document.LoadXml(xml);

                writer.Formatting = Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                mStream.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader sReader = new StreamReader(mStream);

                // Extract the text from the StreamReader.
                string formattedXml = sReader.ReadToEnd();

                result = formattedXml;
            } catch (XmlException) {
                // Handle the exception
            }

            mStream.Close();
            writer.Close();

            return result;
        }

        public async static Task<bool> IsAMSWebServiceAvailable() {
            {
                string query = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ams6=""http://www.sita.aero/ams6-xml-api-webservice"">
   <soapenv:Header/>
   <soapenv:Body>
      <ams6:GetAvailableHomeAirportsForLogin>
         <ams6:token>@token</ams6:token>
      </ams6:GetAvailableHomeAirportsForLogin>
   </soapenv:Body>
</soapenv:Envelope>";

                query = query.Replace("@token", Parameters.TOKEN);

                try {
                    using (var client = new HttpClient()) {

                        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, Parameters.AMS_WEB_SERVICE_URI) {
                            Content = new StringContent(query, Encoding.UTF8, "text/xml")
                        };
                        requestMessage.Headers.Add("SOAPAction", "http://www.sita.aero/ams6-xml-api-webservice/IAMSIntegrationService/GetAvailableHomeAirportsForLogin");
                        using (HttpResponseMessage response = await client.SendAsync(requestMessage)) {
                            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.NoContent) {
                                return true;
                            } else {
                                Console.WriteLine($"AMS Access Problem. Retrieval Error: {response.StatusCode}");
                                return false;
                            }
                        }
                    }
                } catch (Exception) {
                    return false;
                }
            }
        }

        public static bool IsAMSRestServiceAvailable() {
            string res = GetRestURI(Parameters.AMS_REST_SERVICE_URI + "/GlobalVariableDefinitions").Result;
            if (res != "ERROR") {
                return true;
            } else {
                return false;
            }
        }

        public static async Task<string> GetRestURI(string uri) {

            try {
                HttpClient _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("Authorization", Parameters.TOKEN);

                using (var result = await _httpClient.GetAsync(uri)) {
                    string content = await result.Content.ReadAsStringAsync();
                    return content;
                }
            } catch (Exception) {
                return "ERROR";
            }
        }

        public static List<FlightRecord> GetFlightRecords(int fromHoursOffset, int toHoursOffset) {
            var list = new List<FlightRecord>();
            XmlElement flightsXML = GetFlightsXML(fromHoursOffset, toHoursOffset);

            if (Parameters.USE_FLIGHT_QUERY_API) {
                // Process the messages from Flight Query Cache (AIP)
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(flightsXML.OwnerDocument.NameTable);
                nsmgr.AddNamespace("aip", "http://www.sita.aero/aip/XMLSchema");
                nsmgr.AddNamespace("ns2", "http://www.w3.org/2001/12/soap-envelope");
                // Create the flight records and add them to the stands. Position them horizontally.
                foreach (XmlElement el in flightsXML.SelectNodes("//aip:FlightData", nsmgr)) {
                    {
                        FlightRecord flight = new FlightRecord(el, nsmgr);
                        list.Add(flight);
                    }
                }

            } else {
                // Process the messages from AMS Direct (AMSx)

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(flightsXML.OwnerDocument.NameTable);
                nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");
                // Create the flight records and add them to the stands. Position them horizontally.
                foreach (XmlElement el in flightsXML.SelectNodes("//ams:Flights/ams:Flight", nsmgr)) {
                    {
                        FlightRecord flight = new FlightRecord(el, nsmgr);
                        list.Add(flight);
                    }
                }
            }

            return list;
        }

        public static XmlElement GetFlightsXML(int fromHoursOffset, int toHoursOffset) {

            if (Parameters.USE_FLIGHT_QUERY_API) {
                XmlDocument doc = new XmlDocument();
                doc.Load("response.xml");
                return doc.DocumentElement;
            }


            using (AMSIntegrationServiceClient client = new AMSIntegrationServiceClient(AMSTools.GetWSBinding(), AMSTools.GetWSEndPoint())) {
                XmlElement res = client.GetFlights(Parameters.TOKEN, DateTime.Now.AddHours(fromHoursOffset), DateTime.Now.AddHours(toHoursOffset), Parameters.APT_CODE, AirportIdentifierType.IATACode);
                return res;
            }
        }

        public static string GetAirports() {
            using (AMSIntegrationServiceClient client = new AMSIntegrationServiceClient(AMSTools.GetWSBinding(), AMSTools.GetWSEndPoint())) {

                try {
                    XmlElement res = client.GetAirports(Parameters.TOKEN);
                    return res.OuterXml;

                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }

            return null;
        }

        public static string GetAircrafts() {
            using (AMSIntegrationServiceClient client = new AMSIntegrationServiceClient(AMSTools.GetWSBinding(), AMSTools.GetWSEndPoint())) {

                try {
                    XmlElement res = client.GetAircrafts(Parameters.TOKEN);
                    return res.OuterXml;

                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }

            return null;
        }

        public static string GetAircraftTypes() {
            using (AMSIntegrationServiceClient client = new AMSIntegrationServiceClient(AMSTools.GetWSBinding(), AMSTools.GetWSEndPoint())) {

                try {
                    XmlElement res = client.GetAircraftTypes(Parameters.TOKEN);
                    return res.OuterXml;

                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }

            return null;
        }

        public static string GetAirlines(bool csv = false) {
            using (AMSIntegrationServiceClient client = new AMSIntegrationServiceClient(AMSTools.GetWSBinding(), AMSTools.GetWSEndPoint())) {

                try {
                    XmlElement res = client.GetAirlines(Parameters.TOKEN);
                    if (csv) {
                        return ConvertToCSV(res);
                    } else {
                        return res.OuterXml;
                    }

                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }

            return null;
        }

        private static string ConvertToCSV(XmlElement xml) {


            foreach (XmlNode node in xml.SelectNodes("//Value")) {

                String att = null;

                if (node.Attributes.Count > 0) {
                    XmlAttribute a = node.Attributes[0];
                    att = a.Name + ":" + a.Value;
                }

                try {
                    if (node.ChildNodes[0].NodeType == XmlNodeType.Text) {
                        Console.WriteLine($"{node.Name}{att}: {node.InnerText}");
                    }
                } catch (Exception) { }
            }

            return null;
        }

        public static void Out(string text, string fileName) {
            if (fileName == null) {
                Console.WriteLine(text);
            } else {
                using (StreamWriter sw = File.AppendText(fileName)) {
                    sw.WriteLine(text);
                }
            }
        }
        public static bool FileOK(string fileName) {
            if (fileName == null) {
                return true;
            }

            if (!File.Exists(fileName)) {
                return true;
            } else {
                Console.Write($"File {fileName} exists. OK to overwrite? (Y/n)");
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.KeyChar == 'n' || key.KeyChar == 'N') {
                    return false;
                } else {
                    Console.WriteLine("");
                    File.Delete(fileName);
                    return true;
                }
            }
        }

        public static bool SaveToFile(string content, string filename, bool convertToXMLPrettyPrint = false) {
            if (convertToXMLPrettyPrint) {
                content = PrintXML(content);
            }

            return true;
        }
    }

}

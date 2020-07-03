using AMSUtilLib;
using CommandLine;
using CommandLine.Text;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using WorkBridge.Modules.AMS.AMSIntegrationAPI.Mod.Intf.DataTypes;
using WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv;

namespace AMSGet
{
    class Program
    {
        private static List<CSVRule> csvFormatList = new List<CSVRule>();

        [Verb("flights", HelpText = "Get a set of flights")]
        public class FlightsOptions
        {
            [Option('a', "airline", Required = false, HelpText = @"IATA airline code(s) of flights to retrieve")]
            public string Airline { get; set; }

            [Option(SetName = "tomorrow", Required = false, HelpText = "Retrieve flights for tomorrow")]
            public bool Tomorrow { get; set; }

            [Option(SetName = "today", Required = false, HelpText = "Retrieve flights for today")]
            public bool Today { get; set; }

            [Option(SetName = "yesterday", Required = false, HelpText = "Retrieve flights from yesterday")]
            public bool Yesterday { get; set; }

            [Option(SetName = "period", Required = false, HelpText = @"Start of period to retrieve flights e.g. 2020/06/15")]
            public string From { get; set; }

            [Option(SetName = "period", Required = false, HelpText = @"End of period to retrieve flights e.g. 2020/07/15")]
            public string To { get; set; }

            [Option('f', "file", Required = false, HelpText = "File to save output to.")]
            public string FileName { get; set; }

            [Option('c', "csv", Required = false, Default = false, HelpText = "Output in CSV format.")]
            public bool CSV { get; set; }

        }

        [Verb("flight", HelpText = "Get a specfic flight")]
        public class FlightOptions
        {

            [Value(0, MetaName = "airline", Required = true, HelpText = "The airline IATA Code")]
            public string Airline { get; set; }

            [Value(1, MetaName = "fltNum", Required = true, HelpText = "The flight Number")]
            public string FlightNum { get; set; }

            [Option(SetName = "tomorrow", Required = false, HelpText = "Retrieve flight for tomorrow")]
            public bool Tomorrow { get; set; }

            [Option(SetName = "today", Required = false, HelpText = "Retrieve flight for today")]
            public bool Today { get; set; }

            [Option(SetName = "yesterday", Required = false, HelpText = "Retrieve flight from yesterday")]
            public bool Yesterday { get; set; }

            [Option(SetName = "period", Required = false, HelpText = @"Start of period to retrieve flight e.g. 2020/06/15")]
            public string From { get; set; }

            [Option(SetName = "period", Required = false, HelpText = @"End of period to retrieve flight e.g. 2020/07/15")]
            public string To { get; set; }

            [Option('f', "file", Required = false, HelpText = "File to save output to.")]
            public string FileName { get; set; }
        }


        [Verb("stand", HelpText = "Get a specfic stand")]
        public class StandOptions
        {

            [Value(0, MetaName = "stand", HelpText = "The Stand to retrieve")]
            public string Stand { get; set; }

            [Option('f', "file", Required = false, HelpText = "File to save output to.")]
            public string FileName { get; set; }
        }

        [Verb("stands", HelpText = "Get the stands")]
        public class StandsOptions
        {

            [Option('f', "file", Required = false, HelpText = "File to save output to.")]
            public string FileName { get; set; }
        }

        [Verb("gates", HelpText = "Get the gates")]
        public class GatesOptions
        {

            [Option('f', "file", Required = false, HelpText = "File to save output to.")]
            public string FileName { get; set; }
        }

        [Verb("airports", HelpText = "Get the airports")]
        public class AirportsOptions
        {

            [Option('f', "file", Required = false, HelpText = "File to save output to.")]
            public string FileName { get; set; }
        }

        [Verb("aircrafts", HelpText = "Get the aircraft")]
        public class AircraftsOptions
        {

            [Option('f', "file", Required = false, HelpText = "File to save output to.")]
            public string FileName { get; set; }
        }

        [Verb("actypes", HelpText = "Get the aircraft types")]
        public class AircraftTypesOptions
        {

            [Option('f', "file", Required = false, HelpText = "File to save output to.")]
            public string FileName { get; set; }
        }

        [Verb("airlines", HelpText = "Get the airlines")]
        public class AirlinesOptions
        {

            [Option('f', "file", Required = false, HelpText = "File to save output to.")]
            public string FileName { get; set; }

            [Option('c', "csv", Required = false, Default = false, HelpText = "Output in CSV format.")]
            public bool CSV { get; set; }
        }

        [Verb("towings", HelpText = "Get towing events")]
        public class TowingsOptions
        {

            [Option(SetName = "tomorrow", Required = false, HelpText = "Retrieve towing events for tomorrow")]
            public bool Tomorrow { get; set; }

            [Option(SetName = "today", Required = false, HelpText = "Retrieve towing events for today")]
            public bool Today { get; set; }

            [Option(SetName = "yesterday", Required = false, HelpText = "Retrieve towing events from yesterday")]
            public bool Yesterday { get; set; }

            [Option(SetName = "period", Required = false, HelpText = @"Start of period to retrieve towing events e.g. 2020/06/15")]
            public string From { get; set; }

            [Option(SetName = "period", Required = false, HelpText = @"End of period to retrieve towing events e.g. 2020/07/15")]
            public string To { get; set; }

            [Option('f', "file", Required = false, HelpText = "File to save output to.")]
            public string FileName { get; set; }

            [Option('c', "csv", Required = false, Default = false, HelpText = "Output in CSV format.")]
            public bool CSV { get; set; }
        }


        [Verb("config", isDefault: true, HelpText = "Show the configuration")]
        public class Options
        {
            [Option('c', "configuration", Required = false, Default = true, HelpText = "Show the configuration parameters")]
            public bool Verbose { get; set; }

            [Option('a', "ams", Required = false, HelpText = "Check the connection to AMS")]
            public bool CheckAMS { get; set; }
        }
        static void Main(string[] args)
        {
            try
            {
                ReadRules();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
#if DEBUG
            string[] arr = { "flights", "--from", "2020/06/01", "--to", "2020/06/30", "--csv" };
            MyMain(arr);
            Console.WriteLine("Done");
            Console.ReadKey();
#else
            MyMain(args);
#endif

        }

        static void MyMain(string[] args)
        {

            var parser = new Parser(with => with.HelpWriter = null);

            var parserResult = parser.ParseArguments<Options, AirportsOptions, TowingsOptions, AircraftTypesOptions, AirlinesOptions, AircraftsOptions, GatesOptions, StandOptions, StandsOptions, FlightOptions, FlightsOptions>(args);

            parserResult.WithParsed<Options>(opts => ShowConfig(opts))
               .WithParsed<GatesOptions>(opts => GetGates(opts))
               .WithParsed<AirportsOptions>(opts => GetAirports(opts))
               .WithParsed<AirlinesOptions>(opts => GetAirlines(opts))
               .WithParsed<AircraftTypesOptions>(opts => GetAircraftTypes(opts))
               .WithParsed<AircraftsOptions>(opts => GetAircrafts(opts))
               .WithParsed<StandOptions>(opts => GetStand(opts))
               .WithParsed<StandsOptions>(opts => GetStands(opts))
               .WithParsed<FlightOptions>(opts => GetFlight(opts))
               .WithParsed<FlightsOptions>(opts => GetFlights(opts))
               .WithParsed<TowingsOptions>(opts => GetTowings(opts))
               .WithNotParsed(errs => DisplayHelp(parserResult, errs));


        }

        static Func<string> dynamicData = () =>
        {
            var line5 = "\nExamples:\n\namsget help flights\namsget flights --today\namsget flights --from 2020/06/30 --to 2020/07/07\namsget flight QF 001 --yesterday\namsget stand A10";
            return $"{line5}";
        };


        static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.Heading = "\nAMSGet - Utility to get data quickly from SITA AMS";
                h.Copyright = "Copyright 2020";
                h.AdditionalNewLineAfterOption = false;
                h.AddPostOptionsLine(dynamicData());
                h.AutoHelp = true;
                h.AutoVersion = false;
                h.MaximumDisplayWidth = 400;
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }
            );
            Console.WriteLine(helpText);
        }
        private static void ShowConfig(Options opts)
        {
            if (opts.Verbose)
            {
                Console.WriteLine($"\nAMS Rest Server URI:         {Parameters.AMS_REST_SERVICE_URI}");
                Console.WriteLine($"AMS Web Services Server URI: {Parameters.AMS_WEB_SERVICE_URI}");
                Console.WriteLine($"AMS Access Token:            {Parameters.TOKEN}");
                Console.WriteLine($"Airport Code:                {Parameters.APT_CODE}");
            }

            if (opts.CheckAMS)
            {
                Console.WriteLine("\nChecking connection to AMS..");
                if (AMSTools.IsAMSWebServiceAvailable().Result)
                {
                    Console.WriteLine("..AMS Native Web Service Available");
                }
                else
                {
                    Console.WriteLine("..AMS Native Web Service NOT Available");
                }
                if (AMSTools.IsAMSRestServiceAvailable())
                {
                    Console.WriteLine("..AMS RestAPI Service Available");
                }
                else
                {
                    Console.WriteLine("..AMS RestAPI Service NOT Available");
                }
            }
        }
        private static Tuple<DateTime, DateTime> GetFromToTime(bool today, bool yesterday, bool tomorrow, string from, string to)
        {
            if (!today && tomorrow && yesterday)
            {
                today = true;
            }

            DateTime fromTime = DateTime.Now;
            DateTime toTime = DateTime.Now;

            if (today)
            {
                fromTime = DateTime.Today;
                toTime = DateTime.Today.AddHours(23).AddMinutes(59);
            }
            if (yesterday)
            {
                fromTime = DateTime.Today.AddDays(-1);
                toTime = DateTime.Today;
            }
            if (tomorrow)
            {
                fromTime = DateTime.Today.AddDays(1);
                toTime = DateTime.Today.AddDays(1).AddHours(23).AddMinutes(59);
            }

            if (from != null && to != null)
            {
                DateTime.TryParse(from, out fromTime);
                DateTime.TryParse(to, out toTime);

                if (toTime == null || fromTime == null)
                {
                    Console.WriteLine("Incorrectly formatted date");
                    return null;
                }

                if (toTime < fromTime)
                {
                    Console.WriteLine("To date is before from date");
                    return null;
                }
            }

            return new Tuple<DateTime, DateTime>(fromTime, toTime);
        }
        private static int GetFlights(FlightsOptions opts)
        {
            Console.WriteLine($"Get Flights  {opts.FileName}");

            if (!AMSTools.FileOK(opts.FileName))
            {
                return -1;
            }

            var t = GetFromToTime(opts.Today, opts.Yesterday, opts.Tomorrow, opts.From, opts.To);
            if (t == null)
            {
                return -1;
            }

            using (AMSIntegrationServiceClient client = new AMSIntegrationServiceClient(AMSTools.GetWSBinding(), AMSTools.GetWSEndPoint()))
            {

                try
                {
                    XmlElement res = client.GetFlights(Parameters.TOKEN, t.Item1, t.Item2, Parameters.APT_CODE, AirportIdentifierType.IATACode);

                    if (!res.OuterXml.Contains("FLIGHT_NOT_FOUND"))
                    {
                        if (opts.Airline == null)
                        {

                            if (opts.CSV)
                            {
                                AMSTools.Out(GetCSV(res.OuterXml, "Flight", BaseType.Airline, ".//ams:FlightState"), opts.FileName);
                            }
                            else
                            {
                                AMSTools.Out(AMSTools.PrintXML(res.OuterXml), opts.FileName);
                            }
                        }
                        else
                        {
                            XmlNamespaceManager nsmgr = new XmlNamespaceManager(res.OwnerDocument.NameTable);
                            nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");
                            AMSTools.Out("<Flights>", opts.FileName);
                            foreach (XmlElement el in res.SelectNodes("//ams:Flights/ams:Flight", nsmgr))
                            {
                                {
                                    XmlNamespaceManager nsmgr2 = new XmlNamespaceManager(el.OwnerDocument.NameTable);
                                    nsmgr2.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");

                                    XmlNode node = el.SelectSingleNode("//ams:FlightId/ams:AirlineDesignator[@codeContext='IATA']", nsmgr2);
                                    if (node?.InnerText == opts.Airline)
                                    {
                                        AMSTools.Out(AMSTools.PrintXML(el.OuterXml), opts.FileName);
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Flight rejected. Airline Code = {node.InnerText}");
                                    }
                                }
                            }
                            AMSTools.Out("</Flights>", opts.FileName);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }


            return 1;
        }
        private static int GetTowings(TowingsOptions opts)
        {
            if (!AMSTools.FileOK(opts.FileName))
            {
                return -1;
            }

            var t = GetFromToTime(opts.Today, opts.Yesterday, opts.Tomorrow, opts.From, opts.To);
            if (t == null)
            {
                return -1;
            }

            string start = t.Item1.ToString("yyyy-MM-ddTHH:mm:ss");
            string end = t.Item2.ToString("yyyy-MM-ddTHH:mm:ss");

            string uri = Parameters.AMS_REST_SERVICE_URI + "/" + Parameters.APT_CODE + "/Towings/" + start + "/" + end;
            if (opts.CSV)
            {
                AMSTools.Out(GetCSV(AMSTools.GetRestURI(uri).Result, "Towing", BaseType.Towing, null), opts.FileName);
            }
            else
            {
                AMSTools.Out(AMSTools.PrintXML(AMSTools.GetRestURI(uri).Result), opts.FileName);
            }
            return 1;

        }



        private static int GetFlight(FlightOptions opts)
        {
            if (!AMSTools.FileOK(opts.FileName))
            {
                return -1;
            }

            if (!opts.Today && !opts.Tomorrow && !opts.Yesterday)
            {
                opts.Today = true;
            }

            double offset = 0.0;

            if (opts.Today)
            {
                offset = 0.0;
            }
            else if (opts.Yesterday)
            {
                offset = -1.0;
            }
            else if (opts.Tomorrow)
            {
                offset = 1.0;
            }

            double fromOffset = offset;
            double toOffset = offset;

            if (opts.From != null && opts.To != null)
            {
                DateTime fromTime;
                DateTime.TryParse(opts.From, out fromTime);

                DateTime toTime;
                DateTime.TryParse(opts.To, out toTime);

                if (toTime == null || fromTime == null)
                {
                    Console.WriteLine("Incorrectly formatted date");
                    return -1;
                }

                if (toTime < fromTime)
                {
                    Console.WriteLine("To date is before from date");
                    return -1;
                }

                fromOffset = (fromTime - DateTime.Now).TotalDays;
                toOffset = (toTime - DateTime.Now).TotalDays;
            }

            int start = Convert.ToInt32(Math.Floor(fromOffset));
            int stop = Convert.ToInt32(Math.Ceiling(toOffset));

            for (int off = start; off <= stop; off++)
            {
                bool found = false;
                DateTime date = DateTime.Now.AddDays(off);

                using (AMSIntegrationServiceClient client = new AMSIntegrationServiceClient(AMSTools.GetWSBinding(), AMSTools.GetWSEndPoint()))
                {
                    FlightId arr = AMSTools.GetFlightID(true, opts.Airline, opts.FlightNum, off);

                    try
                    {
                        XmlElement res = client.GetFlight(Parameters.TOKEN, arr);

                        if (!res.OuterXml.Contains("FLIGHT_NOT_FOUND"))
                        {
                            found = true;
                            AMSTools.Out(AMSTools.PrintXML(res.OuterXml), opts.FileName);
                            Console.WriteLine($"Arrival Flight {opts.Airline}{opts.FlightNum} found for {date}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }


                    FlightId dep = AMSTools.GetFlightID(false, opts.Airline, opts.FlightNum, off);

                    try
                    {
                        XmlElement res = client.GetFlight(Parameters.TOKEN, dep);
                        if (!res.OuterXml.Contains("FLIGHT_NOT_FOUND"))
                        {
                            found = true;
                            AMSTools.Out(AMSTools.PrintXML(res.OuterXml), opts.FileName);
                            Console.WriteLine($"Departure Flight {opts.Airline}{opts.FlightNum} found for {date}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                if (!found)
                {
                    Console.WriteLine($"Flight {opts.Airline}{opts.FlightNum} not found for {date}");
                }
            }

            return 1;
        }
        private static int GetAirlines(AirlinesOptions opts)
        {
            if (!AMSTools.FileOK(opts.FileName))
            {
                return -1;
            }

            if (opts.CSV)
            {
                AMSTools.Out(GetCSV(AMSTools.GetAirlines(), "Airline", BaseType.Airline, ".//ams:AirlineState"), opts.FileName);
            }
            else
            {
                AMSTools.Out(AMSTools.PrintXML(AMSTools.GetAirlines()), opts.FileName);
            }

            return 1;
        }
        private static int GetAircraftTypes(AircraftTypesOptions opts)
        {
            if (!AMSTools.FileOK(opts.FileName))
            {
                return -1;
            }
            AMSTools.Out(AMSTools.PrintXML(AMSTools.GetAircraftTypes()), opts.FileName);
            return 1;
        }
        private static int GetAirports(AirportsOptions opts)
        {
            if (!AMSTools.FileOK(opts.FileName))
            {
                return -1;
            }
            AMSTools.Out(AMSTools.PrintXML(AMSTools.GetAirports()), opts.FileName);
            return 1;
        }
        private static int GetAircrafts(AircraftsOptions opts)
        {
            if (!AMSTools.FileOK(opts.FileName))
            {
                return -1;
            }
            AMSTools.Out(AMSTools.PrintXML(AMSTools.GetAircrafts()), opts.FileName);
            return 1;
        }
        private static int GetGates(GatesOptions opts)
        {
            if (!AMSTools.FileOK(opts.FileName))
            {
                return -1;
            }
            try
            {
                string uri = Parameters.AMS_REST_SERVICE_URI + $"{Parameters.APT_CODE}/Gates";
                string result = AMSTools.GetRestURI(uri).Result;
                AMSTools.Out(AMSTools.PrintXML(result), opts.FileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return 1;
        }
        private static int GetStands(StandsOptions opts)
        {
            if (!AMSTools.FileOK(opts.FileName))
            {
                return -1;
            }
            try
            {
                string uri = Parameters.AMS_REST_SERVICE_URI + $"{Parameters.APT_CODE}/Stands";
                string result = AMSTools.GetRestURI(uri).Result;
                AMSTools.Out(AMSTools.PrintXML(result), opts.FileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return 1;
        }
        private static int GetStand(StandOptions opts)
        {
            if (!AMSTools.FileOK(opts.FileName))
            {
                return -1;
            }
            try
            {
                string uri = Parameters.AMS_REST_SERVICE_URI + $"{Parameters.APT_CODE}/Stands";
                string result = AMSTools.GetRestURI(uri).Result;

                XElement xmlRoot = XDocument.Parse(result).Root;
                XElement db = (from n in xmlRoot.Descendants() where (n.Name == "FixedResource" && n.Elements("Name").FirstOrDefault().Value == opts.Stand) select n).FirstOrDefault<XElement>();

                if (db == null)
                {
                    Console.WriteLine($"Stand {opts.Stand} not found");
                    return -1;
                }
                AMSTools.Out(AMSTools.PrintXML(db.ToString()), opts.FileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return 1;
        }

        private static string GetCSV(string xml, string tag, BaseType type, string propertyHead)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);


            Dictionary<string, string> headers = new Dictionary<String, string>();
            List<Dictionary<string, string>> entries = new List<Dictionary<string, string>>();

            foreach (XmlElement el in doc.GetElementsByTagName(tag))
            {
                Dictionary<string, string> entry = new Dictionary<String, string>();

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(el.OwnerDocument.NameTable);
                nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");


                foreach (CSVRule rule in csvFormatList)
                {
                    if (rule.type != type)
                    {
                        continue;
                    }

                    string val = el.SelectSingleNode(rule.xpath, nsmgr)?.InnerText;
                    entry.Add(rule.header, val);
                    if (!headers.ContainsKey(rule.header))
                    {
                        headers.Add(rule.header, rule.header);
                    }
                }

                if (propertyHead != null)
                {
                    XmlNode head = el.SelectSingleNode(propertyHead, nsmgr);
                    foreach (XmlNode value in head.SelectNodes("./ams:Value", nsmgr))
                    {
                        string propertyName = value.Attributes["propertyName"].Value;
                        string val = value.InnerText;
                        if (!headers.ContainsKey(propertyName))
                        {
                            headers.Add(propertyName, propertyName);
                        }
                        entry.Add(propertyName, val);
                    }
                }

                entries.Add(entry);
            }

            string csv = "";

            List<string> orderedHeads = new List<string>();
            foreach (KeyValuePair<string, string> entry in headers)
            {
                csv += entry.Key + ",";
                orderedHeads.Add(entry.Key);
            }

            csv = csv.Trim(',');
            csv += "\n";

            foreach (Dictionary<string, string> entry in entries)
            {
                string row = "";
                foreach (string h in orderedHeads)
                {
                    if (entry.ContainsKey(h))
                    {
                        row += entry[h] + ",";
                    }
                    else
                    {
                        row += ",";
                    }
                }

                row = row.Remove(row.Length - 1);
                csv += row + "\n";
            }

            return csv;
        }

        private static void ReadRules()
        {
            try
            {
                using (TextFieldParser parser = new TextFieldParser("CSVFormat.config"))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters("::");


                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        if (fields[0].StartsWith("#") || fields[0].StartsWith(" "))
                        {
                            continue;
                        }
                        csvFormatList.Add(new CSVRule(fields));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("\nHit Any Key to Exit..");
                Console.ReadKey();
                return;
            }
        }

    }

}

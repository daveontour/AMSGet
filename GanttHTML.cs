using AMSUtilLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv;

namespace AMSGet {

    abstract class Record {
        public string GetValue(XmlElement el, string xpath, XmlNamespaceManager nsmgr) {

            try {
                return el.SelectSingleNode(xpath, nsmgr).InnerText;
            } catch (Exception) {
                return null;
            }
        }
        public string GetValue(XmlNode el, string xpath, XmlNamespaceManager nsmgr) {

            try {
                return el.SelectSingleNode(xpath, nsmgr).InnerText;
            } catch (Exception) {
                return null;
            }
        }
        public string GetValue(XmlElement el, string xpath) {

            try {
                return el.SelectSingleNode(xpath).InnerText;
            } catch (Exception) {
                return null;
            }
        }
        public string GetValue(XmlNode el, string xpath) {

            try {
                return el.SelectSingleNode(xpath).InnerText;
            } catch (Exception) {
                return null;
            }
        }
    }

    class DownGradeRecord : Record {
        public bool fullUnavailable = false;
        private string comment;
        private string reason;
        public DateTime start;
        public DateTime end;
        public List<string> standList = new List<string>();
        public bool valid = true;

        public DownGradeRecord(XmlNode el, XmlNamespaceManager nsmgr) {
            this.comment = GetValue(el, "./ams:Value[@propertyName='Comment']", nsmgr);
            this.reason = GetValue(el, "./ams:Value[@propertyName='Reason']", nsmgr);

            var ss = GetValue(el, "./ams:Value[@propertyName='StartTime']", nsmgr);
            var es = GetValue(el, "./ams:Value[@propertyName='EndTime']", nsmgr);

            valid = DateTime.TryParse(ss, out start) && DateTime.TryParse(es, out end);

            foreach (XmlNode stand in el.SelectNodes(".//ams:Stand", nsmgr)) {
                var s = GetValue(stand, "./ams:Value[@propertyName='Name']", nsmgr);
                standList.Add(s);
            }

            var sa = GetValue(el, "./ams:Value[@propertyName='IsFullUnavailability']", nsmgr);

            Boolean.TryParse(sa, out fullUnavailable);
        }

        public override string ToString() {
            string stands = "";
            foreach (string s in standList) {
                stands += $"{s},";
            }

            string status = this.fullUnavailable ? "Full" : "Partial";
            return $"Downgraded Stands: {stands} \"{status}\":\"{start.ToString("YYYY-MM-dd HH:mm")}\":\"{end.ToString("YYYY-MM-dd HH:mm")}\":\"{comment}\":\"{reason}\"";
        }
        public string ToStringPartial() {

            string s = $"Partial Downgrade. From: {start:yyyy-MM-dd HH:mm},  To: {end:yyyy-MM-dd HH:mm}, \"{comment}\":\"{reason}\"";
            if (fullUnavailable) {
                s = $"Full Downgrade. From: {start:yyyy-MM-dd HH:mm},  To: {end:yyyy-MM-dd HH:mm} \"{comment}\":\"{reason}\"";
            }
            return s;
        }

    }
    class StandRecord : Record {
        public string name;
        public string id;
        public string area;
        public int sortOrder;
        public int numRows = 1;
        public List<DownGradeRecord> downgradeList = new List<DownGradeRecord>();
        public List<SlotRecord> slotList = new List<SlotRecord>();
        public List<TowRecord> fromTows = new List<TowRecord>();
        public List<TowRecord> toTows = new List<TowRecord>();

        public StandRecord(XmlNode stand) {
            this.name = stand.SelectSingleNode("./Name").InnerText;
            this.id = stand.SelectSingleNode("./Id").InnerText;
            this.area = stand.SelectSingleNode("./Area").InnerText;
            this.sortOrder = int.Parse(stand.SelectSingleNode("./SortOrder").InnerText);
        }
        public StandRecord() { }
    }
    class SlotRecord : Record {
        public string slotStart;
        public DateTime slotStartDateTime;
        public string slotEnd;
        public DateTime slotEndDateTime;
        public string slotStand;
        public FlightRecord flight;
        public int left;
        public int width;
        public int row;
        internal bool onDowngrade = false;

        public string towFromStand = null;
        public string towToStand = null;

        public SlotRecord(XmlNode slot, XmlNamespaceManager nsmgr, FlightRecord flight) {

            this.slotStart = GetValue(slot, "./ams:Value[@propertyName='StartTime']", nsmgr);
            this.slotEnd = GetValue(slot, "./ams:Value[@propertyName='EndTime']", nsmgr);
            this.slotStand = GetValue(slot, "./ams:Stand/ams:Value[@propertyName='Name']", nsmgr);

            if (slotStart != null) {
                slotStartDateTime = DateTime.Parse(slotStart);
            }
            if (slotEnd != null) {
                slotEndDateTime = DateTime.Parse(slotEnd);
            }

            this.flight = flight;
        }
    }
    class FlightRecord : Record {
        public string airline;
        public string flightUniqueID;
        public string l_flightUniqueID;
        public string fltNum;
        public string type;
        public string sto;
        public string l_airline;
        public string l_fltNum;
        public string l_type;
        public string l_sto;
        public string route;
        public string l_route;
        public string reg;
        public DateTime stoDate;
        public DateTime l_stoDate;


        public string actype;

        public string FlightDescriptor {
            get {
                string d = $"{airline}{fltNum}@{stoDate.ToString("yyyy-MM-ddTHH:mm")}";
                if (type == "Arrival") {
                    return d + "A";
                } else {
                    return d + "D";
                }

            }
        }
        public string LinkedFlightDescriptor {
            get {
                string d = $"{l_airline}{l_fltNum}@{l_stoDate.ToString("yyyy-MM-ddTHH:mm")}";
                if (l_type == "Arrival") {
                    return d + "A";
                } else {
                    return d + "D";
                }

            }
        }


        public FlightRecord(XmlElement el, XmlNamespaceManager nsmgr) {
            this.airline = GetValue(el, "./ams:FlightId/ams:AirlineDesignator[@codeContext='IATA']", nsmgr);
            this.fltNum = GetValue(el, "./ams:FlightId/ams:FlightNumber", nsmgr);
            this.type = GetValue(el, "./ams:FlightId/ams:FlightKind", nsmgr);
            this.sto = GetValue(el, "./ams:FlightState/ams:ScheduledTime", nsmgr);
            this.flightUniqueID = GetValue(el, "./ams:FlightState/ams:Value[@propertyName='FlightUniqueID']", nsmgr);

            DateTime.TryParse(sto, out this.stoDate);

            this.route = GetValue(el, "./ams:FlightState/ams:Route/ams:ViaPoints/ams:RouteViaPoint[@sequenceNumber='0']/ams:AirportCode[@codeContext='IATA']", nsmgr);

            this.l_airline = GetValue(el, "./ams:FlightState/ams:LinkedFlight/ams:FlightId/ams:AirlineDesignator[@codeContext='IATA']", nsmgr);
            this.l_fltNum = GetValue(el, "./ams:FlightState/ams:LinkedFlight/ams:FlightId/ams:FlightNumber", nsmgr);
            this.l_type = GetValue(el, "./ams:FlightState/ams:LinkedFlight/ams:FlightId/ams:FlightKind", nsmgr);
            this.l_sto = GetValue(el, "./ams:FlightState/ams:LinkedFlight/ams:Value[@propertyName='ScheduledTime']", nsmgr);
            this.l_flightUniqueID = GetValue(el, "./ams:FlightState/ams:LinkedFlight/ams:Value[@propertyName='FlightUniqueID']", nsmgr);

            DateTime.TryParse(l_sto, out this.l_stoDate);

            this.actype = GetValue(el, "./ams:FlightState/ams:AircraftType/ams:AircraftTypeId/ams:AircraftTypeCode[@codeContext='IATA']", nsmgr);
            this.reg = GetValue(el, "./ams:FlightState/ams:Aircraft/ams:AircraftId/ams:Registration", nsmgr);
        }
        public bool ShowFlight() {
            bool show = true;

            if (type == "Departure") {
                return true;
            }
            if (type == "Arrival") {
                if (l_fltNum == null) {
                    return true;
                } else {
                    return false;
                }
            }

            return show;
        }

        public string ToString(Dictionary<string, FlightRecord> fltMap) {

            string fltDesc = "===================";

            // Departure and linked Arrival
            if (type == "Departure" && l_fltNum != null) {
                return $"{l_airline}{l_fltNum} / {fltMap[l_flightUniqueID].route} / {reg} / {actype} / {airline}{fltNum} / {route}";
            }

            //Departure, no arrival
            if (type == "Departure" && l_fltNum == null) {
                return $"{airline}{fltNum} / {route} / {reg} / {actype}>";
            }

            //Only arrivals without a departure will get through to here
            if (type == "Arrival") {
                return $"<{airline}{fltNum} / {route} / {reg} / {actype}";
            }

            return fltDesc;

        }
    }

    class TowRecord : Record {
        public string arrDesc;
        public string depDesc;

        public string fromStand;
        public string toStand;

        public DateTime start;
        public DateTime end;

        public TowRecord(XmlNode tow) {

            var first = GetValue(tow, "./FlightDescriptors/FlightDescriptor[1]");
            if (first != null) {
                if (first.EndsWith("A")) {
                    this.arrDesc = first;
                } else {
                    this.depDesc = first;
                }
            }

            var sec = GetValue(tow, "./FlightDescriptors/FlightDescriptor[2]");
            if (sec != null) {
                if (sec.EndsWith("A")) {
                    this.arrDesc = sec;
                } else {
                    this.depDesc = sec;
                }
            }

            this.fromStand = GetValue(tow, "./From");
            this.toStand = GetValue(tow, "./To");

            string st = GetValue(tow, "./ScheduledStart");
            string se = GetValue(tow, "./ScheduledEnd");

            DateTime.TryParse(st, out start);
            DateTime.TryParse(se, out end);
        }
    }
    class Bucket {

        private int minSlotLength = 350;
        public List<List<SlotRecord>> bucket = new List<List<SlotRecord>>();

        internal Bucket() { }

        public void AddToBucket(SlotRecord slot) {

            if (slot.left == 0 && slot.width == 0) {
                return;
            }

            bool added = false;
            int rowIndex = 0;
            foreach (List<SlotRecord> row in bucket) {
                if (CanAddToRow(rowIndex, slot)) {
                    AddToRow(rowIndex, slot);
                    added = true;
                    break;
                }
                rowIndex++;
            }

            if (!added) {
                AddToRow(rowIndex++, slot);
            }
        }

        public List<SlotRecord> GetSlotsList(StandRecord stand) {
            if (stand.id == null) {
                return new List<SlotRecord>();
            }

            foreach (SlotRecord slot in stand.slotList) {
                AddToBucket(slot);
            }

            List<SlotRecord> list = new List<SlotRecord>();
            foreach (List<SlotRecord> row in bucket) {
                list.AddRange(row);
            }
            return list;
        }

        private void AddRow() {
            List<SlotRecord> newBucketRow = new List<SlotRecord>();
            bucket.Add(newBucketRow);
        }

        public void AddToRow(int rowIndex, SlotRecord slot) {
            if (bucket.Count < rowIndex + 1) {
                AddRow();
            }
            bucket[rowIndex].Add(slot);
            slot.row = rowIndex + 1;
        }

        public bool CanAddToRow(int rowIndex, SlotRecord slot) {

            List<SlotRecord> row = bucket[rowIndex];
            foreach (SlotRecord slotRecord in row) {
                int currentLeft = slotRecord.left;
                int currentRight = Math.Max(slotRecord.left + slotRecord.width, slotRecord.left + this.minSlotLength);

                int testLeft = slot.left;
                int testRight = Math.Max(slotRecord.left + slotRecord.width, slotRecord.left + this.minSlotLength);

                if (currentLeft <= testLeft && testLeft <= currentRight) {
                    return false;
                }

                if (testLeft <= currentLeft && currentLeft <= testRight) {
                    return false;
                }
            }

            return true;
        }

        internal int GetRows() {
            return bucket.Count;
        }
    }

    class GanttHTML {

        private XmlDocument doc = new XmlDocument();
        private XmlElement head;
        private XmlElement body;
        private XmlElement style;
        private XmlElement root;
        private DateTime zeroTime;

        private Dictionary<string, List<string>> setsMap = new Dictionary<string, List<string>>();
        private XmlDocument standsDoc;
        private XmlDocument setsDoc = new XmlDocument();
        private Dictionary<string, StandRecord> standMap = new Dictionary<string, StandRecord>();
        private Dictionary<string, List<StandRecord>> areaMap = new Dictionary<string, List<StandRecord>>();
        //private Dictionary<string, List<SlotRecord>> standSlotMap = new Dictionary<string, List<SlotRecord>>();
        private Dictionary<string, FlightRecord> fltMap = new Dictionary<string, FlightRecord>();
        IEnumerable<string> sets;

        public string css;
        public int minSeparation = 400;

        public GanttHTML(IEnumerable<string> sets) {

            this.sets = sets;

            StandRecord unallocated = new StandRecord();
            unallocated.name = "Unallocated";
            unallocated.area = "Unallocated";
            unallocated.id = "Unallocated";
            unallocated.sortOrder = Int32.MaxValue;

            standMap.Add(unallocated.id, unallocated);

            List<StandRecord> u = new List<StandRecord>();
            u.Add(unallocated);
            areaMap.Add("Unallocated", u);


            root = doc.CreateElement("hmtl");
            doc.AppendChild(root);
            this.head = doc.CreateElement("head");
            this.body = doc.CreateElement("body");
            this.style = doc.CreateElement("style");
            css = System.IO.File.ReadAllText(@"GanttStyle.css");
            style.InnerText = this.css;

            setsDoc.LoadXml(System.IO.File.ReadAllText(@"StandSets.xml"));
            foreach (XmlNode set in setsDoc.SelectNodes("//Set")) {
                List<string> areas = new List<string>();
                setsMap.Add(set.Attributes["name"].Value, areas);
                foreach (XmlNode area in set.SelectNodes("./Area")) {
                    areas.Add(area.Attributes["name"].Value);
                }
            }
        }

        public bool Prepare() {

            // Calculate the time of the zero time 
            DateTime now = DateTime.Now;
            zeroTime = now.AddHours(-3);
            zeroTime = new DateTime(zeroTime.Year, zeroTime.Month, zeroTime.Day, zeroTime.Hour, 0, 0);


            head.AppendChild(style);
            root.AppendChild(head);
            root.AppendChild(body);

            try {
                string result = AMSTools.GetRestURI(Parameters.AMS_REST_SERVICE_URI + $"{Parameters.APT_CODE}/Stands").Result;
                standsDoc = new XmlDocument();
                standsDoc.LoadXml(result);
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }


            Console.WriteLine("Retrieving Stands");

            // Create the StandRecords and put them into lists according to Stand Area
            foreach (XmlNode stand in standsDoc.SelectNodes(".//FixedResource")) {
                StandRecord standRecord = new StandRecord(stand);
                standMap.Add(standRecord.id, standRecord);

                if (!areaMap.ContainsKey(standRecord.area)) {
                    areaMap.Add(standRecord.area, new List<StandRecord>());
                }
                areaMap[standRecord.area].Add(standRecord);
            }

            Console.WriteLine("Retrieving Downgrades");
            using (AMSIntegrationServiceClient client = new AMSIntegrationServiceClient(AMSTools.GetWSBinding(), AMSTools.GetWSEndPoint())) {

                // Get the Downgrade records and add them to the appropriat stand
                try {
                    XmlElement xdowngrades = client.GetStandDowngrades(Parameters.TOKEN, DateTime.Now.AddHours(-24), DateTime.Now.AddHours(24), Parameters.APT_CODE, AirportIdentifierType.IATACode);
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(xdowngrades.OwnerDocument.NameTable);
                    nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");
                    foreach (XmlElement el in xdowngrades.SelectNodes("//ams:StandDowngradeState", nsmgr)) {
                        DownGradeRecord drec = new DownGradeRecord(el, nsmgr);
                        foreach (string standID in drec.standList) {
                            standMap[standID].downgradeList.Add(drec);
                        }
                    }

                } catch (Exception e) {
                    Debug.WriteLine(e.Message);
                }
            }

            Console.WriteLine("Retrieving Flights");
            using (AMSIntegrationServiceClient client = new AMSIntegrationServiceClient(AMSTools.GetWSBinding(), AMSTools.GetWSEndPoint())) {


                XmlElement res = client.GetFlights(Parameters.TOKEN, DateTime.Now.AddHours(-24), DateTime.Now.AddHours(24), Parameters.APT_CODE, AirportIdentifierType.IATACode);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(res.OwnerDocument.NameTable);
                nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");


                // Create the flight records and add them to the stands. Position them horizontally.
                foreach (XmlElement el in res.SelectNodes("//ams:Flights/ams:Flight", nsmgr)) {
                    {
                        FlightRecord flight = new FlightRecord(el, nsmgr);

                        fltMap.Add(flight.flightUniqueID, flight);

                        XmlNode slots = el.SelectSingleNode("./ams:FlightState/ams:StandSlots", nsmgr);

                        if (slots != null) {

                            // Iterate through each of the Stand Slots for the flight

                            foreach (XmlNode slot in slots.SelectNodes("./ams:StandSlot", nsmgr)) {
                                SlotRecord slotRecord = new SlotRecord(slot, nsmgr, flight);

                                if (slotRecord.slotStand == null) {
                                    slotRecord.slotStand = "Unallocated";
                                }

                                if (!slotRecord.flight.ShowFlight()) {
                                    continue;
                                }

                                if (slotRecord.slotEndDateTime < this.zeroTime || slotRecord.slotStartDateTime > this.zeroTime.AddHours(23)) {
                                    //Outside range of Gantt
                                    continue;
                                }


                                TimeSpan tss = slotRecord.slotStartDateTime - this.zeroTime;
                                TimeSpan tse = slotRecord.slotEndDateTime - this.zeroTime;

                                // End of slot before start of zeroTime 
                                if (tse.TotalMinutes < 0) {
                                    continue;
                                }

                                // Start of slot more than end of chart
                                if (tss.TotalHours > 23) {
                                    continue;
                                }

                                int width = Convert.ToInt32(tse.TotalMinutes - tss.TotalMinutes);
                                int left = Convert.ToInt32(tss.TotalMinutes);
                                if (left < 0) {
                                    width += left;
                                    left = 0;
                                }

                                slotRecord.left = left;
                                slotRecord.width = width;
                                slotRecord.row = 1;

                                standMap[slotRecord.slotStand].slotList.Add(slotRecord);

                            }
                        } else {

                            // No Stand Slot Defined for the flight
                            SlotRecord slotRecord = new SlotRecord(null, nsmgr, flight);
                            standMap["Unallocated"].slotList.Add(slotRecord);

                        }
                    }
                }
            }


            // Get the towings
            {
                Console.WriteLine("Retrieving Tow Events");
                string start = DateTime.Now.AddHours(-24).ToString("yyyy-MM-ddTHH:mm:ss");
                string end = DateTime.Now.AddHours(24).ToString("yyyy-MM-ddTHH:mm:ss");

                string uri = Parameters.AMS_REST_SERVICE_URI + "/" + Parameters.APT_CODE + "/Towings/" + start + "/" + end;
                string towingsXML = AMSTools.GetRestURI(uri).Result;

                var towsDoc = new XmlDocument();
                towsDoc.LoadXml(towingsXML);

                //Add the tow records to the stand
                foreach (XmlElement el in towsDoc.SelectNodes("//Towing")) {
                    TowRecord towRec = new TowRecord(el);
                    try {
                        standMap[towRec.fromStand].fromTows.Add(towRec);
                    } catch (Exception e) {
                        Debug.WriteLine(e.Message);
                    }
                    try {
                        standMap[towRec.toStand].toTows.Add(towRec);
                    } catch (Exception e) {
                        Debug.WriteLine(e.Message);
                    }
                }

            }
            // Position the flight vertically withing row to avoid overlays

            Console.WriteLine("Arranging Layout");
            DeconflictSlotOverlay();

            //Add Tow Flag to slot record
            AddTowFlags();

            return true;
        }
        private void DeconflictSlotOverlay() {
            // Go through the slots for each stand and make sure they dont overlay each other 
            // By moving them to the next row if necessary.
            foreach (var pair in this.standMap) {
                StandRecord stand = pair.Value;

                // if only 0 or 1 slot, then there is no overlap, so continue;
                if (stand.slotList.Count <= 1) {
                    stand.numRows = 1;
                    continue;
                }

                //Sort from lowest to highest. 
                stand.slotList.Sort((p, q) => p.left.CompareTo(q.left));

                Bucket bucket = new Bucket();
                stand.slotList = bucket.GetSlotsList(stand);
                stand.numRows = bucket.GetRows();

            }

            // Now Lets adjust for flight allocated to downgrade stands
            foreach (var pair in this.standMap) {
                StandRecord stand = pair.Value;
                if (stand.downgradeList.Count == 0 || stand.slotList.Count == 0) {
                    continue;
                }

                stand.numRows++;
                foreach (SlotRecord slot in stand.slotList) {
                    slot.row++;
                    foreach (DownGradeRecord down in stand.downgradeList) {
                        if (slot.slotEndDateTime < down.start || slot.slotStartDateTime > down.end) {
                            continue;
                        }
                        if (slot.slotStartDateTime >= down.start && slot.slotStartDateTime <= down.end) {
                            slot.onDowngrade = true;
                            break;
                        }
                        if (slot.slotEndDateTime >= down.start && slot.slotEndDateTime <= down.end) {
                            slot.onDowngrade = true;
                            break;
                        }
                        if (slot.slotEndDateTime >= down.end && slot.slotStartDateTime <= down.start) {
                            slot.onDowngrade = true;
                            break;
                        }
                    }
                }
            }

            return;
        }

        private void AddTowFlags() {


            foreach (var pair in this.standMap) {

                StandRecord stand = pair.Value;
                foreach (TowRecord rec in stand.fromTows) {
                    foreach (SlotRecord slot in stand.slotList) {
                        FlightRecord flt = slot.flight;
                        if (flt.FlightDescriptor == rec.arrDesc
                            || flt.FlightDescriptor == rec.depDesc
                            || flt.LinkedFlightDescriptor == rec.arrDesc
                            || flt.LinkedFlightDescriptor == rec.depDesc
                            ) {
                            slot.towToStand = $" ({rec.toStand})";
                        }
                    }

                }

                foreach (TowRecord rec in stand.toTows) {
                    foreach (SlotRecord slot in stand.slotList) {
                        FlightRecord flt = slot.flight;
                        if (flt.FlightDescriptor == rec.arrDesc
                            || flt.FlightDescriptor == rec.depDesc
                            || flt.LinkedFlightDescriptor == rec.arrDesc
                            || flt.LinkedFlightDescriptor == rec.depDesc
                            ) {
                            slot.towFromStand = $" ({rec.fromStand})";
                        }
                    }

                }

            }
        }
        public XmlElement AddGanttTable(string area) {

            Console.WriteLine($"Writing output for area {area}");
            XmlElement gantt = doc.CreateElement("div");

            if (!areaMap.ContainsKey(area)) {
                return gantt;
            }

            XmlElement title = doc.CreateElement("h1");
            title.InnerText = area;
            title.SetAttribute("class", "areaTitle");

            gantt.AppendChild(title);



            // The top row showing the time markers
            XmlElement timeRow = doc.CreateElement("div");
            timeRow.SetAttribute("style", "width: 100px; height: 32px; position: relative; display: flex; align-items:center; border-bottom: solid 1px gray; width:1590px");

            DateTime marker = zeroTime;


            //XmlElement tableDiv = doc.CreateElement("div");
            //XmlElement table = doc.CreateElement("table");
            //tableDiv.AppendChild(table);
            //XmlElement topTableRow = doc.CreateElement("tr");
            //table.AppendChild(topTableRow);
            //XmlElement cell1 = doc.CreateElement("td");
            //XmlElement cell2 = doc.CreateElement("td");
            //XmlElement cell3 = doc.CreateElement("td");
            //XmlElement cell4 = doc.CreateElement("td");
            //XmlElement cell5 = doc.CreateElement("td");
            //cell1.InnerText = "Stand";
            //cell2.InnerText = "Start";
            //cell3.InnerText = "End";
            //cell4.InnerText = "Flight Information";


            //topTableRow.AppendChild(cell1);
            //topTableRow.AppendChild(cell2);
            //topTableRow.AppendChild(cell3);
            //topTableRow.AppendChild(cell4);


            for (int i = 0; i < 24; i++) {
                XmlElement cell = doc.CreateElement("div");
                cell.SetAttribute("style", $"left:{120 + i * 60}px; width:60px;position: absolute; text-align:center");
                cell.InnerText = $"{marker.ToString("HH:mm")}";
                timeRow.AppendChild(cell);
                marker = marker.AddHours(1);
            }

            gantt.AppendChild(timeRow);

            // The individual rows for each stand

            int j = 0;

            areaMap[area].Sort((x, y) => x.sortOrder.CompareTo(y.sortOrder));

            foreach (var stand in areaMap[area]) {
                j++;
                XmlElement row = AddGridRow(stand, j);

                foreach (DownGradeRecord dg in stand.downgradeList) {
                    if (dg.end < this.zeroTime || dg.start > this.zeroTime.AddHours(23) || !dg.valid) {
                        //Outside range of Gantt
                        continue;
                    }

                    TimeSpan tss = dg.start - this.zeroTime;
                    TimeSpan tse = dg.end - this.zeroTime;

                    int width = Convert.ToInt32(tse.TotalMinutes - tss.TotalMinutes);
                    int left = Convert.ToInt32(tss.TotalMinutes);
                    if (left < 0) {
                        width += left;
                        left = 0;
                    }
                    width = Math.Min(Convert.ToInt32(tse.TotalMinutes - tss.TotalMinutes), 1440);

                    XmlElement dgDiv = doc.CreateElement("div");
                    dgDiv.SetAttribute("style", $"left:{left + 150}px; width:{width}px; top: 2px; height:{32 + (stand.numRows - 1) * 42 }px; position:absolute; border: 1px solid black;  font-size:12px; font-family: Verdana;  padding-left:2px");

                    if (dg.fullUnavailable) {
                        dgDiv.SetAttribute("class", "downgradeFull");
                    } else {
                        dgDiv.SetAttribute("class", "downgradePartial");
                    }
                    dgDiv.InnerText = dg.ToStringPartial();
                    row.AppendChild(dgDiv);
                }

                foreach (SlotRecord slot in stand.slotList) {

                    if (slot.left == 0 && slot.width == 0) {
                        continue;
                    }

                    row.AppendChild(CreateSlotDiv(slot, stand.downgradeList));


                    // The row for the text table
                    //{
                    //    XmlElement tableRow = doc.CreateElement("tr");
                    //    tableRow.SetAttribute("style", $"font-size:18px; font-family: Verdana;");
                    //    table.AppendChild(tableRow);
                    //    XmlElement cell01 = doc.CreateElement("td");
                    //    XmlElement cell02 = doc.CreateElement("td");
                    //    XmlElement cell03 = doc.CreateElement("td");
                    //    XmlElement cell04 = doc.CreateElement("td");
                    //    XmlElement cell05 = doc.CreateElement("td");
                    //    cell01.InnerText = stand.name;
                    //    cell02.InnerText = slot.slotStartDateTime.ToString();
                    //    cell03.InnerText = slot.slotEndDateTime.ToString(); ;
                    //    cell04.InnerText = f.ToString(fltMap);


                    //    tableRow.AppendChild(cell01);
                    //    tableRow.AppendChild(cell02);
                    //    tableRow.AppendChild(cell03);
                    //    tableRow.AppendChild(cell04);

                    //    table.AppendChild(tableRow);
                    //}
                }
                gantt.AppendChild(row);
            }

            //gantt.AppendChild(tableDiv);

            return gantt;
        }

        private XmlElement CreateSlotDiv(SlotRecord slot, List<DownGradeRecord> downgradeList) {

            //Adjust so dont overflow end of gantt
            if (slot.left + slot.width > 1440) {
                slot.width = 1440 - slot.left;
            }

            XmlElement outerDiv = doc.CreateElement("div");
            XmlElement fromTimeDiv = doc.CreateElement("div");
            fromTimeDiv.SetAttribute("class", "fromTimeDiv");
            XmlElement toTimeDiv = doc.CreateElement("div");
            toTimeDiv.SetAttribute("class", "toTimeDiv");
            toTimeDiv.SetAttribute("style", $"left:{slot.width}px");

            String fromTime = slot.slotStartDateTime.ToString("HH:mm");
            String toTime = slot.slotEndDateTime.ToString("HH:mm");


            fromTimeDiv.InnerText = fromTime;
            toTimeDiv.InnerText = toTime;

            FlightRecord f = slot.flight;
            int radius = 0;
            //if (slot.left == 0) {
            //    radius = 0;
            //}


            XmlElement fltDiv = doc.CreateElement("div");

            //Vertical position in the row
            int top = 2 + (slot.row - 1) * 42;
            outerDiv.SetAttribute("style", $"position:absolute; left:{slot.left + 150}px; top:{top}px;");

            //Horizontal position in the row
            fltDiv.SetAttribute("style", $"width:{slot.width}px; border-radius:{radius}px; ");



            // Clsss dependand on whether it is on a downgraded stand. 
            if (slot.onDowngrade) {
                string clazz = "ondowngradeflight";
                if (slot.towToStand != null) {
                    clazz += " fromStand";
                }
                if (slot.towFromStand != null) {
                    clazz += " toStand";
                }
                fltDiv.SetAttribute("class", clazz);
            } else {
                string clazz = "flight";
                if (slot.slotStand == "Unallocated") {
                    clazz = "flightUnallocated";
                }
                if (slot.towToStand != null) {
                    clazz += " fromStand";
                }
                if (slot.towFromStand != null) {
                    clazz += " toStand";
                }
                fltDiv.SetAttribute("class", clazz);
            }

            // The text for the flight
            fltDiv.InnerText = f.ToString(fltMap);

            outerDiv.AppendChild(fromTimeDiv);
            outerDiv.AppendChild(fltDiv);
            outerDiv.AppendChild(toTimeDiv);
            return outerDiv;
        }

        public XmlElement AddGridRow(StandRecord stand, int rowIndex) {

            XmlElement row = doc.CreateElement("div");

            int rowHeight = Math.Max(42, 42 * stand.numRows);
            row.SetAttribute("style", $"width: 100px; height: {rowHeight}px; position: relative; display: flex; align-items:center; border-bottom: solid 1px gray; width:1590px");
            if (rowIndex % 2 == 0) {
                row.SetAttribute("class", "odd");
            } else {
                row.SetAttribute("class", "even");
            }
            XmlElement titleCell = doc.CreateElement("div");
            titleCell.InnerText = stand.name;
            titleCell.SetAttribute("style", $"left:0px; width:150px");


            row.AppendChild(titleCell);

            for (int i = 0; i < 24; i++) {
                XmlElement cell = doc.CreateElement("div");
                cell.SetAttribute("class", "hourIndicator");
                cell.SetAttribute("style", $"left:{150 + i * 60}px; width:50px");
                cell.InnerText = "&nbsp;";
                row.AppendChild(cell);
            }

            return row;
        }

        public string GetHTML() {

            //foreach (var areaPair in areaMap) {
            //    body.AppendChild(AddGanttTable(areaPair.Key));
            //}

            body.AppendChild(GetSets());
            string html = doc.OuterXml.Replace("&amp;nbsp;", "&nbsp;");
            return html;
        }

        public XmlElement GetSets() {

            XmlElement setsDiv = doc.CreateElement("div");

            foreach (string setName in this.sets) {
                XmlElement setDiv = doc.CreateElement("div");
                XmlElement setHeadTitle = doc.CreateElement("h1");
                setHeadTitle.InnerText = setName;
                setHeadTitle.SetAttribute("class", "setTitle");
                setDiv.AppendChild(setHeadTitle);

                if (!setsMap.ContainsKey(setName)) {
                    setsDiv.AppendChild(setDiv);
                    continue;
                }

                List<string> areaList = setsMap[setName];
                foreach (string area in areaList) {
                    setDiv.AppendChild(AddGanttTable(area));
                }

                setsDiv.AppendChild(setDiv);
            }

            return setsDiv;
        }
    }
}

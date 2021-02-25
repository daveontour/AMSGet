using AMSUtilLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv;

namespace AMSGet {

    class DownGradeRecord {
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
            return $"Downgraded Stands: {stands} \"{status}\":\"{start}\":\"{end}\":\"{comment}\":\"{reason}\"";
        }
        public string ToStringPartial() {
            string status = this.fullUnavailable ? "Full" : "Partial";
            return $"Downgraded \"{status}\":\"{start}\":\"{end}\":\"{comment}\":\"{reason}\"";
        }

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
    }
    class StandRecord {
        public string name;
        public string id;
        public string area;
        public int sortOrder;
        public int numRows = 1;
        public List<DownGradeRecord> downgradeList = new List<DownGradeRecord>();
        public List<SlotRecord> slotList = new List<SlotRecord>();

        public StandRecord(XmlNode stand) {
            this.name = stand.SelectSingleNode("./Name").InnerText;
            this.id = stand.SelectSingleNode("./Id").InnerText;
            this.area = stand.SelectSingleNode("./Area").InnerText;
            this.sortOrder = int.Parse(stand.SelectSingleNode("./SortOrder").InnerText);
        }
        public StandRecord() { }
    }

    class SlotRecord {
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
    }

    class FlightRecord {
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

        public string actype;

        public FlightRecord(XmlElement el, XmlNamespaceManager nsmgr) {
            this.airline = GetValue(el, "./ams:FlightId/ams:AirlineDesignator[@codeContext='IATA']", nsmgr);
            this.fltNum = GetValue(el, "./ams:FlightId/ams:FlightNumber", nsmgr);
            this.type = GetValue(el, "./ams:FlightId/ams:FlightKind", nsmgr);
            this.sto = GetValue(el, "./ams:FlightState/ams:ScheduledTime", nsmgr);
            this.flightUniqueID = GetValue(el, "./ams:FlightState/ams:Value[@propertyName='FlightUniqueID']", nsmgr);

            this.route = GetValue(el, "./ams:FlightState/ams:Route/ams:ViaPoints/ams:RouteViaPoint[@sequenceNumber='0']/ams:AirportCode[@codeContext='IATA']", nsmgr);

            this.l_airline = GetValue(el, "./ams:FlightState/ams:LinkedFlight/ams:FlightId/ams:AirlineDesignator[@codeContext='IATA']", nsmgr);
            this.l_fltNum = GetValue(el, "./ams:FlightState/ams:LinkedFlight/ams:FlightId/ams:FlightNumber", nsmgr);
            this.l_type = GetValue(el, "./ams:FlightState/ams:LinkedFlight/ams:FlightId/ams:FlightKind", nsmgr);
            this.l_sto = GetValue(el, "./ams:FlightState/ams:LinkedFlight/ams:Value[@propertyName='ScheduledTime']", nsmgr);
            this.l_flightUniqueID = GetValue(el, "./ams:FlightState/ams:LinkedFlight/ams:Value[@propertyName='FlightUniqueID']", nsmgr);

            this.actype = GetValue(el, "./ams:FlightState/ams:AircraftType/ams:AircraftTypeId/ams:AircraftTypeCode[@codeContext='IATA']", nsmgr);
            this.reg = GetValue(el, "./ams:FlightState/ams:Aircraft/ams:AircraftId/ams:Registration", nsmgr);
        }

        public string GetValue(XmlNode el, string xpath, XmlNamespaceManager nsmgr) {

            try {
                return el.SelectSingleNode(xpath, nsmgr).InnerText;
            } catch (Exception) {
                return null;
            }
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
    class GanttHTML {

        private XmlDocument doc = new XmlDocument();
        private XmlElement head;
        private XmlElement body;
        private XmlElement style;
        private XmlElement root;
        private DateTime zeroTime;

        private XmlDocument standsDoc;
        private Dictionary<string, StandRecord> standMap = new Dictionary<string, StandRecord>();
        private Dictionary<string, List<StandRecord>> areaMap = new Dictionary<string, List<StandRecord>>();
        //private Dictionary<string, List<SlotRecord>> standSlotMap = new Dictionary<string, List<SlotRecord>>();
        private Dictionary<string, FlightRecord> fltMap = new Dictionary<string, FlightRecord>();

        string css;
        int minSeparation = 400;

        public GanttHTML() {

            StandRecord unallocated = new StandRecord();
            unallocated.name = "Un Allocated";
            unallocated.sortOrder = Int32.MaxValue - 1;

            List<StandRecord> u = new List<StandRecord>();
            u.Add(unallocated);
            //           areaMap.Add("Un Allocated", u);

            StandRecord noSlot = new StandRecord();
            noSlot.name = "No Slot";
            noSlot.sortOrder = Int32.MaxValue;

            List<StandRecord> s = new List<StandRecord>();
            s.Add(unallocated);
            //          areaMap.Add("No Slot", s);

            standMap.Add(unallocated.name, unallocated);
            standMap.Add(noSlot.name, noSlot);

            root = doc.CreateElement("hmtl");
            doc.AppendChild(root);
            this.head = doc.CreateElement("head");
            this.body = doc.CreateElement("body");
            this.style = doc.CreateElement("style");
            css = System.IO.File.ReadAllText(@"GanttStyle.css");
            style.InnerText = this.css;


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

            foreach (XmlNode stand in standsDoc.SelectNodes(".//FixedResource")) {
                StandRecord standRecord = new StandRecord(stand);
                standMap.Add(standRecord.id, standRecord);

                if (!areaMap.ContainsKey(standRecord.area)) {
                    areaMap.Add(standRecord.area, new List<StandRecord>());
                }
                areaMap[standRecord.area].Add(standRecord);
            }

            using (AMSIntegrationServiceClient client = new AMSIntegrationServiceClient(AMSTools.GetWSBinding(), AMSTools.GetWSEndPoint())) {

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
                    Console.WriteLine(e.Message);
                }
            }

            using (AMSIntegrationServiceClient client = new AMSIntegrationServiceClient(AMSTools.GetWSBinding(), AMSTools.GetWSEndPoint())) {


                XmlElement res = client.GetFlights(Parameters.TOKEN, DateTime.Now.AddHours(-24), DateTime.Now.AddHours(24), Parameters.APT_CODE, AirportIdentifierType.IATACode);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(res.OwnerDocument.NameTable);
                nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");

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
                                    slotRecord.slotStand = "Un Allocated";
                                    standMap[slotRecord.slotStand].slotList.Add(slotRecord);
                                    continue;
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
                            standMap["No Slot"].slotList.Add(slotRecord);

                        }
                    }
                }
            }

            DeconflictSlotOverlay();

            return true;
        }

        private void DeconflictSlotOverlay() {
            // Go through the slots for each stand and make sure they dont overlay each other 
            // By moving them to the next row if necessary.
            foreach (var pair in this.standMap) {
                StandRecord stand = pair.Value;

                // if only 0 or 1 slot, then there is no overlap, so continue;
                if (stand.slotList.Count <= 1) {
                    continue;
                }

                //Sort from lowest to highest. 
                stand.slotList.Sort((p, q) => p.left.CompareTo(q.left));

                List<SlotRecord> placedSlots = new List<SlotRecord>();

                foreach (SlotRecord slotRec in stand.slotList) {
                    if (placedSlots.Count == 0) {
                        placedSlots.Add(slotRec);
                        slotRec.row = 1;
                        continue;
                    }

                    slotRec.row = Int16.MaxValue;
                    foreach (SlotRecord slotTest in placedSlots) {
                        int minLeft = Math.Max(slotTest.left + 5 + minSeparation, slotTest.left + slotTest.width);
                        if (slotRec.left > minLeft) {
                            slotRec.row = Math.Min(slotTest.row, slotRec.row);
                        } else {
                            if (slotRec.row == slotTest.row) {
                                slotRec.row++;
                            }
                        }
                    }

                    if (slotRec.row == Int16.MaxValue) {
                        stand.numRows++;
                        slotRec.row = stand.numRows;
                    }

                    placedSlots.Add(slotRec);
                }

                stand.slotList = placedSlots;

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
                    slot.onDowngrade = true;
                }



            }

            return;
        }

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

        public XmlElement AddGanttTable(string area) {

            XmlElement gantt = doc.CreateElement("div");

            XmlElement title = doc.CreateElement("h1");
            title.InnerText = area;
            gantt.AppendChild(title);


            // The top row showing the time markers
            XmlElement timeRow = doc.CreateElement("div");
            timeRow.SetAttribute("style", "width: 100px; height: 32px; position: relative; display: flex; align-items:center; border-bottom: solid 1px gray; width:1590px");

            DateTime marker = zeroTime;


            XmlElement tableDiv = doc.CreateElement("div");
            XmlElement table = doc.CreateElement("table");
            tableDiv.AppendChild(table);
            XmlElement topTableRow = doc.CreateElement("tr");
            table.AppendChild(topTableRow);
            XmlElement cell1 = doc.CreateElement("td");
            XmlElement cell2 = doc.CreateElement("td");
            XmlElement cell3 = doc.CreateElement("td");
            XmlElement cell4 = doc.CreateElement("td");
            XmlElement cell5 = doc.CreateElement("td");
            cell1.InnerText = "Stand";
            cell2.InnerText = "Start";
            cell3.InnerText = "End";
            cell4.InnerText = "Flight Information";


            topTableRow.AppendChild(cell1);
            topTableRow.AppendChild(cell2);
            topTableRow.AppendChild(cell3);
            topTableRow.AppendChild(cell4);


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
                    dgDiv.SetAttribute("style", $"left:{left + 150}px; width:{width}px; top: 2px; height:{stand.numRows * 32 - 4}px; position:absolute; border: 1px solid black;  font-size:12px; font-family: Verdana;  padding-left:2px");

                    if (dg.fullUnavailable) {
                        dgDiv.SetAttribute("class", "downgradeFull");
                    } else {
                        dgDiv.SetAttribute("class", "downgradePartial");
                    }
                    dgDiv.InnerText = dg.ToStringPartial();
                    row.AppendChild(dgDiv);
                }

                foreach (SlotRecord slot in stand.slotList) {

                    int radius = 7;
                    if (slot.left == 0) {
                        radius = 0;
                    }

                    FlightRecord f = slot.flight;
                    // Create and Add the flight indicator if in Range
                    XmlElement flt = doc.CreateElement("div");
                    int top = 2 + (slot.row - 1) * 32;
                    flt.SetAttribute("style", $"left:{slot.left + 150}px; top:{top}px; width:{slot.width}px; border-radius:{radius}px; ");
                    if (slot.onDowngrade) {
                        flt.SetAttribute("class", "ondowngradeflight");
                    } else {
                        flt.SetAttribute("class", "flight");
                    }

                    flt.InnerText = f.ToString(fltMap);
                    row.AppendChild(flt);

                    XmlElement tableRow = doc.CreateElement("tr");
                    tableRow.SetAttribute("style", $"font-size:18px; font-family: Verdana;");
                    table.AppendChild(tableRow);
                    XmlElement cell01 = doc.CreateElement("td");
                    XmlElement cell02 = doc.CreateElement("td");
                    XmlElement cell03 = doc.CreateElement("td");
                    XmlElement cell04 = doc.CreateElement("td");
                    XmlElement cell05 = doc.CreateElement("td");
                    cell01.InnerText = stand.name;
                    cell02.InnerText = slot.slotStartDateTime.ToString();
                    cell03.InnerText = slot.slotEndDateTime.ToString(); ;
                    cell04.InnerText = f.ToString(fltMap);


                    tableRow.AppendChild(cell01);
                    tableRow.AppendChild(cell02);
                    tableRow.AppendChild(cell03);
                    tableRow.AppendChild(cell04);

                    table.AppendChild(tableRow);
                }
                gantt.AppendChild(row);
            }

            gantt.AppendChild(tableDiv);

            return gantt;
        }

        public XmlElement AddGridRow(StandRecord stand, int rowIndex) {

            XmlElement row = doc.CreateElement("div");

            int rowHeight = 32 * stand.numRows;
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

            foreach (var areaPair in areaMap) {
                body.AppendChild(AddGanttTable(areaPair.Key));
            }


            string html = doc.OuterXml.Replace("&amp;nbsp;", "&nbsp;");
            return html;
        }

    }
}

using AMSUtilLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv;

namespace AMSGet {

    public class GanttHTML {
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

        private IEnumerable<string> sets;

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

            root = doc.CreateElement("html");
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
            // using (AMSIntegrationServiceClient client = new AMSIntegrationServiceClient(AMSTools.GetWSBinding(), AMSTools.GetWSEndPoint())) {
            {
                //XmlElement res = AMSTools.GetFlightsXML(-24, 24);
                //XmlNamespaceManager nsmgr = new XmlNamespaceManager(res.OwnerDocument.NameTable);
                //nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");

                // Create the flight records and add them to the stands. Position them horizontally.

                var flts = AMSTools.GetFlightRecords(-24, 24);

                foreach (FlightRecord flight in flts) {
                    {
                        fltMap.Add(flight.flightUniqueID, flight);

                        bool noSlots = true;

                        // Iterate through each of the Stand Slots for the flight
                        foreach (ResourceRecord resRec in flight.resources) {
                            if (resRec.type != "BAY") {
                                continue;
                            }
                            noSlots = false;
                            SlotRecord slotRecord = new SlotRecord(resRec, flight);

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
                        if (noSlots) {
                            // No Stand Slot Defined for the flight
                            SlotRecord slotRecord = new SlotRecord(null, null, flight);
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
                            slot.towToStand = $"{rec.toStand}";
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
                            slot.towFromStand = $"{rec.fromStand}";
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

            if (slot.towToStand != null) {
                toTimeDiv.InnerText = $"{toTime} (";
                XmlElement d = doc.CreateElement("span");
                d.SetAttribute("style", $"font-size:10px");
                d.InnerText = "&#8614;";
                toTimeDiv.AppendChild(d);
                XmlElement d2 = doc.CreateElement("span");
                d2.InnerText = $" {slot.towToStand})";
                toTimeDiv.AppendChild(d2);
            }

            if (slot.towFromStand != null) {
                fromTimeDiv.InnerText = $"{fromTime} ({slot.towFromStand}";
                XmlElement d = doc.CreateElement("span");
                d.SetAttribute("style", $"font-size:10px");
                d.InnerText = "&#8677;";
                fromTimeDiv.AppendChild(d);
                XmlElement d2 = doc.CreateElement("span");
                d2.InnerText = $")";
                fromTimeDiv.AppendChild(d2);
            }

            FlightRecord f = slot.flight;
            int radius = 0;
            //if (slot.left == 0) {
            //    radius = 0;
            //}

            XmlElement fltDiv = doc.CreateElement("div");

            //Vertical position in the row
            int top = 2 + (slot.row - 1) * 42;
            outerDiv.SetAttribute("style", $"position:absolute; left:{slot.left + 150}px; top:{top}px;");

            int width = slot.width;

            //Correct for border width
            width = width - 2;

            //Correct width for wider right border with tows
            if (slot.towToStand != null) {
                width = width - 1;
            }

            //Horizontal position in the row
            fltDiv.SetAttribute("style", $"width:{width}px; border-radius:{radius}px; ");

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
                string clazz = "flightbasic " + f.GetCSSClass(this.fltMap);

                //if (slot.slotStand == "Unallocated") {
                //    clazz = "flightUnallocated";
                //}
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

            // 17-06-2021 Added for HIA Club Grouping Coloring
            if (!string.IsNullOrEmpty(stand.clubGrouping)) {
                titleCell.SetAttribute("class", $"standClub{stand.clubGrouping}");
            }

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
            XmlElement timeDiv = doc.CreateElement("div");
            timeDiv.InnerText = $"AMS Gantt Chart @ {DateTime.Now}";

            body.SetAttribute("style", "background:lightgray");

            body.AppendChild(timeDiv);
            body.AppendChild(GetSets());

            string html = doc.OuterXml.Replace("&amp;nbsp;", "&nbsp;")
                .Replace("&amp;#8614;", "&#8614;")
                .Replace("&amp;#8677;", "&#8677;");
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
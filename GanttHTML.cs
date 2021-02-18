using AMSUtilLib;
using System;
using System.Collections.Generic;
using System.Xml;
using WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv;

namespace AMSGet {

    class StandRecord {
        public string name;
        public string id;
        public string area;
        public int sortOrder;

        public StandRecord(XmlNode stand) {
            this.name = stand.SelectSingleNode("./Name").InnerText;
            this.id = stand.SelectSingleNode("./Id").InnerText;
            this.area = stand.SelectSingleNode("./Area").InnerText;
            this.sortOrder = int.Parse(stand.SelectSingleNode("./SortOrder").InnerText);
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



        string css = @"    .hourIndicator {
    border-left: 1px lightslategray solid;
    position: absolute;
    top: 0px;
    height:100%;

}

.odd {
   background: lightgray;
}

.even {
  background: silver;
}

.downgrade {
  background: red;
  color: white;
}
";
        public GanttHTML() {

            root = doc.CreateElement("hmtl");
            doc.AppendChild(root);
            this.head = doc.CreateElement("head");
            this.body = doc.CreateElement("body");
            this.style = doc.CreateElement("style");
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
                string uri = Parameters.AMS_REST_SERVICE_URI + $"{Parameters.APT_CODE}/Stands";
                string result = AMSTools.GetRestURI(uri).Result;
                Console.WriteLine("STATNDS RECEIVED OK");
                standsDoc = new XmlDocument();
                Console.WriteLine("STATNDS DOC CREATED OK");
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


                XmlElement res = client.GetFlights(Parameters.TOKEN, DateTime.Now, DateTime.Now.AddHours(2), Parameters.APT_CODE, AirportIdentifierType.IATACode);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(res.OwnerDocument.NameTable);
                nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");

                foreach (XmlElement el in res.SelectNodes("//ams:Flights/ams:Flight", nsmgr)) {
                    {
                        Console.WriteLine(el.OuterXml);
                        //XmlNamespaceManager nsmgr2 = new XmlNamespaceManager(el.OwnerDocument.NameTable);
                        //nsmgr2.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");
                        //XmlNode node = el.SelectSingleNode("//ams:FlightId/ams:AirlineDesignator[@codeContext='IATA']", nsmgr2);

                    }
                }
            }

            return true;
        }

        public XmlElement AddGanttTable(string area) {

            XmlElement table = doc.CreateElement("div");

            XmlElement title = doc.CreateElement("h1");
            title.InnerText = area;
            table.AppendChild(title);


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

            table.AppendChild(timeRow);

            // The individual rows for each stand

            int j = 0;
            areaMap[area].Sort((x, y) => x.sortOrder.CompareTo(y.sortOrder));
            foreach (var stand in areaMap[area]) {
                j++;
                XmlElement row = AddGridRow(stand.name, j);

                // The flight allocations for the stand
                XmlElement flt = doc.CreateElement("div");
                flt.SetAttribute("style", $"left:234px; width:150px; top: 2px; height:22px;  position:absolute; border: 1px solid black; border-radius:7px; font-size:12px; font-family: Verdana; padding-top:4px; padding-left:2px");
                flt.SetAttribute("class", "downgrade");
                flt.InnerText = "QF001/MEL/DOH/23:00";
                row.AppendChild(flt);

                table.AppendChild(row);
            }

            return table;
        }

        public XmlElement AddGridRow(string title, int rowIndex) {

            XmlElement row = doc.CreateElement("div");
            row.SetAttribute("style", "width: 100px; height: 32px; position: relative; display: flex; align-items:center; border-bottom: solid 1px gray; width:1590px");
            if (rowIndex % 2 == 0) {
                row.SetAttribute("class", "odd");
            } else {
                row.SetAttribute("class", "even");
            }
            XmlElement titleCell = doc.CreateElement("div");
            titleCell.InnerText = title;
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

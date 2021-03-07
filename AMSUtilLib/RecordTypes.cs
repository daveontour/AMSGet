using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AMSUtilLib {

    abstract public class Record {
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

    public class ResourceRecord {
        public string type;
        public string code;
        public string name;
        public string additoinal;
        public string start;
        public string stop;
        public DateTime startTime;
        public DateTime stopTime;

        public ResourceRecord(string type, string code, string name, string additoinal, string start, string stop) {
            this.type = type;
            this.code = code;
            this.name = name;
            this.additoinal = additoinal;
            this.start = start;
            this.stop = stop;

            DateTime.TryParse(start, out this.startTime);
            DateTime.TryParse(stop, out this.stopTime);
        }

        public override string ToString() {
            return $"Type: {type}, Code: {code}, Start: {startTime}, Stop: {stopTime}";
        }
    }

    public class FlightRecord : Record {
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
        public string noiseCategory;

        public bool violateRule = false;

        public List<ResourceRecord> resources = new List<ResourceRecord>();


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

            // Parse differently, depending on the format that was retrieved

            if (nsmgr.LookupNamespace("aip") == null) {
                ProcessAMSX(el, nsmgr);
            } else {
                ProcessAIP(el, nsmgr);
            }
        }

        private void ProcessAIP(XmlElement el, XmlNamespaceManager nsmgr) {
            this.airline = GetValue(el, "./aip:FlightID/aip:Airline/aip:IATA", nsmgr);
            this.fltNum = GetValue(el, "./aip:FlightID/aip:FlightNumber", nsmgr);
            this.type = GetValue(el, "./aip:FlightID/aip:FlightNature", nsmgr);
            this.sto = GetValue(el, "./aip:FlightID/aip:STO/aip:Date", nsmgr) + "T" + GetValue(el, "./aip:FlightID/aip:STO/aip:Time", nsmgr);
            this.flightUniqueID = GetValue(el, "./aip:FlightID/aip:AIPUniqueID", nsmgr);



            DateTime.TryParse(sto, out this.stoDate);

            if (type == "ARRIVAL") {
                this.route = GetValue(el, "./aip:Route/aip:Origin/aip:IATA", nsmgr);
            } else {
                this.route = GetValue(el, "./aip:Route/aip:Destination/aip:IATA", nsmgr);
            }
            this.l_airline = GetValue(el, "./aip:LinkedFlightID/aip:Airline/aip:IATA", nsmgr);
            this.l_fltNum = GetValue(el, "./aip:LinkedFlightID/aip:FlightNumber", nsmgr);
            this.l_type = GetValue(el, "./aip:LinkedFlightID/aip:FlightNature", nsmgr);

            string ld = GetValue(el, "./aip:LinkedFlightID/aip:STO/aip:Date", nsmgr);
            string lt = GetValue(el, "./aip:LinkedFlightID/aip:STO/aip:Time", nsmgr);

            this.l_flightUniqueID = GetValue(el, "./aip:LinkedFlightID/aip:AIPUniqueID", nsmgr);

            if (lt != null) {
                DateTime.TryParse($"{ld}T{lt}", out this.l_stoDate);
            } else {
                DateTime.TryParse($"{ld}", out this.l_stoDate);
            }

            foreach (XmlNode res in el.SelectNodes(".//aip:ResourceAllocation", nsmgr)) {

                string type = GetValue(res, "./aip:Resource/aip:ResourceType", nsmgr);
                string code = GetValue(res, "./aip:Resource/aip:Code", nsmgr);
                string name = null;
                string additional = null;
                string start = GetValue(res, "./aip:TimeSlot/aip:Start", nsmgr);
                string stop = GetValue(res, "./aip:TimeSlot/aip:Stop", nsmgr);

                resources.Add(new ResourceRecord(type, code, name, additional, start, stop));
            }


            this.actype = GetValue(el, "./aip:PrimaryAircraft/aip:Type/aip:IATA", nsmgr);
            this.reg = GetValue(el, "./aip:PrimaryAircraft/aip:Registration", nsmgr);
        }

        private void ProcessAMSX(XmlElement el, XmlNamespaceManager nsmgr) {
            this.airline = GetValue(el, "./ams:FlightId/ams:AirlineDesignator[@codeContext='IATA']", nsmgr);
            this.fltNum = GetValue(el, "./ams:FlightId/ams:FlightNumber", nsmgr);
            this.type = GetValue(el, "./ams:FlightId/ams:FlightKind", nsmgr);
            this.sto = GetValue(el, "./ams:FlightState/ams:ScheduledTime", nsmgr);
            this.flightUniqueID = GetValue(el, "./ams:FlightState/ams:Value[@propertyName='FlightUniqueID']", nsmgr);

            this.violateRule = bool.Parse(GetValue(el, "./ams:FlightState/ams:Value[@propertyName='B---_Violate_Rule']", nsmgr));

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

            foreach (XmlNode res in el.SelectNodes(".//ams:StandSlots", nsmgr)) {

                string type = "BAY";
                string code = GetValue(res, ".//ams:Stand/ams:Value[@propertyName='Name']", nsmgr);
                string name = null;
                string additional = null;
                string start = GetValue(res, ".//ams:Value[@propertyName='StartTime']", nsmgr);
                string stop = GetValue(res, ".//ams:Value[@propertyName='EndTime']", nsmgr);

                resources.Add(new ResourceRecord(type, code, name, additional, start, stop));
            }

        }

        public bool IsLinked() {
            if (l_flightUniqueID != null) {
                return true;
            } else {
                return false;
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

        public string GetCSSClass(Dictionary<string, FlightRecord> fltMap) {

            if (actype == "388" || actype == "74N") {
                if (this.violateRule) {
                    return "codeFAlert";
                }
                if (fltMap.ContainsKey(l_flightUniqueID)) {
                    if (fltMap[l_flightUniqueID].violateRule) {
                        return "codeFAlert";
                    }
                }
            }
            if (actype == "32A" || actype == "32B") {
                if (this.violateRule) {
                    return "sharkletAlert";
                }
                if (l_flightUniqueID != null) {
                    if (fltMap.ContainsKey(l_flightUniqueID)) {
                        if (fltMap[l_flightUniqueID].violateRule) {
                            return "sharkletAlert";
                        }
                    }
                }
            }
            {
                if (this.violateRule && this.noiseCategory == "9") {
                    return "ruleEViolation";
                }
                if (l_flightUniqueID != null) {
                    if (fltMap.ContainsKey(l_flightUniqueID)) {
                        if (fltMap[l_flightUniqueID].violateRule && fltMap[l_flightUniqueID].noiseCategory == "9") {
                            return "ruleEViolation";
                        }
                    }
                }
            }
            if (actype == "788"
                || actype == "333"
                || actype == "789"
                || actype == "332"
                || actype == "77L"
                || actype == "77W"
                || actype == "346"
                || actype == "313"
                || actype == "772"
                || actype == "773"
                || actype == "359"
                || actype == "351") {
                return "icaoCodeE";
            }
            if (actype == "320" || actype == "321") {
                return "icaoCodeC";
            }
            if (actype == "32A" || actype == "32B") {
                return "icaoCodeCSharklet";
            }
            if (actype == "388") {
                return "icaoCodeF";
            }
            if (actype == "33X" || actype == "77X") {
                return "icaoCodeECargo";
            }
            if (actype == "74N") {
                return "icaoCodeFCargo";
            }
            if (IsLinked()) {
                return "flightLinked";
            }
            return "flightUnlinked";
        }
        public override string ToString() {
            string f = $"<{airline}{fltNum}@{stoDate}/ {type} /{route} / {reg} / {actype}> <{l_airline}{l_fltNum}@{l_stoDate}/ {l_type} /{route} / {reg} / {actype}>";
            foreach (ResourceRecord rec in resources) {
                f = f + "\n" + rec;
            }

            return f;
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

    public class DownGradeRecord : Record {
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

    public class StandRecord : Record {
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

    public class SlotRecord : Record {
        public string slotStart;
        public DateTime slotStartDateTime;
        public string slotEnd;
        public DateTime slotEndDateTime;
        public string slotStand;
        public FlightRecord flight;
        public int left;
        public int width;
        public int row;
        public bool onDowngrade = false;

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
        public SlotRecord(ResourceRecord rec, FlightRecord flight) {
            this.slotStand = rec.code;
            this.slotStartDateTime = rec.startTime;
            this.slotEndDateTime = rec.stopTime;
            this.flight = flight;
        }
    }

    public class TowRecord : Record {
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

    public class Bucket {

        private int minSlotLength = 350;
        public List<List<SlotRecord>> bucket = new List<List<SlotRecord>>();

        public Bucket() { }

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

        public int GetRows() {
            return bucket.Count;
        }
    }

}

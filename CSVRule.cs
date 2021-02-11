using System;

namespace AMSGet {
    public enum BaseType {
        Towing,
        Flight,
        Airline,
        Aircraft,
        Airport,
        AircraftType,
        Gate,
        Checkin,
        Stand,
        Carousel,
        None
    }
    public class CSVRule {
        public BaseType type;
        public string header;
        public string xpath;
        public bool valid = true;

        public CSVRule(string[] entries) {
            try {
                switch (entries[0]) {
                    case "Towing":
                        type = BaseType.Towing;
                        break;
                    case "Flight":
                        type = BaseType.Flight;
                        break;
                    case "Airline":
                        type = BaseType.Airline;
                        break;
                    default:
                        type = BaseType.None;
                        break;
                }

                header = entries[1];
                xpath = entries[2];
            } catch (Exception) {
                valid = false;
            }
        }
    }

}

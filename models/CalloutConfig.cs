using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fivepd_json.models
{
    public class CalloutConfig
    {
        public string shortName { get; set; }
        public string description { get; set; }
        public int responseCode { get; set; }
        public string weapon { get; set; }
        public string pedModel { get; set; }
        public string behavior { get; set; }
        public string vehicleModel { get; set; }
        public float heading { get; set; }
        public bool autoEnd { get; set; } = true;
        public bool pursuit { get; set; } = true;
        public float startDistance { get; set; }
        public List<SuspectConfig> suspects { get; set; }
        public List<VictimConfig> victims { get; set; }
        public LocationData location { get; set; }
        public List<LocationData> locations { get; set; }
    }
}

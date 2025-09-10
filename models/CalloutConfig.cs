using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fivepd_json.models
{
    public class CalloutConfig
    {
        public string updateURL { get; set; }
        public string version { get; set; }
        public string shortName { get; set; }
        public string description { get; set; }
        public int responseCode { get; set; }
        public string weapon { get; set; }
        public string pedModel { get; set; }
        public bool hasBlip { get; set; }
        public string blipColor { get; set; }
        public string behavior { get; set; }
        public string vehicleModel { get; set; }
        public bool vehiclehasBlip { get; set; }
        public float heading { get; set; }
        public bool autoEnd { get; set; }
        public float startDistance { get; set; }
        public bool debug { get; set; }
        public bool CFGGen { get; set; } // Internal to easily generate config files if needed
        public List<SuspectConfig> suspects { get; set; }
        public List<VictimConfig> victims { get; set; }
        public LocationData location { get; set; }
        public List<LocationData> locations { get; set; }
        public List<PedQuestionConfig> questions { get; set; }

    }
}

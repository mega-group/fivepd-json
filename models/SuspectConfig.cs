using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fivepd_json.models
{
    public class SuspectConfig
    {
        public string pedModel { get; set; }
        public string weapon { get; set; }
        public string vehicleModel { get; set; }
        public float heading { get; set; } = 0f;
        public string behavior { get; set; }
        public string vehicleId { get; set; }
        public int? seatIndex { get; set; }
        public bool? excludeFromTrafficStop { get; set; }


    }
}

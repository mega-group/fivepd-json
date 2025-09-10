using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;

namespace fivepd_json.models
{
    public class SuspectConfig
    {
        public string pedModel { get; set; }
        public string weapon { get; set; }
        public string vehicleModel { get; set; }
        public bool vehiclehasBlip { get; set; } = false;
        public float heading { get; set; } = 0f;
        public string behavior { get; set; }
        public bool pursuit { get; set; }
        public string vehicleId { get; set; }
        public int? seatIndex { get; set; }
        public bool? excludeFromTrafficStop { get; set; }
        public bool hasBlip { get; set; } = false;
        public string blipColor { get; set; } = "Red";
        public List<PedQuestionConfig> questions { get; set; }
        public VehicleDataConfig vehicleData { get; set; }
        public PedDataConfig PedData { get; set; }
    }
}

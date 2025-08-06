using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fivepd_json.models
{

        public class VehicleDataConfig 
        {
          public List<VehicleItems> items { get; set; }
          public string licensePlate { get; set; }
          public bool? insurance { get; set; }
          public bool? registration { get; set; }
    }

    public class VehicleItems
    {
        public string Name { get; set; }
        public bool IsIllegal { get; set; }
    }
}

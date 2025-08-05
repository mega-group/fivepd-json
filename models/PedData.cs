using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fivepd_json.net.models
{
        public class PedDataConfig
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string dateOfBirth { get; set; }
            public string warrant { get; set; }
            public string address { get; set; }
            public string gender { get; set; }
            public int? age { get; set; }
            public double? bloodAlcoholLevel { get; set; }
            public bool[] usedDrugs { get; set; }

            public LicenseConfig driverLicense { get; set; }
            public LicenseConfig weaponLicense { get; set; }
            public LicenseConfig huntingLicense { get; set; }
            public LicenseConfig fishingLicense { get; set; }

            public List<ItemConfig> items { get; set; }
            public List<ViolationConfig> violations { get; set; }
        }

        public class LicenseConfig
        {
            public string expiration { get; set; }
            public string licenseStatus { get; set; }
        }

        public class ViolationConfig
        {
            public string Offence { get; set; }
            public string Charge { get; set; }
        }
        public class ItemConfig
        {
            public string Name { get; set; }
            public bool IsIllegal { get; set; }
        }
    }
